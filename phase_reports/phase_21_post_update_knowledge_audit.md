# Phase 21 - Post-update knowledge audit

## Goal

Re-audit the knowledge base after the latest Slay the Spire 2 source update and confirm whether `sts2_guides/` still matches the current game data exposed under `sts2/`.

Constraints preserved throughout this pass:

- only `aibot/` and `sts2_guides/` may be modified
- `sts2/` is read-only and used only as the source of truth
- validate before moving on to the next planned feature phase

## Audit method

I compared the core guide datasets against the current source models and game-info object layer:

- guide schemas reviewed in `aibot/Scripts/Knowledge/GuideModels.cs`
- source-of-truth types reviewed under `sts2/MegaCrit.Sts2.GameInfo.Objects/`
- source coverage compared against `sts2/MegaCrit.Sts2.Core.Models.*`
- special metadata spot-check performed for potion rarity / usage
- numeric-risk scan performed to identify guide entries with hard-coded numbers that may deserve future targeted review

## Findings

### Fully aligned datasets

- `potions.json`: full source coverage, no rarity mismatch, no usage mismatch
- `enchantments.json`: full source coverage
- `events.json`: now full source coverage after this phase's update

### Near-aligned datasets

- `cards.json`: only missing `deprecated-card`, which appears to be a removed/internal placeholder rather than live gameplay knowledge
- `enemies.json`: the apparent gaps are training/base entities (`multi-attack-move-monster`, `one-hp-monster`, `ten-hp-monster`) plus split `decimillipede` segment classes; current guide coverage is still functionally sufficient for player-facing decision support
- `relics.json`: missing source items are internal/fake/helper entries (`fake-*`, `deprecated-relic`, `vakuu-card-selector`) rather than normal collectible relic knowledge

### Curated dataset note

- `powers.json` remains a deliberately curated glossary-style subset rather than a 1:1 mirror of all source power classes
- the large source/guide count delta is therefore not treated as a regression introduced by this update
- if we later decide to expand power coverage, that should be a separate scoped knowledge project rather than part of this hot audit

### Numeric-risk scan

A scan found 154 entries in the current core guides that contain explicit numeric literals inside text. This is not proof of mismatch, but it highlights where future balance updates are most likely to make guide text stale.

High-signal examples include cards such as:

- `acrobatics`
- `adrenaline`
- `backflip`
- `burst`
- `glacier`
- `meteor-strike`
- `nightmare`

## Changes made

Updated `sts2_guides/core/events.json` to add missing player-facing source events:

- `darv`
- `deprecated-ancient-event`
- `neow`
- `nonupeipe`
- `orobas`
- `pael`
- `tanx`
- `tezcatara`
- `the-architect`
- `vakuu`

These were confirmed from the current event model sources and represented true knowledge gaps.

## Validation

Build validation completed successfully:

```powershell
dotnet build aibot\aibot.csproj -c Release
```

Result:

- build succeeded
- no code changes were required in `aibot/`

## Outcome

The knowledge base is now back in sync for the source-confirmed event gap introduced or exposed by the update.

No urgent post-update mismatches were found in potions or enchantments, and no evidence was found that the remaining relic/enemy gaps represent missing live player-facing knowledge.

The next recommended step is to continue the planned feature work, with an optional future follow-up pass focused specifically on numeric-literal guide text hardening.
