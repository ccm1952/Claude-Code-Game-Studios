// 该文件由Cursor 自动生成
using UnityEngine;

namespace ShadowGame.Prototype
{
    /// <summary>
    /// Handles input → raycast → gesture recognition → delegates to InteractableObject.
    /// Supports single-finger drag and two-finger rotation.
    /// Uses Legacy Input (UnityEngine.Input) for prototype simplicity.
    /// </summary>
    public class InteractionController : MonoBehaviour
    {
        [SerializeField] private Camera _mainCamera;
        [SerializeField] private GridSystem _gridSystem;
        [SerializeField] private LayerMask _interactableMask = ~0;

        private InteractionConfig _config;
        private InteractableObject _selected;
        private Plane _dragPlane;
        private Vector3 _dragOffset;

        private Vector2 _pointerDownPos;
        private float _pointerDownTime;
        private bool _isDragRecognized;

        private bool _isTwoFingerActive;
        private float _prevTwoFingerAngle;
        private float _accumulatedAngle;
        private bool _isRotateRecognized;

        private enum Finger { None, One, Two }
        private Finger _activeFinger = Finger.None;

        public InteractableObject Selected => _selected;
        public bool IsDragRecognized => _isDragRecognized;
        public bool IsRotateRecognized => _isRotateRecognized;

        private void Start()
        {
            if (_mainCamera == null) _mainCamera = Camera.main;
            if (_gridSystem != null)
            {
                _config = _gridSystem.Config;
                InitAllInteractables();
            }
        }

        private void InitAllInteractables()
        {
            var all = FindObjectsOfType<InteractableObject>();
            foreach (var obj in all)
                obj.Init(_gridSystem);
            Debug.Log($"[Interaction] Initialized {all.Length} interactable objects.");
        }

        private void Update()
        {
            if (_config == null) return;

            int touchCount = Input.touchCount;

#if UNITY_EDITOR
            if (touchCount == 0)
            {
                HandleMouseInput();
                return;
            }
#endif
            HandleTouchInput(touchCount);
        }

        #region Touch Input

        private void HandleTouchInput(int touchCount)
        {
            if (touchCount >= 2)
            {
                HandleTwoFingerTouch();
                return;
            }

            if (touchCount == 1)
            {
                if (_isTwoFingerActive)
                {
                    EndTwoFinger();
                    return;
                }

                Touch t = Input.GetTouch(0);
                switch (t.phase)
                {
                    case TouchPhase.Began:
                        OnPointerDown(t.position);
                        break;
                    case TouchPhase.Moved:
                    case TouchPhase.Stationary:
                        OnPointerMove(t.position);
                        break;
                    case TouchPhase.Ended:
                    case TouchPhase.Canceled:
                        OnPointerUp(t.position);
                        break;
                }
            }
            else
            {
                if (_activeFinger != Finger.None)
                    OnPointerUp(Vector2.zero);
                if (_isTwoFingerActive)
                    EndTwoFinger();
            }
        }

        private void HandleTwoFingerTouch()
        {
            if (_selected == null) return;

            Touch t0 = Input.GetTouch(0);
            Touch t1 = Input.GetTouch(1);

            if (!_isTwoFingerActive)
            {
                BeginTwoFinger(t0.position, t1.position);
                return;
            }

            if (t0.phase == TouchPhase.Ended || t0.phase == TouchPhase.Canceled ||
                t1.phase == TouchPhase.Ended || t1.phase == TouchPhase.Canceled)
            {
                EndTwoFinger();
                return;
            }

            UpdateTwoFinger(t0.position, t1.position);
        }

        private void BeginTwoFinger(Vector2 p0, Vector2 p1)
        {
            if (_isDragRecognized && _selected != null)
            {
                // Cancel drag, transition to two-finger
            }

            _isDragRecognized = false;
            _isTwoFingerActive = true;
            _isRotateRecognized = false;
            _accumulatedAngle = 0f;
            _prevTwoFingerAngle = AngleBetween(p0, p1);
            _activeFinger = Finger.Two;
        }

        private void UpdateTwoFinger(Vector2 p0, Vector2 p1)
        {
            if (Vector2.Distance(p0, p1) < _config.minFingerDistancePx) return;

            float angle = AngleBetween(p0, p1);
            float delta = Mathf.DeltaAngle(_prevTwoFingerAngle, angle);
            _prevTwoFingerAngle = angle;
            _accumulatedAngle += Mathf.Abs(delta);

            if (!_isRotateRecognized)
            {
                if (_accumulatedAngle >= _config.rotateRecognitionThreshold)
                {
                    _isRotateRecognized = true;
                    _selected?.BeginRotate();
                }
                return;
            }

            _selected?.ContinueRotate(-delta);
        }

        private void EndTwoFinger()
        {
            _isTwoFingerActive = false;
            _isRotateRecognized = false;
            _activeFinger = Finger.None;
            _selected?.Release();
        }

        #endregion

        #region Mouse Input (Editor)

        private void HandleMouseInput()
        {
            if (Input.GetMouseButtonDown(0))
                OnPointerDown(Input.mousePosition);
            else if (Input.GetMouseButton(0))
                OnPointerMove(Input.mousePosition);
            else if (Input.GetMouseButtonUp(0))
                OnPointerUp(Input.mousePosition);

            if (_selected != null && Input.GetMouseButton(1))
            {
                float delta = Input.GetAxis("Mouse X") * 180f;
                if (Mathf.Abs(delta) > 0.01f)
                {
                    if (_selected.CurrentState != InteractableObject.State.Rotating)
                        _selected.BeginRotate();
                    _selected.ContinueRotate(delta);
                }
            }
            else if (_selected != null && Input.GetMouseButtonUp(1))
            {
                if (_selected.CurrentState == InteractableObject.State.Rotating)
                    _selected.Release();
            }
        }

        #endregion

        #region Pointer Abstraction

        private void OnPointerDown(Vector2 screenPos)
        {
            _pointerDownPos = screenPos;
            _pointerDownTime = Time.unscaledTime;
            _isDragRecognized = false;
            _activeFinger = Finger.One;

            InteractableObject hit = RaycastInteractable(screenPos);
            if (hit == null)
            {
                if (_selected != null)
                {
                    _selected.Deselect();
                    _selected = null;
                }
                return;
            }

            if (_selected != null && _selected != hit)
                _selected.Deselect();

            if (hit.TrySelect())
                _selected = hit;
        }

        private void OnPointerMove(Vector2 screenPos)
        {
            if (_activeFinger != Finger.One || _selected == null) return;

            if (!_isDragRecognized)
            {
                float dist = Vector2.Distance(screenPos, _pointerDownPos);
                if (dist < _config.GetDragThresholdPx()) return;
                _isDragRecognized = true;
                _selected.BeginDrag();

                Ray ray = _mainCamera.ScreenPointToRay(screenPos);
                _dragPlane = new Plane(Vector3.up, new Vector3(0, _selected.transform.position.y, 0));
                if (_dragPlane.Raycast(ray, out float enter))
                    _dragOffset = _selected.transform.position - ray.GetPoint(enter);
            }

            if (_isDragRecognized)
            {
                Ray ray = _mainCamera.ScreenPointToRay(screenPos);
                if (_dragPlane.Raycast(ray, out float enter))
                {
                    Vector3 worldPos = ray.GetPoint(enter) + _dragOffset;
                    _selected.ContinueDrag(worldPos);
                }
            }
        }

        private void OnPointerUp(Vector2 screenPos)
        {
            if (_selected != null && (_isDragRecognized || _selected.CurrentState == InteractableObject.State.Dragging))
            {
                _selected.Release();
            }

            _isDragRecognized = false;
            _activeFinger = Finger.None;
        }

        #endregion

        #region Raycast

        private InteractableObject RaycastInteractable(Vector2 screenPos)
        {
            Ray ray = _mainCamera.ScreenPointToRay(screenPos);

            if (Physics.Raycast(ray, out RaycastHit directHit, 50f, _interactableMask))
            {
                var obj = directHit.collider.GetComponentInParent<InteractableObject>();
                if (obj != null) return obj;
            }

            float fatRadius = _config.GetFatFingerWorldRadius(_mainCamera, 5f);
            if (fatRadius > 0.01f && Physics.SphereCast(ray, fatRadius, out RaycastHit fatHit, 50f, _interactableMask))
            {
                var obj = fatHit.collider.GetComponentInParent<InteractableObject>();
                if (obj != null) return obj;
            }

            return null;
        }

        #endregion

        #region Utilities

        private static float AngleBetween(Vector2 p0, Vector2 p1)
        {
            Vector2 dir = p1 - p0;
            return Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        }

        #endregion
    }
}
