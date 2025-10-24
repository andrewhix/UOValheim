using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace UltimaValheim.Core
{
    /// <summary>
    /// Manages persistence for module data across saves.
    /// Provides serialization hooks for player-specific and world-specific data.
    /// </summary>
    public class PersistenceManager
    {
        private readonly Dictionary<string, Dictionary<long, object>> _playerData = new Dictionary<string, Dictionary<long, object>>();
        private readonly Dictionary<string, object> _worldData = new Dictionary<string, object>();

        private readonly string _saveFolder;

        public PersistenceManager()
        {
            // Use BepInEx config path for saves
            _saveFolder = Path.Combine(BepInEx.Paths.ConfigPath, "UltimaValheim", "Saves");

            if (!Directory.Exists(_saveFolder))
            {
                Directory.CreateDirectory(_saveFolder);
            }

            CoreAPI.Log.LogInfo($"[PersistenceManager] Save folder: {_saveFolder}");
        }

        #region Player Data

        /// <summary>
        /// Save player-specific data for a module.
        /// </summary>
        /// <param name="moduleID">Module identifier</param>
        /// <param name="playerID">Player ID</param>
        /// <param name="data">Data to save (must be JSON serializable)</param>
        public void SavePlayerData(string moduleID, long playerID, object data)
        {
            if (string.IsNullOrEmpty(moduleID) || data == null)
            {
                CoreAPI.Log.LogWarning("[PersistenceManager] Invalid SavePlayerData parameters!");
                return;
            }

            if (!_playerData.ContainsKey(moduleID))
            {
                _playerData[moduleID] = new Dictionary<long, object>();
            }

            _playerData[moduleID][playerID] = data;
            CoreAPI.Log.LogDebug($"[PersistenceManager] Saved player data for {moduleID}, player {playerID}");
        }

        /// <summary>
        /// Load player-specific data for a module.
        /// </summary>
        /// <typeparam name="T">Type of data to load</typeparam>
        /// <param name="moduleID">Module identifier</param>
        /// <param name="playerID">Player ID</param>
        /// <returns>Loaded data or default(T) if not found</returns>
        public T LoadPlayerData<T>(string moduleID, long playerID) where T : class
        {
            if (string.IsNullOrEmpty(moduleID))
                return default(T);

            if (_playerData.ContainsKey(moduleID) && _playerData[moduleID].ContainsKey(playerID))
            {
                try
                {
                    return _playerData[moduleID][playerID] as T;
                }
                catch (Exception ex)
                {
                    CoreAPI.Log.LogError($"[PersistenceManager] Error loading player data for {moduleID}: {ex}");
                    return default(T);
                }
            }

            return default(T);
        }

        /// <summary>
        /// Check if player data exists for a module.
        /// </summary>
        public bool HasPlayerData(string moduleID, long playerID)
        {
            return _playerData.ContainsKey(moduleID) && _playerData[moduleID].ContainsKey(playerID);
        }

        /// <summary>
        /// Clear player data for a specific player (e.g., on character deletion).
        /// </summary>
        public void ClearPlayerData(long playerID)
        {
            foreach (var moduleData in _playerData.Values)
            {
                moduleData.Remove(playerID);
            }

            CoreAPI.Log.LogInfo($"[PersistenceManager] Cleared all data for player {playerID}");
        }

        #endregion

        #region World Data

        /// <summary>
        /// Save world-specific data for a module (not player-specific).
        /// </summary>
        public void SaveWorldData(string moduleID, object data)
        {
            if (string.IsNullOrEmpty(moduleID) || data == null)
            {
                CoreAPI.Log.LogWarning("[PersistenceManager] Invalid SaveWorldData parameters!");
                return;
            }

            _worldData[moduleID] = data;
            CoreAPI.Log.LogDebug($"[PersistenceManager] Saved world data for {moduleID}");
        }

        /// <summary>
        /// Load world-specific data for a module.
        /// </summary>
        public T LoadWorldData<T>(string moduleID) where T : class
        {
            if (string.IsNullOrEmpty(moduleID))
                return default(T);

            if (_worldData.ContainsKey(moduleID))
            {
                try
                {
                    return _worldData[moduleID] as T;
                }
                catch (Exception ex)
                {
                    CoreAPI.Log.LogError($"[PersistenceManager] Error loading world data for {moduleID}: {ex}");
                    return default(T);
                }
            }

            return default(T);
        }

        /// <summary>
        /// Check if world data exists for a module.
        /// </summary>
        public bool HasWorldData(string moduleID)
        {
            return _worldData.ContainsKey(moduleID);
        }

        #endregion

        #region File Persistence

        /// <summary>
        /// Persist all data to disk.
        /// </summary>
        public void SaveToDisk()
        {
            try
            {
                string worldName = GetCurrentWorldName();
                if (string.IsNullOrEmpty(worldName))
                {
                    CoreAPI.Log.LogWarning("[PersistenceManager] Cannot save - no active world!");
                    return;
                }

                // Save player data
                string playerDataPath = Path.Combine(_saveFolder, $"{worldName}_players.json");
                string playerJson = JsonConvert.SerializeObject(_playerData, Newtonsoft.Json.Formatting.Indented);
                File.WriteAllText(playerDataPath, playerJson);

                // Save world data
                string worldDataPath = Path.Combine(_saveFolder, $"{worldName}_world.json");
                string worldJson = JsonConvert.SerializeObject(_worldData, Newtonsoft.Json.Formatting.Indented);
                File.WriteAllText(worldDataPath, worldJson);

                CoreAPI.Log.LogInfo($"[PersistenceManager] Saved data to disk for world '{worldName}'");
            }
            catch (Exception ex)
            {
                CoreAPI.Log.LogError($"[PersistenceManager] Failed to save data to disk: {ex}");
            }
        }

        /// <summary>
        /// Load all data from disk.
        /// </summary>
        public void LoadFromDisk()
        {
            try
            {
                string worldName = GetCurrentWorldName();
                if (string.IsNullOrEmpty(worldName))
                {
                    CoreAPI.Log.LogWarning("[PersistenceManager] Cannot load - no active world!");
                    return;
                }

                // Load player data
                string playerDataPath = Path.Combine(_saveFolder, $"{worldName}_players.json");
                if (File.Exists(playerDataPath))
                {
                    string playerJson = File.ReadAllText(playerDataPath);
                    var loaded = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<long, object>>>(playerJson);
                    if (loaded != null)
                    {
                        _playerData.Clear();
                        foreach (var kvp in loaded)
                        {
                            _playerData[kvp.Key] = kvp.Value;
                        }
                    }
                }

                // Load world data
                string worldDataPath = Path.Combine(_saveFolder, $"{worldName}_world.json");
                if (File.Exists(worldDataPath))
                {
                    string worldJson = File.ReadAllText(worldDataPath);
                    var loaded = JsonConvert.DeserializeObject<Dictionary<string, object>>(worldJson);
                    if (loaded != null)
                    {
                        _worldData.Clear();
                        foreach (var kvp in loaded)
                        {
                            _worldData[kvp.Key] = kvp.Value;
                        }
                    }
                }

                CoreAPI.Log.LogInfo($"[PersistenceManager] Loaded data from disk for world '{worldName}'");
            }
            catch (Exception ex)
            {
                CoreAPI.Log.LogError($"[PersistenceManager] Failed to load data from disk: {ex}");
            }
        }

        #endregion

        #region Helper Methods

        private string GetCurrentWorldName()
        {
            if (ZNet.instance != null && ZNet.instance.GetWorldName() != null)
            {
                return ZNet.instance.GetWorldName();
            }

            return null;
        }

        #endregion
    }
}
