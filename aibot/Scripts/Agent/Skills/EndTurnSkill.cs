using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Runs;
using aibot.Scripts.Core;

namespace aibot.Scripts.Agent.Skills;

public sealed class EndTurnSkill : RuntimeBackedSkillBase
{
    public EndTurnSkill(AiBotRuntime runtime) : base(runtime)
    {
    }

    public override string Name => "end_turn";

    public override string Description => "结束当前战斗回合。";

    public override SkillCategory Category => SkillCategory.Combat;

    public override bool CanExecute()
    {
        var runState = RunManager.Instance.DebugOnlyGetState();
        return LocalContext.GetMe(runState) is not null;
    }

    public override async Task<SkillExecutionResult> ExecuteAsync(AgentSkillParameters? parameters, CancellationToken cancellationToken)
    {
        var runState = RunManager.Instance.DebugOnlyGetState();
        var player = LocalContext.GetMe(runState);
        if (player is null)
        {
            return new SkillExecutionResult(false, "当前没有可结束回合的玩家对象。 ");
        }

        PlayerCmd.EndTurn(player, false);
        var actionExecutor = RunManager.Instance.ActionExecutor;
        if (actionExecutor is not null)
        {
            await actionExecutor.FinishedExecutingActions().WaitAsync(cancellationToken);
        }

        return new SkillExecutionResult(true, "已结束当前回合。");
    }
}
