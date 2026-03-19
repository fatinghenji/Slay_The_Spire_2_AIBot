using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.RestSite;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Events.Custom.CrystalSphereEvent;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Rewards;

namespace aibot.Scripts.Decision;

public interface IAiDecisionEngine
{
    Task<PotionDecision> ChoosePotionUseAsync(Player player, IReadOnlyList<PotionModel> usablePotions, IReadOnlyList<CardModel> playableCards, IReadOnlyList<Creature> enemies, RunAnalysis analysis, CancellationToken cancellationToken);

    Task<CombatDecision> ChooseCombatActionAsync(Player player, IReadOnlyList<CardModel> playableCards, IReadOnlyList<Creature> enemies, RunAnalysis analysis, CancellationToken cancellationToken);

    Task<CardRewardDecision> ChooseCardRewardAsync(IReadOnlyList<CardModel> options, RunAnalysis analysis, CancellationToken cancellationToken);

    Task<CardSelectionDecision> ChooseCardSelectionAsync(AiCardSelectionContext context, IReadOnlyList<CardModel> options, RunAnalysis analysis, CancellationToken cancellationToken);

    Task<CardRewardChoiceDecision> ChooseCardRewardChoiceAsync(AiCardSelectionContext context, IReadOnlyList<CardModel> options, IReadOnlyList<DecisionOption> alternatives, RunAnalysis analysis, CancellationToken cancellationToken);

    Task<BundleChoiceDecision> ChooseBundleAsync(AiCardSelectionContext context, IReadOnlyList<CardBundleOption> bundles, RunAnalysis analysis, CancellationToken cancellationToken);

    Task<CrystalSphereActionDecision> ChooseCrystalSphereActionAsync(CrystalSphereMinigame minigame, RunAnalysis analysis, CancellationToken cancellationToken);

    Task<RewardDecision> ChooseRewardAsync(IReadOnlyList<NRewardButton> options, bool hasOpenPotionSlots, RunAnalysis analysis, CancellationToken cancellationToken);

    Task<ShopDecision> ChooseShopPurchaseAsync(IReadOnlyList<MerchantEntry> options, int currentGold, bool hasOpenPotionSlots, RunAnalysis analysis, CancellationToken cancellationToken);

    Task<RestDecision> ChooseRestSiteOptionAsync(Player player, IReadOnlyList<RestSiteOption> options, RunAnalysis analysis, CancellationToken cancellationToken);

    Task<MapDecision> ChooseMapPointAsync(IReadOnlyList<MapPoint> options, int currentHp, int maxHp, int gold, RunAnalysis analysis, CancellationToken cancellationToken);

    Task<EventDecision> ChooseEventOptionAsync(EventModel eventModel, IReadOnlyList<EventOption> options, RunAnalysis analysis, CancellationToken cancellationToken);

    Task<RelicChoiceDecision> ChooseRelicAsync(IReadOnlyList<RelicModel> options, string source, bool allowSkip, RunAnalysis analysis, CancellationToken cancellationToken);
}
