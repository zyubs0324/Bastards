using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace TAoB.Combat.Tests
{
    /// <summary>
    /// 위치가 전투 한 판 안에서 어떻게 움직이는지. 전진·정지는 <see cref="SectionProgressTests"/> 가 규칙으로 보고,
    /// 여기서는 틱 순서·스폰 좌표·스냅샷·해시가 함께 맞물리는지를 본다.
    /// </summary>
    public class LaneInBattleTests
    {
        /// <summary>자리 간격 0 이라 유닛 좌표가 기준점과 같아, 기대값을 눈으로 따라갈 수 있다.</summary>
        private static LaneConfig MovingLane(int marchSpeedFu = 10, int reachFu = 50)
        {
            return TestBalance.StaticLane(marchSpeedFu: marchSpeedFu, frontReachFu: reachFu, backReachFu: reachFu);
        }

        private static BattleSimulation Create(LaneConfig lane, int[] approach, UnitSetup[][] waves, IRandom random = null)
        {
            return new BattleSimulation(
                TestUnits.Setup(
                    new[] { TestUnits.Ally(1) },
                    waves,
                    TestUnits.Balance(lane: lane),
                    waveApproachFu: approach),
                random ?? new FixedRandom());
        }

        [Test]
        public void StaticPreset_NeverMovesAndConsumesNoRandom()
        {
            // 준비된 값이 없는 난수라 확률 판정이 하나라도 끼면 예외가 난다.
            var sim = new BattleSimulation(
                TestUnits.Setup(new[] { TestUnits.Ally(1), TestUnits.Ally(2, Row.Back) }, TestUnits.OneWave(TestUnits.Enemy(101))),
                new FixedRandom());

            SimRunner.Run(sim, 50);

            Assert.AreEqual(0, sim.AnchorX);
            Assert.AreEqual(0, SimRunner.Unit(sim, 1).PosX);
            Assert.AreEqual(0, SimRunner.Unit(sim, 2).PosX);
            Assert.AreEqual(1, SimRunner.Unit(sim, 101).PosX, "접근 거리 1 짜리 적은 스폰 자리에 그대로 있어야 한다.");
        }

        [Test]
        public void SpawnPositions_AreSetBeforeTheFirstTick()
        {
            // 표시 계층이 등장 순간에 낡은 좌표에서 미끄러지지 않으려면 아직 등장하지 않은 조우의 적도 자리를 가져야 한다.
            var sim = Create(
                MovingLane(),
                new[] { 200, 300 },
                new[] { new[] { TestUnits.Enemy(101) }, new[] { TestUnits.Enemy(201) } });

            Assert.AreEqual(0, sim.AnchorX);
            Assert.AreEqual(200, sim.EnemyAnchorX);
            Assert.AreEqual(0, SimRunner.Unit(sim, 1).PosX);
            Assert.AreEqual(200, SimRunner.Unit(sim, 101).PosX);
            Assert.AreEqual(500, SimRunner.Unit(sim, 201).PosX, "두 번째 조우 기준점은 접근 거리의 누적 합이다.");
        }

        [Test]
        public void EnemyAnchorMovesToTheNextEncounter_WhenTheCurrentOneIsCleared()
        {
            var sim = Create(
                MovingLane(),
                new[] { 1, 300 },
                new[] { new[] { TestUnits.Enemy(101, stats: TestUnits.MakeStats(hp: 1, spd: TestUnits.NeverActs)) }, new[] { TestUnits.Enemy(201) } });

            SimRunner.Run(sim, 10);

            Assert.AreEqual(1, sim.CurrentWave);
            Assert.AreEqual(301, sim.EnemyAnchorX);
        }

        [Test]
        public void AnchorAdvancesEveryTick_UntilTheFrontRowEngages()
        {
            var sim = Create(MovingLane(), new[] { 200 }, TestUnits.OneWave(TestUnits.Enemy(101)));

            SimRunner.Run(sim, 15);
            Assert.AreEqual(150, sim.AnchorX, "사거리 50 인 아군이 200 에 선 적에 닿는 150 에서 멈춘다.");
            Assert.AreEqual(150, SimRunner.Unit(sim, 1).PosX, "유닛은 기준점에 붙어 따라와야 한다.");

            SimRunner.Run(sim, 10);
            Assert.AreEqual(150, sim.AnchorX, "교전이 시작되면 더 전진하지 않는다.");
        }

        [Test]
        public void OutOfReach_KeepsThePendingCommandAndTheGauge()
        {
            var sim = Create(MovingLane(), new[] { 2000 }, TestUnits.OneWave(TestUnits.Enemy(101)));
            Assert.AreEqual(CommandResult.Accepted, sim.Enqueue(BattleCommand.UseSkill(1)));

            var waiting = SimRunner.Run(sim, 30);

            Assert.IsEmpty(SimRunner.OfKind(waiting, BattleEventKind.ActionStarted), "사거리 밖에서는 행동하지 않는다.");
            Assert.AreEqual(1000, SimRunner.Unit(sim, 1).ActionGauge, "게이지는 가득 찬 채 기다린다.");

            var closing = SimRunner.Run(sim, 200);

            BattleEvent first = SimRunner.ActionsBy(closing, 1).First();
            Assert.AreEqual((int)CommandKind.UseSkill, first.Detail, "기다리는 동안 사용자가 누른 기술이 사라져서는 안 된다.");
        }

        [Test]
        public void GaugeIsCappedWhileOutOfReach_SoContactDoesNotTriggerTwoActionsInARow()
        {
            // 상한이 2배로 남아 있으면 교전 직전까지 모아둔 게이지로 접촉 직후 두 틱 연속 공격이 터진다.
            var sim = Create(MovingLane(), new[] { 300 }, TestUnits.OneWave(TestUnits.Enemy(101)));

            var events = SimRunner.Run(sim, 40);

            // 25틱에 기준점이 250 까지 가며 사거리 50 안에 들어오고, 같은 틱의 충전·행동 단계에서 첫 공격이 나간다.
            // 넘친 게이지 100 이 남아 다음 공격은 34틱이다. 상한이 2배였다면 25·26틱 연속으로 두 번 터진다.
            CollectionAssert.AreEqual(
                new[] { 25, 34 },
                SimRunner.ActionsBy(events, 1).Select(e => e.Tick).ToArray());
        }

        [Test]
        public void SnapshotCarriesBothAnchorsAndUnitPositions()
        {
            var sim = Create(MovingLane(), new[] { 200 }, TestUnits.OneWave(TestUnits.Enemy(101)));
            SimRunner.Run(sim, 5);

            BattleSnapshot snapshot = sim.CreateSnapshotBuffer();
            sim.FillSnapshot(snapshot);

            Assert.AreEqual(50, snapshot.AnchorX);
            Assert.AreEqual(200, snapshot.EnemyAnchorX);
            Assert.AreEqual(50, snapshot.Units.First(u => u.Id == 1).PosX);
            Assert.AreEqual(200, snapshot.Units.First(u => u.Id == 101).PosX);
        }

        [Test]
        public void OnlyPosXDiffers_HashDiffers()
        {
            // 접근 거리를 바꾸면 기준점까지 달라져 좌표만의 차이가 아니게 된다.
            // 그래서 기준점은 그대로 두고 자리 간격만 바꾼다 — 달라지는 것은 유닛 좌표뿐이다.
            ulong HashFor(int frontSlotOffsetFu)
            {
                LaneConfig lane = TestBalance.StaticLane(
                    marchSpeedFu: 10, frontSlotOffsetFu: frontSlotOffsetFu, frontReachFu: 500, backReachFu: 1000);
                var sim = Create(lane, new[] { 100 }, TestUnits.OneWave(TestUnits.Enemy(101)));
                Assert.AreEqual(0, sim.AnchorX);
                Assert.AreEqual(100, sim.EnemyAnchorX);
                return sim.BuildResult().StateHash;
            }

            Assert.AreEqual(HashFor(0), HashFor(0));
            Assert.AreNotEqual(HashFor(0), HashFor(30), "위치가 해시에 들어가지 않으면 에디터와 폰의 불일치를 잡지 못한다.");
        }

        [Test]
        public void OutOfReachTicks_ConsumeNoRandom()
        {
            // 빈도 롤은 사거리 관문을 지난 뒤에만 돈다(계획서 D6). 준비된 값이 하나뿐이라
            // 건너뛴 틱에 롤이 섞이면 FixedRandom 이 예외를 던진다.
            var random = new FixedRandom(2499);
            var sim = new BattleSimulation(
                TestUnits.Setup(
                    new[] { TestUnits.Ally(1, skillChance: 2500) },
                    TestUnits.OneWave(TestUnits.Enemy(101)),
                    TestUnits.Balance(lane: MovingLane()),
                    waveApproachFu: new[] { 300 }),
                random);

            var waiting = SimRunner.Run(sim, 20);

            Assert.IsEmpty(SimRunner.OfKind(waiting, BattleEventKind.ActionStarted));
            Assert.AreEqual(1, random.Remaining, "사거리 밖에서 건너뛴 틱은 난수를 쓰지 않는다.");

            var contact = SimRunner.Run(sim, 10);

            BattleEvent first = SimRunner.ActionsBy(contact, 1).Single();
            Assert.AreEqual(25, first.Tick);
            Assert.AreEqual((int)CommandKind.UseSkill, first.Detail);
            Assert.AreEqual(0, random.Remaining);
        }

        [Test]
        public void PendingCommandIsInTheHash()
        {
            // 사거리 판정으로 대기 명령의 수명이 길어졌으므로 해시에 들어가야 한다.
            var plain = Create(MovingLane(), new[] { 2000 }, TestUnits.OneWave(TestUnits.Enemy(101)));
            var queued = Create(MovingLane(), new[] { 2000 }, TestUnits.OneWave(TestUnits.Enemy(101)));
            queued.Enqueue(BattleCommand.UseSkill(1));

            SimRunner.Run(plain, 20);
            SimRunner.Run(queued, 20);

            Assert.AreNotEqual(plain.BuildResult().StateHash, queued.BuildResult().StateHash);
        }

        [Test]
        public void OverloadOutOfReach_WaitsInTheQueue_WithoutRollingOrActing()
        {
            // 과부하는 공격이라 사거리 안에서만 나간다(계획서 Phase C). 준비된 값이 없는 난수라 위험 판정이 끼면 예외가 난다.
            UnitSetup Mage() => TestUnits.Ally(1, Row.Front, Job.Mage, skill: TestUnits.Skill(canOverload: true), catalyst: new Catalyst(100));
            var far = new BattleSimulation(
                TestUnits.Setup(new[] { Mage() }, TestUnits.OneWave(TestUnits.Enemy(101)), TestUnits.Balance(lane: MovingLane()), waveApproachFu: new[] { 2000 }),
                new FixedRandom());
            var near = new BattleSimulation(
                TestUnits.Setup(new[] { Mage() }, TestUnits.OneWave(TestUnits.Enemy(101)), TestUnits.Balance(lane: MovingLane()), waveApproachFu: new[] { 20 }),
                new FixedRandom(4000, 4000, 4000));
            far.Enqueue(BattleCommand.Overload(1));
            near.Enqueue(BattleCommand.Overload(1));

            List<BattleEvent> farEvents = SimRunner.Run(far, 30);
            List<BattleEvent> nearEvents = SimRunner.Run(near, 30);

            Assert.IsEmpty(SimRunner.OfKind(farEvents, BattleEventKind.OverloadResolved), "사거리 밖에서는 과부하가 나가지 않는다.");
            Assert.IsEmpty(SimRunner.ActionsBy(farEvents, 1), "대기 중인 명령이 기본 공격으로 새지 않는다.");
            Assert.IsNotEmpty(SimRunner.OfKind(nearEvents, BattleEventKind.OverloadResolved), "대조: 사거리 안이면 같은 명령이 나간다.");
        }

        [Test]
        public void ScatteredAllyHoldsTheFormation_UntilItIsRescued()
        {
            // 과부하는 공격이라 대상이 사거리 안에 있어야 나간다. 뒷열 마법사만 먼 적에 닿는 구성으로 맞춘다 —
            // 앞열은 닿지 않아 진형은 행군 중이고, 흩어짐이 그 행군을 멈추는지 본다.
            LaneConfig longBackReach = TestBalance.StaticLane(marchSpeedFu: 10, frontReachFu: 50, backReachFu: 3000);
            var mage = TestUnits.Ally(2, Row.Back, Job.Mage, skill: TestUnits.Skill(canOverload: true), catalyst: new Catalyst(100));
            var sim = new BattleSimulation(
                TestUnits.Setup(
                    new[] { TestUnits.Ally(1), mage },
                    TestUnits.OneWave(TestUnits.Enemy(101)),
                    TestUnits.Balance(lane: longBackReach),
                    waveApproachFu: new[] { 2000 }),
                // 과부하 초과 500‰ 구간에서 4000 은 흩어짐이다(ScatterRescueTests 와 같은 값).
                new FixedRandom(4000));
            sim.Enqueue(BattleCommand.Overload(2));

            SimRunner.Run(sim, 10);
            Assert.AreEqual(UnitCondition.Scattered, SimRunner.Unit(sim, 2).Condition);
            int anchorWhenScattered = sim.AnchorX;

            SimRunner.Run(sim, 20);
            Assert.AreEqual(anchorWhenScattered, sim.AnchorX, "흩어진 동료를 두고 전진하면 진형이 영구히 벌어진다.");

            sim.Enqueue(BattleCommand.Rescue(1, 2));
            SimRunner.Run(sim, 20);

            Assert.AreEqual(UnitCondition.Normal, SimRunner.Unit(sim, 2).Condition);
            Assert.Greater(sim.AnchorX, anchorWhenScattered, "구조가 끝나면 다시 행군한다.");
        }

        [Test]
        public void AfterAnEncounterIsCleared_TheFormationMarchesAgain_AndEngagesTheNext()
        {
            // 조우 전환의 핵심: 멈췄던 기준점이 다시 전진하고 다음 조우에서 또 멈춘다.
            var frail = TestUnits.MakeStats(hp: 1, spd: TestUnits.NeverActs);
            var sim = Create(
                MovingLane(),
                new[] { 100, 400 },
                new[] { new[] { TestUnits.Enemy(101, stats: frail) }, new[] { TestUnits.Enemy(201, stats: frail) } });

            SimRunner.Run(sim, 10);
            Assert.AreEqual(1, sim.CurrentWave, "첫 조우를 정리하고 다음 조우로 넘어가야 한다.");
            int anchorAfterFirst = sim.AnchorX;

            SimRunner.Run(sim, 10);
            Assert.Greater(sim.AnchorX, anchorAfterFirst, "조우가 바뀌면 다시 행군한다.");

            SimRunner.Run(sim, 60);
            Assert.AreEqual(BattleOutcome.Victory, sim.Outcome, "두 번째 조우까지 닿아 정리해야 한다.");
        }

        [Test]
        public void TimeLimitDuringTheMarch_IsADefeat()
        {
            // 접근 거리가 제한 시간보다 길면 적을 만나지 못한 채 끝난다. 규칙 모듈은 이 구성을 막지 않는다
            // (파일 간 시간 예산 검증은 데이터 로더의 몫이다) — 그래도 전투가 매달리지 않고 끝나야 한다.
            var sim = new BattleSimulation(
                TestUnits.Setup(
                    new[] { TestUnits.Ally(1) },
                    TestUnits.OneWave(TestUnits.Enemy(101)),
                    TestUnits.Balance(lane: MovingLane()),
                    timeLimitTicks: 30,
                    waveApproachFu: new[] { 5000 }),
                new FixedRandom());

            var events = SimRunner.Run(sim, 40);

            Assert.AreEqual(BattleOutcome.DefeatTimeLimit, sim.Outcome);
            Assert.AreEqual(300, sim.AnchorX, "제한 시간까지 행군만 한다.");
            Assert.IsEmpty(SimRunner.OfKind(events, BattleEventKind.Damage));
        }

        [Test]
        public void RetreatDuringTheMarch_EndsTheBattleImmediately()
        {
            var sim = Create(MovingLane(), new[] { 2000 }, TestUnits.OneWave(TestUnits.Enemy(101)));
            SimRunner.Run(sim, 5);
            int anchorBeforeRetreat = sim.AnchorX;

            Assert.AreEqual(CommandResult.Accepted, sim.Enqueue(BattleCommand.Retreat()));
            SimRunner.Run(sim, 5);

            Assert.AreEqual(BattleOutcome.DefeatRetreat, sim.Outcome);
            Assert.AreEqual(anchorBeforeRetreat, sim.AnchorX,
                "후퇴는 이동 단계 앞에서 전투를 끝내므로 그 틱에는 기준점이 움직이지 않는다.");
        }

        [Test]
        public void LastEnemyFallingInTheSameTick_StillLetsTheNextAllyAct()
        {
            // 마지막 적이 쓰러진 뒤 행동하는 동료에게는 상대가 아예 없다.
            // 그 경우를 "사거리 밖"과 같이 다루면 게이지가 영원히 차 있는 채로 전투가 멈춘다.
            var sim = new BattleSimulation(
                TestUnits.Setup(
                    new[] { TestUnits.Ally(1), TestUnits.Ally(2) },
                    TestUnits.OneWave(TestUnits.Enemy(101, stats: TestUnits.MakeStats(hp: 1, spd: TestUnits.NeverActs))),
                    TestUnits.Balance(lane: MovingLane()),
                    waveApproachFu: new[] { 1 }),
                new FixedRandom());

            List<BattleEvent> events = SimRunner.Run(sim, 10);

            Assert.AreEqual(BattleOutcome.Victory, sim.Outcome);
            Assert.AreEqual(2, SimRunner.OfKind(events, BattleEventKind.ActionStarted).Count);
            Assert.AreEqual(1, SimRunner.OfKind(events, BattleEventKind.Damage).Count, "쓰러진 적에게 두 번째 공격이 들어가지는 않는다.");
        }
    }
}
