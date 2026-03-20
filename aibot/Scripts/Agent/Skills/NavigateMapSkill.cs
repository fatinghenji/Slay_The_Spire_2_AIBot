using MegaCrit.Sts2.Core.AutoSlay.Helpers;
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
            return new SkillExecutionResult(false, "当前不在可选路径的地图界面。 ");
        }

        var candidates = UiHelper.FindAll<NMapPoint>(NMapScreen.Instance)
            .Where(node => node.State == MapPointState.Travelable)
            .OrderBy(node => node.Point.coord.row)
            .ThenBy(node => node.Point.coord.col)
            .ToList();
        if (candidates.Count == 0)
        {
            return new SkillExecutionResult(false, "当前没有可到达的地图节点。 ");
        }

        var selected = parameters?.MapCol is not null
            ? candidates.FirstOrDefault(node => node.Point.coord.col == parameters.MapCol.Value)
            : null;
        selected ??= candidates[0];

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
}