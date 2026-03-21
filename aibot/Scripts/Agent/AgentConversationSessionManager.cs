using System.Text;

namespace aibot.Scripts.Agent;

public enum AgentConversationRole
{
    System,
    User,
    Agent
}

public sealed record AgentConversationMessage(AgentConversationRole Role, string Content, DateTimeOffset TimestampUtc);

public sealed class AgentConversationSessionManager
{
    private readonly object _gate = new();
    private readonly Func<int> _maxHistoryProvider;
    private readonly Dictionary<AgentMode, List<AgentConversationMessage>> _sessions = new();

    public AgentConversationSessionManager(Func<int> maxHistoryProvider)
    {
        _maxHistoryProvider = maxHistoryProvider;
        foreach (AgentMode mode in Enum.GetValues(typeof(AgentMode)))
        {
            _sessions[mode] = new List<AgentConversationMessage>();
        }
    }

    public void AddSystemMessage(AgentMode mode, string? content)
    {
        AddMessage(mode, AgentConversationRole.System, content);
    }

    public void AddUserMessage(AgentMode mode, string? content)
    {
        AddMessage(mode, AgentConversationRole.User, content);
    }

    public void AddAgentMessage(AgentMode mode, string? content)
    {
        AddMessage(mode, AgentConversationRole.Agent, content);
    }

    public void AddMessage(AgentMode mode, AgentConversationRole role, string? content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return;
        }

        lock (_gate)
        {
            var messages = _sessions[mode];
            messages.Add(new AgentConversationMessage(role, content.Trim(), DateTimeOffset.UtcNow));

            var maxHistory = Math.Max(1, _maxHistoryProvider());
            while (messages.Count > maxHistory)
            {
                messages.RemoveAt(0);
            }
        }
    }

    public IReadOnlyList<AgentConversationMessage> GetMessages(AgentMode mode, int? maxMessages = null)
    {
        lock (_gate)
        {
            var snapshot = _sessions[mode].ToList();
            if (maxMessages.HasValue && maxMessages.Value > 0 && snapshot.Count > maxMessages.Value)
            {
                snapshot = snapshot.Skip(snapshot.Count - maxMessages.Value).ToList();
            }

            return snapshot;
        }
    }

    public string BuildTranscript(AgentMode mode, int maxMessages = 8, string? excludeTrailingUserMessage = null)
    {
        var messages = GetMessages(mode, maxMessages).ToList();
        if (!string.IsNullOrWhiteSpace(excludeTrailingUserMessage)
            && messages.Count > 0
            && messages[^1].Role == AgentConversationRole.User
            && string.Equals(messages[^1].Content.Trim(), excludeTrailingUserMessage.Trim(), StringComparison.Ordinal))
        {
            messages.RemoveAt(messages.Count - 1);
        }

        if (messages.Count == 0)
        {
            return string.Empty;
        }

        var builder = new StringBuilder();
        foreach (var message in messages)
        {
            builder.Append(RoleLabel(message.Role)).Append(": ").AppendLine(message.Content);
        }

        return builder.ToString().Trim();
    }

    private static string RoleLabel(AgentConversationRole role)
    {
        return role switch
        {
            AgentConversationRole.System => "System",
            AgentConversationRole.User => "User",
            AgentConversationRole.Agent => "Agent",
            _ => "Message"
        };
    }
}