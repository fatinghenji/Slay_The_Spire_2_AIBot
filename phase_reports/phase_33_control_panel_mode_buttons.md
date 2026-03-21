# Phase 33 - Control Panel Mode Buttons

## Background

User testing showed the keyboard shortcuts for mode switching and chat toggle remained unreliable in the live mod environment. The user preferred replacing shortcut-driven interaction with direct UI buttons, and also requested that the right-side log panel move down so it no longer overlaps the base game's upper-right buttons.

## Changes

### 1. Refactored the decision log panel into a right-side control panel

Updated `aibot/Scripts/Ui/AiBotDecisionPanel.cs`:

- Moved the panel from top-right anchoring to right-side vertical centering.
- Kept the decision log content, but added persistent control widgets underneath it.
- Added mode switch buttons for:
  - Full Auto
  - Semi Auto
  - Assist
  - QnA
- Added a status line for mode-switch feedback.
- Added an in-panel confirmation area for mode-switch requests.
- Added a chat toggle button for Semi Auto / QnA.

### 2. Removed the standalone mode panel from the primary workflow

Updated `aibot/Scripts/Agent/AgentCore.cs`:

- Stopped creating `AgentModePanel` during agent initialization.
- Mode switching is now intended to happen through the right-side control panel instead of the separate left-side mode panel.

### 3. Made the decision/control panel persist across modes

Updated `aibot/Scripts/Core/AiBotRuntime.cs`:

- Ensure the control panel is created during runtime initialization.
- Changed panel visibility logic so it remains available after leaving Full Auto.

### 4. Added a direct chat toggle entry point

Updated `aibot/Scripts/Ui/AgentChatDialog.cs`:

- Added `IsDialogVisible`.
- Added `ToggleForMode(AgentMode mode)` so other UI surfaces can open/close the chat dialog without depending on keyboard shortcuts.

## Expected Result

- The main right-side panel should sit lower and avoid the game's upper-right interaction area.
- Mode switching should be available by clicking on-screen buttons instead of using `F5`-`F8`.
- Semi Auto and QnA chat should be openable from the same panel through a visible button instead of `Tab`.
- Switching away from Full Auto should no longer remove the only visible mode-switch entry point.

## Validation

- `dotnet msbuild aibot\\aibot.csproj /restore /t:CoreCompile /p:Configuration=Release` completed successfully.
- Full `dotnet build` generated `aibot.dll`, but deployment into the game's mod folder still failed because `SlayTheSpire2.exe` was locking the existing `mods\\aibot\\aibot.dll`.
