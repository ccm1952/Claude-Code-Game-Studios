// 该文件由Cursor 自动生成
using UnityEngine;

namespace ShadowGame.Prototype
{
    /// <summary>
    /// Handles grid snapping, boundary clamping, and rebound animation.
    /// </summary>
    public class GridSystem : MonoBehaviour
    {
        [SerializeField] private InteractionConfig _config;

        [Header("Boundary (world-space XZ rect)")]
        [SerializeField] private Vector2 _boundsMin = new(-2.5f, 0.5f);
        [SerializeField] private Vector2 _boundsMax = new(2.5f, 2.5f);

        public InteractionConfig Config => _config;
        public Vector2 BoundsMin => _boundsMin;
        public Vector2 BoundsMax => _boundsMax;

        public Vector3 SnapPosition(Vector3 raw)
        {
            float gs = _config.gridSize;
            return new Vector3(
                Mathf.Round(raw.x / gs) * gs,
                raw.y,
                Mathf.Round(raw.z / gs) * gs
            );
        }

        public float SnapAngle(float rawDeg)
        {
            float step = _config.rotationStep;
            return Mathf.Round(rawDeg / step) * step;
        }

        public Vector3 ClampToBounds(Vector3 pos, float objectRadius = 0f)
        {
            pos.x = Mathf.Clamp(pos.x, _boundsMin.x + objectRadius, _boundsMax.x - objectRadius);
            pos.z = Mathf.Clamp(pos.z, _boundsMin.y + objectRadius, _boundsMax.y - objectRadius);
            return pos;
        }

        public bool IsInsideBounds(Vector3 pos, float objectRadius = 0f)
        {
            return pos.x >= _boundsMin.x + objectRadius && pos.x <= _boundsMax.x - objectRadius &&
                   pos.z >= _boundsMin.y + objectRadius && pos.z <= _boundsMax.y - objectRadius;
        }

        public float CalcSnapDuration(float distance)
        {
            if (distance < 0.001f) return 0f;
            return Mathf.Clamp(distance / _config.snapSpeed, _config.minSnapDuration, _config.maxSnapDuration);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
            Vector3 center = new(
                (_boundsMin.x + _boundsMax.x) * 0.5f,
                0.01f,
                (_boundsMin.y + _boundsMax.y) * 0.5f
            );
            Vector3 size = new(_boundsMax.x - _boundsMin.x, 0.02f, _boundsMax.y - _boundsMin.y);
            Gizmos.DrawCube(center, size);
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(center, size);
        }
    }
}
