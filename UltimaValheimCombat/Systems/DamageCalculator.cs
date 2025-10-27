using System;
using System.Collections.Generic;
using UnityEngine;
using UltimaValheim.Core;
using UltimaValheim.Combat.Data;

namespace UltimaValheim.Combat.Systems
{
    /// <summary>
    /// Calculates damage with caching for performance.
    /// Implements the formula: (Base + Quality) × Material × (1 + Skill/100)
    /// </summary>
    public class DamageCalculator
    {
        private readonly WeaponDatabase _weaponDatabase;
        private readonly bool _skillsModuleAvailable;
        
        // Cache structure: playerID -> CachedDamageStats
        private readonly Dictionary<long, CachedDamageStats> _damageCache = new Dictionary<long, CachedDamageStats>();

        public DamageCalculator(WeaponDatabase weaponDatabase, bool skillsModuleAvailable)
        {
            _weaponDatabase = weaponDatabase;
            _skillsModuleAvailable = skillsModuleAvailable;
        }

        /// <summary>
        /// Calculate final damage for a player's current weapon.
        /// Uses cache when possible for performance.
        /// </summary>
        public float CalculateDamage(Player player, Character target, HitData hit)
        {
            if (player == null)
                return hit.GetTotalDamage(); // Fallback to vanilla

            long playerID = player.GetPlayerID();
            ItemDrop.ItemData weapon = player.GetCurrentWeapon();

            if (weapon == null)
                return hit.GetTotalDamage(); // No weapon, use vanilla

            // Try to get from cache
            if (TryGetCachedDamage(playerID, weapon, out float cachedDamage))
            {
                return cachedDamage;
            }

            // Cache miss - calculate and cache
            float damage = CalculateAndCache(player, weapon);
            return damage;
        }

        /// <summary>
        /// Calculate damage without caching (used for initial calculation)
        /// </summary>
        private float CalculateAndCache(Player player, ItemDrop.ItemData weapon)
        {
            long playerID = player.GetPlayerID();

            // Get weapon data from database
            if (!_weaponDatabase.TryGetWeaponData(weapon, out WeaponData weaponData))
            {
                // Not a custom weapon, return vanilla damage
                return weapon.GetDamage().GetTotalDamage();
            }

            // Step 1: Base damage from CSV
            float baseDamage = weaponData.BaseDamage;

            // Step 2: Add quality bonus (flat addition)
            int qualityTier = GetWeaponQuality(weapon);
            float qualityBonus = GetQualityDamageBonus(qualityTier);
            float damageWithQuality = baseDamage + qualityBonus;

            // Step 3: Apply material multiplier
            float materialMult = WeaponData.GetMaterialMultiplier(weaponData.Material);
            float damageWithMaterial = damageWithQuality * materialMult;

            // Step 4: Apply skill multiplier
            float skillBonus = GetSkillBonus(player, weaponData.SkillType);
            float finalDamage = damageWithMaterial * (1.0f + skillBonus);

            // Cache the result
            CachedDamageStats stats = new CachedDamageStats
            {
                BaseDamage = baseDamage,
                MaterialMultiplier = materialMult,
                QualityMultiplier = 1.0f, // Not used in current formula
                SkillBonus = skillBonus,
                WeaponHash = weapon.GetHashCode(),
                LastCalculated = Time.time
            };

            // Store additional data for reconstruction
            stats.BaseDamage = damageWithQuality; // Store base+quality for efficiency

            _damageCache[playerID] = stats;

            return finalDamage;
        }

        /// <summary>
        /// Try to get damage from cache. Returns true if cache hit.
        /// </summary>
        private bool TryGetCachedDamage(long playerID, ItemDrop.ItemData weapon, out float damage)
        {
            damage = 0f;

            if (!_damageCache.TryGetValue(playerID, out CachedDamageStats cached))
                return false;

            // Validate cache - check if weapon changed
            int currentWeaponHash = weapon.GetHashCode();
            if (cached.WeaponHash != currentWeaponHash)
                return false;

            // Cache is valid - reconstruct damage
            damage = cached.BaseDamage * cached.MaterialMultiplier * (1.0f + cached.SkillBonus);
            return true;
        }

        /// <summary>
        /// Get quality damage bonus (flat addition)
        /// </summary>
        private float GetQualityDamageBonus(int qualityTier)
        {
            return qualityTier switch
            {
                0 => 0f,    // No quality
                1 => 3f,    // Ruin
                2 => 6f,    // Might
                3 => 9f,    // Force
                4 => 12f,   // Power
                5 => 15f,   // Vanquishing
                _ => 0f
            };
        }

        /// <summary>
        /// Get weapon quality tier from item custom data
        /// </summary>
        private int GetWeaponQuality(ItemDrop.ItemData weapon)
        {
            // Check for custom data key "UOV_Quality"
            if (weapon.m_customData.TryGetValue("UOV_Quality", out string qualityStr))
            {
                if (int.TryParse(qualityStr, out int quality))
                {
                    return quality;
                }
            }

            return 0; // No quality
        }

        /// <summary>
        /// Get skill bonus for damage calculation
        /// </summary>
        private float GetSkillBonus(Player player, string skillType)
        {
            if (!_skillsModuleAvailable)
                return 0f; // No skills module, no bonus

            // Try to get skill level from Skills module
            try
            {
                // This will need to be adjusted based on Skills module API
                // For now, return a placeholder
                // TODO: Integrate with Skills module to get actual skill level
                
                // Placeholder: Check if we can get skill through events or API
                return 0f; // Will be implemented when Skills integration is complete
            }
            catch
            {
                return 0f;
            }
        }

        #region Cache Management

        /// <summary>
        /// Initialize cache for a player
        /// </summary>
        public void InitializePlayerCache(long playerID)
        {
            if (!_damageCache.ContainsKey(playerID))
            {
                _damageCache[playerID] = default;
            }
        }

        /// <summary>
        /// Clear cache for a specific player
        /// </summary>
        public void ClearPlayerCache(long playerID)
        {
            _damageCache.Remove(playerID);
        }

        /// <summary>
        /// Invalidate cache for a player (force recalculation on next hit)
        /// </summary>
        public void InvalidatePlayerCache(long playerID)
        {
            if (_damageCache.ContainsKey(playerID))
            {
                _damageCache.Remove(playerID);
            }
        }

        /// <summary>
        /// Clear all cached data
        /// </summary>
        public void Cleanup()
        {
            _damageCache.Clear();
        }

        /// <summary>
        /// Get cache statistics (for monitoring/debugging)
        /// </summary>
        public (int CachedPlayers, int TotalEntries) GetCacheStats()
        {
            return (_damageCache.Count, _damageCache.Count);
        }

        #endregion
    }
}
