// 该文件由Cursor 自动生成
// S6-01 Playtest Hold Mode spike — manual playtest infrastructure 占位 spike
//   per production/playtests/playtest-vs-chapter-1-session-1-2026-05-13.md (S6-01 evidence doc)。
//
// 目的：
//   让 main menu 显示后由用户手动驱动游戏完成 ≥30 min internal playtest session，
//   而非 R3 PlayMode test spike 自动跑 case 干扰 manual playtest 节奏。
//
// 关联文档:
//   * production/playtests/playtest-vs-chapter-1-session-1-2026-05-13.md (S6-01 evidence doc + Phase 2 起步 checklist)
//   * Assets/GameScripts/HotFix/GameLogic/GameFlow/DevTestState.cs       ([main-menu] mode HasSpike list)
//   * Assets/GameScripts/HotFix/GameLogic/GameApp.cs                     (RegisterDevSpikes 注册 S601PlaytestSpike)
//
// 与其他 R3 PlayMode test spike (S5-02 / S6-04 / S6-07 / S6-08 等) 区别：
//   * R3 spike: Launch() 内 AddComponent MonoBehaviour → Awake/Start 自动跑 R3 case + assert（Editor 内自动化）
//   * Playtest spike: Launch() no-op，不挂 MonoBehaviour，不 fire 任何 GameEvent，不跑 R3 case
//                     仅作为 DevTestState [main-menu] mode 的触发器 — HasSpike("S6-01-playtest")=true 走
//                     ShowMainMenuPanelAsync，之后由用户在 main menu 手动 click NewGame Button →
//                     ISceneEvent.OnRequestSceneChange(1) → chapter 1 load
//
// 复用范围:
//   Sprint 6+ 所有 manual playtest session 复用此 spike (S6-02 / S6-03 / 未来 chapter 2 playtest 等)。
//   重跑 S6-04 等 R3 spike 时仅需 GameApp.RegisterDevSpikes 切换注册行（spike file 一一保留）。
//
// V3.0.1 dp14 candidate NEW (2026-05-14 Session 32):
//   playtest infrastructure pattern gap — S6-01 Phase 1 prep 仅想到 evidence doc 模板，没想到 spike RunAllAsync
//   与 manual playtest 兼容性问题；Phase 2 起步时即 surface。Sprint 6 retro 议题 — 评估 promote 为 ADR-029 V3
//   正式 dp + standard PlaytestMode pattern 文档化。
//
// 整文件仅在 UNITY_EDITOR || DEBUG 编译，Release 包零残留。

#if UNITY_EDITOR || DEBUG
using TEngine;

namespace GameLogic.DevTest.Spikes
{
    /// <summary>
    /// S6-01 Playtest Hold Mode spike — playtest infrastructure no-op spike。
    ///
    /// 作用：让 DevTestState 的 [main-menu] mode HasSpike 检测命中 → ShowMainMenuPanelAsync 显示 MainMenuPanel，
    /// 之后由用户在 main menu 手动 click NewGame Button 进入 chapter 1 完成 manual playtest session。
    /// </summary>
    public sealed class S601PlaytestSpike : IDevSpike
    {
        public string Id => "S6-01-playtest";

        public string Name => "S6-01 Playtest Hold Mode (manual playtest infrastructure — main menu 显示后由用户手动驱动)";

        public void Launch()
        {
            Log.Info("[S6-01-playtest] no-op launch — playtest hold mode：main menu 显示后由用户手动驱动 chapter 1");
        }
    }
}
#endif
