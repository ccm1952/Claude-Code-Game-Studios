// 该文件由Cursor 自动生成
using System;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 屏幕坐标 → gameplay 世界坐标（chapter 1 物件交互共用）。
    /// <para>Chapter 1 相机沿 +Z 看向物件（ gameplay 在 <b>XY @ Z=0</b> 平面）：
    /// 拖拽必须用 <see cref="TryScreenToWorldOnPlane"/>（Z 平面）；
    /// 水平面 Y=常数 仅适用于俯视/地面 Y-up 场景（screen 竖直滑动映射到世界 Z，写入 XY 时会“只能横移”）。</para>
    /// </summary>
    public static class GameplayScreenProjection
    {
        /// <summary>屏幕射线与 Z 常数平面（法线 +Z，过 <paramref name="planeZ"/>）求交。</summary>
        public static bool TryScreenToWorldOnPlane(Camera camera, Vector2 screenPos, float planeZ, out Vector3 worldPoint)
        {
            worldPoint = default;
            if (camera == null) return false;

            var plane = new Plane(Vector3.forward, new Vector3(0f, 0f, planeZ));
            return TryRaycastPlane(camera, screenPos, plane, out worldPoint);
        }

        /// <summary>屏幕射线与水平面（法线 +Y，高度 <paramref name="planeY"/>）求交 — 用于拖拽跟手。</summary>
        public static bool TryScreenToWorldOnHorizontalPlane(
            Camera camera, Vector2 screenPos, float planeY, out Vector3 worldPoint)
        {
            worldPoint = default;
            if (camera == null) return false;

            var plane = new Plane(Vector3.up, new Vector3(0f, planeY, 0f));
            return TryRaycastPlane(camera, screenPos, plane, out worldPoint);
        }

        /// <summary>在水平面 <paramref name="planeY"/> 上，水平 1 屏幕像素对应的世界单位长度。</summary>
        public static float GetWorldUnitsPerScreenPixelOnHorizontalPlane(Camera camera, Vector2 screenPos, float planeY)
        {
            if (camera == null) return 0.01f;

            var plane = new Plane(Vector3.up, new Vector3(0f, planeY, 0f));
            return GetWorldUnitsPerScreenPixelOnPlane(camera, screenPos, plane);
        }

        /// <summary>在 Z 常数平面 <paramref name="planeZ"/> 上，水平 1 屏幕像素对应的世界单位长度。</summary>
        public static float GetWorldUnitsPerScreenPixel(Camera camera, Vector2 screenPos, float planeZ)
        {
            if (camera == null) return 0.01f;

            var plane = new Plane(Vector3.forward, new Vector3(0f, 0f, planeZ));
            return GetWorldUnitsPerScreenPixelOnPlane(camera, screenPos, plane);
        }

        private static bool TryRaycastPlane(Camera camera, Vector2 screenPos, Plane plane, out Vector3 worldPoint)
        {
            worldPoint = default;
            var ray = camera.ScreenPointToRay(screenPos);
            if (!plane.Raycast(ray, out float enter)) return false;

            worldPoint = ray.GetPoint(enter);
            return true;
        }

        private static float GetWorldUnitsPerScreenPixelOnPlane(Camera camera, Vector2 screenPos, Plane plane)
        {
            var rayA = camera.ScreenPointToRay(screenPos);
            var rayB = camera.ScreenPointToRay(screenPos + Vector2.right);
            if (!plane.Raycast(rayA, out float enterA) || !plane.Raycast(rayB, out float enterB))
                return 0.01f;

            var wA = rayA.GetPoint(enterA);
            var wB = rayB.GetPoint(enterB);
            return Vector2.Distance(new Vector2(wA.x, wA.y), new Vector2(wB.x, wB.y));
        }

        /// <summary>
        /// 透视/正交相机视口在 Z 常数平面上的可见 XY 范围（拖拽 clamp / grid snap 共用）。
        /// <paramref name="insetWorld"/> 内缩世界单位，避免物件贴边后 collider 仍超出屏幕。
        /// </summary>
        public static bool TryComputeVisibleBoundsOnZPlane(
            Camera camera, float planeZ, float insetWorld, out InteractionBounds bounds)
        {
            bounds = default;
            if (camera == null) return false;

            float minX = float.PositiveInfinity;
            float maxX = float.NegativeInfinity;
            float minY = float.PositiveInfinity;
            float maxY = float.NegativeInfinity;
            int hits = 0;

            var plane = new Plane(Vector3.forward, new Vector3(0f, 0f, planeZ));
            ReadOnlySpan<Vector2> viewportSamples = stackalloc Vector2[]
            {
                new(0f, 0f), new(1f, 0f), new(0f, 1f), new(1f, 1f),
                new(0.5f, 0f), new(0.5f, 1f), new(0f, 0.5f), new(1f, 0.5f),
            };

            for (int i = 0; i < viewportSamples.Length; i++)
            {
                var vp = viewportSamples[i];
                var ray = camera.ViewportPointToRay(new Vector3(vp.x, vp.y, 0f));
                if (!plane.Raycast(ray, out float enter)) continue;

                var world = ray.GetPoint(enter);
                minX = Mathf.Min(minX, world.x);
                maxX = Mathf.Max(maxX, world.x);
                minY = Mathf.Min(minY, world.y);
                maxY = Mathf.Max(maxY, world.y);
                hits++;
            }

            if (hits < 4) return false;

            bounds = new InteractionBounds(
                minX + insetWorld, maxX - insetWorld,
                minY + insetWorld, maxY - insetWorld);
            return bounds.IsValid;
        }
    }
}
