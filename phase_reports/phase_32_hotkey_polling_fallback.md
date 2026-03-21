# Phase 32 - Hotkey Polling Fallback

## Background

After switching the mod hotkeys from `_UnhandledInput()` to `_Input()`, user testing still showed no response for `F5`-`F8` mode switching and `Tab` chat toggle. This suggested the issue was not just callback timing, but that relying on input events alone remained fragile inside the Slay the Spire 2 mod runtime.

## Findings

- Godot 4.5.1 does support both `Input.IsKeyPressed()` and `Input.IsPhysicalKeyPressed()` for keyboard polling.
- The mod UI nodes are created during `AgentCore.Initialize()`, so the hotkey handlers are present in the tree.
- In a Godot mod layered on top of the game UI, input events can still be swallowed or rerouted before our custom UI code sees them consistently.

## Changes

### 1. Added frame-based hotkey polling to `AgentModePanel`

Updated `aibot/Scripts/Ui/AgentModePanel.cs`:

- Enabled `_Process()` explicitly in `_Ready()`.
- Replaced event-only mode hotkey handling with polling through:
  - `Input.IsKeyPressed(parsed)`
  - `Input.IsPhysicalKeyPressed(parsed)`
- Added edge-trigger state tracking so hotkeys fire only once per key press.
- Kept `F4` mode panel toggle on the same polling path.
- Added log output when a mode hotkey or panel toggle is detected.

### 2. Added frame-based hotkey polling to `AgentChatDialog`

Updated `aibot/Scripts/Ui/AgentChatDialog.cs`:

- Added `_Ready()` and explicitly enabled `_Process()`.
- Extended `_Process()` to poll the configured chat hotkey every frame.
- Added edge-trigger state tracking so the chat dialog toggles only once per key press.
- Added log output when the chat hotkey is detected.

## Expected Result

- `F5` / `F6` / `F7` / `F8` should now switch or request switching modes even if the base game consumes the corresponding input event.
- `Tab` should now toggle the chat dialog based on real keyboard state instead of depending on event propagation.
- This approach is more robust for a Godot-based game mod than relying only on `_Input()` or `_UnhandledInput()`.

## Validation

- Verified locally that the Godot 4.5.1 C# API exposes both polling methods needed for this fallback path.
- A full project compile could not be used as a reliable validator in the current environment because the build toolchain is returning broader project reference resolution errors unrelated to these two UI files.

## Next Step If Issue Persists

If hotkeys still fail after this phase, the next investigation target should be whether the modded UI nodes are actually processing every frame in the live game scene, or whether we need to register through the game's own hotkey manager layer instead of using standalone UI polling.
