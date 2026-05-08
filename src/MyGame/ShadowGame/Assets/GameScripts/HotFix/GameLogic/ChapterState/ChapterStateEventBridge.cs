// 该文件由Cursor 自动生成
using TEngine;

namespace GameLogic
{
    /// <summary>
    /// Story-004 (S2-03) wire-up: connects <see cref="ChapterStateManager"/>'s
    /// four C# <see cref="System.Action"/> callback hooks to the TEngine
    /// <see cref="IChapterStateEvent"/> dispatcher.
    ///
    /// <para>Architectural rationale (ADR-027 §1 + ADR-001):</para>
    /// <list type="bullet">
    ///   <item>ADR-001 forbids C# events / delegates for cross-module
    ///   communication. All cross-module state broadcasts must go through
    ///   TEngine <c>[EventInterface]</c> dispatchers.</item>
    ///   <item>However, <see cref="ChapterStateManager"/> is a pure data-layer
    ///   class and must not depend on TEngine. ADR-027 §7 permits a single
    ///   module to internally use explicit dependency injection (plain
    ///   <see cref="System.Action"/> hooks) provided that the boundary
    ///   between module and the rest of the game is a TEngine event dispatch.</item>
    ///   <item>This bridge is that boundary: every Action invocation in
    ///   the manager is translated 1:1 into
    ///   <c>GameEvent.Get&lt;IChapterStateEvent&gt;().OnXxx(...)</c>. No
    ///   extra logic, no transformation — pure forwarding.</item>
    /// </list>
    ///
    /// <para>Lifecycle: call <see cref="Attach"/> once, immediately after
    /// <see cref="ChapterStateManager.Init(IChapterProgress, PuzzleMeta[][])"/>
    /// succeeds but before any gameplay interaction begins. Call
    /// <see cref="Detach"/> during shutdown or on domain reload.</para>
    /// </summary>
    public static class ChapterStateEventBridge
    {
        /// <summary>
        /// Wire all four callback hooks to <see cref="IChapterStateEvent"/>
        /// dispatch. Overwrites any existing callbacks on the manager — the
        /// bridge assumes exclusive ownership of the hooks (there is only
        /// one <c>IChapterStateEvent</c> broadcast channel per game).
        /// <para>AC-8: exactly 4 forwarders are attached in this single call,
        /// matching the 4 interface methods.</para>
        /// </summary>
        public static void Attach(ChapterStateManager mgr)
        {
            if (mgr == null) return;

            mgr.OnPuzzleStateChangedCallback = (chapterId, puzzleId, newState) =>
                GameEvent.Get<IChapterStateEvent>()
                    .OnPuzzleStateChanged(chapterId, puzzleId, newState);

            mgr.OnChapterCompletedCallback = chapterId =>
                GameEvent.Get<IChapterStateEvent>().OnChapterComplete(chapterId);

            mgr.OnChapterUnlockedCallback = chapterId =>
                GameEvent.Get<IChapterStateEvent>().OnChapterUnlocked(chapterId);

            mgr.OnGameCompletedCallback = () =>
                GameEvent.Get<IChapterStateEvent>().OnGameComplete();
        }

        /// <summary>
        /// Clear all four callback hooks from the manager. Safe to call on
        /// a manager that was never attached.
        /// </summary>
        public static void Detach(ChapterStateManager mgr)
        {
            if (mgr == null) return;

            mgr.OnPuzzleStateChangedCallback = null;
            mgr.OnChapterCompletedCallback = null;
            mgr.OnChapterUnlockedCallback = null;
            mgr.OnGameCompletedCallback = null;
        }
    }
}
