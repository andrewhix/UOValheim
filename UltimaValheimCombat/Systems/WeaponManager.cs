using System;
using System.Collections.Generic;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;
using UltimaValheim.Core;
using UltimaValheim.Combat.Data;

namespace UltimaValheim.Combat.Systems
{
    /// <summary>
    /// Manages weapon registration with Jotunn and weapon generation.
    /// Handles creation of base weapons and magic quality variants.
    /// </summary>
    public class WeaponManager
    {
        private readonly WeaponDatabase _weaponDatabase;
        private readonly Dictionary<string, GameObject> _baseWeaponPrefabs = new Dictionary<string, GameObject>();

        // Material ordering for progression
        private readonly string[] _materials = new[]
        {
            "iron", "shadow", "gold", "agapite", "verite", 
            "snow", "ice", "bloodrock", "valorite", "blackrock"
        };

        public WeaponManager(WeaponDatabase weaponDatabase)
        {
            _weaponDatabase = weaponDatabase;
        }

        /// <summary>
        /// Register all weapons with Jotunn.
        /// Currently only registers longswords - will expand to other weapon types later.
        /// </summary>
        public void RegisterWeapons()
        {
            CoreAPI.Log.LogInfo("[WeaponManager] Registering weapons...");

            // Wait for vanilla prefabs to be available
            PrefabManager.OnVanillaPrefabsAvailable += () =>
            {
                // Register longswords for all materials
                RegisterWeaponType("Longsword");

                CoreAPI.Log.LogInfo($"[WeaponManager] Registered {_baseWeaponPrefabs.Count} base weapon prefabs");
            };
        }

        /// <summary>
        /// Register all material variants for a weapon type
        /// </summary>
        private void RegisterWeaponType(string weaponType)
        {
            foreach (string material in _materials)
            {
                RegisterBaseWeapon(weaponType, material);
            }
        }

        /// <summary>
        /// Register a single base weapon (one material variant)
        /// </summary>
        private void RegisterBaseWeapon(string weaponType, string material)
        {
            // Get weapon data from database
            if (!_weaponDatabase.TryGetWeaponData(weaponType, material, out WeaponData weaponData))
            {
                CoreAPI.Log.LogWarning($"[WeaponManager] No data found for {material} {weaponType}");
                return;
            }

            // Create prefab name (e.g., "IronLongsword")
            string prefabName = GetPrefabName(weaponType, material);

            try
            {
                // Clone from vanilla weapon as base
                GameObject prefab = CreateWeaponPrefab(prefabName, weaponData);

                if (prefab == null)
                {
                    CoreAPI.Log.LogError($"[WeaponManager] Failed to create prefab for {prefabName}");
                    return;
                }

                // Create custom item
                CustomItem customItem = new CustomItem(prefab, fixReference: true);

                // Add crafting recipe
                AddCraftingRecipe(customItem, weaponData, prefabName);

                // Register with Jotunn
                ItemManager.Instance.AddItem(customItem);

                // Store reference
                _baseWeaponPrefabs[prefabName] = prefab;

                CoreAPI.Log.LogInfo($"[WeaponManager] Registered {prefabName}");
            }
            catch (Exception ex)
            {
                CoreAPI.Log.LogError($"[WeaponManager] Error registering {prefabName}: {ex}");
            }
        }

        /// <summary>
        /// Create weapon prefab from vanilla template
        /// </summary>
        private GameObject CreateWeaponPrefab(string prefabName, WeaponData weaponData)
        {
            // Clone from bronze sword as template
            GameObject template = PrefabManager.Instance.GetPrefab("SwordBronze");
            if (template == null)
            {
                CoreAPI.Log.LogError("[WeaponManager] Could not find bronze sword template!");
                return null;
            }

            GameObject prefab = PrefabManager.Instance.CreateClonedPrefab(prefabName, template);
            if (prefab == null)
                return null;

            // Get ItemDrop component and configure
            ItemDrop itemDrop = prefab.GetComponent<ItemDrop>();
            if (itemDrop != null)
            {
                ConfigureWeaponStats(itemDrop.m_itemData.m_shared, weaponData);
            }

            return prefab;
        }

        /// <summary>
        /// Configure weapon stats on SharedData
        /// </summary>
        private void ConfigureWeaponStats(ItemDrop.ItemData.SharedData shared, WeaponData weaponData)
        {
            // Set display name
            shared.m_name = $"{CapitalizeFirst(weaponData.Material)} {weaponData.WeaponName}";
            shared.m_description = $"A {weaponData.Material} {weaponData.WeaponName.ToLower()}";

            // Set damage
            shared.m_damages.m_damage = weaponData.BaseDamage;
            
            // Set damage type based on weapon
            switch (weaponData.DamageType.ToLower())
            {
                case "slash":
                    shared.m_damages.m_slash = weaponData.BaseDamage;
                    shared.m_damages.m_damage = 0;
                    break;
                case "pierce":
                    shared.m_damages.m_pierce = weaponData.BaseDamage;
                    shared.m_damages.m_damage = 0;
                    break;
                case "blunt":
                    shared.m_damages.m_blunt = weaponData.BaseDamage;
                    shared.m_damages.m_damage = 0;
                    break;
            }

            // Set attack speed based on speed note
            if (weaponData.SpeedNote.IndexOf("Fast", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                shared.m_attack.m_attackStamina = 10f; // Lower stamina cost
                shared.m_attack.m_speedFactor = 1.2f; // Faster attacks
            }
            else if (weaponData.SpeedNote.IndexOf("Slow", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                shared.m_attack.m_attackStamina = 20f; // Higher stamina cost
                shared.m_attack.m_speedFactor = 0.8f; // Slower attacks
            }
            else // Baseline
            {
                shared.m_attack.m_attackStamina = 15f;
                shared.m_attack.m_speedFactor = 1.0f;
            }

            // Set durability based on material tier
            shared.m_maxDurability = GetDurabilityForMaterial(weaponData.Material);
            shared.m_durabilityPerLevel = 50f;

            // Set skill type
            shared.m_skillType = GetSkillType(weaponData.SkillType);
        }

        /// <summary>
        /// Add crafting recipe for base weapon using Jotunn's CustomRecipe
        /// </summary>
        private void AddCraftingRecipe(CustomItem item, WeaponData weaponData, string prefabName)
        {
            try
            {
                // Get crafting station
                string stationName = GetCraftingStation(weaponData.Material);
                
                // Build recipe using native Recipe class
                Recipe recipe = ScriptableObject.CreateInstance<Recipe>();
                recipe.name = $"Recipe_{prefabName}";
                recipe.m_item = item.ItemDrop;
                recipe.m_amount = 1;
                recipe.m_enabled = true;
                
                // Set crafting station
                recipe.m_craftingStation = PrefabManager.Instance.GetPrefab(stationName)?.GetComponent<CraftingStation>();
                
                // Get requirements
                var requirements = GetCraftingRequirements(weaponData.Material);
                recipe.m_resources = requirements.ToArray();

                // Create CustomRecipe and add to manager
                CustomRecipe customRecipe = new CustomRecipe(recipe, fixReference: true, fixRequirementReferences: true);
                ItemManager.Instance.AddRecipe(customRecipe);

                CoreAPI.Log.LogInfo($"[WeaponManager] Added recipe for {prefabName}");
            }
            catch (Exception ex)
            {
                CoreAPI.Log.LogWarning($"[WeaponManager] Could not add recipe for {prefabName}: {ex.Message}");
            }
        }

        /// <summary>
        /// Get crafting requirements as Piece.Requirement array
        /// </summary>
        private List<Piece.Requirement> GetCraftingRequirements(string material)
        {
            var requirements = new List<Piece.Requirement>();

            // Wood handle for all weapons
            requirements.Add(CreateRequirement("Wood", 10));

            // Leather wrap
            requirements.Add(CreateRequirement("DeerHide", 2));

            // Material-specific ore/bar
            string materialItem = GetMaterialItemName(material);
            int materialAmount = GetMaterialAmount(material);
            requirements.Add(CreateRequirement(materialItem, materialAmount));

            return requirements;
        }

        /// <summary>
        /// Create a Piece.Requirement from item name and amount
        /// </summary>
        private Piece.Requirement CreateRequirement(string itemName, int amount)
        {
            GameObject itemPrefab = PrefabManager.Instance.GetPrefab(itemName);
            
            return new Piece.Requirement
            {
                m_resItem = itemPrefab?.GetComponent<ItemDrop>(),
                m_amount = amount,
                m_amountPerLevel = 0,
                m_recover = true
            };
        }

        /// <summary>
        /// Get the item name for crafting material
        /// </summary>
        private string GetMaterialItemName(string material)
        {
            return material.ToLower() switch
            {
                "iron" => "Iron",
                "shadow" => "UOshadow_ingot", // Custom ore from mining module
                "gold" => "UOgold_ingot",
                "agapite" => "UOagapite_ingot",
                "verite" => "UOverite_ingot",
                "snow" => "UOsnow_ingot",
                "ice" => "UOice_ingot",
                "bloodrock" => "UObloodrock_ingot",
                "valorite" => "UOvalorite_ingot",
                "blackrock" => "UOblackrock_ingot",
                _ => "Iron"
            };
        }

        /// <summary>
        /// Get material amount needed for crafting
        /// </summary>
        private int GetMaterialAmount(string material)
        {
            return material.ToLower() switch
            {
                "iron" => 20,
                "shadow" => 25,
                "gold" => 30,
                "agapite" => 35,
                "verite" => 40,
                "snow" => 45,
                "ice" => 50,
                "bloodrock" => 55,
                "valorite" => 60,
                "blackrock" => 70,
                _ => 20
            };
        }

        /// <summary>
        /// Get crafting station based on material tier
        /// </summary>
        private string GetCraftingStation(string material)
        {
            return material.ToLower() switch
            {
                "iron" or "shadow" => "forge",
                "gold" or "agapite" => "forge",
                "verite" or "snow" => "forge",
                "ice" or "bloodrock" => "blackforge",
                "valorite" or "blackrock" => "blackforge",
                _ => "forge"
            };
        }

        /// <summary>
        /// Get durability based on material
        /// </summary>
        private float GetDurabilityForMaterial(string material)
        {
            return material.ToLower() switch
            {
                "iron" => 100f,
                "shadow" => 125f,
                "gold" => 135f,
                "agapite" => 150f,
                "verite" => 180f,
                "snow" => 200f,
                "ice" => 225f,
                "bloodrock" => 250f,
                "valorite" => 300f,
                "blackrock" => 400f,
                _ => 100f
            };
        }

        /// <summary>
        /// Convert skill name to Valheim skill type
        /// </summary>
        private Skills.SkillType GetSkillType(string skillName)
        {
            return skillName.ToLower() switch
            {
                "swordsmanship" => Skills.SkillType.Swords,
                "macing" => Skills.SkillType.Clubs,
                "fencing" => Skills.SkillType.Knives,
                "archery" => Skills.SkillType.Bows,
                _ => Skills.SkillType.Swords
            };
        }

        /// <summary>
        /// Generate prefab name (e.g., "IronLongsword")
        /// </summary>
        private string GetPrefabName(string weaponType, string material)
        {
            return $"{CapitalizeFirst(material)}{weaponType}";
        }

        /// <summary>
        /// Capitalize first letter of string
        /// </summary>
        private string CapitalizeFirst(string s)
        {
            if (string.IsNullOrEmpty(s))
                return s;
            return char.ToUpper(s[0]) + s.Substring(1).ToLower();
        }

        #region Weapon Generation (Magic Qualities)

        /// <summary>
        /// Create a weapon with magic quality (called by Loot module)
        /// </summary>
        public ItemDrop.ItemData CreateWeaponWithQuality(string weaponType, string material, WeaponQuality quality)
        {
            string prefabName = GetPrefabName(weaponType, material);

            if (!_baseWeaponPrefabs.TryGetValue(prefabName, out GameObject basePrefab))
            {
                CoreAPI.Log.LogError($"[WeaponManager] Base prefab not found: {prefabName}");
                return null;
            }

            // Clone the base item data
            ItemDrop itemDrop = basePrefab.GetComponent<ItemDrop>();
            if (itemDrop == null)
                return null;

            ItemDrop.ItemData itemData = itemDrop.m_itemData.Clone();

            // Apply quality
            if (quality != WeaponQuality.None)
            {
                ApplyQuality(itemData, quality);
            }

            return itemData;
        }

        /// <summary>
        /// Apply magic quality to a weapon
        /// </summary>
        private void ApplyQuality(ItemDrop.ItemData itemData, WeaponQuality quality)
        {
            // Add quality bonus to display name
            string qualityName = quality.ToString();
            itemData.m_shared.m_name += $" of {qualityName}";

            // Store quality in custom data for damage calculator
            itemData.m_customData["UOV_Quality"] = ((int)quality).ToString();

            // Note: Actual damage bonus is applied by DamageCalculator at runtime
            // We don't modify the base damage here to keep prefab clean
        }

        #endregion
    }
}
