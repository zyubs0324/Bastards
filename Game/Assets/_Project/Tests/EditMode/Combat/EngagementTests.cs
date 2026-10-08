using NUnit.Framework;

namespace TAoB.Combat.Tests
{
    /// <summary>사거리 쌍 술어의 경계값과 비대칭성을 고정한다(계획서 D5·D6).</summary>
    public class EngagementTests
    {
        private static readonly LaneConfig Config = TestBalance.StaticLane(frontReachFu: 100, backReachFu: 300, engageMarginFu: 20);

        [Test]
        public void TargetExactlyAtReach_CanBeReached()
        {
            UnitState actor = TestField.Ally(1, Row.Front);
            UnitState target = TestField.Enemy(101, Row.Front, posX: 100);

            Assert.IsTrue(Engagement.CanReach(actor, target, Config));
        }

        [Test]
        public void TargetOneStepBeyondReach_CannotBeReached()
        {
            UnitState actor = TestField.Ally(1, Row.Front);
            UnitState target = TestField.Enemy(101, Row.Front, posX: 101);

            Assert.IsFalse(Engagement.CanReach(actor, target, Config));
        }

        [Test]
        public void ReachComesFromActorRow_SoThePredicateIsAsymmetric()
        {
            UnitState front = TestField.Ally(1, Row.Front);
            UnitState back = TestField.Ally(2, Row.Back);
            UnitState target = TestField.Enemy(101, Row.Front, posX: 250);

            Assert.IsFalse(Engagement.CanReach(front, target, Config));
            Assert.IsTrue(Engagement.CanReach(back, target, Config));
        }

        [Test]
        public void EngagementStartsMarginInsideReach()
        {
            UnitState actor = TestField.Ally(1, Row.Front);
            UnitState atMargin = TestField.Enemy(101, Row.Front, posX: 80);
            UnitState justOutside = TestField.Enemy(102, Row.Front, posX: 81);

            Assert.IsTrue(Engagement.IsEngagedAt(actor, actor.PosX, atMargin.PosX, Config));
            Assert.IsFalse(Engagement.IsEngagedAt(actor, actor.PosX, justOutside.PosX, Config));
            // 교전은 아니지만 때릴 수는 있다 — 여유는 멈춰 설 자리를 정하는 값이다.
            Assert.IsTrue(Engagement.CanReach(actor, justOutside, Config));
        }

        [Test]
        public void SameSideUnits_AreNeverOpponents()
        {
            UnitState actor = TestField.Ally(1, Row.Front);
            UnitState[] units = TestField.ById(actor, TestField.Ally(2, Row.Front, posX: 10));

            Assert.IsFalse(Engagement.HasReachableOpponent(actor, units, Config));
            Assert.IsFalse(Engagement.HasOpponentOnField(actor, units));
        }

        [Test]
        public void DownedAndOffFieldUnits_AreNotOpponents()
        {
            UnitState actor = TestField.Ally(1, Row.Front);
            UnitState[] units = TestField.ById(
                actor,
                TestField.Enemy(101, Row.Front, posX: 10, condition: UnitCondition.Downed),
                TestField.Enemy(201, Row.Front, posX: 10, wave: 1));

            Assert.IsFalse(Engagement.HasReachableOpponent(actor, units, Config));
            Assert.IsFalse(Engagement.HasOpponentOnField(actor, units));
        }

        [Test]
        public void ScatteredOpponent_IsStillAnOpponent()
        {
            UnitState actor = TestField.Ally(1, Row.Front);
            UnitState[] units = TestField.ById(actor, TestField.Enemy(101, Row.Front, posX: 10, condition: UnitCondition.Scattered));

            Assert.IsTrue(Engagement.HasReachableOpponent(actor, units, Config));
        }

        [Test]
        public void OpponentFarAway_IsOnFieldButNotReachable()
        {
            // 이 두 질의의 차이가 "사거리 밖이라 기다린다"와 "상대가 없다"를 가른다.
            UnitState actor = TestField.Ally(1, Row.Front);
            UnitState[] units = TestField.ById(actor, TestField.Enemy(101, Row.Front, posX: 5000));

            Assert.IsTrue(Engagement.HasOpponentOnField(actor, units));
            Assert.IsFalse(Engagement.HasReachableOpponent(actor, units, Config));
            Assert.IsFalse(Engagement.IsEngagedAt(actor, actor.PosX, 5000, Config));
        }
    }
}
