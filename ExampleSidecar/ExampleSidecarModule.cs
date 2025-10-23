using System;
using System.Collections.Generic;
using UltimaValheim.Core;

namespace UltimaValheim.Example
{
    /// <summary>
    /// Example Sidecar module demonstrating Core integration.
    /// This module tracks player login times and synchronizes them across the network.
    /// </summary>
    public class ExampleSidecarModule : ICoreModule
    {
        public string ModuleID => "UltimaValheim.Example";
        public Version ModuleVersion => new Version(1, 0, 0);

        private readonly Dictionary<long, DateTime> _playerLoginTimes = new Dictionary<long, DateTime>();
        private BepInEx.Configuration.ConfigEntry<bool> _enableLogging;
        private BepInEx.Configuration.ConfigEntry<int> _maxPlayersToTrack;

        public void OnCoreReady()
        {
            CoreAPI.Log.LogInfo($"[{ModuleID}] Module initializing...");

            // 1. Load configuration
            _enableLogging = CoreAPI.Config.Bind(
                ModuleID,
                "EnableLogging",
                true,
                "Enable detailed logging for this module"
            );

            _maxPlayersToTrack = CoreAPI.Config.Bind(
                ModuleID,
                "MaxPlayersToTrack",
                100,
                "Maximum number of player login times to track"
            );

            // 2. Subscribe to Core events
            CoreAPI.Events.Subscribe<Player>("OnPlayerJoin", HandlePlayerJoinEvent);
            CoreAPI.Events.Subscribe<Player>("OnPlayerLeave", HandlePlayerLeaveEvent);

            // 3. Register network RPCs (if needed for multiplayer sync)
            CoreAPI.Network.RegisterRPC(ModuleID, "SyncLoginTime", HandleSyncLoginTime);

            // 4. Load persisted data
            LoadPersistedData();

            CoreAPI.Log.LogInfo($"[{ModuleID}] Module initialized successfully!");
        }

        public void OnPlayerJoin(Player player)
        {
            if (player == null)
                return;

            long playerID = player.GetPlayerID();
            DateTime loginTime = DateTime.UtcNow;

            // Track login time
            _playerLoginTimes[playerID] = loginTime;

            if (_enableLogging.Value)
            {
                CoreAPI.Log.LogInfo($"[{ModuleID}] Player {player.GetPlayerName()} ({playerID}) joined at {loginTime:HH:mm:ss}");
            }

            // Sync to other clients if we're the server
            if (CoreAPI.Network.IsServer())
            {
                SyncLoginTimeToClients(playerID, loginTime);
            }

            // Publish custom event for other modules
            CoreAPI.Events.Publish("OnPlayerLoginTracked", player, loginTime);
        }

        public void OnPlayerLeave(Player player)
        {
            if (player == null)
                return;

            long playerID = player.GetPlayerID();

            if (_playerLoginTimes.ContainsKey(playerID))
            {
                DateTime loginTime = _playerLoginTimes[playerID];
                TimeSpan sessionDuration = DateTime.UtcNow - loginTime;

                if (_enableLogging.Value)
                {
                    CoreAPI.Log.LogInfo($"[{ModuleID}] Player {player.GetPlayerName()} played for {sessionDuration.TotalMinutes:F1} minutes");
                }

                // Could publish session stats for other modules
                CoreAPI.Events.Publish("OnPlayerSessionEnd", player, sessionDuration);
            }
        }

        public void OnSave()
        {
            if (_enableLogging.Value)
            {
                CoreAPI.Log.LogInfo($"[{ModuleID}] Saving data...");
            }

            // Save player login times to persistence
            foreach (var kvp in _playerLoginTimes)
            {
                CoreAPI.Persistence.SavePlayerData(ModuleID, kvp.Key, new PlayerLoginData
                {
                    LastLoginTime = kvp.Value,
                    TotalLogins = GetTotalLogins(kvp.Key) + 1
                });
            }

            // Persist to disk
            CoreAPI.Persistence.SaveToDisk();
        }

        public void OnShutdown()
        {
            CoreAPI.Log.LogInfo($"[{ModuleID}] Shutting down...");

            // Cleanup
            _playerLoginTimes.Clear();

            // Unsubscribe from events (optional, but good practice)
            CoreAPI.Events.Unsubscribe<Player>("OnPlayerJoin", HandlePlayerJoinEvent);
            CoreAPI.Events.Unsubscribe<Player>("OnPlayerLeave", HandlePlayerLeaveEvent);
        }

        #region Private Methods

        private void HandlePlayerJoinEvent(Player player)
        {
            // Additional event-specific logic
            // This demonstrates using the EventBus in addition to ICoreModule lifecycle
        }

        private void HandlePlayerLeaveEvent(Player player)
        {
            // Additional event-specific logic
        }

        private void SyncLoginTimeToClients(long playerID, DateTime loginTime)
        {
            ZPackage package = new ZPackage();
            package.Write(playerID);
            package.Write(loginTime.ToBinary());

            CoreAPI.Network.SendToAll(ModuleID, "SyncLoginTime", package);
        }

        private void HandleSyncLoginTime(ZRpc rpc, ZPackage package)
        {
            try
            {
                long playerID = package.ReadLong();
                long timeBinary = package.ReadLong();
                DateTime loginTime = DateTime.FromBinary(timeBinary);

                _playerLoginTimes[playerID] = loginTime;

                if (_enableLogging.Value)
                {
                    CoreAPI.Log.LogInfo($"[{ModuleID}] Synced login time for player {playerID}");
                }
            }
            catch (Exception ex)
            {
                CoreAPI.Log.LogError($"[{ModuleID}] Error handling SyncLoginTime RPC: {ex}");
            }
        }

        private void LoadPersistedData()
        {
            // Load saved data for all players
            // In a real implementation, you'd iterate through saved player data
            CoreAPI.Log.LogInfo($"[{ModuleID}] Loaded persisted data");
        }

        private int GetTotalLogins(long playerID)
        {
            var data = CoreAPI.Persistence.LoadPlayerData<PlayerLoginData>(ModuleID, playerID);
            return data?.TotalLogins ?? 0;
        }

        #endregion

        #region Data Classes

        [Serializable]
        private class PlayerLoginData
        {
            public DateTime LastLoginTime { get; set; }
            public int TotalLogins { get; set; }
        }

        #endregion
    }
}
