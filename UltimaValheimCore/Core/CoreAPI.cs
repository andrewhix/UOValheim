using System;
using BepInEx.Logging;

namespace UltimaValheim.Core
{
    /// <summary>
    /// Central API providing access to all Core subsystems.
    /// This is the main interface that Sidecar modules use to interact with the Core.
    /// </summary>
    public static class CoreAPI
    {
        /// <summary>
        /// BepInEx logger for Core and modules
        /// </summary>
        public static ManualLogSource Log { get; private set; }

        /// <summary>
        /// Global event bus for inter-module communication
        /// </summary>
        public static EventBus Events { get; private set; }

        /// <summary>
        /// Event router for game lifecycle events (player join/leave, save, etc.)
        /// </summary>
        public static CoreEventRouter Router { get; private set; }

        /// <summary>
        /// Network manager for RPC registration and multiplayer sync
        /// </summary>
        public static NetworkManager Network { get; private set; }

        /// <summary>
        /// Persistence manager for saving/loading player and world data
        /// </summary>
        public static PersistenceManager Persistence { get; private set; }

        /// <summary>
        /// Configuration manager for BepInEx config integration
        /// </summary>
        public static ConfigManager Config { get; private set; }

        /// <summary>
        /// Player data manager for ServerCharacters integration
        /// </summary>
        public static PlayerDataManager PlayerData { get; private set; }

        /// <summary>
        /// Skills manager for Valheim skill UI integration
        /// </summary>
        public static SkillsManager Skills { get; private set; }

        /// <summary>
        /// Whether the Core systems have been fully initialized
        /// </summary>
        public static bool IsReady { get; private set; }

        /// <summary>
        /// Initialize all Core systems. Called once by the main plugin.
        /// </summary>
        internal static void Initialize(ManualLogSource log)
        {
            if (IsReady)
            {
                log.LogWarning("[CoreAPI] Attempted to initialize Core twice!");
                return;
            }

            Log = log;

            // Initialize all managers in dependency order
            Config = new ConfigManager();
            Events = new EventBus();
            Router = new CoreEventRouter();
            Network = new NetworkManager();
            Persistence = new PersistenceManager();
            PlayerData = new PlayerDataManager();
            Skills = new SkillsManager();

            Log.LogInfo("[CoreAPI] All core systems initialized.");

            IsReady = true;

            // Notify all registered modules that Core is ready
            CoreLifecycle.NotifyCoreReady();

            // Initialize network system when ZNet is available
            if (ZNet.instance != null && ZRoutedRpc.instance != null)
            {
                Network.InitializeNetwork();
            }
        }

        /// <summary>
        /// Register a Sidecar module with the Core.
        /// </summary>
        public static void RegisterModule(ICoreModule module)
        {
            if (module == null)
            {
                Log?.LogWarning("[CoreAPI] Attempted to register null module!");
                return;
            }

            CoreLifecycle.RegisterModule(module);
            Log?.LogInfo($"[CoreAPI] Registered module: {module.ModuleID} v{module.ModuleVersion}");
        }
    }
}