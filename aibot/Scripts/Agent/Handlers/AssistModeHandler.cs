using MegaCrit.Sts2.Core.Logging;
using aibot.Scripts.Core;
using aibot.Scripts.Localization;
using aibot.Scripts.Ui;

namespace aibot.Scripts.Agent.Handlers;

public sealed class AssistModeHandler : IAgentModeHandler
{
    private readonly AiBotRuntime _runtime;
    private readonly string _activationReason;

    public AssistModeHandler(AiBotRuntime runtime, string activationReason)
    {
        _runtime = runtime;
        _activationReason = activationReason;
    }

    public AgentMode Mode => AgentMode.Assist;

    public Task OnActivateAsync(CancellationToken cancellationToken)
    {
        _runtime.DeactivateLegacyFullAuto();
        AgentRecommendOverlay.EnsureCreated(_runtime);
        AgentRecommendOverlay.ShowOverlay();
        Log.Info($"[AiBot.Agent] Assist mode entered. Reason={_activationReason}");
        return Task.CompletedTask;
    }

    public Task OnDeactivateAsync()
    {
        AgentRecommendOverlay.HideOverlay();
        return Task.CompletedTask;
    }

    public Task OnTickAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public Task<string> OnUserInputAsync(string input, CancellationToken cancellationToken)
    {
        return Task.FromResult(AiBotText.Pick(
            _runtime.Config,
            "辅助模式已开启：我会在关键决策点贴出推荐标签，但不会自动点击执行。当前支持战斗出牌、卡牌奖励、奖励领取、bundle、遗物、宝箱遗物、地图路线、商店、篝火和事件选项推荐。",
            "Assist mode is active: I will place recommendation badges on major decision points without clicking for you. Current coverage includes combat plays, card rewards, reward claims, bundles, relics, treasure-room relics, map routes, shops, rest sites, and event options."));
    }

    public void Dispose()
    {
    }
}
