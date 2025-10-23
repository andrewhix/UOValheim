# Getting Started with Ultima Valheim Core + Sidecar System

This guide will help you understand and use the Core + Sidecar architecture for Ultima Valheim.

## What is the Core + Sidecar Pattern?

The **Core** is the central authority that provides:
- Event management (EventBus)
- Network synchronization (NetworkManager)
- Data persistence (PersistenceManager)
- Configuration management (ConfigManager)
- Lifecycle event routing (CoreEventRouter)

**Sidecars** are independent modules that:
- Implement `ICoreModule` interface
- Communicate through Core APIs only
- Have no direct dependencies on each other
- Can be loaded/unloaded independently

## Quick Start: Creating Your First Sidecar

### Step 1: Create the Module Class

```csharp
using System;
using UltimaValheim.Core;

namespace UltimaValheim.Skills
{
    public class SkillsModule : ICoreModule
    {
        // Required interface properties
        public string ModuleID => "UltimaValheim.Skills";
        public Version ModuleVersion => new Version(1, 0, 0);

        // Your module's data
        private Dictionary<long, PlayerSkills> _playerSkills = new();
        
        // Called when Core is ready
        public void OnCoreReady()
        {
            CoreAPI.Log.LogInfo($"[Skills] Initializing...");
            
            // Setup your module here
            RegisterEvents();
            RegisterNetworkHandlers();
            LoadConfig();
        }

        // Called when player joins
        public void OnPlayerJoin(Player player)
        {
            long playerID = player.GetPlayerID();
            LoadPlayerSkills(playerID);
        }

        // Called when player leaves
        public void OnPlayerLeave(Player player)
        {
            long playerID = player.GetPlayerID();
            SavePlayerSkills(playerID);
        }

        // Called on save
        public void OnSave()
        {
            SaveAllPlayerData();
        }

        // Called on shutdown
        public void OnShutdown()
        {
            // Cleanup resources
            _playerSkills.Clear();
        }
    }
}
```

### Step 2: Register Events

```csharp
private void RegisterEvents()
{
    // Listen to mining events from Mining module
    CoreAPI.Events.Subscribe<Player, string>("OnOreHit", (player, oreName) => {
        AddSkillXP(player, "Mining", 5);
    });
    
    // Listen to combat events from Combat module
    CoreAPI.Events.Subscribe<Player, float>("OnDamageDealt", (player, damage) => {
        AddSkillXP(player, "Combat", damage * 0.1f);
    });
}
```

### Step 3: Use Persistence

```csharp
private void SavePlayerSkills(long playerID)
{
    if (_playerSkills.ContainsKey(playerID))
    {
        var skills = _playerSkills[playerID];
        CoreAPI.Persistence.SavePlayerData(ModuleID, playerID, skills);
    }
}

private void LoadPlayerSkills(long playerID)
{
    var skills = CoreAPI.Persistence.LoadPlayerData<PlayerSkills>(ModuleID, playerID);
    
    if (skills != null)
    {
        _playerSkills[playerID] = skills;
    }
    else
    {
        _playerSkills[playerID] = new PlayerSkills();
    }
}
```

### Step 4: Setup Networking

```csharp
private void RegisterNetworkHandlers()
{
    // Register RPC to sync skill XP to clients
    CoreAPI.Network.RegisterRPC(ModuleID, "SyncSkillXP", HandleSyncSkillXP);
}

private void SyncSkillXPToClient(long playerID, string skillName, float xp)
{
    if (CoreAPI.Network.IsServer())
    {
        ZPackage pkg = new ZPackage();
        pkg.Write(playerID);
        pkg.Write(skillName);
        pkg.Write(xp);
        
        CoreAPI.Network.SendToAll(ModuleID, "SyncSkillXP", pkg);
    }
}

private void HandleSyncSkillXP(ZRpc rpc, ZPackage pkg)
{
    long playerID = pkg.ReadLong();
    string skillName = pkg.ReadString();
    float xp = pkg.ReadSingle();
    
    // Update local data
    if (_playerSkills.ContainsKey(playerID))
    {
        _playerSkills[playerID].SetXP(skillName, xp);
    }
}
```

### Step 5: Use Configuration

```csharp
private void LoadConfig()
{
    // Bind config values
    var maxLevel = CoreAPI.Config.Bind(
        ModuleID, 
        "MaxLevel", 
        100, 
        "Maximum skill level"
    );
    
    var xpMultiplier = CoreAPI.Config.Bind(
        ModuleID,
        "XPMultiplier",
        1.0f,
        "XP gain multiplier"
    );
    
    // Use config values
    _maxLevel = maxLevel.Value;
    _xpMultiplier = xpMultiplier.Value;
}
```

## Inter-Module Communication

Modules communicate through the EventBus, never directly:

### Module A (Publisher)
```csharp
// Mining module publishes when ore is hit
CoreAPI.Events.Publish("OnOreHit", player, "Copper");
```

### Module B (Subscriber)
```csharp
// Skills module listens and grants XP
CoreAPI.Events.Subscribe<Player, string>("OnOreHit", (player, oreName) => {
    AddMiningXP(player, 5);
});
```

### Module C (Also Subscriber)
```csharp
// Economy module listens and tracks resource gathering
CoreAPI.Events.Subscribe<Player, string>("OnOreHit", (player, oreName) => {
    TrackResourceGathering(player, oreName);
});
```

## Module Discovery

The Core automatically discovers your module:

1. Compile your module as a .dll
2. Place it in `BepInEx/plugins/`
3. Core scans all assemblies for `ICoreModule` implementations
4. Core instantiates and registers your module
5. Core calls `OnCoreReady()` when systems are initialized

**No manual registration required!**

## Best Practices

### ✅ DO:
- Use the EventBus for inter-module communication
- Namespace your events (e.g., "Skills.OnLevelUp")
- Check if Core is ready before using APIs
- Clean up resources in `OnShutdown()`
- Use the provided logging (`CoreAPI.Log`)
- Save data in `OnSave()` callback

### ❌ DON'T:
- Reference other Sidecar assemblies directly
- Store state in static variables without cleanup
- Assume other modules are present
- Block the main thread with heavy operations
- Ignore exceptions in event handlers

## Event Naming Conventions

Use clear, descriptive event names:

```csharp
// Good
"OnSkillLevelUp"
"OnPlayerDeath"
"OnOreHit"
"OnSpellCast"

// Bad
"Update"
"Event1"
"Trigger"
```

## Debugging Tips

### Enable Debug Logging
```csharp
CoreAPI.Log.LogDebug($"[{ModuleID}] Debug info here");
```

### Check Module Registration
```csharp
bool isRegistered = CoreLifecycle.IsModuleRegistered("UltimaValheim.Skills");
```

### Monitor Event Subscriptions
```csharp
int count = CoreAPI.Events.GetSubscriberCount("OnSkillLevelUp");
CoreAPI.Log.LogInfo($"OnSkillLevelUp has {count} subscriber(s)");
```

## Example Module Structure

```
UltimaValheim.Skills/
├── SkillsModule.cs          # Main module class (ICoreModule)
├── Data/
│   ├── PlayerSkills.cs      # Data models
│   └── SkillDefinition.cs
├── Systems/
│   ├── SkillXPSystem.cs     # XP calculation logic
│   └── SkillEffectSystem.cs # Skill effects/bonuses
└── SkillsModule.csproj      # Project file
```

## Testing Your Module

1. Build your module
2. Copy to `BepInEx/plugins/`
3. Launch Valheim
4. Check `BepInEx/LogOutput.log` for:
   - Core initialization
   - Your module being discovered
   - `OnCoreReady()` being called

## Next Steps

- Study the ExampleSidecarModule for a complete working example
- Review the Core API documentation
- Look at other Sidecar modules (Skills, Combat, Magic) for patterns
- Join the modding community for support

## Common Patterns

### Pattern: Conditional Module Integration
```csharp
public void OnCoreReady()
{
    // Check if Skills module is present
    if (CoreLifecycle.IsModuleRegistered("UltimaValheim.Skills"))
    {
        // Subscribe to skills events
        CoreAPI.Events.Subscribe<Player, string, int>("OnSkillLevelUp", HandleSkillLevelUp);
    }
}
```

### Pattern: Rate-Limited Events
```csharp
private float _lastEventTime = 0f;
private const float EVENT_COOLDOWN = 1.0f;

public void Update()
{
    if (Time.time - _lastEventTime > EVENT_COOLDOWN)
    {
        CoreAPI.Events.Publish("OnPeriodicUpdate");
        _lastEventTime = Time.time;
    }
}
```

### Pattern: Batched Persistence
```csharp
private bool _isDirty = false;

public void AddXP(long playerID, string skill, float amount)
{
    // Update data
    _playerSkills[playerID].AddXP(skill, amount);
    _isDirty = true;
}

public void OnSave()
{
    if (_isDirty)
    {
        SaveAllPlayerData();
        _isDirty = false;
    }
}
```

## Support

- Documentation: See Core README.md
- Examples: See ExampleSidecarModule.cs
- Issues: GitHub Issues
- Community: Discord server

Happy modding! 🎮
