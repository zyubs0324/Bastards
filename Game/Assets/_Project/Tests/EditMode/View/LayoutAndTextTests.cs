using NUnit.Framework;
using TAoB.Combat;
using UnityEngine;

namespace TAoB.View.Tests
{
    public class LayoutAndTextTests
    {
        [Test]
        public void Allies_Left_Enemies_Right_FrontNearerCenter()
        {
            Vector2 allyFront = BattleLayout.UnitPosition(Side.Ally, Row.Front, 0, 1);
            Vector2 allyBack = BattleLayout.UnitPosition(Side.Ally, Row.Back, 0, 1);
            Vector2 enemyFront = BattleLayout.UnitPosition(Side.Enemy, Row.Front, 0, 1);

            Assert.Less(allyFront.x, 0f);
            Assert.Greater(enemyFront.x, 0f);
            Assert.Less(Mathf.Abs(allyFront.x), Mathf.Abs(allyBack.x));
            Assert.AreEqual(-enemyFront.x, allyFront.x, 0.001f, "좌우 대칭");
        }

        [Test]
        public void Slots_AreCentredVertically_TopToBottom()
        {
            Vector2 single = BattleLayout.UnitPosition(Side.Ally, Row.Front, 0, 1);
            Vector2 first = BattleLayout.UnitPosition(Side.Ally, Row.Front, 0, 2);
            Vector2 second = BattleLayout.UnitPosition(Side.Ally, Row.Front, 1, 2);

            Assert.Greater(first.y, second.y, "0번 자리가 위");
            Assert.AreEqual(single.y, (first.y + second.y) * 0.5f, 0.001f, "두 자리의 가운데 = 한 자리 위치");
        }

        [Test]
        public void EveryOutcome_HasDistinctText()
        {
            var seen = new System.Collections.Generic.HashSet<string>();
            foreach (BattleOutcome outcome in System.Enum.GetValues(typeof(BattleOutcome)))
            {
                Assert.IsTrue(seen.Add(UiText.Outcome(outcome)), outcome.ToString());
            }
        }

        [Test]
        public void EveryCommandResult_HasText()
        {
            foreach (CommandResult result in System.Enum.GetValues(typeof(CommandResult)))
            {
                Assert.AreNotEqual("거부", UiText.CommandResult(result), result.ToString());
            }
        }

        [Test]
        public void JobGlyph_IsOneCharacter()
        {
            foreach (Job job in System.Enum.GetValues(typeof(Job)))
            {
                Assert.AreEqual(1, UiText.JobGlyph(job).Length, job.ToString());
            }
        }

        [Test]
        public void EveryOverloadOutcome_HasDistinctText()
        {
            var seen = new System.Collections.Generic.HashSet<string>();
            foreach (OverloadOutcome outcome in System.Enum.GetValues(typeof(OverloadOutcome)))
            {
                Assert.IsTrue(seen.Add(UiText.OverloadOutcome(outcome)), outcome.ToString());
            }
        }

        [Test]
        public void Condition_NormalIsEmpty_OthersHaveText()
        {
            Assert.AreEqual("", UiText.Condition(UnitCondition.Normal), "정상 상태는 글자를 띄우지 않는다");
            Assert.AreNotEqual("", UiText.Condition(UnitCondition.Scattered));
            Assert.AreNotEqual("", UiText.Condition(UnitCondition.Downed));
            Assert.AreNotEqual(UiText.Condition(UnitCondition.Scattered), UiText.Condition(UnitCondition.Downed));
        }

        [Test]
        public void WaveBanner_CountsFromOne()
        {
            Assert.AreEqual("웨이브 1 / 3", UiText.WaveBanner(0, 3));
            Assert.AreEqual("웨이브 3 / 3", UiText.WaveBanner(2, 3));
        }

        [Test]
        public void JobColor_EnemyIsDarkerThanAlly_AndJobsDiffer()
        {
            foreach (Job job in System.Enum.GetValues(typeof(Job)))
            {
                Color ally = BattleLayout.JobColor(job, Side.Ally);
                Color enemy = BattleLayout.JobColor(job, Side.Enemy);
                Assert.Less(enemy.grayscale, ally.grayscale, job.ToString());
            }

            Assert.AreNotEqual(BattleLayout.JobColor(Job.Knight, Side.Ally), BattleLayout.JobColor(Job.Mage, Side.Ally));
            Assert.AreNotEqual(BattleLayout.JobColor(Job.Mage, Side.Ally), BattleLayout.JobColor(Job.Perceptive, Side.Ally));
        }
    }
}
