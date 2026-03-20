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
        return Runtime.IsInitialized;
    }

    public override Task<SkillExecutionResult> ExecuteAsync(AgentSkillParameters? parameters, CancellationToken cancellationToken)
    {
        return Task.FromResult(NotReady("事件选项 Skill 已抽象接入，后续会与事件按钮节点正式绑定。"));
    }
}