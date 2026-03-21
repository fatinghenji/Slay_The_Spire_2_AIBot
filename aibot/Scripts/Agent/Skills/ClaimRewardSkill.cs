using MegaCrit.Sts2.Core.AutoSlay.Helpers;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Nodes.Rewards;
using MegaCrit.Sts2.Core.Nodes.Screens;
using MegaCrit.Sts2.Core.Nodes.Screens.Overlays;
using MegaCrit.Sts2.Core.Runs;
using aibot.Scripts.Core;

namespace aibot.Scripts.Agent.Skills;

public sealed class ClaimRewardSkill : RuntimeBackedSkillBase
{
    public ClaimRewardSkill(AiBotRuntime runtime) : base(runtime)
    {
    }

    public override string Name => "claim_reward";

    public override string Description => "在奖励界面中领取一个可领取的奖励。";

    public override SkillCategory Category => SkillCategory.Economy;

    public override bool CanExecute()
    {
        return NOverlayStack.Instance?.Peek() is NRewardsScreen;
    }

    public override async Task<SkillExecutionResult> ExecuteAsync(AgentSkillParameters? parameters, CancellationToken cancellationToken)
    {
        var screen = NOverlayStack.Instance?.Peek() as NRewardsScreen;
        if (screen is null)
        {
            return new SkillExecutionResult(false, "当前不在奖励界面。");
        }

        var buttons = UiHelper.FindAll<NRewardButton>(screen)
            .Where(candidate => candidate.IsEnabled && candidate.Visible && candidate.IsVisibleInTree())
            .ToList();
        if (buttons.Count == 0)
        {
            return new SkillExecutionResult(false, "当前没有可点击的奖励按钮。");
        }

        var query = parameters?.ItemName ?? parameters?.OptionId;
        var requestedIndex = ParseRequestedIndex(parameters?.OptionId, buttons.Count);
        NRewardButton? selected = requestedIndex is not null ? buttons[requestedIndex.Value] : null;
        selected ??= buttons.FirstOrDefault(button => MatchesRewardQuery(query, button));

        if (selected is null && Runtime.DecisionEngine is not null)
        {
            var player = LocalContext.GetMe(RunManager.Instance.DebugOnlyGetState());
            var decision = await Runtime.DecisionEngine.ChooseRewardAsync(
                buttons,
                player?.HasOpenPotionSlots ?? false,
                Runtime.GetCurrentAnalysis(),
                cancellationToken);
            selected = decision.Button;
        }

        selected ??= buttons[0];
        await UiHelper.Click(selected);
        return new SkillExecutionResult(true, $"已领取奖励：{DescribeRewardButton(selected)}");
    }

    private static bool MatchesRewardQuery(string? query, NRewardButton button)
    {
        return MatchesQuery(query, DescribeRewardButton(button), button.Reward?.GetType().Name);
    }

    private static string DescribeRewardButton(NRewardButton button)
    {
        return button.Reward?.GetType().Name ?? "reward";
    }
}
