using MegaCrit.Sts2.Core.AutoSlay.Helpers;
using MegaCrit.Sts2.Core.Nodes.Rewards;
using MegaCrit.Sts2.Core.Nodes.Screens;
using MegaCrit.Sts2.Core.Nodes.Screens.Overlays;
using aibot.Scripts.Core;

namespace aibot.Scripts.Agent.Skills;

public sealed class ClaimRewardSkill : RuntimeBackedSkillBase
{
    public ClaimRewardSkill(AiBotRuntime runtime) : base(runtime)
    {
    }

    public override string Name => "claim_reward";

    public override string Description => "领取奖励界面中的一个可领取奖励。";

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
            return new SkillExecutionResult(false, "当前不在奖励界面。 ");
        }

        var button = UiHelper.FindAll<NRewardButton>(screen)
            .FirstOrDefault(candidate => candidate.IsEnabled && candidate.Visible && candidate.IsVisibleInTree());
        if (button is null)
        {
            return new SkillExecutionResult(false, "当前没有可点击的奖励按钮。 ");
        }

        await UiHelper.Click(button);
        return new SkillExecutionResult(true, "已领取一个奖励项。 ");
    }
}
