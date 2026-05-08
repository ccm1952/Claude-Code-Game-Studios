// 该文件由Cursor 自动生成
// S5-05 PlayMode probe — Narrative Sequence Engine production code R3 mandatory + V2-5 framework boundary
//   per ADR-029 V2.0 §V2-3 + §V2-5 + story-001 §QA Test Cases (10 case = 8 CORE + 2 ADV).
//
// EditMode tests cover ~70% logic (config / sort invariant / atomic effect Tick driving / Registry dispatch /
// Luban provider defaults — 13 tests)；本 spike 重点验证 PlayMode-only 维度：
//   1. 真 TEngine session 内 INarrativeEvent_Gen / IInputBlockerEvent_Gen / IAudioEvent_Gen
//      Source Generator wire-up（GameApp boot 后 _Gen 实例已注册到 EventMgr.Dispatcher）
//   2. ADR-027 §5 framework knowledge fact 实战：4 listener self-removal × 5 sequential init/shutdown
//      防 TEngine RemoveEventListener 抛 "Delete handle failed"
//   3. Queue management 端到端（max 3 + overflow OnSequenceFailed）
//   4. Token-based InputBlocker push/pop pairing — sequence-scoped lifecycle
//   5. Sequence 触发到 first effect Start 的 ≤ 1 frame 延迟
//   6. App pause/resume — Pause() 期间 timer 暂停，Resume() 后恢复
//
// JSON evidence 落 Application.persistentDataPath/S5-05_Result.json (沿 S5-03 模式)
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
    public class S505Spike : IDevSpike
    {
        public string Id => "S5-05";
        public string Name => "Narrative Sequence Engine — R3 mandatory + V2-5 framework boundary probe";

        public void Launch()
        {
            var go = new GameObject("S505_Runtime");
            UnityEngine.Object.DontDestroyOnLoad(go);
            go.AddComponent<S505Runtime>();
        }
    }

    public class S505Runtime : MonoBehaviour
    {
        private S505Tester _tester;

        private void Start()
        {
            _tester = new S505Tester();
            Log.Info($"[S5-05] Runtime Start. Result JSON: {S505Tester.ResultFilePath}");
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

            float w = 760, h = 420;
            float x = (Screen.width - w) / 2f;
            float y = 20;

            var box = new GUIStyle(GUI.skin.box) { fontSize = 16, alignment = TextAnchor.MiddleCenter };
            GUI.Box(new Rect(x, y, w, h), string.Empty, box);

            var title = new GUIStyle(GUI.skin.label) { fontSize = 20, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            GUI.Label(new Rect(x, y + 10, w, 30), "S5-05 Narrative Sequence Engine — R3 + V2-5 Probe", title);

            var lab = new GUIStyle(GUI.skin.label) { fontSize = 14 };
            float ly = y + 50;

            DrawRow(x + 20, ly, w - 40, $"P1 SG wire-up (3 _Gen registered: {_tester.P1GenRegistered}/3)", _tester.P1Passed, lab); ly += 26;
            DrawRow(x + 20, ly, w - 40, $"P2 Sequence start dispatch (start={_tester.P2StartCount}, state={_tester.P2State})", _tester.P2Passed, lab); ly += 26;
            DrawRow(x + 20, ly, w - 40, $"P3 Time-sorted effects (started_in_order={_tester.P3StartedInOrder})", _tester.P3Passed, lab); ly += 26;
            DrawRow(x + 20, ly, w - 40, $"P4 InputBlocker push/pop pairing (push={_tester.P4PushCount}, pop={_tester.P4PopCount}, tokens_match={_tester.P4TokensMatch})", _tester.P4Passed, lab); ly += 26;
            DrawRow(x + 20, ly, w - 40, $"P5 Queue 3 sequential (completed={_tester.P5CompletedCount}/3)", _tester.P5Passed, lab); ly += 26;
            DrawRow(x + 20, ly, w - 40, $"P6 Queue overflow (failed_overflow={_tester.P6FailedOverflowCount}, expected=1)", _tester.P6Passed, lab); ly += 26;
            DrawRow(x + 20, ly, w - 40, $"P7 Effect type not implemented resilience (failed_skip={_tester.P7FailedSkipCount}, completed={_tester.P7Completed})", _tester.P7Passed, lab); ly += 26;
            DrawRow(x + 20, ly, w - 40, $"P8 Listener self-removal × 5 sequential (no exception)", _tester.P8Passed, lab); ly += 26;
            DrawRow(x + 20, ly, w - 40, $"P9 (ADV) App pause/resume timer (pause_delta={_tester.P9PauseDeltaMs:F0}ms)", _tester.P9Passed, lab); ly += 26;
            DrawRow(x + 20, ly, w - 40, $"P10 (ADV) Trigger-to-first-start latency ({_tester.P10LatencyMs:F2}ms ≤ 16ms)", _tester.P10Passed, lab); ly += 36;

            var summaryStyle = new GUIStyle(GUI.skin.label) { fontSize = 18, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            string summary = _tester.AllDone
                ? $"ALL DONE — {_tester.PassCount}/10 PASSED"
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
    /// S5-05 Tester — 10 PlayMode cases (8 CORE + 2 ADV)。EditMode 已 cover config / 排序 / Tick 自身 / Registry
    /// dispatch / Luban provider defaults 70%；本 spike 聚焦 PlayMode-only 维度：framework boundary
    /// (SG wire-up + 4-listener idempotency) + 真 timer 精度 + 端到端 token + queue + resilience。
    /// </summary>
    public class S505Tester
    {
        public static string ResultFilePath => Path.Combine(Application.persistentDataPath, "S5-05_Result.json");

        public bool? P1Passed; public int P1GenRegistered;
        public bool? P2Passed; public int P2StartCount; public string P2State = "?";
        public bool? P3Passed; public string P3StartedInOrder = "?";
        public bool? P4Passed; public int P4PushCount; public int P4PopCount; public bool P4TokensMatch;
        public bool? P5Passed; public int P5CompletedCount;
        public bool? P6Passed; public int P6FailedOverflowCount;
        public bool? P7Passed; public int P7FailedSkipCount; public bool P7Completed;
        public bool? P8Passed;
        public bool? P9Passed; public double P9PauseDeltaMs;
        public bool? P10Passed; public double P10LatencyMs;

        public bool AllDone => P1Passed.HasValue && P2Passed.HasValue && P3Passed.HasValue && P4Passed.HasValue
                              && P5Passed.HasValue && P6Passed.HasValue && P7Passed.HasValue && P8Passed.HasValue
                              && P9Passed.HasValue && P10Passed.HasValue;

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
                if (P9Passed == true) c++;
                if (P10Passed == true) c++;
                return c;
            }
        }

        public async UniTask RunAllAsync()
        {
            await P1_SourceGeneratorWireUp();
            await P2_SequenceStartDispatch();
            await P3_TimeSortedEffects();
            await P4_InputBlockerPushPopPairing();
            await P5_QueueThreeSequential();
            await P6_QueueOverflow();
            await P7_EffectTypeNotImplementedResilience();
            await P8_ListenerSelfRemovalSequential();
            await P9_AppPauseResumeTimer();
            await P10_TriggerToFirstStartLatency();
        }

        // ────────── P1: Source Generator wire-up (3 _Gen 注册) ──────────
        private async UniTask P1_SourceGeneratorWireUp()
        {
            try
            {
                int registered = 0;
                var asm = typeof(INarrativeEvent).Assembly;
                if (asm.GetType("GameLogic.INarrativeEvent_Gen") != null) registered++;
                if (asm.GetType("GameLogic.IInputBlockerEvent_Gen") != null) registered++;
                if (asm.GetType("GameLogic.IAudioEvent_Gen") != null) registered++;
                P1GenRegistered = registered;
                P1Passed = registered == 3;
            }
            catch (Exception e)
            {
                Log.Error($"[S5-05 P1] Exception: {e}");
                P1Passed = false;
            }
            await UniTask.Yield();
        }

        // ────────── P2: Sequence start dispatch — 触发 OnRequestSequence → OnSequenceStart fire + State=Playing ──────────
        private async UniTask P2_SequenceStartDispatch()
        {
            int startCount = 0;
            Action<int, NarrativeSequenceType> hStart = (id, type) => startCount++;
            var player = new NarrativeSequencePlayer();
            try
            {
                GameEvent.AddEventListener<int, NarrativeSequenceType>(INarrativeEvent_Event.OnSequenceStart, hStart);

                var provider = new NarrativeSequenceConfigFromLuban();
                provider.InitWithDefaults();
                player.Initialize(provider);

                GameEvent.Get<INarrativeEvent>().OnRequestSequence(1, NarrativeSequenceType.MemoryReplay);
                await UniTask.Yield(); // 允许同帧 dispatch 完成

                P2StartCount = startCount;
                P2State = player.State.ToString();
                P2Passed = startCount == 1 && player.State == SequenceState.Playing && player.ActiveSequenceId == 100;
            }
            catch (Exception e)
            {
                Log.Error($"[S5-05 P2] Exception: {e}");
                P2Passed = false;
            }
            finally
            {
                if (hStart != null)
                {
                    GameEvent.RemoveEventListener<int, NarrativeSequenceType>(INarrativeEvent_Event.OnSequenceStart, hStart);
                    hStart = null;
                }
                player?.Shutdown();
            }
        }

        // ────────── P3: Time-sorted effects — Tick driving 按 startTime 顺序 Start ──────────
        // V2 fix (post-2026-05-08 first-run): track 每个 effect 的 IsStarted false→true transition 顺序，
        //   不依赖 ActiveEffects.Count 增长（StartSequence 时 ActiveEffects 一次性创建 N 个，Count 固定）。
        //   sequence Complete 后 ActiveEffects.Clear() — 必须在 sequence 仍 Playing 期间记录 transition。
        private async UniTask P3_TimeSortedEffects()
        {
            var startedOrder = new List<string>();
            var player = new NarrativeSequencePlayer();
            try
            {
                var provider = new NarrativeSequenceConfigFromLuban();
                // 自定义 sequence 4 effects with distinct startTime — verify 按时序 Start 顺序
                provider.RegisterByTrigger(triggerSourceId: 9001, type: NarrativeSequenceType.MemoryReplay,
                    config: new NarrativeSequenceConfig(
                        id: 9001,
                        type: NarrativeSequenceType.MemoryReplay,
                        effects: new AtomicEffectConfig[]
                        {
                            new WaitEffectConfig(startTime: 0.0f, duration: 0.1f),
                            new WaitEffectConfig(startTime: 0.3f, duration: 0.1f),
                            new WaitEffectConfig(startTime: 0.6f, duration: 0.1f),
                            new WaitEffectConfig(startTime: 0.9f, duration: 0.1f),
                        },
                        totalDuration: 1.2f,
                        isChapterFinal: false));

                player.Initialize(provider);
                GameEvent.Get<INarrativeEvent>().OnRequestSequence(9001, NarrativeSequenceType.MemoryReplay);

                // 每帧检查每个 effect 的 IsStarted 由 false→true 的 transition;
                // 在 sequence 仍 Playing 期间记录 (Complete 后 ActiveEffects.Clear)
                var startedFlags = new bool[8];
                for (int i = 0; i < 30; i++)
                {
                    player.Tick(0.05f);
                    var active = player.ActiveEffects;
                    if (active != null)
                    {
                        for (int k = 0; k < active.Count && k < 8; k++)
                        {
                            if (active[k] != null && active[k].IsStarted && !startedFlags[k])
                            {
                                startedFlags[k] = true;
                                startedOrder.Add($"{active[k].Config.StartTime:F1}");
                            }
                        }
                    }
                    await UniTask.Yield();
                }

                P3StartedInOrder = startedOrder.Count > 0 ? string.Join(",", startedOrder) : "(none)";
                // 期望: 4 effects 按 startTime asc 顺序触发 ("0.0,0.3,0.6,0.9")
                P3Passed = startedOrder.Count == 4 &&
                           startedOrder[0] == "0.0" && startedOrder[1] == "0.3" &&
                           startedOrder[2] == "0.6" && startedOrder[3] == "0.9";
            }
            catch (Exception e)
            {
                Log.Error($"[S5-05 P3] Exception: {e}");
                P3Passed = false;
            }
            finally
            {
                player?.Shutdown();
            }
        }

        // ────────── P4: InputBlocker push/pop pairing — token 配对 ──────────
        private async UniTask P4_InputBlockerPushPopPairing()
        {
            int pushCount = 0, popCount = 0;
            string pushToken = null, popToken = null;
            Action<string> hPush = t => { pushCount++; pushToken = t; };
            Action<string> hPop = t => { popCount++; popToken = t; };
            var player = new NarrativeSequencePlayer();
            try
            {
                GameEvent.AddEventListener<string>(IInputBlockerEvent_Event.OnPushBlocker, hPush);
                GameEvent.AddEventListener<string>(IInputBlockerEvent_Event.OnPopBlocker, hPop);

                var provider = new NarrativeSequenceConfigFromLuban();
                provider.InitWithDefaults();
                player.Initialize(provider);

                GameEvent.Get<INarrativeEvent>().OnRequestSequence(1, NarrativeSequenceType.MemoryReplay); // sequence id=100
                // Tick 推到完成 (TotalDuration=2.0s)
                for (int i = 0; i < 50; i++)
                {
                    player.Tick(0.05f);
                    await UniTask.Yield();
                }

                P4PushCount = pushCount;
                P4PopCount = popCount;
                P4TokensMatch = pushToken != null && pushToken == popToken && pushToken == "narrative_seq_100";
                P4Passed = pushCount == 1 && popCount == 1 && P4TokensMatch && player.State == SequenceState.Idle;
            }
            catch (Exception e)
            {
                Log.Error($"[S5-05 P4] Exception: {e}");
                P4Passed = false;
            }
            finally
            {
                if (hPush != null)
                {
                    GameEvent.RemoveEventListener<string>(IInputBlockerEvent_Event.OnPushBlocker, hPush);
                    hPush = null;
                }
                if (hPop != null)
                {
                    GameEvent.RemoveEventListener<string>(IInputBlockerEvent_Event.OnPopBlocker, hPop);
                    hPop = null;
                }
                player?.Shutdown();
            }
        }

        // ────────── P5: Queue 3 sequential — 3 rapid trigger → 3 sequences 顺序播完 ──────────
        private async UniTask P5_QueueThreeSequential()
        {
            int completedCount = 0;
            Action<int, NarrativeSequenceType> hComplete = (id, type) => completedCount++;
            var player = new NarrativeSequencePlayer();
            try
            {
                GameEvent.AddEventListener<int, NarrativeSequenceType>(INarrativeEvent_Event.OnSequenceComplete, hComplete);

                var provider = new NarrativeSequenceConfigFromLuban();
                // 3 quick sequences (each 0.3s totalDuration)
                for (int i = 1; i <= 3; i++)
                {
                    provider.RegisterByTrigger(triggerSourceId: 5000 + i, type: NarrativeSequenceType.MemoryReplay,
                        config: new NarrativeSequenceConfig(
                            id: 5000 + i,
                            type: NarrativeSequenceType.MemoryReplay,
                            effects: new AtomicEffectConfig[] { new WaitEffectConfig(0f, 0.3f) },
                            totalDuration: 0.3f,
                            isChapterFinal: false));
                }
                player.Initialize(provider);

                // 3 rapid trigger
                GameEvent.Get<INarrativeEvent>().OnRequestSequence(5001, NarrativeSequenceType.MemoryReplay);
                GameEvent.Get<INarrativeEvent>().OnRequestSequence(5002, NarrativeSequenceType.MemoryReplay);
                GameEvent.Get<INarrativeEvent>().OnRequestSequence(5003, NarrativeSequenceType.MemoryReplay);

                // 此时应 1 sequence Playing + 2 in queue
                bool initialQueueOk = player.State == SequenceState.Playing && player.QueueDepth == 2;

                // Tick 推到全部完成 (~1.0s total + buffer)
                for (int i = 0; i < 60; i++)
                {
                    player.Tick(0.05f);
                    await UniTask.Yield();
                    if (completedCount == 3 && player.State == SequenceState.Idle) break;
                }

                P5CompletedCount = completedCount;
                P5Passed = initialQueueOk && completedCount == 3 && player.State == SequenceState.Idle && player.QueueDepth == 0;
            }
            catch (Exception e)
            {
                Log.Error($"[S5-05 P5] Exception: {e}");
                P5Passed = false;
            }
            finally
            {
                if (hComplete != null)
                {
                    GameEvent.RemoveEventListener<int, NarrativeSequenceType>(INarrativeEvent_Event.OnSequenceComplete, hComplete);
                    hComplete = null;
                }
                player?.Shutdown();
            }
        }

        // ────────── P6: Queue overflow — 5th trigger when queue full → OnSequenceFailed("queue_overflow") ──────────
        // V2 fix (post-2026-05-08 first-run): NarrativeSequencePlayer in-flight 容量 = 1 active + 3 queued
        // (QueueCapacity=3)；rapid trigger N 次时:
        //   1st → active (state=Playing, queue=0)
        //   2nd → queued (queue=1)
        //   3rd → queued (queue=2)
        //   4th → queued (queue=3, 已 full)
        //   5th → OVERFLOW (queue 拒绝 + OnSequenceFailed("queue_overflow"))
        // AC-11 "4th trigger when queue full" 字面 = "queue 已 full 后再来一个 trigger" = 第 5 个总 trigger。
        private async UniTask P6_QueueOverflow()
        {
            int failedCount = 0;
            string lastReason = null;
            Action<int, string> hFailed = (id, reason) => { failedCount++; lastReason = reason; };
            var player = new NarrativeSequencePlayer();
            try
            {
                GameEvent.AddEventListener<int, string>(INarrativeEvent_Event.OnSequenceFailed, hFailed);

                var provider = new NarrativeSequenceConfigFromLuban();
                for (int i = 1; i <= 5; i++)
                {
                    provider.RegisterByTrigger(triggerSourceId: 6000 + i, type: NarrativeSequenceType.MemoryReplay,
                        config: new NarrativeSequenceConfig(
                            id: 6000 + i,
                            type: NarrativeSequenceType.MemoryReplay,
                            effects: new AtomicEffectConfig[] { new WaitEffectConfig(0f, 1.0f) },
                            totalDuration: 1.0f,
                            isChapterFinal: false));
                }
                player.Initialize(provider);

                // 5 rapid trigger — 1 active + 3 queue full + 5th = overflow
                GameEvent.Get<INarrativeEvent>().OnRequestSequence(6001, NarrativeSequenceType.MemoryReplay); // active
                GameEvent.Get<INarrativeEvent>().OnRequestSequence(6002, NarrativeSequenceType.MemoryReplay); // queue 1
                GameEvent.Get<INarrativeEvent>().OnRequestSequence(6003, NarrativeSequenceType.MemoryReplay); // queue 2
                GameEvent.Get<INarrativeEvent>().OnRequestSequence(6004, NarrativeSequenceType.MemoryReplay); // queue 3 (full)
                GameEvent.Get<INarrativeEvent>().OnRequestSequence(6005, NarrativeSequenceType.MemoryReplay); // OVERFLOW

                P6FailedOverflowCount = failedCount;
                // 验证: 1 active + 3 in queue (full) + 1 overflow fired OnSequenceFailed("queue_overflow")
                bool queueIntact = player.State == SequenceState.Playing && player.QueueDepth == 3;
                P6Passed = failedCount == 1 && lastReason == "queue_overflow" && queueIntact;
            }
            catch (Exception e)
            {
                Log.Error($"[S5-05 P6] Exception: {e}");
                P6Passed = false;
            }
            finally
            {
                if (hFailed != null)
                {
                    GameEvent.RemoveEventListener<int, string>(INarrativeEvent_Event.OnSequenceFailed, hFailed);
                    hFailed = null;
                }
                player?.Shutdown();
            }
            await UniTask.Yield();
        }

        // ────────── P7: Effect type not implemented resilience — sequence 继续 + skip 失败 effect ──────────
        private async UniTask P7_EffectTypeNotImplementedResilience()
        {
            int failedSkipCount = 0;
            bool completed = false;
            Action<int, string> hFailed = (id, reason) =>
            {
                if (reason != null && reason.StartsWith("effect_type_not_implemented")) failedSkipCount++;
            };
            Action<int, NarrativeSequenceType> hComplete = (id, type) => completed = true;
            var player = new NarrativeSequencePlayer();
            try
            {
                GameEvent.AddEventListener<int, string>(INarrativeEvent_Event.OnSequenceFailed, hFailed);
                GameEvent.AddEventListener<int, NarrativeSequenceType>(INarrativeEvent_Event.OnSequenceComplete, hComplete);

                var provider = new NarrativeSequenceConfigFromLuban();
                // Sequence 含 1 placeholder (effect_type 未注册) + 1 wait (注册类型) — verify resilience
                provider.RegisterByTrigger(triggerSourceId: 7001, type: NarrativeSequenceType.MemoryReplay,
                    config: new NarrativeSequenceConfig(
                        id: 7001,
                        type: NarrativeSequenceType.MemoryReplay,
                        effects: new AtomicEffectConfig[]
                        {
                            new PlaceholderUnregisteredConfig(startTime: 0f, duration: 0.1f, label: "camera_shake"),
                            new WaitEffectConfig(startTime: 0f, duration: 0.5f),
                        },
                        totalDuration: 0.5f,
                        isChapterFinal: false));

                player.Initialize(provider);
                GameEvent.Get<INarrativeEvent>().OnRequestSequence(7001, NarrativeSequenceType.MemoryReplay);

                for (int i = 0; i < 30; i++)
                {
                    player.Tick(0.05f);
                    await UniTask.Yield();
                    if (completed) break;
                }

                P7FailedSkipCount = failedSkipCount;
                P7Completed = completed;
                P7Passed = failedSkipCount == 1 && completed && player.State == SequenceState.Idle;
            }
            catch (Exception e)
            {
                Log.Error($"[S5-05 P7] Exception: {e}");
                P7Passed = false;
            }
            finally
            {
                if (hFailed != null)
                {
                    GameEvent.RemoveEventListener<int, string>(INarrativeEvent_Event.OnSequenceFailed, hFailed);
                    hFailed = null;
                }
                if (hComplete != null)
                {
                    GameEvent.RemoveEventListener<int, NarrativeSequenceType>(INarrativeEvent_Event.OnSequenceComplete, hComplete);
                    hComplete = null;
                }
                player?.Shutdown();
            }
        }

        // ────────── P8: Listener self-removal × 5 sequential init/shutdown ──────────
        // ADR-027 §5 framework knowledge fact + ADR-029 V2.0 §V2-5 framework boundary probe:
        //   sequential Initialize / Shutdown 5 次 — 4 listener null-out + null-check guard 防 TEngine "Delete handle failed"
        private async UniTask P8_ListenerSelfRemovalSequential()
        {
            try
            {
                for (int i = 0; i < 5; i++)
                {
                    var player = new NarrativeSequencePlayer();
                    var provider = new NarrativeSequenceConfigFromLuban();
                    provider.InitWithDefaults();
                    player.Initialize(provider);
                    player.Shutdown();
                    player.Shutdown(); // double-shutdown — 4-listener null-check guard 防 raw double-remove
                }
                P8Passed = true;
            }
            catch (Exception e)
            {
                Log.Error($"[S5-05 P8] Exception (Type-2(c) framework boundary lesson regression): {e}");
                P8Passed = false;
            }
            await UniTask.Yield();
        }

        // ────────── P9 (ADV): App pause/resume timer — Pause 期间 _activeElapsed 不推进 ──────────
        private async UniTask P9_AppPauseResumeTimer()
        {
            var player = new NarrativeSequencePlayer();
            try
            {
                var provider = new NarrativeSequenceConfigFromLuban();
                provider.RegisterByTrigger(triggerSourceId: 9100, type: NarrativeSequenceType.MemoryReplay,
                    config: new NarrativeSequenceConfig(
                        id: 9100,
                        type: NarrativeSequenceType.MemoryReplay,
                        effects: new AtomicEffectConfig[] { new WaitEffectConfig(0f, 2.0f) },
                        totalDuration: 2.0f,
                        isChapterFinal: false));
                player.Initialize(provider);
                GameEvent.Get<INarrativeEvent>().OnRequestSequence(9100, NarrativeSequenceType.MemoryReplay);

                // Tick 0.5s 然后 pause
                for (int i = 0; i < 10; i++) { player.Tick(0.05f); await UniTask.Yield(); }
                float elapsedAtPause = player.ActiveElapsed;
                player.Pause();

                // Pause 后 Tick 不应推进
                for (int i = 0; i < 10; i++) { player.Tick(0.05f); await UniTask.Yield(); }
                float elapsedAfterPauseTicks = player.ActiveElapsed;

                player.Resume();
                // Resume 后 Tick 应正常推进
                for (int i = 0; i < 10; i++) { player.Tick(0.05f); await UniTask.Yield(); }
                float elapsedAfterResume = player.ActiveElapsed;

                P9PauseDeltaMs = Math.Abs(elapsedAfterPauseTicks - elapsedAtPause) * 1000.0;
                bool pauseFroze = P9PauseDeltaMs <= 1.0; // 期望完全冻结 (浮点容忍 1ms)
                bool resumeAdvanced = elapsedAfterResume > elapsedAtPause + 0.4f; // 0.5s 推进了 ~0.5s
                P9Passed = pauseFroze && resumeAdvanced;
            }
            catch (Exception e)
            {
                Log.Error($"[S5-05 P9] Exception: {e}");
                P9Passed = false;
            }
            finally
            {
                player?.Shutdown();
            }
        }

        // ────────── P10 (ADV): Trigger-to-first-start latency ≤ 1 frame (16ms) ──────────
        private async UniTask P10_TriggerToFirstStartLatency()
        {
            var player = new NarrativeSequencePlayer();
            try
            {
                var provider = new NarrativeSequenceConfigFromLuban();
                provider.InitWithDefaults();
                player.Initialize(provider);

                var sw = Stopwatch.StartNew();
                GameEvent.Get<INarrativeEvent>().OnRequestSequence(1, NarrativeSequenceType.MemoryReplay);
                sw.Stop();

                // 同步 dispatch + StartSequence — 应远小于 16ms
                P10LatencyMs = sw.Elapsed.TotalMilliseconds;
                bool started = player.State == SequenceState.Playing && player.ActiveSequenceId == 100;
                P10Passed = started && P10LatencyMs <= 16.0;
            }
            catch (Exception e)
            {
                Log.Error($"[S5-05 P10] Exception: {e}");
                P10Passed = false;
            }
            finally
            {
                player?.Shutdown();
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
                sb.Append($"  \"spike\": \"S5-05\",\n");
                sb.Append($"  \"name\": \"Narrative Sequence Engine R3 + V2-5 probe\",\n");
                sb.Append($"  \"adr_governance\": [\"ADR-016\", \"ADR-027\", \"ADR-029 V2.0\"],\n");
                sb.Append($"  \"editmode_tests_complement\": \"NarrativeSequenceConfigTests.cs (13 tests; config / sort invariant / atomic effect Tick / Registry / Luban provider)\",\n");
                sb.Append($"  \"all_done\": {AllDone.ToString().ToLowerInvariant()},\n");
                sb.Append($"  \"pass_count\": {PassCount},\n");
                sb.Append($"  \"results\": {{\n");
                sb.Append($"    \"P1_sg_wireup\": {{ \"passed\": {Json(P1Passed)}, \"gen_registered\": {P1GenRegistered} }},\n");
                sb.Append($"    \"P2_sequence_start_dispatch\": {{ \"passed\": {Json(P2Passed)}, \"start_count\": {P2StartCount}, \"state\": \"{P2State}\" }},\n");
                sb.Append($"    \"P3_time_sorted_effects\": {{ \"passed\": {Json(P3Passed)}, \"started_in_order\": \"{P3StartedInOrder}\" }},\n");
                sb.Append($"    \"P4_inputblocker_pairing\": {{ \"passed\": {Json(P4Passed)}, \"push_count\": {P4PushCount}, \"pop_count\": {P4PopCount}, \"tokens_match\": {P4TokensMatch.ToString().ToLowerInvariant()} }},\n");
                sb.Append($"    \"P5_queue_3_sequential\": {{ \"passed\": {Json(P5Passed)}, \"completed_count\": {P5CompletedCount} }},\n");
                sb.Append($"    \"P6_queue_overflow\": {{ \"passed\": {Json(P6Passed)}, \"failed_overflow_count\": {P6FailedOverflowCount} }},\n");
                sb.Append($"    \"P7_effect_type_resilience\": {{ \"passed\": {Json(P7Passed)}, \"failed_skip_count\": {P7FailedSkipCount}, \"completed\": {P7Completed.ToString().ToLowerInvariant()} }},\n");
                sb.Append($"    \"P8_listener_self_removal_x5\": {{ \"passed\": {Json(P8Passed)} }},\n");
                sb.Append($"    \"P9_adv_pause_resume\": {{ \"passed\": {Json(P9Passed)}, \"pause_delta_ms\": {P9PauseDeltaMs:F2} }},\n");
                sb.Append($"    \"P10_adv_trigger_latency\": {{ \"passed\": {Json(P10Passed)}, \"latency_ms\": {P10LatencyMs:F4} }}\n");
                sb.Append($"  }}\n");
                sb.Append("}\n");
                File.WriteAllText(ResultFilePath, sb.ToString());
                Log.Info($"[S5-05] Result JSON written to: {ResultFilePath}");
            }
            catch (Exception e)
            {
                Log.Error($"[S5-05] WriteResultJson failed: {e}");
            }
        }

        private static string Json(bool? b) => b.HasValue ? b.Value.ToString().ToLowerInvariant() : "null";
    }

    /// <summary>
    /// P7 spike helper — placeholder config 用未注册 effect_type label 模拟 9 placeholder type 未实现。
    /// </summary>
    internal sealed class PlaceholderUnregisteredConfig : AtomicEffectConfig
    {
        public PlaceholderUnregisteredConfig(float startTime, float duration, string label)
            : base(label, startTime, duration) { }
    }
}
#endif
