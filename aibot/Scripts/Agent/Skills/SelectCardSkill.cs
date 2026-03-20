using aibot.Scripts.Core;

namespace aibot.Scripts.Agent.Skills;

public sealed class SelectCardSkill : RuntimeBackedSkillBase
{
    public SelectCardSkill(AiBotRuntime runtime) : base(runtime)
    {
    }

    public override string Name => "select_card";

    public override string Description => "处理升级、移除、变形、附魔等卡牌选择场景。";

    public override SkillCategory Category => SkillCategory.DeckManagement;

    public override bool CanExecute()
    {
        return Runtime.IsInitialized;
    }

    public override Task<SkillExecutionResult> ExecuteAsync(AgentSkillParameters? parameters, CancellationToken cancellationToken)
    {
        return Task.FromResult(NotReady("卡牌选择 Skill 已抽象接入，后续会与 `AiBotCardSelector` 和各类选择界面正式打通。"));
    }
}