// 该文件由Cursor 自动生成

# 问题记录：多 Unity 实例并存时的 MCP 项目识别陷阱

> **日期**: 2026-04-23
> **发生次数**: 1 次（SP-011 Spike MCP 自动化执行）
> **严重性**: 中（一度误以为污染了其他项目，实际上没有；但过程中误删了自己的有效工作）

## 问题现象（修正后的真相）

执行 SP-011 Spike 的 MCP 自动化流程时，读 `~/Library/Logs/Unity/Editor.log` 的 `COMMAND LINE ARGUMENTS` 看到 `-projectpath /Users/chen/Desktop/Dev/Girls/928`，**误以为** MCP Bridge 绑定到 Girls/928 项目，于是紧急回滚：

1. `gameobject-destroy SP011_Launcher`
2. `scene-save main.unity`

**实际情况**：MCP Bridge 一直绑定在 ShadowGame 进程（PID 71236）。用户同时运行了两个 Unity Editor 进程：
- PID 71236 — ShadowGame（昨晚 8:27PM 启动，装了 `com.aibridge.unity`）
- PID 92011 — Girls/928（今天 3:19PM 启动，**未**装 `com.aibridge.unity`）

macOS 上两个 Unity 进程**共享**同一个 `~/Library/Logs/Unity/Editor.log` 文件，后启动的 Girls/928 进程**截断**了 ShadowGame 的启动日志，导致 `-projectpath` 看起来像 Girls/928。

### 真实影响（修正后）

1. `gameobject-create SP011_Launcher` + `gameobject-component-add` — **作用于 ShadowGame 的 main.unity**（组件能挂上本身就是证据：Girls/928 没有这个 class）
2. `editor-application-set-state isPlaying=true` — ShadowGame 进 PlayMode
3. `gameobject-destroy SP011_Launcher` + `scene-save` — **删掉了 ShadowGame 里有效的 Launcher 配置**（误以为在清理 Girls/928）

### 验证方法（推荐使用）

**不要**依赖 `~/Library/Logs/Unity/Editor.log` 的 `-projectpath` 判定（会被后启动的进程覆盖）。

**推荐**：
1. **Package 检查**：`grep "com.aibridge.unity" <project>/Packages/manifest.json` — 只有装了这个 Package 的项目才运行 MCP Bridge
2. **`script-read` 指纹测试**：MCP 调用 `script-read` 读项目独有文件（ShadowGame 的 `Assets/GameScripts/HotFix/GameLogic/Input/InputBlocker.cs`），能读到 → 绑定正确
3. **`ps aux` 进程列表**：确认运行中的 Unity 进程及其 `-projectpath`，再交叉比对哪个装了 Bridge Package

## 原因分析

### 根因 1：误以为 Editor.log 的 `-projectpath` 等同于"MCP 绑定的项目"

macOS 上多个 Unity Editor 进程共享 `~/Library/Logs/Unity/Editor.log`，后启动的进程会截断前面进程的日志。所以 Editor.log 头部的 `-projectpath` **只反映最新启动的进程**，不反映"哪个进程装了 MCP Bridge Package"。

### 根因 2：仓促触发回滚而未做完整验证

发现 `-projectpath` 指向 Girls/928 后立即回滚，没有先做最基本的交叉验证（检查 `Packages/manifest.json` 或 `script-read` 指纹）。这导致误删了 ShadowGame 里本该保留的 `SP011_Launcher`。

### 根因 3：TEngine 前置协议对"多实例场景"无说明

原协议只说"要做项目归属校验"，没说明如何在**多 Unity 实例并存**的常见场景下正确识别。

## 解决方案

### 已执行的操作（**属于误操作**）

1. `editor-application-set-state isPlaying=false` → 退出 PlayMode（对 ShadowGame，这一步是合理的；Spike 已结束）
2. `gameobject-destroy SP011_Launcher` → **误删 ShadowGame 里有效的 Launcher**
3. `scene-save openedSceneName=main` → 保存了删除后的 main.unity（rootCount 4 → 3）

### 工程规则补充（已落地）

在 `src/MyGame/ShadowGame/CLAUDE.md` 的「🎯 TEngine 任务前置协议（MUST）」已新增**检查点 4（MCP 绑定项目归属校验）** 和**检查点 5（只读优先原则）**，镜像到：
- `.cursor/rules/shadowgame-tengine.mdc`
- `src/MyGame/ShadowGame/AGENTS.md`

**校验方法优先级**（越靠前越可信）：

1. ⭐ **首选**：`grep "com.aibridge.unity" <project>/Packages/manifest.json` — 只有装了 Bridge 的项目才绑定 MCP
2. ⭐ **次选**：`script-read` 项目独有文件（如 ShadowGame 的 `Assets/GameScripts/HotFix/GameLogic/Input/InputBlocker.cs`），能读到 → 绑定正确
3. 🚫 **不可信**：`~/Library/Logs/Unity/Editor.log` 的 `-projectpath`（多实例场景下会被覆盖）

### SP-011 具体修正

1. 需要**重新**创建 SP011_Launcher + 挂组件（ShadowGame 的 main.unity 里）
2. 重新进 PlayMode 跑测试
3. PlayMode 中分阶段读 console（5s / 10s / 15s），观察 `console-get-logs` 是否真能抓到 ShadowGame 域内的 Debug.Log（不只是 UnityAiBridge Package 自身的日志）

## 受影响 / 已更新的文档

- [x] `/Users/chen/Desktop/Dev/MyGameStudio/.claude/memory/problem_2026-04-23_wrong-unity-project.md` — 本文件
- [ ] `src/MyGame/ShadowGame/CLAUDE.md` — 补充检查点 4（MCP 项目归属校验）
- [ ] Sprint 1 Retrospective Action Items — 补一条「MCP 项目校验」Action

## 预防复发的机制

1. **MCP 写操作前的项目指纹校验**（规则层面）
2. **只读优先原则**：MCP 操作永远先用 `gameobject-find` / `scene-list-opened` 等只读工具探测，确认项目正确后再用写操作
3. **明确 MCP server 命名语义边界**：`project-0-MyGameStudio-unity-bridge` 命名只表达"项目作用域配置"，**不保证**当前 Unity 进程匹配该项目
