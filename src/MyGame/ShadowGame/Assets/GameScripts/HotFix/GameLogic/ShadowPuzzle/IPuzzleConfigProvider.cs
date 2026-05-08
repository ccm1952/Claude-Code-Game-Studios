// 该文件由Cursor 自动生成
namespace GameLogic
{
    /// <summary>
    /// Puzzle 配置 provider 接口（DI 契约；per ADR-014 §B "PuzzleConfig + provider 注入" + ADR-013 v3 lifecycle hook 模式）。
    /// </summary>
    /// <remarks>
    /// <para>测试 fixture 可实现本接口替代 Luban 真表（沿 IInputConfigProvider pattern）。</para>
    /// <para>Production 实现：<see cref="PuzzleConfigFromLuban"/>（Sprint 5 阶段为 hardcoded MVP config 字典 stub；
    /// future ADR-014 真 Luban TbPuzzle 表生成后替换）。</para>
    /// <para>R3 stub 兼容性（ADR-029 V2.0 §V2-2）：所有方法都可被 test stub 实现 — 简单 dictionary lookup 即可。</para>
    /// </remarks>
    public interface IPuzzleStateConfigProvider
    {
        /// <summary>
        /// 按 puzzleId 查询 puzzle 状态机配置；返回 <c>null</c> 表示该 puzzle 未配置（caller 决定 fallback 策略）。
        /// </summary>
        PuzzleStateConfig GetConfig(int puzzleId);

        /// <summary>
        /// 返回是否包含指定 puzzleId 的配置（preflight 检查；避免 GetConfig null 重试）。
        /// </summary>
        bool HasConfig(int puzzleId);
    }
}
