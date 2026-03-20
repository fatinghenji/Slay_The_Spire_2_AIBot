using System.Text;
using aibot.Scripts.Core;

namespace aibot.Scripts.Agent.Tools;

public sealed class AnalyzeRunTool : RuntimeBackedToolBase
{
    public AnalyzeRunTool(AiBotRuntime runtime) : base(runtime)
    {
    }

    public override string Name => "analyze_run";

    public override string Description => "输出当前整局游戏的综合分析摘要。";

    public override Task<string> QueryAsync(string? parameters, CancellationToken cancellationToken)
    {
        var analysis = Runtime.GetCurrentAnalysis();
        var builder = new StringBuilder();
        builder.AppendLine($"角色：{analysis.CharacterName}");
        builder.AppendLine($"推荐构筑：{analysis.RecommendedBuildName}");
        if (!string.IsNullOrWhiteSpace(analysis.RunProgressSummary))
        {
            builder.AppendLine();
            builder.AppendLine("进度：");
            builder.AppendLine(analysis.RunProgressSummary);
        }
        if (!string.IsNullOrWhiteSpace(analysis.PlayerStateSummary))
        {
            builder.AppendLine();
            builder.AppendLine("玩家状态：");
            builder.AppendLine(analysis.PlayerStateSummary);
        }
        if (!string.IsNullOrWhiteSpace(analysis.StrategicNeedsSummary))
        {
            builder.AppendLine();
            builder.AppendLine("策略需求：");
            builder.AppendLine(analysis.StrategicNeedsSummary);
        }
        if (!string.IsNullOrWhiteSpace(analysis.RemovalCandidateSummary))
        {
            builder.AppendLine();
            builder.AppendLine("移除候选：");
            builder.AppendLine(analysis.RemovalCandidateSummary);
        }
        return Task.FromResult(builder.ToString().Trim());
    }
}
