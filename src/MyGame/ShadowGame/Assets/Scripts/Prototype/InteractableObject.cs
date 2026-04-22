// 该文件由Cursor 自动生成
using UnityEngine;

namespace ShadowGame.Prototype
{
    /// <summary>
    /// Per-object state machine handling selection, drag, rotation, snap, and boundary rebound.
    /// States: Idle → Selected → Dragging/Rotating → Snapping → Idle
    ///         Any → Locked (external only)
    /// </summary>
    public class InteractableObject : MonoBehaviour
    {
        public enum State { Idle, Selected, Dragging, Rotating, Snapping, Locked }

        [SerializeField] private float _colliderRadius = 0.1f;

        private State _state = State.Idle;
        private GridSystem _grid;
        private InteractionConfig _config;
        private Vector3 _originalScale;
        private float _currentYAngle;
        private bool _initialized;

        private float _snapTimer;
        private float _snapDuration;
        private Vector3 _snapStartPos;
        private Vector3 _snapTargetPos;
        private bool _needRebound;

        private float _scaleTimer;
        private float _scaleDuration;
        private float _scaleStart = 1f;
        private float _scaleTarget = 1f;

        private float _bounceTimer;

        public State CurrentState => _state;
        public float ColliderRadius => _colliderRadius;

        private void Awake()
        {
            _originalScale = transform.localScale;
            _currentYAngle = transform.eulerAngles.y;
        }

        public void Init(GridSystem grid)
        {
            _grid = grid;
            _config = grid.Config;
            _originalScale = transform.localScale;
            _currentYAngle = transform.eulerAngles.y;
            _initialized = true;
        }

        public bool TrySelect()
        {
            if (_state != State.Idle && _state != State.Snapping) return false;
            _state = State.Selected;
            StartScaleAnim(_config.selectScaleMultiplier, _config.selectAnimDuration);
            return true;
        }

        public void Deselect()
        {
            if (_state == State.Locked) return;
            _state = State.Idle;
            StartScaleAnim(1f, _config.deselectAnimDuration);
        }

        public void BeginDrag()
        {
            if (_state != State.Selected) return;
            _state = State.Dragging;
        }

        public void ContinueDrag(Vector3 worldPos)
        {
            if (_state != State.Dragging) return;
            Vector3 clamped = _grid.ClampToBounds(worldPos, _colliderRadius);
            clamped.y = transform.position.y;
            transform.position = clamped;
        }

        public void BeginRotate()
        {
            if (_state != State.Selected && _state != State.Dragging) return;
            _state = State.Rotating;
        }

        public void ContinueRotate(float angleDeltaDeg)
        {
            if (_state != State.Rotating) return;
            _currentYAngle += angleDeltaDeg;
            transform.rotation = Quaternion.Euler(0f, _currentYAngle, 0f);
        }

        public void Release()
        {
            if (_state == State.Locked) return;

            if (_state == State.Dragging)
            {
                StartPositionSnap();
            }
            else if (_state == State.Rotating)
            {
                float snappedAngle = _grid.SnapAngle(_currentYAngle);
                _currentYAngle = snappedAngle;
                transform.rotation = Quaternion.Euler(0f, snappedAngle, 0f);
                StartPositionSnap();
            }
        }

        public void Lock()
        {
            _state = State.Locked;
            StartScaleAnim(1f, _config.deselectAnimDuration);
        }

        public void Unlock()
        {
            _state = State.Idle;
        }

        /// <summary>
        /// System-driven snap to a specific position (e.g. PerfectMatch).
        /// Works even when Locked.
        /// </summary>
        public void SnapToTarget(Vector3 target)
        {
            _snapStartPos = transform.position;
            _snapTargetPos = target;
            float dist = Vector3.Distance(_snapStartPos, _snapTargetPos);
            _snapDuration = Mathf.Clamp(dist / 1.5f, 0.3f, 0.8f);
            _snapTimer = 0f;
            _needRebound = false;
            if (_state != State.Locked)
                _state = State.Snapping;
        }

        private void StartPositionSnap()
        {
            _state = State.Snapping;
            _snapStartPos = transform.position;
            Vector3 snapped = _grid.SnapPosition(transform.position);
            snapped.y = transform.position.y;

            _needRebound = !_grid.IsInsideBounds(snapped, _colliderRadius);
            _snapTargetPos = _needRebound ? _grid.ClampToBounds(snapped, _colliderRadius) : snapped;
            _snapTargetPos.y = transform.position.y;

            float dist = Vector3.Distance(_snapStartPos, _snapTargetPos);
            _snapDuration = _needRebound
                ? _config.reboundDuration
                : _grid.CalcSnapDuration(dist);

            _snapTimer = 0f;
            _bounceTimer = 0f;

            if (_snapDuration < 0.001f)
            {
                transform.position = _snapTargetPos;
                FinishSnap();
            }
        }

        private void FinishSnap()
        {
            _bounceTimer = _config.bounceDuration;
            _state = State.Idle;
        }

        private void StartScaleAnim(float targetMultiplier, float duration)
        {
            float origX = _originalScale.x;
            _scaleStart = origX > 0.001f ? transform.localScale.x / origX : 1f;
            _scaleTarget = targetMultiplier;
            _scaleDuration = duration;
            _scaleTimer = 0f;
        }

        private void Update()
        {
            if (!_initialized) return;
            UpdateSnapAnim();
            UpdateScaleAnim();
            UpdateBounce();
        }

        private void UpdateSnapAnim()
        {
            if (_state != State.Snapping) return;

            _snapTimer += Time.deltaTime;
            float t = Mathf.Clamp01(_snapTimer / _snapDuration);

            float eased = _needRebound ? EaseOutBack(t, _config.reboundOvershoot) : EaseOutQuad(t);
            transform.position = Vector3.LerpUnclamped(_snapStartPos, _snapTargetPos, eased);

            if (t >= 1f)
                FinishSnap();
        }

        private void UpdateScaleAnim()
        {
            if (Mathf.Abs(transform.localScale.x / _originalScale.x - _scaleTarget) < 0.001f) return;

            _scaleTimer += Time.deltaTime;
            float t = Mathf.Clamp01(_scaleTimer / Mathf.Max(_scaleDuration, 0.01f));

            float eased = _scaleTarget > 1f ? EaseOutBack(t, 1.7f) : EaseOutQuad(t);

            float scale = Mathf.LerpUnclamped(_scaleStart, _scaleTarget, eased);
            transform.localScale = _originalScale * scale;
        }

        private void UpdateBounce()
        {
            if (_bounceTimer <= 0f) return;

            _bounceTimer -= Time.deltaTime;
            if (_bounceTimer <= 0f)
            {
                transform.localScale = _originalScale;
                _bounceTimer = 0f;
                return;
            }

            float t = 1f - (_bounceTimer / _config.bounceDuration);
            float bounce = Mathf.Sin(t * Mathf.PI * 2f) * _config.bounceAmplitude * (1f - t);
            transform.localScale = _originalScale * (1f + bounce);
        }

        private static float EaseOutQuad(float t) => 1f - (1f - t) * (1f - t);

        private static float EaseOutBack(float t, float overshoot)
        {
            float s = overshoot;
            t -= 1f;
            return 1f + (s + 1f) * t * t * t + s * t * t;
        }
    }
}
