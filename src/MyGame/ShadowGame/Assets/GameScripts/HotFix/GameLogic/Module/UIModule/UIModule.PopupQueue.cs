// 该文件由Cursor 自动生成
using System;
using System.Collections.Generic;
using TEngine;

namespace GameLogic
{
    /// <summary>
    /// 弹窗队列请求项。
    /// </summary>
    internal class PopupRequest
    {
        public Type WindowType;
        public int Priority;
        public int EnqueueOrder;
        public object[] UserDatas;
    }

    /// <summary>
    /// UIModule 弹窗队列扩展。
    /// <para>独立于 _uiStack 管理，排队中的弹窗不进入 _uiStack，轮到时才调用 ShowUI。</para>
    /// <para>支持数字优先级（值越大越优先）、单实例复用、暂停/恢复/清空。</para>
    /// </summary>
    public sealed partial class UIModule
    {
        private readonly List<PopupRequest> _popupQueue = new List<PopupRequest>(32);
        private int _enqueueCounter;
        private Type _currentPopupType;
        private bool _isPopupQueuePaused;

        /// <summary>
        /// 当前队列中等待弹出的数量。
        /// </summary>
        public int PopupQueueCount => _popupQueue.Count;

        /// <summary>
        /// 队列是否暂停中。
        /// </summary>
        public bool IsPopupQueuePaused => _isPopupQueuePaused;

        /// <summary>
        /// 当前是否有队列弹窗正在展示。
        /// </summary>
        public bool HasActivePopup => _currentPopupType != null;

        /// <summary>
        /// 将弹窗加入队列。
        /// </summary>
        /// <param name="priority">优先级，值越大越优先。同优先级按添加顺序。</param>
        /// <param name="userDatas">传递给 UIWindow 的自定义数据（在 OnRefresh 中通过 UserDatas 读取）。</param>
        /// <typeparam name="T">UIWindow 子类。</typeparam>
        public void EnqueuePopup<T>(int priority = 0, params object[] userDatas) where T : UIWindow, new()
        {
            EnqueuePopup(typeof(T), priority, userDatas);
        }

        /// <summary>
        /// 将弹窗加入队列。
        /// </summary>
        /// <param name="type">UIWindow 类型。</param>
        /// <param name="priority">优先级，值越大越优先。同优先级按添加顺序。</param>
        /// <param name="userDatas">传递给 UIWindow 的自定义数据。</param>
        public void EnqueuePopup(Type type, int priority = 0, params object[] userDatas)
        {
            var request = new PopupRequest
            {
                WindowType = type,
                Priority = priority,
                EnqueueOrder = _enqueueCounter++,
                UserDatas = userDatas
            };

            InsertByPriority(request);

            if (_currentPopupType == null && !_isPopupQueuePaused)
            {
                TryShowNextPopup();
            }
        }

        /// <summary>
        /// 暂停弹窗队列，当前正在展示的弹窗不受影响，但不会再弹出下一个。
        /// </summary>
        public void PausePopupQueue()
        {
            _isPopupQueuePaused = true;
        }

        /// <summary>
        /// 恢复弹窗队列，如果当前没有弹窗在展示则立即弹出下一个。
        /// </summary>
        public void ResumePopupQueue()
        {
            _isPopupQueuePaused = false;

            if (_currentPopupType == null)
            {
                TryShowNextPopup();
            }
        }

        /// <summary>
        /// 清空弹窗队列。不会关闭当前正在展示的队列弹窗。
        /// </summary>
        public void ClearPopupQueue()
        {
            _popupQueue.Clear();
        }

        /// <summary>
        /// 清空弹窗队列并关闭当前正在展示的队列弹窗。
        /// </summary>
        public void ClearAndClosePopupQueue()
        {
            _popupQueue.Clear();

            if (_currentPopupType != null)
            {
                var type = _currentPopupType;
                _currentPopupType = null;
                CloseUI(type);
            }
        }

        /// <summary>
        /// 由 CloseUI / HideUI 调用的钩子。当关闭或隐藏的窗口是当前队列弹窗时，自动弹出下一个。
        /// </summary>
        private void OnPopupClosed(Type type)
        {
            if (_currentPopupType == null || _currentPopupType != type)
            {
                return;
            }

            _currentPopupType = null;

            if (!_isPopupQueuePaused)
            {
                TryShowNextPopup();
            }
        }

        /// <summary>
        /// 尝试从队列中弹出下一个弹窗。
        /// </summary>
        private void TryShowNextPopup()
        {
            if (_popupQueue.Count == 0)
            {
                return;
            }

            var request = _popupQueue[0];
            _popupQueue.RemoveAt(0);

            _currentPopupType = request.WindowType;

            ShowUIImp(request.WindowType, true, request.UserDatas);
        }

        /// <summary>
        /// 按优先级插入队列（priority DESC, enqueueOrder ASC）。
        /// </summary>
        private void InsertByPriority(PopupRequest request)
        {
            int insertIndex = _popupQueue.Count;

            for (int i = 0; i < _popupQueue.Count; i++)
            {
                var existing = _popupQueue[i];
                if (request.Priority > existing.Priority ||
                    (request.Priority == existing.Priority && request.EnqueueOrder < existing.EnqueueOrder))
                {
                    insertIndex = i;
                    break;
                }
            }

            _popupQueue.Insert(insertIndex, request);
        }
    }
}
