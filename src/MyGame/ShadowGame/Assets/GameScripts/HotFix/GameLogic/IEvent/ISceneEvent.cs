// 该文件由Cursor 自动生成
using TEngine;

namespace GameLogic
{
    /// <summary>
    /// Scene Management epic 的单一接口事件契约（ADR-027 §1）。
    /// 替代 ADR-006 的 Evt_Scene* 常量 + XxxPayload struct 方案（整体作废）。
    /// </summary>
    /// <remarks>
    /// <para><b>Sender 分工</b>：</para>
    /// <list type="bullet">
    /// <item><c>OnRequestSceneChange</c> — Chapter State / Narrative / Pause Menu 发起；Scene Manager 唯一 listener</item>
    /// <item><c>OnScene*</c>（其余 8 方法）— Scene Manager 唯一 sender；UI / Audio / Gameplay 按需订阅</item>
    /// </list>
    /// <para><b>实装范围</b>：S2-05 Story 001 实装 <c>OnRequestSceneChange</c> + <c>OnSceneReady</c>；
    /// 其余 6 方法签名在本 story 冻结，sender 实现留 S2-17 Story 005。</para>
    /// <para><b>Cascade</b>：所有方法调用不得超过 3 层级联（ADR-027 §2 继承 ADR-006 re-entrancy 约束）。</para>
    /// </remarks>
    [EventInterface(EEventGroup.GroupLogic)]
    public interface ISceneEvent
    {
        /// <summary>请求切换章节场景。</summary>
        /// <param name="targetChapterId">目标章节 ID（1..5；同 CurrentChapterId → 静默派发 OnSceneReady；无效值由 Scene Manager 进入 Error 状态）</param>
        /// <para>Sender: Chapter State / Narrative Event / Pause Menu / Title Screen</para>
        /// <para>Listener: Scene Manager（唯一）</para>
        /// <para>Cascade: 可能触发 OnSceneTransitionBegin / OnSceneReady</para>
        void OnRequestSceneChange(int targetChapterId);

        /// <summary>场景完全就绪，玩家输入可解锁。</summary>
        /// <param name="chapterId">就绪章节 ID</param>
        /// <para>Sender: Scene Manager（同章请求时 + Story 002/005 真实过渡 TransitionIn 结束前）</para>
        /// <para>Listener: UI（关闭 loading 覆盖）, Input（解锁），Gameplay（启用章节逻辑）</para>
        /// <para>Cascade: 可能触发 UI / Audio 后续反应</para>
        void OnSceneReady(int chapterId);

        /// <summary>[Reserved — S2-17 Story 005] 过渡开始，fade out 前的第一个广播。</summary>
        /// <para>Sender: Scene Manager (Step 3 of 11)</para>
        /// <para>Listener: UI（锁输入 + 显示 overlay）, Audio（BGM 渐弱）</para>
        void OnSceneTransitionBegin(int fromChapterId, int toChapterId);

        /// <summary>[Reserved — S2-17 Story 005] 旧场景即将卸载，各系统必须释放 AssetHandle + 自移除 scene-scoped listener。</summary>
        /// <para>Sender: Scene Manager (Step 5 of 11)</para>
        /// <para>Listener: All scene-scoped systems</para>
        void OnSceneUnloadBegin(int chapterId);

        /// <summary>[Reserved — S2-17 Story 005] YooAsset 场景包下载进度（仅首次加载未缓存时触发）。</summary>
        /// <para>Sender: Scene Manager (Step 8 of 11, during download)</para>
        /// <para>Listener: UI（下载进度条）</para>
        void OnSceneDownloadProgress(float progress, long downloadedBytes, long totalBytes);

        /// <summary>[Reserved — S2-17 Story 005] 场景加载进度（0..1）。</summary>
        /// <para>Sender: Scene Manager (Step 9 of 11, during LoadSceneAsync)</para>
        /// <para>Listener: UI（加载进度条）</para>
        void OnSceneLoadProgress(string sceneName, float progress);

        /// <summary>[Reserved — S2-17 Story 005] 场景加载完成（资源就绪，尚未 fade in）。BgmAsset 由 Luban chapter 配置转发。</summary>
        /// <para>Sender: Scene Manager (Step 10 of 11)</para>
        /// <para>Listener: Audio（切换 BGM）</para>
        void OnSceneLoadComplete(int chapterId, string bgmAsset);

        /// <summary>[Reserved — S2-17 Story 005] Fade in 完成，过渡结束（OnSceneReady 之后立即触发）。</summary>
        /// <para>Sender: Scene Manager (Step 11 of 11)</para>
        /// <para>Listener: All（过渡结束确认）</para>
        void OnSceneTransitionEnd(int chapterId);

        /// <summary>[Reserved — S2-17 Story 005] 加载失败；重试已耗尽。</summary>
        /// <para>Sender: Scene Manager (Error path, after MAX_LOAD_RETRY exhausted)</para>
        /// <para>Listener: UI（错误对话框）</para>
        void OnSceneLoadFailed(int chapterId, string error);
    }
}
