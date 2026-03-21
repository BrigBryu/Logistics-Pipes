#!/bin/bash
# Deploy TransportMod to game Mods folder
GAME_MODS="$HOME/Library/Application Support/Steam/steamapps/common/Stardew Valley/Contents/MacOS/Mods"
MOD_DIR="$GAME_MODS/TransportMod"

mkdir -p "$MOD_DIR"
cp bin/Debug/net6.0/TransportMod.dll "$MOD_DIR/"
cp manifest.json "$MOD_DIR/"
cp -r assets "$MOD_DIR/"
echo "Deployed to $MOD_DIR"
