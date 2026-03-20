using System.Text;
using aibot.Scripts.Core;

namespace aibot.Scripts.Agent.Tools;

public sealed class InspectPotionsTool : RuntimeBackedToolBase
{
    public InspectPotionsTool(AiBotRuntime runtime) : base(runtime)
    {
    }

    public override string Name => "inspect_potions";

    public override string Description => "查看当前药水/消耗品与其摘要。";

    public override Task<string> QueryAsync(string? parameters, CancellationToken cancellationToken)
    {
        var analysis = Runtime.GetCurrentAnalysis();
        var builder = new StringBuilder();
        builder.AppendLine("当前药水：");
        builder.AppendLine(analysis.PotionNames.Count == 0 ? "无" : string.Join("、", analysis.PotionNames));
        if (!string.IsNullOrWhiteSpace(analysis.PotionSummary))
        {
            builder.AppendLine();
            builder.AppendLine("药水摘要：");
            builder.AppendLine(analysis.PotionSummary);
        }
        return Task.FromResult(builder.ToString().Trim());
    }
}
