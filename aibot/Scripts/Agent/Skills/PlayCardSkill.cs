using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using aibot.Scripts.Core;

namespace aibot.Scripts.Agent.Skills;

public sealed class PlayCardSkill : RuntimeBackedSkillBase
{
    public PlayCardSkill(AiBotRuntime runtime) : base(runtime)
    {
    }

    public override string Name => "play_card";

    public override string Description => "在战斗中打出一张可用手牌。";

    public override SkillCategory Category => SkillCategory.Combat;

    public override bool CanExecute()
    {
        var runState = RunManager.Instance.DebugOnlyGetState();
        var player = LocalContext.GetMe(runState);
        return player?.Creature?.CombatState is not null;
    }

    public override async Task<SkillExecutionResult> ExecuteAsync(AgentSkillParameters? parameters, CancellationToken cancellationToken)
    {
        var runState = RunManager.Instance.DebugOnlyGetState();
        var player = LocalContext.GetMe(runState);
        if (player?.Creature?.CombatState is null)
        {
            return new SkillExecutionResult(false, "当前不在可出牌的战斗阶段。");
        }

        var hand = PileType.Hand.GetPile(player).Cards.ToList();
        var playable = hand.Where(CanPlay).ToList();
        if (playable.Count == 0)
        {
            return new SkillExecutionResult(false, "当前没有可打出的手牌。");
        }

        var card = !string.IsNullOrWhiteSpace(parameters?.CardName)
            ? playable.FirstOrDefault(candidate => candidate.Title.Contains(parameters.CardName, StringComparison.OrdinalIgnoreCase))
            : null;
        card ??= playable[0];

        var enemies = player.Creature.CombatState.HittableEnemies?.Where(enemy => enemy.IsAlive).ToList() ?? new List<Creature>();
        var target = ChooseTarget(card, enemies, parameters?.TargetName);
        var success = card.TryManualPlay(target);
        if (!success)
        {
            return new SkillExecutionResult(false, $"未能打出卡牌：{card.Title}");
        }

        var actionExecutor = RunManager.Instance.ActionExecutor;
        if (actionExecutor is not null)
        {
            await actionExecutor.FinishedExecutingActions().WaitAsync(cancellationToken);
        }

        return new SkillExecutionResult(true, $"已执行出牌：{card.Title}", target is null ? null : $"目标：{target.Name}");
    }

    private static bool CanPlay(CardModel card)
    {
        AbstractModel? preventer;
        UnplayableReason reason;
        return card.CanPlay(out reason, out preventer);
    }

    private static Creature? ChooseTarget(CardModel card, IReadOnlyList<Creature> enemies, string? targetName)
    {
        if (!string.IsNullOrWhiteSpace(targetName))
        {
            var explicitTarget = enemies.FirstOrDefault(enemy => enemy.Name.Contains(targetName, StringComparison.OrdinalIgnoreCase));
            if (explicitTarget is not null)
            {
                return explicitTarget;
            }
        }

        return card.TargetType switch
        {
            TargetType.AnyEnemy => enemies.OrderBy(enemy => enemy.CurrentHp).FirstOrDefault(),
            _ => null
        };
    }
}
