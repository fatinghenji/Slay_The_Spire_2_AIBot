using MegaCrit.Sts2.Core.Logging;
using aibot.Scripts.Core;
using aibot.Scripts.Ui;

namespace aibot.Scripts.Agent.Handlers;

public sealed class QnAModeHandler : IAgentModeHandler
{
    private readonly AiBotRuntime _runtime;
    private readonly string _activationReason;

    public QnAModeHandler(AiBotRuntime runtime, string activationReason)
    {
        _runtime = runtime;
        _activationReason = activationReason;
    }

    public AgentMode Mode => AgentMode.QnA;

    public Task OnActivateAsync(CancellationToken cancellationToken)
    {
        _runtime.DeactivateLegacyFullAuto();
        AgentChatDialog.EnsureCreated(_runtime);
        AgentChatDialog.ShowForMode(Mode, "问答模式已开启：当前阶段优先提供模式占位回复，后续会接知识检索与受限问答。 ");
        Log.Info($"[AiBot.Agent] QnA mode entered. Reason={_activationReason}");
        return Task.CompletedTask;
    }

    public Task OnDeactivateAsync()
    {
        AgentChatDialog.HideDialog();
        return Task.CompletedTask;
    }

    public Task OnTickAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public Task<string> OnUserInputAsync(string input, CancellationToken cancellationToken)
    {
        return Task.FromResult("问答模式骨架已接入。后续阶段会补充知识检索、会话管理和游戏边界过滤。");
    }

    public void Dispose()
    {
    }
}
