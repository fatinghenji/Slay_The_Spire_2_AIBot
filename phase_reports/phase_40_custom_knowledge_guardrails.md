# Phase 40 - Custom Knowledge Guardrails and Temporary-State Rules

## Background

Live testing exposed a recurrent reasoning error:

- the agent could sometimes justify ending turn with unused energy as if that energy would be saved for a later turn.

This pointed to a broader issue than a single rule:

1. some baseline combat-state rules were still too weakly phrased for decision correction,
2. the custom knowledge directory did not yet provide a clear Chinese authoring guide,
3. markdown authoring constraints were not documented precisely enough for non-technical users.

## Changes

### 1. Expanded custom mechanic rules for temporary combat state

- Reworked `sts2_guides/custom/game_mechanics.json`.
- Added or strengthened custom mechanic rules around:
  - unused energy not carrying across turns,
  - block not persisting by default,
  - combat cleanup removing temporary state,
  - non-retained hand cards leaving hand at end of turn,
  - Ethereal cards exhausting if left in hand,
  - single-turn retain expiring after end-of-turn cleanup,
  - temporary cost modifiers expiring at end of turn.

These rules are written as decision guardrails, not only descriptive facts, so they can better suppress invalid planning logic such as "save energy for next turn" or "hold a normal hand card for later without Retain".

### 2. Replaced the custom knowledge README with a Chinese authoring guide

- Reworked `sts2_guides/custom/README.md`.
- The new README now explains:
  - when to use JSON versus Markdown,
  - which JSON file names are actually supported,
  - how override-by-`id` or `slug` works,
  - which Markdown file names are actually consumed by the knowledge loader,
  - what Markdown constraints will cause validation rejection,
  - a practical workflow for fixing agent mistakes through custom knowledge.

### 3. Clarified Markdown limitations in user-facing language

- The README now explains validator restrictions without relying on code-oriented wording.
- This makes the instructions easier for Chinese-speaking players to follow while still matching the actual validator behavior.

## Validation

Validated the updated custom mechanics file with JSON parsing:

```powershell
Get-Content sts2_guides\custom\game_mechanics.json -Raw | ConvertFrom-Json | Out-Null
```

Result:

- JSON parsed successfully
- no format error in the custom mechanic file

## Expected Player-Facing Outcome

- The agent should be less likely to justify lines based on banking unused energy or carrying temporary state incorrectly.
- Players now have a clear Chinese guide for writing their own custom rules and Markdown notes.
- Future corrections can be added faster and more safely through the `custom` layer without editing built-in knowledge files.
