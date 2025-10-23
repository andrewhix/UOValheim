# Build Instructions

This guide explains how to build the Ultima Valheim (UOV) mod from source.

## Prerequisites

### Required Software
- **Visual Studio 2019 or later** (with .NET Framework 4.8 support)
  - OR **Rider** by JetBrains
  - OR **.NET SDK 6.0+** for command-line builds
- **Valheim** (installed via Steam)
- **BepInEx 5.x** (installed in Valheim)
- **Jotunn Mod Library** (installed in BepInEx/plugins/)

### Environment Setup

1. **Set VALHEIM_INSTALL Environment Variable**

   **Windows (System-wide):**
   ```cmd
   setx VALHEIM_INSTALL "C:\Program Files (x86)\Steam\steamapps\common\Valheim"
   ```

   **Windows (Current Session):**
   ```cmd
   set VALHEIM_INSTALL=C:\Program Files (x86)\Steam\steamapps\common\Valheim
   ```

   **Alternative:** Edit `Directory.Build.props` and change the default path:
   ```xml
   <VALHEIM_INSTALL>YOUR_PATH_HERE</VALHEIM_INSTALL>
   ```

2. **Verify BepInEx Installation**
   - Check that `BepInEx/` folder exists in your Valheim directory
   - Verify `BepInEx/core/BepInEx.dll` exists
   - Verify `BepInEx/plugins/Jotunn.dll` exists

## Building with Visual Studio

1. **Open the Solution**
   - Double-click `UOV.sln`
   - Visual Studio will open with both projects loaded

2. **Restore NuGet Packages**
   - Right-click solution → "Restore NuGet Packages"
   - Or it will restore automatically on first build

3. **Build the Solution**
   - Press `Ctrl+Shift+B` or select **Build → Build Solution**
   - Or right-click solution → "Build Solution"

4. **Output Location**
   - DLLs will be in `Builds/` folder at the solution root
   - `UltimaValheimCore.dll`
   - `ExampleSidecar.dll`

## Building with Rider

1. **Open the Solution**
   - File → Open → Select `UOV.sln`

2. **Restore and Build**
   - Rider will automatically restore NuGet packages
   - Press `Ctrl+Shift+F9` or select **Build → Build Solution**

3. **Output Location**
   - DLLs will be in `Builds/` folder

## Building from Command Line

```bash
# Navigate to repository root
cd C:\Valheim Modding\UOV

# Restore NuGet packages
dotnet restore

# Build in Release mode
dotnet build --configuration Release

# Build in Debug mode (includes debug symbols)
dotnet build --configuration Debug
```

## Installing Built Mods

After building, copy the DLLs to Valheim:

```bash
# Copy Core (required)
copy Builds\UltimaValheimCore.dll "%VALHEIM_INSTALL%\BepInEx\plugins\"

# Copy Example (optional, for testing)
copy Builds\ExampleSidecar.dll "%VALHEIM_INSTALL%\BepInEx\plugins\"
```

## Troubleshooting

### "Could not find assembly_valheim.dll"

**Solution:** Ensure `VALHEIM_INSTALL` points to your Valheim installation:
```cmd
echo %VALHEIM_INSTALL%
```

Should output: `C:\Program Files (x86)\Steam\steamapps\common\Valheim`

### "Could not find BepInEx.dll"

**Solution:** Install BepInEx:
1. Download from https://github.com/BepInEx/BepInEx/releases
2. Extract to Valheim folder
3. Run Valheim once to generate configs

### "Could not find Jotunn.dll"

**Solution:** Install Jotunn:
1. Download from https://valheim.thunderstore.io/package/ValheimModding/Jotunn/
2. Extract to `BepInEx/plugins/`

### "publicized_assemblies not found"

**Solution:** You need publicized assemblies for modding:
1. Use BepInEx.AssemblyPublicizer
2. Or download pre-publicized assemblies from Jotunn

Update `Directory.Build.props`:
```xml
<Reference Include="assembly_valheim_publicized">
    <HintPath>$(VALHEIM_INSTALL)\valheim_Data\Managed\publicized_assemblies\assembly_valheim_publicized.dll</HintPath>
</Reference>
```

### Build succeeds but mod doesn't load

**Check these:**
1. DLL is in `BepInEx/plugins/`
2. BepInEx is installed correctly
3. Jotunn is installed
4. Check `BepInEx/LogOutput.log` for errors

## Development Tips

### Debugging

1. **Build in Debug mode** for debug symbols:
   ```bash
   dotnet build --configuration Debug
   ```

2. **Attach Visual Studio to Valheim**:
   - Start Valheim
   - In Visual Studio: Debug → Attach to Process
   - Select `valheim.exe`
   - Set breakpoints in your code

3. **Enable BepInEx Logging**:
   - Edit `BepInEx/config/BepInEx.cfg`
   - Set `[Logging.Console] Enabled = true`
   - Set `[Logging.Console] LogLevels = All`

### Hot Reload (Limited)

For quick iteration:
1. Build your module
2. Exit Valheim completely
3. Copy new DLL to plugins folder
4. Restart Valheim

**Note:** Harmony patches require full restart.

### Testing Multiplayer

1. Build and install on both machines
2. Host a game on one machine
3. Join from the second machine
4. Check logs on both for sync issues

## CI/CD (GitHub Actions)

The repository includes a GitHub Actions workflow that automatically builds on push:

- Location: `.github/workflows/build.yml`
- Triggered on: push to `main` or `develop` branches
- Artifacts: Available in GitHub Actions tab

## Project Structure

```
UOV/
├── UOV.sln                          # Visual Studio solution
├── Directory.Build.props            # Shared build configuration
├── Builds/                          # Output folder (gitignored)
├── UltimaValheimCore/              # Core module project
│   ├── UltimaValheimCore.csproj
│   ├── UltimaValheimCore.cs
│   └── Core/                        # Core systems
└── ExampleSidecar/                  # Example module project
    ├── ExampleSidecar.csproj
    └── ExampleSidecarModule.cs
```

## Next Steps

After building successfully:

1. Read [GETTING_STARTED.md](docs/GETTING_STARTED.md) to learn how to create modules
2. Study [ExampleSidecar](ExampleSidecar/) for a working example
3. Check [QUICK_REFERENCE.md](docs/QUICK_REFERENCE.md) for API reference
4. Join the community for support and collaboration

## Support

- **GitHub Issues**: Report build problems
- **Discord**: Get help from the community
- **Jotunn Docs**: https://valheim-modding.github.io/Jotunn/

Happy modding! 🔨
