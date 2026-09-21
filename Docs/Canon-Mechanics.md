# Canon Mechanics (locked chat decisions)

Source: GDD + explicit chat decisions (2026-08-02 / 2026-08-03 gap audit). Do not invent mechanics outside this doc without approval.

## Four stats

Wholeness, Stability, Safety, Closeness. Range 0…Max (default 100). Starting value from config (default 50). **Stats never trigger Split at Max** — Max-pole Split is **permanently out of scope**.

## Split (Zero only)

- A **level fail** happens when any stat reaches **≤ 0** (FailThreshold).
- The failing fantasy routes to one of **four Splits** (one per stat), with tie-break order from config (default: Safety → Stability → Wholeness → Closeness).
- Each Split is a **DBT-flavored multi-step exercise** (not a binary stub):
  - Safety → STOP pause sequence
  - Stability → Check-the-Facts prompts
  - Wholeness → Wise Mind middle-path choice
  - Closeness → DEAR MAN ask/boundaries sequence

### Survive Split

- Stabilize the failed meter(s) to a soft floor.
- Unlock the mapped DBT skill and grant charges.
- Grant Split success XP (unspent + total).
- **Resume the same level deck at the crisis card index** with the **same situation text and same left/right/skill deltas** (everyday routine may repeat unchanged).
- **Does not** count as a level clear; Meta opens only after a normal level win (XP threshold or deck empty).

### Die in Split

- Restart the **same level** from the beginning of that level attempt.
- Restore stats from the **level-start snapshot**; deck index resets with a fresh `Begin`.
- No run ending. Death is not a finale.

## Level completion

A level is won when **either**:

1. `LevelXp >= XpToCompleteLevel`, or
2. the card deck is empty.

Each left / right / skill choice grants `XpPerCardChoice` toward `LevelXp` (also adds to run XP / unspent XP).

## Content: acts and scale

- **Target run length: 45 levels** across **3 acts** (`LevelsPerAct = 15`).
- `LevelsBeforeEnding` defaults to **45** (full target). Shorter demo runs may override config only — do not hardcode 5 in code paths.
- Card **decks are grouped by act** (`Resources/Content/Decks/ActNN/`).
- `LevelIndex` maps to act via `LevelsPerAct`.
- **Within an act**: curated pool; deck for a level uses a **level-band offset** (rotate start index by `levelIndex % LevelsPerAct`) so later levels in the act see a shifted sequence of the same pool.
- Fallback: default deck asset if act deck missing.

## Run ending

After `LevelsBeforeEnding` level clears, show **one of four endings** (four Zero fantasies).

**Ending selection** uses a composite score per fantasy (weights in `EndingResolver`; locked here):

| Signal | Weight |
|--------|--------|
| Split survive per matching fantasy | ×3 |
| Symptom points reduced (total) | ×1 each to Stability and Wholeness |
| Traumatic memory unlocks | ×2 Wholeness |
| Unlocked emotions | ×2 Stability |
| Unlocked needs | ×2 Closeness |
| Unlocked skills | ×1 Wholeness |
| Character A bias | +2 Safety, +1 Wholeness |
| Character B bias | +2 Stability, +1 Wholeness |
| Character C bias | +2 Closeness, +1 Safety |

Ties use `SplitTieBreakOrder`. Endings are not “cured” messaging. Strictly **four buckets** (no mixed epilogues).

## Nine BPD symptoms

All nine DSM-aligned axes start at `MaxSymptomSeverity` each run. Meta spends XP to reduce any symptom (−1 severity per spend). Each symptom has a **gameplay modifier** when reduced (see `ModifierRegistry` / `GameConfig`):

| Symptom | Modifier when severity falls |
|---------|------------------------------|
| FearOfAbandonment | Softens negative Closeness deltas |
| UnstableRelationships | Softens relationship-var swings (idealize/devalue magnitude) |
| UnstableSelfImage | Softens negative Wholeness deltas |
| Impulsivity | Softens negative Safety deltas |
| SelfHarmSuicidality | Raises Soft floor / reduces Safety crisis pull (scale on Safety negatives) |
| AffectiveInstability | Softens negative Stability deltas |
| ChronicEmptiness | Mild positive Wholeness on skill choices when low severity |
| IntenseAnger | Softens Closeness + Stability double-hits on anger-tagged cards (generic anger scale) |
| ParanoiaDissociation | Softens Stability negatives under stress; high severity can amplify Stability loss |

Educational copy only — never diagnose the player.

## Therapy

- Chain analysis: Situation + Thought → Emotion; Emotion + Behavior → Need.
- Content is **data-driven** (`Resources/Content/Therapy/` SO or JSON); stub factory remains fallback.
- Unlocking an Emotion with a `MemoryId` increments `TraumaticMemoriesUnlocked` **once per memory id** (not per emotion id).
- Every unlocked Emotion counts toward ending Stability score; every unlocked Need counts toward Closeness (even without skill unlock).
- Need merge may unlock a DBT skill via `SkillUnlockId`.
- Memory penalty: −25% Stability at next level start **per distinct memory** unlocked.

## DBT skills

- Limited-use alternatives that replace unhealthy swipe outcomes on cards that allow skill use.
- Unlock paths only:
  - Survive Split → skill mapped to fail stat
  - Therapy Need merge → `SkillUnlockId`
- Cards should set `RequiredSkill` when a specific skill fits; if `None`, first usable unlocked skill applies.
- Every `DbtSkillId` (except `None`) has display name, educational description, module, and charges-on-unlock.

## Card authoring

- Choices may apply `StatEffects`, `RelationshipOps`, and `HiddenVariableOps`.
- Cards may declare **inclusion gates** (`RequiredHiddenMin` / relationship / symptom severity) so decks can branch by run state.
- Crisis resume uses identical authored choice data (no auto-variant).

## Characters

- A / B / C are backgrounds only (no visible protagonist). Ending body text may flavor by character. Optional starting bias lives in ending scorer, not visible avatar.

## Tone / safety reminders

- Second-person, gender-neutral player-facing copy.
- No diagnosing the player; educational DBT wording only.
- Progress is non-linear; character is not “fixed” or “cured.”
- Self-harm / crisis themes stay in Safety / crisis framing without sensationalism.
- Crisis resources footer (hotlines / “not a substitute for therapy”) may appear on Safety Split and Ending screens — informational only, not gameplay.
