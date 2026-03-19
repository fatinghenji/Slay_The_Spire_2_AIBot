<!-- Copilot / AI agent instructions for contributors -->
# Copilot Instructions — Slay_The_Spire_2_AIBot

Purpose: help an AI coding agent become productive quickly in this workspace.

- **High level:** This repo contains two cooperating C# projects:
  - `aibot/` — the mod project and runtime that implements the AI decision loop and UI.
  - `sts2/` — game API bindings and support code that reference the installed Slay the Spire 2 game DLLs.

- **Build & run (practical):**
  - Requires .NET 9 and the Godot .NET SDK used by `aibot.csproj` (see `aibot/aibot.csproj`).
  - The `aibot` project requires the local Slay the Spire 2 installation path. Build with MSBuild property `Sts2Dir` if your environment differs from the hardcoded path:

```powershell
dotnet build aibot\aibot.csproj -c Release -p:Sts2Dir="C:\Path\To\Slay the Spire 2"
dotnet build sts2\sts2.csproj -c Release
```

  - `aibot.csproj` contains a `Copy Mod` MSBuild Target that copies the compiled DLL and guide files into the game's `mods/` folder automatically after build (so building is the main step before launching the game for integration testing).

- **External dependencies / setup notes:**
  - The `sts2/` project references several game DLLs via HintPath (GodotSharp.dll, 0Harmony.dll, Steamworks.NET.dll, etc.). These come from your local Steam installation — ensure the `Sts2Dir` points to your copy of the game. See `sts2/sts2.csproj` for the exact referenced DLL list.
  - `aibot/config.json` contains runtime toggles and the configured cloud provider (example: `deepseek`). It may contain API keys; do not commit secrets.

- **Key components & integration points (quick map):**
  - Mod entry point: [aibot/Scripts/Entry.cs](aibot/Scripts/Entry.cs#L1) — registers the mod with `[ModInitializer("Init")]` and calls `AiBotRuntime.Initialize`.
  - Runtime loop & decision flow: [aibot/Scripts/Core/AiBotRuntime.cs](aibot/Scripts/Core/AiBotRuntime.cs#L1) — main tick loop, activation/deactivation, overlay/combat/map handlers.
  - Decision engines: [aibot/Scripts/Decision/GuideHeuristicDecisionEngine.cs](aibot/Scripts/Decision/GuideHeuristicDecisionEngine.cs#L1), [aibot/Scripts/Decision/DeepSeekDecisionEngine.cs](aibot/Scripts/Decision/DeepSeekDecisionEngine.cs#L1), [aibot/Scripts/Decision/HybridDecisionEngine.cs](aibot/Scripts/Decision/HybridDecisionEngine.cs#L1). The runtime composes heuristic + optional cloud engine (toggle via config).
  - Knowledge base: [aibot/Scripts/Knowledge/GuideKnowledgeBase.cs](aibot/Scripts/Knowledge/GuideKnowledgeBase.cs#L1) — loads `sts2_guides/` JSON and Markdown used by the heuristic and prompt construction.
  - Patches: [aibot/Scripts/Harmony/AiBotPatches.cs](aibot/Scripts/Harmony/AiBotPatches.cs#L1) — Harmony postfix patches that hook `NGame.StartNewSingleplayerRun` and `NGame.LoadRun` to activate the bot.

- **Project-specific conventions & patterns:**
  - Harmony is used for runtime patches; new hooks should follow existing `[HarmonyPatch]/[HarmonyPostfix]` style in the `Harmony/` folder.
  - Godot integration: scripts are registered via `ScriptManagerBridge.LookupScriptsInAssembly` (see `Entry.cs`) so new Godot script classes must be discoverable in the compiled assembly.
  - Knowledge-driven decisions: the code builds a `GuideKnowledgeBase` from `sts2_guides/` and prefers local heuristic results; cloud model is optional and gated by `config.json`.
  - Logging: use `MegaCrit.Sts2.Core.Logging.Log.*` for consistency with the project.

- **Testing & debugging tips:**
  - There is no black-box unit test suite to run by default; the fastest feedback loop is: build (which copies the mod) → launch the game → reproduce a scenario. Attach a debugger to the running game's .NET process for step-through.
  - There are helper classes under `sts2/RiderTestRunner/` (project-specific test runner scaffolding) — useful if you use Rider or a similar .NET test runner.

- **Security / commit guidance for AI edits:**
  - `aibot/config.json` may contain API keys (e.g. `provider.apiKey`). Never commit secrets. If a change touches `config.json`, move secrets to CI/secret storage or document that the value must be replaced locally.

- **What an AI agent can change safely:**
  - Implement new decision heuristics inside `aibot/Scripts/Decision/` and add tests/mocks where possible.
  - Update or extend `sts2_guides/` content to improve local knowledge, but avoid changing the game's DLL references in `sts2/sts2.csproj` unless you have the user's local path.

- **Files to check first when triaging bugs:**
  - [aibot/Scripts/Core/AiBotRuntime.cs](aibot/Scripts/Core/AiBotRuntime.cs#L1)
  - [aibot/Scripts/Decision/HybridDecisionEngine.cs](aibot/Scripts/Decision/HybridDecisionEngine.cs#L1)
  - [aibot/Scripts/Knowledge/GuideKnowledgeBase.cs](aibot/Scripts/Knowledge/GuideKnowledgeBase.cs#L1)
  - [aibot/aibot.csproj](aibot/aibot.csproj#L1) and [sts2/sts2.csproj](sts2/sts2.csproj#L1) for build quirks

If anything here is unclear or you want a different level of detail (more examples, test commands, or CI guidance), tell me which section to expand and I will iterate.
