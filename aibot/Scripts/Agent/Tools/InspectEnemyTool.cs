using System.Text;
using aibot.Scripts.Core;

namespace aibot.Scripts.Agent.Tools;

public sealed class InspectEnemyTool : RuntimeBackedToolBase
{
    public InspectEnemyTool(AiBotRuntime runtime) : base(runtime)
    {
    }

    public override string Name => "inspect_enemy";

    public override string Description => "查看当前敌人、战斗态势与威胁摘要。";

    public override Task<string> QueryAsync(string? parameters, CancellationToken cancellationToken)
    {
        var analysis = Runtime.GetCurrentAnalysis();
        var builder = new StringBuilder();
        if (!string.IsNullOrWhiteSpace(analysis.CombatSummary))
        {
            builder.AppendLine("战斗摘要：");
            builder.AppendLine(analysis.CombatSummary);
        }
        if (!string.IsNullOrWhiteSpace(analysis.EnemySummary))
        {
            builder.AppendLine();
            builder.AppendLine("敌人摘要：");
            builder.AppendLine(analysis.EnemySummary);
        }
        return Task.FromResult(builder.ToString().Trim());
    }
}
