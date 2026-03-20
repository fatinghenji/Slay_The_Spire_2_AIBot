using System.Text;
using aibot.Scripts.Core;

namespace aibot.Scripts.Agent.Tools;

public sealed class InspectDeckTool : RuntimeBackedToolBase
{
    public InspectDeckTool(AiBotRuntime runtime) : base(runtime)
    {
    }

    public override string Name => "inspect_deck";

    public override string Description => "查看当前卡组、结构与建议构筑摘要。";

    public override Task<string> QueryAsync(string? parameters, CancellationToken cancellationToken)
    {
        var analysis = Runtime.GetCurrentAnalysis();
        var builder = new StringBuilder();
        builder.AppendLine($"推荐构筑：{analysis.RecommendedBuildName}");
        if (!string.IsNullOrWhiteSpace(analysis.RecommendedBuildSummary))
        {
            builder.AppendLine(analysis.RecommendedBuildSummary);
        }
        if (!string.IsNullOrWhiteSpace(analysis.DeckSummary))
        {
            builder.AppendLine();
            builder.AppendLine("卡组摘要：");
            builder.AppendLine(analysis.DeckSummary);
        }
        if (!string.IsNullOrWhiteSpace(analysis.DeckStructureSummary))
        {
            builder.AppendLine();
            builder.AppendLine("卡组结构：");
            builder.AppendLine(analysis.DeckStructureSummary);
        }
        return Task.FromResult(builder.ToString().Trim());
    }
}
