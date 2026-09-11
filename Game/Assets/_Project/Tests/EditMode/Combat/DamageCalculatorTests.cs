using NUnit.Framework;

namespace TAoB.Combat.Tests
{
    public class DamageCalculatorTests
    {
        private BalanceConfig _balance;

        [SetUp]
        public void SetUp()
        {
            _balance = TestBalance.Create(
                defenseK: 100,
                weakAffinityPermille: 750,
                normalAffinityPermille: 1000,
                strongAffinityPermille: 1250,
                frontDamageTakenPermille: 1000,
                backDamageTakenPermille: 800,
                critBaseChance: 500,
                critChancePerSpd: 20,
                critMultiplierPermille: 1500);
        }

        [TestCase(0, 0)]
        [TestCase(100, 500)]
        [TestCase(300, 750)]
        public void DefenseReduction_IsDefOverDefPlusK(int def, int expectedPermille)
        {
            Assert.AreEqual(expectedPermille, DamageCalculator.DefenseReductionPermille(def, _balance));
        }

        [Test]
        public void Compute_NormalFrontNoDefenseNoCrit_ReturnsAttackPower()
        {
            var input = new DamageInput(200, 1000, AffinityLevel.Normal, Row.Front, 0, false);

            Assert.AreEqual(200, DamageCalculator.Compute(input, _balance));
        }

        [Test]
        public void Compute_CombinesSkillAffinityFormationDefenseAndCrit()
        {
            // 200 ×1.5(기술) =300 ×1.25(강함) =375 ×0.8(뒷열) =300 ×(1−0.5)(DEF 100) =150 ×1.5(크리티컬) =225
            var input = new DamageInput(200, 1500, AffinityLevel.Strong, Row.Back, 100, true);

            Assert.AreEqual(225, DamageCalculator.Compute(input, _balance));
        }

        [Test]
        public void Compute_TruncatesAfterEachMultiplication()
        {
            // 7 ×0.75 =5.25→5, ×0.8 =4
            var input = new DamageInput(7, 1000, AffinityLevel.Weak, Row.Back, 0, false);

            Assert.AreEqual(4, DamageCalculator.Compute(input, _balance));
        }

        [Test]
        public void Compute_NeverReturnsLessThanMinimumDamage()
        {
            var input = new DamageInput(1, 1000, AffinityLevel.Weak, Row.Back, 100000, false);

            Assert.AreEqual(1, DamageCalculator.Compute(input, _balance));
        }

        [Test]
        public void Compute_WithZeroMinimumDamage_CanReturnZero()
        {
            var balance = TestBalance.Create(minimumDamage: 0);
            var input = new DamageInput(1, 1000, AffinityLevel.Weak, Row.Back, 100000, false);

            Assert.AreEqual(0, DamageCalculator.Compute(input, balance));
        }

        [Test]
        public void Compute_ExtremeValues_StayWithinIntRange()
        {
            var balance = TestBalance.Create(
                strongAffinityPermille: CombatScale.MaxPermille,
                frontDamageTakenPermille: CombatScale.MaxPermille,
                critMultiplierPermille: CombatScale.MaxPermille);
            var input = new DamageInput(int.MaxValue, int.MaxValue, AffinityLevel.Strong, Row.Front, 0, true);

            Assert.AreEqual(int.MaxValue, DamageCalculator.Compute(input, balance));
        }

        [TestCase(AffinityLevel.Weak, 750)]
        [TestCase(AffinityLevel.Normal, 1000)]
        [TestCase(AffinityLevel.Strong, 1250)]
        public void AffinityPermille_ReadsBalanceConfig(AffinityLevel level, int expected)
        {
            Assert.AreEqual(expected, DamageCalculator.AffinityPermille(level, _balance));
        }

        [TestCase(Row.Front, 1000)]
        [TestCase(Row.Back, 800)]
        public void FormationDamageTaken_ReadsBalanceConfig(Row row, int expected)
        {
            Assert.AreEqual(expected, Formation.DamageTakenPermille(row, _balance));
        }

        [TestCase(0, 500)]
        [TestCase(100, 2500)]
        [TestCase(1000, 10000)]
        public void CritChance_GrowsWithSpdAndCapsAt10000(int spd, int expectedChance)
        {
            Assert.AreEqual(expectedChance, DamageCalculator.CritChance(spd, _balance));
        }
    }
}
