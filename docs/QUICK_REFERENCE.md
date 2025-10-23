# Ultima Valheim Core API - Quick Reference

## 🎯 Creating a Module

```csharp
using System;
using UltimaValheim.Core;

public class MyModule : ICoreModule
{
    public string ModuleID => "UltimaValheim.MyModule";
    public Version ModuleVersion => new Version(1, 0, 0);
    
    public void OnCoreReady() { /* Initialize */ }
    public void OnPlayerJoin(Player player) { /* Handle join */ }
    public void OnPlayerLeave(Player player) { /* Handle leave */ }
    public void OnSave() { /* Save data */ }
    public void OnShutdown() { /* Cleanup */ }
}
```

## 📡 Event System

### Subscribe to Events
```csharp
// No arguments
CoreAPI.Events.Subscribe("OnSave", () => { });

// One argument
CoreAPI.Events.Subscribe<Player>("OnPlayerJoin", player => { });

// Two arguments
CoreAPI.Events.Subscribe<Player, string>("OnSkillGain", (player, skill) => { });

// Three arguments
CoreAPI.Events.Subscribe<Player, string, int>("OnLevelUp", 
    (player, skill, level) => { });

// Four arguments
CoreAPI.Events.Subscribe<Player, string, int, float>("OnXPGain", 
    (player, skill, level, xp) => { });
```

### Publish Events
```csharp
CoreAPI.Events.Publish("OnSave");
CoreAPI.Events.Publish("OnPlayerJoin", player);
CoreAPI.Events.Publish("OnSkillGain", player, "Mining");
CoreAPI.Events.Publish("OnLevelUp", player, "Mining", 15);
CoreAPI.Events.Publish("OnXPGain", player, "Mining", 15, 125.5f);
```

### Unsubscribe
```csharp
CoreAPI.Events.Unsubscribe("OnSave", myHandler);
```

## 🌐 Network System

### Register RPC Handler
```csharp
CoreAPI.Network.RegisterRPC(ModuleID, "MyRPC", (rpc, pkg) => {
    string data = pkg.ReadString();
    int value = pkg.ReadInt();
    // Process...
});
```

### Send RPC to Server
```csharp
ZPackage pkg = new ZPackage();
pkg.Write("some data");
pkg.Write(123);
CoreAPI.Network.SendToServer(ModuleID, "MyRPC", pkg);
```

### Send RPC to All Clients
```csharp
ZPackage pkg = new ZPackage();
pkg.Write("broadcast data");
CoreAPI.Network.SendToAll(ModuleID, "MyRPC", pkg);
```

### Send RPC to Specific Peer
```csharp
ZNetPeer peer = GetSomePeer();
ZPackage pkg = new ZPackage();
pkg.Write("direct data");
CoreAPI.Network.SendToPeer(peer, ModuleID, "MyRPC", pkg);
```

### Check Server Status
```csharp
bool isServer = CoreAPI.Network.IsServer();
bool isConnected = CoreAPI.Network.IsConnected();
```

## 💾 Persistence System

### Save Player Data
```csharp
CoreAPI.Persistence.SavePlayerData(ModuleID, playerID, myData);
```

### Load Player Data
```csharp
var data = CoreAPI.Persistence.LoadPlayerData<MyDataType>(ModuleID, playerID);
if (data != null) {
    // Use data
}
```

### Save World Data
```csharp
CoreAPI.Persistence.SaveWorldData(ModuleID, worldData);
```

### Load World Data
```csharp
var data = CoreAPI.Persistence.LoadWorldData<MyWorldData>(ModuleID);
```

### Persist to Disk
```csharp
CoreAPI.Persistence.SaveToDisk();  // Save all data
CoreAPI.Persistence.LoadFromDisk(); // Load all data
```

### Check if Data Exists
```csharp
bool hasData = CoreAPI.Persistence.HasPlayerData(ModuleID, playerID);
bool hasWorld = CoreAPI.Persistence.HasWorldData(ModuleID);
```

## ⚙️ Configuration System

### Bind Config Value
```csharp
var maxLevel = CoreAPI.Config.Bind(
    ModuleID,           // Module ID
    "MaxLevel",         // Config key
    100,                // Default value
    "Maximum level"     // Description
);
```

### Use Config Value
```csharp
int level = maxLevel.Value;
maxLevel.Value = 150;  // Update value
```

### Get Existing Config
```csharp
var entry = CoreAPI.Config.GetEntry<int>(ModuleID, "MaxLevel");
if (entry != null) {
    int value = entry.Value;
}
```

### Save/Reload Configs
```csharp
CoreAPI.Config.SaveAll();   // Save all configs
CoreAPI.Config.ReloadAll(); // Reload from disk
```

## 📝 Logging

```csharp
CoreAPI.Log.LogInfo($"[{ModuleID}] Info message");
CoreAPI.Log.LogWarning($"[{ModuleID}] Warning message");
CoreAPI.Log.LogError($"[{ModuleID}] Error message");
CoreAPI.Log.LogDebug($"[{ModuleID}] Debug message");
```

## 🔍 Module Queries

### Check if Module is Registered
```csharp
bool exists = CoreLifecycle.IsModuleRegistered("UltimaValheim.Skills");
```

### Get Module Reference
```csharp
var module = CoreLifecycle.GetModule("UltimaValheim.Skills");
if (module != null) {
    // Interact with module
}
```

### Get All Modules
```csharp
var modules = CoreLifecycle.Modules;
foreach (var module in modules) {
    CoreAPI.Log.LogInfo($"Found: {module.ModuleID} v{module.ModuleVersion}");
}
```

## 🎮 Common Patterns

### Pattern: Safe Module Integration
```csharp
public void OnCoreReady()
{
    if (CoreLifecycle.IsModuleRegistered("UltimaValheim.Skills"))
    {
        CoreAPI.Events.Subscribe<Player, string, int>("Skills.OnLevelUp", 
            OnSkillLevelUp);
    }
}
```

### Pattern: Player Data Management
```csharp
public void OnPlayerJoin(Player player)
{
    long playerID = player.GetPlayerID();
    var data = CoreAPI.Persistence.LoadPlayerData<PlayerData>(ModuleID, playerID);
    
    if (data == null)
    {
        data = new PlayerData();  // New player
    }
    
    _playerData[playerID] = data;
}

public void OnPlayerLeave(Player player)
{
    long playerID = player.GetPlayerID();
    if (_playerData.ContainsKey(playerID))
    {
        CoreAPI.Persistence.SavePlayerData(ModuleID, playerID, _playerData[playerID]);
        _playerData.Remove(playerID);
    }
}
```

### Pattern: Network Sync
```csharp
// Server: Broadcast change to all clients
if (CoreAPI.Network.IsServer())
{
    ZPackage pkg = new ZPackage();
    pkg.Write(playerID);
    pkg.Write(newValue);
    CoreAPI.Network.SendToAll(ModuleID, "SyncValue", pkg);
}

// Client: Request sync from server
if (!CoreAPI.Network.IsServer())
{
    ZPackage pkg = new ZPackage();
    pkg.Write(playerID);
    CoreAPI.Network.SendToServer(ModuleID, "RequestSync", pkg);
}
```

### Pattern: Conditional Features
```csharp
private ConfigEntry<bool> _enableFeature;

public void OnCoreReady()
{
    _enableFeature = CoreAPI.Config.Bind(ModuleID, "EnableFeature", true, 
        "Enable this feature");
    
    if (_enableFeature.Value)
    {
        InitializeFeature();
    }
}
```

## 🎯 Best Practices Checklist

- ✅ Always check for null before using player references
- ✅ Use ModuleID as event namespace: "MyModule.EventName"
- ✅ Save data in OnSave() callback
- ✅ Clean up resources in OnShutdown()
- ✅ Check IsServer() before broadcasting
- ✅ Use try-catch in event handlers
- ✅ Log important operations
- ✅ Validate RPC data before processing
- ✅ Don't block the main thread
- ✅ Test in both single-player and multiplayer

## 📚 Event Naming Conventions

```
Good Event Names:
- OnPlayerJoin
- OnSkillLevelUp
- OnDamageDealt
- OnOreHit
- OnSpellCast
- Skills.XPGained
- Combat.WeaponSwing

Bad Event Names:
- Update
- Event1
- Trigger
- DoStuff
```

## 🔧 Debugging

```csharp
// Check event subscribers
int count = CoreAPI.Events.GetSubscriberCount("MyEvent");
bool hasSubscribers = CoreAPI.Events.HasSubscribers("MyEvent");

// Check Core status
bool ready = CoreAPI.IsReady;

// List all modules
foreach (var module in CoreLifecycle.Modules)
{
    CoreAPI.Log.LogInfo($"{module.ModuleID} v{module.ModuleVersion}");
}
```

## 🚀 Quick Start Template

```csharp
using System;
using System.Collections.Generic;
using UltimaValheim.Core;

namespace UltimaValheim.MyModule
{
    public class MyModule : ICoreModule
    {
        public string ModuleID => "UltimaValheim.MyModule";
        public Version ModuleVersion => new Version(1, 0, 0);
        
        private ConfigEntry<bool> _enabled;
        private Dictionary<long, PlayerData> _playerData = new();

        public void OnCoreReady()
        {
            _enabled = CoreAPI.Config.Bind(ModuleID, "Enabled", true);
            
            CoreAPI.Events.Subscribe<Player>("OnPlayerJoin", HandlePlayerJoin);
            CoreAPI.Network.RegisterRPC(ModuleID, "Sync", HandleSync);
            
            CoreAPI.Log.LogInfo($"[{ModuleID}] Initialized!");
        }

        public void OnPlayerJoin(Player player)
        {
            long id = player.GetPlayerID();
            _playerData[id] = CoreAPI.Persistence.LoadPlayerData<PlayerData>(ModuleID, id) 
                ?? new PlayerData();
        }

        public void OnPlayerLeave(Player player)
        {
            long id = player.GetPlayerID();
            if (_playerData.ContainsKey(id))
            {
                CoreAPI.Persistence.SavePlayerData(ModuleID, id, _playerData[id]);
                _playerData.Remove(id);
            }
        }

        public void OnSave()
        {
            CoreAPI.Persistence.SaveToDisk();
        }

        public void OnShutdown()
        {
            _playerData.Clear();
        }

        private void HandlePlayerJoin(Player player) { }
        private void HandleSync(ZRpc rpc, ZPackage pkg) { }
    }

    [Serializable]
    public class PlayerData
    {
        public int Level { get; set; }
        public float XP { get; set; }
    }
}
```

---

**For complete documentation, see:**
- `README.md` - Project overview
- `GETTING_STARTED.md` - Detailed tutorial
- `UltimaValheimCore/README.md` - Core API reference
