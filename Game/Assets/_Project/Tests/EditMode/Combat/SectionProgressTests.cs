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

            Assert.AreEqual(10, SectionProgress.AdvanceFu(units, Config, 0, 500));
        }

        [Test]
        public void StopsWhenTheFrontRowHasTheEnemyInReach()
        {
            UnitState[] units = TestField.ById(TestField.Ally(1, Row.Front), TestField.Enemy(101, Row.Front, posX: 100));

            Assert.AreEqual(0, SectionProgress.AdvanceFu(units, Config, 0, 100));
        }

        [Test]
        public void KeepsMarchingWhenOnlyTheBackRowCanReach()
        {
            // 뒷열 사거리가 더 길다. 뒷열 기준으로 멈추면 앞열이 사거리 밖에 남아 진형을 유지한 전투가 되지 않는다.
            UnitState[] units = TestField.ById(
                TestField.Ally(1, Row.Front),
                TestField.Ally(2, Row.Back),
                TestField.Enemy(101, Row.Front, posX: 250));

            Assert.IsTrue(Engagement.IsEngagedAt(units[1], units[1].PosX, 250, Config), "뒷열은 이미 닿는 거리다.");
            Assert.AreEqual(10, SectionProgress.AdvanceFu(units, Config, 0, 250));
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
            Assert.AreEqual(0, SectionProgress.AdvanceFu(units, Config, 0, 250));
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

            Assert.AreEqual(0, SectionProgress.AdvanceFu(units, Config, 0, 500));
        }

        [Test]
        public void StopsAtReachMinusMargin_NotAtTheReachBoundary()
        {
            // 경계에서 멈추면 한 걸음에 들락거리며 떨린다. 교전 판정은 여유만큼 안쪽을 봐야 한다.
            UnitState[] atMargin = TestField.ById(TestField.Ally(1, Row.Front), TestField.Enemy(101, Row.Front, posX: 80));
            UnitState[] justOutside = TestField.ById(TestField.Ally(2, Row.Front), TestField.Enemy(102, Row.Front, posX: 81));

            Assert.AreEqual(0, SectionProgress.AdvanceFu(atMargin, WithMargin, 0, 80));
            Assert.AreEqual(10, SectionProgress.AdvanceFu(justOutside, WithMargin, 0, 81));
        }

        [Test]
        public void ChargerAheadOfTheLine_DoesNotStopTheFormationEarly()
        {
            // 돌진한 유닛의 실제 위치(60)는 사거리 안이지만 자리(0)는 아니다. 실제 위치로 판정하면 진형이 일찍 서서
            // 나머지 동료는 사거리 밖에서 구경만 한다.
            UnitState charger = TestField.Ally(1, Row.Front, posX: 60, stance: Stance.Charge);
            UnitState enemy = TestField.Enemy(101, Row.Front, posX: 160);
            UnitState[] units = TestField.ById(charger, enemy);

            Assert.AreEqual(10, SectionProgress.AdvanceFu(units, Config, 0, 160));
        }

        [Test]
        public void FlankingEnemyAheadOfItsLine_DoesNotStopTheFormationEarly()
        {
            // 침투하러 앞서 나온 적의 실제 위치(60)는 사거리 안이지만 그 적의 자리(160)는 아니다. 실제 위치로 판정하면
            // 아군이 일찍 멈춰, 침투한 적이 앞으로 나온 만큼 도로 멀어져 뒷열에 닿지 못한다.
            UnitState ally = TestField.Ally(1, Row.Front);
            UnitState flanker = TestField.Enemy(101, Row.Front, posX: 60, stance: Stance.Charge, isFlanking: true);
            UnitState[] units = TestField.ById(ally, flanker);

            Assert.AreEqual(10, SectionProgress.AdvanceFu(units, Config, 0, 160));
        }

        [Test]
        public void SlotInEngagementReach_StopsTheFormation_EvenIfTheUnitStoodBack()
        {
            // 반대로 물러선 유닛의 실제 위치(-60)는 사거리 밖이지만 자리(0)가 닿으면 진형은 멈춘다.
            UnitState coward = TestField.Ally(1, Row.Front, posX: -60, stance: Stance.StepBack);
            UnitState enemy = TestField.Enemy(101, Row.Front, posX: 100);
            UnitState[] units = TestField.ById(coward, enemy);

            Assert.AreEqual(0, SectionProgress.AdvanceFu(units, Config, 0, 100));
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
            Assert.AreEqual(0, SectionProgress.AdvanceFu(units, Config, 0, 250));
        }

        [Test]
        public void DownedEnemyDoesNotHoldTheFormation()
        {
            // 앞열 적은 자리(100)가 닿는 거리고 뒷열 적은 자리(500)가 멀다. 빈사 앞열 하나만 닿는 구성이다.
            LaneConfig staggered = TestBalance.StaticLane(marchSpeedFu: 10, frontReachFu: 100, backReachFu: 1000, backSlotOffsetFu: 400);
            UnitState[] downedFront = TestField.ById(
                TestField.Ally(1, Row.Front),
                TestField.Enemy(101, Row.Front, condition: UnitCondition.Downed),
                TestField.Enemy(102, Row.Back));
            UnitState[] aliveFront = TestField.ById(
                TestField.Ally(1, Row.Front),
                TestField.Enemy(101, Row.Front),
                TestField.Enemy(102, Row.Back));

            Assert.AreEqual(10, SectionProgress.AdvanceFu(downedFront, staggered, 0, 100), "빈사 적은 정지선을 만들지 않는다.");
            Assert.AreEqual(0, SectionProgress.AdvanceFu(aliveFront, staggered, 0, 100), "대조: 같은 자리에 산 적이 있으면 멈춘다.");
        }

        [TestCase(Stance.Charge)]
        [TestCase(Stance.Forward)]
        [TestCase(Stance.HoldBack)]
        [TestCase(Stance.StepBack)]
        public void StopLineIsTheSlotDistance_WhateverTheStanceOffsets(Stance stance)
        {
            // 실데이터 수치: 자리 간격 120, 사거리 440, 여유 120 이므로 양쪽 자리가 320 이내면 멈춘다(적 기준점 560 이하).
            // 자세 목표(자리 +- 드리프트)로 판정하면 자세마다 경계가 달라진다.
            LaneConfig lane = TestBalance.RealisticLane();
            UnitState[] stopped = TestField.ById(TestField.Ally(1, Row.Front, stance: stance), TestField.Enemy(101, Row.Front, stance: stance));
            UnitState[] marching = TestField.ById(TestField.Ally(2, Row.Front, stance: stance), TestField.Enemy(102, Row.Front, stance: stance));

            Assert.AreEqual(0, SectionProgress.AdvanceFu(stopped, lane, 0, 560));
            Assert.AreEqual(40, SectionProgress.AdvanceFu(marching, lane, 0, 561));
        }
    }
}
