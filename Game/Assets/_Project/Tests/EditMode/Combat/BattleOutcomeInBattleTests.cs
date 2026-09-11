using System.Linq;
using NUnit.Framework;

namespace TAoB.Combat.Tests
{
    public class BattleOutcomeInBattleTests
    {
        [Test]
        public void TimeLimit_NotReachedOneTickBefore_DefeatOnTheLimitTick()
        {
            var ally = TestUnits.Ally(1, stats: TestUnits.MakeStats(atk: 1, hp: TestUnits.Unkillable));
            var sim = new BattleSimulation(
                TestUnits.Setup(new[] { ally }, TestUnits.OneWave(TestUnits.Enemy(101)), timeLimitTicks: 30),
                new FixedRandom());

            SimRunner.Run(sim, 29);
            Assert.AreEqual(BattleOutcome.InProgress, sim.Outcome);

            var last = SimRunner.Run(sim, 1);
            Assert.AreEqual(BattleOutcome.DefeatTimeLimit, sim.Outcome);
            Assert.AreEqual((int)BattleOutcome.DefeatTimeLimit, SimRunner.OfKind(last, BattleEventKind.BattleEnded).Single().Detail);
        }

        [Test]
        public void AfterBattleEnds_StepDoesNothing_AndCommandsAreRejected()
        {
            var sim = new BattleSimulation(
                TestUnits.Setup(new[] { TestUnits.Ally(1) }, TestUnits.OneWave(TestUnits.Enemy(101)), timeLimitTicks: 1),
                new FixedRandom());
            SimRunner.Run(sim, 1);

            var after = SimRunner.Run(sim, 5);

            Assert.IsEmpty(after);
            Assert.AreEqual(1, sim.Tick);
            Assert.AreEqual(CommandResult.RejectedBattleOver, sim.Enqueue(BattleCommand.Retreat()));
        }

        [Test]
        public void Retreat_EndsBattleOnNextTick()
        {
            var sim = new BattleSimulation(TestUnits.Setup(new[] { TestUnits.Ally(1) }, TestUnits.OneWave(TestUnits.Enemy(101))), new FixedRandom());

            sim.Enqueue(BattleCommand.Retreat());
            SimRunner.Run(sim, 1);

            Assert.AreEqual(BattleOutcome.DefeatRetreat, sim.Outcome);
            Assert.AreEqual(1, sim.Tick);
        }

        [TestCase(PreconditionKind.NoAllyDowned, BattleOutcome.DefeatPrecondition)]
        [TestCase(PreconditionKind.None, BattleOutcome.InProgress)]
        public void AllyDowned_BreaksNoAllyDownedPrecondition(PreconditionKind precondition, BattleOutcome expected)
        {
            var fragile = TestUnits.Ally(1, Row.Front, stats: TestUnits.MakeStats(hp: 10));
            var backup = TestUnits.Ally(2, Row.Back, stats: TestUnits.MakeStats(spd: TestUnits.NeverActs));
            var brute = TestUnits.Enemy(101, stats: TestUnits.MakeStats(atk: 100, hp: TestUnits.Unkillable, spd: 1000));
            var sim = new BattleSimulation(
                TestUnits.Setup(new[] { fragile, backup }, TestUnits.OneWave(brute), precondition: precondition),
                new FixedRandom());

            SimRunner.Run(sim, 1);

            Assert.AreEqual(expected, sim.Outcome);
        }

        [Test]
        public void ClearingWaveStartsNextWave_ClearingFinalWaveWithBossIsVictory()
        {
            var waves = new[]
            {
                new[] { TestUnits.Enemy(101, stats: TestUnits.MakeStats(hp: 50, spd: TestUnits.NeverActs)) },
                new[] { TestUnits.Enemy(201, stats: TestUnits.MakeStats(hp: 50, spd: TestUnits.NeverActs), isBoss: true) },
            };
            var sim = new BattleSimulation(TestUnits.Setup(new[] { TestUnits.Ally(1) }, waves), new FixedRandom());

            var first = SimRunner.Run(sim, 10);
            Assert.AreEqual(BattleOutcome.InProgress, sim.Outcome);
            Assert.AreEqual(1, SimRunner.OfKind(first, BattleEventKind.WaveStarted).Single().Value);
            Assert.AreEqual(0, SimRunner.Unit(sim, 101).Hp, "HP 정확히 0 → 빈사");

            var second = SimRunner.Run(sim, 10);
            Assert.AreEqual(201, SimRunner.OfKind(second, BattleEventKind.Downed).Single().TargetId);
            Assert.AreEqual(BattleOutcome.Victory, sim.Outcome);
        }

        [Test]
        public void AllAlliesDowned_IsWipe()
        {
            var fragile = TestUnits.Ally(1, stats: TestUnits.MakeStats(hp: 10));
            var brute = TestUnits.Enemy(101, stats: TestUnits.MakeStats(atk: 100, hp: TestUnits.Unkillable, spd: 1000));
            var sim = new BattleSimulation(TestUnits.Setup(new[] { fragile }, TestUnits.OneWave(brute)), new FixedRandom());

            SimRunner.Run(sim, 1);

            Assert.AreEqual(BattleOutcome.DefeatWipe, sim.Outcome);
        }

        [TestCase(PreconditionKind.None, BattleOutcome.Victory)]
        [TestCase(PreconditionKind.NoAllyDowned, BattleOutcome.DefeatPrecondition)]
        public void SameTickVictoryAndLastAllyDowned_ResolvedByPriority(PreconditionKind precondition, BattleOutcome expected)
        {
            // 공격으로 마지막 적을 쓰러뜨리는 동시에 마도구 반발(RES 부족 10 × 1)로 HP 5 인 아군도 쓰러진다.
            var recoil = new Magitool(atkBonus: 0, requiredRes: 10, requiredMnd: 0, maxDurability: 10, durabilityCostPerUse: 1);
            var ally = TestUnits.Ally(1, stats: TestUnits.MakeStats(hp: 5, res: 0), magitool: recoil);
            var enemy = TestUnits.Enemy(101, stats: TestUnits.MakeStats(hp: 1, spd: TestUnits.NeverActs));
            var balance = TestUnits.Balance(resSelfDamagePerShortfall: 1, equipBlockResShortfall: 1000);
            var sim = new BattleSimulation(
                TestUnits.Setup(new[] { ally }, TestUnits.OneWave(enemy), balance, precondition: precondition),
                new FixedRandom());

            SimRunner.Run(sim, 10);

            Assert.AreEqual(UnitCondition.Downed, SimRunner.Unit(sim, 1).Condition);
            Assert.AreEqual(expected, sim.Outcome);
        }
    }
}
