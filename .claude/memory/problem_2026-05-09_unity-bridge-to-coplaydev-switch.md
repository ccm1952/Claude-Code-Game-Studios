// 该文件由Cursor 自动生成

# 问题记录：unity-bridge → CoplayDev/unity-mcp 工具链切换 + fastmcp socks5 启动陷阱

> **日期**: 2026-05-09
> **发生次数**: 1 次（S5-01 dev-story 启动前的 R2 假设校验暴露）
> **严重性**: 中高（如果继续用 unity-bridge，story 6-batch plan 整体跑不动；如果不修 fastmcp 启动陷阱，CoplayDev 装好也起不来）

---

## 问题现象

S5-01 dev-story `story-001-scene-build.md` §Implementation Notes 假设了 6 个 batch 用 `manage_*` 命名 + `batch_execute` 一次推送：

```yaml
batch_1: manage_scene + manage_gameobject (建场景 + 5 root GO)
batch_2: manage_gameobject create primitive + manage_components add (建 Walls/Floor/...)
batch_3: manage_editor add_tag + manage_gameobject modify tag (FixedLight tag)
batch_4: manage_gameobject create + components_to_add=["Light"] + component_properties (4 个 Spot Light)
batch_5: manage_material create + set_material_color + assign_material_to_renderer (墙体材质)
batch_6: manage_scene save + manage_camera screenshot (落盘 + 出图)
```

**实测发现 R2.1~R2.10 假设全部对应** `CoplayDev/unity-mcp` v9.6.x 的 schema，但本项目装的是 `butterlatte-zhang/unity-ai-bridge` (`com.aibridge.unity`)，命名是 kebab-case (`scene-create` / `gameobject-create` / `assets-find`...)，且**没有** `manage_editor add_tag` / `batch_execute` 这两个核心工具。

**根本原因**：story 文档参考的是 `tengine-dev` skill 里的 `references/unity-mcp-guide.md`、`scene-gameobject.md`、`material-shader-vfx.md`、`editor-automation.md`，这些 reference 文档基于 **CoplayDev/unity-mcp** 的工具集编写，而工程**实际安装的**是另一个完全不兼容的 Unity Package。这是一次"文档预设的 MCP 工具集" vs "工程实际安装的 MCP 工具集"严重 drift。

**叠加陷阱**：换装 CoplayDev 后，第一次 Start Server 立即抛 `ImportError: Using SOCKS proxy, but the 'socksio' package is not installed.`，traceback 一路追到 `fastmcp/utilities/version_check.py` 的 PyPI 自检 banner —— 原因是用户 shell 配了 `all_proxy=socks5://127.0.0.1:7897`，fastmcp 启动横幅会调 `httpx.get(PYPI_URL)` 自动走 SOCKS handler，但 `uv` venv 里没装 `httpx[socks]` 子依赖。普通 PyPI 代理工具（pip/uv 自身）不走这个链路，所以本机其他 Python 工具都正常，**只有 fastmcp 的 banner 触发**。

---

## 根因

### 1. unity-bridge vs CoplayDev/unity-mcp 是两个完全独立的项目

| 维度 | butterlatte-zhang/unity-ai-bridge | CoplayDev/unity-mcp |
|---|---|---|
| 包名 | `com.aibridge.unity` | `com.coplaydev.unity-mcp` |
| 工具命名 | kebab-case：`scene-create`, `gameobject-create`, `gameobject-component-add`, `assets-find`, `script-execute`, ... | snake_case + manage 前缀：`manage_scene`, `manage_gameobject`, `manage_components`, `manage_material`, `manage_camera`, `manage_editor`, `find_gameobjects`, `read_console`, `batch_execute`, `manage_animation`, `manage_vfx`, `manage_prefabs`, `manage_packages`, `manage_ui`, `manage_physics`, `manage_graphics`, `script_apply_edits`, `unity_reflect`, `unity_docs`, `execute_code`, `execute_menu_item`, `execute_custom_tool`, ... |
| Bridge 机制 | Unity 端 `aibridge.editor.FileBridgePoller` 轮询本地文件，Python `mcp_server.py` (stdio) 通过文件交换 | Unity 端 HTTP server（默认 8080，本工程 8888），fastmcp Python 端 HTTP client；MCP transport=streamable-http |
| Cursor server 名 | `project-0-MyGameStudio-unity-bridge`（project-scoped，由本工程 `.cursor/mcp.json` 启动 stdio） | `user-unityMCP`（user-global，由 `~/.cursor/mcp.json` 通过 HTTP url 连接 fastmcp） |
| 多实例 | 假定单实例，Bridge 绑定到"装了 Package 的 Unity 进程" | 多实例支持：`mcpforunity://instances` resource + `set_active_instance` tool + per-call `unity_instance` 参数 |
| `batch_execute` | ❌ 无 | ✅ 一等公民，default 25 cmd/批，hard max 100，10~100x speedup |
| `manage_editor add_tag` | ❌ 无（要走 `script-execute` 或手动） | ✅ 内置，`tag_name` 参数 |
| screenshot | `script-execute` 跑 C# `ScreenCapture.CaptureScreenshot` | ✅ 一等公民 `manage_camera action=screenshot`（+ `capture_source=scene_view`、`batch=surround/orbit`、`include_image=true` 内联 base64 PNG 等） |

→ 任何基于 CoplayDev API 写的 story plan / reference doc，**搬到 unity-bridge 上一定整体不可执行**。

### 2. fastmcp 版本自检 + SOCKS 代理的隐式冲突

fastmcp 启动 HTTP server 时调用 `log_server_banner()` → `check_for_newer_version()` → `httpx.get(PYPI_URL, timeout=...)`。`httpx` 的 `Client.__init__` 看到 `all_proxy=socks5://...` 环境变量后，`_init_proxy_transport` 走 SOCKS path，要求 `socksio` 子包；该子包**不在 fastmcp 默认依赖**里（fastmcp 只装 `httpx`，没装 `httpx[socks]`），所以裸 `uv tool install mcp-for-unity` 创建的 venv 里**必然缺这个包**。

普通 SOCKS 代理用户（开 Clash/Surge 等本地代理工具）几乎一定会被命中。

**修复方案**（优先级降序）：

| 方案 | 命令 | 评价 |
|---|---|---|
| ⭐ **关 fastmcp 自检**（root cause） | `export FASTMCP_CHECK_FOR_UPDATES=off`（写 `~/.zshrc`） | 最干净：彻底绕过 PyPI 自检链路，不影响 fastmcp 任何运行时功能；不影响其他 Python 工具的代理；持久化 |
| 给 fastmcp venv 装 socksio | `uv tool install mcp-for-unity --with httpx[socks]` 或重装时 `--with socksio` | 治标：让 SOCKS 链路走通；但每次 fastmcp 升级 venv 重建可能丢失 |
| 启动时清代理 | 在 `mcp-terminal.command` 里 `unset all_proxy http_proxy https_proxy` | 不推荐：fastmcp 自检需要走外网，剥代理就拉不到 PyPI；脚本会被 Unity 重新生成覆盖 |

本工程选 ⭐ 方案，写入 `~/.zshrc`。

### 3. Cursor MCP 配置双源去重

CoplayDev 的 Auto Setup 会主动写 `~/.cursor/mcp.json`（user-global），同时本工程之前手动建过 `<workspace>/.cursor/mcp.json`（project-scoped）。Cursor 加载策略是**两级合并并去重 server name**，导致设置面板出现两个 `unityMCP`，都连同一个 fastmcp HTTP 8888 后端 → 重复占用工具调用槽位。

→ **保留 user-global**（CoplayDev 自动维护，跨项目可复用），**删除 project-scoped**。

---

## 规则（按推荐度从高到低）

### Rule A — 选 MCP Package 之前先确认工具集匹配

> 任何 dev-story / reference doc / skill 引用了具体 MCP 工具名（`scene-create`, `manage_scene`, `gameobject-create`, `manage_gameobject` 等）时，**必须先 `grep` `Packages/manifest.json` 确认装的是哪个 Unity MCP Package**：
>
> - `com.coplaydev.unity-mcp` → `manage_*` 工具集 + `batch_execute` 可用
> - `com.aibridge.unity` → kebab-case 工具集 + 无 `batch_execute`
>
> 不一致时，立刻向用户报告 drift，让用户决定换 Package 还是改文档/story。**不要假装能跑**。

### Rule B — fastmcp + SOCKS 代理 必须先关版本自检再启 server

> macOS / Linux 用户使用 SOCKS5 代理（Clash / Surge / V2Ray 等）时，安装任何基于 fastmcp 的 MCP server 后，**必须**在 shell rc 文件中先 `export FASTMCP_CHECK_FOR_UPDATES=off`，再启动 server，否则 banner 自检 100% ImportError。
>
> 详见 `~/.zshrc` 中 2026-05-09 加的注释段。

### Rule C — Cursor MCP 配置只保留一份

> CoplayDev 的 Auto Setup 会写 `~/.cursor/mcp.json`，**不要再在 `<workspace>/.cursor/mcp.json` 里重复写同名 server**，否则 Cursor 设置面板会出现两个相同 server 都连同一个后端，浪费工具槽位。
>
> 选 user-global 还是 project-scoped 看复用场景：跨项目用同一个 Unity MCP server 选 user-global；多个 Unity 项目想各起独立 server（不同端口、不同实例）选 project-scoped。

### Rule D — MCP 绑定校验从"看 Package 名"升级为"调 get_active 看 dataPath"

> 旧 unity-bridge 时代靠 `grep "com.aibridge.unity" manifest.json` 静态判断 MCP 绑定的 Unity 项目，因为 unity-bridge 是单实例 + Package-coupled 设计。**CoplayDev 是多实例支持设计**，最稳的运行时校验是直接调 `manage_scene action=get_active`（看 `data.path`）或 `mcpforunity://project_info` resource（看 `dataPath`）—— 一次 ~100ms round-trip，比 grep 文件还快还准。详见 `src/MyGame/ShadowGame/CLAUDE.md` §"MCP 绑定项目归属校验"重写后的方法优先级。

### Rule E — schema 文件 vs runtime enum 可能 drift（小 dp）

> CoplayDev v9.6.x 的 `manage_editor` schema enum 含 `telemetry_status / telemetry_ping`，但 runtime server 实测**不接受**这两个 action（报 "Unknown action"）。Schema 文件比 server 实现多 2 项。
>
> → 调用 `manage_*` 工具前如果 enum 列了某个不常用 action 但你不确定，**先 batch_execute 里包一个该 action 试错**（`fail_fast=false`），看 `callFailureCount` 决定是否依赖。生产关键路径不要用 schema 列了但未实测的 action。

---

## Agent 自检 checklist

每次涉及 unity-mcp 工具调用之前，反向检查一遍：

- [ ] 当前 `Packages/manifest.json` 装的是哪个 MCP Package？工具集匹配吗？
- [ ] 写操作之前，调过 `manage_scene get_active` 或 `mcpforunity://project_info` 验过 dataPath 吗？
- [ ] 多 Unity Editor 实例并存吗？需要 `set_active_instance` 锁定吗？
- [ ] `batch_execute` 的 `fail_fast` 是不是按需求设的？read-only 命令请求 `parallel=true` 时，server 实测可能不并行（`parallelApplied=false`），不要假设并行加速生效。
- [ ] schema 文件列了某 action 但你没实测过，敢上生产吗？

每次帮新用户装 CoplayDev unity-mcp 前：

- [ ] 用户的 shell 有 `all_proxy=socks5://...` 吗？有就先嘱咐设 `FASTMCP_CHECK_FOR_UPDATES=off`。
- [ ] 用户已有 `~/.cursor/mcp.json` 里的 unityMCP 条目吗？避免 project-scoped 重复写。

---

## 历史记录

- **2026-05-09 创建**。S5-01 dev-story 启动前 R2 校验阶段发现 unity-bridge 工具集与 story 6-batch plan 完全不匹配，用户决定切换 → CoplayDev/unity-mcp v9.6.8。切换过程踩到 fastmcp + SOCKS5 启动陷阱（2 次 ImportError）+ Cursor MCP 双配置去重问题（设置面板出现 2 个 unityMCP）。
- 影响文件：
  - `src/MyGame/ShadowGame/Packages/manifest.json` (删 `com.aibridge.unity` + 加 `com.coplaydev.unity-mcp`)
  - `src/MyGame/ShadowGame/Packages/packages-lock.json` (同步)
  - `src/MyGame/ShadowGame/.claude/skills/_archive/unity-bridge-2026-05-09/` (旧 skill 归档)
  - `~/.zshrc` (加 `FASTMCP_CHECK_FOR_UPDATES=off`)
  - `<workspace>/.cursor/mcp.json` (删除，去重)
  - `src/MyGame/ShadowGame/CLAUDE.md` §3-§6 (MCP 校验段重写：grep keyword + server 名 + 工具命名 + 多实例处理协议)
  - `.cursor/rules/shadowgame-tengine.mdc` (镜像同步)
  - `src/MyGame/ShadowGame/AGENTS.md` (短入口同步)
  - `src/MyGame/ShadowGame/.claude/skills/tengine-dev/references/unity-mcp-guide.md` (Start Bridge → Start Server，端口 8888，加首次启动 socksio 排查段)
  - `.claude/memory/problem_2026-04-23_wrong-unity-project.md` (加 2026-05-09 切换补注；详见该文件 §"补注（2026-05-09）"段)
- 预期效果：未来任何 dev-story 用 `manage_*` 工具集 + `batch_execute` 都能直接执行；多实例 race 由 CoplayDev 内置 `set_active_instance` / `unity_instance` 参数处理，不再需要"靠 Package 安装与否"间接判断；新用户装 CoplayDev 不再被 fastmcp + SOCKS5 启动陷阱卡住。
