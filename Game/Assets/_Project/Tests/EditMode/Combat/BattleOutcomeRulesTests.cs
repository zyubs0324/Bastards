using NUnit.Framework;

namespace TAoB.Combat.Tests
{
    public class BattleOutcomeRulesTests
    {
        private static BattleOutcome Evaluate(
            bool retreat = false, bool precondition = false, bool victory = false, bool wipe = false, bool timeLimit = false)
        {
            return BattleOutcomeRules.Evaluate(new OutcomeInputs(retreat, precondition, victory, wipe, timeLimit));
        }

        [Test]
        public void NothingHappened_InProgress()
        {
            Assert.AreEqual(BattleOutcome.InProgress, Evaluate());
        }

        [Test]
        public void EachConditionAlone_MapsToItsOutcome()
        {
            Assert.AreEqual(BattleOutcome.DefeatRetreat, Evaluate(retreat: true));
            Assert.AreEqual(BattleOutcome.DefeatPrecondition, Evaluate(precondition: true));
            Assert.AreEqual(BattleOutcome.Victory, Evaluate(victory: true));
            Assert.AreEqual(BattleOutcome.DefeatWipe, Evaluate(wipe: true));
            Assert.AreEqual(BattleOutcome.DefeatTimeLimit, Evaluate(timeLimit: true));
        }

        [Test]
        public void Retreat_OutranksEverything()
        {
            Assert.AreEqual(BattleOutcome.DefeatRetreat,
                Evaluate(retreat: true, precondition: true, victory: true, wipe: true, timeLimit: true));
        }

        [Test]
        public void Precondition_OutranksVictory()
        {
            Assert.AreEqual(BattleOutcome.DefeatPrecondition, Evaluate(precondition: true, victory: true));
        }

        [Test]
        public void Victory_OutranksWipeAndTimeLimit()
        {
            Assert.AreEqual(BattleOutcome.Victory, Evaluate(victory: true, wipe: true, timeLimit: true));
        }

        [Test]
        public void Wipe_OutranksTimeLimit()
        {
            Assert.AreEqual(BattleOutcome.DefeatWipe, Evaluate(wipe: true, timeLimit: true));
        }
    }
}
