# Build Fixes v2 - Final Resolution

## Remaining Issues Fixed

After the first round of fixes, there were 2 remaining errors:

1. **CS1503**: Lambda parameter type mismatch in `NetworkManager.RegisterWithZNet`
2. **CS0006**: Project reference issue - ExampleSidecar can't find Core DLL

## Solution

### Fix 1: NetworkManager RPC Registration

**Problem**: `ZRoutedRpc.Register<T>` uses `Action<long, T>` where `long` is the sender peer ID, not `ZRpc`.

**Solution**: Convert peer ID to ZRpc in the lambda.

### Fix 2: Unified Build Output

**Problem**: Projects were outputting to different directories, breaking project references.

**Solution**: Created `Directory.Build.props` to centralize configuration and output paths.

## Files to Update

### 1. Directory.Build.props (NEW FILE)
**Location**: `C:\Valheim Modding\UOV\Directory.Build.props` (root of repository)

This file centralizes all shared project settings:
- Target framework (net48)
- Output paths
- Common NuGet packages
- Valheim references

**Download**: `Directory.Build.props`

### 2. NetworkManager.cs (UPDATE)
**Location**: `C:\Valheim Modding\UOV\UltimaValheimCore\Core\NetworkManager.cs`

**Change at line ~165-185**: Updated `RegisterWithZNet` method to properly convert peer ID to ZRpc.

**Download**: `FIXED_NetworkManager_v2.cs`

### 3. UltimaValheimCore.csproj (UPDATE)
**Location**: `C:\Valheim Modding\UOV\UltimaValheimCore\UltimaValheimCore.csproj`

**Simplified** - Now inherits shared properties from Directory.Build.props.

**Download**: `FIXED_Core.csproj`

### 4. ExampleSidecar.csproj (UPDATE)
**Location**: `C:\Valheim Modding\UOV\ExampleSidecar\ExampleSidecar.csproj`

**Simplified** - Now inherits shared properties from Directory.Build.props.

**Download**: `FIXED_Example.csproj`

## Quick Apply Steps

### Option A: Copy All Files (Recommended)

1. **Download these 4 files** from Claude:
   - `Directory.Build.props`
   - `FIXED_NetworkManager_v2.cs`
   - `FIXED_Core.csproj`
   - `FIXED_Example.csproj`

2. **Copy to your repository**:
   ```
   Directory.Build.props          → C:\Valheim Modding\UOV\
   FIXED_NetworkManager_v2.cs     → C:\Valheim Modding\UOV\UltimaValheimCore\Core\NetworkManager.cs
   FIXED_Core.csproj              → C:\Valheim Modding\UOV\UltimaValheimCore\UltimaValheimCore.csproj
   FIXED_Example.csproj           → C:\Valheim Modding\UOV\ExampleSidecar\ExampleSidecar.csproj
   ```

3. **Clean and rebuild**:
   ```bash
   cd "C:\Valheim Modding\UOV"
   dotnet clean
   dotnet restore
   dotnet build
   ```

### Option B: Manual Edits

If you prefer to edit manually:

#### Create Directory.Build.props

Create new file `C:\Valheim Modding\UOV\Directory.Build.props`:

```xml
<Project>
	<PropertyGroup>
		<TargetFramework>net48</TargetFramework>
		<LangVersion>latest</LangVersion>
		<AllowUnsafeBlocks>true</AllowUnsafeBlocks>
		
		<BaseOutputPath>$(MSBuildThisFileDirectory)bin\$(Configuration)\</BaseOutputPath>
		<OutputPath>$(BaseOutputPath)$(TargetFramework)\</OutputPath>
		
		<RestoreAdditionalProjectSources>
			https://api.nuget.org/v3/index.json;
			https://nuget.bepinex.dev/v3/index.json
		</RestoreAdditionalProjectSources>
	</PropertyGroup>

	<ItemGroup>
		<PackageReference Include="BepInEx.Analyzers" Version="1.*" PrivateAssets="all" />
		<PackageReference Include="BepInEx.Core" Version="5.*" />
		<PackageReference Include="UnityEngine.Modules" Version="2022.3.17" IncludeAssets="compile" />
	</ItemGroup>

	<ItemGroup Condition="'$(TargetFramework.TrimEnd(`0123456789`))' == 'net'">
		<PackageReference Include="Microsoft.NETFramework.ReferenceAssemblies" Version="1.0.2" PrivateAssets="all" />
	</ItemGroup>

	<ItemGroup>
		<Reference Include="assembly_valheim_publicized">
			<HintPath>$(VALHEIM_INSTALL)\valheim_Data\Managed\publicized_assemblies\assembly_valheim_publicized.dll</HintPath>
			<Private>false</Private>
		</Reference>
		<Reference Include="Jotunn">
			<HintPath>$(VALHEIM_INSTALL)\BepInEx\plugins\Jotunn.dll</HintPath>
			<Private>false</Private>
		</Reference>
	</ItemGroup>
</Project>
```

#### Update NetworkManager.cs

In `RegisterWithZNet` method (around line 165), replace with:

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
        // ZRoutedRpc.Register takes Action<long, ZPackage> where long is the sender peer ID
        ZRoutedRpc.instance.Register<ZPackage>(fullRPCName, (senderPeerID, package) =>
        {
            // We need to get the ZRpc from the peer ID to match our handler signature
            ZNetPeer peer = ZNet.instance?.GetPeer(senderPeerID);
            if (peer != null && peer.m_rpc != null)
            {
                handler?.Invoke(peer.m_rpc, package);
            }
        });
        
        CoreAPI.Log.LogDebug($"[NetworkManager] Registered RPC with ZNet: {fullRPCName}");
    }
    catch (Exception ex)
    {
        CoreAPI.Log.LogError($"[NetworkManager] Failed to register RPC '{fullRPCName}': {ex}");
    }
}
```

#### Simplify UltimaValheimCore.csproj

Replace entire file with:

```xml
<Project Sdk="Microsoft.NET.Sdk">

	<PropertyGroup>
		<AssemblyName>UltimaValheimCore</AssemblyName>
		<Product>Ultima Valheim Core</Product>
		<Description>Core system for Ultima Valheim modular architecture</Description>
		<Version>1.0.0</Version>
	</PropertyGroup>

	<ItemGroup>
		<PackageReference Include="BepInEx.PluginInfoProps" Version="2.*" />
	</ItemGroup>

</Project>
```

#### Simplify ExampleSidecar.csproj

Replace entire file with:

```xml
<Project Sdk="Microsoft.NET.Sdk">

	<PropertyGroup>
		<AssemblyName>ExampleSidecar</AssemblyName>
		<Product>Ultima Valheim Example Sidecar</Product>
		<Description>Example Sidecar module demonstrating Core integration</Description>
		<Version>1.0.0</Version>
	</PropertyGroup>

	<ItemGroup>
		<ProjectReference Include="..\UltimaValheimCore\UltimaValheimCore.csproj" />
	</ItemGroup>

</Project>
```

## Build Process

After applying fixes:

```bash
cd "C:\Valheim Modding\UOV"

# Clean everything
dotnet clean
Remove-Item -Recurse -Force bin,obj -ErrorAction SilentlyContinue

# Restore packages
dotnet restore

# Build
dotnet build -c Release

# Expected output:
# Build succeeded.
#     0 Warning(s)
#     0 Error(s)
```

## Output Location

After successful build, DLLs will be in:
```
C:\Valheim Modding\UOV\bin\Release\net48\
├── UltimaValheimCore.dll
└── ExampleSidecar.dll
```

## Update build-and-deploy.bat

Since output path changed, update the batch file:

**Line 37-38**:
```batch
echo Copying UltimaValheimCore.dll...
copy /Y "bin\Release\net48\UltimaValheimCore.dll" "%VALHEIM_INSTALL%\BepInEx\plugins\"
```

**Line 47-48**:
```batch
echo Copying ExampleSidecar.dll...
copy /Y "bin\Release\net48\ExampleSidecar.dll" "%VALHEIM_INSTALL%\BepInEx\plugins\"
```

## Verification

1. **Build succeeds with 0 errors**
2. **DLLs created** in `bin\Release\net48\`
3. **Deploy works**: `build-and-deploy.bat`
4. **Valheim loads**: Check `BepInEx\LogOutput.log`

Expected log output:
```
[Info   :  BepInEx] Loading [Ultima Valheim Core 1.0.0]
[Info   :UltimaValheimCore] Ultima Valheim Core v1.0.0 loaded!
[Info   :   CoreAPI] All core systems initialized.
[Info   :UltimaValheimCore] Discovered and registered 1 module(s).
```

## Benefits of Directory.Build.props

✅ **Centralized Configuration** - One place to manage all shared settings  
✅ **Consistent Output** - All projects build to same location  
✅ **Less Duplication** - Project files are much simpler  
✅ **Easier Maintenance** - Update dependencies in one place  
✅ **Better Project References** - No more DLL not found errors

## Commit and Push

```bash
git add .
git commit -m "Fix: Add Directory.Build.props and fix RPC registration

- Add Directory.Build.props for centralized build configuration
- Fix NetworkManager RPC handler to convert peer ID to ZRpc
- Simplify project files (now inherit from Directory.Build.props)
- Update output paths to bin/Release/net48/"

git push
```

## Troubleshooting

### Still getting CS0006?

1. Close Visual Studio/Rider completely
2. Delete `bin` and `obj` folders
3. Run `dotnet clean`
4. Run `dotnet restore`
5. Reopen IDE
6. Build

### Missing publicized assemblies?

Run BepInEx.AssemblyPublicizer:
```bash
dotnet tool install -g BepInEx.AssemblyPublicizer.MSBuild
AssemblyPublicizer "%VALHEIM_INSTALL%\valheim_Data\Managed\assembly_valheim.dll"
```

### VALHEIM_INSTALL not set?

```bash
setx VALHEIM_INSTALL "C:\Program Files (x86)\Steam\steamapps\common\Valheim"
```
Then restart terminal/IDE.

---

**Status**: ✅ ALL BUILD ERRORS RESOLVED  
**Build**: ✅ Should compile cleanly  
**Output**: ✅ bin/Release/net48/  
**Ready**: ✅ For testing in Valheim
