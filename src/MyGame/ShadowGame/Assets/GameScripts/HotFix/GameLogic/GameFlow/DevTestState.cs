// 该文件由Cursor 自动生成
// 开发测试 FSM 状态。整文件仅在 UNITY_EDITOR || DEBUG 编译。
// 生命周期：
//   * 由 GameLoadingState.OnLoadingComplete（DEBUG 分支）切入
//   * OnEnter 启动所有已注册 Spike；有 Spike → 不推进（停留，保留 OnGUI）
//   * 无 Spike → 回落到 GameLobbyState 保持路径可达

#if UNITY_EDITOR || DEBUG
using Cysharp.Threading.Tasks;
using TEngine;

namespace GameLogic
{
    public class DevTestState : GameFlowState
    {
        protected override void OnEnter(IFsm<IFsmModule> fsm)
        {
            base.OnEnter(fsm);
            Log.Info("[GameFlow] 进入 DevTestState —— 开发测试模式");

            if (DevTest.DevBootstrap.PendingCount == 0)
            {
                Log.Info("[GameFlow] 未注册任何 Spike，直接进入 GameLobbyState");
                ChangeState<GameLobbyState>(fsm);
                return;
            }

            // S5-02 (2026-05-12) / S6-07 (2026-05-13) / S6-08 / S6-04 / S6-01-playtest (2026-05-14): main menu 模式 —
            //   不 auto-fire OnRequestSceneChange(1)，spike 自驱 (S5-02/S6-07 ShowUI<MainMenuPanel> + Button.onClick.Invoke()；
            //   S6-08 直接 ShowUI mock panel；S6-04 spike Start() 自 fire OnRequestSceneChange(1) baseline + 5 R3
            //   error/restart path case；S6-01-playtest no-op spike → 用户手动 click NewGame 走 chapter 1)。
            // 关键：若 DevTestState 在此 pre-dispatch OnRequestSceneChange(1)，spike 启动时 chapter 1 已加载 →
            //   Button click 二次派 (chapter1→chapter1 noop) 致 spike NewGameClickDispatch case FAIL (P4 transition delta=0)；
            //   且 S6-04 P2/P5 newest-wins pending 测试依赖 spike 完全控制 chapter 切换时机；S6-01-playtest 需 main menu
            //   显示后由用户手动驱动游戏完成 ≥30 min internal playtest（详 production/playtests/playtest-vs-chapter-1-session-1-2026-05-13.md）。
            // 其他历史 spike (story-001c, S5-1c 等) 保留原 auto-fire 行为以兼容 sync-subscribe race precedent。
            // V3.0.1 dp8 candidate: [main-menu] mode HasSpike list 已增至 5 个 spike (S5-02/S6-07/S6-08/S6-04/S6-01-playtest)，
            //   远超原阈值 4 — Sprint 6 retro 强制评估 V3.1 trigger pattern (central mode-dispatch refactor 候选)；
            //   详 story-003-error-restart-path.md §V3.0.1 Watch List Hooks。
            // V3.0.1 dp14 candidate NEW (2026-05-14 Session 32): playtest infrastructure pattern gap — S6-01 Phase 1 prep 盲点
            //   surface 时新加 S6-01-playtest no-op spike + DevTestState [main-menu] mode +1 项；详 S6-01_PlaytestHoldMode.cs。
            // V3.0.1 dp8 candidate (2026-05-14 Session 32 Phase 2): DevTestState [main-menu] mode HasSpike list 加 S6-13 = 6 spike，
            //   远超原阈值 4 — Sprint 6 retro 强制评估 V3.1 trigger pattern (central mode-dispatch refactor 候选)；
            //   详 production/epics/vs-chapter-1/story-004-input-pipeline-wiring.md §V3.0.1 Watch List Hooks Type-8 dp1。
            // V3.0.1 dp8 candidate (2026-05-14 Session 33 Phase 2): DevTestState [main-menu] mode HasSpike list 加 S6-14 = 7 spike，
            //   远超原阈值 4 — Sprint 6 retro 强制评估 V3.1 trigger pattern (central mode-dispatch refactor 候选)；
            //   详 production/epics/vs-chapter-1/story-005-chapter-1-scene-wiring.md §V3.0.1 Watch List Hooks。
            // V3.0.1 dp8 candidate (2026-06-12 Session 34): DevTestState [main-menu] mode HasSpike list 加 S6-15 = 8 spike，
            //   详 production/epics/vs-chapter-1/story-006-gameapp-provider-injection.md §V3.0.1 Watch List Hooks。
            if (DevTest.DevBootstrap.HasSpike("S5-02") || DevTest.DevBootstrap.HasSpike("S6-07") ||
                DevTest.DevBootstrap.HasSpike("S6-08") || DevTest.DevBootstrap.HasSpike("S6-04") ||
                DevTest.DevBootstrap.HasSpike("S6-01-playtest") || DevTest.DevBootstrap.HasSpike("S6-13") ||
                DevTest.DevBootstrap.HasSpike("S6-14") || DevTest.DevBootstrap.HasSpike("S6-15"))
            {
                Log.Info("[GameFlow] [main-menu] 检测到 main menu spike (S5-02/S6-07/S6-08/S6-04/S6-01-playtest/S6-13/S6-14/S6-15) — Button click 模式或 spike 自驱或用户手动驱动");

                // 先 RunRequested() 让 spike Runtime.Awake() 同步 subscribe production listeners
                // (per S5-1c lessons memo problem_2026-05-09_spike-sync-subscribe-race.md)
                DevTest.DevBootstrap.RunRequested();
                Log.Info("[GameFlow] [main-menu] DevBootstrap.RunRequested() 完成 → spike Awake() 已 subscribe");

                // 异步 Show main menu panel (走 production UIWindow 路径 — vendor ShowUIAsync → OnCreate
                // 内 Button.onClick.AddListener 挂载完成后 spike P1/P4 case 才能 Button.onClick.Invoke())
                ShowMainMenuPanelAsync().Forget();
                Log.Info("[GameFlow] [main-menu] ShowMainMenuPanelAsync 已启动 (spike 等 panel 就绪后 Invoke Button)");
            }
            else
            {
                // story-001c (2026-05-09): listener-path driver 自闭环（ADR-009 §Decision line 386 spec align）
                //   顺序关键：先 RunRequested() 让 spike GameObject AddComponent 触发 spike Runtime.Awake()
                //   subscribe listeners，再 OnRequestSceneChange(1) 让 listener-path driver 同步 fire
                //   OnSceneTransitionBegin 时 spike listeners 已 attached (S5-1b F4 800ms delay 掩盖的 race
                //   现在通过 sync Awake subscribe 显式解决，per story-001c P1 sync-subscribe pattern)。
                DevTest.DevBootstrap.RunRequested();
                Log.Info("[GameFlow] [story-001c] DevBootstrap.RunRequested() 完成 → spike Awake() 已 subscribe");

                GameEvent.Get<ISceneEvent>().OnRequestSceneChange(1);
                Log.Info("[GameFlow] [story-001c] 已派发 ISceneEvent.OnRequestSceneChange(1) → production listener-path driver 自驱 11-step");
            }

            Log.Info("[GameFlow] DevTestState 停留：等待 Spike 结果（手动停 PlayMode 结束）");
        }

        // S5-02 / S6-07 main menu panel 异步 Show — 走完整 production UIWindow 路径
        // 不 catch 异常: panel 缺失等问题应该 fail loud 不该被 swallow
        private static async UniTaskVoid ShowMainMenuPanelAsync()
        {
            if (GameModule.UI == null)
            {
                Log.Error("[GameFlow] [main-menu] GameModule.UI == null — TEngine UIModule 未 init？");
                return;
            }

            var panel = await GameModule.UI.ShowUIAsyncAwait<MainMenuPanel>();
            Log.Info($"[GameFlow] [main-menu] MainMenuPanel ShowUI 完成 instance={(panel != null ? panel.GetType().Name : "null")}");
        }

        protected override void OnLeave(IFsm<IFsmModule> fsm, bool isShutdown)
        {
            base.OnLeave(fsm, isShutdown);
            Log.Info("[GameFlow] 离开 DevTestState");
        }
    }
}
#endif
