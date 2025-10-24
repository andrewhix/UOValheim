using System;

namespace UltimaValheim.Mining
{
    /// <summary>
    /// Defines an ore type that can be mined from rocks.
    /// Based on Ultima Online ore system.
    /// </summary>
    [Serializable]
    public class OreDefinition
    {
        /// <summary>
        /// Unique identifier for the ore (e.g., "UOiron_ore")
        /// </summary>
        public string OreID { get; set; }

        /// <summary>
        /// Display name (e.g., "Iron Ore")
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// Minimum mining skill required to mine this ore
        /// </summary>
        public float SkillRequired { get; set; }

        /// <summary>
        /// Base chance to successfully mine this ore (0-100)
        /// </summary>
        public float BaseChance { get; set; }

        /// <summary>
        /// Amount of ore dropped on successful mine
        /// </summary>
        public int DropAmount { get; set; }

        /// <summary>
        /// Prefab name of the rock this ore comes from (optional - for specific rock types)
        /// Leave null/empty to mine from any rock
        /// </summary>
        public string RockPrefab { get; set; }

        /// <summary>
        /// What this ore smelts into (e.g., "Iron Ingot")
        /// </summary>
        public string SmeltsInto { get; set; }

        /// <summary>
        /// Weight for random ore selection (higher = more common within skill range)
        /// Calculated as: 100 - SkillRequired
        /// </summary>
        public float SelectionWeight => Math.Max(1f, 100f - SkillRequired);

        public override string ToString()
        {
            return $"{DisplayName} (Skill: {SkillRequired}, Chance: {BaseChance}%, Drop: {DropAmount})";
        }
    }
}
