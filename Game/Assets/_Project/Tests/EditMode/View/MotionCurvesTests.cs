using NUnit.Framework;
using UnityEngine;

namespace TAoB.View.Tests
{
    /// <summary>연출 곡선은 순수 함수라 엔진 없이 고정한다(수직 슬라이스 계획서 Phase 4.5 검증).</summary>
    public class MotionCurvesTests
    {
        [Test]
        public void Lunge_StartsAndEndsAtZero_PeaksInMiddle()
        {
            Assert.AreEqual(0f, MotionCurves.Lunge(0f, 1f));
            Assert.AreEqual(0f, MotionCurves.Lunge(1f, 1f), "지속 시간 이상");
            Assert.AreEqual(0f, MotionCurves.Lunge(2f, 1f), "지속 시간을 넘음");
            Assert.AreEqual(1f, MotionCurves.Lunge(0.5f, 1f), 0.001f, "절반 지점이 최대");
            Assert.Greater(MotionCurves.Lunge(0.5f, 1f), MotionCurves.Lunge(0.1f, 1f));
        }

        [Test]
        public void Pop_StartsAndEndsAtOne_PeaksAtGivenScale()
        {
            Assert.AreEqual(1f, MotionCurves.Pop(0f, 1f, 1.5f));
            Assert.AreEqual(1f, MotionCurves.Pop(1f, 1f, 1.5f), "지속 시간 이상");
            Assert.AreEqual(1f, MotionCurves.Pop(2f, 1f, 1.5f), "지속 시간을 넘음");
            Assert.AreEqual(1.5f, MotionCurves.Pop(0.5f, 1f, 1.5f), 0.001f, "절반 지점이 최대");
        }

        [Test]
        public void Shake_IsZeroAtAndAfterDuration()
        {
            Assert.AreEqual(0f, MotionCurves.Shake(1f, 1f, 30f, 10f));
            Assert.AreEqual(0f, MotionCurves.Shake(2f, 1f, 30f, 10f));
        }

        /// <summary>지속 시간이 0 이하면(잘못된 호출) 세 곡선 모두 원위치·기본값을 낸다 — 예외 대신 안전한 기본 동작.</summary>
        [Test]
        public void ZeroOrNegativeDuration_ReturnsBaseline()
        {
            Assert.AreEqual(0f, MotionCurves.Lunge(0.1f, 0f));
            Assert.AreEqual(0f, MotionCurves.Lunge(0.1f, -1f));

            Assert.AreEqual(1f, MotionCurves.Pop(0.1f, 0f, 1.5f));
            Assert.AreEqual(1f, MotionCurves.Pop(0.1f, -1f, 1.5f));

            Assert.AreEqual(0f, MotionCurves.Shake(0.1f, 0f, 30f, 10f));
            Assert.AreEqual(0f, MotionCurves.Shake(0.1f, -1f, 30f, 10f));
        }

        [Test]
        public void Shake_AmplitudeDecaysOverTime()
        {
            float earlyPeak = 0f;
            float latePeak = 0f;
            for (float t = 0f; t < 0.25f; t += 0.001f)
            {
                earlyPeak = Mathf.Max(earlyPeak, Mathf.Abs(MotionCurves.Shake(t, 1f, 30f, 10f)));
            }

            for (float t = 0.75f; t < 1f; t += 0.001f)
            {
                latePeak = Mathf.Max(latePeak, Mathf.Abs(MotionCurves.Shake(t, 1f, 30f, 10f)));
            }

            Assert.Greater(earlyPeak, latePeak, "앞 구간 진폭이 뒤 구간보다 커야 한다");
        }

        [Test]
        public void Pulse_RiskZeroOrBelow_IsAlwaysOne()
        {
            for (float t = 0f; t < 3f; t += 0.3f)
            {
                Assert.AreEqual(1f, MotionCurves.Pulse(t, 0, 5f, 2f, 0.08f));
                Assert.AreEqual(1f, MotionCurves.Pulse(t, -1, 5f, 2f, 0.08f));
            }
        }

        [Test]
        public void Pulse_HigherRisk_ReachesPeakSooner()
        {
            float lowPeakTime = TimeToNearPeak(1);
            float highPeakTime = TimeToNearPeak(5);

            Assert.Less(highPeakTime, lowPeakTime, "위험 단계가 높을수록 주기가 짧아 먼저 최댓값에 닿는다");
        }

        [Test]
        public void Pulse_RangeIsBoundedByAmplitude()
        {
            float min = 2f;
            float max = 0f;
            for (float t = 0f; t < 5f; t += 0.01f)
            {
                float v = MotionCurves.Pulse(t, 3, 5f, 2f, 0.08f);
                min = Mathf.Min(min, v);
                max = Mathf.Max(max, v);
            }

            Assert.GreaterOrEqual(min, 1f - 0.0001f);
            Assert.LessOrEqual(max, 1.08f + 0.0001f);
            Assert.Greater(max - min, 0.07f, "진폭(0.08)만큼 실제로 오갔는지 확인 — 0.1 은 진폭보다 큰 잘못된 기준이었다");
        }

        [Test]
        public void Breathe_IsPeriodic()
        {
            const float frequency = 2f;
            float period = 2f * Mathf.PI / frequency;

            Assert.AreEqual(
                MotionCurves.Breathe(0.3f, 0f, frequency, 4f),
                MotionCurves.Breathe(0.3f + period, 0f, frequency, 4f), 0.001f);
        }

        [Test]
        public void Breathe_DifferentPhase_DiffersAtSameInstant()
        {
            Assert.AreNotEqual(
                MotionCurves.Breathe(0f, 0f, 2f, 4f),
                MotionCurves.Breathe(0f, 1f, 2f, 4f));
        }

        [Test]
        public void Breathe_StaysWithinAmplitude()
        {
            for (float t = 0f; t < 5f; t += 0.05f)
            {
                Assert.LessOrEqual(Mathf.Abs(MotionCurves.Breathe(t, 0.5f, 3f, 4f)), 4f + 0.0001f);
            }
        }

        private static float TimeToNearPeak(int riskLevel)
        {
            for (float t = 0f; t < 5f; t += 0.001f)
            {
                if (MotionCurves.Pulse(t, riskLevel, 5f, 2f, 0.08f) > 1f + 0.08f * 0.99f) return t;
            }

            return float.MaxValue;
        }
    }
}
