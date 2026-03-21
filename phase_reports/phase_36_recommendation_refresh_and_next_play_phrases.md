# Phase 36 - Recommendation Refresh and Next-Play Phrase Support

## Background

After the previous gameplay fixes, two live issues remained:

1. `Assist` mode could still briefly repeat an outdated combat recommendation, and the new recommendation sometimes arrived late.
2. In `Semi Auto` and `QnA`, phrases like "接下来我应该出什么" still failed to route to combat advice.

## Changes

### 1. Broadened combat-advice phrase matching

- Reworked `aibot/Scripts/Agent/CombatAdvisor.cs` into a clean UTF-8 version.
- `IsCombatAdviceQuestion(...)` now recognizes both "current play" and "next play" phrasing, including variants such as:
  - `现在我应该出什么`
  - `接下来我应该出什么`
  - `下一步该出什么`
  - `接下来怎么出牌`
  - `what should I play next`
- This applies to both `Semi Auto` and `QnA`, because both handlers call the same `CombatAdvisor` helper.

### 2. Reduced stale recommendation carry-over in Assist mode

- Updated `aibot/Scripts/Ui/AgentRecommendOverlay.cs`.
- When the detected interaction signature changes, the old badge is now cleared immediately instead of staying visible while a new async recommendation is still computing.
- Added a queued-signature mechanism so if the game state changes during an in-flight refresh, the overlay is forced to refresh again as soon as the current refresh completes.
- Reduced the minimum refresh interval from `150ms` to `50ms`.

### 3. Switched combat refresh signature to real combat state

- The combat signature previously depended on visible `NCardHolder` UI nodes.
- That can lag behind actual hand state during animation and cause the overlay to think the old state is still current.
- The combat signature now uses runtime combat data instead:
  - current energy,
  - actual hand card ids,
  - alive enemy HP/block.
- This makes stale badges much less likely after the player has already played the previously recommended card.

### 4. Drop stale async recommendation results

- `RefreshAsync(...)` now receives the expected signature it started from.
- If the current UI state no longer matches that signature by the time the refresh finishes, the stale badge is cleared instead of being left on screen.

## Validation

Build succeeded with:

```powershell
dotnet build aibot\aibot.csproj -c Release /p:CopyModAfterBuild=false
```

Result:

- `0 errors`
- output DLL compiled successfully

Note:

- Copying into the game mod folder did **not** complete during validation because `SlayTheSpire2.exe` was still locking `mods\aibot\aibot.dll`.

## Expected Player-Facing Outcome

- `Assist` mode should stop showing an old recommendation for as long after the player has already used that card.
- New combat recommendations should appear faster after the board state changes.
- `Semi Auto` / `QnA` should now understand "接下来我应该出什么" as a valid combat-advice question.
