using System;
using System.Collections.Generic;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;
using UltimaValheim.Core;
using UltimaValheim.Combat.Data;

namespace UltimaValheim.Combat.Systems
{
    public class WeaponManager
    {
        private readonly WeaponDatabase _weaponDatabase;
        private readonly Dictionary<string, GameObject> _baseWeaponPrefabs = new Dictionary<string, GameObject>();
        private AssetBundle _weaponAssetBundle;

        private readonly string[] _materials = new[]
        {
            "iron", "shadow", "gold", "agapite", "verite", 
            "snow", "ice", "bloodrock", "valorite", "blackrock"
        };

        private const string ASSET_BUNDLE_NAME = "uoweapons";
        private const string LONGSWORD_ASSET_PREFIX = "UOlongsword_";

        // Hardcoded weapon stats (from your CSV)
        private readonly Dictionary<string, WeaponStats> _weaponStats = new Dictionary<string, WeaponStats>
        {
            ["iron"] = new WeaponStats { Base = 25, Ruin = 28, Might = 31, Force = 34, Power = 37, Vanquishing = 40 },
            ["shadow"] = new WeaponStats { Base = 30, Ruin = 33, Might = 36, Force = 39, Power = 42, Vanquishing = 45 },
            ["gold"] = new WeaponStats { Base = 35, Ruin = 38, Might = 41, Force = 44, Power = 47, Vanquishing = 50 },
            ["agapite"] = new WeaponStats { Base = 40, Ruin = 43, Might = 46, Force = 49, Power = 52, Vanquishing = 55 },
            ["verite"] = new WeaponStats { Base = 45, Ruin = 48, Might = 51, Force = 54, Power = 57, Vanquishing = 60 },
            ["snow"] = new WeaponStats { Base = 50, Ruin = 53, Might = 56, Force = 59, Power = 62, Vanquishing = 65 },
            ["ice"] = new WeaponStats { Base = 55, Ruin = 58, Might = 61, Force = 64, Power = 67, Vanquishing = 70 },
            ["bloodrock"] = new WeaponStats { Base = 60, Ruin = 63, Might = 66, Force = 69, Power = 72, Vanquishing = 75 },
            ["valorite"] = new WeaponStats { Base = 65, Ruin = 68, Might = 71, Force = 74, Power = 77, Vanquishing = 80 },
            ["blackrock"] = new WeaponStats { Base = 70, Ruin = 73, Might = 76, Force = 79, Power = 82, Vanquishing = 85 }
        };

        public WeaponManager(WeaponDatabase weaponDatabase)
        {
            _weaponDatabase = weaponDatabase;
        }

        private void LoadAssetBundle()
        {
            try
            {
#if DEBUG
                CoreAPI.Log.LogInfo($"[WeaponManager] Embedded resources: {string.Join(", ", typeof(WeaponManager).Assembly.GetManifestResourceNames())}");
#endif

                _weaponAssetBundle = Jotunn.Utils.AssetUtils.LoadAssetBundleFromResources(ASSET_BUNDLE_NAME);

                if (_weaponAssetBundle != null)
                {
                    CoreAPI.Log.LogInfo($"[WeaponManager] Successfully loaded {ASSET_BUNDLE_NAME} asset bundle from embedded resources");

#if DEBUG
                    string[] assetNames = _weaponAssetBundle.GetAllAssetNames();
                    CoreAPI.Log.LogInfo($"[WeaponManager] Bundle contains {assetNames.Length} assets");
#endif
                }
                else
                {
                    CoreAPI.Log.LogError($"[WeaponManager] Failed to load {ASSET_BUNDLE_NAME} asset bundle from embedded resources");
                }
            }
            catch (Exception ex)
            {
                CoreAPI.Log.LogError($"[WeaponManager] Error loading asset bundle: {ex}");
            }
        }

        public void RegisterWeapons()
        {
            CoreAPI.Log.LogInfo("[WeaponManager] Registering weapons...");

            LoadAssetBundle();

            if (_weaponAssetBundle == null)
            {
                CoreAPI.Log.LogError("[WeaponManager] Cannot register weapons - asset bundle not loaded!");
                return;
            }

            PrefabManager.OnVanillaPrefabsAvailable += () =>
            {
                RegisterBaseWeapons();
                CoreAPI.Log.LogInfo($"[WeaponManager] Registered {_baseWeaponPrefabs.Count} base weapon prefabs");
            };
        }

        private void RegisterBaseWeapons()
        {
            foreach (string material in _materials)
            {
                RegisterBaseWeapon("Longsword", material);
            }
        }

        private void RegisterBaseWeapon(string weaponType, string material)
        {
            // Check if we have stats for this material
            if (!_weaponStats.ContainsKey(material))
            {
                CoreAPI.Log.LogWarning($"[WeaponManager] No stats found for {material} {weaponType}");
                return;
            }

            // Asset name in bundle (e.g., "UOlongsword_iron")
            string assetName = GetAssetName(weaponType, material);
            
            // Load asset from bundle - DON'T Instantiate!
            GameObject weaponPrefab = _weaponAssetBundle.LoadAsset<GameObject>(assetName);
            if (weaponPrefab == null)
            {
                CoreAPI.Log.LogError($"[WeaponManager] Asset not found in bundle: {assetName}");
                return;
            }

            // Prefab name for Valheim (e.g., "IronLongsword")
            string prefabName = GetPrefabName(weaponType, material);
            
            // Display name (e.g., "Iron Longsword")
            string displayName = GetDisplayName(weaponType, material, WeaponQuality.None);

            try
            {
                // Set prefab name
                weaponPrefab.name = prefabName;

                // Get damage value
                float damage = GetDamageForQuality(material, WeaponQuality.None);

                // ONLY set the damage - preserve all other Unity prefab data!
                ItemDrop itemDrop = weaponPrefab.GetComponent<ItemDrop>();
                if (itemDrop != null)
                {
                    itemDrop.m_itemData.m_shared.m_damages.m_slash = damage;
                    
                    // Set localization keys
                    itemDrop.m_itemData.m_shared.m_name = $"${prefabName}";
                    itemDrop.m_itemData.m_shared.m_description = $"${prefabName}_description";
                }
                else
                {
                    CoreAPI.Log.LogError($"[WeaponManager] No ItemDrop component on {assetName}");
                    return;
                }

                // Create CustomItem with fixReference: true (CRITICAL!)
                CustomItem customWeapon = new CustomItem(weaponPrefab, fixReference: true);
                ItemManager.Instance.AddItem(customWeapon);

                // Add localization
                AddWeaponLocalization(prefabName, displayName);

                // Store reference
                _baseWeaponPrefabs[prefabName] = weaponPrefab;

                CoreAPI.Log.LogInfo($"[WeaponManager] Registered base weapon: {prefabName} ({damage} damage)");
            }
            catch (Exception ex)
            {
                CoreAPI.Log.LogError($"[WeaponManager] Failed to register {prefabName}: {ex}");
            }
        }

        private string GetAssetName(string weaponType, string material)
        {
            return $"{LONGSWORD_ASSET_PREFIX}{material.ToLower()}";
        }

        private string GetPrefabName(string weaponType, string material)
        {
            string materialCap = char.ToUpper(material[0]) + material.Substring(1);
            return $"{materialCap}{weaponType}";
        }

        private string GetDisplayName(string weaponType, string material, WeaponQuality quality)
        {
            string materialCap = char.ToUpper(material[0]) + material.Substring(1);
            string baseName = $"{materialCap} {weaponType}";

            if (quality == WeaponQuality.None)
            {
                return baseName;
            }

            string qualityCap = char.ToUpper(quality.ToString()[0]) + quality.ToString().Substring(1);
            return $"{baseName} of {qualityCap}";
        }

        private float GetDamageForQuality(string material, WeaponQuality quality)
        {
            if (!_weaponStats.ContainsKey(material))
                return 10; // Fallback

            WeaponStats stats = _weaponStats[material];
            
            return quality switch
            {
                WeaponQuality.None => stats.Base,
                WeaponQuality.Ruin => stats.Ruin,
                WeaponQuality.Might => stats.Might,
                WeaponQuality.Force => stats.Force,
                WeaponQuality.Power => stats.Power,
                WeaponQuality.Vanquishing => stats.Vanquishing,
                _ => stats.Base
            };
        }

        private void AddWeaponLocalization(string prefabName, string displayName)
        {
            LocalizationManager.Instance.GetLocalization().AddTranslation("English", 
                new Dictionary<string, string>
                {
                    { prefabName, displayName },
                    { $"{prefabName}_description", $"A {displayName.ToLower()}" }
                });
        }

        /// <summary>
        /// Create a weapon with specific quality (runtime generation for loot/crafting)
        /// </summary>
        public GameObject CreateWeaponWithQuality(string material, WeaponQuality quality)
        {
            string weaponType = "Longsword";
            string basePrefabName = GetPrefabName(weaponType, material);

            // Get the base prefab
            if (!_baseWeaponPrefabs.TryGetValue(basePrefabName, out GameObject basePrefab))
            {
                CoreAPI.Log.LogWarning($"[WeaponManager] Base prefab not found: {basePrefabName}");
                return null;
            }

            try
            {
                // Clone the base weapon
                GameObject weaponInstance = UnityEngine.Object.Instantiate(basePrefab);
                
                // Set unique name
                string displayName = GetDisplayName(weaponType, material, quality);
                weaponInstance.name = displayName.Replace(" ", "");

                // Set damage based on quality
                float damage = GetDamageForQuality(material, quality);

                // Configure with quality stats
                ItemDrop itemDrop = weaponInstance.GetComponent<ItemDrop>();
                if (itemDrop != null)
                {
                    itemDrop.m_itemData.m_shared.m_damages.m_slash = damage;

                    // Set display name directly (since this is runtime-generated)
                    itemDrop.m_itemData.m_shared.m_name = displayName;
                    itemDrop.m_itemData.m_shared.m_description = $"A powerful {displayName.ToLower()}";
                }

                CoreAPI.Log.LogInfo($"[WeaponManager] Created weapon: {displayName} ({damage} damage)");
                return weaponInstance;
            }
            catch (Exception ex)
            {
                CoreAPI.Log.LogError($"[WeaponManager] Failed to create weapon: {ex}");
                return null;
            }
        }
    }

    /// <summary>
    /// Simple stats structure for hardcoded weapon data
    /// </summary>
    public struct WeaponStats
    {
        public float Base;
        public float Ruin;
        public float Might;
        public float Force;
        public float Power;
        public float Vanquishing;
    }
}
