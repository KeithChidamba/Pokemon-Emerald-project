#!/bin/bash
# Path to your Unity executable (adjust this to your system)
UNITY_PATH="$HOME/Unity/Hub/Editor/2022.3.61f1/Editor/Unity"

# Project root folder
PROJECT_PATH="$HOME/Pokemon Luminary"

# Build target: WebGL, StandaloneWindows64, Android, etc.
BUILD_TARGET="WebGL"

# Method to execute (your custom build method)
BUILD_METHOD="WebGLBuilder.BuildGame"  # Use namespace if needed

# Output build folder
BUILD_PATH="$PROJECT_PATH/Build/"

rm -rf "$BUILD_PATH"/*

find "$BUILD_PATH" -maxdepth 1 -type f -name ".*" -exec rm -f {} +

# Run Unity in batch mode to build
"$UNITY_PATH" -batchmode -nographics -quit \
  -projectPath "$PROJECT_PATH" \
  -buildTarget "$BUILD_TARGET" \
  -executeMethod "$BUILD_METHOD" \
  -logFile "$PROJECT_PATH/build.log"

# Check if build was successful
if [ $? -eq 0 ]; then
  echo "✅ Build succeeded!"
else
  echo "❌ Build failed. Check build.log"
fi

