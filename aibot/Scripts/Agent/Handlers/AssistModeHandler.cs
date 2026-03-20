using MegaCrit.Sts2.Core.Logging;
using aibot.Scripts.Core;
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
        return Task.FromResult("辅助模式已开启：我会在关键决策点贴出“推荐”标签，但不会自动点击执行。当前首版支持卡牌奖励、遗物选择和地图选路推荐。");
    }

    public void Dispose()
    {
    }
}
