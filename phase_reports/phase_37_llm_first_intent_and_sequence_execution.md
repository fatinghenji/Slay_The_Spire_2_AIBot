# Phase 37 - LLM-First Intent Parsing and Sequence Execution

## Background

Live testing revealed a structural weakness in the old interaction flow:

- `Semi Auto` first relied on local phrase matching and legality checks, and only used the LLM as fallback.
- This made natural language commands brittle and hard to scale.
- Multi-action instructions such as `帮我打出 打击 防御 打击` were especially poorly represented, because the old parser only modeled a single skill at a time.
- `QnA` also performed brittle front-loaded routing checks before the LLM had a chance to interpret the user's real intent.

## Changes

### 1. Switched Semi Auto intent parsing to LLM-first

- Updated `aibot/Scripts/Agent/IntentParser.cs`.
- Added a new `ParsedIntentKind.Sequence`.
- `ParseWithFallbackAsync(...)` now attempts LLM action-plan parsing first and only falls back to local rule parsing if no valid structured plan is returned.
- The local phrase parser remains as an offline/safety fallback instead of being the primary path.

### 2. Added structured action-plan parsing in the LLM bridge

- Updated `aibot/Scripts/Agent/AgentLlmBridge.cs`.
- Added `RecognizeActionPlanAsync(...)`.
- The LLM can now output one of four structured intent kinds:
  - `unknown`
  - `tool`
  - `skill`
  - `sequence`
- Added validation so the returned tool/skill names must exist in the local whitelist before they are accepted.
- Added structured JSON parsing for ordered multi-step action sequences.

### 3. Semi Auto now auto-executes legal skills and legal sequences

- Reworked `aibot/Scripts/Agent/Handlers/SemiAutoModeHandler.cs`.
- Semi Auto no longer routes recognized legal actions into a confirmation-only pending state.
- It now executes:
  - a single legal skill immediately,
  - a legal ordered sequence immediately,
  - a tool request directly,
  - current-turn autoplay/advice requests directly.
- Sequence execution stops if a later step becomes invalid after the board state changes, and returns the partial execution log.

### 4. QnA no longer relies on brittle front-loaded domain gating

- Reworked `aibot/Scripts/Agent/Handlers/QnAModeHandler.cs`.
- Removed the early hard guard that rejected many short natural-language questions before the LLM and knowledge layers had a chance to reason about them.
- Current-play advice, tool-like questions, local knowledge lookup, and LLM fallback now flow in a more natural order.

## Expected Player-Facing Outcome

- `Semi Auto` should understand much more free-form phrasing without depending on a huge hand-written phrase list.
- Commands like `帮我打出 打击 防御 打击` can now be represented as an ordered multi-step execution plan instead of collapsing into one `play_card`.
- `QnA` should be more forgiving with natural short questions because legality/domain interpretation is no longer blocked so early by rigid local checks.

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
