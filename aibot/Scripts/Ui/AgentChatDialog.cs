using System.Collections.Concurrent;
using System.Text;
using Godot;
using MegaCrit.Sts2.Core.Logging;
using aibot.Scripts.Agent;
using aibot.Scripts.Core;
using aibot.Scripts.Localization;

namespace aibot.Scripts.Ui;

public sealed partial class AgentChatDialog : CanvasLayer
{
    private static AgentChatDialog? _instance;

    private readonly ConcurrentQueue<(string Role, string Content)> _pendingMessages = new();
    private readonly List<(string Role, string Content)> _messages = new();

    private readonly PanelContainer _panel;
    private readonly Label _title;
    private readonly RichTextLabel _history;
    private readonly LineEdit _input;
    private readonly Button _sendButton;
    private readonly PanelContainer _pendingPanel;
    private readonly Label _pendingLabel;
    private readonly Button _confirmButton;
    private readonly Button _cancelButton;

    private AiBotRuntime? _runtime;
    private AgentMode _mode;
    private bool _chatHotkeyDown;

    public AgentChatDialog()
    {
        Layer = 210;
        ProcessMode = ProcessModeEnum.Always;

        _panel = new PanelContainer();
        _panel.AnchorLeft = 0f;
        _panel.AnchorRight = 0f;
        _panel.AnchorTop = 1f;
        _panel.AnchorBottom = 1f;
        _panel.OffsetLeft = 12f;
        _panel.OffsetRight = 520f;
        _panel.OffsetTop = -420f;
        _panel.OffsetBottom = -12f;
        AddChild(_panel);

        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left", 10);
        margin.AddThemeConstantOverride("margin_right", 10);
        margin.AddThemeConstantOverride("margin_top", 10);
        margin.AddThemeConstantOverride("margin_bottom", 10);
        _panel.AddChild(margin);

        var layout = new VBoxContainer();
        layout.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        margin.AddChild(layout);

        _title = new Label
        {
            HorizontalAlignment = HorizontalAlignment.Left
        };
        layout.AddChild(_title);

        _history = new RichTextLabel
        {
            BbcodeEnabled = false,
            ScrollActive = true,
            FitContent = false,
            SelectionEnabled = true,
            SizeFlagsVertical = Control.SizeFlags.ExpandFill,
            AutowrapMode = TextServer.AutowrapMode.WordSmart
        };
        layout.AddChild(_history);

        var inputRow = new HBoxContainer();
        layout.AddChild(inputRow);

        _input = new LineEdit
        {
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        };
        inputRow.AddChild(_input);

        _sendButton = new Button();
        inputRow.AddChild(_sendButton);

        _sendButton.Pressed += async () => await SubmitAsync();
        _input.TextSubmitted += async _ => await SubmitAsync();

        _pendingPanel = new PanelContainer
        {
            Visible = false
        };
        layout.AddChild(_pendingPanel);

        var pendingMargin = new MarginContainer();
        pendingMargin.AddThemeConstantOverride("margin_left", 8);
        pendingMargin.AddThemeConstantOverride("margin_right", 8);
        pendingMargin.AddThemeConstantOverride("margin_top", 8);
        pendingMargin.AddThemeConstantOverride("margin_bottom", 8);
        _pendingPanel.AddChild(pendingMargin);

        var pendingLayout = new VBoxContainer();
        pendingMargin.AddChild(pendingLayout);

        _pendingLabel = new Label
        {
            Text = string.Empty,
            AutowrapMode = TextServer.AutowrapMode.WordSmart
        };
        pendingLayout.AddChild(_pendingLabel);

        var pendingButtons = new HBoxContainer();
        pendingLayout.AddChild(pendingButtons);

        _confirmButton = new Button();
        _confirmButton.Pressed += async () => await SubmitSpecialCommandAsync("确认执行");
        pendingButtons.AddChild(_confirmButton);

        _cancelButton = new Button();
        _cancelButton.Pressed += async () => await SubmitSpecialCommandAsync("取消执行");
        pendingButtons.AddChild(_cancelButton);

        Visible = false;
    }

    public static void EnsureCreated(AiBotRuntime runtime)
    {
        if (_instance is not null && GodotObject.IsInstanceValid(_instance) && _instance.GetParent() is not null)
        {
            _instance._runtime = runtime;
            _instance.ApplyLanguage();
            return;
        }

        var root = ((SceneTree)Engine.GetMainLoop()).Root;
        _instance = new AgentChatDialog
        {
            Name = "AgentChatDialog",
            _runtime = runtime
        };
        root.AddChild(_instance);
    }

    public static void ShowForMode(AgentMode mode, string? systemMessage = null)
    {
        if (_instance is null)
        {
            return;
        }

        if (_instance._runtime?.Config.Ui.ShowChatDialog == false)
        {
            _instance.Visible = false;
            return;
        }

        _instance._mode = mode;
        _instance.ApplyLanguage();
        if (mode != AgentMode.SemiAuto)
        {
            _instance.ClearPendingActionInternal();
        }

        _instance.Visible = true;
        if (!string.IsNullOrWhiteSpace(systemMessage))
        {
            AgentCore.Instance.ConversationSessions.AddSystemMessage(mode, systemMessage);
        }

        _instance.SyncMessagesFromSession();
        _instance._input.GrabFocus();
    }

    public static void HideDialog()
    {
        if (_instance is null)
        {
            return;
        }

        _instance.Visible = false;
    }

    public static bool IsDialogVisible =>
        _instance is not null &&
        GodotObject.IsInstanceValid(_instance) &&
        _instance.Visible;

    public static void ToggleForMode(AgentMode mode)
    {
        if (_instance is null)
        {
            return;
        }

        if (_instance._runtime?.Config.Ui.ShowChatDialog != true)
        {
            _instance.Visible = false;
            return;
        }

        if (mode is not AgentMode.SemiAuto and not AgentMode.QnA)
        {
            return;
        }

        if (_instance.Visible && _instance._mode == mode)
        {
            HideDialog();
            return;
        }

        ShowForMode(mode);
    }

    public static void ShowPendingAction(string text)
    {
        if (_instance is null)
        {
            return;
        }

        _instance._pendingLabel.Text = text;
        _instance._pendingPanel.Visible = _instance._mode == AgentMode.SemiAuto;
    }

    public static void ClearPendingAction()
    {
        _instance?.ClearPendingActionInternal();
    }

    public override void _Ready()
    {
        base._Ready();
        SetProcess(true);
        SetProcessShortcutInput(true);
        SetProcessUnhandledKeyInput(true);
        if (_runtime is not null)
        {
            _runtime.UiLanguageChanged += OnUiLanguageChanged;
        }

        ApplyLanguage();
    }

    public override void _ExitTree()
    {
        if (_runtime is not null)
        {
            _runtime.UiLanguageChanged -= OnUiLanguageChanged;
        }

        base._ExitTree();
    }

    public override void _Process(double delta)
    {
        base._Process(delta);
        var changed = false;
        while (_pendingMessages.TryDequeue(out var item))
        {
            _messages.Add(item);
            changed = true;
        }

        if (changed)
        {
            RefreshText();
        }

        PollChatHotkey();
    }

    public override void _ShortcutInput(InputEvent @event)
    {
        base._ShortcutInput(@event);
        CaptureFocusedKeyboardInput(@event);
    }

    public override void _UnhandledKeyInput(InputEvent @event)
    {
        base._UnhandledKeyInput(@event);
        CaptureFocusedKeyboardInput(@event);
    }

    public void SyncMessagesFromSession()
    {
        while (_pendingMessages.TryDequeue(out _))
        {
        }

        _messages.Clear();
        foreach (var message in AgentCore.Instance.ConversationSessions.GetMessages(_mode))
        {
            _messages.Add((MapRole(message.Role), message.Content));
        }

        RefreshText();
    }

    private async Task SubmitAsync()
    {
        var text = _input.Text.Trim();
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        _input.Text = string.Empty;
        EnqueueMessage(AiBotText.RoleName(_runtime?.Config, AgentConversationRole.User), text);
        EnqueueMessage(AiBotText.RoleName(_runtime?.Config, AgentConversationRole.System), AiBotText.Pick(_runtime?.Config, "处理中...", "Working..."));

        try
        {
            await AgentCore.Instance.SubmitUserInputAsync(text);
            SyncMessagesFromSession();
        }
        catch (Exception ex)
        {
            AgentCore.Instance.ConversationSessions.AddAgentMessage(_mode, AiBotText.Pick(_runtime?.Config, $"处理失败：{ex.Message}", $"Request failed: {ex.Message}"));
            SyncMessagesFromSession();
        }
    }

    private async Task SubmitSpecialCommandAsync(string command)
    {
        EnqueueMessage(AiBotText.RoleName(_runtime?.Config, AgentConversationRole.User), command);
        EnqueueMessage(AiBotText.RoleName(_runtime?.Config, AgentConversationRole.System), AiBotText.Pick(_runtime?.Config, "处理中...", "Working..."));

        try
        {
            await AgentCore.Instance.SubmitUserInputAsync(command);
            SyncMessagesFromSession();
        }
        catch (Exception ex)
        {
            AgentCore.Instance.ConversationSessions.AddAgentMessage(_mode, AiBotText.Pick(_runtime?.Config, $"处理失败：{ex.Message}", $"Request failed: {ex.Message}"));
            SyncMessagesFromSession();
        }
    }

    private void EnqueueMessage(string role, string content)
    {
        _pendingMessages.Enqueue((role, content));
    }

    private void RefreshText()
    {
        if (_messages.Count == 0)
        {
            _history.Text = AiBotText.Pick(_runtime?.Config,
                "输入消息后，Agent 会在这里回复。",
                "Type a message and the agent will reply here.");
            return;
        }

        var builder = new StringBuilder();
        foreach (var (role, content) in _messages)
        {
            builder.Append('[').Append(role).Append("] ").AppendLine(content);
            builder.AppendLine();
        }

        _history.Text = builder.ToString().TrimEnd();
        _history.ScrollToLine(Math.Max(0, _history.GetLineCount() - 1));
    }

    private void ClearPendingActionInternal()
    {
        _pendingLabel.Text = string.Empty;
        _pendingPanel.Visible = false;
    }

    private string MapRole(AgentConversationRole role)
    {
        return AiBotText.RoleName(_runtime?.Config, role);
    }

    private void PollChatHotkey()
    {
        if (_runtime?.Config.Ui.ShowChatDialog != true)
        {
            _chatHotkeyDown = false;
            return;
        }

        var isPressed = IsHotkeyPressed(_runtime.Config.Ui.ChatHotkey);
        if (Visible && _input.HasFocus())
        {
            _chatHotkeyDown = isPressed;
            return;
        }

        if (isPressed && !_chatHotkeyDown)
        {
            Visible = !Visible;
            Log.Info($"[AiBot.Agent] Chat hotkey detected. Visible={Visible}");
            if (Visible)
            {
                _input.GrabFocus();
            }
        }

        _chatHotkeyDown = isPressed;
    }

    private void ApplyLanguage()
    {
        _title.Text = _mode switch
        {
            AgentMode.SemiAuto => AiBotText.Pick(_runtime?.Config, "Agent 对话 - 半自动", "Agent Chat - Semi Auto"),
            AgentMode.QnA => AiBotText.Pick(_runtime?.Config, "Agent 对话 - 问答", "Agent Chat - QnA"),
            _ => AiBotText.Pick(_runtime?.Config, "Agent 对话", "Agent Chat")
        };

        _input.PlaceholderText = AiBotText.Pick(_runtime?.Config, "输入指令或问题...", "Enter a command or question...");
        _sendButton.Text = AiBotText.Pick(_runtime?.Config, "发送", "Send");
        _confirmButton.Text = AiBotText.Pick(_runtime?.Config, "确认执行", "Confirm");
        _cancelButton.Text = AiBotText.Pick(_runtime?.Config, "取消执行", "Cancel");
        RefreshText();
    }

    private void OnUiLanguageChanged(AiBotLanguage language)
    {
        ApplyLanguage();
    }

    private void CaptureFocusedKeyboardInput(InputEvent @event)
    {
        if (!ShouldCaptureFocusedKeyboardInput(@event))
        {
            return;
        }

        GetViewport().SetInputAsHandled();
    }

    private bool ShouldCaptureFocusedKeyboardInput(InputEvent @event)
    {
        return Visible
            && _input.HasFocus()
            && @event is InputEventKey;
    }

    private static bool IsHotkeyPressed(string? configuredHotkey)
    {
        return TryParseHotkey(configuredHotkey, out var parsed)
            && (Input.IsKeyPressed(parsed) || Input.IsPhysicalKeyPressed(parsed));
    }

    private static bool TryParseHotkey(string? configuredHotkey, out Key parsed)
    {
        parsed = Key.None;
        if (string.IsNullOrWhiteSpace(configuredHotkey))
        {
            return false;
        }

        var normalized = configuredHotkey.Trim().Replace("-", string.Empty).Replace(" ", string.Empty);
        return Enum.TryParse(normalized, true, out parsed);
    }
}
