// 该文件由Cursor 自动生成
using System;
using NUnit.Framework;
using GameLogic;
using TEngine;

namespace ShadowGame.Tests.EditMode.SceneManagement
{
    /// <summary>
    /// S2-07 — Luban Scene Name ↔ Chapter ID Mapping
    /// (story-006-luban-scene-mapping.md, ADR-007 + ADR-009 + ADR-027).
    ///
    /// <para>当前 Luban TbChapter 表尚未生成（详见 <c>Tables.cs</c> 仅有 demo TbItem）；
    /// 本 fixture 通过 <c>Func&lt;int, ChapterData&gt;</c> provider 注入手工 fixture
    /// 验证 SceneManager 的 ChapterData 解析与 fail-loud 错误处理路径。
    /// 真实 Luban 接入由后续 story 处理（boot pipeline 改注册
    /// <c>id =&gt; ConfigSystem.Tables.TbChapter.Get(id)</c>，本 fixture 仍可保留为 unit test）。</para>
    ///
    /// <para><b>柔和 fail-loud 语义</b>：provider 未注册时 SceneManager 跳过校验
    /// （兼容 S2-05/S2-06 baseline），provider 注册了但返 <c>null</c> 时进 Error。
    /// 详见 <see cref="SceneManager"/>.<c>TryResolveOrFail</c> 注释。</para>
    /// </summary>
    [TestFixture]
    public class LubanSceneMappingTests
    {
        private SceneManager _sm;
        private int _sceneLoadFailedCount;
        private int _lastFailedChapterId;
        private string _lastFailedReason;

        // ────────── Fixture lifecycle ──────────

        [SetUp]
        public void SetUp()
        {
            GameEvent.EventMgr.Init();
            _ = new ISceneEvent_Gen(GameEvent.EventMgr.GetDispatcher());

            _sceneLoadFailedCount = 0;
            _lastFailedChapterId = 0;
            _lastFailedReason = null;
            GameEvent.AddEventListener<int, string>(
                ISceneEvent_Event.OnSceneLoadFailed, OnSceneLoadFailedHandler);

            _sm = new SceneManager();
            _sm.Init();
        }

        [TearDown]
        public void TearDown()
        {
            GameEvent.RemoveEventListener<int, string>(
                ISceneEvent_Event.OnSceneLoadFailed, OnSceneLoadFailedHandler);
            _sm?.Dispose();
            _sm = null;
        }

        private void OnSceneLoadFailedHandler(int chapterId, string reason)
        {
            _sceneLoadFailedCount++;
            _lastFailedChapterId = chapterId;
            _lastFailedReason = reason;
        }

        private static void FireRequest(int chapterId) =>
            GameEvent.Get<ISceneEvent>().OnRequestSceneChange(chapterId);

        /// <summary>测试 fixture：5 章 in-memory ChapterData（仅本 fixture 内可见，不污染 production code）。</summary>
        private static ChapterData ResolveFixture(int id)
        {
            return id switch
            {
                1 => new ChapterData(1, "Test_Chapter_01", "test/bgm/01", 0.8f, "#FFE6D5"),
                2 => new ChapterData(2, "Test_Chapter_02", "test/bgm/02", 1.0f, "#E6F0FF"),
                3 => new ChapterData(3, "Test_Chapter_03", "test/bgm/03", 1.0f, "#FFF5E1"),
                4 => new ChapterData(4, "Test_Chapter_04", "test/bgm/04", 1.2f, "#D5DCE6"),
                5 => new ChapterData(5, "Test_Chapter_05", "test/bgm/05", 1.5f, "#A8B4BF"),
                _ => null,
            };
        }

        // ==================================================================
        // §1. ChapterData POCO（构造器 + 字段不可变）
        // ==================================================================

        [Test]
        public void ChapterData_Constructor_AssignsAllFields()
        {
            var data = new ChapterData(2, "Chapter_02_SharedSpace", "bgm/sharedspace", 1.0f, "#E6F0FF");
            Assert.AreEqual(2, data.Id);
            Assert.AreEqual("Chapter_02_SharedSpace", data.SceneId);
            Assert.AreEqual("bgm/sharedspace", data.BgmAsset);
            Assert.AreEqual(1.0f, data.EmotionalWeight);
            Assert.AreEqual("#E6F0FF", data.OverlayColor);
        }

        [Test]
        public void ChapterData_DefaultEmotionalWeightAndOverlay_AreSane()
        {
            var data = new ChapterData(1, "X", "bgm/x");
            Assert.AreEqual(1.0f, data.EmotionalWeight, "默认 EmotionalWeight 应为 1.0（中性 fade 时长）。");
            Assert.AreEqual("#FFFFFF", data.OverlayColor, "默认 OverlayColor 应为白色（无色调染色）。");
        }

        // ==================================================================
        // §2. Provider 默认未注册 → 兼容旧行为（不校验）
        // ==================================================================

        [Test]
        public void DefaultNoProvider_RequestAnyChapter_LegacyPath_NoFailLoud()
        {
            // 未调 RegisterChapterDataProvider；请求合法章节应进过渡（兼容 S2-05 baseline）
            FireRequest(2);

            Assert.AreEqual(SceneManagerState.TransitionOut, _sm.CurrentState,
                "未注册 provider 时 SceneManager 跳过校验，走旧行为进 TransitionOut。");
            Assert.AreEqual(0, _sceneLoadFailedCount,
                "未注册 provider 不应触发 OnSceneLoadFailed（柔和 fail-loud 语义）。");
            Assert.AreEqual(2, _sm.CurrentChapterId);
            Assert.AreEqual(2, _sm.InflightChapterIdForTest);
        }

        // ==================================================================
        // §3. Provider 已注册：已知章节通过 / 未知章节进 Error
        // ==================================================================

        [Test]
        public void RegisteredProvider_KnownChapter_PassesThroughToTransitionOut()
        {
            _sm.RegisterChapterDataProvider(ResolveFixture);

            FireRequest(1);

            Assert.AreEqual(SceneManagerState.TransitionOut, _sm.CurrentState);
            Assert.AreEqual(1, _sm.CurrentChapterId);
            Assert.AreEqual(1, _sm.InflightChapterIdForTest);
            Assert.AreEqual(0, _sceneLoadFailedCount,
                "已知章节不应触发 OnSceneLoadFailed。");
        }

        [Test]
        public void RegisteredProvider_UnknownChapter_FailLoud_EntersErrorAndDispatchesFailed()
        {
            _sm.RegisterChapterDataProvider(ResolveFixture);
            int currentBefore = _sm.CurrentChapterId; // 应为 NoChapterId
            int inflightBefore = _sm.InflightChapterIdForTest;

            FireRequest(99);

            Assert.AreEqual(SceneManagerState.Error, _sm.CurrentState,
                "未知章节应直接进 Error 状态。");
            Assert.AreEqual(1, _sceneLoadFailedCount,
                "未知章节应派发恰好 1 次 OnSceneLoadFailed。");
            Assert.AreEqual(99, _lastFailedChapterId,
                "OnSceneLoadFailed 携带请求的 chapterId。");
            Assert.IsNotNull(_lastFailedReason);
            Assert.IsTrue(_lastFailedReason.Contains("99"),
                $"OnSceneLoadFailed reason 应包含 chapterId 字面值；actual='{_lastFailedReason}'.");
            Assert.AreEqual(currentBefore, _sm.CurrentChapterId,
                "fail-loud 路径不应更新 _currentChapterId。");
            Assert.AreEqual(inflightBefore, _sm.InflightChapterIdForTest,
                "fail-loud 路径不应更新 _inflightChapterId。");
        }

        [Test]
        public void RegisterProvider_PassNull_RestoresLegacyBehavior()
        {
            _sm.RegisterChapterDataProvider(ResolveFixture);
            _sm.RegisterChapterDataProvider(null); // 清除

            FireRequest(99); // 99 在 fixture 里是未知，但 provider 已被清

            Assert.AreEqual(SceneManagerState.TransitionOut, _sm.CurrentState,
                "provider 被清除后回到旧行为，不校验，请求 99 进 TransitionOut。");
            Assert.AreEqual(0, _sceneLoadFailedCount,
                "provider 已清除 → 不应触发 fail-loud。");
        }

        // ==================================================================
        // §4. DrainPending 路径上的 ChapterData 校验
        // ==================================================================

        [Test]
        public void DrainPending_KnownChapter_PassesThroughToTransitionOut()
        {
            _sm.RegisterChapterDataProvider(ResolveFixture);

            FireRequest(1); // Idle → TransitionOut, current=1, inflight=1
            _sm.AdvanceStateForTest(SceneManagerState.Unloading);
            _sm.AdvanceStateForTest(SceneManagerState.Loading);

            FireRequest(2); // pending=2
            Assert.AreEqual(2, _sm.PendingTargetChapterIdForTest);

            _sm.AdvanceStateForTest(SceneManagerState.TransitionIn);
            _sm.AdvanceStateForTest(SceneManagerState.Idle); // drain → 校验 + 启新过渡

            Assert.AreEqual(SceneManagerState.TransitionOut, _sm.CurrentState,
                "drain 出来的合法章节应通过校验进 TransitionOut。");
            Assert.AreEqual(2, _sm.CurrentChapterId);
            Assert.AreEqual(2, _sm.InflightChapterIdForTest);
            Assert.AreEqual(0, _sceneLoadFailedCount);
        }

        [Test]
        public void DrainPending_UnknownChapter_FailLoud_EntersError()
        {
            _sm.RegisterChapterDataProvider(ResolveFixture);

            FireRequest(1);
            _sm.AdvanceStateForTest(SceneManagerState.Unloading);
            _sm.AdvanceStateForTest(SceneManagerState.Loading);

            FireRequest(99); // pending=99（fixture 里未知，但当前 Loading 过渡，此时不查 provider）
            Assert.AreEqual(99, _sm.PendingTargetChapterIdForTest,
                "Loading 中收到未知 chapter 请求 → 入 pending（pending 入队不查 provider）。");

            _sm.AdvanceStateForTest(SceneManagerState.TransitionIn);
            _sm.AdvanceStateForTest(SceneManagerState.Idle); // drain → 校验失败

            Assert.AreEqual(SceneManagerState.Error, _sm.CurrentState,
                "drain 出来的未知章节应触发 fail-loud 进 Error。");
            Assert.AreEqual(1, _sceneLoadFailedCount,
                "drain 失败应派发恰好 1 次 OnSceneLoadFailed。");
            Assert.AreEqual(99, _lastFailedChapterId);
            Assert.IsNull(_sm.PendingTargetChapterIdForTest,
                "pending 已被 DrainPending 消费清空（即使后续校验失败）。");
            Assert.AreEqual(1, _sm.CurrentChapterId,
                "drain fail-loud 不应更新 _currentChapterId（保留进 drain 时的值 1）。");
        }
    }
}
