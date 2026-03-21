# Phase 15 — `sts2_guides/core` 目录落地与新旧命名兼容

## 已完成内容

### 1. `GuideKnowledgeBase` 已支持新旧 Markdown 命名并存
已更新：

- `aibot/Scripts/Knowledge/GuideKnowledgeBase.cs`

此前加载器对 Markdown 的假设仍然停留在旧平铺结构：

- `00_OVERVIEW.md`
- `sts2_knowledge_base.md`
- `ironclad_complete_guide.md`
- `silent_complete_guide.md`
- ...

这与 `claude_plan.md` 中建议的目录结构并不一致，因为 `plan` 期望的是：

- `core/guides/overview.md`
- `core/guides/general_strategy.md`
- `core/guides/ironclad.md`
- `core/guides/silent.md`
- ...

本阶段已把加载器改成“新旧命名兼容”的过渡实现：

- `overview.md` 与 `00_OVERVIEW.md` 都可被读取
- `general_strategy.md` 与 `sts2_knowledge_base.md` 都可被读取
- 角色 guide 现在优先尝试：
  - `slug.md`
  - 再回退 `slug_complete_guide.md`

这样可以保证：

- 新目录结构可以马上被运行时使用
- 旧平铺知识库不会因此失效
- 后续可以渐进迁移，而不是一次性重构全部文件名

---

### 2. `sts2_guides/core/` 过渡目录已正式落地
本阶段已在知识库目录下创建：

- `sts2_guides/core/`
- `sts2_guides/core/guides/`

并将现有平铺知识文件复制为规范化命名版本：

#### 结构化 JSON
- `core/characters.json`
- `core/builds.json`
- `core/cards.json`
- `core/relics.json`

#### Markdown guides
- `core/guides/overview.md`
- `core/guides/general_strategy.md`
- `core/guides/ironclad.md`
- `core/guides/silent.md`
- `core/guides/defect.md`
- `core/guides/regent.md`
- `core/guides/necrobinder.md`

这一步的意义不是“知识数据已经补全”，而是把目录结构先从 `plan` 的抽象设计，推进成了项目中真实存在、且运行时可识别的基础布局。

---

### 3. 已新增 `core/schema.json`
已新增：

- `sts2_guides/core/schema.json`

当前 schema 文件提供的是轻量目录说明与规范映射，而不是完整 JSON Schema。

目前记录了：

- 支持的 canonical JSON 文件名
- 每类知识的实体类型名
- 主要 key 字段
- canonical guide 文件清单

它的价值在于：

- 为后续玩家与开发者维护 `core/` / `custom/` 提供统一入口
- 与代码中的 `KnowledgeSchema.cs` 形成“运行时约束 + 目录文档”双层对应
- 为后续进一步扩展到更严格字段校验提供落点

---

### 4. 已为缺失的 canonical 知识文件补空占位
本阶段还补齐了 `plan` 中列出的核心文件骨架：

- `sts2_guides/core/potions.json`
- `sts2_guides/core/powers.json`
- `sts2_guides/core/enemies.json`
- `sts2_guides/core/events.json`
- `sts2_guides/core/enchantments.json`
- `sts2_guides/core/game_mechanics.json`

当前内容先使用空数组 `[]` 作为占位。

这样做的目的有两个：

1. 让 canonical 目录结构完整落地
2. 让后续知识补全工作可以直接填充目标文件，而不必再次调整目录约定

这也和 `plan` 中“代码与知识库补全并行推进”的思路一致。

---

### 5. 本阶段已完成编译验证
已执行：

- `dotnet build aibot\aibot.csproj -c Release`

结果：

- 构建成功
- 当前无新增编译错误

---

## 本阶段补齐的计划缺口
本阶段主要补齐：

- 第 10 节目录结构重构的实际落地
- 第 12 节加载器对 canonical 文件名的兼容支持
- 第 34 节中 `sts2_guides/core/` 相关知识文件骨架

这意味着当前项目不再只是“代码支持 `core/custom`”，而是已经拥有了一套真实存在的 `core/` 知识目录可供继续扩展。

---

## 未完成内容

### 1. `core/` 目录目前仍是过渡拷贝，不是最终整理结果
当前 `core/characters.json`、`core/builds.json` 等本质上还是由旧平铺文件复制而来。

所以本阶段完成的是：

- 目录落地
- 命名规范统一
- 加载器兼容

但还没有做：

- 字段清洗
- 内容补全
- 冗余字段整理
- `source` 的外部数据规范化

### 2. 药水 / Power / 敌人 / 事件 / 附魔 / 机制仍缺真实数据
虽然 canonical 文件已经在目录中存在，但目前仍然是空数组占位。

下一步如果继续沿 `plan` 推进，最直接的价值点就是逐步补：

- `potions.json`
- `powers.json`
- `enemies.json`
- `events.json`
- `game_mechanics.json`

### 3. 还没有把旧平铺结构彻底淘汰
当前仍保留旧文件以保证兼容和可用。

这符合当前阶段目标，但长期看仍需要决定：

- 是否把旧平铺文件保留为 fallback
- 还是未来在知识库成熟后彻底切到 `core/` 规范目录

### 4. `schema.json` 目前偏文档化，尚未参与运行时严格校验
当前运行时真正生效的约束仍主要来自：

- `aibot/Scripts/Knowledge/KnowledgeSchema.cs`
- `aibot/Scripts/Knowledge/KnowledgeValidator.cs`

也就是说，目录中的 `schema.json` 当前更像“对外可读规范”，还没有直接被代码消费。

---

## 遇到的问题

### 问题 1：`plan` 里的 canonical 命名与现有运行时代码的旧命名不一致
如果只创建 `core/guides/overview.md` 等新文件，但不改加载器，运行时不会真正读取这些文件。

### 解决方案
本阶段先从代码层补 alias：

- 新 canonical 命名优先
- 旧平铺命名作为 fallback

这样目录结构落地后即可立即生效。

---

### 问题 2：不能一次性删除旧平铺知识文件
当前系统已经在使用旧文件，贸然删除会增大回归风险。

### 解决方案
本阶段采用“复制 + 兼容加载”的过渡策略：

1. 保留旧平铺文件
2. 创建 `core/` 新目录
3. 让加载器优先识别新结构
4. 保持旧结构兜底

这样既推进了 `plan`，又避免影响当前可用性。

---

### 问题 3：部分 canonical 文件当前没有真实数据来源
例如药水、Power、敌人、事件、附魔、机制规则，还没有足够完整的数据源可直接写入。

### 解决方案
本阶段先补空文件占位，把目录规范固定下来；后续当数据来源完善后，直接往这些文件填充即可。
