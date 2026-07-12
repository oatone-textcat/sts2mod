#!/bin/zsh
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
FILE_STEM="IntegratedStrategyEvents"
MANIFEST_SRC="$ROOT/assets/$FILE_STEM.json"
GAME_APP="${STS2_GAME_APP:-/Users/iniad/Library/Application Support/Steam/steamapps/common/Slay the Spire 2/SlayTheSpire2.app}"
GAME_BIN="$GAME_APP/Contents/MacOS/Slay the Spire 2"
MOD_DIR="$GAME_APP/Contents/MacOS/mods/$FILE_STEM"
RITSULIB_MOD_DIR="$GAME_APP/Contents/MacOS/mods/STS2-RitsuLib"
RITSULIB_WORKSHOP_DIR="${RITSULIB_WORKSHOP_DIR:-/Users/iniad/Library/Application Support/Steam/steamapps/workshop/content/2868840/3747602295}"
RITSULIB_REQUIRED_VERSION="${RITSULIB_REQUIRED_VERSION:-0.4.53}"
BUILD_OUT="$ROOT/src/bin/Release/net9.0"
PROJECT_PATH="$ROOT/src/$FILE_STEM.csproj"
IMPORT_PROJECT="$ROOT/.build/import_project"
ROOT_COMPAT_SRC="$ROOT/assets_root_compat"
DEFAULT_GODOT_EDITOR="$ROOT/../.tools/godot-4.5.1/Godot_mono.app/Contents/MacOS/Godot"
if [[ -z "${GODOT_EDITOR:-}" && -x "$DEFAULT_GODOT_EDITOR" ]]; then
  GODOT_EDITOR="$DEFAULT_GODOT_EDITOR"
else
  GODOT_EDITOR="${GODOT_EDITOR:-/opt/homebrew/bin/godot}"
fi

if (( ${+commands[dotnet]} )); then
  DOTNET_BIN="${commands[dotnet]}"
elif [[ -x "/opt/homebrew/Cellar/dotnet/9.0.8/libexec/dotnet" ]]; then
  DOTNET_BIN="/opt/homebrew/Cellar/dotnet/9.0.8/libexec/dotnet"
else
  print -u2 "Could not find a usable dotnet executable. Install .NET 9 SDK or add dotnet to PATH."
  exit 1
fi

DOTNET_ROOT="$(cd "$(dirname "$DOTNET_BIN")" && pwd)"

clean_macos_metadata() {
  local target="$1"
  [[ -d "$target" ]] || return 0

  find "$target" -name "__MACOSX" -type d -prune -exec rm -rf {} +
  find "$target" -name ".DS_Store" -type f -delete
  find "$target" -name "._*" -type f -delete
}

ritsulib_has_required_version() {
  local manifest="$1"
  [[ -f "$manifest" ]] || return 1
  grep -q "\"version\"[[:space:]]*:[[:space:]]*\"$RITSULIB_REQUIRED_VERSION\"" "$manifest"
}

major_minor_version() {
  sed -E 's/^([0-9]+[.][0-9]+).*/\1/' <<< "$1"
}

ensure_ritsulib() {
  if ritsulib_has_required_version "$RITSULIB_MOD_DIR/mod_manifest.json"; then
    return 0
  fi

  if [[ -d "$RITSULIB_WORKSHOP_DIR" ]] && ritsulib_has_required_version "$RITSULIB_WORKSHOP_DIR/mod_manifest.json"; then
    rm -rf "$RITSULIB_MOD_DIR"
    mkdir -p "$RITSULIB_MOD_DIR"
    rsync -a "$RITSULIB_WORKSHOP_DIR/" "$RITSULIB_MOD_DIR/"
    clean_macos_metadata "$RITSULIB_MOD_DIR"
    return 0
  fi

  print -u2 "RitsuLib $RITSULIB_REQUIRED_VERSION is required. Subscribe to Workshop item 3747602295 or set RITSULIB_WORKSHOP_DIR."
  exit 1
}

if [[ ! -x "$GODOT_EDITOR" ]]; then
  print -u2 "Could not find a usable Godot editor executable at $GODOT_EDITOR."
  exit 1
fi

GAME_GODOT_VERSION="$("$GAME_BIN" --version 2>/dev/null | head -n 1)"
IMPORT_GODOT_VERSION="$("$GODOT_EDITOR" --version 2>/dev/null | head -n 1)"
if [[ -n "$GAME_GODOT_VERSION" && -n "$IMPORT_GODOT_VERSION" \
  && "$(major_minor_version "$GAME_GODOT_VERSION")" != "$(major_minor_version "$IMPORT_GODOT_VERSION")" ]]; then
  print -u2 "Warning: asset import Godot version ($IMPORT_GODOT_VERSION) differs from game runtime ($GAME_GODOT_VERSION)."
  print -u2 "Set GODOT_EDITOR to a matching 4.5.x editor if texture compatibility regresses."
fi

rm -rf "$ROOT/src/bin" "$ROOT/src/obj" "$ROOT/dist" "$ROOT/.build"

ensure_ritsulib

DOTNET_ROOT="$DOTNET_ROOT" "$DOTNET_BIN" build "$PROJECT_PATH" -c Release

mkdir -p "$ROOT/dist"
rm -rf "$MOD_DIR"

mkdir -p "$IMPORT_PROJECT/$FILE_STEM"
cp "$ROOT/tools/project.godot" "$IMPORT_PROJECT/project.godot"
rsync -a --exclude "$FILE_STEM.json" "$ROOT/assets/" "$IMPORT_PROJECT/$FILE_STEM/"
if [[ -d "$ROOT_COMPAT_SRC" ]]; then
  rsync -a "$ROOT_COMPAT_SRC/" "$IMPORT_PROJECT/_root_compat/"
fi
clean_macos_metadata "$IMPORT_PROJECT"

"$GODOT_EDITOR" --headless \
  --path "$IMPORT_PROJECT" \
  --import

cp "$MANIFEST_SRC" "$ROOT/dist/$FILE_STEM.json"

"$GAME_BIN" --headless \
  --path "$ROOT/tools" \
  -s res://pack_mod.gd -- \
  "$MANIFEST_SRC" \
  "$ROOT/dist/$FILE_STEM.pck" \
  "$IMPORT_PROJECT"

for dll in "$BUILD_OUT"/*.dll; do
  base_name="$(basename "$dll")"
  case "$base_name" in
    sts2.dll|GodotSharp.dll|STS2-RitsuLib.dll|0Harmony.dll)
      continue
      ;;
  esac

  cp "$dll" "$ROOT/dist/$base_name"
done
clean_macos_metadata "$ROOT/dist"

mkdir -p "$MOD_DIR"
cp "$ROOT/dist/$FILE_STEM.json" "$MOD_DIR/$FILE_STEM.json"
cp "$ROOT/dist/$FILE_STEM.pck" "$MOD_DIR/$FILE_STEM.pck"

for dll in "$ROOT/dist"/*.dll; do
  cp "$dll" "$MOD_DIR/$(basename "$dll")"
done
clean_macos_metadata "$MOD_DIR"

if [[ "${KEEP_BUILD_ARTIFACTS:-0}" != "1" ]]; then
  rm -rf "$ROOT/.build"
fi

echo "Deployed to $MOD_DIR"
