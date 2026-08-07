# DERIVATIVENESS PASS 01

**Date:** 2026-08-07
**Scope:** every name, mechanic, UI arrangement and character silhouette produced so far —
the world bible, the shipped content, and the Unity adapter layer.
**Run by:** Claude Code (`claude-opus-5`)
**Method (build-protocol §3.3):** for each item, ask what specific existing game it came
from. Anything that answers cleanly gets changed.

This is simultaneously the IP-safety check and the originality check. Findings are
recorded whether or not they were acted on, including the ones referred upward.

---

## 1. Changes made

### 1.1 Brann → **Coll** (changed)

**Answered cleanly:** Bran Stark, *Game of Thrones*. One letter apart, and one of the
better-known character names of the last decade. The characters have nothing in common —
ours is an elderly sluicekeeper — but the pass does not grade on whether the resemblance
is fair.

**Changed to Coll.** Short, consonant-final, fits the naming convention in world bible §2,
and has no prominent character referent. Applied across content, tests and docs; the
suite still passes.

### 1.2 "Investigation board" → **the account** (changed)

**Answered cleanly:** the corkboard-with-red-string is the signature visual of a whole
detective-game lineage — *Sherlock Holmes: Crimes & Punishments*, *Ace Attorney*'s
evidence screens, *Return of the Obra Dinn*'s book. The *name* invites the *art*, and the
art is the actual signature.

Ours is mechanically much thinner: a read-only list of what the player knows, grouped by
thread. No link-drawing, no deduction mini-game, no "connect two clues to unlock a third".

**Changed the name to "the account"** — plain English, doubles as a narrative account and a
ledger record, and fits the valley's bookkeeping texture (Orrel keeps the ledgers).

**Binding art constraint recorded here:** no corkboard, no pins, no red string, no
photographs. It is a page.

### 1.3 Pip as a moth, not a fairy (changed at design time, recorded here)

**Would have answered cleanly:** a small glowing flying companion that draws the player's
attention is Navi (*Ocarina of Time*), and behind Navi is Tinker Bell. The silhouette — a
bright point with wings, trailing sparkle — is close to unusable.

Already diverged before content was authored, and recorded so the decision is not
accidentally reversed by an artist later:

- **Moth, not fairy.** No humanoid form, no face, no trailing sparkle.
- **No highlighting, no pinging, no pointing.** Pip's light *dims* near the residue. It
  is an analog signal the player has to notice, not a marker that does the noticing for
  them. `PipCompanion.cs` uses a squared falloff specifically so it reads as a reaction
  rather than a proximity gauge.
- **Nobody explains it.** No character tells the player what Pip's light means.

---

## 2. Findings referred upward — owner's decision required

### 2.1 The title "Hidden Valley" — `[CONFIRM]`

Two independent problems, neither of which an agent should decide:

1. **Trademark.** *Hidden Valley* is a well-known US food brand (Hidden Valley Ranch,
   owned by Clorox). Different goods class, so not automatically a conflict — but any
   title search on this name will surface it, and app-store name collisions are decided by
   whoever complains first.
2. **Genre adjacency.** "\<Adjective\> Valley" for a cosy rural life-sim reads directly as
   *Stardew Valley*. That is the single most-cited comparison this project will get, and
   the title volunteers it before anyone has seen a screenshot.

The name came with the brief, so it is out of scope to change unilaterally. **Recommend
changing it.** The world bible's naming convention (§2 — function plus physical feature,
plain compound English) generates alternatives easily; the valley's defining feature is
the cut stone channel running through it.

Logged as `[CONFIRM]` #8 in the phase log.

---

## 3. Findings inspected and cleared

Recorded so the next pass does not re-litigate them.

| Item | Nearest existing thing | Verdict |
|---|---|---|
| **Heartwood** (village) | A real timber term, used widely and owned by nobody | Clear. Generic vocabulary, not a mark. |
| **Pip** (name) | Mandated by the brief. *Great Expectations*; various minor game characters | Clear. Common diminutive, no distinctive referent. |
| **Vesk, Orrel, Sedge, Sile** | Nothing located | Clear. Invented, consistent phonology. |
| **The Ash Shelf, the Undersluice** | Nothing located | Clear. Built from the naming convention. |
| **Water-gate / sluice puzzle** | Water-level puzzles are a genre staple (*Zelda* water temples, many others) | Clear as a *generic* mechanic. Nobody owns "redirect water". Mitigated by the plate-requires-lens dependency, which is ours and is what makes the chain specific. |
| **Time-gated resource** (sunmoss opens at dusk) | *Stardew Valley*, *Animal Crossing* | Clear. Time-of-day gating is genre grammar, not a signature. |
| **Recipe families granted by an NPC** | Skill trees and teacher NPCs generally | Clear. No distinctive arrangement copied. |
| **Trade post opened by a quest** | Common | Clear. |
| **Quietday** (the phenomenon) | Nothing located | Clear. |
| **The mystery's resolution** — an inherited instruction nobody understands, obeyed for decades | Structurally reminiscent of a lot of folk horror; no specific game | Clear. The *shape* is old; the execution is not traceable. |

---

## 4. Not yet assessable

Recorded as outstanding rather than passed, because claiming otherwise would be the exact
dishonesty this pass exists to prevent.

- **Character silhouettes.** No art exists. The three NPCs and Pip have written silhouette
  *intent* only. This must be re-run against actual character art before Phase 3 closes,
  and it is the highest-risk remaining item — silhouette is where IP problems actually
  live, far more than names.
- **UI layout.** No UI exists beyond a debug frame-time readout. When it is built, the
  arrangement to consciously avoid is the cosy-game default: circular portrait bottom-left,
  minimap top-right, quest tracker top-left with a coloured pin. That layout is not owned
  by anyone in particular, which is exactly why arriving at it by default would read as
  having no opinion.
- **Audio.** Nothing exists. `[CONFIRM]` #7 is still open.

---

## 5. Next pass

Trigger: **first character art, and first UI layout.** Whichever lands first.

The two items in §4 are the ones most likely to produce inconvenient findings, which per
the protocol makes them the ones most likely to get skipped. They are written here so that
skipping them requires deleting a line rather than merely forgetting.
