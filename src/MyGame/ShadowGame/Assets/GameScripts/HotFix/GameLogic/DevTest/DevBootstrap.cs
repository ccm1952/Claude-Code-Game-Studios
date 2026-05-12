// 该文件由Cursor 自动生成
// 开发测试启动器：由 DevTestState.OnEnter 调 RunRequested()。
// 注册时机：GameApp.Entrance 在 StartGameLogic 之前调用 Register(new XxxSpike())。

#if UNITY_EDITOR || DEBUG
using System;
using System.Collections.Generic;
using TEngine;

namespace GameLogic.DevTest
{
    public static class DevBootstrap
    {
        private static readonly List<IDevSpike> _pending = new List<IDevSpike>();

        public static int PendingCount => _pending.Count;

        /// <summary>
        /// 检查指定 Spike Id 是否已注册（用于 DevTestState 分支判断）。
        /// 用例: S5-02 dev-story 需要 DevTestState 区分 "S5-02 main menu 模式" vs "S5-1c 直 fire 模式"；
        /// 历史 spike (S5-1c) 在 OnEnter 内自动 fire OnRequestSceneChange，新 spike (S5-02) 走 main menu Button click 路径。
        /// </summary>
        public static bool HasSpike(string id)
        {
            if (string.IsNullOrEmpty(id)) return false;
            for (var i = 0; i < _pending.Count; i++)
            {
                if (string.Equals(_pending[i].Id, id, System.StringComparison.Ordinal)) return true;
            }
            return false;
        }

        public static void Register(IDevSpike spike)
        {
            if (spike == null)
            {
                Log.Warning("[DevBootstrap] Register 收到 null spike，已忽略");
                return;
            }
            _pending.Add(spike);
            Log.Info($"[DevBootstrap] 已注册 Spike: {spike.Id} - {spike.Name}");
        }

        public static void RunRequested()
        {
            if (_pending.Count == 0)
            {
                Log.Info("[DevBootstrap] 无待运行 Spike");
                return;
            }

            Log.Info($"[DevBootstrap] ═══ 开始运行 {_pending.Count} 个 Spike ═══");
            foreach (var s in _pending)
            {
                try
                {
                    Log.Info($"[DevBootstrap] → Launch {s.Id} - {s.Name}");
                    s.Launch();
                }
                catch (Exception e)
                {
                    Log.Error($"[DevBootstrap] Spike {s.Id} Launch 异常: {e}");
                }
            }
        }
    }
}
#endif
