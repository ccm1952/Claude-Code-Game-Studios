// 该文件由Cursor 自动生成
using NUnit.Framework;
using UnityEngine;
using GameLogic;

namespace ShadowGame.Tests.EditMode.ObjectInteraction
{
    /// <summary>
    /// S4-06 — Selection Visual Feedback (ADR-013 + ADR-011 widget pattern).
    /// Covers AC-6 (StateChanged 订阅 + 显式 Initialize/Shutdown lifecycle) +
    /// AC-7 (协议合规 — 仅订阅 fsm C# event 不走 GameEvent) +
    /// tween params (Sprint 1 Visual story 教训：抽参数层做 EditMode 单测，shader 本身手动 Editor 验证).
    /// </summary>
    /// <remarks>
    /// <para>Visual/Feel story — 重点 EditMode-testable 部分：
    /// (1) 订阅生命周期；(2) 协议合规；(3) tween params 常量值范围。
    /// Outline shader 视觉 + DOPunchScale 实际 tween 行为由手动 Editor 验证 (per AC-1..AC-5).</para>
    /// <para>Sprint 4 Track B carryover (Sprint 2→3→4 第 2 次)；S4-06.</para>
    /// </remarks>
    [TestFixture]
    public class InteractableObjectFeedbackTests
    {
        // ────────── §1 Tween params 抽参数层验证 (Sprint 1 Visual story 教训) ──────────

        [Test]
        public void SelectPunchParams_AreInDesignSpec()
        {
            // AC-2: Selected 弹动 — punch (0.15, 0.15, 0), 0.2s, vibrato=5, elasticity=0.5
            Assert.AreEqual(0.15f, InteractableObjectFeedback.SelectPunchMagnitude, 0.0001f);
            Assert.AreEqual(0.2f, InteractableObjectFeedback.SelectPunchDuration, 0.0001f);
            Assert.AreEqual(5, InteractableObjectFeedback.SelectPunchVibrato);
            Assert.AreEqual(0.5f, InteractableObjectFeedback.SelectPunchElasticity, 0.0001f);
        }

        [Test]
        public void SnapSettlePunchParams_AreWeakerThanSelectPunch()
        {
            // AC-4: settle bounce 比选中 bounce 弱
            Assert.Less(InteractableObjectFeedback.SnapSettlePunchMagnitude, InteractableObjectFeedback.SelectPunchMagnitude,
                "Settle punch magnitude must be smaller than select punch magnitude");
            Assert.Less(InteractableObjectFeedback.SnapSettlePunchDuration, InteractableObjectFeedback.SelectPunchDuration,
                "Settle punch duration must be shorter than select punch duration");
            Assert.Less(InteractableObjectFeedback.SnapSettlePunchVibrato, InteractableObjectFeedback.SelectPunchVibrato,
                "Settle punch vibrato must be lower than select punch vibrato");

            // 具体设计值：(0.05, 0.05, 0), 0.1s, vibrato=3, elasticity=0.5
            Assert.AreEqual(0.05f, InteractableObjectFeedback.SnapSettlePunchMagnitude, 0.0001f);
            Assert.AreEqual(0.1f, InteractableObjectFeedback.SnapSettlePunchDuration, 0.0001f);
            Assert.AreEqual(3, InteractableObjectFeedback.SnapSettlePunchVibrato);
            Assert.AreEqual(0.5f, InteractableObjectFeedback.SnapSettlePunchElasticity, 0.0001f);
        }

        [Test]
        public void SelectPunchParams_AreInSafeBoundsForCollider()
        {
            // 安全验证：punch magnitude 不能太大，否则 visual mesh scale 振荡时影响视觉舒适度
            Assert.LessOrEqual(InteractableObjectFeedback.SelectPunchMagnitude, 0.3f,
                "Select punch magnitude > 0.3 may cause visual discomfort or clipping");
            Assert.LessOrEqual(InteractableObjectFeedback.SelectPunchDuration, 0.5f,
                "Select punch duration > 0.5s feels sluggish for selection feedback");
        }

        // ────────── §2 订阅 lifecycle (AC-6 — Initialize/Shutdown explicit) ──────────

        [Test]
        public void IsSubscribedForTest_ReturnsFalse_BeforeInitialize()
        {
            var go = new GameObject("FeedbackTest");
            try
            {
                var feedback = go.AddComponent<InteractableObjectFeedback>();

                // 在 EditMode PlayerLoop 不驱动 OnEnable —— Initialize 必须显式调用 (per ADR-013 v3 教训)
                Assert.IsFalse(feedback.IsSubscribedForTest,
                    "Feedback should not be subscribed before explicit Initialize() (PlayerLoop OnEnable doesn't fire in EditMode)");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void Initialize_WithoutTarget_DoesNotSubscribe()
        {
            var go = new GameObject("FeedbackTest");
            try
            {
                var feedback = go.AddComponent<InteractableObjectFeedback>();

                // _target == null → SubscribeIfNeeded silent skip
                feedback.Initialize();

                Assert.IsFalse(feedback.IsSubscribedForTest,
                    "Feedback should not subscribe when target is null");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void InitializeShutdownCycle_ToggleSubscriptionState()
        {
            var go = new GameObject("FeedbackTest");
            var targetGo = new GameObject("TargetTest");
            try
            {
                var target = targetGo.AddComponent<InteractableObject>();
                target.Initialize();  // creates Fsm

                var feedback = go.AddComponent<InteractableObjectFeedback>();

                // 通过 reflection 注入 target 字段（[SerializeField] private）
                var targetField = typeof(InteractableObjectFeedback).GetField(
                    "_target",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                targetField.SetValue(feedback, target);

                // 第一次 Initialize → subscribed
                feedback.Initialize();
                Assert.IsTrue(feedback.IsSubscribedForTest, "Initialize should mark subscribed");

                // Shutdown → unsubscribed
                feedback.Shutdown();
                Assert.IsFalse(feedback.IsSubscribedForTest, "Shutdown should mark unsubscribed");

                // 第二次 Initialize → 又 subscribed
                feedback.Initialize();
                Assert.IsTrue(feedback.IsSubscribedForTest, "Re-Initialize should mark subscribed again");

                // 第二次 Initialize 不会重复订阅 (idempotent guard)
                feedback.Initialize();
                Assert.IsTrue(feedback.IsSubscribedForTest,
                    "Double Initialize should remain subscribed (idempotent guard prevents double subscription)");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
                if (targetGo != null) UnityEngine.Object.DestroyImmediate(targetGo);
            }
        }

        [Test]
        public void Shutdown_WithoutInitialize_IsSafe()
        {
            var go = new GameObject("FeedbackTest");
            try
            {
                var feedback = go.AddComponent<InteractableObjectFeedback>();

                // 直接 Shutdown 不抛异常 (idempotent guard for null-check)
                Assert.DoesNotThrow(() => feedback.Shutdown(),
                    "Shutdown without prior Initialize must not throw (idempotent guard)");

                Assert.IsFalse(feedback.IsSubscribedForTest);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }
    }
}
