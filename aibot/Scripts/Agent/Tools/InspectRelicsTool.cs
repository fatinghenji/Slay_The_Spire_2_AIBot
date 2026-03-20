using System.Text;
using aibot.Scripts.Core;

namespace aibot.Scripts.Agent.Tools;

public sealed class InspectRelicsTool : RuntimeBackedToolBase
{
    public InspectRelicsTool(AiBotRuntime runtime) : base(runtime)
    {
    }

    public override string Name => "inspect_relics";

    public override string Description => "查看当前遗物列表与遗物摘要。";

    public override Task<string> QueryAsync(string? parameters, CancellationToken cancellationToken)
    {
        var analysis = Runtime.GetCurrentAnalysis();
        var builder = new StringBuilder();
        builder.AppendLine("当前遗物：");
        builder.AppendLine(analysis.RelicNames.Count == 0 ? "无" : string.Join("、", analysis.RelicNames));
        if (!string.IsNullOrWhiteSpace(analysis.RelicSummary))
        {
            builder.AppendLine();
            builder.AppendLine("遗物摘要：");
            builder.AppendLine(analysis.RelicSummary);
        }
        return Task.FromResult(builder.ToString().Trim());
    }
}
