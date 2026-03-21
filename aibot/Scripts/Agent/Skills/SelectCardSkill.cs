using MegaCrit.Sts2.Core.AutoSlay.Helpers;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.Screens.CardSelection;
using MegaCrit.Sts2.Core.Nodes.Screens.Overlays;
using aibot.Scripts.Core;
using aibot.Scripts.Decision;

namespace aibot.Scripts.Agent.Skills;

public sealed class SelectCardSkill : RuntimeBackedSkillBase
{
    public SelectCardSkill(AiBotRuntime runtime) : base(runtime)
    {
    }

    public override string Name => "select_card";

    public override string Description => "处理升级、移除、变形、附魔等卡牌选择场景。";

    public override SkillCategory Category => SkillCategory.DeckManagement;

    public override bool CanExecute()
    {
        return NOverlayStack.Instance?.Peek() is NChooseACardSelectionScreen;
    }

    public override async Task<SkillExecutionResult> ExecuteAsync(AgentSkillParameters? parameters, CancellationToken cancellationToken)
    {
        var screen = NOverlayStack.Instance?.Peek() as NChooseACardSelectionScreen;
        if (screen is null)
        {
            return new SkillExecutionResult(false, "当前不在卡牌选择界面。");
        }

        var holders = UiHelper.FindAll<NCardHolder>(screen)
            .Where(holder => holder.CardModel is not null)
            .ToList();
        if (holders.Count == 0)
        {
            return new SkillExecutionResult(false, "当前没有可选卡牌。");
        }

        var skipButton = UiHelper.FindFirst<NChoiceSelectionSkipButton>(screen);
        var query = parameters?.CardName ?? parameters?.ItemName ?? parameters?.OptionId;
        if (IsSkipRequest(query) && skipButton is not null && skipButton.IsVisibleInTree() && skipButton.IsEnabled)
        {
            await UiHelper.Click(skipButton);
            await WaitForUiActionAsync(cancellationToken);
            return new SkillExecutionResult(true, "已跳过当前卡牌选择。");
        }

        var requestedIndex = ParseRequestedIndex(parameters?.OptionId, holders.Count);
        NCardHolder? selected = requestedIndex is not null
            ? holders[requestedIndex.Value]
            : null;
        selected ??= holders.FirstOrDefault(holder => MatchesQuery(query, holder.CardModel?.Id.Entry, holder.CardModel?.Title));

        if (selected is null && Runtime.DecisionEngine is not null)
        {
            var context = new AiCardSelectionContext(
                AiCardSelectionKind.ChooseACard,
                "Choose one card from the current selection screen.",
                1,
                1,
                skipButton is not null && skipButton.IsVisibleInTree() && skipButton.IsEnabled,
                "choice-screen",
                nameof(NChooseACardSelectionScreen),
                $"OptionCount={holders.Count}");

            var decision = await Runtime.DecisionEngine.ChooseCardSelectionAsync(
                context,
                holders.Select(holder => holder.CardModel!).ToList(),
                Runtime.GetCurrentAnalysis(),
                cancellationToken);

            selected = decision.Card is not null
                ? holders.FirstOrDefault(holder => holder.CardModel == decision.Card || holder.CardModel?.Id == decision.Card.Id)
                : null;
        }

        selected ??= holders[0];
        selected.EmitSignal(NCardHolder.SignalName.Pressed, selected);
        await WaitForUiActionAsync(cancellationToken);
        return new SkillExecutionResult(true, $"已选择卡牌：{selected.CardModel?.Title}");
    }

    private static bool IsSkipRequest(string? query)
    {
        return MatchesQuery(query, "skip", "跳过", "不要", "pass");
    }
}
