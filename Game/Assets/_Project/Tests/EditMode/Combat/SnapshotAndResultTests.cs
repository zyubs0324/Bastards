using System;
using System.Linq;
using NUnit.Framework;

namespace TAoB.Combat.Tests
{
    public class SnapshotAndResultTests
    {
        private static BattleSimulation CreateTwoWaveBattle()
        {
            var waves = new[]
            {
                new[] { TestUnits.Enemy(101) },
                new[] { TestUnits.Enemy(201, isBoss: true) },
            };
            return new BattleSimulation(TestUnits.Setup(new[] { TestUnits.Ally(1) }, waves), new FixedRandom());
        }

        [Test]
        public void SnapshotBuffer_ContainsAllUnits_WithFutureWavesOffField()
        {
            var sim = CreateTwoWaveBattle();
            BattleSnapshot snapshot = sim.CreateSnapshotBuffer();

            sim.FillSnapshot(snapshot);

            Assert.AreEqual(3, snapshot.Units.Count);
            Assert.IsTrue(snapshot.Units.Single(u => u.Id == 101).IsOnField);
            Assert.IsFalse(snapshot.Units.Single(u => u.Id == 201).IsOnField);
            Assert.IsTrue(snapshot.Units.Single(u => u.Id == 201).IsBoss);
            Assert.IsFalse(snapshot.Units.Single(u => u.Id == 101).IsBoss);
            Assert.AreEqual(0, snapshot.CurrentWave);
            Assert.AreEqual(2, snapshot.WaveCount);
        }

        [Test]
        public void FillSnapshot_CopiesValues_LaterStepsDoNotChangeFilledSnapshot()
        {
            var sim = CreateTwoWaveBattle();
            BattleSnapshot snapshot = sim.CreateSnapshotBuffer();
            SimRunner.Run(sim, 5);
            sim.FillSnapshot(snapshot);

            SimRunner.Run(sim, 5);

            Assert.AreEqual(5, snapshot.Tick);
            Assert.AreEqual(500, snapshot.Units.Single(u => u.Id == 1).ActionGauge);
            sim.FillSnapshot(snapshot);
            Assert.AreEqual(10, snapshot.Tick);
            Assert.AreEqual(0, snapshot.Units.Single(u => u.Id == 1).ActionGauge);
            Assert.AreEqual(TestUnits.Unkillable - 50, snapshot.Units.Single(u => u.Id == 101).Hp);
        }

        [Test]
        public void FillSnapshot_WithForeignBuffer_Throws()
        {
            var sim = CreateTwoWaveBattle();
            var otherSim = new BattleSimulation(
                TestUnits.Setup(new[] { TestUnits.Ally(1), TestUnits.Ally(2) }, TestUnits.OneWave(TestUnits.Enemy(101))),
                new FixedRandom());

            Assert.Throws<ArgumentException>(() => sim.FillSnapshot(otherSim.CreateSnapshotBuffer()));
        }

        [Test]
        public void OverloadRisk_ReflectsExcessTier_AndIsZeroWithoutCatalyst()
        {
            // 원석 200 × 1.5 = 300, MND 100 → 초과 2000‰ → 셋째 구간(위험 단계 2)
            var mage = TestUnits.Ally(1, Row.Back, Job.Mage, skill: TestUnits.Skill(canOverload: true), catalyst: new Catalyst(200));
            var sim = new BattleSimulation(
                TestUnits.Setup(new[] { mage, TestUnits.Ally(2) }, TestUnits.OneWave(TestUnits.Enemy(101))),
                new FixedRandom());

            Assert.AreEqual(2, SimRunner.Unit(sim, 1).OverloadRisk);
            Assert.AreEqual(0, SimRunner.Unit(sim, 2).OverloadRisk);
        }

        [Test]
        public void StateHash_DiffersWhenOnlyOneValueDiffers()
        {
            ulong HashFor(int enemyHp)
            {
                var enemy = TestUnits.Enemy(101, stats: TestUnits.MakeStats(hp: enemyHp, spd: TestUnits.NeverActs));
                var sim = new BattleSimulation(TestUnits.Setup(new[] { TestUnits.Ally(1) }, TestUnits.OneWave(enemy)), new FixedRandom());
                SimRunner.Run(sim, 10);
                return sim.BuildResult().StateHash;
            }

            Assert.AreEqual(HashFor(1000), HashFor(1000));
            Assert.AreNotEqual(HashFor(1000), HashFor(1001));
        }
    }
}
