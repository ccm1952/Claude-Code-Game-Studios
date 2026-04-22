// 该文件由Cursor 自动生成
using UnityEngine;

namespace ShadowGame.Prototype
{
    /// <summary>
    /// Minimal drag controller for shadow prototype testing.
    /// Supports mouse and single-touch input for moving interactable objects.
    /// </summary>
    public class SimpleDragController : MonoBehaviour
    {
        [SerializeField] private Camera _mainCamera;
        [SerializeField] private LayerMask _draggableMask = ~0;
        [SerializeField] private float _dragPlaneY = 0.5f;

        private Transform _dragging;
        private Vector3 _dragOffset;
        private Plane _dragPlane;

        private void Start()
        {
            if (_mainCamera == null)
                _mainCamera = Camera.main;
        }

        private void Update()
        {
            if (GetPointerDown())
                TryBeginDrag();
            else if (GetPointerHeld() && _dragging != null)
                ContinueDrag();
            else if (GetPointerUp())
                EndDrag();
        }

        private void TryBeginDrag()
        {
            Ray ray = _mainCamera.ScreenPointToRay(GetPointerPosition());
            if (!Physics.Raycast(ray, out RaycastHit hit, 50f, _draggableMask))
                return;

            _dragging = hit.transform;
            _dragPlane = new Plane(Vector3.up, new Vector3(0, _dragging.position.y, 0));

            if (_dragPlane.Raycast(ray, out float enter))
            {
                Vector3 hitPoint = ray.GetPoint(enter);
                _dragOffset = _dragging.position - hitPoint;
            }
        }

        private void ContinueDrag()
        {
            Ray ray = _mainCamera.ScreenPointToRay(GetPointerPosition());
            if (!_dragPlane.Raycast(ray, out float enter))
                return;

            Vector3 hitPoint = ray.GetPoint(enter);
            _dragging.position = hitPoint + _dragOffset;
        }

        private void EndDrag()
        {
            _dragging = null;
        }

        private static bool GetPointerDown()
        {
            if (Input.touchCount > 0)
                return Input.GetTouch(0).phase == TouchPhase.Began;
            return Input.GetMouseButtonDown(0);
        }

        private static bool GetPointerHeld()
        {
            if (Input.touchCount > 0)
            {
                var phase = Input.GetTouch(0).phase;
                return phase == TouchPhase.Moved || phase == TouchPhase.Stationary;
            }
            return Input.GetMouseButton(0);
        }

        private static bool GetPointerUp()
        {
            if (Input.touchCount > 0)
                return Input.GetTouch(0).phase == TouchPhase.Ended;
            return Input.GetMouseButtonUp(0);
        }

        private static Vector3 GetPointerPosition()
        {
            if (Input.touchCount > 0)
                return Input.GetTouch(0).position;
            return Input.mousePosition;
        }
    }
}
