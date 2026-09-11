using System.Linq;
using NUnit.Framework;

namespace TAoB.Combat.Tests
{
    public class TargetingTests
    {
        private static BattleSimulation Create(UnitSetup ally, params UnitSetup[] enemies)
        {
            return new BattleSimulation(TestUnits.Setup(new[] { ally }, TestUnits.OneWave(enemies)), new FixedRandom());
        }

        [Test]
        public void FrontFirst_TargetsLowestIdInFrontRow()
        {
            var sim = Create(TestUnits.Ally(1),
                TestUnits.Enemy(101, Row.Back),
                TestUnits.Enemy(102, Row.Front),
                TestUnits.Enemy(103, Row.Front));

            var events = SimRunner.Run(sim, 10);

            Assert.AreEqual(102, SimRunner.OfKind(events, BattleEventKind.Damage).Single().TargetId);
        }

        [Test]
        public void FrontFirst_UsesBackRowWhenFrontRowIsEmpty()
        {
            var sim = Create(TestUnits.Ally(1),
                TestUnits.Enemy(101, Row.Back),
                TestUnits.Enemy(102, Row.Back));

            var events = SimRunner.Run(sim, 10);

            Assert.AreEqual(101, SimRunner.OfKind(events, BattleEventKind.Damage).Single().TargetId);
        }

        [Test]
        public void LowestHp_TargetsLowestCurrentHp_TieBrokenByLowestId()
        {
            var sim = Create(TestUnits.Ally(1, skill: TestUnits.Skill(targetRule: TargetRule.LowestHp)),
                TestUnits.Enemy(101, stats: TestUnits.MakeStats(hp: 500, spd: TestUnits.NeverActs)),
                TestUnits.Enemy(102, stats: TestUnits.MakeStats(hp: 300, spd: TestUnits.NeverActs)),
                TestUnits.Enemy(103, stats: TestUnits.MakeStats(hp: 300, spd: TestUnits.NeverActs)));
            sim.Enqueue(BattleCommand.UseSkill(1));

            var events = SimRunner.Run(sim, 10);

            Assert.AreEqual(102, SimRunner.OfKind(events, BattleEventKind.Damage).Single().TargetId);
        }

        [Test]
        public void All_HitsEveryLivingOpponent()
        {
            var sim = Create(TestUnits.Ally(1, skill: TestUnits.Skill(targetRule: TargetRule.All)),
                TestUnits.Enemy(101), TestUnits.Enemy(102, Row.Back), TestUnits.Enemy(103));
            sim.Enqueue(BattleCommand.UseSkill(1));

            var damage = SimRunner.OfKind(SimRunner.Run(sim, 10), BattleEventKind.Damage);

            CollectionAssert.AreEqual(new[] { 101, 102, 103 }, damage.Select(d => d.TargetId).ToArray());
            Assert.IsTrue(damage.All(d => d.Value == 75));
        }

        [Test]
        public void DownedOpponents_AreNotTargeted()
        {
            var sim = Create(TestUnits.Ally(1),
                TestUnits.Enemy(101, stats: TestUnits.MakeStats(hp: 50, spd: TestUnits.NeverActs)),
                TestUnits.Enemy(102));

            var first = SimRunner.Run(sim, 10);
            var second = SimRunner.Run(sim, 10);

            Assert.AreEqual(101, SimRunner.OfKind(first, BattleEventKind.Downed).Single().TargetId);
            Assert.AreEqual(102, SimRunner.OfKind(second, BattleEventKind.Damage).Single().TargetId);
        }
    }
}
