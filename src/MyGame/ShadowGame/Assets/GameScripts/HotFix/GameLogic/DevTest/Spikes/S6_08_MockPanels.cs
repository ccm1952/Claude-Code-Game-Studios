// 该文件由Cursor 自动生成
// S6-08 ui-system-008 popup queue + auto inputblocker — DevTest spike 专用 mock panels (6 class)。
//
// 关联文档:
//   * production/epics/ui-system/story-008-ui-layer-strategy.md  (AC-1~AC-9 + R3 P1~P5)
//   * R3 P1 Top sender + P2 UI/Bottom/System no-fire contrast + P3 Tips popup queue priority +
//     P4 same-layer + cross-layer sorting + P5 pause/resume/clear
//
// 实施说明:
//   * 6 class 各自 [Window(UILayer.X, fromResources: true, location: "UI/MockXxx")] — class-level
//     attribute 不被 inherit，必须各自标记 (per WindowAttribute.cs:20 [AttributeUsage(AttributeTargets.Class)])
//   * UIWindow vendor 是 abstract class 无 abstract member — subclass 空 body 即可
//   * 静态 LastInstance reference 供 spike R3 case 拿 hierarchy / reflect 用
//   * UI(1) layer 测试沿 S5_08_MockMinimalPanel 已有 fixture（不重复创建）
//
// Layer 分布:
//   - MockTopPanel  / MockTopPanel2 — UILayer.Top(2)  — P1/P4 same-layer + sender verify
//   - MockTipsPanelA / MockTipsPanelB / MockTipsPanelC — UILayer.Tips(3) — P3 popup queue + P4 cross-layer + P5 pause/resume/clear
//   - MockBottomPanel — UILayer.Bottom(0) — P2 no-fire contrast (background 非交互)
//   - MockSystemPanel — UILayer.System(4) — P2 no-fire contrast (always-on-top 系统通讯)
//
// 本文件仅在 UNITY_EDITOR || DEBUG 编译，Release 包零残留。

#if UNITY_EDITOR || DEBUG
using UnityEngine;

namespace GameLogic.DevTest.Spikes
{
    /// <summary>
    /// S6-08 mock Top(2) layer panel — R3 P1 sender verify + P4 same-layer sorting verify。
    /// </summary>
    [Window(UILayer.Top, fromResources: true, location: "UI/MockTopPanel")]
    public class MockTopPanel : UIWindow
    {
        public static MockTopPanel LastInstance { get; private set; }

        protected override void OnCreate()
        {
            base.OnCreate();
            LastInstance = this;
        }

        protected override void OnDestroy()
        {
            if (LastInstance == this)
            {
                LastInstance = null;
            }
            base.OnDestroy();
        }
    }

    /// <summary>
    /// S6-08 mock Top(2) layer panel 第 2 个 — R3 P4 same-layer sorting verify
    /// (MockTopPanel + MockTopPanel2 同 layer Depth = layerBase + N * WINDOW_DEEP)。
    /// </summary>
    [Window(UILayer.Top, fromResources: true, location: "UI/MockTopPanel2")]
    public class MockTopPanel2 : UIWindow
    {
        public static MockTopPanel2 LastInstance { get; private set; }

        protected override void OnCreate()
        {
            base.OnCreate();
            LastInstance = this;
        }

        protected override void OnDestroy()
        {
            if (LastInstance == this)
            {
                LastInstance = null;
            }
            base.OnDestroy();
        }
    }

    /// <summary>
    /// S6-08 mock Tips(3) layer panel A — R3 P3 popup queue + P4 cross-layer + P5 pause/resume/clear。
    /// </summary>
    [Window(UILayer.Tips, fromResources: true, location: "UI/MockTipsPanelA")]
    public class MockTipsPanelA : UIWindow
    {
        public static MockTipsPanelA LastInstance { get; private set; }

        protected override void OnCreate()
        {
            base.OnCreate();
            LastInstance = this;
        }

        protected override void OnDestroy()
        {
            if (LastInstance == this)
            {
                LastInstance = null;
            }
            base.OnDestroy();
        }
    }

    /// <summary>
    /// S6-08 mock Tips(3) layer panel B — R3 P3 popup queue priority DESC verify。
    /// </summary>
    [Window(UILayer.Tips, fromResources: true, location: "UI/MockTipsPanelB")]
    public class MockTipsPanelB : UIWindow
    {
        public static MockTipsPanelB LastInstance { get; private set; }

        protected override void OnCreate()
        {
            base.OnCreate();
            LastInstance = this;
        }

        protected override void OnDestroy()
        {
            if (LastInstance == this)
            {
                LastInstance = null;
            }
            base.OnDestroy();
        }
    }

    /// <summary>
    /// S6-08 mock Tips(3) layer panel C — R3 P3 popup queue enqueueOrder ASC tiebreak verify。
    /// </summary>
    [Window(UILayer.Tips, fromResources: true, location: "UI/MockTipsPanelC")]
    public class MockTipsPanelC : UIWindow
    {
        public static MockTipsPanelC LastInstance { get; private set; }

        protected override void OnCreate()
        {
            base.OnCreate();
            LastInstance = this;
        }

        protected override void OnDestroy()
        {
            if (LastInstance == this)
            {
                LastInstance = null;
            }
            base.OnDestroy();
        }
    }

    /// <summary>
    /// S6-08 mock Bottom(0) layer panel — R3 P2 no-fire contrast
    /// (Bottom 是 background 非交互 — ShowUI/CloseUI 期间 push/pop delta == 0)。
    /// </summary>
    [Window(UILayer.Bottom, fromResources: true, location: "UI/MockBottomPanel")]
    public class MockBottomPanel : UIWindow
    {
        public static MockBottomPanel LastInstance { get; private set; }

        protected override void OnCreate()
        {
            base.OnCreate();
            LastInstance = this;
        }

        protected override void OnDestroy()
        {
            if (LastInstance == this)
            {
                LastInstance = null;
            }
            base.OnDestroy();
        }
    }

    /// <summary>
    /// S6-08 mock System(4) layer panel — R3 P2 no-fire contrast
    /// (System 是 always-on-top 系统通讯非用户交互态阻塞 — ShowUI/CloseUI 期间 push/pop delta == 0)。
    /// </summary>
    [Window(UILayer.System, fromResources: true, location: "UI/MockSystemPanel")]
    public class MockSystemPanel : UIWindow
    {
        public static MockSystemPanel LastInstance { get; private set; }

        protected override void OnCreate()
        {
            base.OnCreate();
            LastInstance = this;
        }

        protected override void OnDestroy()
        {
            if (LastInstance == this)
            {
                LastInstance = null;
            }
            base.OnDestroy();
        }
    }
}
#endif
