using System.Linq;
using NUnit.Framework;

namespace TAoB.Combat.Tests
{
    public class MagitoolInBattleTests
    {
        [Test]
        public void Durability_DropsPerAction_AndBonusIsLostAtZero()
        {
            var tool = new Magitool(atkBonus: 50, requiredRes: 0, requiredMnd: 0, maxDurability: 2, durabilityCostPerUse: 1);
            var sim = new BattleSimulation(
                TestUnits.Setup(new[] { TestUnits.Ally(1, magitool: tool) }, TestUnits.OneWave(TestUnits.Enemy(101))),
                new FixedRandom());

            var first = SimRunner.Run(sim, 10);
            var second = SimRunner.Run(sim, 10);
            Assert.AreEqual(0, SimRunner.Unit(sim, 1).MagitoolDurability);
            var third = SimRunner.Run(sim, 10);

            Assert.AreEqual(100, SimRunner.OfKind(first, BattleEventKind.Damage).Single().Value);
            Assert.AreEqual(100, SimRunner.OfKind(second, BattleEventKind.Damage).Single().Value);
            Assert.AreEqual(1, SimRunner.OfKind(second, BattleEventKind.MagitoolDepleted).Count);
            Assert.AreEqual(50, SimRunner.OfKind(third, BattleEventKind.Damage).Single().Value);
        }

        [Test]
        public void ResShortfall_CausesSelfDamageOnEachUse()
        {
            // 요구 RES 110, 아군 RES 100 → 부족 10 × 2 = 20
            var tool = new Magitool(atkBonus: 0, requiredRes: 110, requiredMnd: 0, maxDurability: 10, durabilityCostPerUse: 1);
            var sim = new BattleSimulation(
                TestUnits.Setup(new[] { TestUnits.Ally(1, magitool: tool) }, TestUnits.OneWave(TestUnits.Enemy(101))),
                new FixedRandom());

            var events = SimRunner.Run(sim, 10);

            Assert.AreEqual(20, SimRunner.OfKind(events, BattleEventKind.SelfDamage).Single().Value);
            Assert.AreEqual(980, SimRunner.Unit(sim, 1).Hp);
        }

        [Test]
        public void Result_ReportsDurabilityUsedPerUnit()
        {
            var tool = new Magitool(atkBonus: 0, requiredRes: 0, requiredMnd: 0, maxDurability: 10, durabilityCostPerUse: 3);
            var sim = new BattleSimulation(
                TestUnits.Setup(new[] { TestUnits.Ally(1, magitool: tool), TestUnits.Ally(2) }, TestUnits.OneWave(TestUnits.Enemy(101))),
                new FixedRandom());

            SimRunner.Run(sim, 20);
            MagitoolWear wear = sim.BuildResult().MagitoolWear.Single();

            Assert.AreEqual(1, wear.UnitId);
            Assert.AreEqual(6, wear.DurabilityUsed);
        }
    }
}
