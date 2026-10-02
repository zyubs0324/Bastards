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
