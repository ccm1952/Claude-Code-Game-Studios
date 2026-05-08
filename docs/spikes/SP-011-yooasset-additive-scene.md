// 该文件由Cursor 自动生成

# SP-011 Findings: YooAsset Additive Scene 在 HybridCLR 热更环境下的兼容性

> **Status**: ✅ **ALL PASSED** — Sprint 2 Scene Management epic 风险解除
> **Date (Plan)**: 2026-04-22
> **Date (Execution)**: 2026-04-23
> **Sprint**: Sprint 2 预置 Spike（0.5 点）
> **Owner**: @chen
> **Scope**: Sprint 2 Scene Management epic 前置降风险
> **Verdict**: 🟢 **PASS** — S2-05 / S2-06 / S2-14 按原计划执行，无调整

---

## 目标

验证 `TEngine.SceneModule.LoadSceneAsync(location, LoadSceneMode.Additive)` — 它底层调用 YooAsset 2.3.17 的 `YooAssets.LoadSceneAsync` — 在 **GameLogic 热更程序集**中能正常运作，为 Sprint 2 的 Scene Management 三个故事（S2-05 Scene Loader、S2-06 Scene Transition Manager、S2-14 Memory Cleanup Sequence）去除 MEDIUM 风险。

## 3 项关键验证点

| # | 验证点 | 方法 | 通过条件 | 实际结果 |
|:-:|:-------|:-----|:---------|:--------:|
| **P1** | 热更程序集能 await `GameModule.Scene.LoadSceneAsync(Additive)` | 直接调用，断言返回的 `Scene` | `Scene.IsValid() == true`、`isLoaded == true`、`SceneManager.sceneCount` 递增 1 | ✅ PASS |
| **P2** | `UnloadAsync` 正确释放 Additive 场景 | Load 一个场景后 Unload，观察 sceneCount 回退 | `UnloadAsync` 返回 `true` 且 `sceneCount` 回到 baseline | ✅ PASS |
| **P3** | 5×cycle Load/Unload 内存稳定 | 循环 Load/Activate/Unload（交替 SceneA/B）+ `Resources.UnloadUnusedAssets` + `GC.Collect` | `sceneCount` 回到 baseline；内存 delta < 5%（软指标） | ✅ PASS |

---

## 运行结果

| 项 | 值 |
|----|----|
| **运行日期** | 2026-04-23 16:00:31 (北京时间) |
| **执行方式** | MCP 自动化（Cursor + unity-mcp bridge） + 手动 Add Component |
| **Unity 版本** | 2022.3.62f2 LTS |
| **PlayMode 模式** | EditorSimulateMode |
| **运行设备** | macOS Editor (OSXEditor) |
| **挂载入口** | `Assets/Scenes/main.unity` → `SP011_Launcher` GameObject → `SP011_YooAssetAdditiveLauncher` |
| **测试程序集** | **GameLogic** （HybridCLR 热更程序集，与线上运行同一路径）✅ |

### P1: LoadSceneAsync(Additive)

- ✅ **PASS**
- 返回 Scene.name: `SP011_SceneA`
- Scene.IsValid(): `true`
- Scene.isLoaded: `true`
- sceneCount: 1 → 2 (+1)

### P2: UnloadAsync

- ✅ **PASS**
- UnloadAsync 返回: `true`
- sceneCount 回退: 2 → 1 (baseline)

### P3: 5-cycle Load/Unload（交替 SceneA / SceneB）

- ✅ **PASS**
- 5 轮循环完成：SceneA / SceneB / SceneA / SceneB / SceneA
- 每轮均包含 `LoadSceneAsync(Additive)` → `ActivateScene` → `UnloadAsync`
- Baseline memory: **276.01 MB**
- After-cycle memory: **272.50 MB**
- Delta: **-3.51 MB (-1.27%)** — 内存**不升反降**，释放完全到位
- sceneCount: 1 → 1（baseline，无场景泄漏）

### 证据文件

- **结构化结果**: `~/Library/Application Support/DefaultCompany/Unity/SP011_Result.json`

```json
{
  "timestamp": "2026-04-23T08:00:31.7178140Z",
  "phase": "done",
  "p1_loadAdditive": true,
  "p2_unloadRelease": true,
  "p3_cycleMemory": true,
  "allPassed": true,
  "sceneCountBaseline": 1,
  "sceneCountNow": 1,
  "memoryBaselineMB": 276.01,
  "memoryNowMB": 272.50,
  "assembly": "GameLogic",
  "statusText": "ALL PASSED",
  "lastError": "",
  "persistentDataPath": "/Users/chen/Library/Application Support/DefaultCompany/Unity"
}
```

- **屏幕截图**: `assets/image-5820fe56-ed2f-456e-a68d-ddbd48590b04.png`（OnGUI 面板 ALL PASSED 绿字）
- **OnGUI 显示确认**: `Assembly: GameLogic`，三行 `✅` 齐全，底部绿色 `ALL PASSED`

### 总结

- [x] **ALL PASSED** → Sprint 2 Scene Management 故事解除风险，按原计划执行
- [ ] 部分失败 → 见下方「失败应对方案」

---

## 对 Sprint 2 故事的影响（实际决策）

| 故事 | 原风险 | 决策 |
|------|:------:|:----|
| **S2-05** Scene Loader | MEDIUM | 🟢 **按原计划**使用 `GameModule.Scene.LoadSceneAsync(Additive)`，无需回退到原生 SceneManager |
| **S2-06** Scene Transition Manager | MEDIUM | 🟢 按原计划 |
| **S2-14** Memory Cleanup Sequence | MEDIUM | 🟢 按原计划；**内存验收标准保持 ±5%**（本次 -1.27% 证明该标准可达） |

---

## 与既有 ADR 的关系

- **ADR-005 YooAsset 包策略**：SP-003 确认单包策略。本 Spike 进一步验证单包下的 Additive Scene 功能。**无需修改 ADR-005**。
- **ADR-002 HybridCLR 热更边界**：本 Spike 是对 HybridCLR 热更程序集访问 YooAsset API 的第二次验证（首次为 SP-007 API 路径 Spike）。Assembly 字段显示 `GameLogic` 而非 `Assembly-CSharp`，再次确认 HotFix 边界正确。**无需修改 ADR-002**。

---

## 执行过程中发现并修复的问题（副作用产出）

| 问题 | 归档位置 | 影响规则升级 |
|------|---------|-----------|
| MCP 绑定项目归属在多 Unity 实例场景下的误判风险 | `/.claude/memory/problem_2026-04-23_wrong-unity-project.md` | CLAUDE.md「MCP 绑定项目归属校验」（三级优先级 + Editor.log 不可信警示） |
| MonoBehaviour 类与 `.cs` 文件名不一致导致 Inspector 搜不到、Awake 不执行 | `/.claude/memory/problem_2026-04-23_monobehaviour-filename-mismatch.md` | CLAUDE.md 编码红线第 6 条 + 挂载前 Glob 自检 |
| TEngine 前置协议执行度不足（首次触发时未读 tengine-dev SKILL.md） | `/.claude/memory/problem_2026-04-22_tengine-skill-violation.md` | CLAUDE.md「🎯 TEngine 任务前置协议（MUST）」整节 |

---

## 参考代码

- 测试脚本（纯 C# 验证逻辑）: [`src/MyGame/ShadowGame/Assets/GameScripts/HotFix/GameLogic/Test/SP011_YooAssetAdditiveTest.cs`](../../src/MyGame/ShadowGame/Assets/GameScripts/HotFix/GameLogic/Test/SP011_YooAssetAdditiveTest.cs)
- 挂载入口（MonoBehaviour）: [`src/MyGame/ShadowGame/Assets/GameScripts/HotFix/GameLogic/Test/SP011_YooAssetAdditiveLauncher.cs`](../../src/MyGame/ShadowGame/Assets/GameScripts/HotFix/GameLogic/Test/SP011_YooAssetAdditiveLauncher.cs)
- TEngine 封装层: [`src/MyGame/ShadowGame/Assets/TEngine/Runtime/Module/SceneModule/SceneModule.cs`](../../src/MyGame/ShadowGame/Assets/TEngine/Runtime/Module/SceneModule/SceneModule.cs)
- Collector 配置: [`src/MyGame/ShadowGame/Assets/Editor/AssetBundleCollector/AssetBundleCollectorSetting.asset`](../../src/MyGame/ShadowGame/Assets/Editor/AssetBundleCollector/AssetBundleCollectorSetting.asset)（行 119-131：Scenes Group）
- 测试场景: `Assets/AssetRaw/Scenes/SP011_SceneA.unity` / `SP011_SceneB.unity`

---

## 签收

- [x] 所有 3 项 PASS → 更新 `production/sprint-status.yaml` 的 `spikes.SP-011.status: "completed"`
- [x] 解除 S2-05 / S2-06 / S2-14 的 MEDIUM 风险（Scene Management 相关 story 可按原计划开工）
- [x] 测试脚本保留供后续 Sprint 2 Scene Management 集成测试参考（不删除）
- [x] SP011_Launcher GameObject 已从 main.unity 移除
- [x] **架构改造 + 二次验证**（2026-04-23 16:34）

---

## 补：架构改造后的二次验证（2026-04-23 16:34）

针对首次验证的副作用"把 Spike MonoBehaviour 静态挂到 main.unity"（违反启动场景最小化原则），重构为 `IDevSpike` + `DevBootstrap` + `DevTestState` 架构后**全量重跑**：

| 指标 | 旧路径（main.unity 静态挂载） | 新路径（DevTestState 动态挂载） |
|------|:-----:|:-----:|
| 时间戳 | 2026-04-23 16:00:31 | **2026-04-23 16:34:25** |
| allPassed | ✅ | ✅ |
| baseline MB | 276.01 | 281.15 |
| after MB | 272.50 | 276.86 |
| 内存 delta | -1.27% | **-1.53%** |
| Assembly | GameLogic | **GameLogic**（保持热更域验证一致性） |

**结论**：
- 新架构下 SP-011 ALL PASSED，**验证结论不受架构改造影响**
- `main.unity` 现仅挂 `GameEntry`，启动场景最小化原则已落地
- SP011 Spike 代码整文件 `#if UNITY_EDITOR || DEBUG` 包裹，Release 包零残留

架构改造的详细讨论 + 规则沉淀见 [`/.claude/memory/problem_2026-04-23_test-mount-architecture.md`](../../.claude/memory/problem_2026-04-23_test-mount-architecture.md) 与 CLAUDE.md 编码红线第 7 条。
