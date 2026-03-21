# Phase 43 - Project-Level Bilingual README And Documentation Cleanup

## Background

The repository had already been cleaned up structurally, but it still lacked a proper project-level entry document at the root:

1. there was no root README in English,
2. there was no unified project introduction for new players or contributors,
3. important information was split across code, phase reports, and the custom knowledge guide,
4. GitHub would not present the project clearly to Chinese-speaking users by default.

This made the repo harder to understand even though the actual codebase and knowledge layout had already become much clearer.

## Changes

### 1. Added a root Chinese README

- Added `README.md` at the repository root as the default GitHub landing document.
- Positioned it as the main onboarding and maintenance guide for Chinese-speaking users.
- Included:
  - project purpose
  - feature overview
  - mode descriptions
  - decision coverage
  - knowledge base layout
  - custom knowledge guidance
  - configuration explanation
  - build and deployment notes
  - internal architecture entry points
  - phase report navigation
  - known scope boundaries
  - future enhancement directions

### 2. Added a matching English README

- Added `README_en.md` at the repository root.
- Kept the overall structure aligned with the Chinese root README.
- Added cross-links between the two root README files so bilingual users can switch directly.

### 3. Aligned the root documentation with the cleaned knowledge layout

- Documented that `sts2_guides` now uses the canonical `core/` and `custom/` layout only.
- Linked the project-level README to:
  - `sts2_guides/custom/README.md`
  - `sts2_guides/custom/README_en.md`
- Reduced the chance that users would look for deleted legacy knowledge files or misunderstand which files actually take effect.

## Validation

Documentation-only change.

Checks performed:

- confirmed the repository root now contains both `README.md` and `README_en.md`
- confirmed both README files link to each other
- confirmed the documentation points users toward the canonical `core/custom` knowledge layout
- confirmed the custom knowledge guide is linked from the project-level documentation

## Expected Outcome

- GitHub now presents a clear root-level project introduction.
- Chinese users get a detailed default landing page.
- English-speaking users have a matching entry document instead of having to infer project behavior from code and phase reports.
- Players and contributors are less likely to misunderstand the knowledge-base structure or edit the wrong files.
