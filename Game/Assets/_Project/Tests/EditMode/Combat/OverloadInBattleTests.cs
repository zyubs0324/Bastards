using System.Linq;
using NUnit.Framework;

namespace TAoB.Combat.Tests
{
    /// <summary>
    /// 과부하 네 갈래를 고정 난수로 강제한다.
    /// 마법사: ATK 50, MND 100, 원석 100 → 과부하 출력 150, 초과 500‰ → 첫 구간(성공 0~3999 · 흩어짐 ~6999 · 폭주 ~8999 · 한계돌파 ~9999).
    /// </summary>
    public class OverloadInBattleTests
    {
        private static BattleSimulation CreateMageBattle(FixedRandom random)
        {
            var mage = TestUnits.Ally(1, Row.Back, Job.Mage,
                stats: TestUnits.MakeStats(atk: 50, mnd: 100),
                skill: TestUnits.Skill(powerPermille: 1500, canOverload: true, cooldownTicks: 30),
                catalyst: new Catalyst(100));
            var knight = TestUnits.Ally(2, Row.Front, Job.Knight, stats: TestUnits.MakeStats(spd: TestUnits.NeverActs));
            var sim = new BattleSimulation(TestUnits.Setup(new[] { mage, knight }, TestUnits.OneWave(TestUnits.Enemy(101))), random);
            sim.Enqueue(BattleCommand.Overload(1));
            return sim;
        }

        [Test]
        public void Success_DealsOverloadDamage_AndStartsCooldown()
        {
            var random = new FixedRandom(0);
            var sim = CreateMageBattle(random);

            var events = SimRunner.Run(sim, 10);

            BattleEvent resolved = SimRunner.OfKind(events, BattleEventKind.OverloadResolved).Single();
            Assert.AreEqual((int)OverloadOutcome.Success, resolved.Detail);
            Assert.AreEqual(500, resolved.Value);
            // (ATK 50 + 과부하 출력 150) × 기술 1.5
            Assert.AreEqual(300, SimRunner.OfKind(events, BattleEventKind.Damage).Single(d => d.TargetId == 101).Value);
            Assert.AreEqual(0, random.Remaining);
            Assert.AreEqual(CommandResult.RejectedCooldown, sim.Enqueue(BattleCommand.Overload(1)));
        }

        [Test]
        public void Scatter_IncapacitatesCaster_WithoutDealingDamage()
        {
            var sim = CreateMageBattle(new FixedRandom(4000));

            var events = SimRunner.Run(sim, 10);

            Assert.AreEqual(1, SimRunner.OfKind(events, BattleEventKind.Scattered).Single().TargetId);
            Assert.IsEmpty(SimRunner.OfKind(events, BattleEventKind.Damage));
            UnitSnapshot mage = SimRunner.Unit(sim, 1);
            Assert.AreEqual(UnitCondition.Scattered, mage.Condition);
            Assert.AreEqual(0, mage.ActionGauge);
            Assert.AreEqual(CommandResult.RejectedIncapacitated, sim.Enqueue(BattleCommand.UseSkill(1)));
        }

        [Test]
        public void Runaway_HitsRandomUnitIncludingAllies_AndDamagesCaster()
        {
            // 두 번째 값 0: 후보(시전자 제외 [2, 101]) 중 첫째 = 아군 기사
            var sim = CreateMageBattle(new FixedRandom(7000, 0));

            var events = SimRunner.Run(sim, 10);

            BattleEvent hit = SimRunner.OfKind(events, BattleEventKind.Damage).Single();
            Assert.AreEqual(2, hit.TargetId);
            Assert.AreEqual(300, hit.Value);
            BattleEvent self = SimRunner.OfKind(events, BattleEventKind.SelfDamage).Single();
            Assert.AreEqual(1, self.TargetId);
            Assert.AreEqual(200, self.Value);
            Assert.AreEqual(800, SimRunner.Unit(sim, 1).Hp);
            Assert.AreEqual(700, SimRunner.Unit(sim, 2).Hp);
        }

        [Test]
        public void Breakthrough_DealsDamage_AndRaisesMndPermanently()
        {
            var sim = CreateMageBattle(new FixedRandom(9000));

            var events = SimRunner.Run(sim, 10);

            Assert.AreEqual(300, SimRunner.OfKind(events, BattleEventKind.Damage).Single().Value);
            Assert.AreEqual(110, SimRunner.OfKind(events, BattleEventKind.Breakthrough).Single().Value);
            Assert.AreEqual(110, SimRunner.Unit(sim, 1).Mnd);
        }
    }
}
