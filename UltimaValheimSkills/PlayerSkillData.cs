using System;
using System.Collections.Generic;
using System.Linq;

namespace UltimaValheim.Skills
{
    /// <summary>
    /// Stores all skill values for a single player.
    /// Supports serialization for persistence.
    /// </summary>
    [Serializable]
    public class PlayerSkillData
    {
        // Dictionary of skill name -> skill value (0-100)
        private Dictionary<string, float> _skills = new Dictionary<string, float>();

        // Default skill value for new skills
        private const float DEFAULT_SKILL = 10f;

        /// <summary>
        /// Get a skill value. Returns default (50) if skill not yet trained.
        /// </summary>
        public float GetSkill(string skillName)
        {
            if (_skills.TryGetValue(skillName, out float value))
                return value;

            return DEFAULT_SKILL;
        }

        /// <summary>
        /// Set a skill value. Automatically clamps to 0-100 range.
        /// </summary>
        public void SetSkill(string skillName, float value)
        {
            _skills[skillName] = UnityEngine.Mathf.Clamp(value, 0f, 100f);
        }

        /// <summary>
        /// Check if player has trained a specific skill.
        /// </summary>
        public bool HasSkill(string skillName)
        {
            return _skills.ContainsKey(skillName);
        }

        /// <summary>
        /// Get all trained skills.
        /// </summary>
        public Dictionary<string, float> GetAllSkills()
        {
            return new Dictionary<string, float>(_skills);
        }

        /// <summary>
        /// Get total skill points (sum of all skills).
        /// </summary>
        public float GetTotalSkillPoints()
        {
            return _skills.Values.Sum();
        }

        /// <summary>
        /// Reset a skill to default value.
        /// </summary>
        public void ResetSkill(string skillName)
        {
            if (_skills.ContainsKey(skillName))
            {
                _skills[skillName] = DEFAULT_SKILL;
            }
        }

        /// <summary>
        /// Reset all skills to default values.
        /// </summary>
        public void ResetAllSkills()
        {
            var skillNames = _skills.Keys.ToList();
            foreach (var skillName in skillNames)
            {
                _skills[skillName] = DEFAULT_SKILL;
            }
        }

        /// <summary>
        /// Serialize skill data to ZPackage for network sync.
        /// </summary>
        public void Serialize(ZPackage package)
        {
            // Write number of skills
            package.Write(_skills.Count);

            // Write each skill name and value
            foreach (var kvp in _skills)
            {
                package.Write(kvp.Key);
                package.Write(kvp.Value);
            }
        }

        /// <summary>
        /// Deserialize skill data from ZPackage.
        /// </summary>
        public void Deserialize(ZPackage package)
        {
            _skills.Clear();

            // Read number of skills
            int count = package.ReadInt();

            // Read each skill
            for (int i = 0; i < count; i++)
            {
                string skillName = package.ReadString();
                float value = package.ReadSingle();
                _skills[skillName] = value;
            }
        }

        /// <summary>
        /// Get skill statistics summary.
        /// </summary>
        public SkillStatistics GetStatistics()
        {
            return new SkillStatistics
            {
                TotalSkills = _skills.Count,
                TotalSkillPoints = GetTotalSkillPoints(),
                AverageSkillLevel = _skills.Count > 0 ? GetTotalSkillPoints() / _skills.Count : 0f,
                HighestSkill = _skills.Count > 0 ? _skills.Values.Max() : 0f,
                LowestSkill = _skills.Count > 0 ? _skills.Values.Min() : 0f
            };
        }
    }

    /// <summary>
    /// Statistics about a player's skills.
    /// </summary>
    public struct SkillStatistics
    {
        public int TotalSkills;
        public float TotalSkillPoints;
        public float AverageSkillLevel;
        public float HighestSkill;
        public float LowestSkill;
    }
}