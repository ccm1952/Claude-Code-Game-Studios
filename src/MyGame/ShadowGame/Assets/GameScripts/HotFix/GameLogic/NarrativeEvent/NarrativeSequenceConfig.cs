// 该文件由Cursor 自动生成
using System;
using System.Collections.Generic;

namespace GameLogic
{
    /// <summary>
    /// Narrative sequence 完整配置 POCO（per ADR-016 §A 设计 + ADR-029 V2.0 R3 mandatory sealed/readonly）。
    /// <para>Sealed + readonly — constructor-only construction（测试 fixture / Luban provider 都用 ctor，
    /// 不用 object initializer，per ADR-029 V2.0 §V2-3 R3 PuzzleConfig CS7036/CS0191 lesson）。</para>
    /// <para>Effects 列表在 ctor 内 sort by StartTime（time-sorted invariant）— 调用方传入顺序无关。</para>
    /// </summary>
    public sealed class NarrativeSequenceConfig
    {
        /// <summary>Sequence 唯一 ID（per Luban table primary key — 0 为 sentinel "未配置"）。</summary>
        public int Id { get; }

        /// <summary>Sequence 类型（决定 player 内 branch 处理）。</summary>
        public NarrativeSequenceType Type { get; }

        /// <summary>Atomic effects 列表（构造后 sort by StartTime；外部不可变）。</summary>
        public IReadOnlyList<AtomicEffectConfig> Effects { get; }

        /// <summary>Sequence 总时长（秒）— Tick 跨过此值时 CompleteSequence。</summary>
        public float TotalDuration { get; }

        /// <summary>是否 chapter 终止 sequence（章节切换最终 sequence；用于 ChapterStateManager 决策）。</summary>
        public bool IsChapterFinal { get; }

        public NarrativeSequenceConfig(
            int id,
            NarrativeSequenceType type,
            IReadOnlyList<AtomicEffectConfig> effects,
            float totalDuration,
            bool isChapterFinal)
        {
            Id = id;
            Type = type;
            TotalDuration = totalDuration;
            IsChapterFinal = isChapterFinal;

            // Defensive: 保证 effects 非 null + 按 StartTime 升序排列（time-sorted invariant per ADR-016 §A）。
            // 不做去重，调用方负责 unique startTime per effect（parallel effects 共享 startTime 是合法语义）。
            if (effects == null)
            {
                Effects = Array.Empty<AtomicEffectConfig>();
            }
            else
            {
                var sorted = new List<AtomicEffectConfig>(effects);
                sorted.Sort(static (a, b) => a.StartTime.CompareTo(b.StartTime));
                Effects = sorted.AsReadOnly();
            }
        }
    }
}
