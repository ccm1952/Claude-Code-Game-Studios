// 该文件由Cursor 自动生成
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 拼图静态配置（S2-09 / S2-10 / S2-13 共享数据源；ADR-013 §"Configuration" + ADR-007 §Luban Access）。
    /// </summary>
    /// <remarks>
    /// <para><b>来源</b>：生产环境从 Luban <c>TbPuzzle</c> 表读取（每行映射成一个 <see cref="PuzzleConfig"/> 实例）；
    /// S2-09 阶段 Luban <c>TbPuzzle</c> 链路尚未接通，<see cref="InteractableObject"/> 通过
    /// <c>Func&lt;int, PuzzleConfig&gt;</c> provider 注入获得（默认 <c>null</c>，fail-loud
    /// — 与 <c>SceneManager.RegisterChapterDataProvider</c> / S2-07 同模式）。</para>
    ///
    /// <para><b>不可变性</b>：所有字段 <see langword="readonly"/>。运行时**禁止**修改一个 <see cref="PuzzleConfig"/>
    /// 实例的字段（ADR-007 §"Config data objects are read-only after Init()"）。需要不同数据 → 重新通过 provider 解析。</para>
    ///
    /// <para><b>S2-09 范围</b>：本 story 仅消费 <see cref="InteractionBounds"/>；<see cref="GridSize"/> / <see cref="SnapSpeed"/>
    /// 字段签名先行冻结，由 S2-10 (Grid Snap) 实施时接入。</para>
    ///
    /// <para><b>S2-11 扩展</b>：加 <see cref="RotationStep"/>（角度 snap 步长，默认 15°；与 GridSize 同源 Luban <c>TbPuzzle</c>）；
    /// 与 SnapSpeed 复用同一时长字段（位置 snap 与角度 snap 同 Ease + duration 视觉一致）。</para>
    /// </remarks>
    public sealed class PuzzleConfig
    {
        /// <summary>拼图 ID（对应 Luban <c>TbPuzzle.Id</c>；与 <c>InteractableObject._puzzleId</c> 匹配）。</summary>
        public readonly int Id;

        /// <summary>可交互物体的 2D 平面边界（drag clamp + grid snap clamp 共享）。</summary>
        public readonly InteractionBounds InteractionBounds;

        /// <summary>网格大小（snap 公式 <c>round(rawPos / gridSize) * gridSize</c>）。S2-10 用。</summary>
        public readonly float GridSize;

        /// <summary>Snap DOTween 动画时长（秒）。S2-10 / S2-11 共用（位置 snap + 角度 snap 同 duration）。</summary>
        public readonly float SnapSpeed;

        /// <summary>角度 snap 步长（度；公式 <c>round(angle / RotationStep) * RotationStep</c>）。S2-11 用。默认 15°（24 个等分位）。</summary>
        public readonly float RotationStep;

        public PuzzleConfig(int id, InteractionBounds interactionBounds, float gridSize = 1.0f, float snapSpeed = 0.2f, float rotationStep = 15f)
        {
            Id = id;
            InteractionBounds = interactionBounds;
            GridSize = gridSize;
            SnapSpeed = snapSpeed;
            RotationStep = rotationStep;
        }
    }

    /// <summary>
    /// 可交互物体的 2D 平面活动边界（drag 期间 clamp + snap 后置 clamp 共享）。
    /// </summary>
    /// <remarks>
    /// <para>X / Y 独立 clamp。Z 保持 transform 当前值（drag 不改 Z）。</para>
    /// </remarks>
    public readonly struct InteractionBounds
    {
        public readonly float MinX;
        public readonly float MaxX;
        public readonly float MinY;
        public readonly float MaxY;

        public InteractionBounds(float minX, float maxX, float minY, float maxY)
        {
            MinX = minX;
            MaxX = maxX;
            MinY = minY;
            MaxY = maxY;
        }

        /// <summary>判定一个 2D 点是否在边界内（含边界）。</summary>
        public bool Contains(Vector2 point)
            => point.x >= MinX && point.x <= MaxX && point.y >= MinY && point.y <= MaxY;

        /// <summary>把一个 2D 点 clamp 到边界内（X/Y 独立）。</summary>
        public Vector2 Clamp(Vector2 point)
            => new Vector2(Mathf.Clamp(point.x, MinX, MaxX), Mathf.Clamp(point.y, MinY, MaxY));
    }
}
