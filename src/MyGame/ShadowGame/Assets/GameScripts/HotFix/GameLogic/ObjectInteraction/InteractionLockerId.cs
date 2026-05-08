// 该文件由Cursor 自动生成
namespace GameLogic
{
    /// <summary>
    /// 合法 puzzle lock token 常量集合（SP-006 + ADR-013 §"Risks"）。
    /// </summary>
    /// <remarks>
    /// <para>用作 <see cref="InteractionLockManager.PushLock"/> / <see cref="InteractionLockManager.PopLock"/> 的 lockerId 入参。
    /// HashSet token 锁机制要求每个 sender 独占一个稳定 ID（push/pop 必须配对）；用集中常量避免 typo + Magic string。</para>
    ///
    /// <para><b>合法 lockerId 列表（详见 SP-006）</b>：
    /// <list type="bullet">
    /// <item><see cref="ShadowPuzzle"/> = <c>"shadow_puzzle"</c> — Shadow Puzzle 评估系统在阴影计算期间锁住所有交互</item>
    /// <item><see cref="Narrative"/> = <c>"narrative"</c> — 剧情序列（cutscene / dialog）期间锁所有交互</item>
    /// <item><see cref="Tutorial"/> = <c>"tutorial"</c> — 引导高亮单一物体时锁定其他物体（白名单逻辑由后续 epic 加）</item>
    /// </list></para>
    ///
    /// <para><b>未列出的 token</b>：<see cref="InteractionLockManager.PopLock"/> 收到未知 token → <c>Log.Warning</c> + no-op
    /// （不抛异常）。<see cref="InteractionLockManager.PushLock"/> 不校验 token，但**强烈建议**仅用本类常量；
    /// 后续 PR 若需新增 token，必须先扩 <see cref="All"/> 再实施 sender。</para>
    /// </remarks>
    public static class InteractionLockerId
    {
        /// <summary>Shadow Puzzle 评估系统专用 lock token。</summary>
        public const string ShadowPuzzle = "shadow_puzzle";

        /// <summary>Narrative 剧情序列（cutscene / dialog）专用 lock token。</summary>
        public const string Narrative = "narrative";

        /// <summary>Tutorial 引导专用 lock token。</summary>
        public const string Tutorial = "tutorial";

        /// <summary>所有合法 lockerId 列表（用于 warning 日志枚举 + 测试参数化）。</summary>
        public static readonly string[] All = { ShadowPuzzle, Narrative, Tutorial };
    }
}
