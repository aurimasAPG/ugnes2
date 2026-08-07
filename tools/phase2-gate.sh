#!/usr/bin/env bash
#
# Phase 2 gate check.
#
# The gate is a yes/no question: "was that C# diff genuinely empty?"
#
# This script answers it mechanically rather than by assertion. It finds the commit
# that added the fourth NPC as data, and lists every runtime C# file that changed in
# that same commit. If the list is not empty, the gate has failed and kill criterion 2
# applies: content production stops until it is fixed.
#
# It then runs the test suite, which proves the fourth NPC actually works -- an empty
# diff that ships a broken NPC passes the letter of the gate and fails the point of it.
#
# Usage:  tools/phase2-gate.sh [base-ref]

set -euo pipefail

cd "$(dirname "$0")/.."

GATE_FILE="docs/phase2-gate/npc4.json"
RUNTIME_DIR="Assets/HiddenValley/Runtime"

if [ ! -f "$GATE_FILE" ]; then
  echo "FAIL: $GATE_FILE is missing. There is nothing to demonstrate."
  exit 1
fi

GATE_COMMIT="$(git log --format=%H --diff-filter=A -1 -- "$GATE_FILE")"
if [ -z "$GATE_COMMIT" ]; then
  echo "FAIL: $GATE_FILE has never been committed, so there is no diff to inspect."
  exit 1
fi

BASE="${1:-${GATE_COMMIT}^}"

echo "Phase 2 gate"
echo "  baseline commit : $(git rev-parse --short "$BASE")"
echo "  gate commit     : $(git rev-parse --short "$GATE_COMMIT")  (adds the fourth NPC)"
echo

echo "Files changed by the gate commit:"
git diff --name-only "$BASE".."$GATE_COMMIT" | sed 's/^/  /'
echo

CHANGED_RUNTIME_CS="$(git diff --name-only "$BASE".."$GATE_COMMIT" -- "$RUNTIME_DIR" | grep '\.cs$' || true)"

if [ -n "$CHANGED_RUNTIME_CS" ]; then
  echo "GATE FAILED — runtime C# changed in order to add an NPC:"
  echo "$CHANGED_RUNTIME_CS" | sed 's/^/  /'
  echo
  echo "Kill criterion 2 applies. Stop content production and refactor until adding"
  echo "content touches no runtime code. The difference between 3 NPCs and 20 is"
  echo "entirely whether NPC #4 costs an afternoon or a sprint."
  exit 1
fi

echo "Runtime C# changed by the gate commit: none."
echo

echo "Running the suite (the NPC must also work, not merely load)..."
dotnet test tools/HiddenValley.Tests -v q --nologo

echo
echo "GATE PASSED (data-driven half)."
echo
echo "Still outstanding for the full Phase 2 gate, and NOT claimed by this script:"
echo "  - the demonstration must be performed on the running game, on device"
echo "  - see blocker B1 in docs/phase-log.md"
