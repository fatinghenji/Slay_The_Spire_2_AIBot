using MegaCrit.Sts2.Core.AutoSlay.Helpers;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.RestSite;
using MegaCrit.Sts2.Core.Nodes.RestSite;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Runs;
using aibot.Scripts.Core;

namespace aibot.Scripts.Agent.Skills;

public sealed class RestSiteSkill : RuntimeBackedSkillBase
{
    public RestSiteSkill(AiBotRuntime runtime) : base(runtime)
    {
    }

    public override string Name => "rest_site";

    public override string Description => "在休息点选择休息、升级等选项。";

    public override SkillCategory Category => SkillCategory.RoomInteraction;

    public override bool CanExecute()
    {
        return GetAbsoluteNodeOrNull<NRestSiteRoom>("/root/Game/RootSceneContainer/Run/RoomContainer/RestSiteRoom") is not null;
    }

    public override async Task<SkillExecutionResult> ExecuteAsync(AgentSkillParameters? parameters, CancellationToken cancellationToken)
    {
        var room = GetAbsoluteNodeOrNull<NRestSiteRoom>("/root/Game/RootSceneContainer/Run/RoomContainer/RestSiteRoom");
        if (room is null)
        {
            return new SkillExecutionResult(false, "当前不在休息点界面。");
        }

        var buttons = UiHelper.FindAll<NRestSiteButton>(room)
            .Where(button => button.IsEnabled && button.Visible)
            .ToList();
        if (buttons.Count == 0)
        {
            return new SkillExecutionResult(false, "当前休息点没有可选项。");
        }

        var query = parameters?.OptionId ?? parameters?.ItemName;
        var requestedIndex = ParseRequestedIndex(parameters?.OptionId, buttons.Count);
        NRestSiteButton? selected = requestedIndex is not null
            ? buttons[requestedIndex.Value]
            : null;
        selected ??= buttons.FirstOrDefault(button => MatchesRestOption(query, button.Option));

        if (selected is null)
        {
            var runState = RunManager.Instance.DebugOnlyGetState();
            var player = LocalContext.GetMe(runState);
            if (player is not null && Runtime.DecisionEngine is not null)
            {
                var decision = await Runtime.DecisionEngine.ChooseRestSiteOptionAsync(
                    player,
                    buttons.Select(button => button.Option).ToList(),
                    Runtime.GetCurrentAnalysis(),
                    cancellationToken);

                selected = decision.Option is not null
                    ? buttons.FirstOrDefault(button => button.Option.OptionId == decision.Option.OptionId)
                    : null;
            }
        }

        selected ??= buttons[0];
        await UiHelper.Click(selected);
        await WaitForUiActionAsync(cancellationToken);
        return new SkillExecutionResult(true, $"已选择休息点选项：{selected.Option.Title.GetFormattedText()}");
    }

    private static bool MatchesRestOption(string? query, RestSiteOption option)
    {
        return MatchesQuery(query, option.OptionId, option.Title.GetFormattedText(), option.Description.GetFormattedText());
    }
}
