# Phase 30 - Ascension and ancient system rule expansion

## Goal

Continue the master plan by closing the remaining "special systems" knowledge gap in `game_mechanics.json`.

This phase targets the final P2-style system knowledge area that was still underrepresented after the earlier combat-rule expansions:

- ascension ladder behavior
- run-start ascension penalties
- ancient-event gating and healing behavior
- economy pressure tied to ascension

## Why this phase

By phase 29, the Agent had broad coverage for cards, relics, potions, powers, enemies, events, relic structure, and enchantments.

The remaining obvious hole from the plan was the note about:

- ascension
- ancients
- special systems

Those systems are important because they change the run baseline before the player even reaches a normal decision point. Encoding them as mechanic rules is a better fit than scattering them across unrelated lookup entries.

## Source areas reviewed

The following read-only source files were used for this phase:

- `sts2/MegaCrit.Sts2.Core.Entities.Ascension/AscensionLevel.cs`
- `sts2/MegaCrit.Sts2.Core.Entities.Ascension/AscensionManager.cs`
- `sts2/MegaCrit.Sts2.Core.Helpers/AscensionHelper.cs`
- `sts2/MegaCrit.Sts2.Core.Models/AncientEventModel.cs`
- `sts2/MegaCrit.Sts2.Core.Models.Cards/AscendersBane.cs`

## Changes made

Updated `sts2_guides/core/game_mechanics.json` with new source-backed rules for:

- ascension levels behaving as cumulative thresholds up to 10
- Tight Belt reducing max potion slots by 1
- Ascender's Bane being added to the starting deck at the relevant ascension threshold
- Poverty using a 0.75 gold multiplier baseline
- ancient events healing missing HP first, with Weary Traveler reducing that heal to 80%, and blocked ancients falling back to a simple Proceed flow

## Validation

Validation for this phase should include:

```powershell
dotnet build aibot\\aibot.csproj -c Release /p:CopyModAfterBuild=false
```

## Outcome

This phase turns the remaining ascension / ancient system behavior into explicit local mechanic rules, which gives the Agent one more reliable source-backed answer surface for run-level questions that are not tied to a single card, relic, or event entry.
