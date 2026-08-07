# Hidden Valley

Vertical slice: one TestFlight-installable iOS build, ~25 minutes of polished gameplay.
One hub village, one forest region, one companion, 3 NPCs, 8 quests, 1 puzzle chain,
1 mystery thread.

| File | What it is |
|---|---|
| [`CLAUDE.md`](CLAUDE.md) | The mission and the scope ceiling. Start here. |
| [`docs/build-protocol.md`](docs/build-protocol.md) | Phase gates (§2), the three passes (§3), kill criteria (§4). |
| [`docs/phase-log.md`](docs/phase-log.md) | Running record — current phase, open `[CONFIRM]` items, blockers. **Read before starting work.** |
| [`docs/systems-README.md`](docs/systems-README.md) | How to add an NPC, a quest, a clue. No code required. |
| [`docs/world-bible.md`](docs/world-bible.md) | Lore, names, character material. Agent-authored — see its provenance header. |
| [`docs/passes/`](docs/passes/) | Recorded iteration passes. |

## Layout

```
Assets/HiddenValley/Runtime/Core    the rules. Plain C#, no UnityEngine reference.
Assets/HiddenValley/Runtime/Unity   MonoBehaviours. Views onto Core.
Assets/StreamingAssets/Content      every NPC, quest, item, clue and line of dialogue
tools/HiddenValley.Tests            xunit suite over Core and the shipped content
```

## Commands

```bash
dotnet test tools/HiddenValley.Tests   # 22 tests: engine, content, full playthrough
tools/phase2-gate.sh                   # Phase 2 gate: did adding NPC #4 touch any C#?
tools/build-ios.sh                     # Phase 0 gate: one-line build, times itself
```

The first works anywhere. The last needs macOS, Unity and a device.

## Status

**No phase gate is cleared.** Every gate requires a physical device, and the environment
this was built in has none — see blocker B1 in the phase log.

What exists and is verified: the rules engine, the slice's content, and a validator that
enforces the brief's own design constraints. 22 tests pass, including a scripted
playthrough of the whole slice.

What exists and is **not** verified: the Unity adapter layer, which has never been
compiled. There is no build, no frame-time measurement, and no human has played this.

Four `[CONFIRM]` decisions are open, including whether the title can stay.
