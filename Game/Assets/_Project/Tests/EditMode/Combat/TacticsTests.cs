using NUnit.Framework;

namespace TAoB.Combat.Tests
{
    /// <summary>
    /// 조우 시작 때 자세·침투를 정하는 규칙(계획서 Phase C). 확률 경계(미만)와 "확정이면 난수를 쓰지 않는다",
    /// 유닛별 독립 판정, ID 순서가 이 규칙의 정본이다.
    /// </summary>
    public class TacticsTests
    {
        private static readonly LaneConfig NoFlank = TestBalance.StaticLane();
        private static readonly LaneConfig Flank = TestBalance.RealisticLane(flankChance: 3500);

        [TestCase(Preference.None, Stance.Hold)]
        [TestCase(Preference.Guardian, Stance.Forward)]
        [TestCase(Preference.Charger, Stance.Charge)]
        [TestCase(Preference.Rearguard, Stance.HoldBack)]
        [TestCase(Preference.Coward, Stance.StepBack)]
        public void FullFormationChance_FollowsThePreference_WithoutConsumingRandom(Preference preference, Stance expected)
        {
            UnitState ally = TestField.Ally(1, preference: preference, formationChance: CombatScale.ChanceRange);

            // 준비된 값이 없는 난수라 한 번이라도 꺼내면 예외가 난다.
            Tactics.DecideEncounter(TestField.ById(ally), 0, NoFlank, new FixedRandom());

            Assert.AreEqual(expected, ally.Stance);
        }

        [Test]
        public void ZeroFormationChance_AlwaysHolds_WithoutConsumingRandom()
        {
            UnitState ally = TestField.Ally(1, preference: Preference.Charger, formationChance: 0);

            Tactics.DecideEncounter(TestField.ById(ally), 0, NoFlank, new FixedRandom());

            Assert.AreEqual(Stance.Hold, ally.Stance);
        }

        [Test]
        public void RollBelowFormationChance_FollowsPreference()
        {
            UnitState ally = TestField.Ally(1, preference: Preference.Guardian, formationChance: 3000);

            Tactics.DecideEncounter(TestField.ById(ally), 0, NoFlank, new FixedRandom(2999));

            Assert.AreEqual(Stance.Forward, ally.Stance);
        }

        [Test]
        public void RollAtFormationChance_FallsBackToHolding()
        {
            // 경계는 "미만"이다. 3000 이 성공이면 만분율 3000 이 3001 처럼 동작한다.
            UnitState ally = TestField.Ally(1, preference: Preference.Guardian, formationChance: 3000);

            Tactics.DecideEncounter(TestField.ById(ally), 0, NoFlank, new FixedRandom(3000));

            Assert.AreEqual(Stance.Hold, ally.Stance);
        }

        [Test]
        public void EachUnitRollsIndependently_InIdOrder()
        {
            UnitState first = TestField.Ally(1, preference: Preference.Charger, formationChance: 5000);
            UnitState second = TestField.Ally(2, preference: Preference.Charger, formationChance: 5000);

            // 1 번이 4999(성공), 2 번이 5000(실패). ById 가 ID 순으로 정렬해 넘기므로 굴리는 순서는 ID 순이다.
            Tactics.DecideEncounter(TestField.ById(second, first), 0, NoFlank, new FixedRandom(4999, 5000));

            Assert.AreEqual(Stance.Charge, first.Stance);
            Assert.AreEqual(Stance.Hold, second.Stance);
        }

        [Test]
        public void EnemyFrontRow_RollsFlankPerUnit_AndFlankersChargeAheadOfTheirPreference()
        {
            UnitState flanker = TestField.Enemy(101, Row.Front, preference: Preference.Coward, formationChance: 0);
            UnitState steady = TestField.Enemy(102, Row.Front);

            // 침투 롤만 쓴다(진형유지 값이 0 이라 규율 롤은 없다): 101 은 3499 로 침투, 102 는 3500 으로 아니다.
            Tactics.DecideEncounter(TestField.ById(flanker, steady), 0, Flank, new FixedRandom(3499, 3500));

            Assert.IsTrue(flanker.IsFlanking);
            Assert.AreEqual(Stance.Charge, flanker.Stance, "침투는 성향과 무관하게 돌진 자세다.");
            Assert.IsFalse(steady.IsFlanking);
            Assert.AreEqual(Stance.Hold, steady.Stance);
        }

        [Test]
        public void FlankRoll_AlwaysFollowsTheDisciplineRoll_ForTheSameUnit()
        {
            // 한 유닛의 난수 소비 순서: 진형유지 롤 → 침투 롤. 순서가 바뀌면 같은 시드가 다른 전투를 만든다.
            UnitState enemy = TestField.Enemy(101, Row.Front, preference: Preference.Coward, formationChance: 5000);

            Tactics.DecideEncounter(TestField.ById(enemy), 0, Flank, new FixedRandom(4999, 0));

            Assert.IsTrue(enemy.IsFlanking, "두 번째 값 0 이 침투 롤에 쓰였다.");
        }

        [Test]
        public void NoPreference_HoldsWithoutRolling_EvenWithAFormationChance()
        {
            UnitState ally = TestField.Ally(1, preference: Preference.None, formationChance: 5000);

            // 성향이 없으면 굴려도 결과가 같으니 난수를 쓰지 않는다. 준비된 값이 없는 난수가 예외를 내지 않아야 한다.
            Tactics.DecideEncounter(TestField.ById(ally), 0, NoFlank, new FixedRandom());

            Assert.AreEqual(Stance.Hold, ally.Stance);
        }

        [Test]
        public void RollsAreConsumedUnitByUnit_NotAllDisciplineThenAllFlank()
        {
            UnitState first = TestField.Enemy(101, Row.Front, preference: Preference.Coward, formationChance: 5000);
            UnitState second = TestField.Enemy(102, Row.Front, preference: Preference.Coward, formationChance: 5000);

            // 유닛 단위: 101 은 (규율 4999 성공, 침투 0 성공), 102 는 (규율 5000 실패, 침투 3500 실패).
            // 규율을 전부 먼저 굴리는 구현이면 101<-4999, 102<-0 이 규율에 쓰여 결과가 달라진다.
            Tactics.DecideEncounter(TestField.ById(first, second), 0, Flank, new FixedRandom(4999, 0, 5000, 3500));

            Assert.IsTrue(first.IsFlanking);
            Assert.AreEqual(Stance.Charge, first.Stance);
            Assert.IsFalse(second.IsFlanking);
            Assert.AreEqual(Stance.Hold, second.Stance);
        }

        [Test]
        public void OnlyEnemyFrontRowFlanks()
        {
            UnitState enemyBack = TestField.Enemy(101, Row.Back);
            UnitState allyFront = TestField.Ally(1, Row.Front);

            // 침투 확률이 아무리 높아도 적 뒷열·아군은 침투하지 않고, 롤도 쓰지 않는다.
            LaneConfig always = TestBalance.RealisticLane(flankChance: 5000);
            Tactics.DecideEncounter(TestField.ById(allyFront, enemyBack), 0, always, new FixedRandom());

            Assert.IsFalse(enemyBack.IsFlanking);
            Assert.IsFalse(allyFront.IsFlanking);
        }

        [Test]
        public void ZeroFlankChance_NeverFlanks_WithoutConsumingRandom()
        {
            UnitState enemy = TestField.Enemy(101, Row.Front);

            Tactics.DecideEncounter(TestField.ById(enemy), 0, NoFlank, new FixedRandom());

            Assert.IsFalse(enemy.IsFlanking);
        }

        [Test]
        public void NewEncounter_RedecidesAllies_AndOnlyThatEncountersEnemies()
        {
            UnitState ally = TestField.Ally(1, preference: Preference.Charger, formationChance: 5000, stance: Stance.Hold);
            UnitState wave0 = TestField.Enemy(101, wave: 0, preference: Preference.Guardian, formationChance: CombatScale.ChanceRange);
            UnitState wave1 = TestField.Enemy(201, wave: 1, preference: Preference.Guardian, formationChance: CombatScale.ChanceRange);
            wave1.Stance = Stance.Charge;

            // 조우 1: 아군(1)이 다시 뽑히고(4999 → 성공), 이 조우의 적(201)이 처음 뽑힌다. 조우 0 의 적(101)은 건드리지 않는다.
            Tactics.DecideEncounter(TestField.ById(ally, wave0, wave1), 1, NoFlank, new FixedRandom(4999));

            Assert.AreEqual(Stance.Charge, ally.Stance, "아군은 조우마다 다시 뽑는다.");
            Assert.AreEqual(Stance.Forward, wave1.Stance);
            Assert.AreEqual(Stance.Hold, wave0.Stance, "지난 조우의 적은 그대로다.");
        }

        [Test]
        public void DownedUnits_AreSkipped()
        {
            UnitState downed = TestField.Ally(1, condition: UnitCondition.Downed, preference: Preference.Charger, formationChance: CombatScale.ChanceRange);

            Tactics.DecideEncounter(TestField.ById(downed), 0, NoFlank, new FixedRandom());

            Assert.AreEqual(Stance.Hold, downed.Stance);
        }
    }
}
