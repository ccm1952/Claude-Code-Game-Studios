// 该文件由Cursor 自动生成
using System;

namespace GameLogic
{
    /// <summary>
    /// Whitelist-based gesture filter. When active, only gestures whose
    /// <see cref="GestureType"/> appears in <see cref="ActiveFilterGestures"/>
    /// are dispatched — all others are silently discarded.
    /// <para>
    /// Single-active semantics: a new <see cref="PushFilter"/> overwrites the
    /// previous filter (no stacking). <see cref="PopFilter"/> restores the
    /// "no filter" state (does NOT restore a previous filter).
    /// </para>
    /// </summary>
    public class InputFilter
    {
        private GestureType[] _activeFilter;

        private static readonly GestureType[] s_empty = Array.Empty<GestureType>();

        public bool IsFiltered => _activeFilter != null;
        public GestureType[] ActiveFilterGestures => _activeFilter ?? s_empty;

        /// <summary>
        /// Set the active whitelist. Deep-copies <paramref name="allowedGestures"/>
        /// to prevent external mutation. Overwrites any previous filter.
        /// </summary>
        public void PushFilter(GestureType[] allowedGestures)
        {
            if (allowedGestures == null || allowedGestures.Length == 0)
            {
                _activeFilter = s_empty;
                return;
            }
            _activeFilter = (GestureType[])allowedGestures.Clone();
        }

        /// <summary>
        /// Remove the active filter. All gesture types pass through.
        /// Does NOT restore a previous filter.
        /// </summary>
        public void PopFilter()
        {
            _activeFilter = null;
        }

        /// <summary>
        /// Check whether a gesture type is allowed by the current filter.
        /// Returns <c>true</c> if no filter is active or the type is whitelisted.
        /// </summary>
        public bool IsAllowed(GestureType type)
        {
            if (_activeFilter == null)
                return true;

            for (int i = 0; i < _activeFilter.Length; i++)
            {
                if (_activeFilter[i] == type)
                    return true;
            }
            return false;
        }
    }
}
