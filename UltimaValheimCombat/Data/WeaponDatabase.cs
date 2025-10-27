using System;
using System.Collections.Generic;
using System.IO;
using UltimaValheim.Core;

namespace UltimaValheim.Combat.Data
{
    /// <summary>
    /// Database of weapon stats loaded from CSV.
    /// Provides lookup and query functionality for weapon data.
    /// </summary>
    public class WeaponDatabase
    {
        private readonly Dictionary<string, WeaponData> _weaponsByID = new Dictionary<string, WeaponData>();
        private readonly Dictionary<string, List<WeaponData>> _weaponsByMaterial = new Dictionary<string, List<WeaponData>>();

        public int GetWeaponCount() => _weaponsByID.Count;

        /// <summary>
        /// Load weapon data from CSV file
        /// </summary>
        public void LoadFromCSV(string csvPath)
        {
            CoreAPI.Log.LogInfo($"[WeaponDatabase] Loading weapons from: {csvPath}");

            _weaponsByID.Clear();
            _weaponsByMaterial.Clear();

            try
            {
                string[] lines = File.ReadAllLines(csvPath);
                if (lines.Length < 2)
                {
                    CoreAPI.Log.LogWarning($"[WeaponDatabase] CSV file is empty or missing header!");
                    return;
                }

                // Skip header row
                for (int i = 1; i < lines.Length; i++)
                {
                    try
                    {
                        WeaponData? weapon = ParseWeaponLine(lines[i]);
                        if (weapon.HasValue)
                        {
                            WeaponData weaponValue = weapon.Value;
                            // Store by weapon ID
                            string weaponID = GetWeaponID(weaponValue.WeaponName, weaponValue.Material);
                            _weaponsByID[weaponID] = weaponValue;

                            // Store by material for quick lookup
                            if (!_weaponsByMaterial.ContainsKey(weaponValue.Material))
                            {
                                _weaponsByMaterial[weaponValue.Material] = new List<WeaponData>();
                            }
                            _weaponsByMaterial[weaponValue.Material].Add(weaponValue);
                        }
                    }
                    catch (Exception ex)
                    {
                        CoreAPI.Log.LogWarning($"[WeaponDatabase] Failed to parse line {i}: {ex.Message}");
                    }
                }

                CoreAPI.Log.LogInfo($"[WeaponDatabase] Loaded {_weaponsByID.Count} weapons.");
            }
            catch (Exception ex)
            {
                CoreAPI.Log.LogError($"[WeaponDatabase] Failed to load CSV: {ex}");
            }
        }

        /// <summary>
        /// Parse a CSV line into a WeaponData object
        /// </summary>
        private WeaponData? ParseWeaponLine(string line)
        {
            string[] parts = line.Split(',');
            if (parts.Length < 21) // All columns from CSV
                return null;

            try
            {
                return new WeaponData(
                    weaponName: parts[0].Trim(),
                    skillType: parts[1].Trim(),
                    damageType: parts[2].Trim(),
                    material: parts[3].Trim().ToLower(),
                    baseDamage: float.Parse(parts[4].Trim()),
                    ruinDamage: float.Parse(parts[6].Trim()),
                    ruinDamageAt100: float.Parse(parts[7].Trim()),
                    mightDamage: float.Parse(parts[8].Trim()),
                    mightDamageAt100: float.Parse(parts[9].Trim()),
                    forceDamage: float.Parse(parts[10].Trim()),
                    forceDamageAt100: float.Parse(parts[11].Trim()),
                    powerDamage: float.Parse(parts[12].Trim()),
                    powerDamageAt100: float.Parse(parts[13].Trim()),
                    vanquishingDamage: float.Parse(parts[14].Trim()),
                    vanquishingDamageAt100: float.Parse(parts[15].Trim()),
                    skillMultAt100: float.Parse(parts[16].Trim()),
                    staggerVsPlayers: parts.Length > 19 ? parts[19].Trim() : "",
                    speedNote: parts.Length > 20 ? parts[20].Trim() : ""
                );
            }
            catch (Exception ex)
            {
                CoreAPI.Log.LogWarning($"[WeaponDatabase] Parse error: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Get weapon data by weapon ID
        /// </summary>
        public WeaponData? GetWeapon(string weaponID)
        {
            if (_weaponsByID.ContainsKey(weaponID))
                return _weaponsByID[weaponID];
            return null;
        }

        /// <summary>
        /// Get weapon data by name and material
        /// </summary>
        public WeaponData? GetWeapon(string weaponName, string material)
        {
            string weaponID = GetWeaponID(weaponName, material);
            return GetWeapon(weaponID);
        }

        /// <summary>
        /// Try to get weapon data (returns true if found)
        /// </summary>
        public bool TryGetWeaponData(string weaponName, string material, out WeaponData weaponData)
        {
            WeaponData? result = GetWeapon(weaponName, material);
            if (result.HasValue)
            {
                weaponData = result.Value;
                return true;
            }
            weaponData = default;
            return false;
        }

        /// <summary>
        /// Try to get weapon data from item (returns true if found)
        /// </summary>
        public bool TryGetWeaponData(ItemDrop.ItemData item, out WeaponData weaponData)
        {
            if (item == null)
            {
                weaponData = default;
                return false;
            }

            string weaponType = ExtractWeaponType(item);
            string material = ExtractMaterial(item);
            return TryGetWeaponData(weaponType, material, out weaponData);
        }

        /// <summary>
        /// Get all weapons of a specific material
        /// </summary>
        public List<WeaponData> GetWeaponsByMaterial(string material)
        {
            string key = material.ToLower();
            return _weaponsByMaterial.ContainsKey(key) ? _weaponsByMaterial[key] : new List<WeaponData>();
        }

        /// <summary>
        /// Get weapon ID from name and material
        /// </summary>
        public string GetWeaponID(string weaponName, string material)
        {
            return $"{material.ToLower()}_{weaponName.ToLower()}";
        }

        /// <summary>
        /// Get material multiplier for damage calculation
        /// </summary>
        public float GetMaterialMultiplier(string material)
        {
            switch (material.ToLower())
            {
                case "iron": return 1.0f;
                case "shadow": return 1.2f;
                case "gold": return 1.35f;
                case "agapite": return 1.5f;
                case "verite": return 1.8f;
                case "snow": return 2.0f;
                case "ice": return 2.2f;
                case "bloodrock": return 2.5f;
                case "valorite": return 3.0f;
                case "blackrock": return 4.0f;
                default:
                    CoreAPI.Log.LogWarning($"[WeaponDatabase] Unknown material: {material}, using 1.0x multiplier");
                    return 1.0f;
            }
        }

        /// <summary>
        /// Get quality damage bonus (FLAT, not multiplier)
        /// Ruin = +3, Might = +6, Force = +9, Power = +12, Vanquishing = +15
        /// </summary>
        public int GetQualityBonus(WeaponQuality quality)
        {
            switch (quality)
            {
                case WeaponQuality.Ruin: return 3;
                case WeaponQuality.Might: return 6;
                case WeaponQuality.Force: return 9;
                case WeaponQuality.Power: return 12;
                case WeaponQuality.Vanquishing: return 15;
                default: return 0;
            }
        }

        /// <summary>
        /// Get weapon data from ItemDrop.ItemData (for equipped items)
        /// </summary>
        public WeaponData? GetWeaponFromItem(ItemDrop.ItemData item)
        {
            if (item == null)
                return null;

            // Extract weapon type and material from item
            string weaponType = ExtractWeaponType(item);
            string material = ExtractMaterial(item);

            return GetWeapon(weaponType, material);
        }

        /// <summary>
        /// Extract weapon type from item (e.g., "Longsword" from "IronLongsword")
        /// </summary>
        private string ExtractWeaponType(ItemDrop.ItemData item)
        {
            // For now, try to extract from item name
            // This will need to be refined based on your naming convention
            string itemName = item.m_shared?.m_name ?? "";
            
            // Remove material prefix
            string[] materials = { "Blackrock", "Valorite", "Bloodrock", "Ice", "Snow", "Verite", "Agapite", "Gold", "Shadow", "Iron" };
            foreach (var material in materials)
            {
                // Use IndexOf for .NET 4.8 compatibility
                if (itemName.IndexOf(material, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    // Remove the material from the name
                    int index = itemName.IndexOf(material, StringComparison.OrdinalIgnoreCase);
                    return (itemName.Substring(0, index) + itemName.Substring(index + material.Length)).Trim();
                }
            }

            return itemName;
        }

        /// <summary>
        /// Extract material from item (e.g., "iron" from "IronLongsword")
        /// </summary>
        private string ExtractMaterial(ItemDrop.ItemData item)
        {
            // For now, try to extract from item name
            // This will need to be refined based on your naming convention
            string itemName = item.m_shared?.m_name ?? "";
            
            string[] materials = { "Blackrock", "Valorite", "Bloodrock", "Ice", "Snow", "Verite", "Agapite", "Gold", "Shadow", "Iron" };
            foreach (var material in materials)
            {
                // FIXED: Use IndexOf with StringComparison for .NET 4.8 compatibility
                if (itemName.IndexOf(material, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return material.ToLower();
                }
            }

            // Default to iron if no material found
            return "iron";
        }
    }
}
