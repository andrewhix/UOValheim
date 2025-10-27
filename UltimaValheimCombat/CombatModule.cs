using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using UltimaValheim.Core;
using UltimaValheim.Combat.Data;
using UltimaValheim.Combat.Systems;
using HarmonyLib;

namespace UltimaValheim.Combat
{
    /// <summary>
    /// Core Combat Module - manages weapons, damage calculation, and combat systems.
    /// Implements ICoreModule for lifecycle integration with Ultima Valheim Core.
    /// </summary>
    public class CombatModule : ICoreModule
    {
        #region Module Properties

        public string ModuleID => "UltimaValheim.Combat";
        public System.Version ModuleVersion => new System.Version(1, 0, 0);

        #endregion

        #region Internal Systems

        private WeaponManager _weaponManager;
        private DamageCalculator _damageCalculator;
        private CombatSyncManager _syncManager;
        private WeaponDatabase _weaponDatabase;
        private Harmony _harmony;

        #endregion

        #region Configuration

        private ConfigEntry<bool> _enableDamageBatching;
        private ConfigEntry<float> _damageSyncInterval;
        private ConfigEntry<float> _syncRadius;
        private ConfigEntry<bool> _enableCombatLogging;
        private ConfigEntry<bool> _enableDamageCache;

        #endregion

        #region Lifecycle

        /// <summary>
        /// Called when Core systems are ready. Initialize all combat systems here.
        /// </summary>
        public void OnCoreReady()
        {
            CoreAPI.Log.LogInfo($"[{ModuleID}] Core is ready. Initializing Combat Module...");

            try
            {
                // Load configuration
                LoadConfiguration();

                // Load weapon data from CSV
                LoadWeaponData();

                // Initialize systems
                InitializeSystems();

                // Apply Harmony patches
                ApplyHarmonyPatches();

                // Register network handlers
                RegisterNetworkHandlers();

                // Subscribe to player join events
                SubscribeToPlayerEvents();

                CoreAPI.Log.LogInfo($"[{ModuleID}] Initialization complete!");
            }
            catch (Exception ex)
            {
                CoreAPI.Log.LogError($"[{ModuleID}] Failed to initialize: {ex}");
            }
        }

        /// <summary>
        /// Called when the module is being unloaded/disabled.
        /// </summary>
        public void OnShutdown()
        {
            CoreAPI.Log.LogInfo($"[{ModuleID}] Shutting down...");

            try
            {
                // Cleanup sync manager
                _syncManager?.Shutdown();

                // Note: DamageCalculator and WeaponManager don't have Shutdown methods
                // They will be cleaned up by garbage collector

                // Unpatch Harmony
                _harmony?.UnpatchSelf();

                CoreAPI.Log.LogInfo($"[{ModuleID}] Shutdown complete.");
            }
            catch (Exception ex)
            {
                CoreAPI.Log.LogError($"[{ModuleID}] Error during shutdown: {ex}");
            }
        }

        #endregion

        #region Configuration

        private void LoadConfiguration()
        {
            CoreAPI.Log.LogInfo($"[{ModuleID}] Loading configuration...");

            // Combat performance settings
            _enableDamageBatching = CoreAPI.Config.Bind(
                "Combat.Performance",
                "EnableDamageBatching",
                true,
                "Enable damage batching for network optimization (recommended for servers with 10+ players)"
            );

            _damageSyncInterval = CoreAPI.Config.Bind(
                "Combat.Performance",
                "DamageSyncInterval",
                0.1f,
                "How often (in seconds) to sync batched damage (default 0.1 = 100ms)"
            );

            _syncRadius = CoreAPI.Config.Bind(
                "Combat.Performance",
                "SyncRadius",
                50f,
                "Only sync combat to players within this radius (meters)"
            );

            _enableCombatLogging = CoreAPI.Config.Bind(
                "Combat.Debug",
                "EnableCombatLogging",
                false,
                "Enable detailed combat logging for debugging"
            );

            _enableDamageCache = CoreAPI.Config.Bind(
                "Combat.Performance",
                "EnableDamageCache",
                true,
                "Cache damage calculations per player/weapon (huge performance boost)"
            );

            CoreAPI.Log.LogInfo($"[{ModuleID}] Configuration loaded.");
        }

        #endregion

        #region Data Loading

        private void LoadWeaponData()
        {
            CoreAPI.Log.LogInfo($"[{ModuleID}] Loading weapon data...");

            try
            {
                _weaponDatabase = new WeaponDatabase();
                
                // Load from CSV in mod folder
                string csvPath = System.IO.Path.Combine(
                    BepInEx.Paths.PluginPath,
                    "UltimaValheim",
                    "Data",
                    "Ultima_Valheim_Weapons_Balance.csv"
                );

                if (!System.IO.File.Exists(csvPath))
                {
                    CoreAPI.Log.LogWarning($"[{ModuleID}] Weapon CSV not found at: {csvPath}");
                    CoreAPI.Log.LogWarning($"[{ModuleID}] Using default weapon data.");
                    return;
                }

                _weaponDatabase.LoadFromCSV(csvPath);
                CoreAPI.Log.LogInfo($"[{ModuleID}] Loaded {_weaponDatabase.GetWeaponCount()} weapons from CSV.");
            }
            catch (Exception ex)
            {
                CoreAPI.Log.LogError($"[{ModuleID}] Failed to load weapon data: {ex}");
            }
        }

        #endregion

        #region System Initialization

        private void InitializeSystems()
        {
            CoreAPI.Log.LogInfo($"[{ModuleID}] Initializing combat systems...");

            // Check if Skills module is available
            bool skillsAvailable = CoreLifecycle.IsModuleRegistered("UltimaValheim.Skills");

            // Initialize damage calculator (takes 2 arguments)
            _damageCalculator = new DamageCalculator(
                _weaponDatabase,
                skillsAvailable
            );

            // Initialize network sync manager
            _syncManager = new CombatSyncManager(
                ModuleID,
                _damageSyncInterval.Value,
                _syncRadius.Value,
                _enableDamageBatching.Value
            );

            // Initialize weapon manager (takes 1 argument)
            _weaponManager = new WeaponManager(_weaponDatabase);

            CoreAPI.Log.LogInfo($"[{ModuleID}] Combat systems initialized.");
        }

        #endregion

        #region Network Registration

        private void RegisterNetworkHandlers()
        {
            CoreAPI.Log.LogInfo($"[{ModuleID}] Registering network handlers...");

            // Register RPC for batch damage sync
            CoreAPI.Network.RegisterRPC(ModuleID, "BatchDamageSync", (rpc, package) =>
            {
                _syncManager.ReceiveBatchDamage(package);
            });

            CoreAPI.Log.LogInfo($"[{ModuleID}] Network handlers registered.");
        }

        #endregion

        #region Event Subscriptions

        private void SubscribeToPlayerEvents()
        {
            CoreAPI.Log.LogInfo($"[{ModuleID}] Subscribing to player events...");

            // FIXED: Subscribe with proper Action<Player> delegate type
            CoreAPI.Events.Subscribe<Action<Player>>("Player.OnJoin", player =>
            {
                OnPlayerJoin(player);
            });

            CoreAPI.Events.Subscribe<Action<Player>>("Player.OnLeave", player =>
            {
                OnPlayerLeave(player);
            });

            CoreAPI.Log.LogInfo($"[{ModuleID}] Event subscriptions complete.");
        }

        public void OnPlayerJoin(Player player)
        {
            CoreAPI.Log.LogInfo($"[{ModuleID}] Player joined: {player.GetPlayerName()}");

            // Subscribe to skill level up events for this player
            SubscribeToSkillEvents(player);
        }

        public void OnPlayerLeave(Player player)
        {
            CoreAPI.Log.LogInfo($"[{ModuleID}] Player left: {player.GetPlayerName()}");

            // Clear cache for this player
            _damageCalculator.InvalidatePlayerCache(player.GetPlayerID());
        }

        public void OnSave()
        {
            // Combat Module doesn't need to save any persistent data
            // All combat data is calculated in real-time
            CoreAPI.Log.LogDebug($"[{ModuleID}] OnSave called (no data to save)");
        }

        #endregion

        #region Harmony Patches

        private void ApplyHarmonyPatches()
        {
            CoreAPI.Log.LogInfo($"[{ModuleID}] Applying Harmony patches...");

            try
            {
                _harmony = new Harmony(ModuleID);
                _harmony.PatchAll();

                CoreAPI.Log.LogInfo($"[{ModuleID}] Harmony patches applied successfully.");
            }
            catch (Exception ex)
            {
                CoreAPI.Log.LogError($"[{ModuleID}] Failed to apply Harmony patches: {ex}");
            }
        }

        #endregion

        #region Skills Integration

        private void SubscribeToSkillEvents(Player player)
        {
            // FIXED: Subscribe with proper Action<Player, string, int> delegate type
            CoreAPI.Events.Subscribe<Action<Player, string, int>>("Skills.OnLevelUp",
                (p, skillName, level) =>
                {
                    if (p.GetPlayerID() == player.GetPlayerID() && IsCombatSkill(skillName))
                    {
                        _damageCalculator.InvalidatePlayerCache(p.GetPlayerID());
                        
                        if (_enableCombatLogging.Value)
                        {
                            CoreAPI.Log.LogDebug($"[{ModuleID}] Invalidated damage cache for {p.GetPlayerName()} due to {skillName} level up");
                        }
                    }
                });
        }

        private bool IsCombatSkill(string skillName)
        {
            // Check if skill affects combat
            return skillName.Contains("Sword") ||
                   skillName.Contains("Axe") ||
                   skillName.Contains("Mace") ||
                   skillName.Contains("Spear") ||
                   skillName.Contains("Bow") ||
                   skillName.Contains("Dagger");
        }

        #endregion

        #region Public API

        /// <summary>
        /// Get the damage calculator (for use by other modules or patches)
        /// </summary>
        public DamageCalculator GetDamageCalculator() => _damageCalculator;

        /// <summary>
        /// Get the weapon manager (for weapon generation)
        /// </summary>
        public WeaponManager GetWeaponManager() => _weaponManager;

        /// <summary>
        /// Get the sync manager (for use by patches)
        /// </summary>
        public CombatSyncManager GetSyncManager() => _syncManager;

        /// <summary>
        /// Get the weapon database
        /// </summary>
        public WeaponDatabase GetWeaponDatabase() => _weaponDatabase;

        #endregion
    }
}
