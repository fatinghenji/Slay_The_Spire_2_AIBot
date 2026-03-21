# Phase 24 - Potion target and formula expansion

## Goal

Continue the master plan by improving the structured potion knowledge model so it better matches the intended Agent retrieval surface.

This phase targets the remaining P0 potion-data gap:

- preserve existing potion names / rarity / usage / localized descriptions
- add explicit target typing
- add explicit effect-formula summaries
- expose those new fields through local knowledge retrieval

## Why this phase

Before this phase, `sts2_guides/core/potions.json` already contained a broad list of potion entries, but each entry was still relatively shallow.

The file mostly carried:

- name
- rarity
- usage timing
- localized description

That was enough for simple lookup, but it did not satisfy the plan's requirement that potion knowledge should include usage target information and numerical/effect formulas where available.

For practical Agent use, potion questions are often shaped like:

- “这个药水能对谁用？”
- “它到底回多少 / 给多少格挡 / 给几层状态？”
- “这是自用、指定目标，还是全体效果？”

Encoding those answers directly in the structured dataset is more reliable than forcing the Agent to infer them from description text every time.

## Source areas reviewed

The following read-only source files were used to ground this update:

- `sts2/MegaCrit.Sts2.Core.Models/PotionModel.cs`
- `sts2/MegaCrit.Sts2.Core.Models.Potions/Ashwater.cs`
- `sts2/MegaCrit.Sts2.Core.Models.Potions/AttackPotion.cs`
- `sts2/MegaCrit.Sts2.Core.Models.Potions/BlessingOfTheForge.cs`
- `sts2/MegaCrit.Sts2.Core.Models.Potions/BlockPotion.cs`
- `sts2/MegaCrit.Sts2.Core.Models.Potions/BloodPotion.cs`
- `sts2/MegaCrit.Sts2.Core.Models.Potions/DexterityPotion.cs`
- `sts2/MegaCrit.Sts2.Core.Models.Potions/DistilledChaos.cs`
- `sts2/MegaCrit.Sts2.Core.Models.Potions/Duplicator.cs`
- `sts2/MegaCrit.Sts2.Core.Models.Potions/EnergyPotion.cs`
- `sts2/MegaCrit.Sts2.Core.Models.Potions/EntropicBrew.cs`
- `sts2/MegaCrit.Sts2.Core.Models.Potions/ExplosiveAmpoule.cs`

## Changes made

### 1. Expanded `PotionEntry`

Updated `aibot/Scripts/Knowledge/GuideModels.cs` so each `PotionEntry` can now carry:

- `targetType`
- `effectFormulaEn`
- `effectFormulaZh`

This keeps the data model backward-compatible while letting the Agent answer more specific potion questions locally.

### 2. Updated knowledge search rendering

Updated `aibot/Scripts/Knowledge/KnowledgeSearchEngine.cs` so potion lookups now include:

- target type
- effect formula summaries

This improves local QnA quality without requiring extra LLM reasoning.

### 3. Enriched representative potion entries

Updated `sts2_guides/core/potions.json` with source-backed target / formula metadata for representative potions across several behavioral classes:

- `ashwater`
- `attack-potion`
- `blessing-of-the-forge`
- `block-potion`
- `blood-potion`
- `dexterity-potion`
- `distilled-chaos`
- `duplicator`
- `energy-potion`
- `entropic-brew`
- `explosive-ampoule`

These cover important potion categories:

- self-only utility
- targeted player effects
- all-enemy damage
- random generation / procurement
- power application
- automatic card play behavior

### 4. Updated schema documentation

Updated `sts2_guides/core/schema.json` to document the richer `PotionEntry` shape and bumped the schema version.

## Notes on scope

This phase intentionally improves the shape and retrieval quality of the current curated potion dataset.

It does **not** yet attempt to annotate every potion entry in the file with formula metadata.

That broader sweep can be completed later as a dedicated extraction / completion phase once more potion source classes are reviewed systematically.

## Validation

Build validation completed successfully:

```powershell
dotnet build aibot\aibot.csproj -c Release
```

Result:

- build succeeded
- no compile errors introduced by the new `PotionEntry` fields
- structured potion retrieval remained build-safe

## Outcome

The Agent can now answer potion questions with more operational detail:

- who the potion can target
- whether it is self-use, targeted player use, or all-enemy use
- what concrete effect or formula it applies

This moves the potion knowledge layer closer to the plan's target of a structured, game-specific Agent that can explain and reason about in-game actions reliably from local data.
