// 该文件由Cursor 自动生成
using UnityEngine;

namespace ShadowGame.Prototype
{
    /// <summary>
    /// 物件交互原型的集中配置参数。
    /// 所有默认值对齐 GDD (design/gdd/object-interaction.md) 的 Tuning Knobs。
    /// 可通过 Inspector 或运行时 Debug Panel 调节。
    /// </summary>
    [CreateAssetMenu(fileName = "InteractionConfig", menuName = "ShadowGame/Interaction Config")]
    public class InteractionConfig : ScriptableObject
    {
        [Header("格点吸附")]

        [Tooltip("格点步进大小（世界单位）。增大→操作容易但定位粗糙；减小→精确但移动端易挫败。安全范围: 0.1-0.5")]
        [Range(0.1f, 0.5f)]
        public float gridSize = 0.25f;

        [Tooltip("吸附动画基础速度（单位/秒）。增大→吸附更爽脆；减小→吸附更柔和。安全范围: 2.0-5.0")]
        [Range(2.0f, 5.0f)]
        public float snapSpeed = 3.0f;

        [Tooltip("吸附动画最短时长（秒）。距离极近时避免瞬移感。安全范围: 0.03-0.08")]
        [Range(0.03f, 0.08f)]
        public float minSnapDuration = 0.05f;

        [Tooltip("吸附动画最长时长（秒）。距离远时避免拖沓。安全范围: 0.10-0.25")]
        [Range(0.10f, 0.25f)]
        public float maxSnapDuration = 0.15f;

        [Header("旋转")]

        [Tooltip("旋转步进角度（度）。增大→旋转粗快；减小→精确但移动端微调困难。安全范围: 10-45")]
        [Range(10f, 45f)]
        public float rotationStep = 15f;

        [Header("选中动画")]

        [Tooltip("选中时缩放倍数。增大→选中放大更明显；减小→更微妙。安全范围: 1.02-1.10")]
        [Range(1.02f, 1.10f)]
        public float selectScaleMultiplier = 1.05f;

        [Tooltip("选中放大动画时长（秒）。GDD: 8帧 @ 60fps ≈ 0.133s，EaseOutBack 曲线")]
        public float selectAnimDuration = 8f / 60f;

        [Tooltip("取消选中缩回动画时长（秒）。GDD: 6帧 @ 60fps ≈ 0.100s，EaseOutQuad 曲线")]
        public float deselectAnimDuration = 6f / 60f;

        [Header("放下回弹")]

        [Tooltip("放下回弹振幅。增大→回弹更明显；减小→更克制。安全范围: 0.01-0.05")]
        [Range(0.01f, 0.05f)]
        public float bounceAmplitude = 0.02f;

        [Tooltip("放下回弹总时长（秒）。GDD: 10帧 @ 60fps ≈ 0.167s（6 active + 4 recovery）")]
        public float bounceDuration = 10f / 60f;

        [Header("边界回弹")]

        [Tooltip("边界回弹动画时长（秒）。增大→更柔和；减小→更硬。安全范围: 0.10-0.25")]
        [Range(0.10f, 0.25f)]
        public float reboundDuration = 0.15f;

        [Tooltip("边界回弹 EaseOutBack 过冲系数。增大→更有弹性；减小(=0)→无过冲。安全范围: 0.0-0.8")]
        [Range(0f, 0.8f)]
        public float reboundOvershoot = 0.3f;

        [Header("胖手指补偿")]

        [Tooltip("基础胖手指补偿值（dp）。增大→更容易选中但可能误选相邻物件。安全范围: 4-16")]
        [Range(4f, 16f)]
        public float fatFingerMarginDp = 8f;

        [Tooltip("参考 DPI（基准设备 iPhone 13 Mini = 326）。用于不同屏幕间归一化触碰区域")]
        public float referenceDPI = 326f;

        [Header("手势识别")]

        [Tooltip("双指累计旋转超过此角度（度）才识别为旋转手势。增大→减少误触发；减小→更灵敏。安全范围: 5-15")]
        [Range(5f, 15f)]
        public float rotateRecognitionThreshold = 8f;

        [Tooltip("防抖动：双指间距小于此值（像素）时忽略旋转输入。安全范围: 10-40")]
        [Range(10f, 40f)]
        public float minFingerDistancePx = 20f;

        [Tooltip("点击防抖窗口（秒）。200ms 内的重复触碰合并为同一选中事件")]
        public float tapDebounceWindow = 0.2f;

        [Tooltip("拖拽判定阈值（毫米）。手指移动超过此物理距离才判定为拖拽。安全范围: 2.0-5.0")]
        [Range(2.0f, 5.0f)]
        public float dragThresholdMm = 3.0f;

        /// <summary>
        /// 将毫米阈值转换为当前设备的像素阈值。
        /// 公式: dragThreshold_mm * screenDPI / 25.4
        /// </summary>
        public float GetDragThresholdPx()
        {
            float dpi = Screen.dpi > 0 ? Screen.dpi : 160f;
            return dragThresholdMm * dpi / 25.4f;
        }

        /// <summary>
        /// 将 dp 补偿值转换为世界空间半径，用于 SphereCast 胖手指选中。
        /// </summary>
        public float GetFatFingerWorldRadius(Camera cam, float objectDistance)
        {
            float dpi = Screen.dpi > 0 ? Screen.dpi : 160f;
            float marginPx = fatFingerMarginDp * dpi / referenceDPI;
            float worldPerPx = (2f * objectDistance * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad)) / Screen.height;
            return marginPx * worldPerPx;
        }
    }
}
