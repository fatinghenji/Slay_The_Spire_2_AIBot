using System.Text;
using aibot.Scripts.Core;

namespace aibot.Scripts.Agent.Tools;

public sealed class InspectMapTool : RuntimeBackedToolBase
{
    public InspectMapTool(AiBotRuntime runtime) : base(runtime)
    {
    }

    public override string Name => "inspect_map";

    public override string Description => "查看当前楼层进度、地图状态与近期路线历史。";

    public override Task<string> QueryAsync(string? parameters, CancellationToken cancellationToken)
    {
        var analysis = Runtime.GetCurrentAnalysis();
        var builder = new StringBuilder();
        if (!string.IsNullOrWhiteSpace(analysis.RunProgressSummary))
        {
            builder.AppendLine("进度摘要：");
            builder.AppendLine(analysis.RunProgressSummary);
        }
        if (!string.IsNullOrWhiteSpace(analysis.RecentHistorySummary))
        {
            builder.AppendLine();
            builder.AppendLine("近期路线：");
            builder.AppendLine(analysis.RecentHistorySummary);
        }
        return Task.FromResult(builder.ToString().Trim());
    }
}
