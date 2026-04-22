<!-- 该文件由Cursor 自动生成 -->

# Scene Management — 场景管理系统

> **Status**: Draft
> **Author**: System Design Agent
> **Last Updated**: 2026-04-16
> **Last Verified**: 2026-04-16
> **Implements Pillar**: 关系即谜题（章节场景承载关系弧线的空间叙事）/ 克制表达（场景过渡本身传递情绪变化）

## Summary

场景管理系统负责《影子回忆》中所有 Unity Scene 的异步加载、卸载与切换编排。它维护"常驻基础场景 + 动态章节场景"的 Additive 架构，确保同一时刻内存中只有一个章节场景，并通过 TEngine ResourceModule / SceneModule + YooAsset 资源管线实现热更场景的按需下载与加载。系统对外表现为一个简洁的"切换到第 N 章"接口，对内编排 Fade 过渡、资源卸载/加载、内存清理的完整异步流水线。

> **Quick reference** — Layer: `Core` · Priority: `MVP` · Key deps: `Chapter State System, TEngine ResourceModule, TEngine SceneModule, YooAsset`

## Overview

玩家在《影子回忆》中经历 5 个章节，每章对应一个独立的 Unity Scene（房间场景）。场景管理系统在 Chapter State System 发出切换指令时，执行一套固定的异步流程：画面淡出 → 卸载旧场景及释放资源 → 加载新场景 → 初始化谜题数据 → 画面淡入。整个过程对玩家表现为一次柔和的视觉过渡，不中断音频氛围。系统基于 TEngine 框架的 SceneModule 和 ResourceModule 构建，场景资源包通过 YooAsset 管理，支持首次启动的热更下载和后续的本地缓存加载。

## Player Fantasy

场景切换应该感觉像**翻开相册的下一页**——柔和、自然、带着对下一段记忆的期待。玩家不应意识到"在等加载"或"在切换关卡"，而是感觉空间本身在呼吸般地变换。光线缓缓暗下，再在新的房间中缓缓亮起，仿佛时间在不同记忆之间流淌。

---

## Detailed Design

### Scene Architecture（场景架构）

采用 **Additive Scene** 策略，将常驻基础设施与动态内容分离：

```
BootScene (启动场景, Build Settings 中唯一场景, 非热更)
  └── TEngine RootModule 初始化
  └── 加载 GameApp 程序集 (HybridCLR)
  └── 进入 ProcedureMain → 触发 MainScene 加载

MainScene (常驻 Additive Scene, 热更)
  ├── UI Canvas (全局 UI 根节点)
  ├── Audio Listener + AudioModule 管理器
  ├── Camera Rig (主相机 + 过渡相机)
  ├── Global Managers (EventSystem, SceneManager 单例等)
  └── Transition Overlay (全屏遮罩 Canvas, 用于 Fade 效果)

Chapter_XX_xxx (章节场景, 热更, 同时只存在一个)
  ├── 场景环境 (房间模型、灯光、装饰物件)
  ├── 可交互物件 (Shadow Puzzle 的操作对象)
  ├── 光源配置 (符合该章节色温设定)
  └── 场景专属脚本 (谜题初始化、场景特效)
```

**关键约束**：
- BootScene 是唯一被编入 Build Settings 的场景，不走热更
- MainScene 通过 YooAsset 热更加载后以 Additive 模式常驻
- 章节场景通过 YooAsset 热更加载，以 Additive 模式动态挂载/卸载
- 任意时刻内存中最多只有 **BootScene + MainScene + 1 个章节场景**

### Scene Registry（场景注册表）

章节与场景的映射关系由 Luban 配置表 `TbChapter.sceneId` 驱动（定义见 `chapter-state-and-save.md`），运行时由 Scene Manager 维护一份查询缓存：

| 场景标识 | Unity Scene Name | 资源包名 | 热更 | 常驻 | 估算大小 |
|---------|-----------------|---------|------|------|---------|
| `boot` | BootScene | — (built-in) | 否 | 是 | < 1 MB |
| `main` | MainScene | `scene_main` | 是 | 是 | ~5 MB |
| `chapter_01` | Chapter_01_Approach | `scene_ch01` | 是 | 否 | ~15 MB |
| `chapter_02` | Chapter_02_SharedSpace | `scene_ch02` | 是 | 否 | ~18 MB |
| `chapter_03` | Chapter_03_SharedLife | `scene_ch03` | 是 | 否 | ~20 MB |
| `chapter_04` | Chapter_04_Loosening | `scene_ch04` | 是 | 否 | ~18 MB |
| `chapter_05` | Chapter_05_Absence | `scene_ch05` | 是 | 否 | ~22 MB |

### Core Rules

**场景加载规则：**

1. 所有场景加载必须使用 `async UniTask`，禁止任何同步加载 API
2. 场景通过 TEngine `GameModule.Resource.LoadSceneAsync()` 加载，底层走 YooAsset 资源管线
3. 场景以 `LoadSceneMode.Additive` 模式加载，不使用 `Single` 模式（避免销毁常驻场景）
4. 加载完成后调用 `SceneManager.SetActiveScene()` 将新章节场景设为活跃场景（光照烘焙/默认 Instantiate 目标）
5. 场景加载前必须检查 YooAsset 资源包状态，若未下载则先触发下载流程

**场景卸载规则：**

1. 卸载当前章节场景前，必须先通知所有监听者（Shadow Puzzle、Narrative 等）执行清理
2. 使用 `GameModule.Resource.UnloadSceneAsync()` 卸载场景
3. 场景卸载后必须调用 `GameModule.Resource.UnloadUnusedAssets()` + `Resources.UnloadUnusedAssets()` + `GC.Collect()` 确保释放该场景引用的所有资源
4. 卸载过程中禁止接受新的场景切换请求（防止竞态）

**切换互斥规则：**

1. 同一时刻只允许一个场景切换流程执行
2. 切换进行中收到新请求时，排入等待队列（队列最大长度 = 1，新请求覆盖旧请求）
3. 异常中断的切换流程必须能回退到稳定状态（至少保持 MainScene 可用）

**热更新规则：**

1. 首次启动在 ProcedureMain 中检查所有场景资源包的更新状态
2. MainScene 的资源包在进入主菜单前必须完成下载
3. 章节场景的资源包采用"按需下载"策略——玩家点击进入某章时检查并下载
4. 下载进度通过 GameEvent 广播给 UI 显示进度条
5. 下载失败提供重试入口，不自动静默重试（节省用户流量）

### Scene Transition Flow（场景切换完整流程）

```
Chapter State System                Scene Manager                     ResourceModule / YooAsset
      │                                   │                                    │
      │ ── GameEvent: ──────────────────> │                                    │
      │    RequestSceneChange             │                                    │
      │    { targetChapterId: 3 }         │                                    │
      │                                   │                                    │
      │                                   │ ── [1] 检查是否正在切换 ────────────  │
      │                                   │     (若是，排队/覆盖)               │
      │                                   │                                    │
      │                                   │ ── [2] 广播 SceneTransitionBegin   │
      │                                   │     → UI: 开始 Fade Out            │
      │                                   │     → Audio: 开始音乐渐弱           │
      │                                   │                                    │
      │                                   │ ── [3] await Fade Out ──────────── │
      │                                   │     (FADE_OUT_DURATION = 0.8s)     │
      │                                   │                                    │
      │                                   │ ── [4] 广播 SceneUnloadBegin       │
      │                                   │     → Puzzle: 清理运行时数据        │
      │                                   │     → Narrative: 中止演出           │
      │                                   │                                    │
      │                                   │ ── [5] await 卸载当前章节场景 ─────>│
      │                                   │                                    │ UnloadSceneAsync()
      │                                   │                                    │
      │                                   │ ── [6] await 释放未使用资源 ──────> │
      │                                   │                                    │ UnloadUnusedAssets()
      │                                   │                                    │ + GC.Collect()
      │                                   │                                    │
      │                                   │ ── [7] 检查目标场景资源包 ─────────>│
      │                                   │                                    │ 已下载? → 跳过
      │                                   │                                    │ 未下载? → DownloadAsync()
      │                                   │                                    │   → 广播下载进度
      │                                   │                                    │
      │                                   │ ── [8] await 加载目标章节场景 ─────>│
      │                                   │                                    │ LoadSceneAsync(Additive)
      │                                   │                                    │
      │                                   │ ── [9] SetActiveScene(新场景)       │
      │                                   │                                    │
      │                                   │ ── [10] 广播 SceneLoadComplete     │
      │                                   │      → Chapter State: 确认场景就绪  │
      │                                   │      → Puzzle: 初始化谜题数据       │
      │                                   │      → Audio: 切换章节 BGM          │
      │                                   │                                    │
      │ <── GameEvent: ──────────────────  │                                    │
      │     SceneReady                     │                                    │
      │     { chapterId: 3 }               │                                    │
      │                                   │                                    │
      │                                   │ ── [11] await Fade In ─────────── │
      │                                   │      (FADE_IN_DURATION = 1.2s)     │
      │                                   │                                    │
      │                                   │ ── [12] 广播 SceneTransitionEnd    │
      │                                   │      → 恢复玩家输入               │
      │                                   │                                    │
```

### Startup Flow（启动流程）

```
[App 启动]
    │
    ▼
BootScene (Unity 自动加载)
    │
    ├── TEngine RootModule 初始化
    ├── HybridCLR 加载热更 DLL
    ├── YooAsset 初始化 + 检查更新
    │     ├── 有更新 → 下载补丁包 (显示进度 UI)
    │     └── 无更新 → 继续
    │
    ▼
ProcedureMain
    │
    ├── await LoadSceneAsync("MainScene", Additive)
    │     → MainScene 常驻场景就绪
    │
    ├── 读取存档 (Save System)
    │     ├── 有存档 → 获取 currentChapterId
    │     └── 无存档 → currentChapterId = 1
    │
    ├── 显示主菜单 UI
    │     ├── "继续游戏" → await LoadChapterScene(currentChapterId)
    │     └── "新游戏" → await LoadChapterScene(1)
    │
    ▼
[正常游戏循环]
```

### States and Transitions

**Scene Manager 状态机：**

```
                    ┌──────────────────────────────────────────┐
                    │                                          │
Idle ──[收到切换请求]──→ Loading ──[加载异常]──→ Error ──[恢复]──┘
  ▲                      │
  │                      │
  │               ┌──────┴──────┐
  │               │             │
  │               ▼             ▼
  │        Transitioning    Unloading
  │         (Fade Out)    (卸载旧场景)
  │               │             │
  │               ▼             ▼
  │          Unloading     Loading
  │        (卸载旧场景)   (加载新场景)
  │               │             │
  │               ▼             ▼
  │           Loading     Transitioning
  │        (加载新场景)    (Fade In)
  │               │             │
  │               ▼             │
  │         Transitioning       │
  │          (Fade In)          │
  │               │             │
  └───────────────┴─────────────┘
```

简化为线性流水线：

| State | Entry Condition | Exit Condition | Behavior |
|-------|----------------|----------------|----------|
| **Idle** | 初始状态 / 切换流程完成 / 错误恢复完成 | 收到 `RequestSceneChange` 事件 | 正常游戏状态，可接受切换请求 |
| **Transitioning (Out)** | 收到切换请求且当前为 Idle | Fade Out 动画完成 | 播放 Fade Out（全屏遮罩 alpha 0→1），锁定玩家输入，音频渐弱 |
| **Unloading** | Fade Out 完成 | 旧场景卸载 + 资源释放完成 | 广播 `SceneUnloadBegin`，异步卸载当前章节场景，释放未使用资源 |
| **Loading** | 卸载完成（或无旧场景需卸载） | 新场景加载完成 + 初始化完成 | 检查/下载资源包，异步加载新章节场景，设为活跃场景，广播 `SceneLoadComplete` |
| **Transitioning (In)** | 新场景就绪 | Fade In 动画完成 | 播放 Fade In（全屏遮罩 alpha 1→0），音频渐入，解锁玩家输入 |
| **Error** | 加载/卸载过程中发生异常 | 恢复策略执行完成 | 记录错误日志，执行恢复策略（见 Edge Cases），尝试回到 Idle |

**状态互斥约束**：Transitioning / Unloading / Loading / Error 期间拒绝新的切换请求直接执行，仅记录到待处理队列（最大 1 个，新覆盖旧）。

### Interactions with Other Systems

**与 Chapter State System 的交互（主要触发者）：**

| 数据方向 | 内容 | 机制 |
|---------|------|------|
| Chapter State → Scene Manager | 切换请求（目标 chapterId） | GameEvent: `RequestSceneChange` |
| Chapter State → Scene Manager | 章节对应的 sceneId 查询 | Luban `TbChapter.sceneId` |
| Scene Manager → Chapter State | 场景加载就绪确认 | GameEvent: `SceneReady` |

**与 Shadow Puzzle System 的交互：**

| 数据方向 | 内容 | 机制 |
|---------|------|------|
| Scene Manager → Shadow Puzzle | 卸载通知（清理当前谜题数据） | GameEvent: `SceneUnloadBegin` |
| Scene Manager → Shadow Puzzle | 加载完成通知（初始化新谜题） | GameEvent: `SceneLoadComplete` |

**与 Narrative Event System 的交互：**

| 数据方向 | 内容 | 机制 |
|---------|------|------|
| Scene Manager → Narrative | 卸载通知（中止当前演出） | GameEvent: `SceneUnloadBegin` |
| Narrative → Scene Manager | 章末演出完成后请求场景切换 | 间接：Narrative → Chapter State → Scene Manager |

**与 Audio System 的交互：**

| 数据方向 | 内容 | 机制 |
|---------|------|------|
| Scene Manager → Audio | 过渡开始（音频渐弱） | GameEvent: `SceneTransitionBegin` |
| Scene Manager → Audio | 新场景就绪（切换 BGM + 渐入） | GameEvent: `SceneLoadComplete` 携带 `bgmAsset` 信息 |

**与 UI System 的交互：**

| 数据方向 | 内容 | 机制 |
|---------|------|------|
| Scene Manager → UI | 过渡遮罩控制（Fade Out/In） | 直接控制 Transition Overlay Canvas |
| Scene Manager → UI | 加载进度（用于显示进度条） | GameEvent: `SceneLoadProgress` |
| Scene Manager → UI | 下载进度（首次加载时） | GameEvent: `SceneDownloadProgress` |

**与 TEngine 模块的交互：**

| TEngine 模块 | 使用方式 | 说明 |
|-------------|---------|------|
| ResourceModule | `LoadSceneAsync()` / `UnloadSceneAsync()` | 通过 YooAsset 管线加载/卸载场景 |
| ResourceModule | `UnloadUnusedAssets()` | 卸载后释放未引用的资产 |
| ResourceModule | 资源包下载 API | 首次加载时检查并下载场景资源包 |
| SceneModule | 场景激活管理 | `SetActiveScene()` 设置光照/实例化目标 |

---

## Formulas

### Memory Budget Allocation（内存预算分配）

```
totalBudget = 1536 MB  (1.5 GB 平台预算)

systemOverhead   = 200 MB   (OS + Unity Runtime + Mono/IL2CPP)
engineFramework  = 80 MB    (TEngine + YooAsset + HybridCLR)
mainScene        = 60 MB    (常驻 UI / Audio / Camera / Managers)
audioPool        = 40 MB    (BGM + SFX 缓存)
uiAtlases        = 30 MB    (UI 图集常驻)
shaderVariants   = 25 MB    (预编译 Shader 变体)
safetyMargin     = 101 MB   (预留余量, 防止 OOM)
────────────────────────────
chapterBudget    = totalBudget - Σ(以上) = 1000 MB
```

| Variable | Type | Range | Source | Description |
|----------|------|-------|--------|-------------|
| totalBudget | int (MB) | 1536 | 平台约束 | iOS/Android 目标内存上限 |
| systemOverhead | int (MB) | 180-220 | 实测 | OS + 引擎运行时基础开销 |
| engineFramework | int (MB) | 60-100 | 实测 | TEngine 框架全模块内存 |
| mainScene | int (MB) | 40-80 | 实测 | 常驻场景的资产占用 |
| chapterBudget | int (MB) | ~1000 | 计算 | 单个章节场景可用的最大内存 |

**Expected output range**: chapterBudget ≈ 900-1100 MB（随设备实际系统占用浮动）

**Edge case**: 低端设备（2GB RAM）totalBudget 降至 1024 MB → chapterBudget ≈ 488 MB，需启用 LOD 降级策略。

### Scene Load Time Estimation（加载时间估算）

```
loadTime = downloadTime + deserializeTime + instantiateTime + initTime

downloadTime     = assetSize / networkSpeed       (仅首次, 已缓存时 = 0)
deserializeTime  = assetSize / diskReadSpeed
instantiateTime  = objectCount * avgInstantiateMs
initTime         = SCENE_INIT_FIXED_COST
```

| Variable | Type | Range | Source | Description |
|----------|------|-------|--------|-------------|
| assetSize | float (MB) | 15-22 | 资源统计 | 章节场景资源包大小 |
| networkSpeed | float (MB/s) | 1-5 | WiFi 实测 | WiFi 下载速度 |
| diskReadSpeed | float (MB/s) | 50-200 | 设备实测 | 本地存储读取速度 |
| objectCount | int | 50-200 | 场景统计 | 场景中需实例化的 GameObject 数量 |
| avgInstantiateMs | float (ms) | 0.5-2.0 | 实测 | 单个 GameObject 平均实例化耗时 |
| SCENE_INIT_FIXED_COST | float (ms) | 100-200 | 配置 | 谜题数据初始化 + 灯光计算等固定开销 |

**典型场景估算（Chapter 03, ~20 MB, ~150 objects）：**

| 场景 | 首次 WiFi (3 MB/s) | 本地缓存 (100 MB/s) |
|------|-------------------|-------------------|
| 下载 | 6.7s | 0 |
| 反序列化 | 0.2s | 0.2s |
| 实例化 | 0.15s | 0.15s |
| 初始化 | 0.15s | 0.15s |
| **合计** | **~7.2s** | **~0.5s** |

**性能目标**：

| 场景 | 目标 | 可接受上限 |
|------|------|----------|
| 本地缓存加载 | < 1s | 2s |
| WiFi 首次下载+加载 | < 3s（不含下载UI展示时间） | 10s |
| 4G 首次下载+加载 | < 8s（不含下载UI展示时间） | 15s |

### Fade Duration Calculation（过渡时间计算）

```
fadeOutDuration  = FADE_BASE_OUT * emotionalWeight
fadeInDuration   = FADE_BASE_IN * emotionalWeight
totalBlackScreen = unloadTime + loadTime   (玩家看到黑屏/遮罩的时间)
```

| Variable | Type | Range | Source | Description |
|----------|------|-------|--------|-------------|
| FADE_BASE_OUT | float (s) | 0.8 | 配置 | 基础淡出时长 |
| FADE_BASE_IN | float (s) | 1.2 | 配置 | 基础淡入时长（略长于淡出，营造渐显感） |
| emotionalWeight | float | 0.8-1.5 | Luban 配置 | 章节情绪权重（Ch.3→Ch.4 转折点 = 1.5, 其他 = 1.0） |

**Expected output**:
- 标准切换: fadeOut=0.8s + black ≈ 0.5s + fadeIn=1.2s = **~2.5s**
- 情绪重点 (Ch.3→Ch.4): fadeOut=1.2s + black ≈ 0.5s + fadeIn=1.8s = **~3.5s**

---

## Edge Cases

| Scenario | Expected Behavior | Rationale |
|----------|------------------|-----------|
| 场景加载失败（文件损坏/缺失） | 最多重试 2 次 → 失败后广播 `SceneLoadFailed`，UI 显示"场景加载失败"弹窗，提供"重试"和"返回主菜单"选项 | 玩家需要明确的恢复路径 |
| 资源包下载中网络断开 | 暂停下载，UI 显示"网络已断开，请检查网络连接"提示，网络恢复后自动断点续传 | 移动端网络不稳定是常态，需优雅降级 |
| 资源包下载超时（30s 无进度） | 取消当前下载，UI 显示超时提示，提供"重试"按钮 | 避免无限等待 |
| 玩家在 Fade Out 过程中切到后台 | 暂停 Fade 动画和计时器，回到前台后继续执行 | 通过 `OnApplicationPause` 响应 |
| 玩家在场景加载中退出应用 | 加载中断，无副作用——下次启动从存档恢复上次状态 | Save System 在切换前已保存进度 |
| 玩家在场景卸载中退出应用 | 由 OS 强制回收内存，无副作用——同上 | 存档安全由 Save System 保证 |
| 连续快速点击"下一章"（重复切换请求） | 第一次请求正常执行，后续请求排队（队列长度=1，新覆盖旧），待当前切换完成后执行最新请求 | 防止并发加载导致内存峰值或状态混乱 |
| MainScene 加载失败（启动阶段） | 属于致命错误——显示原生 UI 提示"游戏数据异常，请重新安装"，记录崩溃日志 | MainScene 是基础设施，无法降级 |
| 低内存警告（`OnLowMemory`） | 如果当前处于 Idle 状态，主动执行一次 `UnloadUnusedAssets()` + `GC.Collect()`；如果正在加载，记录日志但不中断 | 移动端内存回收信号 |
| 卸载旧场景后内存未明显下降 | 触发额外的 `Resources.UnloadUnusedAssets()` 和强制 `GC.Collect()`，延迟 1 帧后检查——若仍异常，记录内存快照到日志 | 可能存在资源泄漏，需要诊断数据 |
| 目标场景与当前场景相同（重复加载同一章） | 忽略请求，仅广播 `SceneReady` 确认 | 回放模式可能触发此情况 |
| 首次启动无网络且场景包未缓存 | UI 提示"需要下载游戏资源 (约 XX MB)"，仅提供"连接网络后重试"选项；不允许进入需要未缓存资源的章节 | 无法凭空创造资源 |
| 启动时 YooAsset 资源清单版本检查失败 | 使用本地已缓存的最新版本，记录警告日志——若本地无任何缓存，走"首次启动无网络"流程 | 网络检查失败不应阻塞已有本地缓存的玩家 |

---

## Dependencies

| System | Direction | Nature of Dependency |
|--------|-----------|---------------------|
| Chapter State System | Scene Manager depends on Chapter State | 接收切换指令 + 查询章节场景映射 |
| Shadow Puzzle System | Shadow Puzzle depends on Scene Manager | 场景就绪后初始化谜题；卸载前清理数据 |
| Narrative Event System | Narrative depends on Scene Manager | 间接触发场景切换（通过 Chapter State）；接收卸载通知 |
| Audio System | Audio depends on Scene Manager | 响应过渡事件调整音量/切换 BGM |
| UI System | UI depends on Scene Manager | 显示加载进度/下载进度/过渡遮罩 |
| Save System | Scene Manager depends on Save System（弱） | 启动时 Save System 先加载存档，Scene Manager 才知道加载哪个章节 |
| TEngine ResourceModule | Scene Manager depends on ResourceModule | 核心加载/卸载能力提供者 |
| TEngine SceneModule | Scene Manager depends on SceneModule | 场景激活管理 |
| YooAsset | Scene Manager depends on YooAsset（通过 ResourceModule） | 热更资源包的下载和版本管理 |

---

## Tuning Knobs

| Parameter | Current Value | Safe Range | Effect of Increase | Effect of Decrease |
|-----------|--------------|------------|-------------------|-------------------|
| FADE_BASE_OUT | 0.8s | 0.3-1.5s | 过渡更缓慢，更有仪式感但增加等待感 | 更快但可能显得突兀 |
| FADE_BASE_IN | 1.2s | 0.5-2.0s | 新场景渐显更慢，更"诗意" | 更快呈现新场景 |
| EMOTIONAL_WEIGHT_CH3_TO_CH4 | 1.5 | 1.0-2.0 | Ch.3→Ch.4 转场更慢更沉重 | 与其他章节切换节奏一致 |
| MAX_LOAD_RETRY | 2 | 0-5 | 更多重试机会，但玩家等待更久 | 更快失败反馈 |
| DOWNLOAD_TIMEOUT | 30s | 10-60s | 容忍更慢的网络 | 更快超时反馈 |
| UNLOAD_DELAY | 0 frames | 0-3 frames | 延迟卸载可避免极端情况下的帧卡顿 | 立即卸载，减少内存峰值 |
| GC_COLLECT_AFTER_UNLOAD | true | true/false | 卸载后强制 GC，确保内存释放 | 跳过 GC，减少卡顿但可能内存不及时回收 |
| LOADING_SCREEN_MIN_DISPLAY | 0.5s | 0-2.0s | 加载极快时也保证过渡不"闪烁" | 允许极短过渡（可能造成闪烁） |
| CHAPTER_SCENE_BUNDLE_SIZE_LIMIT | 25 MB | 15-40 MB | 允许更精细的场景但下载更慢 | 强制精简场景资源 |

---

## Visual/Audio Requirements

| Event | Visual Feedback | Audio Feedback | Priority |
|-------|----------------|---------------|----------|
| 场景切换开始 (Fade Out) | Transition Overlay alpha 0→1，颜色为当前章节的主导暗色（非纯黑） | 当前 BGM 渐弱至 10% 音量 (0.8s) | MVP |
| 场景加载中（遮罩期间） | 遮罩中央显示极简呼吸光点动画，不显示百分比进度条 | 极低音量的环境白噪音维持（避免完全静默） | MVP |
| 首次下载中（需要网络） | 全屏加载界面：章节主题色渐变背景 + 下载进度条 + 已下载/总大小 | 轻柔的等待音乐循环 | MVP |
| 场景就绪 (Fade In) | Transition Overlay alpha 1→0，新场景从遮罩色中渐显 | 新章节 BGM 从 0% 渐入至 100% (1.2s) | MVP |
| 过渡完成 | 遮罩完全透明并禁用渲染 | BGM 正常播放 | MVP |
| 下载失败提示 | 遮罩上方弹出半透明提示面板 | 一声轻微的"嗒"提示音 | MVP |
| 低内存警告 (仅调试) | Debug 模式下屏幕角落红色警告文字 | 无 | Alpha |

### 章节过渡遮罩色（配合 Art Bible 色彩弧线）

| 过渡方向 | 遮罩色 | 说明 |
|---------|-------|------|
| → Chapter 1 | 柔象牙暗化 `#3A3530` | 微暖的深色，暗示即将进入温柔的初遇空间 |
| → Chapter 2 | 暖琥珀暗化 `#3D2E1A` | 温暖深棕，对应亲密阶段的暖色调 |
| → Chapter 3 | 深暖琥珀暗化 `#352718` | 全作最暖的过渡色 |
| → Chapter 4 | 静谧蓝暗化 `#1E2830` | 冷意渗入的深蓝灰——关键情绪转折 |
| → Chapter 5 | 冷灰暗化 `#1A1E24` | 接近黑色但带蓝调，暗示缺席的寒意 |

---

## Game Feel

### Feel Reference

场景切换应该感觉像 **Florence 的章节过渡** — 画面自然地沉入柔和的暗色，再在新的空间中缓缓浮现，像记忆之间的自然间隔。**不应该**感觉像传统游戏的 Loading Screen（进度条、旋转图标、提示文字）。

加载等待应该感觉像 **Journey 的区域过渡** — 玩家在狭窄通道中行走时，下一个区域在后台加载完毕；虽然我们没有通道，但用视觉遮罩创造相同的"无缝感"。

### Input Responsiveness

| Action | Max Input-to-Response Latency (ms) | Frame Budget (at 60fps) | Notes |
|--------|-----------------------------------|------------------------|-------|
| 触发场景切换（章节选择点击） | 100ms | 6 frames | Fade Out 必须在 100ms 内开始，让玩家知道系统已响应 |
| 加载中取消/退出（按返回键） | 200ms | 12 frames | 中断加载并回退到主菜单 |
| Fade 动画帧率 | 16.7ms (60fps) | 1 frame | 遮罩动画必须保持 60fps，不受后台加载影响 |

### Animation Feel Targets

| Animation | Startup Frames | Active Frames | Recovery Frames | Feel Goal | Notes |
|-----------|---------------|--------------|----------------|-----------|-------|
| Fade Out（遮罩升起） | 0 | 48 (0.8s) | 0 | 轻柔、渐进、如闭眼 | 使用 EaseInCubic 曲线 |
| 遮罩期间呼吸光点 | 6 | ∞（循环） | 6 | 安静、存在感极低 | 缓慢脉冲 (2s 周期) |
| Fade In（遮罩消退） | 0 | 72 (1.2s) | 0 | 缓慢揭开、如睁眼 | 使用 EaseOutCubic 曲线 |
| 下载进度条 | 12 | ∞ | 12 | 平滑推进、不焦虑 | 进度平滑插值，避免跳跃 |

### Impact Moments

| Impact Type | Duration (ms) | Effect Description | Configurable? |
|-------------|--------------|-------------------|---------------|
| 遮罩最深点（全黑/全遮罩） | 500-1500ms | 完全遮罩的静寂瞬间——让玩家短暂脱离上一段记忆 | Yes |
| 新场景首帧显露 | 1200ms (Fade In 时长) | 从暗到亮的渐变中，新房间的轮廓首先可见，光源最后亮起 | Yes |
| Ch.3→Ch.4 过渡（特殊） | 3500ms | 延长的过渡时间 + 色温从暖到冷的变化 + BGM 从暖调到冷调的交叉渐变 | Yes |

### Weight and Responsiveness Profile

- **Weight**: 轻盈但有存在感。过渡不是"切换"而是"呼吸"——场景暗下来像慢慢闭上眼睛，亮起来像慢慢睁开。
- **Player control**: 被动。过渡开始后玩家无法加速或跳过（设计意图：过渡本身是叙事的一部分）。但玩家可以在等待下载时选择取消。
- **Snap quality**: 平滑渐变。所有数值变化（alpha、音量、色温）都使用缓动曲线，禁止线性插值。
- **Acceleration model**: 慢起慢停。Fade Out 使用 EaseInCubic（慢开始快结束），Fade In 使用 EaseOutCubic（快开始慢结束），整体感受是"呼吸"节奏。
- **Failure texture**: 不可见。加载失败的提示以温和的方式呈现（不使用红色错误弹窗），而是融入当前遮罩风格的柔和提示面板。

### Feel Acceptance Criteria

- [ ] 场景切换时，测试者不会说"在等加载"（本地缓存场景切换全程 < 2.5s）
- [ ] Fade Out/In 动画始终保持 60fps（不受后台加载影响）
- [ ] Ch.3→Ch.4 的过渡让测试者感受到"氛围变了"（色温冷暖转变可被感知）
- [ ] 没有测试者看到"黑屏闪烁"（最短遮罩持续时间保证平滑）
- [ ] 首次下载的进度界面不引起焦虑感

---

## UI Requirements

| Information | Display Location | Update Frequency | Condition |
|-------------|-----------------|-----------------|-----------|
| 过渡遮罩 | 全屏 Overlay Canvas (最高排序层级) | 每帧 | 场景切换期间 |
| 呼吸光点 | 遮罩中央 | 每帧（脉冲动画） | 遮罩 alpha > 0.9 时显示 |
| 下载进度条 | 全屏加载界面中央偏下 | 每 100ms 平滑插值更新 | 需要网络下载时 |
| 下载大小信息 | 进度条下方 | 下载开始时和完成时 | "已下载 12.3 MB / 20.0 MB" |
| 网络错误提示 | 加载界面中央 | 错误发生时 | 网络断开/超时 |
| "重试" 按钮 | 错误提示下方 | 错误发生时 | 下载失败/加载失败 |
| "返回主菜单" 按钮 | 错误提示下方 | 错误发生时 | 所有错误情况 |
| 内存警告 (Debug) | 屏幕左上角 | 触发时 | 仅 Debug 构建 |

---

## Cross-References

| This Document References | Target GDD | Specific Element Referenced | Nature |
|--------------------------|-----------|----------------------------|--------|
| 章节切换由 Chapter State 触发 | `design/gdd/chapter-state-and-save.md` | `ChapterComplete` 状态转换 → 下一章解锁 | State trigger |
| 章节场景 ID 映射 | `design/gdd/chapter-state-and-save.md` | `TbChapter.sceneId` 配置字段 | Data dependency |
| 场景就绪后初始化谜题 | `design/gdd/shadow-puzzle-system.md` | 谜题初始化流程 | State trigger |
| 场景卸载前清理谜题数据 | `design/gdd/shadow-puzzle-system.md` | 谜题运行时数据清理 | State trigger |
| 过渡遮罩色取自章节色彩弧线 | `design/art/art-bible.md` | 章节色彩弧线（Emotional Color Mapping） | Data dependency |
| 章末演出完成后触发场景切换 | `design/gdd/narrative-event-system.md` | `ChapterOutroFinished` → `RequestSceneChange` | State trigger |
| BGM 切换时机 | `design/gdd/audio-system.md` | 章节 BGM 渐变规则 | Rule dependency |
| 存档中 currentChapterId 决定启动加载哪个场景 | `design/gdd/chapter-state-and-save.md` | `IChapterProgress.CurrentChapterId` | Data dependency |

---

## TEngine Integration Details

### ResourceModule 集成

```csharp
// 场景加载（通过 TEngine ResourceModule，底层走 YooAsset）
var sceneHandle = await GameModule.Resource.LoadSceneAsync(sceneId, LoadSceneMode.Additive);

// 场景卸载
await GameModule.Resource.UnloadSceneAsync(sceneHandle);

// 资源释放
GameModule.Resource.UnloadUnusedAssets();
```

**关键注意事项**：
- `LoadSceneAsync` 返回的 handle 必须持有引用，用于后续卸载
- 卸载场景时必须使用同一个 handle，不能用场景名重新查找
- `UnloadUnusedAssets()` 在卸载场景后调用一次即可，不需要每帧调用

### YooAsset 资源包策略

| 资源包 | 下载策略 | 缓存策略 |
|-------|---------|---------|
| `scene_main` | 启动时强制下载 | 永久缓存 |
| `scene_ch01` | 新游戏/继续游戏时预下载 | 永久缓存 |
| `scene_ch02` ~ `scene_ch05` | 按需下载（进入该章时） | 永久缓存 |
| 共享资源包 (`shared_materials`, `shared_shaders`) | 启动时下载 | 永久缓存 |

**预下载策略（可选优化）**：
- 当玩家在 Chapter N 游玩时，后台静默预下载 Chapter N+1 的资源包
- 预下载仅在 WiFi 环境下执行
- 预下载优先级低于当前场景的资源加载

### GameEvent 定义

| Event Name | Payload | 发送者 | 接收者 |
|------------|---------|-------|-------|
| `RequestSceneChange` | `{ int targetChapterId }` | Chapter State | Scene Manager |
| `SceneTransitionBegin` | `{ int fromChapterId, int toChapterId }` | Scene Manager | UI, Audio |
| `SceneUnloadBegin` | `{ int chapterId }` | Scene Manager | Shadow Puzzle, Narrative |
| `SceneLoadProgress` | `{ float progress }` | Scene Manager | UI |
| `SceneDownloadProgress` | `{ float progress, long downloadedBytes, long totalBytes }` | Scene Manager | UI |
| `SceneLoadComplete` | `{ int chapterId, string bgmAsset }` | Scene Manager | Shadow Puzzle, Audio, Chapter State |
| `SceneReady` | `{ int chapterId }` | Scene Manager | Chapter State |
| `SceneTransitionEnd` | `{ int chapterId }` | Scene Manager | All (解锁输入) |
| `SceneLoadFailed` | `{ int chapterId, string error }` | Scene Manager | UI (显示错误) |

---

## Acceptance Criteria

### 核心功能

- [ ] BootScene 启动后自动加载 MainScene（Additive），MainScene 常驻不卸载
- [ ] 收到 `RequestSceneChange` 后完整执行 Fade Out → 卸载 → 加载 → Fade In 流程
- [ ] 任意时刻内存中最多 BootScene + MainScene + 1 个章节场景
- [ ] 场景切换期间玩家输入被锁定，切换完成后恢复
- [ ] 所有场景加载/卸载使用 async UniTask，零同步加载调用

### 内存管理

- [ ] 卸载章节场景后，该场景的资源引用计数归零
- [ ] `UnloadUnusedAssets()` + `GC.Collect()` 在每次卸载后执行
- [ ] 单个章节场景内存占用不超过 chapterBudget（~1000 MB）
- [ ] Profiler 中无资源泄漏（连续切换 5 次场景后内存基线不持续增长）

### 加载性能

- [ ] 本地缓存场景加载 < 1s（中端设备）
- [ ] WiFi 首次下载 + 加载 < 3s（不含下载 UI 展示时间）
- [ ] Fade Out/In 动画全程 60fps（不受后台加载影响）

### 热更新

- [ ] 首次启动正确检查并下载所有必需资源包
- [ ] 按需下载章节资源包，有清晰的 UI 进度反馈
- [ ] 下载失败后"重试"按钮功能正常
- [ ] 已缓存的资源包不重复下载

### 错误恢复

- [ ] 场景加载失败后，系统回到 Idle 状态，MainScene 保持正常
- [ ] 网络断开时有明确的用户提示和恢复路径
- [ ] 连续快速切换请求不导致崩溃或状态混乱
- [ ] Performance: 完整切换流程（Fade Out + 卸载 + 加载 + Fade In）< 3s（本地缓存）
- [ ] 代码中无硬编码的场景名或章节数——全部由 Luban 配置表驱动

---

## Open Questions

| Question | Owner | Deadline | Resolution |
|----------|-------|----------|-----------|
| 是否需要"章节预览"功能（在章节选择界面加载场景缩略图/3D 预览）？ | Game Design | Vertical Slice | 待定——可能需要额外的轻量预览资源包策略 |
| Ch.3→Ch.4 的过渡是否需要特殊的叙事演出（不仅是 Fade，而是更复杂的视觉效果）？ | Game Design + Art | Vertical Slice | 当前设计为延长 Fade + 色温变化；若需更复杂效果需额外评估 |
| 预下载策略（WiFi 下静默下载下一章）是否在 MVP 实现？ | Tech Lead | MVP | 建议 MVP 仅实现按需下载；Vertical Slice 评估预下载的电量/流量影响 |
| 低端设备 (2GB RAM) 需要什么具体的降级策略？ | Tech Lead | Alpha | 需要真机 Profiling 数据后决定——可能的策略：降低贴图分辨率、减少装饰物件 |
| 场景切换过渡是否支持"跳过"（长按跳过 Fade 动画）？ | Game Design | MVP | 建议不支持跳过——过渡是叙事体验的一部分；但需要在玩家测试中验证接受度 |
| YooAsset 资源包的分包粒度（每章一个包 vs 每章拆为多个子包）？ | Tech Lead | MVP | 当前设计为每章一个包；若单包超过 25MB 可考虑拆分为场景+材质+音频子包 |
