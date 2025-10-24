@echo off
REM UOV Build and Deploy Script
REM Run this to build and copy DLLs to Valheim

echo ========================================
echo   UOV Build and Deploy
echo ========================================
echo.

REM Check if VALHEIM_INSTALL is set
if "%VALHEIM_INSTALL%"=="" (
    echo ERROR: VALHEIM_INSTALL environment variable not set!
    echo.
    echo Please set it to your Valheim installation path:
    echo   setx VALHEIM_INSTALL "C:\Program Files (x86)\Steam\steamapps\common\Valheim"
    echo.
    echo Then restart this script.
    pause
    exit /b 1
)

echo Valheim Path: %VALHEIM_INSTALL%
echo.

REM Check if Valheim directory exists
if not exist "%VALHEIM_INSTALL%" (
    echo ERROR: Valheim directory not found at:
    echo   %VALHEIM_INSTALL%
    echo.
    echo Please update VALHEIM_INSTALL environment variable.
    pause
    exit /b 1
)

echo Building UOV...
dotnet build -c Release

if %ERRORLEVEL% NEQ 0 (
    echo.
    echo ERROR: Build failed!
    pause
    exit /b 1
)

echo.
echo Build successful!
echo.

echo Deploying to Valheim...

REM Create plugins directory if it doesn't exist
if not exist "%VALHEIM_INSTALL%\BepInEx\plugins" (
    echo Creating plugins directory...
    mkdir "%VALHEIM_INSTALL%\BepInEx\plugins"
)

REM Copy Core DLL
echo Copying UltimaValheimCore.dll...
copy /Y "bin\Release\net48\UltimaValheimCore.dll" "%VALHEIM_INSTALL%\BepInEx\plugins\"

if %ERRORLEVEL% NEQ 0 (
    echo ERROR: Failed to copy Core DLL!
    pause
    exit /b 1
)

REM Copy Example DLL
echo Copying ExampleSidecar.dll...
copy /Y "bin\Release\net48\ExampleSidecar.dll" "%VALHEIM_INSTALL%\BepInEx\plugins\"

if %ERRORLEVEL% NEQ 0 (
    echo ERROR: Failed to copy Example DLL!
    pause
    exit /b 1
)

echo.
echo ========================================
echo   Deployment Complete!
echo ========================================
echo.
echo DLLs copied to: %VALHEIM_INSTALL%\BepInEx\plugins\
echo.
echo You can now launch Valheim.
echo Check BepInEx\LogOutput.log for loading confirmation.
echo.

pause
