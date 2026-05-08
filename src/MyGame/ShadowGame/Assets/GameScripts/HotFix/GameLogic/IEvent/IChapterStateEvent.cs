// 该文件由Cursor 自动生成
using TEngine;

namespace GameLogic
{
    /// <summary>
    /// Chapter State 广播接口（ChapterStateManager → UI / Save / Narrative / Audio）。
    /// <para>Sender: ChapterStateManager（唯一发送方；通过 <see cref="ChapterStateEventBridge"/> 转发 C# Action callback 到本接口）</para>
    /// <para>Listener: ChapterSelectUI, SaveAutoTrigger, NarrativeDirector, AudioController</para>
    /// <para>协议来源：ADR-027 §1 取代 ADR-006 §1/§2；生命周期 / 顺序保证 / 文档约定继承 ADR-006 §3/§5/§6</para>
    /// </summary>
    [EventInterface(EEventGroup.GroupLogic)]
    public interface IChapterStateEvent
    {
        /// <summary>
        /// 某个 puzzle 的 <see cref="PuzzleStateEnum"/> 发生了实际改变（同值赋值不派发）。
        /// <para>Sender: ChapterStateManager（Init 首 Idle 提升 / SetPuzzleState 成功路径 / OnPuzzleComplete 的 Complete 赋值与下一 Idle 提升）</para>
        /// <para>Listener: ShadowPuzzle FSM, Narrative triggers, SaveManager auto-save, HUD puzzle-state indicator</para>
        /// <para>Cascade depth: 1（监听者可触发 OnChapterComplete → 后续事件，合计深度 ≤ 3；不得再深）</para>
        /// </summary>
        void OnPuzzleStateChanged(int chapterId, int puzzleId, PuzzleStateEnum newState);

        /// <summary>
        /// 某章节全部 puzzle 达到 <see cref="PuzzleStateEnum.Complete"/>，
        /// <see cref="ChapterProgress.IsCompleted"/> 翻转为 <c>true</c>。
        /// <para>Sender: ChapterStateManager（<c>CheckChapterCompletion</c> 主路径；每章节严格一次）</para>
        /// <para>Listener: NarrativeDirector（章节总结）, AudioController（片尾 BGM）, SaveManager（auto-save）</para>
        /// <para>Cascade depth: 2（监听者可触发 OnRequestSceneChange 等后续事件，合计深度 ≤ 3）</para>
        /// </summary>
        void OnChapterComplete(int chapterId);

        /// <summary>
        /// 新章节 <see cref="ChapterProgress.IsUnlocked"/> 从 <c>false</c> 翻转为 <c>true</c>。
        /// <para>Sender: ChapterStateManager（<c>TryUnlockChapter</c> 成功路径）</para>
        /// <para>Listener: ChapterSelectUI（主菜单解锁提示）, SaveManager</para>
        /// <para>Cascade depth: 1（监听者不得进一步派发状态事件；UI 可继续渲染）</para>
        /// </summary>
        void OnChapterUnlocked(int chapterId);

        /// <summary>
        /// Chapter 5 完成 — 全 5 章通关。本事件触发后不再有
        /// <see cref="OnChapterUnlocked"/>（无 Chapter 6）。
        /// <para>Sender: ChapterStateManager（<c>CheckChapterCompletion</c> 的 Ch.5 分支）</para>
        /// <para>Listener: NarrativeDirector（片尾字幕）, CreditsUI, SaveManager（锁定存档 + 解锁 replay 模式）</para>
        /// <para>Cascade depth: 1（监听者可 <c>await UniTask.Yield()</c> 跳离当前栈做片尾序列）</para>
        /// </summary>
        void OnGameComplete();
    }
}
