using System;

namespace UltimaValheim.Core
{
    /// <summary>
    /// Interface that all Sidecar modules must implement to integrate with the Core system.
    /// Provides lifecycle hooks for initialization, player events, persistence, and shutdown.
    /// </summary>
    public interface ICoreModule
    {
        /// <summary>
        /// Unique identifier for this module (e.g., "UltimaValheim.Skills")
        /// </summary>
        string ModuleID { get; }

        /// <summary>
        /// Version of this module
        /// </summary>
        System.Version ModuleVersion { get; }

        /// <summary>
        /// Called when the Core API is ready and all managers are initialized.
        /// Modules should register their events, network handlers, and config here.
        /// </summary>
        void OnCoreReady();

        /// <summary>
        /// Called when a player joins the game (loads their character).
        /// </summary>
        /// <param name="player">The Player instance that joined</param>
        void OnPlayerJoin(Player player);

        /// <summary>
        /// Called when a player leaves the game or disconnects.
        /// </summary>
        /// <param name="player">The Player instance that left</param>
        void OnPlayerLeave(Player player);

        /// <summary>
        /// Called when the game is saving. Modules should persist their data here.
        /// </summary>
        void OnSave();

        /// <summary>
        /// Called when the Core is shutting down (game exit or mod unload).
        /// Modules should clean up resources here.
        /// </summary>
        void OnShutdown();
    }
}
