using MegaCrit.Sts2.Core.AutoSlay.Helpers;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Screens.CardSelection;
using MegaCrit.Sts2.Core.Nodes.Screens.Overlays;
using aibot.Scripts.Core;
using aibot.Scripts.Decision;

namespace aibot.Scripts.Agent.Skills;

public sealed class ChooseBundleSkill : RuntimeBackedSkillBase
{
    public ChooseBundleSkill(AiBotRuntime runtime) : base(runtime)
    {
    }

    public override string Name => "choose_bundle";

    public override string Description => "在卡牌 bundle 选择界面选择一个 bundle。";

    public override SkillCategory Category => SkillCategory.DeckManagement;

    public override bool CanExecute()
    {
        return NOverlayStack.Instance?.Peek() is NChooseABundleSelectionScreen;
    }

    public override async Task<SkillExecutionResult> ExecuteAsync(AgentSkillParameters? parameters, CancellationToken cancellationToken)
    {
        var screen = NOverlayStack.Instance?.Peek() as NChooseABundleSelectionScreen;
        if (screen is null)
        {
            return new SkillExecutionResult(false, "当前不在 bundle 选择界面。");
        }

        var bundles = UiHelper.FindAll<NCardBundle>(screen)
            .Select((bundle, index) => new { Bundle = bundle, Index = index })
            .Where(entry => entry.Bundle.Bundle is { Count: > 0 })
            .ToList();
        if (bundles.Count == 0)
        {
            return new SkillExecutionResult(false, "当前 bundle 选择界面没有可选项。");
        }

        var requestedIndex = parameters?.BundleIndex;
        if (requestedIndex is null)
        {
            requestedIndex = ParseRequestedIndex(parameters?.OptionId, bundles.Count);
        }

        var query = parameters?.CardName ?? parameters?.ItemName;
        var selectedEntry = requestedIndex is not null && requestedIndex.Value >= 0 && requestedIndex.Value < bundles.Count
            ? bundles[requestedIndex.Value]
            : null;
        selectedEntry ??= bundles.FirstOrDefault(entry => entry.Bundle.Bundle.Any(card => MatchesQuery(query, card.Id.Entry, card.Title)));

        if (selectedEntry is null && Runtime.DecisionEngine is not null)
        {
            var context = new AiCardSelectionContext(
                AiCardSelectionKind.BundleChoice,
                "Choose one card bundle.",
                1,
                1,
                false,
                "bundle",
                nameof(NChooseABundleSelectionScreen),
                $"BundleCount={bundles.Count}");

            var decision = await Runtime.DecisionEngine.ChooseBundleAsync(
                context,
                bundles.Select(entry => new CardBundleOption(entry.Index, entry.Bundle.Bundle)).ToList(),
                Runtime.GetCurrentAnalysis(),
                cancellationToken);

            selectedEntry = bundles.FirstOrDefault(entry => entry.Index == decision.SelectedIndex);
        }

        selectedEntry ??= bundles[0];
        await UiHelper.Click(selectedEntry.Bundle.Hitbox);

        var confirmButton = UiHelper.FindFirst<NConfirmButton>(screen);
        if (confirmButton is not null && confirmButton.IsVisibleInTree() && confirmButton.IsEnabled)
        {
            await Task.Delay(Math.Max(0, Runtime.Config.ScreenActionDelayMs), cancellationToken);
            await UiHelper.Click(confirmButton);
        }

        await WaitForUiActionAsync(cancellationToken);
        var pickedCards = string.Join(", ", selectedEntry.Bundle.Bundle.Select(card => card.Title).Take(3));
        return new SkillExecutionResult(true, $"已选择第 {selectedEntry.Index + 1} 个 bundle。", pickedCards);
    }
}
