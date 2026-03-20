using aibot.Scripts.Core;

namespace aibot.Scripts.Agent.Skills;

public sealed class CrystalSphereSkill : RuntimeBackedSkillBase
{
    public CrystalSphereSkill(AiBotRuntime runtime) : base(runtime)
    {
    }

    public override string Name => "crystal_sphere";

    public override string Description => "处理 Crystal Sphere 小游戏选择。";

    public override SkillCategory Category => SkillCategory.RoomInteraction;

    public override bool CanExecute()
    {
        return Runtime.IsInitialized;
    }

    public override Task<SkillExecutionResult> ExecuteAsync(AgentSkillParameters? parameters, CancellationToken cancellationToken)
    {
        return Task.FromResult(NotReady("Crystal Sphere Skill 已抽象接入，后续会绑定到实际网格与道具选择。"));
    }
}