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
