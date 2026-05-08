// 该文件由Cursor 自动生成

# Unity Workflow Onboarding — 影子回忆 (Shadow Memory)

> **Audience**: 新成员（人 / AI agent）首次接触本项目时的 30-60 min onboarding 文档
> **Created**: 2026-05-06 (S4-05；Sprint 1 retro action #3 第 3 次 carryover 硬截止)
> **Owner**: tech-lead
> **Sprint sources**: Sprint 1 retrospective action #3 (Sprint 1 → 2 → 3 → 4 第 3 次 carryover) + Sprint 3 retrospective Process Improvements

---

## 0. 30-Second Project Snapshot

**项目**: 影子回忆 (Shadow Memory) — 单人解谜游戏，玩家通过摆放物体投射影子匹配目标轮廓
**引擎**: Unity 2022.3.62f2 LTS（**不是** Unity 6+；详见 §2 引擎陷阱）
**Framework Stack**:
- TEngine 6.0.0（模块化 framework；`GameModule.XXX` accessor pattern；详见 §3）
- HybridCLR（C# hot-reload；GameLogic assembly 是热更边界；详见 §4）
- YooAsset 2.3.17（async-only 资源加载；详见 §5）
- UniTask 2.5.10（**禁用 Coroutine**；详见 §6）
- Luban（外部 config 生成 `TbXxx` 数据表；详见 §7）

**Sprint 状态**：当前 Sprint 4，governance maturity 阶段
- 21 ADRs Accepted（含 ADR-029 V2.0 governance gate）
- 212 TRs SSoT in `tr-registry.yaml`
- ADR-029 V2.0 R1/R2/R3 readiness gate active（详见 §10）
- Sprint 5 起 VS slice (chapter 1) 实体构建（per ADR-030 VS-skip path 决策；详见 §13）

**首要 onboarding 资料**（按读取优先级）：

1. `src/MyGame/ShadowGame/CLAUDE.md` — 项目级规约
2. `docs/architecture/architecture.md` — 系统架构 v1.0.0
3. `docs/architecture/adr-029-story-impl-notes-verification.md` — V2.0 governance framework
4. `docs/architecture/adr-027-gameevent-interface-protocol.md` — 事件协议（含 §5 ⚠️ TEngine framework knowledge fact）
5. 本文档 — 实操陷阱合一

---

## 1. 仓库结构

```
MyGameStudio/
├── docs/
│   ├── architecture/                    # ADRs (21) + traceability + tr-registry + control-manifest + reviews
│   │   ├── adr-001..018, adr-027..029.md
│   │   ├── architecture.md              # 主架构文档 v1.0.0
│   │   ├── architecture-traceability.md # 212 TRs → ADRs mapping (v1.1 from 2026-05-06)
│   │   ├── tr-registry.yaml             # 212 TRs SSoT (v2 from 2026-05-06)
│   │   ├── control-manifest.md          # forbidden patterns + binding constraints
│   │   ├── consistency-failures.md      # reflexion log (2026-05-06 启动)
│   │   └── architecture-review-2026-XX-XX.md  # 历次 review 报告
│   ├── engine-reference/unity/          # Unity 2022.3 vs 6+ 差异 + breaking changes
│   ├── onboarding/                      # 本文档 + future onboarding 内容
│   └── spikes/                          # Sprint 0 spikes (SP-001..SP-011) closed
│
├── src/MyGame/ShadowGame/                # Unity project root
│   ├── Assets/
│   │   ├── GameScripts/HotFix/          # Hot-reload boundary (HybridCLR; 详见 §4)
│   │   │   └── GameLogic/                # 热更代码（业务逻辑）
│   │   │       ├── ChapterState/         # Chapter 状态机 + Save 集成
│   │   │       ├── DevTest/              # 开发态测试 spike (IDevSpike + DevBootstrap)
│   │   │       │   └── Spikes/            # SP-XXX / S3-XX / S4-XX PlayMode spike 实施
│   │   │       ├── GameFlow/             # FSM states (Loading / Lobby / DevTest / Gameplay / etc.)
│   │   │       ├── IEvent/               # ISceneEvent / IInteractionEvent / IChapterStateEvent (ADR-027)
│   │   │       ├── Input/                # SingleFingerFSM / DualFingerFSM / IInputConfig
│   │   │       ├── Module/               # 业务模块 (GameModule.XXX 真实实现)
│   │   │       ├── ObjectInteraction/    # InteractableObject + Coordinator + LockManager
│   │   │       ├── Save/                 # SaveManager
│   │   │       ├── Scene/                # SceneManager + IFadeOverlay (S3-03)
│   │   │       └── SingletonSystem/
│   │   ├── TEngine/                      # Framework 源码（不要改；详见 §3）
│   │   └── Tests/EditMode/                # NUnit EditMode tests (Sprint 1-3 累计 ~321 tests)
│   ├── design/
│   │   ├── art/                          # Art bible
│   │   ├── gdd/                           # 12 system GDDs + systems-index.md
│   │   ├── ux/                            # UX patterns + screen specs
│   │   └── accessibility-requirements.md
│   └── production/
│       ├── epics/                        # 13 epics + stories per system
│       ├── sprints/                      # Sprint plans + retrospectives (Sprint 1-4)
│       ├── qa/                            # QA plans + grep evidence + tech-debt
│       ├── session-state/active.md        # 当前 active session 历史（连续 session 22 闭环数据）
│       ├── sprint-status.yaml             # Sprint stories 状态机（machine-readable）
│       └── stage.txt                      # 当前 production stage (Pre-Production)
│
└── .claude/skills/                       # 项目级 skill 文档
```

---

## 2. Unity 引擎陷阱（version-specific）

### 2.1 Unity 2022.3.62f2 vs Unity 6 关键差异

**项目锁定 Unity 2022.3.62f2 LTS**。`docs/engine-reference/unity/VERSION.md` 明确：

> "Do NOT suggest Unity 6 APIs"

| API/特性 | Unity 2022.3 (本项目) | Unity 6+ (LLM 默认假设) | 实操影响 |
|---------|---------------------|-----------------------|---------|
| Input class | ✅ `Input.GetTouch()` 仍 primary | ⚠️ Deprecated, 推 New Input System | ADR-010 显式选 legacy Input class |
| UGUI Canvas | ✅ Primary supported | ⚠️ Deprecated but supported | ADR-011 选 UGUI（reject UI Toolkit）|
| URP | 13.x | 17.x+ with RenderGraph | ADR-002 不用 RenderGraph API（Unity 6+ only）|
| `RenderGraph` / `RecordRenderGraph` | ❌ 不存在 | ✅ 主要 API | 0 ADR 引用 |
| `UIDocument` / UI Toolkit | ⚠️ runtime maturity 不足 | Primary | ADR-011 explicitly reject |
| Addressables | ⚠️ 项目用 YooAsset | Primary | YooAsset 2.3.17（ADR-005）|
| `IComponentData` / DOTS / ECS | Optional | Primary in some scenarios | 项目不用 DOTS |

**LLM 写代码时陷阱**：常默认假设 Unity 6 APIs。**抓 drift 工具**：ADR-029 R1/R2 grep gate（`/dev-story` Phase 1.5 强制运行）会抓 0 hit on `RenderGraph` / `UIDocument` / `Addressables.LoadAssetAsync` 等 fantasy API。

### 2.2 Unity API 自动更新陷阱

**症状**：`git pull` 拉了别人提交的代码，编译报错"missing assembly reference"。

**原因**：Unity 的 .meta 文件 / asmdef 改动需要 Editor 重新 import。

**操作流程**：

```bash
# 1. git pull
git pull origin main

# 2. 打开 Unity Editor（或如果已开启则切到 Editor）
# 3. 等待 Editor 自动 reimport changed assets（左下角进度条）
# 4. 看 Console — 如果还有 compile error，手动 Reimport All:
#    Assets → Reimport All
```

**预防**：每次切 branch / pull 后**先回 Unity Editor**而不是直接跑命令。

### 2.3 Unity Test Framework 边界

| Test Mode | 适用场景 | 不适用场景 |
|----------|---------|-----------|
| **EditMode** | 纯逻辑（FSM / 算法 / data POCO）；调用 `[Test]` + NUnit | Scene 操作（LoadSceneAsync 等）/ DOTween 时长 / Raycast 物理 / MonoBehaviour OnEnable lifecycle |
| **PlayMode** | 包含 Unity runtime / scene / DOTween / framework boundary 的全套 | 单纯逻辑 unit test（开销大）|

**项目模式**：
- **EditMode**: `Assets/Tests/EditMode/*` 目录；NUnit `[Test]` / `[TestFixture]`
- **PlayMode (DevSpike pattern)**: 不用 Unity Test Framework `[UnityTest]`；用 `IDevSpike` + `DevBootstrap` + `GameApp.RegisterDevSpikes()` 模式（详见 §11）

详见 §11 spike-driven workflow。

---

## 3. TEngine Framework

### 3.1 GameModule.XXX accessor pattern

**唯一合规方式**：

```csharp
// ✅ 正确
GameModule.Resource.LoadAssetAsync<T>(...)
GameModule.Scene.LoadSceneAsync(...)
GameModule.UI.ShowWindow<T>(...)

// ❌ 禁止 (ADR-001 + ADR-028 forbidden)
ModuleSystem.GetModule<IResourceModule>().LoadAssetAsync<T>(...)
new ResourceModule().LoadAssetAsync<T>(...)  // 直接实例化模块
```

`GameModule.XXX` 是 framework 提供的 facade；底层 `ModuleSystem.GetModule<>` 仅 framework 内部使用。

### 3.2 TEngine UIModule (UGUI binding)

**ADR-011 选 UGUI 而非 UI Toolkit**。Sprint 4 起 UI 实施沿 TEngine UIModule pattern：

- UIWindow 子类 `XxxUI` / `XxxWindow`（继承 `UGuiForm` / `UIWidget`）
- `RegisterEvent()` 内用 `AddUIEvent` 自动 cleanup
- 跨系统事件用 `GameEvent.AddEventListener<TArg>(IXxxEvent_Event.OnYyy, handler)`（per-event 模式 — ADR-027）

### 3.3 Module activation gates

`ADR-028 TEngine Module Usage Policy` 规定：
- AudioModule 激活待 ADR-017 Accepted ✅（2026-05-06 promote 完成；Sprint 4 S4-03 expand 后实装）
- Localization Module / I2 Localization 待 P2

不要在 GameLogic 主路径调 inactive module；调用前 grep 确认 module 已 active per ADR-028。

---

## 4. HybridCLR Hot-Reload Boundary

**装机流程**：

```bash
# 1. 通过 HybridCLR Installer (Tools → HybridCLR → Installer)
#    - 一次性 setup; 把 hot-reload 元数据 dlls 拷贝到正确位置
# 2. 第一次构建 / install 后，每次改 GameLogic 代码:
#    - 直接 PlayMode 运行
#    - HybridCLR runtime 自动 reload GameLogic.dll
# 3. 改非 GameLogic（HotFix asmdef 之外的）需要重新 build:
#    - Cmd+B / Ctrl+Shift+B（取决于平台）
```

**Assembly 边界（ADR-004）**：

```
Default (UnityEngine 主程序集)        ← 不可热更
GameProto (GameLogic.HotFix 之外的工具/Luban gen)  ← 不可热更
GameLogic (Assets/GameScripts/HotFix/)  ← ✅ 热更边界
└── 业务代码全部在此 + asmdef 引用 GameProto
```

**LLM 写代码时陷阱**：在 Default 程序集里调 GameLogic 类（unresolvable reference）。Editor 提示 "missing assembly reference" 即此原因。

---

## 5. YooAsset Resource Lifecycle (ADR-005)

### 5.1 Async-only loading

```csharp
// ✅ 唯一合规
var handle = await GameModule.Resource.LoadAssetAsync<GameObject>(location);
var go = handle.AssetObject as GameObject;
// 用完一定释放：
handle.Release();

// ❌ 禁止
Resources.Load<GameObject>("path");  // sync + non-YooAsset
```

### 5.2 Scene 加载（Special: 用 GameModule.Scene 不是 GameModule.Resource）

```csharp
// ✅ 正确（S3-01 D5 propagation 后 verified）
var sceneName = "Chapter_01_Approach";
await GameModule.Scene.LoadSceneAsync(sceneName, LoadSceneMode.Additive, null);
// SceneManager 内部存 _currentChapterSceneName: string，不缓存 SceneHandle

// ❌ 禁止
await GameModule.Resource.LoadSceneAsync(...);  // fantasy API（不存在 — Type-1 drift）
SceneHandle _handle = await ...;                  // 不要缓存 SceneHandle 字段（D5=[X]）
```

### 5.3 CheckLocationValid 陷阱（Type-2 (a) drift）

```csharp
// ❌ 不要用此 API 验 scene 资产存在性
bool exists = GameModule.Resource.CheckLocationValid(sceneName, "DefaultPackage");
// 实测：scene assets 永返 false（asset/scene location key 体系不同）— S3-01 ADR-029 Type-2 (a) drift 第 1 数据点
```

---

## 6. UniTask (forbidden Coroutine)

**ADR-001 forbidden patterns**:

```csharp
// ✅ 唯一合规
public async UniTask DoSomethingAsync()
{
    await UniTask.Delay(TimeSpan.FromSeconds(1));
    await UniTask.NextFrame();
    await GameModule.Resource.LoadAssetAsync<...>(...);
}

// ❌ 禁止
StartCoroutine(...)               // 全局禁用（ADR-001 forbidden）
yield return new WaitForSeconds(1) // Coroutine 内部
yield return null                   // 同上
```

**EditMode test 中**：UniTask `await` 在 EditMode 不驱动；必须用 `UniTask.RunOnThreadPool` + `WaitWithoutCurrentContext` OR 直接 `task.Status` polling。详见 conventions.md §异步编程规范。

**PlayMode test (IDevSpike)**: `async UniTask` 正常运行；UniTask 在 Unity main thread tick。

---

## 7. Luban Config Access (ADR-007)

```csharp
// ✅ 唯一合规
var data = ConfigSystem.Tables.TbChapter.Get(chapterId);
if (data != null) {
    var sceneId = data.SceneId;
    var emotionalWeight = data.EmotionalWeight;
}

// ❌ 禁止
var data = TbChapter.Instance.Get(chapterId);  // 不是 TEngine pattern
TbChapter.Instance.Items[id]                    // 直接索引
```

**陷阱（SP-004 closed）**：
- `Tables` singleton 必须在 Boot Step 7 (ProcedureMain) 完成 init 后才能调
- ❌ 不在 `UniTask.Run()` (thread pool) 里读 Luban — Luban 不是 thread-safe
- ❌ 不缓存 Luban data 引用到字段（持久化）

---

## 8. EditMode 测试 fixture boilerplate (Sprint 2 + 3 总结)

**所有 EditMode test 必备 boilerplate**（Sprint 1 retro action #4 + 2 retro 总结）：

```csharp
[SetUp]
public void Setup()
{
    // 1. Camera stub (绝大多数 tests 需要)
    if (Camera.main == null)
    {
        var cameraGO = new GameObject("TestCamera");
        var cam = cameraGO.AddComponent<Camera>();
        cam.tag = "MainCamera";
    }

    // 2. Provider 注入（如测试涉及 Save / Chapter / Input config 等）
    _saveManager.RegisterChapterDataProvider(id => /* test fixture */);

    // 3. Listener 计数（per-event listener 模式 — ADR-027）
    _onEventInvokeCount = 0;
    GameEvent.AddEventListener<int>(IXxxEvent_Event.OnY, _ => _onEventInvokeCount++);

    // 4. MonoBehaviour 显式 lifecycle（PlayerLoop 不在 EditMode 驱动 OnEnable）
    _interactableObject.Initialize();  // 必须 explicit (ADR-013 v3 教训)
}

[TearDown]
public void Teardown()
{
    // 1. Listener cleanup with null-check guard (ADR-027 §5 ⚠️ Framework knowledge fact)
    if (_handler != null)
    {
        GameEvent.RemoveEventListener<int>(IXxxEvent_Event.OnY, _handler);
        _handler = null;
    }
    // ❌ 不要 raw remove — TEngine RemoveEventListener 非 idempotent 抛 "Delete handle failed"

    // 2. MonoBehaviour 显式 cleanup
    _interactableObject.Shutdown();

    // 3. Test camera cleanup
    if (cam != null) UnityEngine.Object.DestroyImmediate(cam.gameObject);
}
```

**详见**: `src/MyGame/ShadowGame/.claude/skills/tengine-dev/references/conventions.md §测试 fixture 规范`

---

## 9. DOTween + Visual EditMode 受限

### 9.1 DOTween 时长不可用 EditMode 验

**症状**：EditMode `[Test]` 包 `Stopwatch` + `tween.OnComplete` 不会触发。
**原因**：DOTween 内部 update 依赖 Unity main loop tick；EditMode 不 tick。
**解决**：DOTween 时长断言推 PlayMode（per S3-06 carryover sprint 4）；EditMode 只能验 tween params (duration value / ease curve / from-to range) — Sprint 1 Visual story 教训。

### 9.2 Visual / Shader EditMode 受限

**抽参数层做 EditMode**：tween params / outline width / scale magnitude / debounce interval — 全可 EditMode `[Test]`。
**Shader/material/animation 视觉本身**：手动 Editor 验证（screenshot + designer sign-off）；S4-06 Selection Feedback Visual/Feel pattern 即此。

---

## 10. ADR-029 V2.0 R1/R2/R3 Readiness Gate (核心 governance)

**任何 dev-story 起手必走 5 min readiness checklist**（ADR-029 V2.0 §V2-3 R3 mandatory）：

```markdown
[ ] R1 — per-event listener 模式：所有 GameEvent.AddEventListener<TArg>(...) 是 per-event 签名 → grep `AddEventListener<I\w+Event>\(this\)` 应 0 命中
[ ] R2 — 跨组件 API 调用：所有 obj.SetXxx(...) / Manager.Yyy() / Provider.RegisterZzz(...) 在 framework 实测存在 → grep `public .* Xxx\(` ≥ 1 命中
[ ] R3 — Stub data 类型：所有 stub 数据类型 (PuzzleConfig / GestureData / ...) 构造签名一致 → grep `public XxxConfig\(`
[ ] EditMode fixture boilerplate：是否完整复用？(Camera stub + Provider + Listener + Initialize/Shutdown) — 详见 §8
[ ] LogAssert.Expect：期望的 ERROR/Warning 是否用 LogAssert.Expect 显式声明？
[ ] **R3 PlayMode probe (V2.0 §V2-3 mandatory)**：是否有 PlayMode spike for new method/field/contract? (Type-2 (b/c) drift 仅 PlayMode 可发现)
[ ] Framework boundary behavior probe (V2.0 §V2-5)：listener-not-exists / null-state / over-limit / cancellation / silent-failure 5 类异常路径是否有 spike case?
```

**实战 6 数据点（Sprint 3 累积）**：

| Story | 阶段 | 时间 | Drift type |
|-------|------|------|-----------|
| S3-01 baseline | dev-story | 18 min | Type-1 + Type-2(a) + Type-3 |
| S3-02 Phase 1.5 | grep gate | 8 min | Type-4 (D5 propagation lag) |
| S3-02 R3 PlayMode | runtime | 21 min | **Type-2 (b) cross-method state** |
| S3-03 Phase 1.5 | grep gate | 15 min | Type-4×6 + Type-1×2 |
| S3-03 R3 PlayMode | runtime | 12 min | **Type-2 (c) framework behavior** |
| Lite propagation v2 | meta | 13 min | (V2 #6 refinement — SSoT priority) |

**关键 lessons**：
1. **R3 PlayMode probe 是 mandatory，不是 optional**：Sprint 3 R3 暴露 100% Type-2 (b/c) drift；R1+R2 grep gate 永远抓不到 cross-method state lifecycle 协议不一致 + framework 单 method 行为契约假设错误
2. **Lite propagation v2 SSoT priority**：当 architectural decision 是横切性的（≥10 stories impacted），**优先单点更新引用枢纽 ADR**（如 ADR-027 §5 framework knowledge fact）— ROI 73%-85% vs mass story propagation
3. **Type-2 三子类**：(a) framework facade behavior / (b) cross-method state lifecycle / (c) framework method behavior assumption — 全部 R3 PlayMode-only 可发现

详见: `docs/architecture/adr-029-story-impl-notes-verification.md` V2.0

---

## 11. Spike-Driven Workflow (IDevSpike Pattern)

### 11.1 何时用 spike

**适用场景**：
- PlayMode-only 验证（scene operations / DOTween / Raycast / framework boundary behavior）
- 风险高的 API 集成首测（Sprint 0 spikes SP-001..SP-011）
- ADR Implementation 验证（Sprint 3 起 R3 PlayMode probe）

**不适用**：
- 纯逻辑测试（用 EditMode `[Test]`）
- Tracked 长期 regression test（用 EditMode `[Test]` + asmdef 自动跑）

### 11.2 IDevSpike Pattern 三件套

```csharp
// 1. Spike 入口（实现 IDevSpike）
public class S4XXSpike : IDevSpike
{
    public string Id => "S4-XX";
    public string Name => "...";
    public void Launch()
    {
        var go = new GameObject("S4XX_Runtime");
        UnityEngine.Object.DontDestroyOnLoad(go);
        go.AddComponent<S4XXRuntime>();
    }
}

// 2. Runtime MonoBehaviour（OnGUI 报告 + Start → tester.RunAllAsync）
public class S4XXRuntime : MonoBehaviour
{
    private S4XXTester _tester;
    private void Start() { _tester = new S4XXTester(); RunAsync().Forget(); }
    private async UniTaskVoid RunAsync() => await _tester.RunAllAsync();
    private void OnGUI() { /* report */ }
}

// 3. Tester (纯逻辑 + JSON 落盘 evidence)
public class S4XXTester
{
    public async UniTask RunAllAsync() { /* P1, P2, ... cases */ }
    public void WriteResultJson() { /* Application.persistentDataPath */ }
}
```

### 11.3 GameApp 单 spike 注册（防 type-3 race）

```csharp
// GameApp.RegisterDevSpikes()
private static void RegisterDevSpikes()
{
    // 当前活跃 spike 仅 S4-XX
    // GameLogic.DevTest.DevBootstrap.Register(new GameLogic.DevTest.Spikes.SP011Spike());
    // GameLogic.DevTest.DevBootstrap.Register(new GameLogic.DevTest.Spikes.S301Spike());
    // GameLogic.DevTest.DevBootstrap.Register(new GameLogic.DevTest.Spikes.S302Spike());
    // GameLogic.DevTest.DevBootstrap.Register(new GameLogic.DevTest.Spikes.S303Spike());
    GameLogic.DevTest.DevBootstrap.Register(new GameLogic.DevTest.Spikes.S4XXSpike());
}
```

**只注册 1 个活跃 spike**；其他全部注释。**多 spike 并发 LoadSceneAsync 会撞 YooAsset "while loading" 锁**（Type-3 drift；S3-01 实战 case）。

### 11.4 跑 spike

1. Unity Editor 进 PlayMode
2. 通过 GameApp Entrance → `[ProcedureLogin]` → `[GameLoadingState]` → `[DevTestState]` (Editor / DEBUG only)
3. DevTestState 调 `DevBootstrap.LaunchAll()`，挂当前活跃 spike GameObject
4. spike OnGUI 显示 case 状态；Console log 输出
5. JSON evidence 落 `Application.persistentDataPath/SXXX_Result.json`
6. user paste JSON 给 dev-story flow 验收

**Editor 实例数据存放路径** (macOS): `~/Library/Application Support/DefaultCompany/Unity/`

---

## 12. Lite Propagation v2 Playbook (ADR-029 V2.0 §V2-4)

**当 D-level architectural decision 落地**（影响 ≥2 stories OR ≥1 ADR section），立即跑 lite propagation：

### 12.1 Propagation scope 决策树

```
评估 propagation scope:
  ├── (a) 单点 ADR 文档可承载？ ← 优先
  │     YES + cross-cutting (≥10 stories impacted)
  │     └── 更新 ADR 单点 (highest ROI 73%-85%)
  │     例：S3-03 framework knowledge fact → ADR-027 §5
  │
  ├── (b) 直接受影响的 Ready+ stories ≥5？
  │     YES → mass story propagation (5 min × N stories)
  │     例：S3-01 D5 → 12 active scene-management stories
  │
  └── (c) 全 historical references (已 DONE stories)？
        YES → 不修订 (historical wording acceptable)
        例：story-001 / story-004 / story-006 已 DONE 后 D5 stale 不溯及
```

### 12.2 Lite propagation v2 步骤（10-15 min）

1. **Grep scan**（~3 min）：6 patterns × 全 backlog
2. **命中评估**（~3 min）：分类 active drift / historical / out-of-scope
3. **Propagation 路径选择 + 执行**（~3-10 min）：(a) 单点 ADR 更新 OR (b) mass story propagation
4. **文档化**（~2 min）：propagation 数据点 + 触发条件 ROI 至 ADR-029 history

### 12.3 实战 ROI（Sprint 3 累积 3 次实测）

| Propagation 类型 | 时间 | 覆盖范围 | vs 替代方案 |
|----------------|------|---------|------------|
| S3-01 D5 propagation (mass) | 35 min | 12 active stories | net ROI +5 ~ +35 min |
| Lite propagation v2 to ADR-027 §5 | 13 min | 17 listener stories via SSoT | **节省 72 min ROI 73%** |
| 14 ADR bulk Promotion ceremony | 30 min | 14 ADRs | **节省 180 min ROI 85%** |

---

## 13. consistency-failures.md Reflexion Log

启动于 2026-05-06（Sprint 3 architecture-review 触发）。**append-only log**；记录跨文档 conflict patterns + generalised lessons。

**当下次 conflict 发现时**：

```markdown
### YYYY-MM-DD — /architecture-review — 🔴 CONFLICT-NNN
**Domain**: [Architecture / Engine / Event Protocol / etc.]
**Documents involved**: [ADR-XXX vs ADR-YYY OR intra-ADR section A vs section B]
**What happened**: [具体冲突 — 每个 ADR 声明什么]
**Resolution**: [如何解决]
**Pattern (generalised lesson)**: [future authors 在此 domain 的教训]
```

**Sprint 4 起监控**：每个 Sprint 末 retro 检查是否有新 conflict 应 log。

---

## 14. Sprint 4 起 dev-story 实操路径

### 14.1 起手前置检查

1. 读 `production/sprint-status.yaml` — 确认 story status: ready-for-dev
2. 读 story 文件 (`production/epics/...story-XX.md`)
3. 跑 `/story-readiness [story-file]` — 确认 R1/R2/R3 PASS（详见 §10）
4. 查 `tr-registry.yaml` — 确认 TR-ID 引用有效

### 14.2 实施

1. `/dev-story [story-file]` 起手
2. 完成 5 min readiness checklist (§10)
3. 按 story Implementation Notes 写 production code
4. 写 EditMode test (Logic stories) OR PlayMode spike (Integration / Visual stories) §11
5. 跑 lint + grep evidence (`production/qa/grep-no-fantasy-api-XX.md`)
6. PlayMode 验证（如适用）

### 14.3 闭环

1. `/code-review` (optional)
2. `/story-done` — 自动更新 sprint-status.yaml status: ready → done + completed: YYYY-MM-DD
3. 记录 §Impl Notes drift 修订耗时 metric (ADR-029 V2.0 §V2-7)
4. 如有新 framework knowledge fact → propagate per §12 SSoT priority

### 14.4 sprint 末

1. `/smoke-check sprint` — verify Sprint 4 stories 0 regression
2. `/team-qa sprint` — QA sign-off
3. `/retrospective sprint-4` — 写 retro，含 ADR-029 V3 watch list 5 触发条件检查
4. `/sprint-plan sprint-5` — Sprint 5 plan 起草（VS Build commitment per ADR-030）

---

## 15. 常用命令 Quick Reference

```bash
# Build / Editor 操作
git pull                   # 然后回 Unity Editor 等 reimport
# Editor → Assets → Reimport All  (如有 compile error)

# Lint check（assets-refresh 后）
# Editor: Console panel → 0 Error / 0 Exception / 0 Warning(critical)

# 跑 EditMode tests
# Editor → Window → General → Test Runner → EditMode → Run All
# 期望：~321 tests PASS (Sprint 1-2 baseline) + Sprint 3-4 累积新增

# 跑 PlayMode spike (Sprint 4)
# Editor → ▶ Play → wait DevTestState 进入 → 看 OnGUI 报告
# Console log 输出详细 PASS/FAIL
# JSON evidence: ~/Library/Application Support/DefaultCompany/Unity/SXXX_Result.json (macOS)

# Grep evidence 保存路径
production/qa/grep-no-fantasy-api-[story-slug]-YYYY-MM-DD.md
```

---

## 16. 常见疑问 FAQ

**Q1: 我 git pull 后 Unity 编译报 missing reference，怎么办？**
A: §2.2 — 回 Editor，等 reimport，必要时 Assets → Reimport All。

**Q2: 我写 `await GameModule.Resource.LoadSceneAsync(...)` 编译过但运行时报错，为什么？**
A: §5.2 — Scene 用 `GameModule.Scene.LoadSceneAsync`，不是 Resource。这是 Type-1 fantasy API（ADR-029 R2 grep gate 应 0 hit）。

**Q3: 我写 `GameEvent.AddEventListener<IInteractionEvent>(this)` 提示 API 不存在？**
A: §3.2 — TEngine 不支持整接口订阅；用 per-event 模式 `GameEvent.AddEventListener<int>(IInteractionEvent_Event.OnY, handler)`。

**Q4: 我 RemoveEventListener 后再次 RemoveEventListener 抛 "Delete handle failed"，为什么？**
A: §10 + ADR-027 §5 — TEngine RemoveEventListener 非 idempotent。Required pattern: handler 内 self-remove + `_handler = null` (null-out)；外部 cleanup 必须 `if (_handler != null) RemoveEventListener(...)` (null-check guard)。Type-2 (c) framework behavior assumption drift。

**Q5: 我 EditMode 测试 DOTween tween.OnComplete 不触发？**
A: §9.1 — DOTween 不在 EditMode tick；推 PlayMode spike (§11) 测时长 / OnComplete。EditMode 只测 tween params（抽参数层）。

**Q6: 我 Sprint 4 第一个 story 应该选什么？**
A: 看 `production/sprints/sprint-4.md` Track A/B/C：
- Track A (P1 ADR impl expand) — 解锁 Sprint 5 VS 依赖；建议从 S4-04 VS-skip path 起（governance critical）OR S4-05 (本文档 onboarding，已完成)
- Track B (carryover) — Sprint 1-3 累积债务，必须 Sprint 4 清理
- Track C (governance) — 含 ADR-030 VS-skip path

**Q7: 我发现新 conflict / drift / framework knowledge fact，去哪里 log？**
A: §13 — `docs/architecture/consistency-failures.md` (append-only)；如是 framework knowledge fact 横切多 stories → 走 §12 lite propagation v2 SSoT priority 路径。

---

## 17. 重要文档索引

| 文档 | 路径 | 用途 |
|------|------|-----|
| **CLAUDE.md** | `src/MyGame/ShadowGame/CLAUDE.md` | 项目级 LLM 规约 |
| **AGENTS.md** | `src/MyGame/ShadowGame/AGENTS.md` | Agent 协作规约 |
| **architecture.md** | `docs/architecture/architecture.md` v1.0.0 | 主架构文档 |
| **architecture-traceability.md** | `docs/architecture/architecture-traceability.md` v1.1 | 212 TRs → ADRs |
| **tr-registry.yaml** | `docs/architecture/tr-registry.yaml` v2 | 212 TRs SSoT |
| **control-manifest.md** | `docs/architecture/control-manifest.md` | Forbidden patterns + binding constraints |
| **ADR-027** | `docs/architecture/adr-027-gameevent-interface-protocol.md` | Event protocol + §5 ⚠️ Framework knowledge fact |
| **ADR-028** | `docs/architecture/adr-028-tengine-module-usage-policy.md` | Module activation gates |
| **ADR-029 V2.0** | `docs/architecture/adr-029-story-impl-notes-verification.md` | Governance gate |
| **consistency-failures.md** | `docs/architecture/consistency-failures.md` | Reflexion log (2026-05-06 启动) |
| **conventions.md** | `src/MyGame/ShadowGame/.claude/skills/tengine-dev/references/conventions.md` | 代码规范全 |
| **modules.md** | `src/MyGame/ShadowGame/.claude/skills/tengine-dev/references/modules.md` | TEngine 模块说明 |
| **active.md** | `src/MyGame/ShadowGame/production/session-state/active.md` | 当前 session 历史 |
| **sprint-status.yaml** | `src/MyGame/ShadowGame/production/sprint-status.yaml` | 当前 Sprint stories 状态 |

---

## 18. 完成 onboarding 后的 first commit

新成员 onboard 完成后 first commit 检查清单：

- [ ] 读完本文档 16 节
- [ ] 跑通 `/story-readiness` skill 一次（任选 Sprint 4 ready story）
- [ ] 跑通 EditMode 测试套件 ✅（Run All 期望 ~321+ tests PASS）
- [ ] 跑通 PlayMode 一个 spike ✅（接 OnGUI 报告 + JSON evidence）
- [ ] 至少跟读 1 个 Done story（如 S3-03 scene-management/story-005-scene-events.md）+ 对应 retro 数据点
- [ ] 理解 Sprint 4 三轨架构（Track A/B/C）
- [ ] 与 tech-lead 确认 Sprint 4 first story assignment

**预计 onboarding 时间**：30-60 min（含 quick-start + 第一 sprint 4 dev-story 起手）

---

*End of Unity Workflow Onboarding*
