// 该文件由Cursor 自动生成
using System;

namespace GameLogic
{
    /// <summary>
    /// Per-puzzle state machine configuration POCO（per ADR-014 §B 配置层 + AC-13 Luban TbPuzzle 加载）。
    /// </summary>
    /// <remarks>
    /// <para>命名说明（2026-05-06 R2 readiness check #1 patch 2）：本类原拟名 <c>PuzzleConfig</c>，但
    /// <c>GameLogic.PuzzleConfig</c> 已被 Sprint 2 ObjectInteraction 占用（drag/snap config）。
    /// Sprint 5 dev-story 阶段重命名为 <c>PuzzleStateConfig</c>（语义：state machine 配置；与 puzzle
    /// 物件 drag/snap 配置区分）。同 readiness check 暴露的命名冲突 patch 一并落 ADR-029 V3 candidate #8。</para>
    ///
    /// <para>设计原则（沿 InputConfig pattern；S2-13 PuzzleConfig drift 教训 cover）：</para>
    /// <list type="bullet">
    ///   <item><c>sealed class</c> + <c>readonly</c> 字段 — 一旦构造不可变；测试中只能用 ctor 不能用 object initializer。</item>
    ///   <item>7-arg 显式 constructor，required parameters 全 first；测试中 <c>new PuzzleStateConfig(id: 1, ...)</c> 显式赋值。</item>
    ///   <item>POCO 不依赖 TEngine / Unity，pure data；测试中可直接构造 fixture。</item>
    /// </list>
    /// <para>R3 stub 验证（ADR-029 V2.0 §V2-2）：本类作为 stub data type 已 grep-verified，constructor signature
    /// 与 story-001 §Implementation Notes §2 一致；测试 fixture 可直接构造（per InputConfig pattern；
    /// 防 S2-13 CS7036/CS0191 ×3 drift 复发）。</para>
    /// </remarks>
    public sealed class PuzzleStateConfig
    {
        /// <summary>
        /// Puzzle 唯一 ID（chapter + puzzle 内序号编码；per ADR-014 §B）。
        /// </summary>
        public int Id { get; }

        /// <summary>
        /// 是否为 Absence puzzle（Chapter 5 special；触发 AbsenceAccepted state path 替代 PerfectMatch path）。
        /// </summary>
        public bool IsAbsencePuzzle { get; }

        /// <summary>
        /// NearMatch 进入阈值（matchScore ≥ 此值进入 NearMatch；default 0.40，per ADR-014 §B）。
        /// </summary>
        public float NearMatchThreshold { get; }

        /// <summary>
        /// PerfectMatch 阈值（matchScore ≥ 此值 + 非 grace period 进入 PerfectMatch；default 0.85，per TR-puzzle-004）。
        /// </summary>
        public float PerfectMatchThreshold { get; }

        /// <summary>
        /// Absence puzzle 完成的 max completion score（仅 Absence path 使用；matchScore ≥ 此值 + 5s idle → AbsenceAccepted）。
        /// </summary>
        public float MaxCompletionScore { get; }

        /// <summary>
        /// Absence idle timer 触发延迟（秒）；matchScore ≥ MaxCompletionScore 持续达 此 秒数 → AbsenceAccepted；default 5.0。
        /// </summary>
        public float AbsenceAcceptDelay { get; }

        /// <summary>
        /// Tutorial grace period（秒）；OnTutorialCompleted 后 此 秒内 PerfectMatch / AbsenceAccepted 转换被 block；default 3.0。
        /// </summary>
        public float TutorialGracePeriod { get; }

        /// <summary>
        /// Hysteresis 退出阈值（NearMatch → Active）= NearMatchThreshold - HysteresisOffset（default 0.05）。
        /// </summary>
        public float HysteresisOffset => 0.05f;

        /// <summary>
        /// 7-arg explicit constructor（required parameters all first；testable via positional 或 named args）。
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">参数越界（thresholds out of [0,1] / 时间为负）</exception>
        public PuzzleStateConfig(
            int id,
            bool isAbsencePuzzle,
            float nearMatchThreshold,
            float perfectMatchThreshold,
            float maxCompletionScore,
            float absenceAcceptDelay,
            float tutorialGracePeriod)
        {
            if (id < 0)
                throw new ArgumentOutOfRangeException(nameof(id), id, "Puzzle id must be ≥ 0");
            if (nearMatchThreshold < 0f || nearMatchThreshold > 1f)
                throw new ArgumentOutOfRangeException(nameof(nearMatchThreshold), nearMatchThreshold, "NearMatchThreshold ∈ [0, 1]");
            if (perfectMatchThreshold < 0f || perfectMatchThreshold > 1f)
                throw new ArgumentOutOfRangeException(nameof(perfectMatchThreshold), perfectMatchThreshold, "PerfectMatchThreshold ∈ [0, 1]");
            if (perfectMatchThreshold < nearMatchThreshold)
                throw new ArgumentOutOfRangeException(nameof(perfectMatchThreshold), perfectMatchThreshold, "PerfectMatchThreshold must be ≥ NearMatchThreshold");
            if (maxCompletionScore < 0f || maxCompletionScore > 1f)
                throw new ArgumentOutOfRangeException(nameof(maxCompletionScore), maxCompletionScore, "MaxCompletionScore ∈ [0, 1]");
            if (absenceAcceptDelay < 0f)
                throw new ArgumentOutOfRangeException(nameof(absenceAcceptDelay), absenceAcceptDelay, "AbsenceAcceptDelay must be ≥ 0");
            if (tutorialGracePeriod < 0f)
                throw new ArgumentOutOfRangeException(nameof(tutorialGracePeriod), tutorialGracePeriod, "TutorialGracePeriod must be ≥ 0");

            Id = id;
            IsAbsencePuzzle = isAbsencePuzzle;
            NearMatchThreshold = nearMatchThreshold;
            PerfectMatchThreshold = perfectMatchThreshold;
            MaxCompletionScore = maxCompletionScore;
            AbsenceAcceptDelay = absenceAcceptDelay;
            TutorialGracePeriod = tutorialGracePeriod;
        }
    }
}
