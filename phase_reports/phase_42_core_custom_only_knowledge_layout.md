# Phase 42 - Core/Custom-Only Knowledge Layout

## Background

Even after earlier cleanup, the knowledge directory still had two sources of confusion:

1. root-level legacy compatibility JSON files were still active,
2. the loader still referenced old fallback names and root-level lookup paths.

This made the directory layout look more complex than it really needed to be and could mislead players into editing obsolete files.

## Changes

### 1. Migrated remaining legacy-only data into `core`

- Added the missing `electrodynamics` card entry to `sts2_guides/core/cards.json`.
- Added the missing `dead-branch` relic entry to `sts2_guides/core/relics.json`.

This removed the last real content dependency on the root-level legacy compatibility JSON files.

### 2. Removed root-level legacy compatibility loading

- Updated `aibot/Scripts/Knowledge/GuideKnowledgeBase.cs`.
- The knowledge loader now reads from the canonical layout only:
  - `custom/<file>`
  - `custom/guides/<file>`
  - `core/<file>`
  - `core/guides/<file>`
- Removed root-level fallback loading and old alias references such as:
  - `00_OVERVIEW.md`
  - `sts2_knowledge_base.md`
  - `characters_full.json`
  - `builds_full.json`
  - `cards_full.json`
  - `relics_full.json`

### 3. Simplified the schema to match the canonical layout

- Updated `aibot/Scripts/Knowledge/KnowledgeSchema.cs`.
- Removed legacy JSON file names from the schema registry.
- Updated reserved Markdown file names so the documentation files are clearly treated as documentation rather than knowledge-guide candidates.

### 4. Deleted the root-level legacy JSON files

- Deleted:
  - `sts2_guides/characters_full.json`
  - `sts2_guides/builds_full.json`
  - `sts2_guides/cards_full.json`
  - `sts2_guides/relics_full.json`

## Validation

Build succeeded with:

```powershell
dotnet build aibot\aibot.csproj -c Release /p:CopyModAfterBuild=false
```

Result:

- `0 warnings`
- `0 errors`
- `aibot.dll` built successfully
- mod copy step completed successfully

Additional checks:

- `sts2_guides` root now contains only `core/` and `custom/`
- `core/cards.json` parses successfully
- `core/relics.json` parses successfully
- no remaining source references to the deleted legacy root file names

## Expected Outcome

- The knowledge directory is now structurally clear: players only need to understand `core` and `custom`.
- There is no longer any hidden root-level compatibility path that could cause edits to land in the wrong file.
- The on-disk layout now matches the mental model documented for users.
