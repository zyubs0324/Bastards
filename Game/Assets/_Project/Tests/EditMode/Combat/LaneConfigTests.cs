using System;
using NUnit.Framework;

namespace TAoB.Combat.Tests
{
    /// <summary>이동 수치의 범위와 교착 방지 불변식(계획서 Phase A).</summary>
    public class LaneConfigTests
    {
        [Test]
        public void StaticPreset_KeepsEveryValue()
        {
            LaneConfig lane = TestBalance.StaticLane();

            Assert.AreEqual(1, lane.MarchSpeedFu);
            Assert.AreEqual(0, lane.FrontSlotOffsetFu);
            Assert.AreEqual(0, lane.BackSlotOffsetFu);
            Assert.AreEqual(LaneConfig.MaxReachFu, lane.FrontReachFu);
            Assert.AreEqual(LaneConfig.MaxReachFu, lane.BackReachFu);
            Assert.AreEqual(0, lane.EngageMarginFu);
            Assert.AreEqual(0, lane.FormationDriftFu);
            Assert.AreEqual(0, lane.ChargeDriftFu);
            Assert.AreEqual(0, lane.StepBackFu);
            Assert.AreEqual(0, lane.FlankChance);
        }

        [Test]
        public void BackReachTooShortForWhereTheFrontRowStops_IsRejected()
        {
            // 앞열은 100-10=90 에서 멈추고 뒷열은 거기서 100 만큼 더 뒤에 선다 → 190 을 덮어야 한다.
            var ex = Assert.Throws<ArgumentException>(() => TestBalance.StaticLane(
                frontReachFu: 100, engageMarginFu: 10, frontSlotOffsetFu: 60, backSlotOffsetFu: 40, backReachFu: 180));

            Assert.AreEqual("backReachFu", ex.ParamName);
            StringAssert.Contains("190", ex.Message);
        }

        [Test]
        public void BackReachExactlyCoveringTheStandoff_IsAccepted()
        {
            LaneConfig lane = TestBalance.StaticLane(
                frontReachFu: 100, engageMarginFu: 10, frontSlotOffsetFu: 60, backSlotOffsetFu: 40, backReachFu: 190);

            Assert.AreEqual(190, lane.BackReachFu);
        }

        [Test]
        public void FrontEngagementWindowNarrowerThanOneStep_IsRejected()
        {
            // 창이 한 걸음보다 좁으면 한 틱에 건너뛴다 — 정지선이 영영 걸리지 않아 적을 지나쳐 행군한다.
            var ex = Assert.Throws<ArgumentException>(() => TestBalance.StaticLane(
                marchSpeedFu: 100, frontReachFu: 150, engageMarginFu: 100));

            Assert.AreEqual("frontReachFu", ex.ParamName);
            StringAssert.Contains("200", ex.Message);
        }

        [Test]
        public void BackEngagementWindowNarrowerThanOneStep_IsRejected()
        {
            // 앞열이 전멸하면 정지선이 뒷열로 떨어진다(D2). 뒷열 창도 한 걸음 이상이어야 한다.
            var ex = Assert.Throws<ArgumentException>(() => TestBalance.StaticLane(
                marchSpeedFu: 100, engageMarginFu: 100, backReachFu: 150));

            Assert.AreEqual("backReachFu", ex.ParamName);
        }

        [Test]
        public void EngagementWindowExactlyOneStep_IsAccepted()
        {
            LaneConfig lane = TestBalance.StaticLane(
                marchSpeedFu: 100, frontReachFu: 200, backReachFu: 200, engageMarginFu: 100);

            Assert.AreEqual(100, lane.MarchSpeedFu);
            Assert.AreEqual(200, lane.BackReachFu);
        }

        [Test]
        public void MarchSpeedBelowOne_IsRejected()
        {
            var ex = Assert.Throws<ArgumentException>(() => TestBalance.StaticLane(marchSpeedFu: 0));

            Assert.AreEqual("marchSpeedFu", ex.ParamName);
        }

        [Test]
        public void ValueAboveItsRange_IsRejected()
        {
            var margin = Assert.Throws<ArgumentException>(() => TestBalance.StaticLane(engageMarginFu: LaneConfig.MaxMarginFu + 1));
            var offset = Assert.Throws<ArgumentException>(() => TestBalance.StaticLane(frontSlotOffsetFu: LaneConfig.MaxOffsetFu + 1));
            var reach = Assert.Throws<ArgumentException>(() => TestBalance.StaticLane(frontReachFu: LaneConfig.MaxReachFu + 1));

            Assert.AreEqual("engageMarginFu", margin.ParamName);
            Assert.AreEqual("frontSlotOffsetFu", offset.ParamName);
            Assert.AreEqual("frontReachFu", reach.ParamName);
        }

        [TestCase(31, 30, "formationDriftFu")]
        [TestCase(30, 31, "stepBackFu")]
        public void RetreatingStanceBeyondHalfTheEngageMargin_IsRejected(int formationDrift, int stepBack, string param)
        {
            // 양 진영이 마주 보고 각자 물러설 수 있다. 두 배가 여유(60)를 넘으면 앞열끼리 사거리 밖에서 대치한다.
            var ex = Assert.Throws<ArgumentException>(() => TestBalance.StaticLane(
                frontReachFu: 300, backReachFu: 1000, engageMarginFu: 60, chargeDriftFu: 100,
                formationDriftFu: formationDrift, stepBackFu: stepBack));

            Assert.AreEqual(param, ex.ParamName);
        }

        [Test]
        public void RetreatExactlyHalfTheEngageMargin_IsAccepted_ForBothStances()
        {
            LaneConfig lane = TestBalance.StaticLane(
                frontReachFu: 300, backReachFu: 1000, engageMarginFu: 60, chargeDriftFu: 100, formationDriftFu: 30, stepBackFu: 30);

            Assert.AreEqual(30, lane.FormationDriftFu);
            Assert.AreEqual(30, lane.StepBackFu);
        }

        [TestCase(30, 0)]
        [TestCase(0, 30)]
        public void BackReachMustAlsoCoverBothSidesRetreating(int formationDrift, int stepBack)
        {
            // 앞열이 멈춰 서는 거리 100(=160-60)에 양쪽이 물러선 30+30 을 더해 160 이어야 한다.
            var tooShort = Assert.Throws<ArgumentException>(() => TestBalance.StaticLane(
                frontReachFu: 160, backReachFu: 159, engageMarginFu: 60, chargeDriftFu: 30,
                formationDriftFu: formationDrift, stepBackFu: stepBack));
            LaneConfig exact = TestBalance.StaticLane(
                frontReachFu: 160, backReachFu: 160, engageMarginFu: 60, chargeDriftFu: 30,
                formationDriftFu: formationDrift, stepBackFu: stepBack);

            Assert.AreEqual("backReachFu", tooShort.ParamName);
            Assert.AreEqual(160, exact.BackReachFu);
        }

        [Test]
        public void ChargeShorterThanTheForwardStance_IsRejected()
        {
            var ex = Assert.Throws<ArgumentException>(() => TestBalance.StaticLane(
                frontReachFu: 300, backReachFu: 1000, engageMarginFu: 60, formationDriftFu: 30, chargeDriftFu: 29));
            LaneConfig equal = TestBalance.StaticLane(
                frontReachFu: 300, backReachFu: 1000, engageMarginFu: 60, formationDriftFu: 30, chargeDriftFu: 30);

            Assert.AreEqual("chargeDriftFu", ex.ParamName);
            Assert.AreEqual(30, equal.ChargeDriftFu, "같으면 허용된다.");
        }

        [Test]
        public void ChargePastTheStopLine_IsRejected()
        {
            // 돌진이 정지선(사거리 − 여유 = 240)을 넘으면 적을 지나쳐 서게 된다.
            var past = Assert.Throws<ArgumentException>(() => TestBalance.StaticLane(
                frontReachFu: 300, backReachFu: 1000, engageMarginFu: 60, chargeDriftFu: 240));
            LaneConfig justShort = TestBalance.StaticLane(frontReachFu: 300, backReachFu: 1000, engageMarginFu: 60, chargeDriftFu: 239);

            Assert.AreEqual("chargeDriftFu", past.ParamName);
            Assert.AreEqual(239, justShort.ChargeDriftFu);
        }

        [Test]
        public void Flanking_RequiresTheChargeToAlsoReachARetreatedBackRow()
        {
            // 뒷열이 20 후퇴해 있으면 앞뒤 자리 간격 120 + 20 - 여유 40 = 100 만큼은 나아가야 닿는다.
            // 모자라면 물러선 동료는 침투에서 영구히 안전하다.
            var tooShort = Assert.Throws<ArgumentException>(() => TestBalance.StaticLane(
                frontSlotOffsetFu: 60, backSlotOffsetFu: 60, frontReachFu: 300, backReachFu: 1000, engageMarginFu: 40,
                stepBackFu: 20, chargeDriftFu: 99, flankChance: 1));
            LaneConfig exact = TestBalance.StaticLane(
                frontSlotOffsetFu: 60, backSlotOffsetFu: 60, frontReachFu: 300, backReachFu: 1000, engageMarginFu: 40,
                stepBackFu: 20, chargeDriftFu: 100, flankChance: 1);

            Assert.AreEqual("chargeDriftFu", tooShort.ParamName);
            Assert.AreEqual(100, exact.ChargeDriftFu);
        }

        [Test]
        public void Flanking_RequiresTheChargeToReachTheBackRow()
        {
            // 앞뒤 자리 간격 120 − 여유 20 = 100 만큼은 나아가야 정지선의 적이 뒷열에 닿는다.
            var tooShort = Assert.Throws<ArgumentException>(() => TestBalance.StaticLane(
                frontSlotOffsetFu: 60, backSlotOffsetFu: 60, frontReachFu: 200, backReachFu: 1000, engageMarginFu: 20,
                chargeDriftFu: 99, flankChance: 1));
            LaneConfig exact = TestBalance.StaticLane(
                frontSlotOffsetFu: 60, backSlotOffsetFu: 60, frontReachFu: 200, backReachFu: 1000, engageMarginFu: 20,
                chargeDriftFu: 100, flankChance: 1);
            LaneConfig noFlank = TestBalance.StaticLane(
                frontSlotOffsetFu: 60, backSlotOffsetFu: 60, frontReachFu: 200, backReachFu: 1000, engageMarginFu: 20,
                chargeDriftFu: 99, flankChance: 0);

            Assert.AreEqual("chargeDriftFu", tooShort.ParamName);
            Assert.AreEqual(100, exact.ChargeDriftFu);
            Assert.AreEqual(99, noFlank.ChargeDriftFu, "침투를 끄면 이 조건은 필요 없다.");
        }

        [Test]
        public void BalanceConfig_RequiresLane()
        {
            // TestBalance.Create 는 빠진 값을 정지 프리셋으로 채우므로, 누락을 보려면 생성자를 직접 부른다.
            var ex = Assert.Throws<ArgumentNullException>(() => new BalanceConfig(
                100, 750, 1000, 1250, 1000, 800, 500, 20, 1500, 1, 2, 20, 25, 200, 1000, 5, 200, 1500, 1000, 200, 10,
                TestBalance.DefaultTiers(),
                null));

            Assert.AreEqual("lane", ex.ParamName);
        }
    }
}
