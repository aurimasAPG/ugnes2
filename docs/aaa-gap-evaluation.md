# Hidden Valley — AAA studio gap evaluation

**Date:** 2026-08-07  
**Lens:** What a shipping AAA team would reject as “not yet a real product,” vs what is already sound for a **vertical slice / prototype**.  
**Scope honesty:** The mission defines a 25-minute TestFlight slice, not a full AAA title. This doc separates **slice-legitimate** gaps from **AAA-catalogue** gaps so opportunity cost stays clear.

---

## Executive verdict

The project is a **credible systems prototype** with a rare strength: data-driven content (NPC/quest/item as data only) and a closed Core rules engine that is unit-tested. It is **not** yet a AAA-quality game experience. The largest deltas are **presentation fidelity** (mesh/animation/lighting/audio mix), **feel polish** (camera/movement/VFX), **content density production values**, and **production pipeline** (tools, profiling, live ops). Closing every AAA gap would cost multiple teams × years; closing the **top slice gaps** below is the rational path to a fundable / shippable vertical slice.

---

## What already holds water (keep)

| Area | Assessment |
|---|---|
| Architecture | Core without UnityEngine; content validator; Phase 2 data-driven NPC path |
| Scope discipline | One hub, one forest, 3 NPCs, 8 quests, one mystery — matches brief |
| Systems design | Capability-differentiated NPCs; world flags instead of hardcode shops |
| Build loop | One-command iOS/Mac path; level0 corruption root-caused |
| Test harness | 30+ Core tests including playthrough / save / Phase 2 gate |

---

## Critical gaps vs AAA (priority order for *this* slice)

### P0 — Blocks “this feels like a game”

1. **No authored 3D characters / animation**  
   Capsules + billboards are prototype language. AAA expects skinned meshes, idle/walk cycles, facial or at least head-look, interaction poses.  
   *Mitigation for slice:* better billboards + simple sprite flipbook walk; or asset-store base rigs.

2. **Environment art is grey-box + tiled albedos**  
   AAA: modular kits, LODs, occlusion, material layering, VFX water, foliage wind.  
   *Slice bar:* consistent kitbash village + 1 hero landmark per major beat.

3. **Audio is stub-grade**  
   Beeps ≠ SFX design; no dialogue VO; no mix bus; no music/ambience stems; no spatialization.  
   *This session:* ElevenLabs VO for dialogue lines + stronger SFX; still not a full mix.

4. **UI is IMGUI debug**  
   AAA: designed HUD, accessibility, controller glyphs, localization, narrative letterboxing.  
   *Slice bar:* one skin pass (panel, fonts, icons) without rewiring.

5. **Camera / movement still prototype feel**  
   Recent spin fix helps; AAA still wants collision camera polish, aim assist, coyote time, stick curves, photo mode, etc.

### P1 — Blocks “this feels finished”

6. **No animation of the world** — doors, water, kiln heat, Pip moth wing cycle, residue ash.  
7. **No lighting art direction** — single sun; no time-of-day *visual* response; no interiors.  
8. **No juice** — hit/interact feedback, camera punch, particles, screen-space effects.  
9. **Dialogue presentation** — no typewriter, no lip sync, no history log, no skip/auto, no subtitles options.  
10. **Save UX** — auto-save only; no slots, no cloud, no “last played” card.  
11. **Onboarding** — no first-run coach marks; brief assumes competence.  
12. **Performance budget proof** — frame-time HUD exists; no locked 60 on device with populated scene (Phase 0/1 gates incomplete for device pass record).

### P2 — AAA catalogue / live product (out of slice scope, still “missing”)

13. Full animation graph, cinematics, photo mode  
14. Multi-region open world, streaming, NPCs schedules at scale  
15. Combat/stealth/economy depth (not required by brief — don’t add)  
16. Multiplayer, UGC, battle pass, live ops  
17. Localization, age ratings, platform certification suite  
18. Accessibility (colorblind, remapping, text scale, screen reader)  
19. Analytics, crash reporting, A/B remote config  
20. Marketing trailers, store page, press kit, age rating boards  

---

## Sound & voice — AAA bar vs this slice

| AAA expectation | Current / target this session |
|---|---|
| Recorded or high-end TTS VO per line | ElevenLabs VO per unique dialogue line, speaker-cast voices |
| Designed SFX library + layers | Procedural + generated SFX; not yet Foley-rich |
| Music + adaptive stems | Missing (non-goal unless brief expands) |
| Ambisonic/ambience beds | Missing |
| Mix: ducking dialogue under SFX | Minimal; single bus |
| Middleware (Wwise/FMOD) | Not used; acceptable for slice if clips load reliably |

---

## Art — AAA bar vs this slice

| AAA expectation | Current / target this session |
|---|---|
| Concept → model → LODs → materials | Still 2D textures on primitives |
| Character turnarounds & consistency | Nano Banana Lite refresh of portraits |
| Lighting & post | URP defaults |
| VFX graph | None |

---

## Minimum path to “legit vertical slice” (not full AAA)

1. VO + designed SFX + one ambience loop + simple music sting  
2. One art pass on Heartwood kit (still grey-box scale, readable materials)  
3. Character billboards or low-poly rigs with walk cycle  
4. Non-IMGUI HUD skin  
5. Device pass recorded 10 min @ 60 fps budget  
6. Blind 25-min playtest with the mystery hook question  

That is a **shippable vertical slice**, not AAA catalogue quality. Claiming AAA without P0–P1 is reputation risk.

---

## Session deliverables mapped to gaps

- **ElevenLabs:** dialogue VO for all unique content lines; speaker-differentiated casting  
- **Nano Banana Lite:** upgraded character portraits (player, Pip, Vesk, Coll, Orrel)  
- **Wiring:** GameAudio plays VO by speaker+line hash; fallback to short cue if missing  
- **Docs:** this evaluation  


---

## Progress since evaluation (2026-08-08)

Closed or partially closed for the slice:

- **VO:** 72 ElevenLabs dialogue lines, speaker-cast, wired via voice_map.
- **SFX:** ElevenLabs footstep/jump/interact/pickup/UI; water cue.
- **Ambience / stings:** `amb_valley` loop, `mus_sting_mystery`, `mus_sting_quest`.
- **Art:** Nano Banana Lite portraits + surface textures (grey/moss/water/bark/sand/dark).
- **Dialogue UI:** typewriter reveal; speaker display names; VO stop on advance; ambience duck under speech.
- **Movement:** spin/no-stop fixes retained (wish facing, independent camera yaw, keyboard zero).

Still open for a finished slice: skinned animation, non-IMGUI HUD, environment kit art, device 60fps pass, blind playtest.
