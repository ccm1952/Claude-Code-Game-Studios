// 该文件由Cursor 自动生成

# 问题记录：TEngine UIWindow prefab root 必须自带 Canvas + GraphicRaycaster (vendor 隐式硬要求 / spec 未声明)

> **日期**: 2026-05-11
> **发生工程**: MyGameStudio / src/MyGame/ShadowGame (Sprint 5 S5-08 dev-story Phase 2.3 PlayMode)
> **发生次数**: 第 1 次（S5-08 R3 PlayMode 实跑首次暴露 — 此前 Sprint 5 0 个 story 涉及 UIWindow prefab 自建）
> **严重性**: 中（vendor 框架隐式 component 依赖 → 静默 fail；只有 PlayMode 实跑才暴露；agent 完全按通用 prefab 模板生成会一次失败需 1 round fix）

---

## 问题现象

S5-08 dev-story Phase 2.3 用 Editor 脚本 `Assets/Editor/DevTest/S5_08_MockPanelGenerator.cs` 程序化生成 mock UIWindow prefab，第 1 版 prefab root 节点 component 配置如下：

```
S5_08_MockMinimalPanel (root)
├── RectTransform
├── Image (背景)
└── child: MockButton
    ├── RectTransform
    ├── Image
    ├── Button
    └── child: Text
```

PlayMode 实跑 P2 case 时 vendor 抛出：

```
Exception: Not found Canvas in panel GameLogic.DevTest.Spikes.S5_08_MockMinimalPanel
  at TEngine.UIWindow.Handle_Completed(...) [in UIWindow.cs:484]
```

P2 直接 fail (`UIWindow.Show()` 未完成 → 后续 P3/P4 case 也连锁失败)。

第 1 反应：agent 按"通用 UI prefab"模板生成 (RectTransform + Image)，预期 vendor 框架处理 Canvas/Raycaster 注入 → **错**。

---

## 根因

TEngine vendor `UIWindow.cs` `Handle_Completed` 方法 (UIWindow.cs:484-488) 直接调用：

```csharp
var canvas = _panel.GetComponent<Canvas>();
if (canvas == null)
{
    throw new Exception($"Not found Canvas in panel {GetType().FullName}");
}
```

**vendor 硬性要求 UIWindow prefab 的 root GameObject 自带 Canvas component**。这是一个 vendor 框架的**隐式 component dependency**，但：

1. **ADR-011 UI 系统架构 spec 未声明** — 只描述 UILayer 枚举和 UIRoot 的 Canvas 配置，未提"每个 UIWindow prefab root 也要有 Canvas"
2. **SP-002 UI sub-pillar 未声明** — 只描述 UIWindow lifecycle method，未提 prefab component 要求
3. **TEngine repowiki 未明示** — 没有"UIWindow prefab authoring guide"章节
4. **vendor `LogUI.cs` precedent 不暴露** — LogUI 是 framework 内置 UIWindow，其 prefab 在 vendor asset 内已配好 Canvas，agent 仅看 C# 源码看不出来 prefab 实际 component 配置

辅助要求：root 还需要 `GraphicRaycaster` 否则 Button 点击事件不会触发 (Unity UGUI 标准要求 — vendor 不抛错但功能 silently broken)。

---

## 根因细粒度

为什么 vendor 要求 root 自带 Canvas，而不是 UIRoot 单一 Canvas + UIWindow prefab 仅 RectTransform 也能 work？

- **Per-window Canvas 模式**: vendor 给每个 UIWindow 独立 Canvas 的目的是 **sorting order 隔离** + **per-window render layer**。`UILayer.X.GetSortingOrderBase() = X * 100` 直接赋给 window-level Canvas 的 `sortingOrder`，让多 UIWindow 同 UIRoot 子树下时 layered render order 受控。
- 如果只有 UIRoot 一个 Canvas，多 UIWindow 共享一个 sorting context，layer 间 render order 由 hierarchy index 决定 → 不灵活。
- 因此 **vendor 实现的"独立 Canvas per UIWindow"是架构决定**，不是 spec 选项。

---

## 规则

### 推荐解法（按优先级）

**[1] (推荐) UIWindow prefab 模板 — 标准组件清单**

任何 UIWindow prefab 的 **root GameObject** 必须包含：

| Component | 必需 | 用途 |
|---|---|---|
| `RectTransform` | ✅ | Unity UGUI base |
| `Canvas` | ✅ | vendor `UIWindow.cs:484` 强制要求；用于 per-window sortingOrder |
| `GraphicRaycaster` | ✅ | Button/Toggle/Slider 等 UGUI 交互组件的事件命中检测 |
| `Image` / `CanvasRenderer` | ⛔ 选用 | 视觉背景；非必需，若用则一并加 |

**[2] Editor 程序化生成 prefab 时**

```csharp
// 该文件由Cursor 自动生成
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

public static class MyUIPrefabGenerator
{
    [MenuItem("Tools/MyUI/Generate Prefab")]
    public static void Generate()
    {
        var root = new GameObject("MyPanel");
        var rt = root.AddComponent<RectTransform>();
        // ↓ TEngine UIWindow 硬要求 — vendor UIWindow.cs:484 会 throw
        root.AddComponent<Canvas>();
        // ↓ Button/Toggle 等 UGUI 事件命中检测必需
        root.AddComponent<GraphicRaycaster>();
        // ... 余下 UI 子节点
        PrefabUtility.SaveAsPrefabAsset(root, "Assets/Resources/UI/MyPanel.prefab");
    }
}
```

**[3] 手动在 Unity Editor 创建 prefab 时**

不要用 `GameObject > UI > Panel`（默认 Panel 是给 UIRoot 用的，不带 sorting 独立 Canvas）。改为：
1. 创建空 GameObject → `Add Component: RectTransform`（自动）
2. `Add Component: Canvas` → 设置 `Render Mode: Screen Space - Overlay`（vendor `UIWindow.cs` 内部会修正）
3. `Add Component: Graphic Raycaster`
4. 然后再加子节点。

**[4] 用 vendor `LogUI` precedent 作为参考**

vendor `Assets/TEngine/Resources/UI/LogUI.prefab` 是合规的 UIWindow prefab，可在 Unity Editor 打开查看 root component 配置作为 ground truth。

---

## Agent 自检 checklist

每次生成 / 修改 TEngine UIWindow prefab 时反向检查：

- [ ] prefab root 是否有 `Canvas` component？(vendor `UIWindow.cs:484` 硬要求)
- [ ] prefab root 是否有 `GraphicRaycaster` component？(Button/Toggle 等交互必需)
- [ ] Editor 程序化生成脚本是否 `root.AddComponent<Canvas>()` + `root.AddComponent<GraphicRaycaster>()`？
- [ ] PlayMode 实跑前是否 visual check `Hierarchy` window 确认 root component 列表？
- [ ] 如果失败时控制台出现 `"Not found Canvas in panel ..."` 错误 → 立即 root 加 Canvas，**不是** vendor bug

---

## 修复痕迹

S5-08 dev-story Phase 2.3 修复：

```csharp
// Assets/Editor/DevTest/S5_08_MockPanelGenerator.cs
public static void GenerateMockPanel()
{
    var root = new GameObject("S5_08_MockMinimalPanel");
    var rt = root.AddComponent<RectTransform>();
    rt.anchorMin = Vector2.zero;
    rt.anchorMax = Vector2.one;
    rt.offsetMin = Vector2.zero;
    rt.offsetMax = Vector2.zero;

    // ↓↓↓ 关键 fix: vendor UIWindow.cs:484 _panel.GetComponent<Canvas>() 硬要求 root
    root.AddComponent<Canvas>();
    // ↓↓↓ 关键 fix: GraphicRaycaster Button.onClick 事件命中检测必需
    root.AddComponent<GraphicRaycaster>();

    var bg = root.AddComponent<Image>();
    bg.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
    // ... 余下 child MockButton + Text
    PrefabUtility.SaveAsPrefabAsset(root, "Assets/Resources/UI/S5_08_MockMinimalPanel.prefab");
}
```

修复后 PlayMode 4/4 case PASS first-run。

---

## 关联 cross-references

- **故事**: `src/MyGame/ShadowGame/production/epics/ui-system/story-001-uimodule-setup.md` Phase 2.3 + History Session 26 #6
- **Evidence**: `src/MyGame/ShadowGame/production/qa/playmode-uimodule-setup-2026-05-11.md`
- **Vendor 源码定位**: `src/MyGame/ShadowGame/Assets/GameScripts/HotFix/GameLogic/Module/UIModule/UIWindow.cs:484-488`
- **Commit**: `43b0880` feat(sprint-5): S5-08 done — 4/4 R3 PASS + V3 Type-8 dp1 NEW
- **V3 watch list 关联**: 本问题与 ADR-029 V3 Type-5 'spec/tooling ↔ reality drift' 同源 — spec 文档 (ADR-011/SP-002) ↔ vendor 实际 implementation 不一致；本 case 在 Sprint 5 retro 评估时一并纳入 Type-5b 'spec wording / 隐式依赖未声明' 分支考量；如 Sprint 6 继续做 UIWindow 类 story，prefab template + Canvas/Raycaster 硬要求要嵌入 story-001-ui-strategy.md 或 SP-002 的 implementation notes。

---

## 历史记录

- **2026-05-11 Session 26 #6 (S5-08 dev-story Phase 2.3 PlayMode)**: 第 1 次发生。Editor 脚本 `S5_08_MockPanelGenerator.cs` 程序化生成 mock panel prefab 时 root 仅有 RectTransform + Image，PlayMode P2 case 触发 `UIWindow.cs:484` `_panel.GetComponent<Canvas>()` throw → P2 fail → 修复 Editor 脚本加 `root.AddComponent<Canvas>()` + `root.AddComponent<GraphicRaycaster>()` → 4/4 case PASS first-run。Reactive cost ~10 min (read UIWindow.cs source + edit Editor script + Unity reimport + re-run PlayMode)。Proactive 价值: 防止 S5-02 main menu UIWindow / Sprint 6 ui-system-002~009 series 9 UIWindow prefab 全部踩同一坑，预估 saved cost ~10 × 8 = 80 min。
