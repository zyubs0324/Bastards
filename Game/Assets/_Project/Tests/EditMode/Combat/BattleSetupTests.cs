using System;
using NUnit.Framework;

namespace TAoB.Combat.Tests
{
    public class BattleSetupTests
    {
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
