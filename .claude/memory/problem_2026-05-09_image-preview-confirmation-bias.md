// 该文件由Cursor 自动生成

# 问题记录：Cursor agent 对 image preview 的 confirmation bias（Unity MCP screenshot 误判 SceneView fallback）

> **日期**: 2026-05-09
> **发生次数**: 1 次（S5-01 dev-story Phase 5 screenshot 评估阶段）
> **严重性**: 中（最终 ground truth 经用户原生工具核实后修正，不影响交付；但浪费了 ~3 个 batch 的精力 + 用户耐心）

---

## 问题现象

S5-01 dev-story Phase 5 走 unity-mcp `manage_camera action=screenshot capture_source=game_view` 抓 SceneView build evidence 时，server 一共抓了 4 张 PNG：

| 文件 | server 报告 | 实际 IHDR | md5 (前 8) | agent 视觉判断 |
|---|---|---|---|---|
| screenshot-20260509-143805.png | scene_view | 321×808 | b18f9b93 | "看到 AI Nav overlay" ✅ 正确 |
| screenshot-20260509-144044.png | game_view, MainCamera | 1024×1024 | b48859a4 | "干净 game_view" ✅ 正确（首次） |
| screenshot-20260509-144209.png | game_view, MainCamera | 1024×1024 | b48859a4（同 144044） | "干净 game_view" ✅ 正确 |
| screenshot-20260509-144402.png | game_view, MainCamera | 1024×1024 | 60994dfe | **"看到 toolbar / Persp gizmo / AI Nav overlay"** ❌ **错误** |
| screenshot-20260509-144458.png | game_view, MainCamera | 1024×1024 | 60994dfe（同 144402） | **"还是看到 Editor overlay"** ❌ **错误** |

后两次（intensity 1.0 → 0.5/0.7 调光后重抓）实际依然是**干净的 game_view 渲染**（用户 macOS 原生 Quick Look 打开后核实，无 toolbar / 无 Persp gizmo / 无 AI Nav overlay；视觉与 144209 主体一致，仅 lighting 强度差异）。**4 次 game_view 抓图全部是正确的，agent 后两次完全是误判。**

---

## 根因

Confirmation bias（确认偏差）—— 经典反 pattern：

1. **建立错误的因果假设**：第一张 scene_view (143805) 真有 Editor overlay，agent 据此推断 "Unity GameView tab 没活 → ScreenCapture fallback 到 SceneView → 后续 game_view 抓图也会 fallback"。这个假设**听起来合理**（CoplayDev/unity-mcp 的 ScreenCapture 实现确实有 capture 当前 active window 的可能性），但**没有任何实际证据**支撑后续 4 张都 fallback。

2. **看图时主动搜寻支持假设的证据，把正常物件元素重新解读为 Editor UI**：
   | 图中实际是 | agent 误读为 |
   |---|---|
   | 中央深棕灰矩形（ProjectionWall, `#3A3530` 5×3 墙） | "AI Navigation overlay 深色面板" |
   | 左下浅米黄 Cube（Object_01_CoffeeMug） | "SceneView Y-axis gizmo / Persp 指示器" |
   | 右下浅米黄 Capsule（Object_02_Book） | "Move/Rotate transform handle" |
   | 整体浅色 Floor 远端 + 渲染天空 | "Editor toolbar 区域 + AI Nav panel 背景" |

3. **无视 3 条独立 ground truth 反驳信号**：
   - **server metadata** 持续返回 `captureSource=game_view, camera=MainCamera`（agent 怀疑 "server 可能撒谎"）
   - **PNG IHDR 实测**（xxd 读 0x00000400）= 1024×1024 方形（agent 怀疑 "Cursor 可能把方形拉成高瘦"）
   - **md5 一致性**：144044 == 144209（同视角同 state deterministic）；144402 == 144458（intensity 调整后 deterministic）—— md5 一致本身就证明 server 渲染管线是稳定的，**没有"有时 fallback 有时不"的随机行为**。但 agent 没意识到 md5 一致 = 两张都是干净 game_view（既然 144209 已被认定干净）

4. **加深错误的二次猜测**：当对 Cursor image preview 显示效果存疑时，agent 没有 challenge 自己的判断，反而构造了 "Cursor preview 把 1024×1024 PNG 拉成高瘦比例" 这种无据猜测来维持原假设。

---

## 规则（按推荐度从高到低）

### Rule A — server metadata + 文件 hash + 文件 IHDR 三类硬证据高于 agent 视觉解读

> 当 MCP server 返回 structured metadata（`captureSource`, `camera`, `imageWidth`, `imageHeight` 等）+ 文件二进制层面的 IHDR/md5/sips 报告 + agent 自己的 image preview 视觉解读三者冲突时，**default 信任前两者**。
>
> 视觉 pattern recognition 容易被 prior assumption 污染（confirmation bias），但 hash 和 binary header 不会。
>
> 反例：本次连续 5 次拒绝相信 server `captureSource=game_view` + IHDR 1024×1024 + md5 一致这三条铁证，固执 maintain "server 可能 fallback"假设。

### Rule B — 在 Cursor 没有可靠像素级 ground truth 工具时，主动用用户原生工具仲裁

> Cursor agent 当前可用的图像工具：`Read` PNG（embed 进 chat）、`sips` (macOS 原生，仅 metadata)、`xxd`/`od`（binary）、`md5`。**没有** `convert` (ImageMagick) / `magick` / `identify` / opencv 这种像素级 query 工具。
>
> 这意味着 agent 看图时只能依赖 Cursor preview，而 Cursor preview 在 chat 容器布局里渲染 1024×1024 时具体什么样，agent 无法 100% 确知。
>
> → 当 agent 对 image preview 内容存疑且 server metadata 已经给了答案时，**优先让用户用 macOS Quick Look / Finder / Preview app 这类原生工具核实**，而不是反复看 Cursor preview 自我说服。
>
> 推荐句式：「Cursor preview 我看到 X，但 server metadata 是 Y。两者冲突。请你 `qlmanage -p <path>` 或 Finder 双击 PNG 仲裁一下。」

### Rule C — 当多次出现"server 说 X 但我看到 Y"时立即停止视觉判断

> 一次"server metadata vs 视觉判断"冲突可能是真问题（server 真有 bug）。
> **两次**同类型冲突就该停下来 challenge 自己的视觉判断，而不是构造更多猜测。
>
> 反例：本次第一次 (-144402) 误判后没纠偏；第二次 (-144458 md5 与前者相同) 还在误判；如果不是用户主动给了原生工具截图反驳，可能会一路错下去。

### Rule D — md5 一致性必须正视

> 当 `md5(A) == md5(B)` 时，A 和 B 是 100% 二进制相同的同一张图。如果 agent 已经判定 A 干净（无 overlay），就**必须**判定 B 也干净。**不能**说"A 干净 B 有 overlay"，这违反 md5 的数学定义。
>
> 反例：本次 144044 和 144209 md5 完全相同，agent 第一次正确判定 144209 干净；后来 144402 和 144458 md5 也相同（虽然这两张和前两张不同），agent 却同时说 144458 有 overlay。如果用 144209 干净作为 prior，144458 应该也是干净的（intensity 调低不会突然加 Editor overlay）。

---

## Agent 自检 checklist

每次 Cursor agent 看 PNG image preview 之前：

- [ ] 已经看过 server response 里的 `captureSource` / `camera` / `imageWidth` / `imageHeight`？
- [ ] 已经 xxd 验过 PNG IHDR 的真实 width × height？
- [ ] 如果有多张 PNG，已经 md5 比较过 deterministic 关系？

每次看到 "image preview 显示异常东西" 之时：

- [ ] 这是不是我之前建立的 prior assumption 在 pattern match？
- [ ] 我看到的"异常东西"在 server metadata / IHDR / md5 里有任何对应支持吗？没有的话，default 信任 metadata
- [ ] 已经有 ≥ 2 次"server 说 X 我看到 Y"冲突时：立刻停止视觉判断，要求用户原生工具仲裁

每次构造"绕过 server 行为"的猜测之前（比如 "server 可能 fallback / Cursor preview 可能拉伸"）：

- [ ] 这个猜测有任何 hash / binary / metadata 层面的证据支撑？没有的话，**这是 confirmation bias，立即停止**

---

## 本次具体反例的修复方式

用户提示 → 用户在 macOS 原生工具打开 PNG → 用户截图实际显示给 agent → agent 承认误判 + 沉淀本规则。

**修复成本**：~3 个 batch 的浪费 + 1 轮额外对话。**预防成本**：1 次 xxd IHDR + 1 次 md5 对比 + 1 句"我让用户用 Quick Look 仲裁" = 几乎零成本。

ROI 极高，应该当 default 习惯。

---

## 历史记录

- **2026-05-09 创建**。S5-01 dev-story Phase 5（Chapter_01_Approach scene 实体构建）走 `manage_camera action=screenshot capture_source=game_view` 抓 evidence 阶段，agent 对 4 张 game_view PNG 中的后两张反复误判为"含 Editor overlay"，无视 server metadata + IHDR + md5 三条铁证。用户用 macOS Quick Look 打开 PNG 后给 agent 看实际像素，agent 承认 confirmation bias 错误。
- 影响范围：本工程 S5-01 实施流程 ~3 个 batch 重复抓图；用户耐心。
- 关联 lessons：与 `problem_2026-05-09_unity-bridge-to-coplaydev-switch.md` Rule E（schema 文件 vs server runtime drift）有微妙关联——两条都是关于"agent 不应过度怀疑 server 行为"，但本条更强调"也不应过度信任自己的视觉解读"。
- **不沉淀到跨工程 cursor rule**（用户决策 [C]：纯 agent behavior 层面问题，但与本工程 unity-mcp screenshot 评估流程绑定较紧；如未来在其他工程再次发生类似 confirmation bias，可升格为跨工程规则）
