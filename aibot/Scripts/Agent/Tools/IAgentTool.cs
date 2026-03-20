namespace aibot.Scripts.Agent.Tools;

public interface IAgentTool
{
    string Name { get; }

    string Description { get; }

    Task<string> QueryAsync(string? parameters, CancellationToken cancellationToken);
}
