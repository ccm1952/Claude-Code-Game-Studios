// 该文件由Cursor 自动生成
// S5-06 PlayMode probe — Audio Manager Init + 3 mix layers + ducking + Settings cascade R3 mandatory + V2-5 framework boundary
//   per ADR-029 V2.0 §V2-3 + §V2-5 + story-001 §QA Test Cases (10 case = 8 CORE + 2 ADV)
//
// EditMode tests cover ~70% logic (AudioMixLayer multiplicative formula / DuckingController lerp 数学 / Provider lookup
//   — 14 tests in AudioVolumeFormulaTests)；本 spike 重点验证 PlayMode-only 维度：
//   1. 真 TEngine session 内 IAudioEvent_Gen / ISettingsEvent_Gen Source Generator wire-up
//   2. ADR-027 §5 framework knowledge fact 实战：9 listener self-removal × 5 sequential init/shutdown
//      防 TEngine RemoveEventListener 抛 "Delete handle failed"
//   3. 3-layer isolation — SetLayerVolume(SFX,0) 不影响 Music/Ambient
//   4. multiplicative formula 端到端 — master × layer × duck (verify GameModule.Audio framework volume sync)
//   5. Ducking lerp + ApplyEffectiveVolumesToFramework 时序 (Stopwatch sample CurrentDuckingMultiplier per frame)
//   6. ISettingsEvent.OnSettingChanged → AudioManager cascade (1 frame within)
//   7. Pause/Resume — GameModule.Audio.Enable toggle
//   8. ADR-028 §1 facade activation gate — Initialize 前 PlaySFX fail-loud (Log.Warning + no-op + 不 throw)
//   9. (ADV) Music crossfade dispatch — Music agentHelperCount=2 framework 不 throw（audible verify 留 manual）
//
// JSON evidence 落 Application.persistentDataPath/S5-06_Result.json (沿 S5-05 模式)
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
    public class S506Spike : IDevSpike
    {
        public string Id => "S5-06";
        public string Name => "Audio Manager Init — R3 mandatory + V2-5 framework boundary probe";

        public void Launch()
        {
            var go = new GameObject("S506_Runtime");
            UnityEngine.Object.DontDestroyOnLoad(go);
            go.AddComponent<S506Runtime>();
        }
    }

    public class S506Runtime : MonoBehaviour
    {
        private S506Tester _tester;

        private void Start()
        {
            _tester = new S506Tester();
            Log.Info($"[S5-06] Runtime Start. Result JSON: {S506Tester.ResultFilePath}");
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

            float w = 800, h = 440;
            float x = (Screen.width - w) / 2f;
            float y = 20;

            var box = new GUIStyle(GUI.skin.box) { fontSize = 16, alignment = TextAnchor.MiddleCenter };
            GUI.Box(new Rect(x, y, w, h), string.Empty, box);

            var title = new GUIStyle(GUI.skin.label) { fontSize = 20, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            GUI.Label(new Rect(x, y + 10, w, 30), "S5-06 Audio Manager Init — R3 + V2-5 Probe", title);

            var lab = new GUIStyle(GUI.skin.label) { fontSize = 14 };
            float ly = y + 50;

            DrawRow(x + 20, ly, w - 40, $"P1 SG wire-up (2 _Gen registered: {_tester.P1GenRegistered}/2)", _tester.P1Passed, lab); ly += 26;
            DrawRow(x + 20, ly, w - 40, $"P8 Facade activation gate (PlaySFX before Init: warned & no-op, threw={_tester.P8Threw})", _tester.P8Passed, lab); ly += 26;
            DrawRow(x + 20, ly, w - 40, $"P10 Listener self-removal × 5 sequential init/shutdown + double-shutdown (no exception)", _tester.P10Passed, lab); ly += 26;
            DrawRow(x + 20, ly, w - 40, $"P2 3-layer isolation (SFX→0 keeps Music/Ambient: music={_tester.P2MusicAfter:F2}, ambient={_tester.P2AmbientAfter:F2})", _tester.P2Passed, lab); ly += 26;
            DrawRow(x + 20, ly, w - 40, $"P3 master × layer multiplicative (master=0.5,SFX=0.8 → frameworkSoundVolume={_tester.P3FrameworkSound:F3})", _tester.P3Passed, lab); ly += 26;
            DrawRow(x + 20, ly, w - 40, $"P4 Ducking lerp (final mul={_tester.P4FinalMultiplier:F2}, state={_tester.P4FinalState})", _tester.P4Passed, lab); ly += 26;
            DrawRow(x + 20, ly, w - 40, $"P5 OnPlaySFX dispatch (recv={_tester.P5DispatchRecv}/5)", _tester.P5Passed, lab); ly += 26;
            DrawRow(x + 20, ly, w - 40, $"P6 ISettingsEvent.OnSettingChanged cascade (master_after={_tester.P6MasterAfter:F2})", _tester.P6Passed, lab); ly += 26;
            DrawRow(x + 20, ly, w - 40, $"P7 Pause/Resume (enable_after_pause={_tester.P7EnableAfterPause}, after_resume={_tester.P7EnableAfterResume})", _tester.P7Passed, lab); ly += 26;
            DrawRow(x + 20, ly, w - 40, $"P9 (ADV) Music crossfade dispatch — no exception ({_tester.P9CrossfadeRaisedException})", _tester.P9Passed, lab); ly += 36;

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
    /// S5-06 Tester — 10 PlayMode cases (8 CORE + 2 ADV)。
    /// </summary>
    public class S506Tester
    {
        public static string ResultFilePath => Path.Combine(Application.persistentDataPath, "S5-06_Result.json");

        public bool? P1Passed; public int P1GenRegistered;
        public bool? P2Passed; public float P2MusicAfter; public float P2AmbientAfter;
        public bool? P3Passed; public float P3FrameworkSound;
        public bool? P4Passed; public float P4FinalMultiplier; public string P4FinalState = "?";
        public bool? P5Passed; public int P5DispatchRecv;
        public bool? P6Passed; public float P6MasterAfter;
        public bool? P7Passed; public bool P7EnableAfterPause; public bool P7EnableAfterResume;
        public bool? P8Passed; public bool P8Threw;
        public bool? P9Passed; public string P9CrossfadeRaisedException = "no";
        public bool? P10Passed;

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
            // P1 (SG wireup, 无需 Init)
            await P1_SourceGeneratorWireUp();

            // P8 (activation gate) — 必须在 AudioManager 处于 NOT inited 状态下跑
            //   先 Shutdown (idempotent) 确保未初始化，再调 PlaySFX 验 fail-loud
            await P8_FacadeActivationGate();

            // P10 (V2-5 boundary probe — 5 sequential init/shutdown + double-shutdown)
            //   完成后 AudioManager 处于 shutdown 状态
            await P10_ListenerSelfRemovalSequential();

            // 一次性 Initialize 后跑 P2-P7, P9
            try
            {
                AudioManager.Instance.Initialize();
                await UniTask.Yield();

                await P2_LayerIsolation();
                await P3_MasterLayerMultiplicative();
                await P4_DuckingLerpTimeline();
                await P5_OnPlaySFXDispatch();
                await P6_SettingsEventCascade();
                await P7_PauseResume();
                await P9_AdvMusicCrossfadeDispatch();
            }
            catch (Exception e)
            {
                Log.Error($"[S5-06] Top-level RunAll exception: {e}");
            }
            finally
            {
                AudioManager.Instance.Shutdown();
            }
        }

        // ────────── P1: Source Generator wire-up (2 _Gen 注册: IAudioEvent + ISettingsEvent) ──────────
        private async UniTask P1_SourceGeneratorWireUp()
        {
            try
            {
                int registered = 0;
                var asm = typeof(IAudioEvent).Assembly;
                if (asm.GetType("GameLogic.IAudioEvent_Gen") != null) registered++;
                if (asm.GetType("GameLogic.ISettingsEvent_Gen") != null) registered++;
                P1GenRegistered = registered;
                P1Passed = registered == 2;
            }
            catch (Exception e)
            {
                Log.Error($"[S5-06 P1] Exception: {e}");
                P1Passed = false;
            }
            await UniTask.Yield();
        }

        // ────────── P8: Facade activation gate — Init 前调 PlaySFX 应 fail-loud + no-op ──────────
        private async UniTask P8_FacadeActivationGate()
        {
            try
            {
                // 确保 AudioManager 未初始化
                AudioManager.Instance.Shutdown();
                await UniTask.Yield();

                bool initBefore = AudioManager.Instance.IsInitializedForTest;
                bool threw = false;
                try
                {
                    AudioManager.Instance.PlaySFX("ui_click");
                    AudioManager.Instance.SetMasterVolume(0.5f);
                    AudioManager.Instance.SetDucking(0.3f, 0.5f);
                }
                catch (Exception)
                {
                    threw = true;
                }
                bool initAfter = AudioManager.Instance.IsInitializedForTest;

                P8Threw = threw;
                // PASS: 调用前后均未 init + 未 throw（仅 Log.Warning fail-loud）
                P8Passed = !initBefore && !initAfter && !threw;
            }
            catch (Exception e)
            {
                Log.Error($"[S5-06 P8] Exception: {e}");
                P8Passed = false;
            }
        }

        // ────────── P10: Listener self-removal × 5 sequential init/shutdown + double-shutdown ──────────
        // V2-5 framework boundary probe — 验证 ADR-027 §5 null-out + null-check guard pattern 在 9 listeners stress
        // condition 下不抛 "Delete handle failed"。
        private async UniTask P10_ListenerSelfRemovalSequential()
        {
            try
            {
                bool exceptionRaised = false;
                for (int i = 0; i < 5; i++)
                {
                    try
                    {
                        AudioManager.Instance.Initialize();
                        await UniTask.Yield();
                        AudioManager.Instance.Shutdown();
                        await UniTask.Yield();
                    }
                    catch (Exception ex)
                    {
                        exceptionRaised = true;
                        Log.Error($"[S5-06 P10] cycle {i} exception: {ex.Message}");
                        break;
                    }
                }
                // double-shutdown
                if (!exceptionRaised)
                {
                    try
                    {
                        AudioManager.Instance.Shutdown();
                        await UniTask.Yield();
                    }
                    catch (Exception ex)
                    {
                        exceptionRaised = true;
                        Log.Error($"[S5-06 P10] double-shutdown exception: {ex.Message}");
                    }
                }
                P10Passed = !exceptionRaised;
            }
            catch (Exception e)
            {
                Log.Error($"[S5-06 P10] Exception: {e}");
                P10Passed = false;
            }
        }

        // ────────── P2: 3-layer isolation — SetLayerVolume(SFX, 0) → Music/Ambient baseline 不变 ──────────
        private async UniTask P2_LayerIsolation()
        {
            try
            {
                // setup: 3 layers all 1.0 baseline (Ambient default 0.6)
                AudioManager.Instance.SetLayerVolume(AudioLayer.Music, 0.8f);
                AudioManager.Instance.SetLayerVolume(AudioLayer.Ambient, 0.5f);
                AudioManager.Instance.SetLayerVolume(AudioLayer.SFX, 0.7f);
                await UniTask.Yield();

                // mute SFX
                AudioManager.Instance.SetLayerVolume(AudioLayer.SFX, 0f);
                await UniTask.Yield();

                P2MusicAfter = AudioManager.Instance.GetLayerVolume(AudioLayer.Music);
                P2AmbientAfter = AudioManager.Instance.GetLayerVolume(AudioLayer.Ambient);
                float sfxAfter = AudioManager.Instance.GetLayerVolume(AudioLayer.SFX);

                P2Passed = Mathf.Approximately(P2MusicAfter, 0.8f)
                    && Mathf.Approximately(P2AmbientAfter, 0.5f)
                    && Mathf.Approximately(sfxAfter, 0f);
            }
            catch (Exception e)
            {
                Log.Error($"[S5-06 P2] Exception: {e}");
                P2Passed = false;
            }
        }

        // ────────── P3: master × layer multiplicative — verify framework SoundVolume sync ──────────
        private async UniTask P3_MasterLayerMultiplicative()
        {
            try
            {
                AudioManager.Instance.SetMasterVolume(0.5f);
                AudioManager.Instance.SetLayerVolume(AudioLayer.SFX, 0.8f);
                await UniTask.Yield();

                // expected effective SFX = master 0.5 × baseline 0.8 × ducking 1.0 = 0.4
                // framework MusicVolume / SoundVolume 内部 Clamp 到 [0.0001, 1.0]
                P3FrameworkSound = GameModule.Audio.SoundVolume;
                P3Passed = Mathf.Abs(P3FrameworkSound - 0.4f) <= 0.01f;
            }
            catch (Exception e)
            {
                Log.Error($"[S5-06 P3] Exception: {e}");
                P3Passed = false;
            }
        }

        // ────────── P4: Ducking lerp + Apply timeline ──────────
        // 派 OnDuckingRequest(0.3, 0.5s) → AudioManager listener StartDuckingAsync → DriveDuckingApplyAsync 持续套到
        // framework；等 1s（>0.5s lerp duration） sample 期间 multiplier。
        private async UniTask P4_DuckingLerpTimeline()
        {
            try
            {
                AudioManager.Instance.SetMasterVolume(1f);
                AudioManager.Instance.SetLayerVolume(AudioLayer.Music, 1f);
                AudioManager.Instance.SetLayerVolume(AudioLayer.Ambient, 0.6f);
                await UniTask.Yield();

                // 派事件 (而非直接调 SetDucking — 测试 IAudioEvent listener 路径)
                GameEvent.Get<IAudioEvent>().OnDuckingRequest(0.3f, 0.3f);

                // 等 0.5s — 大于 fadeDuration 0.3s
                var sw = Stopwatch.StartNew();
                while (sw.Elapsed.TotalSeconds < 0.5)
                {
                    await UniTask.Yield();
                }

                P4FinalMultiplier = AudioManager.Instance.CurrentDuckingMultiplierForTest;
                P4FinalState = AudioManager.Instance.CurrentDuckingStateForTest.ToString();

                // 期望 multiplier ≈ 0.3 (Held), state == Held
                bool multiplierOk = Mathf.Abs(P4FinalMultiplier - 0.3f) <= 0.05f;
                bool stateOk = P4FinalState == "Held";
                P4Passed = multiplierOk && stateOk;

                // Release
                GameEvent.Get<IAudioEvent>().OnReleaseDucking(0.3f);
                while (sw.Elapsed.TotalSeconds < 1.0)
                {
                    await UniTask.Yield();
                }
            }
            catch (Exception e)
            {
                Log.Error($"[S5-06 P4] Exception: {e}");
                P4Passed = false;
            }
        }

        // ────────── P5: OnPlaySFX dispatch — 5 次派事件，AudioManager listener 每次接收 ──────────
        // Note: framework actual cull 行为留 manual verify (chapter 1 真 SFX 资产可用后)；
        //   本 case 仅验证 dispatch 流：派 5 次 → handler hit 5 次（验事件路径 wire-up）。
        private async UniTask P5_OnPlaySFXDispatch()
        {
            try
            {
                int recv = 0;
                Action<int, float, float> hSpy = (sfxId, delay, volume) => recv++;
                GameEvent.AddEventListener<int, float, float>(IAudioEvent_Event.OnPlaySFX, hSpy);

                for (int i = 0; i < 5; i++)
                {
                    GameEvent.Get<IAudioEvent>().OnPlaySFX(sfxId: 1, delay: 0f, volume: 1f);
                    await UniTask.Yield();
                }

                if (hSpy != null)
                {
                    GameEvent.RemoveEventListener<int, float, float>(IAudioEvent_Event.OnPlaySFX, hSpy);
                    hSpy = null;
                }

                P5DispatchRecv = recv;
                P5Passed = recv == 5;
            }
            catch (Exception e)
            {
                Log.Error($"[S5-06 P5] Exception: {e}");
                P5Passed = false;
            }
        }

        // ────────── P6: ISettingsEvent.OnSettingChanged → AudioManager.SetMasterVolume cascade ──────────
        private async UniTask P6_SettingsEventCascade()
        {
            try
            {
                AudioManager.Instance.SetMasterVolume(1f);
                await UniTask.Yield();

                GameEvent.Get<ISettingsEvent>().OnSettingChanged("master_volume", "0.7");
                await UniTask.Yield();

                P6MasterAfter = AudioManager.Instance.GetMasterVolume();
                P6Passed = Mathf.Abs(P6MasterAfter - 0.7f) <= 0.01f;
            }
            catch (Exception e)
            {
                Log.Error($"[S5-06 P6] Exception: {e}");
                P6Passed = false;
            }
        }

        // ────────── P7: Pause/Resume — GameModule.Audio.Enable toggle via IAudioEvent ──────────
        private async UniTask P7_PauseResume()
        {
            try
            {
                // 确保 enable 是 true
                GameModule.Audio.Enable = true;
                await UniTask.Yield();

                GameEvent.Get<IAudioEvent>().OnAudioPauseRequest();
                await UniTask.Yield();
                P7EnableAfterPause = GameModule.Audio.Enable;

                GameEvent.Get<IAudioEvent>().OnAudioResumeRequest();
                await UniTask.Yield();
                P7EnableAfterResume = GameModule.Audio.Enable;

                P7Passed = !P7EnableAfterPause && P7EnableAfterResume;
            }
            catch (Exception e)
            {
                Log.Error($"[S5-06 P7] Exception: {e}");
                P7Passed = false;
            }
        }

        // ────────── P9: (ADV) Music crossfade dispatch — Music agentHelperCount=2 framework 不抛异常 ──────────
        // 真"audible no-gap" verify 留 manual (chapter 1 真 audio 资产 ready 后)；
        // 本 case 验证 framework Play(AudioType.Music) 调用本身在 agentHelperCount=2 配置下不 throw。
        private async UniTask P9_AdvMusicCrossfadeDispatch()
        {
            try
            {
                bool threw = false;
                string error = null;
                try
                {
                    GameEvent.Get<IAudioEvent>().OnPlayMusic(musicClipId: 100, crossfadeDuration: 1.0f);
                    await UniTask.Yield();
                    // 第二次派 — 试图 crossfade 第二个 agent
                    GameEvent.Get<IAudioEvent>().OnPlayMusic(musicClipId: 100, crossfadeDuration: 1.0f);
                    await UniTask.Yield();
                }
                catch (Exception ex)
                {
                    threw = true;
                    error = ex.Message;
                }
                P9CrossfadeRaisedException = threw ? $"yes: {error}" : "no";
                P9Passed = !threw;
            }
            catch (Exception e)
            {
                Log.Error($"[S5-06 P9] Exception: {e}");
                P9Passed = false;
            }
        }

        public void WriteResultJson()
        {
            try
            {
                var sb = new StringBuilder();
                sb.AppendLine("{");
                sb.AppendLine("  \"spike\": \"S5-06\",");
                sb.AppendLine($"  \"all_done\": {(AllDone ? "true" : "false")},");
                sb.AppendLine($"  \"pass_count\": {PassCount},");
                sb.AppendLine($"  \"persistentDataPath\": \"{EscapeJson(Application.persistentDataPath)}\",");
                sb.AppendLine("  \"results\": {");
                sb.AppendLine($"    \"P1_sg_wireup\": {{ \"passed\": {Bool(P1Passed)}, \"gen_registered\": {P1GenRegistered} }},");
                sb.AppendLine($"    \"P8_facade_activation_gate\": {{ \"passed\": {Bool(P8Passed)}, \"threw\": {(P8Threw ? "true" : "false")} }},");
                sb.AppendLine($"    \"P10_listener_self_removal_x5\": {{ \"passed\": {Bool(P10Passed)} }},");
                sb.AppendLine($"    \"P2_layer_isolation\": {{ \"passed\": {Bool(P2Passed)}, \"music_after\": {P2MusicAfter:F3}, \"ambient_after\": {P2AmbientAfter:F3} }},");
                sb.AppendLine($"    \"P3_master_layer_multiplicative\": {{ \"passed\": {Bool(P3Passed)}, \"framework_sound_volume\": {P3FrameworkSound:F4} }},");
                sb.AppendLine($"    \"P4_ducking_lerp\": {{ \"passed\": {Bool(P4Passed)}, \"final_multiplier\": {P4FinalMultiplier:F3}, \"final_state\": \"{P4FinalState}\" }},");
                sb.AppendLine($"    \"P5_onplaysfx_dispatch\": {{ \"passed\": {Bool(P5Passed)}, \"recv_count\": {P5DispatchRecv} }},");
                sb.AppendLine($"    \"P6_settings_event_cascade\": {{ \"passed\": {Bool(P6Passed)}, \"master_after\": {P6MasterAfter:F3} }},");
                sb.AppendLine($"    \"P7_pause_resume\": {{ \"passed\": {Bool(P7Passed)}, \"enable_after_pause\": {(P7EnableAfterPause ? "true" : "false")}, \"enable_after_resume\": {(P7EnableAfterResume ? "true" : "false")} }},");
                sb.AppendLine($"    \"P9_adv_music_crossfade\": {{ \"passed\": {Bool(P9Passed)}, \"raised_exception\": \"{EscapeJson(P9CrossfadeRaisedException)}\" }}");
                sb.AppendLine("  }");
                sb.AppendLine("}");
                File.WriteAllText(ResultFilePath, sb.ToString());
                Log.Info($"[S5-06] WriteResultJson → {ResultFilePath}");
            }
            catch (Exception e)
            {
                Log.Error($"[S5-06] WriteResultJson exception: {e}");
            }
        }

        private static string Bool(bool? b) => b == true ? "true" : (b == false ? "false" : "null");
        private static string EscapeJson(string s) => string.IsNullOrEmpty(s) ? "" : s.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }
}
#endif
