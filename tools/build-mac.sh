#!/usr/bin/env bash
#
# One-line macOS standalone build for local play / level0 debugging.
#
#   tools/build-mac.sh
#
# Output lands outside the iCloud-synced repo (same reason as tools/build-ios.sh).

set -euo pipefail
cd "$(dirname "$0")/.."

if [ -z "${UNITY_BIN:-}" ]; then
  UNITY_BIN="$(find /Applications/Unity/Hub/Editor -maxdepth 3 -name Unity -type f -path '*MacOS*' 2>/dev/null | sort -V | tail -1 || true)"
fi

if [ ! -x "${UNITY_BIN:-}" ]; then
  # Fallback: project is pinned to 6000.0.81f1
  UNITY_BIN="/Applications/Unity/Hub/Editor/6000.0.81f1/Unity.app/Contents/MacOS/Unity"
fi

if [ ! -x "$UNITY_BIN" ]; then
  echo "Could not find Unity. Set UNITY_BIN." >&2
  exit 1
fi

BUILD_ROOT="${HV_BUILD_ROOT:-$HOME/Library/Caches/HiddenValleyBuild}"
export HV_MAC_BUILD_DIR="${HV_MAC_BUILD_DIR:-$BUILD_ROOT/mac}"
mkdir -p "$HV_MAC_BUILD_DIR"

echo "Unity: $UNITY_BIN"
echo "Out:   $HV_MAC_BUILD_DIR"
echo

# The layout lives in StreamingAssets and nowhere else — the duplicate under
# Assets/HiddenValley/Layout was deleted when VillageSetup and LayoutSpawner were
# pointed at the same file. Fail loudly if it goes missing rather than building a
# village with nothing in it.
if [ ! -f Assets/StreamingAssets/Layout/heartwood.json ]; then
  echo "Missing Assets/StreamingAssets/Layout/heartwood.json — nothing to spawn." >&2
  exit 1
fi

"$UNITY_BIN" \
  -quit -batchmode -nographics \
  -projectPath "$PWD" \
  -executeMethod HiddenValley.Editor.VillageSetup.GenerateHeartwood \
  -logFile "$BUILD_ROOT/mac-generate.log"

"$UNITY_BIN" \
  -quit -batchmode -nographics \
  -projectPath "$PWD" \
  -executeMethod HiddenValley.Editor.BuildCommand.macOS \
  -logFile "$BUILD_ROOT/mac-build.log"

APP="$HV_MAC_BUILD_DIR/HiddenValley.app"
if [ ! -d "$APP" ]; then
  echo "No app produced. See $BUILD_ROOT/mac-build.log" >&2
  exit 1
fi

echo
echo "Built: $APP"
echo "Run:   open \"$APP\""
echo "Log:   ~/Library/Logs/APG Media/Hidden Valley/Player.log"
echo "Boot:  ~/Library/Application Support/APG Media/Hidden Valley/boot-marker.txt"
