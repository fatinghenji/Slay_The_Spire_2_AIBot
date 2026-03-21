using MegaCrit.Sts2.Core.AutoSlay.Helpers;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Events;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using aibot.Scripts.Core;

namespace aibot.Scripts.Agent.Skills;

public sealed class ChooseEventOptionSkill : RuntimeBackedSkillBase
{
    public ChooseEventOptionSkill(AiBotRuntime runtime) : base(runtime)
    {
    }

    public override string Name => "choose_event_option";

    public override string Description => "在事件界面选择一个事件选项。";

    public override SkillCategory Category => SkillCategory.RoomInteraction;

    public override bool CanExecute()
    {
        return GetAbsoluteNodeOrNull<NEventRoom>("/root/Game/RootSceneContainer/Run/RoomContainer/EventRoom") is { Visible: true };
    }

    public override async Task<SkillExecutionResult> ExecuteAsync(AgentSkillParameters? parameters, CancellationToken cancellationToken)
    {
        var eventRoom = GetAbsoluteNodeOrNull<NEventRoom>("/root/Game/RootSceneContainer/Run/RoomContainer/EventRoom");
        if (eventRoom is null || !eventRoom.Visible || !eventRoom.IsVisibleInTree())
        {
            return new SkillExecutionResult(false, "当前不在事件界面。");
        }

        var options = UiHelper.FindAll<NEventOptionButton>(eventRoom)
            .Where(button => button.IsEnabled && !button.Option.IsLocked)
            .ToList();
        var query = parameters?.ItemName ?? parameters?.OptionId;
        var requestedIndex = ParseRequestedIndex(parameters?.OptionId, options.Count);
        NEventOptionButton? selected = requestedIndex is not null
            ? options[requestedIndex.Value]
            : null;
        selected ??= options.FirstOrDefault(button => MatchesOptionQuery(query, button.Option));

        if (selected is null && options.Count > 0 && Runtime.DecisionEngine is not null)
        {
            var eventModel = options[0].Event;
            var decision = await Runtime.DecisionEngine.ChooseEventOptionAsync(
                eventModel,
                options.Select(button => button.Option).ToList(),
                Runtime.GetCurrentAnalysis(),
                cancellationToken);

            selected = decision.Option is not null
                ? options.FirstOrDefault(button => button.Option == decision.Option || button.Option.TextKey == decision.Option.TextKey)
                : null;
        }

        if (selected is not null)
        {
            await UiHelper.Click(selected);
            await WaitForUiActionAsync(cancellationToken);
            return new SkillExecutionResult(true, $"已选择事件选项：{selected.Option.Title.GetFormattedText()}");
        }

        var proceed = UiHelper.FindFirst<NProceedButton>(eventRoom);
        if (proceed is not null && proceed.IsEnabled && IsProceedRequest(query))
        {
            await UiHelper.Click(proceed);
            await WaitForUiActionAsync(cancellationToken);
            return new SkillExecutionResult(true, "已继续离开当前事件。");
        }

        return new SkillExecutionResult(false, "当前没有可执行的事件选项。");
    }

    private static bool MatchesOptionQuery(string? query, EventOption option)
    {
        return MatchesQuery(
            query,
            option.TextKey,
            option.Title.GetFormattedText(),
            option.Description.GetFormattedText(),
            option.IsProceed ? "proceed" : null,
            option.IsProceed ? "继续" : null);
    }

    private static bool IsProceedRequest(string? query)
    {
        return MatchesQuery(query, "proceed", "继续", "离开", "完成", "结束");
    }
}
