# Phase 27 - Relic trigger and condition schema expansion

## Goal

Continue the master plan by improving the structured relic knowledge model so local QnA and lookup flows can answer more operational relic questions directly.

This phase targets the remaining practical knowledge gap after the recent power / potion / enemy / event expansions:

- preserve the broad relic coverage already present in `relics.json`
- add explicit rarity metadata where curated
- add explicit trigger-window summaries
- add concise effect summaries
- add concise condition / drawback summaries

## Why this phase

By phase 26, `relics.json` already had wide source coverage, but most entries were still shallow.

The file mostly carried:

- name
- localized description
- optional character restriction

That was enough for basic lookup, but not enough for high-value questions like:

- “这个遗物什么时候触发？”
- “它主要给我什么收益？”
- “有没有限制、代价或对称副作用？”
- “这是开场型、每回合型，还是战后奖励型遗物？”

Encoding those answers directly in the structured dataset makes local retrieval more useful and reduces reliance on inference from raw description strings.

## Source areas reviewed

The following read-only source files were used to ground this update:

- `sts2/MegaCrit.Sts2.Core.Models/RelicModel.cs`
- `sts2/MegaCrit.Sts2.GameInfo.Objects/RelicInfo.cs`
- `sts2/MegaCrit.Sts2.Core.Models.Relics/Akabeko.cs`
- `sts2/MegaCrit.Sts2.Core.Models.Relics/Anchor.cs`
- `sts2/MegaCrit.Sts2.Core.Models.Relics/ArtOfWar.cs`
- `sts2/MegaCrit.Sts2.Core.Models.Relics/BagOfPreparation.cs`
- `sts2/MegaCrit.Sts2.Core.Models.Relics/BeltBuckle.cs`
- `sts2/MegaCrit.Sts2.Core.Models.Relics/BigMushroom.cs`
- `sts2/MegaCrit.Sts2.Core.Models.Relics/BingBong.cs`
- `sts2/MegaCrit.Sts2.Core.Models.Relics/BlackBlood.cs`
- `sts2/MegaCrit.Sts2.Core.Models.Relics/BlackStar.cs`
- `sts2/MegaCrit.Sts2.Core.Models.Relics/Brimstone.cs`

## Changes made

### 1. Expanded `RelicGuideEntry`

Updated `aibot/Scripts/Knowledge/GuideModels.cs` so each relic entry can now carry:

- `rarity`
- `triggerTimingEn`
- `triggerTimingZh`
- `effectSummaryEn`
- `effectSummaryZh`
- `conditionSummaryEn`
- `conditionSummaryZh`

This remains backward-compatible with the existing JSON while giving retrieval a more tactical surface.

### 2. Updated relic retrieval rendering

Updated:

- `aibot/Scripts/Knowledge/KnowledgeSearchEngine.cs`
- `aibot/Scripts/Agent/Tools/LookupRelicTool.cs`
- `aibot/Scripts/Knowledge/GuideKnowledgeBase.cs`

Effects:

- local knowledge answers now show rarity when available
- relic lookups can surface trigger windows directly
- effect summaries and condition / drawback summaries are shown before raw descriptions
- runtime relic summaries used elsewhere in the Agent can prefer the new concise structured text over longer raw descriptions

### 3. Enriched representative relic entries

Updated `sts2_guides/core/relics.json` with source-backed structured metadata for representative relics:

- `akabeko`
- `anchor`
- `art-of-war`
- `bag-of-preparation`
- `belt-buckle`
- `big-mushroom`
- `bing-bong`
- `black-blood`
- `black-star`
- `brimstone`

These cover several useful relic behavior classes:

- combat opener
- opening-hand modifier
- delayed next-turn reward
- conditional stat bonus
- tradeoff relic
- deck-growth duplicator
- post-combat sustain
- elite reward modifier
- symmetric scaling drawback relic

### 4. Updated schema documentation

Updated `sts2_guides/core/schema.json` to document the richer `RelicGuideEntry` shape and bumped the schema version.

## Notes on scope

This phase intentionally improves the shape and retrieval quality of the current curated relic dataset.

It does **not** yet attempt to annotate all 278 relic entries with trigger and condition metadata.

That broader sweep is still a worthwhile future phase and can be completed incrementally by relic family or trigger type.

## Validation

Ran:

```powershell
dotnet build aibot\aibot.csproj -c Release /p:CopyModAfterBuild=false
```

Result:

- build succeeded
- 0 warnings
- 0 errors

## Outcome

The Agent can now answer relic questions with more operational detail:

- what rarity bucket a relic belongs to
- when it mainly triggers
- what concrete benefit it provides
- whether it has conditions, symmetry, or drawbacks

This moves relic knowledge closer to the plan's target of a structured, game-specific Agent that can explain in-game objects reliably from local data.
