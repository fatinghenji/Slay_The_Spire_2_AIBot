using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.Screens.CardSelection;
using MegaCrit.Sts2.Core.AutoSlay.Helpers;
using MegaCrit.Sts2.Core.Nodes.Screens.Overlays;
using aibot.Scripts.Core;

namespace aibot.Scripts.Agent.Skills;

public sealed class PickCardRewardSkill : RuntimeBackedSkillBase
{
    public PickCardRewardSkill(AiBotRuntime runtime) : base(runtime)
    {
    }

    public override string Name => "pick_card_reward";

    public override string Description => "在卡牌奖励界面选择一张卡。";

    public override SkillCategory Category => SkillCategory.DeckManagement;

    public override bool CanExecute()
    {
        return NOverlayStack.Instance?.Peek() is NCardRewardSelectionScreen;
    }

    public override Task<SkillExecutionResult> ExecuteAsync(AgentSkillParameters? parameters, CancellationToken cancellationToken)
    {
        var screen = NOverlayStack.Instance?.Peek() as NCardRewardSelectionScreen;
        if (screen is null)
        {
            return Task.FromResult(new SkillExecutionResult(false, "当前不在卡牌奖励界面。"));
        }

        var holders = UiHelper.FindAll<NCardHolder>(screen)
            .Where(holder => holder.CardModel is not null)
            .ToList();
        if (holders.Count == 0)
        {
            return Task.FromResult(new SkillExecutionResult(false, "当前奖励界面没有可选卡牌。"));
        }

        var selected = !string.IsNullOrWhiteSpace(parameters?.CardName)
            ? holders.FirstOrDefault(holder => holder.CardModel!.Title.Contains(parameters.CardName, StringComparison.OrdinalIgnoreCase))
            : null;
        selected ??= holders[0];
        selected.EmitSignal(NCardHolder.SignalName.Pressed, selected);
        return Task.FromResult(new SkillExecutionResult(true, $"已选择奖励卡牌：{selected.CardModel?.Title}"));
    }
}
