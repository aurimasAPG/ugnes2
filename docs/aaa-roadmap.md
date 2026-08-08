# AAA ROADMAP — from grey-box to the game the brief describes

Produced 2026-08-08 by four parallel end-to-end code inspections (rendering pipeline;
world/layout/art direction; characters/feel/UI; content systems + performance). Merged,
deduplicated, and ordered by perceived-quality lift per unit cost. Scope ceiling is
binding throughout: everything below deepens the existing slice; nothing widens it.

The art direction is already written and IP-safe — world bible: *"beautiful in the
specific way that wet stone and low cloud are beautiful… never twee."* Desaturated
wet-mineral world; warm man-made light as the accent (kiln, lamps, Pip). That sentence
is the palette, the grading, and the lighting plan. Nothing below invents a look; it
implements that one.

---

## 0. Fix first — these betray quality regardless of art (all found by inspection)

| # | Finding | Where | Cost |
|---|---|---|---|
| 0.1 | **Save is written but never loaded** — `LoadFromDisk()` has zero call sites; every launch is a New Game. A phone call mid-slice loses 25 minutes. | `GameBootstrap.Awake` / `:109` | S |
| 0.2 | **Crafting has no UI** — `ui.open_crafting` flag has no consumer; Vesk's recipe capability is unreachable on device, which silently inverts the "exactly one undocumented solution" rule (only the drowned-lens route works). Tests miss it because they call `Crafting.Craft()` directly. | `GameHud` (new panel), flag set in `40-gathering.json:87` | M |
| 0.3 | **GC hitch every ~10–30 s** — IMGUI allocations per frame (`new GUIStyle` in four Draw methods, per-frame tracker/clock strings, double `AvailableChoices`). Est. 5–20 ms Boehm spikes on iPhone 13. | `GameHud.cs:292–406`, `QuestEngine.ActiveTrackerLines` | S |
| 0.4 | **VO loads synchronously at line-open** — 10–40 ms hitch at the player's highest-attention moment. Prewarm SFX at boot; `LoadAsync` a speaker's lines when their talk prompt appears. | `GameAudio.cs:316–335` | S–M |
| 0.5 | Mystery sting is a **hardcoded English-substring match** re-firing on replayed lines. Replace with the cue effect (item 3.1). | `GameHud.cs:236–243` | S |
| 0.6 | Audio duck inconsistency: missing-VO path skips the ambience duck entirely. | `GameAudio.cs:188–211` | S |
| 0.7 | "Talk to Pip" prompt targets a wordless companion unless intended; and several VO lines voice their own third-person stage directions ("Vesk does not look up…"). | `LayoutSpawner.cs:171`, `voice_map.json` | S |
| 0.8 | Layout JSON is duplicated (`Assets/HiddenValley/Layout/` vs `StreamingAssets/Layout/`) — schema drift waiting. Make the editor generator read the StreamingAssets copy. | `VillageSetup` | S |

## 1. The atmosphere build — ~80% of the visual jump, one connected system

**`AtmosphereRig`** (runtime component, scene-shell-safe, smoke-test via `BuildCommand.macOS`):

1.1 Gradient skybox (3-stop, ~15-line shader) + `RenderSettings` exponential fog with
    fog color == horizon color + trilight ambient — all from ONE palette table.
    Then cut `farClipPlane` 300→~120 (fog owns the distance) and reclaim culling time.
1.2 **Time-of-day driven by the Core clock** — `Clock.NormalisedTime` exists, is
    documented for exactly this, and has zero consumers. Sun rotation/color, fog,
    ambient, sky lerped through 4–5 phase palettes. Load-bearing: the head-gate puzzle
    *only works at dawn* and dawn is currently invisible; three agents independently
    named this the biggest single multiplier. Keyframes live in JSON.
    Step the sun per game-minute (not per frame) so shadow renders can cache.
1.3 Post-processing volume, code-spawned: color grading (sat −5..−15, lifted cool
    shadows = "wet stone"), vignette ~0.25. **No bloom, no DoF, HDR stays off.**
    Biggest deliberate frame spend: ~0.8–1.2 ms; if the device pass objects, drop
    renderScale to 0.9 (MSAA 4x hides it).
1.4 Shadow strategy made deliberate: 2 cascades, 2048 map, distance ~40 m + fade,
    bias tuned; blob-shadow quads under actors. Recovers ~0.5–1 ms — funds 1.3.
1.5 One palette table (currently duplicated in `VillageSetup.Palette` +
    `RuntimeArt.Fallback` — a real divergence bug), retuned to wet-stone: cool
    low-sat greys, grey-green moss, cold buff sand; per-key smoothness (wet stone
    ~0.45). New keys: `slate`, `stone`, `ash`, `earth`, `lamp`.

Net frame cost of the whole rig: ≈ 1.0–1.5 ms, partially offset by 1.4 + far-plane cut.
Every item confirmed on device via `FrameTimeHud` per quality dimension #1.

## 2. The world reads as a place — layout + generation upgrades (all data-driven)

2.1 **Fix the channel spine** — world bible mandates a north–south channel with three
    bridge crossings, visible from the south entrance; the built layout runs it
    east–west with zero bridges. Rotate, sink it ~0.6 m, add 3 bridges, move the
    sluice-puzzle objects onto it. Pure JSON; the composition everything else sits on.
2.2 **`LayoutKits`** — a `"kit"` schema field expanding one JSON entry into a building
    (base, walls, pitched slate roof, chimney, door inset, porch; Orrel keeps the only
    two-storey silhouette). Shared expander used by BOTH generators (also de-dupes
    `Primitive`/`MakePrimitive`). ~100 extra static draws — fine.
2.3 **Feed the wired-but-unfed texture pipeline** — `RuntimeArt` already tries
    `Resources/Art/Textures/mat_*.png` and warns every boot; zero textures exist.
    Procedurally-generated tileables for all palette keys. Highest visual delta per
    hour in the repo.
2.4 Terrain relief + path network: split the billiard-table ground into offset/tilted
    plates (stepOffset absorbs seams); `paths` schema laying quad-strips entrance →
    plaza → landmarks → climb.
2.5 Seeded `scatter` schema (tufts/stones/sedge; no colliders, no shadows, capped
    ~2,000 instances, instanced or spawn-combined — the one item with real budget risk;
    measure before raising caps).
2.6 **Vista framing at the climb**: notch the cliff at the landing (village overlook
    with roofs + kiln smoke below); frame the pool between tree pairs at the top; put
    the scorched-bark tree ON the sightline. Nearly free; biggest sense-of-place
    per edit.
2.7 Landmark silhouettes: kiln chimney + smoke particle column (narratively
    load-bearing — smoke presence/absence is story signal), Coll's half-buried gear,
    Orrel's roofline. `fx` schema field.
2.8 **Water**: transparent + UV-scrolled flow (`flow` field), foam kerbs at the drop,
    dark basin under the drained pool. Functionally load-bearing: the drowned lens
    (the one undocumented solution) must be *visible through the surface*. No depth
    texture on TBDR — fake the shore.
2.9 `zones` schema for local mood: upper shelf cooler and hushed; **lower shelf warm**
    (amber ambient, ember light on the second kiln, ash ground) — the mystery's answer
    should be feelable before it's stated.
2.10 **Quietday as a rendering state**: flat grey light, wind → zero (eerie stillness),
    desaturation, sparse ash-fall on the eastern leaves — clue 1 is currently only
    text. The moment rendering and narrative touch; this is the identity shot.

## 3. Alive, not animated — characters, feel, presentation

3.1 **Cue effect in Core** (`{"type":"cue","id":"sting.mystery"}` → transient
    `PendingCues`, excluded from save; Unity `CueRouter` drains on `Changed`).
    One tiny effect type lets CONTENT author stingers, camera pushes, Pip reactions,
    weather — and deletes hack 0.5. Prerequisite for half this section.
3.2 **Procedural capsule animator** — lean-into-acceleration, speed-driven gait bob +
    roll, jump stretch / landing squash (about the base, not center), idle breathing.
    `PlayerController.NormalizedSpeed` already exists unconsumed. The single biggest
    "alive" win at grey-box, and the animation contract for real rigs later.
3.3 NPCs face the player in conversation (both turn), per-waypoint idle yaw + ambient
    head-turn within ~4 m; arrival idles instead of freezing.
3.4 **Pip lives**: offset swings with player speed, moth-flutter (two incommensurate
    sines, not one), occasional perch near readables, lamp flicker whose amplitude
    grows with `Dimness` (distress you can see), settles calm during dialogue; the
    dialogue's "light goes thin" line finally matches the light. Additive glow-halo
    quad driven by `1−Dimness`. No second realtime light.
3.5 Camera: sprint FOV +4–6° + distance kick, travel look-ahead (~0.6 m), dialogue
    two-shot framing (pivot to midpoint, tighten ~30%) — the always-look-at-pivot
    invariant survives, so the Phase 1 gate does; re-run the adversarial pass after.
3.6 **iOS haptics** (tiny CoreHaptics bridge): light on interact/advance, medium on
    pickup, success on quest complete, tick on landing. Highest polish-per-hour on
    a phone; zero frame cost.
3.7 Footsteps synced to gait phase (event from 3.2), not a timer; pooled dust-puff on
    landing/sprint steps.
3.8 Dialogue presentation, IMGUI now, survives the Phase 4 reskin: punctuation pauses
    in the typewriter, per-character voice blips pitched per speaker, world-dim behind
    the panel, portrait beside the name, 0.12 s panel ease-in. Plus `Screen.safeArea`
    on all HUD rects TODAY (clock/tracker sit under the notch in landscape).
3.9 **Toast queue** for quest updates / clue found / capability grants, paired with the
    existing stings + haptics so one beat hits ear, eye, and hand on the same frame.
    Clue titles are already good copy nobody sees.
3.10 Mystery on-ramp (pure JSON): pre-quest Pip entry drifting east + one village
    readable pointing at grey mornings — protects the hook test from ordering luck.

## 4. The art tier (when capsules die — Phase 3 proper)

- One shared humanoid rig, 3 variants by silhouette (Vesk broad+apron, Orrel tall —
  echo her two-storey house, Coll stooped), ≤3k tris, one atlas, FOUR clips: idle,
  walk, talk-gesture, per-NPC occupation idle (kiln-tending / writing / listening).
  **No root motion** — movement stays code-owned (no NavMesh exists to reconcile).
- Pip: ~200-tri moth, 2-bone code-driven wing flap (rate rises with `Dimness`),
  emissive body.
- Shared custom shader family: water + wind vertex sway (canopy; Quietday zeroes it)
  + world-Y moss/wetness blend on stone ("overgrown cut stone" from the same cubes).
  Register in Always Included Shaders (`ProjectSetup`) — runtime materials otherwise
  strip from builds.
- Phase 4 UI skin spec (decided now, built then): warm-paper panels, ledger typography
  (the fiction hands us the visual language), 9-slice + 200 ms eases, thumb-height
  choice rows; `ContextAction`/`Locked` input contract untouched.

## Frame-budget ledger (iPhone 13, 16.7 ms)

Adds: atmosphere+grading ≈1.0–1.5 · water/wind/moss shaders ≈0.3–0.6 · smoke+ash
particles ≈0.2–0.4 · scatter ≈0.3–0.5 (capped) · animators/haptics/cues ≈0.1.
Recovers: shadow tuning ≈0.5–1.0 · far-plane cut · GC-hitch fix (0.3) removes 5–20 ms
*spikes*. Net steady-state ≈ +1.5–2 ms on today's ~70-primitive scene — inside budget,
but every line is a claim until `FrameTimeHud` confirms it on the device. Any change to
what a *scene* contains passes the `BuildCommand.macOS` smoke test first (landmine #1).

## Order of battle

1. §0 fixes (mostly S; 0.1/0.2 are correctness, not polish) → device pass.
2. Phase 1 gate work — movement/camera tuning (3.2, 3.5 feed directly into it).
3. §1 atmosphere build + 2.1 channel + 2.3 textures → this is the week the game stops
   looking like a prototype.
4. §2 remainder + §3 remainder, interleaved with the interest-density walk (2.1/2.4/2.6
   change every route — walk after, per the gate).
5. §4 art tier + Phase 4 skin — after the Phase 2 gate is green on device and the
   derivativeness pass re-runs on the assembled look (silhouette risk lives there).
