# HIDDEN VALLEY — PHASE LOG

The running record. Read this first. A pass that is not recorded here did not
happen; a gate that is not marked here is not cleared.

**Current state:** Phase 0 — **not started.** Blocked on `[CONFIRM]` items 1, 3,
4, 5, 7 and on toolchain availability (see Blockers).

| Phase | Gate | State |
|---|---|---|
| 0 — Foundation spike | Build reaches device in <10 min from one command? | ⬜ Not started |
| 1 — Traversal feel | Camera never clips, never loses player, over 5 min adversarial? | ⬜ Not started |
| 2 — Systems skeleton | Was the C# diff genuinely empty? | ⬜ Not started |
| 3 — Content + first art | Interest-density walk passes, no 40s dead stretch? | ⬜ Not started |
| 4 — Play-feel + polish | ≥3 of 5 testers spontaneously want to continue? | ⬜ Not started |

---

## `[CONFIRM]` — open decisions

Seven answers are needed before Phase 0. Per the mission, these are logged rather
than invented. One is now answered.

| # | Decision | State |
|---|---|---|
| 1 | **Engine and version.** Unity 6 LTS + URP assumed by the brief's architecture. Godot 4 is a legitimate, cheaper alternative for a solo mobile build. Unreal is the wrong tool. | `[CONFIRM]` |
| 2 | **Repository.** | ✅ **Answered 2026-08-07** — `aurimasapg/ugnes2`, greenfield. Single initial commit, `LICENSE` + stub `README.md`, no engine project, no prior source. |
| 3 | **Target device floor.** Every performance number in the mission is meaningless without it. iPhone 13 is a reasonable modern floor; iPhone SE 2nd gen means a materially different art budget. | `[CONFIRM]` |
| 4 | **Who is building.** Claude Code solo, Claude Code plus a contract artist, or a team? Determines whether art is a phase or a dependency. | `[CONFIRM]` |
| 5 | **Art pipeline.** Asset-store base meshes with custom shading and custom characters, or fully bespoke? Largest single cost driver in the project. | `[CONFIRM]` |
| 6 | **What this is for.** Commercial product, demonstration artifact (Team of Agents book / conference stage), or personal build? The stakes sentence in the mission is written for the honest default — opportunity cost — and should be replaced if a real audience or deadline is attached. | `[CONFIRM]` |
| 7 | **Audio.** Licensed library versus commissioned original. Original audio direction across seven biomes is a commission, not a task. | `[CONFIRM]` |

---

## Blockers

### B1 — No game toolchain in the execution environment (2026-08-07)

The agent environment is Linux x86_64. Verified absent: `unity`, `unity-editor`,
`godot`, `dotnet`, `mono`, `csc`, `xcodebuild`. There is no macOS host, no Xcode,
no code-signing identity, no TestFlight access, and no physical iOS device.

Consequence, stated plainly rather than worked around:

- **Phase 0's gate cannot be executed here.** It requires a build reaching a
  physical device.
- **Phase 1's gate cannot be executed here.** It requires on-device adversarial
  camera testing and two human testers.
- **Phase 3 and 4 gates cannot be executed here.** Stopwatch walks and five
  observed human testers are physical acts.
- **The device pass (§3.1) cannot be run here** for any phase.

What *can* be produced in this environment without lying about a gate: engine
project scaffolding, the data schemas and content-authoring layer, the C# or
GDScript systems source, the content data files, the beat sheet, and the systems
README. What cannot: any measurement, any gate clearance, any pass.

**This blocker does not have a workaround inside the environment.** It needs either
a macOS + device build host driving this repo, or an explicit decision to treat
this repo as source-only and run gates elsewhere. That decision is upstream of
`[CONFIRM]` #4.

### B2 — World bible absent (2026-08-07)

`/CLAUDE.md` cites `/docs/world-bible.md` as the source for lore, names and
character material. The original 36-section design document was not supplied to
this repo. `/docs/world-bible.md` is currently a stub.

Until it is supplied, Heartwood, Pip, the 3 NPCs, the mystery thread and all naming
have no source material. Inventing them would violate the mission's instruction to
mark unknowns rather than invent answers, and would also pre-empt the
derivativeness pass, which needs deliberate names to check rather than
autocompleted ones.

### B3 — Calendar kill criterion is 25 days out (2026-08-07)

Kill criterion 4 parks the project until 6 October if the Phase 2 gate is not
cleared by **1 September**. As of this entry that is 25 days away, with Phase 0 not
started and B1 unresolved. Flagging early because the criterion is a date, not an
effort threshold — it does not move if the work starts late.

---

## Entries

### 2026-08-07 — Repo setup

Actor: Claude Code (`claude-opus-5`), branch `claude/hidden-valley-build-brief-mvnjdu`.

Done:

- `/CLAUDE.md` — the mission, plus standing rules for agents working in the repo.
- `/docs/build-protocol.md` — §2 phase gates, §3 the three passes, §4 kill criteria.
- `/docs/phase-log.md` — this file.
- `/docs/world-bible.md` — stub, see B2.

Not done, and not claimed: no engine project, no source, no build, no
measurements. No phase opened. No gate cleared. No pass run.

Passes run this entry: **none.** Repo setup is not a phase and has no gate; running
a device pass against four Markdown files would be theatre.
