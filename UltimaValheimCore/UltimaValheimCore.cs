using BepInEx;
using BepInEx.Configuration;
using Jotunn.Entities;
using Jotunn.Managers;
using Jotunn.Utils;
using System;
using UnityEngine;

namespace UltimaValheim.Core
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [BepInDependency(Jotunn.Main.ModGuid)]
    [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Minor)]
    internal class UltimaValheimCore : BaseUnityPlugin
    {
        public const string PluginGUID = "com.valheim.ultima.core";
        public const string PluginName = "Ultima Valheim Core";
        public const string PluginVersion = "1.0.0";

        private void Awake()
        {
            // Initialize Core API first
            CoreAPI.Initialize(Logger);
            
            Logger.LogInfo($"{PluginName} v{PluginVersion} loaded!");

            // Hook into Jotunn events for lifecycle management
            Jotunn.Logger.LogInfo($"[{PluginName}] Hooking into Jotunn lifecycle events...");
            
            // Subscribe to Jotunn's manager ready events
            PrefabManager.OnVanillaPrefabsAvailable += OnVanillaPrefabsAvailable;
            
            // Auto-discover and register modules
            DiscoverAndRegisterModules();
        }

        private void OnVanillaPrefabsAvailable()
        {
            Logger.LogInfo($"[{PluginName}] Vanilla prefabs are now available.");
            PrefabManager.OnVanillaPrefabsAvailable -= OnVanillaPrefabsAvailable;
        }

        private void DiscoverAndRegisterModules()
        {
            Logger.LogInfo($"[{PluginName}] Scanning for Sidecar modules...");
            
            // Scan all loaded assemblies for ICoreModule implementations
            var moduleCount = 0;
            
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    foreach (var type in assembly.GetTypes())
                    {
                        if (typeof(ICoreModule).IsAssignableFrom(type) && 
                            !type.IsInterface && 
                            !type.IsAbstract)
                        {
                            try
                            {
                                var instance = (ICoreModule)Activator.CreateInstance(type);
                                CoreAPI.RegisterModule(instance);
                                moduleCount++;
                                Logger.LogInfo($"[{PluginName}] Registered module: {type.Name}");
                            }
                            catch (Exception ex)
                            {
                                Logger.LogError($"[{PluginName}] Failed to instantiate module {type.Name}: {ex}");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Skip assemblies that can't be reflected
                    Logger.LogDebug($"[{PluginName}] Skipped assembly {assembly.FullName}: {ex.Message}");
                }
            }
            
            Logger.LogInfo($"[{PluginName}] Discovered and registered {moduleCount} module(s).");
        }

        private void OnDestroy()
        {
            Logger.LogInfo($"[{PluginName}] Shutting down...");
            
            // Notify all modules of shutdown
            CoreAPI.Router.TriggerOnShutdown();
        }
    }
}
