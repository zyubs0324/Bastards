using System;
using NUnit.Framework;

namespace TAoB.Combat.Tests
{
    public class SeededRandomTests
    {
        [Test]
        public void SameSeed_ProducesSameSequence()
        {
            var a = new SeededRandom(12345u);
            var b = new SeededRandom(12345u);

            for (int i = 0; i < 100; i++)
            {
                Assert.AreEqual(a.Next(10000), b.Next(10000), $"{i}번째 값");
            }
        }

        [Test]
        public void DifferentSeeds_ProduceDifferentSequences()
        {
            var a = new SeededRandom(1u);
            var b = new SeededRandom(2u);

            bool anyDifferent = false;
            for (int i = 0; i < 20; i++)
            {
                anyDifferent |= a.Next(10000) != b.Next(10000);
            }

            Assert.IsTrue(anyDifferent);
        }

        [Test]
        public void KnownSequence_ForSeed1_IsFixedAcrossPlatforms()
        {
            // xorshift32(13, 17, 5) 의 seed 1 첫 두 값: 270369, 67634689
            var random = new SeededRandom(1u);

            Assert.AreEqual(369, random.Next(1000));
            Assert.AreEqual(689, random.Next(1000));
        }

        [Test]
        public void Next_StaysWithinRange()
        {
            var random = new SeededRandom(777u);

            for (int i = 0; i < 1000; i++)
            {
                int value = random.Next(7);
                Assert.That(value, Is.InRange(0, 6));
            }
        }

        [Test]
        public void ZeroSeed_StillProducesChangingValues()
        {
            var random = new SeededRandom(0u);

            int first = random.Next(10000);
            bool changed = false;
            for (int i = 0; i < 10; i++)
            {
                changed |= random.Next(10000) != first;
            }

            Assert.IsTrue(changed);
        }

        [Test]
        public void Next_ThrowsWhenMaxIsNotPositive()
        {
            var random = new SeededRandom(1u);

            Assert.Throws<ArgumentOutOfRangeException>(() => random.Next(0));
        }

        [Test]
        public void State_ChangesAfterEachDraw()
        {
            var random = new SeededRandom(99u);
            uint before = random.State;

            random.Next(10);

            Assert.AreNotEqual(before, random.State);
        }
    }
}
