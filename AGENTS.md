<!-- 该文件由 Cursor 自动生成 -->

# AGENTS.md — MyGameStudio 工作区 AI 代理总览

> 本文件是所有 AI 代理（Cursor / Claude Code / Codex / Aider 等）进入本工作区时的**跨工具统一入口**。
> 真相源分布在各子模块下的 `CLAUDE.md` / `.claude/` / `.cursor/rules/`；本文件只负责**导航与硬性指针**，不重复协议细节。

---

## 工作区布局

| 子路径 | 内容 | AI 协议入口 |
|--------|------|------------|
| `/` (工程根) | 游戏工作室总控（48 个协作 subagent、设计流程、production 流程） | [`CLAUDE.md`](CLAUDE.md) |
| `src/MyGame/ShadowGame/` | Unity 2022.3.62f2 + TEngine + HybridCLR + YooAsset + UniTask + Luban 商业游戏项目 | [`src/MyGame/ShadowGame/CLAUDE.md`](src/MyGame/ShadowGame/CLAUDE.md) + [`src/MyGame/ShadowGame/AGENTS.md`](src/MyGame/ShadowGame/AGENTS.md) |
| `docs/` | ADR / Spike 发现 / 架构文档 | [`docs/CLAUDE.md`](docs/CLAUDE.md) |
| `design/` | 游戏设计文档（GDD） | [`design/CLAUDE.md`](design/CLAUDE.md) |
| `src/` 其他子目录 | 其他子工程 | 各子目录 `CLAUDE.md`（若有） |

---

## 跨工具通用约定（MUST）

1. **进入子目录前先读该目录的 `CLAUDE.md`**。Cursor 会自动加载 `AGENTS.md`，但 `CLAUDE.md` 需要主动读取。
2. **进入 `src/MyGame/ShadowGame/` 工作时**，必须同时参照：
   - [`src/MyGame/ShadowGame/CLAUDE.md`](src/MyGame/ShadowGame/CLAUDE.md)（TEngine 强制协议原文）
   - [`src/MyGame/ShadowGame/AGENTS.md`](src/MyGame/ShadowGame/AGENTS.md)（跨工具短版）
   - `src/MyGame/ShadowGame/.claude/skills/tengine-dev/SKILL.md`（L-0 导航）
   - Cursor 用户：`.cursor/rules/shadowgame-tengine.mdc` 已按 glob 自动挂载
3. **新生成的代码文件首行必须加标识**：`// 该文件由Cursor 自动生成`（用户规则强制要求，工具无关）。
4. **所有回复使用简体中文**。
5. **先讨论清楚需求与场景处理，再动手改代码**（用户规则）。

---

## 工具差异速查

| 能力 | Cursor | Claude Code |
|------|--------|-------------|
| 自动加载 `AGENTS.md` | ✅ | ✅ |
| 自动加载 `CLAUDE.md` | ❌（需主动读） | ✅（层级自动拼接） |
| `.cursor/rules/*.mdc` 按 glob 强制 | ✅ | ❌ |
| `.claude/skills/*` 自动触发 | ❌（工程根级 skill 除外） | ✅ |
| `.claude/agents/*` 自定义 subagent | ❌（需用 `Task` + `SemanticSearch` 模拟） | ✅ |
| `.claude/commands/*` 斜杠命令 | ❌ | ✅ |

> 在 Cursor 里处理 ShadowGame 任务时，`wiki-query-agent` 子代理不可用，应降级为 `SemanticSearch` + `Read` 查询 `src/MyGame/ShadowGame/repowiki/zh/content/`，并在已读声明中标注"Cursor 模式：wiki 查询已降级"。

---

## 违反记录

发现任何 AI 代理违反协议时（未读前置文档、未评估 unity-mcp、L3+ 未查 wiki 等），立即写入：

```
/.claude/memory/problem_YYYY-MM-DD_<短描述>.md
```

格式参考现有 `problem_2026-04-22_tengine-skill-violation.md` / `problem_2026-04-22_asmdef-source-generator.md`。

---

## 相关真相源（禁止在本文件抄录细节，请直达）

- 游戏工作室总协议：[`CLAUDE.md`](CLAUDE.md)
- ShadowGame TEngine 强制协议：[`src/MyGame/ShadowGame/CLAUDE.md`](src/MyGame/ShadowGame/CLAUDE.md)
- Cursor 硬规则：[`.cursor/rules/shadowgame-tengine.mdc`](.cursor/rules/shadowgame-tengine.mdc)
- 用户规则 / 技术偏好：[`.claude/docs/technical-preferences.md`](.claude/docs/technical-preferences.md)
- 架构控制清单：[`docs/architecture/control-manifest.md`](docs/architecture/control-manifest.md)
