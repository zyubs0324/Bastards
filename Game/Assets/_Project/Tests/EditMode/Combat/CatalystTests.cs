using NUnit.Framework;

namespace TAoB.Combat.Tests
{
    public class CatalystTests
    {
        [Test]
        public void Output_IsCutAtMnd_WhenPowerExceedsMnd()
        {
            var catalyst = new Catalyst(power: 120);

            Assert.AreEqual(80, catalyst.OutputFor(mnd: 80));
        }

        [Test]
        public void Output_IsFullPower_WhenMndIsHigher()
        {
            var catalyst = new Catalyst(power: 120);

            Assert.AreEqual(120, catalyst.OutputFor(mnd: 200));
        }

        [Test]
        public void OverloadOutput_UsesOverloadPowerPermille()
        {
            var balance = TestBalance.Create(overloadPowerPermille: 1500);
            var catalyst = new Catalyst(power: 100);

            Assert.AreEqual(150, catalyst.OverloadOutput(balance));
        }

        [TestCase(100, 500)]
        [TestCase(150, 0)]
        [TestCase(200, 0)]
        public void OverloadExcessPermille_IsOutputAboveMnd(int mnd, int expectedExcess)
        {
            var balance = TestBalance.Create(overloadPowerPermille: 1500);
            var catalyst = new Catalyst(power: 100);

            Assert.AreEqual(expectedExcess, catalyst.OverloadExcessPermille(mnd, balance));
        }
    }
}
