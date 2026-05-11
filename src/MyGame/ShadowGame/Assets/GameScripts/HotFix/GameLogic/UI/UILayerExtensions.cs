// 该文件由Cursor 自动生成
// S5-08 UIModule Setup (Sprint 5 narrow scope) — UILayer 扩展方法
//
// 关联文档:
//   * production/epics/ui-system/story-001-uimodule-setup.md  (AC-1 + AC-6)
//   * docs/architecture/adr-011-uiwindow-management.md         (UI 分层规范源)
//
// 实施背景 (R2.10 Session 26 #5 实证):
//   TEngine vendor 已在 Assets/GameScripts/HotFix/GameLogic/Module/UIModule/WindowAttribute.cs:8
//   定义 public enum UILayer : int { Bottom=0, UI=1, Top=2, Tips=3, System=4 }
//   (namespace GameLogic)。本 story 不新建 UILayer.cs（避免 C# 同 namespace type collision）；
//   仅在本文件加 sorting order helper extension method。
//
// ADR-011 spec wording ↔ vendor wording 映射 (累计 V3 Type-5 dp3 spec ↔ reality drift; ADR-011 §G amend 留 Sprint 5 retro):
//   vendor `Bottom = 0` ≈ ADR-011 spec `Background` (底层背景)
//   vendor `UI     = 1` ≈ ADR-011 spec `HUD`        (主 UI 层)
//   vendor `Top    = 2` ≈ ADR-011 spec `Popup`      (顶层 / 弹窗)
//   vendor `Tips   = 3` ≈ ADR-011 spec `Overlay`    (Tips / Toast / Overlay)
//   vendor `System = 4`  = ADR-011 spec `System`    (系统层，同名)
//
// UIModule 路径提示 (per AC-6):
//   UIModule / UIWindow / UIBase / WindowAttribute 全部位于
//   `Assets/GameScripts/HotFix/GameLogic/Module/UIModule/`（GameLogic 热更程序集，非 TEngine Runtime）。
//   所有访问通过 GameModule.UI 静态门面；禁止 ModuleSystem.GetModule<UIModule>()。

namespace GameLogic
{
    /// <summary>
    /// UI 分层 sorting order 计算 helper。
    /// </summary>
    /// <remarks>
    /// <para>对应的 <see cref="UILayer"/> enum 由 TEngine vendor 定义于
    /// <c>Assets/GameScripts/HotFix/GameLogic/Module/UIModule/WindowAttribute.cs</c>，
    /// 5 个值：Bottom(0) / UI(1) / Top(2) / Tips(3) / System(4)。</para>
    /// <para>每层 sorting order base = <c>layer × 100</c>，给上层 9 个具体 UIWindow
    /// （main menu / pause menu / settings / HUD 等 Sprint 6+ 实施）留 100 个 sorting
    /// order slot 用作同层内的二级 ordering（per ADR-011 §UI 分层规范）。</para>
    /// </remarks>
    public static class UILayerExtensions
    {
        /// <summary>
        /// 返回指定 UI 层的 sorting order 基线值（layer × 100）。
        /// </summary>
        /// <param name="layer">UI 层枚举值（来自 vendor <see cref="UILayer"/>）。</param>
        /// <returns>该层的 base sorting order；上层 UIWindow 可在 base 上叠加二级 offset。</returns>
        /// <example>
        /// <code>
        /// int hudBase    = UILayer.UI.GetSortingOrderBase();     // 100
        /// int popupBase  = UILayer.Top.GetSortingOrderBase();    // 200
        /// int systemBase = UILayer.System.GetSortingOrderBase(); // 400
        /// </code>
        /// </example>
        public static int GetSortingOrderBase(this UILayer layer) => (int)layer * 100;
    }
}
