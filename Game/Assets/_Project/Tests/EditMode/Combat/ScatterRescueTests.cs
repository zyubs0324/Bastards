using System.Linq;
using NUnit.Framework;

namespace TAoB.Combat.Tests
{
    /// <summary>
    /// 10틱에 마법사(1)가 흩어지고, 같은 SPD 의 기사(2)가 그 틱에 기본 공격해 게이지가 비는 상황에서 시작한다.
    /// </summary>
    public class ScatterRescueTests
    {
        private static BattleSimulation CreateScatteredAtTick10(int scatterHpLossPerTick)
        {
            var mage = TestUnits.Ally(1, Row.Back, Job.Mage,
                skill: TestUnits.Skill(canOverload: true),
                catalyst: new Catalyst(100));
            var knight = TestUnits.Ally(2, Row.Front, Job.Knight);
            var setup = TestUnits.Setup(new[] { mage, knight }, TestUnits.OneWave(TestUnits.Enemy(101)),
                balance: TestUnits.Balance(scatterHpLossPerTick: scatterHpLossPerTick));
            var sim = new BattleSimulation(setup, new FixedRandom(4000));
            sim.Enqueue(BattleCommand.Overload(1));
            SimRunner.Run(sim, 10);
            Assert.AreEqual(UnitCondition.Scattered, SimRunner.Unit(sim, 1).Condition, "전제: 10틱에 흩어짐");
            return sim;
        }

        [Test]
        public void Rescue_ReturnsScatteredAllyToNormal()
        {
            var sim = CreateScatteredAtTick10(scatterHpLossPerTick: 5);

            Assert.AreEqual(CommandResult.Accepted, sim.Enqueue(BattleCommand.Rescue(2, 1)));
            var events = SimRunner.Run(sim, 10);

            BattleEvent rescued = SimRunner.OfKind(events, BattleEventKind.Rescued).Single();
            Assert.AreEqual(20, rescued.Tick);
            Assert.AreEqual(2, rescued.SourceId);
            Assert.AreEqual(1, rescued.TargetId);
            UnitSnapshot mage = SimRunner.Unit(sim, 1);
            Assert.AreEqual(UnitCondition.Normal, mage.Condition);
            // 11~20틱 동안 틱당 5 감소
            Assert.AreEqual(950, mage.Hp);
        }

        [Test]
        public void ScatteredAllyNotRescued_IsDownedWhenHpRunsOut()
        {
            var sim = CreateScatteredAtTick10(scatterHpLossPerTick: 100);

            var events = SimRunner.Run(sim, 10);

            BattleEvent downed = SimRunner.OfKind(events, BattleEventKind.Downed).Single();
            Assert.AreEqual(20, downed.Tick);
            Assert.AreEqual(1, downed.TargetId);
            UnitSnapshot mage = SimRunner.Unit(sim, 1);
            Assert.AreEqual(UnitCondition.Downed, mage.Condition);
            Assert.AreEqual(0, mage.Hp);
        }

        [Test]
        public void RescueWhoseTargetIsDownedBeforeAction_FallsBackToBasicAttack()
        {
            var sim = CreateScatteredAtTick10(scatterHpLossPerTick: 100);
            sim.Enqueue(BattleCommand.Rescue(2, 1));

            var events = SimRunner.Run(sim, 10);

            BattleEvent fallback = SimRunner.OfKind(events, BattleEventKind.CommandFallback).Single();
            Assert.AreEqual(2, fallback.SourceId);
            Assert.AreEqual((int)CommandKind.Rescue, fallback.Detail);
            Assert.AreEqual((int)CommandKind.BasicAttack, SimRunner.ActionsBy(events, 2).Single().Detail);
            Assert.IsEmpty(SimRunner.OfKind(events, BattleEventKind.Rescued));
        }

        [Test]
        public void ScatteredAtBattleEnd_IsDownedOnTheEndingTick()
        {
            var sim = CreateScatteredAtTick10(scatterHpLossPerTick: 5);
            sim.Enqueue(BattleCommand.Retreat());

            var events = SimRunner.Run(sim, 1);
            BattleResult result = sim.BuildResult();

            Assert.AreEqual(BattleOutcome.DefeatRetreat, result.Outcome);
            Assert.AreEqual(1, SimRunner.OfKind(events, BattleEventKind.Downed).Single().TargetId);
            Assert.AreEqual(UnitCondition.Downed, SimRunner.Unit(sim, 1).Condition);
            CollectionAssert.Contains(result.DownedAllyIds.ToArray(), 1);
        }

        [TestCase(PreconditionKind.None, BattleOutcome.Victory)]
        [TestCase(PreconditionKind.NoAllyDowned, BattleOutcome.DefeatPrecondition)]
        public void VictoryWithScatteredAlly_IsReEvaluatedAfterDowningThem(PreconditionKind precondition, BattleOutcome expected)
        {
            // 10틱: 마법사(1)가 흩어진 직후 기사(2)가 HP 50 인 마지막 적을 쓰러뜨린다.
            // 교전이 끝나 자동 구조가 그 틱에 시작되지만 구조에는 시간이 걸리므로 끝나지 못한 채 전투가 끝난다.
            var mage = TestUnits.Ally(1, Row.Back, Job.Mage, skill: TestUnits.Skill(canOverload: true), catalyst: new Catalyst(100));
            var knight = TestUnits.Ally(2, Row.Front, Job.Knight);
            var enemy = TestUnits.Enemy(101, stats: TestUnits.MakeStats(hp: 50, spd: TestUnits.NeverActs));
            var sim = new BattleSimulation(
                TestUnits.Setup(new[] { mage, knight }, TestUnits.OneWave(enemy), TestUnits.Balance(rescueTicks: 50), precondition: precondition),
                new FixedRandom(4000));
            sim.Enqueue(BattleCommand.Overload(1));

            SimRunner.Run(sim, 10);

            Assert.AreEqual(expected, sim.Outcome);
            Assert.AreEqual(UnitCondition.Downed, SimRunner.Unit(sim, 1).Condition);
            CollectionAssert.AreEqual(new[] { 1 }, sim.BuildResult().DownedAllyIds.ToArray());
        }

        [Test]
        public void AllAlliesScatteredWhenTimeRunsOut_IsWipe()
        {
            var mage = TestUnits.Ally(1, Row.Back, Job.Mage, skill: TestUnits.Skill(canOverload: true), catalyst: new Catalyst(100));
            var sim = new BattleSimulation(
                TestUnits.Setup(new[] { mage }, TestUnits.OneWave(TestUnits.Enemy(101)), timeLimitTicks: 15),
                new FixedRandom(4000));
            sim.Enqueue(BattleCommand.Overload(1));

            SimRunner.Run(sim, 15);

            Assert.AreEqual(BattleOutcome.DefeatWipe, sim.Outcome);
        }
    }
}
