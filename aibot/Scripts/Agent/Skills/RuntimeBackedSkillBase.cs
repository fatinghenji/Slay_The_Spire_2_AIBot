using Godot;
using MegaCrit.Sts2.Core.Runs;
using aibot.Scripts.Core;
using aibot.Scripts.Knowledge;

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

    protected async Task WaitForUiActionAsync(CancellationToken cancellationToken, int? delayMs = null)
    {
        var actionExecutor = RunManager.Instance.ActionExecutor;
        if (actionExecutor is not null)
        {
            await actionExecutor.FinishedExecutingActions().WaitAsync(cancellationToken);
        }

        var effectiveDelay = delayMs ?? Runtime.Config.ScreenActionDelayMs;
        if (effectiveDelay > 0)
        {
            await Task.Delay(effectiveDelay, cancellationToken);
        }
    }

    protected static T? GetAbsoluteNodeOrNull<T>(string path) where T : class
    {
        var root = ((SceneTree)Engine.GetMainLoop()).Root;
        return root.GetNodeOrNull(path) as T;
    }

    protected static string NormalizeText(string? value)
    {
        return GuideKnowledgeBase.Normalize(value ?? string.Empty);
    }

    protected static bool MatchesQuery(string? query, params string?[] candidates)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return false;
        }

        var normalizedQuery = NormalizeText(query);
        if (string.IsNullOrWhiteSpace(normalizedQuery))
        {
            return false;
        }

        return candidates.Any(candidate =>
        {
            var normalizedCandidate = NormalizeText(candidate);
            return !string.IsNullOrWhiteSpace(normalizedCandidate)
                && (normalizedCandidate.Contains(normalizedQuery, StringComparison.OrdinalIgnoreCase)
                    || normalizedQuery.Contains(normalizedCandidate, StringComparison.OrdinalIgnoreCase));
        });
    }

    protected static int? ParseRequestedIndex(string? optionId, int count)
    {
        if (string.IsNullOrWhiteSpace(optionId) || !optionId.StartsWith("index:", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var value = optionId["index:".Length..].Trim();
        if (!int.TryParse(value, out var index))
        {
            return null;
        }

        return index >= 0 && index < count ? index : null;
    }

    protected static SkillExecutionResult NotReady(string summary)
    {
        return new SkillExecutionResult(false, summary, "该 Skill 已完成抽象接入，但完整执行链路会在后续阶段继续打通。 ");
    }
}
