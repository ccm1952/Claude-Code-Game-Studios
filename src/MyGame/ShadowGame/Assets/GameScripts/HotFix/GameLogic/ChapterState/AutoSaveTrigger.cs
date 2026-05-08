// 该文件由Cursor 自动生成
using System;
using Cysharp.Threading.Tasks;
using TEngine;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// Story-005 AC-6: listens to <see cref="IChapterStateEvent.OnChapterComplete"/>
    /// and <see cref="IChapterStateEvent.OnGameComplete"/> and drives
    /// <see cref="SaveManager.SaveAsync"/>. Single-responsibility listener with
    /// explicit <see cref="IDisposable"/> lifecycle (ADR-027 §3 — listener
    /// adds handlers in ctor, removes them in <see cref="Dispose"/>).
    /// </summary>
    /// <remarks>
    /// <para>Deliberately does NOT subscribe to <c>OnPuzzleStateChanged</c>
    /// (every frame is possible, risks I/O thrash) nor <c>OnChapterUnlocked</c>
    /// (cascaded immediately after <c>OnChapterComplete</c> — the save after
    /// <c>OnChapterComplete</c> already captures the unlock).</para>
    /// <para>Independent from <see cref="ChapterStateEventBridge"/>: the bridge
    /// owns the <em>sender</em> side (Chapter State → TEngine dispatch),
    /// <see cref="AutoSaveTrigger"/> owns a <em>listener</em> on the same
    /// interface. Both can exist simultaneously without conflict.</para>
    /// </remarks>
    public sealed class AutoSaveTrigger : IDisposable
    {
        private readonly SaveManager _saveManager;
        private readonly Func<SaveData> _currentSaveDataProvider;
        private bool _disposed;

        /// <summary>
        /// Construct and immediately subscribe to <see cref="IChapterStateEvent"/>.
        /// </summary>
        /// <param name="saveManager">Target <see cref="SaveManager"/> to call
        /// <see cref="SaveManager.SaveAsync"/> on.</param>
        /// <param name="currentSaveDataProvider">Delegate that returns the
        /// <see cref="SaveData"/> instance to persist. Called once per event.
        /// Return <c>null</c> to skip the save (a warning is logged).</param>
        public AutoSaveTrigger(SaveManager saveManager, Func<SaveData> currentSaveDataProvider)
        {
            _saveManager = saveManager ?? throw new ArgumentNullException(nameof(saveManager));
            _currentSaveDataProvider = currentSaveDataProvider
                ?? throw new ArgumentNullException(nameof(currentSaveDataProvider));

            GameEvent.AddEventListener<int>(
                IChapterStateEvent_Event.OnChapterComplete, OnChapterComplete);
            GameEvent.AddEventListener(
                IChapterStateEvent_Event.OnGameComplete, OnGameComplete);
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            GameEvent.RemoveEventListener<int>(
                IChapterStateEvent_Event.OnChapterComplete, OnChapterComplete);
            GameEvent.RemoveEventListener(
                IChapterStateEvent_Event.OnGameComplete, OnGameComplete);
        }

        private void OnChapterComplete(int chapterId) => TriggerSave($"Chapter {chapterId} complete");

        private void OnGameComplete() => TriggerSave("Game complete");

        private void TriggerSave(string reason)
        {
            if (_disposed) return;

            var data = _currentSaveDataProvider.Invoke();
            if (data == null)
            {
                Debug.LogWarning($"[AutoSaveTrigger] No SaveData available to persist ({reason}).");
                return;
            }
            _saveManager.SaveAsync(data).Forget();
        }
    }
}
