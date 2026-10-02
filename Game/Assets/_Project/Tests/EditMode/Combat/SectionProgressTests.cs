using NUnit.Framework;

namespace TAoB.Combat.Tests
{
    /// <summary>기준점 전진·정지 조건을 고정한다 — 심리스 여부가 이 판정 하나에 달려 있다(계획서 D2).</summary>
    public class SectionProgressTests
    {
        private static readonly LaneConfig Config = TestBalance.StaticLane(
            marchSpeedFu: 10, frontReachFu: 100, backReachFu: 300, engageMarginFu: 0);

        /// <summary>여유 20 을 둔 설정 — 교전 판정이 사거리 경계가 아니라 그 안쪽을 보는지 가른다.</summary>
        private static readonly LaneConfig WithMargin = TestBalance.StaticLane(
            marchSpeedFu: 10, frontReachFu: 100, backReachFu: 300, engageMarginFu: 20);

        [Test]
        public void MarchesWhileTheEnemyIsOutOfReach()
        {
            UnitState[] units = TestField.ById(TestField.Ally(1, Row.Front), TestField.Enemy(101, Row.Front, posX: 500));

            Assert.AreEqual(10, SectionProgress.AdvanceFu(units, Config));
        }

        [Test]
        public void StopsWhenTheFrontRowHasTheEnemyInReach()
        {
            UnitState[] units = TestField.ById(TestField.Ally(1, Row.Front), TestField.Enemy(101, Row.Front, posX: 100));

            Assert.AreEqual(0, SectionProgress.AdvanceFu(units, Config));
        }

        [Test]
        public void KeepsMarchingWhenOnlyTheBackRowCanReach()
        {
            // 뒷열 사거리가 더 길다. 뒷열 기준으로 멈추면 앞열이 사거리 밖에 남아 진형을 유지한 전투가 되지 않는다.
            UnitState[] units = TestField.ById(
                TestField.Ally(1, Row.Front),
                TestField.Ally(2, Row.Back),
                TestField.Enemy(101, Row.Front, posX: 250));

            Assert.IsTrue(Engagement.HasEngagedOpponent(units[1], units, Config), "뒷열은 이미 닿는 거리다.");
            Assert.AreEqual(10, SectionProgress.AdvanceFu(units, Config));
        }

        [Test]
        public void FallsBackToTheBackRow_WhenTheFrontRowIsGone()
        {
            // 폴백이 없으면 조건이 영원히 거짓이 되어 교전 중에도 행군한다.
            UnitState[] units = TestField.ById(
                TestField.Ally(1, Row.Front, condition: UnitCondition.Downed),
                TestField.Ally(2, Row.Back),
                TestField.Enemy(101, Row.Front, posX: 250));

            Assert.AreEqual(Row.Back, SectionProgress.ReferenceRow(units));
            Assert.AreEqual(0, SectionProgress.AdvanceFu(units, Config));
        }

        [Test]
        public void ReferenceRowIsTheFrontRow_WhileAnyFrontAllySurvives()
        {
            UnitState[] units = TestField.ById(
                TestField.Ally(1, Row.Front, condition: UnitCondition.Downed),
                TestField.Ally(2, Row.Front),
                TestField.Ally(3, Row.Back));

            Assert.AreEqual(Row.Front, SectionProgress.ReferenceRow(units));
        }

        [Test]
        public void StopsWhileAnAllyIsScattered_EvenWithNoEnemyInReach()
        {
            // 흩어진 동료를 두고 전진하면 진형이 영구히 벌어진다.
            UnitState[] units = TestField.ById(
                TestField.Ally(1, Row.Front),
                TestField.Ally(2, Row.Back, condition: UnitCondition.Scattered),
                TestField.Enemy(101, Row.Front, posX: 500));

            Assert.AreEqual(0, SectionProgress.AdvanceFu(units, Config));
        }

        [Test]
        public void StopsAtReachMinusMargin_NotAtTheReachBoundary()
        {
            // 경계에서 멈추면 한 걸음에 들락거리며 떨린다. 교전 판정은 여유만큼 안쪽을 봐야 한다.
            UnitState[] atMargin = TestField.ById(TestField.Ally(1, Row.Front), TestField.Enemy(101, Row.Front, posX: 80));
            UnitState[] justOutside = TestField.ById(TestField.Ally(2, Row.Front), TestField.Enemy(102, Row.Front, posX: 81));

            Assert.AreEqual(0, SectionProgress.AdvanceFu(atMargin, WithMargin));
            Assert.AreEqual(10, SectionProgress.AdvanceFu(justOutside, WithMargin));
        }

        [Test]
        public void AllBackRowAllies_UseTheBackRowAsStopLine()
        {
            // 앞열이 아예 편성되지 않은 경우. 폴백이 없으면 교전 중에도 행군한다.
            UnitState[] units = TestField.ById(
                TestField.Ally(1, Row.Back),
                TestField.Ally(2, Row.Back),
                TestField.Enemy(101, Row.Front, posX: 250));

            Assert.AreEqual(Row.Back, SectionProgress.ReferenceRow(units));
            Assert.AreEqual(0, SectionProgress.AdvanceFu(units, Config));
        }

        [Test]
        public void DownedEnemyDoesNotHoldTheFormation()
        {
            UnitState[] units = TestField.ById(
                TestField.Ally(1, Row.Front),
                TestField.Enemy(101, Row.Front, posX: 50, condition: UnitCondition.Downed),
                TestField.Enemy(102, Row.Front, posX: 500));

            Assert.AreEqual(10, SectionProgress.AdvanceFu(units, Config));
        }
    }
}
