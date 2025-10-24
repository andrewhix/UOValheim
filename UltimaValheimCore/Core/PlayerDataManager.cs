using System;
using System.Collections.Generic;

namespace UltimaValheim.Core
{
    /// <summary>
    /// Manages player-specific data storage using ServerCharacters.
    /// Provides a clean API for sidecars to store/retrieve player data.
    /// All data is server-authoritative and persists with the character.
    /// </summary>
    public class PlayerDataManager
    {
        private const string KEY_PREFIX = "UO_";

        public PlayerDataManager()
        {
            CoreAPI.Log.LogInfo("[PlayerDataManager] Initialized.");
        }

        #region Float Data (Skills, Stats)

        /// <summary>
        /// Save a float value for a player (e.g., skill level, stat value).
        /// Uses ServerCharacters' custom data system.
        /// </summary>
        public void SetFloat(Player player, string key, float value)
        {
            if (player == null || string.IsNullOrEmpty(key))
            {
                CoreAPI.Log.LogWarning("[PlayerDataManager] Invalid SetFloat parameters!");
                return;
            }

            try
            {
                string fullKey = KEY_PREFIX + key;
                
                // ServerCharacters stores data in player.m_customData
                if (player.m_customData == null)
                {
                    player.m_customData = new Dictionary<string, string>();
                }

                player.m_customData[fullKey] = value.ToString();
                
                CoreAPI.Log.LogDebug($"[PlayerDataManager] Set {fullKey} = {value} for player {player.GetPlayerID()}");
            }
            catch (Exception ex)
            {
                CoreAPI.Log.LogError($"[PlayerDataManager] Failed to SetFloat: {ex}");
            }
        }

        /// <summary>
        /// Get a float value for a player with a default fallback.
        /// </summary>
        public float GetFloat(Player player, string key, float defaultValue = 0f)
        {
            if (player == null || string.IsNullOrEmpty(key))
            {
                CoreAPI.Log.LogWarning("[PlayerDataManager] Invalid GetFloat parameters!");
                return defaultValue;
            }

            try
            {
                string fullKey = KEY_PREFIX + key;

                if (player.m_customData != null && player.m_customData.TryGetValue(fullKey, out string valueStr))
                {
                    if (float.TryParse(valueStr, out float value))
                    {
                        return value;
                    }
                }

                return defaultValue;
            }
            catch (Exception ex)
            {
                CoreAPI.Log.LogError($"[PlayerDataManager] Failed to GetFloat: {ex}");
                return defaultValue;
            }
        }

        #endregion

        #region Int Data

        /// <summary>
        /// Save an int value for a player.
        /// </summary>
        public void SetInt(Player player, string key, int value)
        {
            if (player == null || string.IsNullOrEmpty(key))
            {
                CoreAPI.Log.LogWarning("[PlayerDataManager] Invalid SetInt parameters!");
                return;
            }

            try
            {
                string fullKey = KEY_PREFIX + key;
                
                if (player.m_customData == null)
                {
                    player.m_customData = new Dictionary<string, string>();
                }

                player.m_customData[fullKey] = value.ToString();
                
                CoreAPI.Log.LogDebug($"[PlayerDataManager] Set {fullKey} = {value} for player {player.GetPlayerID()}");
            }
            catch (Exception ex)
            {
                CoreAPI.Log.LogError($"[PlayerDataManager] Failed to SetInt: {ex}");
            }
        }

        /// <summary>
        /// Get an int value for a player with a default fallback.
        /// </summary>
        public int GetInt(Player player, string key, int defaultValue = 0)
        {
            if (player == null || string.IsNullOrEmpty(key))
            {
                CoreAPI.Log.LogWarning("[PlayerDataManager] Invalid GetInt parameters!");
                return defaultValue;
            }

            try
            {
                string fullKey = KEY_PREFIX + key;

                if (player.m_customData != null && player.m_customData.TryGetValue(fullKey, out string valueStr))
                {
                    if (int.TryParse(valueStr, out int value))
                    {
                        return value;
                    }
                }

                return defaultValue;
            }
            catch (Exception ex)
            {
                CoreAPI.Log.LogError($"[PlayerDataManager] Failed to GetInt: {ex}");
                return defaultValue;
            }
        }

        #endregion

        #region String Data

        /// <summary>
        /// Save a string value for a player.
        /// </summary>
        public void SetString(Player player, string key, string value)
        {
            if (player == null || string.IsNullOrEmpty(key))
            {
                CoreAPI.Log.LogWarning("[PlayerDataManager] Invalid SetString parameters!");
                return;
            }

            try
            {
                string fullKey = KEY_PREFIX + key;
                
                if (player.m_customData == null)
                {
                    player.m_customData = new Dictionary<string, string>();
                }

                player.m_customData[fullKey] = value ?? string.Empty;
                
                CoreAPI.Log.LogDebug($"[PlayerDataManager] Set {fullKey} for player {player.GetPlayerID()}");
            }
            catch (Exception ex)
            {
                CoreAPI.Log.LogError($"[PlayerDataManager] Failed to SetString: {ex}");
            }
        }

        /// <summary>
        /// Get a string value for a player with a default fallback.
        /// </summary>
        public string GetString(Player player, string key, string defaultValue = "")
        {
            if (player == null || string.IsNullOrEmpty(key))
            {
                CoreAPI.Log.LogWarning("[PlayerDataManager] Invalid GetString parameters!");
                return defaultValue;
            }

            try
            {
                string fullKey = KEY_PREFIX + key;

                if (player.m_customData != null && player.m_customData.TryGetValue(fullKey, out string value))
                {
                    return value;
                }

                return defaultValue;
            }
            catch (Exception ex)
            {
                CoreAPI.Log.LogError($"[PlayerDataManager] Failed to GetString: {ex}");
                return defaultValue;
            }
        }

        #endregion

        #region Utility

        /// <summary>
        /// Check if player has a specific data key.
        /// </summary>
        public bool HasKey(Player player, string key)
        {
            if (player == null || string.IsNullOrEmpty(key))
                return false;

            string fullKey = KEY_PREFIX + key;
            return player.m_customData != null && player.m_customData.ContainsKey(fullKey);
        }

        /// <summary>
        /// Remove a data key from player.
        /// </summary>
        public void RemoveKey(Player player, string key)
        {
            if (player == null || string.IsNullOrEmpty(key))
                return;

            string fullKey = KEY_PREFIX + key;
            player.m_customData?.Remove(fullKey);
            CoreAPI.Log.LogDebug($"[PlayerDataManager] Removed {fullKey} for player {player.GetPlayerID()}");
        }

        #endregion
    }
}
