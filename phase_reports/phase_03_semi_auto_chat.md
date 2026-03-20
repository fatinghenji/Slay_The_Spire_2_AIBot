# Phase 03 — SemiAuto 模式消费能力 + 聊天窗口骨架

## 已完成内容

### 1. 本地意图解析器已落地
已新增：

- `aibot/Scripts/Agent/IntentParser.cs`

本阶段实现了一个本地优先的 `IntentParser`，负责把玩家自然语言输入解析成：

- `ParsedIntentKind.Skill`
- `ParsedIntentKind.Tool`
- `ParsedIntentKind.Unknown`

并输出：

- 对应的 `Skill` 名称
- 对应的 `Tool` 名称
- `AgentSkillParameters`
- 原始参数文本

当前已支持的典型解析场景包括：

### Tool 类输入
- `查看卡组`
- `查看遗物`
- `查看药水`
- `查看敌人`
- `查看地图`
- `分析局势`
- `查询卡牌 xxx`
- `查询遗物 xxx`
- `查询构筑 xxx`

### Skill 类输入
- `结束回合`
- `领取奖励`
- `打出 xxx`
- `出牌 xxx`
- `使用药水 xxx`
- `喝药 xxx`
- `选卡 xxx`
- `走左边 / 走中间 / 走右边`

同时还加入了简单的名称补全能力：

- 对出牌输入会尝试从当前手牌中匹配真实卡名
- 对药水输入会尝试从当前药水列表中匹配真实药水名

这意味着从本阶段开始，半自动模式已经具备了“自然语言 → 已注册白名单能力”的第一版本地解析链路。

---

### 2. `SemiAutoModeHandler` 已真正消费 `Registry`
已更新：

- `aibot/Scripts/Agent/Handlers/SemiAutoModeHandler.cs`

本阶段完成后，`SemiAutoModeHandler` 不再只是占位，而是已经具备完整的基础执行流程：

1. 接收玩家输入
2. 调用 `IntentParser.Parse()`
3. 区分是 `Tool` 调用、`Skill` 执行还是无法识别
4. 通过 `AgentCore.Instance.Registry` 查找白名单能力
5. 执行 Tool 查询或 Skill 执行
6. 将结果返回到聊天 UI
7. 将过程写入 `AiBotDecisionFeed`

已完成的关键能力：

- Tool 查询结果可直接返回
- Skill 可执行时可直接执行
- Skill 不可执行时给出明确反馈
- 输入无法识别时给出示例提示
- 所有过程进入决策日志 Feed，方便后续 UI 和调试面板统一查看

这标志着半自动模式已经从“模式骨架”升级为“真正可驱动 Agent 能力集合的模式”。

---

### 3. 聊天窗口 UI 骨架已落地
已新增：

- `aibot/Scripts/Ui/AgentChatDialog.cs`

当前聊天窗口具备以下能力：

- 独立 `CanvasLayer` UI
- 标题栏根据模式动态显示
- 历史消息面板
- 输入框
- 发送按钮
- 回车发送
- 支持 `Tab` 热键显示/隐藏
- 消息角色区分：
  - 你
  - Agent
  - 系统

当前接入方式：

- `AgentCore.Initialize()` 时确保窗口被创建
- `SemiAutoModeHandler.OnActivateAsync()` 会显示聊天窗口并注入系统提示语
- `QnAModeHandler.OnActivateAsync()` 会显示同一聊天窗口并注入问答模式提示语
- 对话提交通过 `AgentCore.Instance.SubmitUserInputAsync()` 统一转发到当前模式处理器

这为后续：

- 半自动模式的执行确认
- 问答模式的知识检索回复
- 模式切换面板联动

打下了直接可用的 UI 基础。

---

### 4. 配置已加入聊天窗口控制项
已更新：

- `aibot/Scripts/Config/AiBotConfig.cs`
- `aibot/config.json`

新增配置：

```json
"ui": {
  "showChatDialog": true,
  "chatHotkey": "Tab"
}
```

当前已接入：

- 是否显示聊天窗口
- 聊天窗口热键（当前逻辑已实现 `Tab`）

这也为后续扩展：

- 模式面板显示配置
- 更多快捷键配置

预留了配置结构位置。

---

### 5. `AgentCore` 已接入聊天窗口初始化
已更新：

- `aibot/Scripts/Agent/AgentCore.cs`

本阶段增加了：

- 在 `Initialize()` 中调用 `AgentChatDialog.EnsureCreated(runtime)`

这样能够保证聊天窗口与 Agent 生命周期保持一致，不需要等到后续某个模式第一次激活时再手动构建整个 UI 树。

---

### 6. 第三阶段已完成编译验证
已执行：

- `dotnet build aibot\aibot.csproj -c Release`

结果：

- 构建成功
- 当前无新增编译错误

---

## 未完成内容

### 1. 半自动模式尚未实现“执行前确认”
当前设计里，半自动模式已经可以：

- 解析指令
- 查找 Skill
- 直接执行

但还没有补上原计划中的：

- 执行前确认按钮
- “已识别操作，是否执行？”二次确认交互

因此当前更接近“轻量直接执行版半自动”。

### 2. `IntentParser` 仍是本地规则优先版本
当前只实现了本地关键词与简单名称匹配，没有实现：

- LLM 受限意图识别
- 更复杂的多轮上下文解析
- 更多游戏术语/缩写/中英文混合命令理解

这意味着当前第三阶段的解析器已经能满足最常用指令，但还不够智能。

### 3. 问答模式仍然是 UI 已接上、能力未接上
本阶段让 `QnAModeHandler` 使用了同一聊天窗口，但它仍然只返回占位回复，尚未完成：

- 本地知识检索
- Tool 优先查询
- 受限问答文本整合
- 游戏外问题拒答

### 4. FullAuto 仍未通过 Skill 逐步执行
虽然第三阶段已经让 `SemiAuto` 真正消费了 `Registry`，但 `FullAuto` 仍然是通过旧 `AiBotRuntime` 兼容入口驱动，而不是逐步把动作切换到 Skill 执行。

---

## 遇到的问题

### 问题 1：聊天窗口需要与 Agent 生命周期协同
如果在模式 handler 内临时创建对话框，容易出现：

- 重复创建
- 模式切换后 UI 残留
- 消息状态丢失

因此本阶段将其上提到 `AgentCore.Initialize()` 时创建，以保证 UI 生命周期更稳定。

### 问题 2：本地解析与真实游戏对象命名并不总是完全一致
例如：

- 玩家输入的是中文简称
- 手牌中的真实 `Title` 与玩家输入不完全一致
- 药水内部标识与玩家习惯叫法不同

本阶段已经通过“从当前手牌/药水列表中做一次本地模糊匹配”缓解了问题，但仍不够强。

### 问题 3：当前聊天窗口还不是最终 UX 形态
目前是一个可用骨架，但还没有：

- 执行确认按钮
- 更丰富的消息卡片样式
- 模式切换按钮
- 位置拖拽
- 消息类型高亮

这属于后续 UI 阶段需要继续增强的内容。

---

## 解决方案与后续建议

### 对问题 1 的解决方案
继续保持：

- UI 由 Agent Core 统一创建
- 模式 handler 只负责显示/隐藏和注入模式说明

后续如果增加模式面板、推荐标签层，也建议遵循同样做法。

### 对问题 2 的解决方案
下一阶段可继续增强 `IntentParser`：

- 引入更多关键词规则
- 支持手牌/药水/遗物/地图节点的更精细匹配
- 增加 Tool 优先检索建议
- 再下一步接入 LLM 受限意图识别作为 fallback

### 对问题 3 的解决方案
后续 UI 阶段可以进一步给 `AgentChatDialog` 增加：

- 执行确认按钮
- 固定系统提示区
- 可折叠/拖动
- 模式标签和状态显示
- 与 `AgentModePanel` 联动

---

## 下一阶段建议目标

建议第四阶段开始推进：

1. 为问答模式接入 Tool 优先知识查询
2. 新建 `KnowledgeSearchEngine`
3. 让 `QnAModeHandler` 真正回答知识库范围内问题
4. 增加游戏边界拒答机制
5. 视情况开始补 `AgentModePanel` 或辅助模式推荐标签

这样第四阶段完成后，四种模式中的“半自动”和“问答”都会开始具备真实可用性。
