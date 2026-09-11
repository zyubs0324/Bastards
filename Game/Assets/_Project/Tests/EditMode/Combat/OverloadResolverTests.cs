using NUnit.Framework;

namespace TAoB.Combat.Tests
{
    public class OverloadResolverTests
    {
        private BalanceConfig _balance;

        [SetUp]
        public void SetUp()
        {
            // 첫 구간(초과 500‰ 이하): 성공 4000 · 흩어짐 3000 · 폭주 2000 · 한계돌파 1000
            _balance = TestBalance.Create();
        }

        [TestCase(0, OverloadOutcome.Success)]
        [TestCase(3999, OverloadOutcome.Success)]
        [TestCase(4000, OverloadOutcome.Scatter)]
        [TestCase(6999, OverloadOutcome.Scatter)]
        [TestCase(7000, OverloadOutcome.Runaway)]
        [TestCase(8999, OverloadOutcome.Runaway)]
        [TestCase(9000, OverloadOutcome.Breakthrough)]
        [TestCase(9999, OverloadOutcome.Breakthrough)]
        public void Resolve_FixedRoll_SelectsBranchByCumulativeChance(int roll, OverloadOutcome expected)
        {
            var random = new FixedRandom(roll);

            OverloadOutcome outcome = OverloadResolver.Resolve(excessPermille: 100, _balance, random);

            Assert.AreEqual(expected, outcome);
            Assert.AreEqual(0, random.Remaining);
        }

        [TestCase(0, 0)]
        [TestCase(500, 0)]
        [TestCase(501, 1)]
        [TestCase(1000, 1)]
        [TestCase(2001, 3)]
        public void RiskLevel_FollowsTierBoundaries(int excessPermille, int expectedLevel)
        {
            Assert.AreEqual(expectedLevel, OverloadResolver.RiskLevel(excessPermille, _balance));
        }

        [Test]
        public void RiskLevel_IsCappedAtThree_WhenMoreTiersExist()
        {
            var balance = TestBalance.Create(overloadTiers: new[]
            {
                new OverloadTier(100, 10000, 0, 0, 0),
                new OverloadTier(200, 10000, 0, 0, 0),
                new OverloadTier(300, 10000, 0, 0, 0),
                new OverloadTier(400, 10000, 0, 0, 0),
                new OverloadTier(int.MaxValue, 10000, 0, 0, 0),
            });

            Assert.AreEqual(3, OverloadResolver.RiskLevel(5000, balance));
        }

        [Test]
        public void Resolve_UsesTierMatchingExcess()
        {
            // 초과 2500‰ → 마지막 구간: 성공 1000 · 흩어짐 4500 · 폭주 4400 · 한계돌파 100
            var random = new FixedRandom(1000);

            Assert.AreEqual(OverloadOutcome.Scatter, OverloadResolver.Resolve(2500, _balance, random));
        }
    }
}
