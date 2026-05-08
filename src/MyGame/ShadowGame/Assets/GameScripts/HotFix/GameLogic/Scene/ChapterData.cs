// 该文件由Cursor 自动生成
namespace GameLogic
{
    /// <summary>
    /// Scene Management 用到的章节静态配置数据（ADR-009 §"Scene Name Resolution" + ADR-007 §Luban Access）。
    /// </summary>
    /// <remarks>
    /// <para><b>来源</b>：生产环境从 Luban <c>TbChapter</c> 表读取（每行映射成一个 <see cref="ChapterData"/> 实例）；
    /// S2-07 阶段 Luban 链路尚未接通，<see cref="SceneManager"/> 通过
    /// <c>Func&lt;int, ChapterData&gt;</c> provider 注入获得（默认 <c>null</c>，fail-loud）。</para>
    ///
    /// <para><b>不可变性</b>：所有字段 <see langword="readonly"/>。运行时**禁止**修改一个 <see cref="ChapterData"/>
    /// 实例的字段（ADR-007 §"Config data objects are read-only after Init()"）。需要不同数据 → 重新通过 provider 解析。</para>
    ///
    /// <para><b>未来 Luban 列对照</b>（<c>TbChapter.xlsx</c>）：
    /// <list type="bullet">
    ///   <item><description><see cref="Id"/> ↔ <c>Id</c> (int)</description></item>
    ///   <item><description><see cref="SceneId"/> ↔ <c>SceneId</c> (string) — Unity scene 名（不带路径）</description></item>
    ///   <item><description><see cref="BgmAsset"/> ↔ <c>BgmAsset</c> (string) — YooAsset 地址</description></item>
    ///   <item><description><see cref="EmotionalWeight"/> ↔ <c>EmotionalWeight</c> (float, 0.5–2.0)</description></item>
    ///   <item><description><see cref="OverlayColor"/> ↔ <c>OverlayColor</c> (string, hex e.g. <c>"#FFE6D5"</c>)</description></item>
    /// </list></para>
    ///
    /// <para><b>S2-07 范围</b>：本 story 只用到 <see cref="Id"/> + 存在性校验（未知 chapter → Error）；
    /// 其余字段（SceneId / BgmAsset / EmotionalWeight / OverlayColor）签名先行冻结，
    /// 真实使用由 S2-14（BeginTransition 11 步）/ S2-17（lifecycle senders）/ Transition Overlay UI 各自接入。</para>
    /// </remarks>
    public sealed class ChapterData
    {
        /// <summary>章节 ID（1..5）。对应 Luban <c>TbChapter.Id</c>。</summary>
        public readonly int Id;

        /// <summary>Unity scene 名（不含 .unity 扩展、不含路径）。对应 Luban <c>TbChapter.SceneId</c>。</summary>
        public readonly string SceneId;

        /// <summary>BGM YooAsset 地址。对应 Luban <c>TbChapter.BgmAsset</c>。</summary>
        public readonly string BgmAsset;

        /// <summary>
        /// 情感权重 — fade 时长乘子。范围 0.5–2.0；默认 1.0。
        /// 对应 Luban <c>TbChapter.EmotionalWeight</c>。
        /// </summary>
        public readonly float EmotionalWeight;

        /// <summary>章节过渡 overlay 的 hex 颜色字符串（例如 <c>"#FFE6D5"</c>）。对应 Luban <c>TbChapter.OverlayColor</c>。</summary>
        public readonly string OverlayColor;

        public ChapterData(int id, string sceneId, string bgmAsset, float emotionalWeight = 1.0f, string overlayColor = "#FFFFFF")
        {
            Id = id;
            SceneId = sceneId;
            BgmAsset = bgmAsset;
            EmotionalWeight = emotionalWeight;
            OverlayColor = overlayColor;
        }
    }
}
