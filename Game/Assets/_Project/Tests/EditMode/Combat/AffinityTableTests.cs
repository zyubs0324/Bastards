using System;
using NUnit.Framework;

namespace TAoB.Combat.Tests
{
    public class AffinityTableTests
    {
        [Test]
        public void UnlistedPair_IsNormal()
        {
            var table = new AffinityTable(new AffinityEntry[0]);

            Assert.AreEqual(AffinityLevel.Normal, table.Get(Job.Knight, Job.Mage));
        }

        [Test]
        public void ListedPair_ReturnsItsLevel_AndIsDirectional()
        {
            var table = new AffinityTable(new[]
            {
                new AffinityEntry(Job.Perceptive, Job.Mage, AffinityLevel.Strong),
            });

            Assert.AreEqual(AffinityLevel.Strong, table.Get(Job.Perceptive, Job.Mage));
            Assert.AreEqual(AffinityLevel.Normal, table.Get(Job.Mage, Job.Perceptive));
        }

        [Test]
        public void DuplicatePair_Throws()
        {
            var entries = new[]
            {
                new AffinityEntry(Job.Knight, Job.Mage, AffinityLevel.Weak),
                new AffinityEntry(Job.Knight, Job.Mage, AffinityLevel.Strong),
            };

            Assert.Throws<ArgumentException>(() => new AffinityTable(entries));
        }
    }
}
