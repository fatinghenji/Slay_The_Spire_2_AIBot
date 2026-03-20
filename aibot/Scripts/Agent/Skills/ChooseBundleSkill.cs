using aibot.Scripts.Core;

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
        return Runtime.IsInitialized;
    }

    public override Task<SkillExecutionResult> ExecuteAsync(AgentSkillParameters? parameters, CancellationToken cancellationToken)
    {
        return Task.FromResult(NotReady("Bundle 选择 Skill 已抽象接入，后续会接到实际 bundle 界面节点执行。"));
    }
}