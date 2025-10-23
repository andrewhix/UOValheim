using HarmonyLib;
using System;
using System.Collections.Generic;

namespace UltimaValheim.Core
{
    /// <summary>
    /// Routes game lifecycle events (player join/leave, save, etc.) to registered modules.
    /// Hooks into Valheim's lifecycle using Harmony patches.
    /// </summary>
    public class CoreEventRouter
    {
        private static Harmony _harmony;
        private static readonly Dictionary<long, Player> _activePlayers = new Dictionary<long, Player>();

        public CoreEventRouter()
        {
            // Apply Harmony patches for lifecycle events
            _harmony = new Harmony("com.valheim.ultima.core.eventrouter");
            ApplyPatches();
        }

        private void ApplyPatches()
        {
            try
            {
                // Patch player spawn/join
                _harmony.Patch(
                    AccessTools.Method(typeof(Player), nameof(Player.OnSpawned)),
                    postfix: new HarmonyMethod(typeof(CoreEventRouter), nameof(OnPlayerSpawned_Postfix))
                );

                // Patch player destroy/leave
                _harmony.Patch(
                    AccessTools.Method(typeof(Player), nameof(Player.OnDestroy)),
                    prefix: new HarmonyMethod(typeof(CoreEventRouter), nameof(OnPlayerDestroy_Prefix))
                );

                // Patch save
                _harmony.Patch(
                    AccessTools.Method(typeof(Game), nameof(Game.SavePlayerProfile)),
                    prefix: new HarmonyMethod(typeof(CoreEventRouter), nameof(OnSave_Prefix))
                );

                CoreAPI.Log.LogInfo("[CoreEventRouter] Applied Harmony patches for lifecycle events.");
            }
            catch (Exception ex)
            {
                CoreAPI.Log.LogError($"[CoreEventRouter] Failed to apply patches: {ex}");
            }
        }

        #region Harmony Patches

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Player), nameof(Player.OnSpawned))]
        private static void OnPlayerSpawned_Postfix(Player __instance)
        {
            if (__instance == null)
                return;

            var playerID = __instance.GetPlayerID();
            
            if (!_activePlayers.ContainsKey(playerID))
            {
                _activePlayers[playerID] = __instance;
                CoreAPI.Log.LogInfo($"[CoreEventRouter] Player joined: {__instance.GetPlayerName()} ({playerID})");
                
                // Notify all modules
                TriggerOnPlayerJoin(__instance);
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(Player), nameof(Player.OnDestroy))]
        private static void OnPlayerDestroy_Prefix(Player __instance)
        {
            if (__instance == null)
                return;

            var playerID = __instance.GetPlayerID();
            
            if (_activePlayers.ContainsKey(playerID))
            {
                CoreAPI.Log.LogInfo($"[CoreEventRouter] Player leaving: {__instance.GetPlayerName()} ({playerID})");
                
                // Notify all modules
                TriggerOnPlayerLeave(__instance);
                
                _activePlayers.Remove(playerID);
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(Game), nameof(Game.SavePlayerProfile))]
        private static void OnSave_Prefix()
        {
            CoreAPI.Log.LogInfo("[CoreEventRouter] Save triggered.");
            TriggerOnSave();
        }

        #endregion

        #region Event Triggers

        /// <summary>
        /// Trigger OnPlayerJoin for all registered modules.
        /// </summary>
        public static void TriggerOnPlayerJoin(Player player)
        {
            if (player == null)
                return;

            foreach (var module in CoreLifecycle.Modules)
            {
                try
                {
                    module.OnPlayerJoin(player);
                }
                catch (Exception ex)
                {
                    CoreAPI.Log.LogError($"[CoreEventRouter] Error in {module.ModuleID}.OnPlayerJoin: {ex}");
                }
            }

            // Also publish to EventBus for generic subscribers
            CoreAPI.Events?.Publish("OnPlayerJoin", player);
        }

        /// <summary>
        /// Trigger OnPlayerLeave for all registered modules.
        /// </summary>
        public static void TriggerOnPlayerLeave(Player player)
        {
            if (player == null)
                return;

            foreach (var module in CoreLifecycle.Modules)
            {
                try
                {
                    module.OnPlayerLeave(player);
                }
                catch (Exception ex)
                {
                    CoreAPI.Log.LogError($"[CoreEventRouter] Error in {module.ModuleID}.OnPlayerLeave: {ex}");
                }
            }

            // Also publish to EventBus
            CoreAPI.Events?.Publish("OnPlayerLeave", player);
        }

        /// <summary>
        /// Trigger OnSave for all registered modules.
        /// </summary>
        public static void TriggerOnSave()
        {
            foreach (var module in CoreLifecycle.Modules)
            {
                try
                {
                    module.OnSave();
                }
                catch (Exception ex)
                {
                    CoreAPI.Log.LogError($"[CoreEventRouter] Error in {module.ModuleID}.OnSave: {ex}");
                }
            }

            // Also publish to EventBus
            CoreAPI.Events?.Publish("OnSave");
        }

        /// <summary>
        /// Trigger OnShutdown for all registered modules.
        /// </summary>
        public static void TriggerOnShutdown()
        {
            CoreAPI.Log.LogInfo("[CoreEventRouter] Triggering shutdown for all modules...");

            foreach (var module in CoreLifecycle.Modules)
            {
                try
                {
                    module.OnShutdown();
                }
                catch (Exception ex)
                {
                    CoreAPI.Log.LogError($"[CoreEventRouter] Error in {module.ModuleID}.OnShutdown: {ex}");
                }
            }

            // Also publish to EventBus
            CoreAPI.Events?.Publish("OnShutdown");

            // Clear active players
            _activePlayers.Clear();
        }

        #endregion

        /// <summary>
        /// Get all currently active players.
        /// </summary>
        public static IReadOnlyDictionary<long, Player> ActivePlayers => _activePlayers;
    }
}
