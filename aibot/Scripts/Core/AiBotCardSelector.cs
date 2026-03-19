using System.Diagnostics;
using MegaCrit.Sts2.Core.Entities.CardRewardAlternatives;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.TestSupport;
using aibot.Scripts.Decision;

namespace aibot.Scripts.Core;

public sealed class AiBotCardSelector : ICardSelector
{
    private readonly Func<RunAnalysis> _analysisFactory;
    private readonly IAiDecisionEngine _decisionEngine;

    public AiBotCardSelector(Func<RunAnalysis> analysisFactory, IAiDecisionEngine decisionEngine)
    {
        _analysisFactory = analysisFactory;
        _decisionEngine = decisionEngine;
    }

    public async Task<IEnumerable<CardModel>> GetSelectedCards(IEnumerable<CardModel> options, int minSelect, int maxSelect)
    {
        var list = options.ToList();
        if (list.Count == 0)
        {
            return Array.Empty<CardModel>();
        }

        var analysis = _analysisFactory();
        var picked = new List<CardModel>();
        var remaining = new List<CardModel>(list);
        var context = InferSelectionContext(list, minSelect, maxSelect);

        while (picked.Count < maxSelect && remaining.Count > 0)
        {
            var minimumRemaining = Math.Max(0, minSelect - picked.Count);
            var currentContext = context with
            {
                MinSelect = minimumRemaining,
                MaxSelect = Math.Min(maxSelect - picked.Count, remaining.Count),
                ExtraInfo = AppendExtraInfo(context.ExtraInfo, $"AlreadyPicked={picked.Count};Remaining={remaining.Count}")
            };

            var decision = await _decisionEngine.ChooseCardSelectionAsync(currentContext, remaining, analysis, CancellationToken.None);
            if (decision.StopSelecting && picked.Count >= minSelect)
            {
                break;
            }

            var choice = decision.Card is not null && remaining.Contains(decision.Card)
                ? decision.Card
                : remaining[0];
            picked.Add(choice);
            remaining.Remove(choice);
        }

        Log.Info($"[AiBot] Card selector picked {picked.Count} card(s).");
        return picked;
    }

    public CardModel? GetSelectedCardReward(IReadOnlyList<CardCreationResult> options, IReadOnlyList<CardRewardAlternative> alternatives)
    {
        var cards = options.Select(option => option.Card).Where(card => card is not null).Cast<CardModel>().ToList();
        if (cards.Count == 0)
        {
            return null;
        }

        var analysis = _analysisFactory();
        var decision = _decisionEngine.ChooseCardRewardAsync(cards, analysis, CancellationToken.None).GetAwaiter().GetResult();
        return decision.Card ?? cards.FirstOrDefault();
    }

    private static AiCardSelectionContext InferSelectionContext(IReadOnlyList<CardModel> options, int minSelect, int maxSelect)
    {
        var trace = new StackTrace();
        var frames = trace.GetFrames() ?? Array.Empty<StackFrame>();
        var frameNames = frames
            .Select(frame => frame.GetMethod()?.Name)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .ToList();

        var kind = InferKind(frameNames);
        var prompt = InferPrompt(kind, minSelect, maxSelect);
        var zone = InferZone(kind);
        var source = frameNames.FirstOrDefault(name => name is not nameof(GetSelectedCards) and not null) ?? "Unknown";
        return new AiCardSelectionContext(kind, prompt, minSelect, maxSelect, minSelect == 0, zone, source, $"OptionCount={options.Count}");
    }

    private static string AppendExtraInfo(string? baseInfo, string addition)
    {
        if (string.IsNullOrWhiteSpace(baseInfo))
        {
            return addition;
        }

        return baseInfo + ";" + addition;
    }

    private static AiCardSelectionKind InferKind(IReadOnlyList<string?> frameNames)
    {
        if (frameNames.Any(name => string.Equals(name, "FromChooseACardScreen", StringComparison.Ordinal))) return AiCardSelectionKind.ChooseACard;
        if (frameNames.Any(name => string.Equals(name, "FromSimpleGridForRewards", StringComparison.Ordinal))) return AiCardSelectionKind.RewardGrid;
        if (frameNames.Any(name => string.Equals(name, "FromDeckForUpgrade", StringComparison.Ordinal))) return AiCardSelectionKind.DeckUpgrade;
        if (frameNames.Any(name => string.Equals(name, "FromDeckForTransformation", StringComparison.Ordinal))) return AiCardSelectionKind.DeckTransform;
        if (frameNames.Any(name => string.Equals(name, "FromDeckForEnchantment", StringComparison.Ordinal))) return AiCardSelectionKind.DeckEnchant;
        if (frameNames.Any(name => string.Equals(name, "FromDeckForRemoval", StringComparison.Ordinal))) return AiCardSelectionKind.DeckRemove;
        if (frameNames.Any(name => string.Equals(name, "FromHandForDiscard", StringComparison.Ordinal))) return AiCardSelectionKind.HandDiscard;
        if (frameNames.Any(name => string.Equals(name, "FromHandForUpgrade", StringComparison.Ordinal))) return AiCardSelectionKind.HandUpgrade;
        if (frameNames.Any(name => string.Equals(name, "FromHand", StringComparison.Ordinal))) return AiCardSelectionKind.HandSelect;
        if (frameNames.Any(name => string.Equals(name, "FromSimpleGrid", StringComparison.Ordinal))) return AiCardSelectionKind.SimpleGrid;
        if (frameNames.Any(name => string.Equals(name, "FromDeckGeneric", StringComparison.Ordinal))) return AiCardSelectionKind.DeckGeneric;
        return AiCardSelectionKind.Unknown;
    }

    private static string InferPrompt(AiCardSelectionKind kind, int minSelect, int maxSelect)
    {
        return kind switch
        {
            AiCardSelectionKind.DeckUpgrade => "Choose cards to upgrade.",
            AiCardSelectionKind.DeckTransform => "Choose cards to transform.",
            AiCardSelectionKind.DeckEnchant => "Choose cards to enchant.",
            AiCardSelectionKind.DeckRemove => "Choose cards to remove.",
            AiCardSelectionKind.HandDiscard => "Choose cards to discard.",
            AiCardSelectionKind.HandUpgrade => "Choose a card in hand to upgrade.",
            AiCardSelectionKind.ChooseACard => "Choose one card.",
            AiCardSelectionKind.RewardGrid => "Choose reward cards.",
            AiCardSelectionKind.SimpleGrid => "Choose cards from the grid.",
            _ => $"Choose {minSelect}-{maxSelect} cards."
        };
    }

    private static string InferZone(AiCardSelectionKind kind)
    {
        return kind switch
        {
            AiCardSelectionKind.HandDiscard or AiCardSelectionKind.HandSelect or AiCardSelectionKind.HandUpgrade => "hand",
            AiCardSelectionKind.RewardGrid => "reward-grid",
            AiCardSelectionKind.ChooseACard => "choice-screen",
            _ => "deck"
        };
    }
}
