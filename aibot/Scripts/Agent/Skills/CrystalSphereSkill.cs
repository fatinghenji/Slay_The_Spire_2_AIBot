using Godot;
using MegaCrit.Sts2.Core.AutoSlay.Helpers;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Events;
using MegaCrit.Sts2.Core.Nodes.Events.Custom.CrystalSphere;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Screens.Overlays;
using aibot.Scripts.Core;

namespace aibot.Scripts.Agent.Skills;

public sealed class CrystalSphereSkill : RuntimeBackedSkillBase
{
    public CrystalSphereSkill(AiBotRuntime runtime) : base(runtime)
    {
    }

    public override string Name => "crystal_sphere";

    public override string Description => "处理 Crystal Sphere 小游戏选择。";

    public override SkillCategory Category => SkillCategory.RoomInteraction;

    public override bool CanExecute()
    {
        return NOverlayStack.Instance?.Peek() is NCrystalSphereScreen;
    }

    public override async Task<SkillExecutionResult> ExecuteAsync(AgentSkillParameters? parameters, CancellationToken cancellationToken)
    {
        var screen = NOverlayStack.Instance?.Peek() as NCrystalSphereScreen;
        if (screen is null)
        {
            return new SkillExecutionResult(false, "当前不在 Crystal Sphere 界面。");
        }

        var proceedButton = screen.GetNodeOrNull<NProceedButton>("%ProceedButton");
        var query = parameters?.OptionId ?? parameters?.ItemName;
        if (proceedButton is not null && proceedButton.IsEnabled && IsProceedRequest(query))
        {
            await UiHelper.Click(proceedButton);
            await WaitForUiActionAsync(cancellationToken);
            return new SkillExecutionResult(true, "已完成 Crystal Sphere 并继续。");
        }

        var cellsContainer = screen.GetNodeOrNull<Control>("%Cells");
        if (cellsContainer is null)
        {
            return new SkillExecutionResult(false, "当前 Crystal Sphere 网格不可用。");
        }

        var hiddenCells = UiHelper.FindAll<NCrystalSphereCell>(cellsContainer)
            .Where(cell => cell.Visible && cell.Entity.IsHidden)
            .ToList();
        if (hiddenCells.Count == 0)
        {
            if (proceedButton is not null && proceedButton.IsEnabled)
            {
                await UiHelper.Click(proceedButton);
                await WaitForUiActionAsync(cancellationToken);
                return new SkillExecutionResult(true, "当前没有更多隐藏格子，已继续。");
            }

            return new SkillExecutionResult(false, "当前没有可点击的 Crystal Sphere 隐藏格子。");
        }

        var entityField = typeof(NCrystalSphereScreen).GetField("_entity", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var minigame = entityField?.GetValue(screen) as MegaCrit.Sts2.Core.Events.Custom.CrystalSphereEvent.CrystalSphereMinigame;

        bool? useBigDivination = parameters?.UseBigDivination;
        int? selectedX = parameters?.GridX;
        int? selectedY = parameters?.GridY;

        if ((useBigDivination is null || selectedX is null || selectedY is null) && minigame is not null && Runtime.DecisionEngine is not null)
        {
            var decision = await Runtime.DecisionEngine.ChooseCrystalSphereActionAsync(minigame, Runtime.GetCurrentAnalysis(), cancellationToken);
            useBigDivination ??= decision.UseBigDivination;
            selectedX ??= decision.X;
            selectedY ??= decision.Y;
        }

        var bigButton = screen.GetNodeOrNull<NDivinationButton>("%BigDivinationButton");
        var smallButton = screen.GetNodeOrNull<NDivinationButton>("%SmallDivinationButton");
        var desiredToolButton = useBigDivination switch
        {
            true => bigButton,
            false => smallButton,
            _ => null
        };
        if (desiredToolButton is not null && desiredToolButton.IsVisibleInTree() && desiredToolButton.IsEnabled)
        {
            await UiHelper.Click(desiredToolButton);
            await Task.Delay(Math.Max(0, Runtime.Config.ScreenActionDelayMs), cancellationToken);
        }

        var selectedCell = hiddenCells.FirstOrDefault(cell => selectedX is not null && selectedY is not null && cell.Entity.X == selectedX.Value && cell.Entity.Y == selectedY.Value)
            ?? hiddenCells[0];

        selectedCell.EmitSignal(NClickableControl.SignalName.Released, selectedCell);
        await WaitForUiActionAsync(cancellationToken);
        return new SkillExecutionResult(
            true,
            $"已执行 Crystal Sphere 选择：({selectedCell.Entity.X}, {selectedCell.Entity.Y})",
            useBigDivination is null ? null : (useBigDivination.Value ? "使用了大占卜。" : "使用了小占卜。"));
    }

    private static bool IsProceedRequest(string? query)
    {
        return MatchesQuery(query, "proceed", "继续", "完成", "结束");
    }
}
