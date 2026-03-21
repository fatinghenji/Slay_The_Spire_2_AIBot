using MegaCrit.Sts2.Core.Logging;
using aibot.Scripts.Agent.Skills;
using aibot.Scripts.Core;
using aibot.Scripts.Decision;
using aibot.Scripts.Localization;
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
        AgentChatDialog.ShowForMode(Mode, AiBotText.Pick(_runtime.Config,
            "半自动模式已开启：你可以直接输入查询、可执行操作，或者让我代打当前回合。",
            "Semi Auto mode is active: you can ask questions, issue executable commands, or ask me to play the current turn."));
        AgentChatDialog.ClearPendingAction();
        Log.Info($"[AiBot.Agent] SemiAuto mode entered. Reason={_activationReason}");
        return Task.CompletedTask;
    }

    public Task OnDeactivateAsync()
    {
        AgentChatDialog.ClearPendingAction();
        AgentChatDialog.HideDialog();
        return Task.CompletedTask;
    }

    public Task OnTickAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public async Task<string> OnUserInputAsync(string input, CancellationToken cancellationToken)
    {
        var automaticCombatAction = await TryHandleTurnExecutionRequestAsync(input, cancellationToken);
        if (!string.IsNullOrWhiteSpace(automaticCombatAction))
        {
            return automaticCombatAction;
        }

        var combatAdvice = await TryHandleCombatAdviceQuestionAsync(input, cancellationToken);
        if (!string.IsNullOrWhiteSpace(combatAdvice))
        {
            return combatAdvice;
        }

        var currentDecisionExecution = await TryHandleCurrentDecisionExecutionAsync(input, cancellationToken);
        if (!string.IsNullOrWhiteSpace(currentDecisionExecution))
        {
            return currentDecisionExecution;
        }

        var intent = await _intentParser.ParseWithFallbackAsync(input, cancellationToken);
        if (intent.Kind == ParsedIntentKind.Unknown)
        {
            var help = AiBotText.Pick(_runtime.Config,
                "未识别你的指令。你可以试试：查看牌组、查看敌人、分析局势、打出 Strike、使用药水、结束回合，或者说“帮我打出 打击 防御 打击”。",
                "I couldn't map that to an action. Try things like: inspect deck, inspect enemies, analyze the run, play Strike, use a potion, end turn, or say 'play Strike, Defend, Strike'.");
            AiBotDecisionFeed.Publish(new DecisionTrace("SemiAuto", "Parser", "Could not parse input", input));
            return help;
        }

        return await ExecuteIntentAsync(intent, input, cancellationToken);
    }

    public void Dispose()
    {
        _intentParser.Dispose();
    }

    private async Task<string?> TryHandleTurnExecutionRequestAsync(string input, CancellationToken cancellationToken)
    {
        if (!CombatAdvisor.IsTurnExecutionRequest(input))
        {
            return null;
        }

        var result = await CombatAdvisor.PlayWholeTurnAsync(_runtime, cancellationToken);
        AiBotDecisionFeed.Publish(new DecisionTrace("SemiAuto", "AutoTurn", "Played current turn automatically", result));
        return result;
    }

    private async Task<string?> TryHandleCombatAdviceQuestionAsync(string input, CancellationToken cancellationToken)
    {
        if (!CombatAdvisor.IsCombatAdviceQuestion(input))
        {
            return null;
        }

        var decision = await CombatAdvisor.GetCombatDecisionAsync(_runtime, cancellationToken);
        if (decision is null)
        {
            return AiBotText.Pick(_runtime.Config,
                "当前不在可以给出出牌建议的战斗阶段。",
                "I can't give a combat-play recommendation because the run is not currently in a playable combat state.");
        }

        var recommendation = CombatAdvisor.FormatRecommendation(_runtime.Config, decision);
        AiBotDecisionFeed.Publish(new DecisionTrace("SemiAuto", "CombatAdvice", "Answered current-play question", recommendation));
        return recommendation;
    }

    private async Task<string?> TryHandleCurrentDecisionExecutionAsync(string input, CancellationToken cancellationToken)
    {
        if (!CurrentDecisionAdvisor.LooksLikeDecisionRequest(input))
        {
            return null;
        }

        var result = await CurrentDecisionAdvisor.TryExecuteCurrentDecisionAsync(_runtime, cancellationToken);
        if (string.IsNullOrWhiteSpace(result))
        {
            return null;
        }

        AiBotDecisionFeed.Publish(new DecisionTrace("SemiAuto", "ContextDecision", "Executed current decision-screen action", result));
        return result;
    }

    private async Task<string> ExecuteIntentAsync(ParsedIntent intent, string rawInput, CancellationToken cancellationToken)
    {
        return intent.Kind switch
        {
            ParsedIntentKind.Tool => await ExecuteToolIntentAsync(intent, rawInput, cancellationToken),
            ParsedIntentKind.Skill => await ExecuteSkillIntentAsync(intent, rawInput, cancellationToken),
            ParsedIntentKind.Sequence => await ExecuteSequenceIntentAsync(intent, rawInput, cancellationToken),
            _ => AiBotText.Pick(_runtime.Config, "未识别你的指令。", "I couldn't understand that command.")
        };
    }

    private async Task<string> ExecuteToolIntentAsync(ParsedIntent intent, string rawInput, CancellationToken cancellationToken)
    {
        var tool = AgentCore.Instance.Registry.FindToolByName(intent.Name);
        if (tool is null)
        {
            return AiBotText.Pick(_runtime.Config, $"未找到可用工具：{intent.Name}", $"Could not find the tool '{intent.Name}'.");
        }

        var result = await tool.QueryAsync(intent.RawArgument, cancellationToken);
        AiBotDecisionFeed.Publish(new DecisionTrace("SemiAuto", "Tool", $"Tool: {tool.Name}", result));
        return result;
    }

    private async Task<string> ExecuteSkillIntentAsync(ParsedIntent intent, string rawInput, CancellationToken cancellationToken)
    {
        var skill = AgentCore.Instance.Registry.FindSkillByName(intent.Name);
        if (skill is null)
        {
            return AiBotText.Pick(_runtime.Config, $"未找到可用技能：{intent.Name}", $"Could not find the skill '{intent.Name}'.");
        }

        if (!skill.CanExecute())
        {
            var cannot = AiBotText.Pick(_runtime.Config,
                $"当前无法执行该操作：{DescribeSkill(skill)}",
                $"That action cannot be executed right now: {DescribeSkill(skill)}");
            AiBotDecisionFeed.Publish(new DecisionTrace("SemiAuto", "Skill", $"Skill unavailable: {skill.Name}", rawInput));
            return cannot;
        }

        var execution = await skill.ExecuteAsync(intent.Parameters, cancellationToken);
        var details = string.IsNullOrWhiteSpace(execution.Details)
            ? execution.Summary
            : execution.Summary + "\n" + execution.Details;
        AiBotDecisionFeed.Publish(new DecisionTrace("SemiAuto", execution.Success ? "Skill" : "SkillFailed", $"Executed {skill.Name}", details));
        return details;
    }

    private async Task<string> ExecuteSequenceIntentAsync(ParsedIntent intent, string rawInput, CancellationToken cancellationToken)
    {
        var steps = intent.Steps ?? Array.Empty<ParsedIntent>();
        if (steps.Count == 0)
        {
            return AiBotText.Pick(_runtime.Config, "未解析到可执行的动作序列。", "No executable action sequence was parsed.");
        }

        var results = new List<string>();
        for (var index = 0; index < steps.Count; index++)
        {
            var step = steps[index];
            if (step.Kind != ParsedIntentKind.Skill)
            {
                continue;
            }

            var skill = AgentCore.Instance.Registry.FindSkillByName(step.Name);
            if (skill is null)
            {
                results.Add(AiBotText.Pick(_runtime.Config, $"第 {index + 1} 步未找到技能：{step.Name}", $"Step {index + 1} references an unknown skill: {step.Name}."));
                break;
            }

            if (!skill.CanExecute())
            {
                results.Add(AiBotText.Pick(_runtime.Config, $"第 {index + 1} 步当前无法执行：{DescribeSkill(skill)}", $"Step {index + 1} can’t be executed right now: {DescribeSkill(skill)}."));
                break;
            }

            var execution = await skill.ExecuteAsync(step.Parameters, cancellationToken);
            var summary = string.IsNullOrWhiteSpace(execution.Details)
                ? execution.Summary
                : execution.Summary + "\n" + execution.Details;
            results.Add(summary);

            if (!execution.Success)
            {
                AiBotDecisionFeed.Publish(new DecisionTrace("SemiAuto", "SkillSequenceFailed", $"Sequence stopped at {skill.Name}", summary));
                return string.Join("\n", results);
            }
        }

        var combined = string.Join("\n", results);
        AiBotDecisionFeed.Publish(new DecisionTrace("SemiAuto", "SkillSequence", $"Executed sequence with {steps.Count} step(s)", combined));
        return combined;
    }

    private string DescribeSkill(IAgentSkill skill)
    {
        return skill.Name switch
        {
            "play_card" => AiBotText.Pick(_runtime.Config, "打出一张手牌", "play a card"),
            "use_potion" => AiBotText.Pick(_runtime.Config, "使用药水", "use a potion"),
            "end_turn" => AiBotText.Pick(_runtime.Config, "结束回合", "end the turn"),
            "inspect_deck" => AiBotText.Pick(_runtime.Config, "查看牌组", "inspect the deck"),
            "choose_relic" => AiBotText.Pick(_runtime.Config, "选择遗物", "choose a relic"),
            "choose_bundle" => AiBotText.Pick(_runtime.Config, "选择卡包", "choose a bundle"),
            "pick_card_reward" => AiBotText.Pick(_runtime.Config, "选择卡牌奖励", "pick a card reward"),
            "select_card" => AiBotText.Pick(_runtime.Config, "选择卡牌", "select a card"),
            "purchase_shop" => AiBotText.Pick(_runtime.Config, "购买商店物品", "buy a shop item"),
            "rest_site" => AiBotText.Pick(_runtime.Config, "执行休息点操作", "perform a rest-site action"),
            "choose_event_option" => AiBotText.Pick(_runtime.Config, "选择事件选项", "choose an event option"),
            "claim_reward" => AiBotText.Pick(_runtime.Config, "领取奖励", "claim rewards"),
            _ => skill.Name
        };
    }
}
