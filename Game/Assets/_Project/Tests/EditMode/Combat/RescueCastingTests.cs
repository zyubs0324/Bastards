using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace TAoB.Combat.Tests
{
    /// <summary>
    /// 구조 캐스팅(계획서 Phase D). 기본 장면: 10틱에 마법사(1)가 흩어지고, 같은 SPD 의 기사(2)는 그 틱에 기본 공격을 해
    /// 20틱에 다음 차례가 온다 — 구조 명령은 그때 시작된다. 정지 프리셋이라 모두가 교전 중이어서 자동 구조는 끼어들지 않는다.
    /// </summary>
    public class RescueCastingTests
    {
        private const int ScatterTick = 10;
        private const int RescueStartTick = 20;

        private static UnitSetup Mage()
        {
            return TestUnits.Ally(1, Row.Back, Job.Mage, skill: TestUnits.Skill(canOverload: true), catalyst: new Catalyst(100));
        }

        /// <summary>25틱·50틱에 앞열(기사)을 때리는 적. SPD 40 이라 게이지 1000 이 25틱마다 찬다.</summary>
        private static UnitSetup StrikerAtTick25(int atk = 10)
        {
            return TestUnits.Enemy(101, stats: TestUnits.MakeStats(atk: atk, hp: TestUnits.Unkillable, spd: 40));
        }

        /// <param name="laterRandom">흩어짐 판정(4000) 뒤에 이어서 쓸 난수. 두 번째 과부하를 보는 테스트가 준다.</param>
        private static BattleSimulation ScatteredAtTick10(
            int rescueTicks, UnitSetup enemy = null, UnitSetup knight = null, UnitSetup third = null, int scatterHpLossPerTick = 5,
            int[] laterRandom = null)
        {
            var allies = new List<UnitSetup> { Mage(), knight ?? TestUnits.Ally(2, Row.Front, Job.Knight) };
            if (third != null) allies.Add(third);

            var sim = new BattleSimulation(
                TestUnits.Setup(
                    allies.ToArray(),
                    TestUnits.OneWave(enemy ?? TestUnits.Enemy(101)),
                    TestUnits.Balance(scatterHpLossPerTick: scatterHpLossPerTick, rescueTicks: rescueTicks)),
                // 과부하 초과 500‰ 구간에서 4000 은 흩어짐이다(ScatterRescueTests 와 같은 값).
                new FixedRandom(new[] { 4000 }.Concat(laterRandom ?? new int[0]).ToArray()));
            sim.Enqueue(BattleCommand.Overload(1));
            SimRunner.Run(sim, ScatterTick);
            Assert.AreEqual(UnitCondition.Scattered, SimRunner.Unit(sim, 1).Condition, "전제: 10틱에 흩어짐");
            return sim;
        }

        [Test]
        public void Rescue_TakesRescueTicks_AndTheStartingTickCountsAsOne()
        {
            var sim = ScatteredAtTick10(rescueTicks: 5);
            sim.Enqueue(BattleCommand.Rescue(2, 1));

            var events = SimRunner.Run(sim, 13);
            Assert.AreEqual(RescueStartTick, SimRunner.OfKind(events, BattleEventKind.RescueStarted).Single().Tick);
            Assert.IsEmpty(SimRunner.OfKind(events, BattleEventKind.Rescued), "23틱(진행 4)까지는 끝나지 않는다.");
            Assert.AreEqual(UnitCondition.Scattered, SimRunner.Unit(sim, 1).Condition);
            Assert.AreEqual(4, SimRunner.Unit(sim, 2).RescueProgress);

            BattleEvent rescued = SimRunner.OfKind(SimRunner.Run(sim, 1), BattleEventKind.Rescued).Single();
            Assert.AreEqual(24, rescued.Tick, "20틱이 진행 1 이므로 다섯 번째 틱은 24틱이다.");
            Assert.AreEqual(2, rescued.SourceId);
            Assert.AreEqual(1, rescued.TargetId);
            Assert.AreEqual(UnitCondition.Normal, SimRunner.Unit(sim, 1).Condition);
            Assert.IsFalse(SimRunner.Unit(sim, 2).IsRescuing, "끝나면 구조 상태가 풀린다.");
        }

        [Test]
        public void Rescuer_NeitherActsNorFillsTheGauge_WhileRescuing()
        {
            var sim = ScatteredAtTick10(rescueTicks: 30);
            sim.Enqueue(BattleCommand.Rescue(2, 1));

            // 11~48틱(구조는 49틱에 끝난다). 구조하지 않았다면 기사는 30·40틱에도 행동했다.
            var events = SimRunner.Run(sim, 38);

            BattleEvent onlyAction = SimRunner.ActionsBy(events, 2).Single();
            Assert.AreEqual((int)CommandKind.Rescue, onlyAction.Detail);
            Assert.AreEqual(RescueStartTick, onlyAction.Tick);
            Assert.AreEqual(0, SimRunner.Unit(sim, 2).ActionGauge, "구조하는 동안 게이지는 멈춘다.");
            Assert.AreEqual(1, SimRunner.Unit(sim, 2).RescueTargetId);
        }

        [Test]
        public void HitOnTheCompletingTick_ResetsTheProgress_InsteadOfCompleting()
        {
            // 진행 6 이 되는 25틱에 적이 기사를 때린다. 피격이 완료보다 먼저라 구조는 처음부터 다시 센다.
            var sim = ScatteredAtTick10(rescueTicks: 6, enemy: StrikerAtTick25());
            sim.Enqueue(BattleCommand.Rescue(2, 1));

            var events = SimRunner.Run(sim, 20);

            BattleEvent interrupted = SimRunner.OfKind(events, BattleEventKind.RescueInterrupted).Single();
            Assert.AreEqual(25, interrupted.Tick);
            Assert.AreEqual(2, interrupted.SourceId);
            Assert.AreEqual(5, interrupted.Value, "잃은 진행은 직전 틱까지 쌓은 5 다.");
            Assert.AreEqual(30, SimRunner.OfKind(events, BattleEventKind.Rescued).Single().Tick, "맞은 25틱이 다시 진행 1 이라 여섯 번째 틱은 30틱이다.");
        }

        [Test]
        public void RescueThatCompletesTheTickBeforeTheHit_IsNotInterrupted()
        {
            var sim = ScatteredAtTick10(rescueTicks: 5, enemy: StrikerAtTick25());
            sim.Enqueue(BattleCommand.Rescue(2, 1));

            var events = SimRunner.Run(sim, 20);

            Assert.AreEqual(24, SimRunner.OfKind(events, BattleEventKind.Rescued).Single().Tick);
            Assert.IsEmpty(SimRunner.OfKind(events, BattleEventKind.RescueInterrupted));
        }

        [Test]
        public void TargetDownedMidCast_CancelsTheRescue_AndTheRescuerFightsAgain()
        {
            // 틱당 60 을 잃어 27틱에 HP 가 바닥난다(17틱 × 60 ≥ 1000). 구조는 20틱에 시작해 39틱에야 끝날 예정이었다.
            var sim = ScatteredAtTick10(rescueTicks: 20, scatterHpLossPerTick: 60);
            sim.Enqueue(BattleCommand.Rescue(2, 1));

            var events = SimRunner.Run(sim, 30);

            BattleEvent cancelled = SimRunner.OfKind(events, BattleEventKind.RescueCancelled).Single();
            Assert.AreEqual(27, cancelled.Tick);
            Assert.AreEqual(2, cancelled.SourceId);
            Assert.IsEmpty(SimRunner.OfKind(events, BattleEventKind.Rescued));
            Assert.IsFalse(SimRunner.Unit(sim, 2).IsRescuing);
            Assert.IsTrue(
                SimRunner.ActionsBy(events, 2).Any(e => e.Tick > 27 && e.Detail == (int)CommandKind.BasicAttack),
                "구조가 취소되면 게이지가 다시 차 전투로 돌아온다.");
        }

        [Test]
        public void RescuerDownedMidCast_CancelsTheRescue()
        {
            var frailKnight = TestUnits.Ally(2, Row.Front, Job.Knight, stats: TestUnits.MakeStats(hp: 5));
            var sim = ScatteredAtTick10(rescueTicks: 20, enemy: StrikerAtTick25(atk: 50), knight: frailKnight);
            sim.Enqueue(BattleCommand.Rescue(2, 1));

            var events = SimRunner.Run(sim, 20);

            Assert.AreEqual(25, SimRunner.OfKind(events, BattleEventKind.RescueCancelled).Single().Tick);
            Assert.IsEmpty(SimRunner.OfKind(events, BattleEventKind.Rescued));
            Assert.IsEmpty(SimRunner.OfKind(events, BattleEventKind.RescueInterrupted), "쓰러진 구조자는 끊긴 것이 아니라 취소된 것이다.");
            Assert.AreEqual(UnitCondition.Scattered, SimRunner.Unit(sim, 1).Condition);
            Assert.AreEqual(BattleEvent.NoUnit, SimRunner.Unit(sim, 2).RescueTargetId);
        }

        [Test]
        public void TargetDownedOnTheCompletingTick_IsCancelled_NotRevived()
        {
            // 구조는 27틱(진행 8)에 끝날 예정이고, 대상은 바로 그 틱의 흩어짐 피해로 쓰러진다.
            // 취소가 완료보다 먼저라 HP 0 인 동료가 정상으로 돌아오지 않는다.
            var sim = ScatteredAtTick10(rescueTicks: 8, scatterHpLossPerTick: 60);
            sim.Enqueue(BattleCommand.Rescue(2, 1));

            var events = SimRunner.Run(sim, 20);

            Assert.AreEqual(27, SimRunner.OfKind(events, BattleEventKind.RescueCancelled).Single().Tick);
            Assert.IsEmpty(SimRunner.OfKind(events, BattleEventKind.Rescued));
            Assert.AreEqual(UnitCondition.Downed, SimRunner.Unit(sim, 1).Condition);
            Assert.AreEqual(0, SimRunner.Unit(sim, 1).Hp);
        }

        [Test]
        public void RescuerDownedOnTheCompletingTick_IsCancelled_AndTheTargetStaysScattered()
        {
            var frailKnight = TestUnits.Ally(2, Row.Front, Job.Knight, stats: TestUnits.MakeStats(hp: 5));
            var sim = ScatteredAtTick10(rescueTicks: 6, enemy: StrikerAtTick25(atk: 50), knight: frailKnight);
            sim.Enqueue(BattleCommand.Rescue(2, 1));

            var events = SimRunner.Run(sim, 20);

            Assert.AreEqual(25, SimRunner.OfKind(events, BattleEventKind.RescueCancelled).Single().Tick, "25틱은 진행 6 이 되는 완료 틱이다.");
            Assert.IsEmpty(SimRunner.OfKind(events, BattleEventKind.Rescued));
            Assert.AreEqual(UnitCondition.Scattered, SimRunner.Unit(sim, 1).Condition);
        }

        [Test]
        public void Completion_CostsTheRescuerTheExtraGauge()
        {
            // 24틱에 구조를 마치면 기사의 게이지는 0 에서 200 이 더 빠진다. 틱당 100 을 채우므로 다음 행동은 12틱 뒤다.
            var sim = ScatteredAtTick10(rescueTicks: 5);
            sim.Enqueue(BattleCommand.Rescue(2, 1));

            var events = SimRunner.Run(sim, 30);

            int nextAction = SimRunner.ActionsBy(events, 2).First(e => e.Tick > 24).Tick;
            Assert.AreEqual(36, nextAction);
        }

        [Test]
        public void HitOnTheStartingTick_LosesNothing()
        {
            // 적(SPD 50)은 20틱에 기사를 때린다. 기사가 먼저 행동해 구조를 시작한 직후라 잃을 진행이 없다.
            var enemy = TestUnits.Enemy(101, stats: TestUnits.MakeStats(atk: 10, hp: TestUnits.Unkillable, spd: 50));
            var sim = ScatteredAtTick10(rescueTicks: 5, enemy: enemy);
            sim.Enqueue(BattleCommand.Rescue(2, 1));

            var events = SimRunner.Run(sim, 15);

            Assert.IsTrue(SimRunner.OfKind(events, BattleEventKind.Damage).Any(e => e.Tick == RescueStartTick && e.TargetId == 2), "전제: 20틱에 기사가 맞았다.");
            Assert.IsEmpty(SimRunner.OfKind(events, BattleEventKind.RescueInterrupted));
            Assert.AreEqual(24, SimRunner.OfKind(events, BattleEventKind.Rescued).Single().Tick);
        }

        [Test]
        public void StrayHitFromAnAllysRunaway_InterruptsTheRescue()
        {
            // 두 번째 마법사(3)가 30틱에 과부하한다. 7000 은 폭주이고, 폭주 후보(시전자를 뺀 ID 순: 1, 2, 101)에서 1 번째인 기사(2)가 맞는다.
            var secondMage = TestUnits.Ally(3, Row.Back, Job.Mage, skill: TestUnits.Skill(canOverload: true), catalyst: new Catalyst(100));
            var sim = ScatteredAtTick10(rescueTicks: 30, third: secondMage, laterRandom: new[] { 7000, 1 });
            sim.Enqueue(BattleCommand.Rescue(2, 1));
            SimRunner.Run(sim, 11);
            sim.Enqueue(BattleCommand.Overload(3));

            var events = SimRunner.Run(sim, 9);

            BattleEvent interrupted = SimRunner.OfKind(events, BattleEventKind.RescueInterrupted).Single();
            Assert.AreEqual(30, interrupted.Tick);
            Assert.AreEqual(2, interrupted.SourceId);
            Assert.AreEqual(10, interrupted.Value, "20~29틱에 쌓은 진행 10 을 잃는다.");
        }

        [Test]
        public void LeftoverGauge_DoesNotLetTheRescuerAct()
        {
            // SPD 1500 인 기사는 매 틱 행동하고도 게이지가 1000 남는다. 구조를 시작한 뒤에도 그 게이지는 남아 있지만 행동하지 않는다.
            var fastKnight = TestUnits.Ally(2, Row.Front, Job.Knight, stats: TestUnits.MakeStats(spd: 1500));
            var sim = ScatteredAtTick10(rescueTicks: 10, knight: fastKnight);
            sim.Enqueue(BattleCommand.Rescue(2, 1));

            // 11~19틱(구조는 11틱에 시작해 20틱에 끝난다).
            var events = SimRunner.Run(sim, 9);

            BattleEvent onlyAction = SimRunner.ActionsBy(events, 2).Single();
            Assert.AreEqual((int)CommandKind.Rescue, onlyAction.Detail);
            Assert.AreEqual(11, onlyAction.Tick);
            Assert.AreEqual(1000, SimRunner.Unit(sim, 2).ActionGauge, "전제: 행동하고도 남을 게이지를 쥔 채 구조하고 있다.");
        }

        [Test]
        public void SecondQueuedRescueForTheSameTarget_FallsBackWhenItsTurnComes()
        {
            // 두 기사가 같은 동료의 구조를 지시받았다(둘 다 아직 차례가 아니라 받아들여진다). 20틱에 2 번이 먼저 시작하면 3 번의 명령은 성립하지 않는다.
            var sim = ScatteredAtTick10(rescueTicks: 30, third: TestUnits.Ally(3, Row.Front, Job.Knight));
            Assert.AreEqual(CommandResult.Accepted, sim.Enqueue(BattleCommand.Rescue(2, 1)));
            Assert.AreEqual(CommandResult.Accepted, sim.Enqueue(BattleCommand.Rescue(3, 1)));

            var events = SimRunner.Run(sim, 10);

            Assert.AreEqual(2, SimRunner.OfKind(events, BattleEventKind.RescueStarted).Single().SourceId);
            BattleEvent fallback = SimRunner.OfKind(events, BattleEventKind.CommandFallback).Single();
            Assert.AreEqual(3, fallback.SourceId);
            Assert.AreEqual((int)CommandKind.Rescue, fallback.Detail);
        }

        [TestCase(0)]
        [TestCase(BattleSetup.MaxTimeLimitTicks + 1)]
        public void RescueTicksOutOfRange_IsRejected(int rescueTicks)
        {
            var ex = Assert.Throws<System.ArgumentException>(() => TestBalance.Create(rescueTicks: rescueTicks));

            Assert.AreEqual("rescueTicks", ex.ParamName);
        }

        [TestCase(1)]
        [TestCase(BattleSetup.MaxTimeLimitTicks)]
        public void RescueTicksAtItsBounds_IsAccepted(int rescueTicks)
        {
            Assert.AreEqual(rescueTicks, TestBalance.Create(rescueTicks: rescueTicks).RescueTicks);
        }

        [Test]
        public void WhileRescuing_TheRescuerTakesNoOtherCommand_AndNobodyElseRescuesTheSameTarget()
        {
            var sim = ScatteredAtTick10(rescueTicks: 30, third: TestUnits.Ally(3, Row.Front, Job.Knight));
            sim.Enqueue(BattleCommand.Rescue(2, 1));
            SimRunner.Run(sim, 11);
            Assert.IsTrue(SimRunner.Unit(sim, 2).IsRescuing, "전제: 20틱에 구조를 시작했다.");

            Assert.AreEqual(CommandResult.RejectedBusy, sim.Enqueue(BattleCommand.UseSkill(2)));
            Assert.AreEqual(CommandResult.RejectedBusy, sim.Enqueue(BattleCommand.Rescue(2, 1)));
            Assert.AreEqual(CommandResult.RejectedInvalidTarget, sim.Enqueue(BattleCommand.Rescue(3, 1)), "한 동료를 둘이 구조하지 않는다.");
        }

        [Test]
        public void EngagedAllies_DoNotStartARescueOnTheirOwn()
        {
            var sim = ScatteredAtTick10(rescueTicks: 5);

            var events = SimRunner.Run(sim, 40);

            Assert.IsEmpty(SimRunner.OfKind(events, BattleEventKind.RescueStarted), "교전 중의 구조는 사용자가 정한다.");
        }

        [Test]
        public void BattleEnd_ClearsARescueInProgress()
        {
            var sim = ScatteredAtTick10(rescueTicks: 30);
            sim.Enqueue(BattleCommand.Rescue(2, 1));
            SimRunner.Run(sim, 11);
            sim.Enqueue(BattleCommand.Retreat());

            SimRunner.Run(sim, 1);

            Assert.AreEqual(BattleOutcome.DefeatRetreat, sim.Outcome);
            Assert.IsFalse(SimRunner.Unit(sim, 2).IsRescuing);
            Assert.AreEqual(UnitCondition.Downed, SimRunner.Unit(sim, 1).Condition);
        }

        // ---- 자동 구조: 아무도 교전 중이 아닐 때 ----

        /// <summary>뒷열 마법사만 먼 적에 닿는 이동 설정. 과부하로 흩어지게 한 뒤에도 앞열은 교전하지 않는다.</summary>
        private static BattleSimulation ScatteredOnTheMarch(int rescueTicks, UnitSetup third = null)
        {
            LaneConfig lane = TestBalance.StaticLane(marchSpeedFu: 10, frontReachFu: 50, backReachFu: 3000);
            var allies = new List<UnitSetup>
            {
                TestUnits.Ally(1, Row.Front, Job.Knight),
                TestUnits.Ally(2, Row.Back, Job.Mage, skill: TestUnits.Skill(canOverload: true), catalyst: new Catalyst(100)),
            };
            if (third != null) allies.Add(third);

            var sim = new BattleSimulation(
                TestUnits.Setup(
                    allies.ToArray(),
                    TestUnits.OneWave(TestUnits.Enemy(101)),
                    TestUnits.Balance(lane: lane, rescueTicks: rescueTicks),
                    waveApproachFu: new[] { 2000 }),
                new FixedRandom(4000));
            sim.Enqueue(BattleCommand.Overload(2));
            return sim;
        }

        [Test]
        public void NobodyEngaged_StartsTheRescueAutomatically_AndTheFormationWaitsForIt()
        {
            var sim = ScatteredOnTheMarch(rescueTicks: 5);

            var events = SimRunner.Run(sim, ScatterTick);
            BattleEvent started = SimRunner.OfKind(events, BattleEventKind.RescueStarted).Single();
            Assert.AreEqual(ScatterTick, started.Tick, "흩어진 그 틱에 바로 구조를 시작한다(교전 중이 아니다).");
            Assert.AreEqual(1, started.SourceId);
            Assert.AreEqual(2, started.TargetId);
            int anchorWhenScattered = sim.AnchorX;

            events = SimRunner.Run(sim, 4);
            Assert.AreEqual(14, SimRunner.OfKind(events, BattleEventKind.Rescued).Single().Tick);
            Assert.AreEqual(anchorWhenScattered, sim.AnchorX, "구조가 끝날 때까지 진형은 전진하지 않는다.");

            SimRunner.Run(sim, 5);
            Assert.Greater(sim.AnchorX, anchorWhenScattered, "구조가 끝나면 다시 행군한다.");
        }

        [Test]
        public void AutomaticRescue_PrefersARearguard_OverTheLowestId()
        {
            var rearguard = TestUnits.Ally(3, Row.Front, Job.Knight, preference: Preference.Rearguard);
            var sim = ScatteredOnTheMarch(rescueTicks: 5, third: rearguard);

            var events = SimRunner.Run(sim, ScatterTick);

            Assert.AreEqual(3, SimRunner.OfKind(events, BattleEventKind.RescueStarted).Single().SourceId);
        }

        [Test]
        public void WhoIsRescuing_IsInTheHash()
        {
            // 두 판은 세 번째 동료의 성향만 다르다(성향은 해시에 없고 진형유지 값이 0 이라 자세도 같다).
            // 그래서 구조자가 3 번이냐 1 번이냐만 갈린다 — 구조 상태가 해시에 없으면 두 판이 같게 나온다.
            var withRearguard = ScatteredOnTheMarch(rescueTicks: 5, third: TestUnits.Ally(3, Row.Front, Job.Knight, preference: Preference.Rearguard));
            var withoutPreference = ScatteredOnTheMarch(rescueTicks: 5, third: TestUnits.Ally(3, Row.Front, Job.Knight));

            SimRunner.Run(withRearguard, ScatterTick);
            SimRunner.Run(withoutPreference, ScatterTick);

            Assert.AreEqual(2, SimRunner.Unit(withRearguard, 3).RescueTargetId, "전제: 후방 유지인 3 번이 구조한다.");
            Assert.AreEqual(2, SimRunner.Unit(withoutPreference, 1).RescueTargetId, "전제: 성향이 없으면 1 번이 구조한다.");
            Assert.AreNotEqual(withRearguard.BuildResult().StateHash, withoutPreference.BuildResult().StateHash);
        }

        [Test]
        public void AutomaticRescue_LeavesATargetTheUserAlreadyOrdered()
        {
            // 10틱: 마법사(1)가 흩어지고 기사(2)가 적(HP 100)을 50 깎는다. 11틱: 사용자가 기사에게 구조를 지시한다(기사 차례는 20틱).
            // 15틱: 세 번째 동료(3)가 적을 쓰러뜨려 다음 조우(멀리 있다)로 넘어간다 — 이때부터 아무도 교전 중이 아니다.
            // 자동 구조가 그 대상을 가로채면 15틱에 시작하고, 사용자의 명령은 실행 불가로 떨어진다.
            LaneConfig lane = TestBalance.StaticLane(marchSpeedFu: 10, frontReachFu: 50, backReachFu: 50);
            UnitSetup[] allies =
            {
                Mage(),
                TestUnits.Ally(2, Row.Front, Job.Knight),
                TestUnits.Ally(3, Row.Front, Job.Knight, stats: TestUnits.MakeStats(spd: 70)),
            };
            UnitSetup[][] waves =
            {
                new[] { TestUnits.Enemy(101, stats: TestUnits.MakeStats(hp: 100, spd: TestUnits.NeverActs)) },
                new[] { TestUnits.Enemy(201) },
            };
            var sim = new BattleSimulation(
                TestUnits.Setup(allies, waves, TestUnits.Balance(lane: lane, rescueTicks: 30), waveApproachFu: new[] { 20, 2000 }),
                new FixedRandom(4000));
            sim.Enqueue(BattleCommand.Overload(1));
            SimRunner.Run(sim, 11);
            Assert.AreEqual(CommandResult.Accepted, sim.Enqueue(BattleCommand.Rescue(2, 1)));

            var events = SimRunner.Run(sim, 12);

            Assert.AreEqual(1, sim.CurrentWave, "전제: 15틱에 첫 조우가 끝났다.");
            BattleEvent started = SimRunner.OfKind(events, BattleEventKind.RescueStarted).Single();
            Assert.AreEqual(RescueStartTick, started.Tick);
            Assert.AreEqual(2, started.SourceId);
            Assert.IsEmpty(SimRunner.OfKind(events, BattleEventKind.CommandFallback));
        }
    }
}
