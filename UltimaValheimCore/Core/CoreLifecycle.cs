using System;
using System.Collections.Generic;

namespace UltimaValheim.Core
{
    /// <summary>
    /// Manages Core initialization and module registration lifecycle.
    /// Handles the sequencing of module initialization and lifecycle events.
    /// </summary>
    public static class CoreLifecycle
    {
        private static readonly List<ICoreModule> _modules = new List<ICoreModule>();
        private static bool _coreReady;

        /// <summary>
        /// Fired when the Core systems are initialized and ready for modules.
        /// </summary>
        public static event Action OnCoreReady;

        /// <summary>
        /// All registered modules
        /// </summary>
        public static IReadOnlyList<ICoreModule> Modules => _modules.AsReadOnly();

        /// <summary>
        /// Called internally by CoreAPI once systems are initialized.
        /// Notifies all registered modules that Core is ready.
        /// </summary>
        internal static void NotifyCoreReady()
        {
            if (_coreReady)
            {
                CoreAPI.Log.LogWarning("[CoreLifecycle] NotifyCoreReady called multiple times!");
                return;
            }
            
            _coreReady = true;

            CoreAPI.Log.LogInfo("[CoreLifecycle] Core is ready. Initializing modules...");

            // Fire global event for subscribers
            OnCoreReady?.Invoke();

            // Initialize each registered module
            foreach (var module in _modules)
            {
                try
                {
                    CoreAPI.Log.LogInfo($"[CoreLifecycle] Initializing module: {module.ModuleID}");
                    module.OnCoreReady();
                }
                catch (Exception ex)
                {
                    CoreAPI.Log.LogError($"[CoreLifecycle] Error initializing module {module.ModuleID}: {ex}");
                }
            }

            CoreAPI.Log.LogInfo($"[CoreLifecycle] Initialized {_modules.Count} module(s).");
        }

        /// <summary>
        /// Registers a module that should be initialized when Core is ready.
        /// If Core is already ready, initializes the module immediately.
        /// </summary>
        public static void RegisterModule(ICoreModule module)
        {
            if (module == null)
            {
                CoreAPI.Log.LogWarning("[CoreLifecycle] Attempted to register null module!");
                return;
            }

            // Check for duplicate registration
            if (_modules.Exists(m => m.ModuleID == module.ModuleID))
            {
                CoreAPI.Log.LogWarning($"[CoreLifecycle] Module {module.ModuleID} is already registered!");
                return;
            }

            _modules.Add(module);
            CoreAPI.Log.LogInfo($"[CoreLifecycle] Registered module: {module.ModuleID} v{module.ModuleVersion}");

            // If Core is already ready, initialize immediately (late registration)
            if (_coreReady)
            {
                try
                {
                    CoreAPI.Log.LogInfo($"[CoreLifecycle] Late-registering module: {module.ModuleID}");
                    module.OnCoreReady();
                }
                catch (Exception ex)
                {
                    CoreAPI.Log.LogError($"[CoreLifecycle] Error initializing late-registered module {module.ModuleID}: {ex}");
                }
            }
        }

        /// <summary>
        /// Get a registered module by its ModuleID
        /// </summary>
        public static ICoreModule GetModule(string moduleID)
        {
            return _modules.Find(m => m.ModuleID == moduleID);
        }

        /// <summary>
        /// Check if a module with the given ID is registered
        /// </summary>
        public static bool IsModuleRegistered(string moduleID)
        {
            return _modules.Exists(m => m.ModuleID == moduleID);
        }
    }
}
