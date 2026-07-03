// 该文件由Cursor 自动生成
// S5-03 Production Code: Puzzle State Machine 主体 impl
//   per ADR-014 §A-§E + ADR-027 §1/§5 + ADR-029 V2.0 §V2-3 R3 mandatory + §V2-5 framework boundary probe.
//
// Story reference: production/epics/shadow-puzzle/story-001-puzzle-state-machine.md
//   16 ACs A/B/C/D 全 cover；deficiency-flagged Step 1 IShadowMatchEvent stub creation 已落地。
//
// Lifecycle pattern (ADR-013 v3 + ADR-027 §5 framework knowledge fact):
//   Initialize(puzzleId, config) → Tick(deltaTime) loop → Shutdown() (handler null-out + null-check guard)
//
// R3 PlayMode probe coverage: per story-001 §QA Test Cases 10 case (8 CORE + 2 ADV) — see S5-03 spike.

using System;
using TEngine;

namespace GameLogic
{
    /// <summary>
    /// Puzzle 状态机契约（per ADR-014 §B；测试 / consumer 通过此接口交互）。
    /// </summary>
    public interface IPuzzleStateMachine
    {
        /// <summary>当前 puzzle 状态。</summary>
        PuzzleState CurrentState { get; }

        /// <summary>当前 matchScore（PerfectMatch / AbsenceAccepted 后 frozen）。</summary>
        float MatchScore { get; }

        /// <summary>是否在 tutorial grace period（剩余时间 &gt; 0）。</summary>
        bool IsInGracePeriod { get; }

        /// <summary>是否为 Absence puzzle (per config)。</summary>
        bool IsAbsencePuzzle { get; }

        /// <summary>Initialize 状态机；订阅 IShadowMatchEvent.OnMatchScoreUpdated。</summary>
        void Initialize(int puzzleId, PuzzleStateConfig config);

        /// <summary>每帧驱动；处理 grace period decrement + absence idle timer。</summary>
        void Tick(float deltaTime);

        /// <summary>Player interaction 通知；Idle → Active 提升 + reset absence idle timer。</summary>
        void OnPlayerInteraction();

        /// <summary>Tutorial 完成通知；启动 tutorialGracePeriod 计时（block PerfectMatch / AbsenceAccepted）。</summary>
        void OnTutorialCompleted();

        /// <summary>Chapter unlock 时由 ChapterStateManager 调用；Locked → Idle 提升。</summary>
        void OnChapterUnlocked();

        /// <summary>Shutdown 状态机；handler null-out + null-check guard removal（ADR-027 §5）。</summary>
        void Shutdown();
    }

    /// <summary>
    /// Puzzle 状态机主体（per ADR-014 §B Production code paths；S5-03 dev-story 创建）。
    /// </summary>
    /// <remarks>
    /// <para>FSM 原则（per ADR-014 §A）：</para>
    /// <list type="bullet">
    ///   <item>7-state enum + 10 transitions 全部可达（AC-1/AC-2）。</item>
    ///   <item>PerfectMatch / AbsenceAccepted 不可逆（matchScore frozen on entry；AC-3/AC-4）。</item>
    ///   <item>NearMatch hysteresis ±0.05 防 flicker（每秒切换 ≤ 1 次；AC-5/AC-6）。</item>
    ///   <item>Tutorial grace period 3s 内 block 终端 transitions（AC-7）。</item>
    ///   <item>Absence idle timer 5s（仅 absence puzzles）；任何 interaction reset（AC-8/AC-9）。</item>
    /// </list>
    /// <para>事件协议（per ADR-027 §1 + §5 framework knowledge fact）：</para>
    /// <list type="bullet">
    ///   <item>Subscribe via per-event <c>AddEventListener&lt;int, float&gt;(IShadowMatchEvent_Event.OnMatchScoreUpdated, _onMatchScoreUpdated)</c>。</item>
    ///   <item>Cleanup pattern: handler null-out + null-check guard（防 TEngine RemoveEventListener 抛 "Delete handle failed"）。</item>
    ///   <item>Sender via <c>GameEvent.Get&lt;IShadowPuzzleEvent&gt;().OnXxx(...)</c> 5 senders。</item>
    /// </list>
    /// <para>Performance budget (TR-puzzle-005)：FSM Tick ≤ 0.05ms p99（含 state check + timer decrement）。</para>
    /// </remarks>
    public sealed class PuzzleStateMachine : IPuzzleStateMachine
    {
        private PuzzleState _state;
        private float _matchScore;
        private bool _isInGracePeriod;
        private float _gracePeriodTimer;
        private float _absenceIdleTimer;
        private bool _isFrozen; // PerfectMatch / AbsenceAccepted 后 true，blocking matchScore 更新
        private bool _puzzleCompleteSent; // 防 double-send OnPuzzleComplete + OnPuzzleLockAll

        private PuzzleStateConfig _config;
        private int _puzzleId;
        private bool _isInitialized;

        // ADR-027 §5 framework knowledge fact — handler 用 named delegate cache 防 raw double-remove
        private Action<int, float> _onMatchScoreUpdated;

        // ──────────── Public API (IPuzzleStateMachine) ────────────

        public PuzzleState CurrentState => _state;
        public float MatchScore => _matchScore;
        public bool IsInGracePeriod => _isInGracePeriod && _gracePeriodTimer > 0f;
        public bool IsAbsencePuzzle => _config != null && _config.IsAbsencePuzzle;

        public void Initialize(int puzzleId, PuzzleStateConfig config)
        {
            if (_isInitialized)
            {
                Log.Warning($"[PuzzleStateMachine] Already initialized for puzzle {_puzzleId}; ignoring re-init request for {puzzleId}.");
                return;
            }
            if (config == null) throw new ArgumentNullException(nameof(config));

            _puzzleId = puzzleId;
            _config = config;
            _state = PuzzleState.Locked;
            _matchScore = 0f;
            _absenceIdleTimer = 0f;
            _gracePeriodTimer = 0f;
            _isInGracePeriod = false;
            _isFrozen = false;
            _puzzleCompleteSent = false;

            // Subscribe to IShadowMatchEvent.OnMatchScoreUpdated (ADR-027 §1 per-event mode)
            _onMatchScoreUpdated = OnMatchScoreUpdatedHandler;
            GameEvent.AddEventListener<int, float>(IShadowMatchEvent_Event.OnMatchScoreUpdated, _onMatchScoreUpdated);

            _isInitialized = true;
        }

        public void Tick(float deltaTime)
        {
            if (!_isInitialized) return;
            if (deltaTime < 0f) return; // 防御负值

            // Grace period decrement (AC-7)
            if (_isInGracePeriod)
            {
                _gracePeriodTimer -= deltaTime;
                if (_gracePeriodTimer <= 0f)
                {
                    _gracePeriodTimer = 0f;
                    _isInGracePeriod = false;
                }
            }

            // Absence idle timer (AC-8/AC-9; 仅 absence puzzle 在 Active/NearMatch 状态)
            if (_config.IsAbsencePuzzle &&
                (_state == PuzzleState.Active || _state == PuzzleState.NearMatch) &&
                !_isFrozen)
            {
                if (_matchScore >= _config.MaxCompletionScore)
                {
                    _absenceIdleTimer += deltaTime;
                    if (_absenceIdleTimer >= _config.AbsenceAcceptDelay && !_isInGracePeriod)
                    {
                        EnterAbsenceAccepted();
                    }
                }
                else
                {
                    _absenceIdleTimer = 0f; // matchScore 跌出 → reset
                }
            }
        }

        public void OnPlayerInteraction()
        {
            if (!_isInitialized || _isFrozen) return;

            // AC-9: 任何 interaction reset absence idle timer
            _absenceIdleTimer = 0f;

            // Idle → Active (per ADR-014 §B transition #2)
            if (_state == PuzzleState.Idle)
            {
                TransitionTo(PuzzleState.Active);
            }
        }

        public void OnTutorialCompleted()
        {
            if (!_isInitialized) return;
            if (_config.TutorialGracePeriod <= 0f) return; // grace period 配置为 0 视为不启用

            _isInGracePeriod = true;
            _gracePeriodTimer = _config.TutorialGracePeriod;
        }

        public void OnChapterUnlocked()
        {
            if (!_isInitialized || _isFrozen) return;

            if (_state == PuzzleState.Locked)
            {
                TransitionTo(PuzzleState.Idle);
            }
        }

        public void Shutdown()
        {
            if (!_isInitialized) return;

            // ADR-027 §5 framework knowledge fact: null-out + null-check guard 防 raw double-remove
            if (_onMatchScoreUpdated != null)
            {
                GameEvent.RemoveEventListener<int, float>(IShadowMatchEvent_Event.OnMatchScoreUpdated, _onMatchScoreUpdated);
                _onMatchScoreUpdated = null;
            }

            _isInitialized = false;
        }

        // ──────────── Private — handler & evaluation ────────────

        private void OnMatchScoreUpdatedHandler(int puzzleId, float newScore)
        {
            if (puzzleId != _puzzleId) return; // 多 puzzle 共存时按 ID 过滤
            if (_isFrozen) return; // PerfectMatch / AbsenceAccepted 后冻结（AC-3/AC-4）
            if (newScore < 0f || newScore > 1f) return; // 防御越界

            _matchScore = newScore;
            EvaluateTransitions();
        }

        private void EvaluateTransitions()
        {
            switch (_state)
            {
                case PuzzleState.Active:
                    // Active → NearMatch (AC-5)
                    if (_matchScore >= _config.NearMatchThreshold)
                    {
                        TransitionTo(PuzzleState.NearMatch);
                        GameEvent.Get<IShadowPuzzleEvent>().OnNearMatchEnter(_puzzleId);
                    }
                    // Standard PerfectMatch path (非 absence puzzle, 非 grace period)
                    if (TryEnterPerfectMatch()) return;
                    break;

                case PuzzleState.NearMatch:
                    // NearMatch → Active (hysteresis: matchScore < threshold - 0.05; AC-6)
                    if (_matchScore < _config.NearMatchThreshold - _config.HysteresisOffset)
                    {
                        TransitionTo(PuzzleState.Active);
                        GameEvent.Get<IShadowPuzzleEvent>().OnNearMatchExit(_puzzleId);
                        return;
                    }
                    if (TryEnterPerfectMatch()) return;
                    break;

                // Idle / Locked: 由 OnPlayerInteraction / OnChapterUnlocked 驱动；不在 score-driven evaluation 范围
                // PerfectMatch / AbsenceAccepted / Complete: frozen，不进入此 switch（_isFrozen guard 在 handler 内）
                default:
                    break;
            }
        }

        private bool TryEnterPerfectMatch()
        {
            if (_config.IsAbsencePuzzle) return false; // absence puzzle 走 idle timer path
            if (_isInGracePeriod) return false; // AC-7: grace period block 终端 transitions
            if (_matchScore < _config.PerfectMatchThreshold) return false;

            EnterPerfectMatch();
            return true;
        }

        private void EnterPerfectMatch()
        {
            _isFrozen = true;
            TransitionTo(PuzzleState.PerfectMatch);
            Log.Info($"[PuzzleStateMachine] EnterPerfectMatch puzzleId={_puzzleId} matchScore={_matchScore:F3} " +
                     $"threshold={_config.PerfectMatchThreshold:F2} → OnPerfectMatch + narrative cascade");
            GameEvent.Get<IShadowPuzzleEvent>().OnPerfectMatch(_puzzleId, _matchScore);
            DispatchPuzzleCompleteAndLock(PuzzleCompletionType.Perfect);
        }

        private void EnterAbsenceAccepted()
        {
            _isFrozen = true;
            TransitionTo(PuzzleState.AbsenceAccepted);
            GameEvent.Get<IShadowPuzzleEvent>().OnAbsenceAccepted(_puzzleId, _matchScore);
            DispatchPuzzleCompleteAndLock(PuzzleCompletionType.Absence);
        }

        private void DispatchPuzzleCompleteAndLock(PuzzleCompletionType completionType)
        {
            if (_puzzleCompleteSent) return; // 防 double-send (AC-10/AC-11 cascade depth ≤ 3)
            _puzzleCompleteSent = true;

            // OnPuzzleComplete 派发（AC-10 第 5 sender）
            GameEvent.Get<IShadowPuzzleEvent>().OnPuzzleComplete(_puzzleId, completionType);

            // OnPuzzleLockAll cascade (AC-11; depth 1)
            GameEvent.Get<IPuzzleLockEvent>().OnPuzzleLockAll();

            // 转入 Complete 终态
            TransitionTo(PuzzleState.Complete);
        }

        private void TransitionTo(PuzzleState next)
        {
            if (_state == next) return; // 同值不派发（防 redundant transitions）
            _state = next;
        }

        // ──────────── Save/Load support (AC-12; per ADR-008 IChapterProgress 集成) ────────────

        /// <summary>
        /// 序列化 snapshot for save system（per AC-12 / ADR-008 IChapterProgress）。
        /// </summary>
        public PuzzleStateSnapshot SaveSnapshot()
        {
            return new PuzzleStateSnapshot(
                puzzleId: _puzzleId,
                state: _state,
                matchScore: _matchScore,
                isFrozen: _isFrozen);
        }

        /// <summary>
        /// 从 save 数据恢复 state（per AC-12）；用于 chapter reload。
        /// </summary>
        public void LoadSnapshot(PuzzleStateSnapshot snapshot)
        {
            if (!_isInitialized)
                throw new InvalidOperationException("Initialize() must be called before LoadSnapshot()");
            if (snapshot.PuzzleId != _puzzleId)
                throw new ArgumentException($"Snapshot puzzleId {snapshot.PuzzleId} mismatches state machine puzzleId {_puzzleId}", nameof(snapshot));

            _state = snapshot.State;
            _matchScore = snapshot.MatchScore;
            _isFrozen = snapshot.IsFrozen;

            // PerfectMatch / AbsenceAccepted / Complete state 恢复后 puzzleComplete 已发过 — 防止 reload 后重发
            if (_state == PuzzleState.PerfectMatch ||
                _state == PuzzleState.AbsenceAccepted ||
                _state == PuzzleState.Complete)
            {
                _puzzleCompleteSent = true;
            }
        }
    }

    /// <summary>
    /// Puzzle state snapshot — save/load roundtrip 用（per AC-12 / ADR-008 IChapterProgress）。
    /// </summary>
    public readonly struct PuzzleStateSnapshot
    {
        public int PuzzleId { get; }
        public PuzzleState State { get; }
        public float MatchScore { get; }
        public bool IsFrozen { get; }

        public PuzzleStateSnapshot(int puzzleId, PuzzleState state, float matchScore, bool isFrozen)
        {
            PuzzleId = puzzleId;
            State = state;
            MatchScore = matchScore;
            IsFrozen = isFrozen;
        }
    }
}
