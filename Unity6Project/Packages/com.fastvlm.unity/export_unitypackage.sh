#!/bin/bash

set -e

echo "📦 FastVLM Unity .unitypackage Export"
echo "===================================="

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PACKAGE_JSON="$SCRIPT_DIR/package.json"

if [[ ! -f "$PACKAGE_JSON" ]]; then
  echo "❌ package.json not found beside this script. Aborting." >&2
  exit 1
fi

# Extract version from package.json
VERSION=$(grep '"version"' "$PACKAGE_JSON" | head -1 | sed -E 's/.*"version" *: *"([^"]+)".*/\1/')
[[ -z "$VERSION" ]] && VERSION="0.0.0"

UNITY_PROJECT_PATH=""
UNITY_BINARY=""
DEST_DIR="$SCRIPT_DIR/Packages"
ASSET_DEST_REL="Assets/FastVLM"    # Where plugin will be copied inside the temp / target project
FORCE_OVERWRITE=false
DRY_RUN=false

usage() {
  cat <<EOF
Usage: $0 --unity-project /path/to/UnityProject [options]

Required:
  --unity-project PATH   Existing Unity project path (must contain Assets/)

Options:
  --unity-binary PATH    Explicit path to Unity executable (batchmode capable)
  --dest DIR             Output directory (default: UnityPlugin/Packages)
  --force                Overwrite existing Assets/FastVLM in project
  --dry-run              Show actions without performing export
  -h, --help             Show this help

Example:
  $0 --unity-project ~/Projects/MyGame

If no --unity-binary given, script attempts auto-detection under:
  /Applications/Unity/Hub/Editor/*/Unity.app/Contents/MacOS/Unity
EOF
}

while [[ $# -gt 0 ]]; do
  case "$1" in
    --unity-project) UNITY_PROJECT_PATH="$2"; shift 2;;
    --unity-binary) UNITY_BINARY="$2"; shift 2;;
    --dest) DEST_DIR="$2"; shift 2;;
    --force) FORCE_OVERWRITE=true; shift;;
    --dry-run) DRY_RUN=true; shift;;
    -h|--help) usage; exit 0;;
    *) echo "Unknown option: $1"; usage; exit 1;;
  esac
done

if [[ -z "$UNITY_PROJECT_PATH" ]]; then
  echo "❌ --unity-project is required" >&2
  usage
  exit 1
fi

if [[ ! -d "$UNITY_PROJECT_PATH/Assets" ]]; then
  echo "❌ $UNITY_PROJECT_PATH does not appear to be a Unity project (Assets/ missing)" >&2
  exit 1
fi

if [[ -z "$UNITY_BINARY" ]]; then
  # Pick latest installed editor
  UNITY_BINARY=$(ls -1d /Applications/Unity/Hub/Editor/*/Unity.app/Contents/MacOS/Unity 2>/dev/null | sort -V | tail -1 || true)
fi

if [[ ! -x "$UNITY_BINARY" ]]; then
  echo "⚠️  Could not auto-detect Unity binary. You can still package manually via Unity GUI." >&2
fi

echo "• Version:          $VERSION"
echo "• Unity Project:    $UNITY_PROJECT_PATH"
echo "• Unity Binary:     ${UNITY_BINARY:-'(not found)'}"
echo "• Output Dir:       $DEST_DIR"
echo "• Force Overwrite:  $FORCE_OVERWRITE"
echo "• Dry Run:          $DRY_RUN"

PLUGIN_TARGET="$UNITY_PROJECT_PATH/$ASSET_DEST_REL"

copy_plugin() {
  if [[ -d "$PLUGIN_TARGET" && "$FORCE_OVERWRITE" != true ]]; then
    echo "⚠️  $ASSET_DEST_REL already exists. Use --force to overwrite." >&2
    return 0
  fi
  echo "🔨 Copying plugin into project ($ASSET_DEST_REL)"
  rm -rf "$PLUGIN_TARGET"
  mkdir -p "$(dirname "$PLUGIN_TARGET")"
  # Copy only relevant folders
  cp -R "$SCRIPT_DIR/Runtime" "$PLUGIN_TARGET/Runtime"
  cp -R "$SCRIPT_DIR/Editor" "$PLUGIN_TARGET/Editor"
  [[ -d "$SCRIPT_DIR/Samples~" ]] && cp -R "$SCRIPT_DIR/Samples~" "$PLUGIN_TARGET/Samples~"
  # Docs / scripts
  for f in README.md IOS_SETUP_GUIDE.md CHANGELOG.md QUICKSTART.md LICENSE; do
    [[ -f "$SCRIPT_DIR/$f" ]] && cp "$SCRIPT_DIR/$f" "$PLUGIN_TARGET/$f"
  done
  # asmdef & package info if present
  if [[ -f "$SCRIPT_DIR/package.json" ]]; then
    cp "$SCRIPT_DIR/package.json" "$PLUGIN_TARGET/package.json"
  fi
}

export_unitypackage() {
  mkdir -p "$DEST_DIR"
  PKG_NAME="FastVLM-Unity-Plugin-${VERSION}.unitypackage"
  echo "📦 Exporting .unitypackage -> $DEST_DIR/$PKG_NAME"
  "$UNITY_BINARY" -batchmode -nographics -quit \
    -projectPath "$UNITY_PROJECT_PATH" \
    -exportPackage "$ASSET_DEST_REL" "$DEST_DIR/$PKG_NAME" \
    -logFile "$DEST_DIR/unitypackage-export.log" || {
      echo "❌ Unity export failed (see log)." >&2; return 1; }
  echo "✅ Created: $DEST_DIR/$PKG_NAME"
}

if [[ "$DRY_RUN" == true ]]; then
  echo "(Dry run) Would copy plugin to: $PLUGIN_TARGET"
  echo "(Dry run) Would export unitypackage to: $DEST_DIR"
  exit 0
fi

copy_plugin

if [[ -x "$UNITY_BINARY" ]]; then
  export_unitypackage || exit 1
else
  echo "⚠️  Skipping automated export (Unity not found). You can now open the project and use:"
  echo "    Assets > Export Package... (select FastVLM folder)"
fi

echo "✨ Done"