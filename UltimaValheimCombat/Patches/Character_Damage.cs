using HarmonyLib;
using UltimaValheim.Core;

namespace UltimaValheim.Combat.Patches
{
    /// <summary>
    /// Harmony patch for Character.Damage method.
    /// Intercepts damage calculations to apply custom combat system.
    /// </summary>
    [HarmonyPatch(typeof(Character), nameof(Character.Damage))]
    public static class Character_Damage
    {
        private static CombatModule _combatModule;

        /// <summary>
        /// Initialize the patch with module reference
        /// </summary>
        public static void Initialize(CombatModule module)
        {
            _combatModule = module;
        }

        [HarmonyPrefix]
        static bool Prefix(Character __instance, HitData hit)
        {
            // Early exit: Not initialized
            if (_combatModule == null)
                return true; // Run original method

            // Early exit: No attacker
            if (hit.GetAttacker() == null)
                return true;

            // Early exit: Not PvP (attacker is not a player)
            Player attackerPlayer = hit.GetAttacker() as Player;
            if (attackerPlayer == null)
                return true; // Let vanilla handle PvE for now

            // Early exit: Target is not valid
            if (__instance == null || __instance.IsDead())
                return true;

            try
            {
                // Calculate custom damage
                var calculator = _combatModule.GetDamageCalculator();
                if (calculator == null)
                    return true;

                float customDamage = calculator.CalculateDamage(attackerPlayer, __instance, hit);

                // Override damage in HitData
                hit.m_damage.m_damage = customDamage;
                hit.m_damage.m_slash = 0f;
                hit.m_damage.m_pierce = 0f;
                hit.m_damage.m_blunt = 0f;
                hit.m_damage.m_chop = 0f;
                hit.m_damage.m_pickaxe = 0f;
                hit.m_damage.m_fire = 0f;
                hit.m_damage.m_frost = 0f;
                hit.m_damage.m_lightning = 0f;
                hit.m_damage.m_poison = 0f;
                hit.m_damage.m_spirit = 0f;

                // Queue damage for batched sync
                var syncManager = _combatModule.GetSyncManager();
                if (syncManager != null)
                {
                    syncManager.QueueDamage(__instance, customDamage);
                }

                // Publish combat event (throttled)
                if (syncManager != null)
                {
                    syncManager.PublishCombatEvent("Combat.OnDamageDealt", attackerPlayer, __instance, customDamage);
                }
            }
            catch (System.Exception ex)
            {
                CoreAPI.Log.LogError($"[Character_Damage] Error in damage calculation: {ex}");
                return true; // Fallback to vanilla on error
            }

            // Continue with original method (will apply our modified damage)
            return true;
        }
    }
}
