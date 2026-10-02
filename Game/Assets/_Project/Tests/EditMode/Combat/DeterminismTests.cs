using System.Collections.Generic;
using NUnit.Framework;

namespace TAoB.Combat.Tests
{
    /// <summary>
    /// 같은 시드·같은 틱별 명령이면 결과가 같아야 한다(계획서 Phase 2 결정론).
    /// 크리티컬·과부하가 실제 난수를 쓰는 조건으로 돌린다.
    /// </summary>
    public class DeterminismTests
    {
        private const int MaxTicks = 1200;

        private readonly struct RunOutput
        {
            public RunOutput(BattleResult result, ulong eventHash, ulong commandHash)
            {
                Result = result;
                EventHash = eventHash;
                CommandHash = commandHash;
            }

            public BattleResult Result { get; }
            public ulong EventHash { get; }
            public ulong CommandHash { get; }
        }

        /// <summary>
        /// 이동이 켜진 수치. 접근 거리가 있어 조우마다 행군이 끼므로, 위치가 결정론을 깨는지 함께 본다.
        /// </summary>
        private static LaneConfig MovingLane()
        {
            return TestBalance.StaticLane(
                marchSpeedFu: 15,
                frontSlotOffsetFu: 60,
                backSlotOffsetFu: 40,
                frontReachFu: 120,
                backReachFu: 300,
                engageMarginFu: 10);
        }

        private static BattleSetup CreateSetup(LaneConfig lane, int[] waveApproachFu)
        {
            var knightTool = new Magitool(atkBonus: 20, requiredRes: 100, requiredMnd: 0, maxDurability: 40, durabilityCostPerUse: 1);
            var allies = new[]
            {
                TestUnits.Ally(1, Row.Front, Job.Knight, TestUnits.MakeStats(atk: 60, def: 30, hp: 800, spd: 90, mnd: 20, res: 120),
                    TestUnits.Skill(1400, TargetRule.FrontFirst, false, 40), magitool: knightTool),
                TestUnits.Ally(2, Row.Back, Job.Mage, TestUnits.MakeStats(atk: 40, def: 10, hp: 600, spd: 110, mnd: 100, res: 30),
                    TestUnits.Skill(1600, TargetRule.LowestHp, true, 40), new Catalyst(120)),
                TestUnits.Ally(3, Row.Front, Job.Perceptive, TestUnits.MakeStats(atk: 45, def: 15, hp: 650, spd: 140, mnd: 10, res: 40),
                    TestUnits.Skill(1300, TargetRule.FrontFirst, false, 30)),
            };
            var waves = new[]
            {
                new[]
                {
                    TestUnits.Enemy(101, Row.Front, Job.Knight, TestUnits.MakeStats(atk: 40, def: 20, hp: 600, spd: 80), TestUnits.Skill(1300, TargetRule.FrontFirst, false, 50)),
                    TestUnits.Enemy(102, Row.Back, Job.Mage, TestUnits.MakeStats(atk: 45, def: 10, hp: 500, spd: 95), TestUnits.Skill(1400, TargetRule.LowestHp, false, 50)),
                },
                new[]
                {
                    TestUnits.Enemy(201, Row.Front, Job.Perceptive, TestUnits.MakeStats(atk: 70, def: 30, hp: 2500, spd: 70), TestUnits.Skill(1200, TargetRule.All, false, 60), isBoss: true),
                },
            };
            var affinity = new AffinityTable(new[]
            {
                new AffinityEntry(Job.Perceptive, Job.Mage, AffinityLevel.Strong),
                new AffinityEntry(Job.Mage, Job.Knight, AffinityLevel.Strong),
                new AffinityEntry(Job.Knight, Job.Perceptive, AffinityLevel.Strong),
                new AffinityEntry(Job.Mage, Job.Perceptive, AffinityLevel.Weak),
            });
            return TestUnits.Setup(allies, waves, TestBalance.Create(lane: lane), MaxTicks, PreconditionKind.None, affinity, waveApproachFu);
        }

        /// <summary>틱 번호(이미 처리된 마지막 틱) → 그 직후 넣을 명령.</summary>
        private static readonly Dictionary<int, BattleCommand[]> Script = new Dictionary<int, BattleCommand[]>
        {
            { 5, new[] { BattleCommand.Overload(2) } },
            { 30, new[] { BattleCommand.UseSkill(1) } },
            { 60, new[] { BattleCommand.UseSkill(3), BattleCommand.Overload(2) } },
            { 90, new[] { BattleCommand.Rescue(1, 2) } },
            { 150, new[] { BattleCommand.Overload(2), BattleCommand.UseSkill(1) } },
        };

        private static RunOutput Run(uint seed, int ticksPerFrame, LaneConfig lane = null, int[] waveApproachFu = null)
        {
            var sim = new BattleSimulation(CreateSetup(lane, waveApproachFu), new SeededRandom(seed));
            var buffer = new List<BattleEvent>();
            var eventHasher = new StateHasher();
            var commandHasher = new StateHasher();

            while (sim.Outcome == BattleOutcome.InProgress)
            {
                for (int i = 0; i < ticksPerFrame && sim.Outcome == BattleOutcome.InProgress; i++)
                {
                    if (Script.TryGetValue(sim.Tick, out BattleCommand[] commands))
                    {
                        foreach (BattleCommand command in commands)
                        {
                            commandHasher.Add((int)sim.Enqueue(command));
                        }
                    }

                    sim.Step(buffer);
                    foreach (BattleEvent e in buffer)
                    {
                        SimRunner.AddEvent(eventHasher, e);
                    }
                }
            }

            return new RunOutput(sim.BuildResult(), eventHasher.Value, commandHasher.Value);
        }

        [Test]
        public void SameSeedAndScript_ProduceIdenticalResult()
        {
            RunOutput a = Run(42u, 1);
            RunOutput b = Run(42u, 1);

            Assert.AreNotEqual(BattleOutcome.InProgress, a.Result.Outcome);
            Assert.AreEqual(a.Result.Outcome, b.Result.Outcome);
            Assert.AreEqual(a.Result.Tick, b.Result.Tick);
            Assert.AreEqual(a.Result.StateHash, b.Result.StateHash);
            Assert.AreEqual(a.EventHash, b.EventHash);
            Assert.AreEqual(a.CommandHash, b.CommandHash);
        }

        [Test]
        public void SteppingInBatches_ProducesSameResultAsOneTickAtATime()
        {
            RunOutput single = Run(42u, 1);
            RunOutput batched = Run(42u, 4);

            Assert.AreEqual(single.Result.Outcome, batched.Result.Outcome);
            Assert.AreEqual(single.Result.Tick, batched.Result.Tick);
            Assert.AreEqual(single.Result.StateHash, batched.Result.StateHash);
            Assert.AreEqual(single.EventHash, batched.EventHash);
        }

        [Test]
        public void MovingFormation_SameSeed_ProducesIdenticalResult()
        {
            var approach = new[] { 400, 500 };
            RunOutput a = Run(42u, 1, MovingLane(), approach);
            RunOutput b = Run(42u, 1, MovingLane(), approach);

            Assert.AreNotEqual(BattleOutcome.InProgress, a.Result.Outcome);
            Assert.AreEqual(a.Result.Outcome, b.Result.Outcome);
            Assert.AreEqual(a.Result.Tick, b.Result.Tick);
            Assert.AreEqual(a.Result.StateHash, b.Result.StateHash);
            Assert.AreEqual(a.EventHash, b.EventHash);
        }

        [Test]
        public void MovingFormation_SteppingInBatches_MatchesOneTickAtATime()
        {
            var approach = new[] { 400, 500 };
            RunOutput single = Run(42u, 1, MovingLane(), approach);
            RunOutput batched = Run(42u, 4, MovingLane(), approach);

            Assert.AreEqual(single.Result.Tick, batched.Result.Tick);
            Assert.AreEqual(single.Result.StateHash, batched.Result.StateHash);
            Assert.AreEqual(single.EventHash, batched.EventHash);
        }

        [Test]
        public void MovingFormation_ChangesTheEventStream_ComparedToStandingStill()
        {
            // 행군하는 동안은 사거리 밖이라 서로 때리지 못한다. 상태 해시는 좌표만 달라도 바뀌므로
            // 이벤트 흐름과 전투 길이로 "이동이 전투 자체를 바꿨는지"를 본다.
            RunOutput standing = Run(42u, 1);
            RunOutput marching = Run(42u, 1, MovingLane(), new[] { 400, 500 });

            Assert.AreNotEqual(standing.EventHash, marching.EventHash);
            Assert.Greater(marching.Result.Tick, standing.Result.Tick);
        }

        [Test]
        public void DifferentSeed_ChangesEventStream()
        {
            Assert.AreNotEqual(Run(42u, 1).EventHash, Run(7u, 1).EventHash);
        }
    }
}
