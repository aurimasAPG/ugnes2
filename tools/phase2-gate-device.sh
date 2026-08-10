#!/usr/bin/env bash
#
# The Phase 2 gate, performed live on the device — one command.
#
#   tools/phase2-gate-device.sh          # inject NPC #4, build, install
#   tools/phase2-gate-device.sh --revert # remove NPC #4 again
#
# It drops docs/phase2-gate/npc4.json into StreamingAssets/Content (data only — the
# static half of the gate, tools/phase2-gate.sh, already proves the C# diff is empty),
# rebuilds, and installs. On the phone: meet Rhun by the south bridge, run their
# two-step quest, save mid-step (background the app), relaunch, confirm it resumes.
#
# Env: UNITY_BIN, TEAM_ID, DEVICE — same as build-ios.sh.

set -euo pipefail
cd "$(dirname "$0")/.."

TARGET="Assets/StreamingAssets/Content/90-npc4.json"

if [ "${1:-}" = "--revert" ]; then
  rm -f "$TARGET" "$TARGET.meta"
  echo "NPC #4 removed. Rebuild to ship without them."
  exit 0
fi

if [ ! -f docs/phase2-gate/npc4.json ]; then
  echo "docs/phase2-gate/npc4.json missing." >&2
  exit 1
fi

cp docs/phase2-gate/npc4.json "$TARGET"
echo "NPC #4 injected as $TARGET (pure data — no C# touched)."
echo

exec tools/build-ios.sh
