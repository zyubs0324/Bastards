using NUnit.Framework;

namespace TAoB.Combat.Tests
{
    /// <summary>확률 판정의 경계와 "확정이면 난수를 쓰지 않는다"를 고정한다(계획서 D8).</summary>
    public class ChanceTests
    {
        [Test]
        public void ZeroChance_IsFalse_AndConsumesNoRandom()
        {
            // 준비된 값이 없는 난수라 한 번이라도 꺼내면 예외가 난다.
            var random = new FixedRandom();

            Assert.IsFalse(Chance.Roll(random, 0));
            Assert.IsFalse(Chance.Roll(random, -1));
            Assert.AreEqual(0, random.Remaining);
        }

        [Test]
        public void FullChance_IsTrue_AndConsumesNoRandom()
        {
            var random = new FixedRandom();

            Assert.IsTrue(Chance.Roll(random, CombatScale.ChanceRange));
            Assert.IsTrue(Chance.Roll(random, CombatScale.ChanceRange + 1));
            Assert.AreEqual(0, random.Remaining);
        }

        [Test]
        public void RolledValueBelowChance_Succeeds()
        {
            Assert.IsTrue(Chance.Roll(new FixedRandom(2499), 2500));
        }

        [Test]
        public void RolledValueAtChance_Fails()
        {
            // 경계는 "미만"이다. 2500 이 성공이면 만분율 2500 이 2501 처럼 동작한다.
            Assert.IsFalse(Chance.Roll(new FixedRandom(2500), 2500));
        }
    }
}
