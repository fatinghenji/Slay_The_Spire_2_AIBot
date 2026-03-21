# Phase 41 - Root Knowledge Directory Cleanup

## Background

The `sts2_guides` root directory still contained a mix of:

- active legacy compatibility files,
- inactive legacy markdown files that no longer win any load path,
- and the canonical `core` / `custom` folders.

This could mislead players into thinking every root-level file still mattered to the mod.

## Findings

Based on `GuideKnowledgeBase` loading order:

- root-level legacy JSON files are still effective:
  - `characters_full.json`
  - `builds_full.json`
  - `cards_full.json`
  - `relics_full.json`
- root-level legacy markdown files were no longer effective because canonical `core/guides/...` files win earlier in the alias resolution path:
  - `00_OVERVIEW.md`
  - `sts2_knowledge_base.md`
  - `ironclad_complete_guide.md`
  - `silent_complete_guide.md`
  - `defect_complete_guide.md`
  - `regent_complete_guide.md`
  - `necrobinder_complete_guide.md`

## Changes

- Deleted the inactive root-level markdown files listed above.
- Kept the root-level legacy JSON files because they are still part of the active compatibility load chain.

## Expected Outcome

- The `sts2_guides` root directory is now less misleading.
- Players should no longer mistake obsolete markdown backups for active knowledge sources.
- The only remaining root-level files are the still-effective legacy JSON compatibility files.
