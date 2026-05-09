// 该文件由Cursor 自动生成
// 开发测试 FSM 状态。整文件仅在 UNITY_EDITOR || DEBUG 编译。
// 生命周期：
//   * 由 GameLoadingState.OnLoadingComplete（DEBUG 分支）切入
//   * OnEnter 启动所有已注册 Spike；有 Spike → 不推进（停留，保留 OnGUI）
//   * 无 Spike → 回落到 GameLobbyState 保持路径可达

#if UNITY_EDITOR || DEBUG
using System;
using System.Reflection;
using Cysharp.Threading.Tasks;
using TEngine;

namespace GameLogic
{
    public class DevTestState : GameFlowState
    {
        protected override void OnEnter(IFsm<IFsmModule> fsm)
        {
            base.OnEnter(fsm);
            Log.Info("[GameFlow] 进入 DevTestState —— 开发测试模式");

            if (DevTest.DevBootstrap.PendingCount == 0)
            {
                Log.Info("[GameFlow] 未注册任何 Spike，直接进入 GameLobbyState");
                ChangeState<GameLobbyState>(fsm);
                return;
            }

            // S5-1b production SceneManager 真接入触发（M1 + F4 模式 sign-off 2026-05-09）：
            //   1. 派 OnRequestSceneChange(1) — 验 listener handler 真订阅 + state guard 推进到 TransitionOut；
            //   2. RunRequested() — spike Launch；
            //   3. delayed driver — 800ms 后 await BeginTransitionAsync(1) 自驱 11-step（ADR-009 listener-path
            //      driver 缺失 deficiency-flagged；DevTestState 临时承担 dev-only driver 职责，evidence doc 内 surface）。
            //   delay 留给 spike Runtime.Start + listener subscribe，避免 OnSceneTransitionBegin 在 listener attach 前派出。
            GameEvent.Get<ISceneEvent>().OnRequestSceneChange(1);
            Log.Info("[GameFlow] [S5-1b] 已派发 ISceneEvent.OnRequestSceneChange(1) → listener handler 接收推进 state=TransitionOut");

            DevTest.DevBootstrap.RunRequested();
            Log.Info("[GameFlow] DevTestState 停留：等待 Spike 结果（手动停 PlayMode 结束）");

            DriveProductionSceneTransitionAsync(targetChapterId: 1, delayMs: 800).Forget();
        }

        /// <summary>
        /// S5-1b F4 driver — DevTestState 临时承担 production SceneManager 11-step 驱动职责（ADR-009 deficiency-flagged，
        /// listener-path driver 后续 ADR-009 driver story 补齐）。反射拿 GameApp._sceneManager + delayed await BeginTransitionAsync。
        /// </summary>
        private static async UniTaskVoid DriveProductionSceneTransitionAsync(int targetChapterId, int delayMs)
        {
            await UniTask.Delay(TimeSpan.FromMilliseconds(delayMs));

            var fi = typeof(GameApp).GetField("_sceneManager", BindingFlags.NonPublic | BindingFlags.Static);
            if (fi == null)
            {
                Log.Error("[GameFlow] [S5-1b] 反射拿 GameApp._sceneManager 字段失败 — driver 跳过");
                return;
            }
            var prodScene = fi.GetValue(null) as SceneManager;
            if (prodScene == null)
            {
                Log.Error("[GameFlow] [S5-1b] GameApp._sceneManager == null — driver 跳过");
                return;
            }

            try
            {
                Log.Info($"[GameFlow] [S5-1b][F4] driver 启动: await BeginTransitionAsync({targetChapterId}) — 模拟业务 boot driver 调用 11-step");
                await prodScene.BeginTransitionAsync(targetChapterId);
                Log.Info($"[GameFlow] [S5-1b][F4] driver done: BeginTransitionAsync({targetChapterId}) 完成 + state={prodScene.CurrentState}");
            }
            catch (Exception e)
            {
                Log.Error($"[GameFlow] [S5-1b][F4] driver 异常：{e}");
            }
        }

        protected override void OnLeave(IFsm<IFsmModule> fsm, bool isShutdown)
        {
            base.OnLeave(fsm, isShutdown);
            Log.Info("[GameFlow] 离开 DevTestState");
        }
    }
}
#endif
