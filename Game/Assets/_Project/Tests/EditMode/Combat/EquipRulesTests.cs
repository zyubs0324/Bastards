using NUnit.Framework;

namespace TAoB.Combat.Tests
{
    public class EquipRulesTests
    {
        private BalanceConfig _balance;
        private Magitool _tool;

        [SetUp]
        public void SetUp()
        {
            _balance = TestBalance.Create(
                resSelfDamagePerShortfall: 2,
                equipBlockResShortfall: 20,
                mndPenaltyPerShortfallPermille: 25,
                minMndEffectPermille: 200);
            _tool = new Magitool(atkBonus: 100, requiredRes: 50, requiredMnd: 40, maxDurability: 10, durabilityCostPerUse: 3);
        }

        [Test]
        public void ResEqualToRequirement_NoSelfDamage_CanEquip()
        {
            Assert.AreEqual(0, EquipRules.SelfDamagePerUse(50, _tool, _balance));
            Assert.IsTrue(EquipRules.CanEquip(50, _tool, _balance));
        }

        [Test]
        public void ResOneShort_SelfDamageProportionalToShortfall()
        {
            Assert.AreEqual(2, EquipRules.SelfDamagePerUse(49, _tool, _balance));
            Assert.IsTrue(EquipRules.CanEquip(49, _tool, _balance));
        }

        [Test]
        public void ResShortfallJustBelowBlockThreshold_CanEquip()
        {
            Assert.IsTrue(EquipRules.CanEquip(50 - 19, _tool, _balance));
        }

        [Test]
        public void ResShortfallAtBlockThreshold_CannotEquip()
        {
            Assert.IsFalse(EquipRules.CanEquip(50 - 20, _tool, _balance));
        }

        [Test]
        public void MndEqualToRequirement_FullEffect()
        {
            Assert.AreEqual(1000, EquipRules.MndEffectPermille(40, _tool, _balance));
        }

        [Test]
        public void MndShort_EffectReducedPerShortfall()
        {
            // 10 부족 × 25‰ = 250‰ 감소
            Assert.AreEqual(750, EquipRules.MndEffectPermille(30, _tool, _balance));
        }

        [Test]
        public void MndFarShort_EffectFlooredAtMinimum()
        {
            Assert.AreEqual(200, EquipRules.MndEffectPermille(0, _tool, _balance));
        }

        [Test]
        public void Use_AppliesMndEffectToBonus_AndConsumesDurability()
        {
            MagitoolUse use = EquipRules.Use(res: 49, mnd: 30, durability: 10, _tool, _balance);

            Assert.AreEqual(75, use.AtkBonus);
            Assert.AreEqual(2, use.SelfDamage);
            Assert.AreEqual(7, use.DurabilityAfter);
        }

        [Test]
        public void Use_DurabilityNeverGoesBelowZero()
        {
            MagitoolUse use = EquipRules.Use(res: 50, mnd: 40, durability: 2, _tool, _balance);

            Assert.AreEqual(100, use.AtkBonus);
            Assert.AreEqual(0, use.DurabilityAfter);
        }

        [Test]
        public void SelfDamage_WithHugeShortfall_ClampsInsteadOfOverflowing()
        {
            var balance = TestBalance.Create(resSelfDamagePerShortfall: CombatScale.MaxValue);
            var heavy = new Magitool(atkBonus: 0, requiredRes: CombatScale.MaxValue, requiredMnd: 0, maxDurability: 1, durabilityCostPerUse: 1);

            Assert.AreEqual(int.MaxValue, EquipRules.SelfDamagePerUse(0, heavy, balance));
        }

        [Test]
        public void Use_AtZeroDurability_LosesEffectAndSelfDamage()
        {
            MagitoolUse use = EquipRules.Use(res: 0, mnd: 40, durability: 0, _tool, _balance);

            Assert.AreEqual(0, use.AtkBonus);
            Assert.AreEqual(0, use.SelfDamage);
            Assert.AreEqual(0, use.DurabilityAfter);
        }
    }
}
