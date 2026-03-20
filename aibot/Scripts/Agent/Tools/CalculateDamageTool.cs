using System.Text;
using aibot.Scripts.Core;

namespace aibot.Scripts.Agent.Tools;

public sealed class CalculateDamageTool : RuntimeBackedToolBase
{
    public CalculateDamageTool(AiBotRuntime runtime) : base(runtime)
    {
    }

    public override string Name => "calculate_damage";

    public override string Description => "基于当前局势输出已有的战斗、威胁和机制摘要，作为伤害/防御估算入口。";

    public override Task<string> QueryAsync(string? parameters, CancellationToken cancellationToken)
    {
        var analysis = Runtime.GetCurrentAnalysis();
        var builder = new StringBuilder();
        builder.AppendLine("当前版本提供的是基于现有分析器的战斗估算摘要，完整数值公式将在知识库补全后继续增强。\n");
        if (!string.IsNullOrWhiteSpace(analysis.CombatSummary))
        {
            builder.AppendLine("战斗摘要：");
            builder.AppendLine(analysis.CombatSummary);
        }
        if (!string.IsNullOrWhiteSpace(analysis.CharacterCombatMechanicSummary))
        {
            builder.AppendLine();
            builder.AppendLine("角色战斗机制：");
            builder.AppendLine(analysis.CharacterCombatMechanicSummary);
        }
        if (!string.IsNullOrWhiteSpace(analysis.StrategicNeedsSummary))
        {
            builder.AppendLine();
            builder.AppendLine("策略需求：");
            builder.AppendLine(analysis.StrategicNeedsSummary);
        }
        return Task.FromResult(builder.ToString().Trim());
    }
}
