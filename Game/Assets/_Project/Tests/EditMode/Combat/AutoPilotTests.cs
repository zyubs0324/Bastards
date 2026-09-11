using System.Linq;
using NUnit.Framework;

namespace TAoB.Combat.Tests
{
    public class AutoPilotTests
    {
        [Test]
        public void Ally_OnlyBasicAttacksAutomatically_EvenWhenSkillIsReady()
        {
            var ally = TestUnits.Ally(1, skill: TestUnits.Skill(cooldownTicks: 0));
            var sim = new BattleSimulation(TestUnits.Setup(new[] { ally }, TestUnits.OneWave(TestUnits.Enemy(101))), new FixedRandom());

            var events = SimRunner.Run(sim, 50);

            var actions = SimRunner.ActionsBy(events, 1);
            Assert.AreEqual(5, actions.Count);
            Assert.IsTrue(actions.All(a => a.Detail == (int)CommandKind.BasicAttack));
        }

        [Test]
        public void Enemy_UsesSkillWhenCooldownReady_OtherwiseBasicAttack()
        {
            var ally = TestUnits.Ally(1, stats: TestUnits.MakeStats(hp: TestUnits.Unkillable, spd: TestUnits.NeverActs));
            var enemy = TestUnits.Enemy(101,
                stats: TestUnits.MakeStats(atk: 10, hp: TestUnits.Unkillable, spd: 100),
                skill: TestUnits.Skill(cooldownTicks: 25));
            var sim = new BattleSimulation(TestUnits.Setup(new[] { ally }, TestUnits.OneWave(enemy)), new FixedRandom());

            var events = SimRunner.Run(sim, 40);

            // 10틱 기술(재사용 대기 25) → 20·30틱 대기 중이라 기본 공격 → 35틱에 대기 종료 → 40틱 기술
            CollectionAssert.AreEqual(
                new[] { (int)CommandKind.UseSkill, (int)CommandKind.BasicAttack, (int)CommandKind.BasicAttack, (int)CommandKind.UseSkill },
                SimRunner.ActionsBy(events, 101).Select(a => a.Detail).ToArray());
        }

        [Test]
        public void Enemy_NeverOverloadsOrRescues()
        {
            var ally = TestUnits.Ally(1, stats: TestUnits.MakeStats(hp: TestUnits.Unkillable, spd: TestUnits.NeverActs));
            var caster = TestUnits.Enemy(101,
                stats: TestUnits.MakeStats(atk: 10, hp: TestUnits.Unkillable, spd: 100),
                skill: TestUnits.Skill(canOverload: true, cooldownTicks: 0),
                catalyst: new Catalyst(100));
            var sim = new BattleSimulation(TestUnits.Setup(new[] { ally }, TestUnits.OneWave(caster)), new FixedRandom());

            var events = SimRunner.Run(sim, 50);

            var details = SimRunner.ActionsBy(events, 101).Select(a => a.Detail).ToArray();
            Assert.AreEqual(5, details.Length);
            Assert.IsFalse(details.Contains((int)CommandKind.Overload));
            Assert.IsFalse(details.Contains((int)CommandKind.Rescue));
        }
    }
}
