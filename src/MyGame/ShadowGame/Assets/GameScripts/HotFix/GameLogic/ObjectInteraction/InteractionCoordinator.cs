// 该文件由Cursor 自动生成
using System;
using System.Collections.Generic;
using TEngine;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 章节场景级 InteractableObject 管理器（S2-13）。
    /// </summary>
    /// <remarks>
    /// <para><b>职责</b>（参 ADR-013 §"Architecture" + ADR-027 + SP-006）：
    /// <list type="number">
    /// <item>持有当前 chapter scene 的 <see cref="InteractableObject"/> 列表（Inspector 预填，<b>禁</b> FindObjectsOfType）</item>
    /// <item>实例化 + 生命周期管理 <see cref="InteractionLockManager"/>（S2-12）</item>
    /// <item>订阅 <see cref="IGestureEvent.OnTap"/> 做 Raycast 选取 + fat finger compensation + 200ms debounce + 单选切换</item>
    /// <item>订阅 <see cref="IGestureEvent.OnDrag"/> 转发 Began/Ended 到当前选中 object 的 fsm；Updated phase 由 <see cref="InteractableObject"/> 自身消费</item>
    /// <item>订阅 <see cref="IGestureEvent.OnRotate"/>（S2-11）转发**全部 phase**到当前选中 object 的 <see cref="InteractableObject.HandleRotate"/>（持续旋转 + Ended snap 由 InteractableObject 内部消费）</item>
    /// <item>订阅 <see cref="IInteractionEvent.OnInteractionLockChanged"/>：isLocked=true 时清自己持有的 <see cref="CurrentSelectedObject"/> 引用（避免悬挂指针；fsm 自身已 → Locked 由 InteractableObject 自订阅处理）</item>
    /// </list></para>
    ///
    /// <para><b>非职责</b>：
    /// <list type="bullet">
    /// <item>fsm.OnTapHit / OnDeselect 内部已派发 <see cref="IInteractionEvent.OnObjectSelected"/> / <see cref="IInteractionEvent.OnObjectDeselected"/> — Coordinator <b>不</b>重复派发</item>
    /// <item>fsm.OnLockChanged(true) 在 from==Selected 时已派发 OnObjectDeselected — Coordinator <b>不</b>重复派发</item>
    /// <item>Drag Updated phase 的 ScreenPos 缓存 — 由 InteractableObject.OnDrag 直接消费（push 模式，S2-09）</item>
    /// </list></para>
    ///
    /// <para><b>注入约定</b>：<see cref="RegisterInputConfigProvider"/> 静态注入 <see cref="IInputConfig"/>（与
    /// <see cref="InteractableObject.RegisterPuzzleConfigProvider"/> 同模式）。生产 boot pipeline 在 Coordinator
    /// <c>OnEnable</c> 之前注入；EditMode 测试中由 fixture <c>SetUp</c> 注入 stub 后再 <c>Initialize()</c>。
    /// fail-loud：未注册或 provider 返 null → <c>Log.Error</c> + 走 InputConfigFromLuban.InitWithDefaults fallback
    /// （同 InteractableObject 的 ResolvePuzzleConfig fail-loud 风格）。</para>
    ///
    /// <para><b>Lifecycle hooks</b>：<see cref="Initialize"/> / <see cref="Shutdown"/> 是显式生命周期入口，幂等。
    /// 生产路径由 Unity 的 <c>OnEnable</c> / <c>OnDisable</c> 自动委托；EditMode 测试中 OnEnable 不被
    /// PlayerLoop 自动驱动，须显式 <c>coord.Initialize()</c>（与 InteractableObject 同模式）。</para>
    ///
    /// <para><b>200ms debounce</b>：用 <see cref="Time.unscaledTime"/>（避开 timeScale 缩放，pause 菜单不影响）。
    /// 距离上次成功选取 &lt; 0.2s 的 Tap 整体忽略（A 保持选中，B 不被选；listener 不收到任何派发）。</para>
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class InteractionCoordinator : MonoBehaviour
    {
        // ----------------------------------------------------------------- Inspector

        [Tooltip("场景中所有 InteractableObject 列表（设计师在 Inspector 预填；禁 FindObjectsOfType 运行时查找）")]
        [SerializeField] private List<InteractableObject> _objects = new List<InteractableObject>();

        [Tooltip("Tap raycast 仅命中此 LayerMask（Interactable layer）；防止误选 UI / 背景")]
        [SerializeField] private LayerMask _interactableLayer;

        [Tooltip("游戏相机引用（Inspector 拖入；ADR-012 禁止 Camera.main fallback）")]
        [SerializeField] private Camera _gameplayCamera;

        // ----------------------------------------------------------------- Static provider

        private static Func<IInputConfig> _inputConfigProvider;

        /// <summary>
        /// 注入 IInputConfig provider（与 <see cref="InteractableObject.RegisterPuzzleConfigProvider"/> 同模式）。
        /// 通常由 boot pipeline 在 Coordinator OnEnable 之前调用一次。
        /// </summary>
        public static void RegisterInputConfigProvider(Func<IInputConfig> provider)
        {
            _inputConfigProvider = provider;
        }

        /// <summary>测试专用：清空 provider，避免跨用例污染。</summary>
        public static void ClearInputConfigProviderForTest() => _inputConfigProvider = null;

        // ----------------------------------------------------------------- Public state

        /// <summary>当前唯一选中的 InteractableObject（Selected 或 Dragging 状态）。可为 null。</summary>
        public InteractableObject CurrentSelectedObject { get; private set; }

        /// <summary>
        /// 透传 LockManager.IsLocked。<see cref="Initialize"/> 之前为 false。
        /// </summary>
        public bool IsLocked => _lockManager?.IsLocked ?? false;

        /// <summary>底层 LockManager 实例（S2-12）；测试可读但不应外部修改。</summary>
        public InteractionLockManager LockManager => _lockManager;

        // ----------------------------------------------------------------- Constants

        /// <summary>选取 debounce 时长（秒；ADR-013）。</summary>
        public const float DebounceSeconds = 0.2f;

        /// <summary>1 inch = 25.4 mm（fat finger 半径换算）。</summary>
        private const float MmPerInch = 25.4f;

        // ----------------------------------------------------------------- Private state

        private InteractionLockManager _lockManager;
        private IInputConfig _inputConfig;
        private float _lastSelectionTime = -999f;
        private bool _listenersRegistered;

        // ----------------------------------------------------------------- Lifecycle

        /// <summary>
        /// 显式生命周期入口。幂等：重复调用直接 return。
        /// <para>生产路径：由 <c>OnEnable</c> 自动委托。</para>
        /// <para>EditMode 测试路径：测试中 <c>OnEnable</c> 不被 PlayerLoop 自动驱动，须显式调用本方法。</para>
        /// </summary>
        public void Initialize()
        {
            if (_lockManager != null) return;   // 幂等

            _lockManager = new InteractionLockManager();
            _lockManager.Init();

            ResolveInputConfig();

            // _objects 校验：null 元素 → Log.Error 但不抛（保留其他元素可用）
            for (int i = 0; i < _objects.Count; i++)
            {
                if (_objects[i] == null)
                {
                    Log.Error($"[InteractionCoordinator] _objects[{i}] is null — 跳过该项；请在 Inspector 检查");
                }
            }

            if (_gameplayCamera == null)
            {
                Log.Error("[InteractionCoordinator] _gameplayCamera 未配置 — Tap raycast 不可用（ADR-012 禁止 Camera.main fallback）");
            }

            GameEvent.AddEventListener<GestureData>(IGestureEvent_Event.OnTap, OnTap);
            GameEvent.AddEventListener<GestureData>(IGestureEvent_Event.OnDrag, OnDrag);
            GameEvent.AddEventListener<GestureData>(IGestureEvent_Event.OnRotate, OnRotate);
            GameEvent.AddEventListener<bool>(
                IInteractionEvent_Event.OnInteractionLockChanged, OnInteractionLockChanged);
            GameEvent.AddEventListener<int, Vector3, Quaternion>(
                IInteractionEvent_Event.OnObjectTransformChanged, OnObjectTransformChanged);
            _listenersRegistered = true;
        }

        /// <summary>
        /// 显式生命周期收尾入口。幂等：未初始化时直接 return。
        /// </summary>
        public void Shutdown()
        {
            if (_listenersRegistered)
            {
                GameEvent.RemoveEventListener<GestureData>(IGestureEvent_Event.OnTap, OnTap);
                GameEvent.RemoveEventListener<GestureData>(IGestureEvent_Event.OnDrag, OnDrag);
                GameEvent.RemoveEventListener<GestureData>(IGestureEvent_Event.OnRotate, OnRotate);
                GameEvent.RemoveEventListener<bool>(
                    IInteractionEvent_Event.OnInteractionLockChanged, OnInteractionLockChanged);
                GameEvent.RemoveEventListener<int, Vector3, Quaternion>(
                    IInteractionEvent_Event.OnObjectTransformChanged, OnObjectTransformChanged);
                _listenersRegistered = false;
            }

            _lockManager?.Dispose();
            _lockManager = null;
            CurrentSelectedObject = null;
        }

        private void OnEnable() => Initialize();
        private void OnDisable() => Shutdown();

        // ----------------------------------------------------------------- Public API（供测试 bypass listener / Inspector workflow）

        /// <summary>
        /// 单选切换决策（公开供测试 bypass raycast）。
        /// <list type="bullet">
        /// <item><c>hit == null</c>：deselect 当前；clear <see cref="CurrentSelectedObject"/></item>
        /// <item><c>hit == CurrentSelectedObject</c>：若 fsm 已 Idle（drag→snap 落定后）则允许再选；否则 no-op</item>
        /// <item>其他：deselect 旧（如有）；select 新；更新 _lastSelectionTime</item>
        /// </list>
        /// <para><b>200ms debounce</b>：距离上次 _lastSelectionTime &lt; <see cref="DebounceSeconds"/> → 整体忽略，返 false。</para>
        /// <para><b>Locked 状态</b>：返 false（不切换；锁定时整个交互系统应不响应 Tap）。</para>
        /// </summary>
        /// <param name="hit">命中的 InteractableObject（可为 null = 命中空白）</param>
        /// <returns>true = 选取状态发生变化（含切换 / 取消选）；false = 被 debounce / locked / 无变化拒绝</returns>
        public bool TrySelectObject(InteractableObject hit)
        {
            if (IsLocked) return false;
            if (Time.unscaledTime - _lastSelectionTime < DebounceSeconds) return false;

            if (hit == null)
            {
                // 命中空白 → deselect 当前
                if (CurrentSelectedObject == null) return false;
                if (CurrentSelectedObject.Fsm != null &&
                    CurrentSelectedObject.Fsm.CurrentState == InteractableObjectState.Selected)
                {
                    CurrentSelectedObject.Fsm.OnDeselect();   // fsm 内部派 OnObjectDeselected
                }
                CurrentSelectedObject = null;
                _lastSelectionTime = Time.unscaledTime;
                return true;
            }

            if (hit == CurrentSelectedObject)
            {
                if (hit.Fsm != null && hit.Fsm.CurrentState == InteractableObjectState.Idle)
                {
                    hit.Fsm.OnTapHit();
                    _lastSelectionTime = Time.unscaledTime;
                    return true;
                }

                return false;
            }

            // 切换：deselect 旧（仅当 fsm 在 Selected；Dragging 不允许 Tap 切换 — 由 fsm.OnDeselect 守卫）
            if (CurrentSelectedObject != null && CurrentSelectedObject.Fsm != null &&
                CurrentSelectedObject.Fsm.CurrentState == InteractableObjectState.Selected)
            {
                CurrentSelectedObject.Fsm.OnDeselect();
            }

            CurrentSelectedObject = hit;
            if (hit.Fsm != null) hit.Fsm.OnTapHit();   // fsm 内部派 OnObjectSelected
            _lastSelectionTime = Time.unscaledTime;
            return true;
        }

        // ----------------------------------------------------------------- Listeners

        /// <summary>
        /// <see cref="IGestureEvent.OnTap"/> per-method handler — 走 Raycast → <see cref="TrySelectObject"/>。
        /// </summary>
        private void OnTap(GestureData data)
        {
            if (data.Phase != GesturePhase.Ended) return;   // Tap 仅在 Ended 帧消费
            if (_gameplayCamera == null) return;

            var hit = RaycastWithFatFinger(data.ScreenPosition);
            TrySelectObject(hit);
        }

        /// <summary>
        /// <see cref="IGestureEvent.OnDrag"/> per-method handler — 转发 Began/Ended 到当前选中 fsm；
        /// Updated phase 由 InteractableObject 自身消费（push 模式，S2-09）。
        /// </summary>
        private void OnDrag(GestureData data)
        {
            if (CurrentSelectedObject == null) return;
            if (CurrentSelectedObject.Fsm == null) return;

            switch (data.Phase)
            {
                case GesturePhase.Began:
                    CurrentSelectedObject.Fsm.OnDragBegan();
                    break;
                case GesturePhase.Ended:
                    CurrentSelectedObject.Fsm.OnDragEnded();
                    break;
                // Updated / Cancelled — 不在本 listener 处理
            }
        }

        /// <summary>
        /// <see cref="IGestureEvent.OnRotate"/> per-method handler — 转发**全部 phase**到当前选中 object 的
        /// <see cref="InteractableObject.HandleRotate"/>（S2-11）。
        /// <para>InteractableObject.HandleRotate 内部会守卫 fsm.CurrentState（仅 Selected/Dragging 响应；
        /// Locked 时静默忽略）+ 处理 Began/Updated/Ended/Cancelled 全 phase。</para>
        /// </summary>
        private void OnRotate(GestureData data)
        {
            if (CurrentSelectedObject == null) return;
            CurrentSelectedObject.HandleRotate(data);
        }

        /// <summary>
        /// <see cref="IInteractionEvent.OnInteractionLockChanged"/> per-method handler。
        /// <list type="bullet">
        /// <item><c>isLocked == true</c>：清 <see cref="CurrentSelectedObject"/> 引用（fsm 自身已被 InteractableObject 自订阅 listener 转 Locked + 派 OnObjectDeselected if from Selected）</item>
        /// <item><c>isLocked == false</c>：no-op（解锁后下次 Tap 重选）</item>
        /// </list>
        /// </summary>
        private void OnInteractionLockChanged(bool isLocked)
        {
            if (isLocked)
            {
                CurrentSelectedObject = null;
            }
        }

        /// <summary>
        /// snap / rotation 落定后 <see cref="InteractableObject"/> 派
        /// <see cref="IInteractionEvent.OnObjectTransformChanged"/> 且 fsm 已 Idle — 释放 Coordinator 悬挂选中引用，
        /// 避免「同物件再点」被 <c>hit == CurrentSelectedObject</c> 误判为 no-op。
        /// </summary>
        private void OnObjectTransformChanged(int objectId, Vector3 position, Quaternion rotation)
        {
            if (CurrentSelectedObject == null) return;
            if (CurrentSelectedObject.Fsm == null || CurrentSelectedObject.Fsm.ObjectId != objectId) return;
            if (CurrentSelectedObject.Fsm.CurrentState != InteractableObjectState.Idle) return;

            CurrentSelectedObject = null;
        }

        // ----------------------------------------------------------------- Raycast

        /// <summary>
        /// Tap raycast + fat finger compensation。返 null = 命中空白。
        /// <para>实现：<see cref="Physics.Raycast"/> / <see cref="Physics.SphereCast"/> 命中父级
        /// <see cref="BoxCollider"/>（与 3D mesh 视觉对齐；chapter 1 透视相机场景）。
        /// 2D <c>Hitbox2D</c> 仅作 Sprint 6 同存解遗留，Tap 不再走 Physics2D。</para>
        /// <para><b>fat finger 公式</b>：<c>radiusPx = FatFingerMarginMm * dpi / 25.4</c>；转 world：
        /// <c>radiusWorld = radiusPx * worldUnitsPerPixel</c>（水平面 Y≈0.5 上差分估算）。</para>
        /// <para>public 而非 private — asmdef 间 internal 不可见，沿用 InteractableObject.TickDrag 先例供测试调用。</para>
        /// </summary>
        public InteractableObject RaycastWithFatFinger(Vector2 screenPos)
        {
            if (_gameplayCamera == null) return null;
            if (_inputConfig == null) return null;

            var ray = _gameplayCamera.ScreenPointToRay(screenPos);
            const float maxDistance = 100f;
            int layerMask = _interactableLayer.value;

            if (Physics.Raycast(ray, out RaycastHit hit, maxDistance, layerMask))
            {
                var direct = ResolveInteractableFromCollider(hit.collider);
                if (direct != null) return direct;
            }

            float dpi = Screen.dpi > 0f ? Screen.dpi : _inputConfig.FallbackDpi;
            float radiusPx = _inputConfig.FatFingerMarginMm * dpi / MmPerInch;
            const float fatFingerPlaneY = 0f;
            float worldPerPx = GameplayScreenProjection.GetWorldUnitsPerScreenPixelOnHorizontalPlane(
                _gameplayCamera, screenPos, fatFingerPlaneY);
            float radiusWorld = radiusPx * worldPerPx;

            if (radiusWorld > 0.001f
                && Physics.SphereCast(ray, radiusWorld, out hit, maxDistance, layerMask))
            {
                return ResolveInteractableFromCollider(hit.collider);
            }

            return null;
        }

        private InteractableObject ResolveInteractableFromCollider(Collider collider)
        {
            if (collider == null) return null;
            var io = collider.GetComponentInParent<InteractableObject>();
            if (io == null) return null;
            if (!_objects.Contains(io)) return null;
            return io;
        }

        // ----------------------------------------------------------------- Provider resolution

        /// <summary>
        /// 通过 <see cref="_inputConfigProvider"/> 解析 <see cref="_inputConfig"/>，fail-loud 用 InitWithDefaults fallback。
        /// </summary>
        private void ResolveInputConfig()
        {
            if (_inputConfigProvider == null)
            {
                Log.Error("[InteractionCoordinator] InputConfigProvider 未注册 — 调 InteractionCoordinator.RegisterInputConfigProvider(Func<IInputConfig>) 后再启用本组件；本次回退到 InputConfigFromLuban.InitWithDefaults");
                var fallback = new InputConfigFromLuban();
                fallback.InitWithDefaults();
                _inputConfig = fallback;
                return;
            }
            _inputConfig = _inputConfigProvider();
            if (_inputConfig == null)
            {
                Log.Error("[InteractionCoordinator] InputConfigProvider 返回 null — 回退到 InputConfigFromLuban.InitWithDefaults");
                var fallback = new InputConfigFromLuban();
                fallback.InitWithDefaults();
                _inputConfig = fallback;
            }
        }

        // ----------------------------------------------------------------- Test inspection

        /// <summary>测试专用：注入 _objects 列表。</summary>
        public void SetObjectsForTest(List<InteractableObject> objects) => _objects = objects ?? new List<InteractableObject>();

        /// <summary>测试专用：注入 _gameplayCamera。</summary>
        public void SetGameplayCameraForTest(Camera cam) => _gameplayCamera = cam;

        /// <summary>测试专用：读 _lastSelectionTime（验证 debounce 逻辑）。</summary>
        public float LastSelectionTime => _lastSelectionTime;

        /// <summary>测试专用：手动重置 _lastSelectionTime（避开 Time.unscaledTime 不可控问题）。</summary>
        public void ResetDebounceForTest() => _lastSelectionTime = -999f;
    }
}
