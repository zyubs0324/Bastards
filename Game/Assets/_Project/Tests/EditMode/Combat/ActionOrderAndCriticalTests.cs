using System.Linq;
using NUnit.Framework;

namespace TAoB.Combat.Tests
{
    public class ActionOrderAndCriticalTests
    {
        [Test]
        public void ReadyOnSameTick_ActsBySpdDescendingThenIdAscending()
        {
            // 2틱에 셋 다 준비된다(SPD 600 → 1200, SPD 500 → 1000). 설정 순서와 무관하게 정렬돼야 한다.
            var allies = new[]
            {
                TestUnits.Ally(3, stats: TestUnits.MakeStats(spd: 500)),
                TestUnits.Ally(1, stats: TestUnits.MakeStats(spd: 500)),
                TestUnits.Ally(2, stats: TestUnits.MakeStats(spd: 600)),
            };
            var sim = new BattleSimulation(TestUnits.Setup(allies, TestUnits.OneWave(TestUnits.Enemy(101))), new FixedRandom());

            var events = SimRunner.Run(sim, 2);

            CollectionAssert.AreEqual(
                new[] { 2, 1, 3 },
                SimRunner.OfKind(events, BattleEventKind.ActionStarted).Select(e => e.SourceId).ToArray());
        }

        [Test]
        public void GaugeOverflow_IsCarriedOverAfterAction()
        {
            // SPD 150: 7틱에 1050 → 행동 후 50 이 남는다.
            var sim = new BattleSimulation(
                TestUnits.Setup(new[] { TestUnits.Ally(1, stats: TestUnits.MakeStats(spd: 150)) }, TestUnits.OneWave(TestUnits.Enemy(101))),
                new FixedRandom());

            var events = SimRunner.Run(sim, 7);

            Assert.AreEqual(1, SimRunner.ActionsBy(events, 1).Count);
            Assert.AreEqual(50, SimRunner.Unit(sim, 1).ActionGauge);
        }

        [Test]
        public void GuaranteedCritical_DoesNotConsumeRandom()
        {
            // 난수가 하나도 준비되지 않은 FixedRandom: 난수를 쓰면 예외가 난다.
            var sim = new BattleSimulation(
                TestUnits.Setup(new[] { TestUnits.Ally(1) }, TestUnits.OneWave(TestUnits.Enemy(101)),
                    balance: TestUnits.Balance(critBaseChance: CombatScale.ChanceRange)),
                new FixedRandom());

            BattleEvent damage = SimRunner.OfKind(SimRunner.Run(sim, 10), BattleEventKind.Damage).Single();

            Assert.AreEqual(1, damage.Detail);
            Assert.AreEqual(75, damage.Value);
        }

        [Test]
        public void ZeroCriticalChance_DoesNotConsumeRandom()
        {
            var sim = new BattleSimulation(
                TestUnits.Setup(new[] { TestUnits.Ally(1) }, TestUnits.OneWave(TestUnits.Enemy(101)),
                    balance: TestUnits.Balance(critBaseChance: 0)),
                new FixedRandom());

            BattleEvent damage = SimRunner.OfKind(SimRunner.Run(sim, 10), BattleEventKind.Damage).Single();

            Assert.AreEqual(0, damage.Detail);
            Assert.AreEqual(50, damage.Value);
        }
    }
}
