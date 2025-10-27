Ultima Valheim - Core + Sidecar Architecture
A modular, extensible framework for Valheim modding that enables independent, hot-swappable gameplay systems with multiplayer-safe persistence and event-driven communication.
🎯 Architecture Overview
Core + Sidecar pattern where:

Core = Central authority providing EventBus, NetworkManager, PersistenceManager, and ConfigManager
Sidecars = Independent modules (Skills, Magic, Combat, etc.) that communicate through Core APIs only
Zero direct dependencies between modules - all communication via events
Hot-swappable modules without affecting other systems

┌─────────────────────────────┐
│    UltimaValheimCore        │
│  ┌───────────────────────┐  │
│  │ ConfigManager         │  │
│  │ EventBus              │  │
│  │ NetworkManager        │  │
│  │ PersistenceManager    │  │
│  └───────────────────────┘  │
│       ▲      ▲      ▲       │
└───────┼──────┼──────┼───────┘
        │      │      │
    ┌───┴──┐ ┌─┴───┐ ┌─┴────┐
    │Skills│ │Combat│ │Magic │
    │Module│ │Module│ │Module│
    └──────┘ └──────┘ └──────┘
✨ Key Features

🔌 Modular Design: Each system is a standalone module - add or remove without breaking others
🌐 Multiplayer-Safe: Built-in network sync with RPC routing and version checking
💾 Persistent Data: Automatic save/load for player and world data via JSON
📡 Event-Driven: Loose coupling via publish/subscribe EventBus
⚙️ Centralized Config: BepInEx configuration with per-module sections
🛡️ Error Resilient: Module failures are isolated and don't cascade

🚀 Quick Start
For Users

Install BepInEx + Jotunn
Copy UltimaValheimCore.dll to BepInEx/plugins/
Add any Sidecar module DLLs
Launch Valheim

For Developers
csharpusing System;
using UltimaValheim.Core;

public class MyModule : ICoreModule
{
    public string ModuleID => "UltimaValheim.MyModule";
    public Version ModuleVersion => new Version(1, 0, 0);

    public void OnCoreReady()
    {
        // Subscribe to events from other modules
        CoreAPI.Events.Subscribe<Player>("OnPlayerJoin", player => {
            CoreAPI.Log.LogInfo($"{player.GetPlayerName()} joined!");
        });
        
        // Publish your own events
        CoreAPI.Events.Publish("OnMyEventHappened", someData);
    }

    public void OnPlayerJoin(Player player) { }
    public void OnPlayerLeave(Player player) { }
    public void OnSave() { }
    public void OnShutdown() { }
}
```

## 🎮 Planned Modules

- **Skills**: Mining, Lumberjacking, Magery, Combat skills with XP progression
- **Magic**: Full 64-spell system across 8 circles (Ultima Online-inspired)
- **Combat**: Enhanced weapon tiers (Ruin/Might/Force/Power/Vanquishing) with quality-based scaling
- **Economy**: Vendor NPCs, currency system, player trading
- **Housing**: Expanded building with furniture and decorations

## 📚 How It Works

1. **Core loads first** and initializes all managers
2. **Core discovers Sidecars** via reflection (`ICoreModule` interface)
3. **Sidecars register** their events, network handlers, and persistence needs
4. **Runtime communication** happens through EventBus - no direct calls
5. **Lifecycle events** (`OnPlayerJoin`, `OnSave`, etc.) keep modules in sync

### Example: Skill XP Gain
```
Player mines ore
   ↓
Mining module publishes event → CoreAPI.Events.Publish("OnMiningHit", player, nodeID)
   ↓
Skills module listens → CoreAPI.Events.Subscribe("OnMiningHit", ...)
   ↓
Skills adds XP → Skills.AddXP(player, "Mining", 5)
   ↓
PersistenceManager saves → OnSave() hook
   ↓
NetworkManager syncs → Client receives updated XP
🔧 Building from Source
bash# Prerequisites: .NET 4.8, BepInEx, Jotunn
git clone https://github.com/yourusername/UltimaValheim.git
cd UltimaValheim
dotnet build UltimaValheimCore.sln -c Release
# Output: Builds/UltimaValheimCore.dll
📖 Documentation

Getting Started Guide - Tutorial for creating modules
Architecture Doc - Full technical specification
Example Module - Reference implementation
Jotunn Docs - Modding API reference

🤝 Contributing
Contributions welcome! Please:

Follow existing code style
Document all public APIs
Test multiplayer functionality
Update docs for new features

📄 License
MIT License
🙏 Credits

Valheim by Iron Gate Studio
BepInEx modding framework
Jotunn Valheim mod library
Ultima Online (design inspiration)


Built for the Valheim modding community ❤️
