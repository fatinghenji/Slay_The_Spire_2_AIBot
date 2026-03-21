# Phase 28 - SemiAuto executable skill expansion

## Goal

Continue the master plan by closing the largest remaining `Skill` execution gap in SemiAuto mode.

Before this phase, several skills had already been registered in the Agent whitelist, but they still returned `NotReady(...)` and could not actually drive the existing in-game UI flows.

This phase focuses on turning those declared skills into real executable capabilities by wiring them to the runtime's existing overlay / room interaction paths.

## Why this phase

The earlier Agent phases already established:

- mode handlers
- skill / tool registration
- SemiAuto chat and intent parsing
- confirmation flow

But a meaningful part of the promised Agent action surface was still missing in practice:

- relic selection
- event choices
- shop purchasing
- rest-site choices
- Crystal Sphere actions
- bundle choices
- generic choose-a-card overlays

As long as those skills stayed stubbed, SemiAuto mode could parse more intents than it could actually execute.

That created an architectural mismatch with the plan's goal of a constrained but truly executable game Agent.

## Scope

Only `aibot/` and `phase_reports/` were modified.

No changes were made under `sts2/`.

## Changes made

### 1. Added shared runtime-backed skill helpers

Updated:

- `aibot/Scripts/Agent/Skills/RuntimeBackedSkillBase.cs`

Added reusable helpers for runtime-backed skill execution:

- waiting for the action queue to drain
- applying UI delay consistently after interaction
- resolving absolute room nodes
- normalized text matching
- parsing zero-based `index:*` option hints

This keeps the new skill implementations consistent instead of each file re-implementing queue / UI timing helpers.

### 2. Replaced stub skills with real executable flows

Updated:

- `aibot/Scripts/Agent/Skills/ChooseRelicSkill.cs`
- `aibot/Scripts/Agent/Skills/ChooseEventOptionSkill.cs`
- `aibot/Scripts/Agent/Skills/PurchaseShopSkill.cs`
- `aibot/Scripts/Agent/Skills/RestSiteSkill.cs`
- `aibot/Scripts/Agent/Skills/CrystalSphereSkill.cs`
- `aibot/Scripts/Agent/Skills/ChooseBundleSkill.cs`
- `aibot/Scripts/Agent/Skills/SelectCardSkill.cs`

Effects:

- `choose_relic` now works on both overlay relic selection and treasure-room relic choices
- `choose_event_option` now clicks unlocked event options and supports explicit proceed requests
- `purchase_shop` now opens merchant inventory if needed, resolves entries by name / index, and executes real purchases including card removal
- `rest_site` now executes enabled rest-site buttons and can still fall back to the decision engine when no explicit option is given
- `crystal_sphere` now supports proceed, tool choice, target cell selection, and decision-engine fallback
- `choose_bundle` now supports explicit index / card-name matching and confirmation
- `select_card` now executes the generic choose-a-card overlay instead of returning `NotReady`

This removes the remaining obvious `NotReady(...)` execution holes from the registered skill set.

### 3. Expanded SemiAuto intent parsing for the new skills

Updated:

- `aibot/Scripts/Agent/IntentParser.cs`

New locally parsed action categories now include:

- relic choice
- bundle choice
- shop purchase
- rest-site actions
- event option choice
- Crystal Sphere actions
- generic card-selection overlays

Also added:

- ordinal parsing such as first / second / third and numeric `第N个`
- contextual index routing for currently open overlay screens
- simple keyword normalization for `skip` / `proceed`
- basic Crystal Sphere coordinate parsing

This makes the newly executable skills reachable from SemiAuto natural-language input without depending on cloud fallback.

## Notes on behavior

- Explicit user intent still wins when a name / option / index is provided.
- If the user gives no explicit target and the runtime decision engine is available, the skill now falls back to the existing decision engine instead of hard-failing.
- The implementation intentionally reuses the game's current visible UI nodes and existing runtime behavior rather than inventing a parallel action path.

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

SemiAuto mode now has a much more honest execution surface:

- more registered skills are truly executable
- natural-language commands map to real game actions more often
- the Agent architecture is closer to the plan's intended "restricted but actionable" design

This phase also makes future work easier, because remaining SemiAuto improvements can now build on live skills instead of placeholders.
