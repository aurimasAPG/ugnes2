#!/usr/bin/env bash
#
# The one-line build for the Phase 0 gate.
#
#   tools/build-ios.sh
#
# The gate is: "does a build reach the device in under 10 minutes, from a one-line
# command?" This script is the one line. It times itself and tells you whether the gate
# passed, because a gate you have to time by hand is a gate that quietly stops being
# measured around week three.
#
# Requires macOS with Unity and Xcode. It cannot run in the agent container -- see
# blocker B1 in docs/phase-log.md.
#
# Environment:
#   UNITY_BIN   path to the Unity executable (else the newest under /Applications/Unity/Hub)
#   TEAM_ID     Apple Developer team id, for the Xcode step
#   DEVICE      optional; ios-deploy target udid

set -euo pipefail

cd "$(dirname "$0")/.."

START=$(date +%s)

if [ -z "${UNITY_BIN:-}" ]; then
  UNITY_BIN="$(find /Applications/Unity/Hub/Editor -maxdepth 3 -name Unity -type f -path '*MacOS*' 2>/dev/null | sort -V | tail -1 || true)"
fi

if [ ! -x "${UNITY_BIN:-}" ]; then
  echo "Could not find Unity. Set UNITY_BIN to the editor binary." >&2
  exit 1
fi

echo "Unity: $UNITY_BIN"
echo

# Build OUTSIDE the repo. The repo lives under an iCloud-synced path on the owner's
# Mac, and the file provider re-tags outputs with Finder metadata faster than it can
# be stripped — codesign then rejects them ("resource fork ... detritus not allowed").
# ~/Library/Caches is never synced.
BUILD_ROOT="${HV_BUILD_ROOT:-$HOME/Library/Caches/HiddenValleyBuild}"
export HV_BUILD_DIR="$BUILD_ROOT/iOS"
mkdir -p "$BUILD_ROOT"
echo "Build root: $BUILD_ROOT"
echo

echo "== 1/3  Unity -> Xcode project =="
"$UNITY_BIN" \
  -quit -batchmode -nographics \
  -projectPath "$PWD" \
  -executeMethod HiddenValley.Editor.BuildCommand.iOS \
  -logFile - | tail -40

echo
echo "== 2/3  Xcode -> .app =="

# Belt and braces for anything that was tagged before the move out of the repo.
xattr -rc "$BUILD_ROOT" 2>/dev/null || true

# A concrete device destination lets -allowProvisioningDeviceRegistration register
# the phone with the team; the generic destination cannot.
DEST="generic/platform=iOS"
[ -n "${DEVICE:-}" ] && DEST="platform=iOS,id=$DEVICE"

xcodebuild \
  -project "$HV_BUILD_DIR/Unity-iPhone.xcodeproj" \
  -scheme Unity-iPhone \
  -configuration Release \
  -destination "$DEST" \
  -derivedDataPath "$BUILD_ROOT/DerivedData" \
  ${TEAM_ID:+DEVELOPMENT_TEAM="$TEAM_ID"} \
  -allowProvisioningUpdates \
  -allowProvisioningDeviceRegistration \
  build | tail -20

echo
echo "== 3/3  install =="
APP="$(find "$BUILD_ROOT/DerivedData/Build/Products" -maxdepth 2 -name '*.app' | head -1)"
if [ -z "$APP" ]; then
  echo "No .app produced." >&2
  exit 1
fi

if command -v ios-deploy >/dev/null 2>&1; then
  ios-deploy --bundle "$APP" ${DEVICE:+--id "$DEVICE"} --no-wifi
else
  echo "ios-deploy not installed; built at: $APP"
  echo "  brew install ios-deploy"
fi

# -- optional TestFlight lane -------------------------------------------------
# HV_UPLOAD=1 archives and uploads instead of installing locally. Requires the paid
# Apple Developer Program plus App Store Connect API auth in the environment:
#   HV_ASC_KEY_ID, HV_ASC_ISSUER_ID and the .p8 at ~/.appstoreconnect/private_keys/.
if [ "${HV_UPLOAD:-0}" = "1" ]; then
  echo
  echo "== TestFlight upload =="
  xcodebuild -project "$HV_BUILD_DIR/Unity-iPhone.xcodeproj" -scheme Unity-iPhone \
    -configuration Release -destination 'generic/platform=iOS' \
    -archivePath "$BUILD_ROOT/HiddenValley.xcarchive" \
    ${TEAM_ID:+DEVELOPMENT_TEAM="$TEAM_ID"} -allowProvisioningUpdates archive | tail -3
  xcodebuild -exportArchive -archivePath "$BUILD_ROOT/HiddenValley.xcarchive" \
    -exportOptionsPlist "$(dirname "$0")/exportOptions.plist" \
    -exportPath "$BUILD_ROOT/export" -allowProvisioningUpdates | tail -3
  xcrun altool --upload-app -f "$BUILD_ROOT/export/"*.ipa -t ios \
    --apiKey "${HV_ASC_KEY_ID:?set HV_ASC_KEY_ID}" \
    --apiIssuer "${HV_ASC_ISSUER_ID:?set HV_ASC_ISSUER_ID}"
fi

ELAPSED=$(( $(date +%s) - START ))
echo
echo "Total: ${ELAPSED}s"

if [ "$ELAPSED" -lt 600 ]; then
  echo "PHASE 0 GATE: PASS (under 10 minutes)"
else
  echo "PHASE 0 GATE: FAIL (${ELAPSED}s, budget 600s)"
  echo "Fix the build pipeline before anything else. A slow build loop compounds across"
  echo "every later phase and is the most expensive thing to leave broken."
  exit 1
fi
