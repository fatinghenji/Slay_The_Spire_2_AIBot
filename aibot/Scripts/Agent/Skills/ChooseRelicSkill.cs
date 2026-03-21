using MegaCrit.Sts2.Core.AutoSlay.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Relics;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Screens;
using MegaCrit.Sts2.Core.Nodes.Screens.CardSelection;
using MegaCrit.Sts2.Core.Nodes.Screens.Overlays;
using MegaCrit.Sts2.Core.Nodes.Screens.TreasureRoomRelic;
using aibot.Scripts.Core;

namespace aibot.Scripts.Agent.Skills;

public sealed class ChooseRelicSkill : RuntimeBackedSkillBase
{
    public ChooseRelicSkill(AiBotRuntime runtime) : base(runtime)
    {
    }

    public override string Name => "choose_relic";

    public override string Description => "在遗物选择界面选择一个遗物。";

    public override SkillCategory Category => SkillCategory.RoomInteraction;

    public override bool CanExecute()
    {
        return NOverlayStack.Instance?.Peek() is NChooseARelicSelection
            || GetAbsoluteNodeOrNull<NTreasureRoom>("/root/Game/RootSceneContainer/Run/RoomContainer/TreasureRoom") is not null;
    }

    public override async Task<SkillExecutionResult> ExecuteAsync(AgentSkillParameters? parameters, CancellationToken cancellationToken)
    {
        var overlayScreen = NOverlayStack.Instance?.Peek() as NChooseARelicSelection;
        if (overlayScreen is not null)
        {
            return await ExecuteOverlaySelectionAsync(overlayScreen, parameters, cancellationToken);
        }

        var treasureRoom = GetAbsoluteNodeOrNull<NTreasureRoom>("/root/Game/RootSceneContainer/Run/RoomContainer/TreasureRoom");
        if (treasureRoom is not null)
        {
            return await ExecuteTreasureSelectionAsync(treasureRoom, parameters, cancellationToken);
        }

        return new SkillExecutionResult(false, "当前不在可选遗物的界面。");
    }

    private async Task<SkillExecutionResult> ExecuteOverlaySelectionAsync(NChooseARelicSelection screen, AgentSkillParameters? parameters, CancellationToken cancellationToken)
    {
        var holders = UiHelper.FindAll<NRelicBasicHolder>(screen)
            .Where(holder => holder.Relic?.Model is not null)
            .ToList();
        if (holders.Count == 0)
        {
            return new SkillExecutionResult(false, "当前遗物选择界面没有可选遗物。");
        }

        var query = parameters?.ItemName ?? parameters?.OptionId;
        var requestedIndex = ParseRequestedIndex(parameters?.OptionId, holders.Count);
        var skipButton = UiHelper.FindFirst<NChoiceSelectionSkipButton>(screen);
        if (IsSkipRequest(query) && skipButton is not null && skipButton.IsVisibleInTree() && skipButton.IsEnabled)
        {
            await UiHelper.Click(skipButton);
            await WaitForUiActionAsync(cancellationToken);
            return new SkillExecutionResult(true, "已跳过当前遗物选择。");
        }

        NRelicBasicHolder? selected = requestedIndex is not null
            ? holders[requestedIndex.Value]
            : null;
        selected ??= holders.FirstOrDefault(holder => MatchesRelicQuery(query, holder.Relic.Model));

        if (selected is null && Runtime.DecisionEngine is not null)
        {
            var relicModels = holders.Select(holder => holder.Relic.Model).ToList();
            var decision = await Runtime.DecisionEngine.ChooseRelicAsync(
                relicModels,
                nameof(NChooseARelicSelection),
                skipButton is not null && skipButton.IsVisibleInTree() && skipButton.IsEnabled,
                Runtime.GetCurrentAnalysis(),
                cancellationToken);

            selected = decision.Relic is not null
                ? holders.FirstOrDefault(holder => holder.Relic.Model == decision.Relic || holder.Relic.Model.Id == decision.Relic.Id)
                : null;
        }

        selected ??= holders[0];
        await UiHelper.Click(selected);
        await WaitForUiActionAsync(cancellationToken);
        return new SkillExecutionResult(true, $"已选择遗物：{selected.Relic.Model.Title.GetFormattedText()}");
    }

    private async Task<SkillExecutionResult> ExecuteTreasureSelectionAsync(NTreasureRoom room, AgentSkillParameters? parameters, CancellationToken cancellationToken)
    {
        var relicHolders = UiHelper.FindAll<NTreasureRoomRelicHolder>(room)
            .Where(holder => holder.IsEnabled && holder.Visible && holder.Relic?.Model is not null)
            .ToList();
        if (relicHolders.Count == 0)
        {
            return new SkillExecutionResult(false, "当前宝箱界面没有可选遗物。");
        }

        var query = parameters?.ItemName ?? parameters?.OptionId;
        var requestedIndex = ParseRequestedIndex(parameters?.OptionId, relicHolders.Count);
        NTreasureRoomRelicHolder? selected = requestedIndex is not null
            ? relicHolders[requestedIndex.Value]
            : null;
        selected ??= relicHolders.FirstOrDefault(holder => MatchesRelicQuery(query, holder.Relic.Model));

        if (selected is null && Runtime.DecisionEngine is not null)
        {
            var relicModels = relicHolders.Select(holder => holder.Relic.Model).ToList();
            var decision = await Runtime.DecisionEngine.ChooseRelicAsync(relicModels, "Treasure Room", false, Runtime.GetCurrentAnalysis(), cancellationToken);
            selected = decision.Relic is not null
                ? relicHolders.FirstOrDefault(holder => holder.Relic.Model == decision.Relic || holder.Relic.Model.Id == decision.Relic.Id)
                : null;
        }

        selected ??= relicHolders[0];
        await UiHelper.Click(selected);
        await WaitForUiActionAsync(cancellationToken);
        return new SkillExecutionResult(true, $"已选择宝箱遗物：{selected.Relic.Model.Title.GetFormattedText()}");
    }

    private bool MatchesRelicQuery(string? query, RelicModel model)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return false;
        }

        if (MatchesQuery(query, model.Id.Entry, model.Title.GetFormattedText()))
        {
            return true;
        }

        var analysis = Runtime.GetCurrentAnalysis();
        var guide = Runtime.KnowledgeBase?.FindRelic(query, analysis.CharacterId) ?? Runtime.KnowledgeBase?.FindRelic(query);
        return guide is not null
            && MatchesQuery(guide.Slug, model.Id.Entry, model.Title.GetFormattedText(), guide.NameEn, guide.NameZh);
    }

    private static bool IsSkipRequest(string? query)
    {
        return MatchesQuery(query, "skip", "跳过", "不要", "pass");
    }
}
