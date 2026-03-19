using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Entities.RestSite;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Events.Custom.CrystalSphereEvent;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Rewards;
using MegaCrit.Sts2.Core.Rewards;
using aibot.Scripts.Knowledge;

namespace aibot.Scripts.Decision;

public sealed class GuideHeuristicDecisionEngine : IAiDecisionEngine
{
    private readonly GuideKnowledgeBase _knowledgeBase;

    public GuideHeuristicDecisionEngine(GuideKnowledgeBase knowledgeBase)
    {
        _knowledgeBase = knowledgeBase;
    }

    public Task<PotionDecision> ChoosePotionUseAsync(Player player, IReadOnlyList<PotionModel> usablePotions, IReadOnlyList<CardModel> playableCards, IReadOnlyList<Creature> enemies, RunAnalysis analysis, CancellationToken cancellationToken)
    {
        var currentHp = player.Creature.CurrentHp;
        var maxHp = player.Creature.MaxHp;
        var healthRatio = maxHp <= 0 ? 1f : currentHp / (float)maxHp;

        var best = usablePotions
            .Select(potion => new
            {
                Potion = potion,
                Score = ScorePotionUse(potion, playableCards, enemies, healthRatio, analysis)
            })
            .OrderByDescending(x => x.Score)
            .FirstOrDefault();

        if (best is null || best.Score < 100)
        {
            return Task.FromResult(new PotionDecision(null, null, "Local heuristic kept potions for later.", new DecisionTrace("Consumable", "Local", "Hold consumables", "Local heuristic did not find a potion use with enough value in the current combat state.")));
        }

        var target = ChoosePotionTarget(best.Potion, player, enemies);
        if (RequiresPotionTarget(best.Potion) && target is null)
        {
            return Task.FromResult(new PotionDecision(null, null, $"Skipped {best.Potion.Title} because no valid target was available.", new DecisionTrace("Consumable", "Local", $"Skip {best.Potion.Title}", $"Local heuristic considered {best.Potion.Title} but found no valid target for {best.Potion.TargetType}.")));
        }

        return Task.FromResult(new PotionDecision(best.Potion, target, $"Local heuristic selected potion {best.Potion.Title}.", new DecisionTrace("Consumable", "Local", $"Use {best.Potion.Title}", $"Local heuristic chose {best.Potion.Title} with score {best.Score} based on HP {currentHp}/{maxHp}, {usablePotions.Count} usable potions, and {playableCards.Count} playable cards.")));
    }

    public Task<CombatDecision> ChooseCombatActionAsync(Player player, IReadOnlyList<CardModel> playableCards, IReadOnlyList<Creature> enemies, RunAnalysis analysis, CancellationToken cancellationToken)
    {
        var chosen = playableCards
            .OrderByDescending(card => ScoreCombatCard(card, analysis))
            .FirstOrDefault();

        if (chosen is null)
        {
            return Task.FromResult(new CombatDecision(null, null, true, "No playable cards available.", new DecisionTrace("Combat", "Local", "End turn", "No playable cards were available, so the local heuristic ended the turn.")));
        }

        var target = ChooseCombatTarget(chosen, player, enemies);

        var targetText = target is null ? "no target required" : $"targeting {target.Name}";
        return Task.FromResult(new CombatDecision(chosen, target, false, $"Local heuristic selected {chosen.Title}.", new DecisionTrace("Combat", "Local", $"Play {chosen.Title}", $"Local heuristic chose {chosen.Title} because it had the best combat score for the current hand, with {targetText}.")));
    }

    public Task<CardRewardDecision> ChooseCardRewardAsync(IReadOnlyList<CardModel> options, RunAnalysis analysis, CancellationToken cancellationToken)
    {
        var chosen = options
            .OrderByDescending(card => ScoreRewardCard(card, analysis))
            .FirstOrDefault();

        return Task.FromResult(new CardRewardDecision(chosen, chosen is null ? "No card options." : $"Preferred card: {chosen.Title}", new DecisionTrace("Card Reward", "Local", chosen is null ? "Skip card choice" : $"Take {chosen.Title}", chosen is null ? "No selectable card rewards were available." : $"Local heuristic preferred {chosen.Title} because it best matched the current build plan {analysis.RecommendedBuildName}.")));
    }

    public Task<CardSelectionDecision> ChooseCardSelectionAsync(AiCardSelectionContext request, IReadOnlyList<CardModel> options, RunAnalysis analysis, CancellationToken cancellationToken)
    {
        var scored = options
            .Select(card => new
            {
                Card = card,
                Score = ScoreSelectionCard(card, request, analysis)
            })
            .OrderByDescending(entry => entry.Score)
            .ToList();

        var best = scored.FirstOrDefault();
        if (best is null)
        {
            return Task.FromResult(new CardSelectionDecision(null, true, "No selectable cards.", new DecisionTrace("Card Selection", "Local", "No card selection", $"No selectable cards were available for {DescribeSelectionKind(request.Kind)}.")));
        }

        if (request.MinSelect == 0 && request.Cancelable && best.Score < GetSelectionThreshold(request.Kind))
        {
            return Task.FromResult(new CardSelectionDecision(null, true, "Local heuristic chose to stop selecting.", new DecisionTrace("Card Selection", "Local", "Stop selecting", $"Local heuristic stopped card selection for {DescribeSelectionKind(request.Kind)} because the best score {best.Score} was below threshold.")));
        }

        return Task.FromResult(new CardSelectionDecision(best.Card, false, $"Local heuristic selected {best.Card.Title}.", new DecisionTrace("Card Selection", "Local", $"Select {best.Card.Title}", $"Local heuristic chose {best.Card.Title} for {DescribeSelectionKind(request.Kind)} with score {best.Score}.")));
    }

    public Task<CardRewardChoiceDecision> ChooseCardRewardChoiceAsync(AiCardSelectionContext request, IReadOnlyList<CardModel> options, IReadOnlyList<DecisionOption> alternatives, RunAnalysis analysis, CancellationToken cancellationToken)
    {
        var bestCard = options
            .Select(card => new
            {
                Card = card,
                Score = ScoreRewardCard(card, analysis)
            })
            .OrderByDescending(entry => entry.Score)
            .FirstOrDefault();

        var reroll = FindAlternative(alternatives, "reroll");
        var skip = FindAlternative(alternatives, "skip");

        if (bestCard is null)
        {
            if (reroll is not null)
            {
                return Task.FromResult(new CardRewardChoiceDecision(null, reroll.Key, "Local heuristic preferred reroll because no card reward was available.", new DecisionTrace("Card Reward", "Local", $"Choose {reroll.Label}", "No card rewards were available, so the local heuristic selected the reroll alternative.")));
            }

            if (skip is not null)
            {
                return Task.FromResult(new CardRewardChoiceDecision(null, skip.Key, "Local heuristic skipped the reward because no card reward was available.", new DecisionTrace("Card Reward", "Local", $"Choose {skip.Label}", "No card rewards were available, so the local heuristic selected the skip alternative.")));
            }

            return Task.FromResult(new CardRewardChoiceDecision(null, null, "No reward choice available.", new DecisionTrace("Card Reward", "Local", "No reward choice", "No card rewards or reward alternatives were available.")));
        }

        if (bestCard.Score < 25 && reroll is not null)
        {
            return Task.FromResult(new CardRewardChoiceDecision(null, reroll.Key, "Local heuristic preferred reroll over weak card rewards.", new DecisionTrace("Card Reward", "Local", $"Choose {reroll.Label}", $"Best card reward score was only {bestCard.Score}, so the local heuristic chose reroll instead.")));
        }

        if (bestCard.Score < 10 && skip is not null)
        {
            return Task.FromResult(new CardRewardChoiceDecision(null, skip.Key, "Local heuristic skipped weak card rewards.", new DecisionTrace("Card Reward", "Local", $"Choose {skip.Label}", $"Best card reward score was only {bestCard.Score}, so the local heuristic skipped the reward.")));
        }

        return Task.FromResult(new CardRewardChoiceDecision(bestCard.Card, null, $"Local heuristic selected {bestCard.Card.Title}.", new DecisionTrace("Card Reward", "Local", $"Take {bestCard.Card.Title}", $"Local heuristic preferred {bestCard.Card.Title} with score {bestCard.Score}.")));
    }

    public Task<BundleChoiceDecision> ChooseBundleAsync(AiCardSelectionContext request, IReadOnlyList<CardBundleOption> bundles, RunAnalysis analysis, CancellationToken cancellationToken)
    {
        var best = bundles
            .Select(bundle => new
            {
                Bundle = bundle,
                Score = ScoreBundle(bundle, analysis)
            })
            .OrderByDescending(entry => entry.Score)
            .FirstOrDefault();

        if (best is null)
        {
            return Task.FromResult(new BundleChoiceDecision(-1, "No bundles available.", new DecisionTrace("Bundle Choice", "Local", "No bundle choice", "No selectable bundles were available.")));
        }

        var summary = string.Join(", ", best.Bundle.Cards.Take(3).Select(card => card.Title.ToString()));
        return Task.FromResult(new BundleChoiceDecision(best.Bundle.Index, $"Local heuristic selected bundle {best.Bundle.Index}.", new DecisionTrace("Bundle Choice", "Local", $"Choose bundle {best.Bundle.Index}", $"Local heuristic chose bundle {best.Bundle.Index} ({summary}) with score {best.Score}.")));
    }

    public Task<CrystalSphereActionDecision> ChooseCrystalSphereActionAsync(CrystalSphereMinigame minigame, RunAnalysis analysis, CancellationToken cancellationToken)
    {
        var actions = BuildCrystalSphereActionCandidates(minigame)
            .OrderByDescending(action => action.Score)
            .ToList();

        var best = actions.FirstOrDefault();
        if (best is null)
        {
            return Task.FromResult(new CrystalSphereActionDecision(0, 0, true, "No hidden Crystal Sphere cells remained.", new DecisionTrace("Crystal Sphere", "Local", "No divination action", "No hidden Crystal Sphere cells remained.")));
        }

        var toolLabel = best.UseBigDivination ? "big" : "small";
        return Task.FromResult(new CrystalSphereActionDecision(best.X, best.Y, best.UseBigDivination, $"Local heuristic selected {toolLabel} divination at ({best.X},{best.Y}).", new DecisionTrace("Crystal Sphere", "Local", $"Use {toolLabel} divination at ({best.X},{best.Y})", $"Local heuristic chose a Crystal Sphere action with score {best.Score}, coverage {best.Coverage}, centerBias {best.CenterBias}.")));
    }

    public Task<RewardDecision> ChooseRewardAsync(IReadOnlyList<NRewardButton> options, bool hasOpenPotionSlots, RunAnalysis analysis, CancellationToken cancellationToken)
    {
        var filtered = options
            .Where(button => button.IsEnabled)
            .Where(button => hasOpenPotionSlots || button.Reward is not PotionReward)
            .OrderByDescending(button => ScoreRewardButton(button, analysis))
            .ToList();

        var chosen = filtered.FirstOrDefault();
        return Task.FromResult(new RewardDecision(chosen, chosen is null ? "No enabled reward buttons." : $"Selected reward type {chosen.Reward?.GetType().Name ?? "unknown"}.", new DecisionTrace("Rewards", "Local", chosen is null ? "No reward selected" : $"Take {chosen.Reward?.GetType().Name ?? "reward"}", chosen is null ? "No enabled rewards were available." : $"Local heuristic prioritized {chosen.Reward?.GetType().Name ?? "reward"} based on the current reward ordering rules.")));
    }

    public Task<ShopDecision> ChooseShopPurchaseAsync(IReadOnlyList<MerchantEntry> options, int currentGold, bool hasOpenPotionSlots, RunAnalysis analysis, CancellationToken cancellationToken)
    {
        var best = options
            .Where(option => option.IsStocked && option.EnoughGold)
            .Where(option => hasOpenPotionSlots || option is not MerchantPotionEntry)
            .Select(option => new
            {
                Entry = option,
                Score = ScoreShopEntry(option, analysis, currentGold)
            })
            .OrderByDescending(x => x.Score)
            .FirstOrDefault();

        if (best is null || best.Score < 115)
        {
            return Task.FromResult(new ShopDecision(null, "Local heuristic chose to save gold.", new DecisionTrace("Shop", "Local", "Leave shop", $"Local heuristic did not find a shop purchase above threshold with {currentGold} gold and potion slot open={hasOpenPotionSlots}.")));
        }

        return Task.FromResult(new ShopDecision(best.Entry, $"Local heuristic selected {DescribeShopEntry(best.Entry)}.", new DecisionTrace("Shop", "Local", $"Buy {DescribeShopEntry(best.Entry)}", $"Local heuristic chose {DescribeShopEntry(best.Entry)} with score {best.Score} while holding {currentGold} gold.")));
    }

    public Task<RestDecision> ChooseRestSiteOptionAsync(Player player, IReadOnlyList<RestSiteOption> options, RunAnalysis analysis, CancellationToken cancellationToken)
    {
        var best = options
            .Where(option => option.IsEnabled)
            .Select(option => new
            {
                Option = option,
                Score = ScoreRestSiteOption(option, player, analysis)
            })
            .OrderByDescending(x => x.Score)
            .FirstOrDefault();

        if (best is null)
        {
            return Task.FromResult(new RestDecision(null, "No enabled rest site options.", new DecisionTrace("Rest Site", "Local", "No campfire action", "No enabled rest site options were available.")));
        }

        return Task.FromResult(new RestDecision(best.Option, $"Local heuristic selected {best.Option.Title.GetFormattedText()}.", new DecisionTrace("Rest Site", "Local", $"Choose {best.Option.Title.GetFormattedText()}", $"Local heuristic chose {best.Option.OptionId} with score {best.Score} at HP {player.Creature.CurrentHp}/{player.Creature.MaxHp}.")));
    }

    public Task<MapDecision> ChooseMapPointAsync(IReadOnlyList<MapPoint> options, int currentHp, int maxHp, int gold, RunAnalysis analysis, CancellationToken cancellationToken)
    {
        var chosen = options
            .OrderByDescending(point => ScoreMapPoint(point, currentHp, maxHp, gold))
            .ThenBy(point => point.coord.col)
            .FirstOrDefault();

        return Task.FromResult(new MapDecision(chosen, chosen is null ? "No map options." : $"Selected map point type {chosen.PointType}.", new DecisionTrace("Map", "Local", chosen is null ? "No map move" : $"Go to {chosen.PointType} ({chosen.coord.row},{chosen.coord.col})", chosen is null ? "No travelable map nodes were available." : $"Local heuristic selected {chosen.PointType} at ({chosen.coord.row},{chosen.coord.col}) using current HP {currentHp}/{maxHp} and gold {gold}.")));
    }

    public Task<EventDecision> ChooseEventOptionAsync(EventModel eventModel, IReadOnlyList<EventOption> options, RunAnalysis analysis, CancellationToken cancellationToken)
    {
        var available = options
            .Where(option => !option.IsLocked)
            .Select(option => new
            {
                Option = option,
                Score = ScoreEventOption(option, analysis)
            })
            .OrderByDescending(x => x.Score)
            .ToList();

        var chosen = available.FirstOrDefault();
        if (chosen is null)
        {
            return Task.FromResult(new EventDecision(null, "No enabled event options.", new DecisionTrace("Event", "Local", "No event choice", "No selectable event options were available.")));
        }

        var title = GetEventOptionLabel(chosen.Option);
        return Task.FromResult(new EventDecision(chosen.Option, $"Local heuristic selected {title}.", new DecisionTrace("Event", "Local", $"Choose {title}", $"Local heuristic chose event option {chosen.Option.TextKey} with score {chosen.Score}.")));
    }

    public Task<RelicChoiceDecision> ChooseRelicAsync(IReadOnlyList<RelicModel> options, string source, bool allowSkip, RunAnalysis analysis, CancellationToken cancellationToken)
    {
        var chosen = options
            .Select(relic => new
            {
                Relic = relic,
                Score = ScoreRelicChoice(relic, analysis)
            })
            .OrderByDescending(x => x.Score)
            .FirstOrDefault();

        if (chosen is null)
        {
            return Task.FromResult(new RelicChoiceDecision(null, allowSkip, $"No relic options in {source}.", new DecisionTrace("Relic Choice", "Local", allowSkip ? "Skip relic" : "No relic choice", $"No selectable relic options were available in {source}.")));
        }

        if (allowSkip && chosen.Score < 95)
        {
            return Task.FromResult(new RelicChoiceDecision(null, true, $"Local heuristic skipped relic selection in {source}.", new DecisionTrace("Relic Choice", "Local", "Skip relic", $"Best relic score in {source} was only {chosen.Score}, so the local heuristic skipped the selection.")));
        }

        var title = chosen.Relic.Title.GetFormattedText();
        return Task.FromResult(new RelicChoiceDecision(chosen.Relic, false, $"Local heuristic selected {title}.", new DecisionTrace("Relic Choice", "Local", $"Choose {title}", $"Local heuristic chose relic {title} from {source} with score {chosen.Score}.")));
    }

    private int ScoreCombatCard(CardModel card, RunAnalysis analysis)
    {
        var score = 0;
        switch (card.Type)
        {
            case CardType.Power:
                score += 80;
                break;
            case CardType.Attack:
                score += 55;
                break;
            case CardType.Skill:
                score += 35;
                break;
            default:
                score -= 50;
                break;
        }

        if (card.EnergyCost.GetAmountToSpend() == 0)
        {
            score += 20;
        }

        if (card.EnergyCost.CostsX)
        {
            score += 5;
        }

        score += ScoreKnowledgeMatch(card.Title, analysis);
        score += ScoreKnowledgeMatch(card.Id.Entry, analysis);
        return score;
    }

    private int ScoreRewardCard(CardModel card, RunAnalysis analysis)
    {
        var score = 0;
        switch (card.Type)
        {
            case CardType.Power:
                score += 30;
                break;
            case CardType.Attack:
                score += 18;
                break;
            case CardType.Skill:
                score += 12;
                break;
        }

        score += ScoreKnowledgeMatch(card.Title, analysis) * 2;
        score += ScoreKnowledgeMatch(card.Id.Entry, analysis) * 2;
        return score;
    }

    private int ScoreSelectionCard(CardModel card, AiCardSelectionContext request, RunAnalysis analysis)
    {
        return request.Kind switch
        {
            AiCardSelectionKind.DeckRemove => ScoreRemovalCandidate(card, analysis),
            AiCardSelectionKind.DeckTransform => ScoreRemovalCandidate(card, analysis) + 20,
            AiCardSelectionKind.HandDiscard => ScoreDiscardCandidate(card, analysis),
            AiCardSelectionKind.DeckUpgrade or AiCardSelectionKind.HandUpgrade => ScoreUpgradeCandidate(card, analysis),
            AiCardSelectionKind.DeckEnchant => ScoreUpgradeCandidate(card, analysis) + 10,
            AiCardSelectionKind.HandSelect => ScoreCombatCard(card, analysis),
            AiCardSelectionKind.SimpleGrid or AiCardSelectionKind.DeckGeneric => ScoreGenericSelectionCard(card, request, analysis),
            _ => ScoreRewardCard(card, analysis)
        };
    }

    private int ScoreRemovalCandidate(CardModel card, RunAnalysis analysis)
    {
        var text = GuideKnowledgeBase.Normalize($"{card.Title} {card.Id.Entry}");
        var score = 120 - ScoreRewardCard(card, analysis) * 2;

        if (ContainsAny(text, "curse", "status", "burn", "wound", "slime", "dazed", "void", "regret", "shame", "pain", "decay"))
        {
            score += 400;
        }

        if (ContainsAny(text, "strike", "defend"))
        {
            score += 180;
        }

        if (card.IsUpgraded)
        {
            score -= 60;
        }

        if (!string.IsNullOrWhiteSpace(analysis.RemovalCandidateSummary) && analysis.RemovalCandidateSummary.Contains(card.Title.ToString(), StringComparison.OrdinalIgnoreCase))
        {
            score += 120;
        }

        return score;
    }

    private int ScoreUpgradeCandidate(CardModel card, RunAnalysis analysis)
    {
        var score = ScoreRewardCard(card, analysis) * 2;
        score += card.IsUpgraded ? -180 : 80;
        score += card.Type switch
        {
            CardType.Power => 40,
            CardType.Attack => 20,
            CardType.Skill => 15,
            _ => 0
        };

        if (card.EnergyCost.GetAmountToSpend() >= 2)
        {
            score += 15;
        }

        return score;
    }

    private int ScoreDiscardCandidate(CardModel card, RunAnalysis analysis)
    {
        var text = GuideKnowledgeBase.Normalize($"{card.Title} {card.Id.Entry}");
        var score = 140 - ScoreCombatCard(card, analysis);

        if (ContainsAny(text, "status", "curse", "burn", "wound", "slime", "dazed", "void"))
        {
            score += 250;
        }

        score += card.EnergyCost.GetAmountToSpend() >= 2 ? 20 : 0;
        score -= card.EnergyCost.GetAmountToSpend() == 0 ? 10 : 0;
        return score;
    }

    private int ScoreGenericSelectionCard(CardModel card, AiCardSelectionContext request, RunAnalysis analysis)
    {
        var prompt = GuideKnowledgeBase.Normalize(request.PromptText);
        if (ContainsAny(prompt, "remove", "purge", "exhaust", "transform"))
        {
            return ScoreRemovalCandidate(card, analysis);
        }

        if (ContainsAny(prompt, "upgrade", "smith", "enchant"))
        {
            return ScoreUpgradeCandidate(card, analysis);
        }

        if (ContainsAny(prompt, "discard"))
        {
            return ScoreDiscardCandidate(card, analysis);
        }

        return ScoreRewardCard(card, analysis);
    }

    private int ScoreBundle(CardBundleOption bundle, RunAnalysis analysis)
    {
        return bundle.Cards
            .OrderByDescending(card => ScoreRewardCard(card, analysis))
            .Take(3)
            .Sum(card => ScoreRewardCard(card, analysis));
    }

    private static int GetSelectionThreshold(AiCardSelectionKind kind)
    {
        return kind switch
        {
            AiCardSelectionKind.HandDiscard => 30,
            AiCardSelectionKind.SimpleGrid or AiCardSelectionKind.DeckGeneric => 20,
            _ => 0
        };
    }

    private int ScoreRewardButton(NRewardButton button, RunAnalysis analysis)
    {
        var reward = button.Reward;
        if (reward is null)
        {
            return -100;
        }

        var typeName = reward.GetType().Name;
        var score = typeName switch
        {
            nameof(CardReward) => 100,
            nameof(RelicReward) => 90,
            nameof(GoldReward) => 70,
            nameof(PotionReward) => 50,
            _ => 40
        };

        return score;
    }

    private int ScorePotionUse(PotionModel potion, IReadOnlyList<CardModel> playableCards, IReadOnlyList<Creature> enemies, float healthRatio, RunAnalysis analysis)
    {
        if (potion.IsQueued || potion.HasBeenRemovedFromState || !potion.PassesCustomUsabilityCheck)
        {
            return int.MinValue;
        }

        if (potion.Usage is PotionUsage.Automatic or PotionUsage.None)
        {
            return int.MinValue;
        }

        var normalized = GuideKnowledgeBase.Normalize($"{potion.Title}");
        var enemyCount = enemies.Count(enemy => enemy.IsAlive);
        var lowestEnemyHp = enemies.Where(enemy => enemy.IsAlive).Select(enemy => enemy.CurrentHp).DefaultIfEmpty(int.MaxValue).Min();
        var score = potion.Usage == PotionUsage.AnyTime ? 65 : 90;

        if (ContainsAny(normalized, "energy", "swift", "speed", "strength", "dexterity", "focus", "regen", "ghost", "gigantification", "bronze", "courage", "serum", "tonic", "fortifier"))
        {
            score += 45;
        }

        if (ContainsAny(normalized, "attack", "skill", "power", "colorless", "cunning", "memories", "bottled", "chaos"))
        {
            score += playableCards.Count <= 1 ? 50 : 25;
        }

        if (ContainsAny(normalized, "fire", "poison", "weak", "vulnerable", "shackling", "doom", "explosive", "ashwater", "rock"))
        {
            score += 40;
            if (lowestEnemyHp <= 25)
            {
                score += 30;
            }

            if (enemyCount >= 2)
            {
                score += 15;
            }
        }

        if (ContainsAny(normalized, "blood", "fruit", "entropic"))
        {
            score += healthRatio < 0.55f ? 55 : -25;
        }

        score += ScoreKnowledgeMatch($"{potion.Title}", analysis);
        return score;
    }

    private int ScoreShopEntry(MerchantEntry entry, RunAnalysis analysis, int currentGold)
    {
        var score = entry switch
        {
            MerchantRelicEntry relicEntry when relicEntry.Model is not null => 180 + ScoreKnowledgeMatch($"{relicEntry.Model.Title}", analysis) * 2 + (relicEntry.Model.Rarity switch
            {
                RelicRarity.Shop => 35,
                RelicRarity.Rare => 25,
                RelicRarity.Uncommon => 15,
                RelicRarity.Ancient => 20,
                _ => 5
            }),
            MerchantCardEntry cardEntry when cardEntry.CreationResult?.Card is not null => 110 + ScoreRewardCard(cardEntry.CreationResult.Card, analysis) * 2 + (cardEntry.IsOnSale ? 35 : 0),
            MerchantPotionEntry potionEntry when potionEntry.Model is not null => 85 + potionEntry.Model.Rarity switch
            {
                PotionRarity.Rare => 20,
                PotionRarity.Uncommon => 10,
                _ => 0
            },
            MerchantCardRemovalEntry removalEntry when !removalEntry.Used => analysis.DeckCardNames.Count >= 14 ? 150 : 95,
            _ => 0
        };

        score += Math.Max(0, 40 - entry.Cost / 5);
        if (entry.Cost >= currentGold)
        {
            score -= 20;
        }

        return score;
    }

    private int ScoreRelicChoice(RelicModel relic, RunAnalysis analysis)
    {
        var score = relic.Rarity switch
        {
            RelicRarity.Rare => 130,
            RelicRarity.Ancient => 120,
            RelicRarity.Shop => 110,
            RelicRarity.Uncommon => 95,
            RelicRarity.Common => 75,
            RelicRarity.Starter => 55,
            _ => 60
        };

        score += ScoreKnowledgeMatch(relic.Title.GetFormattedText(), analysis) * 2;
        score += ScoreKnowledgeMatch(relic.Id.Entry, analysis) * 2;

        var guide = _knowledgeBase.FindRelic(relic.Title.GetFormattedText(), analysis.CharacterId);
        if (guide is not null)
        {
            score += 30;
            var guideText = GuideKnowledgeBase.Normalize(guide.DescriptionEn);
            if (ContainsAny(guideText, "synergy", "combo", "draw", "energy", "block", "strength", "dexterity", "scaling", "upgrade", "poison", "orb", "stance", "discard"))
            {
                score += 15;
            }
        }

        return score;
    }

    private int ScoreEventOption(EventOption option, RunAnalysis analysis)
    {
        if (option.IsLocked)
        {
            return int.MinValue;
        }

        var score = option.IsProceed ? -35 : 20;
        var title = GuideKnowledgeBase.Normalize(option.Title.GetFormattedText());
        var description = GuideKnowledgeBase.Normalize(option.Description.GetFormattedText());
        var text = $"{title} {description}";

        if (option.WillKillPlayer is not null)
        {
            score -= 1000;
        }

        if (option.Relic is not null)
        {
            score += 70 + ScoreRelicChoice(option.Relic, analysis);
        }

        if (ContainsAny(text, "remove", "purge", "cleanse", "transform", "upgrade", "smith"))
        {
            score += 60;
        }

        if (ContainsAny(text, "relic", "artifact", "trinket"))
        {
            score += 55;
        }

        if (ContainsAny(text, "gold", "maxhp", "heal", "recover", "potion", "card"))
        {
            score += 25;
        }

        if (ContainsAny(text, "losehp", "damage", "curse", "wound", "regret", "shame", "pain", "decay"))
        {
            score -= 45;
        }

        if (option.IsProceed && score < 30)
        {
            score -= 20;
        }

        return score;
    }

    private static int ScoreRestSiteOption(RestSiteOption option, Player player, RunAnalysis analysis)
    {
        var healthRatio = player.Creature.MaxHp <= 0 ? 1f : player.Creature.CurrentHp / (float)player.Creature.MaxHp;
        var optionId = option.OptionId.ToLowerInvariant();
        var score = 0;

        if (optionId.Contains("heal") || optionId.Contains("rest"))
        {
            score += healthRatio < 0.45f ? 220 : healthRatio < 0.65f ? 130 : 40;
        }

        if (optionId.Contains("smith"))
        {
            score += healthRatio > 0.60f ? 180 : 85;
            if (!string.IsNullOrWhiteSpace(analysis.StrategicNeedsSummary) && analysis.StrategicNeedsSummary.Contains("Upgrades still have high value", StringComparison.OrdinalIgnoreCase))
            {
                score += 40;
            }
        }

        if (optionId.Contains("dig") || optionId.Contains("lift") || optionId.Contains("cook") || optionId.Contains("clone") || optionId.Contains("hatch") || optionId.Contains("mend"))
        {
            score += healthRatio > 0.72f ? 160 : 95;
        }

        return score;
    }

    private int ScoreMapPoint(MapPoint point, int currentHp, int maxHp, int gold)
    {
        var healthRatio = maxHp <= 0 ? 1f : currentHp / (float)maxHp;
        return point.PointType switch
        {
            MapPointType.RestSite when healthRatio < 0.45f => 150,
            MapPointType.Shop when gold >= 180 => 130,
            MapPointType.Treasure => 120,
            MapPointType.Monster => 100,
            MapPointType.Elite when healthRatio > 0.70f => 115,
            MapPointType.Elite => 70,
            MapPointType.Unknown => 95,
            MapPointType.RestSite => 80,
            MapPointType.Shop => 75,
            MapPointType.Boss => 200,
            _ => 50
        };
    }

    private int ScoreKnowledgeMatch(string cardName, RunAnalysis analysis)
    {
        var normalized = GuideKnowledgeBase.Normalize(cardName);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return 0;
        }

        if (analysis.DeckCardNames.Any(name => GuideKnowledgeBase.Normalize(name) == normalized))
        {
            return 12;
        }

        foreach (var build in _knowledgeBase.GetBuildsForCharacter(analysis.CharacterId))
        {
            if (_knowledgeBase.MentionsCard(normalized, build))
            {
                return build.NameEn == analysis.RecommendedBuildName ? 25 : 10;
            }
        }

        return 0;
    }

    private static Creature? ChoosePotionTarget(PotionModel potion, Player player, IReadOnlyList<Creature> enemies)
    {
        return ChooseTargetForType(potion.TargetType, player, enemies);
    }

    private static bool RequiresPotionTarget(PotionModel potion)
    {
        return potion.TargetType is TargetType.AnyEnemy or TargetType.AnyAlly or TargetType.AnyPlayer or TargetType.Self or TargetType.TargetedNoCreature;
    }

    private static Creature? ChooseCombatTarget(CardModel card, Player player, IReadOnlyList<Creature> enemies)
    {
        return ChooseTargetForType(card.TargetType, player, enemies);
    }

    private static Creature? ChooseTargetForType(TargetType targetType, Player player, IReadOnlyList<Creature> enemies)
    {
        return targetType switch
        {
            TargetType.AnyEnemy => enemies.Where(enemy => enemy.IsAlive).OrderBy(enemy => enemy.CurrentHp).ThenBy(enemy => enemy.Block).FirstOrDefault(),
            TargetType.AnyAlly => GetAllyTargets(player).OrderBy(ally => ally.CurrentHp / (float)Math.Max(1, ally.MaxHp)).ThenBy(ally => ally.CurrentHp).FirstOrDefault(),
            _ => null
        };
    }

    private static IEnumerable<Creature> GetAllyTargets(Player player)
    {
        yield return player.Creature;

        if (player.PlayerCombatState is null)
        {
            yield break;
        }

        foreach (var pet in player.PlayerCombatState.Pets.Where(pet => pet.IsAlive))
        {
            yield return pet;
        }
    }

    private static string DescribeShopEntry(MerchantEntry entry)
    {
        return entry switch
        {
            MerchantCardEntry cardEntry when cardEntry.CreationResult?.Card is not null => $"card {cardEntry.CreationResult.Card.Title}",
            MerchantRelicEntry relicEntry when relicEntry.Model is not null => $"relic {relicEntry.Model.Title}",
            MerchantPotionEntry potionEntry when potionEntry.Model is not null => $"potion {potionEntry.Model.Title}",
            MerchantCardRemovalEntry => "card removal",
            _ => "shop item"
        };
    }

    private static bool ContainsAny(string source, params string[] fragments)
    {
        return fragments.Any(fragment => source.Contains(fragment, StringComparison.OrdinalIgnoreCase));
    }

    private static DecisionOption? FindAlternative(IReadOnlyList<DecisionOption> alternatives, string key)
    {
        return alternatives.FirstOrDefault(option => string.Equals(option.Key, key, StringComparison.OrdinalIgnoreCase));
    }

    private static List<CrystalSphereActionCandidate> BuildCrystalSphereActionCandidates(CrystalSphereMinigame minigame)
    {
        var candidates = new List<CrystalSphereActionCandidate>();
        for (var x = 0; x < minigame.GridSize.X; x++)
        {
            for (var y = 0; y < minigame.GridSize.Y; y++)
            {
                if (!minigame.cells[x, y].IsHidden)
                {
                    continue;
                }

                var centerBias = 12 - Math.Abs(x - minigame.GridSize.X / 2) - Math.Abs(y - minigame.GridSize.Y / 2);
                candidates.Add(new CrystalSphereActionCandidate(x, y, true, GetCrystalSphereCoverage(minigame, x, y, true), centerBias));
                candidates.Add(new CrystalSphereActionCandidate(x, y, false, GetCrystalSphereCoverage(minigame, x, y, false), centerBias));
            }
        }

        return candidates;
    }

    private static int GetCrystalSphereCoverage(CrystalSphereMinigame minigame, int x, int y, bool useBigDivination)
    {
        var coverage = 0;
        var radius = useBigDivination ? 1 : 0;
        for (var offsetX = -radius; offsetX <= radius; offsetX++)
        {
            for (var offsetY = -radius; offsetY <= radius; offsetY++)
            {
                var targetX = x + offsetX;
                var targetY = y + offsetY;
                if (targetX < 0 || targetY < 0 || targetX >= minigame.GridSize.X || targetY >= minigame.GridSize.Y)
                {
                    continue;
                }

                if (minigame.cells[targetX, targetY].IsHidden)
                {
                    coverage++;
                }
            }
        }

        return coverage;
    }

    private sealed record CrystalSphereActionCandidate(int X, int Y, bool UseBigDivination, int Coverage, int CenterBias)
    {
        public int Score => Coverage * (UseBigDivination ? 20 : 9) + CenterBias;
    }

    private static string DescribeSelectionKind(AiCardSelectionKind kind)
    {
        return kind switch
        {
            AiCardSelectionKind.DeckRemove => "deck removal",
            AiCardSelectionKind.DeckUpgrade => "deck upgrade",
            AiCardSelectionKind.DeckTransform => "deck transform",
            AiCardSelectionKind.DeckEnchant => "deck enchant",
            AiCardSelectionKind.HandDiscard => "hand discard",
            AiCardSelectionKind.HandUpgrade => "hand upgrade",
            AiCardSelectionKind.BundleChoice => "bundle choice",
            _ => kind.ToString()
        };
    }

    private static string GetEventOptionLabel(EventOption option)
    {
        var title = option.Title.GetFormattedText();
        if (!string.IsNullOrWhiteSpace(title))
        {
            return title;
        }

        var description = option.Description.GetFormattedText();
        return string.IsNullOrWhiteSpace(description) ? option.TextKey : description;
    }
}
