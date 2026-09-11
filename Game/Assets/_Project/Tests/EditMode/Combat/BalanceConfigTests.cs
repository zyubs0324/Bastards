using System;
using NUnit.Framework;

namespace TAoB.Combat.Tests
{
    public class BalanceConfigTests
    {
        [Test]
        public void ValidValues_AreStored()
        {
            BalanceConfig balance = TestBalance.Create(defenseK: 123, actionGaugeMax: 900, minimumDamage: 3);

            Assert.AreEqual(123, balance.DefenseK);
            Assert.AreEqual(900, balance.ActionGaugeMax);
            Assert.AreEqual(3, balance.MinimumDamage);
            Assert.AreEqual(4, balance.OverloadTiers.Count);
        }

        [Test]
        public void TierChancesNotSummingTo10000_Throws()
        {
            var tiers = new[] { new OverloadTier(int.MaxValue, 5000, 3000, 1000, 999) };

            Assert.Throws<ArgumentException>(() => TestBalance.Create(overloadTiers: tiers));
        }

        [Test]
        public void TiersNotAscending_Throws()
        {
            var tiers = new[]
            {
                new OverloadTier(1000, 10000, 0, 0, 0),
                new OverloadTier(500, 10000, 0, 0, 0),
                new OverloadTier(int.MaxValue, 10000, 0, 0, 0),
            };

            Assert.Throws<ArgumentException>(() => TestBalance.Create(overloadTiers: tiers));
        }

        [Test]
        public void LastTierNotOpenEnded_Throws()
        {
            var tiers = new[] { new OverloadTier(1000, 10000, 0, 0, 0) };

            Assert.Throws<ArgumentException>(() => TestBalance.Create(overloadTiers: tiers));
        }

        [Test]
        public void EmptyTiers_Throws()
        {
            Assert.Throws<ArgumentException>(() => TestBalance.Create(overloadTiers: new OverloadTier[0]));
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void NonPositiveDefenseK_Throws(int defenseK)
        {
            Assert.Throws<ArgumentException>(() => TestBalance.Create(defenseK: defenseK));
        }

        [Test]
        public void NonPositiveActionGaugeMax_Throws()
        {
            Assert.Throws<ArgumentException>(() => TestBalance.Create(actionGaugeMax: 0));
        }

        [Test]
        public void MultiplierAboveMaxPermille_Throws()
        {
            Assert.Throws<ArgumentException>(() => TestBalance.Create(strongAffinityPermille: CombatScale.MaxPermille + 1));
        }

        [Test]
        public void ValueAboveMaxValue_Throws()
        {
            Assert.Throws<ArgumentException>(() => TestBalance.Create(resSelfDamagePerShortfall: CombatScale.MaxValue + 1));
        }

        [Test]
        public void NegativeMinimumDamage_Throws()
        {
            Assert.Throws<ArgumentException>(() => TestBalance.Create(minimumDamage: -1));
        }

        [Test]
        public void NegativeMultiplier_Throws()
        {
            Assert.Throws<ArgumentException>(() => TestBalance.Create(backDamageTakenPermille: -1));
        }

        [Test]
        public void ChanceAbove10000_Throws()
        {
            Assert.Throws<ArgumentException>(() => TestBalance.Create(critBaseChance: 10001));
        }
    }
}
