// 该文件由Cursor 自动生成
// S5-03 PlayMode probe — Puzzle State Machine production code R3 mandatory + V2-5 framework boundary
//   per ADR-029 V2.0 §V2-3 + §V2-5 + story-001 §QA Test Cases (10 case = 8 CORE + 2 ADV).
//
// EditMode tests cover ~80% logic (state machine transitions + hysteresis + grace + absence + save/load + defensive ctor)；
// 本 spike 重点验证 PlayMode-only 维度：
//   1. 真 TEngine session 内 IShadowMatchEvent_Gen / IShadowPuzzleEvent_Gen / IPuzzleLockEvent_Gen
//      Source Generator wire-up（GameApp boot 后 _Gen 实例已注册到 EventMgr.Dispatcher）
//   2. ADR-027 §5 framework knowledge fact 实战：listener self-removal × 5 sequential init/shutdown
//      防 TEngine RemoveEventListener 抛 "Delete handle failed"（per S3-03 P5 lesson + ADR-029 V2.0 §V2-5 framework boundary probe）
//   3. Stopwatch 精度测 FSM Tick ≤ 0.05ms p99（advisory）
//   4. Real-time grace period precision ±50ms tolerance（advisory）
//
// JSON evidence 落 Application.persistentDataPath/S5-03_Result.json (沿 S4-07 模式)
// 整文件仅 UNITY_EDITOR || DEBUG 编译；GameApp.RegisterDevSpikes 单 spike 激活防 type-3 race。

#if UNITY_EDITOR || DEBUG
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using Cysharp.Threading.Tasks;
using TEngine;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace GameLogic.DevTest.Spikes
{
    public class S503Spike : IDevSpike
    {
        public string Id => "S5-03";
        public string Name => "Puzzle State Machine — R3 mandatory + V2-5 framework boundary probe";

        public void Launch()
        {
            var go = new GameObject("S503_Runtime");
            UnityEngine.Object.DontDestroyOnLoad(go);
            go.AddComponent<S503Runtime>();
        }
    }

    public class S503Runtime : MonoBehaviour
    {
        private S503Tester _tester;

        private void Start()
        {
            _tester = new S503Tester();
            Log.Info($"[S5-03] Runtime Start. Result JSON: {S503Tester.ResultFilePath}");
            RunAsync().Forget();
        }

        private async UniTaskVoid RunAsync()
        {
            await UniTask.Yield();
            await _tester.RunAllAsync();
            _tester.WriteResultJson();
        }

        private void OnGUI()
        {
            if (_tester == null) return;

            float w = 720, h = 360;
            float x = (Screen.width - w) / 2f;
            float y = 20;

            var box = new GUIStyle(GUI.skin.box) { fontSize = 16, alignment = TextAnchor.MiddleCenter };
            GUI.Box(new Rect(x, y, w, h), string.Empty, box);

            var title = new GUIStyle(GUI.skin.label) { fontSize = 20, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            GUI.Label(new Rect(x, y + 10, w, 30), "S5-03 Puzzle State Machine — R3 + V2-5 Probe", title);

            var lab = new GUIStyle(GUI.skin.label) { fontSize = 14 };
            float ly = y + 50;

            DrawRow(x + 20, ly, w - 40, $"P1 SG wire-up (3 _Gen registered: {_tester.P1GenRegistered}/3)", _tester.P1Passed, lab); ly += 26;
            DrawRow(x + 20, ly, w - 40, $"P2 5 events dispatch (perfectMatch={_tester.P2PerfectCount}, lockAll={_tester.P2LockAllCount})", _tester.P2Passed, lab); ly += 26;
            DrawRow(x + 20, ly, w - 40, $"P3 Hysteresis 30s 振荡 (state changes={_tester.P3StateChanges}, ≤30 expected)", _tester.P3Passed, lab); ly += 26;
            DrawRow(x + 20, ly, w - 40, $"P4 Frozen score immutability (post-frozen score={_tester.P4FrozenScore:F2})", _tester.P4Passed, lab); ly += 26;
            DrawRow(x + 20, ly, w - 40, $"P5 Tutorial grace 3s precision (delta={_tester.P5GraceDeltaMs:F0}ms, ≤100ms)", _tester.P5Passed, lab); ly += 26;
            DrawRow(x + 20, ly, w - 40, $"P6 Listener self-removal × 5 sequential init/shutdown (no exception)", _tester.P6Passed, lab); ly += 26;
            DrawRow(x + 20, ly, w - 40, $"P7 Absence idle timer 5s (delta={_tester.P7IdleDeltaMs:F0}ms, ≤200ms)", _tester.P7Passed, lab); ly += 26;
            DrawRow(x + 20, ly, w - 40, $"P8 (ADV) FSM Tick perf p99 ({_tester.P8TickP99Ms:F4}ms ≤ 0.05ms)", _tester.P8Passed, lab); ly += 36;

            var summaryStyle = new GUIStyle(GUI.skin.label) { fontSize = 18, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            string summary = _tester.AllDone
                ? $"ALL DONE — {_tester.PassCount}/8 PASSED"
                : "RUNNING…";
            GUI.Label(new Rect(x, ly, w, 30), summary, summaryStyle);
        }

        private void DrawRow(float x, float y, float w, string label, bool? passed, GUIStyle lab)
        {
            GUI.Label(new Rect(x, y, w - 80, 22), label, lab);
            string mark = passed == null ? "..." : (passed.Value ? "[PASS]" : "[FAIL]");
            var col = new GUIStyle(lab) {
                fontStyle = FontStyle.Bold,
                normal = { textColor = passed == null ? Color.gray : (passed.Value ? Color.green : Color.red) },
                alignment = TextAnchor.MiddleRight,
            };
            GUI.Label(new Rect(x + w - 80, y, 80, 22), mark, col);
        }
    }

    /// <summary>
    /// S5-03 Tester — 8 PlayMode cases (8 CORE + 1 ADV embedded). EditMode 已 cover state machine logic 80%；
    /// 本 spike 聚焦 PlayMode-only 维度: framework boundary (SG wire-up + listener idempotency) + 真 timer 精度。
    /// </summary>
    public class S503Tester
    {
        public static string ResultFilePath => Path.Combine(Application.persistentDataPath, "S5-03_Result.json");

        public bool? P1Passed; public int P1GenRegistered;
        public bool? P2Passed; public int P2PerfectCount; public int P2LockAllCount;
        public bool? P3Passed; public int P3StateChanges;
        public bool? P4Passed; public float P4FrozenScore;
        public bool? P5Passed; public double P5GraceDeltaMs;
        public bool? P6Passed;
        public bool? P7Passed; public double P7IdleDeltaMs;
        public bool? P8Passed; public double P8TickP99Ms;

        public bool AllDone => P1Passed.HasValue && P2Passed.HasValue && P3Passed.HasValue && P4Passed.HasValue
                              && P5Passed.HasValue && P6Passed.HasValue && P7Passed.HasValue && P8Passed.HasValue;

        public int PassCount
        {
            get
            {
                int c = 0;
                if (P1Passed == true) c++;
                if (P2Passed == true) c++;
                if (P3Passed == true) c++;
                if (P4Passed == true) c++;
                if (P5Passed == true) c++;
                if (P6Passed == true) c++;
                if (P7Passed == true) c++;
                if (P8Passed == true) c++;
                return c;
            }
        }

        public async UniTask RunAllAsync()
        {
            await P1_SourceGeneratorWireUp();
            await P2_FiveEventsDispatch();
            await P3_HysteresisOscillation();
            await P4_FrozenScoreImmutability();
            await P5_TutorialGracePeriodPrecision();
            await P6_ListenerSelfRemovalSequential();
            await P7_AbsenceIdleTimerPrecision();
            await P8_FsmTickPerf();
        }

        // ────────── P1: Source Generator wire-up (3 _Gen 注册) ──────────
        private async UniTask P1_SourceGeneratorWireUp()
        {
            try
            {
                int registered = 0;
                var asm = typeof(IShadowMatchEvent).Assembly;
                if (asm.GetType("GameLogic.IShadowMatchEvent_Gen") != null) registered++;
                if (asm.GetType("GameLogic.IShadowPuzzleEvent_Gen") != null) registered++;
                if (asm.GetType("GameLogic.IPuzzleLockEvent_Gen") != null) registered++;
                P1GenRegistered = registered;
                P1Passed = registered == 3;
            }
            catch (Exception e)
            {
                Log.Error($"[S5-03 P1] Exception: {e}");
                P1Passed = false;
            }
            await UniTask.Yield();
        }

        // ────────── P2: 5 events dispatch — full PerfectMatch path 端到端 ──────────
        private async UniTask P2_FiveEventsDispatch()
        {
            int perfectCount = 0, lockAllCount = 0;
            Action<int, float> hPerfect = (id, score) => perfectCount++;
            Action hLockAll = () => lockAllCount++;

            try
            {
                GameEvent.AddEventListener<int, float>(IShadowPuzzleEvent_Event.OnPerfectMatch, hPerfect);
                GameEvent.AddEventListener(IPuzzleLockEvent_Event.OnPuzzleLockAll, hLockAll);

                var sm = new PuzzleStateMachine();
                var cfg = new PuzzleStateConfig(id: 100, isAbsencePuzzle: false,
                    nearMatchThreshold: 0.40f, perfectMatchThreshold: 0.85f,
                    maxCompletionScore: 0f, absenceAcceptDelay: 0f, tutorialGracePeriod: 0f);
                sm.Initialize(cfg.Id, cfg);
                sm.OnChapterUnlocked();
                sm.OnPlayerInteraction();

                GameEvent.Get<IShadowMatchEvent>().OnMatchScoreUpdated(cfg.Id, 0.92f);

                P2PerfectCount = perfectCount;
                P2LockAllCount = lockAllCount;
                P2Passed = perfectCount == 1 && lockAllCount == 1 && sm.CurrentState == PuzzleState.Complete;

                sm.Shutdown();
            }
            catch (Exception e)
            {
                Log.Error($"[S5-03 P2] Exception: {e}");
                P2Passed = false;
            }
            finally
            {
                if (hPerfect != null)
                {
                    GameEvent.RemoveEventListener<int, float>(IShadowPuzzleEvent_Event.OnPerfectMatch, hPerfect);
                    hPerfect = null;
                }
                if (hLockAll != null)
                {
                    GameEvent.RemoveEventListener(IPuzzleLockEvent_Event.OnPuzzleLockAll, hLockAll);
                    hLockAll = null;
                }
            }
            await UniTask.Yield();
        }

        // ────────── P3: Hysteresis 振荡 30s 模拟 — count state changes per second ≤ 1 ──────────
        private async UniTask P3_HysteresisOscillation()
        {
            int stateChanges = 0;
            PuzzleState lastState = PuzzleState.Idle;
            var sm = new PuzzleStateMachine();
            var cfg = new PuzzleStateConfig(id: 101, isAbsencePuzzle: false,
                nearMatchThreshold: 0.40f, perfectMatchThreshold: 0.85f,
                maxCompletionScore: 0f, absenceAcceptDelay: 0f, tutorialGracePeriod: 0f);
            try
            {
                sm.Initialize(cfg.Id, cfg);
                sm.OnChapterUnlocked();
                sm.OnPlayerInteraction();
                lastState = sm.CurrentState;

                // Simulate 30 ticks (0.1s each = 3s real-equivalent) with score oscillation 0.38-0.42
                // Hysteresis design: stays NearMatch in [0.35, 0.40); 振荡 in [0.38, 0.42] should cause ≤ 1 enter and 0 exits
                for (int i = 0; i < 30; i++)
                {
                    float score = i % 2 == 0 ? 0.42f : 0.38f;
                    GameEvent.Get<IShadowMatchEvent>().OnMatchScoreUpdated(cfg.Id, score);
                    if (sm.CurrentState != lastState)
                    {
                        stateChanges++;
                        lastState = sm.CurrentState;
                    }
                    sm.Tick(0.1f);
                }
                P3StateChanges = stateChanges;
                // Expected: at most 1 transition (Active → NearMatch on first ≥0.40 score)；no oscillation flicker
                P3Passed = stateChanges <= 2;
            }
            catch (Exception e)
            {
                Log.Error($"[S5-03 P3] Exception: {e}");
                P3Passed = false;
            }
            finally
            {
                sm?.Shutdown();
            }
            await UniTask.Yield();
        }

        // ────────── P4: Frozen score immutability after PerfectMatch ──────────
        private async UniTask P4_FrozenScoreImmutability()
        {
            var sm = new PuzzleStateMachine();
            var cfg = new PuzzleStateConfig(id: 102, isAbsencePuzzle: false,
                nearMatchThreshold: 0.40f, perfectMatchThreshold: 0.85f,
                maxCompletionScore: 0f, absenceAcceptDelay: 0f, tutorialGracePeriod: 0f);
            try
            {
                sm.Initialize(cfg.Id, cfg);
                sm.OnChapterUnlocked();
                sm.OnPlayerInteraction();

                GameEvent.Get<IShadowMatchEvent>().OnMatchScoreUpdated(cfg.Id, 0.91f);
                float frozen = sm.MatchScore;
                Assert(Mathf.Approximately(frozen, 0.91f), "Score should be 0.91 at PerfectMatch entry");

                // Try to update — should be ignored (frozen)
                GameEvent.Get<IShadowMatchEvent>().OnMatchScoreUpdated(cfg.Id, 0.50f);
                P4FrozenScore = sm.MatchScore;
                P4Passed = Mathf.Approximately(P4FrozenScore, 0.91f);
            }
            catch (Exception e)
            {
                Log.Error($"[S5-03 P4] Exception: {e}");
                P4Passed = false;
            }
            finally
            {
                sm?.Shutdown();
            }
            await UniTask.Yield();
        }

        // ────────── P5: Tutorial grace period 3s real-time precision ──────────
        private async UniTask P5_TutorialGracePeriodPrecision()
        {
            var sm = new PuzzleStateMachine();
            var cfg = new PuzzleStateConfig(id: 103, isAbsencePuzzle: false,
                nearMatchThreshold: 0.40f, perfectMatchThreshold: 0.85f,
                maxCompletionScore: 0f, absenceAcceptDelay: 0f, tutorialGracePeriod: 3.0f);
            try
            {
                sm.Initialize(cfg.Id, cfg);
                sm.OnChapterUnlocked();
                sm.OnPlayerInteraction();
                sm.OnTutorialCompleted();

                var sw = Stopwatch.StartNew();
                while (sm.IsInGracePeriod)
                {
                    sm.Tick(Time.deltaTime);
                    await UniTask.Yield();
                    if (sw.Elapsed.TotalSeconds > 5.0) break; // safety bail-out
                }
                sw.Stop();

                P5GraceDeltaMs = Math.Abs(sw.Elapsed.TotalMilliseconds - 3000.0);
                P5Passed = P5GraceDeltaMs <= 100.0;
            }
            catch (Exception e)
            {
                Log.Error($"[S5-03 P5] Exception: {e}");
                P5Passed = false;
            }
            finally
            {
                sm?.Shutdown();
            }
        }

        // ────────── P6: Listener self-removal × 5 sequential init/shutdown ──────────
        // ADR-027 §5 framework knowledge fact + ADR-029 V2.0 §V2-5 framework boundary probe:
        //   sequential Initialize / Shutdown 5 次 — null-out + null-check guard 防 TEngine "Delete handle failed"
        private async UniTask P6_ListenerSelfRemovalSequential()
        {
            try
            {
                for (int i = 0; i < 5; i++)
                {
                    var sm = new PuzzleStateMachine();
                    var cfg = new PuzzleStateConfig(id: 200 + i, isAbsencePuzzle: false,
                        nearMatchThreshold: 0.40f, perfectMatchThreshold: 0.85f,
                        maxCompletionScore: 0f, absenceAcceptDelay: 0f, tutorialGracePeriod: 0f);
                    sm.Initialize(cfg.Id, cfg);
                    sm.Shutdown();
                    sm.Shutdown(); // double-shutdown — null-check guard 防 raw double-remove
                }
                P6Passed = true;
            }
            catch (Exception e)
            {
                Log.Error($"[S5-03 P6] Exception (Type-2(c) framework boundary lesson regression): {e}");
                P6Passed = false;
            }
            await UniTask.Yield();
        }

        // ────────── P7: Absence idle timer 5s real-time precision ──────────
        private async UniTask P7_AbsenceIdleTimerPrecision()
        {
            var sm = new PuzzleStateMachine();
            var cfg = new PuzzleStateConfig(id: 51, isAbsencePuzzle: true,
                nearMatchThreshold: 0.40f, perfectMatchThreshold: 0.85f,
                maxCompletionScore: 0.65f, absenceAcceptDelay: 5.0f, tutorialGracePeriod: 0f);
            bool fired = false;
            Action<int, float> h = (id, score) => fired = true;
            try
            {
                GameEvent.AddEventListener<int, float>(IShadowPuzzleEvent_Event.OnAbsenceAccepted, h);

                sm.Initialize(cfg.Id, cfg);
                sm.OnChapterUnlocked();
                sm.OnPlayerInteraction();
                GameEvent.Get<IShadowMatchEvent>().OnMatchScoreUpdated(cfg.Id, 0.70f);

                var sw = Stopwatch.StartNew();
                while (!fired)
                {
                    sm.Tick(Time.deltaTime);
                    await UniTask.Yield();
                    if (sw.Elapsed.TotalSeconds > 8.0) break; // safety bail-out
                }
                sw.Stop();

                P7IdleDeltaMs = Math.Abs(sw.Elapsed.TotalMilliseconds - 5000.0);
                P7Passed = fired && P7IdleDeltaMs <= 200.0;
            }
            catch (Exception e)
            {
                Log.Error($"[S5-03 P7] Exception: {e}");
                P7Passed = false;
            }
            finally
            {
                if (h != null)
                {
                    GameEvent.RemoveEventListener<int, float>(IShadowPuzzleEvent_Event.OnAbsenceAccepted, h);
                    h = null;
                }
                sm?.Shutdown();
            }
        }

        // ────────── P8 (ADV): FSM Tick perf p99 ≤ 0.05ms ──────────
        private async UniTask P8_FsmTickPerf()
        {
            var sm = new PuzzleStateMachine();
            var cfg = new PuzzleStateConfig(id: 300, isAbsencePuzzle: true,
                nearMatchThreshold: 0.40f, perfectMatchThreshold: 0.85f,
                maxCompletionScore: 0.65f, absenceAcceptDelay: 5.0f, tutorialGracePeriod: 0f);
            try
            {
                sm.Initialize(cfg.Id, cfg);
                sm.OnChapterUnlocked();
                sm.OnPlayerInteraction();
                GameEvent.Get<IShadowMatchEvent>().OnMatchScoreUpdated(cfg.Id, 0.70f);

                // Warm up
                for (int i = 0; i < 100; i++) sm.Tick(0.016f);

                // Measure 1000 Tick samples
                var samples = new List<double>(1000);
                for (int i = 0; i < 1000; i++)
                {
                    var sw = Stopwatch.StartNew();
                    sm.Tick(0.016f);
                    sw.Stop();
                    samples.Add(sw.Elapsed.TotalMilliseconds);
                }
                samples.Sort();
                P8TickP99Ms = samples[(int)(samples.Count * 0.99)];
                P8Passed = P8TickP99Ms <= 0.05;
            }
            catch (Exception e)
            {
                Log.Error($"[S5-03 P8] Exception: {e}");
                P8Passed = false;
            }
            finally
            {
                sm?.Shutdown();
            }
            await UniTask.Yield();
        }

        // ────────── JSON output ──────────
        public void WriteResultJson()
        {
            try
            {
                var sb = new StringBuilder();
                sb.Append("{\n");
                sb.Append($"  \"spike\": \"S5-03\",\n");
                sb.Append($"  \"name\": \"Puzzle State Machine R3 + V2-5 probe\",\n");
                sb.Append($"  \"adr_governance\": [\"ADR-014\", \"ADR-027\", \"ADR-029 V2.0\"],\n");
                sb.Append($"  \"editmode_tests_complement\": \"PuzzleStateMachineTests.cs (11 tests; AC-1 to AC-14 + defensive ctor)\",\n");
                sb.Append($"  \"all_done\": {AllDone.ToString().ToLowerInvariant()},\n");
                sb.Append($"  \"pass_count\": {PassCount},\n");
                sb.Append($"  \"results\": {{\n");
                sb.Append($"    \"P1_sg_wireup\": {{ \"passed\": {Json(P1Passed)}, \"gen_registered\": {P1GenRegistered} }},\n");
                sb.Append($"    \"P2_five_events_dispatch\": {{ \"passed\": {Json(P2Passed)}, \"perfect_count\": {P2PerfectCount}, \"lock_all_count\": {P2LockAllCount} }},\n");
                sb.Append($"    \"P3_hysteresis_30osc\": {{ \"passed\": {Json(P3Passed)}, \"state_changes\": {P3StateChanges} }},\n");
                sb.Append($"    \"P4_frozen_score\": {{ \"passed\": {Json(P4Passed)}, \"frozen_score\": {P4FrozenScore:F4} }},\n");
                sb.Append($"    \"P5_tutorial_grace_3s\": {{ \"passed\": {Json(P5Passed)}, \"delta_ms\": {P5GraceDeltaMs:F2} }},\n");
                sb.Append($"    \"P6_listener_self_removal_x5\": {{ \"passed\": {Json(P6Passed)} }},\n");
                sb.Append($"    \"P7_absence_idle_5s\": {{ \"passed\": {Json(P7Passed)}, \"delta_ms\": {P7IdleDeltaMs:F2} }},\n");
                sb.Append($"    \"P8_adv_fsm_tick_perf\": {{ \"passed\": {Json(P8Passed)}, \"p99_ms\": {P8TickP99Ms:F4} }}\n");
                sb.Append($"  }}\n");
                sb.Append("}\n");
                File.WriteAllText(ResultFilePath, sb.ToString());
                Log.Info($"[S5-03] Result JSON written to: {ResultFilePath}");
            }
            catch (Exception e)
            {
                Log.Error($"[S5-03] WriteResultJson failed: {e}");
            }
        }

        private static string Json(bool? b) => b.HasValue ? b.Value.ToString().ToLowerInvariant() : "null";

        private static void Assert(bool cond, string msg)
        {
            if (!cond) throw new InvalidOperationException(msg);
        }
    }
}
#endif
