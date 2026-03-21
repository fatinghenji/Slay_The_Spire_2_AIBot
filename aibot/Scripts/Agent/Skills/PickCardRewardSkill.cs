using MegaCrit.Sts2.Core.AutoSlay.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.Screens.CardSelection;
using MegaCrit.Sts2.Core.Nodes.Screens.Overlays;
using aibot.Scripts.Core;

namespace aibot.Scripts.Agent.Skills;

public sealed class PickCardRewardSkill : RuntimeBackedSkillBase
{
    public PickCardRewardSkill(AiBotRuntime runtime) : base(runtime)
    {
    }

    public override string Name => "pick_card_reward";

    public override string Description => "在卡牌奖励界面选择一张奖励卡牌。";

    public override SkillCategory Category => SkillCategory.DeckManagement;

    public override bool CanExecute()
    {
        return NOverlayStack.Instance?.Peek() is NCardRewardSelectionScreen;
    }

    public override async Task<SkillExecutionResult> ExecuteAsync(AgentSkillParameters? parameters, CancellationToken cancellationToken)
    {
        var screen = NOverlayStack.Instance?.Peek() as NCardRewardSelectionScreen;
        if (screen is null)
        {
            return new SkillExecutionResult(false, "当前不在卡牌奖励界面。");
        }

        var holders = UiHelper.FindAll<NCardHolder>(screen)
            .Where(holder => holder.CardModel is not null)
            .ToList();
        if (holders.Count == 0)
        {
            return new SkillExecutionResult(false, "当前奖励界面没有可选卡牌。");
        }

        var selected = !string.IsNullOrWhiteSpace(parameters?.CardName)
            ? holders.FirstOrDefault(holder => holder.CardModel!.Title.Contains(parameters.CardName, StringComparison.OrdinalIgnoreCase))
            : null;

        if (selected is null && Runtime.DecisionEngine is not null)
        {
            var options = holders.Select(holder => holder.CardModel).Cast<CardModel>().ToList();
            var decision = await Runtime.DecisionEngine.ChooseCardRewardAsync(options, Runtime.GetCurrentAnalysis(), cancellationToken);
            selected = decision.Card is not null
                ? holders.FirstOrDefault(holder => holder.CardModel == decision.Card || holder.CardModel?.Id == decision.Card.Id)
                : null;
        }

        selected ??= holders[0];
        selected.EmitSignal(NCardHolder.SignalName.Pressed, selected);
        return new SkillExecutionResult(true, $"已选择奖励卡牌：{selected.CardModel?.Title}");
    }
}
