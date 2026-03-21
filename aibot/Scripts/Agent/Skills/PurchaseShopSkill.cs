using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using aibot.Scripts.Core;

namespace aibot.Scripts.Agent.Skills;

public sealed class PurchaseShopSkill : RuntimeBackedSkillBase
{
    public PurchaseShopSkill(AiBotRuntime runtime) : base(runtime)
    {
    }

    public override string Name => "purchase_shop";

    public override string Description => "在商店中购买商品或执行移除服务。";

    public override SkillCategory Category => SkillCategory.Economy;

    public override bool CanExecute()
    {
        return GetAbsoluteNodeOrNull<NMerchantRoom>("/root/Game/RootSceneContainer/Run/RoomContainer/MerchantRoom") is not null;
    }

    public override async Task<SkillExecutionResult> ExecuteAsync(AgentSkillParameters? parameters, CancellationToken cancellationToken)
    {
        var room = GetAbsoluteNodeOrNull<NMerchantRoom>("/root/Game/RootSceneContainer/Run/RoomContainer/MerchantRoom");
        if (room is null)
        {
            return new SkillExecutionResult(false, "当前不在商店房间。");
        }

        if (room.Inventory is null)
        {
            return new SkillExecutionResult(false, "当前商店库存不可用。");
        }

        if (!room.Inventory.IsOpen)
        {
            if (room.MerchantButton is null || !room.MerchantButton.IsEnabled)
            {
                return new SkillExecutionResult(false, "当前无法打开商店库存。");
            }

            room.OpenInventory();
            await Task.Delay(Math.Max(0, Runtime.Config.ScreenActionDelayMs), cancellationToken);
        }

        var inventory = room.Inventory.Inventory;
        if (inventory is null)
        {
            return new SkillExecutionResult(false, "当前商店库存数据不可用。");
        }

        var player = inventory.Player;
        var options = inventory.AllEntries
            .Where(entry => entry.IsStocked && entry.EnoughGold)
            .Where(entry => player.HasOpenPotionSlots || entry is not MerchantPotionEntry)
            .ToList();
        if (options.Count == 0)
        {
            return new SkillExecutionResult(false, "当前商店没有可购买的项目。");
        }

        var query = parameters?.ItemName ?? parameters?.OptionId;
        var requestedIndex = ParseRequestedIndex(parameters?.OptionId, options.Count);
        MerchantEntry? selected = requestedIndex is not null
            ? options[requestedIndex.Value]
            : null;
        selected ??= options.FirstOrDefault(entry => MatchesEntryQuery(query, entry));

        if (selected is null && Runtime.DecisionEngine is not null)
        {
            var decision = await Runtime.DecisionEngine.ChooseShopPurchaseAsync(
                options,
                player.Gold,
                player.HasOpenPotionSlots,
                Runtime.GetCurrentAnalysis(),
                cancellationToken);

            selected = decision.Entry;
        }

        if (selected is null)
        {
            return new SkillExecutionResult(false, "没有找到符合条件的商店项目。");
        }

        var entryLabel = GetEntryLabel(selected);
        var success = await TryPurchaseAsync(selected, inventory);
        await WaitForUiActionAsync(cancellationToken);
        return success
            ? new SkillExecutionResult(true, $"已购买：{entryLabel}", $"花费：{selected.Cost} Gold")
            : new SkillExecutionResult(false, $"未能购买：{entryLabel}");
    }

    private static bool MatchesEntryQuery(string? query, MerchantEntry entry)
    {
        return MatchesQuery(query, GetEntryAliases(entry).ToArray());
    }

    private static string GetEntryLabel(MerchantEntry entry)
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

    private static IEnumerable<string?> GetEntryAliases(MerchantEntry entry)
    {
        yield return GetEntryLabel(entry);

        switch (entry)
        {
            case MerchantCardEntry cardEntry when cardEntry.CreationResult?.Card is not null:
                yield return cardEntry.CreationResult.Card.Id.Entry;
                yield return cardEntry.CreationResult.Card.Title;
                break;
            case MerchantRelicEntry relicEntry when relicEntry.Model is not null:
                yield return relicEntry.Model.Id.Entry;
                yield return relicEntry.Model.Title.GetFormattedText();
                break;
            case MerchantPotionEntry potionEntry when potionEntry.Model is not null:
                yield return potionEntry.Model.Id.Entry;
                yield return potionEntry.Model.Title.GetFormattedText();
                break;
            case MerchantCardRemovalEntry:
                yield return "remove";
                yield return "card removal";
                yield return "移除";
                yield return "删卡";
                break;
        }
    }

    private static async Task<bool> TryPurchaseAsync(MerchantEntry entry, MerchantInventory inventory)
    {
        return entry switch
        {
            MerchantCardRemovalEntry removalEntry => await removalEntry.OnTryPurchaseWrapper(inventory, false, false),
            _ => await entry.OnTryPurchaseWrapper(inventory)
        };
    }
}
