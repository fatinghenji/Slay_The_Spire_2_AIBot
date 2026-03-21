using MegaCrit.Sts2.Core.AutoSlay.Helpers;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;
using MegaCrit.Sts2.Core.Runs;
using aibot.Scripts.Core;

namespace aibot.Scripts.Agent.Skills;

public sealed class NavigateMapSkill : RuntimeBackedSkillBase
{
    public NavigateMapSkill(AiBotRuntime runtime) : base(runtime)
    {
    }

    public override string Name => "navigate_map";

    public override string Description => "在地图界面选择一个可到达节点。";

    public override SkillCategory Category => SkillCategory.Navigation;

    public override bool CanExecute()
    {
        return NMapScreen.Instance is { IsTraveling: false } && NMapScreen.Instance.IsVisibleInTree();
    }

    public override async Task<SkillExecutionResult> ExecuteAsync(AgentSkillParameters? parameters, CancellationToken cancellationToken)
    {
        if (NMapScreen.Instance is null || NMapScreen.Instance.IsTraveling || !NMapScreen.Instance.IsVisibleInTree())
        {
            return new SkillExecutionResult(false, "当前不在可选路线的地图界面。");
        }

        var runState = RunManager.Instance.DebugOnlyGetState();
        var player = LocalContext.GetMe(runState);
        if (runState is null || player is null)
        {
            return new SkillExecutionResult(false, "当前无法读取地图状态。");
        }

        var allMapPoints = UiHelper.FindAll<NMapPoint>(NMapScreen.Instance).ToList();
        if (allMapPoints.Count == 0)
        {
            return new SkillExecutionResult(false, "当前没有可到达的地图节点。");
        }

        var pointLookup = allMapPoints.ToDictionary(point => point.Point.coord, point => point);
        var candidateNodes = ResolveCandidateMapNodes(runState, allMapPoints, pointLookup)
            .Where(node => node.State == MapPointState.Travelable)
            .OrderBy(node => node.Point.coord.col)
            .ToList();

        if (candidateNodes.Count == 0)
        {
            candidateNodes = allMapPoints
                .Where(node => node.State == MapPointState.Travelable)
                .OrderBy(node => node.Point.coord.row)
                .ThenBy(node => node.Point.coord.col)
                .ToList();
        }

        if (candidateNodes.Count == 0)
        {
            return new SkillExecutionResult(false, "当前没有可到达的地图节点。");
        }

        NMapPoint? selected = null;
        if (parameters?.MapRow is not null && parameters.MapCol is not null)
        {
            selected = candidateNodes.FirstOrDefault(node => node.Point.coord.row == parameters.MapRow.Value && node.Point.coord.col == parameters.MapCol.Value);
        }

        selected ??= parameters?.MapCol is not null
            ? candidateNodes.FirstOrDefault(node => node.Point.coord.col == parameters.MapCol.Value)
            : null;

        if (selected is null && Runtime.DecisionEngine is not null)
        {
            var decision = await Runtime.DecisionEngine.ChooseMapPointAsync(
                candidateNodes.Select(node => node.Point).ToList(),
                player.Creature.CurrentHp,
                player.Creature.MaxHp,
                player.Gold,
                Runtime.GetCurrentAnalysis(),
                cancellationToken);

            selected = decision.Point is not null && pointLookup.TryGetValue(decision.Point.coord, out var mapped)
                ? mapped
                : null;
        }

        selected ??= candidateNodes[0];

        var roomEnteredTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        void OnRoomEntered() => roomEnteredTcs.TrySetResult();
        RunManager.Instance.RoomEntered += OnRoomEntered;
        try
        {
            await UiHelper.Click(selected);
            try
            {
                await roomEnteredTcs.Task.WaitAsync(TimeSpan.FromSeconds(10), cancellationToken);
            }
            catch (TimeoutException)
            {
            }
        }
        finally
        {
            RunManager.Instance.RoomEntered -= OnRoomEntered;
        }

        return new SkillExecutionResult(true, $"已选择地图节点：({selected.Point.coord.row}, {selected.Point.coord.col})");
    }

    private static List<NMapPoint> ResolveCandidateMapNodes(RunState runState, List<NMapPoint> allMapPoints, Dictionary<MapCoord, NMapPoint> pointLookup)
    {
        if (runState.VisitedMapCoords.Count == 0)
        {
            return allMapPoints
                .Where(point => point.Point.coord.row == 0)
                .OrderBy(point => point.Point.coord.col)
                .ToList();
        }

        var lastCoord = runState.VisitedMapCoords[^1];
        var lastNode = allMapPoints.FirstOrDefault(point => point.Point.coord.Equals(lastCoord));
        if (lastNode is null)
        {
            return new List<NMapPoint>();
        }

        return lastNode.Point.Children
            .Select(child => pointLookup.TryGetValue(child.coord, out var childNode) ? childNode : null)
            .Where(childNode => childNode is not null)
            .Cast<NMapPoint>()
            .OrderBy(point => point.Point.coord.col)
            .ToList();
    }
}
