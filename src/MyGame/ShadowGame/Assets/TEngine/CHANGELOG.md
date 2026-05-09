# Changelog

All notable changes to this package will be documented in this file.

> **ShadowGame 注**：本仓库的 TEngine 是 **vendor in-tree** 副本（不是 UPM 包），版本号保留在 `package.json` 的 `6.0.0` 不变（避免版本号语义模糊）。每次从上游 cherry-pick patch 都在本文件追加 `6.0.0+shadowgame-patches.<DATE>` 形式的 entry，同时注明上游 issue/PR 号。

---

## 6.0.0+shadowgame-patches.20260509 (2026-05-09) — doc-fix

**Scope**: 仅文档修订，无 code 改动。

### Fixed

- `.claude/skills/tengine-dev/references/hotpatch-management.md` 的「完整启动流程链路」节修订为**本工程真实链路**：
  - 删除虚构节点 `ProcedureUpdateVersion` / `ProcedureUpdateManifest`（5-08 sync 时误按上游标准链路写入，本工程实际不存在）
  - 新增本工程独有节点 `ProcedureGetVersion` ⭐（UnityWebRequest 拉 `version.txt` → `SetRemoteServicesUrl(hostUrl, hostUrl)`），位于 `Splash → InitPackage` 之间
  - manifest 更新合并到 `ProcedureInitResources` 的事实补充说明（无独立节点）
  - 新增 13 节点清单表 + 后继关系（按 ChangeState 实测反推）
  - 新增 Mermaid FSM 跳转图（含 InitResources / CreateDownloader / DownloadFile / DownloadOver 4 处分叉）
  - 新增 Editor 模式实际路径段 + 「GetVersion 始终被进入」警示
  - 新增与上游 TEngine 标准链路对比表 + 「禁止回退」标注

**Ground truth 来源**：`Glob Assets/GameScripts/**/Procedure*.cs`（13 文件） + `Grep ChangeState`（21 处调用） + `Read ProcedureGetVersion.cs` 全文。

---

## 6.0.0+shadowgame-patches.20260508 (2026-05-08)

**Sync source**: TEngine upstream 6.2.1 selected bug fixes（不同步 feature）。
**Strategy**: P0/P1 高影响 bug fix 全量同步；feature update 跳过；agent skill 流程对齐上游。

### Bug fixes（cherry-pick from upstream 6.2.1）

| 上游来源 | 影响文件 | 修复内容 |
|---------|---------|---------|
| issue #253 | `Assets/TEngine/Runtime/Module/ResourceModule/ResourceModule.cs` | `LoadAssetAsync` / `LoadGameObjectAsync` 在 cancelOrFailed 分支补 `handle.Dispose()`，修复异步加载取消/失败时 YooAsset bundle 引用计数不归零的资源泄漏 |
| 6.2.1 commit | `Assets/TEngine/Runtime/Module/ResourceModule/Reference/AssetsReference.cs` | `OnDestroy` 移除 `_originalRefs` 自身条目；`Ref` 用 indexer 替代 `ContainsKey + Add`，修复同 GameObject 重复 Ref 时引用不更新 → 后续克隆判定失败 |
| PR #248 思路移植 | `Assets/GameScripts/HotFix/GameLogic/SingletonSystem/SingletonSystem.cs` | `Release()` 主动清理 `_updates` / `_fixedUpdates` / `_lateUpdates` / `_drawGizmos*` 生命周期 lists + 单例释放加 null-conditional + 显式 DeInit；防止 Restart 流程残留旧引用 |

> **跳过同步说明**：
> - **B4** EventMgr 由 `Dictionary<string, ...>` 改为 `Dictionary<Type, ...>`：本工程 EventMgr.cs 早前已手工同步（v6.2.0），此次确认无需重复 patch。
> - **B5** `[EventInterface]` 与 Obfuz 混淆兼容：本工程未启用 Obfuz，跳过。
> - **B6** Module 跨命名空间访问：本工程已正常工作，跳过。
> - **B7** Hotfix 流程 `cancelOrFailed` 取反 + 参数顺序调整：本工程未启用 hotpatch flow（`UpdateSetting.Enable = false`），跳过。

### tengine-dev skill references 修订

`.claude/skills/tengine-dev/references/` 6 个文档增量修订（不重写、按主题追加章节）：

- `event-system.md` — 追加「框架 v6.2.x 修订亮点」（EventMgr type-key 升级、不存在的 GameEvent API 警告、Lambda 捕获反模式、高频事件风暴反模式）+「Sprint 5 drift v2-(b) — ISettingsEvent.OnSettingChanged 真签名」
- `modules.md` — 追加 GameModule 完整入口（Base / Debugger / Procedure / Localization 等）、SceneModule progress 真实参数名、Log 系统完整 API + Sprint 5 AudioModule 自动 Initialize drift
- `troubleshooting.md` — 追加 v6.2.x 速查表（UIWindow 生命周期签名、SetSprite callback 类型、GameEvent 清理方法）+ Sprint 5 spike-design parity check 经验
- `resource-management.md` — 追加 SetSprite/SetSubSprite 4 个签名速查、句柄式加载（LoadAssetSyncHandle / LoadAssetAsyncHandle）、SetRemoteServicesUrl 双地址
- `hotpatch-management.md` — 追加 GameApp.Entrance 真签名（`object[]` 而非 `Assembly[]`）+ partial class 拓展模式 + 完整启动流程链路
- `conventions.md` — 追加 UIScriptGenerator 长前缀优先匹配警示 + `m_canvas_` / `m_dropdown_` / `m_tmpInput_` / `m_tmpDropdown_` / `m_richText_` 5 个新前缀

### Cursor rule 硬规则补建

`.cursor/rules/shadowgame-tengine.mdc` 末尾新增「框架 vendor patch 硬规则（v6.2.x sync 后必守）」节，4 条永不可回退红线：

- **R1** EventMgr 必须用 `Dictionary<System.Type, EventEntryData>`
- **R2** AssetsReference 用 `_originalRefs[gameObject] = this` indexer + OnDestroy 必须移除自身
- **R3** SingletonSystem.Release() 必须清理生命周期 lists
- **R4** ResourceModule 异步加载 cancel/failure 分支必须 `handle.Dispose()`

并附「修改 TEngine 框架代码时的自检 checklist」。

### AI workflow 流程对齐上游

- **删除** `.claude/agents/wiki-query-agent.md`（上游 6.2.1 已用 `tengine-dev` skill 取代该 subagent）
- 修订 `src/MyGame/ShadowGame/CLAUDE.md` / `src/MyGame/ShadowGame/AGENTS.md` / 工程根 `AGENTS.md` / `.cursor/rules/shadowgame-tengine.mdc`：
  - L-2 深度查询从「调 wiki-query-agent subagent」改为「`SemanticSearch` / `Grep` / `Read` 查 `repowiki/zh/content/` + 必要时 `Read` 真实源码」
  - L3/L4 reference 修订工作流从 `/wiki:sync` 命令改为「直接 edit `.claude/skills/tengine-dev/references/<file>.md`」
- 主上下文压力问题改由 SemanticSearch 解决（按主题精确读章节而非整份文档 Read）

---

## 6.0.0 (2024 / vendored, 具体 import 日期已遗失)

> 本工程在 5.0.0 之后从上游引入 6.0.0 改动作为 vendor base，历史 import diff 未单独记录。后续修改全部以 `6.0.0+shadowgame-patches.<DATE>` 形式追加。

- 升级到 TEngine 6.x base（YooAsset 2.3.x、UIWindow 重构、Procedure 体系优化）
- 引入 GameModule 统一入口、ModuleSystem 重构
- 引入 GameEvent 接口事件 + Source Generator
- 此版本同步过 EventMgr `Dictionary<Type, EventEntryData>` 改造（来自上游 6.2.0）

---

## 5.0.0 (2023-03-18)
- TEngine5.0发布
- 全面更新，升级YooAsset2.1.1、资源模块核心调度优化、UIWindow最新框架、I2Localization。
- 支持边玩边下载，合理化webgl下的流程。
- 优化资源异步加载从缓存中延迟分帧处理。
- 优化框架轮询逻辑 ,用脏数据构建Execute数组。
- 支持Unity6。

## 4.0.0 (2023-10-08)

- TEngine框架4.0重构发布。

## 3.0.0 (2023-05-03)

- TEngine框架3.0重构发布。

## 2.0.0 (2022-08-25)

- TEngine框架2.0重构发布。

## 1.2.0 (2022-07-22)

- HybirdCLR（HuaTuo）新版更新

## 1.1.0 (2022-06-21)

- 优化TSingleton和TEngine使用体验

- 接入Huatuo最佳实践

## 1.0.0 (2022-05-17)

### Improvements

- TEngine框架1.0初始化。