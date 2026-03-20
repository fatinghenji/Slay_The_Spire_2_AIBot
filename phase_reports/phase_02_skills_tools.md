# Phase 02 — Skills / Tools 能力抽象层

## 已完成内容

### 1. Skill 抽象层已落地
已新增：

- `aibot/Scripts/Agent/Skills/IAgentSkill.cs`
- `aibot/Scripts/Agent/Skills/RuntimeBackedSkillBase.cs`

本阶段正式引入了 Agent 的“可执行能力”模型，统一了：

- Skill 分类 `SkillCategory`
- Skill 参数模型 `AgentSkillParameters`
- Skill 执行结果模型 `SkillExecutionResult`
- 统一接口 `IAgentSkill`

这样后续：

- 全自动模式
- 半自动模式
- 意图解析器
- LLM 受限调用

都可以围绕同一组白名单技能进行工作，而不必直接耦合到 `AiBotRuntime` 的私有流程中。

### 2. 第二阶段已补齐初版 Skill 集合
已新增下列 Skill 文件：

- `PlayCardSkill`
- `UsePotionSkill`
- `EndTurnSkill`
- `NavigateMapSkill`
- `PickCardRewardSkill`
- `SelectCardSkill`
- `ChooseBundleSkill`
- `ChooseRelicSkill`
- `CrystalSphereSkill`
- `PurchaseShopSkill`
- `RestSiteSkill`
- `ChooseEventOptionSkill`
- `ClaimRewardSkill`

其中本阶段已实现“可直接执行”的能力包括：

- `PlayCardSkill`
- `UsePotionSkill`
- `EndTurnSkill`
- `NavigateMapSkill`
- `PickCardRewardSkill`
- `ClaimRewardSkill`

这些 Skill 已能够在当前运行时状态中直接调用游戏内已有接口完成实际操作。

其中本阶段以“安全骨架”形式接入的能力包括：

- `SelectCardSkill`
- `ChooseBundleSkill`
- `ChooseRelicSkill`
- `CrystalSphereSkill`
- `PurchaseShopSkill`
- `RestSiteSkill`
- `ChooseEventOptionSkill`

这些 Skill 已完成：

- 正式命名
- 白名单注册
- 类型归类
- 统一执行接口接入

但复杂界面节点绑定和执行细节仍将在后续阶段继续补完。

### 3. Tool 抽象层已落地
已新增：

- `aibot/Scripts/Agent/Tools/IAgentTool.cs`
- `aibot/Scripts/Agent/Tools/RuntimeBackedToolBase.cs`

并完成以下 Tool：

- `InspectDeckTool`
- `InspectRelicsTool`
- `InspectPotionsTool`
- `InspectEnemyTool`
- `InspectMapTool`
- `LookupCardTool`
- `LookupRelicTool`
- `LookupBuildTool`
- `CalculateDamageTool`
- `AnalyzeRunTool`

这批 Tool 已可直接使用当前：

- `RunAnalysis`
- `GuideKnowledgeBase`
- 现有构筑、卡牌、遗物知识库

输出只读结果。

这意味着从本阶段开始，问答模式和半自动模式已经拥有一套正式的只读查询能力白名单，而不需要直接暴露底层运行时对象。

### 4. Agent 注册表已接入 `AgentCore`
已新增：

- `aibot/Scripts/Agent/AgentSkillRegistry.cs`

并在 `AgentCore.Initialize()` 中正式注册：

- 全部初版 Skills
- 全部初版 Tools

当前 `AgentCore.Registry` 已成为后续所有模式共享的能力白名单中心，具备：

- `RegisterSkill`
- `RegisterTool`
- `FindSkillByName`
- `FindToolByName`
- `GetAvailableSkills(mode)`
- `GetAvailableTools(mode)`
- `GetSkillDescriptions()`
- `GetToolDescriptions()`

这已经满足了后续阶段对以下能力的基础需求：

- 半自动模式中自然语言 → Skill 映射
- 问答模式中知识查询 Tool 白名单
- LLM 输出动作白名单约束
- FullAuto 与 SemiAuto 共用统一执行能力集合

### 5. 第二阶段已完成编译验证
已执行：

- `dotnet build aibot\aibot.csproj -c Release`

结果：

- 构建成功
- 当前无新增编译错误

---

## 未完成内容

### 1. 复杂 Skill 仍未全部接到真实 UI 节点执行链路
当前仍为骨架/占位的 Skill：

- `SelectCardSkill`
- `ChooseBundleSkill`
- `ChooseRelicSkill`
- `CrystalSphereSkill`
- `PurchaseShopSkill`
- `RestSiteSkill`
- `ChooseEventOptionSkill`

这些 Skill 已进入正式架构，但尚未完成：

- 界面节点定位
- 按参数精确选择目标
- 与旧 `AiBotRuntime` 私有流程完全一致的操作细节

### 2. FullAuto 模式尚未改成完全通过 Skill 驱动
本阶段完成的是：

- Skill 能力模型落地
- Registry 接入
- 一批 Skill 可执行

但 `FullAutoModeHandler` 目前仍通过旧的兼容入口调用 `AiBotRuntime` 的完整自动流程，而没有把每一步 action 改为由 Registry 中的 Skill 驱动。

这属于预期内的渐进式重构。

### 3. SemiAuto 和 QnA 还没有真正消费这些能力
虽然第二阶段已经把 Skills / Tools 都注册完成，但尚未完成：

- 自然语言意图解析
- 聊天窗口
- Tool 调用入口
- Skill 执行确认 UI

这些会在后续阶段接上。

---

## 遇到的问题

### 问题 1：现有 `AiBotRuntime` 执行逻辑与 UI 节点耦合较深
很多自动化行为不是单个命令调用，而是“读取当前 UI 状态 → 找节点 → 点击 → 等待 Action Queue”。

这导致部分 Skill 虽然从概念上已经明确，但真正做成“稳定独立的执行单元”时，需要同步迁移：

- 节点查找逻辑
- 冷却与等待逻辑
- 失败兜底逻辑
- 目标选择逻辑

### 问题 2：部分游戏类型与节点命名空间分散
在实现阶段二时，出现了多处：

- `NOverlayStack`
- `NCardRewardSelectionScreen`
- `NRewardsScreen`
- `TargetType`

分布在不同 namespace 的问题，说明后续如果继续直接在 Skill 中拼接节点类型，复杂度会越来越高。

### 问题 3：并不是所有 Skill 都适合在第二阶段立即完全落地
例如：

- 商店购买
- 水晶球
- 卡牌选择
- 遗物/Bundle 选择

这些行为虽然已被抽象为 Skill，但如果在第二阶段就强行实现全部完整细节，容易把阶段目标从“能力模型抽象”拖回“旧 Runtime 全量迁移”。

---

## 解决方案与后续建议

### 对问题 1 的解决方案
后续建议引入一个更明确的执行辅助层，例如：

- `RuntimeInteractionHelper`
- 或 `AgentActionExecutor`

把以下逻辑从 Skill 中抽出来复用：

- 查找当前活跃 UI 节点
- 等待队列清空
- 等待冷却窗口
- 兜底目标选择
- 控件点击与确认流程

### 对问题 2 的解决方案
后续可建立更集中的“节点/界面适配层”，例如：

- `GameScreenLocator`
- `RewardScreenAdapter`
- `ShopScreenAdapter`
- `MapScreenAdapter`

让 Skill 面向抽象适配器，而不是直接知道过多 Godot 节点细节。

### 对问题 3 的解决方案
保持当前分层节奏：

- 第二阶段先完成能力抽象和白名单注册
- 第三阶段开始让 SemiAuto 与 FullAuto 逐步真正消费 Skills / Tools
- 复杂 Skill 在实际被某个模式正式使用时，再逐个补执行细节

这样最稳妥，也最符合渐进式重构路线。

---

## 下一阶段建议目标

建议第三阶段开始推进：

1. 让 `SemiAutoModeHandler` 开始真正使用 `AgentCore.Registry`
2. 新建 `IntentParser`
3. 新建模式对话框 UI 雏形
4. 给 Tools 提供统一调用入口
5. 逐步让 `FullAuto` 中的部分动作改为通过 Skill 执行

这样第三阶段结束后，项目将从“已经有能力抽象”进入“能力开始被模式消费”的状态。
