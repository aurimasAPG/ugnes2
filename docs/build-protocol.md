# HIDDEN VALLEY — BUILD PROTOCOL

Operating rules referred to by `/CLAUDE.md`. Sections are numbered to match the
references in the mission (§2 phase gates, §3 the three passes, §4 kill criteria).

---

## §2. PHASE GATES

Each phase has a **done-state that is observable** (a build, a measurement, a
recording — never "the system is implemented") and a **gate that is a yes/no
question**. A failed gate does not mean try harder; it means take the stated
rescope action.

### Phase 0 — Foundation spike

Repo, engine version, render pipeline configured for mobile, input system, one
grey-box scene, on-device build pipeline working end to end.

- **Done:** a grey-box room installed and running on the physical target device,
  with touch joystick, follow camera, and an on-screen frame-time readout.
- **Gate:** does a build reach the device in under 10 minutes, from a one-line
  command?
- **Fail →** fix the build pipeline before anything else. A slow build loop
  compounds across every later phase and is the most expensive thing to leave
  broken.

### Phase 1 — Traversal feel

Movement acceleration/deceleration, slope and step handling, camera smoothing,
camera-collision avoidance, sprint, jump. No art. No systems.

- **Done:** five minutes of grey-box movement on device; two people who did not
  build it are handed the phone with no instructions and both move competently
  within 15 seconds.
- **Gate:** in five minutes of deliberately adversarial camera testing — backing
  into corners, spinning against walls, running down slopes — does the camera
  never clip geometry and never lose the player?
- **Fail →** stay in Phase 1. This is a genre where the player does nothing but
  move for hours; movement that is merely acceptable caps the entire project's
  ceiling, and it cannot be fixed later without touching everything built on top
  of it.

### Phase 2 — Systems skeleton, data-driven

Interaction, dialogue, inventory, quest tracking, save/load, time-of-day.
Placeholder content only.

- **Done:** a live demonstration in which a new NPC with a new two-step quest and
  a new inventory item is added to the running game **by editing data files only**,
  with the C# diff empty, then saved, reloaded, and the quest resumes at the
  correct step.
- **Gate:** was that C# diff genuinely empty?
- **Fail → stop all content production and refactor.** See kill criterion 2.
  Building 20 NPCs on a system that requires code per NPC is the exact failure the
  original brief's architecture section was trying to prevent, and it is invisible
  until it is catastrophic.

### Phase 3 — Content and first art pass

Heartwood Village, forest region, Pip, the 3 NPCs with schedules, the 8 quests,
the puzzle chain, the mystery thread and investigation board.

- **Done:** the full slice playable start to finish without a crash or a
  soft-lock, at a locked frame budget with the village fully populated.
- **Gate:** does the interest-density walk pass — every path, stopwatch, no
  40-second dead stretch?
- **Fail →** fix by layout and placement, not by adding content volume. Dead
  stretches are almost always a level-design problem wearing a content-shortage
  costume.

### Phase 4 — Play-feel and device polish

Audio, particles, transitions, save robustness, low-end device fallback, the
30-minute beat sheet timed against real sessions.

- **Done:** five first-time testers, at least two of them children in the 8–12
  band, complete the slice unassisted while being observed and timed.
- **Gate:** at the end, do at least three of the five spontaneously ask a question
  about the mystery, or ask to keep playing, **without being prompted**?
- **Fail →** see kill criterion 3. Do not add regions.

---

## §3. THE THREE PASSES, DEFINED

"Polish" is not a pass. These are. Run all three before calling any phase done.

### 1. The device pass (verification)

Build to the physical device. Play the affected content for ten unbroken minutes.

Record:

- worst frame time, **not average**
- peak memory
- any null reference or exception in the log
- whether save-and-reload restores exact state

The lie this pass catches is "it works," which in games always means "it worked in
the editor on a desktop GPU."

### 2. The play-feel pass (strength)

Play the affected content with a stopwatch and the beat sheet open.

Record:

- time to first interaction
- longest gap between interesting things
- whether each quest's next step was obvious without opening the tracker

This pass is done *by playing*, and its output is timings — an assertion that
something "feels good" is not a completed pass.

### 3. The derivativeness pass (de-AI / sharpness)

Take every name, mechanic, UI arrangement and character silhouette produced in the
phase and ask what specific existing game it came from. Anything that answers
cleanly gets changed.

This is simultaneously the IP-safety check and the originality check, and it is the
pass most likely to be skipped, because its findings are always inconvenient.

---

## §4. KILL CRITERIA

An agent with no stop condition either over-plans or over-builds. These are the
stop conditions.

1. **Frame budget.** If Phase 1 cannot hold the target frame rate on the target
   device with a representative scene, stop and rescope fidelity — fewer dynamic
   lights, baked shadows, a smaller draw distance, or a fixed-angle camera. Do not
   proceed into content. Content authored against a frame budget you cannot afford
   has to be rebuilt, and rebuilding content is the single largest cost in this
   project.

2. **The data-driven gate.** If Phase 2's C# diff is not empty, content production
   stops until it is. This is not a preference; the difference between 3 NPCs and
   20 NPCs is entirely a question of whether NPC #4 costs an afternoon or a sprint.

3. **The hook test.** If fewer than three of five testers spontaneously want to
   continue at the end of the slice, the mystery system is broken and no amount of
   additional region content will fix it. Rework the mystery's information design —
   what the player knows, when, and what question it leaves open — and retest
   before building anything new.

4. **The calendar.** If the Phase 2 gate is not cleared by 1 September, park the
   project until 6 October. September is EcomExpo run-up; a half-built game held in
   working memory through a conference launch is worse than a paused one, because
   the context cost of resumption is lower than the cost of a distracted launch.
