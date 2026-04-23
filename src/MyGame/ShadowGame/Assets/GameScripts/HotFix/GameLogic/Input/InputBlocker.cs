// 该文件由Cursor 自动生成
using System.Collections.Generic;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// Stack-based input blocker. When any blocker token is active, ALL gesture input
    /// is suppressed (highest priority gate in the input pipeline).
    /// <para>
    /// Tokens are matched by string value — <see cref="PopBlocker"/> only removes the
    /// specific token that was pushed, preventing accidental cross-module unlocks.
    /// </para>
    /// </summary>
    public class InputBlocker
    {
        private readonly List<string> _tokens = new List<string>(4);
        private readonly List<float> _pushTimes = new List<float>(4);
        private float _lastLeakCheckTime;

        private const float LeakThresholdSeconds = 30f;
        private const float LeakCheckIntervalSeconds = 1f;

        public bool IsBlocked => _tokens.Count > 0;
        public int BlockerCount => _tokens.Count;

        public void PushBlocker(string token)
        {
            _tokens.Add(token);
            _pushTimes.Add(Time.realtimeSinceStartup);
        }

        public void PopBlocker(string token)
        {
            int idx = _tokens.LastIndexOf(token);
            if (idx < 0)
            {
                Debug.LogWarning($"InputBlocker: attempted to pop token '{token}' which is not in stack");
                return;
            }
            _tokens.RemoveAt(idx);
            _pushTimes.RemoveAt(idx);
        }

        public void ForcePopAllBlockers()
        {
            Debug.LogWarning("InputBlocker: ForcePopAllBlockers() called — all blockers cleared");
            _tokens.Clear();
            _pushTimes.Clear();
        }

        /// <summary>
        /// Call once per frame. Checks for tokens that have been active longer than
        /// <see cref="LeakThresholdSeconds"/> and logs a warning.
        /// </summary>
        public void CheckLeaks(float realtimeSinceStartup)
        {
            if (realtimeSinceStartup - _lastLeakCheckTime < LeakCheckIntervalSeconds)
                return;
            _lastLeakCheckTime = realtimeSinceStartup;

            for (int i = 0; i < _pushTimes.Count; i++)
            {
                float alive = realtimeSinceStartup - _pushTimes[i];
                if (alive > LeakThresholdSeconds)
                {
                    Debug.LogWarning(
                        $"InputBlocker: token '{_tokens[i]}' has been active for {alive:F1}s, possible leak");
                }
            }
        }
    }
}
