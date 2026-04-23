// 该文件由Cursor 自动生成

# SP-009 Findings: I2 Localization 与 TEngine 集成模式

> **Status**: ✅ 已验证（源码审查）
> **Date**: 2026-04-22
> **Source**: `Assets/TEngine/Runtime/Module/LocalizationModule/` (80+ 源文件)

## 结论

**I2 Localization 已被 TEngine 完全内嵌封装。项目应通过 `ILocalizationModule` 接口访问，不直接调用 I2 API。**

## 发现详情

### TEngine 封装方式

| 维度 | 状态 |
|------|------|
| 命名空间 | `TEngine` / `TEngine.Localization`（**无** `I2.Loc`） |
| 核心类 | `LocalizationManager` (partial, TEngine 命名空间内) |
| 外部接口 | `ILocalizationModule` |
| I2 痕迹 | `I2Utils`, `I2_` 前缀, AddComponentMenu 带 "I2" 等 |
| 独立 I2 目录 | **不存在** `Assets/I2/` |

### 关键发现

1. **全部 80+ 源文件的命名空间均为 TEngine 相关**，无 `I2.Loc`
2. **无独立 I2 包**: `Assets/` 下不存在 `I2` 资源目录
3. **asmdef 无 I2 引用**: `TEngine.Runtime.asmdef` 及所有项目 asmdef 均无 I2 程序集依赖
4. **GameScripts 使用方式**: 通过 `ILocalizationModule`（如 `ProcedureLaunch.cs`、`GameModule.Localization`），不直接使用 `LocalizationManager`

### 语言切换 API

```csharp
// 正确方式：通过 TEngine 模块接口
GameModule.Localization.SetLanguage("Chinese (Simplified)");
GameModule.Localization.SetLanguage("English");

// 不要直接调用：
// LocalizationManager.CurrentLanguage = "...";  ← 内部实现，不应直接用
```

### YooAsset 集成

TEngine 的 LocalizationModule 内嵌了 I2 的资源管理（`ResourceManager`、`LanguageSource` 等），语言资源随 TEngine 模块一起管理，不需要额外配置 YooAsset 自定义 `IResourceExtractor`。

## 编码规范

| 规则 | 说明 |
|------|------|
| ✅ 使用 `GameModule.Localization` | 统一通过 TEngine 模块接口 |
| ❌ 不 `using I2.Loc` | I2 命名空间不存在于项目中 |
| ❌ 不直接 new LocalizationManager | 由 TEngine 模块生命周期管理 |
| ✅ UI 文本使用 Localize 组件 | TEngine Localization 提供的 MonoBehaviour |

## ADR-022 影响

- 标题应改为 "TEngine Localization (I2-embedded) Integration"
- API 调用路径统一为 `ILocalizationModule`
- 无需处理 YooAsset + I2 的资源加载兼容问题
