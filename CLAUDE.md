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
