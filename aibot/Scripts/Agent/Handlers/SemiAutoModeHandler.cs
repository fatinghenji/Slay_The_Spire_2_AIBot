using MegaCrit.Sts2.Core.Logging;
using aibot.Scripts.Decision;
using aibot.Scripts.Core;
using aibot.Scripts.Ui;

namespace aibot.Scripts.Agent.Handlers;

public sealed class SemiAutoModeHandler : IAgentModeHandler
{
    private readonly AiBotRuntime _runtime;
    private readonly string _activationReason;
    private readonly IntentParser _intentParser;

    public SemiAutoModeHandler(AiBotRuntime runtime, string activationReason)
    {
        _runtime = runtime;
        _activationReason = activationReason;
        _intentParser = new IntentParser(runtime, AgentCore.Instance.Registry);
    }

    public AgentMode Mode => AgentMode.SemiAuto;

    public Task OnActivateAsync(CancellationToken cancellationToken)
    {
        _runtime.DeactivateLegacyFullAuto();
        AgentChatDialog.EnsureCreated(_runtime);
        AgentChatDialog.ShowForMode(Mode, "半自动模式已开启：你可以输入查询或游戏内操作指令。");
        Log.Info($"[AiBot.Agent] SemiAuto mode entered. Reason={_activationReason}");
        return Task.CompletedTask;
    }

    public Task OnDeactivateAsync()
    {
        AgentChatDialog.HideDialog();
        return Task.CompletedTask;
    }

    public Task OnTickAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public async Task<string> OnUserInputAsync(string input, CancellationToken cancellationToken)
    {
        var intent = _intentParser.Parse(input);
        if (intent.Kind == ParsedIntentKind.Unknown)
        {
            var help = "未识别你的指令。你可以尝试：查看卡组、查看遗物、分析局势、打出 Strike、使用药水、结束回合、领取奖励。";
            AiBotDecisionFeed.Publish(new DecisionTrace("SemiAuto", "Parser", "无法识别输入", input));
            return help;
        }

        if (intent.Kind == ParsedIntentKind.Tool)
        {
            var tool = AgentCore.Instance.Registry.FindToolByName(intent.Name);
            if (tool is null)
            {
                return $"未找到可用工具：{intent.Name}";
            }

            var result = await tool.QueryAsync(intent.RawArgument, cancellationToken);
            AiBotDecisionFeed.Publish(new DecisionTrace("SemiAuto", "Tool", $"调用工具 {tool.Name}", result));
            return result;
        }

        var skill = AgentCore.Instance.Registry.FindSkillByName(intent.Name);
        if (skill is null)
        {
            return $"未找到可用技能：{intent.Name}";
        }

        if (!skill.CanExecute())
        {
            var cannot = $"当前无法执行技能：{skill.Description}";
            AiBotDecisionFeed.Publish(new DecisionTrace("SemiAuto", "Skill", $"技能不可执行 {skill.Name}", input));
            return cannot;
        }

        var execution = await skill.ExecuteAsync(intent.Parameters, cancellationToken);
        var details = string.IsNullOrWhiteSpace(execution.Details)
            ? execution.Summary
            : execution.Summary + "\n" + execution.Details;
        AiBotDecisionFeed.Publish(new DecisionTrace("SemiAuto", execution.Success ? "Skill" : "SkillFailed", $"执行技能 {skill.Name}", details));
        return details;
    }

    public void Dispose()
    {
    }
}
