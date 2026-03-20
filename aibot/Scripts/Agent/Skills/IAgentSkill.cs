namespace aibot.Scripts.Agent.Skills;

public enum SkillCategory
{
    Combat,
    Navigation,
    DeckManagement,
    Economy,
    RoomInteraction
}

public sealed record AgentSkillParameters(
    string? CardName = null,
    string? TargetName = null,
    string? PotionName = null,
    int? MapRow = null,
    int? MapCol = null,
    string? OptionId = null,
    string? ItemName = null,
    int? BundleIndex = null,
    int? GridX = null,
    int? GridY = null,
    bool? UseBigDivination = null);

public sealed record SkillExecutionResult(
    bool Success,
    string Summary,
    string? Details = null);

public interface IAgentSkill
{
    string Name { get; }

    string Description { get; }

    SkillCategory Category { get; }

    bool CanExecute();

    Task<SkillExecutionResult> ExecuteAsync(AgentSkillParameters? parameters, CancellationToken cancellationToken);
}
