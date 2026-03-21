# Phase 19 - Unified Conversation Session Management

## Goal

Complete the shared session-management task from the plan by moving SemiAuto / QnA conversation history out of the UI layer and into a reusable Agent-level session service.

## Scope

Only `aibot/` was modified. No changes were made under `sts2/`.

## What changed

### 1. Added a shared conversation session manager

Added `aibot/Scripts/Agent/AgentConversationSessionManager.cs`.

Responsibilities:
- store structured conversation messages per `AgentMode`
- keep user / agent / system roles explicit
- trim history centrally via `Agent.MaxConversationHistory`
- expose recent message snapshots for UI rendering
- build compact transcripts for LLM prompts

### 2. AgentCore now owns real session state

Updated:
- `aibot/Scripts/Agent/AgentCore.cs`

Effects:
- `AgentCore` now creates and owns `ConversationSessions`
- SemiAuto and QnA user inputs are recorded before handler execution
- SemiAuto and QnA agent responses are recorded after handler execution
- history trimming is no longer only a chat-panel concern

### 3. LLM bridge now receives recent conversation context

Updated:
- `aibot/Scripts/Agent/AgentLlmBridge.cs`
- `aibot/Scripts/Agent/Handlers/QnAModeHandler.cs`
- `aibot/Scripts/Agent/IntentParser.cs`

Effects:
- QnA cloud answers can see recent QnA turns, not just the current question
- SemiAuto LLM intent fallback can see recent SemiAuto interaction context
- the current user turn is excluded from the appended transcript when building prompt context, avoiding duplicate injection

### 4. Chat UI now renders shared session history instead of owning it

Updated:
- `aibot/Scripts/Ui/AgentChatDialog.cs`

Effects:
- mode activation system messages are recorded into shared session state
- the dialog syncs from `AgentCore.ConversationSessions`
- per-frame UI trimming logic is removed from the dialog
- pending local queue entries are drained when session sync completes, avoiding stale `处理中...` messages from reappearing
- mode-specific history persists consistently across SemiAuto and QnA reopen / reuse flows

## Implementation notes

- The shared session manager is intentionally mode-scoped so QnA and SemiAuto can preserve separate histories without cross-contaminating prompts.
- The UI still uses a small pending queue for immediate local feedback, but the canonical source of truth is now the Agent session service.
- This phase keeps behavior surgical: it does not introduce summarization, persistence-to-disk, or cross-run memory.

## Validation

Ran:

```powershell
dotnet build aibot\aibot.csproj -c Release
```

Result:
- build succeeded
- the new shared session manager, LLM prompt wiring, and chat dialog sync compiled cleanly together

## Follow-up notes

- If future phases add richer multi-turn planning, the session manager is now the correct place for transcript summarization or token-budget compression.
- If Assist mode later needs user-visible chat continuity, it can be attached to the same session service without reintroducing UI-owned history.
