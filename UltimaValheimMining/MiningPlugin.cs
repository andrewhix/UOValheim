using BepInEx;
using UltimaValheim.Core;

namespace UltimaValheim.Mining
{
    /// <summary>
    /// BepInEx plugin entry point for UltimaValheimMining.
    /// Registers the Mining module with the Core system.
    /// </summary>
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [BepInDependency("com.valheim.ultima.core", BepInDependency.DependencyFlags.HardDependency)]
    public class MiningPlugin : BaseUnityPlugin
    {
        public const string PluginGUID = "com.valheim.ultima.mining";
        public const string PluginName = "Ultima Valheim Mining";
        public const string PluginVersion = "1.0.0";

        private MiningModule _module;

        private void Awake()
        {
            Logger.LogInfo($"[{PluginName}] Loading...");

            // Create and register the module with Core
            _module = new MiningModule();
            CoreAPI.RegisterModule(_module);

            Logger.LogInfo($"[{PluginName}] Registered Mining module with Core");
        }

        private void OnDestroy()
        {
            Logger.LogInfo($"[{PluginName}] Shutting down...");
        }
    }
}
