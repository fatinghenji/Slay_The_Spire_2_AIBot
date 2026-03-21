using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Screens;
using MegaCrit.Sts2.Core.Nodes.Screens.CardSelection;
using MegaCrit.Sts2.Core.Nodes.Screens.Overlays;
using MegaCrit.Sts2.Core.Runs;
using System.Text.RegularExpressions;
using aibot.Scripts.Agent.Skills;
using aibot.Scripts.Core;

namespace aibot.Scripts.Agent;

public enum ParsedIntentKind
{
    Unknown,
    Skill,
    Tool
}

public sealed record ParsedIntent(
    ParsedIntentKind Kind,
    string Name,
    AgentSkillParameters? Parameters = null,
    string? RawArgument = null);

public sealed class IntentParser
{
    private readonly AiBotRuntime _runtime;
    private readonly AgentSkillRegistry _registry;
    private readonly AgentLlmBridge? _llmBridge;

    public IntentParser(AiBotRuntime runtime, AgentSkillRegistry registry)
    {
        _runtime = runtime;
        _registry = registry;
        _llmBridge = runtime.Config.CanUseCloud ? new AgentLlmBridge(runtime.Config) : null;
    }

    public ParsedIntent Parse(string input)
    {
        var text = (input ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(text))
        {
            return new ParsedIntent(ParsedIntentKind.Unknown, string.Empty);
        }

        var normalized = text.ToLowerInvariant();

        var directTool = _registry.FindToolByName(normalized);
        if (directTool is not null)
        {
            return new ParsedIntent(ParsedIntentKind.Tool, directTool.Name, RawArgument: text);
        }

        var directSkill = _registry.FindSkillByName(normalized);
        if (directSkill is not null)
        {
            return new ParsedIntent(ParsedIntentKind.Skill, directSkill.Name, new AgentSkillParameters());
        }

        if (ContainsAny(normalized, "查看卡组", "卡组", "deck"))
        {
            return new ParsedIntent(ParsedIntentKind.Tool, "inspect_deck", RawArgument: text);
        }

        if (ContainsAny(normalized, "查看遗物", "遗物", "relic"))
        {
            return new ParsedIntent(ParsedIntentKind.Tool, "inspect_relics", RawArgument: text);
        }

        if (ContainsAny(normalized, "查看药水", "药水", "potion") && !ContainsAny(normalized, "使用", "喝", "use"))
        {
            return new ParsedIntent(ParsedIntentKind.Tool, "inspect_potions", RawArgument: text);
        }

        if (ContainsAny(normalized, "敌人", "怪物", "enemy"))
        {
            return new ParsedIntent(ParsedIntentKind.Tool, "inspect_enemies", RawArgument: text);
        }

        if (ContainsAny(normalized, "地图", "路线", "map"))
        {
            return new ParsedIntent(ParsedIntentKind.Tool, "inspect_map", RawArgument: text);
        }

        if (ContainsAny(normalized, "分析", "局势", "run analysis"))
        {
            return new ParsedIntent(ParsedIntentKind.Tool, "analyze_run", RawArgument: text);
        }

        if (normalized.StartsWith("查询卡牌") || normalized.StartsWith("卡牌 ") || normalized.StartsWith("lookup card"))
        {
            var argument = ExtractArgument(text, "查询卡牌", "卡牌", "lookup card");
            return new ParsedIntent(ParsedIntentKind.Tool, "lookup_card", RawArgument: argument);
        }

        if (normalized.StartsWith("查询遗物") || normalized.StartsWith("lookup relic"))
        {
            var argument = ExtractArgument(text, "查询遗物", "lookup relic");
            return new ParsedIntent(ParsedIntentKind.Tool, "lookup_relic", RawArgument: argument);
        }

        if (ContainsAny(normalized, "构筑", "build"))
        {
            var argument = ExtractArgument(text, "查询构筑", "构筑", "build");
            return new ParsedIntent(ParsedIntentKind.Tool, "lookup_build", RawArgument: argument);
        }

        if (ContainsAny(normalized, "结束回合", "end turn"))
        {
            return new ParsedIntent(ParsedIntentKind.Skill, "end_turn", new AgentSkillParameters());
        }

        if (ContainsAny(normalized, "领取奖励", "拿奖励", "claim reward"))
        {
            return new ParsedIntent(ParsedIntentKind.Skill, "claim_reward", new AgentSkillParameters());
        }

        if (ContainsAny(normalized, "打出", "出牌", "play "))
        {
            var cardName = ExtractArgument(text, "打出", "出牌", "play");
            cardName = ResolveHandCardName(cardName) ?? cardName;
            return new ParsedIntent(ParsedIntentKind.Skill, "play_card", new AgentSkillParameters(CardName: cardName));
        }

        if (ContainsAny(normalized, "喝药", "用药水", "使用药水", "use potion"))
        {
            var potionName = ExtractArgument(text, "喝药", "用药水", "使用药水", "use potion");
            potionName = ResolvePotionName(potionName) ?? potionName;
            return new ParsedIntent(ParsedIntentKind.Skill, "use_potion", new AgentSkillParameters(PotionName: potionName));
        }

        if (ContainsAny(normalized, "选卡", "选择奖励卡", "pick card") && NOverlayStack.Instance?.Peek() is not NChooseACardSelectionScreen)
        {
            var cardName = ExtractArgument(text, "选卡", "选择奖励卡", "pick card");
            return new ParsedIntent(ParsedIntentKind.Skill, "pick_card_reward", new AgentSkillParameters(CardName: cardName));
        }

        if (ContainsAny(normalized, "选这张", "选牌", "移除", "升级这张", "变形", "附魔", "select card", "remove card", "upgrade card", "transform card", "enchant card"))
        {
            var cardName = ExtractArgument(text, "选这张", "选牌", "移除", "升级这张", "变形", "附魔", "select card", "remove card", "upgrade card", "transform card", "enchant card");
            var index = ExtractOrdinalIndex(normalized);
            return new ParsedIntent(
                ParsedIntentKind.Skill,
                "select_card",
                new AgentSkillParameters(
                    CardName: cardName,
                    ItemName: cardName,
                    OptionId: BuildIndexOptionId(index) ?? NormalizeChoiceKeyword(cardName)));
        }

        if (ContainsAny(normalized, "选遗物", "拿遗物", "choose relic", "pick relic"))
        {
            var relicName = ExtractArgument(text, "选遗物", "拿遗物", "choose relic", "pick relic");
            var index = ExtractOrdinalIndex(normalized);
            return new ParsedIntent(
                ParsedIntentKind.Skill,
                "choose_relic",
                new AgentSkillParameters(
                    ItemName: relicName,
                    OptionId: BuildIndexOptionId(index) ?? NormalizeChoiceKeyword(relicName)));
        }

        if (ContainsAny(normalized, "卡包", "牌包", "bundle"))
        {
            var argument = ExtractArgument(text, "选卡包", "选牌包", "选bundle", "choose bundle", "bundle");
            var index = ExtractOrdinalIndex(normalized);
            return new ParsedIntent(
                ParsedIntentKind.Skill,
                "choose_bundle",
                new AgentSkillParameters(
                    CardName: argument,
                    BundleIndex: index,
                    OptionId: BuildIndexOptionId(index)));
        }

        if (ContainsAny(normalized, "商店", "购买", "buy ", "shop") && !ContainsAny(normalized, "查看", "看看", "inspect"))
        {
            var itemName = ExtractArgument(text, "买商店", "购买", "买", "buy", "purchase", "shop");
            var index = ExtractOrdinalIndex(normalized);
            return new ParsedIntent(
                ParsedIntentKind.Skill,
                "purchase_shop",
                new AgentSkillParameters(
                    ItemName: itemName,
                    OptionId: BuildIndexOptionId(index) ?? NormalizeChoiceKeyword(itemName)));
        }

        if (ContainsAny(normalized, "营火", "休息点", "休息", "锻造", "smith", "rest"))
        {
            var optionId = InferRestSiteOptionId(normalized);
            var index = ExtractOrdinalIndex(normalized);
            return new ParsedIntent(
                ParsedIntentKind.Skill,
                "rest_site",
                new AgentSkillParameters(
                    OptionId: optionId ?? BuildIndexOptionId(index),
                    ItemName: optionId));
        }

        if (ContainsAny(normalized, "事件选项", "选事件", "事件里选", "choose event"))
        {
            var optionText = ExtractArgument(text, "事件选项", "选事件", "事件里选", "choose event");
            var index = ExtractOrdinalIndex(normalized);
            return new ParsedIntent(
                ParsedIntentKind.Skill,
                "choose_event_option",
                new AgentSkillParameters(
                    ItemName: optionText,
                    OptionId: NormalizeChoiceKeyword(optionText) ?? BuildIndexOptionId(index)));
        }

        if (ContainsAny(normalized, "水晶球", "占卜", "crystal sphere", "divination"))
        {
            var (gridX, gridY) = ExtractGridCoordinates(normalized);
            bool? useBigDivination = normalized.Contains("大占卜", StringComparison.OrdinalIgnoreCase) || normalized.Contains("big", StringComparison.OrdinalIgnoreCase)
                ? true
                : normalized.Contains("小占卜", StringComparison.OrdinalIgnoreCase) || normalized.Contains("small", StringComparison.OrdinalIgnoreCase)
                    ? false
                    : null;

            return new ParsedIntent(
                ParsedIntentKind.Skill,
                "crystal_sphere",
                new AgentSkillParameters(
                    GridX: gridX,
                    GridY: gridY,
                    UseBigDivination: useBigDivination,
                    OptionId: normalized.Contains("继续", StringComparison.OrdinalIgnoreCase) || normalized.Contains("proceed", StringComparison.OrdinalIgnoreCase)
                        ? "proceed"
                        : null));
        }

        if (ContainsAny(normalized, "走左", "左边", "left"))
        {
            return new ParsedIntent(ParsedIntentKind.Skill, "navigate_map", new AgentSkillParameters(MapCol: 0));
        }

        if (ContainsAny(normalized, "走右", "右边", "right"))
        {
            return new ParsedIntent(ParsedIntentKind.Skill, "navigate_map", new AgentSkillParameters(MapCol: 2));
        }

        if (ContainsAny(normalized, "走中", "中间", "middle", "center"))
        {
            return new ParsedIntent(ParsedIntentKind.Skill, "navigate_map", new AgentSkillParameters(MapCol: 1));
        }

        var contextualIntent = TryParseContextualOrdinalIntent(normalized);
        if (contextualIntent is not null)
        {
            return contextualIntent;
        }

        return new ParsedIntent(ParsedIntentKind.Unknown, string.Empty);
    }

    public async Task<ParsedIntent> ParseWithFallbackAsync(string input, CancellationToken cancellationToken)
    {
        var parsed = Parse(input);
        if (parsed.Kind != ParsedIntentKind.Unknown)
        {
            return parsed;
        }

        if (_llmBridge is null)
        {
            return parsed;
        }

        var analysis = _runtime.GetCurrentAnalysis();
        var availableSkills = _registry.GetAvailableSkills(AgentMode.SemiAuto)
            .Select(skill => skill.Name)
            .ToList();
        var recentConversation = AgentCore.Instance.ConversationSessions.BuildTranscript(AgentMode.SemiAuto, 8, input);
        var llmIntent = await _llmBridge.RecognizeSkillIntentAsync(input, analysis, availableSkills, recentConversation, cancellationToken);
        if (llmIntent is null || string.IsNullOrWhiteSpace(llmIntent.SkillName))
        {
            return parsed;
        }

        var skill = _registry.FindSkillByName(llmIntent.SkillName);
        if (skill is null)
        {
            return parsed;
        }

        var normalizedParameters = NormalizeSkillParameters(skill.Name, llmIntent.Parameters);
        return new ParsedIntent(ParsedIntentKind.Skill, skill.Name, normalizedParameters, input);
    }

    public void Dispose()
    {
        _llmBridge?.Dispose();
    }

    private string? ResolveHandCardName(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        var runState = RunManager.Instance.DebugOnlyGetState();
        var player = LocalContext.GetMe(runState);
        if (player is null)
        {
            return raw;
        }

        var analysis = _runtime.GetCurrentAnalysis();
        var matchedGuide = _runtime.KnowledgeBase?.FindCard(raw, analysis.CharacterId) ?? _runtime.KnowledgeBase?.FindCard(raw);
        var normalizedRaw = aibot.Scripts.Knowledge.GuideKnowledgeBase.Normalize(raw);

        return PileType.Hand.GetPile(player).Cards
            .Select(card => new
            {
                card.Title,
                Score = ScoreCardMatch(card, normalizedRaw, matchedGuide)
            })
            .Where(entry => entry.Score > 0)
            .OrderByDescending(entry => entry.Score)
            .Select(entry => entry.Title)
            .FirstOrDefault() ?? raw;
    }

    private string? ResolvePotionName(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        var runState = RunManager.Instance.DebugOnlyGetState();
        var player = LocalContext.GetMe(runState);
        if (player is null)
        {
            return raw;
        }

        var matchedGuide = _runtime.KnowledgeBase?.FindPotion(raw);
        var normalizedRaw = aibot.Scripts.Knowledge.GuideKnowledgeBase.Normalize(raw);

        return player.Potions
            .Select(potion => new
            {
                Name = potion.Id.Entry,
                Score = ScorePotionMatch(potion, normalizedRaw, matchedGuide)
            })
            .Where(entry => entry.Score > 0)
            .OrderByDescending(entry => entry.Score)
            .Select(entry => entry.Name)
            .FirstOrDefault() ?? raw;
    }

    private static bool ContainsAny(string text, params string[] keywords)
    {
        return keywords.Any(keyword => text.Contains(keyword, StringComparison.OrdinalIgnoreCase));
    }

    private static string? ExtractArgument(string input, params string[] keywords)
    {
        foreach (var keyword in keywords)
        {
            var index = input.IndexOf(keyword, StringComparison.OrdinalIgnoreCase);
            if (index >= 0)
            {
                var value = input[(index + keyword.Length)..].Trim(' ', '：', ':', '，', ',');
                if (!string.IsNullOrWhiteSpace(value))
                {
                    return value;
                }
            }
        }

        return null;
    }

    private ParsedIntent? TryParseContextualOrdinalIntent(string normalized)
    {
        var index = ExtractOrdinalIndex(normalized);
        if (index is null)
        {
            return null;
        }

        if (NOverlayStack.Instance?.Peek() is NChooseABundleSelectionScreen)
        {
            return new ParsedIntent(ParsedIntentKind.Skill, "choose_bundle", new AgentSkillParameters(BundleIndex: index, OptionId: BuildIndexOptionId(index)));
        }

        if (NOverlayStack.Instance?.Peek() is NChooseARelicSelection)
        {
            return new ParsedIntent(ParsedIntentKind.Skill, "choose_relic", new AgentSkillParameters(OptionId: BuildIndexOptionId(index)));
        }

        if (GetAbsoluteNodeOrNull<NEventRoom>("/root/Game/RootSceneContainer/Run/RoomContainer/EventRoom") is { Visible: true })
        {
            return new ParsedIntent(ParsedIntentKind.Skill, "choose_event_option", new AgentSkillParameters(OptionId: BuildIndexOptionId(index)));
        }

        if (GetAbsoluteNodeOrNull<NMerchantRoom>("/root/Game/RootSceneContainer/Run/RoomContainer/MerchantRoom") is not null && ContainsAny(normalized, "买", "购买", "shop"))
        {
            return new ParsedIntent(ParsedIntentKind.Skill, "purchase_shop", new AgentSkillParameters(OptionId: BuildIndexOptionId(index)));
        }

        return null;
    }

    private static int? ExtractOrdinalIndex(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        if (ContainsAny(text, "第一个", "第一项", "first")) return 0;
        if (ContainsAny(text, "第二个", "第二项", "second")) return 1;
        if (ContainsAny(text, "第三个", "第三项", "third")) return 2;

        var match = Regex.Match(text, @"第?\s*(\d+)\s*(个|项|option|bundle|relic)?", RegexOptions.IgnoreCase);
        if (!match.Success || !int.TryParse(match.Groups[1].Value, out var number) || number <= 0)
        {
            return null;
        }

        return number - 1;
    }

    private static string? BuildIndexOptionId(int? index)
    {
        return index is null ? null : $"index:{index.Value}";
    }

    private static string? InferRestSiteOptionId(string normalized)
    {
        if (ContainsAny(normalized, "休息", "heal", "回血"))
        {
            return "heal";
        }

        if (ContainsAny(normalized, "锻造", "升级", "smith"))
        {
            return "smith";
        }

        if (ContainsAny(normalized, "mend", "修补"))
        {
            return "mend";
        }

        return null;
    }

    private static string? NormalizeChoiceKeyword(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        var normalized = raw.ToLowerInvariant();
        if (ContainsAny(normalized, "skip", "跳过", "不要"))
        {
            return "skip";
        }

        if (ContainsAny(normalized, "proceed", "继续", "离开", "完成"))
        {
            return "proceed";
        }

        return null;
    }

    private static (int? x, int? y) ExtractGridCoordinates(string text)
    {
        var pairMatch = Regex.Match(text, @"(\d+)\s*[,，xX ]\s*(\d+)");
        if (pairMatch.Success
            && int.TryParse(pairMatch.Groups[1].Value, out var x)
            && int.TryParse(pairMatch.Groups[2].Value, out var y))
        {
            return (x, y);
        }

        return (null, null);
    }

    private static T? GetAbsoluteNodeOrNull<T>(string path) where T : class
    {
        var root = ((Godot.SceneTree)Godot.Engine.GetMainLoop()).Root;
        return root.GetNodeOrNull(path) as T;
    }

    private int ScoreCardMatch(CardModel card, string normalizedRaw, aibot.Scripts.Knowledge.CardGuideEntry? matchedGuide)
    {
        var score = ScoreValue(normalizedRaw, card.Title, card.Id.Entry);
        if (matchedGuide is null)
        {
            return score;
        }

        score = Math.Max(score, ScoreValue(normalizedRaw, matchedGuide.Slug, matchedGuide.NameEn, matchedGuide.NameZh));
        if (GuideMatchesCard(card, matchedGuide))
        {
            score += 50;
        }

        return score;
    }

    private static bool GuideMatchesCard(CardModel card, aibot.Scripts.Knowledge.CardGuideEntry guide)
    {
        var normalizedId = aibot.Scripts.Knowledge.GuideKnowledgeBase.Normalize(card.Id.Entry);
        var normalizedTitle = aibot.Scripts.Knowledge.GuideKnowledgeBase.Normalize(card.Title);
        return normalizedId == aibot.Scripts.Knowledge.GuideKnowledgeBase.Normalize(guide.Slug)
            || normalizedTitle == aibot.Scripts.Knowledge.GuideKnowledgeBase.Normalize(guide.NameEn)
            || normalizedTitle == aibot.Scripts.Knowledge.GuideKnowledgeBase.Normalize(guide.NameZh);
    }

    private int ScorePotionMatch(PotionModel potion, string normalizedRaw, aibot.Scripts.Knowledge.PotionEntry? matchedGuide)
    {
        var score = ScoreValue(normalizedRaw, potion.Id.Entry, potion.Title.GetFormattedText());
        if (matchedGuide is null)
        {
            return score;
        }

        score = Math.Max(score, ScoreValue(normalizedRaw, matchedGuide.Slug, matchedGuide.NameEn, matchedGuide.NameZh));
        var normalizedId = aibot.Scripts.Knowledge.GuideKnowledgeBase.Normalize(potion.Id.Entry);
        if (normalizedId == aibot.Scripts.Knowledge.GuideKnowledgeBase.Normalize(matchedGuide.Slug))
        {
            score += 50;
        }

        return score;
    }

    private static int ScoreValue(string normalizedRaw, params string?[] candidates)
    {
        var score = 0;
        foreach (var candidate in candidates)
        {
            var normalizedCandidate = aibot.Scripts.Knowledge.GuideKnowledgeBase.Normalize(candidate);
            if (string.IsNullOrWhiteSpace(normalizedCandidate))
            {
                continue;
            }

            if (normalizedCandidate == normalizedRaw)
            {
                score = Math.Max(score, 100);
            }
            else if (normalizedCandidate.Contains(normalizedRaw, StringComparison.OrdinalIgnoreCase))
            {
                score = Math.Max(score, 70);
            }
            else if (normalizedRaw.Contains(normalizedCandidate, StringComparison.OrdinalIgnoreCase))
            {
                score = Math.Max(score, 40);
            }
        }

        return score;
    }

    private AgentSkillParameters NormalizeSkillParameters(string skillName, AgentSkillParameters parameters)
    {
        return skillName switch
        {
            "play_card" => parameters with { CardName = ResolveHandCardName(parameters.CardName) ?? parameters.CardName },
            "use_potion" => parameters with { PotionName = ResolvePotionName(parameters.PotionName) ?? parameters.PotionName },
            _ => parameters
        };
    }
}
