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
        return Runtime.IsInitialized;
    }

    public override Task<SkillExecutionResult> ExecuteAsync(AgentSkillParameters? parameters, CancellationToken cancellationToken)
    {
        return Task.FromResult(NotReady("遗物选择 Skill 已抽象接入，后续会与实际遗物界面节点绑定。"));
    }
}