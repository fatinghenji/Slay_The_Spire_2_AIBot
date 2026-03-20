using aibot.Scripts.Core;

namespace aibot.Scripts.Agent.Tools;

public abstract class RuntimeBackedToolBase : IAgentTool
{
    protected RuntimeBackedToolBase(AiBotRuntime runtime)
    {
        Runtime = runtime;
    }

    protected AiBotRuntime Runtime { get; }

    public abstract string Name { get; }

    public abstract string Description { get; }

    public abstract Task<string> QueryAsync(string? parameters, CancellationToken cancellationToken);

    protected string GetAnalysisSection(Func<aibot.Scripts.Decision.RunAnalysis, string> selector, string emptyFallback)
    {
        var analysis = Runtime.GetCurrentAnalysis();
        var text = selector(analysis)?.Trim();
        return string.IsNullOrWhiteSpace(text) ? emptyFallback : text;
    }
}
