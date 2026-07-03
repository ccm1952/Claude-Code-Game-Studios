// 该文件由Cursor 自动生成
// S6-16 story-007 ShadowMatch production wire — MVP fixture pose-distance scoring。
//
// ADR-012 完整 multi-anchor + AsyncGPUReadback pipeline deferred to shadow-puzzle epic
// (story-002/story-003)。本 VS Chapter 1 emergent fix 仅落地：
//   * IInteractionEvent.OnObjectTransformChanged listener（非 polling Update）
//   * 连续 matchScore ∈ [0,1] + epsilon-gated IShadowMatchEvent.OnMatchScoreUpdated fire
//   * chapter 1 puzzle 1 fixture target poses（S5-01 scene Object_01/02 默认布局衍生）
//
// Production 升级路径：future shadow-puzzle epic 替换 CalculatePuzzleScore 内部为 ADR-012 真算法，
// listener 签名与 GameApp 生命周期保持不变。

using System;
using System.Collections.Generic;
using TEngine;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// Shadow match calculator — MVP chapter 1 fixture（per story-007 Scenario C inline impl）。
    /// <para>Listener: <see cref="IInteractionEvent.OnObjectTransformChanged"/>（per-event AddEventListener）。</para>
    /// <para>Sender: <see cref="IShadowMatchEvent.OnMatchScoreUpdated"/>（epsilon-gated；PuzzleStateMachine 已订阅）。</para>
    /// </summary>
    public sealed class ShadowMatchCalculator
    {
        private const float ScorePublishEpsilon = 0.01f;
        private const float DefaultMaxPositionDistance = 2.0f;
        private const float DefaultMaxAngleDegrees = 45f;

        private readonly struct ObjectMatchTarget
        {
            public readonly int ObjectId;
            public readonly int PuzzleId;
            public readonly Vector3 TargetPosition;
            public readonly Quaternion TargetRotation;

            public ObjectMatchTarget(int objectId, int puzzleId, Vector3 targetPosition, Quaternion targetRotation)
            {
                ObjectId = objectId;
                PuzzleId = puzzleId;
                TargetPosition = targetPosition;
                TargetRotation = targetRotation;
            }
        }

        private readonly Dictionary<int, ObjectMatchTarget> _targetsByObjectId = new Dictionary<int, ObjectMatchTarget>();
        private readonly Dictionary<int, Vector3> _latestPositionByObjectId = new Dictionary<int, Vector3>();
        private readonly Dictionary<int, Quaternion> _latestRotationByObjectId = new Dictionary<int, Quaternion>();
        private readonly Dictionary<int, float> _lastPublishedScoreByPuzzleId = new Dictionary<int, float>();

        private Action<int, Vector3, Quaternion> _onObjectTransformChanged;
        private bool _initialized;

        public bool IsInitialized => _initialized;

        public void Init()
        {
            if (_initialized)
            {
                Log.Warning("[ShadowMatchCalculator] 已初始化，忽略重复 Init");
                return;
            }

            RegisterChapter1FixtureTargets();

            _onObjectTransformChanged = OnObjectTransformChangedHandler;
            GameEvent.AddEventListener<int, Vector3, Quaternion>(
                IInteractionEvent_Event.OnObjectTransformChanged, _onObjectTransformChanged);

            _initialized = true;
            Log.Info("[ShadowMatchCalculator] Init 完成 — 已订阅 OnObjectTransformChanged（chapter 1 fixture targets）");
        }

        public void Dispose()
        {
            if (!_initialized) return;

            if (_onObjectTransformChanged != null)
            {
                GameEvent.RemoveEventListener<int, Vector3, Quaternion>(
                    IInteractionEvent_Event.OnObjectTransformChanged, _onObjectTransformChanged);
                _onObjectTransformChanged = null;
            }

            _targetsByObjectId.Clear();
            _latestPositionByObjectId.Clear();
            _latestRotationByObjectId.Clear();
            _lastPublishedScoreByPuzzleId.Clear();
            _initialized = false;
            Log.Info("[ShadowMatchCalculator] Dispose 完成");
        }

        /// <summary>chapter 1 puzzle 1 — Object_01/02 目标位姿 fixture（沿 Chapter_01_Approach scene 默认 grid 对齐位姿 ±1 X）。</summary>
        private void RegisterChapter1FixtureTargets()
        {
            var identity = Quaternion.identity;
            // S6-17 story-008 e2e 实证：gridSize=1 snap 落点与 ±0.5 目标错位致 max score≈0.75 < perfectThreshold 0.85；
            // 对齐 Chapter_01 scene 默认 Object_01/02 @ y=0 与 grid snap 落点。
            RegisterTarget(new ObjectMatchTarget(1, 1, new Vector3(-1f, 0f, 0f), identity));
            RegisterTarget(new ObjectMatchTarget(2, 1, new Vector3(1f, 0f, 0f), identity));
        }

        private void RegisterTarget(ObjectMatchTarget target)
        {
            _targetsByObjectId[target.ObjectId] = target;
        }

        private void OnObjectTransformChangedHandler(int objectId, Vector3 position, Quaternion rotation)
        {
            if (!_targetsByObjectId.ContainsKey(objectId)) return;

            _latestPositionByObjectId[objectId] = position;
            _latestRotationByObjectId[objectId] = rotation;

            int puzzleId = _targetsByObjectId[objectId].PuzzleId;
            float score = CalculatePuzzleScore(puzzleId);
            TryPublishScore(puzzleId, score);
        }

        /// <summary>
        /// MVP 评分：puzzle 内<strong>全部</strong> fixture target 参与分母；未上报 transform 的物体计 0 分。
        /// <para>防 playtest 回归：仅移动单物体且落点正确时 score=1.0 → 过早 PerfectMatch（S6-01 实证）。</para>
        /// </summary>
        internal float CalculatePuzzleScore(int puzzleId)
        {
            float sum = 0f;
            int targetCount = 0;

            foreach (var kv in _targetsByObjectId)
            {
                if (kv.Value.PuzzleId != puzzleId) continue;

                targetCount++;
                int objectId = kv.Key;
                if (_latestPositionByObjectId.TryGetValue(objectId, out var pos) &&
                    _latestRotationByObjectId.TryGetValue(objectId, out var rot))
                {
                    sum += CalculateObjectScore(kv.Value, pos, rot);
                }
            }

            if (targetCount == 0) return 0f;
            return Mathf.Clamp01(sum / targetCount);
        }

        private static float CalculateObjectScore(ObjectMatchTarget target, Vector3 position, Quaternion rotation)
        {
            float posDist = Vector3.Distance(position, target.TargetPosition);
            float posScore = 1f - Mathf.Clamp01(posDist / DefaultMaxPositionDistance);

            float angle = Quaternion.Angle(rotation, target.TargetRotation);
            float rotScore = 1f - Mathf.Clamp01(angle / DefaultMaxAngleDegrees);

            return Mathf.Clamp01(posScore * rotScore);
        }

        private void TryPublishScore(int puzzleId, float score)
        {
            score = Mathf.Clamp01(score);
            _lastPublishedScoreByPuzzleId.TryGetValue(puzzleId, out float last);

            if (Mathf.Abs(score - last) < ScorePublishEpsilon) return;

            _lastPublishedScoreByPuzzleId[puzzleId] = score;

            int targetCount = 0;
            int reportedCount = 0;
            foreach (var kv in _targetsByObjectId)
            {
                if (kv.Value.PuzzleId != puzzleId) continue;
                targetCount++;
                if (_latestPositionByObjectId.ContainsKey(kv.Key))
                    reportedCount++;
            }

            Log.Info($"[ShadowMatchCalculator] OnMatchScoreUpdated puzzleId={puzzleId} score={score:F3} " +
                     $"(prev={last:F3}) reported={reportedCount}/{targetCount} objects");
            GameEvent.Get<IShadowMatchEvent>().OnMatchScoreUpdated(puzzleId, score);
        }
    }
}
