using System.Linq;
using NUnit.Framework;

namespace TAoB.Combat.Tests
{
    /// <summary>
    /// 기술 자동 사용(숨은 빈도 스탯)과 사용자의 강제 발동(재사용 대기)은 별개 규칙이다(계획서 D7).
    /// 자동 사용이 대기를 소모하면 "대기가 차면 몸이 빛나고 사용자가 터뜨린다"는 개입의 가치가 사라진다.
    /// </summary>
    public class SkillFrequencyTests
    {
        private static BattleSimulation Create(UnitSetup ally, IRandom random)
        {
            return new BattleSimulation(TestUnits.Setup(new[] { ally }, TestUnits.OneWave(TestUnits.Enemy(101))), random);
        }

        [Test]
        public void FullFrequency_UsesSkillEveryAction_AndConsumesNoRandom()
        {
            // 준비된 값이 없는 난수다 — 확정 결과에 난수를 쓰면 예외가 난다.
            var sim = Create(TestUnits.Ally(1, skillChance: CombatScale.ChanceRange), new FixedRandom());

            var events = SimRunner.Run(sim, 30);

            var actions = SimRunner.ActionsBy(events, 1);
            Assert.AreEqual(3, actions.Count);
            Assert.IsTrue(actions.All(a => a.Detail == (int)CommandKind.UseSkill));
        }

        [Test]
        public void AutoSkill_DoesNotStartTheForcedActivationCooldown()
        {
            var ally = TestUnits.Ally(1, skill: TestUnits.Skill(cooldownTicks: 60), skillChance: CombatScale.ChanceRange);
            var sim = Create(ally, new FixedRandom());

            SimRunner.Run(sim, 30);

            Assert.AreEqual(0, SimRunner.Unit(sim, 1).CooldownRemaining);
            Assert.AreEqual(CommandResult.Accepted, sim.Enqueue(BattleCommand.UseSkill(1)), "강제 발동은 계속 쓸 수 있어야 한다.");
        }

        [Test]
        public void ForcedSkill_StartsTheCooldown_WhileAutoSkillKeepsFiring()
        {
            var ally = TestUnits.Ally(1, skill: TestUnits.Skill(cooldownTicks: 30), skillChance: CombatScale.ChanceRange);
            var sim = Create(ally, new FixedRandom());
            sim.Enqueue(BattleCommand.UseSkill(1));

            SimRunner.Run(sim, 10);
            Assert.AreEqual(30, SimRunner.Unit(sim, 1).CooldownRemaining);
            Assert.AreEqual(CommandResult.RejectedCooldown, sim.Enqueue(BattleCommand.UseSkill(1)));

            var next = SimRunner.Run(sim, 10);

            Assert.AreEqual((int)CommandKind.UseSkill, SimRunner.ActionsBy(next, 1).Single().Detail);
            Assert.AreEqual(20, SimRunner.Unit(sim, 1).CooldownRemaining, "자동 사용은 대기를 다시 걸지 않고 틱당 1 만 줄어든다.");
        }

        [Test]
        public void PendingForcedCommand_SkipsTheFrequencyRoll()
        {
            // 순서는 사거리 질의 → 대기 명령 조회 → (명령이 없을 때만) 빈도 롤이다(계획서 D6).
            // 준비된 2500 은 롤을 실패시키는 값이다 — 소비되면 기본 공격이 나가고 값도 사라진다.
            var random = new FixedRandom(2500);
            var sim = Create(TestUnits.Ally(1, skillChance: 2500), random);
            sim.Enqueue(BattleCommand.UseSkill(1));

            var events = SimRunner.Run(sim, 10);

            Assert.AreEqual((int)CommandKind.UseSkill, SimRunner.ActionsBy(events, 1).Single().Detail);
            Assert.AreEqual(1, random.Remaining, "대기 명령이 있으면 빈도 롤을 돌지 않는다.");
        }

        [Test]
        public void FrequencyRoll_SucceedsBelowTheChance_AndFailsAtIt()
        {
            // 크리티컬 확률이 0 이라 난수는 빈도 롤만 쓴다. 10틱·20틱에 각각 2499·2500 이 나간다.
            var sim = Create(TestUnits.Ally(1, skillChance: 2500), new FixedRandom(2499, 2500));

            var events = SimRunner.Run(sim, 20);

            CollectionAssert.AreEqual(
                new[] { (int)CommandKind.UseSkill, (int)CommandKind.BasicAttack },
                SimRunner.ActionsBy(events, 1).Select(a => a.Detail).ToArray());
        }
    }
}
