using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace TAoB.Combat.Tests
{
    /// <summary>
    /// 선호·규율·침투가 전투 한 판 안에서 맞물리는지(계획서 Phase C). 개별 규칙은 <see cref="TacticsTests"/>·
    /// <see cref="TargetSelectorTests"/> 가 보고, 여기서는 조우 시작 결정 → 이동 → 대상 선택이 이어진 결과를 본다.
    /// </summary>
    public class StanceInBattleTests
    {
        private static LaneConfig RealisticLane(int flankChance = 0)
        {
            return TestBalance.RealisticLane(flankChance);
        }

        private static BattleSimulation Create(UnitSetup[] allies, UnitSetup[][] waves, LaneConfig lane, IRandom random, int[] approach = null)
        {
            return new BattleSimulation(
                TestUnits.Setup(allies, waves, TestUnits.Balance(lane: lane), waveApproachFu: approach ?? TestUnits.Approach(waves.Length)),
                random);
        }

        private static UnitSetup Sturdy(int id, Row row, Preference preference = Preference.None, int formationChance = 0)
        {
            return TestUnits.Ally(
                id, row, stats: TestUnits.MakeStats(hp: TestUnits.Unkillable, spd: TestUnits.NeverActs),
                preference: preference, formationChance: formationChance);
        }

        private static UnitSetup Striker(int id)
        {
            return TestUnits.Enemy(id, stats: TestUnits.MakeStats(atk: 10, hp: TestUnits.Unkillable, spd: 1000));
        }

        private static BattleEvent FirstDamage(BattleSimulation sim, int ticks)
        {
            return SimRunner.OfKind(SimRunner.Run(sim, ticks), BattleEventKind.Damage).First();
        }

        private static HashSet<int> DamagedTargets(BattleSimulation sim, int ticks)
        {
            return new HashSet<int>(SimRunner.OfKind(SimRunner.Run(sim, ticks), BattleEventKind.Damage).Select(e => e.TargetId));
        }

        [Test]
        public void FlankingEnemy_StrikesTheBackRow_WhileASteadyEnemyStrikesTheFront()
        {
            UnitSetup[] allies = { Sturdy(1, Row.Front), Sturdy(2, Row.Back) };
            int[] approach = { 600 };

            var flanking = Create(allies, TestUnits.OneWave(Striker(101)), RealisticLane(CombatScale.ChanceRange), new FixedRandom(), approach);
            var steady = Create(allies, TestUnits.OneWave(Striker(101)), RealisticLane(0), new FixedRandom(), approach);

            // 처음에는 앞열만 사거리 안이라 어느 쪽이든 앞열이 먼저 맞는다. 침투한 적이 앞으로 나와 뒷열에 닿은 뒤에야 갈린다.
            Assert.IsTrue(DamagedTargets(flanking, 300).Contains(2), "침투한 적은 결국 뒷열을 때린다.");
            Assert.IsFalse(DamagedTargets(steady, 300).Contains(2), "침투하지 않은 적은 뒷열에 닿지 못한다.");
        }

        [Test]
        public void Guardian_PullsTheEnemyAttackAwayFromTheOtherFrontAlly()
        {
            // 앞열 우선 대상 선택이면 작은 ID(1)를 때리지만, 도발 중인 동료 보호(2)가 사거리 안에 있어 그쪽을 맞는다.
            UnitSetup[] allies = { Sturdy(1, Row.Front), Sturdy(2, Row.Front, Preference.Guardian, CombatScale.ChanceRange) };
            var sim = Create(allies, TestUnits.OneWave(Striker(101)), RealisticLane(), new FixedRandom(), new[] { 600 });

            Assert.AreEqual(2, FirstDamage(sim, 300).TargetId);
        }

        [Test]
        public void Charger_StandsAheadOfTheLine_ByTheChargeDistance()
        {
            UnitSetup[] allies = { Sturdy(1, Row.Front), Sturdy(2, Row.Front, Preference.Charger, CombatScale.ChanceRange) };
            var sim = Create(allies, TestUnits.OneWave(Striker(101)), RealisticLane(), new FixedRandom(), new[] { 600 });

            SimRunner.Run(sim, 60);

            int holder = SimRunner.Unit(sim, 1).PosX;
            int charger = SimRunner.Unit(sim, 2).PosX;
            Assert.AreEqual(RealisticLane().ChargeDriftFu, charger - holder, "자리를 지키는 동료보다 돌진 거리만큼 앞에 선다.");
            Assert.AreEqual(Stance.Charge, SimRunner.Unit(sim, 2).Stance, "스냅샷이 자세를 싣는다.");
            Assert.AreEqual(Stance.Hold, SimRunner.Unit(sim, 1).Stance);
        }

        [Test]
        public void Coward_StandsBack_ButStaysInsideItsOwnReach()
        {
            UnitSetup[] allies = { Sturdy(1, Row.Front, Preference.Coward, CombatScale.ChanceRange), Sturdy(2, Row.Front) };
            var sim = Create(allies, TestUnits.OneWave(Striker(101)), RealisticLane(), new FixedRandom(), new[] { 600 });

            SimRunner.Run(sim, 60);

            Assert.AreEqual(RealisticLane().StepBackFu, SimRunner.Unit(sim, 2).PosX - SimRunner.Unit(sim, 1).PosX);
            int enemyX = SimRunner.Unit(sim, 101).PosX;
            int distance = enemyX - SimRunner.Unit(sim, 1).PosX;
            Assert.LessOrEqual(distance, RealisticLane().FrontReachFu, "물러서도 사거리 안에 남아야 전투에 기여한다(생성자 불변식).");
        }

        [Test]
        public void AllCowardAllies_StillFinishTheBattle_WithoutStallingToTheTimeLimit()
        {
            // 교착 검증: 모두가 물러서도 정지선은 자리 기준이라 전투가 진행되고 사거리 안에서 싸운다.
            var weakStats = TestUnits.MakeStats(atk: 60, hp: 1000, spd: 100);
            UnitSetup[] allies =
            {
                TestUnits.Ally(1, Row.Front, stats: weakStats, preference: Preference.Coward, formationChance: CombatScale.ChanceRange),
                TestUnits.Ally(2, Row.Back, stats: weakStats, preference: Preference.Coward, formationChance: CombatScale.ChanceRange),
            };
            UnitSetup[][] waves = { new[] { TestUnits.Enemy(101, stats: TestUnits.MakeStats(hp: 120, spd: TestUnits.NeverActs)) } };
            var sim = Create(allies, waves, RealisticLane(), new FixedRandom(), new[] { 600 });

            List<BattleEvent> events = SimRunner.Run(sim, 400);

            Assert.AreEqual(BattleOutcome.Victory, sim.Outcome);
            Assert.IsNotEmpty(SimRunner.ActionsBy(events, 2), "물러선 뒷열도 사거리 안에서 실제로 공격에 참여해야 한다.");
        }

        [Test]
        public void StanceAndFlankingAreInTheHash()
        {
            ulong HashFor(Preference preference)
            {
                UnitSetup[] allies = { Sturdy(1, Row.Front, preference, CombatScale.ChanceRange) };
                return Create(allies, TestUnits.OneWave(Striker(101)), RealisticLane(), new FixedRandom(), new[] { 600 }).BuildResult().StateHash;
            }

            Assert.AreEqual(HashFor(Preference.Guardian), HashFor(Preference.Guardian));
            Assert.AreNotEqual(HashFor(Preference.None), HashFor(Preference.Guardian), "자세가 해시에 들어가지 않으면 에디터와 폰의 불일치를 잡지 못한다.");
        }

        [Test]
        public void FlankingFlag_IsInTheHash_EvenWhenTheStanceIsTheSame()
        {
            // 두 적 모두 돌진 자세다. 하나는 돌진 성향이고 하나는 침투해서 돌진이다. 갈리는 것은 침투 여부뿐이다.
            UnitSetup[] allies = { Sturdy(1, Row.Front) };
            UnitSetup charger = TestUnits.Enemy(101, preference: Preference.Charger, formationChance: CombatScale.ChanceRange);
            var plain = Create(allies, TestUnits.OneWave(charger), RealisticLane(0), new FixedRandom(), new[] { 600 });
            var flanking = Create(allies, TestUnits.OneWave(TestUnits.Enemy(101)), RealisticLane(CombatScale.ChanceRange), new FixedRandom(), new[] { 600 });

            Assert.AreEqual(Stance.Charge, SimRunner.Unit(plain, 101).Stance);
            Assert.AreEqual(Stance.Charge, SimRunner.Unit(flanking, 101).Stance);
            Assert.AreNotEqual(plain.BuildResult().StateHash, flanking.BuildResult().StateHash);
        }

        [Test]
        public void AlliesAreRedecidedWhenAnEncounterStarts_NotOncePerBattle()
        {
            // 조우 0 시작에서 4999(성공 → 돌진), 조우 1 시작에서 5000(실패 → 자리 지킴).
            var frail = TestUnits.MakeStats(hp: 1, spd: TestUnits.NeverActs);
            UnitSetup[] allies =
            {
                TestUnits.Ally(1, Row.Front, stats: TestUnits.MakeStats(atk: 50, hp: 1000, spd: 100), preference: Preference.Charger, formationChance: 5000),
            };
            UnitSetup[][] waves = { new[] { TestUnits.Enemy(101, stats: frail) }, new[] { TestUnits.Enemy(201, stats: frail) } };
            var sim = Create(allies, waves, RealisticLane(), new FixedRandom(4999, 5000), new[] { 200, 200 });

            Assert.AreEqual(Stance.Charge, SimRunner.Unit(sim, 1).Stance, "첫 조우에서 성향을 따르기로 뽑았다.");

            var events = new List<BattleEvent>();
            while (sim.CurrentWave == 0 && sim.Tick < 300) sim.Step(events);

            Assert.AreEqual(1, sim.CurrentWave);
            Assert.AreEqual(Stance.Hold, SimRunner.Unit(sim, 1).Stance, "다음 조우에서 다시 뽑아 이번에는 흐트러졌다.");
        }

        [Test]
        public void SameSeedAndBatching_ProduceTheSameBattle_WithRealRandomnessInTheDecisions()
        {
            // 규율 롤과 침투 롤이 실제 난수를 쓰는 구성(진형유지 50%·침투 35%). 결정이 난수열에 섞여도 결정론이 유지돼야 한다.
            ulong Run(uint seed, int ticksPerFrame)
            {
                UnitSetup[] allies =
                {
                    TestUnits.Ally(1, Row.Front, stats: TestUnits.MakeStats(atk: 60, hp: 800, spd: 100), preference: Preference.Guardian, formationChance: 5000),
                    TestUnits.Ally(2, Row.Back, stats: TestUnits.MakeStats(atk: 50, hp: 500, spd: 110), preference: Preference.Rearguard, formationChance: 5000),
                    TestUnits.Ally(3, Row.Front, stats: TestUnits.MakeStats(atk: 55, hp: 700, spd: 120), preference: Preference.Charger, formationChance: 5000),
                };
                UnitSetup[][] waves =
                {
                    new[]
                    {
                        TestUnits.Enemy(101, stats: TestUnits.MakeStats(atk: 40, hp: 400, spd: 90), preference: Preference.Charger, formationChance: 5000),
                        TestUnits.Enemy(102, stats: TestUnits.MakeStats(atk: 40, hp: 400, spd: 95)),
                    },
                    new[] { TestUnits.Enemy(201, stats: TestUnits.MakeStats(atk: 60, hp: 900, spd: 80), isBoss: true) },
                };
                var sim = Create(allies, waves, RealisticLane(3500), new SeededRandom(seed), new[] { 800, 800 });
                var buffer = new List<BattleEvent>();
                while (sim.Outcome == BattleOutcome.InProgress && sim.Tick < 1500)
                {
                    for (int i = 0; i < ticksPerFrame && sim.Outcome == BattleOutcome.InProgress; i++) sim.Step(buffer);
                }

                return sim.BuildResult().StateHash;
            }

            Assert.AreEqual(Run(42u, 1), Run(42u, 1));
            Assert.AreEqual(Run(42u, 1), Run(42u, 4), "한 틱씩과 묶음 실행의 결과가 같아야 한다.");
        }
    }
}
