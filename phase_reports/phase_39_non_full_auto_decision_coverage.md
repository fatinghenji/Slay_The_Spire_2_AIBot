# Phase 39 - Non-Full-Auto Decision Coverage Expansion

## Background

After live testing, a structural gap became clear:

1. `Full Auto` could already handle reward picking, map routing, relic choice, shop, rest site, and event decisions through `AiBotRuntime`.
2. `Semi Auto` and `QnA` were much stronger in combat than on non-combat decision screens.
3. `Assist` mode could recommend on several screens, but map routing had a stricter visibility filter than `Full Auto`, and treasure-room relic choice was not fully covered in the recommendation flow.

This meant three user-visible issues:

- In `Semi Auto`, inputs like `帮我选择奖励` or `帮我选一条路` could miss the intended current-screen action.
- In `QnA`, questions like `帮我选一条路` could fail to return the expected recommendation even though the decision engine already had enough information.
- In `Assist`, map-route recommendation could disappear on valid route-selection screens, and treasure-room relic choice did not have parity with other relic-choice surfaces.

## Changes

### 1. Added shared current-decision routing for `Semi Auto` and `QnA`

- Added `aibot/Scripts/Agent/CurrentDecisionAdvisor.cs`.
- This helper detects whether the player is currently on a decision screen and routes non-combat requests directly to the relevant decision-backed action or recommendation path.
- Covered decision contexts now include:
  - card reward
  - rewards screen
  - bundle selection
  - overlay relic selection
  - treasure-room relic selection
  - Crystal Sphere
  - map route selection
  - merchant/shop
  - rest site
  - event options

### 2. `Semi Auto` now executes current-screen decisions directly

- Updated `aibot/Scripts/Agent/Handlers/SemiAutoModeHandler.cs`.
- Before falling back to generic intent parsing, `Semi Auto` now checks whether the input looks like a current decision request and, if so, executes the correct skill for the active screen.
- This brings requests such as `帮我选择奖励` and `帮我选一条路` much closer to `Full Auto` behavior.

### 3. `QnA` now answers current-screen decisions directly

- Updated `aibot/Scripts/Agent/Handlers/QnAModeHandler.cs`.
- Before generic tool-like QnA routing, `QnA` now checks whether the player is on a decision screen and returns a decision-engine-backed recommendation for that exact screen.
- This lets questions such as `帮我选奖励` or `帮我选一条路` return a proper recommendation instead of slipping into unrelated retrieval logic.

### 4. Upgraded key skills to use the decision engine as fallback

- Reworked:
  - `aibot/Scripts/Agent/Skills/PickCardRewardSkill.cs`
  - `aibot/Scripts/Agent/Skills/NavigateMapSkill.cs`
  - `aibot/Scripts/Agent/Skills/ClaimRewardSkill.cs`
- Previously these skills could fall back to simplistic "pick the first option" behavior.
- They now use the same decision engine used by `Full Auto` whenever the player does not specify an explicit option.

### 5. Fixed `Assist` mode map recommendation parity

- Updated `aibot/Scripts/Ui/AgentRecommendOverlay.cs`.
- Removed the extra `IsVisibleInTree()` restriction from map candidate filtering so the route recommendation logic now matches `Full Auto` more closely.
- Added recommendation support for treasure-room relic selection.
- Updated the overlay signature logic so treasure-room relic choices also trigger refreshes correctly.

### 6. Refreshed `Assist` mode coverage text

- Updated `aibot/Scripts/Agent/Handlers/AssistModeHandler.cs`.
- The player-facing message now reflects the broader recommendation coverage instead of only mentioning the earlier subset of supported screens.

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

## Expected Player-Facing Outcome

- `Semi Auto` should now handle requests like `帮我选择奖励` and `帮我选一条路` by executing the current decision directly.
- `QnA` should now answer reward/map/shop/rest/event/relic decision questions with decision-engine-backed recommendations instead of missing the current context.
- `Assist` should now provide route recommendations more reliably on the map screen and should also support treasure-room relic recommendations.
