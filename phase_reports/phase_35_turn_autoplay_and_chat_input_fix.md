# Phase 35 - Turn Autoplay and Chat Input Fix

## Background

After live testing, three gaps remained in the agent interaction flow:

1. In `Semi Auto`, saying "帮我打这一回合" only executed one action instead of finishing the whole turn.
2. In both `Semi Auto` and `QnA`, asking "现在我应该出什么" could still fall through to "unrecognized input".
3. While typing Chinese with an IME in the chat box, keystrokes were sometimes swallowed by the base game UI, causing missing characters.

## Changes

### 1. Added full-turn autoplay loop

- Reworked `aibot/Scripts/Agent/CombatAdvisor.cs`.
- Added `PlayWholeTurnAsync(...)`, which repeatedly:
  - queries the current best combat action,
  - executes it,
  - continues until the engine recommends ending the turn or the combat state stops changing.
- Added broader natural-language matching for:
  - turn execution requests like "帮我打这一回合",
  - combat advice questions like "现在我应该出什么".
- Replaced the previous anti-loop check with a board-state signature based on:
  - current energy,
  - current hand contents,
  - visible enemy HP/block,
  so repeated same-title cards do not prematurely stop the loop.

### 2. Fixed Semi Auto current-play understanding

- Reworked `aibot/Scripts/Agent/Handlers/SemiAutoModeHandler.cs`.
- `OnUserInputAsync(...)` now handles two combat-native natural-language intents before generic parser fallback:
  - "帮我打这一回合" -> executes `CombatAdvisor.PlayWholeTurnAsync(...)`
  - "现在我应该出什么" -> returns `CombatAdvisor.FormatRecommendation(...)`
- Improved bilingual responses and help text so the handler stays consistent with the new language switch system.

### 3. Fixed QnA current-play question routing

- Reworked `aibot/Scripts/Agent/Handlers/QnAModeHandler.cs`.
- Moved combat-advice detection ahead of the "in-game domain" guardrail.
- This prevents short combat questions like "现在我应该出什么" from being rejected before advice logic gets a chance to run.
- Kept tool-style knowledge lookups and local/LLM fallback behavior intact.

### 4. Reduced chat input swallowing under Chinese IME

- Reworked `aibot/Scripts/Ui/AgentChatDialog.cs`.
- Added stronger keyboard capture while the `LineEdit` has focus:
  - chat hotkey polling is suppressed while the text box is focused,
  - `_ShortcutInput(...)` and `_UnhandledKeyInput(...)` mark focused keyboard input as handled after the text box has had a chance to process it.
- This is designed to keep text input inside the chat field instead of leaking to the base game UI.

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

- `Semi Auto`: "帮我打这一回合" should now continue playing until the turn is actually finished.
- `Semi Auto` / `QnA`: "现在我应该出什么" should now return a current-turn recommendation instead of "未识别".
- Chat input should be noticeably more stable when using a Chinese IME.
