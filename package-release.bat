@echo off
REM Package UOV for release
REM This script creates a zip file ready for distribution

echo ========================================
echo UOV Release Packaging Script
echo ========================================
echo.

REM Check if Builds folder exists
if not exist "Builds\" (
    echo ERROR: Builds folder not found!
    echo Please build the solution first.
    pause
    exit /b 1
)

REM Create release folder
set RELEASE_DIR=Release
if exist "%RELEASE_DIR%" rd /s /q "%RELEASE_DIR%"
mkdir "%RELEASE_DIR%"
mkdir "%RELEASE_DIR%\BepInEx"
mkdir "%RELEASE_DIR%\BepInEx\plugins"

REM Copy DLLs
echo Copying DLLs...
copy "Builds\UltimaValheimCore.dll" "%RELEASE_DIR%\BepInEx\plugins\" >nul
copy "Builds\ExampleSidecar.dll" "%RELEASE_DIR%\BepInEx\plugins\" >nul

REM Copy documentation
echo Copying documentation...
copy "README.md" "%RELEASE_DIR%\" >nul
copy "LICENSE" "%RELEASE_DIR%\" >nul
mkdir "%RELEASE_DIR%\docs"
copy "docs\GETTING_STARTED.md" "%RELEASE_DIR%\docs\" >nul
copy "docs\QUICK_REFERENCE.md" "%RELEASE_DIR%\docs\" >nul

REM Create installation instructions
echo Creating install instructions...
(
echo # Installation Instructions
echo.
echo 1. Install BepInEx 5.x for Valheim
echo 2. Install Jotunn Mod Library
echo 3. Copy the contents of this folder to your Valheim installation directory
echo 4. Launch Valheim
echo.
echo ## Folder Structure
echo ```
echo Valheim/
echo └── BepInEx/
echo     └── plugins/
echo         ├── UltimaValheimCore.dll
echo         └── ExampleSidecar.dll  (optional)
echo ```
echo.
echo ## Verifying Installation
echo Check BepInEx/LogOutput.log for:
echo - "Ultima Valheim Core v1.0.0 loaded!"
echo - "Registered module: UltimaValheim.Example"
echo.
echo ## Support
echo - GitHub: https://github.com/andrewhix/UOV
echo - Issues: https://github.com/andrewhix/UOV/issues
) > "%RELEASE_DIR%\INSTALL.md"

REM Create version file
echo Creating version file...
(
echo UOV - Ultima Valheim
echo Version: 1.0.0
echo Build Date: %date% %time%
echo.
echo Components:
echo - UltimaValheimCore.dll
echo - ExampleSidecar.dll
) > "%RELEASE_DIR%\VERSION.txt"

REM Create zip file (requires PowerShell)
echo Creating zip file...
set ZIP_NAME=UOV-v1.0.0.zip
if exist "%ZIP_NAME%" del "%ZIP_NAME%"

powershell -Command "Compress-Archive -Path '%RELEASE_DIR%\*' -DestinationPath '%ZIP_NAME%'"

if exist "%ZIP_NAME%" (
    echo.
    echo ========================================
    echo SUCCESS!
    echo ========================================
    echo Release package created: %ZIP_NAME%
    echo.
    echo Contents:
    dir "%RELEASE_DIR%" /b /s
    echo.
    echo Ready for distribution!
) else (
    echo.
    echo ERROR: Failed to create zip file
    echo Make sure PowerShell is available
)

echo.
pause
