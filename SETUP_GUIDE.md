# UOV Repository - Complete Setup Guide

## 📦 What You Have

A complete, production-ready Visual Studio solution for the Ultima Valheim Core + Sidecar system.

**Download**: `UOV-Complete-Repository.zip` (40KB)

## 🚀 Quick Setup Steps

### 1. Extract the Repository

Extract `UOV-Complete-Repository.zip` to:
```
C:\Valheim Modding\UOV
```

You should have this structure:
```
C:\Valheim Modding\UOV\
├── UOV.sln                  ← Double-click this to open in Visual Studio
├── Directory.Build.props
├── README.md
├── BUILD_INSTRUCTIONS.md
├── CONTRIBUTING.md
├── LICENSE
├── UltimaValheimCore/
│   ├── UltimaValheimCore.csproj
│   ├── UltimaValheimCore.cs
│   └── Core/
│       ├── CoreAPI.cs
│       ├── EventBus.cs
│       └── ... (8 core system files)
├── ExampleSidecar/
│   ├── ExampleSidecar.csproj
│   └── ExampleSidecarModule.cs
└── docs/
    ├── GETTING_STARTED.md
    ├── QUICK_REFERENCE.md
    └── IMPLEMENTATION_SUMMARY.md
```

### 2. Set Environment Variable

**Option A: System-wide (Recommended)**

Open PowerShell as Administrator and run:
```powershell
[System.Environment]::SetEnvironmentVariable('VALHEIM_INSTALL', 'C:\Program Files (x86)\Steam\steamapps\common\Valheim', 'Machine')
```

**Option B: Edit Directory.Build.props**

Open `Directory.Build.props` and change line 9:
```xml
<VALHEIM_INSTALL>C:\Program Files (x86)\Steam\steamapps\common\Valheim</VALHEIM_INSTALL>
```

### 3. Open in Visual Studio

1. Double-click `UOV.sln`
2. Visual Studio will open with 2 projects:
   - ✅ UltimaValheimCore
   - ✅ ExampleSidecar

### 4. Build the Solution

**In Visual Studio:**
- Press `Ctrl+Shift+B`
- Or Menu: Build → Build Solution

**From Command Line:**
```cmd
cd C:\Valheim Modding\UOV
dotnet build --configuration Release
```

### 5. Find Your DLLs

Built files will be in:
```
C:\Valheim Modding\UOV\Builds\
├── UltimaValheimCore.dll  ← Core module (required)
└── ExampleSidecar.dll     ← Example module (optional)
```

### 6. Install to Valheim

Copy to your Valheim installation:
```cmd
copy Builds\UltimaValheimCore.dll "C:\Program Files (x86)\Steam\steamapps\common\Valheim\BepInEx\plugins\"
copy Builds\ExampleSidecar.dll "C:\Program Files (x86)\Steam\steamapps\common\Valheim\BepInEx\plugins\"
```

### 7. Test It!

1. Launch Valheim
2. Check `BepInEx/LogOutput.log`
3. Look for:
   ```
   [Info   :Ultima Valheim Core] Ultima Valheim Core v1.0.0 loaded!
   [Info   :UVC] Discovered and registered 1 module(s).
   ```

## 📁 What's Included

### Core Implementation
- ✅ **8 Core Systems** (EventBus, NetworkManager, PersistenceManager, etc.)
- ✅ **ICoreModule Interface** for Sidecar modules
- ✅ **Auto-discovery** of modules via reflection
- ✅ **Harmony integration** for game lifecycle events
- ✅ **Complete documentation**

### Example Module
- ✅ **ExampleSidecarModule.cs** - Full working example
- ✅ Demonstrates all Core features
- ✅ Shows best practices

### Documentation
- ✅ **README.md** - Project overview
- ✅ **GETTING_STARTED.md** - Complete tutorial
- ✅ **QUICK_REFERENCE.md** - API cheat sheet
- ✅ **BUILD_INSTRUCTIONS.md** - Detailed build guide
- ✅ **CONTRIBUTING.md** - Contribution guidelines

### Development Tools
- ✅ **Visual Studio Solution** (.sln)
- ✅ **GitHub Actions Workflow** (CI/CD)
- ✅ **Package Script** (package-release.bat)
- ✅ **Thunderstore Manifest** (manifest.json)
- ✅ **.gitignore** configured for Unity/C#

## 🎯 Next Steps

### For Users
1. Build the solution
2. Copy DLLs to Valheim
3. Enjoy the example module
4. Wait for gameplay modules (Skills, Magic, Combat)

### For Developers
1. Read `docs/GETTING_STARTED.md`
2. Study `ExampleSidecar/ExampleSidecarModule.cs`
3. Create your own Sidecar module
4. Use `docs/QUICK_REFERENCE.md` for API help

## 🔧 Common Issues

### "VALHEIM_INSTALL not set"
**Fix:** Set the environment variable or edit `Directory.Build.props`

### "Could not find BepInEx.dll"
**Fix:** Install BepInEx 5.x in Valheim first

### "Could not find Jotunn.dll"
**Fix:** Install Jotunn from Thunderstore

### "publicized_assemblies not found"
**Fix:** Use BepInEx.AssemblyPublicizer or download pre-publicized assemblies

## 📤 Upload to GitHub

### Using Git Command Line

```bash
cd C:\Valheim Modding\UOV
git init
git add .
git commit -m "Initial commit - Core + Sidecar architecture"
git branch -M main
git remote add origin https://github.com/andrewhix/UOV.git
git push -u origin main
```

### Using GitHub Desktop

1. Open GitHub Desktop
2. File → Add Local Repository
3. Choose `C:\Valheim Modding\UOV`
4. Publish repository
5. Push to GitHub

### Using Visual Studio

1. In Solution Explorer, right-click solution
2. Select "Add Solution to Source Control"
3. Choose Git
4. Connect to GitHub
5. Publish

## 📚 Documentation Hierarchy

Start here based on your goal:

**I want to USE the mod:**
→ README.md → Build and install

**I want to CREATE a module:**
→ docs/GETTING_STARTED.md → Complete tutorial

**I need API reference:**
→ docs/QUICK_REFERENCE.md → Code examples

**I want to understand internals:**
→ UltimaValheimCore/README.md → Architecture details

## 🎮 Creating Your First Module

Quick template:

```csharp
using System;
using UltimaValheim.Core;

namespace UltimaValheim.MyMod
{
    public class MyModule : ICoreModule
    {
        public string ModuleID => "UltimaValheim.MyMod";
        public Version ModuleVersion => new Version(1, 0, 0);

        public void OnCoreReady()
        {
            CoreAPI.Log.LogInfo($"[{ModuleID}] Hello from my mod!");
        }

        public void OnPlayerJoin(Player player) { }
        public void OnPlayerLeave(Player player) { }
        public void OnSave() { }
        public void OnShutdown() { }
    }
}
```

Add to solution, build, copy DLL → Done!

## ✨ Key Features

- 🔌 **Hot-swappable modules** - Independent loading
- 🌐 **Multiplayer-safe** - Built-in network sync
- 💾 **Auto-persistence** - Save/load handled for you
- 📡 **Event-driven** - Loose coupling via EventBus
- ⚙️ **Configurable** - BepInEx config integration
- 🛡️ **Error isolation** - One crash won't affect others

## 🤝 Community

- **GitHub**: https://github.com/andrewhix/UOV
- **Issues**: Report bugs and request features
- **Discussions**: Ask questions and share ideas
- **Pull Requests**: Contribute code

## 📄 License

MIT License - Free to use, modify, and distribute

## 🙏 Credits

- **Valheim**: Iron Gate Studio
- **BepInEx**: BepInEx team
- **Jotunn**: Valheim Modding community
- **Ultima Online**: Origin Systems (inspiration)

---

## 🎉 You're Ready!

Everything is set up and ready to go:

✅ Complete Visual Studio solution  
✅ All Core systems implemented  
✅ Example module included  
✅ Comprehensive documentation  
✅ Build scripts and tools  
✅ GitHub-ready structure  

**Just open `UOV.sln` in Visual Studio and start building!**

Happy modding! ⚔️🛡️
