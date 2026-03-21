# Phase 29 - Enchantment schema expansion

## Goal

Continue the master plan by turning enchantment knowledge from shallow text lookup into structured, retrieval-friendly data.

This phase targets the remaining P2 knowledge gap around `enchantments.json`:

- preserve the full enchantment list
- add explicit applicability summaries
- add explicit trigger-window summaries
- add concise effect summaries
- add concise condition / limitation summaries

## Why this phase

Before this phase, local enchantment knowledge was much thinner than the other structured datasets expanded in phases 23 through 27.

The file effectively only exposed:

- enchantment name
- localized description
- source

That was enough for raw lookup, but not enough for questions like:

- "这个附魔适合什么牌？"
- "它什么时候触发？"
- "它是一次性效果、永久改造，还是战斗内成长？"
- "它有什么限制或代价？"

This phase brings enchantments up to the same structured retrieval standard as powers, potions, enemies, events, and relics.

## Source areas reviewed

The following read-only source files were used to ground the new structured fields:

- `sts2/MegaCrit.Sts2.Core.Models/EnchantmentModel.cs`
- `sts2/MegaCrit.Sts2.GameInfo.Objects/EnchantmentInfo.cs`
- `sts2/MegaCrit.Sts2.Core.Models.Enchantments/Adroit.cs`
- `sts2/MegaCrit.Sts2.Core.Models.Enchantments/Clone.cs`
- `sts2/MegaCrit.Sts2.Core.Models.Enchantments/Corrupted.cs`
- `sts2/MegaCrit.Sts2.Core.Models.Enchantments/DeprecatedEnchantment.cs`
- `sts2/MegaCrit.Sts2.Core.Models.Enchantments/Favored.cs`
- `sts2/MegaCrit.Sts2.Core.Models.Enchantments/Glam.cs`
- `sts2/MegaCrit.Sts2.Core.Models.Enchantments/Goopy.cs`
- `sts2/MegaCrit.Sts2.Core.Models.Enchantments/Imbued.cs`
- `sts2/MegaCrit.Sts2.Core.Models.Enchantments/Instinct.cs`
- `sts2/MegaCrit.Sts2.Core.Models.Enchantments/Momentum.cs`
- `sts2/MegaCrit.Sts2.Core.Models.Enchantments/Nimble.cs`
- `sts2/MegaCrit.Sts2.Core.Models.Enchantments/PerfectFit.cs`
- `sts2/MegaCrit.Sts2.Core.Models.Enchantments/RoyallyApproved.cs`
- `sts2/MegaCrit.Sts2.Core.Models.Enchantments/Sharp.cs`
- `sts2/MegaCrit.Sts2.Core.Models.Enchantments/Slither.cs`
- `sts2/MegaCrit.Sts2.Core.Models.Enchantments/SlumberingEssence.cs`
- `sts2/MegaCrit.Sts2.Core.Models.Enchantments/SoulsPower.cs`
- `sts2/MegaCrit.Sts2.Core.Models.Enchantments/Sown.cs`
- `sts2/MegaCrit.Sts2.Core.Models.Enchantments/Spiral.cs`
- `sts2/MegaCrit.Sts2.Core.Models.Enchantments/Steady.cs`
- `sts2/MegaCrit.Sts2.Core.Models.Enchantments/Swift.cs`
- `sts2/MegaCrit.Sts2.Core.Models.Enchantments/TezcatarasEmber.cs`
- `sts2/MegaCrit.Sts2.Core.Models.Enchantments/Vigorous.cs`

## Changes made

### 1. Expanded `EnchantmentEntry`

Updated `aibot/Scripts/Knowledge/GuideModels.cs` so each enchantment entry can now carry:

- `applicableToEn`
- `applicableToZh`
- `triggerTimingEn`
- `triggerTimingZh`
- `effectSummaryEn`
- `effectSummaryZh`
- `conditionSummaryEn`
- `conditionSummaryZh`

This remains backward-compatible at the model level while making the data much more useful for local QnA.

### 2. Updated knowledge search rendering

Updated `aibot/Scripts/Knowledge/KnowledgeSearchEngine.cs` so enchantment lookups now include:

- card applicability
- trigger timing
- effect summary
- condition / limitation summary

This lets local retrieval answer practical enchantment questions without leaning on free-form inference.

### 3. Rebuilt `enchantments.json` into valid structured data

Updated `sts2_guides/core/enchantments.json` by:

- replacing the previously malformed JSON payload with a valid UTF-8 JSON file
- keeping full enchantment coverage
- enriching every listed enchantment with structured bilingual metadata

Representative coverage now includes:

- on-play reward enchantments like `adroit`, `sown`, and `swift`
- damage-modifier enchantments like `favored`, `sharp`, `momentum`, and `vigorous`
- permanent deck-modification enchantments like `instinct`, `steady`, `souls-power`, and `tezcataras-ember`
- event-like or utility enchantments such as `clone`, `perfect-fit`, and `slither`

### 4. Updated schema notes

Updated `sts2_guides/core/schema.json`:

- schema version bumped to `7`
- enchantment notes added so future edits know which structured fields are expected

## Validation

Validation for this phase should include:

```powershell
dotnet build aibot\\aibot.csproj -c Release /p:CopyModAfterBuild=false
```

## Outcome

After this phase, enchantments are no longer a shallow appendix in the knowledge base.

They now expose the same kind of practical structured retrieval surface already established for other major gameplay entities, which reduces one more obvious remaining gap in the master plan.
