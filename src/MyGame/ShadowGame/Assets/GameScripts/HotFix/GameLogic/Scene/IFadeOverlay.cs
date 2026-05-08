// 该文件由Cursor 自动生成
using Cysharp.Threading.Tasks;

namespace GameLogic
{
    /// <summary>
    /// 章节切换 fade overlay 抽象层（S2-17 Story 005 / patch v2 placeholder pattern）。
    /// </summary>
    /// <remarks>
    /// <para><b>用途</b>：<see cref="SceneManager.BeginTransitionAsync"/> 11 步流程的 Step 4 (fade-out) +
    /// Step 11 (fade-in) 通过本接口委托给 UI 层；解耦 SceneManager 与 fade UI infra（fade UI 可未实装）。</para>
    /// <para><b>默认 impl</b>：<see cref="NoOpFadeOverlay"/>（两 method 直接 return <c>UniTask.CompletedTask</c>）；
    /// SceneManager 内置作为初始值，无需外部注入即可工作。</para>
    /// <para><b>future fade story 替换</b>：通过
    /// <see cref="SceneManager.RegisterFadeOverlay"/> setter 注入真实 UI impl
    /// （与 <see cref="SceneManager.RegisterChapterDataProvider"/> 同 DI pattern）。
    /// 在 boot pipeline UI 模块就绪后调
    /// <c>scene.RegisterFadeOverlay(new UIFadeOverlay(...))</c>。</para>
    /// </remarks>
    public interface IFadeOverlay
    {
        /// <summary>过渡开始时调（Step 4）。完成后 SceneManager 进入 Unloading。</summary>
        UniTask FadeOutAsync();

        /// <summary>过渡结束时调（Step 11，Ready 之后 / TransitionEnd 之前）。完成后派 OnSceneTransitionEnd。</summary>
        UniTask FadeInAsync();
    }

    /// <summary>
    /// 占位 impl — 两 method 直接 return <see cref="UniTask.CompletedTask"/>；future fade story 替换为真实 UI impl。
    /// </summary>
    /// <remarks>
    /// SceneManager 默认持有该 impl 作为字段初始值；即使 boot pipeline 没有显式调
    /// <see cref="SceneManager.RegisterFadeOverlay"/>，过渡也能正常完成（无 fade 视觉效果）。
    /// </remarks>
    public sealed class NoOpFadeOverlay : IFadeOverlay
    {
        public UniTask FadeOutAsync() => UniTask.CompletedTask;
        public UniTask FadeInAsync() => UniTask.CompletedTask;
    }
}
