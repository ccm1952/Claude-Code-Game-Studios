// 该文件由Cursor 自动生成

# 问题记录：测试 fixture 漏复用 boilerplate（stub camera 等）+ Unity NUnit 把 ERROR log 视为测试失败

> **日期**: 2026-04-29
> **发生次数**: 1 次显式（S2-10 GridSnapTests 11/11 全失败）+ 1 次潜在（S2-09 InteractableObject fail-loud 改造已埋下风险，DragMechanicsTests 当时已正确配置才幸免）
> **严重性**: 中（编译通过、测试运行起来才暴露；定位需要倒推 SetUp）

## 问题现象

`GridSnapTests` 11 个 NUnit 测试**全部失败**，每个 test 触发的失败信号是相同的 ERROR log：

```
[InteractableObject#42] _gameplayCamera 未配置 — drag 不可用（ADR-012 禁止 Camera.main fallback）
  at GameLogic.InteractableObject:Initialize() (InteractableObject.cs:144)
  at ShadowGame.Tests.EditMode.ObjectInteraction.GridSnapTests:MakeInteractableObject (GridSnapTests.cs:96)
  at ShadowGame.Tests.EditMode.ObjectInteraction.GridSnapTests:SetUp () (GridSnapTests.cs:61)
```

Test Runner 把 11 个 test 标红，但 ERROR 来源**不是被测代码**——而是 SetUp 阶段的 fixture 配置不全。

## 根本原因（双层）

### 第一层（直接原因）

`GridSnapTests.SetUp()` 创建 `InteractableObject` 时漏了两件事：

1. 创建 `Camera` 桩 GameObject + 配置 `orthographic / pixelRect` 等
2. 在 `MakeInteractableObject` 中通过 reflection 注入 `_gameplayCamera = _camera`

而 `DragMechanicsTests`（S2-09 同一 epic）已经有完整的 boilerplate（行 36-95），只是新加 `GridSnapTests` 时**没有完整复用**——agent 写的时候挑了部分（GameEvent.Init / IInteractionEvent_Gen / PuzzleConfigProvider 都对，但漏了 camera 这一段）。

### 第二层（深层原因 — 重要）

S2-09 X1 patch v3 把 `InteractableObject._gameplayCamera == null` 行为从 `enabled = false` 改成 `Log.Error`（fail-loud 设计）。**与此同时**，Unity NUnit 默认行为是：

> `[SetUp]` / `[Test]` 期间产生**未 expect** 的 `LogType.Error` log → 测试自动失败。

两者叠加：任何 fixture 缺少必要 stub（camera / provider / 其他被 fail-loud 检查的依赖）→ Initialize 触发 Log.Error → SetUp 失败 → 该 fixture 全部 test 红。

**风险传染性**：未来如果再加新的 fail-loud 校验（比如 `_inputDispatcher == null` 报 Error），所有既有 fixture 又会 boom 全红，除非每个 fixture 都跟着加新的 stub 注入。这是个隐式耦合，没有编译期保护。

## 解决方案

### 本次立即修复（已完成）

`GridSnapTests` 完整对齐 `DragMechanicsTests` 的 fixture：补 stub Camera 创建 + `_gameplayCamera` 注入 + TearDown 销毁。

### 工程规则层面（防复发）

#### 规则 R1 — 测试 fixture 必须完整复用既有同 epic boilerplate

新建测试 fixture（同一 epic / 同一 system）时，**必须**先读同 epic 内既有 fixture 的 SetUp/TearDown/MakeXxx 工厂方法，**整段复用**而非选择性挑选。差异只能是：

- ✅ 允许：增加额外 stub（新 system 多出来的依赖）
- ✅ 允许：减少 dispatcher 桩（如本 fixture 不依赖 IGestureEvent，可不实例化 `IGestureEvent_Gen`——但要在注释里说明"不依赖 X"）
- ❌ 禁止：减少 fail-loud 校验涉及的 stub（camera / config provider / 其他 Initialize 校验依赖）

判断哪些 stub 属于 fail-loud 必备的方法：`Grep "Log.Error" Assets/GameScripts/HotFix/GameLogic/<system>/<class>.cs` 查所有 Error 日志触发条件，逐一对应 SetUp 注入。

#### 规则 R2 — fail-loud 改造与测试 fixture 同步审查

被测组件改 fail-loud 路径（`Log.Error` 替换 `enabled = false` 或 silent return）时，必须同时：

1. 列出所有引用此组件的测试 fixture（`Grep "AddComponent<<class>>" Assets/Tests/`）
2. 每个 fixture 的 SetUp 是否注入了新 fail-loud 校验涉及的 stub？没有 → 在同一次提交补齐
3. 在 ADR / story Implementation Notes 标注："fail-loud 校验 X 已审查 N 个 fixture，全部已注入 stub"

#### 规则 R3 — 预期 ERROR 用 LogAssert.Expect 显式声明

测试**期望**触发 ERROR log 的场景（如 `AC5_OnEnable_LogsError_WhenProviderNotRegistered`），必须用 `LogAssert.Expect(LogType.Error, regex)` 显式声明（DragMechanicsTests 行 266 / 283 就是正确范式）。**禁止**用 `LogAssert.ignoreFailingMessages = true` 全局放行（会掩盖真正的 fixture bug）。

## 预防复发的机制

1. **新 fixture 创建 checklist（强制）**：
   - [ ] 已读同 epic 已有 fixture 的 SetUp/TearDown 全段
   - [ ] 已 Grep 被测类的 `Log.Error`，每条触发条件对应一个 SetUp stub 注入或一个 `LogAssert.Expect`
   - [ ] 测试 asmdef 含 `GameLogic` + `TEngine.Runtime`（已有 problem_2026-04-22_asmdef-source-generator.md 规范）
   - [ ] 显式调 `Initialize()`（已有 problem 2026-04-29 X1 patch v3 规范，DragMechanicsTests 注释行 88-89 已说明）

2. **review 阶段强制项**：`/code-review` 跑测试相关 PR 时，必须 diff 检查 SetUp 段是否漏 stub。

3. **未来抽 fixture base class（优先级 P2）**：当 ObjectInteraction epic 出现第 3 个测试 fixture（S2-11 / S2-12 / S2-13）时，把 SetUp/TearDown 共同 boilerplate 抽到 `InteractableObjectFixtureBase` abstract class，子类只 override 必要差异。本次先记规则不改代码，避免 scope 蔓延。

## 受影响 / 已更新的文档

- [x] `.claude/memory/problem_2026-04-29_test-fixture-fail-loud-boilerplate-skip.md` — 本文件
- [x] `src/MyGame/ShadowGame/Assets/Tests/EditMode/ObjectInteraction/GridSnapTests.cs` — 已修复 (camera stub 注入 + TearDown 销毁)
- [x] `src/MyGame/ShadowGame/.claude/skills/tengine-dev/references/conventions.md` — 已追加 "## 测试 fixture 规范" 章节（R1/R2/R3 + checklist）

## 复盘：本次流程的教训

| 教训 | 原因 |
|------|------|
| fail-loud 不是免费的——它会"耦合" fixture 配置完整性 | Log.Error 替换 enabled=false 后，缺 stub 不再是 silent no-op，会被测试框架视为失败 |
| 同 epic 多 fixture 的 boilerplate 复用必须**整段**而不是 cherry-pick | Agent 倾向只抄"看上去相关"的 stub，跳过"看上去不相关但其实必备"的 stub（camera 在 GridSnap 不直接被用，但被 Initialize 校验）|
| Unity NUnit 把 SetUp 阶段的 ERROR log 视为失败是隐式行为 | 没有显式异常、没有 Assert.Fail，Test Runner 红条只显示堆栈头部，需要看 Console 日志才能确认根因 |
| 报错堆栈的 line number（InteractableObject.cs:144）误导人去看被测代码 | 真正错的是 SetUp:61 + MakeInteractableObject:96，但用户视线被引到 Initialize 上 |
