// 该文件由Cursor 自动生成

# 问题记录：测试 / Spike 代码违反"启动场景最小化"原则

> **Date**: 2026-04-23
> **Category**: 架构 / 启动流程
> **Severity**: MEDIUM（不影响功能正确性，但污染启动场景 + 违背 TEngine 分层）
> **Status**: ✅ 已解决（架构改造 + 规则沉淀）

---

## 问题描述

执行 SP-011 YooAsset Additive Scene Spike 时，采取的做法是：

1. 在 `main.unity`（冷启动场景）里手动 `gameobject-create` 一个 `SP011_Launcher`
2. `gameobject-component-add` 挂上 `SP011_YooAssetAdditiveLauncher : MonoBehaviour`（来自 `GameLogic` 热更程序集）
3. Launcher 在 `main.unity` 场景一 Awake 就尝试访问 `GameModule.Scene`
4. 但此时 `ProcedureStartGame` 尚未跑完 → `GameApp.Entrance` 尚未被调用 → `GameModule.Scene == null`
5. 只能用 **30 秒轮询**兜底等 `GameModule.Scene != null`

这整条路径有 4 个反模式叠加：

### 反模式 1: 污染冷启动场景

`main.unity` 按照 TEngine 约定**只挂一个 `GameEntry` MonoBehaviour** 作为 Launcher。往里面塞业务 / 测试 / Spike 组件等于：

- 破坏"启动场景不变、业务动态加载"的分层
- Git diff 把场景变更和业务变更混在一起
- Release 包还需要额外记得清理，否则有残留

### 反模式 2: 生命周期错位

`main.unity` 里挂的 MonoBehaviour 在场景激活时（`Awake` / `Start`）立即运行，而此时：

- `ProcedureSplash → ProcedureInitPackage → ... → ProcedureLoadAssembly` 整条主包启动链还没走完
- 热更程序集 `GameLogic.dll` 甚至可能还未加载（或刚加载，`GameApp.Entrance` 未执行）
- 访问 `GameModule.Scene`、`ConfigSystem.Instance` 等全部是 `null`

### 反模式 3: 轮询兜底

因反模式 2 的副作用，Launcher 只能写 **30 秒 `while GameModule.Scene == null` 轮询**，这是典型的"症状治疗"：

```csharp
// ❌ BAD
int waitTicks = 0;
while (GameModule.Scene == null && waitTicks < 60)
{
    await UniTask.Delay(TimeSpan.FromSeconds(0.5));
    waitTicks++;
}
```

不仅丑陋，还给后来人留坑：以后有人拷这段代码到别处，都会继承这个"先挂后等"的坏习惯。

### 反模式 4: 与线上路径不一致

Release 包里不可能有 `SP011_Launcher` GameObject。这意味着 Spike 验证的是"Editor + 手动挂载"这条独特路径，**不等同于**线上实际走的"`ProcedureStartGame` → `GameApp.Entrance` → 业务 FSM"路径。虽然本次 SP-011 结论仍然可信（因为底层的 `GameModule.Scene` API 一致），但这是侥幸。

---

## 根因

**缺失的架构约束**：CLAUDE.md / AGENTS.md / `.cursor/rules/` 里没有明文规定"测试 / Spike / 诊断代码应该放在哪个生命周期阶段"。agents 看到"要验证 `GameModule.Scene`"就按最直觉的方式（挂 MonoBehaviour 到场景）做，绕过了 `GameApp.Entrance` 这个正确入口。

---

## 解决方案

### 架构设计

引入 `DevTest` 层 + FSM State 分叉：

```
main.unity (只挂 GameEntry)
  ↓ GameEntry.Awake → 主包 Procedure FSM
  ↓ ... → ProcedureLoadAssembly → 反射 GameApp.Entrance
  ↓
GameApp.Entrance:
  ├─ GameEventHelper.Init()
  ├─ ConfigSystem.Instance.Load()
  ├─ [#if UNITY_EDITOR || DEBUG] DevBootstrap.Register(new SP011Spike())
  └─ StartGameLogic() 启动业务 FSM
       │
       ├─ GameLoadingState
       │    OnLoadingComplete:
       │      #if UNITY_EDITOR || DEBUG
       │          ChangeState<DevTestState>       ← DEBUG 分叉
       │      #else
       │          // Release 暂停留（业务 UI 就绪后接 GameLobbyState）
       │
       ├─ GameLobbyState / LevelLoadingState / GameplayState / LevelEndState
       │
       └─ [#if UNITY_EDITOR || DEBUG] DevTestState
            OnEnter:
              DevBootstrap.RunRequested()
              → 动态 new GameObject + AddComponent<XxxRuntime>
              → DontDestroyOnLoad
              → OnGUI 持续显示结果直到手动停 PlayMode
```

### 代码结构

```
Assets/GameScripts/HotFix/GameLogic/
├── GameApp.cs                          [修改]
├── GameFlow/
│   ├── GameLoadingState.cs             [修改] DEBUG 分叉到 DevTestState
│   └── DevTestState.cs                 [新增] 整文件 #if UNITY_EDITOR || DEBUG
└── DevTest/                            [新增目录]
    ├── IDevSpike.cs                    interface
    ├── DevBootstrap.cs                 Register + RunRequested
    └── Spikes/
        └── SP011_YooAssetAdditive.cs   SP011Spike + SP011Runtime + SP011Tester
```

### 编码红线（第 7 条）写入核心规则

- `src/MyGame/ShadowGame/CLAUDE.md` — 核心原则（编码红线）第 7 条
- `src/MyGame/ShadowGame/AGENTS.md` — 升级为"七条编码红线"
- `.cursor/rules/shadowgame-tengine.mdc` — 同步第 7 条 + 强制执行检查点

---

## 与既有规则的关系

| 规则 | 来源 | 本次如何呼应 |
|------|------|------------|
| TEngine 任务前置协议 | `problem_2026-04-22_tengine-skill-violation.md` | 本次修改前强制读 `tengine-dev/SKILL.md`（已读 `hotfix-development.md` / `architecture.md`）|
| MCP 绑定项目归属校验 | `problem_2026-04-23_wrong-unity-project.md` | 本次无 MCP 写操作，纯代码修改 |
| MonoBehaviour 文件命名 | `problem_2026-04-23_monobehaviour-filename-mismatch.md` | 新的 `SP011Runtime` 是 public 顶层类，文件名含其它类（SP011Spike / SP011Tester）— 但 SP011Runtime 不再由 Inspector 手动挂载，而是由代码 `AddComponent<SP011Runtime>()`，**代码挂载不受该红线限制**（该红线只针对 Inspector 搜索场景）。为保守起见仍可拆分，但考虑本 Spike 一次性用途，保持单文件以降低维护成本 |

---

## 预防机制

### 编码阶段

- CLAUDE.md / AGENTS.md / `.cursor/rules/` 明文第 7 条红线：main.unity 只挂 GameEntry
- 所有测试 / Spike / 诊断代码必须走 `IDevSpike` → `DevBootstrap` → `DevTestState` 路径
- 所有相关文件必须用 `#if UNITY_EDITOR || DEBUG` 包裹，Release 包零残留

### 检查点

在 `.cursor/rules/shadowgame-tengine.mdc` 的强制执行检查点清单里追加：

- [ ] 涉及测试 / Spike / 诊断代码：**未**静态挂到 `main.unity`，**已**用 `IDevSpike` + `DevBootstrap` 注册 + `#if UNITY_EDITOR || DEBUG` 包裹

### PR 审查

- 凡修改 `main.unity` 的 PR 都需要解释改动必要性（默认不应该改）
- 凡新增 `MonoBehaviour` 直接挂到启动场景的代码必须被拒

---

## 后续 TODO（不阻塞本次）

- `SP007_HybridCLRAsyncGPUTest.cs` 位于 `Test/` 目录，也是 MonoBehaviour。虽然当前未挂到任何场景，但为保持架构一致，后续可以迁移到 `DevTest/Spikes/` 并提供 `IDevSpike` 包装
- 当业务 UI 实际接入时，移除 `GameLoadingState.OnLoadingComplete` 的 Release 分支空实现，恢复 `ChangeState<GameLobbyState>`
