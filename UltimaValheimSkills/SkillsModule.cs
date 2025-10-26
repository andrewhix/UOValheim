using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UltimaValheim.Core;
using HarmonyLib;

namespace UltimaValheim.Skills
{
    /// <summary>
    /// Skills module for Ultima Valheim.
    /// Centralizes all skill management with UO-style 0-100 skill system.
    /// </summary>
    public class SkillsModule : ICoreModule
    {
        public string ModuleID => "UltimaValheim.Skills";
        public System.Version ModuleVersion => new System.Version(1, 0, 0);

        // Configuration
        private BepInEx.Configuration.ConfigEntry<bool> _enableSkillSystem;
        private BepInEx.Configuration.ConfigEntry<float> _skillGainMultiplier;
        private BepInEx.Configuration.ConfigEntry<float> _totalSkillCap;
        private BepInEx.Configuration.ConfigEntry<bool> _enableSkillCap;
        private BepInEx.Configuration.ConfigEntry<bool> _showSkillNotifications;
        private BepInEx.Configuration.ConfigEntry<bool> _enableVerboseLogging;

        // Internal state
        private Dictionary<long, PlayerSkillData> _playerSkills = new Dictionary<long, PlayerSkillData>();
        private SkillDefinitionManager _skillDefinitions;
        private Harmony _harmony;

        // Skill update constants
        private const float SKILL_UPDATE_INTERVAL = 0.5f; // Save skills every 0.5 seconds if changed
        private Dictionary<long, float> _lastSkillSaveTime = new Dictionary<long, float>();

        public void OnCoreReady()
        {
            CoreAPI.Log.LogInfo($"[{ModuleID}] Initializing Skills module...");

            // Load configuration
            LoadConfiguration();

            if (!_enableSkillSystem.Value)
            {
                CoreAPI.Log.LogInfo($"[{ModuleID}] Skills system is disabled in config.");
                return;
            }

            // Initialize skill definitions
            _skillDefinitions = new SkillDefinitionManager();
            RegisterDefaultSkills();

            // Setup network handlers for multiplayer sync
            RegisterNetworkHandlers();

            // Apply Harmony patches if needed
            _harmony = new Harmony("com.valheim.ultima.skills");
            ApplyHarmonyPatches();

            // Load existing player data
            LoadAllPlayerData();

            CoreAPI.Log.LogInfo($"[{ModuleID}] Skills module initialized successfully!");
            CoreAPI.Log.LogInfo($"[{ModuleID}] Registered {_skillDefinitions.GetAllSkills().Count} skill definitions");
        }

        public void OnPlayerJoin(Player player)
        {
            if (!_enableSkillSystem.Value || player == null)
                return;

            long playerID = player.GetPlayerID();

            CoreAPI.Log.LogInfo($"[{ModuleID}] Player {player.GetPlayerName()} ({playerID}) joined - loading skills...");

            // Load or initialize player skills
            LoadPlayerSkills(playerID);

            // Sync to client
            SyncPlayerSkillsToClient(playerID);

            // Publish event for other modules
            CoreAPI.Events.Publish("OnPlayerSkillsLoaded", player, _playerSkills[playerID]);
        }

        public void OnPlayerLeave(Player player)
        {
            if (!_enableSkillSystem.Value || player == null)
                return;

            long playerID = player.GetPlayerID();

            CoreAPI.Log.LogInfo($"[{ModuleID}] Player {player.GetPlayerName()} ({playerID}) leaving - saving skills...");

            // Save player skills
            SavePlayerSkills(playerID);

            // Clear from cache
            _playerSkills.Remove(playerID);
            _lastSkillSaveTime.Remove(playerID);
        }

        public void OnSave()
        {
            if (!_enableSkillSystem.Value)
                return;

            CoreAPI.Log.LogInfo($"[{ModuleID}] Saving all player skills...");

            // Save all active player skills
            foreach (var playerID in _playerSkills.Keys.ToList())
            {
                SavePlayerSkills(playerID);
            }

            // Persist to disk
            CoreAPI.Persistence.SaveToDisk();
        }

        public void OnShutdown()
        {
            CoreAPI.Log.LogInfo($"[{ModuleID}] Shutting down Skills module...");

            // Save all player data
            OnSave();

            // Cleanup
            _playerSkills.Clear();
            _lastSkillSaveTime.Clear();

            // Unpatch Harmony
            _harmony?.UnpatchSelf();
        }

        #region Configuration

        private void LoadConfiguration()
        {
            _enableSkillSystem = CoreAPI.Config.Bind(
                ModuleID,
                "EnableSkillSystem",
                true,
                "Enable the Ultima Online style skill system"
            );

            _skillGainMultiplier = CoreAPI.Config.Bind(
                ModuleID,
                "SkillGainMultiplier",
                1.0f,
                "Multiplier for all skill gains (1.0 = normal, 2.0 = double speed)"
            );

            _totalSkillCap = CoreAPI.Config.Bind(
                ModuleID,
                "TotalSkillCap",
                700.0f,
                "Maximum total of all skills combined (UO default is 700)"
            );

            _enableSkillCap = CoreAPI.Config.Bind(
                ModuleID,
                "EnableSkillCap",
                true,
                "Enable skill cap (when total skills reach cap, gains become harder)"
            );

            _showSkillNotifications = CoreAPI.Config.Bind(
                ModuleID,
                "ShowSkillNotifications",
                true,
                "Show notifications when skills increase"
            );

            _enableVerboseLogging = CoreAPI.Config.Bind(
                ModuleID,
                "EnableVerboseLogging",
                false,
                "Enable detailed logging for skill gains (for debugging)"
            );
        }

        #endregion

        #region Skill Registration

        private void RegisterDefaultSkills()
        {
            // Combat skills
            _skillDefinitions.RegisterSkill("Swords", "Combat skill with bladed weapons", SkillCategory.Combat);
            _skillDefinitions.RegisterSkill("Macing", "Combat skill with blunt weapons", SkillCategory.Combat);
            _skillDefinitions.RegisterSkill("Fencing", "Combat skill with piercing weapons", SkillCategory.Combat);
            _skillDefinitions.RegisterSkill("Archery", "Combat skill with bows", SkillCategory.Combat);
            _skillDefinitions.RegisterSkill("Parrying", "Defensive skill with shields", SkillCategory.Combat);
            _skillDefinitions.RegisterSkill("Tactics", "Overall combat effectiveness", SkillCategory.Combat);

            // Magic skills
            _skillDefinitions.RegisterSkill("Magery", "Spellcasting ability", SkillCategory.Magic);
            _skillDefinitions.RegisterSkill("EvalInt", "Intelligence-based magic evaluation", SkillCategory.Magic);
            _skillDefinitions.RegisterSkill("Meditation", "Mana regeneration", SkillCategory.Magic);
            _skillDefinitions.RegisterSkill("Resist", "Magic resistance", SkillCategory.Magic);

            // Crafting skills
            _skillDefinitions.RegisterSkill("Mining", "Extract ore from rocks", SkillCategory.Resource);
            _skillDefinitions.RegisterSkill("Lumberjacking", "Harvest wood from trees", SkillCategory.Resource);
            _skillDefinitions.RegisterSkill("Blacksmithy", "Craft metal weapons and armor", SkillCategory.Crafting);
            _skillDefinitions.RegisterSkill("Carpentry", "Craft wooden items", SkillCategory.Crafting);
            _skillDefinitions.RegisterSkill("Tailoring", "Craft cloth armor", SkillCategory.Crafting);
            _skillDefinitions.RegisterSkill("Alchemy", "Create potions and reagents", SkillCategory.Crafting);
            _skillDefinitions.RegisterSkill("Cooking", "Prepare food", SkillCategory.Crafting);

            // Utility skills
            _skillDefinitions.RegisterSkill("Healing", "Heal wounds and cure poison", SkillCategory.Utility);
            _skillDefinitions.RegisterSkill("AnimalLore", "Understand creatures", SkillCategory.Utility);
            _skillDefinitions.RegisterSkill("Camping", "Set up camps and rest", SkillCategory.Utility);
        }

        #endregion

        #region Public API

        /// <summary>
        /// Get a player's current skill value.
        /// </summary>
        public float GetSkill(Player player, string skillName)
        {
            if (player == null)
                return 0f;

            return GetSkill(player.GetPlayerID(), skillName);
        }

        /// <summary>
        /// Get a player's current skill value by player ID.
        /// </summary>
        public float GetSkill(long playerID, string skillName)
        {
            if (!_playerSkills.TryGetValue(playerID, out var skillData))
                return 50f; // Default starting skill

            return skillData.GetSkill(skillName);
        }

        /// <summary>
        /// Set a player's skill value directly (for admin commands).
        /// </summary>
        public void SetSkill(Player player, string skillName, float value)
        {
            if (player == null)
                return;

            SetSkill(player.GetPlayerID(), skillName, value);
        }

        /// <summary>
        /// Set a player's skill value directly by player ID.
        /// </summary>
        public void SetSkill(long playerID, string skillName, float value)
        {
            // Clamp value to 0-100 range
            value = Mathf.Clamp(value, 0f, 100f);

            if (!_playerSkills.TryGetValue(playerID, out var skillData))
            {
                skillData = new PlayerSkillData();
                _playerSkills[playerID] = skillData;
            }

            float oldValue = skillData.GetSkill(skillName);
            skillData.SetSkill(skillName, value);

            CoreAPI.Log.LogInfo($"[{ModuleID}] Set skill {skillName} from {oldValue:F1} to {value:F1} for player {playerID}");

            // Mark for save
            MarkPlayerForSave(playerID);

            // Sync to client if online
            SyncSkillToClient(playerID, skillName, value);
        }

        /// <summary>
        /// Add skill experience. This is the primary method other modules should call.
        /// </summary>
        public void AddSkillExperience(Player player, string skillName, float difficulty, float successModifier = 1.0f)
        {
            if (player == null || !_enableSkillSystem.Value)
                return;

            AddSkillExperience(player.GetPlayerID(), skillName, difficulty, successModifier);
        }

        /// <summary>
        /// Add skill experience by player ID.
        /// </summary>
        public void AddSkillExperience(long playerID, string skillName, float difficulty, float successModifier = 1.0f)
        {
            if (!_playerSkills.TryGetValue(playerID, out var skillData))
            {
                skillData = new PlayerSkillData();
                _playerSkills[playerID] = skillData;
            }

            float currentSkill = skillData.GetSkill(skillName);
            float skillGain = CalculateSkillGain(currentSkill, difficulty, successModifier);

            if (skillGain > 0)
            {
                float newSkill = Mathf.Clamp(currentSkill + skillGain, 0f, 100f);
                skillData.SetSkill(skillName, newSkill);

                if (_enableVerboseLogging.Value)
                {
                    CoreAPI.Log.LogInfo($"[{ModuleID}] Player {playerID} gained {skillGain:F2} in {skillName} (now {newSkill:F1})");
                }

                // Mark for save
                MarkPlayerForSave(playerID);

                // Sync to client
                SyncSkillToClient(playerID, skillName, newSkill);

                // Publish event for other modules
                CoreAPI.Events.Publish("OnSkillGain", playerID, skillName, skillGain, newSkill);

                // Check for skill level up (whole number milestone)
                int oldLevel = Mathf.FloorToInt(currentSkill);
                int newLevel = Mathf.FloorToInt(newSkill);
                if (newLevel > oldLevel)
                {
                    OnSkillLevelUp(playerID, skillName, newLevel);
                }
            }
        }

        /// <summary>
        /// Check if a player has minimum skill requirement.
        /// </summary>
        public bool HasSkillRequirement(Player player, string skillName, float requiredSkill)
        {
            if (player == null)
                return false;

            return GetSkill(player, skillName) >= requiredSkill;
        }

        /// <summary>
        /// Get all skill definitions.
        /// </summary>
        public List<SkillDefinition> GetAllSkillDefinitions()
        {
            return _skillDefinitions.GetAllSkills();
        }

        /// <summary>
        /// Get a player's total skill points.
        /// </summary>
        public float GetTotalSkillPoints(Player player)
        {
            if (player == null)
                return 0f;

            return GetTotalSkillPoints(player.GetPlayerID());
        }

        /// <summary>
        /// Get a player's total skill points by ID.
        /// </summary>
        public float GetTotalSkillPoints(long playerID)
        {
            if (!_playerSkills.TryGetValue(playerID, out var skillData))
                return 0f;

            return skillData.GetTotalSkillPoints();
        }

        #endregion

        #region Skill Calculation

        /// <summary>
        /// Calculate skill gain based on current skill, task difficulty, and success.
        /// Uses UO-style difficulty-based skill gain formula.
        /// </summary>
        private float CalculateSkillGain(float currentSkill, float difficulty, float successModifier)
        {
            // No gain if at max skill
            if (currentSkill >= 100f)
                return 0f;

            // Base gain chance calculation (UO formula)
            // Harder tasks give more gain, but only if within reasonable skill range
            float skillDelta = difficulty - currentSkill;

            // Can't gain from tasks too easy (more than 25 points below skill)
            if (skillDelta < -25f)
                return 0f;

            // Can't gain from tasks too hard (more than 25 points above skill)
            if (skillDelta > 25f)
                return 0f;

            // Optimal gain is when difficulty matches skill level
            // Gain decreases as you get better than the task, or task is too hard
            float gainChance = 1.0f - (Mathf.Abs(skillDelta) / 25f);

            // Roll for gain
            if (UnityEngine.Random.value > gainChance)
                return 0f;

            // Calculate gain amount
            // Higher skills gain slower (UO power curve)
            float baseGain = 0.1f * (100f - currentSkill) / 100f;

            // Adjust by success modifier
            baseGain *= successModifier;

            // Apply global multiplier
            baseGain *= _skillGainMultiplier.Value;

            // Apply skill cap penalty if enabled
            if (_enableSkillCap.Value)
            {
                float totalSkills = GetTotalSkillPoints(0); // TODO: Get actual player ID
                if (totalSkills >= _totalSkillCap.Value)
                {
                    // Significantly reduce gain when at cap
                    baseGain *= 0.1f;
                }
                else if (totalSkills >= _totalSkillCap.Value * 0.9f)
                {
                    // Moderately reduce gain when approaching cap
                    baseGain *= 0.5f;
                }
            }

            return baseGain;
        }

        #endregion

        #region Events

        private void OnSkillLevelUp(long playerID, string skillName, int newLevel)
        {
            CoreAPI.Log.LogInfo($"[{ModuleID}] Player {playerID} reached level {newLevel} in {skillName}!");

            // Show notification if enabled
            if (_showSkillNotifications.Value)
            {
                // TODO: Show HUD notification when player is online
            }

            // Publish event for other modules
            CoreAPI.Events.Publish("OnSkillLevelUp", playerID, skillName, newLevel);
        }

        #endregion

        #region Persistence

        private void LoadAllPlayerData()
        {
            // Player data will be loaded on-demand when players join
            CoreAPI.Log.LogInfo($"[{ModuleID}] Player skill data will be loaded on player join");
        }

        private void LoadPlayerSkills(long playerID)
        {
            var skillData = CoreAPI.Persistence.LoadPlayerData<PlayerSkillData>(ModuleID, playerID);

            if (skillData != null)
            {
                _playerSkills[playerID] = skillData;
                CoreAPI.Log.LogInfo($"[{ModuleID}] Loaded skills for player {playerID} - Total: {skillData.GetTotalSkillPoints():F1}");
            }
            else
            {
                // Initialize new player with default skills
                skillData = new PlayerSkillData();
                _playerSkills[playerID] = skillData;
                CoreAPI.Log.LogInfo($"[{ModuleID}] Created new skill data for player {playerID}");
            }
        }

        private void SavePlayerSkills(long playerID)
        {
            if (!_playerSkills.TryGetValue(playerID, out var skillData))
                return;

            CoreAPI.Persistence.SavePlayerData(ModuleID, playerID, skillData);

            if (_enableVerboseLogging.Value)
            {
                CoreAPI.Log.LogInfo($"[{ModuleID}] Saved skills for player {playerID}");
            }
        }

        private void MarkPlayerForSave(long playerID)
        {
            _lastSkillSaveTime[playerID] = Time.time;

            // Auto-save after interval
            // TODO: Implement deferred save in Update() if needed
        }

        #endregion

        #region Networking

        private void RegisterNetworkHandlers()
        {
            if (!CoreAPI.Network.IsConnected())
                return;

            // Register RPC handlers for skill sync
            CoreAPI.Network.RegisterRPC(ModuleID, "SyncSkill", HandleSyncSkillRPC);
            CoreAPI.Network.RegisterRPC(ModuleID, "SyncAllSkills", HandleSyncAllSkillsRPC);
        }

        private void SyncSkillToClient(long playerID, string skillName, float value)
        {
            if (!CoreAPI.Network.IsServer())
                return;

            ZPackage package = new ZPackage();
            package.Write(playerID);
            package.Write(skillName);
            package.Write(value);

            CoreAPI.Network.SendToAll(ModuleID, "SyncSkill", package);
        }

        private void SyncPlayerSkillsToClient(long playerID)
        {
            if (!CoreAPI.Network.IsServer())
                return;

            if (!_playerSkills.TryGetValue(playerID, out var skillData))
                return;

            ZPackage package = new ZPackage();
            package.Write(playerID);
            skillData.Serialize(package);

            CoreAPI.Network.SendToAll(ModuleID, "SyncAllSkills", package);
        }

        private void HandleSyncSkillRPC(ZRpc rpc, ZPackage package)
        {
            try
            {
                long playerID = package.ReadLong();
                string skillName = package.ReadString();
                float value = package.ReadSingle();

                if (!_playerSkills.TryGetValue(playerID, out var skillData))
                {
                    skillData = new PlayerSkillData();
                    _playerSkills[playerID] = skillData;
                }

                skillData.SetSkill(skillName, value);
            }
            catch (Exception ex)
            {
                CoreAPI.Log.LogError($"[{ModuleID}] Error handling SyncSkill RPC: {ex}");
            }
        }

        private void HandleSyncAllSkillsRPC(ZRpc rpc, ZPackage package)
        {
            try
            {
                long playerID = package.ReadLong();
                var skillData = new PlayerSkillData();
                skillData.Deserialize(package);

                _playerSkills[playerID] = skillData;
            }
            catch (Exception ex)
            {
                CoreAPI.Log.LogError($"[{ModuleID}] Error handling SyncAllSkills RPC: {ex}");
            }
        }

        #endregion

        #region Harmony Patches

        private void ApplyHarmonyPatches()
        {
            // Currently no patches needed
            // Future patches could hook into character stats UI to display skills
        }

        #endregion
    }
}
