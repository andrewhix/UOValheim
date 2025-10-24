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
        // Store handlers in Valheim's native format (long peer ID, not ZRpc)
        private readonly Dictionary<string, Action<long, ZPackage>> _registeredRPCs = new Dictionary<string, Action<long, ZPackage>>();
        private bool _isServerInitialized = false;

        public NetworkManager()
        {
            // Network initialization will happen when Core is ready
            CoreAPI.Log?.LogInfo("[NetworkManager] Initialized.");
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

            // Wrap the user's handler (which expects ZRpc) to work with Valheim's API (which provides long peer ID)
            Action<long, ZPackage> wrappedHandler = (senderPeerID, package) =>
            {
                // Convert peer ID to ZRpc for the module's handler
                ZNetPeer peer = ZNet.instance?.GetPeer(senderPeerID);
                if (peer != null && peer.m_rpc != null)
                {
                    handler.Invoke(peer.m_rpc, package);
                }
                else
                {
                    CoreAPI.Log.LogWarning($"[NetworkManager] Could not get ZRpc for peer {senderPeerID} in RPC '{fullName}'");
                }
            };

            _registeredRPCs[fullName] = wrappedHandler;

            // If server is already running, register immediately
            if (_isServerInitialized && ZRoutedRpc.instance != null)
            {
                RegisterWithZNet(fullName, wrappedHandler);
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
            return ZNet.instance != null && ZNet.instance.IsServer() || (ZNet.instance != null && ZNet.instance.GetPeer(ZRoutedRpc.Everybody) != null);
        }

        #region Internal Methods

        private string GetNamespacedRPCName(string moduleID, string rpcName)
        {
            return $"{moduleID}.{rpcName}";
        }

        private void RegisterWithZNet(string fullRPCName, Action<long, ZPackage> handler)
        {
            if (ZRoutedRpc.instance == null)
            {
                CoreAPI.Log.LogWarning($"[NetworkManager] Cannot register '{fullRPCName}' - ZRoutedRpc not available!");
                return;
            }

            try
            {
                // Register with Valheim's native signature - handler is already wrapped
                ZRoutedRpc.instance.Register<ZPackage>(fullRPCName, handler);

                CoreAPI.Log.LogDebug($"[NetworkManager] Registered RPC with ZNet: {fullRPCName}");
            }
            catch (Exception ex)
            {
                CoreAPI.Log.LogError($"[NetworkManager] Failed to register RPC '{fullRPCName}': {ex}");
            }
        }

        /// <summary>
        /// Initialize network system when Valheim's network is ready.
        /// Called by Core when ZNet is available.
        /// </summary>
        internal void InitializeNetwork()
        {
            if (_isServerInitialized)
                return;

            if (ZRoutedRpc.instance != null)
            {
                _isServerInitialized = true;

                // Register all pending RPCs with ZNet
                foreach (var kvp in _registeredRPCs)
                {
                    RegisterWithZNet(kvp.Key, kvp.Value);
                }

                CoreAPI.Log.LogInfo($"[NetworkManager] Network initialized, registered {_registeredRPCs.Count} RPC(s)");
            }
        }

        #endregion
    }
}
