using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UltimaValheim.Core;

namespace UltimaValheim.Mining
{
    /// <summary>
    /// Manages the Ultima Online-style mining system.
    /// Handles ore drops based on player mining skill with throttling and optimizations.
    /// </summary>
    public class MiningSystem
    {
        private readonly Dictionary<string, OreDefinition> _oreTable = new Dictionary<string, OreDefinition>();
        private readonly Dictionary<long, float> _lastMineAttempt = new Dictionary<long, float>();
        private readonly Dictionary<int, OreDefinition[]> _oreBrackets = new Dictionary<int, OreDefinition[]>();
        private const float MINE_COOLDOWN = 1f; // 1 second cooldown between ore checks

        // ThreadStatic RNG for better performance and thread safety
        [System.ThreadStatic]
        private static System.Random _localRng;
        private static System.Random RNG => _localRng ?? (_localRng = new System.Random(Environment.TickCount ^ System.Threading.Thread.CurrentThread.ManagedThreadId));

        public MiningSystem()
        {
            InitializeOreTable();
            CoreAPI.Log.LogInfo("[MiningSystem] Initialized with " + _oreTable.Count + " ore types.");
        }

        /// <summary>
        /// Initialize the ore table from CSV data.
        /// This is loaded once at startup and cached for performance.
        /// </summary>
        private void InitializeOreTable()
        {
            // Data from: Valehim_to_UO_Server_-_Ores_materials.csv
            // NOTE: Using vanilla IronOre instead of custom UOiron_ore
            AddOre("IronOre", "Iron Ore", 0f, 80f, 2, null, "Iron");
            AddOre("UOshadow_ore", "Shadow Ore", 30f, 70f, 2, "UOminerock_shadow", "UOshadow_ingot");
            AddOre("UOgold_ore", "Gold Ore", 40f, 60f, 2, "UOminerock_gold", "UOgold_ingot");
            AddOre("UOagapite_ore", "Agapite Ore", 50f, 50f, 2, "UOminerock_agapite", "UOagapite_ingot");
            AddOre("UOverite_ore", "Verite Ore", 60f, 40f, 1, "UOminerock_verite", "UOverite_ingot");
            AddOre("UOsnow_ore", "Snow Ore", 70f, 35f, 1, "UOminerock_valorite", "UOsnow_ingot");
            AddOre("UOice_ore", "Ice Ore", 75f, 35f, 1, "UOminerock_bloodrock", "UOice_ingot");
            AddOre("UObloodrock_ore", "Bloodrock Ore", 90f, 20f, 1, "UOminerock_ice", "UObloodrock_ingot");
            AddOre("UOvalorite_ore", "Valorite Ore", 95f, 20f, 1, "UOminerock_snow", "UOvalorite_ingot");
            AddOre("UOblackrock_ore", "Blackrock Ore", 95f, 20f, 1, "UOminerock_blackrock", "UOblackrock_ingot");
        }

        private void AddOre(string oreID, string displayName, float skillReq, float chance, int dropAmount, string rockPrefab, string smeltsInto)
        {
            _oreTable[oreID] = new OreDefinition
            {
                OreID = oreID,
                DisplayName = displayName,
                SkillRequired = skillReq,
                BaseChance = chance,
                DropAmount = dropAmount,
                RockPrefab = rockPrefab,
                SmeltsInto = smeltsInto
            };
        }

        /// <summary>
        /// Precompute weighted ore brackets for fast selection.
        /// Called once during initialization to eliminate runtime LINQ allocations.
        /// </summary>
        public void PrecomputeOreBrackets()
        {
            for (int skill = 0; skill <= 100; skill += 10)
            {
                var eligible = new List<OreDefinition>();
                foreach (var ore in _oreTable.Values)
                {
                    if (skill >= ore.SkillRequired)
                    {
                        eligible.Add(ore);
                    }
                }

                if (eligible.Count == 0)
                {
                    _oreBrackets[skill] = new OreDefinition[0];
                    continue;
                }

                // Build weighted array
                var weighted = new List<OreDefinition>();
                foreach (var ore in eligible)
                {
                    int weight = Mathf.RoundToInt(ore.SelectionWeight);
                    for (int i = 0; i < weight; i++)
                    {
                        weighted.Add(ore);
                    }
                }

                _oreBrackets[skill] = weighted.ToArray();
            }

            CoreAPI.Log.LogInfo($"[MiningSystem] Precomputed {_oreBrackets.Count} ore brackets for fast selection.");
        }

        /// <summary>
        /// Handle a mining attempt by a player on a rock.
        /// This is called when player damages a rock with a pickaxe.
        /// Includes throttling to prevent spam.
        /// </summary>
        public void OnRockMined(Player player, GameObject rock)
        {
            if (player == null || rock == null)
                return;

            long playerID = player.GetPlayerID();

            // Throttle: Only check once per second per player
            float currentTime = Time.time;
            if (_lastMineAttempt.TryGetValue(playerID, out float lastAttempt))
            {
                if (currentTime - lastAttempt < MINE_COOLDOWN)
                {
                    return; // Too soon, ignore
                }
            }

            _lastMineAttempt[playerID] = currentTime;

            // Get player's mining skill
            float miningSkill = GetPlayerMiningSkill(player);

            // Determine which ore to give based on skill
            OreDefinition selectedOre = SelectOreForSkill(miningSkill);

            if (selectedOre == null)
            {
                // No ore available for this skill level
                return;
            }

            // Roll for success
            if (!RollForOre(selectedOre, miningSkill))
            {
                // Failed to mine ore this time
                return;
            }

            // Success! Give the player ore
            GiveOreToPlayer(player, selectedOre);

            // TODO: Add skill gain logic when Skills module is implemented
        }

        /// <summary>
        /// Get the player's current mining skill.
        /// TODO: Replace with SkillsAPI when Skills module is implemented.
        /// </summary>
        private float GetPlayerMiningSkill(Player player)
        {
            // Hardcoded for now - will be replaced with:
            // return CoreAPI.PlayerData.GetFloat(player, "Mining_Skill", 0f);

            // For testing: Return a skill value from player data (defaults to 50)
            return CoreAPI.PlayerData.GetFloat(player, "Mining_Skill", 50f);
        }

        /// <summary>
        /// Select which ore the player gets based on their skill level.
        /// Uses precomputed weighted brackets for O(1) selection.
        /// </summary>
        private OreDefinition SelectOreForSkill(float miningSkill)
        {
            // Round skill to nearest bracket (0, 10, 20, ... 100)
            int bracket = Mathf.Clamp(Mathf.FloorToInt(miningSkill / 10f) * 10, 0, 100);

            if (!_oreBrackets.TryGetValue(bracket, out var ores) || ores.Length == 0)
                return null;

            // Fast O(1) random selection from precomputed weighted array
            return ores[RNG.Next(ores.Length)];
        }

        /// <summary>
        /// Roll RNG to determine if player successfully mines the ore.
        /// </summary>
        private bool RollForOre(OreDefinition ore, float miningSkill)
        {
            // Base chance from ore definition
            float successChance = ore.BaseChance;

            // TODO: Add skill bonus later if desired
            // successChance += (miningSkill - ore.SkillRequired) * 0.1f;

            float roll = (float)(RNG.NextDouble() * 100f);
            return roll <= successChance;
        }

        /// <summary>
        /// Give ore to the player.
        /// Server-authoritative: only server spawns items.
        /// </summary>
        private void GiveOreToPlayer(Player player, OreDefinition ore)
        {
            // Only server should spawn items
            if (!ZNet.instance.IsServer())
                return;

            try
            {
#if DEBUG
                CoreAPI.Log.LogInfo($"[MiningSystem] Player {player.GetPlayerName()} mined {ore.DropAmount}x {ore.DisplayName}");
#endif

                // Get the ore prefab from ObjectDB
                GameObject orePrefab = ObjectDB.instance.GetItemPrefab(ore.OreID);

                if (orePrefab != null)
                {
                    // Spawn the ore drops near the player with better spread
                    for (int i = 0; i < ore.DropAmount; i++)
                    {
                        // Larger random offset for better spread (prevents stacking)
                        Vector3 randomOffset = UnityEngine.Random.insideUnitSphere * 0.5f;
                        randomOffset.y = Mathf.Abs(randomOffset.y) + 1.5f; // Keep drops above ground

                        Vector3 spawnPos = player.transform.position + randomOffset;
                        GameObject drop = UnityEngine.Object.Instantiate(orePrefab, spawnPos, Quaternion.identity);

                        // Make sure the drop is networked and add physics
                        if (drop.GetComponent<ZNetView>() != null)
                        {
                            var rb = drop.GetComponent<Rigidbody>();
                            if (rb != null)
                            {
                                // Add random force for nice spread effect
                                Vector3 force = UnityEngine.Random.insideUnitSphere * 2f;
                                force.y = Mathf.Abs(force.y) + 3f;
                                rb.AddForce(force, ForceMode.VelocityChange);
                            }
                        }
                    }

                    // Visual/audio feedback
                    player.Message(MessageHud.MessageType.Center, $"You mine {ore.DropAmount}x {ore.DisplayName}!");
                }
                else
                {
                    CoreAPI.Log.LogWarning($"[MiningSystem] Ore prefab not found: {ore.OreID}");
                    // Fallback message
                    player.Message(MessageHud.MessageType.Center, $"You would have mined {ore.DropAmount}x {ore.DisplayName}!");
                }
            }
            catch (Exception ex)
            {
                CoreAPI.Log.LogError($"[MiningSystem] Failed to give ore to player: {ex}");
            }
        }

        /// <summary>
        /// Set a player's mining skill (for testing/admin commands).
        /// </summary>
        public void SetPlayerMiningSkill(Player player, float skill)
        {
            if (player == null)
                return;

            skill = Mathf.Clamp(skill, 0f, 100f);
            CoreAPI.PlayerData.SetFloat(player, "Mining_Skill", skill);
            CoreAPI.Log.LogInfo($"[MiningSystem] Set {player.GetPlayerName()}'s Mining skill to {skill}");
        }

        /// <summary>
        /// Get ore definition by ID (for debugging/admin tools).
        /// </summary>
        public OreDefinition GetOreDefinition(string oreID)
        {
            _oreTable.TryGetValue(oreID, out var ore);
            return ore;
        }

        /// <summary>
        /// Get all ore definitions.
        /// </summary>
        public IEnumerable<OreDefinition> GetAllOres()
        {
            return _oreTable.Values;
        }
    }
}
