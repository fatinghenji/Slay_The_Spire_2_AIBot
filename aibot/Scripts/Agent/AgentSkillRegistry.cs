using aibot.Scripts.Agent.Skills;
using aibot.Scripts.Agent.Tools;

namespace aibot.Scripts.Agent;

public sealed class AgentSkillRegistry
{
    private readonly Dictionary<string, IAgentSkill> _skills = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, IAgentTool> _tools = new(StringComparer.OrdinalIgnoreCase);

    public void RegisterSkill(IAgentSkill skill)
    {
        _skills[skill.Name] = skill;
    }

    public void RegisterTool(IAgentTool tool)
    {
        _tools[tool.Name] = tool;
    }

    public IAgentSkill? FindSkillByName(string name)
    {
        return _skills.TryGetValue(name, out var skill) ? skill : null;
    }

    public IAgentTool? FindToolByName(string name)
    {
        return _tools.TryGetValue(name, out var tool) ? tool : null;
    }

    public IReadOnlyList<IAgentSkill> GetAvailableSkills(AgentMode mode)
    {
        return mode switch
        {
            AgentMode.FullAuto => _skills.Values.OrderBy(skill => skill.Name).ToList(),
            AgentMode.SemiAuto => _skills.Values.OrderBy(skill => skill.Name).ToList(),
            AgentMode.Assist => Array.Empty<IAgentSkill>(),
            AgentMode.QnA => Array.Empty<IAgentSkill>(),
            _ => Array.Empty<IAgentSkill>()
        };
    }

    public IReadOnlyList<IAgentTool> GetAvailableTools(AgentMode mode)
    {
        return _tools.Values.OrderBy(tool => tool.Name).ToList();
    }

    public IReadOnlyList<string> GetSkillDescriptions()
    {
        return _skills.Values
            .OrderBy(skill => skill.Name)
            .Select(skill => $"{skill.Name}: {skill.Description}")
            .ToList();
    }

    public IReadOnlyList<string> GetToolDescriptions()
    {
        return _tools.Values
            .OrderBy(tool => tool.Name)
            .Select(tool => $"{tool.Name}: {tool.Description}")
            .ToList();
    }
}
