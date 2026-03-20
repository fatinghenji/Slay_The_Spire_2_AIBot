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
        return Runtime.IsInitialized;
    }

    public override Task<SkillExecutionResult> ExecuteAsync(AgentSkillParameters? parameters, CancellationToken cancellationToken)
    {
        return Task.FromResult(NotReady("休息点 Skill 已抽象接入，后续会与 NRestSiteRoom 实际按钮执行链路绑定。"));
    }
}