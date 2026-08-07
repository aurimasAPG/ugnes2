# HIDDEN VALLEY — SYSTEMS README

How the game is built, and how to add content to it without writing code.

If you only read one section, read [§3, Adding an NPC](#3-adding-an-npc). The entire
architecture exists to make that section short.

---

## 1. The shape of the thing

```
Assets/
  HiddenValley/
    Runtime/
      Core/          plain C#, NO UnityEngine reference. All the rules live here.
      Unity/         MonoBehaviours. Views onto Core. No rules.
    Editor/          build command, content validation menu item
  StreamingAssets/
    Content/         every NPC, quest, item, clue, recipe and line of dialogue
tools/
  HiddenValley.Tests/  xunit suite over Core + the shipped content
  build-ios.sh         the Phase 0 one-line build
  phase2-gate.sh       the Phase 2 gate check
docs/
  world-bible.md       lore, names, character material
  build-protocol.md    phase gates, the three passes, kill criteria
  phase-log.md         the running record. Read it first.
  passes/              recorded iteration passes
```

**`Core` has `"noEngineReferences": true` in its assembly definition.** That is not
tidiness — it is enforced by Unity's compiler, and it means the rules can be built and
tested on a machine with no engine installed. That is how this repo's logic is verified
at all right now (phase log, blocker B1), and it is why `dotnet test` works.

Run the suite:

```bash
dotnet test tools/HiddenValley.Tests
```

---

## 2. The one idea

There are two closed vocabularies. **Conditions** ask about the world; **effects**
change it. All content is built from them, and neither list grows when content is added.

The important consequence is what is *missing* from the effect list. There is no
`open_shop`, no `unlock_area`, no `spawn_resource_node`. Those are not effects — they
are **conditions on world objects**. An NPC who changes the contents of the world just
sets a flag, and the objects are watching the flag.

That inversion is why Orrel's trade post and Coll's cliff cost zero lines of C#.

### Conditions

| type | fields | true when |
|---|---|---|
| `flag` | `key`, `value` (default `"true"`) | the flag equals the value, compared as a string |
| `has_item` | `item`, `count` (default 1) | the player holds at least that many |
| `quest_state` | `quest`, `state` (`notstarted`/`active`/`completed`) | the quest is in that state |
| `quest_step` | `quest`, `step`, `compare` (`at`/`at_or_past`/`past`) | position relative to a step, by **id** not index |
| `clue_known` | `clue` | the clue is on the account |
| `recipe_known` | `recipe` | the recipe is craftable |
| `time_of_day` | `phases` (list) or `phase` | the clock is in one of them |
| `all` / `any` | `of` (list) | as you'd expect |
| `not` | `of` (single) | negation |
| `always` | `value` | constant |

A **null condition is true**. Omitting `when` means "always".

### Effects

| type | fields | does |
|---|---|---|
| `give_item` / `take_item` | `item`, `count` | inventory |
| `set_flag` | `key`, `value` | sets a flag to a string |
| `cycle_flag` | `key`, `values` (list) | steps to the next value, wrapping — for levers, dials, gates |
| `start_quest` / `advance_quest` / `complete_quest` | `quest` | quest control |
| `learn_recipe` | `recipe` **or** `family` | grants one recipe, or a whole family at once |
| `learn_clue` | `clue` | adds to the account |
| `set_time` | `phase` | advances to the next occurrence of a phase (sleeping, waiting) |
| `advance_time` | `minutes` | advances the clock |

**Do not add to either list to make a piece of content work.** If you think you need a
new type, you almost certainly need a flag and a condition on a world object instead.
The loader throws on unknown types precisely so this decision gets made deliberately.

---

## 3. Adding an NPC

**Zero C# changes. One new file.** A complete worked example — a fourth NPC with a
two-step quest, a new item, and the world objects to support it — is committed at
`docs/phase2-gate/npc4.json`. Copy it.

1. Create `Assets/StreamingAssets/Content/2N-yourname.json`.
2. Put everything that NPC needs in that one file. A file may contain any mix of
   `items`, `recipes`, `clues`, `npcs`, `quests`, `dialogue` and `world` arrays — you do
   not edit a registry anywhere, and there is no central list to append to.
3. Run `dotnet test tools/HiddenValley.Tests`. The validator will tell you about dangling
   ids, unreachable quests and dead-end steps before a player finds them.

Minimum viable NPC:

```json
{
  "npcs": [
    {
      "id": "npc.yourname",
      "name": "Yourname",
      "region": "heartwood",
      "capability_kind": "none",
      "schedule": [ { "from": "dawn", "to": "dusk", "waypoint": "wp.somewhere" } ],
      "dialogue_entries": [ { "dialogue": "dlg.yourname", "node": "idle" } ]
    }
  ],
  "dialogue": [
    {
      "id": "dlg.yourname",
      "nodes": [ { "id": "idle", "speaker": "npc.yourname", "lines": [ "\"Hello.\"" ] } ]
    }
  ]
}
```

### Dialogue entries are ordered, and order is a design decision

An NPC's current conversation is **the first entry whose condition passes**. Put specific
states above general ones and always finish with an unconditional fallback, or a player
can walk up and get nothing, which reads as a bug.

**There is one ordering trap, and it has already bitten this project once.** A mystery
beat placed above an unoffered quest will pre-empt it — so the very first time the player
ever meets that character, they get the middle of a thread instead of an introduction,
and the quest is never offered. Both Vesk and Coll shipped with this bug and it was
caught by the playthrough test, not by reading.

Rule: **an entry that fires on world knowledge goes below the entries that offer that
character's own quests.** Both files carry a `_note` at the exact spot explaining why.
(Any key starting with `_` is ignored by the loader — use it for authoring notes.)

### Capability kinds

`capability_kind` is one of `recipe_family`, `world_contents`, `traversal` or `none`, and
the validator enforces that the slice's three named NPCs cover all three. This is the
brief's rule that NPCs must differ **at the systems level, not the personality level** —
three charming people who all say "fetch me five crystals" is the stated failure state.

A companion or a background character uses `none`.

---

## 4. Adding a quest

Quests live in the same file as whoever gives them.

```json
{
  "id": "quest.example",
  "title": "An Example",
  "shape": "fetch",
  "giver": "npc.yourname",
  "summary": "One line, for the journal.",
  "start_when": null,
  "steps": [
    {
      "id": "first",
      "tracker": "What the player should do now.",
      "completion": [
        { "id": "normal", "when": { "type": "has_item", "item": "item.thing", "count": 2 } }
      ],
      "on_complete": []
    }
  ],
  "on_complete": [ { "type": "set_flag", "key": "example.done", "value": "true" } ]
}
```

- **`shape`** is one of `fetch`, `repair`, `investigate`, `craft-to-spec`,
  `observe-and-report`, `escort`, `trade`, `unlock`. **No more than two quests may share
  a shape** — the validator fails the build otherwise, because this is the rule most
  likely to rot silently as content grows.
- **A step completes when ANY of its completion sets passes.** Several sets means several
  routes. Mark a route `"documented": false` when the tracker text does not describe it.
  The slice must have **exactly one** such quest, and the validator counts.
- **A step with an empty `completion` list** is advanced by an `advance_quest` effect,
  normally from dialogue. That is the pattern for "now go and tell them".
- `start_when` makes a quest start itself. Quests with neither `start_when` nor a
  `start_quest` effect pointing at them are unreachable, and the validator says so.

Which route the player took is recorded automatically as a flag,
`solved.<quest>.<step>` = the completion set's id. Use it to have characters react — and
to find out afterwards whether any tester ever found the undocumented route.

---

## 5. Adding a clue

```json
{ "id": "clue.example", "thread": "quietday", "order": 6,
  "title": "short label", "text": "What the player now knows." }
```

Clues are granted by a `learn_clue` effect from dialogue or a world object, and shown on
the account, grouped by `thread` and sorted by `order`. `order` is a display
hint only — clues may be found in any order, and the mystery's steps gate on *which*
clues are known, never on the sequence.

---

## 6. Adding a world object

This is the type that does the most work. Shops, resource nodes, barriers, pickups,
levers, readables and puzzle mechanisms are all this.

```json
{
  "id": "world.example",
  "kind": "pickup",
  "region": "heartwood",
  "interact_label": "Take it",
  "text": "Optional body text, for things that are read.",
  "visible_when": null,
  "blocks_when": null,
  "interact_when": null,
  "on_interact": [ { "type": "give_item", "item": "item.thing", "count": 1 } ]
}
```

- `visible_when` — exists in the world at all. This is how Orrel's shop and sunmoss nodes
  appear only after her quest.
- `blocks_when` — blocks navigation. This is how Coll's cliff is closed and then open.
- `interact_when` — offered to the player. Gate on items, time of day, quest state.

In the scene, a `WorldObjectBinder` component holds the id and mirrors all three. Two
binders may point at one anchor — the dawn and dusk observation points do exactly that,
because effects cannot branch on time, so each hour gets its own object.

---

## 7. Save files

The save format is a flat set of id-keyed maps: flags, inventory, quests, clues, recipes,
clock. **No field is named after a specific quest, NPC or item**, so adding content never
changes the format and a save written before an NPC existed still loads afterwards.

Content that has since been deleted is **skipped on load, not fatal** — removing a quest
mid-project must not brick a tester's save. A step index past the end of a quest is
clamped. A save from a newer build is refused with a clear message rather than
half-loaded. All four behaviours are tested in `SaveLoadTests.cs`.

Saves are written atomically (temp file, then move) because testers close apps at the
worst possible moment.

---

## 8. The gates, mechanically

| Gate | Command | What it proves |
|---|---|---|
| Phase 0 | `tools/build-ios.sh` | build reaches device, and times itself against the 10-minute budget |
| Phase 2 | `tools/phase2-gate.sh` | adding NPC #4 changed no runtime C#, and the NPC works |
| content | `dotnet test tools/HiddenValley.Tests` | references resolve, brief constraints hold, the slice completes with no soft-lock |

`ContentValidator` is also on the Unity menu under **Hidden Valley → Validate Content**,
and runs automatically before every iOS build. A content typo fails the build instead of
reaching a tester.

---

## 9. What is deliberately not here

- **No dependency injection container, no event bus, no service locator.** `Game` is one
  object you can construct in a test in one line. Naming twenty-two systems does not
  produce decoupling; a reproducible test does.
- **No per-content C# types.** There is no `VeskController`. If you find yourself writing
  one, the architecture has failed and kill criterion 2 applies.
- **No inheritance hierarchy for quests or NPCs.** A new NPC is a row of data.

---

## 10. Placing things: the layout file

Content JSON decides **what** exists; `Assets/HiddenValley/Layout/heartwood.json`
decides **where**. The scene file is never authored by hand — it is generated:

    Unity -quit -batchmode -projectPath . -executeMethod HiddenValley.Editor.VillageSetup.GenerateHeartwood

The layout file has four lists:

- `blocks` — dumb geometry: name, shape (`cube`/`cylinder`/`sphere`), `pos`, `size`,
  optional `rot`, and a `mat` from the fixed grey-box palette (`grey`, `dark`, `sand`,
  `moss`, `bark`, `water`).
- `worldObjects` — one entry per content world-object id. Same geometry fields, plus
  `blocking: true` to wire the collider the binder toggles (barriers, gates), and
  `solid: false` for pickups you walk through. Every entry gets a `WorldObjectBinder`
  and an interaction zone automatically.
- `npcs` — spawn position and a `waypoints` map from schedule waypoint ids to
  positions. Every entry gets a `NavMeshAgent` and an `NpcBinder`, fully wired.
- `player` / `pip` — spawn points.

The generator bakes the NavMesh last, then registers the scene in Build Settings.
**Moving a landmark to fix a dead stretch found by the interest-density walk is a JSON
edit and a re-run.** Adding NPC #4's home, waypoints and quest objects is the same —
still zero C#.

Rule of thumb baked into the current layout: walk speed is 4.5 m/s, the 40-second rule
is therefore 180 m, and no leg is longer than ~30 m without something to look at or
touch. Keep it that way.

## 11. The HUD

`GameHud` (IMGUI, like the rest of the debug layer — no canvas, no prefabs, survives
scene regeneration) draws: the clock, the quest tracker, the interaction prompt,
dialogue with choices, the bag, the account, and readable text.

Input is **one context button** (`TouchControls.ContextAction`): a right-half tap
talks to the NPC in front of you, else interacts with what is in front of you, else
advances dialogue, else jumps. Dialogue and panels lock movement, so aimed taps
(choices, toggles) are single-touch. Phase 4 replaces the skin, not this wiring.
