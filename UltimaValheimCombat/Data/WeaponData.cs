using System;

namespace UltimaValheim.Combat.Data
{
    /// <summary>
    /// Represents the base stats for a weapon from the balance CSV.
    /// Immutable data structure for performance.
    /// </summary>
    public readonly struct WeaponData
    {
        public readonly string WeaponName;
        public readonly string SkillType;
        public readonly string DamageType;
        public readonly string Material;
        public readonly float BaseDamage;
        
        // Quality tier final damage values (at skill 0)
        public readonly float RuinDamage;
        public readonly float MightDamage;
        public readonly float ForceDamage;
        public readonly float PowerDamage;
        public readonly float VanquishingDamage;
        
        // Quality tier damage at skill 100
        public readonly float RuinDamageAt100;
        public readonly float MightDamageAt100;
        public readonly float ForceDamageAt100;
        public readonly float PowerDamageAt100;
        public readonly float VanquishingDamageAt100;
        
        public readonly float SkillMultAt100;
        
        // Additional properties
        public readonly string StaggerVsPlayers;
        public readonly string SpeedNote;

        public WeaponData(
            string weaponName, 
            string skillType, 
            string damageType, 
            string material, 
            float baseDamage,
            float ruinDamage, 
            float ruinDamageAt100,
            float mightDamage, 
            float mightDamageAt100,
            float forceDamage, 
            float forceDamageAt100,
            float powerDamage, 
            float powerDamageAt100,
            float vanquishingDamage, 
            float vanquishingDamageAt100,
            float skillMultAt100,
            string staggerVsPlayers, 
            string speedNote)
        {
            WeaponName = weaponName;
            SkillType = skillType;
            DamageType = damageType;
            Material = material;
            BaseDamage = baseDamage;
            RuinDamage = ruinDamage;
            RuinDamageAt100 = ruinDamageAt100;
            MightDamage = mightDamage;
            MightDamageAt100 = mightDamageAt100;
            ForceDamage = forceDamage;
            ForceDamageAt100 = forceDamageAt100;
            PowerDamage = powerDamage;
            PowerDamageAt100 = powerDamageAt100;
            VanquishingDamage = vanquishingDamage;
            VanquishingDamageAt100 = vanquishingDamageAt100;
            SkillMultAt100 = skillMultAt100;
            StaggerVsPlayers = staggerVsPlayers;
            SpeedNote = speedNote;
        }

        /// <summary>
        /// Get the quality multiplier based on tier (0 = no quality, 1-5 = Ruin to Vanquishing)
        /// </summary>
        public float GetQualityMultiplier(int qualityTier)
        {
            if (BaseDamage <= 0) return 1.0f;
            
            switch (qualityTier)
            {
                case 0: return 1.0f; // No quality
                case 1: return RuinDamage / BaseDamage; // Ruin (1.5x)
                case 2: return MightDamage / BaseDamage; // Might (1.8x)
                case 3: return ForceDamage / BaseDamage; // Force (2.2x)
                case 4: return PowerDamage / BaseDamage; // Power (2.6x)
                case 5: return VanquishingDamage / BaseDamage; // Vanquishing (3.5x)
                default: return 1.0f;
            }
        }
        
        /// <summary>
        /// Get the material multiplier based on material name
        /// </summary>
        public static float GetMaterialMultiplier(string material)
        {
            return material?.ToLower() switch
            {
                "iron" => 1.0f,
                "shadow" => 1.2f,
                "gold" => 1.35f,
                "agapite" => 1.5f,
                "verite" => 1.8f,
                "snow" => 2.0f,
                "ice" => 2.2f,
                "bloodrock" => 2.5f,
                "valorite" => 3.0f,
                "blackrock" => 4.0f,
                _ => 1.0f
            };
        }
    }

    /// <summary>
    /// Weapon quality tiers (drop-only enhancements)
    /// </summary>
    public enum WeaponQuality
    {
        None = 0,
        Ruin = 1,        // 1.5x damage
        Might = 2,       // 1.8x damage
        Force = 3,       // 2.2x damage
        Power = 4,       // 2.6x damage
        Vanquishing = 5  // 3.5x damage
    }

    /// <summary>
    /// Cached damage stats for a player's current weapon.
    /// Struct for performance (value type, no GC allocation).
    /// </summary>
    public struct CachedDamageStats
    {
        public float BaseDamage;
        public float MaterialMultiplier;
        public float QualityMultiplier;
        public float SkillBonus;
        public int WeaponHash;      // For cache invalidation
        public float LastCalculated; // Time.time when calculated

        public float GetFinalDamage()
        {
            return BaseDamage * MaterialMultiplier * QualityMultiplier * (1.0f + SkillBonus);
        }
    }
}
