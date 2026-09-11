using NUnit.Framework;

namespace TAoB.Combat.Tests
{
    public class ActionGaugeCapTests
    {
        [Test]
        public void ExtremeSpd_KeepsActingEveryTickUntilTimeLimit()
        {
            // SPD 1,000,000 · 게이지 최대 1: 상한이 없으면 약 2148틱에서 int 가 넘쳐 음수가 되고, 다시 차오를 때까지 한동안 행동하지 못한다.
            // 게이지 최대가 1이라 SPD 1 인 적도 매 틱 공격하므로, 아군 HP 를 제한 시간 동안 버티게 둔다.
            var balance = TestBalance.Create(critBaseChance: 0, critChancePerSpd: 0, actionGaugeMax: 1);
            var ally = TestUnits.Ally(1, stats: TestUnits.MakeStats(hp: TestUnits.Unkillable, spd: CombatScale.MaxValue));
            var sim = new BattleSimulation(
                TestUnits.Setup(new[] { ally }, TestUnits.OneWave(TestUnits.Enemy(101)), balance),
                new FixedRandom());

            var events = SimRunner.Run(sim, BattleSetup.MaxTimeLimitTicks);

            Assert.AreEqual(BattleSetup.MaxTimeLimitTicks, SimRunner.ActionsBy(events, 1).Count);
            Assert.AreEqual(BattleOutcome.DefeatTimeLimit, sim.Outcome);
            Assert.That(SimRunner.Unit(sim, 1).ActionGauge, Is.InRange(0, 1));
        }
    }
}
