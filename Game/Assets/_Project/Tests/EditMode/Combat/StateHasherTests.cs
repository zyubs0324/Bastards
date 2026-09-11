using NUnit.Framework;

namespace TAoB.Combat.Tests
{
    public class StateHasherTests
    {
        private const ulong FnvOffsetBasis = 14695981039346656037UL;

        [Test]
        public void NewHasher_StartsAtFnvOffsetBasis()
        {
            var hasher = new StateHasher();

            Assert.AreEqual(FnvOffsetBasis, hasher.Value);
        }

        [Test]
        public void SameInputs_ProduceSameHash()
        {
            var a = new StateHasher();
            var b = new StateHasher();

            foreach (int value in new[] { 3, -7, 1000, int.MaxValue })
            {
                a.Add(value);
                b.Add(value);
            }

            Assert.AreEqual(a.Value, b.Value);
        }

        [Test]
        public void OneFieldDifferent_ProducesDifferentHash()
        {
            var a = new StateHasher();
            var b = new StateHasher();

            a.Add(10); a.Add(20); a.Add(30);
            b.Add(10); b.Add(21); b.Add(30);

            Assert.AreNotEqual(a.Value, b.Value);
        }

        [Test]
        public void InputOrder_ChangesHash()
        {
            var a = new StateHasher();
            var b = new StateHasher();

            a.Add(1); a.Add(2);
            b.Add(2); b.Add(1);

            Assert.AreNotEqual(a.Value, b.Value);
        }

        [Test]
        public void AddingZero_ChangesHash()
        {
            var hasher = new StateHasher();

            hasher.Add(0);

            Assert.AreNotEqual(FnvOffsetBasis, hasher.Value);
        }

        [Test]
        public void Reset_ReturnsToOffsetBasis()
        {
            var hasher = new StateHasher();
            hasher.Add(42);

            hasher.Reset();

            Assert.AreEqual(FnvOffsetBasis, hasher.Value);
        }
    }
}
