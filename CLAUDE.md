# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

# HIDDEN VALLEY — MISSION

This file is the mission. The operating rules it refers to live in
`/docs/build-protocol.md`. Lore, names and character material live in
`/docs/world-bible.md`. The running record lives in `/docs/phase-log.md`.

---

## The mission

I want you to build the Hidden Valley vertical slice: **one TestFlight-installable
iOS build containing ~25 minutes of polished gameplay** — one hub village
(Heartwood), one adjacent forest region, the companion character Pip, 3 named
NPCs, 8 quests, 1 multi-step environmental puzzle, and 1 complete mystery thread
that resolves into an unanswered question. Content beyond this list is out of
scope; if you find yourself building Frostpeak, you have failed the brief.

The three NPCs must be **fundamentally different at the systems level, not the
personality level**: each one must hand the player a capability they did not
previously have — one grants a crafting recipe family, one changes what the world
contains (a shop, or a resource node that only appears after their quest), one
gates traversal into a space that was closed. Three charming people who all say
"fetch me five crystals" is the failure state. Likewise the 8 quests: no more than
two may share a quest *shape*, drawn from — fetch, repair, investigate,
craft-to-spec, observe-and-report, escort, trade, unlock. And exactly one of the
eight must be solvable in a way the quest text does not describe.

Quality dimensions, in priority order:

1. **Frame budget honesty** — 60fps measured on the physical target device with
   the fully populated village scene loaded, never in the editor, never on a
   grey-box scene.
2. **Interest density** — no 40-second stretch of traversal anywhere in the slice
   without an interactable, a readable landmark, or an NPC beat; this is measured
   by walking every path with a stopwatch, not asserted.
3. **Data-driven content authoring** — every NPC, quest, item, clue and dialogue
   line lives in ScriptableObjects or JSON, and adding a fourth NPC must require
   zero C# changes; this single property is what decides whether the remaining six
   regions are affordable or fictional.
4. **IP safety** — no character name, silhouette, colour-and-shape signature, UI
   layout, or signature mechanic traceable to a specific existing property; when a
   design lands close to one, change it and note the change.

Resources: the world bible at `/docs/world-bible.md` for lore, names and character
material; the engine's own profiler and frame debugger for pass 1; the phase gates
in `/docs/build-protocol.md`, which you must not skip. For instance, a reasonable
route is to grey-box the entire village-plus-forest layout and walk it for interest
density *before* any art exists, because interest density is a layout property and
re-laying-out a finished scene is where projects die.

Before you call any phase done, run the **three iteration passes** defined in
`/docs/build-protocol.md` §3: the device pass, the play-feel pass, and the
derivativeness pass. A pass you did not record in the phase log did not happen.

**Definition of done for the whole mission:** an installable build, a recorded
uncut 25-minute playthrough by someone who did not build it, and a systems README
containing the content-authoring guide (how to add an NPC, a quest, a clue) — plus
the phase log showing every gate cleared or consciously waived.

**The real stake is opportunity cost:** these are the same founder-hours as the
agency and the EcomExpo run-up, so a slice that slips past its gates is not a
delayed game, it is a decision to spend September on the wrong thing.

Work autonomously through each phase, mark unknowns `[CONFIRM]` in the log rather
than inventing answers, and stop only at a gate failure or a true blocker.

---

## Standing rules for any agent working in this repo

- **Read `/docs/phase-log.md` first.** It says which phase is open, which gates are
  cleared, and which `[CONFIRM]` items are still unanswered. Do not start work in a
  phase whose predecessor has an uncleared gate.
- **Do not skip a gate.** A gate is a yes/no question with a stated rescope action
  on failure. Failing a gate does not mean try harder; it means take the action.
- **Do not invent answers to `[CONFIRM]` items.** Log them and proceed with the
  work that does not depend on them.
- **A pass you did not record did not happen.** Every device pass, play-feel pass
  and derivativeness pass gets an entry in the phase log with its measurements.
- **Scope ceiling is binding.** One village, one forest, one companion, 3 NPCs,
  8 quests, 1 puzzle chain, 1 mystery thread. Nothing else.

---

## Commands

```bash
# Tests — 43 xunit tests over Core + shipped content, incl. a full scripted playthrough.
# No Unity needed. dotnet may be user-local at ~/.dotnet/dotnet.
dotnet test tools/HiddenValley.Tests

# One-line device build (the Phase 0 gate; times itself against the 600 s budget).
# Env: UNITY_BIN (editor binary), TEAM_ID, DEVICE (udid). Uses devicectl-era ios-deploy
# only for install; debugging via ios-deploy is obsolete on iOS 17+ — use:
#   xcrun devicectl device install app --device <udid> <path.app>
#   xcrun devicectl device process launch --console --device <udid> <bundle-id>
tools/build-ios.sh

# Phase 2 gate: proves adding NPC #4 changed zero runtime C#.
tools/phase2-gate.sh

# Headless editor commands (Unity at /Applications/Unity/Hub/Editor/<ver>/Unity.app/Contents/MacOS/Unity):
Unity -quit -batchmode -projectPath . -executeMethod HiddenValley.Editor.ProjectSetup.All          # URP + player settings + both scenes
Unity -quit -batchmode -projectPath . -executeMethod HiddenValley.Editor.VillageSetup.GenerateHeartwood
Unity -quit -batchmode -projectPath . -executeMethod HiddenValley.Editor.BuildCommand.iOS           # respects HV_BUILD_DIR
Unity -quit -batchmode -projectPath . -executeMethod HiddenValley.Editor.BuildCommand.macOS         # debug player; HV_MAC_BUILD_DIR
```

## Architecture

Three assemblies, strictly layered:

- **`Runtime/Core`** — the whole game as plain C# (`noEngineReferences: true`). A closed
  condition/effect vocabulary interpreted by generic engines (quests, dialogue, crafting,
  world state, clock, save). There are no per-content types — a new NPC is data, never a
  class — and that property IS the Phase 2 gate. Testable on any machine with .NET.
- **`Runtime/Unity`** — MonoBehaviour views onto Core. Change notification is one int:
  views poll `GameState.Revision` per frame. `GameBootstrap` owns the `Game`;
  `LayoutSpawner` instantiates world objects/NPCs/Pip at runtime; binders mirror state.
- **`Editor`** — headless generators. Scenes are *generated, never hand-authored*:
  `ProjectSetup` (config + grey-box room), `VillageSetup` (Heartwood shell from layout).

Data lives in `Assets/StreamingAssets/`: `Content/*.json` decides WHAT exists (NPCs,
quests, items, clues, dialogue — validated by `ContentValidator` before every build);
`Layout/heartwood.json` decides WHERE (geometry, spawns, waypoints). Moving or adding
content is a JSON edit. `docs/systems-README.md` is the authoring guide.

## Landmines (each cost real debugging time — do not rediscover them)

1. **The packed-scene corruption (Unity 6000.0.81f1).** A generated scene serialized with
   the full object population packs into a `level0` the player rejects ("corrupted",
   "Position out of bounds") on iOS *and* macOS. The scene must stay a minimal shell —
   static blocks, player, camera, controls, bootstrap — and everything else spawns at
   runtime via `LayoutSpawner`. Never add binders, NPCs, the HUD, or any
   `NavMeshSurface` (baked or not) to a serialized scene. After any change to what a
   scene contains, smoke-test with `BuildCommand.macOS` + launch before touching a device.
2. **No NavMesh anywhere.** NPCs walk waypoints directly (`NpcBinder.Update`).
3. **`GameHud` is code-spawned** (from `TouchControls.Start`), never scene-serialized.
4. **Builds go outside the repo** (`~/Library/Caches/HiddenValleyBuild`, via
   `HV_BUILD_ROOT`). The repo sits in iCloud-synced `~/Documents`; the file provider
   re-tags outputs with Finder metadata and codesign rejects them ("detritus").
5. **Never uninstall the app from the phone to "clean up"** — it resets the developer
   trust and someone has to re-trust in Settings. Install over the top.
6. IMGUI + legacy `Input` everywhere by design (input handler is set to "Both");
   grey-box builds need no canvas, prefabs, or font assets.
7. **An unfocused player stops updating entirely** — Update halts, coroutines never
   resume. Any unattended verification run must set `Application.runInBackground`.
   To photograph the game, use its own hook rather than macOS `screencapture` (which
   needs window focus and accessibility permission to send keys):
   `-hvshot <path.tga> [seconds]` captures and quits; `-hvminute <m>` sets the hour;
   `-hvpause` opens the pause panel. Convert with
   `sips -s format png shot.tga --out shot.png`. TGA is written by hand because
   `Packages/manifest.json` trims the `screencapture`/`imageconversion` modules.
