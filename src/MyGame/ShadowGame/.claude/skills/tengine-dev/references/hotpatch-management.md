# TEngine 热更包管理

## 目录

- [核心概念](#核心概念)
- [热更流程](#热更流程)
- [API 速查](#api-速查)
- [完整示例](#完整示例)

---

## 核心概念

热更包管理基于 **YooAsset** 实现，整体流程由主包流程状态机驱动（不可热更），分为以下阶段：

```
ProcedureInitResources     → 初始化 YooAsset 资源系统
ProcedureInitPackage       → 初始化资源包（决定运行模式）
ProcedureCreateDownloader  → 创建下载器（检查是否需要更新）
ProcedureDownloadFile      → 下载热更资源文件
ProcedurePreload           → 预加载 tag="PRELOAD" 的资源
ProcedureLoadAssembly      → 加载热更 DLL（HybridCLR）
```

完整启动流程见 [architecture.md](architecture.md#启动流程)。

---

## 热更流程

### 1. 获取版本信息

```csharp
// 获取当前本地资源包版本
string localVersion = GameModule.Resource.GetPackageVersion();

// 请求远端最新版本
var op = GameModule.Resource.RequestPackageVersionAsync();
await op.Task;
string remoteVersion = op.PackageVersion;
```

### 2. 更新 Manifest

```csharp
// 下载资源清单（对比本地与远端差异）
var updateOp = GameModule.Resource.UpdatePackageManifestAsync(remoteVersion);
await updateOp.Task;
```

### 3. 创建下载器并下载

```csharp
// 创建下载器（获取差量文件列表）
ResourceDownloaderOperation downloader = GameModule.Resource.CreateResourceDownloader();

int fileCount   = downloader.TotalDownloadCount;
long totalBytes = downloader.TotalDownloadBytes;

if (fileCount == 0)
{
    // 无需下载，直接进入下一流程
    return;
}

// 注册进度/错误回调，开始下载
downloader.OnDownloadProgressCallback = OnProgress;
downloader.OnDownloadErrorCallback    = OnError;
downloader.BeginDownload();
await downloader.Task;
```

### 4. 清理缓存（可选）

```csharp
// 清理本地冗余缓存（版本更新后调用）
GameModule.Resource.ClearCacheFilesAsync();
```

---

## API 速查

| 方法 | 说明 |
|------|------|
| `GetPackageVersion()` | 获取当前本地资源包版本号 |
| `RequestPackageVersionAsync()` | 向远端请求最新版本号 |
| `UpdatePackageManifestAsync(version)` | 更新资源清单到指定版本 |
| `CreateResourceDownloader()` | 创建差量下载器 |
| `downloader.TotalDownloadCount` | 待下载文件数量 |
| `downloader.TotalDownloadBytes` | 待下载总字节数 |
| `downloader.BeginDownload()` | 开始下载 |
| `ClearCacheFilesAsync()` | 清理本地冗余缓存 |

---

## 完整示例

```csharp
// 典型热更流程（简化版，对应 ProcedureCreateDownloader + ProcedureDownloadFile）
public async UniTask CheckAndDownloadUpdate()
{
    // 1. 请求远端版本
    var versionOp = GameModule.Resource.RequestPackageVersionAsync();
    await versionOp.Task;
    string remoteVersion = versionOp.PackageVersion;

    // 2. 更新 Manifest
    var manifestOp = GameModule.Resource.UpdatePackageManifestAsync(remoteVersion);
    await manifestOp.Task;

    // 3. 创建下载器
    var downloader = GameModule.Resource.CreateResourceDownloader();
    if (downloader.TotalDownloadCount == 0)
        return; // 已是最新，无需下载

    // 4. 显示下载 UI，绑定回调
    ShowDownloadUI(downloader.TotalDownloadCount, downloader.TotalDownloadBytes);
    downloader.OnDownloadProgressCallback = OnDownloadProgress;
    downloader.OnDownloadErrorCallback    = OnDownloadError;

    // 5. 执行下载
    downloader.BeginDownload();
    await downloader.Task;

    // 6. 清理旧缓存
    await GameModule.Resource.ClearCacheFilesAsync();
}
```

---

## 框架 v6.2.x 修订亮点（同步上游）

### GameApp.Entrance 真签名（修正 hotfix-workflow 文档）

```csharp
// ✅ 真实签名（ProcedureLoadAssembly 反射调用）
public static void Entrance(object[] objects)
{
    GameEventHelper.Init();                                 // 1. 必须最先调用（Source Generator 注册）
    _hotfixAssembly = (List<Assembly>)objects[0];           // 2. 保存热更程序集
    Utility.Unity.AddDestroyListener(Release);              // 3. 注册销毁回调
    StartGameLogic();                                       // 4. 启动游戏逻辑
}

// ❌ 错误签名（旧 wiki 描述）
public static void Entrance(Assembly[] assemblies)          // 不是 Assembly[]，是 object[]
public static void Entrance(List<Assembly> assemblies)      // 也不对
```

`objects[0]` 是 `List<Assembly>`，使用前需要 `(List<Assembly>)objects[0]` 强转。

### partial class GameApp 拓展模式

`GameApp` 是 `partial class`，可以分多个 `.cs` 文件拓展入口逻辑（避免单文件过大）：

```csharp
// GameApp.cs (主入口)
public partial class GameApp
{
    public static void Entrance(object[] objects) { /* ... */ }
}

// GameApp_RegisterSystem.cs (注册系统)
public partial class GameApp
{
    private static void RegisterSystems() { /* ... */ }
}

// GameApp_RegisterEvent.cs (注册事件)
public partial class GameApp
{
    private static void RegisterEvents() { /* ... */ }
}
```

### 完整启动流程链路（ShadowGame 实际版本）

> **重要**：本工程对上游 TEngine 标准链路做了改造，**不**完全等同上游。差异见本节末「与上游 TEngine 标准链路对比」表。

#### 节点清单（13 个 Procedure，路径 `Assets/GameScripts/Procedure/`）

| Procedure | 职责 | 后继（按 ChangeState 实际调用） |
|-----------|------|-------------------------------|
| `ProcedureLaunch` | 启动主流程 | `ProcedureSplash` |
| `ProcedureSplash` | Splash 界面 | `ProcedureGetVersion` |
| **`ProcedureGetVersion`** ⭐ | **本工程独有**：下载远程 `version.txt`（HostPlayMode/WebPlayMode 分支），解析后调用 `_resourceModule.SetRemoteServicesUrl(hostUrl, hostUrl)`；Editor/Offline 模式直接跳过 | `ProcedureInitPackage` |
| `ProcedureInitPackage` | 初始化资源包（按 PlayMode 决定运行模式） | `ProcedureInitResources` |
| `ProcedureInitResources` | 初始化 YooAsset + manifest，根据是否需要更新分叉 | 有更新 → `ProcedureCreateDownloader`<br/>无更新/失败兜底 → `ProcedurePreload` |
| `ProcedureCreateDownloader` | 创建下载器，统计下载量 | 有文件 → `ProcedureDownloadFile`<br/>无文件 → `ProcedureDownloadOver` |
| `ProcedureDownloadFile` | 执行下载 | 成功 → `ProcedureDownloadOver`<br/>失败 → `ProcedureCreateDownloader`（重试） |
| `ProcedureDownloadOver` | 下载完成判断 | 有缓存要清 → `ProcedureClearCache`<br/>无缓存 → `ProcedurePreload` |
| `ProcedureClearCache` | 清理旧版本缓存 | `ProcedurePreload` |
| `ProcedurePreload` | 预加载 PRELOAD 标签资源 | `ProcedureLoadAssembly` |
| `ProcedureLoadAssembly` | 加载热更 DLL（HybridCLR） | `ProcedureStartGame` |
| `ProcedureStartGame` | 反射调用 `GameApp.Entrance(object[])` 进入热更入口 | （热更域接管）|
| `ProcedureBase` | 抽象基类，不参与流转 | — |

#### FSM 跳转图

```mermaid
graph TD
    A[ProcedureLaunch] --> B[ProcedureSplash]
    B --> C[ProcedureGetVersion ⭐ 本工程独有]
    C -->|Editor/Offline 直跳| D
    C -->|Host/Web: 下载 version.txt 后| D[ProcedureInitPackage]
    D --> E[ProcedureInitResources]
    E -->|有更新| F[ProcedureCreateDownloader]
    E -->|无更新/失败| J[ProcedurePreload]
    F -->|有文件| G[ProcedureDownloadFile]
    F -->|无文件| H[ProcedureDownloadOver]
    G -->|成功| H
    G -->|失败重试| F
    H -->|有缓存| I[ProcedureClearCache]
    H -->|无缓存| J
    I --> J
    J --> K[ProcedureLoadAssembly]
    K --> L[ProcedureStartGame]
    L -.GameApp.Entrance.-> M((热更域))
```

#### Editor 模式下的实际路径

`ProcedureGetVersion` 在 `EPlayMode.EditorSimulateMode` / `EPlayMode.OfflinePlayMode` 下不发起 webRequest，直接 `ChangeState<ProcedureInitPackage>`。所以 Editor 下冷启动实际经过的状态序列：

```
Launch → Splash → GetVersion(快速跳过) → InitPackage → InitResources
→ Preload(无更新分支) → LoadAssembly → StartGame
```

> ShadowGame 当前未启用 hotpatch flow（`UpdateSetting.Enable = false`），但 `GetVersion` 节点**始终**会被进入（即使在 Editor 模式也会经过，只是逻辑分支不同）。所以**禁止**说"启动跳过 GetVersion"——它一定执行，区别在于是否做 webRequest。

#### 与上游 TEngine 标准链路对比

| 阶段 | 上游 TEngine | ShadowGame 本工程 |
|------|-------------|------------------|
| version 检查 | `ProcedureUpdateVersion`（用 YooAsset 自带 UpdatePackageVersionOperation） | **`ProcedureGetVersion`**（自实现：UnityWebRequest 拉 `version.txt` 文本，再调 SetRemoteServicesUrl 拼 hostUrl）|
| manifest 更新 | `ProcedureUpdateManifest`（独立节点） | **合并到 `ProcedureInitResources`**（在 OnEnter 内部完成 manifest 更新+判断分叉）|
| Splash 之后 | `Splash → InitPackage` | `Splash → GetVersion → InitPackage` |

修改这块时**不要**回退为上游标准链路（会破坏本工程的 version.txt 拉取协议 + 服务端 CDN 路径约定）。
