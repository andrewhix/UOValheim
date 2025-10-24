using BepInEx;
using UltimaValheim.Core;

namespace UltimaValheim.Example
{
    /// <summary>
    /// BepInEx plugin entry point for ExampleSidecar.
    /// This registers the ExampleSidecarModule with the Core system.
    /// </summary>
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [BepInDependency("com.valheim.ultima.core", BepInDependency.DependencyFlags.HardDependency)]
    public class ExampleSidecar : BaseUnityPlugin
    {
        public const string PluginGUID = "com.valheim.ultima.example";
        public const string PluginName = "Ultima Valheim Example Sidecar";
        public const string PluginVersion = "1.0.0";

        private ExampleSidecarModule _module;

        private void Awake()
        {
            Logger.LogInfo($"[{PluginName}] Loading...");

            // Create and register the module with Core
            _module = new ExampleSidecarModule();
            CoreAPI.RegisterModule(_module);

            Logger.LogInfo($"[{PluginName}] Registered module with Core: {_module.ModuleID}");
        }

        private void OnDestroy()
        {
            Logger.LogInfo($"[{PluginName}] Shutting down...");
        }
    }
}
