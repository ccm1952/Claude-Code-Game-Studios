// 该文件由Cursor 自动生成
using TEngine;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// Object Interaction epic 的单一接口事件契约（ADR-027 §1）。
    /// 替代 ADR-006 的 Evt_Object* / Evt_Puzzle* 常量 + XxxPayload struct 方案（整体作废）。
    /// </summary>
    /// <remarks>
    /// <para><b>Sender 分工</b>：</para>
    /// <list type="bullet">
    /// <item><c>OnObjectSelected / OnObjectDeselected</c> — InteractableObjectFsm / InteractionCoordinator 唯一 sender；UI / Audio / Analytics 按需订阅</item>
    /// <item><c>OnObjectTransformChanged</c> — InteractableObjectFsm（SnappingState OnComplete + RotationSnap OnComplete）唯一 sender；Shadow Puzzle / Save System listener</item>
    /// <item><c>OnRequestPuzzleLockAll / OnRequestPuzzleUnlock</c> — Narrative / Shadow Puzzle / Tutorial 发起；InteractionLockManager 唯一 listener</item>
    /// <item><c>OnInteractionLockChanged</c> — InteractionLockManager 唯一 sender；InteractableObjectFsm / UI 按需订阅</item>
    /// </list>
    /// <para><b>实装范围</b>：</para>
    /// <list type="bullet">
    /// <item>Story 001 实装 <c>OnObjectSelected / OnObjectDeselected</c>（FSM transition sender）</item>
    /// <item>Story 003 / 004 实装 <c>OnObjectTransformChanged</c>（snap 完成 sender）</item>
    /// <item>Story 006 实装 <c>OnRequestPuzzleLockAll / OnRequestPuzzleUnlock</c> listener + <c>OnInteractionLockChanged</c> sender</item>
    /// <item>Story 007 实装 InteractionCoordinator 单选语义（消费 Selected/Deselected 协议）</item>
    /// <item><c>OnRequestSnapToTarget / OnLightPositionChanged</c> 接口签名冻结，sender / listener 实现留待 Narrative epic / LightSource 子系统</item>
    /// </list>
    /// <para><b>Cascade</b>：所有方法调用不得超过 3 层级联（ADR-027 §2 继承 ADR-006 re-entrancy 约束）。
    /// 典型链路：<c>IGestureEvent.OnTap → InteractionCoordinator → IInteractionEvent.OnObjectSelected</c>（cascade=2）。</para>
    /// </remarks>
    [EventInterface(EEventGroup.GroupLogic)]
    public interface IInteractionEvent
    {
        // ====== 物体生命周期通知（Object Interaction → 外部消费方） ======

        /// <summary>物体进入 <c>Selected</c> 状态。</summary>
        /// <param name="objectId">被选中物体的 ID（Luban TbInteractable 主键）</param>
        /// <para>Sender: InteractableObjectFsm（IdleState→SelectedState 进入回调）/ InteractionCoordinator（切换选中时）</para>
        /// <para>Listener: UI（选中提示）, Audio（选中音效）, Analytics（操作埋点）</para>
        /// <para>Cascade: 通常 0；UI listener 不应再派发新事件</para>
        void OnObjectSelected(int objectId);

        /// <summary>物体退出 <c>Selected</c> 状态（含主动取消、切换到其他物体、Lock 强制取消）。</summary>
        /// <param name="objectId">被取消选中物体的 ID</param>
        /// <para>Sender: InteractableObjectFsm（SelectedState 离开回调）/ InteractionCoordinator</para>
        /// <para>Listener: UI（关闭选中提示）, Audio（取消音效）</para>
        void OnObjectDeselected(int objectId);

        /// <summary>物体 transform 已确定地变化（snap 动画完成后 / 旋转 snap 完成后 / Narrative SnapToTarget 完成后触发）。
        /// 拖拽/旋转过程中**不**派发本事件——仅在最终落位时派发一次，确保 Shadow Puzzle 仅在稳定状态下重新匹配。</summary>
        /// <param name="objectId">变换的物体 ID</param>
        /// <param name="position">最终世界坐标</param>
        /// <param name="rotation">最终世界旋转</param>
        /// <para>Sender: InteractableObjectFsm（SnappingState DOTween OnComplete / SelectedState rotation snap OnComplete）</para>
        /// <para>Listener: Shadow Puzzle（重算阴影匹配）, Save System（持久化布局）, Analytics</para>
        /// <para>Cascade: 可触发 Shadow Puzzle 内部 IShadowRTEvent.OnShadowReevaluate 等</para>
        void OnObjectTransformChanged(int objectId, Vector3 position, Quaternion rotation);

        // ====== 锁定命令 + 通知（外部 → InteractionLockManager → InteractableObjectFsm） ======

        /// <summary>请求锁定所有可交互物体（HashSet token 累加；幂等）。</summary>
        /// <param name="lockerId">锁源 ID（必须取自 <c>InteractionLockerId</c> 常量集合：<c>"shadow_puzzle"</c> / <c>"narrative"</c> / <c>"tutorial"</c>）</param>
        /// <para>Sender: Narrative Sequence / Shadow Puzzle / Tutorial</para>
        /// <para>Listener: InteractionLockManager（唯一）</para>
        /// <para>Cascade: 可能触发 OnInteractionLockChanged(true)（仅在 set 由空→非空时）</para>
        void OnRequestPuzzleLockAll(string lockerId);

        /// <summary>请求解锁（HashSet token 移除；token 集合空时真正解锁；未知 token 仅产生 warning，不抛异常）。</summary>
        /// <param name="lockerId">锁源 ID（必须与之前 OnRequestPuzzleLockAll 配对）</param>
        /// <para>Sender: Narrative Sequence / Shadow Puzzle / Tutorial</para>
        /// <para>Listener: InteractionLockManager（唯一）</para>
        /// <para>Cascade: 可能触发 OnInteractionLockChanged(false)（仅在 set 由非空→空时）</para>
        void OnRequestPuzzleUnlock(string lockerId);

        /// <summary>整体锁定状态发生变化（HashSet 由空→非空 或 由非空→空 时各派发一次；中间增量无派发）。</summary>
        /// <param name="isLocked"><c>true</c> = 当前任意 token 锁定；<c>false</c> = 全部解锁</param>
        /// <para>Sender: InteractionLockManager（唯一）</para>
        /// <para>Listener: InteractableObjectFsm（决定是否进入/离开 Locked 状态）, UI（锁定指示器）</para>
        /// <para>Cascade: InteractableObjectFsm Any→Locked / Locked→Idle 转换</para>
        void OnInteractionLockChanged(bool isLocked);

        // ====== 预留位（接口签名冻结，本轮 X1 迁移不实装 sender / listener） ======

        /// <summary>[Reserved — Narrative epic / S3+] Narrative 命令物体精确 snap 到指定 transform（剧情桥段使用）。</summary>
        /// <param name="objectId">目标物体 ID</param>
        /// <param name="position">目标世界坐标</param>
        /// <param name="rotation">目标世界旋转</param>
        /// <param name="duration">DOTween 动画时长（秒；&lt;= 0 表示立即 snap 无动画）</param>
        /// <para>Sender: Narrative Sequence Engine（未来）</para>
        /// <para>Listener: InteractableObjectFsm（强制进入 Snapping 子流程；忽略 Locked 状态）</para>
        void OnRequestSnapToTarget(int objectId, Vector3 position, Quaternion rotation, float duration);

        /// <summary>[Reserved — LightSource 子系统 / S3+] 光源沿 track 移动后的位置通知。Light FSM（Fixed/TrackIdle/TrackDragging/TrackSnapping）非 MVP，签名先冻结避免后续破坏性改动。</summary>
        /// <param name="lightId">光源 ID（Luban TbLightSource 主键）</param>
        /// <param name="trackT">沿 track 的归一化位置（0..1）</param>
        /// <para>Sender: LightSourceController（未来）</para>
        /// <para>Listener: Shadow Puzzle（光向重算）, Audio（光源移动音效）</para>
        void OnLightPositionChanged(int lightId, float trackT);
    }
}
