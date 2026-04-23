// 该文件由Cursor 自动生成

# SP-011 Findings: YooAsset Additive Scene 在 HybridCLR 热更环境下的兼容性

> **Status**: ⏳ 待验证（脚本已就绪，等待用户在 Unity Editor 运行）
> **Date**: 2026-04-22
> **Sprint**: Sprint 2 预置 Spike（0.5 点）
> **Owner**: @chen
> **Scope**: Sprint 2 Scene Management epic 前置降风险

---

## 目标

验证 `TEngine.SceneModule.LoadSceneAsync(location, LoadSceneMode.Additive)` — 它底层调用 YooAsset 2.3.17 的 `YooAssets.LoadSceneAsync` — 在 **GameLogic 热更程序集**中能正常运作，为 Sprint 2 的 Scene Management 三个故事（S2-05 Scene Loader、S2-06 Scene Transition Manager、S2-14 Memory Cleanup Sequence）去除 MEDIUM 风险。

## 3 项关键验证点（与 sprint-2 Plan 和 QA Plan 对齐）

| # | 验证点 | 方法 | 通过条件 |
|:-:|:-------|:-----|:---------|
| **P1** | 热更程序集能 await `GameModule.Scene.LoadSceneAsync(Additive)` | 直接调用，断言返回的 `Scene` | `Scene.IsValid() == true`、`isLoaded == true`、`SceneManager.sceneCount` 递增 1 |
| **P2** | `UnloadAsync` 正确释放 Additive 场景 | Load 一个场景后 Unload，观察 sceneCount 回退 | `UnloadAsync` 返回 `true` 且 `sceneCount` 回到 baseline |
| **P3** | 5×cycle Load/Unload 内存稳定 | 循环 Load/Activate/Unload（交替 SceneA/B） + `Resources.UnloadUnusedAssets` + `GC.Collect` | `sceneCount` 回到 baseline；内存 delta < 5%（软指标，硬失败仅在 sceneCount 未回退时触发） |

---

## 前置条件

### 1. 场景资源（用户手动创建）

在 Unity Editor 中：

- **新建 2 个场景**，保存为：
  - `Assets/AssetRaw/Scenes/SP011_SceneA.unity`
  - `Assets/AssetRaw/Scenes/SP011_SceneB.unity`
- 场景内可留空，或分别放置一个 `SP011_MarkerA` / `SP011_MarkerB` 空 GameObject，便于视觉确认。
- **无需改动 YooAsset Collector**：`Assets/AssetRaw/Scenes` 已注册为 `DefaultPackage / Scenes Group`（`AddressByFileName`），Address 即场景文件名（不带扩展名）。

### 2. 运行环境

- **PlayMode**: `EditorSimulateMode`（项目默认），无需构建 AssetBundle。
- **HotFix 流程**: 必须走完 `ProcedureStartGame`，确保 `GameModule.Scene` 已初始化。若更早触发会报 `GameModule.Scene == null`。

### 3. 挂载 Launcher

在 Editor 中：

1. 打开 `Assets/Scenes/main.unity`（或任意启动场景）。
2. 创建空 GameObject，命名 `SP011_Launcher`。
3. Add Component → 搜索 `SP011_YooAssetAdditiveLauncher`（来自 `GameLogic` 热更程序集）。
4. Play → Console 看 `[SP-011]` 日志 + 屏幕顶部面板显示 P1/P2/P3 状态。

> 若找不到组件：确认 GameLogic 已编译，Unity Console 有无 `.g.cs` / `asmdef` 报错（参考 `docs/problems/problem_2026-04-22_asmdef-source-generator.md`）。

---

## 期望输出

运行脚本后，Console 会打印如下汇总块，复制回本报告。

```
[SP-011] ═══════════════════════════════════════════
[SP-011]           验 证 报 告
[SP-011] ═══════════════════════════════════════════
[SP-011] P1 LoadAdditive       : ✅ PASS
[SP-011] P2 UnloadRelease      : ✅ PASS
[SP-011] P3 CycleMemory(×5)    : ✅ PASS
[SP-011] 程序集                 : GameLogic
[SP-011] ═══════════════════════════════════════════
[SP-011] 🎉 ALL PASSED — Sprint 2 Scene Management stories 可以开工
```

---

## 运行结果（填写位置）

**运行日期**: <YYYY-MM-DD>
**执行者**: <姓名>
**Unity 版本**: 2022.3.62f2 LTS
**PlayMode**: EditorSimulateMode
**设备**: macOS Editor（OSXEditor）

### P1: LoadSceneAsync(Additive)

- [ ] PASS / FAIL：
- 返回 Scene.name：
- 加载耗时 (ms)：
- 备注：

### P2: UnloadAsync

- [ ] PASS / FAIL：
- 卸载耗时 (ms)：
- 备注：

### P3: 5-cycle Load/Unload

- [ ] PASS / FAIL：
- 总耗时 (ms)：
- Baseline memory：  MB
- After-cycle memory：  MB
- Delta %：
- 备注：

### 总结

- [ ] ALL PASSED → Sprint 2 Scene Management 故事解除风险，按原计划执行
- [ ] 部分失败 → 见下方「失败应对方案」

---

## 失败应对方案

| 场景 | 应对 |
|:-----|:-----|
| P1 失败：`Scene` 无效 | 检查 `Assets/AssetRaw/Scenes/` 下场景是否真实存在；确认 YooAsset Collector Groups 的 Scenes Group 仍启用；Build Asset Bundle Collector（编辑器菜单）后重试 |
| P1 失败：`GameModule.Scene == null` | 确认已走完 `ProcedureStartGame`，考虑加大 `_delaySeconds`（默认 1 秒）到 3 秒 |
| P2 失败：`UnloadAsync` 返回 false | 检查 location 字符串是否完全匹配（区分大小写、不含扩展名） |
| P3 失败：`sceneCount` 未回到 baseline | 场景泄漏 — **阻塞 S2-05**，需先修 TEngine.SceneModule 或回退到原生 `SceneManager` |
| P3 警告：内存 delta > 5% | 非硬失败，记录为 S2-14 Memory Cleanup Sequence 的已知注意点 |

---

## 对 Sprint 2 故事的影响（决策矩阵）

| 故事 | ALL PASSED 情况 | P1 失败 | P2 失败 | P3 失败 |
|------|:---------------:|:-------:|:-------:|:-------:|
| **S2-05** Scene Loader | 按计划实现 `SceneManager.LoadChapterSceneAsync(Additive)` | ⛔ 暂缓，切换 S2-05 为「原生 SceneManager 骨架」 | 按计划 | ⛔ 暂缓 |
| **S2-06** Scene Transition | 按计划 | 无影响（依赖 S2-05） | 按计划 | 按计划 |
| **S2-14** Memory Cleanup Sequence | 按计划 | 按计划 | ⛔ 增加 `sceneUnloadRequested` 超时告警 | ⚠️ 调整 AC：内存下降门槛调低到 ±10% |

---

## 与既有 ADR 的关系

- **ADR-005 YooAsset 包策略**：SP-003 确认单包策略。本 Spike 进一步验证单包下的 Additive Scene 功能。**无需修改 ADR-005**。
- **ADR-002 HybridCLR 热更边界**：本 Spike 是对 HybridCLR 热更程序集访问 YooAsset API 的第二次验证（首次为 SP-007）。**无需修改 ADR-002**。

---

## 参考代码

- 测试脚本：[`src/MyGame/ShadowGame/Assets/GameScripts/HotFix/GameLogic/Test/SP011_YooAssetAdditiveTest.cs`](../../src/MyGame/ShadowGame/Assets/GameScripts/HotFix/GameLogic/Test/SP011_YooAssetAdditiveTest.cs)
- 封装层：[`src/MyGame/ShadowGame/Assets/TEngine/Runtime/Module/SceneModule/SceneModule.cs`](../../src/MyGame/ShadowGame/Assets/TEngine/Runtime/Module/SceneModule/SceneModule.cs)
- Collector 配置：[`src/MyGame/ShadowGame/Assets/Editor/AssetBundleCollector/AssetBundleCollectorSetting.asset`](../../src/MyGame/ShadowGame/Assets/Editor/AssetBundleCollector/AssetBundleCollectorSetting.asset)（行 119-131：Scenes Group）

---

## 签收

- [ ] 所有 3 项 PASS：更新 `production/sprint-status.yaml` 的 `spikes.SP-011.status: "completed"`
- [ ] 任一项 FAIL：按上方「决策矩阵」调整 Sprint 2 对应故事，并在 `active.md` 的 Risks 部分登记新风险
