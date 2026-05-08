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
