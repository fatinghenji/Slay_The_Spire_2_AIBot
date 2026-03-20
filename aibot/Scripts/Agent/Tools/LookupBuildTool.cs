using System.Text;
using aibot.Scripts.Core;

namespace aibot.Scripts.Agent.Tools;

public sealed class LookupBuildTool : RuntimeBackedToolBase
{
    public LookupBuildTool(AiBotRuntime runtime) : base(runtime)
    {
    }

    public override string Name => "lookup_build";

    public override string Description => "按角色或构筑名称查询推荐构筑信息。";

    public override Task<string> QueryAsync(string? parameters, CancellationToken cancellationToken)
    {
        if (Runtime.KnowledgeBase is null)
        {
            return Task.FromResult("当前知识库不可用。");
        }

        var analysis = Runtime.GetCurrentAnalysis();
        var query = parameters?.Trim();
        var builds = Runtime.KnowledgeBase.Builds
            .Where(build => build.CharacterId == analysis.CharacterId)
            .Where(build => string.IsNullOrWhiteSpace(query)
                || build.NameEn.Contains(query, StringComparison.OrdinalIgnoreCase)
                || build.NameZh.Contains(query, StringComparison.OrdinalIgnoreCase)
                || build.Slug.Contains(query, StringComparison.OrdinalIgnoreCase))
            .Take(5)
            .ToList();

        if (builds.Count == 0)
        {
            return Task.FromResult(string.IsNullOrWhiteSpace(query)
                ? "当前角色没有可用的构筑条目。"
                : $"未找到匹配的构筑：{query}");
        }

        var builder = new StringBuilder();
        foreach (var build in builds)
        {
            builder.AppendLine($"构筑：{build.NameEn} / {build.NameZh}");
            if (!string.IsNullOrWhiteSpace(build.SummaryEn))
            {
                builder.AppendLine($"摘要：{build.SummaryEn}");
            }
            if (!string.IsNullOrWhiteSpace(build.TipsEn))
            {
                builder.AppendLine($"要点：{build.TipsEn}");
            }
            builder.AppendLine();
        }

        return Task.FromResult(builder.ToString().Trim());
    }
}
