using System;
using System.Collections.Generic;
using System.Linq;

namespace UltimaValheim.Skills
{
    /// <summary>
    /// Skill category grouping.
    /// </summary>
    public enum SkillCategory
    {
        Combat,
        Magic,
        Crafting,
        Resource,
        Utility,
        Custom
    }

    /// <summary>
    /// Definition of a skill including metadata.
    /// </summary>
    public class SkillDefinition
    {
        public string SkillName { get; set; }
        public string Description { get; set; }
        public SkillCategory Category { get; set; }
        public bool IsCustom { get; set; }

        public SkillDefinition(string skillName, string description, SkillCategory category, bool isCustom = false)
        {
            SkillName = skillName;
            Description = description;
            Category = category;
            IsCustom = isCustom;
        }
    }

    /// <summary>
    /// Manages all registered skill definitions.
    /// Allows other modules to register custom skills.
    /// </summary>
    public class SkillDefinitionManager
    {
        private Dictionary<string, SkillDefinition> _skills = new Dictionary<string, SkillDefinition>();

        /// <summary>
        /// Register a new skill definition.
        /// </summary>
        public bool RegisterSkill(string skillName, string description, SkillCategory category, bool isCustom = false)
        {
            if (string.IsNullOrEmpty(skillName))
                return false;

            if (_skills.ContainsKey(skillName))
            {
                // Skill already registered
                return false;
            }

            var definition = new SkillDefinition(skillName, description, category, isCustom);
            _skills[skillName] = definition;

            return true;
        }

        /// <summary>
        /// Get a skill definition by name.
        /// </summary>
        public SkillDefinition GetSkill(string skillName)
        {
            if (_skills.TryGetValue(skillName, out var definition))
                return definition;

            return null;
        }

        /// <summary>
        /// Check if a skill is registered.
        /// </summary>
        public bool IsSkillRegistered(string skillName)
        {
            return _skills.ContainsKey(skillName);
        }

        /// <summary>
        /// Get all registered skills.
        /// </summary>
        public List<SkillDefinition> GetAllSkills()
        {
            return _skills.Values.ToList();
        }

        /// <summary>
        /// Get skills by category.
        /// </summary>
        public List<SkillDefinition> GetSkillsByCategory(SkillCategory category)
        {
            return _skills.Values
                .Where(s => s.Category == category)
                .ToList();
        }

        /// <summary>
        /// Get all skill names.
        /// </summary>
        public List<string> GetAllSkillNames()
        {
            return _skills.Keys.ToList();
        }

        /// <summary>
        /// Unregister a skill (only for custom skills).
        /// </summary>
        public bool UnregisterSkill(string skillName)
        {
            if (!_skills.TryGetValue(skillName, out var definition))
                return false;

            // Only allow unregistering custom skills
            if (!definition.IsCustom)
                return false;

            _skills.Remove(skillName);
            return true;
        }

        /// <summary>
        /// Get count of registered skills.
        /// </summary>
        public int GetSkillCount()
        {
            return _skills.Count;
        }

        /// <summary>
        /// Get count of skills by category.
        /// </summary>
        public int GetSkillCountByCategory(SkillCategory category)
        {
            return _skills.Values.Count(s => s.Category == category);
        }
    }
}