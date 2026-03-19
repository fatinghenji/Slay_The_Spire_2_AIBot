using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.RestSite;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Events.Custom.CrystalSphereEvent;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Rewards;
using aibot.Scripts.Config;
using aibot.Scripts.Ui;

namespace aibot.Scripts.Decision;

public sealed class HybridDecisionEngine : IAiDecisionEngine, IDisposable
{
    private readonly AiBotConfig _config;
    private readonly IAiDecisionEngine _heuristic;
    private readonly DeepSeekDecisionEngine? _cloud;

    public HybridDecisionEngine(AiBotConfig config, IAiDecisionEngine heuristic, DeepSeekDecisionEngine? cloud)
    {
        _config = config;
        _heuristic = heuristic;
        _cloud = cloud;
    }

    public async Task<PotionDecision> ChoosePotionUseAsync(Player player, IReadOnlyList<PotionModel> usablePotions, IReadOnlyList<CardModel> playableCards, IReadOnlyList<Creature> enemies, RunAnalysis analysis, CancellationToken cancellationToken)
    {
        if (_config.CanUseCloud && _cloud is not null)
        {
            try
            {
                return Publish(await _cloud.ChoosePotionUseAsync(player, usablePotions, playableCards, enemies, analysis, cancellationToken));
            }
            catch (Exception ex)
            {
                Log.Warn($"[AiBot] Cloud decision failed. Falling back to heuristics. {ex.Message}");
                return Publish(AnnotateFallback(await _heuristic.ChoosePotionUseAsync(player, usablePotions, playableCards, enemies, analysis, cancellationToken), ex.Message));
            }
        }

        return Publish(await _heuristic.ChoosePotionUseAsync(player, usablePotions, playableCards, enemies, analysis, cancellationToken));
    }

    public async Task<CombatDecision> ChooseCombatActionAsync(Player player, IReadOnlyList<CardModel> playableCards, IReadOnlyList<Creature> enemies, RunAnalysis analysis, CancellationToken cancellationToken)
    {
        if (_config.CanUseCloud && _cloud is not null)
        {
            try
            {
                return Publish(await _cloud.ChooseCombatActionAsync(player, playableCards, enemies, analysis, cancellationToken));
            }
            catch (Exception ex)
            {
                Log.Warn($"[AiBot] Cloud decision failed. Falling back to heuristics. {ex.Message}");
                return Publish(AnnotateFallback(await _heuristic.ChooseCombatActionAsync(player, playableCards, enemies, analysis, cancellationToken), ex.Message));
            }
        }

        return Publish(await _heuristic.ChooseCombatActionAsync(player, playableCards, enemies, analysis, cancellationToken));
    }

    public async Task<CardRewardDecision> ChooseCardRewardAsync(IReadOnlyList<CardModel> options, RunAnalysis analysis, CancellationToken cancellationToken)
    {
        if (_config.CanUseCloud && _cloud is not null)
        {
            try
            {
                return Publish(await _cloud.ChooseCardRewardAsync(options, analysis, cancellationToken));
            }
            catch (Exception ex)
            {
                Log.Warn($"[AiBot] Cloud decision failed. Falling back to heuristics. {ex.Message}");
                return Publish(AnnotateFallback(await _heuristic.ChooseCardRewardAsync(options, analysis, cancellationToken), ex.Message));
            }
        }

        return Publish(await _heuristic.ChooseCardRewardAsync(options, analysis, cancellationToken));
    }

    public async Task<CardSelectionDecision> ChooseCardSelectionAsync(AiCardSelectionContext request, IReadOnlyList<CardModel> options, RunAnalysis analysis, CancellationToken cancellationToken)
    {
        if (_config.CanUseCloud && _cloud is not null)
        {
            try
            {
                return Publish(await _cloud.ChooseCardSelectionAsync(request, options, analysis, cancellationToken));
            }
            catch (Exception ex)
            {
                Log.Warn($"[AiBot] Cloud decision failed. Falling back to heuristics. {ex.Message}");
                return Publish(AnnotateFallback(await _heuristic.ChooseCardSelectionAsync(request, options, analysis, cancellationToken), ex.Message));
            }
        }

        return Publish(await _heuristic.ChooseCardSelectionAsync(request, options, analysis, cancellationToken));
    }

    public async Task<CardRewardChoiceDecision> ChooseCardRewardChoiceAsync(AiCardSelectionContext request, IReadOnlyList<CardModel> options, IReadOnlyList<DecisionOption> alternatives, RunAnalysis analysis, CancellationToken cancellationToken)
    {
        if (_config.CanUseCloud && _cloud is not null)
        {
            try
            {
                return Publish(await _cloud.ChooseCardRewardChoiceAsync(request, options, alternatives, analysis, cancellationToken));
            }
            catch (Exception ex)
            {
                Log.Warn($"[AiBot] Cloud decision failed. Falling back to heuristics. {ex.Message}");
                return Publish(AnnotateFallback(await _heuristic.ChooseCardRewardChoiceAsync(request, options, alternatives, analysis, cancellationToken), ex.Message));
            }
        }

        return Publish(await _heuristic.ChooseCardRewardChoiceAsync(request, options, alternatives, analysis, cancellationToken));
    }

    public async Task<BundleChoiceDecision> ChooseBundleAsync(AiCardSelectionContext request, IReadOnlyList<CardBundleOption> bundles, RunAnalysis analysis, CancellationToken cancellationToken)
    {
        if (_config.CanUseCloud && _cloud is not null)
        {
            try
            {
                return Publish(await _cloud.ChooseBundleAsync(request, bundles, analysis, cancellationToken));
            }
            catch (Exception ex)
            {
                Log.Warn($"[AiBot] Cloud decision failed. Falling back to heuristics. {ex.Message}");
                return Publish(AnnotateFallback(await _heuristic.ChooseBundleAsync(request, bundles, analysis, cancellationToken), ex.Message));
            }
        }

        return Publish(await _heuristic.ChooseBundleAsync(request, bundles, analysis, cancellationToken));
    }

    public async Task<CrystalSphereActionDecision> ChooseCrystalSphereActionAsync(CrystalSphereMinigame minigame, RunAnalysis analysis, CancellationToken cancellationToken)
    {
        if (_config.CanUseCloud && _cloud is not null)
        {
            try
            {
                return Publish(await _cloud.ChooseCrystalSphereActionAsync(minigame, analysis, cancellationToken));
            }
            catch (Exception ex)
            {
                Log.Warn($"[AiBot] Cloud decision failed. Falling back to heuristics. {ex.Message}");
                return Publish(AnnotateFallback(await _heuristic.ChooseCrystalSphereActionAsync(minigame, analysis, cancellationToken), ex.Message));
            }
        }

        return Publish(await _heuristic.ChooseCrystalSphereActionAsync(minigame, analysis, cancellationToken));
    }

    public async Task<RewardDecision> ChooseRewardAsync(IReadOnlyList<NRewardButton> options, bool hasOpenPotionSlots, RunAnalysis analysis, CancellationToken cancellationToken)
    {
        if (_config.CanUseCloud && _cloud is not null)
        {
            try
            {
                return Publish(await _cloud.ChooseRewardAsync(options, hasOpenPotionSlots, analysis, cancellationToken));
            }
            catch (Exception ex)
            {
                Log.Warn($"[AiBot] Cloud decision failed. Falling back to heuristics. {ex.Message}");
                return Publish(AnnotateFallback(await _heuristic.ChooseRewardAsync(options, hasOpenPotionSlots, analysis, cancellationToken), ex.Message));
            }
        }

        return Publish(await _heuristic.ChooseRewardAsync(options, hasOpenPotionSlots, analysis, cancellationToken));
    }

    public async Task<ShopDecision> ChooseShopPurchaseAsync(IReadOnlyList<MerchantEntry> options, int currentGold, bool hasOpenPotionSlots, RunAnalysis analysis, CancellationToken cancellationToken)
    {
        if (_config.CanUseCloud && _cloud is not null)
        {
            try
            {
                return Publish(await _cloud.ChooseShopPurchaseAsync(options, currentGold, hasOpenPotionSlots, analysis, cancellationToken));
            }
            catch (Exception ex)
            {
                Log.Warn($"[AiBot] Cloud decision failed. Falling back to heuristics. {ex.Message}");
                return Publish(AnnotateFallback(await _heuristic.ChooseShopPurchaseAsync(options, currentGold, hasOpenPotionSlots, analysis, cancellationToken), ex.Message));
            }
        }

        return Publish(await _heuristic.ChooseShopPurchaseAsync(options, currentGold, hasOpenPotionSlots, analysis, cancellationToken));
    }

    public async Task<RestDecision> ChooseRestSiteOptionAsync(Player player, IReadOnlyList<RestSiteOption> options, RunAnalysis analysis, CancellationToken cancellationToken)
    {
        if (_config.CanUseCloud && _cloud is not null)
        {
            try
            {
                return Publish(await _cloud.ChooseRestSiteOptionAsync(player, options, analysis, cancellationToken));
            }
            catch (Exception ex)
            {
                Log.Warn($"[AiBot] Cloud decision failed. Falling back to heuristics. {ex.Message}");
                return Publish(AnnotateFallback(await _heuristic.ChooseRestSiteOptionAsync(player, options, analysis, cancellationToken), ex.Message));
            }
        }

        return Publish(await _heuristic.ChooseRestSiteOptionAsync(player, options, analysis, cancellationToken));
    }

    public async Task<MapDecision> ChooseMapPointAsync(IReadOnlyList<MapPoint> options, int currentHp, int maxHp, int gold, RunAnalysis analysis, CancellationToken cancellationToken)
    {
        if (_config.CanUseCloud && _cloud is not null)
        {
            try
            {
                return Publish(await _cloud.ChooseMapPointAsync(options, currentHp, maxHp, gold, analysis, cancellationToken));
            }
            catch (Exception ex)
            {
                Log.Warn($"[AiBot] Cloud decision failed. Falling back to heuristics. {ex.Message}");
                return Publish(AnnotateFallback(await _heuristic.ChooseMapPointAsync(options, currentHp, maxHp, gold, analysis, cancellationToken), ex.Message));
            }
        }

        return Publish(await _heuristic.ChooseMapPointAsync(options, currentHp, maxHp, gold, analysis, cancellationToken));
    }

    public async Task<EventDecision> ChooseEventOptionAsync(EventModel eventModel, IReadOnlyList<EventOption> options, RunAnalysis analysis, CancellationToken cancellationToken)
    {
        if (_config.CanUseCloud && _cloud is not null)
        {
            try
            {
                return Publish(await _cloud.ChooseEventOptionAsync(eventModel, options, analysis, cancellationToken));
            }
            catch (Exception ex)
            {
                Log.Warn($"[AiBot] Cloud decision failed. Falling back to heuristics. {ex.Message}");
                return Publish(AnnotateFallback(await _heuristic.ChooseEventOptionAsync(eventModel, options, analysis, cancellationToken), ex.Message));
            }
        }

        return Publish(await _heuristic.ChooseEventOptionAsync(eventModel, options, analysis, cancellationToken));
    }

    public async Task<RelicChoiceDecision> ChooseRelicAsync(IReadOnlyList<RelicModel> options, string source, bool allowSkip, RunAnalysis analysis, CancellationToken cancellationToken)
    {
        if (_config.CanUseCloud && _cloud is not null)
        {
            try
            {
                return Publish(await _cloud.ChooseRelicAsync(options, source, allowSkip, analysis, cancellationToken));
            }
            catch (Exception ex)
            {
                Log.Warn($"[AiBot] Cloud decision failed. Falling back to heuristics. {ex.Message}");
                return Publish(AnnotateFallback(await _heuristic.ChooseRelicAsync(options, source, allowSkip, analysis, cancellationToken), ex.Message));
            }
        }

        return Publish(await _heuristic.ChooseRelicAsync(options, source, allowSkip, analysis, cancellationToken));
    }

    private static CombatDecision AnnotateFallback(CombatDecision decision, string errorMessage)
    {
        return decision with
        {
            Trace = decision.Trace is null
                ? new DecisionTrace("Combat", "Local/Fallback", decision.Reason, $"Cloud decision failed and local heuristic was used instead. Error: {errorMessage}")
                : decision.Trace with { Source = "Local/Fallback", Details = $"{decision.Trace.Details} Cloud fallback reason: {errorMessage}" }
        };
    }

    private static PotionDecision AnnotateFallback(PotionDecision decision, string errorMessage)
    {
        return decision with
        {
            Trace = decision.Trace is null
                ? new DecisionTrace("Consumable", "Local/Fallback", decision.Reason, $"Cloud decision failed and local heuristic was used instead. Error: {errorMessage}")
                : decision.Trace with { Source = "Local/Fallback", Details = $"{decision.Trace.Details} Cloud fallback reason: {errorMessage}" }
        };
    }

    private static RewardDecision AnnotateFallback(RewardDecision decision, string errorMessage)
    {
        return decision with
        {
            Trace = decision.Trace is null
                ? new DecisionTrace("Rewards", "Local/Fallback", decision.Reason, $"Cloud decision failed and local heuristic was used instead. Error: {errorMessage}")
                : decision.Trace with { Source = "Local/Fallback", Details = $"{decision.Trace.Details} Cloud fallback reason: {errorMessage}" }
        };
    }

    private static CardRewardDecision AnnotateFallback(CardRewardDecision decision, string errorMessage)
    {
        return decision with
        {
            Trace = decision.Trace is null
                ? new DecisionTrace("Card Reward", "Local/Fallback", decision.Reason, $"Cloud decision failed and local heuristic was used instead. Error: {errorMessage}")
                : decision.Trace with { Source = "Local/Fallback", Details = $"{decision.Trace.Details} Cloud fallback reason: {errorMessage}" }
        };
    }

    private static CardSelectionDecision AnnotateFallback(CardSelectionDecision decision, string errorMessage)
    {
        return decision with
        {
            Trace = decision.Trace is null
                ? new DecisionTrace("Card Selection", "Local/Fallback", decision.Reason, $"Cloud decision failed and local heuristic was used instead. Error: {errorMessage}")
                : decision.Trace with { Source = "Local/Fallback", Details = $"{decision.Trace.Details} Cloud fallback reason: {errorMessage}" }
        };
    }

    private static CardRewardChoiceDecision AnnotateFallback(CardRewardChoiceDecision decision, string errorMessage)
    {
        return decision with
        {
            Trace = decision.Trace is null
                ? new DecisionTrace("Card Reward", "Local/Fallback", decision.Reason, $"Cloud decision failed and local heuristic was used instead. Error: {errorMessage}")
                : decision.Trace with { Source = "Local/Fallback", Details = $"{decision.Trace.Details} Cloud fallback reason: {errorMessage}" }
        };
    }

    private static BundleChoiceDecision AnnotateFallback(BundleChoiceDecision decision, string errorMessage)
    {
        return decision with
        {
            Trace = decision.Trace is null
                ? new DecisionTrace("Bundle Choice", "Local/Fallback", decision.Reason, $"Cloud decision failed and local heuristic was used instead. Error: {errorMessage}")
                : decision.Trace with { Source = "Local/Fallback", Details = $"{decision.Trace.Details} Cloud fallback reason: {errorMessage}" }
        };
    }

    private static CrystalSphereActionDecision AnnotateFallback(CrystalSphereActionDecision decision, string errorMessage)
    {
        return decision with
        {
            Trace = decision.Trace is null
                ? new DecisionTrace("Crystal Sphere", "Local/Fallback", decision.Reason, $"Cloud decision failed and local heuristic was used instead. Error: {errorMessage}")
                : decision.Trace with { Source = "Local/Fallback", Details = $"{decision.Trace.Details} Cloud fallback reason: {errorMessage}" }
        };
    }

    private static MapDecision AnnotateFallback(MapDecision decision, string errorMessage)
    {
        return decision with
        {
            Trace = decision.Trace is null
                ? new DecisionTrace("Map", "Local/Fallback", decision.Reason, $"Cloud decision failed and local heuristic was used instead. Error: {errorMessage}")
                : decision.Trace with { Source = "Local/Fallback", Details = $"{decision.Trace.Details} Cloud fallback reason: {errorMessage}" }
        };
    }

    private static ShopDecision AnnotateFallback(ShopDecision decision, string errorMessage)
    {
        return decision with
        {
            Trace = decision.Trace is null
                ? new DecisionTrace("Shop", "Local/Fallback", decision.Reason, $"Cloud decision failed and local heuristic was used instead. Error: {errorMessage}")
                : decision.Trace with { Source = "Local/Fallback", Details = $"{decision.Trace.Details} Cloud fallback reason: {errorMessage}" }
        };
    }

    private static EventDecision AnnotateFallback(EventDecision decision, string errorMessage)
    {
        return decision with
        {
            Trace = decision.Trace is null
                ? new DecisionTrace("Event", "Local/Fallback", decision.Reason, $"Cloud decision failed and local heuristic was used instead. Error: {errorMessage}")
                : decision.Trace with { Source = "Local/Fallback", Details = $"{decision.Trace.Details} Cloud fallback reason: {errorMessage}" }
        };
    }

    private static RelicChoiceDecision AnnotateFallback(RelicChoiceDecision decision, string errorMessage)
    {
        return decision with
        {
            Trace = decision.Trace is null
                ? new DecisionTrace("Relic Choice", "Local/Fallback", decision.Reason, $"Cloud decision failed and local heuristic was used instead. Error: {errorMessage}")
                : decision.Trace with { Source = "Local/Fallback", Details = $"{decision.Trace.Details} Cloud fallback reason: {errorMessage}" }
        };
    }

    private static RestDecision AnnotateFallback(RestDecision decision, string errorMessage)
    {
        return decision with
        {
            Trace = decision.Trace is null
                ? new DecisionTrace("Rest Site", "Local/Fallback", decision.Reason, $"Cloud decision failed and local heuristic was used instead. Error: {errorMessage}")
                : decision.Trace with { Source = "Local/Fallback", Details = $"{decision.Trace.Details} Cloud fallback reason: {errorMessage}" }
        };
    }

    private static PotionDecision Publish(PotionDecision decision)
    {
        if (decision.Trace is not null)
        {
            AiBotDecisionFeed.Publish(decision.Trace);
        }

        return decision;
    }

    private static CombatDecision Publish(CombatDecision decision)
    {
        if (decision.Trace is not null)
        {
            AiBotDecisionFeed.Publish(decision.Trace);
        }

        return decision;
    }

    private static RewardDecision Publish(RewardDecision decision)
    {
        if (decision.Trace is not null)
        {
            AiBotDecisionFeed.Publish(decision.Trace);
        }

        return decision;
    }

    private static CardRewardDecision Publish(CardRewardDecision decision)
    {
        if (decision.Trace is not null)
        {
            AiBotDecisionFeed.Publish(decision.Trace);
        }

        return decision;
    }

    private static CardSelectionDecision Publish(CardSelectionDecision decision)
    {
        if (decision.Trace is not null)
        {
            AiBotDecisionFeed.Publish(decision.Trace);
        }

        return decision;
    }

    private static CardRewardChoiceDecision Publish(CardRewardChoiceDecision decision)
    {
        if (decision.Trace is not null)
        {
            AiBotDecisionFeed.Publish(decision.Trace);
        }

        return decision;
    }

    private static BundleChoiceDecision Publish(BundleChoiceDecision decision)
    {
        if (decision.Trace is not null)
        {
            AiBotDecisionFeed.Publish(decision.Trace);
        }

        return decision;
    }

    private static CrystalSphereActionDecision Publish(CrystalSphereActionDecision decision)
    {
        if (decision.Trace is not null)
        {
            AiBotDecisionFeed.Publish(decision.Trace);
        }

        return decision;
    }

    private static MapDecision Publish(MapDecision decision)
    {
        if (decision.Trace is not null)
        {
            AiBotDecisionFeed.Publish(decision.Trace);
        }

        return decision;
    }

    private static ShopDecision Publish(ShopDecision decision)
    {
        if (decision.Trace is not null)
        {
            AiBotDecisionFeed.Publish(decision.Trace);
        }

        return decision;
    }

    private static RestDecision Publish(RestDecision decision)
    {
        if (decision.Trace is not null)
        {
            AiBotDecisionFeed.Publish(decision.Trace);
        }

        return decision;
    }

    private static EventDecision Publish(EventDecision decision)
    {
        if (decision.Trace is not null)
        {
            AiBotDecisionFeed.Publish(decision.Trace);
        }

        return decision;
    }

    private static RelicChoiceDecision Publish(RelicChoiceDecision decision)
    {
        if (decision.Trace is not null)
        {
            AiBotDecisionFeed.Publish(decision.Trace);
        }

        return decision;
    }

    public void Dispose()
    {
        _cloud?.Dispose();
    }
}
