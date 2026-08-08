# RELEASE-CANDIDATE PLAN

Written 2026-08-08. Target: the mission's definition of done — **TestFlight build +
recorded uncut 25-minute playthrough by a non-builder + systems README + phase log with
every gate cleared or waived.** Deadline pressure: kill criterion 4 parks the project on
1 September if the Phase 2 gate is not cleared — **24 days out.** Everything below is
ordered so the gates clear earliest and art lands last (art on top of cleared gates,
never under them).

Task tags: [C] = code/agent work · [O] = owner-only (device in hand, decisions, money)
· [G] = protocol gate · (S/M/L) = effort.

---

## M0 — Stabilize what exists (now; 1 session)

- [ ] 0.1 [C] Commit the in-flight working tree (LayoutSpawner, RuntimeArt, GameAudio,
      VO assets, CLAUDE.md, roadmap docs) on a reviewed branch; phase-log entry. (S)
- [ ] 0.2 [C+O] Full-slice device verification: build Heartwood via `tools/build-ios.sh`,
      confirm it boots past LayoutSpawner on the iPhone, play 10 min. **This is the
      first true device pass of the full slice** — record worst frame time, memory,
      save/reload, in the phase log. (S–M)
- [ ] 0.3 [C] §0 correctness fixes from `docs/aaa-roadmap.md`:
      save actually loads on launch (0.1) · crafting panel consuming `ui.open_crafting`
      (0.2) · GC-hitch removal in GameHud (0.3) · async VO prewarm (0.4) ·
      cue-effect groundwork replacing the substring sting hack (0.5) · duck fix (0.6) ·
      Pip talk-prompt + stage-direction VO cleanup (0.7) · single-source layout JSON
      (0.8). (M total)
- [ ] 0.4 [C] Add `ContentValidator` rules that would have caught 0.2: flags set but
      never consumed; VO mapped to narration lines. (S)
- [ ] 0.5 [C+O] Re-run device pass after fixes; commit; merge branch to main. (S)

## M1 — Phase 1: traversal feel (this week) [G]

- [ ] 1.1 [C] Feel upgrades that feed the gate: procedural capsule animator (lean, bob,
      landing squash), camera sprint FOV/distance kick, travel look-ahead. (M)
- [ ] 1.2 [O] Tune on device: walk the greybox room, adjust the serialized feel fields
      (accel/decel/turn, camera lags) until movement is *good*, not acceptable. (M)
- [ ] 1.3 [O] **GATE:** two people who didn't build it, no instructions, moving
      competently within 15 s; 5 min of adversarial camera abuse — zero clips, never
      loses the player. Record in phase log. Fail → stay in Phase 1. (S)

## M2 — Phase 2 gate on device (before 1 Sept — hard deadline) [G]

- [ ] 2.1 [C] Prepare the live demo: `docs/phase2-gate/npc4.json` drops into
      StreamingAssets on a build; NPC #4 spawns, quest runs, save/reload resumes
      mid-step — all in the running game on the phone. (S — the data half is proven)
- [ ] 2.2 [O] **GATE:** perform it, confirm the C# diff is empty, record. (S)

## M3 — The visual transformation (the "atmosphere week")

- [ ] 3.1 [C] `AtmosphereRig`: gradient sky + fog(=horizon) + trilight ambient from one
      palette table; far-plane 300→120. macOS smoke test (landmine #1) then device. (M)
- [ ] 3.2 [C] Time-of-day: sun/fog/ambient/sky lerped from `Clock.NormalisedTime`,
      keyframes in JSON; sun stepped per game-minute. Dawn must *look* like dawn —
      the head-gate puzzle depends on it. (M)
- [ ] 3.3 [C] Post volume: grading (wet-stone: desat, lifted cool shadows) + vignette;
      no bloom/DoF; measure ~1 ms cost on device, renderScale 0.9 fallback. (S)
- [ ] 3.4 [C] Deliberate shadows: 2 cascades, 2048, 40 m fade, bias; actor blob
      shadows. Recovers budget for 3.3. (S)
- [ ] 3.5 [C] Single palette table (kill the duplicate), retuned wet-stone + new keys
      (slate/stone/ash/earth/lamp) + per-key smoothness. (S)
- [ ] 3.6 [C] Generate the tileable texture set feeding `RuntimeArt`'s already-wired
      loader (currently zero textures exist). (S–M)
- [ ] 3.7 [O] Device pass: full slice, all of M3 on, worst-frame vs 16.7 ms. (S)

## M4 — The world reads as a place

- [ ] 4.1 [C] **Channel spine fix** — rotate N–S per world bible, sink it, 3 bridges,
      sluice objects repositioned onto it. Pure JSON. (M)
- [ ] 4.2 [C] `LayoutKits` building vocabulary (house/kiln/gearhouse/bridge kits;
      Orrel's the only two-storey). De-dupes the two primitive generators. (M)
- [ ] 4.3 [C] Terrain relief plates + `paths` schema (entrance→plaza→landmarks→climb). (M)
- [ ] 4.4 [C] Vista framing: cliff notch overlook at the climb landing; pool framed at
      the top; scorched-bark tree on the sightline. (S)
- [ ] 4.5 [C] Landmark silhouettes + kiln smoke column (`fx` schema). (M)
- [ ] 4.6 [C] Water: transparent, flowing, foam at the drop, dark basin — drowned lens
      visible through the surface (the undocumented solution depends on it). (M)
- [ ] 4.7 [C] Seeded `scatter` schema, capped + instanced; measure before raising. (M)
- [ ] 4.8 [C] `zones` mood schema: hushed upper shelf; warm lower shelf (ember light,
      ash ground). (M)
- [ ] 4.9 [C] Quietday rendering state: grey light, zero wind, ash-fall east. (M)
- [ ] 4.10 [O] **Interest-density walk** (Phase 3 gate precondition): every path,
      stopwatch, no 40 s dead stretch; fix by layout JSON edits, re-walk. (M)

## M5 — Alive layer + content completeness

- [ ] 5.1 [C] Cue router + content-authored cues in the mystery/quests (stingers, Pip
      reactions, screen moments). (M)
- [ ] 5.2 [C] NPC conversation facing + waypoint idles + head-turn. (S)
- [ ] 5.3 [C] Pip life: velocity-lag follow, flutter, perch, Dimness-driven gutter +
      glow halo, dialogue settle. (M)
- [ ] 5.4 [C] Haptics bridge + call sites (interact/pickup/complete/landing). (M)
- [ ] 5.5 [C] Gait-synced footsteps + landing dust (pooled). (S)
- [ ] 5.6 [C] Dialogue presentation: punctuation pauses, per-char blips, world-dim,
      portraits, panel ease; safe-area fix for all HUD rects. (M)
- [ ] 5.7 [C] Toast queue for quest/clue/capability beats, paired with stings+haptics. (S)
- [ ] 5.8 [C] Mystery on-ramp JSON (pre-quest Pip eastward drift + village readable). (S)
- [ ] 5.9 [C] Dialogue copy polish pass over all NPC lines against the world bible's
      voice notes; re-generate VO for changed lines. (M)

## M6 — Phase 3 gate [G]

- [ ] 6.1 [O] Full slice start-to-finish on device: no crash, no soft-lock, frame
      budget held with the village fully populated. (M — play time)
- [ ] 6.2 [O] **GATE:** the interest-density walk passes (4.10 re-run on the final
      layout). Record. Fail → layout fixes, not content volume. (S)
- [ ] 6.3 [C] Derivativeness pass 02 on the assembled look (palette + silhouettes
      together — the highest remaining IP risk). Record; change what answers cleanly. (M)

## M7 — Art tier (Phase 3 art pass; gated on [CONFIRM] #5)

- [ ] 7.1 [O] **Decide [CONFIRM] #5**: procedural/generated art (current trajectory) vs
      asset-store base + custom shading vs commissioned. The plan below assumes
      generated + shader-driven; a different answer reshapes M7 only. (decision)
- [ ] 7.2 [C] Shared shader family: water + wind sway (Quietday zeroes it) + world-Y
      moss/wetness on stone; Always-Included registration. (M)
- [ ] 7.3 [C/O] Characters: one rig, 3 silhouette variants, 4 clips each, no root
      motion; Pip moth with code-driven wings; player silhouette distinct at 6 m. (L)
- [ ] 7.4 [C] Retire billboards; swap capsule spawns for prefabs keyed by the same
      content ids (data-driven preserved). (M)
- [ ] 7.5 [O] Device pass with all characters + art in the populated village. (S)

## M8 — Phase 4: polish + the hook test [G]

- [ ] 8.1 [C] Phase 4 UI skin per the decided spec (warm paper, ledger type, 9-slice,
      eased transitions; input contract untouched). (L)
- [ ] 8.2 [C] Audio completeness: phase-crossfaded ambience beds, water proximity bed,
      night bed; VO QA listen-through. [CONFIRM] #7 close-out. (M)
- [ ] 8.3 [C] Save robustness: mid-quest kill/relaunch matrix, stale-content load,
      low-storage write failure handling. (M)
- [ ] 8.4 [C] Low-end fallback: renderScale/shadow tier for older devices; iPhone 13
      stays the measured floor. (S)
- [ ] 8.5 [C+O] 30-minute beat sheet timed against two real sessions. (M)
- [ ] 8.6 [O] **GATE — the hook test:** five first-time testers, ≥2 children 8–12,
      unassisted, observed; ≥3 of 5 spontaneously ask about the mystery or ask to
      keep playing. Fail → rework the mystery's information design, retest; do NOT
      build new content. (M)

## M9 — Release candidate

- [ ] 9.1 [O] Apple Developer Program enrollment ($99/yr — the free personal team
      cannot ship TestFlight). App Store Connect app record. (S + wait)
- [ ] 9.2 [O] **Decide [CONFIRM] #8 — the title.** "Hidden Valley" carries a trademark
      collision and a Stardew-adjacent read; blocks store listing, bundle id, icon. (decision)
- [ ] 9.3 [C] Release build config: signing for distribution, icon + launch screen
      (derivativeness-checked), privacy manifest, `FrameTimeHud` off by default with a
      3-finger dev toggle, build number automation in `build-ios.sh`. (M)
- [ ] 9.4 [C+O] Upload to TestFlight; internal test on ≥2 devices; fix what surfaces. (M)
- [ ] 9.5 [O] **The recorded playthrough:** one uncut 25-minute run by someone who did
      not build it, screen-recorded on device. If they finish and the recording shows
      the slice working end to end, that video is the RC's proof artifact. (S)
- [ ] 9.6 [C] Docs close-out: systems README current (add layout kits/zones/cues
      authoring), phase log shows every gate ✅/waived, world bible provenance intact. (S)
- [ ] 9.7 [C+O] Tag `rc-1` on main. (S)

---

## Open decisions that block specific milestones (owner)

| # | Decision | Blocks |
|---|---|---|
| [CONFIRM] #4/#5 | Artist? Art pipeline? | M7 shape |
| [CONFIRM] #6 | Commercial / demo artifact / personal | RC messaging, 9.1 urgency |
| [CONFIRM] #8 | Title | M9 store listing, icon, bundle id |
| New | Apple Developer Program purchase | 9.1, hard TestFlight dependency |

## Sequencing logic

Gates before beauty: M1/M2 clear the two cheapest gates while the calendar is friendly
(kill criterion 4 = 1 Sept). M3+M4 are the perceived-quality transformation and feed
M6's gate directly. M5 rides in parallel wherever hands are free. M7 art lands on a
world whose layout already passed its walk — re-laying-out finished art is where
projects die (mission §resources). M8's hook test is deliberately late: it tests the
finished feel, and its kill criterion (rework mystery, not add content) needs
everything else stable to be meaningful. M9 is mechanical once the gates are green.
