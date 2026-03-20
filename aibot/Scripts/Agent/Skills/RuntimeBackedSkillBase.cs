using aibot.Scripts.Core;

namespace aibot.Scripts.Agent.Skills;

public abstract class RuntimeBackedSkillBase : IAgentSkill
{
    protected RuntimeBackedSkillBase(AiBotRuntime runtime)
    {
        Runtime = runtime;
    }

    protected AiBotRuntime Runtime { get; }

    public abstract string Name { get; }

    public abstract string Description { get; }

    public abstract SkillCategory Category { get; }

    public abstract bool CanExecute();

    public abstract Task<SkillExecutionResult> ExecuteAsync(AgentSkillParameters? parameters, CancellationToken cancellationToken);

    protected static SkillExecutionResult NotReady(string summary)
    {
        return new SkillExecutionResult(false, summary, "该 Skill 已完成抽象接入，但完整执行链路会在后续阶段继续打通。 ");
    }
}
