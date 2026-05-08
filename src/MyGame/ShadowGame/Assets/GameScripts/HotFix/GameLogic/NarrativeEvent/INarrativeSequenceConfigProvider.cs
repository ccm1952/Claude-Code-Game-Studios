// 该文件由Cursor 自动生成
namespace GameLogic
{
    /// <summary>
    /// Narrative sequence config provider 抽象接口（per ADR-016 §B 设计；ADR-029 V2.0 R3 mandatory 接口对依赖
    /// 注入；测试可注入 mock provider 绕过 Luban 真表）。
    /// </summary>
    public interface INarrativeSequenceConfigProvider
    {
        /// <summary>
        /// 解析指定 trigger source + sequence type 对应的 sequence 配置。
        /// </summary>
        /// <param name="triggerSourceId">触发源 ID（puzzleId / chapterId）</param>
        /// <param name="sequenceType">sequence 类型</param>
        /// <returns>对应配置；未配置则返回 null（NarrativeSequencePlayer 应派发 OnSequenceFailed("config_not_found")）。</returns>
        NarrativeSequenceConfig Resolve(int triggerSourceId, NarrativeSequenceType sequenceType);

        /// <summary>检查指定 trigger source + sequence type 是否有配置。</summary>
        bool HasConfig(int triggerSourceId, NarrativeSequenceType sequenceType);
    }
}
