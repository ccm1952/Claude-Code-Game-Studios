// 该文件由Cursor 自动生成

# SP-003 Findings: YooAsset ResourcePackage 包策略

> **Status**: ✅ 已验证（源码审查）
> **Date**: 2026-04-22
> **Source**: `ProcedureInitPackage.cs`, `ProcedureInitResources.cs`, `ProcedurePreload.cs`, `ResourceModule.cs`

## 结论

**项目当前使用单包策略（Single Package），且对于 5 章内容的手游项目足够。** 无需改为多包。

## 发现详情

### 当前配置

- `ProcedureInitPackage` 仅对 `_resourceModule.DefaultPackageName` 调用一次 `InitPackage`
- `DefaultPackageName` 默认值为 `"DefaultPackage"`，可被外部覆盖
- `ProcedureInitResources` 和 `ProcedurePreload` 使用同一 `_resourceModule`，未创建第二个包
- `ResourceModule` 底层有 `PackageMap` 多包能力，但启动流程仅显式初始化默认包

### 决策：单包策略

| 维度 | 评估 |
|------|------|
| 内容体量 | 5 章场景 + 共享资源，预估总量 < 1GB，单包足够 |
| 下载策略 | 5 章可通过 YooAsset Downloader 按 Tag 分批下载，无需多包 |
| 初始化复杂度 | 单包远低于多包（无跨包引用问题） |
| 热更需求 | 单包 + HybridCLR 已满足代码 + 资源热更 |

### 资源释放验证要点

场景卸载时共享资源（UI prefab, SFX）是否被误卸载：

- YooAsset 引用计数机制：SceneHandle 卸载不影响独立 LoadAssetAsync 加载的资源
- 关键规则：常驻资源（UI prefab）通过独立 Handle 持有，不与场景 Handle 绑定
- **需 Sprint 1 代码中实际验证**

## ADR-005 影响

无需修改。ADR-005 已按单包设计。

## 行动项

- [x] 确认当前单包策略 — 已确认
- [x] 评估多包必要性 — 不需要
- [ ] Sprint 1 中验证场景卸载不影响共享资源 Handle
