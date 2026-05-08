// 该文件由Cursor 自动生成
// S5-05 Production Code: Narrative Sequence Player 主体 impl
//   per ADR-016 §A-§E + ADR-027 §1/§5 + ADR-029 V2.0 §V2-3 R3 mandatory + §V2-5 framework boundary probe.
//
// Story reference: production/epics/narrative-event/story-001-sequence-engine.md
//   15 ACs A-E 全 cover；deficiency-flagged Step 1 IEvent stubs (INarrativeEvent / IInputBlockerEvent /
//   IAudioEvent) + NarrativeSequenceConfigFromLuban hardcoded MVP defaults 已落地。
//
// Lifecycle pattern (ADR-013 v3 + ADR-027 §5 framework knowledge fact):
//   Initialize(provider) → Tick(deltaTime) loop → Shutdown() (handler null-out + null-check guard, 4 listeners)
//
// Token-based locking deviation note:
//   ADR-016 §A line 367-368 描述 IPuzzleLockEvent.OnPuzzleLockAll(string token) + OnPuzzleUnlock(string token)
//   是 token-based push/pop；S5-03 创建的 IPuzzleLockEvent contract 是 parameter-less terminal-lock
//   (per ADR-014 §A puzzle complete final lock 设计)。S5-05 NarrativeSequencePlayer 采取 pragmatic
//   single-layer 锁策略：仅通过 IInputBlockerEvent.OnPushBlocker(token) / OnPopBlocker(token) 锁 raw input;
//   raw input blocked = puzzle drag/rotate/snap 自然 freeze, 不需双 layer。
//   ADR-016 / ADR-014 alignment 留 future cross-ADR review (Sprint 5 retro action item)。

using System.Collections.Generic;
using TEngine;

namespace GameLogic
{
    /// <summary>
    /// Narrative sequence player 契约（per ADR-016 §B；测试 / consumer 通过此接口交互）。
    /// </summary>
    public interface INarrativeSequencePlayer
    {
        /// <summary>当前 player 状态。</summary>
        SequenceState State { get; }

        /// <summary>当前活动 sequence ID（无活动时 -1）。</summary>
        int ActiveSequenceId { get; }

        /// <summary>当前队列深度（≤ 3 per AC-10）。</summary>
        int QueueDepth { get; }

        /// <summary>当前 active sequence 已运行时间（秒；无活动时 0）。</summary>
        float ActiveElapsed { get; }

        /// <summary>当前 active sequence 已 Start 的 effect 列表（测试 inspect 用）。</summary>
        IReadOnlyList<AtomicEffect> ActiveEffects { get; }

        /// <summary>Initialize player：注入 config provider + 注册 4 listeners。</summary>
        void Initialize(INarrativeSequenceConfigProvider provider);

        /// <summary>每帧驱动；推进 active sequence + 触发 due effects。</summary>
        void Tick(float deltaTime);

        /// <summary>App pause: timer 暂停 + state preservation（per AC-14）。</summary>
        void Pause();

        /// <summary>App resume: timer 恢复（从断点继续；per AC-14）。</summary>
        void Resume();

        /// <summary>Shutdown: 4 listener null-out + null-check guard removal + 清空 queue。</summary>
        void Shutdown();
    }

    /// <summary>
    /// Narrative sequence player 主体 impl（per ADR-016 §B；S5-05 dev-story 创建）。
    /// </summary>
    /// <remarks>
    /// <para>FSM 原则：</para>
    /// <list type="bullet">
    ///   <item>Idle → Playing：HandleRequestSequence resolve config 成功后 StartSequence。</item>
    ///   <item>Playing → Idle：_elapsed >= TotalDuration 时 CompleteSequence，dequeue 下一项（若有）。</item>
    ///   <item>Playing → Paused：Pause() 调用；timer 不推进。</item>
    ///   <item>Paused → Playing：Resume() 调用。</item>
    /// </list>
    /// <para>事件协议（per ADR-027 §1 + §5 framework knowledge fact）：</para>
    /// <list type="bullet">
    ///   <item>Subscribe 4 listeners via per-event AddEventListener；handler null-out + null-check guard cleanup。</item>
    ///   <item>Sender via GameEvent.Get&lt;T&gt;().OnXxx(...) — INarrativeEvent / IInputBlockerEvent / IAudioEvent。</item>
    /// </list>
    /// <para>Cascade depth budget: 3（OnRequestSequence → StartSequence → OnSequenceStart + OnPushBlocker
    /// → atomic effects fire；合计 ≤ 3 per ADR-027 §2 base guardrail）。</para>
    /// </remarks>
    public sealed class NarrativeSequencePlayer : INarrativeSequencePlayer
    {
        private const int QueueCapacity = 3;
        private const string TokenPrefix = "narrative_seq_";

        private INarrativeSequenceConfigProvider _provider;
        private AtomicEffectRegistry _effectRegistry;

        private SequenceState _state;
        private NarrativeSequenceConfig _activeSequence;
        private float _activeElapsed;
        private List<AtomicEffect> _activeEffects = new List<AtomicEffect>();
        private Queue<NarrativeSequenceConfig> _queue = new Queue<NarrativeSequenceConfig>(QueueCapacity);

        // ADR-027 §5 framework knowledge fact — handler delegate cache 防 raw double-remove
        private System.Action<int, NarrativeSequenceType> _onRequestSequence;
        private System.Action<int, float> _onPerfectMatch;
        private System.Action<int, float> _onAbsenceAccepted;
        private System.Action<int> _onChapterComplete;

        private bool _isInitialized;

        // ──────────── Public API (INarrativeSequencePlayer) ────────────

        public SequenceState State => _state;
        public int ActiveSequenceId => _activeSequence?.Id ?? -1;
        public int QueueDepth => _queue.Count;
        public float ActiveElapsed => _activeElapsed;
        public IReadOnlyList<AtomicEffect> ActiveEffects => _activeEffects;

        public void Initialize(INarrativeSequenceConfigProvider provider)
        {
            if (_isInitialized)
            {
                Log.Warning("[NarrativeSequencePlayer] Already initialized; ignoring re-init request.");
                return;
            }
            _provider = provider ?? throw new System.ArgumentNullException(nameof(provider));
            _effectRegistry = new AtomicEffectRegistry();

            _state = SequenceState.Idle;
            _activeSequence = null;
            _activeElapsed = 0f;
            _activeEffects.Clear();
            _queue.Clear();

            // Subscribe 4 listeners (ADR-027 §1 per-event mode + §5 null-out/null-check pattern)
            _onRequestSequence = OnRequestSequenceHandler;
            _onPerfectMatch = OnPerfectMatchHandler;
            _onAbsenceAccepted = OnAbsenceAcceptedHandler;
            _onChapterComplete = OnChapterCompleteHandler;

            GameEvent.AddEventListener<int, NarrativeSequenceType>(INarrativeEvent_Event.OnRequestSequence, _onRequestSequence);
            GameEvent.AddEventListener<int, float>(IShadowPuzzleEvent_Event.OnPerfectMatch, _onPerfectMatch);
            GameEvent.AddEventListener<int, float>(IShadowPuzzleEvent_Event.OnAbsenceAccepted, _onAbsenceAccepted);
            GameEvent.AddEventListener<int>(IChapterStateEvent_Event.OnChapterComplete, _onChapterComplete);

            _isInitialized = true;
        }

        public void Tick(float deltaTime)
        {
            if (!_isInitialized) return;
            if (_state != SequenceState.Playing) return; // Paused / Idle 时不推进
            if (_activeSequence == null) return;
            if (deltaTime < 0f) return; // 防御负值

            _activeElapsed += deltaTime;

            // Phase 1: 启动新到 startTime 的 effects（time-sorted invariant — Effects 已按 StartTime 升序）
            for (int i = 0; i < _activeEffects.Count; i++)
            {
                var eff = _activeEffects[i];
                if (eff != null && !eff.IsStarted && eff.Config.StartTime <= _activeElapsed)
                {
                    eff.Start();
                }
            }

            // Phase 2: Tick 已 Start 且未完成的 effects
            for (int i = 0; i < _activeEffects.Count; i++)
            {
                var eff = _activeEffects[i];
                if (eff != null && eff.IsStarted && !eff.IsComplete)
                {
                    eff.Tick(deltaTime);
                }
            }

            // Phase 3: 检查 sequence 完成（_elapsed >= TotalDuration；不需所有 effects 都 IsComplete —
            // fire-and-forget effects 立即 complete，time-driven effects duration 在 TotalDuration 内 by design）
            if (_activeElapsed >= _activeSequence.TotalDuration)
            {
                CompleteSequence();
            }
        }

        public void Pause()
        {
            if (!_isInitialized) return;
            if (_state == SequenceState.Playing)
            {
                _state = SequenceState.Paused;
            }
        }

        public void Resume()
        {
            if (!_isInitialized) return;
            if (_state == SequenceState.Paused)
            {
                _state = SequenceState.Playing;
            }
        }

        public void Shutdown()
        {
            if (!_isInitialized) return;

            // ADR-027 §5 framework knowledge fact: null-out + null-check guard 防 raw double-remove (4 listeners)
            if (_onRequestSequence != null)
            {
                GameEvent.RemoveEventListener<int, NarrativeSequenceType>(INarrativeEvent_Event.OnRequestSequence, _onRequestSequence);
                _onRequestSequence = null;
            }
            if (_onPerfectMatch != null)
            {
                GameEvent.RemoveEventListener<int, float>(IShadowPuzzleEvent_Event.OnPerfectMatch, _onPerfectMatch);
                _onPerfectMatch = null;
            }
            if (_onAbsenceAccepted != null)
            {
                GameEvent.RemoveEventListener<int, float>(IShadowPuzzleEvent_Event.OnAbsenceAccepted, _onAbsenceAccepted);
                _onAbsenceAccepted = null;
            }
            if (_onChapterComplete != null)
            {
                GameEvent.RemoveEventListener<int>(IChapterStateEvent_Event.OnChapterComplete, _onChapterComplete);
                _onChapterComplete = null;
            }

            // 清理 active sequence + queue (无 unlock fire — Shutdown 是非正常 lifecycle 终止；
            // listener side 应通过 OnSequenceFailed 或自行处理 orphan)
            _activeSequence = null;
            _activeEffects.Clear();
            _queue.Clear();
            _activeElapsed = 0f;
            _state = SequenceState.Idle;

            _isInitialized = false;
        }

        // ──────────── Listener handlers ────────────

        private void OnRequestSequenceHandler(int triggerSourceId, NarrativeSequenceType sequenceType)
        {
            HandleRequestSequence(triggerSourceId, sequenceType);
        }

        private void OnPerfectMatchHandler(int puzzleId, float finalMatchScore)
        {
            HandleRequestSequence(puzzleId, NarrativeSequenceType.MemoryReplay);
        }

        private void OnAbsenceAcceptedHandler(int puzzleId, float finalMatchScore)
        {
            HandleRequestSequence(puzzleId, NarrativeSequenceType.AbsencePuzzle);
        }

        private void OnChapterCompleteHandler(int chapterId)
        {
            HandleRequestSequence(chapterId, NarrativeSequenceType.ChapterTransition);
        }

        // ──────────── Sequence lifecycle ────────────

        private void HandleRequestSequence(int triggerSourceId, NarrativeSequenceType sequenceType)
        {
            if (!_isInitialized) return;

            var config = _provider.Resolve(triggerSourceId, sequenceType);
            if (config == null)
            {
                Log.Warning($"[NarrativeSequencePlayer] No config found for trigger={triggerSourceId} type={sequenceType}; firing OnSequenceFailed.");
                GameEvent.Get<INarrativeEvent>().OnSequenceFailed(-1, "config_not_found");
                return;
            }

            if (_state == SequenceState.Idle)
            {
                StartSequence(config);
            }
            else if (_queue.Count < QueueCapacity)
            {
                _queue.Enqueue(config);
            }
            else
            {
                // AC-11 queue overflow — drop 4th + fire OnSequenceFailed("queue_overflow")
                Log.Warning($"[NarrativeSequencePlayer] Queue full (={QueueCapacity}); dropping seq={config.Id} type={config.Type}.");
                GameEvent.Get<INarrativeEvent>().OnSequenceFailed(config.Id, "queue_overflow");
            }
        }

        private void StartSequence(NarrativeSequenceConfig config)
        {
            _activeSequence = config;
            _activeElapsed = 0f;
            _state = SequenceState.Playing;
            _activeEffects.Clear();

            // 创建 effect instances（time-sorted；未注册 effect type 派发 OnSequenceFailed + skip）
            for (int i = 0; i < config.Effects.Count; i++)
            {
                var effCfg = config.Effects[i];
                if (_effectRegistry.TryCreate(effCfg, out var eff))
                {
                    _activeEffects.Add(eff);
                }
                else
                {
                    var label = effCfg?.EffectType ?? "<null>";
                    Log.Warning($"[NarrativeSequencePlayer] Effect type '{label}' not implemented; skipping in seq={config.Id}.");
                    GameEvent.Get<INarrativeEvent>().OnSequenceFailed(config.Id, $"effect_type_not_implemented:{label}");
                    // 注意：sequence 继续 — AC-12 resilience（fire-and-skip + 不 abort full sequence）
                }
            }

            var token = TokenPrefix + config.Id;

            // S5-05 deviation note: 仅 InputBlocker 单层锁；不调 IPuzzleLockEvent.OnPuzzleLockAll
            // (ADR-014 contract 是 parameter-less terminal-lock；不适用 sequence-scoped use case)
            GameEvent.Get<IInputBlockerEvent>().OnPushBlocker(token);
            GameEvent.Get<INarrativeEvent>().OnSequenceStart(config.Id, config.Type);
        }

        private void CompleteSequence()
        {
            if (_activeSequence == null) return;

            var config = _activeSequence;
            var token = TokenPrefix + config.Id;

            GameEvent.Get<IInputBlockerEvent>().OnPopBlocker(token);
            GameEvent.Get<INarrativeEvent>().OnSequenceComplete(config.Id, config.Type);

            _activeSequence = null;
            _activeEffects.Clear();
            _activeElapsed = 0f;
            _state = SequenceState.Idle;

            // Dequeue 下一项 if any (AC-10 sequential playback)
            if (_queue.Count > 0)
            {
                StartSequence(_queue.Dequeue());
            }
        }
    }
}
