using System;

namespace UltimaValheim.Core
{
    /// <summary>
    /// Attribute to declare module dependencies.
    /// Used by CoreLifecycle to ensure proper load order.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class ModuleDependencyAttribute : Attribute
    {
        /// <summary>
        /// Module ID this module depends on (e.g., "UltimaValheim.Core")
        /// </summary>
        public string ModuleID { get; }

        /// <summary>
        /// Minimum required version (format: "1.0.0")
        /// </summary>
        public string MinVersion { get; set; }

        /// <summary>
        /// Whether this is a soft dependency (optional).
        /// Soft dependencies don't affect load order, but module can check if they're present.
        /// </summary>
        public bool Soft { get; set; }

        /// <summary>
        /// Create a module dependency declaration.
        /// </summary>
        /// <param name="moduleID">The ModuleID this module depends on</param>
        public ModuleDependencyAttribute(string moduleID)
        {
            ModuleID = moduleID;
            MinVersion = "0.0.0";
            Soft = false;
        }
    }
}