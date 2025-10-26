using System;
using System.Linq;
using UnityEngine;
using UltimaValheim.Core;

namespace UltimaValheim.Skills
{
    /// <summary>
    /// Console commands for Skills module debugging and admin control.
    /// These commands can be used in the F5 console in-game.
    /// </summary>
    public class SkillsConsoleCommands
    {
        private SkillsModule _skillsModule;

        public SkillsConsoleCommands(SkillsModule skillsModule)
        {
            _skillsModule = skillsModule;
            RegisterCommands();
        }

        private void RegisterCommands()
        {
            // Note: In a real implementation, you would register these with
            // Valheim's console system or a command framework
            // For now, this shows the structure

            CoreAPI.Log.LogInfo("[Skills] Console commands ready (use F5 console)");
        }

        /// <summary>
        /// Show player's current skills.
        /// Usage: skills.show
        /// </summary>
        public void ShowSkills(Player player)
        {
            if (player == null)
            {
                CoreAPI.Log.LogWarning("[Skills] No player specified");
                return;
            }

            var stats = GetPlayerStats(player.GetPlayerID());

            Console.instance.Print($"=== Skills for {player.GetPlayerName()} ===");
            Console.instance.Print($"Total Skills: {stats.TotalSkills}");
            Console.instance.Print($"Total Points: {stats.TotalSkillPoints:F1} / {_skillsModule.GetType().GetField("_totalSkillCap", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(_skillsModule)}");
            Console.instance.Print($"Average Level: {stats.AverageSkillLevel:F1}");
            Console.instance.Print("");

            // Show all skills grouped by category
            var allDefs = _skillsModule.GetAllSkillDefinitions();
            var categories = allDefs.GroupBy(d => d.Category).OrderBy(g => g.Key);

            foreach (var category in categories)
            {
                Console.instance.Print($"--- {category.Key} ---");
                foreach (var def in category.OrderBy(d => d.SkillName))
                {
                    float value = _skillsModule.GetSkill(player, def.SkillName);
                    Console.instance.Print($"  {def.SkillName}: {value:F1}");
                }
                Console.instance.Print("");
            }
        }

        /// <summary>
        /// Set a specific skill value.
        /// Usage: skills.set Mining 100
        /// </summary>
        public void SetSkill(Player player, string skillName, float value)
        {
            if (player == null)
            {
                CoreAPI.Log.LogWarning("[Skills] No player specified");
                return;
            }

            if (string.IsNullOrEmpty(skillName))
            {
                Console.instance.Print("Usage: skills.set <skillname> <value>");
                return;
            }

            _skillsModule.SetSkill(player, skillName, value);
            Console.instance.Print($"Set {skillName} to {value:F1} for {player.GetPlayerName()}");
        }

        /// <summary>
        /// Give skill experience.
        /// Usage: skills.gain Mining 50
        /// </summary>
        public void GainSkillExperience(Player player, string skillName, float difficulty)
        {
            if (player == null)
            {
                CoreAPI.Log.LogWarning("[Skills] No player specified");
                return;
            }

            if (string.IsNullOrEmpty(skillName))
            {
                Console.instance.Print("Usage: skills.gain <skillname> <difficulty>");
                return;
            }

            float oldValue = _skillsModule.GetSkill(player, skillName);
            _skillsModule.AddSkillExperience(player, skillName, difficulty, 1.0f);
            float newValue = _skillsModule.GetSkill(player, skillName);

            Console.instance.Print($"{player.GetPlayerName()} gained experience in {skillName}");
            Console.instance.Print($"  {oldValue:F2} -> {newValue:F2} (+{newValue - oldValue:F2})");
        }

        /// <summary>
        /// Reset a skill to default (50).
        /// Usage: skills.reset Mining
        /// </summary>
        public void ResetSkill(Player player, string skillName)
        {
            if (player == null)
            {
                CoreAPI.Log.LogWarning("[Skills] No player specified");
                return;
            }

            if (string.IsNullOrEmpty(skillName))
            {
                Console.instance.Print("Usage: skills.reset <skillname>");
                return;
            }

            _skillsModule.SetSkill(player, skillName, 50f);
            Console.instance.Print($"Reset {skillName} to 50.0 for {player.GetPlayerName()}");
        }

        /// <summary>
        /// Set all skills to a value.
        /// Usage: skills.setall 75
        /// </summary>
        public void SetAllSkills(Player player, float value)
        {
            if (player == null)
            {
                CoreAPI.Log.LogWarning("[Skills] No player specified");
                return;
            }

            var allSkills = _skillsModule.GetAllSkillDefinitions();
            foreach (var skill in allSkills)
            {
                _skillsModule.SetSkill(player, skill.SkillName, value);
            }

            Console.instance.Print($"Set all {allSkills.Count} skills to {value:F1} for {player.GetPlayerName()}");
        }

        /// <summary>
        /// Show skill gain simulation.
        /// Usage: skills.simulate Mining 50 100
        /// </summary>
        public void SimulateSkillGain(string skillName, float currentSkill, int attempts)
        {
            Console.instance.Print($"=== Simulating {attempts} attempts of {skillName} at skill {currentSkill:F1} ===");
            Console.instance.Print("");

            // Test various difficulties
            float[] difficulties = { currentSkill - 25, currentSkill - 10, currentSkill, currentSkill + 10, currentSkill + 25 };

            foreach (float difficulty in difficulties)
            {
                int successCount = 0;
                float totalGain = 0f;

                for (int i = 0; i < attempts; i++)
                {
                    // Simulate gain check
                    float skillDelta = difficulty - currentSkill;
                    if (skillDelta < -25f || skillDelta > 25f)
                        continue;

                    float gainChance = 1.0f - (Mathf.Abs(skillDelta) / 25f);
                    if (UnityEngine.Random.value <= gainChance)
                    {
                        successCount++;
                        float baseGain = 0.1f * (100f - currentSkill) / 100f;
                        totalGain += baseGain;
                    }
                }

                float avgGain = successCount > 0 ? totalGain / successCount : 0f;
                float successRate = (float)successCount / attempts * 100f;

                Console.instance.Print($"Difficulty {difficulty:F1} (delta {difficulty - currentSkill:+F1;-F1}): {successRate:F1}% success, avg gain {avgGain:F3}");
            }

            Console.instance.Print("");
            Console.instance.Print("Best gains are at difficulty = current skill");
        }

        /// <summary>
        /// List all registered skills.
        /// Usage: skills.list
        /// </summary>
        public void ListAllSkills()
        {
            var allSkills = _skillsModule.GetAllSkillDefinitions();

            Console.instance.Print($"=== All Registered Skills ({allSkills.Count}) ===");
            Console.instance.Print("");

            var categories = allSkills.GroupBy(s => s.Category).OrderBy(g => g.Key);

            foreach (var category in categories)
            {
                Console.instance.Print($"--- {category.Key} ({category.Count()}) ---");
                foreach (var skill in category.OrderBy(s => s.SkillName))
                {
                    Console.instance.Print($"  {skill.SkillName}: {skill.Description}");
                }
                Console.instance.Print("");
            }
        }

        /// <summary>
        /// Export player skills to console (for backup/debugging).
        /// Usage: skills.export
        /// </summary>
        public void ExportSkills(Player player)
        {
            if (player == null)
            {
                CoreAPI.Log.LogWarning("[Skills] No player specified");
                return;
            }

            Console.instance.Print($"=== Skills Export for {player.GetPlayerName()} ===");
            Console.instance.Print($"// Copy these commands to restore skills:");
            Console.instance.Print("");

            var allSkills = _skillsModule.GetAllSkillDefinitions();
            foreach (var skill in allSkills.OrderBy(s => s.SkillName))
            {
                float value = _skillsModule.GetSkill(player, skill.SkillName);
                if (value != 50f) // Only export non-default skills
                {
                    Console.instance.Print($"skills.set {skill.SkillName} {value:F1}");
                }
            }
        }

        /// <summary>
        /// Show detailed stats about skill system.
        /// Usage: skills.stats
        /// </summary>
        public void ShowStats(Player player)
        {
            if (player == null)
            {
                Console.instance.Print("=== Skills Module Stats ===");
                var allSkills = _skillsModule.GetAllSkillDefinitions();
                Console.instance.Print($"Total Registered Skills: {allSkills.Count}");
                Console.instance.Print($"  Combat: {allSkills.Count(s => s.Category == SkillCategory.Combat)}");
                Console.instance.Print($"  Magic: {allSkills.Count(s => s.Category == SkillCategory.Magic)}");
                Console.instance.Print($"  Crafting: {allSkills.Count(s => s.Category == SkillCategory.Crafting)}");
                Console.instance.Print($"  Resource: {allSkills.Count(s => s.Category == SkillCategory.Resource)}");
                Console.instance.Print($"  Utility: {allSkills.Count(s => s.Category == SkillCategory.Utility)}");
                Console.instance.Print($"  Custom: {allSkills.Count(s => s.Category == SkillCategory.Custom)}");
            }
            else
            {
                var stats = GetPlayerStats(player.GetPlayerID());

                Console.instance.Print($"=== Skill Stats for {player.GetPlayerName()} ===");
                Console.instance.Print($"Total Skills: {stats.TotalSkills}");
                Console.instance.Print($"Total Points: {stats.TotalSkillPoints:F1}");
                Console.instance.Print($"Average Level: {stats.AverageSkillLevel:F1}");
                Console.instance.Print($"Highest Skill: {stats.HighestSkill:F1}");
                Console.instance.Print($"Lowest Skill: {stats.LowestSkill:F1}");

                // Find which skills are highest/lowest
                var allSkills = _skillsModule.GetAllSkillDefinitions();
                var skillValues = allSkills.Select(s => new {
                    Name = s.SkillName,
                    Value = _skillsModule.GetSkill(player, s.SkillName)
                }).OrderByDescending(s => s.Value).ToList();

                Console.instance.Print("");
                Console.instance.Print("Top 5 Skills:");
                foreach (var skill in skillValues.Take(5))
                {
                    Console.instance.Print($"  {skill.Name}: {skill.Value:F1}");
                }
            }
        }

        private SkillStatistics GetPlayerStats(long playerID)
        {
            // Access via reflection since PlayerSkillData is internal
            var skillDataField = _skillsModule.GetType()
                .GetField("_playerSkills", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (skillDataField != null)
            {
                var playerSkills = skillDataField.GetValue(_skillsModule) as System.Collections.IDictionary;
                if (playerSkills != null && playerSkills.Contains(playerID))
                {
                    var skillData = playerSkills[playerID] as PlayerSkillData;
                    if (skillData != null)
                    {
                        return skillData.GetStatistics();
                    }
                }
            }

            return new SkillStatistics
            {
                TotalSkills = 0,
                TotalSkillPoints = 0f,
                AverageSkillLevel = 0f,
                HighestSkill = 0f,
                LowestSkill = 0f
            };
        }
    }
}