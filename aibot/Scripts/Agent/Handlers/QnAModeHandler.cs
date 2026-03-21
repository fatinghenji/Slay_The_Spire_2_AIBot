using MegaCrit.Sts2.Core.Logging;
using aibot.Scripts.Agent.Tools;
using aibot.Scripts.Core;
using aibot.Scripts.Decision;
using aibot.Scripts.Knowledge;
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
        AgentChatDialog.ShowForMode(Mode, "问答模式已开启：优先回答杀戮尖塔2相关机制、卡牌、遗物、构筑与当前对局问题。 ");
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
            return "你可以直接问我卡牌、遗物、构筑、机制或当前局势。";
        }

        if (!IsInGameDomain(question))
        {
            var refusal = "我目前只回答《杀戮尖塔2》游戏相关问题，例如卡牌、遗物、机制、路线和当前对局建议。";
            AiBotDecisionFeed.Publish(new DecisionTrace("QnA", "Guardrail", "拒绝游戏外问题", question));
            return refusal;
        }

        var toolResult = await TryHandleToolLikeQuestionAsync(question, cancellationToken);
        if (!string.IsNullOrWhiteSpace(toolResult))
        {
            AiBotDecisionFeed.Publish(new DecisionTrace("QnA", "Tool", "工具式问答", toolResult));
            return toolResult;
        }

        var analysis = _runtime.GetCurrentAnalysis();
        var knowledgeAnswer = _searchEngine?.Search(question, analysis) ?? KnowledgeAnswer.Empty;
        if (knowledgeAnswer.HasAnswer)
        {
            var response = AppendContextHint(knowledgeAnswer.Answer, analysis);
            AiBotDecisionFeed.Publish(new DecisionTrace("QnA", "Knowledge", "本地知识检索", response));
            return response;
        }

        var llmAnswer = await TryAnswerWithLlmAsync(question, analysis, knowledgeAnswer, cancellationToken);
        if (!string.IsNullOrWhiteSpace(llmAnswer))
        {
            AiBotDecisionFeed.Publish(new DecisionTrace("QnA", "LLM", "受限云端补答", llmAnswer));
            return llmAnswer;
        }

        var fallback = BuildAnalysisBackedFallback(analysis);
        AiBotDecisionFeed.Publish(new DecisionTrace("QnA", "Fallback", "局势摘要兜底", fallback));
        return fallback;
    }

    public void Dispose()
    {
        _llmBridge?.Dispose();
    }

    private async Task<string?> TryAnswerWithLlmAsync(string question, RunAnalysis analysis, KnowledgeAnswer knowledgeAnswer, CancellationToken cancellationToken)
    {
        if (_llmBridge is null)
        {
            return null;
        }

        var recentConversation = AgentCore.Instance.ConversationSessions.BuildTranscript(Mode, 8, question);
        var answer = await _llmBridge.AnswerQuestionAsync(question, analysis, knowledgeAnswer, recentConversation, cancellationToken);
        if (answer is null || string.IsNullOrWhiteSpace(answer.Content))
        {
            return null;
        }

        return AppendContextHint(answer.Content, analysis);
    }

    private async Task<string?> TryHandleToolLikeQuestionAsync(string question, CancellationToken cancellationToken)
    {
        var registry = AgentCore.Instance.Registry;
        var normalized = GuideKnowledgeBase.Normalize(question);

        if (ContainsAny(normalized, "卡组", "deck"))
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

        if (ContainsAny(normalized, "敌人", "enemy", "怪", "意图"))
        {
            return await QueryToolAsync(registry.FindToolByName("inspect_enemy"), null, cancellationToken);
        }

        if (ContainsAny(normalized, "地图", "路线", "map", "path"))
        {
            return await QueryToolAsync(registry.FindToolByName("inspect_map"), null, cancellationToken);
        }

        if (ContainsAny(normalized, "局势", "分析", "现在该", "run"))
        {
            return await QueryToolAsync(registry.FindToolByName("analyze_run"), null, cancellationToken);
        }

        if (ContainsAny(normalized, "卡牌", "card", "这张牌", "牌") )
        {
            var cardName = ExtractTarget(question, new[] { "卡牌", "card", "这张牌", "牌" });
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

    private static bool IsInGameDomain(string question)
    {
        var normalized = GuideKnowledgeBase.Normalize(question);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return true;
        }

        var allowedMarkers = new[]
        {
            "杀戮尖塔", "sts", "spire", "卡牌", "遗物", "药水", "战斗", "构筑", "路线", "地图", "敌人", "boss", "角色", "职业", "回合", "能量", "格挡", "抽牌", "伤害", "debuff", "buff", "relic", "card", "build", "deck", "enemy", "map"
        };

        var blockedMarkers = new[]
        {
            "python", "javascript", "c#", "java", "算法", "股票", "天气", "新闻", "翻译", "写邮件", "简历", "数学题", "菜谱", "电影", "小说", "政治"
        };

        if (blockedMarkers.Any(marker => normalized.Contains(GuideKnowledgeBase.Normalize(marker), StringComparison.OrdinalIgnoreCase)))
        {
            return false;
        }

        return allowedMarkers.Any(marker => normalized.Contains(GuideKnowledgeBase.Normalize(marker), StringComparison.OrdinalIgnoreCase));
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
            tail = tail.TrimStart('是', '：', ':', '？', '?', '的');
            if (!string.IsNullOrWhiteSpace(tail))
            {
                return tail.Trim();
            }
        }

        return null;
    }

    private static string AppendContextHint(string answer, RunAnalysis analysis)
    {
        if (string.IsNullOrWhiteSpace(analysis.RecommendedBuildName))
        {
            return answer;
        }

        return answer + $"\n\n当前对局参考构筑：{analysis.RecommendedBuildName}";
    }

    private static string BuildAnalysisBackedFallback(RunAnalysis analysis)
    {
        var parts = new List<string>
        {
            "我没有在本地知识库中检索到足够直接的答案，可以尝试改问更具体的卡牌、遗物或构筑名称。"
        };

        if (!string.IsNullOrWhiteSpace(analysis.RunProgressSummary))
        {
            parts.Add("当前进度：\n" + analysis.RunProgressSummary);
        }

        if (!string.IsNullOrWhiteSpace(analysis.StrategicNeedsSummary))
        {
            parts.Add("当前策略需求：\n" + analysis.StrategicNeedsSummary);
        }

        return string.Join("\n\n", parts);
    }
}
