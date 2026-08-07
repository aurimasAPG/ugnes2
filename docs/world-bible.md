# HIDDEN VALLEY — WORLD BIBLE

> **PROVENANCE: agent-authored, 2026-08-07.** The original 36-section design
> document was never supplied to this repo (blocker B2). On 2026-08-07 the owner
> decided explicitly to have this drafted rather than leave content blocked, on the
> condition it be flagged. Everything below is invented by Claude Code, not
> inherited. Treat it as a first draft with an owner's veto on every name.
>
> It has been through one derivativeness pass (§3.3) — recorded in
> `/docs/passes/derivativeness-01.md`. Names that were changed are noted there
> with what they were changed away from.

Scope ceiling: this file covers **only** the vertical slice — Heartwood, the one
adjacent forest region, Pip, three NPCs, and one mystery thread. The other six
regions are deliberately absent. If Frostpeak appears in this file, delete it.

---

## 1. The premise, in one paragraph

Hidden Valley is a narrow, wet, north-facing valley that was industrial once and
is quietly agricultural now. The people who live here inherited the machinery of
an older, larger operation — sluices, kilns, cut stone channels — and use maybe a
tenth of it. The rest sits in the trees with moss on it. The valley is not
mysterious because something magical happened; it is mysterious because it was
built by people who left, and nobody currently living here knows what all of it
was for. That distinction is the whole tone. **The player is not discovering a
secret. The player is reading a machine nobody finished explaining.**

Emotional target: competent, unhurried, slightly haunted. Not whimsical. The
valley is beautiful in the specific way that wet stone and low cloud are
beautiful, and it is never twee.

---

## 2. Naming conventions

So that new names are *derivable* rather than invented per-asset — this matters
directly for the data-driven requirement, since content authors will need to name
things without a meeting.

- **People** get one short given name, one or two syllables, hard or soft
  consonant, no apostrophes, no invented diacritics. They are addressed by given
  name alone. A trade may follow as an epithet: *Vesk the glasswright*.
- **Places and structures** are named for their **function plus a physical
  feature**, in plain compound English: the *Undersluice*, the *Ash Shelf*, the
  *Long Ledger*. If a place name sounds like fantasy, it is wrong. These people
  named things while working, not while composing.
- **Objects** are named for material or purpose, never for a quality:
  *reading lens*, *white sand*, *paddle key*. Never *Lens of Truth*.
- **Nothing in the valley has a proper noun that a person did not give it.**
  Natural features get flat descriptive names — *the pool*, *the eastern leaves*.

Test for a new name: could a tired person who works here have said it out loud
without smiling? If not, change it.

---

## 3. Heartwood Village

The hub. Twenty-odd buildings on the valley floor, built along a stone water
channel that runs straight through the middle of town — the channel is the single
strongest readable landmark and the spine of the layout. Everything the player
needs is within sight of the water.

Layout intent, written as a level-design constraint rather than a description:

- The channel runs **north–south**. The player enters from the south. The forest
  region opens to the north-east.
- **Three landmarks are visible from the village entrance**: the kiln smoke
  (Vesk, east bank), the ledger house (Orrel, west bank, the only two-storey
  building), and the sluice gear housing (Coll, north end, where the channel
  drops out of sight). This is deliberate — it satisfies interest density at the
  moment of arrival and it teaches the map without a tutorial.
- The channel is crossable in **three** places. Bridge placement is the primary
  tool for controlling walk times; the interest-density walk (§2 Phase 3) will
  almost certainly move at least one of them.
- **The village is small on purpose.** ~25 minutes of gameplay cannot afford a
  village the player gets lost in. Density beats extent.

Heartwood is named for a timber term, not for a heart. Nobody in the valley thinks
the name is sentimental and no character should ever remark on it.

---

## 4. The forest region — the Ash Shelf

The one adjacent region in scope. Reached north-east from the village.

It is a **terraced** forest: two flat shelves separated by a stone drop of about
four metres, cut by the old operation and now overgrown. The upper shelf is open
at the start of the slice. **The lower shelf is closed** and is the traversal gate
(§5.3).

- The eastern leaves — the ones that catch grey ash on Quietdays — are on the
  **upper** shelf, visible early, explicable only later. This is the mystery's
  first planted object and the player will walk past it before it means anything.
- The pool sits at the foot of the drop, fed by the sluice. It is full at the
  start. Draining it is the puzzle chain's payoff and reveals the lower shelf
  floor.
- The forest is quiet by design. Its interest density comes from **readable
  structures** — grates, channel stones, a collapsed gear housing — not from
  scattered pickups. Pickups placed to fill dead space are the failure mode the
  Phase 3 gate exists to catch.

---

## 5. The three NPCs

The brief's binding constraint: each hands the player a **capability they did not
previously have**, and the three capabilities are of different *kinds*. They are
designed from the capability outward. Personality is applied afterward, which is
the correct order and the opposite of how the original brief did it.

### 5.1 Vesk — the glasswright → **grants a recipe family**

Works the kiln on the east bank. Middle-aged, brusque, entirely uninterested in
the player until the player is useful, and then abruptly generous. Talks about
temperature the way other people talk about weather.

- **Capability granted:** the **lensmithing** recipe family — reading lens, wide
  lens, and the burning lens. Before Vesk, the player cannot craft glass at all;
  after, an entire crafting branch is open.
- **Why it is a family and not a recipe:** a single recipe is an item with extra
  steps. A family changes what the player *can consider doing*, which is the point
  of the capability requirement.
- **Systems hook:** `learn_recipe` effects. Nothing else in the engine knows Vesk
  is special.

### 5.2 Orrel — the ledgerkeeper → **changes what the world contains**

Keeps the valley's accounts in the two-storey house on the west bank. Precise,
dry, faintly amused. Has decided the valley is worth more than it thinks it is and
is quietly proving it.

- **Capability granted:** completing her thread **opens the Trade Post** (a shop
  that did not exist) and **surfaces sunmoss nodes** — a harvestable resource that
  is not present in the world until her quest resolves. The world literally
  contains more things than it did.
- **Why sunmoss and not a generic resource:** it is the input to the trade economy
  she opens, so the two halves of her contribution reinforce rather than sit
  side by side.
- **Systems hook:** world objects with a `visible_when` condition on a flag she
  sets. **No new effect type, no new code** — this is the case that proves the
  architecture, because "an NPC changes the contents of the world" is exactly the
  requirement that usually gets hardcoded.

### 5.3 Coll — the sluicekeeper → **gates traversal**

Minds the gear housing at the north end, where the channel drops. Old, slow,
careful, and the only person in the valley who understood the machinery from the
inside. Answers questions with questions, not to be coy but because he is checking
whether you have thought about it.

- **Capability granted:** the **Undersluice** — draining the pool opens the lower
  shelf of the Ash Shelf, a space that was closed. This is spatial capability, not
  inventory capability.
- **Dependency, deliberate:** the puzzle chain that opens it **requires a reading
  lens from Vesk's family** to read the weathered setting plate. The three NPCs
  are therefore not parallel; two of them compose. This is what stops them being
  three dispensers in different hats.
- **Systems hook:** a barrier world object with an `active_when` condition. Again
  no bespoke code.

### 5.4 The distinctness check, stated so it can be tested

| NPC | Capability kind | Test that it is real |
|---|---|---|
| Vesk | Recipe family | Player can craft a class of item they previously could not |
| Orrel | World contents | Object count in the world increases; a shop exists that did not |
| Coll | Traversal | A navigable space that was closed is open |

If any two of these rows could be swapped without changing the game, the brief has
been failed. This table is machine-checked by the content validator.

---

## 6. Pip — the companion

Pip is a **lamp-moth**: palm-sized, slow, and carries a steady cold light. Pip is
not a guide, not a hint system, and never speaks in words.

**Pip's one mechanic:** Pip's light **dims in the presence of the residue** — the
grey ash of the Quietdays and anything that has been near the source of it. Not a
marker, not a highlight, not a ping. An analog signal the player learns to read.
Standing still and watching Pip get dimmer is a legitimate way to search.

Design notes that matter:

- **Pip's dimming is never explained by a character.** The player works it out.
  The first Quietday happens near Pip and the correlation is available; nobody
  points at it.
- Pip has no inventory function, no combat function, and cannot be upgraded. The
  temptation to make the companion a systems hub is exactly what makes companions
  forgettable.
- Pip is emphatically not cute-adjacent-to-marketable. Moth, not fairy.

---

## 7. The mystery thread — the Quietdays

The one complete mystery in the slice. It must resolve, and its resolution must
land on a question the player cannot answer. Kill criterion 3 measures precisely
this.

**What the player observes (Act 1).** Some mornings the valley is silent — no
birds — and a fine grey ash has settled on the eastern leaves of the Ash Shelf.
The villagers have a word for it, *a Quietday*, and treat it as weather. Nobody is
alarmed. That everyone is unbothered is the hook: the player is the only one
treating it as a question.

**What investigation yields (Act 2).** Clues, in the order they are findable:

1. **Scorched bark** on the upper shelf — heat damage, but no fire scar on the
   ground. Heat from *above* or from *inside* the stone.
2. **The ash is kiln ash**, not wood ash. Vesk can identify it and is puzzled,
   because his kiln was cold on the last Quietday and he can prove it.
3. **The channel runs warm** on Quietday mornings. Coll has known this for years
   and never mentioned it, because to him it is simply what the channel does.
4. **A second kiln** exists — upstream, under the shelf, part of the old
   operation, not on any map the villagers use.
5. **It is lit.** It has been lit for a long time. It is being *kept* lit.

**The resolution (Act 3).** The player reaches the second kiln. It is banked and
tended — recently, competently, by someone who knows the machinery. There is a
worked routine here: a stack of fuel cut to size, a swept floor. The kiln is not
a ruin someone forgot to put out. **Someone is deliberately keeping it warm.**

Coll, asked directly, does not deny it. He has been tending it since the previous
sluicekeeper showed him how, and *she* did not explain why either. He was told to
keep it warm and never let it go out, and he has done that for thirty years,
because the instruction came from someone who understood the machinery better than
he does.

**The unanswered question.** Warm for *what*? The kiln heats the stone under the
lower shelf. Something down there needs to stay above a temperature. Neither Coll
nor the player ever learns what it is, and the slice ends with the player having
resolved every factual question — what the ash is, where it comes from, who tends
it — and acquired a much better one.

**Information-design rules for this thread** (these are the levers to pull if the
hook test fails, per kill criterion 3):

- The player must find clue 1 **before** anyone mentions Quietdays as a concept.
  Object first, word second.
- Coll must be a **source of clue 3 without knowing it is a clue**. An informant
  who knows they are withholding is a different, worse story.
- The final scene contains **no antagonist and no danger**. The tension is
  entirely "this has been going on the whole time and is not for you."
- The unanswered question must be **askable in one sentence by an eight-year-old**.
  If a tester cannot phrase it, the information design has failed regardless of how
  elegant it is.

---

## 8. The puzzle chain — the Undersluice

One multi-step environmental puzzle. It is the spine that ties the three NPCs
together and the reason the slice is a system rather than a list.

- **Step 1 — the grate.** The debris grate at the channel head is choked and
  broken. Repairing it is a physical, tool-using task. (Quest: *The Grate*.)
- **Step 2 — the setting plate.** Three paddle gates must be set to a
  configuration recorded on a carved stone plate. **The plate is weathered
  illegible** — it requires a **reading lens**, which requires **Vesk's recipe
  family**. This is the cross-system dependency and the moment the player realises
  the NPCs are not parallel.
- **Step 3 — the head gate, at dawn.** Flow is only sufficient at dawn. The player
  must have set everything correctly and then **wait for, or sleep to, the right
  time of day**. This is the time-of-day system's one load-bearing use in the
  slice — a system used once, meaningfully, rather than everywhere decoratively.

Draining the pool opens the lower shelf, which is where the second kiln is
reachable. **The puzzle chain and the mystery thread resolve into the same
space** — this is intentional and is the strongest structural decision in the
slice. The player does not do a puzzle and then a story; the puzzle *is* how the
story opens.

---

## 9. The eight quests

Shapes are constrained: no more than two may share a shape. All eight below are
**distinct** shapes, which leaves headroom for content changes without breaching
the constraint. Machine-checked by the validator.

| # | Quest | Shape | Giver | What it moves |
|---|---|---|---|---|
| 1 | Kiln-Ash | fetch | Vesk | Opens the lensmithing family |
| 2 | A Lens for Reading | craft-to-spec | Vesk | Produces the reading lens **← the undocumented solution** |
| 3 | The Grate | repair | Coll | Puzzle step 1 |
| 4 | The Undersluice | unlock | Coll | Puzzle steps 2–3; opens the lower shelf |
| 5 | Quietday | investigate | Pip / self-started | The mystery thread |
| 6 | Countings | observe-and-report | Orrel | Opens the Trade Post and sunmoss |
| 7 | The Long Ledger | trade | Orrel | Exercises the economy she opened |
| 8 | Cart to the Shelf | escort | Orrel | Uses the space Coll opened |

**The undocumented solution (quest 2).** The quest text says to craft a reading
lens from Vesk's recipe. It is *also* completable by recovering the **drowned
lens** — an old lens lying in the pool, findable by anyone who looks in the water
before draining it, and handable straight to Vesk. Nothing tells the player this.
Vesk's dialogue acknowledges it drily if they do.

This satisfies the brief's "exactly one of the eight" requirement. It is
implemented as two alternative completion condition sets on the same step, so the
engine needs no special case — and the validator asserts that exactly one quest in
the slice has a step with more than one completion set.

---

## 10. What is deliberately not here

Recorded so that a later agent does not treat absence as an oversight:

- **No combat.** Nothing in the slice needs it and adding it would double the
  Phase 1 surface.
- **No farming or property ownership.** The obvious genre move, and the one most
  likely to make this read as derivative. See the derivativeness pass.
- **No romance, no calendar of festivals, no multi-day relationship tracks.**
  Twenty-five minutes cannot land any of them.
- **No named region beyond the Ash Shelf.** Frostpeak does not exist yet.
