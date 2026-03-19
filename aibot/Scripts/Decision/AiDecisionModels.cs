using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.Entities.RestSite;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Rewards;

namespace aibot.Scripts.Decision;

public sealed record DecisionTrace(
    string Category,
    string Source,
    string Summary,
    string Details);

public sealed record RunAnalysis(
    int CharacterId,
    string CharacterName,
    string RecommendedBuildName,
    string RecommendedBuildSummary,
    IReadOnlyList<string> DeckCardNames,
    IReadOnlyList<string> RelicNames,
    IReadOnlyList<string> PotionNames,
    string CharacterBrief,
    string DeckSummary,
    string RelicSummary,
    string PotionSummary,
    string KnowledgeDigest,
    string RunProgressSummary,
    string PlayerStateSummary,
    string CombatSummary,
    string CharacterCombatMechanicSummary,
    string EnemySummary,
    string RecentHistorySummary,
    string StrategicNeedsSummary,
    string DeckStructureSummary,
    string RemovalCandidateSummary);

public sealed record CombatDecision(
    CardModel? Card,
    Creature? Target,
    bool EndTurn,
    string Reason,
    DecisionTrace? Trace = null);

public sealed record RewardDecision(
    NRewardButton? Button,
    string Reason,
    DecisionTrace? Trace = null);

public sealed record PotionDecision(
    PotionModel? Potion,
    Creature? Target,
    string Reason,
    DecisionTrace? Trace = null);

public sealed record ShopDecision(
    MerchantEntry? Entry,
    string Reason,
    DecisionTrace? Trace = null);

public sealed record RestDecision(
    RestSiteOption? Option,
    string Reason,
    DecisionTrace? Trace = null);

public sealed record CardRewardDecision(
    CardModel? Card,
    string Reason,
    DecisionTrace? Trace = null);

public enum AiCardSelectionKind
{
    Unknown,
    ChooseACard,
    CardReward,
    RewardGrid,
    SimpleGrid,
    DeckUpgrade,
    DeckTransform,
    DeckEnchant,
    DeckRemove,
    DeckGeneric,
    HandSelect,
    HandDiscard,
    HandUpgrade,
    BundleChoice
}

public sealed record AiCardSelectionContext(
    AiCardSelectionKind Kind,
    string PromptText,
    int MinSelect,
    int MaxSelect,
    bool Cancelable,
    string Zone,
    string? Source = null,
    string? ExtraInfo = null);

public sealed record CardSelectionDecision(
    CardModel? Card,
    bool StopSelecting,
    string Reason,
    DecisionTrace? Trace = null);

public sealed record CardRewardChoiceDecision(
    CardModel? Card,
    string? AlternativeOptionId,
    string Reason,
    DecisionTrace? Trace = null);

public sealed record BundleChoiceDecision(
    int SelectedIndex,
    string Reason,
    DecisionTrace? Trace = null);

public sealed record CrystalSphereActionDecision(
    int X,
    int Y,
    bool UseBigDivination,
    string Reason,
    DecisionTrace? Trace = null);

public sealed record MapDecision(
    MapPoint? Point,
    string Reason,
    DecisionTrace? Trace = null);

public sealed record EventDecision(
    EventOption? Option,
    string Reason,
    DecisionTrace? Trace = null);

public sealed record RelicChoiceDecision(
    RelicModel? Relic,
    bool SkipSelection,
    string Reason,
    DecisionTrace? Trace = null);

public sealed record DecisionOption(string Key, string Label, string ReasonHint);

public sealed record CardBundleOption(
    int Index,
    IReadOnlyList<CardModel> Cards);
