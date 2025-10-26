using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UltimaValheim.Core;

namespace UltimaValheim.Mining
{
    /// <summary>
    /// Core mining system that handles ore generation based on mining skill.
    /// OPTIMIZED VERSION with performance improvements and DROP CHANCE system.
    /// </summary>
    public class MiningSystem
    {
        // ThreadStatic for thread-safe random number generation
        [ThreadStatic]
        private static System.Random _localRng;

        private static System.Random RNG
        {
            get
            {
                if (_localRng == null)
                    _localRng = new System.Random();
                return _localRng;
            }
        }

        // Drop chance configuration
        private const float BASE_DROP_CHANCE = 0.30f;  // 30% base chance for ore drop
        private const float SKILL_BONUS_PER_10 = 0.01f; // +1% per 10 skill levels
        private const float MAX_DROP_CHANCE = 0.45f;    // Cap at 45% even at max skill

        // Ore table with skill requirements and weights
        private readonly Dictionary<string, OreData> _oreTable = new Dictionary<string, OreData>();

        // OPTIMIZATION: Precomputed ore selection arrays by skill bracket
        private readonly Dictionary<int, WeightedOre[]> _oreBrackets = new Dictionary<int, WeightedOre[]>();

        private struct WeightedOre
        {
            public string OreID;
            public float CumulativeWeight;
        }

        public class OreData
        {
            public string OreID { get; set; }
            public string DisplayName { get; set; }
            public float SkillRequired { get; set; }
            public float SelectionWeight { get; set; }
            public int MinDropAmount { get; set; }
            public int MaxDropAmount { get; set; }
            public string SmeltInto { get; set; }
            public string OreColor { get; set; }
        }

        public MiningSystem()
        {
            InitializeOreTable();
            PrecomputeOreBrackets();
        }

        private void InitializeOreTable()
        {
            // Initialize ore types based on skill levels
            // Using vanilla IronOre instead of custom UOiron_ore

            // Skill 0+ (Beginner) - Using vanilla iron
            AddOre("IronOre", "Iron Ore", 0f, 80f, 1, 3, null, "Iron");

            // Skill 30+ (Apprentice)
            AddOre("UOshadow_ore", "Shadow Ore", 30f, 70f, 1, 3, "UOshadow_ingot", "DarkGray");

            // Skill 50+ (Journeyman)
            AddOre("UOgold_ore", "Gold Ore", 50f, 60f, 1, 2, "UOgold_ingot", "Gold");
            AddOre("UOagapite_ore", "Agapite Ore", 55f, 50f, 1, 2, "UOagapite_ingot", "Orange");

            // Skill 70+ (Expert)
            AddOre("UOverite_ore", "Verite Ore", 70f, 40f, 1, 2, "UOverite_ingot", "Green");
            AddOre("UOsnow_ore", "Snow Ore", 75f, 35f, 1, 2, "UOsnow_ingot", "White");

            // Skill 85+ (Master)
            AddOre("UOice_ore", "Ice Ore", 85f, 30f, 1, 2, "UOice_ingot", "Cyan");
            AddOre("UObloodrock_ore", "Bloodrock Ore", 90f, 25f, 1, 2, "UObloodrock_ingot", "DarkRed");

            // Skill 95+ (Grandmaster)
            AddOre("UOvalorite_ore", "Valorite Ore", 95f, 20f, 1, 1, "UOvalorite_ingot", "Blue");

            // Skill 100 (Legendary)
            AddOre("UOblackrock_ore", "Blackrock Ore", 100f, 15f, 1, 1, "UOblackrock_ingot", "Black");

            CoreAPI.Log.LogInfo($"[MiningSystem] Initialized {_oreTable.Count} ore types with drop chance system");
        }

        private void PrecomputeOreBrackets()
        {
            // OPTIMIZATION: Precompute weighted ore arrays for each skill bracket (0, 10, 20...100)
            for (int skill = 0; skill <= 100; skill += 10)
            {
                var eligibleOres = _oreTable.Values
                    .Where(ore => skill >= ore.SkillRequired)
                    .OrderBy(ore => ore.SkillRequired)
                    .ToList();

                if (eligibleOres.Count == 0)
                    continue;

                float totalWeight = eligibleOres.Sum(ore => ore.SelectionWeight);
                var weightedArray = new WeightedOre[eligibleOres.Count];
                float cumulative = 0f;

                for (int i = 0; i < eligibleOres.Count; i++)
                {
                    cumulative += eligibleOres[i].SelectionWeight / totalWeight;
                    weightedArray[i] = new WeightedOre
                    {
                        OreID = eligibleOres[i].OreID,
                        CumulativeWeight = cumulative
                    };
                }

                _oreBrackets[skill] = weightedArray;
            }

#if DEBUG
            CoreAPI.Log.LogInfo($"[MiningSystem] Precomputed {_oreBrackets.Count} ore selection brackets");
#endif
        }

        private void AddOre(string oreID, string displayName, float skillRequired, float weight,
                           int minDrop, int maxDrop, string smeltInto, string color)
        {
            _oreTable[oreID] = new OreData
            {
                OreID = oreID,
                DisplayName = displayName,
                SkillRequired = skillRequired,
                SelectionWeight = weight,
                MinDropAmount = minDrop,
                MaxDropAmount = maxDrop,
                SmeltInto = smeltInto,
                OreColor = color
            };
        }

        /// <summary>
        /// Called when a player mines a rock with a pickaxe.
        /// Now includes DROP CHANCE to reduce ore abundance.
        /// </summary>
        public void OnRockMined(Player player, GameObject rockObject)
        {
            if (player == null || rockObject == null)
                return;

            // Get player's mining skill (default 50 if not set)
            float miningSkill = GetPlayerMiningSkill(player);

            // Calculate drop chance based on skill
            float dropChance = CalculateDropChance(miningSkill);

            // Roll for ore drop
            if (RNG.NextDouble() > dropChance)
            {
                // No ore this time - just get stone (vanilla behavior)
#if DEBUG
                CoreAPI.Log.LogInfo($"[MiningSystem] No ore dropped (failed {dropChance:P0} chance)");
#endif
                return;
            }

            // Select ore type based on skill
            string selectedOre = SelectOreOptimized(miningSkill);
            if (string.IsNullOrEmpty(selectedOre))
                return;

            // Get ore data
            if (!_oreTable.TryGetValue(selectedOre, out OreData oreData))
                return;

            // Calculate drop amount (reduced from original)
            int dropAmount = RNG.Next(oreData.MinDropAmount, oreData.MaxDropAmount + 1);

            // Rare chance for bonus ore at high skill (reduced from 20% to 10%)
            if (miningSkill >= 75f && RNG.NextDouble() < 0.10) // 10% chance at high skill
                dropAmount++;

#if DEBUG
            CoreAPI.Log.LogInfo($"[MiningSystem] Player {player.GetPlayerName()} mined {dropAmount}x {oreData.DisplayName} (Skill: {miningSkill:F1}, Drop chance: {dropChance:P0})");
#endif

            // Drop the ore items
            DropOreItems(player, rockObject.transform.position, selectedOre, dropAmount);

            // Optional: Send a message to the player when they find ore (only for local player)
            if (player == Player.m_localPlayer)
            {
                string message = $"Found {dropAmount}x {oreData.DisplayName}";
                MessageHud.instance?.ShowMessage(MessageHud.MessageType.TopLeft, message);
            }

            // TODO: Award skill gain when Skills module is implemented
            // float skillGain = CalculateSkillGain(miningSkill, oreData.SkillRequired);
            // CoreAPI.Skills.AddSkillExperience(player, "Mining", skillGain);
        }

        /// <summary>
        /// Calculate the chance of getting ore based on mining skill.
        /// </summary>
        private float CalculateDropChance(float skill)
        {
            // Base chance + skill bonus
            float skillBonus = (skill / 10f) * SKILL_BONUS_PER_10;
            float totalChance = BASE_DROP_CHANCE + skillBonus;

            // Cap at maximum
            return Mathf.Min(totalChance, MAX_DROP_CHANCE);
        }

        /// <summary>
        /// OPTIMIZED: Select ore type using precomputed brackets (O(1) lookup).
        /// </summary>
        private string SelectOreOptimized(float skill)
        {
            // Round down to nearest 10 for bracket lookup
            int bracket = ((int)(skill / 10)) * 10;

            // Clamp to valid range
            bracket = Mathf.Clamp(bracket, 0, 100);

            // Find the highest available bracket at or below skill level
            while (bracket >= 0 && !_oreBrackets.ContainsKey(bracket))
                bracket -= 10;

            if (bracket < 0 || !_oreBrackets.TryGetValue(bracket, out var ores) || ores.Length == 0)
                return "IronOre"; // Fallback to vanilla iron

            // Binary search for ore selection (very fast)
            float roll = (float)RNG.NextDouble();

            for (int i = 0; i < ores.Length; i++)
            {
                if (roll <= ores[i].CumulativeWeight)
                    return ores[i].OreID;
            }

            // Fallback (should never reach here)
            return ores[ores.Length - 1].OreID;
        }

        /// <summary>
        /// Drop ore items at the specified position.
        /// OPTIMIZED: Better spread to prevent stacking.
        /// </summary>
        private void DropOreItems(Player player, Vector3 position, string oreID, int amount)
        {
            try
            {
                // Get the ore prefab
                GameObject orePrefab = null;

                // Special handling for vanilla iron ore
                if (oreID == "IronOre")
                {
                    orePrefab = ZNetScene.instance.GetPrefab("IronOre");
                }
                else
                {
                    // For custom ores, try to get from ItemManager
                    var customItem = Jotunn.Managers.ItemManager.Instance.GetItem(oreID);
                    if (customItem != null)
                    {
                        orePrefab = customItem.ItemPrefab;
                    }
                }

                if (orePrefab == null)
                {
                    CoreAPI.Log.LogWarning($"[MiningSystem] Could not find ore prefab for {oreID}");
                    return;
                }

                // Drop the items with improved spread
                for (int i = 0; i < amount; i++)
                {
                    // OPTIMIZATION: Larger random offset for better spread
                    Vector3 randomOffset = new Vector3(
                        UnityEngine.Random.Range(-0.5f, 0.5f),
                        0.3f,
                        UnityEngine.Random.Range(-0.5f, 0.5f)
                    );

                    Vector3 dropPosition = position + Vector3.up * 0.5f + randomOffset;

                    // Instantiate the ore
                    GameObject oreInstance = UnityEngine.Object.Instantiate(orePrefab, dropPosition, Quaternion.identity);

                    // Add some physics force for natural spread
                    Rigidbody rb = oreInstance.GetComponent<Rigidbody>();
                    if (rb != null)
                    {
                        Vector3 force = new Vector3(
                            UnityEngine.Random.Range(-2f, 2f),
                            UnityEngine.Random.Range(2f, 4f),
                            UnityEngine.Random.Range(-2f, 2f)
                        );
                        rb.AddForce(force, ForceMode.VelocityChange);
                    }

                    // Register with ZNetScene
                    ZNetView netView = oreInstance.GetComponent<ZNetView>();
                    if (netView != null)
                    {
                        netView.SetLocalScale(oreInstance.transform.localScale);
                    }
                }
            }
            catch (Exception ex)
            {
                CoreAPI.Log.LogError($"[MiningSystem] Failed to drop ore items: {ex}");
            }
        }

        /// <summary>
        /// Get a player's current mining skill level.
        /// </summary>
        public float GetPlayerMiningSkill(Player player)
        {
            if (player == null)
                return 50f; // Default skill level

            // Get from player data (will integrate with Skills module later)
            return CoreAPI.PlayerData.GetFloat(player, "Mining_Skill", 50f);
        }

        /// <summary>
        /// Set a player's mining skill level (for admin commands).
        /// </summary>
        public void SetPlayerMiningSkill(Player player, float skill)
        {
            if (player == null)
                return;

            skill = Mathf.Clamp(skill, 0f, 100f);
            CoreAPI.PlayerData.SetFloat(player, "Mining_Skill", skill);

            CoreAPI.Log.LogInfo($"[MiningSystem] Set {player.GetPlayerName()}'s mining skill to {skill:F1}");
        }

        /// <summary>
        /// Calculate skill gain based on ore difficulty.
        /// </summary>
        private float CalculateSkillGain(float currentSkill, float oreSkillRequired)
        {
            // Higher skill gain for mining ores close to your skill level
            float difficulty = oreSkillRequired / (currentSkill + 1f);
            float baseGain = 0.1f;

            if (difficulty > 0.8f && difficulty < 1.2f)
                baseGain = 0.3f; // Optimal difficulty range
            else if (difficulty > 0.5f && difficulty < 1.5f)
                baseGain = 0.2f;

            // Reduce gains at very high skill levels
            if (currentSkill > 90f)
                baseGain *= 0.5f;

            return baseGain;
        }

        /// <summary>
        /// Get information about an ore type.
        /// </summary>
        public OreData GetOreData(string oreID)
        {
            return _oreTable.TryGetValue(oreID, out OreData data) ? data : null;
        }

        /// <summary>
        /// Get all registered ore types.
        /// </summary>
        public IEnumerable<OreData> GetAllOres()
        {
            return _oreTable.Values;
        }

        /// <summary>
        /// Get current drop chance for a given skill level (for UI/debugging).
        /// </summary>
        public float GetDropChanceForSkill(float skill)
        {
            return CalculateDropChance(skill);
        }
    }
}