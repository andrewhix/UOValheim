# Ultima Valheim - Core + Sidecar Architecture

A modular, extensible architecture for Valheim modding that enables hot-swappable gameplay systems with multiplayer-safe persistence.

## 🎯 Overview

Ultima Valheim uses a **Core + Sidecar** architecture where:
- **Core** provides centralized event management, networking, persistence, and configuration
- **Sidecars** are independent modules (Skills, Magic, Combat, etc.) that communicate through Core APIs
- Modules can be loaded/unloaded independently without affecting each other
- No direct dependencies between modules - all communication via EventBus

## 🏗️ Architecture Diagram

```
┌──────────────────────────────────┐
│     UltimaValheimCore            │
│ ┌──────────────────────────────┐ │
│ │ ConfigManager                │ │
│ │ EventBus                     │ │
│ │ NetworkManager               │ │
│ │ PersistenceManager           │ │
│ │ CoreEventRouter              │ │
│ └──────────────────────────────┘ │
│          ▲      ▲      ▲         │
│          │      │      │         │
└──────────┼──────┼──────┼─────────┘
           │      │      │
           ▼      ▼      ▼
    ┌─────────┐ ┌─────────┐ ┌─────────┐
    │ Skills  │ │ Combat  │ │  Magic  │
    │ Sidecar │ │ Sidecar │ │ Sidecar │
    └─────────┘ └─────────┘ └─────────┘
```

## 📦 What's Included

### Core Module
- **EventBus**: Global publish/subscribe event system
- **NetworkManager**: RPC registration and multiplayer sync
- **PersistenceManager**: Save/load player and world data
- **ConfigManager**: BepInEx configuration integration
- **CoreEventRouter**: Game lifecycle event routing with Harmony patches
- **ICoreModule Interface**: Contract for all Sidecar modules

### Example Module
- **ExampleSidecarModule**: Fully functional reference implementation
- Demonstrates all Core features:
  - Event subscription
  - Network synchronization
  - Data persistence
  - Configuration management
  - Lifecycle handling

### Documentation
- **README.md**: Core module documentation
- **GETTING_STARTED.md**: Step-by-step guide for creating Sidecars
- Inline code documentation

## 🚀 Quick Start

### For Users

1. Install BepInEx for Valheim
2. Install Jotunn Mod Library
3. Download and extract this package
4. Copy `UltimaValheimCore.dll` to `BepInEx/plugins/`
5. Copy any Sidecar modules to `BepInEx/plugins/`
6. Launch Valheim!

### For Developers

```csharp
using System;
using UltimaValheim.Core;

public class MyModule : ICoreModule
{
    public string ModuleID => "UltimaValheim.MyModule";
    public Version ModuleVersion => new Version(1, 0, 0);

    public void OnCoreReady()
    {
        // Subscribe to events
        CoreAPI.Events.Subscribe<Player>("OnPlayerJoin", player => {
            CoreAPI.Log.LogInfo($"{player.GetPlayerName()} joined!");
        });
    }

    public void OnPlayerJoin(Player player) { }
    public void OnPlayerLeave(Player player) { }
    public void OnSave() { }
    public void OnShutdown() { }
}
```

See `GETTING_STARTED.md` for a complete tutorial.

## 🎮 Planned Modules

The following Sidecar modules are planned:

- **Skills**: Mining, Lumberjacking, Magery, Combat skills
- **Magic**: Full Ultima Online spell system (8 circles)
- **Combat**: Enhanced weapon system with quality tiers
- **Economy**: Vendor system and currency
- **Housing**: Expanded building system

## ✨ Key Features

### 🔌 Hot-Swappable Modules
Load or unload modules without affecting others. Perfect for testing and iteration.

### 🌐 Multiplayer-Safe
Network synchronization built-in. All RPC calls are namespaced and version-checked.

### 💾 Persistent Data
Player and world data automatically saved and loaded. JSON-based storage.

### ⚙️ Centralized Config
BepInEx configuration with per-module sections. Easy to customize.

### 📡 Event-Driven
Loose coupling via EventBus. Modules communicate without direct dependencies.

### 🛡️ Error Resilient
Module errors are isolated. One module crashing won't take down others.

## 📚 Documentation

- **[Core README](UltimaValheimCore/README.md)**: Core API reference
- **[Getting Started Guide](GETTING_STARTED.md)**: Tutorial for creating modules
- **[Example Module](ExampleSidecar/ExampleSidecarModule.cs)**: Reference implementation
- **[Design Document](UltimaValheim_Core_and_Sidecar_system.md)**: Full architecture specification

## 🔧 Building from Source

### Prerequisites
- .NET Framework 4.8 SDK
- Visual Studio 2019+ or Rider
- Valheim installed
- BepInEx and Jotunn installed

### Build Steps

1. Clone the repository
2. Set `VALHEIM_INSTALL` environment variable to your Valheim directory
3. Open solution in Visual Studio or Rider
4. Build solution (Ctrl+Shift+B)
5. DLLs will be in `Builds/` folder

```bash
# Using dotnet CLI
dotnet build UltimaValheimCore.sln -c Release
```

## 🧪 Testing

1. Copy `UltimaValheimCore.dll` to `BepInEx/plugins/`
2. Copy `ExampleSidecarModule.dll` to test Core functionality
3. Launch Valheim
4. Check `BepInEx/LogOutput.log` for initialization messages
5. Join a world to trigger lifecycle events

## 🤝 Contributing

Contributions welcome! Please:
1. Follow existing code style
2. Document public APIs
3. Test multiplayer functionality
4. Update documentation for new features

## 📄 License

MIT License - See LICENSE file

## 🙏 Credits

- **Valheim**: Iron Gate Studio
- **BepInEx**: BepInEx team
- **Jotunn**: Jotunn Mod Library team
- **Ultima Online**: Origin Systems / EA (inspiration)

## 📞 Support

- **Issues**: GitHub Issues
- **Discord**: [Join our server]
- **Wiki**: [Project Wiki]

## 🗺️ Roadmap

### v1.0 (Current)
- ✅ Core architecture
- ✅ Event system
- ✅ Network manager
- ✅ Persistence manager
- ✅ Config manager
- ✅ Example module
- ✅ Documentation

### v1.1 (Next)
- ⏳ Skills module
- ⏳ Combat module  
- ⏳ Mining enhancements
- ⏳ Basic economy

### v2.0 (Future)
- ⏳ Magic system (8 circles)
- ⏳ Advanced housing
- ⏳ NPC vendors
- ⏳ Quest system

## 💡 Design Philosophy

1. **Modularity**: Each system is independent and swappable
2. **Loose Coupling**: Communication via events, never direct calls
3. **Multiplayer First**: Network sync built-in from the start
4. **Developer Friendly**: Clear APIs and extensive documentation
5. **User Configurable**: Everything is configurable via BepInEx config

## 🎓 Learn More

- Read the [Getting Started Guide](GETTING_STARTED.md)
- Study the [Example Module](ExampleSidecar/ExampleSidecarModule.cs)
- Review the [Architecture Document](UltimaValheim_Core_and_Sidecar_system.md)
- Check out [Jotunn Documentation](https://valheim-modding.github.io/Jotunn/)

---

**Built with ❤️ for the Valheim modding community**
