using System.Linq;
using NUnit.Framework;

namespace TAoB.Combat.Tests
{
    public class AutoPilotTests
    {
        [Test]
        public void Ally_BasicAttacksOnly_WhenSkillFrequencyIsZero()
        {
            // 자동 사용은 빈도 스탯만 본다 — 재사용 대기가 0 이어도 빈도가 0 이면 기술이 나가지 않는다.
            var ally = TestUnits.Ally(1, skill: TestUnits.Skill(cooldownTicks: 0));
            var sim = new BattleSimulation(TestUnits.Setup(new[] { ally }, TestUnits.OneWave(TestUnits.Enemy(101))), new FixedRandom());

            var events = SimRunner.Run(sim, 50);

            var actions = SimRunner.ActionsBy(events, 1);
            Assert.AreEqual(5, actions.Count);
            Assert.IsTrue(actions.All(a => a.Detail == (int)CommandKind.BasicAttack));
        }

        [Test]
        public void Enemy_UsesSkillByFrequency_NotByCooldown()
        {
            // 적도 아군과 같은 규칙을 쓴다. 빈도가 가득이면 재사용 대기가 길어도 매번 기술이 나간다(계획서 D7).
            var ally = TestUnits.Ally(1, stats: TestUnits.MakeStats(hp: TestUnits.Unkillable, spd: TestUnits.NeverActs));
            var enemy = TestUnits.Enemy(101,
                stats: TestUnits.MakeStats(atk: 10, hp: TestUnits.Unkillable, spd: 100),
                skill: TestUnits.Skill(cooldownTicks: 25),
                skillChance: CombatScale.ChanceRange);
            var sim = new BattleSimulation(TestUnits.Setup(new[] { ally }, TestUnits.OneWave(enemy)), new FixedRandom());

            var events = SimRunner.Run(sim, 40);

            CollectionAssert.AreEqual(
                Enumerable.Repeat((int)CommandKind.UseSkill, 4).ToArray(),
                SimRunner.ActionsBy(events, 101).Select(a => a.Detail).ToArray());
            Assert.AreEqual(0, SimRunner.Unit(sim, 101).CooldownRemaining, "자동 사용은 대기를 걸지 않는다.");
        }

        [Test]
        public void Enemy_NeverOverloadsOrRescues()
        {
            var ally = TestUnits.Ally(1, stats: TestUnits.MakeStats(hp: TestUnits.Unkillable, spd: TestUnits.NeverActs));
            // 빈도를 가득 채워 기술 분기를 지난다 — 빈도 0 이면 분기에 들어가지도 않아 회귀를 잡지 못한다.
            var caster = TestUnits.Enemy(101,
                stats: TestUnits.MakeStats(atk: 10, hp: TestUnits.Unkillable, spd: 100),
                skill: TestUnits.Skill(canOverload: true, cooldownTicks: 0),
                catalyst: new Catalyst(100),
                skillChance: CombatScale.ChanceRange);
            var sim = new BattleSimulation(TestUnits.Setup(new[] { ally }, TestUnits.OneWave(caster)), new FixedRandom());

            var events = SimRunner.Run(sim, 50);

            var details = SimRunner.ActionsBy(events, 101).Select(a => a.Detail).ToArray();
            Assert.AreEqual(5, details.Length);
            Assert.IsFalse(details.Contains((int)CommandKind.Overload));
            Assert.IsFalse(details.Contains((int)CommandKind.Rescue));
        }
    }
}
