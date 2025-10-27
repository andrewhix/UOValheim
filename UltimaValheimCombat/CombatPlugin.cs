using BepInEx;
using UltimaValheim.Core;

namespace UltimaValheim.Combat
{
    /// <summary>
    /// BepInEx plugin for Ultima Valheim Combat module.
    /// Registers the combat module with the Core system.
    /// </summary>
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [BepInDependency("com.valheim.ultima.core", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("com.jotunn.jotunn", BepInDependency.DependencyFlags.HardDependency)]
    public class CombatPlugin : BaseUnityPlugin
    {
        public const string PluginGUID = "com.valheim.ultima.combat";
        public const string PluginName = "UltimaValheim.Combat";
        public const string PluginVersion = "1.0.0";

        private CombatModule _combatModule;

        void Awake()
        {
            Logger.LogInfo($"{PluginName} {PluginVersion} starting...");

            // Create and register the combat module
            _combatModule = new CombatModule();
            CoreLifecycle.RegisterModule(_combatModule);

            // Initialize Harmony patches with module reference
            Patches.Character_Damage.Initialize(_combatModule);
            Patches.Humanoid_EquipItem.Initialize(_combatModule);
            Patches.Player_Update.Initialize(_combatModule);

            Logger.LogInfo($"{PluginName} initialized successfully!");
        }

        void OnDestroy()
        {
            Logger.LogInfo($"{PluginName} shutting down...");
            
            // FIXED: CoreLifecycle doesn't have UnregisterModule method
            // Just call the module's shutdown directly
            if (_combatModule != null)
            {
                _combatModule.OnShutdown();
            }
        }
    }
}
