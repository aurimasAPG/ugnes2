# HIDDEN VALLEY — PHASE LOG

The running record. Read this first. A pass that is not recorded here did not happen; a
gate that is not marked here is not cleared.

**Current state:** Phase 0 — **in progress on the owner's Mac** (2026-08-07, second
session). Unity 6 LTS is installing; the Phase 0 runtime pieces and a headless project
setup command are authored. Blocked on three user-only steps: Unity license sign-in,
a connected iPhone, and an Apple ID in Xcode. See B1 (revised) and the 2026-08-07 Mac
session entry.

| Phase | Gate | State |
|---|---|---|
| 0 — Foundation spike | Build reaches device in <10 min from one command? | ✅ **CLEARED 2026-08-07** — 238 s cold, one command, installed on the target iPhone |
| 1 — Traversal feel | Camera never clips, never loses player, over 5 min adversarial? | ⬜ Not started |
| 2 — Systems skeleton | Was the C# diff genuinely empty? | 🟨 **Data-driven half demonstrated.** Gate not cleared — see below |
| 3 — Content + first art | Interest-density walk passes, no 40s dead stretch? | ⬜ Not started — no layout, no art |
| 4 — Play-feel + polish | ≥3 of 5 testers spontaneously want to continue? | ⬜ Not started |

### Why Phase 2 is amber and not green

The gate asks for a live demonstration **in the running game** that a new NPC, a new
two-step quest and a new item can be added by editing data only, then saved, reloaded, and
resumed at the correct step.

What has been demonstrated here:

- `docs/phase2-gate/npc4.json` adds a fourth NPC, a two-step quest, a new item and two
  world objects. It is the entire change.
- `tools/phase2-gate.sh` diffs `Assets/HiddenValley/Runtime` across the commit that added
  it. **Runtime C# changed: none.** Verified 2026-08-07, baseline `ba0c12f`, gate commit
  `f556370`.
- The new NPC is playable end to end, and a save taken mid-quest reloads on the correct
  step. Covered by `Phase2GateTests.cs`.

What has **not** been demonstrated: any of it *in the running game*. There is no running
game — there is a tested rules engine and a Unity adapter layer that has never been
compiled by Unity. The gate stays amber until someone runs it on a device.

---

## `[CONFIRM]` — open decisions

| # | Decision | State |
|---|---|---|
| 1 | **Engine and version.** | ✅ **Answered 2026-08-07** — Unity 6 LTS + URP. |
| 2 | **Repository.** | ✅ **Answered 2026-08-07** — `aurimasapg/ugnes2`, greenfield. |
| 3 | **Target device floor.** | ✅ **Answered 2026-08-07** — iPhone 13. Frame budget is therefore 16.7 ms, and `FrameTimeHud` is configured against it. |
| 4 | **Who is building.** | 🟨 **Partly answered** — Claude Code writes source here; builds, device passes and all gates run by the owner on a Mac with the device. Whether a contract artist exists is still open, and it decides whether art is a phase or a dependency. |
| 5 | **Art pipeline.** Asset-store base meshes with custom shading, or fully bespoke? | `[CONFIRM]` — largest single cost driver, and Phase 3 cannot be scoped without it. |
| 6 | **What this is for.** Commercial, demonstration artifact, or personal? | `[CONFIRM]` — the stakes sentence in `/CLAUDE.md` is still the honest default (opportunity cost). |
| 7 | **Audio.** Licensed library versus commissioned original. | `[CONFIRM]` — nothing authored. |
| 8 | **The title "Hidden Valley".** | `[CONFIRM]` — **raised by the derivativeness pass, 2026-08-07.** Two problems: a well-known US food trademark, and a genre-adjacency read straight to *Stardew Valley*. Recommend changing. Full finding in `docs/passes/derivativeness-01.md` §2.1. |

---

## Blockers

### B1 — No game toolchain in the execution environment (2026-08-07, MOSTLY RESOLVED)

**Revised 2026-08-07, Mac session.** The original blocker described the Linux agent
container. This session runs on the owner's MacBook Pro (Apple silicon, Xcode 26.6):

- .NET 8 SDK installed (user-level, `~/.dotnet`); **all 22 tests pass on this machine.**
- Unity Hub installed; Unity **6000.0.81f1 + iOS module** installing headlessly.

Still open, and all three are user-only steps:

1. **Unity license** — open Unity Hub once and sign in (free Personal license
   activates automatically). Batch mode refuses to run unlicensed.
2. **Device** — no iPhone connected (`xctrace list devices` shows none).
3. **Signing** — zero codesigning identities on this Mac; sign into Xcode with an
   Apple ID (Settings → Accounts), and set `TEAM_ID` for `tools/build-ios.sh`.

Original entry follows for the record.

### B1 (original) — No game toolchain in the execution environment (2026-08-07, superseded)

Linux x86_64. Verified absent: `unity`, `unity-editor`, `godot`, `mono`, `xcodebuild`. No
macOS host, no Xcode, no signing identity, no TestFlight, no device.

**Partially mitigated 2026-08-07.** The .NET 8 SDK was installed into the session
scratchpad, and the game's rules were deliberately written as plain C# with
`"noEngineReferences": true` on the Core assembly definition. The rules therefore compile
and test here — 22 tests, including a scripted playthrough of the whole slice. Unity
compiles the same files.

**Still blocked, and not worked around:** every phase gate, the device pass, the
interest-density walk, and all human testing. The Unity adapter layer
(`Assets/HiddenValley/Runtime/Unity`, `Assets/HiddenValley/Editor`) has **never been
compiled**, because compiling it requires Unity. Treat it as reviewed-but-unverified
source.

### B2 — World bible was absent (2026-08-07, RESOLVED by decision)

The original 36-section document was never supplied. On 2026-08-07 the owner chose
explicitly to have the world bible drafted by the agent and flagged as such, rather than
leave content blocked. `docs/world-bible.md` carries a provenance header. Every name in it
is subject to owner veto.

### B3 — Calendar kill criterion is 25 days out (2026-08-07, OPEN)

Kill criterion 4 parks the project until 6 October if the Phase 2 gate is not cleared by
**1 September**. As of this entry that is 25 days away. The gate's data-driven half is
demonstrated; what remains is a working Unity project and a device, which is mostly
`[CONFIRM]` #4 and #5 rather than engineering time.

---

## Entries

### 2026-08-07 — Repo setup

Actor: Claude Code (`claude-opus-5`), branch `claude/hidden-valley-build-brief-mvnjdu`.

`/CLAUDE.md` (the mission), `/docs/build-protocol.md` (gates, passes, kill criteria),
this log, and a world-bible stub.

Passes run: **none.** Repo setup is not a phase and has no gate.

### 2026-08-07 — World bible authored

`docs/world-bible.md`. Heartwood, the Ash Shelf, Pip, the three NPCs, the Quietday mystery
with its resolution and its unanswered question, the Undersluice puzzle chain, the eight
quests and their shapes, and a naming convention so future names are derivable.

The three NPCs were designed **from their systems contribution outward** — Vesk grants a
recipe family, Orrel changes what the world contains, Coll opens a closed space — and
personality applied afterwards. The puzzle chain deliberately forces two of them to
compose: the setting plate needs a lens, so Coll's traversal gate depends on Vesk's
crafting family.

Flagged throughout as agent-authored. See B2.

### 2026-08-07 — Core, content, validator, Phase 2 gate demo

**Engine.** `Assets/HiddenValley/Runtime/Core` — a closed condition/effect vocabulary that
all content composes from, plus generic interpreters for quests, dialogue, crafting,
world state, time of day and save/load. No `UnityEngine` reference, enforced by the
assembly definition.

The architectural decision worth recording: **there is no `open_shop` effect and no
`unlock_area` effect.** Those are conditions on world objects. An NPC who changes the
contents of the world sets a flag; the objects watch the flag. That inversion is why
Orrel's shop and Coll's cliff cost zero lines of C#, and it is the single reason the
Phase 2 gate is passable at all.

**Content.** The full slice as JSON: 3 NPCs plus Pip, 8 quests across 8 distinct shapes,
the puzzle chain, the mystery, 11 items, 4 recipes, 5 clues, 30 world objects.

**Validator.** `ContentValidator` asserts the brief's design constraints as code —
quest-shape spread, the three capability kinds, exactly one undocumented solution — next
to ordinary reference integrity. It runs before every iOS build, so a content typo fails
the build rather than reaching a tester.

**Bug found by the playthrough test, worth recording because it is a class not an
instance:** a mystery dialogue entry sat above an unoffered quest in two NPCs' entry
lists. A player who found the scorched bark before meeting Vesk or Coll would get the
middle of the mystery thread as that character's first-ever line, and never be offered
their quest. Found by the scripted playthrough, not by reading the file. Fixed by
ordering, with `_note` markers at both sites and a rule written into
`docs/systems-README.md` §3.

**22 tests pass** (`dotnet test tools/HiddenValley.Tests`), including a full scripted
playthrough proving the dependency graph closes with no soft-lock, the undocumented
drowned-lens route, save/reload at correct step, and stale-content tolerance on load.

Passes run this entry:

- **Device pass — NOT RUN.** Impossible here (B1). No frame times, no memory figures, no
  device log. Nothing in this entry may be read as a performance claim.
- **Play-feel pass — NOT RUN.** Requires playing. There are no timings, and the
  interest-density walk has not happened because there is no layout to walk.
- **Derivativeness pass — RUN.** See next entry.

### 2026-08-07 — Unity adapter layer

`Assets/HiddenValley/Runtime/Unity` and `Assets/HiddenValley/Editor`: content bootstrap
with atomic saves, world-object binder, interaction controller, NPC schedule binder, Pip's
light, the frame-time HUD required by the Phase 0 done-state, and a one-line iOS build
command. `Packages/manifest.json`, three assembly definitions, `tools/build-ios.sh`.

**None of this has been compiled.** See B1. Package versions in the manifest are
best-effort for Unity 6 and will be reconciled by the editor on first open.

### 2026-08-07 — Derivativeness pass 01

Recorded in full at `docs/passes/derivativeness-01.md`.

Changed: **Brann → Coll** (one letter from Bran Stark); **"investigation board" → "the
account"**, with a binding art constraint against the corkboard-and-red-string visual;
Pip's moth-not-fairy silhouette and dimming-not-highlighting mechanic recorded so they are
not reversed by default later.

Referred upward: **the title "Hidden Valley"** — now `[CONFIRM]` #8.

Cleared and recorded: Heartwood, Pip, the invented names, the sluice puzzle, time-gated
resources, the mystery's structure.

**Explicitly not assessable yet:** character silhouettes and UI layout, because no art and
no UI exist. These are the highest-risk remaining items — silhouette is where IP problems
actually live — and the next pass is triggered by whichever lands first.

### 2026-08-07 — Mac session: toolchain + the missing Phase 0 runtime

Actor: Claude Code (`claude-fable-5`), on the owner's MacBook Pro.

**Toolchain.** .NET 8 SDK (user-level), Unity Hub, Unity 6000.0.81f1 + iOS module
(headless install). `dotnet test` on this machine: **22/22 pass, 42 ms** — the Core
engine is verified on macOS/arm64, not just in the Linux container.

**Gap found and closed.** The Phase 0 done-state requires a touch joystick, a follow
camera and the frame-time readout in a grey-box room on device. The HUD existed; the
joystick, the player controller, the camera and the scene did not. Authored:

- `PlayerController.cs` — CharacterController movement, camera-relative, explicit
  accel/decel/turn-rate fields as the Phase 1 tuning surface, sprint, jump.
- `FollowCamera.cs` — no free orbit (cannot lose the player by construction),
  sphere-cast occlusion pulled in instantly and released eased, exponential damping
  that is framerate-independent.
- `TouchControls.cs` — floating left-half joystick from raw touches, right-half
  tap = jump / hold = sprint, desktop fallback in the editor, IMGUI overlay (no
  canvas in grey-box builds).
- `Editor/ProjectSetup.cs` — one headless command
  (`-executeMethod HiddenValley.Editor.ProjectSetup.All`) that configures URP for
  mobile (no HDR, MSAA 4x, 45 m shadows), sets iOS player settings (landscape-only,
  iOS 16 floor, provisional bundle id `lt.apgmedia.hiddenvalley`), sets the input
  handler to Both, and generates the grey-box room — perimeter, tight alcove, narrow
  corridor, pillars, a 20° ramp and a 0.25 m staircase, i.e. the exact furniture the
  Phase 1 adversarial camera test names — then registers the scene in Build Settings.
- `ProjectSettings/ProjectVersion.txt` pinned to 6000.0.81f1.

**None of the Unity-layer code has compiled yet** — that is the first thing to run
once the license exists. Treat everything above as reviewed-but-unverified source,
same status as the adapter layer.

Passes run this entry: **none.** No device, no build. The derivativeness exposure of
this entry is low (grey-box geometry, a floating joystick and tap-to-jump are genre
furniture, not property signatures) but it is recorded as *not run*, not as passed.

### 2026-08-07 — Play HUD and the data-driven layout

Actor: Claude Code (`claude-fable-5`), Mac session, while the editor downloads.

**Two gaps closed, both required before Phase 3 can even be attempted:**

1. **`GameHud`** — the slice was a black box: the engine could run a quest but the
   player had no way to see dialogue, the tracker, the bag, the account, or a readable.
   IMGUI like the rest of the debug layer, so grey-box builds need no canvas, no
   prefabs, no font assets. Input is one context button routed through
   `TouchControls.ContextAction`: tap = talk / interact / advance dialogue / jump, in
   that priority. Dialogue and panels lock movement. Phase 4 restyles this; it does
   not rewire it.

2. **`Assets/HiddenValley/Layout/heartwood.json` + `VillageSetup`** — the project's
   data-driven property applied to space. The layout JSON places every world-object
   id (all 32), every NPC spawn and schedule waypoint, player and Pip; the generator
   builds the grey-box village + climb + Ash Shelf + lower shelf, wires every binder
   and interaction zone, bakes the NavMesh, and registers the scene. The scene file
   is never hand-authored. Re-cutting the layout after a failed interest-density walk
   is a JSON edit and a re-run — re-laying-out a finished scene is where projects
   die, and this is the mechanism that makes re-laying-out cheap.

   Layout arithmetic on record: walk speed 4.5 m/s → the 40-second rule is 180 m; the
   longest leg in the authored layout is ~30 m (the climb's second ramp), with a
   landmark tree and a resin pickup on the landing. **This is arithmetic, not the
   gate** — the gate is walked with a stopwatch on device, and stays unclear.

   Also fixed before first compile: `HiddenValley.Editor.asmdef` was missing the URP
   and AI-navigation references its code uses — caught by review, would have been the
   first compile error.

Passes run this entry: **none** (no device, no build — B1's remaining user steps).
Derivativeness exposure: "one context button" and a floating joystick are genre
furniture; the account/bag naming follows the world bible. Next derivativeness pass
triggers when art or real UI styling lands, per pass 01.

### 2026-08-07 — First compile, both scenes generated, build pipeline proven to the signing wall

Actor: Claude Code (`claude-fable-5`), Mac session, after the owner signed into Unity Hub.

**The Unity layer compiles.** 26 C# files authored blind across two environments
produced exactly one compile error — `BuildFailedException` needed
`using UnityEditor.Build;` in `BuildCommand.cs`. Fixed. Zero errors after.

**Headless setup ran end to end.** URP configured, iOS player settings applied,
`Greybox.unity` and `Heartwood.unity` both generated with NavMesh baked, meta files
and ProjectSettings committed from Unity 6000.0.81f1.

**`tools/build-ios.sh` measured, twice:**

- Unity → Xcode project: **53 s cold, 36 s incremental**, content validator passed,
  0 errors, both runs.
- Xcode stage first failed on a missing iOS platform SDK (fresh Xcode 26.6);
  installed via `xcodebuild -downloadPlatform iOS`, no GUI needed.
- Xcode stage now proceeds to: `"Unity-iPhone" requires a provisioning profile` —
  the exact expected wall. **Zero signing identities on this Mac.**

**Gate arithmetic so far:** the Unity stage spends ~40 s of the 600 s budget. The
gate is not cleared — it requires the app to reach the physical device — but the
budget is in no visible danger.

**Remaining before the Phase 0 gate can be attempted, both user-only:**
1. Apple ID in Xcode (Settings → Accounts) — the free personal team is enough for
   on-device development builds.
2. iPhone plugged in and trusted. Then `TEAM_ID=<team> tools/build-ios.sh`
   (`brew install ios-deploy` for the install step).

Passes run this entry: **none** — still no app on a device. Timings above are build
telemetry, not a device pass.

### 2026-08-07 — PHASE 0 GATE CLEARED

Actor: Claude Code (`claude-fable-5`) driving; owner performing the device-side steps.

**`tools/build-ios.sh` → `PHASE 0 GATE: PASS (under 10 minutes)` — total 238 s, cold**,
from one command, ending with the signed app installed on the physical target device
(iPhone 16 Pro, iOS 26.6 — above the iPhone 13 floor from `[CONFIRM]` #3).

The five failures between "code compiles" and "app on phone", each fixed in the build
system so they never recur:

1. **Missing iOS platform SDK** in fresh Xcode → `xcodebuild -downloadPlatform iOS`.
2. **Manual signing in the generated project** → Unity now sets
   `appleEnableAutomaticSigning` + team id in `ProjectSetup.ConfigurePlayerSettings`.
3. **Device not registered with the team** → build targets the concrete device
   (`platform=iOS,id=$DEVICE`) so `-allowProvisioningDeviceRegistration` works.
4. **Developer Mode off** (two-step toggle, easy to half-complete) → verified against
   the device itself via `devicectl` before building.
5. **iCloud Drive corrupting signatures** — the repo lives in synced `~/Documents`;
   the file provider re-tags outputs with Finder metadata and codesign refuses them
   ("detritus not allowed"). **Builds now go to `~/Library/Caches/HiddenValleyBuild`**,
   outside any synced tree, via `HV_BUILD_DIR`. Flagged for later: the repo itself
   (especially Unity's `Library/`) still syncs to iCloud pointlessly.

Owner-side one-time setup completed this session: Unity license, Apple ID in Xcode
(personal team `32U8KR34UT`, extracted from the certificate), device trust, Developer
Mode.

**What this gate does NOT claim:** the app has not yet been launched and played on the
device. The Phase 0 done-state (grey-box scene running with joystick, follow camera,
frame-time readout) is verified the moment the owner opens the app; the ten-minute
device pass is still to run. The installed build boots the Heartwood slice scene.

### 2026-08-07 — Launch crash found by bisection: editor-baked NavMesh corrupts the packed scene

The installed build crashed on boot: `The file '...level0' is corrupted!`,
`[Position out of bounds!]`, signal 5, grey screen for a second.

Hypotheses eliminated in order, each by a full rebuild-and-launch cycle with the
console attached via `devicectl`:

1. ios-deploy transfer corruption — reinstalled with `devicectl`: same crash.
2. `-nographics` export — exported with graphics: same crash.
3. Unsaved in-memory NavMeshData reference — persisted as asset: same crash
   (this WAS a real serialization bug — the pre-fix level0 was 34 KB longer than its
   own header claimed — but fixing it did not stop the crash).
4. Stale device container — full uninstall + reinstall: same crash.
5. Half-stale Data folder from incremental export — fully clean export: same crash.

**The decisive bisect:** the Greybox scene (no NavMesh) shipped alone — **runs on
device**, confirmed by process liveness and by the owner's eyes: grey-box room,
joystick, follow camera, frame-time HUD all live. That is the Phase 0 done-state,
observed. Then Heartwood WITHOUT its NavMesh bake — **also runs**. The editor-time
NavMesh bake was the single poison: a batch-generated scene saved with baked
NavMeshData produces a level0 the iOS player rejects, whether the data is an asset
or not.

**Permanent fix (revised, same session):** the NavMesh-only story was incomplete.
Further Mac-player bisection showed:

| Scene profile | Result |
|---|---|
| Greybox | ALIVE |
| Heartwood shell (blocks + player + systems) | ALIVE |
| shell + world objects only | ALIVE |
| shell + NPCs only | ALIVE |
| shell + Pip only | ALIVE |
| any **pair** of (wo, npc, pip) | ALIVE |
| **full** (wo + npc + pip all serialized) | **DEAD** — level0 corrupt |

So the poison is not a single component type: packing the full Heartwood object set
into one scene produces a bad level0. NavMesh made it worse earlier; the full village
does it alone.

**Permanent fix (final):**

1. **No NavMesh in the shipped scene** — NPCs walk straight to schedule waypoints
   (`NpcBinder` direct motion). Grey-box fidelity does not need obstacle avoidance.
2. **Runtime layout spawn** — `LayoutSpawner` reads
   `StreamingAssets/Layout/heartwood.json` at Awake (−950) and spawns world objects,
   NPCs and Pip in code. The serialized scene stays at the known-good shell size
   (static blocks + player + camera + controls + bootstrap). Data-driven property
   preserved: moving content is still a JSON edit, zero C#.
3. **GameHud** attaches at runtime from `TouchControls.Start` (not scene-serialized).

**Mac end-to-end verified 2026-08-07:**

- `tools/build-mac.sh` → `~/Library/Caches/HiddenValleyBuild/mac/HiddenValley.app`
- Process alive 12+ s (no level0 crash)
- Player.log: content ready (4 npcs, 8 quests, 32 world objects); layout spawn
  `wo=32 npc=3 pip=True` in 5 ms
- boot-marker written under Application Support
- Controls: WASD, Space (context/jump), Shift sprint, Bag/Account IMGUI

Trust-on-uninstall note for future sessions: uninstalling the app resets the
developer-profile trust on the phone; reinstalling over the top does not.

### 2026-08-08 — M0: correctness fixes found by four-agent inspection

Actor: Claude Code (`claude-fable-5`). Full findings in `docs/aaa-roadmap.md`;
milestones in `docs/release-plan.md`.

Fixed, each verified by test and/or the macOS smoke player:

1. **Save restored on launch** — `LoadFromDisk` had zero call sites; every launch was
   New Game. Restore now runs in `GameBootstrap.Awake` before `Ready`; player position
   rides in a `player.pos` flag and is reapplied in `Start`.
2. **Crafting panel** — `ui.open_crafting` finally has a consumer in `GameHud`;
   Vesk's capability (and quest.lens's documented route) is reachable on device.
3. **GC hitches removed** — derived GUIStyles hoisted, clock/tracker/choices/bag all
   revision-gated caches; the per-OnGUI allocation churn (est. 5–20 ms spikes every
   ~10–30 s) is gone.
4. **Cue effects** — new Core `CueEffect` (`{"type":"cue","id":"sting.mystery"}`),
   transient `PendingCues` drained by `GameBootstrap.Cue`; `GameAudio` routes
   `sting.*`/`sfx.*`. The hardcoded English-substring sting matcher in GameHud is
   deleted; the mystery's stings are authored in `30-quietday.json` where they belong.
5. **Latent revision bug** — `LearnClueEffect`/`LearnRecipeEffect` mutated state
   without `Touch()`, so clue/recipe discovery never notified views. Now routed
   through `GameState.LearnClue/LearnRecipe`. Regression-tested.
6. **Audio** — VO duck no longer flaps on the fallback path; short SFX prewarmed at
   boot; per-speaker VO async-prewarmed when a talk prompt first appears.
7. **VO hygiene** — 9 narration lines ("Vesk does not look up…") were mapped to
   character voice; unmapped, with a regression test.
8. **Validator** — new flag-consistency check (set-but-never-read / read-but-never-set
   with `ui.`/`solved.`/`player.` namespaces); would have caught #2 at authoring time.
9. **Single-source layout** — the duplicated `Assets/HiddenValley/Layout` copy is
   deleted; `VillageSetup` reads the same StreamingAssets file as `LayoutSpawner`.

Tests: **47/47.** macOS smoke: full slice boots, restores a save, spawns 32/3/1 in
33 ms. Passes run: none (smoke ≠ device pass). **Still owed by the owner: the first
full-slice device pass** — build to phone, 10 minutes, worst frame time into this log.

### 2026-08-08 — M1–M5 implemented: feel, atmosphere, world, alive layer

Actor: Claude Code (`claude-fable-5`), autonomous run against `docs/release-plan.md`.
Each milestone verified by the .NET suite (47/47) plus a macOS player build-and-boot;
M3 and M4 additionally verified by screenshot (dawn light over the kit-built village,
7.5 ms / 9.4 ms worst with 780 scatter instances on the Mac).

- **M1 (code half):** CapsuleAnimator (lean/gait/squash/breath, runtime-attached),
  camera sprint FOV+boom kick and travel look-ahead riding the occlusion cast,
  gait-clocked footsteps.
- **M3:** AtmosphereRig — gradient sky, fog==horizon, trilight ambient, four phase
  palettes through the Core clock (sun steps per game-minute), post volume, far plane
  140; single wet-stone palette in RuntimeArt with per-key smoothness; 14 near-white
  detail textures (palette owns hue); deliberate shadows.
- **M4:** layout rewritten to the bible — sunken N–S channel spine with three bridge
  kits and the sluice on it, building kits (pitched slate roofs, kiln chimney + smoke
  column, Coll's gear), terrain plates, path network, climb vista notch, cool/warm
  zones, 780 seeded scatter, flowing transparent water (drowned lens visible through
  it), ember light, and Quietday as a rendering state (grey light + eastern ashfall
  while the mystery is open).
- **M5:** conversation facing (both parties), NPC arrived-idles with player-aware
  head-turn, Pip life (speed-lagged follow, wander, moth flutter that roughens with
  Dimness, Perlin gutter, additive halo, dialogue calm), iOS haptics bridge wired to
  interact/choice/pickup/quest/landing, dialogue punctuation pauses + world-dim +
  panel ease + portraits, safe-area insets, toast queue paired with stings/haptics,
  and Pip's pre-quest "restless" on-ramp pointing east before the bark is found.

Passes run: **none on device** — every claim above is Mac-verified only. The owner's
device pass covers M0–M5 in one session: play 10 minutes, record worst frame time,
confirm haptics, walk one dawn and one dusk.

### 2026-08-08 — M7 characters + release scaffolding + decision docs

- **Character rigs replace capsules and billboards** (`CharacterRig`): per-key
  proportion table — Vesk broad with apron, Orrel tall with ledger, Coll stooped
  with gear charm, hooded player — built at runtime onto the Visual anchor so
  CapsuleAnimator drives them unchanged; Pip is a moth with code-flapped wings whose
  beat quickens with `Dimness`. World billboards retired; portraits stay in dialogue.
  [CONFIRM] #5 resolved *de facto*: the pipeline is fully generated/procedural;
  reverse only by explicit owner decision.
- **Release scaffolding:** app icon (lamp-moth over slate valley, deliberately
  title-independent), monotonic TestFlight build numbers, FrameTimeHud release-gated
  behind a 3-finger toggle, `tools/phase2-gate-device.sh` (the on-device gate as one
  command), save-robustness tests (garbage/truncated saves → clean fresh start;
  50/50 total).
- **Decision docs:** `docs/title-shortlist.md` (recommendation: "The Undersluice"),
  `docs/beat-sheet.md` (13 beats, timings to be filled from real sessions),
  `docs/passes/derivativeness-02.md` — the assembled look assessed: palette,
  silhouettes, Pip, buildings all clear; title remains the standing risk.
- **Deferred with reason:** wind vertex sway + moss/wetness shader — wants real
  device profiling first (vertex-stage cost is a claim until measured on the A15).

Verified: 50/50 tests; Mac player boots and runs; screenshots confirm the rigged
characters and the dawn look. Device passes still owed by the owner.

### 2026-08-08 — Full slice installed and running on the target device; final polish layer

- **The complete game is on the iPhone 16 Pro and runs** (process verified via
  `devicectl`): atmosphere, channel-spine village, character rigs, moth Pip, audio,
  haptics build. Installed via `devicectl` — note for the record: ios-deploy's
  device discovery no longer works on iOS 26; `build-ios.sh`'s install step should
  migrate to devicectl next time it is touched.
- **Wind sway** on the scatter field (staggered transform updates, ~170 writes/frame)
  that stills to zero on Quietdays — the wrong morning is visible in the grass.
- **Phase 4 paper-and-ink skin** applied to all five panels (generated deckle-edged
  paper, ink text, paper choice buttons); floating HUD text stays light-on-world;
  the input contract untouched.
- **TestFlight lane**: `tools/exportOptions.plist` + guarded `HV_UPLOAD=1` archive/
  export/upload stage in `build-ios.sh` — inert until the paid account exists.

Verified: 50/50 tests, Mac player runs, device process alive. **What remains for the
gates is play, not code**: the timed device pass, the two-person Phase 1 test, the
on-device Phase 2 performance (`tools/phase2-gate-device.sh`, before 1 Sept), the
stopwatch walk, the five-tester hook test, the recorded playthrough, the Apple
Developer purchase, and the title sign-off (`docs/title-shortlist.md`).

### 2026-08-08 — FIRST ON-DEVICE MEASUREMENTS, captured by the game itself

The self-measuring build launched on the iPhone 16 Pro via the unlock-trap loop and
wrote its own numbers: **33.3 ms steady, worst 36.4 ms, p99 34.4 ms, 90 MB.**
A perfectly steady 33.3 ms is a cap, not a struggle — Unity's iOS default
`targetFrameRate` is 30. Fixed (`Application.targetFrameRate = 60` in bootstrap);
the 60fps build is compiled and armed to auto-install + re-measure the moment the
phone (currently unplugged) reconnects.

This is exactly the class of finding the device-pass rule exists for: invisible in
the editor, invisible on the Mac, real on the phone. The 60 fps verdict against the
16.7 ms budget is the reconnect-trap's output; until it lands, quality dimension #1
remains UNMEASURED at 60.

---

## What the next session should do

1. **Play the Mac build** — walk Heartwood, talk to Vesk/Coll/Orrel/Pip, pick up an
   item, open Bag/Account. Confirm feel before Phase 1 tuning.
   `open ~/Library/Caches/HiddenValleyBuild/mac/HiddenValley.app`
2. **Phase 1 on Mac first** (faster loop than device), then re-run `tools/build-ios.sh`
   with the runtime-layout scene and clear the device pass.
3. **Answer `[CONFIRM]` #5 and #8.** Art pipeline gates Phase 3; the title gates naming.
4. Grey-box interest-density walk with a stopwatch before any art.

Rebuild Mac: `tools/build-mac.sh`. Rebuild iOS: `TEAM_ID=32U8KR34UT tools/build-ios.sh`.
