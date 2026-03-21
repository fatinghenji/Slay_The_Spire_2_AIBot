using Godot;
using MegaCrit.Sts2.Core.AutoSlay.Helpers;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.Events;
using MegaCrit.Sts2.Core.Nodes.Events.Custom.CrystalSphere;
using MegaCrit.Sts2.Core.Nodes.Relics;
using MegaCrit.Sts2.Core.Nodes.Rewards;
using MegaCrit.Sts2.Core.Nodes.RestSite;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Screens;
using MegaCrit.Sts2.Core.Nodes.Screens.CardSelection;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;
using MegaCrit.Sts2.Core.Nodes.Screens.Overlays;
using MegaCrit.Sts2.Core.Nodes.Screens.Shops;
using MegaCrit.Sts2.Core.Nodes.Screens.TreasureRoomRelic;
using MegaCrit.Sts2.Core.Runs;
using aibot.Scripts.Core;
using aibot.Scripts.Decision;
using aibot.Scripts.Knowledge;
using aibot.Scripts.Localization;

namespace aibot.Scripts.Agent;

public static class CurrentDecisionAdvisor
{
    private static readonly string[] DecisionMarkers =
    {
        "选",
        "选择",
        "拿",
        "挑",
        "决定",
        "奖励",
        "路线",
        "地图",
        "遗物",
        "商店",
        "篝火",
        "事件",
        "bundle",
        "relic",
        "reward",
        "route",
        "path",
        "map",
        "shop",
        "rest",
        "event",
        "choose",
        "pick",
        "take",
        "which",
        "what should i choose"
    };

    public static bool LooksLikeDecisionRequest(string input)
    {
        var normalized = Normalize(input);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return false;
        }

        return DecisionMarkers.Any(marker => normalized.Contains(Normalize(marker), StringComparison.OrdinalIgnoreCase));
    }

    public static async Task<string?> TryExecuteCurrentDecisionAsync(AiBotRuntime runtime, CancellationToken cancellationToken)
    {
        var registry = AgentCore.Instance.Registry;

        if (NOverlayStack.Instance?.Peek() is NCardRewardSelectionScreen)
        {
            return await ExecuteSkillAsync(registry, "pick_card_reward", cancellationToken);
        }

        if (NOverlayStack.Instance?.Peek() is NRewardsScreen)
        {
            return await ExecuteSkillAsync(registry, "claim_reward", cancellationToken);
        }

        if (NOverlayStack.Instance?.Peek() is NChooseABundleSelectionScreen)
        {
            return await ExecuteSkillAsync(registry, "choose_bundle", cancellationToken);
        }

        if (NOverlayStack.Instance?.Peek() is NChooseARelicSelection || HasTreasureRelicChoice())
        {
            return await ExecuteSkillAsync(registry, "choose_relic", cancellationToken);
        }

        if (NOverlayStack.Instance?.Peek() is NCrystalSphereScreen)
        {
            return await ExecuteSkillAsync(registry, "crystal_sphere", cancellationToken);
        }

        if (NMapScreen.Instance is { IsTraveling: false } && NMapScreen.Instance.IsVisibleInTree())
        {
            return await ExecuteSkillAsync(registry, "navigate_map", cancellationToken);
        }

        if (GetAbsoluteNodeOrNull<NMerchantRoom>("/root/Game/RootSceneContainer/Run/RoomContainer/MerchantRoom") is not null)
        {
            return await ExecuteSkillAsync(registry, "purchase_shop", cancellationToken);
        }

        if (GetAbsoluteNodeOrNull<NRestSiteRoom>("/root/Game/RootSceneContainer/Run/RoomContainer/RestSiteRoom") is not null)
        {
            return await ExecuteSkillAsync(registry, "rest_site", cancellationToken);
        }

        if (GetAbsoluteNodeOrNull<NEventRoom>("/root/Game/RootSceneContainer/Run/RoomContainer/EventRoom") is { Visible: true })
        {
            return await ExecuteSkillAsync(registry, "choose_event_option", cancellationToken);
        }

        return null;
    }

    public static async Task<string?> TryRecommendCurrentDecisionAsync(AiBotRuntime runtime, CancellationToken cancellationToken)
    {
        if (runtime.DecisionEngine is null)
        {
            return null;
        }

        if (NOverlayStack.Instance?.Peek() is NCardRewardSelectionScreen rewardScreen)
        {
            var holders = UiHelper.FindAll<NCardHolder>(rewardScreen)
                .Where(holder => holder.CardModel is not null)
                .ToList();
            if (holders.Count == 0)
            {
                return null;
            }

            var options = holders.Select(holder => holder.CardModel!).ToList();
            var decision = await runtime.DecisionEngine.ChooseCardRewardAsync(options, runtime.GetCurrentAnalysis(), cancellationToken);
            var title = decision.Card?.Title ?? holders[0].CardModel?.Title ?? "reward card";
            return AiBotText.Pick(
                runtime.Config,
                $"建议选择奖励卡牌 {title}。\n理由：{GetReason(decision.Reason, decision.Trace)}",
                $"I recommend taking reward card {title}.\nReason: {GetReason(decision.Reason, decision.Trace)}");
        }

        if (NOverlayStack.Instance?.Peek() is NRewardsScreen rewardsScreen)
        {
            var runState = RunManager.Instance.DebugOnlyGetState();
            var player = LocalContext.GetMe(runState);
            var buttons = UiHelper.FindAll<NRewardButton>(rewardsScreen)
                .Where(button => button.IsEnabled && button.Visible && button.IsVisibleInTree())
                .ToList();
            if (buttons.Count == 0)
            {
                return null;
            }

            var decision = await runtime.DecisionEngine.ChooseRewardAsync(
                buttons,
                player?.HasOpenPotionSlots ?? false,
                runtime.GetCurrentAnalysis(),
                cancellationToken);
            var label = DescribeRewardButton(decision.Button ?? buttons[0]);
            return AiBotText.Pick(
                runtime.Config,
                $"建议领取 {label}。\n理由：{GetReason(decision.Reason, decision.Trace)}",
                $"I recommend taking {label}.\nReason: {GetReason(decision.Reason, decision.Trace)}");
        }

        if (NOverlayStack.Instance?.Peek() is NChooseABundleSelectionScreen bundleScreen)
        {
            var bundles = UiHelper.FindAll<NCardBundle>(bundleScreen)
                .Select((bundle, index) => new { Bundle = bundle, Index = index })
                .Where(entry => entry.Bundle.Bundle is { Count: > 0 })
                .ToList();
            if (bundles.Count == 0)
            {
                return null;
            }

            var context = new AiCardSelectionContext(AiCardSelectionKind.BundleChoice, "Choose one card bundle.", 1, 1, false, "bundle");
            var decision = await runtime.DecisionEngine.ChooseBundleAsync(
                context,
                bundles.Select(entry => new CardBundleOption(entry.Index, entry.Bundle.Bundle)).ToList(),
                runtime.GetCurrentAnalysis(),
                cancellationToken);
            return AiBotText.Pick(
                runtime.Config,
                $"建议选择第 {decision.SelectedIndex + 1} 个 bundle。\n理由：{GetReason(decision.Reason, decision.Trace)}",
                $"I recommend choosing bundle {decision.SelectedIndex + 1}.\nReason: {GetReason(decision.Reason, decision.Trace)}");
        }

        if (NOverlayStack.Instance?.Peek() is NChooseARelicSelection relicScreen)
        {
            var holders = UiHelper.FindAll<NRelicBasicHolder>(relicScreen)
                .Where(holder => holder.Relic?.Model is not null)
                .ToList();
            if (holders.Count == 0)
            {
                return null;
            }

            var decision = await runtime.DecisionEngine.ChooseRelicAsync(
                holders.Select(holder => holder.Relic!.Model).ToList(),
                nameof(NChooseARelicSelection),
                false,
                runtime.GetCurrentAnalysis(),
                cancellationToken);
            var title = decision.Relic?.Title.GetFormattedText() ?? holders[0].Relic!.Model.Title.GetFormattedText();
            return AiBotText.Pick(
                runtime.Config,
                $"建议选择遗物 {title}。\n理由：{GetReason(decision.Reason, decision.Trace)}",
                $"I recommend choosing relic {title}.\nReason: {GetReason(decision.Reason, decision.Trace)}");
        }

        var treasureRoom = GetAbsoluteNodeOrNull<NTreasureRoom>("/root/Game/RootSceneContainer/Run/RoomContainer/TreasureRoom");
        if (treasureRoom is not null)
        {
            var relicHolders = UiHelper.FindAll<NTreasureRoomRelicHolder>(treasureRoom)
                .Where(holder => holder.IsEnabled && holder.Visible && holder.Relic?.Model is not null)
                .ToList();
            if (relicHolders.Count > 0)
            {
                var decision = await runtime.DecisionEngine.ChooseRelicAsync(
                    relicHolders.Select(holder => holder.Relic!.Model).ToList(),
                    "Treasure Room",
                    false,
                    runtime.GetCurrentAnalysis(),
                    cancellationToken);
                var title = decision.Relic?.Title.GetFormattedText() ?? relicHolders[0].Relic!.Model.Title.GetFormattedText();
                return AiBotText.Pick(
                    runtime.Config,
                    $"建议选择宝箱遗物 {title}。\n理由：{GetReason(decision.Reason, decision.Trace)}",
                    $"I recommend choosing treasure-room relic {title}.\nReason: {GetReason(decision.Reason, decision.Trace)}");
            }
        }

        if (NMapScreen.Instance is { IsTraveling: false } && NMapScreen.Instance.IsVisibleInTree())
        {
            var mapRecommendation = await TryBuildMapRecommendationAsync(runtime, cancellationToken);
            if (!string.IsNullOrWhiteSpace(mapRecommendation))
            {
                return mapRecommendation;
            }
        }

        var merchantRoom = GetAbsoluteNodeOrNull<NMerchantRoom>("/root/Game/RootSceneContainer/Run/RoomContainer/MerchantRoom");
        if (merchantRoom?.Inventory?.Inventory is not null)
        {
            var player = merchantRoom.Inventory.Inventory.Player;
            var options = merchantRoom.Inventory.Inventory.AllEntries
                .Where(entry => entry.IsStocked && entry.EnoughGold)
                .Where(entry => player.HasOpenPotionSlots || entry is not MerchantPotionEntry)
                .ToList();
            if (options.Count > 0)
            {
                var decision = await runtime.DecisionEngine.ChooseShopPurchaseAsync(
                    options,
                    player.Gold,
                    player.HasOpenPotionSlots,
                    runtime.GetCurrentAnalysis(),
                    cancellationToken);
                if (decision.Entry is not null)
                {
                    return AiBotText.Pick(
                        runtime.Config,
                        $"建议购买 {DescribeShopEntry(decision.Entry)}。\n理由：{GetReason(decision.Reason, decision.Trace)}",
                        $"I recommend buying {DescribeShopEntry(decision.Entry)}.\nReason: {GetReason(decision.Reason, decision.Trace)}");
                }
            }
        }

        var restRoom = GetAbsoluteNodeOrNull<NRestSiteRoom>("/root/Game/RootSceneContainer/Run/RoomContainer/RestSiteRoom");
        if (restRoom is not null)
        {
            var runState = RunManager.Instance.DebugOnlyGetState();
            var player = LocalContext.GetMe(runState);
            var buttons = UiHelper.FindAll<NRestSiteButton>(restRoom)
                .Where(button => button.IsEnabled && button.Visible)
                .ToList();
            if (player is not null && buttons.Count > 0)
            {
                var decision = await runtime.DecisionEngine.ChooseRestSiteOptionAsync(
                    player,
                    buttons.Select(button => button.Option).ToList(),
                    runtime.GetCurrentAnalysis(),
                    cancellationToken);
                var option = decision.Option ?? buttons[0].Option;
                return AiBotText.Pick(
                    runtime.Config,
                    $"建议选择 {option.Title.GetFormattedText()}。\n理由：{GetReason(decision.Reason, decision.Trace)}",
                    $"I recommend choosing {option.Title.GetFormattedText()}.\nReason: {GetReason(decision.Reason, decision.Trace)}");
            }
        }

        var eventRoom = GetAbsoluteNodeOrNull<NEventRoom>("/root/Game/RootSceneContainer/Run/RoomContainer/EventRoom");
        if (eventRoom is not null && eventRoom.Visible && eventRoom.IsVisibleInTree())
        {
            var buttons = UiHelper.FindAll<NEventOptionButton>(eventRoom)
                .Where(button => button.IsEnabled && !button.Option.IsLocked)
                .ToList();
            if (buttons.Count > 0)
            {
                var decision = await runtime.DecisionEngine.ChooseEventOptionAsync(
                    buttons[0].Event,
                    buttons.Select(button => button.Option).ToList(),
                    runtime.GetCurrentAnalysis(),
                    cancellationToken);
                var option = decision.Option ?? buttons[0].Option;
                return AiBotText.Pick(
                    runtime.Config,
                    $"建议选择事件选项 {option.Title.GetFormattedText()}。\n理由：{GetReason(decision.Reason, decision.Trace)}",
                    $"I recommend choosing event option {option.Title.GetFormattedText()}.\nReason: {GetReason(decision.Reason, decision.Trace)}");
            }
        }

        return null;
    }

    private static async Task<string?> ExecuteSkillAsync(AgentSkillRegistry registry, string skillName, CancellationToken cancellationToken)
    {
        var skill = registry.FindSkillByName(skillName);
        if (skill is null)
        {
            return null;
        }

        var result = await skill.ExecuteAsync(null, cancellationToken);
        return string.IsNullOrWhiteSpace(result.Details)
            ? result.Summary
            : result.Summary + "\n" + result.Details;
    }

    private static async Task<string?> TryBuildMapRecommendationAsync(AiBotRuntime runtime, CancellationToken cancellationToken)
    {
        var runState = RunManager.Instance.DebugOnlyGetState();
        var player = LocalContext.GetMe(runState);
        if (runState is null || player is null || NMapScreen.Instance is null)
        {
            return null;
        }

        var allMapPoints = UiHelper.FindAll<NMapPoint>(NMapScreen.Instance).ToList();
        if (allMapPoints.Count == 0)
        {
            return null;
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
            return null;
        }

        var decision = await runtime.DecisionEngine!.ChooseMapPointAsync(
            candidateNodes.Select(node => node.Point).ToList(),
            player.Creature.CurrentHp,
            player.Creature.MaxHp,
            player.Gold,
            runtime.GetCurrentAnalysis(),
            cancellationToken);
        var selected = decision.Point ?? candidateNodes[0].Point;
        return AiBotText.Pick(
            runtime.Config,
            $"建议走到地图节点 ({selected.coord.row}, {selected.coord.col})，类型是 {selected.PointType}。\n理由：{GetReason(decision.Reason, decision.Trace)}",
            $"I recommend going to map node ({selected.coord.row}, {selected.coord.col}), type {selected.PointType}.\nReason: {GetReason(decision.Reason, decision.Trace)}");
    }

    private static List<NMapPoint> ResolveCandidateMapNodes(
        RunState runState,
        List<NMapPoint> allMapPoints,
        Dictionary<MapCoord, NMapPoint> pointLookup)
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

    private static bool HasTreasureRelicChoice()
    {
        return GetAbsoluteNodeOrNull<NTreasureRoom>("/root/Game/RootSceneContainer/Run/RoomContainer/TreasureRoom") is not null;
    }

    private static string GetReason(string reason, DecisionTrace? trace)
    {
        return string.IsNullOrWhiteSpace(trace?.Details) ? reason : trace.Details;
    }

    private static string DescribeRewardButton(NRewardButton button)
    {
        return button.Reward?.GetType().Name ?? "reward";
    }

    private static string DescribeShopEntry(MerchantEntry entry)
    {
        return entry switch
        {
            MerchantCardEntry cardEntry when cardEntry.CreationResult?.Card is not null => cardEntry.CreationResult.Card.Title,
            MerchantRelicEntry relicEntry when relicEntry.Model is not null => relicEntry.Model.Title.GetFormattedText(),
            MerchantPotionEntry potionEntry when potionEntry.Model is not null => potionEntry.Model.Title.GetFormattedText(),
            MerchantCardRemovalEntry => "移除卡牌服务",
            _ => entry.GetType().Name
        };
    }

    private static T? GetAbsoluteNodeOrNull<T>(string path) where T : class
    {
        var root = ((SceneTree)Engine.GetMainLoop()).Root;
        return root.GetNodeOrNull(path) as T;
    }

    private static string Normalize(string? value)
    {
        return GuideKnowledgeBase.Normalize(value ?? string.Empty);
    }
}
