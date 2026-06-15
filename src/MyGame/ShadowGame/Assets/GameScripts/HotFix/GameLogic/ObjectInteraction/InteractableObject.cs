// 该文件由Cursor 自动生成
using System;
using DG.Tweening;
using TEngine;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 可交互物体绑定层（S2-08 + S2-09 + S2-10 + S2-11）。
    /// </summary>
    /// <remarks>
    /// <para><b>职责</b>：
    /// <list type="bullet">
    /// <item>S2-08：持有 <see cref="InteractableObjectFsm"/> 纯 C# 实例 + Unity lifecycle 桥接 + <see cref="IInteractionEvent.OnInteractionLockChanged"/> 转发到 <see cref="InteractableObjectFsm.OnLockChanged"/></item>
    /// <item>S2-09：自订阅 <see cref="IGestureEvent.OnDrag"/>（push 模式）+ <see cref="Update"/> 中 1:1 跟手指 + 边界 clamp + race-fallback rebound（DOTween EaseOutBack）</item>
    /// <item>S2-10：Snapping 状态进入第一帧启动 grid snap（DOTween EaseOutQuad；公式 <c>round(rawPos / gridSize) * gridSize</c>）；OnComplete 调 <c>fsm.OnSnapCompleted</c> + 派发 <see cref="IInteractionEvent.OnObjectTransformChanged"/>；中断由 <see cref="InteractableObjectFsm.StateChanged"/> Snapping→非 Idle 监听到则 DOKill(false)</item>
    /// <item>S2-11：<see cref="HandleRotate"/> 公开 API（由 <c>InteractionCoordinator</c> 转发，不订阅 IGestureEvent.OnRotate 自身）+ <see cref="_accumulatedAngleDeg"/> 累加器避开 eulerAngles wrap-around + <see cref="SnapRotation"/> Ended/Cancelled phase 触发（DOTween OutQuad；公式 <c>round(angle / RotationStep) * RotationStep</c>，归一化 [0,360)）+ OnComplete 派 <see cref="IInteractionEvent.OnObjectTransformChanged"/>。Locked 状态忽略 OnRotate（守卫在 fsm.CurrentState 检查 + Coordinator 端 IsLocked 检查双层防御）</item>
    /// </list></para>
    ///
    /// <para><b>非职责</b>：
    /// <list type="bullet">
    /// <item>Tap 选取 / Drag Began-Ended fsm 触发 — 由 <c>InteractionCoordinator</c>（S2-13）调用 <c>Fsm.OnTapHit / OnDragBegan / OnDragEnded</c> 驱动</item>
    /// <item>视觉反馈 — S2-15 通过 <see cref="InteractableObjectFsm.StateChanged"/> 订阅</item>
    /// </list></para>
    ///
    /// <para><b>Drag 数据流（S2-09 X1 patch v2）</b>：push 模式自订阅 <c>IGestureEvent.OnDrag</c>。
    /// handler 早期 return 守卫：fsm 非 Dragging 或 Phase ∉ {Began, Updated} 时立即 return；
    /// 否则缓存 <c>ScreenPos</c>，<see cref="Update"/>（→<see cref="TickDrag"/>）下一帧消费。
    /// Began/Ended fsm 触发由 InteractionCoordinator (S2-13) 调用，本组件不调 fsm trigger。</para>
    ///
    /// <para><b>Provider 注入（S2-09）</b>：<see cref="RegisterPuzzleConfigProvider"/> 静态注入
    /// <c>Func&lt;int, PuzzleConfig&gt;</c>（与 <c>SceneManager.RegisterChapterDataProvider</c> 同模式），
    /// 避免硬依赖 Luban <c>TbPuzzle</c>（尚未生成）。<c>OnEnable</c> fail-loud：provider 未注册或返 null → Log.Error + 禁用本组件。</para>
    ///
    /// <para><b>Lifecycle hooks（S2-09 X1 patch v3）</b>：<see cref="Initialize"/> / <see cref="Shutdown"/>
    /// 是显式生命周期入口，幂等。生产路径由 Unity 的 <c>OnEnable</c> / <c>OnDisable</c> 自动委托；
    /// EditMode 测试中 <c>OnEnable</c> 不被 PlayerLoop 自动驱动，须显式 <c>io.Initialize()</c>（与
    /// <c>SceneManager.Init() / Dispose()</c> 同模式）。</para>
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class InteractableObject : MonoBehaviour
    {
        // ----------------------------------------------------------------- Inspector

        [Tooltip("物体 ID（Luban TbInteractable 主键）；由场景搭建时在 Inspector 指定。")]
        [SerializeField] private int _objectId = -1;

        [Tooltip("拼图 ID（Luban TbPuzzle 主键）；用于通过 PuzzleConfigProvider 解析 InteractionBounds / GridSize / SnapSpeed。")]
        [SerializeField] private int _puzzleId = -1;

        [Tooltip("Drag 时 ScreenToWorldPoint 的 Z 分量（相机视角下到物体的距离）。一般等于物体相对相机的距离。")]
        [SerializeField] private float _dragDepth = 10f;

        [Tooltip("游戏相机引用（Inspector 拖入；禁止 Camera.main fallback —— ADR-012）。")]
        [SerializeField] private Camera _gameplayCamera;

        // ----------------------------------------------------------------- Static provider

        private static Func<int, PuzzleConfig> _puzzleConfigProvider;

        /// <summary>
        /// 注入 PuzzleConfig provider（与 <c>SceneManager.RegisterChapterDataProvider</c> 同模式）。
        /// 通常由 boot pipeline / chapter scene 引导器在 InteractableObject OnEnable 之前调用一次。
        /// </summary>
        public static void RegisterPuzzleConfigProvider(Func<int, PuzzleConfig> provider)
        {
            _puzzleConfigProvider = provider;
        }

        /// <summary>
        /// 测试专用：清空 provider，避免跨用例污染（与 SceneManager 同 ForTest 套路）。
        /// public 而非 internal —— EditModeTests 跨 asmdef 访问，沿用 S2-05 先例（asmdef 间 internal 不可见）。
        /// </summary>
        public static void ClearPuzzleConfigProviderForTest() => _puzzleConfigProvider = null;

        // ----------------------------------------------------------------- Public state

        /// <summary>底层纯 C# 状态机；<c>OnEnable</c> 后非 null，<c>OnDisable</c> 后 null。</summary>
        public InteractableObjectFsm Fsm { get; private set; }

        /// <summary>透传 fsm.ObjectId；fsm 未初始化时回退到 Inspector 字段。</summary>
        public int ObjectId => Fsm?.ObjectId ?? _objectId;

        /// <summary>透传 fsm.CurrentState；fsm 未初始化时回退到 <see cref="InteractableObjectState.Idle"/>。</summary>
        public InteractableObjectState CurrentState => Fsm?.CurrentState ?? InteractableObjectState.Idle;

        /// <summary>
        /// Race-fallback rebound 是否进行中（S2-09）。
        /// <see cref="bool"/>true</see> 时 S2-10 grid snap 推迟启动，等 rebound OnComplete 后下一帧 Update 自然接管。
        /// 生产主路径上长期为 <see cref="bool"/>false</see>（每帧 clamp 已让 rebound 跳过）。
        /// </summary>
        public bool IsReboundActive { get; private set; }

        // ----------------------------------------------------------------- Drag state (S2-09)

        private PuzzleConfig _puzzleConfig;
        private Vector2 _lastDragScreenPos;
        private bool _hasFreshDrag;
        /// <summary>拖拽起点相对指下点的世界偏移（避免 Began 帧物体跳到指心）。</summary>
        private Vector3 _dragGrabOffset;
        private bool _dragGrabOffsetValid;
        private bool _listenersRegistered;

        // ----------------------------------------------------------------- Snap state (S2-10)

        /// <summary>
        /// Snapping 状态的"已启动一次性 snap"标记。<see cref="OnFsmStateChanged"/> 从 Snapping 离开时清零；
        /// <see cref="Update"/> 路由 Snapping case 见此 flag = false 时调 <see cref="StartSnap"/>。
        /// </summary>
        private bool _didStartSnap;

        // ----------------------------------------------------------------- Rotation state (S2-11)

        /// <summary>
        /// 旋转累加器（度）。避开 <c>transform.eulerAngles.z</c> 在跨 360°/0° 边界时的 wrap-around 跳变 ——
        /// 持续旋转期间所有 delta 累加到本字段，<see cref="HandleRotate"/> 写 <c>transform.eulerAngles.z = _accumulatedAngleDeg</c>；
        /// <see cref="SnapRotation"/> 用本字段做 round 计算后归一化到 [0, 360) 写回。
        /// <para>Began phase 重置：从 <c>transform.eulerAngles.z</c> 重新加载（避免上次 Locked 中卡住的旧值污染本次手势）。</para>
        /// <para>public getter / private setter — 测试需读取本字段验证 wrap-around 累加；asmdef 间 internal 不可见，沿用 S2-09 LastWorldPosUnclamped 先例改 public。</para>
        /// </summary>
        public float AccumulatedAngleDeg { get; private set; }

        /// <summary>
        /// 最近一帧 OnDrag 的 raw worldPos（未 clamp，用于 rebound 检测）。
        /// public getter / private setter —— 测试需读取本字段验证 race-fallback rebound 触发条件；
        /// asmdef 间 internal 不可见，沿用 S2-05 先例改 public。
        /// </summary>
        public Vector3 LastWorldPosUnclamped { get; private set; }

        // ----------------------------------------------------------------- Constants

        /// <summary>Rebound DOMove 时长（秒）。ADR-013 §"Snap on release"。</summary>
        private const float ReboundDurationSeconds = 0.25f;

        /// <summary>Rebound 触发距离阈值（小于此值视为 transform 已在 clampedFingerPos 上，跳过 DOMove）。</summary>
        private const float ReboundDistanceEpsilon = 0.001f;

        /// <summary>相机可见 gameplay 平面内缩（世界单位），避免物件贴屏幕边缘后 collider 仍不可点。</summary>
        private const float VisibleBoundsInsetWorld = 0.15f;

        // ----------------------------------------------------------------- Lifecycle

        /// <summary>
        /// 显式生命周期入口（S2-09 X1 patch v3）。幂等：重复调用直接 return。
        /// <para>生产路径：由 <c>OnEnable</c> 自动委托。</para>
        /// <para>EditMode 测试路径：测试中 <c>OnEnable</c> 不被 PlayerLoop 自动驱动，须显式调用本方法
        /// （与 <c>SceneManager.Init()</c> 同模式）。</para>
        /// </summary>
        public void Initialize()
        {
            if (Fsm != null) return;   // 幂等

            Fsm = new InteractableObjectFsm(_objectId);
            Fsm.StateChanged += OnFsmStateChanged;

            // fail-loud 仅 Log.Error，不调 enabled=false（避免 OnDisable 反射性清 Fsm 影响测试可观测性）。
            // 生产路径：TickDrag 因 _puzzleConfig 或 _gameplayCamera 为 null 而早期 return，等效"drag 不工作"。
            ResolvePuzzleConfig();
            if (_gameplayCamera == null)
            {
                Log.Error($"[InteractableObject#{_objectId}] _gameplayCamera 未配置 — drag 不可用（ADR-012 禁止 Camera.main fallback）");
            }

            GameEvent.AddEventListener<bool>(
                IInteractionEvent_Event.OnInteractionLockChanged, OnInteractionLockChanged);
            GameEvent.AddEventListener<GestureData>(
                IGestureEvent_Event.OnDrag, OnDrag);
            _listenersRegistered = true;
        }

        /// <summary>
        /// 显式生命周期收尾入口（S2-09 X1 patch v3）。幂等：未初始化时直接 return。
        /// <para>生产路径：由 <c>OnDisable</c> 自动委托。</para>
        /// <para>EditMode 测试路径：测试中 <c>OnDisable</c> 不被 PlayerLoop 自动驱动，须显式调用本方法
        /// （或 <c>Object.DestroyImmediate(go)</c> 间接触发）。</para>
        /// </summary>
        public void Shutdown()
        {
            if (_listenersRegistered)
            {
                GameEvent.RemoveEventListener<bool>(
                    IInteractionEvent_Event.OnInteractionLockChanged, OnInteractionLockChanged);
                GameEvent.RemoveEventListener<GestureData>(
                    IGestureEvent_Event.OnDrag, OnDrag);
                _listenersRegistered = false;
            }

            // race-fallback rebound 进行中 → 强制中断（避免悬挂 OnComplete 在已 disabled 的实例上回调）
            if (IsReboundActive)
            {
                transform.DOKill(complete: false);
                IsReboundActive = false;
            }

            if (Fsm != null)
            {
                Fsm.StateChanged -= OnFsmStateChanged;
                Fsm = null;
            }

            _hasFreshDrag = false;
            _didStartSnap = false;
        }

        // ----------------------------------------------------------------- Unity lifecycle (delegate to Initialize/Shutdown)

        private void OnEnable() => Initialize();

        private void OnDisable() => Shutdown();

        /// <summary>
        /// 主循环路由（X1 patch v3 + S2-10）：
        /// <list type="bullet">
        /// <item><c>Dragging</c> → <see cref="TickDrag"/>（S2-09 1:1 跟手指 + 边界 clamp）</item>
        /// <item><c>Snapping</c> → <see cref="StartSnap"/>（S2-10 grid snap；首帧一次性，IsReboundActive 时推迟）</item>
        /// <item>其他 → reset <see cref="_didStartSnap"/></item>
        /// </list>
        /// </summary>
        private void Update()
        {
            if (Fsm == null) return;
            switch (Fsm.CurrentState)
            {
                case InteractableObjectState.Dragging:
                    TickDrag();
                    break;
                case InteractableObjectState.Snapping:
                    if (IsReboundActive) break;       // S2-09 race-fallback rebound 优先；rebound OnComplete 后下一帧 Update 自然接管
                    if (!_didStartSnap) StartSnap();
                    break;
                default:
                    _didStartSnap = false;
                    break;
            }
        }

        // ----------------------------------------------------------------- Internal API (testability)

        /// <summary>
        /// 主 drag tick 逻辑。生产由 <see cref="Update"/> 每帧调用；EditMode 测试可直接 invoke 验证 clamp 数学。
        /// public 而非 private —— asmdef 间 internal 不可见，沿用 S2-05 SceneManager.AdvanceStateForTest 先例。
        /// </summary>
        public void TickDrag()
        {
            if (Fsm == null) return;
            if (Fsm.CurrentState != InteractableObjectState.Dragging) return;
            if (!_hasFreshDrag) return;
            if (_gameplayCamera == null) return;
            if (_puzzleConfig == null) return;

            float planeZ = transform.position.z;
            if (!GameplayScreenProjection.TryScreenToWorldOnPlane(
                    _gameplayCamera, _lastDragScreenPos, planeZ, out var worldPos))
                return;

            if (!_dragGrabOffsetValid)
            {
                _dragGrabOffset = transform.position - worldPos;
                _dragGrabOffsetValid = true;
            }

            worldPos += _dragGrabOffset;
            LastWorldPosUnclamped = worldPos;

            var bounds = GetEffectiveInteractionBounds();
            var pos = transform.position;
            pos.x = Mathf.Clamp(worldPos.x, bounds.MinX, bounds.MaxX);
            pos.y = Mathf.Clamp(worldPos.y, bounds.MinY, bounds.MaxY);
            transform.position = pos;

            _hasFreshDrag = false;
        }

        /// <summary>测试专用：读取 Luban bounds ∩ 相机可见 gameplay 区域。</summary>
        public InteractionBounds GetEffectiveInteractionBoundsForTest() => GetEffectiveInteractionBounds();

        private InteractionBounds GetEffectiveInteractionBounds()
        {
            var configBounds = _puzzleConfig.InteractionBounds;
            if (_gameplayCamera == null) return configBounds;

            if (!GameplayScreenProjection.TryComputeVisibleBoundsOnZPlane(
                    _gameplayCamera, transform.position.z, VisibleBoundsInsetWorld, out var visibleBounds))
                return configBounds;

            var merged = InteractionBounds.Intersect(configBounds, visibleBounds);
            return merged.IsValid ? merged : configBounds;
        }

        /// <summary>
        /// 测试专用：直接注入 <c>_lastWorldPosUnclamped</c>（模拟 race condition：finger 在 bounds 外但 transform 还未及时 clamp 到边界）。
        /// 生产代码通过 <see cref="TickDrag"/> 路径自然写入。
        /// public 而非 internal —— asmdef 间 internal 不可见，沿用 S2-05 ForTest 先例。
        /// </summary>
        public void SetLastWorldPosUnclampedForTest(Vector3 worldPos) => LastWorldPosUnclamped = worldPos;

        // ----------------------------------------------------------------- Listeners

        /// <summary><see cref="IInteractionEvent.OnInteractionLockChanged"/> per-method handler — 转发到 fsm 转换规则 7/8。</summary>
        private void OnInteractionLockChanged(bool isLocked) => Fsm?.OnLockChanged(isLocked);

        /// <summary>
        /// <see cref="IGestureEvent.OnDrag"/> per-method handler（S2-09 push 模式）。
        /// 早期 return 守卫：fsm == null / 非 Dragging 状态 / Phase ∉ {Began, Updated} 立即 return；
        /// 否则缓存 ScreenPos，<see cref="TickDrag"/> 下一帧消费。
        /// </summary>
        private void OnDrag(GestureData data)
        {
            if (Fsm == null) return;
            if (Fsm.CurrentState != InteractableObjectState.Dragging) return;
            if (data.Phase != GesturePhase.Updated && data.Phase != GesturePhase.Began) return;

            if (data.Phase == GesturePhase.Began)
                _dragGrabOffsetValid = false;

            _lastDragScreenPos = data.ScreenPosition;
            _hasFreshDrag = true;
        }

        /// <summary>
        /// fsm.StateChanged 订阅（S2-09 + S2-10 合并）：
        /// <list type="bullet">
        /// <item><c>Dragging → Snapping</c> → <see cref="TryStartRebound"/>（race-fallback rebound 检测）</item>
        /// <item><c>Snapping → 非 Idle</c> → <see cref="Transform.DOKill"/>(complete: false)（snap 中断；object 停在中间帧；规则 6/7）</item>
        /// </list>
        /// <para><c>Snapping → Idle</c> 是自然完成路径（DOTween OnComplete 已调过 <see cref="OnSnapComplete"/>）—
        /// 此处不 DOKill 避免对已 complete 的 tween 多余操作（DOTween 内部已自动清理）。</para>
        /// </summary>
        private void OnFsmStateChanged(InteractableObjectState prev, InteractableObjectState next)
        {
            if (prev == InteractableObjectState.Dragging)
                _dragGrabOffsetValid = false;

            if (prev == InteractableObjectState.Dragging && next == InteractableObjectState.Snapping)
            {
                TryStartRebound();
            }

            // S2-10 中断处理：snap 进行中，fsm 被外部触发离开 Snapping（OnTapHit→Selected / OnLockChanged(true)→Locked）
            if (prev == InteractableObjectState.Snapping && next != InteractableObjectState.Idle)
            {
                transform.DOKill(complete: false);
                _didStartSnap = false;
                // 落点未定 — 不派发 IInteractionEvent.OnObjectTransformChanged（AC-6 / AC-7）
            }
        }

        // ----------------------------------------------------------------- Rebound

        /// <summary>
        /// Race-fallback rebound：当 finger 最后一次 worldPos 在 bounds 外且 transform 当前位置距 clampedFingerPos &gt; epsilon
        /// 时，启动 0.25s OutBack DOMove。生产主路径上 <see cref="TickDrag"/> 每帧 clamp 已使两位置重合（距离退化为 0），
        /// 此分支不触发；保留作为多帧 skip / 时序竞争的 fallback。
        /// </summary>
        private void TryStartRebound()
        {
            if (_puzzleConfig == null) return;
            var b = GetEffectiveInteractionBounds();
            bool fingerOutOfBounds =
                LastWorldPosUnclamped.x < b.MinX || LastWorldPosUnclamped.x > b.MaxX ||
                LastWorldPosUnclamped.y < b.MinY || LastWorldPosUnclamped.y > b.MaxY;
            if (!fingerOutOfBounds) return;

            var pos = transform.position;
            var clamped = new Vector3(
                Mathf.Clamp(LastWorldPosUnclamped.x, b.MinX, b.MaxX),
                Mathf.Clamp(LastWorldPosUnclamped.y, b.MinY, b.MaxY),
                pos.z);
            if (Vector3.Distance(pos, clamped) < ReboundDistanceEpsilon) return;

            IsReboundActive = true;
            transform.DOMove(clamped, ReboundDurationSeconds)
                .SetEase(DG.Tweening.Ease.OutBack)
                .OnComplete(() => IsReboundActive = false);
        }

        // ----------------------------------------------------------------- Grid Snap (S2-10)

        /// <summary>位置已在格点的容差（<c>Vector3.Distance &lt; SnapDistanceEpsilon</c> 视为已 snap）。</summary>
        private const float SnapDistanceEpsilon = 0.001f;

        /// <summary>
        /// 启动 grid snap（ADR-013 §"Grid Snap Mechanics" + 转换规则 5）。一次性：<see cref="_didStartSnap"/> = true 后等
        /// fsm 离开 Snapping 时由 <see cref="OnFsmStateChanged"/> 重置。
        /// <para>公式：<c>snappedPos = round(rawPos / gridSize) * gridSize</c>（X/Y 各算一次；Z 保持）；
        /// snap 后 <see cref="InteractionBounds.Clamp"/> 至 bounds（避免落到 bounds 外的格点）。</para>
        /// <para>已在格点（距离 &lt; <see cref="SnapDistanceEpsilon"/>）→ 跳过 DOMove，同步调 <see cref="OnSnapComplete"/>
        /// （AC-4 等价路径；亦避免 DOTween 0-time tween 的特殊行为）。</para>
        /// </summary>
        public void StartSnap()
        {
            if (_didStartSnap) return;
            if (_puzzleConfig == null)
            {
                Log.Error($"[InteractableObject#{_objectId}] StartSnap 时 _puzzleConfig 为 null — 无法计算格点");
                return;
            }

            _didStartSnap = true;

            float gs = _puzzleConfig.GridSize;
            var b = GetEffectiveInteractionBounds();
            var pos = transform.position;
            float snappedX = Mathf.Round(pos.x / gs) * gs;
            float snappedY = Mathf.Round(pos.y / gs) * gs;
            // 后置 clamp：snap 公式可能算出 bounds 外格点（例：finger 在 MaxX=3 边附近 release，rawX=3.4→snap 4.0→clamp 3.0）
            snappedX = Mathf.Clamp(snappedX, b.MinX, b.MaxX);
            snappedY = Mathf.Clamp(snappedY, b.MinY, b.MaxY);

            var snappedPos = new Vector3(snappedX, snappedY, pos.z);

            if (Vector3.Distance(pos, snappedPos) < SnapDistanceEpsilon)
            {
                // AC-4：已在格点 → 跳 tween，同步走 OnComplete 路径（fsm.OnSnapCompleted + 派 OnObjectTransformChanged）
                OnSnapComplete();
                return;
            }

            transform.DOMove(snappedPos, _puzzleConfig.SnapSpeed)
                .SetEase(DG.Tweening.Ease.OutQuad)
                .OnComplete(OnSnapComplete);
        }

        /// <summary>
        /// Grid snap 自然完成回调。先 <c>fsm.OnSnapCompleted()</c>（Snapping→Idle）再派
        /// <see cref="IInteractionEvent.OnObjectTransformChanged"/>（最终 transform）。
        /// <para>派发顺序：fsm transition 在前，事件在后 — 与 S2-08 OnObjectSelected 同模式
        /// （fsm 是 transform 状态权威）。</para>
        /// <para>中断路径（OnTapHit/OnLockChanged）**不**走本方法 — 由 <see cref="OnFsmStateChanged"/>
        /// Snapping→非 Idle 分支 DOKill；OnObjectTransformChanged 不派发（落点未定）。</para>
        /// </summary>
        private void OnSnapComplete()
        {
            if (Fsm == null) return;   // OnDisable 已清；防御性
            Fsm.OnSnapCompleted();
            GameEvent.Get<IInteractionEvent>()
                .OnObjectTransformChanged(Fsm?.ObjectId ?? _objectId, transform.position, transform.rotation);
        }

        // ----------------------------------------------------------------- Rotation (S2-11)

        /// <summary>
        /// 单指旋转手势处理入口（由 <c>InteractionCoordinator</c> 转发，不订阅 IGestureEvent.OnRotate 自身）。
        /// <list type="bullet">
        /// <item><c>fsm.CurrentState != Selected &amp;&amp; != Dragging</c> → 静默忽略（Locked / Idle / Snapping）</item>
        /// <item><c>Phase == Began</c> → 累加器从 <c>transform.eulerAngles.z</c> 重新加载，再叠 delta；写 transform</item>
        /// <item><c>Phase == Updated</c> → 累加 delta；写 transform</item>
        /// <item><c>Phase == Ended || Cancelled</c> → 调 <see cref="SnapRotation"/></item>
        /// </list>
        /// <para><b>delta 单位</b>：<c>data.AngleDelta</c> 是弧度/帧（参 IGestureEvent.cs 注释），本方法转度后累加到累加器。</para>
        /// <para><b>累加器策略</b>：跨 360°/0° 边界时 <c>transform.eulerAngles.z</c> 会 wrap 到 [0, 360)，但本累加器持续累加（可超 360 / 负），
        /// 写回 transform 时由 Unity 内部归一化；snap 阶段对累加器自身做 mod 360 归一化保证最终落点合法。</para>
        /// </summary>
        public void HandleRotate(GestureData data)
        {
            if (Fsm == null) return;
            var st = Fsm.CurrentState;
            if (st != InteractableObjectState.Selected && st != InteractableObjectState.Dragging) return;

            switch (data.Phase)
            {
                case GesturePhase.Began:
                    AccumulatedAngleDeg = transform.eulerAngles.z + data.AngleDelta * Mathf.Rad2Deg;
                    transform.eulerAngles = new Vector3(0f, 0f, AccumulatedAngleDeg);
                    break;
                case GesturePhase.Updated:
                    AccumulatedAngleDeg += data.AngleDelta * Mathf.Rad2Deg;
                    transform.eulerAngles = new Vector3(0f, 0f, AccumulatedAngleDeg);
                    break;
                case GesturePhase.Ended:
                case GesturePhase.Cancelled:
                    SnapRotation();
                    break;
            }
        }

        /// <summary>
        /// 旋转 snap 启动（Ended / Cancelled phase 触发）。
        /// <para>公式：<c>snapped = (round(_accumulatedAngleDeg / RotationStep) * RotationStep) mod 360</c>，归一化 [0, 360)。</para>
        /// <para>已在格点（与归一化 transform.z 距离 &lt; <see cref="RotationSnapEpsilonDeg"/>）→ 跳过 DOTween，同步走 OnComplete 路径
        /// （AC-3 Edge：当前已是 step 倍数，仍派 OnObjectTransformChanged 一次保证下游一致）。</para>
        /// <para>OnComplete：<c>AccumulatedAngleDeg = snapped</c> + 派 <c>IInteractionEvent.OnObjectTransformChanged</c>。</para>
        /// <para>public 而非 private — asmdef 间 internal 不可见，测试可直接调 verify snap 行为。</para>
        /// </summary>
        public void SnapRotation()
        {
            if (Fsm == null) return;
            if (_puzzleConfig == null)
            {
                Log.Error($"[InteractableObject#{_objectId}] SnapRotation 时 _puzzleConfig 为 null — 无法计算 RotationStep");
                return;
            }

            float step = _puzzleConfig.RotationStep;
            float raw = AccumulatedAngleDeg;
            float snapped = Mathf.Round(raw / step) * step;
            snapped = (snapped % 360f + 360f) % 360f;   // 归一化 [0, 360)

            // 已在格点（注意：transform.eulerAngles.z 是归一化后的 [0, 360)；与 snapped 比较）
            float currentNormalized = (transform.eulerAngles.z % 360f + 360f) % 360f;
            float diff = Mathf.Abs(Mathf.DeltaAngle(currentNormalized, snapped));
            if (diff < RotationSnapEpsilonDeg)
            {
                AccumulatedAngleDeg = snapped;
                transform.eulerAngles = new Vector3(0f, 0f, snapped);
                GameEvent.Get<IInteractionEvent>()
                    .OnObjectTransformChanged(Fsm?.ObjectId ?? _objectId, transform.position, transform.rotation);
                return;
            }

            transform.DORotate(new Vector3(0f, 0f, snapped), _puzzleConfig.SnapSpeed)
                .SetEase(DG.Tweening.Ease.OutQuad)
                .OnComplete(() =>
                {
                    AccumulatedAngleDeg = snapped;
                    if (Fsm == null) return;   // OnDisable 已清
                    GameEvent.Get<IInteractionEvent>()
                        .OnObjectTransformChanged(Fsm.ObjectId, transform.position, transform.rotation);
                });
        }

        /// <summary>角度 snap 容差（度；<c>Mathf.Abs(currentAngle - snappedAngle) &lt; epsilon</c> 视为已 snap）。</summary>
        private const float RotationSnapEpsilonDeg = 0.01f;

        // ----------------------------------------------------------------- Provider resolution

        /// <summary>
        /// 通过 <see cref="_puzzleConfigProvider"/> 解析 <see cref="_puzzleConfig"/>，fail-loud。
        /// </summary>
        /// <returns>true=解析成功；false=provider 未注册或返 null（已 Log.Error）</returns>
        private bool ResolvePuzzleConfig()
        {
            if (_puzzleConfigProvider == null)
            {
                Log.Error($"[InteractableObject#{_objectId}] PuzzleConfigProvider 未注册 — 调用 InteractableObject.RegisterPuzzleConfigProvider(Func<int, PuzzleConfig>) 后再启用本组件");
                return false;
            }
            _puzzleConfig = _puzzleConfigProvider(_puzzleId);
            if (_puzzleConfig == null)
            {
                Log.Error($"[InteractableObject#{_objectId}] PuzzleConfigProvider 对 puzzleId={_puzzleId} 返回 null");
                return false;
            }
            return true;
        }
    }
}
