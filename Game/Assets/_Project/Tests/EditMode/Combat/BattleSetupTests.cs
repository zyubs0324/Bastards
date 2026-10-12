using System;
using NUnit.Framework;

namespace TAoB.Combat.Tests
{
    public class BattleSetupTests
    {
        private static BattleSetup WithApproach(int[] approach)
        {
            return TestUnits.Setup(
                new[] { TestUnits.Ally(1) }, TestUnits.OneWave(TestUnits.Enemy(101)), waveApproachFu: approach);
        }

        [Test]
        public void ApproachCountMustMatchTheWaveCount()
        {
            var ex = Assert.Throws<ArgumentException>(() => WithApproach(new[] { 100, 200 }));

            Assert.AreEqual("waveApproachFu", ex.ParamName);
        }

        [Test]
        public void ApproachMissing_Throws()
        {
            var ex = Assert.Throws<ArgumentNullException>(() => new BattleSetup(
                TestUnits.Balance(),
                new AffinityTable(new AffinityEntry[0]),
                new[] { TestUnits.Ally(1) },
                TestUnits.OneWave(TestUnits.Enemy(101)),
                null,
                BattleSetup.MaxTimeLimitTicks,
                PreconditionKind.None,
                new ConsumableSetup[0]));

            Assert.AreEqual("waveApproachFu", ex.ParamName);
        }

        [Test]
        public void ApproachBelowOne_Throws()
        {
            // 0 이면 적과 같은 자리에서 시작해 "등장 → 행군 → 조우"의 행군 구간이 사라진다.
            Assert.Throws<ArgumentException>(() => WithApproach(new[] { 0 }));
        }

        [Test]
        public void ApproachAtItsMaximum_IsAccepted()
        {
            BattleSetup setup = WithApproach(new[] { BattleSetup.MaxApproachFu });

            Assert.AreEqual(BattleSetup.MaxApproachFu, setup.WaveApproachFu[0]);
        }

        [Test]
        public void ApproachAboveItsMaximum_Throws()
        {
            Assert.Throws<ArgumentException>(() => WithApproach(new[] { BattleSetup.MaxApproachFu + 1 }));
        }

        [Test]
        public void ApproachSumAboveTheFieldSpan_Throws()
        {
            // 좌표가 int 범위에 남도록 전장 전체 길이에 상한을 둔다.
            int waveCount = BattleSetup.MaxFieldSpanFu / BattleSetup.MaxApproachFu + 1;
            var waves = new UnitSetup[waveCount][];
            var approach = new int[waveCount];
            for (int i = 0; i < waveCount; i++)
            {
                waves[i] = new[] { TestUnits.Enemy(1000 + i) };
                approach[i] = BattleSetup.MaxApproachFu;
            }

            var ex = Assert.Throws<ArgumentException>(() =>
                TestUnits.Setup(new[] { TestUnits.Ally(1) }, waves, waveApproachFu: approach));

            Assert.AreEqual("waveApproachFu", ex.ParamName);
        }

        [Test]
        public void DuplicateUnitIds_Throw()
        {
            Assert.Throws<ArgumentException>(() =>
                TestUnits.Setup(new[] { TestUnits.Ally(1) }, TestUnits.OneWave(TestUnits.Enemy(1))));
        }

        [Test]
        public void EnemyInAllyList_Throws()
        {
            Assert.Throws<ArgumentException>(() =>
                TestUnits.Setup(new[] { TestUnits.Enemy(1) }, TestUnits.OneWave(TestUnits.Enemy(101))));
        }

        [Test]
        public void AllyInWave_Throws()
        {
            Assert.Throws<ArgumentException>(() =>
                TestUnits.Setup(new[] { TestUnits.Ally(1) }, TestUnits.OneWave(TestUnits.Ally(101))));
        }

        [Test]
        public void NoWaves_Throws()
        {
            Assert.Throws<ArgumentException>(() =>
                TestUnits.Setup(new[] { TestUnits.Ally(1) }, new UnitSetup[0][]));
        }

        [Test]
        public void EmptyWave_Throws()
        {
            Assert.Throws<ArgumentException>(() =>
                TestUnits.Setup(new[] { TestUnits.Ally(1) }, new[] { new UnitSetup[0] }));
        }

        [TestCase(0)]
        [TestCase(-5)]
        public void NonPositiveTimeLimit_Throws(int ticks)
        {
            Assert.Throws<ArgumentException>(() =>
                TestUnits.Setup(new[] { TestUnits.Ally(1) }, TestUnits.OneWave(TestUnits.Enemy(101)), timeLimitTicks: ticks));
        }

        [Test]
        public void TimeLimitAboveMaximum_Throws()
        {
            Assert.Throws<ArgumentException>(() =>
                TestUnits.Setup(new[] { TestUnits.Ally(1) }, TestUnits.OneWave(TestUnits.Enemy(101)),
                    timeLimitTicks: BattleSetup.MaxTimeLimitTicks + 1));
        }

        [Test]
        public void StatAboveMaxValue_Throws()
        {
            Assert.Throws<ArgumentException>(() => TestUnits.MakeStats(atk: CombatScale.MaxValue + 1));
        }

        [Test]
        public void MagitoolBeyondResBlockThreshold_Throws()
        {
            // 요구 RES 120, 아군 RES 100 → 부족 20 = 장착 불가 임계
            var tool = new Magitool(atkBonus: 10, requiredRes: 120, requiredMnd: 0, maxDurability: 10, durabilityCostPerUse: 1);

            Assert.Throws<ArgumentException>(() =>
                TestUnits.Setup(new[] { TestUnits.Ally(1, magitool: tool) }, TestUnits.OneWave(TestUnits.Enemy(101))));
        }

        [Test]
        public void TicksPerSecond_IsTen()
        {
            Assert.AreEqual(10, BattleSimulation.TicksPerSecond);
        }
    }
}
