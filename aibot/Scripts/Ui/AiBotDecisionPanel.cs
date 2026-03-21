using System.Collections.Concurrent;
using System.Text;
using Godot;
using MegaCrit.Sts2.Core.Helpers;
using aibot.Scripts.Agent;
using aibot.Scripts.Core;
using aibot.Scripts.Localization;

namespace aibot.Scripts.Ui;

public sealed partial class AiBotDecisionPanel : CanvasLayer
{
    private const float ExpandedTop = -250f;
    private const float ExpandedBottom = 250f;
    private const float CollapsedTop = -36f;
    private const float CollapsedBottom = 36f;

    private readonly ConcurrentQueue<AiBotDecisionFeedEntry> _pendingEntries = new();
    private readonly List<AiBotDecisionFeedEntry> _entries = new();
    private readonly Dictionary<AgentMode, Button> _modeButtons = new();

    private readonly AiBotRuntime _runtime = AiBotRuntime.Instance;
    private readonly PanelContainer _panel;
    private readonly Label _title;
    private readonly Label _currentMode;
    private readonly OptionButton _languageSelector;
    private readonly VBoxContainer _body;
    private readonly RichTextLabel _content;
    private readonly Label _statusLabel;
    private readonly Button _chatToggleButton;
    private readonly PanelContainer _confirmPanel;
    private readonly Label _confirmLabel;
    private readonly Button _confirmYesButton;
    private readonly Button _confirmNoButton;
    private readonly Button _toggleButton;

    private int _maxEntries;
    private bool _isCollapsed;
    private AgentModeChangeRequest? _pendingRequest;

    public AiBotDecisionPanel(int maxEntries = 16)
    {
        Layer = 200;
        ProcessMode = ProcessModeEnum.Always;
        _maxEntries = Math.Max(4, maxEntries);

        _panel = new PanelContainer();
        _panel.AnchorLeft = 1f;
        _panel.AnchorRight = 1f;
        _panel.AnchorTop = 0.5f;
        _panel.AnchorBottom = 0.5f;
        _panel.OffsetLeft = -460f;
        _panel.OffsetRight = -12f;
        _panel.OffsetTop = ExpandedTop;
        _panel.OffsetBottom = ExpandedBottom;
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

        var header = new HBoxContainer();
        layout.AddChild(header);

        _title = new Label
        {
            HorizontalAlignment = HorizontalAlignment.Left,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        };
        header.AddChild(_title);

        _currentMode = new Label
        {
            HorizontalAlignment = HorizontalAlignment.Right
        };
        header.AddChild(_currentMode);

        _toggleButton = new Button();
        _toggleButton.Pressed += ToggleCollapsed;
        header.AddChild(_toggleButton);

        _languageSelector = new OptionButton();
        _languageSelector.AddItem("中文", 0);
        _languageSelector.AddItem("English", 1);
        _languageSelector.ItemSelected += OnLanguageSelected;
        layout.AddChild(_languageSelector);

        _body = new VBoxContainer
        {
            SizeFlagsVertical = Control.SizeFlags.ExpandFill
        };
        layout.AddChild(_body);

        _content = new RichTextLabel
        {
            BbcodeEnabled = false,
            ScrollActive = true,
            FitContent = false,
            SelectionEnabled = true,
            SizeFlagsVertical = Control.SizeFlags.ExpandFill,
            AutowrapMode = TextServer.AutowrapMode.WordSmart
        };
        _body.AddChild(_content);

        _statusLabel = new Label
        {
            AutowrapMode = TextServer.AutowrapMode.WordSmart
        };
        layout.AddChild(_statusLabel);

        var buttonGrid = new GridContainer
        {
            Columns = 2
        };
        layout.AddChild(buttonGrid);

        foreach (var mode in new[] { AgentMode.FullAuto, AgentMode.SemiAuto, AgentMode.Assist, AgentMode.QnA })
        {
            var button = new Button
            {
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
            };
            button.Pressed += () => TaskHelper.RunSafely(RequestModeSwitchAsync(mode));
            buttonGrid.AddChild(button);
            _modeButtons[mode] = button;
        }

        _chatToggleButton = new Button
        {
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        };
        _chatToggleButton.Pressed += ToggleChatDialog;
        layout.AddChild(_chatToggleButton);

        _confirmPanel = new PanelContainer
        {
            Visible = false
        };
        layout.AddChild(_confirmPanel);

        var confirmMargin = new MarginContainer();
        confirmMargin.AddThemeConstantOverride("margin_left", 8);
        confirmMargin.AddThemeConstantOverride("margin_right", 8);
        confirmMargin.AddThemeConstantOverride("margin_top", 8);
        confirmMargin.AddThemeConstantOverride("margin_bottom", 8);
        _confirmPanel.AddChild(confirmMargin);

        var confirmLayout = new VBoxContainer();
        confirmMargin.AddChild(confirmLayout);

        _confirmLabel = new Label
        {
            Text = string.Empty,
            AutowrapMode = TextServer.AutowrapMode.WordSmart
        };
        confirmLayout.AddChild(_confirmLabel);

        var confirmButtons = new HBoxContainer();
        confirmLayout.AddChild(confirmButtons);

        _confirmYesButton = new Button();
        _confirmYesButton.Pressed += () => TaskHelper.RunSafely(ConfirmPendingRequestAsync());
        confirmButtons.AddChild(_confirmYesButton);

        _confirmNoButton = new Button();
        _confirmNoButton.Pressed += CancelPendingRequest;
        confirmButtons.AddChild(_confirmNoButton);
    }

    public override void _Ready()
    {
        base._Ready();
        AiBotDecisionFeed.EntryAdded += OnEntryAdded;
        AgentCore.Instance.ModeChangeRequested += OnModeChangeRequested;
        AgentCore.Instance.ModeChanged += OnModeChanged;
        _runtime.UiLanguageChanged += OnUiLanguageChanged;

        foreach (var entry in AiBotDecisionFeed.GetEntries())
        {
            _entries.Add(entry);
        }

        UpdateModeLabel(AgentCore.Instance.CurrentMode);
        UpdateModeButtons(AgentCore.Instance.CurrentMode);
        ApplyLanguage();
        ApplyCollapsedState();
        RefreshText();
    }

    public override void _ExitTree()
    {
        AiBotDecisionFeed.EntryAdded -= OnEntryAdded;
        AgentCore.Instance.ModeChangeRequested -= OnModeChangeRequested;
        AgentCore.Instance.ModeChanged -= OnModeChanged;
        _runtime.UiLanguageChanged -= OnUiLanguageChanged;
        base._ExitTree();
    }

    public override void _Process(double delta)
    {
        var changed = false;
        while (_pendingEntries.TryDequeue(out var entry))
        {
            _entries.Add(entry);
            changed = true;
        }

        if (changed)
        {
            while (_entries.Count > _maxEntries)
            {
                _entries.RemoveAt(0);
            }

            RefreshText();
        }
    }

    public void SetMaxEntries(int maxEntries)
    {
        _maxEntries = Math.Max(4, maxEntries);
        while (_entries.Count > _maxEntries)
        {
            _entries.RemoveAt(0);
        }

        RefreshText();
    }

    private void OnEntryAdded(AiBotDecisionFeedEntry entry)
    {
        _pendingEntries.Enqueue(entry);
    }

    private void OnModeChanged(AgentMode mode)
    {
        UpdateModeLabel(mode);
        UpdateModeButtons(mode);
        UpdateChatToggleButton(mode);
        _pendingRequest = null;
        _confirmPanel.Visible = false;
        SetStatus(AiBotText.Pick(_runtime.Config, $"已切换到 {GetModeDisplayName(mode)}。", $"Switched to {GetModeDisplayName(mode)}."), false);
    }

    private void OnModeChangeRequested(AgentModeChangeRequest request)
    {
        _pendingRequest = request;
        _confirmLabel.Text = AiBotText.Pick(_runtime.Config,
            $"要从 {GetModeDisplayName(request.CurrentMode)} 切换到 {GetModeDisplayName(request.RequestedMode)} 吗？\n原因：{request.Reason}",
            $"Switch from {GetModeDisplayName(request.CurrentMode)} to {GetModeDisplayName(request.RequestedMode)}?\nReason: {request.Reason}");
        _confirmPanel.Visible = !_isCollapsed && request.RequiresConfirmation;
        SetStatus(AiBotText.Pick(_runtime.Config, $"等待确认切换到 {GetModeDisplayName(request.RequestedMode)}。", $"Awaiting confirmation for {GetModeDisplayName(request.RequestedMode)}."), false);
    }

    private void RefreshText()
    {
        if (_entries.Count == 0)
        {
            _content.Text = AiBotText.Pick(_runtime.Config, "等待 Agent 决策记录...", "Waiting for agent decisions...");
            return;
        }

        var builder = new StringBuilder();
        foreach (var entry in _entries.OrderByDescending(e => e.Timestamp))
        {
            builder.Append('[').Append(entry.Timestamp.ToString("HH:mm:ss")).Append("] ");
            builder.Append('[').Append(entry.Source).Append("] ");
            builder.Append('[').Append(entry.Category).Append("] ").AppendLine(entry.Summary);
            builder.AppendLine(entry.Details);
            builder.AppendLine();
        }

        _content.Text = builder.ToString().TrimEnd();
        _content.ScrollToLine(0);
    }

    private void ToggleCollapsed()
    {
        _isCollapsed = !_isCollapsed;
        ApplyCollapsedState();
    }

    private void ApplyCollapsedState()
    {
        _languageSelector.Visible = !_isCollapsed;
        _body.Visible = !_isCollapsed;
        _statusLabel.Visible = !_isCollapsed;
        foreach (var button in _modeButtons.Values)
        {
            button.Visible = !_isCollapsed;
        }

        _chatToggleButton.Visible = !_isCollapsed;
        _confirmPanel.Visible = !_isCollapsed && _pendingRequest is not null && _pendingRequest.RequiresConfirmation;
        _toggleButton.Text = AiBotText.Pick(_runtime.Config, _isCollapsed ? "展开" : "收起", _isCollapsed ? "Expand" : "Collapse");
        _panel.OffsetTop = _isCollapsed ? CollapsedTop : ExpandedTop;
        _panel.OffsetBottom = _isCollapsed ? CollapsedBottom : ExpandedBottom;
    }

    private void UpdateModeLabel(AgentMode mode)
    {
        _currentMode.Text = AiBotText.Pick(_runtime.Config, $"模式：{GetModeDisplayName(mode)}", $"Mode: {GetModeDisplayName(mode)}");
    }

    private async Task RequestModeSwitchAsync(AgentMode mode)
    {
        if (AgentCore.Instance.CurrentMode == mode)
        {
            SetStatus(AiBotText.Pick(_runtime.Config, $"当前已经是 {GetModeDisplayName(mode)}。", $"Already in {GetModeDisplayName(mode)}."), false);
            return;
        }

        SetStatus(AiBotText.Pick(_runtime.Config, $"正在切换到 {GetModeDisplayName(mode)}...", $"Switching to {GetModeDisplayName(mode)}..."), false);
        var changed = await AgentCore.Instance.SwitchModeAsync(mode, $"decision-panel:{mode}");
        if (changed)
        {
            SetStatus(AiBotText.Pick(_runtime.Config, $"已切换到 {GetModeDisplayName(mode)}。", $"Switched to {GetModeDisplayName(mode)}."), false);
        }
    }

    private async Task ConfirmPendingRequestAsync()
    {
        if (_pendingRequest is null)
        {
            return;
        }

        var request = _pendingRequest;
        _pendingRequest = null;
        _confirmPanel.Visible = false;
        SetStatus(AiBotText.Pick(_runtime.Config, $"正在确认切换到 {GetModeDisplayName(request.RequestedMode)}...", $"Confirming {GetModeDisplayName(request.RequestedMode)}..."), false);
        var changed = await AgentCore.Instance.SwitchModeAsync(request.RequestedMode, request.Reason + ":confirmed", true);
        if (!changed)
        {
            SetStatus(AiBotText.Pick(_runtime.Config, $"切换到 {GetModeDisplayName(request.RequestedMode)} 失败。", $"Failed to switch to {GetModeDisplayName(request.RequestedMode)}."), true);
        }
    }

    private void CancelPendingRequest()
    {
        _pendingRequest = null;
        _confirmPanel.Visible = false;
        SetStatus(AiBotText.Pick(_runtime.Config, "已取消模式切换。", "Mode switch canceled."), false);
    }

    private void ToggleChatDialog()
    {
        var mode = AgentCore.Instance.CurrentMode;
        if (mode is not AgentMode.SemiAuto and not AgentMode.QnA)
        {
            SetStatus(AiBotText.Pick(_runtime.Config, "聊天仅在半自动和问答模式中可用。", "Chat is only available in Semi Auto and QnA modes."), true);
            return;
        }

        AgentChatDialog.ToggleForMode(mode);
        UpdateChatToggleButton(mode);
        SetStatus(AgentChatDialog.IsDialogVisible
            ? AiBotText.Pick(_runtime.Config, "聊天窗口已打开。", "Chat opened.")
            : AiBotText.Pick(_runtime.Config, "聊天窗口已隐藏。", "Chat hidden."), false);
    }

    private void UpdateModeButtons(AgentMode activeMode)
    {
        foreach (var pair in _modeButtons)
        {
            pair.Value.Disabled = pair.Key == activeMode;
            pair.Value.Text = GetModeDisplayName(pair.Key);
        }
    }

    private void UpdateChatToggleButton(AgentMode mode)
    {
        var supported = mode is AgentMode.SemiAuto or AgentMode.QnA;
        _chatToggleButton.Disabled = !supported;
        _chatToggleButton.Text = supported
            ? (AgentChatDialog.IsDialogVisible
                ? AiBotText.Pick(_runtime.Config, "隐藏聊天", "Hide Chat")
                : AiBotText.Pick(_runtime.Config, "打开聊天", "Open Chat"))
            : AiBotText.Pick(_runtime.Config, "聊天（仅半自动 / 问答）", "Chat (SemiAuto / QnA only)");
    }

    private void SetStatus(string text, bool isError)
    {
        _statusLabel.Text = text;
        _statusLabel.Modulate = isError ? new Color(1f, 0.7f, 0.7f) : Colors.White;
    }

    private void ApplyLanguage()
    {
        _title.Text = AiBotText.Pick(_runtime.Config, "AiBot 控制面板", "AiBot Control Panel");
        _languageSelector.Select(_runtime.Config.Ui.GetLanguage() == AiBotLanguage.English ? 1 : 0);
        UpdateModeLabel(AgentCore.Instance.CurrentMode);
        UpdateModeButtons(AgentCore.Instance.CurrentMode);
        UpdateChatToggleButton(AgentCore.Instance.CurrentMode);
        _confirmYesButton.Text = AiBotText.Pick(_runtime.Config, "确认", "Confirm");
        _confirmNoButton.Text = AiBotText.Pick(_runtime.Config, "取消", "Cancel");

        if (string.IsNullOrWhiteSpace(_statusLabel.Text))
        {
            _statusLabel.Text = AiBotText.Pick(_runtime.Config, "使用下面的按钮切换模式。", "Use the buttons below to switch modes.");
        }

        ApplyCollapsedState();
        RefreshText();
    }

    private void OnLanguageSelected(long index)
    {
        var language = index == 1 ? AiBotLanguage.English : AiBotLanguage.Chinese;
        _runtime.SetUiLanguage(language);
    }

    private void OnUiLanguageChanged(AiBotLanguage language)
    {
        ApplyLanguage();
    }

    private string GetModeDisplayName(AgentMode mode)
    {
        return mode switch
        {
            AgentMode.FullAuto => AiBotText.Pick(_runtime.Config, "全自动", "Full Auto"),
            AgentMode.SemiAuto => AiBotText.Pick(_runtime.Config, "半自动", "Semi Auto"),
            AgentMode.Assist => AiBotText.Pick(_runtime.Config, "辅助", "Assist"),
            AgentMode.QnA => "QnA",
            _ => mode.ToString()
        };
    }
}
