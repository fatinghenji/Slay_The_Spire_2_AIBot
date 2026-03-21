# Phase 31 - Global hotkey input routing fix

## Goal

Fix non-responsive Agent hotkeys observed during real gameplay testing.

Reported symptoms:

- `F5` / `F6` / `F7` / `F8` produced no visible mode-switch response
- `Tab` did not open or close the Agent chat dialog

## Root cause

The Agent UI layers were listening for keyboard shortcuts through:

- `AgentModePanel._UnhandledInput()`
- `AgentChatDialog._UnhandledInput()`

For a Godot-based game mod, that is too late in the input pipeline for global hotkeys.

`_UnhandledInput()` only runs after earlier input consumers decline the event. In practice, the base game UI or focused controls can consume those key events first, which makes the mod hotkeys appear dead even though the configured key names themselves are valid.

So this was not primarily a "Godot 4.5.1 cannot recognize F-keys" problem. It was an input-routing problem.

## Changes made

Updated:

- `aibot/Scripts/Ui/AgentModePanel.cs`
- `aibot/Scripts/Ui/AgentChatDialog.cs`

### 1. Switched global shortcut handling from `_UnhandledInput()` to `_Input()`

This makes the Agent receive keyboard events earlier, before normal scene/UI consumers can swallow them.

### 2. Improved key matching

Hotkey checks now match against both:

- `InputEventKey.Keycode`
- `InputEventKey.PhysicalKeycode`

This makes the shortcut layer more robust across keyboard layouts and avoids depending on only one key representation.

## Outcome

After this phase, the Agent hotkeys are wired as true global mod shortcuts rather than "only if the rest of the game leaves the event alone" shortcuts.

That should restore practical responsiveness for:

- `F5` / `F6` / `F7` / `F8`
- `F4`
- `Tab`

## Validation

Validation for this phase should include:

```powershell
dotnet build aibot\\aibot.csproj -c Release /p:CopyModAfterBuild=false
```
