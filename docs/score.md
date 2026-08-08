# SCORECARD — the game on a 1–10 scale

Rubric fixed here so scores are comparable across passes. Each dimension is scored
against "a polished small commercial mobile game" as 10, from evidence only
(screenshots, device telemetry, code inspection, test results) — never intention.
Overall = weighted mean (visual ×2, feel ×2, others ×1).

## Baseline — 2026-08-08, after the M0–M7 run (pre-loop)

| Dimension | Score | Evidence and what's holding it down |
|---|---|---|
| Visual | 4.5 | Atmosphere/fog/time-of-day genuinely good; but trees are bare trunks (no canopies), buildings are sharp-edged boxes, ground plates visibly seam, water is a flat scrolling slab, and the dawn screenshot shows large empty mid-ground areas |
| Game feel | 6 | Animator + camera kicks + haptics + gait audio are real; untuned on device (the feel fields have never been touched by a human hand at 60fps) |
| World-alive | 6 | NPCs walk/face/glance, Pip flutters, smoke rises, grass sways, Quietday stills it — strong for the tier; no ambient critters, no window light at night |
| Content/writing | 6 | 8 shaped quests, real mystery structure, undocumented solution; dialogue is competent but first-draft — few lines would be quoted back |
| UX | 5.5 | Paper skin is coherent; but default IMGUI font, no settings, no pause, account has no scrolling (long clue list clips), toggles are small |
| Audio | 5 | SFX/VO/stings/ambience beds exist; one ambience loop, no night bed, no UI variety, VO unreviewed by ear |
| Performance | 5 | 30fps cap found and fixed; 60fps verdict UNMEASURED (phone unplugged) — score capped until measured |
| Cohesion | 7 | The wet-stone identity holds across palette, sky, paper, writing — the strongest dimension |

**Overall: 5.4 / 10.**

## Pass log

Scores only move with evidence attached (screenshot, telemetry, or test).

### Pass 1 — trees, windows, props (2026-08-08, screenshot-verified)

Canopies, window panes, and plaza props landed and read clearly at dawn.
Defects found by the same screenshot: scatter tufts float above tilted terrain
plates; Mac 1%-high crept to 16.8 ms (more draws + sway writes).

Visual 4.5 → **5.5**. Others unchanged. **Overall 5.4 → 5.6.**

### Pass 2 — scatter grounding + sway trim (screenshot-verified)
Tufts raycast-snapped to plates; sway cost trimmed. Steady 8.2/9.4ms on Mac.
Performance 5 → **5.5** (Mac evidence; device 60fps still pending). Overall 5.6.

### Pass 3 — swifts + scrolling panels + thumb targets (screenshot-verified)
Seven banking swifts over the village (absent on Quietdays — absence as
information); Bag/Account/Crafting scroll; toggles enlarged.
World-alive 6 → **6.5**, UX 5.5 → **6**. **Overall 5.9.**

### Pass 4 — synthesized night audio + gusts (boot-log-verified)
amb_night (brown-noise floor + cricket chirp trains, seamless loop) crossfades
in at dusk/night; sparse daytime gusts, silenced by Quietday stillness.
Audio 5 → **6**. **Overall 6.0.**

### Pass 5 — time-of-day dev hook, emissive windows, night verified (screenshots)
`-hvminute` launch arg for photographing any hour; lamp material gains emission
after the first night shot showed dark facades. Night re-shoot: lit windows
across the village, Pip's pool of light on the path — the identity image.
Visual 5.5 → **6**, cohesion 7 → **7.5**. **Overall 6.2.**

Honest ceiling note: with procedural primitives + lighting, visual tops out
near 7; feel and performance need the human device pass to move further; the
hook test alone arbitrates anything above ~8. The loop continues inside those
limits and states them rather than inflating.

### Pass 6 — writing audit (evidence correction, no changes needed)
Read every readable, clue, and tracker line intending to polish; found them
strong ("The number is the point, not the moss", "the way you cover a fire you
intend to come back to"). Baseline content score was set on thin evidence.
Content 6 → **7** (correction). **Overall 6.3.**

## Standing of the loop

6.3, with the ceilings stated in pass 5: the next points live behind (a) the
device pass and feel tuning (feel, performance), (b) the played gates (anything
above ~8 is the hook test's to award, not this scorecard's), and (c) art beyond
procedural primitives if visual is to pass ~7. The loop resumes the moment any
of that evidence lands — the scorecard only moves on evidence, in both
directions, and 10 is reachable only through the same human gates the release
plan already names.

### Pass 7 — interact pulse (build-verified)
Successful interactions answer with a squash pulse on the touched object —
tap now lands in ear, hand, and eye. Feel 6 → **6.25**. **Overall ≈ 6.4.**

### Pass 8 — the painted cast (Nano Banana Pro, screenshot-verified in-game)
Player (hooded traveler), Vesk, Orrel, Coll and moth-Pip generated with
gemini-3-pro-image-preview in one gouache style contract, alpha-cut via
border flood-fill, shipped as lit cutout billboards (Don't Starve stance) at
per-character statures. Defeated en route: the URP alpha-test variant
stripping (cutout material must be a baked asset, not runtime-built).
Sprite pipeline is repeatable: docs/systems-README + scratchpad prompts.
Visual 6 → **7**. **Overall 6.5.**

### Pass 9 — HDR bloom + blob shadows (night screenshot-verified)
Pip's moth carries a true bloom halo; windows glow soft across the dark;
painted characters grounded by blob shadows. Watch item: Mac 1%-high hit
17.6 ms with HDR — the device re-measure decides if renderScale gives it back.
One hot lamp pane to tame. Visual 7 → **7.5**. **Overall 6.6.**

### Pass 10 — painted surface tileables (screenshot-verified on the kiln)
Nano-Banana-painted plaster, slate shingle and coursed masonry detail maps,
contrast-normalized into the tintable band so the palette keeps owning hue
across day/night. Buildings gain hand-painted surface without breaking the
color system. Visual holds 7.5 (texture detail arrived; awaiting a clear
full-frame verify for more). **Overall 6.6.**
