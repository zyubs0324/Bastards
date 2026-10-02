using NUnit.Framework;

namespace TAoB.Combat.Tests
{
    /// <summary>틱당 이동 한 단계. 자리 계산과 한 걸음의 경계가 여기서 고정된다.</summary>
    public class MovementTests
    {
        private static readonly LaneConfig Config = TestBalance.StaticLane(
            marchSpeedFu: 10, frontSlotOffsetFu: 60, backSlotOffsetFu: 40, frontReachFu: 100, backReachFu: 300);

        [Test]
        public void AllyFrontStandsAheadOfAnchor_AndAllyBackBehindIt()
        {
            Assert.AreEqual(1060, Movement.SlotFu(TestField.Ally(1, Row.Front), 1000, Config));
            Assert.AreEqual(960, Movement.SlotFu(TestField.Ally(2, Row.Back), 1000, Config));
        }

        [Test]
        public void EnemySlotsMirrorTheAllySlots_BecauseTheyFaceTheOtherWay()
        {
            Assert.AreEqual(940, Movement.SlotFu(TestField.Enemy(101, Row.Front), 1000, Config));
            Assert.AreEqual(1040, Movement.SlotFu(TestField.Enemy(102, Row.Back), 1000, Config));
        }

        [Test]
        public void FrontRowsFaceEachOther_SoTheGapIsTheClosestPair()
        {
            // 아군 기준점 0 · 적 기준점 400 → 앞열끼리 60 + 60 을 좁혀 280 이 남는다.
            int allyFront = Movement.SlotFu(TestField.Ally(1, Row.Front), 0, Config);
            int enemyFront = Movement.SlotFu(TestField.Enemy(101, Row.Front), 400, Config);

            Assert.AreEqual(280, Lane.Distance(allyFront, enemyFront));
        }

        [Test]
        public void UnitWalksTowardItsSlot_OneStepPerCall()
        {
            UnitState ally = TestField.Ally(1, Row.Front, posX: 0);
            UnitState[] units = TestField.ById(ally);

            Movement.Advance(units, 100, 0, Config);

            Assert.AreEqual(10, ally.PosX);
        }

        [Test]
        public void UnitStopsOnItsSlot_WithoutOvershooting()
        {
            UnitState ally = TestField.Ally(1, Row.Front, posX: 155);
            UnitState[] units = TestField.ById(ally);

            Movement.Advance(units, 100, 0, Config);
            Assert.AreEqual(160, ally.PosX);

            Movement.Advance(units, 100, 0, Config);
            Assert.AreEqual(160, ally.PosX, "자리에 도착한 유닛은 더 움직이지 않아야 한다.");
        }

        [Test]
        public void UnitFallsBack_WhenItsSlotIsBehind()
        {
            UnitState ally = TestField.Ally(1, Row.Front, posX: 300);
            UnitState[] units = TestField.ById(ally);

            Movement.Advance(units, 100, 0, Config);

            Assert.AreEqual(290, ally.PosX);
        }

        [Test]
        public void DownedAndScatteredUnits_StayWhereTheyAre()
        {
            UnitState downed = TestField.Ally(1, Row.Front, posX: 0, condition: UnitCondition.Downed);
            UnitState scattered = TestField.Ally(2, Row.Front, posX: 0, condition: UnitCondition.Scattered);
            UnitState[] units = TestField.ById(downed, scattered);

            Movement.Advance(units, 1000, 0, Config);

            Assert.AreEqual(0, downed.PosX);
            Assert.AreEqual(0, scattered.PosX);
        }

        [Test]
        public void UnitsOfALaterEncounter_DoNotMoveBeforeTheyAppear()
        {
            UnitState waiting = TestField.Enemy(201, Row.Front, posX: 5000, wave: 1);
            UnitState[] units = TestField.ById(waiting);

            Movement.Advance(units, 0, 0, Config);

            Assert.AreEqual(5000, waiting.PosX);
        }

        [Test]
        public void EnemiesHoldTheirGround_WhileAlliesMarchTowardThem()
        {
            UnitState ally = TestField.Ally(1, Row.Front, posX: 60);
            UnitState enemy = TestField.Enemy(101, Row.Front, posX: 940);
            UnitState[] units = TestField.ById(ally, enemy);

            for (int tick = 0; tick < 3; tick++)
            {
                Movement.Advance(units, 30 * (tick + 1), 1000, Config);
            }

            Assert.AreEqual(90, ally.PosX);
            Assert.AreEqual(940, enemy.PosX, "적 기준점이 고정이면 적은 제자리에 있어야 한다.");
        }

        [Test]
        public void UnitKeepsFollowingTheAnchor_StepByStep()
        {
            UnitState ally = TestField.Ally(1, Row.Front, posX: 60);
            UnitState[] units = TestField.ById(ally);

            for (int tick = 1; tick <= 5; tick++)
            {
                Movement.Advance(units, 10 * tick, 0, Config);
            }

            // 기준점이 한 걸음과 같은 속도로 가면 유닛은 자기 자리에 붙어 간다.
            Assert.AreEqual(110, ally.PosX);
        }
    }
}
