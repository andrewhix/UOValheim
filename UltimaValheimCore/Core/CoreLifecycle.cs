using System;
using System.Collections.Generic;
using System.Linq;

namespace UltimaValheim.Core
{
    /// <summary>
    /// Manages Core initialization and module registration lifecycle.
    /// Handles the sequencing of module initialization and lifecycle events.
    /// ENHANCED: Includes dependency-based load ordering.
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
        /// ENHANCED: Modules are initialized in dependency order.
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

            // ENHANCEMENT: Sort modules by dependencies before initialization
            var sortedModules = SortModulesByDependencies(_modules);

            // Initialize each registered module in dependency order
            foreach (var module in sortedModules)
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

            CoreAPI.Log.LogInfo($"[CoreLifecycle] Initialized {sortedModules.Count} module(s) in dependency order.");
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

            // Validate dependencies
            ValidateModuleDependencies(module);

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

        #region Dependency Management

        /// <summary>
        /// Sort modules by dependencies using topological sort.
        /// Ensures dependencies load before dependents.
        /// </summary>
        private static List<ICoreModule> SortModulesByDependencies(List<ICoreModule> modules)
        {
            var sorted = new List<ICoreModule>();
            var visited = new HashSet<string>();
            var visiting = new HashSet<string>();

            void Visit(ICoreModule module)
            {
                if (visited.Contains(module.ModuleID))
                    return;

                if (visiting.Contains(module.ModuleID))
                {
                    CoreAPI.Log.LogError($"[CoreLifecycle] Circular dependency detected: {module.ModuleID}");
                    return;
                }

                visiting.Add(module.ModuleID);

                // Get hard dependencies from attributes (soft dependencies don't affect load order)
                var deps = module.GetType()
                    .GetCustomAttributes(typeof(ModuleDependencyAttribute), true)
                    .Cast<ModuleDependencyAttribute>()
                    .Where(d => !d.Soft);

                foreach (var dep in deps)
                {
                    var depModule = modules.Find(m => m.ModuleID == dep.ModuleID);
                    if (depModule != null)
                    {
                        Visit(depModule);
                    }
                    else
                    {
                        CoreAPI.Log.LogWarning($"[CoreLifecycle] Missing hard dependency: {module.ModuleID} requires {dep.ModuleID}");
                    }
                }

                visiting.Remove(module.ModuleID);
                visited.Add(module.ModuleID);
                sorted.Add(module);
            }

            // Visit all modules
            foreach (var module in modules)
            {
                Visit(module);
            }

            CoreAPI.Log.LogInfo($"[CoreLifecycle] Module load order: {string.Join(" -> ", sorted.Select(m => m.ModuleID))}");

            return sorted;
        }

        /// <summary>
        /// Validate that a module's dependencies are satisfied.
        /// Logs warnings for missing dependencies.
        /// </summary>
        private static void ValidateModuleDependencies(ICoreModule module)
        {
            var deps = module.GetType()
                .GetCustomAttributes(typeof(ModuleDependencyAttribute), true)
                .Cast<ModuleDependencyAttribute>();

            foreach (var dep in deps)
            {
                var depModule = _modules.Find(m => m.ModuleID == dep.ModuleID);

                if (depModule == null)
                {
                    if (dep.Soft)
                    {
                        CoreAPI.Log.LogInfo($"[CoreLifecycle] {module.ModuleID}: Optional dependency '{dep.ModuleID}' not present (OK)");
                    }
                    else
                    {
                        CoreAPI.Log.LogWarning($"[CoreLifecycle] {module.ModuleID}: Required dependency '{dep.ModuleID}' is missing!");
                    }
                }
                else
                {
                    // Check version if specified
                    if (!string.IsNullOrEmpty(dep.MinVersion) && dep.MinVersion != "0.0.0")
                    {
                        try
                        {
                            var requiredVer = System.Version.Parse(dep.MinVersion);
                            var actualVer = depModule.ModuleVersion;

                            if (actualVer.CompareTo(requiredVer) < 0)
                            {
                                CoreAPI.Log.LogWarning($"[CoreLifecycle] {module.ModuleID}: Dependency '{dep.ModuleID}' version {actualVer} is older than required {requiredVer}");
                            }
                        }
                        catch (Exception ex)
                        {
                            CoreAPI.Log.LogWarning($"[CoreLifecycle] {module.ModuleID}: Could not parse version for dependency '{dep.ModuleID}': {ex.Message}");
                        }
                    }
                }
            }
        }

        #endregion
    }
}