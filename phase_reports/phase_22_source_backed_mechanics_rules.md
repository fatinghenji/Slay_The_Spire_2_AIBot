# Phase 22 - Source-backed mechanics rules

## Goal

Continue the plan by improving the quality of the structured mechanics knowledge base, with a focus on source-backed combat rules rather than guide-level generalities.

This phase targets the P0 mechanics portion of the knowledge plan:

- damage and block interaction rules
- common combat status modifiers
- resource and cleanup rules that are repeatedly queried by the Agent

## Why this phase

The previous `sts2_guides/core/game_mechanics.json` file was valid, but many entries were broad summaries derived from general gameplay understanding rather than directly grounded in the current source.

Because the Agent increasingly relies on structured retrieval for QnA, Assist, and explanation workflows, it is more useful to store compact mechanic rules that correspond to actual code behavior.

## Source areas reviewed

The following source files were used as the basis for this update:

- `sts2/MegaCrit.Sts2.Core.Entities.Players/PlayerCombatState.cs`
- `sts2/MegaCrit.Sts2.Core.Entities.Players/Player.cs`
- `sts2/MegaCrit.Sts2.Core.Models.Powers/StrengthPower.cs`
- `sts2/MegaCrit.Sts2.Core.Models.Powers/DexterityPower.cs`
- `sts2/MegaCrit.Sts2.Core.Models.Powers/WeakPower.cs`
- `sts2/MegaCrit.Sts2.Core.Models.Powers/VulnerablePower.cs`
- `sts2/MegaCrit.Sts2.Core.Models.Powers/FrailPower.cs`
- `sts2/MegaCrit.Sts2.Core.Models.Powers/PoisonPower.cs`
- `sts2/MegaCrit.Sts2.Core.Models.Powers/BarricadePower.cs`
- `sts2/MegaCrit.Sts2.Core.Models.Powers/BlurPower.cs`
- `sts2/MegaCrit.Sts2.Core.Models.Powers/IntangiblePower.cs`
- `sts2/MegaCrit.Sts2.Core.Models.Powers/NoDrawPower.cs`
- `sts2/MegaCrit.Sts2.Core.Models.Powers/FocusPower.cs`
- `sts2/MegaCrit.Sts2.Core.Models.Powers/PlatingPower.cs`
- `sts2/MegaCrit.Sts2.Core.Models.Powers/RegenPower.cs`
- `sts2/MegaCrit.Sts2.Core.Models.Powers/RitualPower.cs`

## Changes made

Updated `sts2_guides/core/game_mechanics.json` to replace generic principles with source-backed rules for:

- baseline energy reset and resource legality checks
- default block clearing and combat cleanup
- Barricade and Blur block retention behavior
- Strength and Dexterity additive scaling
- Weak, Vulnerable, and Frail multipliers
- Poison turn-start damage and decay behavior
- Intangible damage cap behavior
- No Draw turn-scoped draw suppression
- Focus orb-value clamping
- Plating end-turn block and layer loss
- Regen end-turn healing
- Ritual end-turn Strength growth

## Notes on content style

The updated mechanic rules are intentionally concise and retrieval-friendly:

- each rule is short enough to inject into prompts
- each summary points to stable behavior rather than noisy implementation detail
- the file stays schema-compatible with the existing `MechanicRule` model

This keeps the knowledge base practical for Agent retrieval while still increasing factual grounding.

## Validation

Build validation completed successfully:

```powershell
dotnet build aibot\aibot.csproj -c Release
```

Result:

- build succeeded
- no C# code changes were required for this phase

## Outcome

The Agent now has a stronger structured mechanics reference for common combat rule explanations and source-backed reasoning.

This phase does not yet attempt to fully formalize the entire damage pipeline or every edge-case hook, but it materially improves the factual quality of the core mechanics dataset and moves the plan forward in a clean, incremental way.
