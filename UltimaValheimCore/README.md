# Ultima Valheim Core

The **Core** module is the foundation of the Ultima Valheim modular architecture. It provides centralized systems for event management, persistence, networking, and configuration that all Sidecar modules depend on.

## Architecture Overview

The Core implements a **Core + Sidecar** pattern where:
- **Core** = Central authority and data plane
- **Sidecars** = Modular logic and behavior injectors

This allows for:
- Hot-swappable modules
- Communication without direct dependencies
- Multiplayer-safe persistence
- Unified network sync

## Core Systems

### 1. EventBus
Global publish/subscribe event system for inter-module communication.

```csharp
// Subscribe to an event
CoreAPI.Events.Subscribe<Player, string, int>("OnSkillLevelUp", (player, skill, level) => {
    CoreAPI.Log.LogInfo($"{player.GetPlayerName()} leveled {skill} to {level}");
});

// Publish an event
CoreAPI.Events.Publish("OnSkillLevelUp", player, "Mining", 15);
```

### 2. NetworkManager
Handles RPC registration and multiplayer synchronization.

```csharp
// Register an RPC handler
CoreAPI.Network.RegisterRPC("MyModule", "SyncData", (rpc, pkg) => {
    string data = pkg.ReadString();
    // Process data
});

// Send RPC to server
ZPackage package = new ZPackage();
package.Write("some data");
CoreAPI.Network.SendToServer("MyModule", "SyncData", package);
```

### 3. PersistenceManager
Manages saving/loading player and world data.

```csharp
// Save player data
CoreAPI.Persistence.SavePlayerData("MyModule", playerID, myData);

// Load player data
var data = CoreAPI.Persistence.LoadPlayerData<MyDataType>("MyModule", playerID);

// Persist to disk
CoreAPI.Persistence.SaveToDisk();
```

### 4. ConfigManager
Centralized BepInEx configuration management.

```csharp
// Bind a config value
var maxLevel = CoreAPI.Config.Bind("MyModule", "MaxLevel", 100, "Maximum skill level");

// Use the config value
int level = maxLevel.Value;
```

### 5. CoreEventRouter
Routes game lifecycle events to all modules.

Automatically triggers:
- `OnPlayerJoin(Player player)` - When a player spawns
- `OnPlayerLeave(Player player)` - When a player disconnects
- `OnSave()` - When the game saves
- `OnShutdown()` - When the mod unloads

## Creating a Sidecar Module

To create a new module, implement the `ICoreModule` interface:

```csharp
using System;
using UltimaValheim.Core;

namespace UltimaValheim.MyModule
{
    public class MyModule : ICoreModule
    {
        public string ModuleID => "UltimaValheim.MyModule";
        public Version ModuleVersion => new Version(1, 0, 0);

        public void OnCoreReady()
        {
            CoreAPI.Log.LogInfo($"[{ModuleID}] Initializing...");
            
            // Register events
            CoreAPI.Events.Subscribe<Player>("OnPlayerJoin", OnPlayerJoined);
            
            // Register network handlers
            CoreAPI.Network.RegisterRPC(ModuleID, "SyncData", HandleSyncData);
            
            // Load config
            var enabled = CoreAPI.Config.Bind(ModuleID, "Enabled", true, "Enable this module");
        }

        public void OnPlayerJoin(Player player)
        {
            CoreAPI.Log.LogInfo($"[{ModuleID}] Player joined: {player.GetPlayerName()}");
        }

        public void OnPlayerLeave(Player player)
        {
            // Cleanup player-specific data
        }

        public void OnSave()
        {
            // Save module data
            CoreAPI.Persistence.SaveToDisk();
        }

        public void OnShutdown()
        {
            // Cleanup resources
        }

        private void HandleSyncData(ZRpc rpc, ZPackage package)
        {
            // Handle incoming RPC
        }

        private void OnPlayerJoined(Player player)
        {
            // Custom logic
        }
    }
}
```

## Module Discovery

The Core automatically discovers and registers all `ICoreModule` implementations at startup using reflection. Simply implement the interface and compile your module - no manual registration needed!

## Dependencies

- BepInEx 5.x
- Jotunn Mod Library
- Valheim (Assembly-CSharp)

## Installation

1. Install BepInEx
2. Install Jotunn
3. Place `UltimaValheimCore.dll` in `BepInEx/plugins/`
4. Place any Sidecar modules in `BepInEx/plugins/`

## Development

### Building
```bash
dotnet build
```

### Project Structure
```
UltimaValheimCore/
├── Core/
│   ├── CoreAPI.cs           # Main API interface
│   ├── ICoreModule.cs       # Module interface
│   ├── CoreLifecycle.cs     # Module lifecycle management
│   ├── EventBus.cs          # Event system
│   ├── CoreEventRouter.cs   # Game event routing
│   ├── NetworkManager.cs    # Network/RPC handling
│   ├── PersistenceManager.cs # Save/load system
│   └── ConfigManager.cs     # Config management
└── UltimaValheimCore.cs     # Main plugin entry point
```

## License

MIT License - See LICENSE file for details

## Contributing

Contributions welcome! Please follow the existing code style and architecture patterns.
