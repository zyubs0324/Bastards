using System.Linq;
using NUnit.Framework;

namespace TAoB.Combat.Tests
{
    public class CommandQueueTests
    {
        private static BattleSimulation Create(UnitSetup[] allies, UnitSetup[][] waves = null, IRandom random = null)
        {
            return new BattleSimulation(
                TestUnits.Setup(allies, waves ?? TestUnits.OneWave(TestUnits.Enemy(101))),
                random ?? new FixedRandom());
        }

        [Test]
        public void AllyWithoutCommand_BasicAttacksWhenGaugeFills()
        {
            var sim = Create(new[] { TestUnits.Ally(1) });

            var early = SimRunner.Run(sim, 9);
            var tick10 = SimRunner.Run(sim, 1);

            Assert.IsEmpty(SimRunner.OfKind(early, BattleEventKind.ActionStarted));
            BattleEvent action = SimRunner.ActionsBy(tick10, 1).Single();
            Assert.AreEqual(10, action.Tick);
            Assert.AreEqual((int)CommandKind.BasicAttack, action.Detail);
            BattleEvent damage = SimRunner.OfKind(tick10, BattleEventKind.Damage).Single();
            Assert.AreEqual(101, damage.TargetId);
            Assert.AreEqual(50, damage.Value);
        }

        [Test]
        public void PendingSkill_WaitsForFullGauge_ThenExecutes()
        {
            var sim = Create(new[] { TestUnits.Ally(1) });

            Assert.AreEqual(CommandResult.Accepted, sim.Enqueue(BattleCommand.UseSkill(1)));
            var early = SimRunner.Run(sim, 9);
            var tick10 = SimRunner.Run(sim, 1);

            Assert.IsEmpty(SimRunner.OfKind(early, BattleEventKind.ActionStarted));
            Assert.AreEqual((int)CommandKind.UseSkill, SimRunner.ActionsBy(tick10, 1).Single().Detail);
            Assert.AreEqual(75, SimRunner.OfKind(tick10, BattleEventKind.Damage).Single().Value);
        }

        [Test]
        public void NewCommand_ReplacesPendingCommand()
        {
            var mage = TestUnits.Ally(1, Row.Back, Job.Mage, skill: TestUnits.Skill(canOverload: true), catalyst: new Catalyst(100));
            var sim = Create(new[] { mage }, random: new FixedRandom(0));

            Assert.AreEqual(CommandResult.Accepted, sim.Enqueue(BattleCommand.UseSkill(1)));
            Assert.AreEqual(CommandResult.Replaced, sim.Enqueue(BattleCommand.Overload(1)));
            var events = SimRunner.Run(sim, 10);

            Assert.AreEqual((int)CommandKind.Overload, SimRunner.ActionsBy(events, 1).Single().Detail);
        }

        [Test]
        public void EnemyCommandsAndManualBasicAttack_AreNotControllable()
        {
            var sim = Create(new[] { TestUnits.Ally(1) });

            Assert.AreEqual(CommandResult.RejectedNotControllable, sim.Enqueue(BattleCommand.UseSkill(101)));
            Assert.AreEqual(CommandResult.RejectedNotControllable, sim.Enqueue(BattleCommand.BasicAttack(1)));
            Assert.AreEqual(CommandResult.RejectedUnknownUnit, sim.Enqueue(BattleCommand.UseSkill(999)));
        }

        [Test]
        public void Overload_RequiresCatalystAndOverloadableSkill()
        {
            var noCatalyst = TestUnits.Ally(1, skill: TestUnits.Skill(canOverload: true));
            var notOverloadable = TestUnits.Ally(2, catalyst: new Catalyst(100), skill: TestUnits.Skill(canOverload: false));
            var sim = Create(new[] { noCatalyst, notOverloadable });

            Assert.AreEqual(CommandResult.RejectedCannotOverload, sim.Enqueue(BattleCommand.Overload(1)));
            Assert.AreEqual(CommandResult.RejectedCannotOverload, sim.Enqueue(BattleCommand.Overload(2)));
        }

        [Test]
        public void CommandForDownedUnit_IsRejected()
        {
            var fragile = TestUnits.Ally(1, Row.Front, stats: TestUnits.MakeStats(hp: 10));
            var backup = TestUnits.Ally(2, Row.Back, stats: TestUnits.MakeStats(spd: TestUnits.NeverActs));
            var brute = TestUnits.Enemy(101, stats: TestUnits.MakeStats(atk: 100, hp: TestUnits.Unkillable, spd: 1000));
            var sim = Create(new[] { fragile, backup }, TestUnits.OneWave(brute));

            SimRunner.Run(sim, 1);

            Assert.AreEqual(UnitCondition.Downed, SimRunner.Unit(sim, 1).Condition);
            Assert.AreEqual(CommandResult.RejectedIncapacitated, sim.Enqueue(BattleCommand.UseSkill(1)));
        }

        [Test]
        public void Rescue_RequiresScatteredAllyOtherThanSelf()
        {
            var sim = Create(new[] { TestUnits.Ally(1), TestUnits.Ally(2) });

            Assert.AreEqual(CommandResult.RejectedInvalidTarget, sim.Enqueue(BattleCommand.Rescue(2, 1)));
            Assert.AreEqual(CommandResult.RejectedInvalidTarget, sim.Enqueue(BattleCommand.Rescue(1, 1)));
            Assert.AreEqual(CommandResult.RejectedInvalidTarget, sim.Enqueue(BattleCommand.Rescue(1, 101)));
        }

        [Test]
        public void UseSkill_RejectedOneTickBeforeCooldownEnds_AcceptedOnTheTickItEnds()
        {
            var sim = Create(new[] { TestUnits.Ally(1, skill: TestUnits.Skill(cooldownTicks: 5)) });
            sim.Enqueue(BattleCommand.UseSkill(1));
            SimRunner.Run(sim, 10);

            SimRunner.Run(sim, 4);
            Assert.AreEqual(1, SimRunner.Unit(sim, 1).CooldownRemaining);
            Assert.AreEqual(CommandResult.RejectedCooldown, sim.Enqueue(BattleCommand.UseSkill(1)));

            SimRunner.Run(sim, 1);
            Assert.AreEqual(0, SimRunner.Unit(sim, 1).CooldownRemaining);
            Assert.AreEqual(CommandResult.Accepted, sim.Enqueue(BattleCommand.UseSkill(1)));
        }

        [Test]
        public void Retreat_CanBeRequestedMoreThanOnce()
        {
            var sim = Create(new[] { TestUnits.Ally(1) });

            Assert.AreEqual(CommandResult.Accepted, sim.Enqueue(BattleCommand.Retreat()));
            Assert.AreEqual(CommandResult.Accepted, sim.Enqueue(BattleCommand.Retreat()));
        }
    }
}
