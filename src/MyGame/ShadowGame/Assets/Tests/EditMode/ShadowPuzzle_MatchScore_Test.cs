// 该文件由Cursor 自动生成
using NUnit.Framework;

namespace ShadowGame.Tests.EditMode
{
    /// <summary>
    /// EditMode test for Shadow Puzzle match scoring.
    /// Validates the weighted multi-anchor scoring formula:
    /// matchScore = Σ(w_i × s_i) / Σ(w_i)
    /// where s_i = positionScore × directionScore × visibilityScore
    /// </summary>
    [TestFixture]
    public class ShadowPuzzleMatchScoreTests
    {
        private const float PerfectMatchThreshold = 0.85f;
        private const float NearMatchLower = 0.65f;
        private const float Epsilon = 0.001f;

        [Test]
        public void CalculateMatchScore_AllAnchorsAligned_ReturnsPerfectMatch()
        {
            float[] weights = { 1.0f, 1.0f, 1.0f };
            float[] scores = { 0.95f, 0.90f, 0.88f };

            float matchScore = CalculateWeightedAverage(weights, scores);

            Assert.That(matchScore, Is.GreaterThanOrEqualTo(PerfectMatchThreshold),
                "All anchors aligned should exceed PerfectMatch threshold (0.85)");
        }

        [Test]
        public void CalculateMatchScore_MixedAlignment_ReturnsNearMatch()
        {
            float[] weights = { 1.0f, 0.5f, 1.0f };
            float[] scores = { 0.80f, 0.50f, 0.70f };

            float matchScore = CalculateWeightedAverage(weights, scores);

            Assert.That(matchScore, Is.InRange(NearMatchLower, PerfectMatchThreshold),
                $"Mixed alignment should be in NearMatch range ({NearMatchLower}-{PerfectMatchThreshold})");
        }

        [Test]
        public void CalculateMatchScore_NoAlignment_ReturnsBelowNearMatch()
        {
            float[] weights = { 1.0f, 1.0f, 1.0f };
            float[] scores = { 0.20f, 0.15f, 0.30f };

            float matchScore = CalculateWeightedAverage(weights, scores);

            Assert.That(matchScore, Is.LessThan(NearMatchLower),
                $"No alignment should be below NearMatch threshold ({NearMatchLower})");
        }

        [Test]
        public void CalculateMatchScore_SingleAnchor_ReturnsRawScore()
        {
            float[] weights = { 1.0f };
            float[] scores = { 0.75f };

            float matchScore = CalculateWeightedAverage(weights, scores);

            Assert.That(matchScore, Is.EqualTo(0.75f).Within(Epsilon));
        }

        [Test]
        public void CalculateMatchScore_ZeroWeights_ReturnsZero()
        {
            float[] weights = { 0f, 0f, 0f };
            float[] scores = { 0.90f, 0.85f, 0.95f };

            float matchScore = CalculateWeightedAverage(weights, scores);

            Assert.That(matchScore, Is.EqualTo(0f).Within(Epsilon));
        }

        [Test]
        public void AnchorCombinedScore_AllFactorsHigh_ReturnsHighScore()
        {
            float position = 0.95f;
            float direction = 0.90f;
            float visibility = 1.0f;

            float combined = position * direction * visibility;

            Assert.That(combined, Is.GreaterThan(0.85f));
        }

        [Test]
        public void AnchorCombinedScore_VisibilityZero_ReturnsZero()
        {
            float position = 0.95f;
            float direction = 0.90f;
            float visibility = 0.0f;

            float combined = position * direction * visibility;

            Assert.That(combined, Is.EqualTo(0f).Within(Epsilon));
        }

        private static float CalculateWeightedAverage(float[] weights, float[] scores)
        {
            float sumWeighted = 0f;
            float sumWeights = 0f;

            for (int i = 0; i < weights.Length; i++)
            {
                sumWeighted += weights[i] * scores[i];
                sumWeights += weights[i];
            }

            if (sumWeights <= 0f) return 0f;
            return sumWeighted / sumWeights;
        }
    }
}
