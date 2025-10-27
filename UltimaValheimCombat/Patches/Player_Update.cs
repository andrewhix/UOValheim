using HarmonyLib;
using UltimaValheim.Core;

namespace UltimaValheim.Combat.Patches
{
    /// <summary>
    /// Harmony patch for Player.Update method.
    /// Ticks the combat sync manager for batched damage updates.
    /// </summary>
    [HarmonyPatch(typeof(Player), "Update")]
    public static class Player_Update
    {
        private static CombatModule _combatModule;

        /// <summary>
        /// Initialize the patch with module reference
        /// </summary>
        public static void Initialize(CombatModule module)
        {
            _combatModule = module;
        }

        [HarmonyPostfix]
        static void Postfix(Player __instance)
        {
            // Early exit: Not initialized
            if (_combatModule == null)
                return;

            // Early exit: Not the local player (only tick once per frame)
            if (__instance != Player.m_localPlayer)
                return;

            try
            {
                // Tick the sync manager for batched updates
                var syncManager = _combatModule.GetSyncManager();
                if (syncManager != null)
                {
                    syncManager.Update();
                }
            }
            catch (System.Exception ex)
            {
                CoreAPI.Log.LogError($"[Player_Update] Error ticking sync manager: {ex}");
            }
        }
    }
}
