using HarmonyLib;
using UltimaValheim.Core;

namespace UltimaValheim.Combat.Patches
{
    /// <summary>
    /// Harmony patch for Humanoid.EquipItem method.
    /// Invalidates damage cache when player changes equipment.
    /// </summary>
    [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.EquipItem))]
    public static class Humanoid_EquipItem
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
        static void Postfix(Humanoid __instance, ItemDrop.ItemData item, bool __result)
        {
            // Early exit: Not initialized
            if (_combatModule == null)
                return;

            // Early exit: Equip failed
            if (!__result)
                return;

            // Early exit: Not a player
            Player player = __instance as Player;
            if (player == null)
                return;

            // Early exit: Not a weapon
            if (item == null || !item.IsWeapon())
                return;

            try
            {
                // Invalidate damage cache for this player
                var calculator = _combatModule.GetDamageCalculator();
                if (calculator != null)
                {
                    long playerID = player.GetPlayerID();
                    calculator.InvalidatePlayerCache(playerID);

                    CoreAPI.Log.LogDebug($"[Humanoid_EquipItem] Invalidated damage cache for {player.GetPlayerName()} - equipped {item.m_shared.m_name}");
                }
            }
            catch (System.Exception ex)
            {
                CoreAPI.Log.LogError($"[Humanoid_EquipItem] Error invalidating cache: {ex}");
            }
        }
    }

    /// <summary>
    /// Harmony patch for Humanoid.UnequipItem method.
    /// Invalidates damage cache when player unequips items.
    /// </summary>
    [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.UnequipItem))]
    public static class Humanoid_UnequipItem
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
        static void Postfix(Humanoid __instance, ItemDrop.ItemData item)
        {
            // Early exit: Not initialized
            if (_combatModule == null)
                return;

            // Early exit: Not a player
            Player player = __instance as Player;
            if (player == null)
                return;

            // Early exit: Not a weapon
            if (item == null || !item.IsWeapon())
                return;

            try
            {
                // Invalidate damage cache for this player
                var calculator = _combatModule.GetDamageCalculator();
                if (calculator != null)
                {
                    long playerID = player.GetPlayerID();
                    calculator.InvalidatePlayerCache(playerID);

                    CoreAPI.Log.LogDebug($"[Humanoid_UnequipItem] Invalidated damage cache for {player.GetPlayerName()} - unequipped {item.m_shared.m_name}");
                }
            }
            catch (System.Exception ex)
            {
                CoreAPI.Log.LogError($"[Humanoid_UnequipItem] Error invalidating cache: {ex}");
            }
        }
    }
}
