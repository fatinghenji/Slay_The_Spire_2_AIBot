# Phase 38 - QnA Tool Augmentation and Full Reason Display

## Background

After switching to LLM-first intent parsing, one important QnA gap remained:

1. Asking `我该出什么` in `QnA` could still miss the direct combat-advice route and fall into a generic LLM answer such as "缺少手牌信息".
2. The displayed combat recommendation reason often looked truncated or too generic.
3. QnA still answered many questions in a passive way instead of first deciding whether it should fetch more context through existing tools.

## Changes

### 1. Expanded direct combat-advice phrase coverage

- Reworked `aibot/Scripts/Agent/CombatAdvisor.cs`.
- `IsCombatAdviceQuestion(...)` now explicitly recognizes shorter natural variants such as:
  - `我该出什么`
  - `我应该出什么`
  - `我该打什么`
- This makes QnA route these questions directly into the current combat decision engine instead of falling back to a generic text answer.

### 2. Show the full combat rationale instead of the short summary

- `CombatAdvisor.FormatRecommendation(...)` now prefers `decision.Trace.Details` when available.
- Previously the player-facing reply mostly showed `decision.Reason`, which is often only a short summary such as "DeepSeek selected X".
- The detailed model rationale was already available in `DecisionTrace`, but it was not being surfaced in the final recommendation text.

### 3. Added tool-augmented QnA answering

- Reworked `aibot/Scripts/Agent/Handlers/QnAModeHandler.cs`.
- When local knowledge is insufficient, QnA now:
  - asks the LLM whether one of the registered tools should be used,
  - executes the selected tool if valid,
  - passes the resulting tool output back into the final LLM answer as supplemental context.
- This is closer to the intended flow of:
  - decide what information is missing,
  - fetch it,
  - then answer.

### 4. Added supplemental-context support in the LLM question bridge

- Updated `aibot/Scripts/Agent/AgentLlmBridge.cs`.
- `AnswerQuestionAsync(...)` now accepts an extra `supplementalContext` argument.
- That context is included in the question prompt under `Supplemental tool context`.

### 5. Unified enemy inspection tool naming

- Updated `aibot/Scripts/Agent/Tools/InspectEnemyTool.cs`.
- Renamed the registered tool name from `inspect_enemies` to `inspect_enemy` so it now matches the rest of the codebase and QnA/tool routing logic.

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

- `QnA` should now handle `我该出什么` as a direct current-turn combat advice question.
- Recommendation replies should show fuller decision reasoning instead of only a short generic summary.
- QnA should make better use of existing in-game tools before giving up with a vague or underspecified answer.
