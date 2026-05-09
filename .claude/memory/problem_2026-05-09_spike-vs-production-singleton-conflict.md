// 该文件由Cursor 自动生成

# 问题记录：Spike 自构建实例 vs Production singleton 冲突 — M1 dual-layer 解决方案

> **日期**: 2026-05-09
> **发生工程**: MyGameStudio / src/MyGame/ShadowGame (Sprint 5 S5-1b dev-story)
> **发生次数**: 第 2 次（Sprint 3 期间 SP-011 + S3-01 spike 撞过 YooAsset 锁，当时用"单 spike 模式"回避，没正面解；S5-1b 是首次正面解）
> **严重性**: 中（spike 设计层面 — 错了会导致 R3 PlayMode probe 跑不动 / 死锁 / event 不到 / 资源 leak；正面解了反而是 R3 设计上限提升）

---

## 问题现象

S5-1b 设计 R3 PlayMode probe 时撞到一个 spike 设计悖论：

| Case | 想测什么 | 自然实现 | 冲突点 |
|------|----------|----------|--------|
| P1 first-boot | production `_sceneManager.BeginTransitionAsync(1)` 8 lifecycle event 顺序 | spike new 一个本地 SceneManager + RegisterChapterDataProvider + BeginTransitionAsync | 与 production `GameApp._sceneManager` 抢 YooAsset scene handle —— Additive scene 同名只能加载一份 |
| P2 same-chapter dedupe | 第 2 次调 BeginTransitionAsync(1) 不重 load | 同上 | 同上 + production scene 已 active 时 spike 本地 SceneManager 调 LoadSceneAsync 行为未定义 |
| P3 unload | UnloadCurrentChapterAsync() 后 OnSceneUnloadBegin 派发 + state=Idle | 同上 | spike 本地 SceneManager 没 chapter 1 状态可 unload |
| **P4 unknown chapter** | provider 返 null → fail-loud OnSceneLoadFailed | spike new 本地 SceneManager + RegisterChapterDataProvider 返 null | **不能与 production 共用 _sceneManager** — production fixture provider 不能返 null（会污染 production state） |
| **P5 provider null** | 不 RegisterChapterDataProvider → fail-loud | spike new 本地 SceneManager + 跳过 RegisterChapterDataProvider | 同 P4 — production 已 register 不能撤 |

**核心悖论**：

- P1/P2/P3 happy path 想跑 **production singleton 的真实状态**（不然 boot pipeline 接入根本没 verify 到）
- P4/P5 fail-loud path 想跑 **隔离实例**（不然异常分支会污染 production 状态 + 抢 YooAsset 锁）

如果统一用 production singleton：P4/P5 跑不了（污染 + 抢锁）。
如果统一用 spike 自构建：P1/P2/P3 测的是"spike 自己的 SceneManager 而非 production 接入"，AC 失效。

---

## 根因

这是 Unity / TEngine 项目里 **"singleton-style production state vs spike isolated state"** 一个通用 design tension:

1. **Production 端有真实 singleton 资源占用**（YooAsset scene handle / Audio handle / FsmModule registered fsm / GameModule.UI canvas 等）
2. **Singleton 设计本身排斥多实例**（同名 scene 同时只能 active 一份；fsm name 全局唯一；canvas root 全局唯一）
3. **R3 PlayMode probe 既要测 happy path** (verify production 接入正确) **又要测 fail-loud** (verify 异常分支正确)
4. **同 spike session 两路并存** → 资源争抢 / state 污染 / event listener 错位

Sprint 3 期间 S3-01 / SP-011 撞同类问题，当时回避方案是**"单 spike 模式 + 注释关掉其他 spike"**（一次 PlayMode 只跑一个 spike），但这只回避了"多 spike 之间冲突"，没解决"单 spike 内既测 production 接入又测 fail-loud"的需求。

---

## M1 dual-layer 解决方案（S5-1b 实测有效）

**核心思路**：单 spike 内**两套互不干扰的 SceneManager 引用** + **case 编号决定走哪套**:

```csharp
public class S51bTester
{
    private SceneManager _localScene;  // P4/P5 用 — isolated 隔离实例
    private SceneManager _prodScene;   // P1/P2/P3 用 — 反射拿 GameApp._sceneManager

    public async UniTask RunAllAsync()
    {
        // ===== Layer 1: production reflection (happy path) =====
        _prodScene = GetProductionSceneManager();  // reflect GameApp._sceneManager
        if (_prodScene == null) { /* fail-loud */ return; }

        SubscribeProductionEvents();
        await RunP1Async();  // verify production first-boot 8 events
        await RunP2Async();  // verify same-chapter dedupe on production
        await RunP3Async();  // verify unload on production
        UnsubscribeProductionEvents();

        // ===== Layer 2: isolated local instance (fail-loud) =====
        await RunP4Async();  // new local SceneManager + null provider → fail-loud
        await RunP5Async();  // new local SceneManager + skip register → fail-loud

        WriteResultJson();
    }

    private static SceneManager GetProductionSceneManager()
    {
        var fi = typeof(GameApp).GetField("_sceneManager",
            BindingFlags.NonPublic | BindingFlags.Static);
        return fi?.GetValue(null) as SceneManager;
    }

    private async UniTask RunP4Async()
    {
        _localScene = new SceneManager();
        _localScene.Init();
        _localScene.RegisterChapterDataProvider(id => null);  // 故意返 null
        _localScene.RegisterFadeOverlay(new NoOpFadeOverlay());
        try
        {
            UnityEngine.TestTools.LogAssert.Expect(LogType.Error,
                new System.Text.RegularExpressions.Regex(".*chapter 99.*"));
            await _localScene.BeginTransitionAsync(99);
        }
        finally { _localScene.Dispose(); _localScene = null; }
    }
}
```

**关键约束**：

1. **time-shift**：Layer 1 全部 case 跑完 + 反订阅 production events 后才 enter Layer 2 — 不能并行
2. **resource isolation**：Layer 2 用 chapter id 99 (unknown) / null provider → 不抢 YooAsset scene handle（YooAsset 在 Failed 状态不持有 handle）
3. **expected error sentinel**：Layer 2 用 `UnityEngine.TestTools.LogAssert.Expect(LogType.Error, regex)` 标 P4/P5 expected error → console 不会因 fail-loud 把整个 PlayMode test 判 FAIL
4. **dispose loop**：Layer 2 每个 case 自己 new + dispose — 不留 dangling state 给下一个 case

**适用前提**：

- production 端有 stable singleton accessor（field 反射 / `GameApp.Instance.SceneManager` property）
- spike 用的 system 自身**支持 multiple instance**（即 ctor + Init 没硬编码 singleton 检查）— ShadowGame `SceneManager` 是 POCO 不是 `Singleton<T>` 所以 OK；如果是 `MonoSingleton<UIModule>` 这类则 dual-layer 不可行（要用 mock）

---

## 反过来：什么时候 dual-layer 不适用

| 场景 | 替代方案 |
|------|----------|
| Production 类是 `Singleton<T>` / `MonoSingleton<T>` 强制单例 | spike 全用 production singleton + state 重置 hook (`ResetForTest` testhook) |
| Spike 系统状态污染影响其他 PlayMode test | 单 spike 模式（GameApp.RegisterDevSpikes 只注册当前 spike）+ 注释其他 spike — Sprint 3 模式 |
| 系统资源是全局排他的（如 AudioListener / Main Camera tag） | 不要测 fail-loud，只测 happy path；fail-loud case 改 EditMode unit test (隔离构造直接验) |
| Fail-loud 路径只是 throw / Log.Error，不涉及资源持有 | EditMode unit test 替代（更轻 + 更快），不用 spike |

---

## Agent 自检 checklist（仅本工程，写 spike 时）

- [ ] 当前 spike 是否要同时测 happy path（要 production state）+ fail-loud path（要 isolated state）？
- [ ] 如是，production 端 singleton accessor 怎么拿？（公共 property / 反射 private static field）
- [ ] Production 系统支不支持 ctor + Init 多实例？grep `Singleton<` / `MonoSingleton<` 判定
- [ ] 写 dual-layer 时 Layer 1 / Layer 2 是否 time-shift（**不**并行）？
- [ ] Layer 2 用什么 chapter id / asset key 保证不抢 YooAsset 锁 / 不污染 production state？（用 unknown id / null provider 让 framework 早早 fail-loud）
- [ ] Layer 2 expected error 是否用 `LogAssert.Expect` 标记，避免把整个 PlayMode 判 FAIL？
- [ ] Layer 2 dispose 路径是否在 finally 内、case 之间不串 state？
- [ ] Spike 文件结构是否符合本工程 "1 文件 + 3 内类"（`*Spike : IDevSpike` + `*Runtime : MonoBehaviour` + `*Tester` 纯逻辑）惯例？

---

## 与其他文档的关系

- 与 `~/.cursor/rules/*` 跨工程 rule 正交（dual-layer 是 ShadowGame TEngine + IDevSpike + SceneManager singleton 高度耦合的设计模式，跨工程 rule 难抽象）
- 与 `S5-1b evidence doc` (`production/qa/playmode-bootscene-load-2026-05-09.md` §M1 详述): 两处一致 surface
- 与 `ADR-029 V2.0` R3 PlayMode probe mandatory: 本 dual-layer 模式扩展了 R3 的 spike 设计上限（V2-5 listener self-removal × N cycles 之外，再增加 V2-? "dual-layer production reflection + isolated local"候选）；Sprint 5 retro 评估是否 promote 为 V3 candidate
- 与 Sprint 3 "单 spike 模式 + 注释其他 spike" 路径 supersede 关系：单 spike 模式是回避方案，dual-layer 是正面解；两者互补 — 多 spike 之间冲突仍走单 spike 模式，单 spike 内 happy + fail-loud 共存走 dual-layer

---

## 历史记录

- **2026-05-09 创建**（Session 25 #2）。触发场景：S5-1b dev-story user 选 [Q1=A] DevTestState auto-trigger 后，spike 设计阶段发现自构建 SceneManager 与 production singleton 冲突；用户选 [A] (M1) 推荐 — production reflection 拿 prod scene + Layer 2 isolated 隔离 P4/P5；R3 PlayMode 5/5 PASS + 22/22 asserts first-run 验证 dual-layer 设计有效。按 `problem-to-rule-promotion.mdc` 协议判定为"反复发生 ≥ 2 次（Sprint 3 SP-011/S3-01 撞过同类）+ 修复路径不显然 + 与 ShadowGame TEngine + IDevSpike + SceneManager 高度耦合"，仅写本工程 lessons memo，不沉淀跨工程 cursor rule。
- **预期 follow-up**：Sprint 5 retro 评估 dual-layer 是否 promote 为 ADR-029 V3 candidate "spike resource isolation pattern"；S5-1b spike 文件 (`Assets/GameScripts/HotFix/GameLogic/DevTest/Spikes/S5-1b_BootSceneLoad.cs`) 作为 dual-layer 实施模板供后续 spike (chapter 切换 / 真 fade overlay 替换 / Luban migration story) 引用。
