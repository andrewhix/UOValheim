#!/bin/bash
# UOV Build and Deploy Script
# Run this to build and copy DLLs to Valheim

echo "========================================"
echo "  UOV Build and Deploy"
echo "========================================"
echo ""

# Check if VALHEIM_INSTALL is set
if [ -z "$VALHEIM_INSTALL" ]; then
    echo "ERROR: VALHEIM_INSTALL environment variable not set!"
    echo ""
    echo "Please set it to your Valheim installation path:"
    echo "  export VALHEIM_INSTALL=\"/path/to/valheim\""
    echo ""
    echo "Add to ~/.bashrc or ~/.zshrc for persistence."
    exit 1
fi

echo "Valheim Path: $VALHEIM_INSTALL"
echo ""

# Check if Valheim directory exists
if [ ! -d "$VALHEIM_INSTALL" ]; then
    echo "ERROR: Valheim directory not found at:"
    echo "  $VALHEIM_INSTALL"
    echo ""
    echo "Please update VALHEIM_INSTALL environment variable."
    exit 1
fi

echo "Building UOV..."
dotnet build -c Release

if [ $? -ne 0 ]; then
    echo ""
    echo "ERROR: Build failed!"
    exit 1
fi

echo ""
echo "Build successful!"
echo ""

echo "Deploying to Valheim..."

# Create plugins directory if it doesn't exist
mkdir -p "$VALHEIM_INSTALL/BepInEx/plugins"

# Copy Core DLL
echo "Copying UltimaValheimCore.dll..."
cp -f "bin/Release/net48/UltimaValheimCore.dll" "$VALHEIM_INSTALL/BepInEx/plugins/"

if [ $? -ne 0 ]; then
    echo "ERROR: Failed to copy Core DLL!"
    exit 1
fi

# Copy Example DLL
echo "Copying ExampleSidecar.dll..."
cp -f "bin/Release/net48/ExampleSidecar.dll" "$VALHEIM_INSTALL/BepInEx/plugins/"

if [ $? -ne 0 ]; then
    echo "ERROR: Failed to copy Example DLL!"
    exit 1
fi

echo ""
echo "========================================"
echo "  Deployment Complete!"
echo "========================================"
echo ""
echo "DLLs copied to: $VALHEIM_INSTALL/BepInEx/plugins/"
echo ""
echo "You can now launch Valheim."
echo "Check BepInEx/LogOutput.log for loading confirmation."
echo ""
