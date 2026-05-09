// 该文件由Cursor 自动生成
using System;
using Cysharp.Threading.Tasks;
using TEngine;
using UnityEngine;
using UnityEngine.SceneManagement;
using YooAsset;

namespace GameLogic
{
    /// <summary>
    /// Scene Manager 的 6 个状态（ADR-009 Decision §"Scene Manager State Machine"）。
    /// 本 S2-05 story 仅落地状态转换本身；每个状态对应的真实 I/O 由 Story 002..005 逐步接入。
    /// </summary>
    public enum SceneManagerState
    {
        Idle,
        TransitionOut,
        Unloading,
        Loading,
        TransitionIn,
        Error
    }

    /// <summary>
    /// Scene Manager 对外只读 API（ADR-009）。外部系统通过只读属性观察，不得直接调可变方法。
    /// 所有切换通过 <see cref="ISceneEvent.OnRequestSceneChange"/> 请求（ADR-027 §1）。
    /// </summary>
    public interface ISceneManager
    {
        /// <summary>当前状态机状态。</summary>
        SceneManagerState CurrentState { get; }

        /// <summary>当前已激活的章节 ID（<see cref="SceneManager.NoChapterId"/> 表示尚未加载任何章节）。</summary>
        int CurrentChapterId { get; }

        /// <summary>是否处于过渡中。仅 <see cref="SceneManagerState.Idle"/> / <see cref="SceneManagerState.Error"/> 为 false。</summary>
        bool IsTransitioning { get; }
    }

    /// <summary>
    /// Scene Management epic 的核心状态机（S2-05 Story 001）。
    /// </summary>
    /// <remarks>
    /// <para><b>职责</b>：订阅 <see cref="ISceneEvent.OnRequestSceneChange"/>；维护 6 状态机（Idle → TransitionOut → Unloading → Loading → TransitionIn → Idle + Error）；
    /// 最多缓存 1 个 pending 请求（newest wins）；同章请求立即回应 <see cref="ISceneEvent.OnSceneReady"/>；Error 状态需显式 <see cref="RecoverToIdle"/> 恢复。</para>
    /// <para><b>非职责</b>：11 步 transition 流程（YooAsset LoadSceneAsync / fade / download / BGM 切换）由 Story 002 驱动；
    /// 其余 6 个 <c>ISceneEvent</c> lifecycle sender（TransitionBegin / UnloadBegin / ... LoadFailed）由 Story 003/005 实装。</para>
    /// <para><b>使用约定</b>：在 boot pipeline 中 <c>new SceneManager()</c> 并调用 <see cref="Init"/>；
    /// 进程结束或 domain reload 前调用 <see cref="Dispose"/> 解绑 listener。Story 002 起会通过内部路径驱动状态机，
    /// 测试可用 <see cref="AdvanceStateForTest"/> 桩函数模拟。</para>
    /// </remarks>
    public sealed class SceneManager : ISceneManager, IDisposable
    {
        // ------------------------------------------------------------------
        // Sentinels
        // ------------------------------------------------------------------

        /// <summary>
        /// "无章节"哨兵值。当前语义涵盖：(1) 尚未加载任何章节（<see cref="_currentChapterId"/> 初始）；
        /// (2) 无 in-flight 加载（<see cref="_inflightChapterId"/>）。两者同属"不存在有效章节"，共享 <c>-1</c>。
        /// 未来若需细分（例如 Error 中断后降级），新增 <c>NoChapterId_*</c> 同前缀变体即可。
        /// </summary>
        public const int NoChapterId = -1;

        // ------------------------------------------------------------------
        // S3-01: Additive Scene Loading 常量（ADR-009 + ADR-005 + SP-003 单包策略）
        // ------------------------------------------------------------------

        /// <summary>
        /// 加载失败重试次数上限（D2=[α] 统一外层重试）。
        /// 1 次 attempt = download + LoadSceneAsync + ActivateScene 合起来；2 次耗尽 → OnSceneLoadFailed + Error。
        /// </summary>
        public const int MaxLoadRetry = 2;

        /// <summary>
        /// YooAsset 单包策略（SP-003）— 所有章节场景资源在 <c>"DefaultPackage"</c> 内。
        /// </summary>
        public const string DefaultPackage = "DefaultPackage";

        // ------------------------------------------------------------------
        // State
        // ------------------------------------------------------------------

        private SceneManagerState _state = SceneManagerState.Idle;
        private int _currentChapterId = NoChapterId;
        private int? _pendingTargetChapterId;
        private int _inflightChapterId = NoChapterId;
        private bool _initialized;

        /// <summary>
        /// 当前已加载章节的 sceneName（YooAsset location）。
        /// <para>D5=[X] 只存 string 字符串；不缓存 <see cref="UnityEngine.SceneManagement.Scene"/> 对象 —
        /// <c>UnloadAsync</c> / <c>ActivateScene</c> / <c>IsContainScene</c> 全部以 location 字符串为准。</para>
        /// <para>赋值时机：<see cref="LoadChapterSceneAsync"/> 成功路径末段（Step 10 派 OnSceneLoadComplete 之前），
        /// 与 <see cref="_currentLoadedChapterId"/> 配对赋值，构成 D5 "已加载章节身份" 单一事实源。</para>
        /// <para>清空时机：Story 003 cleanup sequence 调用 <see cref="ClearCurrentChapterSceneName"/>
        /// 后置空（同一 SceneManager 实例内）；该方法同时清 <see cref="_currentLoadedChapterId"/>。</para>
        /// </summary>
        private string _currentChapterSceneName;

        /// <summary>
        /// 当前已加载章节的 chapter id（与 <see cref="_currentChapterSceneName"/> 配对的"已加载身份"，
        /// S3-02 PlayMode runtime 暴露的 Type-2 cross-method protocol drift 修复 / 2026-04-30 v3）。
        /// <para><b>语义区分</b>：
        /// <list type="bullet">
        ///   <item><description><see cref="_currentChapterId"/>（旧）— 状态机视角的"目标章节 id"，
        ///   由 <see cref="OnRequestSceneChange"/> 在过渡发起时即更新到 NEW target。</description></item>
        ///   <item><description><see cref="_currentLoadedChapterId"/>（本字段）— 实际场景视角的"已加载章节 id"，
        ///   仅由 <see cref="LoadChapterSceneAsync"/> 成功路径设置（与 sceneName 同步），
        ///   由 <see cref="ClearCurrentChapterSceneName"/> 同步清空。</description></item>
        /// </list></para>
        /// <para><b>修复目的</b>：S3-02 <see cref="UnloadCurrentChapterAsync"/> 在 spike 直接调用路径
        /// 与生产 state machine driver 路径下，<see cref="_currentChapterId"/> 状态不一致——
        /// spike 路径 _currentChapterId 永远 = <see cref="NoChapterId"/>（守卫始终短路）；
        /// 生产 driver 路径 _currentChapterId 已是 NEW target（sender 派错 chapter id）。
        /// 引入本独立字段，让 cleanup 序列只依赖"已加载身份"，与状态机推进解耦。</para>
        /// <para>初始值 <see cref="NoChapterId"/>；与 <see cref="_currentChapterSceneName"/>(null) 共同表示"无已加载章节"。</para>
        /// </summary>
        private int _currentLoadedChapterId = NoChapterId;

        /// <summary>测试桩：只读当前章节 sceneName（生产代码不得读；外部观察走 ChapterData provider）。</summary>
        public string CurrentChapterSceneNameForTest => _currentChapterSceneName;

        /// <summary>测试桩：只读当前已加载章节 id（与 <see cref="CurrentChapterSceneNameForTest"/> 配对；
        /// 生产代码不得读，外部观察走 <see cref="CurrentChapterId"/>）。</summary>
        public int CurrentLoadedChapterIdForTest => _currentLoadedChapterId;

        /// <summary>
        /// 章节静态配置 provider（S2-07）。生产环境注入
        /// <c>id =&gt; ConfigSystem.Tables.TbChapter.Get(id)</c>；
        /// boot pipeline 必须在切场景之前调 <see cref="RegisterChapterDataProvider"/>。
        /// 默认 <see langword="null"/> —— 任何切章请求都会 fail-loud（<see cref="ISceneEvent.OnSceneLoadFailed"/> + Error）。
        /// </summary>
        private System.Func<int, ChapterData> _chapterDataProvider;

        /// <summary>
        /// 章节切换 fade overlay (S3-03 Story 005 / patch v2 placeholder)。
        /// <para>默认 <see cref="NoOpFadeOverlay"/>（两 method 立即完成，无视觉效果）；
        /// future fade story 通过 <see cref="RegisterFadeOverlay"/> 注入真实 UI impl。</para>
        /// <para><see cref="BeginTransitionAsync"/> Step 4 (fade-out) + Step 11 (fade-in) 委托给本字段。</para>
        /// </summary>
        private IFadeOverlay _fadeOverlay = new NoOpFadeOverlay();

        public SceneManagerState CurrentState => _state;
        public int CurrentChapterId => _currentChapterId;

        public bool IsTransitioning =>
            _state != SceneManagerState.Idle && _state != SceneManagerState.Error;

        /// <summary>
        /// 测试 / Story 002 桩：只读 pending 队列内容（<c>null</c> 表示空）。
        /// 命名的 <c>ForTest</c> 后缀明示非生产 API —— 生产代码不得读该属性，
        /// Story 002 的 11 步流程驱动会通过 <see cref="AdvanceStateForTest"/> 同 asmdef 内访问。
        /// </summary>
        public int? PendingTargetChapterIdForTest => _pendingTargetChapterId;

        /// <summary>
        /// 测试 / Story 002 桩：只读 in-flight 章节（<see cref="NoChapterId"/> 表示当前无加载）。
        /// 与 <see cref="PendingTargetChapterIdForTest"/> 同语义层级（非生产 API）。
        /// 设值时机：<see cref="OnRequestSceneChange"/> 或 <see cref="DrainPending"/> 启动新过渡时。
        /// 清值时机：<see cref="AdvanceStateForTest"/>(Idle) / <see cref="RecoverToIdle"/>（模拟 Loading 完成 / Error 中断）。
        /// </summary>
        public int InflightChapterIdForTest => _inflightChapterId;

        // ------------------------------------------------------------------
        // Lifecycle (AC-6 / AC-14)
        // ------------------------------------------------------------------

        /// <summary>
        /// 订阅 <c>ISceneEvent.OnRequestSceneChange</c>。
        /// <para>AC-6：若已初始化且处于过渡中，no-op + warning（不重置进行中的过渡）；Idle/Error 下 no-op（listener 已经挂着）。</para>
        /// <para>AC-14：首次调用才 AddEventListener，严格配对 Dispose。</para>
        /// </summary>
        public void Init()
        {
            if (_initialized && IsTransitioning)
            {
                Log.Warning("[SceneManager] Init called while transition in progress — ignored.");
                return;
            }

            if (!_initialized)
            {
                GameEvent.AddEventListener<int>(
                    ISceneEvent_Event.OnRequestSceneChange, OnRequestSceneChange);
                _initialized = true;
            }
        }

        /// <summary>AC-14：取消 listener 订阅；再次调用为 no-op。</summary>
        public void Dispose()
        {
            if (!_initialized) return;
            GameEvent.RemoveEventListener<int>(
                ISceneEvent_Event.OnRequestSceneChange, OnRequestSceneChange);
            _initialized = false;
        }

        // ------------------------------------------------------------------
        // S2-07: ChapterData provider 注入（ADR-007 + ADR-009）
        // ------------------------------------------------------------------

        /// <summary>
        /// 注入章节静态配置 provider。生产环境通常在 boot pipeline `Tables.Init()` 之后调
        /// <c>scene.RegisterChapterDataProvider(id =&gt; ConfigSystem.Tables.TbChapter.Get(id))</c>；
        /// 测试中可注入手工 fixture 的 lambda。
        /// 传 <see langword="null"/> 表示清除当前 provider，回到 fail-loud 默认（任何切章请求触发 Error）。
        /// </summary>
        /// <remarks>
        /// Provider 约定：未知 <c>chapterId</c> 必须返回 <see langword="null"/>（与 Luban
        /// <c>TbChapter.Get(id)</c> 真实行为一致）；不可抛异常。
        /// </remarks>
        public void RegisterChapterDataProvider(System.Func<int, ChapterData> provider)
        {
            _chapterDataProvider = provider;
        }

        /// <summary>
        /// 注入章节切换 fade overlay impl（S3-03 patch v2）。boot pipeline 在 UI 模块就绪后调
        /// <c>scene.RegisterFadeOverlay(new UIFadeOverlay(...))</c> 替换默认 <see cref="NoOpFadeOverlay"/>。
        /// 传 <see langword="null"/> 表示恢复默认 NoOp impl（不报错；与 ChapterDataProvider null 语义不同）。
        /// </summary>
        public void RegisterFadeOverlay(IFadeOverlay fadeOverlay)
        {
            _fadeOverlay = fadeOverlay ?? new NoOpFadeOverlay();
        }

        /// <summary>
        /// 校验 <paramref name="chapterId"/> 在当前 provider 下能否解析为合法 <see cref="ChapterData"/>。
        /// </summary>
        /// <remarks>
        /// <para>**柔和 fail-loud 语义**：
        /// <list type="bullet">
        ///   <item><description><c>provider == null</c>（未注册）→ 兼容老行为，跳过校验返回 <c>true</c>。
        ///   生产 boot pipeline 必须显式调 <see cref="RegisterChapterDataProvider"/>，未注册视为**部署 bug**
        ///   而非运行时错误。Control Manifest §2.5 列明该约束。</description></item>
        ///   <item><description><c>provider != null</c> 但 <c>provider(chapterId) == null</c>
        ///   （已注册但表里没这条）→ 派发 <see cref="ISceneEvent.OnSceneLoadFailed"/>
        ///   + 进入 <see cref="SceneManagerState.Error"/>，返回 <c>false</c>。
        ///   这是 story-006 AC-3 真正保护的场景。</description></item>
        /// </list></para>
        ///
        /// <para>失败路径不更新 <see cref="_currentChapterId"/> / <see cref="_inflightChapterId"/>，
        /// 保留它们的"上次合法值"（可能是 <see cref="NoChapterId"/>）；调用方需在自己的分支里提早 return，
        /// 避免推进到 <see cref="SceneManagerState.TransitionOut"/>。</para>
        /// </remarks>
        private bool TryResolveOrFail(int chapterId)
        {
            if (_chapterDataProvider == null)
            {
                // Provider 未注册 → 兼容旧行为；不校验（与 S2-05/S2-06 baseline 一致）
                return true;
            }

            if (_chapterDataProvider(chapterId) != null) return true;

            var reason = $"Chapter ID {chapterId} not found in TbChapter.";
            Log.Warning($"[SceneManager] Chapter resolve failed: {reason}");
            GameEvent.Get<ISceneEvent>().OnSceneLoadFailed(chapterId, reason);
            TransitionTo(SceneManagerState.Error);
            return false;
        }

        // ------------------------------------------------------------------
        // Test hook
        // ------------------------------------------------------------------

        /// <summary>
        /// 测试 / Story 002 桩：由测试或 Story 002 的内部状态推进逻辑调用。
        /// 落到 <see cref="SceneManagerState.Idle"/> 时会触发 pending 队列 drain（AC-11）。
        /// 命名的 <c>ForTest</c> 后缀明示非生产 API —— 外部系统不得直接调用，
        /// 真实过渡入口是 <see cref="ISceneEvent.OnRequestSceneChange"/>。
        /// </summary>
        public void AdvanceStateForTest(SceneManagerState next)
        {
            TransitionTo(next);
            if (next == SceneManagerState.Idle)
            {
                _inflightChapterId = NoChapterId;
                DrainPending();
            }
        }

        // ------------------------------------------------------------------
        // AC-12: Error → Idle recovery
        // ------------------------------------------------------------------

        /// <summary>
        /// 从 Error 显式恢复到 Idle；保留 <c>_pendingTargetChapterId</c>（若有）以便继续。
        /// 非 Error 状态调用为 no-op + warning。
        /// </summary>
        public void RecoverToIdle()
        {
            if (_state != SceneManagerState.Error)
            {
                Log.Warning($"[SceneManager] RecoverToIdle called in {_state}; no-op.");
                return;
            }
            TransitionTo(SceneManagerState.Idle);
            _inflightChapterId = NoChapterId;
            DrainPending();
        }

        // ------------------------------------------------------------------
        // AC-7..AC-10: OnRequestSceneChange handler
        // ------------------------------------------------------------------

        private void OnRequestSceneChange(int targetChapterId)
        {
            // AC-10: Error 状态静默丢弃 + warning
            if (_state == SceneManagerState.Error)
            {
                Log.Warning($"[SceneManager] OnRequestSceneChange({targetChapterId}) dropped — Error state.");
                return;
            }

            // S2-06 AC-5: in-flight 去重 —— 过渡中重复请求同一目标章节，静默丢弃（不污染 pending）。
            // IsTransitioning 守卫保证 Idle/Error 下不命中（Idle 走 AC-8 同章 OnSceneReady 路径，语义不同）。
            if (IsTransitioning && targetChapterId == _inflightChapterId)
            {
                Log.Info($"[SceneManager] OnRequestSceneChange({targetChapterId}) ignored — already in-flight.");
                return;
            }

            if (_state == SceneManagerState.Idle)
            {
                // AC-8: 同章（且已加载过）→ 静默派发 OnSceneReady，不进过渡
                if (targetChapterId == _currentChapterId && _currentChapterId != NoChapterId)
                {
                    GameEvent.Get<ISceneEvent>().OnSceneReady(targetChapterId);
                    return;
                }

                // S2-07 AC-3: 不同章 → 校验 ChapterData 存在；未知则 fail-loud（不更新 current/inflight）
                if (!TryResolveOrFail(targetChapterId))
                {
                    return;
                }

                // AC-7: 不同章 → 开始过渡
                _currentChapterId = targetChapterId;
                _inflightChapterId = targetChapterId;
                TransitionTo(SceneManagerState.TransitionOut);
                // story-001c: listener-path driver 接管 11-step (ADR-009 §Decision line 386 spec align)
                DriveTransitionAsync(targetChapterId).Forget();
                return;
            }

            // AC-9: Non-Idle（TransitionOut / Unloading / Loading / TransitionIn）→ newest-wins 排队
            _pendingTargetChapterId = targetChapterId;
        }

        // ------------------------------------------------------------------
        // AC-11: Drain pending on return-to-Idle
        // ------------------------------------------------------------------

        private void DrainPending()
        {
            if (!_pendingTargetChapterId.HasValue) return;

            int next = _pendingTargetChapterId.Value;
            _pendingTargetChapterId = null;

            // 同章合并语义同 AC-8：静默派发 OnSceneReady，保持 Idle
            if (next == _currentChapterId && _currentChapterId != NoChapterId)
            {
                GameEvent.Get<ISceneEvent>().OnSceneReady(next);
                return;
            }

            // S2-07 AC-3: drain 出来的 pending 也需要校验 ChapterData；未知则 fail-loud
            // 注意：pending 已被先前 Take 出来清空（_pendingTargetChapterId = null），
            // 此处不回填 —— 语义"bad pending 已被消费但状态进 Error"。
            if (!TryResolveOrFail(next))
            {
                return;
            }

            _currentChapterId = next;
            _inflightChapterId = next;
            TransitionTo(SceneManagerState.TransitionOut);
            // story-001c: listener-path driver 接管 11-step (ADR-009 §Decision line 386 spec align)
            DriveTransitionAsync(next).Forget();
        }

        /// <summary>
        /// story-001c (2026-05-09): listener-path internal driver — 由 <see cref="OnRequestSceneChange"/>
        /// 与 <see cref="DrainPending"/> 在状态机推进到 TransitionOut 后调用，await
        /// <see cref="BeginTransitionAsync"/> 走完 11 步流程。
        /// </summary>
        /// <remarks>
        /// <para><b>异常处理</b>：<see cref="BeginTransitionAsync"/> 内部已有 fail-loud 协议
        /// (state=Error + OnSceneLoadFailed via <see cref="LoadChapterSceneAsync"/> + <see cref="TryResolveOrFail"/>)；
        /// 本 catch 仅作兜底，捕获理论上不应发生的"BeginTransitionAsync 之外"的异常逃逸。</para>
        /// <para><b>UniTaskVoid 选择理由</b>：listener handler (<see cref="OnRequestSceneChange"/>) 是
        /// <c>void</c> 签名（per <c>GameEvent.AddEventListener&lt;T&gt;(Action&lt;...&gt;)</c> 协议），
        /// 调用方无法 await；UniTaskVoid + .Forget() 是 fire-and-forget 标准模式（与项目内 IInputBlockerEvent /
        /// ISettingsEvent / IAudioEvent listener 一致）。</para>
        /// <para><b>ADR-009 §Decision spec align</b>：spec line 386 "Scene Manager subscribes to
        /// IChapterStateEvent.OnRequestSceneChange and orchestrates the entire 11-step flow internally"
        /// 与本方法 1:1 alignment；S5-1b 期间 <c>DevTestState.DriveProductionSceneTransitionAsync</c>
        /// F4 dev-only stub 已 removed (story-001c AC-4)。</para>
        /// </remarks>
        private async UniTaskVoid DriveTransitionAsync(int targetChapterId)
        {
            try
            {
                await BeginTransitionAsync(targetChapterId);
            }
            catch (Exception e)
            {
                Log.Error($"[SceneManager] DriveTransitionAsync({targetChapterId}) 异常 (兜底): {e}");
            }
        }

        // ------------------------------------------------------------------
        // S3-01: Additive Scene Loading（11 步流程 Step 5–10）
        // ------------------------------------------------------------------

        /// <summary>
        /// 加载章节场景（11 步流程的 Step 8–10；Step 5–7 cleanup 由 Story 003 在前置调用）。
        /// <para>调用方约定：在状态机推进到 <see cref="SceneManagerState.Loading"/> 之后调用；
        /// First-boot（<see cref="_currentChapterId"/> == <see cref="NoChapterId"/>）路径已由
        /// <see cref="OnRequestSceneChange"/> 内部短路（D6=[①]），本方法不区分 first-boot vs 非 first-boot。</para>
        /// <para>失败路径（manifest invalid / download fail / scene invalid / LoadScene throw）—
        /// 派 <see cref="ISceneEvent.OnSceneLoadFailed"/> + <see cref="TransitionTo"/>(<see cref="SceneManagerState.Error"/>)；
        /// 调用方需在自己的分支读 <see cref="CurrentState"/> 检测失败。</para>
        /// </summary>
        /// <remarks>
        /// <para><b>Implementation contract</b> (<c>story-002</c> v2 patch / 2026-04-30 P0 修订)：</para>
        /// <list type="number">
        /// <item><description>Resolve <c>sceneName</c> via <see cref="_chapterDataProvider"/>（<c>null</c> → fail-loud；唯一 fail-loud 路径）</description></item>
        /// <item><description>Step 8 — Download 分支（<c>downloader.TotalDownloadCount > 0</c> 才走；<c>DownloadUpdateCallback</c> 派 <see cref="ISceneEvent.OnSceneDownloadProgress"/>）</description></item>
        /// <item><description>Step 9 — <c>GameModule.Scene.LoadSceneAsync(sceneName, Additive, progressCallBack)</c>；<c>Scene.IsValid()</c> + <c>.isLoaded</c> 双断言；invalid sceneName 走 catch 进 retry</description></item>
        /// <item><description>Step 10a — <c>GameModule.Scene.ActivateScene(sceneName)</c>（false 仅 warning 不阻塞）</description></item>
        /// <item><description>Step 10b — <see cref="ISceneEvent.OnSceneLoadComplete"/> 派 <c>(targetChapterId, ChapterData.BgmAsset)</c></description></item>
        /// <item><description>整段包 <see cref="MaxLoadRetry"/> 轮 try-catch；耗尽后派 <see cref="ISceneEvent.OnSceneLoadFailed"/> + Error</description></item>
        /// </list>
        /// <para><b>P0 修订原因</b>：原 patch v2 加了 <c>GameModule.Resource.CheckLocationValid(sceneName, "DefaultPackage")</c> 前置 fail-loud；
        /// 实测对 YooAsset scene 资产（.unity）一律返 <c>false</c> —— scene 与 asset 走不同的 location key 体系，
        /// SP-011 实证 PASS 路径根本不需要前置校验。改：移除 CheckLocationValid 前置；
        /// 由 LoadSceneAsync 自身对 invalid sceneName 抛异常 → catch → retry 2 次 → 耗尽 fail；
        /// AC-4 invalid scene 路径走 retry exhaust 形式；ChapterDataProvider 缺失/返 null 仍是唯一 fail-loud。</para>
        /// <para><b>State machine 接入</b>：本方法本身不主动推进 <c>Loading → TransitionIn</c>；
        /// fade in 流程（Step 11）由 Story 005 lifecycle senders 接管。失败路径直接进 <c>Error</c>。</para>
        /// </remarks>
        public async UniTask LoadChapterSceneAsync(int targetChapterId)
        {
            if (_chapterDataProvider == null)
            {
                var reason = $"ChapterDataProvider not registered (RegisterChapterDataProvider must be called in boot pipeline before scene load).";
                Log.Error($"[SceneManager] LoadChapterSceneAsync({targetChapterId}) failed — {reason}");
                GameEvent.Get<ISceneEvent>().OnSceneLoadFailed(targetChapterId, reason);
                TransitionTo(SceneManagerState.Error);
                return;
            }

            var chapterData = _chapterDataProvider(targetChapterId);
            if (chapterData == null)
            {
                var reason = $"ChapterData null for id={targetChapterId} (Luban TbChapter 未配置或 provider 返 null).";
                Log.Error($"[SceneManager] LoadChapterSceneAsync({targetChapterId}) failed — {reason}");
                GameEvent.Get<ISceneEvent>().OnSceneLoadFailed(targetChapterId, reason);
                TransitionTo(SceneManagerState.Error);
                return;
            }

            string sceneName = chapterData.SceneId;

            // 注：原 patch v2 在此前置 GameModule.Resource.CheckLocationValid(sceneName, DefaultPackage)；
            // 实测对 YooAsset scene 资产（.unity）一律返 false → 已移除。
            // invalid sceneName 改由 LoadSceneAsync 抛异常 → catch → retry → exhaust 后 OnSceneLoadFailed。

            Exception lastError = null;
            for (int attempt = 1; attempt <= MaxLoadRetry; attempt++)
            {
                try
                {
                    var downloader = GameModule.Resource.CreateResourceDownloader(DefaultPackage);
                    if (downloader != null && downloader.TotalDownloadCount > 0)
                    {
                        downloader.DownloadUpdateCallback = data =>
                        {
                            GameEvent.Get<ISceneEvent>().OnSceneDownloadProgress(
                                downloader.Progress,
                                data.CurrentDownloadBytes,
                                data.TotalDownloadBytes);
                        };
                        downloader.BeginDownload();
                        await downloader;
                        if (downloader.Status != EOperationStatus.Succeed)
                        {
                            throw new Exception($"Download failed: status={downloader.Status}");
                        }
                    }

                    var loadedScene = await GameModule.Scene.LoadSceneAsync(
                        sceneName,
                        LoadSceneMode.Additive,
                        progressCallBack: p =>
                            GameEvent.Get<ISceneEvent>().OnSceneLoadProgress(sceneName, p));

                    if (!loadedScene.IsValid() || !loadedScene.isLoaded)
                    {
                        throw new Exception($"Scene returned invalid: name={loadedScene.name}, valid={loadedScene.IsValid()}, loaded={loadedScene.isLoaded}");
                    }

                    bool activated = GameModule.Scene.ActivateScene(sceneName);
                    if (!activated)
                    {
                        Log.Warning($"[SceneManager] ActivateScene returned false: {sceneName} (non-blocking — SP-011 P3 同模式)");
                    }

                    _currentChapterSceneName = sceneName;
                    _currentLoadedChapterId = targetChapterId;

                    GameEvent.Get<ISceneEvent>().OnSceneLoadComplete(targetChapterId, chapterData.BgmAsset);

                    Log.Info($"[SceneManager] LoadChapterSceneAsync({targetChapterId}) success on attempt {attempt}/{MaxLoadRetry} (scene={sceneName}).");
                    return;
                }
                catch (Exception ex)
                {
                    lastError = ex;
                    Log.Warning($"[SceneManager] Load attempt {attempt}/{MaxLoadRetry} failed for chapter {targetChapterId}: {ex.Message}");
                }
            }

            var failReason = lastError?.Message ?? "unknown error after all retries exhausted";
            Log.Error($"[SceneManager] LoadChapterSceneAsync({targetChapterId}) all {MaxLoadRetry} attempts exhausted — {failReason}");
            GameEvent.Get<ISceneEvent>().OnSceneLoadFailed(targetChapterId, failReason);
            TransitionTo(SceneManagerState.Error);
        }

        /// <summary>
        /// Story 003 cleanup 接入点（S3-01 暴露 setter；S3-02 cleanup sequence 调用）。
        /// <para>接受 <see langword="null"/> 表示已卸载，下次切场景视为 first-boot 路径。</para>
        /// <para>S3-02 v3 修复 / 2026-04-30：同步清 <see cref="_currentLoadedChapterId"/>，
        /// 保证"已加载身份"双字段（sceneName + loadedChapterId）始终原子配对，
        /// 否则 cleanup 后 sceneName 已 null 但 loadedChapterId 残留，下次 unload guard 会误判。</para>
        /// </summary>
        internal void ClearCurrentChapterSceneName()
        {
            _currentChapterSceneName = null;
            _currentLoadedChapterId = NoChapterId;
        }

        /// <summary>
        /// 测试桩 setter（S3-02 spike P4 路径）：仅供 PlayMode spike / EditMode tests 强写当前章节 sceneName，
        /// 用于模拟 cleanup-on-error 等错误注入场景。生产代码不得调用此方法。
        /// </summary>
        /// <remarks>
        /// 命名 <c>ForTest</c> 后缀明示非生产 API；调用方约定仅 <c>S302_CleanupSequence.cs</c> spike + 同 asmdef 内 testhook 使用。
        /// 与 <see cref="ClearCurrentChapterSceneName"/> 的区别：clear setter 给生产 cleanup 调用，本 setter 给测试错误注入。
        /// </remarks>
        internal void SetCurrentChapterSceneNameForTest(string sceneName)
        {
            _currentChapterSceneName = sceneName;
        }

        // ------------------------------------------------------------------
        // S3-02: Mandatory Cleanup Sequence（Step 5–8 of 11-step flow）
        // ------------------------------------------------------------------

        /// <summary>
        /// 卸载当前章节场景的 mandatory cleanup 序列（4 步：notify → unload → UnloadUnusedAssets → GC）。
        /// <para>调用方约定：在状态机推进到 <see cref="SceneManagerState.Unloading"/> 后调用；
        /// First-boot（<see cref="_currentLoadedChapterId"/> == <see cref="NoChapterId"/> 或
        /// <see cref="_currentChapterSceneName"/> 为 null/空）路径：方法直接 return，
        /// 不派 <see cref="ISceneEvent.OnSceneUnloadBegin"/>，不调 UnloadAsync（AC-3 / AC-8）。</para>
        /// <para>失败路径（UnloadAsync 抛异常或返 false）— try-finally 兜底，<c>ClearCurrentChapterSceneName()</c> +
        /// <c>Resources.UnloadUnusedAssets()</c> + <c>GC.Collect()</c> 永不跳过（AC-7）。
        /// 异常 propagate 给调用方处理（state machine 推进到 Error 由 driver 决定）。</para>
        /// </summary>
        /// <remarks>
        /// <para><b>Implementation contract</b> (story-003 patch v3 / 2026-04-30 — Type-2 cross-method protocol fix)：</para>
        /// <list type="number">
        /// <item><description>Step 5 — <see cref="ISceneEvent.OnSceneUnloadBegin"/>(unloadingChapterId) 派发（ADR-027 sender 模式）；
        /// <b>v3 修复</b>：unloadingChapterId 取自 <see cref="_currentLoadedChapterId"/>（已加载身份），
        /// 而非 <see cref="_currentChapterId"/>（状态机目标）—— 后者在生产 driver 路径下已被
        /// <see cref="OnRequestSceneChange"/> 提前更新到 NEW target，会导致 sender 派错 chapter id。</description></item>
        /// <item><description>AC-2 — <c>await UniTask.Yield()</c> 一帧给 listeners 释放自己持有的 AssetHandle</description></item>
        /// <item><description>Step 6 — <c>await GameModule.Scene.UnloadAsync(unloadingSceneName)</c>（framework wrapper；返 <c>UniTask&lt;bool&gt;</c>，false 仅 warning）</description></item>
        /// <item><description>finally — <c>ClearCurrentChapterSceneName()</c>（v3 同步清 sceneName + loadedChapterId）+
        /// <c>await Resources.UnloadUnusedAssets().ToUniTask()</c> +
        /// <c>GC.Collect()</c>（AC-7 cleanup never skipped on error；try-finally 兜底）</description></item>
        /// </list>
        /// <para><b>v3 Type-2 drift 修复（2026-04-30 PlayMode runtime 暴露）</b>：原 v2 用 <see cref="_currentChapterId"/>
        /// 做 first-boot guard 与 sender chapter id —— 但该字段仅由 <see cref="OnRequestSceneChange"/> 设置，
        /// spike 直接调用 <see cref="LoadChapterSceneAsync"/> 时不会更新它，导致 guard 永远短路（silent return）。
        /// v3 引入 <see cref="_currentLoadedChapterId"/> 独立字段配对 sceneName，与状态机推进解耦。
        /// 详见 ADR-029 history 第 3 数据点 + active.md session 21 (continued #5)。</para>
        /// <para><b>State machine 接入</b>：本方法本身不主动推进 <c>Unloading → Loading</c>；
        /// driver 应在调用 <see cref="UnloadCurrentChapterAsync"/> 完成后显式调
        /// <see cref="LoadChapterSceneAsync"/> 进入 Loading phase。失败路径异常 propagate；
        /// driver 自行决定是否 <see cref="TransitionTo"/>(<see cref="SceneManagerState.Error"/>)。</para>
        /// </remarks>
        public async UniTask UnloadCurrentChapterAsync()
        {
            // AC-3 / AC-8 first-boot guard：loadedChapterId 或 sceneName 缺则 skip Step 5-8
            // v3 修复：守卫从 _currentChapterId（状态机目标）切到 _currentLoadedChapterId（已加载身份），
            // 与 sceneName 配对作为单一事实源；spike 直调路径与生产 driver 路径同语义。
            if (_currentLoadedChapterId == NoChapterId || string.IsNullOrEmpty(_currentChapterSceneName))
            {
                return;
            }

            int unloadingChapterId = _currentLoadedChapterId;
            string unloadingSceneName = _currentChapterSceneName;

            // Step 5 — sender 派发（接口模式，ADR-027）
            GameEvent.Get<ISceneEvent>().OnSceneUnloadBegin(unloadingChapterId);

            // AC-2 — 一帧 yield 给 listeners 自移除 + 释放 AssetHandle
            await UniTask.Yield();

            try
            {
                // Step 6 — framework wrapper UnloadAsync(sceneName)；S3-01 P2 切换路径已实证
                bool unloaded = await GameModule.Scene.UnloadAsync(unloadingSceneName);
                if (!unloaded)
                {
                    Log.Warning($"[SceneManager] UnloadAsync({unloadingSceneName}) returned false; cleanup continues.");
                }
            }
            finally
            {
                // S3-01 D5 setter；不直写字段
                ClearCurrentChapterSceneName();

                // AC-7 — cleanup never skipped on UnloadAsync exception/false
                var op = Resources.UnloadUnusedAssets();
                await op.ToUniTask();
                GC.Collect();
            }
        }

        // ------------------------------------------------------------------
        // S3-03: 11-step Transition Driver（Story 005）
        // ------------------------------------------------------------------

        /// <summary>
        /// 完整 11 步过渡驱动入口（S3-03 Story 005）。
        /// <para>编排 S3-01 <see cref="LoadChapterSceneAsync"/> + S3-02 <see cref="UnloadCurrentChapterAsync"/> +
        /// fade overlay，并补齐 2 个本 story 新增 sender：
        /// <see cref="ISceneEvent.OnSceneTransitionBegin"/> (Step 3) +
        /// <see cref="ISceneEvent.OnSceneTransitionEnd"/> (Step 11)。</para>
        /// </summary>
        /// <param name="targetChapterId">目标章节 id（要求 ChapterDataProvider 能解析；否则 LoadChapterSceneAsync fail-loud）。</param>
        /// <remarks>
        /// <para><b>11 步流程（与 ADR-009 对齐）</b>：</para>
        /// <list type="number">
        /// <item><description>Step 3 — 派发 <see cref="ISceneEvent.OnSceneTransitionBegin"/>(fromChapterId, targetChapterId)；
        /// <c>fromChapterId = _currentLoadedChapterId</c>（S3-02 v3 cross-method protocol；first-boot 路径下 = <see cref="NoChapterId"/>）</description></item>
        /// <item><description>Step 4 — <c>await _fadeOverlay.FadeOutAsync()</c>（默认 NoOp 立即完成）+ <c>TransitionTo(Unloading)</c></description></item>
        /// <item><description>Step 5-7 — <c>await UnloadCurrentChapterAsync()</c>（S3-02 实装；内部 first-boot guard via _currentLoadedChapterId == NoChapterId 自动跳过整段 cleanup）</description></item>
        /// <item><description>Step 8-10 — <c>TransitionTo(Loading)</c> + <c>await LoadChapterSceneAsync(targetChapterId)</c>（S3-01 实装）；
        /// 失败检测通过 <see cref="CurrentState"/> == <see cref="SceneManagerState.Error"/>，失败路径直接 return（OnSceneLoadFailed 已由内部派发；不再派 OnSceneReady / OnSceneTransitionEnd — AC-3 / AC-6）</description></item>
        /// <item><description>Step 11 — <c>TransitionTo(TransitionIn)</c> + 派 <see cref="ISceneEvent.OnSceneReady"/>(targetChapterId) +
        /// <c>await _fadeOverlay.FadeInAsync()</c> + 派 <see cref="ISceneEvent.OnSceneTransitionEnd"/>(targetChapterId) +
        /// <c>TransitionTo(Idle)</c> + <c>_inflightChapterId = NoChapterId</c> + <c>DrainPending()</c></description></item>
        /// </list>
        /// <para><b>State machine 接入</b>：调用方应在状态机推进到 <see cref="SceneManagerState.TransitionOut"/> 后调本方法
        /// （由 <see cref="OnRequestSceneChange"/> 入口推进；本方法内部依次推进
        /// TransitionOut → Unloading → Loading → TransitionIn → Idle）。失败路径推进到 Error 由
        /// <see cref="LoadChapterSceneAsync"/> 内部完成；本方法仅检测 <see cref="CurrentState"/> 提前 return。</para>
        /// <para><b>AC-3 失败短路</b>：失败路径 return 时不调 <c>_inflightChapterId = NoChapterId</c> + <c>DrainPending()</c>；
        /// 因为 state machine 推进到 Error 后需要外部显式 <see cref="RecoverToIdle"/> 才能消费 pending（语义与 AC-12 一致）。</para>
        /// </remarks>
        public async UniTask BeginTransitionAsync(int targetChapterId)
        {
            // S3-02 v3: fromChapterId 取自"已加载身份"字段（非状态机目标 _currentChapterId）
            int fromChapterId = _currentLoadedChapterId;

            // Step 3 — sender A (本 story 新增)
            GameEvent.Get<ISceneEvent>().OnSceneTransitionBegin(fromChapterId, targetChapterId);

            // Step 4 — fade out + 状态机推进 Unloading
            await _fadeOverlay.FadeOutAsync();
            TransitionTo(SceneManagerState.Unloading);

            // Step 5-7 — cleanup 序列（S3-02；内部 first-boot guard 自动 skip OnSceneUnloadBegin）
            await UnloadCurrentChapterAsync();

            // Step 8-10 — 加载（S3-01）
            TransitionTo(SceneManagerState.Loading);
            await LoadChapterSceneAsync(targetChapterId);

            // 失败短路：LoadChapterSceneAsync 失败路径已派 OnSceneLoadFailed + TransitionTo(Error)
            // AC-3 / AC-6 — 不再派 OnSceneReady / OnSceneTransitionEnd
            if (_state == SceneManagerState.Error)
            {
                return;
            }

            // Step 11 — fade in + Ready + End
            TransitionTo(SceneManagerState.TransitionIn);
            GameEvent.Get<ISceneEvent>().OnSceneReady(targetChapterId);
            await _fadeOverlay.FadeInAsync();
            GameEvent.Get<ISceneEvent>().OnSceneTransitionEnd(targetChapterId); // sender B (本 story 新增)

            TransitionTo(SceneManagerState.Idle);
            _inflightChapterId = NoChapterId;
            DrainPending();
        }

        // ------------------------------------------------------------------
        // AC-13: debug log
        // ------------------------------------------------------------------

        private void TransitionTo(SceneManagerState next)
        {
#if UNITY_EDITOR || DEBUG
            Log.Info($"[SceneManager] {_state} → {next}");
#endif
            _state = next;
        }
    }
}
