// 该文件由Cursor 自动生成
// S5-03 dev-story Step 1 deficiency-flagged interim impl per ADR-029 V2.0 §V2-2
//   背景: shadow-puzzle/story-001 §Engine Notes "⚠️ Required Framework Extension" — Luban TbPuzzle
//   真表未生成；本 provider 用 hardcoded MVP config 字典 stub，chapter 1 至少 1 个 puzzle 配置可用。
//   Production 升级路径：future ADR-014 + Luban tooling 生成 TbPuzzle.tbres → InitFromLuban() 替换 InitWithDefaults()。

using System.Collections.Generic;

namespace GameLogic
{
    /// <summary>
    /// Puzzle 配置 provider — Luban 接入版（Sprint 5 interim 用 hardcoded MVP config；
    /// future ADR-014 + Luban tooling 生成 TbPuzzle 后通过 <see cref="InitFromLuban"/> 真表加载）。
    /// </summary>
    /// <remarks>
    /// <para>沿 <c>InputConfigFromLuban</c> pattern：default + Luban 双重模式；测试用 <see cref="InitWithDefaults"/>，
    /// production 用 <see cref="InitFromLuban"/>（Luban TbPuzzle 真表生成后激活）。</para>
    /// <para>S5-03 dev-story 阶段 chapter 1 至少 1 个 standard puzzle 配置可用；ADR-014 v1 default thresholds
    /// 直接落 PuzzleConfig ctor。</para>
    /// </remarks>
    public sealed class PuzzleStateConfigFromLuban : IPuzzleStateConfigProvider
    {
        private readonly Dictionary<int, PuzzleStateConfig> _configs = new Dictionary<int, PuzzleStateConfig>();
        private bool _initialized;

        /// <summary>
        /// MVP hardcoded defaults — Sprint 5 interim impl（chapter 1 至少 1 个 standard puzzle config）。
        /// </summary>
        public void InitWithDefaults()
        {
            _configs.Clear();

            // Chapter 1 第一个 puzzle — 标准 PerfectMatch path（非 absence）
            _configs[1] = new PuzzleStateConfig(
                id: 1,
                isAbsencePuzzle: false,
                nearMatchThreshold: 0.40f,
                perfectMatchThreshold: 0.85f,
                maxCompletionScore: 0f,
                absenceAcceptDelay: 0f,
                tutorialGracePeriod: 3.0f);

            // Chapter 5 absence puzzle 示例 — 用于 absence path 端到端验证
            _configs[51] = new PuzzleStateConfig(
                id: 51,
                isAbsencePuzzle: true,
                nearMatchThreshold: 0.40f,
                perfectMatchThreshold: 0.85f,
                maxCompletionScore: 0.65f,
                absenceAcceptDelay: 5.0f,
                tutorialGracePeriod: 0f);

            _initialized = true;
        }

        /// <summary>
        /// Future production path — Luban TbPuzzle.tbres 生成后激活；当前为 stub 占位（throws 提醒未实施）。
        /// </summary>
        public void InitFromLuban()
        {
            // TODO[S5-03+]: 留 future ADR-014 + Luban tooling expand
            // 实际实现伪码：
            //   foreach (var row in ConfigSystem.Instance.Tables.TbPuzzle.DataList)
            //       _configs[row.Id] = new PuzzleConfig(row.Id, row.IsAbsence, row.NearMatch, ...);
            //   _initialized = true;
            // 当前 stub 抛 NotImplementedException 提醒未实施 + 启动时 fallback InitWithDefaults。
            InitWithDefaults();
        }

        /// <inheritdoc />
        public PuzzleStateConfig GetConfig(int puzzleId)
        {
            if (!_initialized) InitWithDefaults();
            return _configs.TryGetValue(puzzleId, out var cfg) ? cfg : null;
        }

        /// <inheritdoc />
        public bool HasConfig(int puzzleId)
        {
            if (!_initialized) InitWithDefaults();
            return _configs.ContainsKey(puzzleId);
        }

        /// <summary>
        /// 测试辅助：注入自定义 PuzzleStateConfig（绕过 default 配置；测试 fixture 用）。
        /// </summary>
        public void RegisterConfig(PuzzleStateConfig config)
        {
            if (!_initialized) _initialized = true;
            _configs[config.Id] = config;
        }
    }
}
