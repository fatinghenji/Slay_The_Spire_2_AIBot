using MegaCrit.Sts2.Core.Logging;
using aibot.Scripts.Agent.Tools;
using aibot.Scripts.Core;
using aibot.Scripts.Decision;
using aibot.Scripts.Knowledge;
using aibot.Scripts.Localization;
using aibot.Scripts.Ui;

namespace aibot.Scripts.Agent.Handlers;

public sealed class QnAModeHandler : IAgentModeHandler
{
    private readonly AiBotRuntime _runtime;
    private readonly string _activationReason;
    private readonly KnowledgeSearchEngine? _searchEngine;
    private readonly AgentLlmBridge? _llmBridge;

    public QnAModeHandler(AiBotRuntime runtime, string activationReason)
    {
        _runtime = runtime;
        _activationReason = activationReason;
        _searchEngine = runtime.KnowledgeBase is null ? null : new KnowledgeSearchEngine(runtime.KnowledgeBase);
        _llmBridge = runtime.Config.CanUseCloud ? new AgentLlmBridge(runtime.Config) : null;
    }

    public AgentMode Mode => AgentMode.QnA;

    public Task OnActivateAsync(CancellationToken cancellationToken)
    {
        _runtime.DeactivateLegacyFullAuto();
        AgentChatDialog.EnsureCreated(_runtime);
        AgentChatDialog.ShowForMode(Mode, AiBotText.Pick(_runtime.Config,
            "问答模式已开启：你可以问机制、卡牌、遗物、构筑，也可以直接问“我该出什么”。",
            "QnA mode is active: ask about mechanics, cards, relics, builds, or directly ask 'what should I play'."));
        Log.Info($"[AiBot.Agent] QnA mode entered. Reason={_activationReason}");
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
        var question = input?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(question))
        {
            return AiBotText.Pick(_runtime.Config,
                "你可以直接问我卡牌、遗物、构筑、机制，或者问“我该出什么”。",
                "Ask me about cards, relics, builds, mechanics, or ask 'what should I play'.");
        }

        var combatAdvice = await TryHandleCombatAdviceQuestionAsync(question, cancellationToken);
        if (!string.IsNullOrWhiteSpace(combatAdvice))
        {
            AiBotDecisionFeed.Publish(new DecisionTrace("QnA", "CombatAdvice", "Answered current-play question", combatAdvice));
            return combatAdvice;
        }

        var currentDecisionAdvice = await TryHandleCurrentDecisionAdviceAsync(question, cancellationToken);
        if (!string.IsNullOrWhiteSpace(currentDecisionAdvice))
        {
            AiBotDecisionFeed.Publish(new DecisionTrace("QnA", "ContextDecision", "Answered current decision-screen question", currentDecisionAdvice));
            return currentDecisionAdvice;
        }

        var toolResult = await TryHandleToolLikeQuestionAsync(question, cancellationToken);
        if (!string.IsNullOrWhiteSpace(toolResult))
        {
            AiBotDecisionFeed.Publish(new DecisionTrace("QnA", "Tool", "Tool-style answer", toolResult));
            return toolResult;
        }

        var analysis = _runtime.GetCurrentAnalysis();
        var knowledgeAnswer = _searchEngine?.Search(question, analysis) ?? KnowledgeAnswer.Empty;
        if (knowledgeAnswer.HasAnswer)
        {
            var response = AppendContextHint(knowledgeAnswer.Answer, analysis);
            AiBotDecisionFeed.Publish(new DecisionTrace("QnA", "Knowledge", "Local knowledge answer", response));
            return response;
        }

        var augmentedToolContext = await TryBuildSupplementalToolContextAsync(question, analysis, cancellationToken);
        var llmAnswer = await TryAnswerWithLlmAsync(question, analysis, knowledgeAnswer, augmentedToolContext, cancellationToken);
        if (!string.IsNullOrWhiteSpace(llmAnswer))
        {
            AiBotDecisionFeed.Publish(new DecisionTrace("QnA", "LLM", "LLM answer", llmAnswer));
            return llmAnswer;
        }

        var fallback = BuildAnalysisBackedFallback(analysis, augmentedToolContext);
        AiBotDecisionFeed.Publish(new DecisionTrace("QnA", "Fallback", "Analysis fallback", fallback));
        return fallback;
    }

    public void Dispose()
    {
        _llmBridge?.Dispose();
    }

    private async Task<string?> TryHandleCombatAdviceQuestionAsync(string question, CancellationToken cancellationToken)
    {
        if (!CombatAdvisor.IsCombatAdviceQuestion(question))
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

        return CombatAdvisor.FormatRecommendation(_runtime.Config, decision);
    }

    private async Task<string?> TryHandleCurrentDecisionAdviceAsync(string question, CancellationToken cancellationToken)
    {
        if (!CurrentDecisionAdvisor.LooksLikeDecisionRequest(question))
        {
            return null;
        }

        return await CurrentDecisionAdvisor.TryRecommendCurrentDecisionAsync(_runtime, cancellationToken);
    }

    private async Task<string?> TryAnswerWithLlmAsync(
        string question,
        RunAnalysis analysis,
        KnowledgeAnswer knowledgeAnswer,
        string? supplementalContext,
        CancellationToken cancellationToken)
    {
        if (_llmBridge is null)
        {
            return null;
        }

        var recentConversation = AgentCore.Instance.ConversationSessions.BuildTranscript(Mode, 8, question);
        var answer = await _llmBridge.AnswerQuestionAsync(question, analysis, knowledgeAnswer, recentConversation, supplementalContext, cancellationToken);
        if (answer is null || string.IsNullOrWhiteSpace(answer.Content))
        {
            return null;
        }

        return AppendContextHint(answer.Content, analysis);
    }

    private async Task<string?> TryBuildSupplementalToolContextAsync(string question, RunAnalysis analysis, CancellationToken cancellationToken)
    {
        if (_llmBridge is null)
        {
            return null;
        }

        var registry = AgentCore.Instance.Registry;
        var availableTools = registry.GetAvailableTools(Mode)
            .Select(tool => $"{tool.Name}: {tool.Description}")
            .ToList();
        if (availableTools.Count == 0)
        {
            return null;
        }

        var recentConversation = AgentCore.Instance.ConversationSessions.BuildTranscript(Mode, 8, question);
        var plan = await _llmBridge.RecognizeActionPlanAsync(
            question,
            analysis,
            Array.Empty<string>(),
            availableTools,
            recentConversation,
            cancellationToken);

        if (plan is null || !string.Equals(plan.Kind, "tool", StringComparison.OrdinalIgnoreCase) || string.IsNullOrWhiteSpace(plan.ToolName))
        {
            return null;
        }

        var tool = registry.FindToolByName(plan.ToolName);
        if (tool is null)
        {
            return null;
        }

        var result = await tool.QueryAsync(plan.ToolArgument, cancellationToken);
        if (string.IsNullOrWhiteSpace(result))
        {
            return null;
        }

        return $"{tool.Name}\n{result.Trim()}";
    }

    private async Task<string?> TryHandleToolLikeQuestionAsync(string question, CancellationToken cancellationToken)
    {
        var registry = AgentCore.Instance.Registry;
        var normalized = GuideKnowledgeBase.Normalize(question);

        if (ContainsAny(normalized, "牌组", "deck"))
        {
            return await QueryToolAsync(registry.FindToolByName("inspect_deck"), null, cancellationToken);
        }

        if (ContainsAny(normalized, "遗物", "relic"))
        {
            var relicName = ExtractTarget(question, new[] { "遗物", "relic" });
            if (!string.IsNullOrWhiteSpace(relicName))
            {
                return await QueryToolAsync(registry.FindToolByName("lookup_relic"), relicName, cancellationToken);
            }

            return await QueryToolAsync(registry.FindToolByName("inspect_relics"), null, cancellationToken);
        }

        if (ContainsAny(normalized, "药水", "potion"))
        {
            return await QueryToolAsync(registry.FindToolByName("inspect_potions"), null, cancellationToken);
        }

        if (ContainsAny(normalized, "敌人", "enemy", "意图", "怪物"))
        {
            return await QueryToolAsync(registry.FindToolByName("inspect_enemy"), null, cancellationToken);
        }

        if (ContainsAny(normalized, "地图", "路线", "map", "path"))
        {
            return await QueryToolAsync(registry.FindToolByName("inspect_map"), null, cancellationToken);
        }

        if (ContainsAny(normalized, "局势", "分析", "当前局势", "run"))
        {
            return await QueryToolAsync(registry.FindToolByName("analyze_run"), null, cancellationToken);
        }

        if (ContainsAny(normalized, "卡牌", "card"))
        {
            var cardName = ExtractTarget(question, new[] { "卡牌", "card" });
            if (!string.IsNullOrWhiteSpace(cardName))
            {
                return await QueryToolAsync(registry.FindToolByName("lookup_card"), cardName, cancellationToken);
            }
        }

        if (ContainsAny(normalized, "构筑", "build", "流派", "套路"))
        {
            var buildName = ExtractTarget(question, new[] { "构筑", "build", "流派", "套路" });
            return await QueryToolAsync(registry.FindToolByName("lookup_build"), buildName, cancellationToken);
        }

        return null;
    }

    private static async Task<string?> QueryToolAsync(IAgentTool? tool, string? parameters, CancellationToken cancellationToken)
    {
        if (tool is null)
        {
            return null;
        }

        var result = await tool.QueryAsync(parameters, cancellationToken);
        return string.IsNullOrWhiteSpace(result) ? null : result;
    }

    private static bool ContainsAny(string haystack, params string[] markers)
    {
        return markers.Any(marker => haystack.Contains(GuideKnowledgeBase.Normalize(marker), StringComparison.OrdinalIgnoreCase));
    }

    private static string? ExtractTarget(string question, IEnumerable<string> markers)
    {
        foreach (var marker in markers)
        {
            var index = question.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
            if (index < 0)
            {
                continue;
            }

            var tail = question[(index + marker.Length)..].Trim();
            tail = tail.TrimStart('是', '：', ':', '，', ',', '？', '?', '的');
            if (!string.IsNullOrWhiteSpace(tail))
            {
                return tail.Trim();
            }
        }

        return null;
    }

    private string AppendContextHint(string answer, RunAnalysis analysis)
    {
        if (string.IsNullOrWhiteSpace(analysis.RecommendedBuildName))
        {
            return answer;
        }

        return answer + AiBotText.Pick(_runtime.Config,
            $"\n\n当前局势参考构筑：{analysis.RecommendedBuildName}",
            $"\n\nCurrent build context: {analysis.RecommendedBuildName}");
    }

    private string BuildAnalysisBackedFallback(RunAnalysis analysis, string? supplementalContext)
    {
        var parts = new List<string>
        {
            AiBotText.Pick(_runtime.Config,
                "我没有在本地知识中找到足够直接的答案。你可以把问题问得更具体一些，比如某张卡、某个遗物，或者直接问当前回合该怎么打。",
                "I couldn't find a direct local answer yet. Try asking about a specific card, relic, or ask what the best play is right now.")
        };

        if (!string.IsNullOrWhiteSpace(supplementalContext))
        {
            parts.Add(AiBotText.Pick(_runtime.Config,
                "补充上下文：\n" + supplementalContext,
                "Supplemental context:\n" + supplementalContext));
        }

        if (!string.IsNullOrWhiteSpace(analysis.RunProgressSummary))
        {
            parts.Add(AiBotText.Pick(_runtime.Config,
                "当前进度：\n" + analysis.RunProgressSummary,
                "Current progress:\n" + analysis.RunProgressSummary));
        }

        if (!string.IsNullOrWhiteSpace(analysis.StrategicNeedsSummary))
        {
            parts.Add(AiBotText.Pick(_runtime.Config,
                "当前策略需求：\n" + analysis.StrategicNeedsSummary,
                "Current strategic needs:\n" + analysis.StrategicNeedsSummary));
        }

        return string.Join("\n\n", parts);
    }
}
