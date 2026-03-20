# Phase 06 — Assist 模式推荐覆盖层

## 已完成内容

### 1. 辅助模式推荐层已落地
已新增：

- `aibot/Scripts/Ui/AgentRecommendOverlay.cs`

本阶段新增了一个独立的 `AgentRecommendOverlay`，用于在 `Assist` 模式下给当前关键决策点贴上“推荐”标签。

当前实现特点：

- 使用独立 `CanvasLayer`
- 不修改 `sts2/`
- 不自动点击
- 不影响原有 UI 交互
- 目标节点位置变化时标签会跟随更新
- 场景切换后旧标签会自动清理

这满足了计划中“先用轻量推荐标签替代复杂高亮”的落地方向。

---

### 2. 首版已支持 3 类推荐场景
本阶段优先实现了最稳妥的三个场景：

- 卡牌奖励推荐
- 遗物选择推荐
- 地图节点推荐

#### 卡牌奖励
当出现 `NCardRewardSelectionScreen` 时，推荐层会：

1. 读取当前可选卡牌
2. 调用现有 `DecisionEngine.ChooseCardRewardAsync()`
3. 将“推荐”标签贴到对应卡牌上
4. tooltip 中保留简短理由

#### 遗物选择
当出现 `NChooseARelicSelection` 时，推荐层会：

1. 读取当前可选遗物
2. 调用现有 `DecisionEngine.ChooseRelicAsync()`
3. 将“推荐”标签贴到对应遗物上
4. tooltip 中保留理由

#### 地图节点
当地图界面打开且可选路时，推荐层会：

1. 收集当前可达地图节点
2. 复用现有地图候选逻辑
3. 调用 `DecisionEngine.ChooseMapPointAsync()`
4. 将“推荐”标签贴到推荐节点上

这意味着辅助模式现在已经从“纯骨架”升级为“真正会提示推荐项”的模式。

---

### 3. 推荐层已接入 Assist 模式生命周期
已更新：

- `aibot/Scripts/Agent/Handlers/AssistModeHandler.cs`
- `aibot/Scripts/Agent/AgentCore.cs`

当前实现中：

- `AgentCore.Initialize()` 会创建推荐层
- 进入 `Assist` 模式时显示推荐层
- 离开 `Assist` 模式时隐藏并清理推荐层

这样做保证了推荐层不会常驻影响其他模式，也不会残留旧标签。

---

### 4. 已补推荐层配置默认项
已更新：

- `aibot/Scripts/Config/AiBotConfig.cs`

新增默认配置字段：

- `showRecommendOverlay`

仍然保持旧配置兼容：

- 如果本地 `config.json` 没有该字段，则自动使用默认值
- 不需要强制用户手动更新配置文件

---

### 5. 第六阶段已完成编译验证
已执行：

- `dotnet build aibot\aibot.csproj -c Release`

结果：

- 构建成功
- 当前无新增编译错误

---

## 未完成内容

### 1. 还未覆盖全部计划中的推荐场景
本阶段优先实现了三类高价值场景，但以下场景仍未接入：

- 战斗手牌推荐
- 商店商品推荐
- 休息点选项推荐
- 事件选项推荐
- 奖励领取推荐
- Bundle / Crystal Sphere 等更特殊选择场景

### 2. 当前标签仍是轻量版
目前的推荐标签已经可用，但还没有：

- 更丰富的颜色区分
- 多个标签同时展示
- 更细的理由卡片样式
- 点击后展开完整解释

### 3. `AssistModeHandler` 仍不是完整逻辑中心
本阶段为了快速可用，采用的是“推荐层自驱动轮询”的方式，而不是依赖 `AssistModeHandler.OnTickAsync()`。原因是当前模式 handler 还没有统一 tick 驱动。

这是一种务实方案，但未来如果统一 Agent 模式 tick，可再回收这部分职责。

---

## 遇到的问题

### 问题 1：当前模式 handler 没有被统一 tick 驱动
如果只在 `AssistModeHandler.OnTickAsync()` 里写推荐逻辑，当前架构下它不会被持续调用，因此推荐标签不会真正出现。

### 解决方案
本阶段改为：

- 让 `AgentRecommendOverlay` 自己在 `_Process()` 中轮询
- 仅在 `Assist` 模式下启用
- 通过签名去重与刷新节流，避免每帧重复计算

这在当前架构下是更稳的落地方式。

---

### 问题 2：不能为了推荐层重写一套决策逻辑
如果推荐层单独实现“推荐哪张卡/哪件遗物/哪条路线”，会导致与现有 Decision Engine 分叉。

### 解决方案
直接复用现有：

- `ChooseCardRewardAsync()`
- `ChooseRelicAsync()`
- `ChooseMapPointAsync()`

这样推荐层与全自动/半自动共享同一套推荐核心，后续维护成本更低。

---

### 问题 3：标签必须跟随目标节点但不能干扰点击
推荐层既要贴在目标节点上，又不能抢占鼠标事件。

### 解决方案
当前标签实现采用：

- `CanvasLayer` + 独立 `PanelContainer`
- `MouseFilter = Ignore`
- 每帧同步目标节点位置
- 节点失效时自动移除标签

这样既能跟随 UI，又不会破坏原交互。

---

## 解决方案与后续建议

### 下一阶段建议 1：继续扩 Assist 场景覆盖
最自然的下一步是继续补齐：

- 战斗手牌推荐
- 休息点推荐
- 事件选项推荐
- 商店推荐

### 下一阶段建议 2：统一 Agent 模式 tick
后续若要让模式层更干净，可以考虑让 `AgentCore` 拥有统一 tick 分发，再把目前推荐层中的部分轮询逻辑回收到 `AssistModeHandler`。

### 下一阶段建议 3：强化推荐理由展示
可以继续增强推荐标签体验：

- tooltip 显示更完整理由
- 点击标签展开详情
- 用不同颜色区分风险/收益型推荐

### 下一阶段建议 4：和聊天窗口联动
后续可加入：

- 点击推荐标签时在聊天窗口解释原因
- 玩家询问“为什么推荐这个？”时直接复用当前推荐理由

这样辅助模式会更接近完整的“边推荐边解释”的游戏 Agent 体验。
