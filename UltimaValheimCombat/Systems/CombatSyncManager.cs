using System;
using System.Collections.Generic;
using UnityEngine;
using UltimaValheim.Core;

namespace UltimaValheim.Combat.Systems
{
    /// <summary>
    /// Manages network synchronization for combat with batching and spatial culling.
    /// Critical for performance in large-scale PvP.
    /// </summary>
    public class CombatSyncManager
    {
        private readonly string _moduleID;
        private readonly float _syncInterval;
        private readonly float _syncRadius;
        private readonly bool _batchingEnabled;

        // Pending damage updates: targetID -> accumulated damage
        private readonly Dictionary<long, float> _pendingDamage = new Dictionary<long, float>();
        private float _lastSyncTime = 0f;

        // Track combat events for throttling
        private float _lastCombatEvent = 0f;
        private const float COMBAT_EVENT_COOLDOWN = 0.5f;

        public CombatSyncManager(string moduleID, float syncInterval, float syncRadius, bool batchingEnabled)
        {
            _moduleID = moduleID;
            _syncInterval = syncInterval;
            _syncRadius = syncRadius;
            _batchingEnabled = batchingEnabled;

            CoreAPI.Log.LogInfo($"[CombatSyncManager] Initialized - Batching: {batchingEnabled}, Interval: {syncInterval}s, Radius: {syncRadius}m");
        }

        /// <summary>
        /// Queue damage for batched sync (called from damage calculation)
        /// </summary>
        public void QueueDamage(Character target, float damage)
        {
            if (!_batchingEnabled)
            {
                // Batching disabled, sync immediately
                SyncDamageImmediate(target, damage);
                return;
            }

            // FIXED: Proper way to get ZDO from Character
            ZNetView nview = target.GetComponent<ZNetView>();
            if (nview == null || !nview.IsValid())
                return;

            long targetID = nview.GetZDO().m_uid.ID;
            if (targetID == 0)
                return;

            lock (_pendingDamage)
            {
                if (!_pendingDamage.ContainsKey(targetID))
                {
                    _pendingDamage[targetID] = 0f;
                }
                _pendingDamage[targetID] += damage;
            }
        }

        /// <summary>
        /// Update method - call from Player.Update patch or similar
        /// </summary>
        public void Update()
        {
            if (!_batchingEnabled)
                return;

            if (Time.time - _lastSyncTime >= _syncInterval)
            {
                SyncBatchedDamage();
                _lastSyncTime = Time.time;
            }
        }

        /// <summary>
        /// Sync accumulated damage to network
        /// </summary>
        private void SyncBatchedDamage()
        {
            Dictionary<long, float> damageToBroadcast;
            
            lock (_pendingDamage)
            {
                if (_pendingDamage.Count == 0)
                    return;

                // Copy pending damage and clear
                damageToBroadcast = new Dictionary<long, float>(_pendingDamage);
                _pendingDamage.Clear();
            }

            // Get package from pool
            ZPackage pkg = NetworkManager.GetPackage();
            try
            {
                // Write batch data
                pkg.Write(damageToBroadcast.Count);
                foreach (var kvp in damageToBroadcast)
                {
                    pkg.Write(kvp.Key);   // Target ID
                    pkg.Write(kvp.Value); // Total damage
                }

                // Send with spatial culling
                BroadcastWithSpatialCulling(pkg, Vector3.zero); // Will need actual combat position
            }
            finally
            {
                NetworkManager.ReturnPackage(pkg);
            }
        }

        /// <summary>
        /// Immediate sync (when batching is disabled)
        /// </summary>
        private void SyncDamageImmediate(Character target, float damage)
        {
            // FIXED: Proper way to get ZDO from Character
            ZNetView nview = target.GetComponent<ZNetView>();
            if (nview == null || !nview.IsValid())
                return;

            long targetID = nview.GetZDO().m_uid.ID;
            if (targetID == 0)
                return;

            ZPackage pkg = NetworkManager.GetPackage();
            try
            {
                pkg.Write(1); // Count
                pkg.Write(targetID);
                pkg.Write(damage);

                Vector3 position = target.transform.position;
                BroadcastWithSpatialCulling(pkg, position);
            }
            finally
            {
                NetworkManager.ReturnPackage(pkg);
            }
        }

        /// <summary>
        /// Broadcast package only to players within sync radius (spatial culling)
        /// </summary>
        private void BroadcastWithSpatialCulling(ZPackage package, Vector3 position)
        {
            if (ZNet.instance == null)
                return;

            List<ZNetPeer> peers = ZNet.instance.GetConnectedPeers();
            if (peers == null || peers.Count == 0)
                return;

            foreach (ZNetPeer peer in peers)
            {
                if (peer == null)
                    continue;

                // Get player for this peer
                Player player = Player.GetPlayer(peer.m_uid);
                if (player == null)
                    continue;

                // Check distance (spatial culling)
                float distance = Vector3.Distance(player.transform.position, position);
                if (distance <= _syncRadius)
                {
                    // Within range - send update
                    CoreAPI.Network.SendToPeer(peer, _moduleID, "BatchDamageSync", package);
                }
            }
        }

        /// <summary>
        /// Receive batched damage sync from network
        /// </summary>
        public void ReceiveBatchDamage(ZPackage package)
        {
            try
            {
                int count = package.ReadInt();
                
                for (int i = 0; i < count; i++)
                {
                    long targetID = package.ReadLong();
                    float damage = package.ReadSingle();

                    // Apply damage to target
                    ApplyReceivedDamage(targetID, damage);
                }
            }
            catch (Exception ex)
            {
                CoreAPI.Log.LogError($"[CombatSyncManager] Error receiving batch damage: {ex}");
            }
        }

        /// <summary>
        /// Apply received damage to a character
        /// </summary>
        private void ApplyReceivedDamage(long targetID, float damage)
        {
            // Find character by ZDO ID
            ZDO zdo = ZDOMan.instance?.GetZDO(new ZDOID(targetID, 0));
            if (zdo == null)
                return;

            // Get character from ZDO
            Character character = ZNetScene.instance?.FindInstance(zdo)?.GetComponent<Character>();
            if (character == null)
                return;

            // Apply damage
            HitData hit = new HitData
            {
                m_damage = new HitData.DamageTypes { m_damage = damage },
                m_point = character.GetCenterPoint()
            };

            character.ApplyDamage(hit, true, true);
        }

        /// <summary>
        /// Publish throttled combat event
        /// </summary>
        public void PublishCombatEvent(string eventName, params object[] args)
        {
            if (Time.time - _lastCombatEvent < COMBAT_EVENT_COOLDOWN)
                return; // Throttled

            CoreAPI.Events.Publish(eventName, args);
            _lastCombatEvent = Time.time;
        }

        /// <summary>
        /// Shutdown and cleanup
        /// </summary>
        public void Shutdown()
        {
            lock (_pendingDamage)
            {
                _pendingDamage.Clear();
            }
        }

        /// <summary>
        /// Get sync statistics (for monitoring/debugging)
        /// </summary>
        public (int PendingDamageCount, float LastSyncTime) GetSyncStats()
        {
            lock (_pendingDamage)
            {
                return (_pendingDamage.Count, _lastSyncTime);
            }
        }
    }
}
