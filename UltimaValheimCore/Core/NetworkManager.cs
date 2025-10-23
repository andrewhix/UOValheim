using System;
using System.Collections.Generic;

namespace UltimaValheim.Core
{
    /// <summary>
    /// Manages network RPC registration and routing for modules.
    /// Wraps Valheim's ZRoutedRpc system with module namespacing.
    /// </summary>
    public class NetworkManager
    {
        private readonly Dictionary<string, Action<ZRpc, ZPackage>> _registeredRPCs = new Dictionary<string, Action<ZRpc, ZPackage>>();
        private bool _isServerInitialized = false;

        public NetworkManager()
        {
            // Hook into ZNet to detect server initialization
            ZNet.m_onNewConnection += OnNewConnection;
        }

        /// <summary>
        /// Register a named RPC handler for a module.
        /// Module name is automatically prefixed for namespacing.
        /// </summary>
        /// <param name="moduleID">Module identifier (e.g., "UltimaValheim.Skills")</param>
        /// <param name="rpcName">Name of the RPC (e.g., "SyncSkillXP")</param>
        /// <param name="handler">Handler to process incoming RPC</param>
        public void RegisterRPC(string moduleID, string rpcName, Action<ZRpc, ZPackage> handler)
        {
            if (string.IsNullOrEmpty(moduleID) || string.IsNullOrEmpty(rpcName) || handler == null)
            {
                CoreAPI.Log.LogWarning("[NetworkManager] Invalid RPC registration parameters!");
                return;
            }

            string fullName = GetNamespacedRPCName(moduleID, rpcName);

            if (_registeredRPCs.ContainsKey(fullName))
            {
                CoreAPI.Log.LogWarning($"[NetworkManager] RPC '{fullName}' is already registered!");
                return;
            }

            _registeredRPCs[fullName] = handler;

            // If server is already running, register immediately
            if (_isServerInitialized && ZRoutedRpc.instance != null)
            {
                RegisterWithZNet(fullName, handler);
            }

            CoreAPI.Log.LogInfo($"[NetworkManager] Registered RPC: {fullName}");
        }

        /// <summary>
        /// Send an RPC to a specific peer.
        /// </summary>
        public void SendToPeer(ZNetPeer peer, string moduleID, string rpcName, ZPackage package)
        {
            if (peer == null || package == null)
            {
                CoreAPI.Log.LogWarning("[NetworkManager] Cannot send RPC with null peer or package!");
                return;
            }

            string fullName = GetNamespacedRPCName(moduleID, rpcName);
            
            if (ZRoutedRpc.instance != null)
            {
                ZRoutedRpc.instance.InvokeRoutedRPC(peer.m_uid, fullName, package);
                CoreAPI.Log.LogDebug($"[NetworkManager] Sent RPC '{fullName}' to peer {peer.m_uid}");
            }
            else
            {
                CoreAPI.Log.LogWarning($"[NetworkManager] Cannot send RPC '{fullName}' - ZRoutedRpc not initialized!");
            }
        }

        /// <summary>
        /// Send an RPC to all connected peers (broadcast).
        /// </summary>
        public void SendToAll(string moduleID, string rpcName, ZPackage package)
        {
            if (package == null)
            {
                CoreAPI.Log.LogWarning("[NetworkManager] Cannot send RPC with null package!");
                return;
            }

            string fullName = GetNamespacedRPCName(moduleID, rpcName);

            if (ZNet.instance == null)
            {
                CoreAPI.Log.LogWarning($"[NetworkManager] Cannot broadcast RPC '{fullName}' - ZNet not initialized!");
                return;
            }

            foreach (ZNetPeer peer in ZNet.instance.GetConnectedPeers())
            {
                if (peer != null && ZRoutedRpc.instance != null)
                {
                    ZRoutedRpc.instance.InvokeRoutedRPC(peer.m_uid, fullName, package);
                }
            }

            CoreAPI.Log.LogDebug($"[NetworkManager] Broadcast RPC '{fullName}' to all peers");
        }

        /// <summary>
        /// Send an RPC to the server (from client).
        /// </summary>
        public void SendToServer(string moduleID, string rpcName, ZPackage package)
        {
            if (package == null)
            {
                CoreAPI.Log.LogWarning("[NetworkManager] Cannot send RPC with null package!");
                return;
            }

            if (ZNet.instance == null || !ZNet.instance.IsServer())
            {
                string fullName = GetNamespacedRPCName(moduleID, rpcName);
                
                if (ZRoutedRpc.instance != null)
                {
                    ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.instance.GetServerPeerID(), fullName, package);
                    CoreAPI.Log.LogDebug($"[NetworkManager] Sent RPC '{fullName}' to server");
                }
                else
                {
                    CoreAPI.Log.LogWarning($"[NetworkManager] Cannot send to server - ZRoutedRpc not initialized!");
                }
            }
            else
            {
                CoreAPI.Log.LogDebug("[NetworkManager] Already on server, skipping SendToServer");
            }
        }

        /// <summary>
        /// Check if we're currently the server.
        /// </summary>
        public bool IsServer()
        {
            return ZNet.instance != null && ZNet.instance.IsServer();
        }

        /// <summary>
        /// Check if we're connected to a server.
        /// </summary>
        public bool IsConnected()
        {
            return ZNet.instance != null && ZNet.instance.IsConnected();
        }

        #region Internal Methods

        private string GetNamespacedRPCName(string moduleID, string rpcName)
        {
            return $"{moduleID}.{rpcName}";
        }

        private void RegisterWithZNet(string fullRPCName, Action<ZRpc, ZPackage> handler)
        {
            if (ZRoutedRpc.instance == null)
            {
                CoreAPI.Log.LogWarning($"[NetworkManager] Cannot register '{fullRPCName}' - ZRoutedRpc not available!");
                return;
            }

            try
            {
                ZRoutedRpc.instance.Register(fullRPCName, handler);
                CoreAPI.Log.LogDebug($"[NetworkManager] Registered RPC with ZNet: {fullRPCName}");
            }
            catch (Exception ex)
            {
                CoreAPI.Log.LogError($"[NetworkManager] Failed to register RPC '{fullRPCName}': {ex}");
            }
        }

        private void OnNewConnection(ZNetPeer peer)
        {
            if (!_isServerInitialized && ZRoutedRpc.instance != null)
            {
                _isServerInitialized = true;
                
                // Register all pending RPCs with ZNet
                foreach (var kvp in _registeredRPCs)
                {
                    RegisterWithZNet(kvp.Key, kvp.Value);
                }

                CoreAPI.Log.LogInfo($"[NetworkManager] Server initialized, registered {_registeredRPCs.Count} RPC(s)");
            }
        }

        #endregion
    }
}
