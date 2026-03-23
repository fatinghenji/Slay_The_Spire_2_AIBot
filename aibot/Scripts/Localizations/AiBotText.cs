using aibot.Scripts.Agent;
using aibot.Scripts.Config;

namespace aibot.Scripts.Localization;

public enum AiBotLanguage
{
    Chinese,
    English
}

public static class AiBotText
{
    public static AiBotLanguage Resolve(AiBotConfig? config = null)
    {
        return config?.Ui.GetLanguage() ?? AiBotLanguage.Chinese;
    }

    public static bool IsEnglish(AiBotConfig? config = null)
    {
        return Resolve(config) == AiBotLanguage.English;
    }

    public static string Pick(AiBotConfig? config, string chinese, string english)
    {
        return Pick(Resolve(config), chinese, english);
    }

    public static string Pick(AiBotLanguage language, string chinese, string english)
    {
        return language == AiBotLanguage.English ? english : chinese;
    }

    public static string RoleName(AiBotConfig? config, AgentConversationRole role)
    {
        return role switch
        {
            AgentConversationRole.System => Pick(config, "系统", "System"),
            AgentConversationRole.User => Pick(config, "你", "You"),
            AgentConversationRole.Agent => "Agent",
            _ => Pick(config, "消息", "Message")
        };
    }
}
