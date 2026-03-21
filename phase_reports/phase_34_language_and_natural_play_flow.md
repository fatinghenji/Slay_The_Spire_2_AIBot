# Phase 34 - Language Toggle And Natural Play Flow

## Background

User feedback after Phase 33 surfaced five concrete issues:

1. The UI and replies still mixed Chinese and English.
2. Semi Auto did not correctly handle natural-language turn execution requests like "帮我打这一个回合".
3. QnA did not correctly answer "现在应该出什么" style questions.
4. Assist mode recommendation badges were too small.
5. Assist mode could briefly keep recommending a card that had already been played until the next refresh.

## Changes

### 1. Added runtime language switching

Updated:

- `aibot/Scripts/Config/AiBotConfig.cs`
- `aibot/config.json`
- `aibot/Scripts/Core/AiBotRuntime.cs`
- `aibot/Scripts/Localization/AiBotText.cs`

Added a UI language setting with:

- default `zh-CN`
- support for `zh-CN` and `en-US`
- runtime persistence back to `config.json`
- a `UiLanguageChanged` event so open UI panels refresh immediately

### 2. Added a language selector to the right-side control panel

Updated `aibot/Scripts/Ui/AiBotDecisionPanel.cs`:

- added a Chinese/English selector in the panel
- localized panel title, mode names, button labels, confirmation text, and status text
- kept mode switching and chat toggle in the same right-side control panel

### 3. Localized chat UI

Updated `aibot/Scripts/Ui/AgentChatDialog.cs`:

- localized chat title, placeholder, send button, and pending action buttons
- refreshes immediately when language changes
- role labels (`系统` / `你` / `Agent`) now follow the selected language

### 4. Improved natural-language Semi Auto execution

Updated `aibot/Scripts/Agent/Handlers/SemiAutoModeHandler.cs`
Added `aibot/Scripts/Agent/CombatAdvisor.cs`

Semi Auto now explicitly recognizes "play this turn for me" style requests and:

- asks the current decision engine for the best combat action
- executes that action directly
- returns a localized execution summary

This is separate from the ordinary parser-confirmation path, so users can still issue precise commands when they want to.

### 5. Improved QnA current-play answers

Updated `aibot/Scripts/Agent/Handlers/QnAModeHandler.cs`
Added `aibot/Scripts/Agent/CombatAdvisor.cs`

QnA now explicitly recognizes "现在应该出什么" / "what should I play now" style questions and:

- asks the current decision engine for a combat recommendation
- responds with a localized suggestion instead of falling through to generic knowledge search

### 6. Localized LLM answer language

Updated:

- `aibot/Scripts/Agent/AgentLlmBridge.cs`
- `aibot/Scripts/Decision/DeepSeekDecisionEngine.cs`

The selected UI language is now also used to steer:

- QnA free-form answers
- LLM reasoning text returned in decision traces

This reduces Chinese/English mixing in user-facing answers and recommendation reasons.

### 7. Improved Assist recommendation readability and freshness

Updated `aibot/Scripts/Ui/AgentRecommendOverlay.cs`

Changes:

- increased recommendation badge padding
- increased recommendation badge font size
- localized the badge label
- reduced recommendation refresh interval
- suppress recommendations while the action queue is still resolving
- restricted combat badge attachment to card holders that still correspond to the current hand

This prevents the overlay from briefly repeating recommendations on cards that have already been played but are still animating out.

## Additional Fix

Updated `aibot/Scripts/Agent/IntentParser.cs`:

- corrected the enemy inspection tool mapping from `inspect_enemies` to the actual registered tool `inspect_enemy`

## Validation

- `dotnet build aibot\\aibot.csproj -c Release /p:CopyModAfterBuild=false`
- Result: success, `0 warnings / 0 errors`
- The build also successfully copied the updated mod output into the game mod folder in this run.
