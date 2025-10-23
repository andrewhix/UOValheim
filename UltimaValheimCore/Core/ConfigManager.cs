using BepInEx.Configuration;
using System.Collections.Generic;

namespace UltimaValheim.Core
{
    /// <summary>
    /// Manages BepInEx configuration for all modules.
    /// Provides centralized config access with module-specific sections.
    /// </summary>
    public class ConfigManager
    {
        private readonly Dictionary<string, ConfigFile> _moduleConfigs = new Dictionary<string, ConfigFile>();
        
        public ConfigManager()
        {
            CoreAPI.Log.LogInfo("[ConfigManager] Initialized.");
        }

        /// <summary>
        /// Get or create a config entry for a module.
        /// </summary>
        /// <typeparam name="T">Type of the config value</typeparam>
        /// <param name="moduleID">Module identifier (used as config section)</param>
        /// <param name="key">Config key name</param>
        /// <param name="defaultValue">Default value if not set</param>
        /// <param name="description">Description of the config entry</param>
        /// <returns>ConfigEntry that can be read/written</returns>
        public ConfigEntry<T> Bind<T>(string moduleID, string key, T defaultValue, string description = "")
        {
            if (string.IsNullOrEmpty(moduleID) || string.IsNullOrEmpty(key))
            {
                CoreAPI.Log.LogWarning("[ConfigManager] Invalid Bind parameters!");
                return null;
            }

            // Get or create config file for this module
            if (!_moduleConfigs.ContainsKey(moduleID))
            {
                string configPath = System.IO.Path.Combine(BepInEx.Paths.ConfigPath, $"{moduleID}.cfg");
                _moduleConfigs[moduleID] = new ConfigFile(configPath, true);
                CoreAPI.Log.LogInfo($"[ConfigManager] Created config file for module: {moduleID}");
            }

            ConfigFile configFile = _moduleConfigs[moduleID];

            // Bind the config entry
            var entry = configFile.Bind(
                section: moduleID,
                key: key,
                defaultValue: defaultValue,
                description: description
            );

            CoreAPI.Log.LogDebug($"[ConfigManager] Bound config: {moduleID}.{key} = {defaultValue}");

            return entry;
        }

        /// <summary>
        /// Get a config entry if it exists, otherwise returns null.
        /// </summary>
        public ConfigEntry<T> GetEntry<T>(string moduleID, string key)
        {
            if (string.IsNullOrEmpty(moduleID) || string.IsNullOrEmpty(key))
                return null;

            if (!_moduleConfigs.ContainsKey(moduleID))
                return null;

            ConfigFile configFile = _moduleConfigs[moduleID];

            // Try to get existing entry
            if (configFile.TryGetEntry(moduleID, key, out ConfigEntry<T> entry))
            {
                return entry;
            }

            return null;
        }

        /// <summary>
        /// Save all module configs to disk.
        /// </summary>
        public void SaveAll()
        {
            foreach (var kvp in _moduleConfigs)
            {
                try
                {
                    kvp.Value.Save();
                    CoreAPI.Log.LogDebug($"[ConfigManager] Saved config for module: {kvp.Key}");
                }
                catch (System.Exception ex)
                {
                    CoreAPI.Log.LogError($"[ConfigManager] Failed to save config for {kvp.Key}: {ex}");
                }
            }

            CoreAPI.Log.LogInfo($"[ConfigManager] Saved {_moduleConfigs.Count} module config(s).");
        }

        /// <summary>
        /// Reload all module configs from disk.
        /// </summary>
        public void ReloadAll()
        {
            foreach (var kvp in _moduleConfigs)
            {
                try
                {
                    kvp.Value.Reload();
                    CoreAPI.Log.LogDebug($"[ConfigManager] Reloaded config for module: {kvp.Key}");
                }
                catch (System.Exception ex)
                {
                    CoreAPI.Log.LogError($"[ConfigManager] Failed to reload config for {kvp.Key}: {ex}");
                }
            }

            CoreAPI.Log.LogInfo($"[ConfigManager] Reloaded {_moduleConfigs.Count} module config(s).");
        }

        /// <summary>
        /// Get the ConfigFile for a specific module (for advanced usage).
        /// </summary>
        public ConfigFile GetModuleConfig(string moduleID)
        {
            if (_moduleConfigs.ContainsKey(moduleID))
            {
                return _moduleConfigs[moduleID];
            }

            return null;
        }
    }
}
