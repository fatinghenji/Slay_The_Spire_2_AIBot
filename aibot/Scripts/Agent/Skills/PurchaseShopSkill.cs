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
        return Runtime.IsInitialized;
    }

    public override Task<SkillExecutionResult> ExecuteAsync(AgentSkillParameters? parameters, CancellationToken cancellationToken)
    {
        return Task.FromResult(NotReady("商店购买 Skill 已抽象接入，后续会与 MerchantEntry 节点和购买流程正式绑定。"));
    }
}