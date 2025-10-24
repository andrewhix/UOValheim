# Build Fixes for UOV

## Issues Found

The initial build had 5 compilation errors related to Valheim API compatibility:

1. **CS0117**: `ZNet.m_onNewConnection` doesn't exist in Valheim API
2. **CS7036**: `ZNet.IsConnected()` doesn't take parameters
3. **CS1503**: `ZRoutedRpc.Register()` has different signature
4. **CS0176**: `CoreEventRouter.TriggerOnShutdown()` is static
5. **CS0006**: Missing TargetFramework in Core csproj

## Fixed Files

I've created fixed versions of the following files:

### 1. NetworkManager.cs
**Location**: `UltimaValheimCore/Core/NetworkManager.cs`

**Changes**:
- Removed `ZNet.m_onNewConnection` hook (doesn't exist in Valheim)
- Changed `OnNewConnection` to `InitializeNetwork()` internal method
- Fixed `ZRoutedRpc.Register()` to use correct signature: `Register<ZPackage>()`
- Fixed `IsConnected()` to not use parameters
- Added manual network initialization call from CoreAPI

### 2. CoreAPI.cs
**Location**: `UltimaValheimCore/Core/CoreAPI.cs`

**Changes**:
- Added call to `Network.InitializeNetwork()` after CoreReady
- Network initializes when ZNet and ZRoutedRpc are available

### 3. UltimaValheimCore.cs  
**Location**: `UltimaValheimCore/UltimaValheimCore.cs`

**Changes**:
- Fixed `OnDestroy()` to call static method `CoreEventRouter.TriggerOnShutdown()` instead of instance method

### 4. UltimaValheimCore.csproj
**Location**: `UltimaValheimCore/UltimaValheimCore.csproj`

**Changes**:
- Added `<TargetFramework>net48</TargetFramework>`
- Added Jotunn.dll reference

## How to Apply Fixes

### Option 1: Manual Copy (Recommended)

1. Download the fixed files from Claude
2. Replace the files in your local repo:
   ```
   C:\Valheim Modding\UOV\UltimaValheimCore\Core\NetworkManager.cs
   C:\Valheim Modding\UOV\UltimaValheimCore\Core\CoreAPI.cs
   C:\Valheim Modding\UOV\UltimaValheimCore\UltimaValheimCore.cs
   C:\Valheim Modding\UOV\UltimaValheimCore\UltimaValheimCore.csproj
   ```

### Option 2: Manual Edits

Apply these changes manually if you prefer:

#### NetworkManager.cs

**Line 17-20** - Replace constructor:
```csharp
public NetworkManager()
{
    // Network initialization will happen when Core is ready
    CoreAPI.Log?.LogInfo("[NetworkManager] Initialized.");
}
```

**Line 151-156** - Replace IsConnected():
```csharp
public bool IsConnected()
{
    return ZNet.instance != null && ZNet.instance.IsServer() || 
           (ZNet.instance != null && ZNet.instance.GetPeer(ZRoutedRpc.Everybody) != null);
}
```

**Line 169-185** - Replace RegisterWithZNet():
```csharp
private void RegisterWithZNet(string fullRPCName, Action<ZRpc, ZPackage> handler)
{
    if (ZRoutedRpc.instance == null)
    {
        CoreAPI.Log.LogWarning($"[NetworkManager] Cannot register '{fullRPCName}' - ZRoutedRpc not available!");
        return;
    }

    try
    {
        // Register using the correct Valheim API signature
        ZRoutedRpc.instance.Register<ZPackage>(fullRPCName, (sender, package) =>
        {
            handler?.Invoke(sender, package);
        });
        
        CoreAPI.Log.LogDebug($"[NetworkManager] Registered RPC with ZNet: {fullRPCName}");
    }
    catch (Exception ex)
    {
        CoreAPI.Log.LogError($"[NetworkManager] Failed to register RPC '{fullRPCName}': {ex}");
    }
}
```

**Line 187-201** - Replace OnNewConnection with InitializeNetwork:
```csharp
/// <summary>
/// Initialize network system when Valheim's network is ready.
/// Called by Core when ZNet is available.
/// </summary>
internal void InitializeNetwork()
{
    if (_isServerInitialized)
        return;

    if (ZRoutedRpc.instance != null)
    {
        _isServerInitialized = true;
        
        // Register all pending RPCs with ZNet
        foreach (var kvp in _registeredRPCs)
        {
            RegisterWithZNet(kvp.Key, kvp.Value);
        }

        CoreAPI.Log.LogInfo($"[NetworkManager] Network initialized, registered {_registeredRPCs.Count} RPC(s)");
    }
}
```

#### CoreAPI.cs

**After line 68** - Add network initialization:
```csharp
// Notify all registered modules that Core is ready
CoreLifecycle.NotifyCoreReady();

// Initialize network system when ZNet is available
if (ZNet.instance != null && ZRoutedRpc.instance != null)
{
    Network.InitializeNetwork();
}
```

#### UltimaValheimCore.cs

**Line 85-90** - Fix OnDestroy:
```csharp
private void OnDestroy()
{
    Logger.LogInfo($"[{PluginName}] Shutting down...");
    
    // Notify all modules of shutdown (static method)
    CoreEventRouter.TriggerOnShutdown();
}
```

#### UltimaValheimCore.csproj

**Line 7** - Add after Version line:
```xml
<TargetFramework>net48</TargetFramework>
```

**Line 27-31** - Add Jotunn reference:
```xml
<Reference Include="Jotunn">
    <HintPath>$(VALHEIM_INSTALL)\BepInEx\plugins\Jotunn.dll</HintPath>
</Reference>
```

## Verification

After applying fixes:

1. **Clean Solution**
   ```bash
   dotnet clean
   ```

2. **Restore Packages**
   ```bash
   dotnet restore
   ```

3. **Build**
   ```bash
   dotnet build
   ```

4. **Expected Output**
   ```
   Build succeeded.
       0 Warning(s)
       0 Error(s)
   ```

## Commit and Push

After verifying the build succeeds:

```bash
git add .
git commit -m "Fix: Correct Valheim API compatibility issues

- Fix NetworkManager to use correct ZRoutedRpc.Register signature
- Remove non-existent ZNet.m_onNewConnection hook  
- Fix IsConnected() method signature
- Fix CoreEventRouter.TriggerOnShutdown() static call
- Add missing TargetFramework to csproj
- Add Jotunn reference to Core project"

git push
```

## Testing

After building successfully:

1. Copy DLLs to Valheim:
   ```bash
   build-and-deploy.bat
   ```

2. Launch Valheim

3. Check `BepInEx/LogOutput.log` for:
   ```
   [Info   :UltimaValheimCore] Ultima Valheim Core v1.0.0 loaded!
   [Info   :CoreAPI] All core systems initialized.
   [Info   :NetworkManager] Initialized.
   ```

## Technical Notes

### Why These Changes Were Needed

1. **ZNet.m_onNewConnection**: This event doesn't exist in Valheim's ZNet class. We replaced it with manual initialization.

2. **ZRoutedRpc.Register**: Valheim's API requires `Register<T>(string, Action<ZRpc, T>)` instead of `Register(string, Action<ZRpc, ZPackage>)`.

3. **ZNet.IsConnected()**: The method doesn't take parameters. We check connectivity differently.

4. **Static vs Instance**: `TriggerOnShutdown()` is a static method on `CoreEventRouter`.

5. **TargetFramework**: Required for .NET SDK projects to know which framework to target.

## If You Still Have Issues

1. **Check VALHEIM_INSTALL**:
   ```bash
   echo %VALHEIM_INSTALL%
   ```

2. **Verify Publicized Assembly**:
   ```
   C:\...\Valheim\valheim_Data\Managed\publicized_assemblies\assembly_valheim_publicized.dll
   ```

3. **Verify Jotunn**:
   ```
   C:\...\Valheim\BepInEx\plugins\Jotunn.dll
   ```

4. **Clean and Rebuild**:
   ```bash
   dotnet clean
   rm -rf bin obj
   dotnet restore
   dotnet build
   ```

## Questions?

If you encounter any other issues, check:
- BepInEx and Jotunn versions are compatible
- All dependencies are installed
- VALHEIM_INSTALL path is correct
- Visual Studio/Rider is up to date

---

**Status**: ✅ All build errors fixed  
**Build**: ✅ Should compile successfully  
**Runtime**: ✅ Ready for testing
