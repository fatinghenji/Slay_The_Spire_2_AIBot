using Godot;
using MegaCrit.Sts2.Core.AutoSlay.Helpers;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.Relics;
using MegaCrit.Sts2.Core.Nodes.Screens;
using MegaCrit.Sts2.Core.Nodes.Screens.CardSelection;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;
using MegaCrit.Sts2.Core.Nodes.Screens.Overlays;
using MegaCrit.Sts2.Core.Runs;
using aibot.Scripts.Agent;
using aibot.Scripts.Core;
using aibot.Scripts.Decision;

namespace aibot.Scripts.Ui;

public sealed partial class AgentRecommendOverlay : CanvasLayer
{
    private static AgentRecommendOverlay? _instance;

    private readonly List<RecommendationBadge> _badges = new();
    private AiBotRuntime? _runtime;
    private string _lastSignature = string.Empty;
    private bool _refreshInFlight;
    private double _lastRefreshTime;

    public AgentRecommendOverlay()
    {
        Layer = 215;
        ProcessMode = ProcessModeEnum.Always;
        Visible = false;
    }

    public static void EnsureCreated(AiBotRuntime runtime)
    {
        if (_instance is not null && GodotObject.IsInstanceValid(_instance) && _instance.GetParent() is not null)
        {
            _instance._runtime = runtime;
            return;
        }

        var root = ((SceneTree)Engine.GetMainLoop()).Root;
        _instance = new AgentRecommendOverlay
        {
            Name = "AgentRecommendOverlay",
            _runtime = runtime
        };
        root.AddChild(_instance);
    }

    public static void ShowOverlay()
    {
        if (_instance is null)
        {
            return;
        }

        if (_instance._runtime?.Config.Ui.ShowRecommendOverlay == false)
        {
            _instance.Visible = false;
            return;
        }

        _instance.Visible = true;
    }

    public static void HideOverlay()
    {
        if (_instance is null)
        {
            return;
        }

        _instance.Visible = false;
        _instance.ClearBadges();
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        UpdateBadgePositions();

        if (!Visible || _runtime is null)
        {
            return;
        }

        if (AgentCore.Instance.CurrentMode != AgentMode.Assist)
        {
            if (_badges.Count > 0)
            {
                ClearBadges();
            }

            return;
        }

        if (_refreshInFlight || Time.GetTicksMsec() - _lastRefreshTime < 650)
        {
            return;
        }

        var signature = BuildSignature();
        if (signature == _lastSignature)
        {
            return;
        }

        _lastSignature = signature;
        _lastRefreshTime = Time.GetTicksMsec();
        _refreshInFlight = true;
        TaskHelper.RunSafely(RefreshAsync());
    }

    private async Task RefreshAsync()
    {
        try
        {
            if (_runtime?.DecisionEngine is null)
            {
                ClearBadges();
                return;
            }

            var added = await TryRefreshOverlayRecommendationAsync()
                || await TryRefreshMapRecommendationAsync();

            if (!added)
            {
                ClearBadges();
            }
        }
        catch (Exception ex)
        {
            Log.Warn($"[AiBot.Agent] Recommend overlay refresh failed: {ex.Message}");
            ClearBadges();
        }
        finally
        {
            _refreshInFlight = false;
        }
    }

    private async Task<bool> TryRefreshOverlayRecommendationAsync()
    {
        var overlay = NOverlayStack.Instance?.Peek();
        switch (overlay)
        {
            case NCardRewardSelectionScreen rewardScreen:
                return await RefreshCardRewardRecommendationAsync(rewardScreen);
            case NChooseARelicSelection relicScreen:
                return await RefreshRelicRecommendationAsync(relicScreen);
            default:
                return false;
        }
    }

    private async Task<bool> RefreshCardRewardRecommendationAsync(NCardRewardSelectionScreen screen)
    {
        if (_runtime?.DecisionEngine is null)
        {
            return false;
        }

        var holders = UiHelper.FindAll<NCardHolder>(screen)
            .Where(holder => holder.CardModel is not null && holder.IsVisibleInTree())
            .ToList();
        if (holders.Count == 0)
        {
            return false;
        }

        var analysis = _runtime.GetCurrentAnalysis();
        var options = holders.Select(holder => holder.CardModel).Where(card => card is not null).Cast<CardModel>().ToList();
        var decision = await _runtime.DecisionEngine.ChooseCardRewardAsync(options, analysis, CancellationToken.None);
        var target = decision.Card is not null
            ? holders.FirstOrDefault(holder => holder.CardModel == decision.Card || holder.CardModel?.Id == decision.Card.Id)
            : holders.FirstOrDefault();
        if (target is null)
        {
            return false;
        }

        ReplaceWithSingleBadge(target, decision.Reason, "卡牌奖励推荐");
        return true;
    }

    private async Task<bool> RefreshRelicRecommendationAsync(NChooseARelicSelection screen)
    {
        if (_runtime?.DecisionEngine is null)
        {
            return false;
        }

        var holders = UiHelper.FindAll<NRelicBasicHolder>(screen)
            .Where(holder => holder.Relic?.Model is not null && holder.IsVisibleInTree())
            .ToList();
        if (holders.Count == 0)
        {
            return false;
        }

        var analysis = _runtime.GetCurrentAnalysis();
        var options = holders.Select(holder => holder.Relic.Model).ToList();
        var decision = await _runtime.DecisionEngine.ChooseRelicAsync(options, nameof(NChooseARelicSelection), false, analysis, CancellationToken.None);
        var target = decision.Relic is not null
            ? holders.FirstOrDefault(holder => holder.Relic.Model == decision.Relic || holder.Relic.Model.Id == decision.Relic.Id)
            : holders.FirstOrDefault();
        if (target is null)
        {
            return false;
        }

        ReplaceWithSingleBadge(target, decision.Reason, "遗物推荐");
        return true;
    }

    private async Task<bool> TryRefreshMapRecommendationAsync()
    {
        if (_runtime?.DecisionEngine is null || NMapScreen.Instance is null || !NMapScreen.Instance.IsVisibleInTree() || NMapScreen.Instance.IsTraveling)
        {
            return false;
        }

        var runState = RunManager.Instance.DebugOnlyGetState();
        if (runState is null)
        {
            return false;
        }

        var player = LocalContext.GetMe(runState);
        if (player is null)
        {
            return false;
        }

        var allMapPoints = UiHelper.FindAll<NMapPoint>(NMapScreen.Instance).ToList();
        if (allMapPoints.Count == 0)
        {
            return false;
        }

        var pointLookup = allMapPoints.ToDictionary(point => point.Point.coord, point => point);
        var candidateNodes = ResolveCandidateMapNodes(runState, allMapPoints, pointLookup)
            .Where(node => node.State == MapPointState.Travelable && node.IsVisibleInTree())
            .OrderBy(node => node.Point.coord.col)
            .ToList();

        if (candidateNodes.Count == 0)
        {
            candidateNodes = allMapPoints
                .Where(node => node.State == MapPointState.Travelable && node.IsVisibleInTree())
                .OrderBy(node => node.Point.coord.row)
                .ThenBy(node => node.Point.coord.col)
                .ToList();
        }

        if (candidateNodes.Count == 0)
        {
            return false;
        }

        var analysis = _runtime.GetCurrentAnalysis();
        var decision = await _runtime.DecisionEngine.ChooseMapPointAsync(
            candidateNodes.Select(node => node.Point).ToList(),
            player.Creature.CurrentHp,
            player.Creature.MaxHp,
            player.Gold,
            analysis,
            CancellationToken.None);

        var target = decision.Point is not null && pointLookup.TryGetValue(decision.Point.coord, out var nodeTarget)
            ? nodeTarget
            : candidateNodes[0];

        ReplaceWithSingleBadge(target, decision.Reason, "路线推荐");
        return true;
    }

    private void ReplaceWithSingleBadge(Control target, string reason, string category)
    {
        ClearBadges();

        var badge = new RecommendationBadge(target, reason, category);
        _badges.Add(badge);
        AddChild(badge.Container);
        badge.UpdatePosition();
    }

    private void ClearBadges()
    {
        foreach (var badge in _badges)
        {
            if (GodotObject.IsInstanceValid(badge.Container))
            {
                badge.Container.QueueFree();
            }
        }

        _badges.Clear();
    }

    private void UpdateBadgePositions()
    {
        for (var index = _badges.Count - 1; index >= 0; index--)
        {
            var badge = _badges[index];
            if (!badge.IsValid())
            {
                if (GodotObject.IsInstanceValid(badge.Container))
                {
                    badge.Container.QueueFree();
                }

                _badges.RemoveAt(index);
                continue;
            }

            badge.UpdatePosition();
        }
    }

    private string BuildSignature()
    {
        var overlay = NOverlayStack.Instance?.Peek();
        if (overlay is NCardRewardSelectionScreen rewardScreen)
        {
            var names = UiHelper.FindAll<NCardHolder>(rewardScreen)
                .Where(holder => holder.CardModel is not null)
                .Select(holder => holder.CardModel!.Title.ToString())
                .ToList();
            return "card-reward:" + string.Join("|", names);
        }

        if (overlay is NChooseARelicSelection relicScreen)
        {
            var names = UiHelper.FindAll<NRelicBasicHolder>(relicScreen)
                .Where(holder => holder.Relic?.Model is not null)
                .Select(holder => holder.Relic.Model.Title.GetFormattedText())
                .ToList();
            return "relic:" + string.Join("|", names);
        }

        if (NMapScreen.Instance is not null && NMapScreen.Instance.IsVisibleInTree() && !NMapScreen.Instance.IsTraveling)
        {
            var coords = UiHelper.FindAll<NMapPoint>(NMapScreen.Instance)
                .Where(node => node.State == MapPointState.Travelable)
                .Select(node => $"{node.Point.coord.row}:{node.Point.coord.col}:{node.Point.PointType}")
                .ToList();
            return "map:" + string.Join("|", coords);
        }

        return string.Empty;
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

    private sealed class RecommendationBadge
    {
        private readonly WeakReference<Control> _targetRef;

        public RecommendationBadge(Control target, string reason, string category)
        {
            _targetRef = new WeakReference<Control>(target);
            Container = new PanelContainer
            {
                MouseFilter = Control.MouseFilterEnum.Ignore,
                TooltipText = $"{category}\n{reason}",
                Modulate = new Color(1f, 0.95f, 0.55f)
            };

            var margin = new MarginContainer();
            margin.AddThemeConstantOverride("margin_left", 6);
            margin.AddThemeConstantOverride("margin_right", 6);
            margin.AddThemeConstantOverride("margin_top", 2);
            margin.AddThemeConstantOverride("margin_bottom", 2);
            Container.AddChild(margin);

            var label = new Label
            {
                Text = "推荐",
                MouseFilter = Control.MouseFilterEnum.Ignore
            };
            margin.AddChild(label);
        }

        public PanelContainer Container { get; }

        public bool IsValid()
        {
            if (!_targetRef.TryGetTarget(out var target) || !GodotObject.IsInstanceValid(target))
            {
                return false;
            }

            return target.IsVisibleInTree();
        }

        public void UpdatePosition()
        {
            if (!_targetRef.TryGetTarget(out var target) || !GodotObject.IsInstanceValid(target))
            {
                return;
            }

            var x = target.GlobalPosition.X + Mathf.Max(0f, target.Size.X - 52f);
            var y = target.GlobalPosition.Y - 18f;
            Container.Position = new Vector2(x, y);
        }
    }
}