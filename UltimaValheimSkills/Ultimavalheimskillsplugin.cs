using BepInEx;
using UltimaValheim.Core;

namespace UltimaValheim.Skills
{
    /// <summary>
    /// BepInEx plugin wrapper for the Ultima Valheim Skills module.
    /// This registers the Skills module with BepInEx and the Core system.
    /// </summary>
    [BepInPlugin(PLUGIN_GUID, PLUGIN_NAME, PLUGIN_VERSION)]
    [BepInDependency("com.valheim.ultima.core", BepInDependency.DependencyFlags.HardDependency)]
    public class UltimaValheimSkillsPlugin : BaseUnityPlugin
    {
        public const string PLUGIN_GUID = "com.valheim.ultima.skills";
        public const string PLUGIN_NAME = "Ultima Valheim Skills";
        public const string PLUGIN_VERSION = "1.0.0";

        private SkillsModule _skillsModule;

        private void Awake()
        {
            Logger.LogInfo($"[{PLUGIN_NAME}] Loading...");

            // Create and register the Skills module
            _skillsModule = new SkillsModule();
            CoreAPI.RegisterModule(_skillsModule);

            Logger.LogInfo($"[{PLUGIN_NAME}] Registered Skills module with Core");
        }
    }
}