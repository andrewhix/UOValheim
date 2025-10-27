using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;

namespace UltimaValheim.Core
{
    /// <summary>
    /// Core manager for integrating custom skills into Valheim's native skill system.
    /// Handles UI display and skill synchronization with Valheim's Skills class.
    /// </summary>
    public class SkillsManager
    {
        private Harmony _harmony;
        private static SkillsManager _instance;

        // Custom skill definitions registered by modules
        private static Dictionary<Skills.SkillType, CustomSkillDefinition> _customSkills = new Dictionary<Skills.SkillType, CustomSkillDefinition>();

        // Map skill names to skill types for quick lookup
        private static Dictionary<string, Skills.SkillType> _skillNameToType = new Dictionary<string, Skills.SkillType>(StringComparer.OrdinalIgnoreCase);

        // Starting ID for custom skills (high number to avoid vanilla conflicts)
        private const int CUSTOM_SKILL_ID_START = 999;
        private static int _nextCustomSkillId = CUSTOM_SKILL_ID_START;

        public class CustomSkillDefinition
        {
            public Skills.SkillType SkillType { get; set; }
            public string SkillName { get; set; }
            public string Description { get; set; }
            public Func<Player, float> GetSkillValue { get; set; }
            public Action<Player, float> SetSkillValue { get; set; }
        }

        public SkillsManager()
        {
            _instance = this;
            _harmony = new Harmony("com.valheim.ultima.core.skills");
            ApplyPatches();
        }

        /// <summary>
        /// Register a custom skill that will appear in Valheim's skill window.
        /// </summary>
        public Skills.SkillType RegisterCustomSkill(
            string skillName,
            string description,
            Func<Player, float> getSkillValue,
            Action<Player, float> setSkillValue = null)
        {
            if (string.IsNullOrEmpty(skillName))
            {
                CoreAPI.Log.LogError("[SkillsManager] Cannot register skill with empty name");
                return Skills.SkillType.None;
            }

            // Check if already registered
            if (_skillNameToType.ContainsKey(skillName))
            {
                CoreAPI.Log.LogWarning($"[SkillsManager] Skill '{skillName}' is already registered");
                return _skillNameToType[skillName];
            }

            // Create custom skill type
            var customSkillType = (Skills.SkillType)_nextCustomSkillId++;

            var skillDef = new CustomSkillDefinition
            {
                SkillType = customSkillType,
                SkillName = skillName,
                Description = description ?? skillName,
                GetSkillValue = getSkillValue,
                SetSkillValue = setSkillValue
            };

            _customSkills[customSkillType] = skillDef;
            _skillNameToType[skillName] = customSkillType;

            // Add localization strings if Localization is ready
            AddSkillLocalization(skillDef);

            CoreAPI.Log.LogInfo($"[SkillsManager] Registered custom skill: {skillName} (ID: {(int)customSkillType})");

            return customSkillType;
        }

        /// <summary>
        /// Get custom skill type by name.
        /// </summary>
        public Skills.SkillType GetSkillType(string skillName)
        {
            if (_skillNameToType.TryGetValue(skillName, out var skillType))
                return skillType;

            return Skills.SkillType.None;
        }

        /// <summary>
        /// Check if a skill type is a custom skill.
        /// </summary>
        public static bool IsCustomSkill(Skills.SkillType skillType)
        {
            return (int)skillType >= CUSTOM_SKILL_ID_START && _customSkills.ContainsKey(skillType);
        }

        private void ApplyPatches()
        {
            try
            {
                // Patch Skills.GetSkillList to add custom skills
                _harmony.Patch(
                    original: AccessTools.Method(typeof(Skills), nameof(Skills.GetSkillList)),
                    postfix: new HarmonyMethod(typeof(SkillsManager), nameof(GetSkillList_Postfix))
                );

                // Patch Skills.GetSkill to return custom skill values
                _harmony.Patch(
                    original: AccessTools.Method(typeof(Skills), nameof(Skills.GetSkill)),
                    postfix: new HarmonyMethod(typeof(SkillsManager), nameof(GetSkill_Postfix))
                );

                // Patch Skills.CheatResetSkill to handle custom skills
                _harmony.Patch(
                    original: AccessTools.Method(typeof(Skills), nameof(Skills.CheatResetSkill)),
                    prefix: new HarmonyMethod(typeof(SkillsManager), nameof(CheatResetSkill_Prefix))
                );

                // Patch Skills.CheatRaiseSkill to handle custom skills
                _harmony.Patch(
                    original: AccessTools.Method(typeof(Skills), nameof(Skills.CheatRaiseSkill)),
                    prefix: new HarmonyMethod(typeof(SkillsManager), nameof(CheatRaiseSkill_Prefix))
                );

                // Patch Localization.Load to refresh localizations when language changes
                var localizationType = AccessTools.TypeByName("Localization");
                if (localizationType != null)
                {
                    var loadMethod = AccessTools.Method(localizationType, "Load");
                    if (loadMethod != null)
                    {
                        _harmony.Patch(
                            original: loadMethod,
                            postfix: new HarmonyMethod(typeof(SkillsManager), nameof(Localization_Load_Postfix))
                        );
                    }
                }

                CoreAPI.Log.LogInfo("[SkillsManager] Successfully patched Valheim skill system");
            }
            catch (Exception ex)
            {
                CoreAPI.Log.LogError($"[SkillsManager] Failed to patch skill system: {ex}");
            }
        }

        #region Harmony Patches

        /// <summary>
        /// Add custom skills to the skill list shown in UI.
        /// </summary>
        [HarmonyPostfix]
        private static void GetSkillList_Postfix(Skills __instance, ref List<Skills.Skill> __result)
        {
            try
            {
                if (__instance?.m_player == null || _customSkills.Count == 0)
                    return;

                var player = __instance.m_player;

                foreach (var kvp in _customSkills.OrderBy(x => x.Value.SkillName))
                {
                    var skillDef = kvp.Value;

                    // Get current skill value from the registered getter
                    float skillValue = 10f; // Default
                    if (skillDef.GetSkillValue != null)
                    {
                        try
                        {
                            skillValue = skillDef.GetSkillValue(player);
                        }
                        catch (Exception ex)
                        {
                            CoreAPI.Log.LogError($"[SkillsManager] Error getting skill value for {skillDef.SkillName}: {ex}");
                        }
                    }

                    // Create Valheim skill object
                    var skillInfo = CreateSkillDef(skillDef.SkillType, skillDef.SkillName, skillDef.Description);
                    var skill = new Skills.Skill(skillInfo);
                    skill.m_level = skillValue;
                    skill.m_accumulator = 0f;

                    __result.Add(skill);
                }
            }
            catch (Exception ex)
            {
                CoreAPI.Log.LogError($"[SkillsManager] Error in GetSkillList_Postfix: {ex}");
            }
        }

        /// <summary>
        /// Return custom skill values when GetSkill is called.
        /// </summary>
        [HarmonyPostfix]
        private static void GetSkill_Postfix(Skills __instance, Skills.SkillType skillType, ref Skills.Skill __result)
        {
            try
            {
                if (__instance?.m_player == null)
                    return;

                // Check if this is a custom skill
                if (_customSkills.TryGetValue(skillType, out var skillDef))
                {
                    float skillValue = 10f; // Default
                    if (skillDef.GetSkillValue != null)
                    {
                        try
                        {
                            skillValue = skillDef.GetSkillValue(__instance.m_player);
                        }
                        catch (Exception ex)
                        {
                            CoreAPI.Log.LogError($"[SkillsManager] Error getting skill value for {skillDef.SkillName}: {ex}");
                        }
                    }

                    var skillInfo = CreateSkillDef(skillType, skillDef.SkillName, skillDef.Description);
                    __result = new Skills.Skill(skillInfo);
                    __result.m_level = skillValue;
                    __result.m_accumulator = 0f;
                }
            }
            catch (Exception ex)
            {
                CoreAPI.Log.LogError($"[SkillsManager] Error in GetSkill_Postfix: {ex}");
            }
        }

        /// <summary>
        /// Handle resetskill cheat command for custom skills.
        /// </summary>
        [HarmonyPrefix]
        private static bool CheatResetSkill_Prefix(Skills __instance, string name)
        {
            try
            {
                if (__instance?.m_player == null)
                    return true;

                // Check if this is a custom skill
                if (_skillNameToType.TryGetValue(name, out var skillType) && _customSkills.TryGetValue(skillType, out var skillDef))
                {
                    if (skillDef.SetSkillValue != null)
                    {
                        skillDef.SetSkillValue(__instance.m_player, 10f);
                        __instance.m_player.Message(MessageHud.MessageType.TopLeft, $"Skill {skillDef.SkillName} reset to 10");
                        CoreAPI.Log.LogInfo($"[SkillsManager] Reset skill {skillDef.SkillName} to 10 for player {__instance.m_player.GetPlayerName()}");
                    }
                    else
                    {
                        __instance.m_player.Message(MessageHud.MessageType.TopLeft, $"Cannot reset skill {skillDef.SkillName} (read-only)");
                    }

                    return false; // Skip original method
                }
            }
            catch (Exception ex)
            {
                CoreAPI.Log.LogError($"[SkillsManager] Error in CheatResetSkill_Prefix: {ex}");
            }

            return true; // Continue with original for vanilla skills
        }

        /// <summary>
        /// Handle raiseskill cheat command for custom skills.
        /// </summary>
        [HarmonyPrefix]
        private static bool CheatRaiseSkill_Prefix(Skills __instance, string name, float value)
        {
            try
            {
                if (__instance?.m_player == null)
                    return true;

                // Check if this is a custom skill
                if (_skillNameToType.TryGetValue(name, out var skillType) && _customSkills.TryGetValue(skillType, out var skillDef))
                {
                    if (skillDef.SetSkillValue != null && skillDef.GetSkillValue != null)
                    {
                        float currentValue = skillDef.GetSkillValue(__instance.m_player);
                        float newValue = Mathf.Clamp(currentValue + value, 0f, 100f);
                        skillDef.SetSkillValue(__instance.m_player, newValue);
                        __instance.m_player.Message(MessageHud.MessageType.TopLeft, $"Skill {skillDef.SkillName} increased to {newValue:F1}");
                        CoreAPI.Log.LogInfo($"[SkillsManager] Raised skill {skillDef.SkillName} to {newValue:F1} for player {__instance.m_player.GetPlayerName()}");
                    }
                    else
                    {
                        __instance.m_player.Message(MessageHud.MessageType.TopLeft, $"Cannot modify skill {skillDef.SkillName} (read-only)");
                    }

                    return false; // Skip original method
                }
            }
            catch (Exception ex)
            {
                CoreAPI.Log.LogError($"[SkillsManager] Error in CheatRaiseSkill_Prefix: {ex}");
            }

            return true; // Continue with original for vanilla skills
        }

        /// <summary>
        /// Refresh localizations when Localization loads.
        /// </summary>
        [HarmonyPostfix]
        private static void Localization_Load_Postfix(object __instance)
        {
            try
            {
                RefreshAllLocalizations();
            }
            catch (Exception ex)
            {
                CoreAPI.Log.LogError($"[SkillsManager] Error in Localization_Load_Postfix: {ex}");
            }
        }

        #endregion

        /// <summary>
        /// Add localization strings for a custom skill.
        /// </summary>
        private static void AddSkillLocalization(CustomSkillDefinition skillDef)
        {
            try
            {
                // Use reflection to access Localization at runtime
                var localizationType = AccessTools.TypeByName("Localization");
                if (localizationType == null)
                    return;

                var instanceProperty = AccessTools.Property(localizationType, "instance");
                if (instanceProperty == null)
                    return;

                var localization = instanceProperty.GetValue(null);
                if (localization == null)
                    return;

                string skillKey = $"skill_{(int)skillDef.SkillType}";
                string descKey = $"skill_{(int)skillDef.SkillType}_description";

                // Call AddWord method
                var addWordMethod = AccessTools.Method(localizationType, "AddWord", new[] { typeof(string), typeof(string) });
                if (addWordMethod != null)
                {
                    addWordMethod.Invoke(localization, new object[] { skillKey, skillDef.SkillName });
                    addWordMethod.Invoke(localization, new object[] { descKey, skillDef.Description });
                }
            }
            catch (Exception ex)
            {
                CoreAPI.Log.LogWarning($"[SkillsManager] Could not add localization for {skillDef.SkillName}: {ex.Message}");
            }
        }

        /// <summary>
        /// Add localization for all registered skills (called when Localization reloads).
        /// </summary>
        public static void RefreshAllLocalizations()
        {
            try
            {
                if (_customSkills.Count == 0)
                    return;

                // Use reflection to access Localization
                var localizationType = AccessTools.TypeByName("Localization");
                if (localizationType == null)
                    return;

                var instanceProperty = AccessTools.Property(localizationType, "instance");
                if (instanceProperty == null)
                    return;

                var localization = instanceProperty.GetValue(null);
                if (localization == null)
                    return;

                foreach (var kvp in _customSkills)
                {
                    AddSkillLocalization(kvp.Value);
                }

                CoreAPI.Log.LogInfo($"[SkillsManager] Refreshed localization for {_customSkills.Count} custom skills");
            }
            catch (Exception ex)
            {
                CoreAPI.Log.LogError($"[SkillsManager] Error refreshing localizations: {ex}");
            }
        }

        /// <summary>
        /// Create a SkillDef for a custom skill.
        /// </summary>
        private static Skills.SkillDef CreateSkillDef(Skills.SkillType skillType, string skillName, string description)
        {
            return new Skills.SkillDef
            {
                m_skill = skillType,
                m_description = description,
                m_icon = null, // Could load custom icons if needed
                m_increseStep = 1f
            };
        }

        /// <summary>
        /// Cleanup Harmony patches.
        /// </summary>
        public void Dispose()
        {
            _harmony?.UnpatchSelf();
        }
    }
}