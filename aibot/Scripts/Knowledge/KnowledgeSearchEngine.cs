using System.Text;
using aibot.Scripts.Decision;

namespace aibot.Scripts.Knowledge;

public sealed class KnowledgeSearchEngine
{
    private readonly GuideKnowledgeBase _knowledgeBase;

    public KnowledgeSearchEngine(GuideKnowledgeBase knowledgeBase)
    {
        _knowledgeBase = knowledgeBase;
    }

    public KnowledgeAnswer Search(string question, RunAnalysis analysis)
    {
        var query = question?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(query))
        {
            return KnowledgeAnswer.Empty;
        }

        var sections = new List<string>();
        var sources = new List<string>();
        var normalized = GuideKnowledgeBase.Normalize(query);
        var cardName = ExtractNamedTarget(query, new[] { "卡牌", "card", "牌" });
        var relicName = ExtractNamedTarget(query, new[] { "遗物", "relic" });
        var buildName = ExtractNamedTarget(query, new[] { "构筑", "build", "流派", "套路" });

        if (!string.IsNullOrWhiteSpace(cardName))
        {
            var card = _knowledgeBase.FindCard(cardName, analysis.CharacterId) ?? _knowledgeBase.FindCard(cardName);
            if (card is not null)
            {
                sections.Add(BuildCardSection(card));
                sources.Add($"card:{card.Slug}");
            }
        }

        if (!string.IsNullOrWhiteSpace(relicName))
        {
            var relic = _knowledgeBase.FindRelic(relicName, analysis.CharacterId) ?? _knowledgeBase.FindRelic(relicName);
            if (relic is not null)
            {
                sections.Add(BuildRelicSection(relic));
                sources.Add($"relic:{relic.Slug}");
            }
        }

        var buildMatches = FindBuildMatches(buildName ?? query, analysis.CharacterId);
        if (buildMatches.Count > 0)
        {
            sections.Add(BuildBuildSection(buildMatches));
            sources.AddRange(buildMatches.Select(build => $"build:{build.Slug}"));
        }

        if (LooksLikeCharacterQuestion(normalized))
        {
            var characterBrief = _knowledgeBase.BuildCharacterBrief(analysis.CharacterId);
            var characterGuide = _knowledgeBase.BuildCharacterGuideSummary(analysis.CharacterId);
            var characterSection = ComposeSection("角色攻略", characterBrief, characterGuide);
            if (!string.IsNullOrWhiteSpace(characterSection))
            {
                sections.Add(characterSection);
                sources.Add($"character:{analysis.CharacterId}");
            }
        }

        if (LooksLikeMechanicsQuestion(normalized))
        {
            if (!string.IsNullOrWhiteSpace(_knowledgeBase.CoreMechanicsSummary))
            {
                sections.Add("核心机制：\n" + _knowledgeBase.CoreMechanicsSummary);
                sources.Add("mechanics:core");
            }
        }

        var snippets = ExtractSnippets(query, analysis);
        if (snippets.Count > 0)
        {
            sections.Add("相关知识片段：\n" + string.Join("\n", snippets.Select(snippet => $"- {snippet}")));
            sources.Add("markdown:snippets");
        }

        if (sections.Count == 0)
        {
            var fallback = BuildFallbackAnswer(analysis);
            return new KnowledgeAnswer(false, fallback, Array.Empty<string>());
        }

        var answer = string.Join("\n\n", sections.Distinct());
        return new KnowledgeAnswer(true, Trim(answer, 3200), sources.Distinct().ToList());
    }

    private List<BuildGuideEntry> FindBuildMatches(string query, int characterId)
    {
        var normalized = GuideKnowledgeBase.Normalize(query);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return _knowledgeBase.GetBuildsForCharacter(characterId).Take(3).ToList();
        }

        return _knowledgeBase.Builds
            .Where(build => build.CharacterId == characterId)
            .Where(build => Matches(build.NameEn, normalized)
                || Matches(build.NameZh, normalized)
                || Matches(build.Slug, normalized)
                || Matches(build.SummaryEn, normalized)
                || Matches(build.StrategyEn, normalized)
                || Matches(build.TipsEn, normalized))
            .Take(3)
            .ToList();
    }

    private List<string> ExtractSnippets(string query, RunAnalysis analysis)
    {
        var candidates = Tokenize(query)
            .Concat(analysis.DeckCardNames.Take(4))
            .Concat(analysis.RelicNames.Take(3))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Where(term => !string.IsNullOrWhiteSpace(term))
            .Take(6)
            .ToList();

        if (candidates.Count == 0)
        {
            return new List<string>();
        }

        var digest = _knowledgeBase.BuildKnowledgeDigest(analysis.CharacterId, candidates, candidates, Array.Empty<string>(), 6, 4);
        if (string.IsNullOrWhiteSpace(digest))
        {
            return new List<string>();
        }

        return digest
            .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(line => line.StartsWith("- ", StringComparison.Ordinal))
            .Select(line => line[2..].Trim())
            .Where(line => !line.EndsWith(':'))
            .Distinct()
            .Take(6)
            .ToList();
    }

    private static string BuildCardSection(CardGuideEntry card)
    {
        var lines = new List<string>
        {
            $"卡牌：{card.NameEn} / {card.NameZh}",
            $"类型：{card.CardType ?? "Unknown"}"
        };

        if (!string.IsNullOrWhiteSpace(card.DescriptionEn))
        {
            lines.Add($"描述：{card.DescriptionEn}");
        }

        return string.Join("\n", lines);
    }

    private static string BuildRelicSection(RelicGuideEntry relic)
    {
        var lines = new List<string>
        {
            $"遗物：{relic.NameEn} / {relic.NameZh}"
        };

        if (!string.IsNullOrWhiteSpace(relic.DescriptionEn))
        {
            lines.Add($"描述：{relic.DescriptionEn}");
        }

        return string.Join("\n", lines);
    }

    private static string BuildBuildSection(IReadOnlyList<BuildGuideEntry> builds)
    {
        var builder = new StringBuilder();
        builder.AppendLine("推荐构筑：");
        foreach (var build in builds)
        {
            builder.AppendLine($"- {build.NameEn} / {build.NameZh}");
            if (!string.IsNullOrWhiteSpace(build.SummaryEn))
            {
                builder.AppendLine($"  摘要：{build.SummaryEn}");
            }

            if (!string.IsNullOrWhiteSpace(build.TipsEn))
            {
                builder.AppendLine($"  要点：{build.TipsEn}");
            }
        }

        return builder.ToString().Trim();
    }

    private static string ComposeSection(string title, params string[] values)
    {
        var parts = values
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim())
            .ToList();

        if (parts.Count == 0)
        {
            return string.Empty;
        }

        return title + "：\n" + string.Join("\n", parts);
    }

    private static bool LooksLikeCharacterQuestion(string normalized)
    {
        return normalized.Contains("角色", StringComparison.OrdinalIgnoreCase)
            || normalized.Contains("职业", StringComparison.OrdinalIgnoreCase)
            || normalized.Contains("玩法", StringComparison.OrdinalIgnoreCase)
            || normalized.Contains("playstyle", StringComparison.OrdinalIgnoreCase)
            || normalized.Contains("build", StringComparison.OrdinalIgnoreCase)
            || normalized.Contains("构筑", StringComparison.OrdinalIgnoreCase);
    }

    private static bool LooksLikeMechanicsQuestion(string normalized)
    {
        return normalized.Contains("机制", StringComparison.OrdinalIgnoreCase)
            || normalized.Contains("能量", StringComparison.OrdinalIgnoreCase)
            || normalized.Contains("格挡", StringComparison.OrdinalIgnoreCase)
            || normalized.Contains("抽牌", StringComparison.OrdinalIgnoreCase)
            || normalized.Contains("状态", StringComparison.OrdinalIgnoreCase)
            || normalized.Contains("debuff", StringComparison.OrdinalIgnoreCase)
            || normalized.Contains("buff", StringComparison.OrdinalIgnoreCase)
            || normalized.Contains("keyword", StringComparison.OrdinalIgnoreCase);
    }

    private static bool Matches(string? value, string normalizedQuery)
    {
        return !string.IsNullOrWhiteSpace(value)
            && GuideKnowledgeBase.Normalize(value).Contains(normalizedQuery, StringComparison.OrdinalIgnoreCase);
    }

    private static IEnumerable<string> Tokenize(string query)
    {
        return query
            .Split(new[] { ' ', '　', ',', '，', '。', '.', '?', '？', '!', '！', ':', '：', '/', '\\', '|', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(token => token.Length >= 2);
    }

    private static string? ExtractNamedTarget(string query, IEnumerable<string> markers)
    {
        foreach (var marker in markers)
        {
            var index = query.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
            if (index < 0)
            {
                continue;
            }

            var tail = query[(index + marker.Length)..].Trim();
            tail = tail.TrimStart('是', '：', ':', '？', '?', '的');
            if (!string.IsNullOrWhiteSpace(tail))
            {
                return tail.Trim();
            }
        }

        return null;
    }

    private static string BuildFallbackAnswer(RunAnalysis analysis)
    {
        var builder = new StringBuilder();
        builder.AppendLine("我没有在本地知识库里找到足够直接的条目。你可以换一种更具体的问法，例如：");
        builder.AppendLine("- 这张卡有什么用？");
        builder.AppendLine("- 当前角色有哪些推荐构筑？");
        builder.AppendLine("- 某个遗物适合什么套路？");

        if (!string.IsNullOrWhiteSpace(analysis.RecommendedBuildName))
        {
            builder.AppendLine();
            builder.AppendLine($"当前局面参考构筑：{analysis.RecommendedBuildName}");
        }

        return builder.ToString().Trim();
    }

    private static string Trim(string value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length <= maxLength)
        {
            return value;
        }

        return value[..Math.Max(0, maxLength - 3)].TrimEnd() + "...";
    }
}

public sealed record KnowledgeAnswer(bool HasAnswer, string Answer, IReadOnlyList<string> Sources)
{
    public static KnowledgeAnswer Empty { get; } = new(false, string.Empty, Array.Empty<string>());
}