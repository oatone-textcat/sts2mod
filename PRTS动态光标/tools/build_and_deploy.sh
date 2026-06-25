#!/bin/zsh
set -euo pipefail

ROOT="/Users/iniad/sts2-mods/PRTSCursor"
FILE_STEM="PRTSCursor"
MANIFEST_SRC="$ROOT/assets/$FILE_STEM.json"
GAME_APP="/Users/iniad/Library/Application Support/Steam/steamapps/common/Slay the Spire 2/SlayTheSpire2.app"
GAME_BIN="$GAME_APP/Contents/MacOS/Slay the Spire 2"
MOD_DIR="$GAME_APP/Contents/MacOS/mods/$FILE_STEM"
BUILD_OUT="$ROOT/src/bin/Release/net9.0"
PROJECT_PATH="$ROOT/src/$FILE_STEM.csproj"
IMPORT_PROJECT="$ROOT/.build/import_project"
DEFAULT_GODOT_EDITOR="/Users/iniad/sts2-mods/.tools/godot-4.5.1/Godot_mono.app/Contents/MacOS/Godot"

if (( ${+commands[dotnet]} )); then
  DOTNET_BIN="${commands[dotnet]}"
elif [[ -x "/opt/homebrew/bin/dotnet" ]]; then
  DOTNET_BIN="/opt/homebrew/bin/dotnet"
else
  print -u2 "Could not find a usable dotnet executable. Install .NET 9 SDK or add dotnet to PATH."
  exit 1
fi

DOTNET_ROOT="$(cd "$(dirname "$DOTNET_BIN")" && pwd)"

if [[ -z "${GODOT_EDITOR:-}" && -x "$DEFAULT_GODOT_EDITOR" ]]; then
  GODOT_EDITOR="$DEFAULT_GODOT_EDITOR"
elif [[ -n "${GODOT_EDITOR:-}" ]]; then
  GODOT_EDITOR="$GODOT_EDITOR"
elif [[ -x "/opt/homebrew/bin/godot" ]]; then
  GODOT_EDITOR="/opt/homebrew/bin/godot"
elif (( ${+commands[godot]} )); then
  GODOT_EDITOR="${commands[godot]}"
else
  print -u2 "Could not find a Godot editor executable for asset import."
  exit 1
fi

if [[ ! -x "$GAME_BIN" ]]; then
  print -u2 "Game binary not found: $GAME_BIN"
  exit 1
fi

clean_macos_metadata() {
  local target="$1"
  [[ -d "$target" ]] || return 0

  find "$target" -name "__MACOSX" -type d -prune -exec rm -rf {} +
  find "$target" -name ".DS_Store" -type f -delete
  find "$target" -name "._*" -type f -delete
}

GAME_GODOT_VERSION="$("$GAME_BIN" --version 2>/dev/null | head -n 1 || true)"
IMPORT_GODOT_VERSION="$("$GODOT_EDITOR" --version 2>/dev/null | head -n 1 || true)"
print "Game Godot: ${GAME_GODOT_VERSION:-unknown}"
print "Import Godot: ${IMPORT_GODOT_VERSION:-unknown}"
if [[ -n "$GAME_GODOT_VERSION" && -n "$IMPORT_GODOT_VERSION" ]]; then
  if [[ "${GAME_GODOT_VERSION%%.*}.${${GAME_GODOT_VERSION#*.}%%.*}" != "${IMPORT_GODOT_VERSION%%.*}.${${IMPORT_GODOT_VERSION#*.}%%.*}" ]]; then
    print -u2 "Warning: game and import Godot major/minor versions differ."
  fi
fi

rm -rf "$ROOT/src/bin" "$ROOT/src/obj" "$ROOT/dist" "$ROOT/.build"

DOTNET_ROOT="$DOTNET_ROOT" "$DOTNET_BIN" build "$PROJECT_PATH" -c Release

MAIN_DLL="$BUILD_OUT/$FILE_STEM.dll"
if [[ ! -f "$MAIN_DLL" ]]; then
  print -u2 "Missing main mod DLL after build: $MAIN_DLL"
  exit 1
fi

EXTRA_DLLS=()
for dll in "$BUILD_OUT"/*.dll(N); do
  base_name="$(basename "$dll")"
  if [[ "$base_name" != "$FILE_STEM.dll" ]]; then
    EXTRA_DLLS+=("$base_name")
  fi
done

if (( ${#EXTRA_DLLS[@]} > 0 )); then
  print -u2 "Unexpected dependency DLLs in build output: ${EXTRA_DLLS[*]}"
  print -u2 "$FILE_STEM must ship as a standard single-DLL mod; mark references Private=false or remove package dependencies."
  exit 1
fi

mkdir -p "$ROOT/dist" "$IMPORT_PROJECT/$FILE_STEM"
rm -rf "$MOD_DIR"

cp "$ROOT/tools/project.godot" "$IMPORT_PROJECT/project.godot"
rsync -a --exclude "$FILE_STEM.json" "$ROOT/assets/" "$IMPORT_PROJECT/$FILE_STEM/"

"$GODOT_EDITOR" --headless --path "$IMPORT_PROJECT" --import

cp "$MANIFEST_SRC" "$ROOT/dist/$FILE_STEM.json"

"$GAME_BIN" --headless \
  --path "$ROOT/tools" \
  -s res://pack_mod.gd -- \
  "$MANIFEST_SRC" \
  "$ROOT/dist/$FILE_STEM.pck" \
  "$IMPORT_PROJECT"

cp "$MAIN_DLL" "$ROOT/dist/$FILE_STEM.dll"
clean_macos_metadata "$ROOT/dist"

mkdir -p "$MOD_DIR"
cp "$ROOT/dist/$FILE_STEM.json" "$MOD_DIR/$FILE_STEM.json"
cp "$ROOT/dist/$FILE_STEM.pck" "$MOD_DIR/$FILE_STEM.pck"
cp "$ROOT/dist/$FILE_STEM.dll" "$MOD_DIR/$FILE_STEM.dll"
clean_macos_metadata "$MOD_DIR"

echo "Deployed to $MOD_DIR"
