using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace TAoB.Combat.Tests
{
    /// <summary>
    /// 대상 선택의 사거리 필터·도발·침투(계획서 Phase C). 후보는 사거리 안의 상대뿐이고,
    /// 도발은 가중 난수가 아니라 정해진 우선순위다(난수열이 바뀌지 않고 플레이어가 읽을 수 있다).
    /// </summary>
    public class TargetSelectorTests
    {
        // 앞열·뒷열 사거리 모두 100. 거리 100 은 닿고 101 은 닿지 않는다.
        private static readonly LaneConfig Lane = TestBalance.StaticLane(frontReachFu: 100, backReachFu: 100);

        private static List<UnitState> Pick(TargetRule rule, UnitState actor, params UnitState[] units)
        {
            var results = new List<UnitState>();
            TargetSelector.Select(rule, actor, TestField.ById(units), Lane, results);
            return results;
        }

        private static int[] Ids(List<UnitState> units)
        {
            return units.Select(u => u.Id).ToArray();
        }

        [Test]
        public void OnlyOpponentsInReach_AreCandidates()
        {
            UnitState actor = TestField.Ally(1);
            UnitState near = TestField.Enemy(101, Row.Back, posX: 100);
            UnitState far = TestField.Enemy(102, Row.Front, posX: 101);

            // 앞열이 사거리 밖이면 앞열 우선 규칙도 닿는 뒷열을 고른다.
            CollectionAssert.AreEqual(new[] { 101 }, Ids(Pick(TargetRule.FrontFirst, actor, actor, near, far)));
        }

        [Test]
        public void NobodyInReach_YieldsNoTarget()
        {
            UnitState actor = TestField.Ally(1);
            UnitState far = TestField.Enemy(101, posX: 500);

            Assert.IsEmpty(Pick(TargetRule.FrontFirst, actor, actor, far));
            Assert.IsEmpty(Pick(TargetRule.LowestHp, actor, actor, far));
            Assert.IsEmpty(Pick(TargetRule.All, actor, actor, far));
        }

        [Test]
        public void AreaRule_HitsEveryOpponentInReach_AndNoOneElse()
        {
            UnitState actor = TestField.Ally(1);
            UnitState a = TestField.Enemy(101, posX: 50);
            UnitState b = TestField.Enemy(102, Row.Back, posX: 100);
            UnitState far = TestField.Enemy(103, posX: 101);

            CollectionAssert.AreEqual(new[] { 101, 102 }, Ids(Pick(TargetRule.All, actor, actor, a, b, far)));
        }

        [Test]
        public void LowestHp_IgnoresWeakerOpponentsOutOfReach()
        {
            UnitState actor = TestField.Ally(1);
            UnitState strongNear = TestField.Enemy(101, posX: 50);
            UnitState weakFar = TestField.Enemy(102, posX: 500);
            weakFar.Hp = 1;

            CollectionAssert.AreEqual(new[] { 101 }, Ids(Pick(TargetRule.LowestHp, actor, actor, strongNear, weakFar)));
        }

        [Test]
        public void Taunt_PullsSingleTargetAttacksToTheTaunterInReach()
        {
            UnitState actor = TestField.Ally(1);
            UnitState front = TestField.Enemy(101, Row.Front, posX: 50);
            UnitState taunter = TestField.Enemy(102, Row.Back, posX: 80, stance: Stance.Forward);

            // 앞열 우선이라면 101 이지만 도발 중인 102 가 사거리 안에 있어 그쪽을 먼저 맞는다.
            CollectionAssert.AreEqual(new[] { 102 }, Ids(Pick(TargetRule.FrontFirst, actor, actor, front, taunter)));
            CollectionAssert.AreEqual(new[] { 102 }, Ids(Pick(TargetRule.LowestHp, actor, actor, front, taunter)));
        }

        [Test]
        public void Taunt_DoesNotAffectAreaAttacks()
        {
            UnitState actor = TestField.Ally(1);
            UnitState front = TestField.Enemy(101, Row.Front, posX: 50);
            UnitState taunter = TestField.Enemy(102, Row.Back, posX: 80, stance: Stance.Forward);

            CollectionAssert.AreEqual(new[] { 101, 102 }, Ids(Pick(TargetRule.All, actor, actor, front, taunter)));
        }

        [Test]
        public void TauntOutOfReach_AttractsNothing()
        {
            UnitState actor = TestField.Ally(1);
            UnitState front = TestField.Enemy(101, Row.Front, posX: 50);
            UnitState farTaunter = TestField.Enemy(102, Row.Back, posX: 500, stance: Stance.Forward);

            CollectionAssert.AreEqual(new[] { 101 }, Ids(Pick(TargetRule.FrontFirst, actor, actor, front, farTaunter)));
        }

        [Test]
        public void FlankingAttacker_TargetsTheBackRowFirst_AndIgnoresTaunt()
        {
            UnitState flanker = TestField.Enemy(101, Row.Front, posX: 0, isFlanking: true);
            UnitState front = TestField.Ally(1, Row.Front, posX: 50, stance: Stance.Forward);
            UnitState back = TestField.Ally(2, Row.Back, posX: 90);

            // 아군 앞열이 도발 중이고 사거리 안인데도, 침투한 적은 뒷열로 간다.
            CollectionAssert.AreEqual(new[] { 2 }, Ids(Pick(TargetRule.FrontFirst, flanker, flanker, front, back)));
        }

        [Test]
        public void FlankingAttacker_FallsBackToTheFrontRow_WhenTheBackRowIsOutOfReach()
        {
            UnitState flanker = TestField.Enemy(101, Row.Front, posX: 0, isFlanking: true);
            UnitState front = TestField.Ally(1, Row.Front, posX: 50);
            UnitState farBack = TestField.Ally(2, Row.Back, posX: 300);

            CollectionAssert.AreEqual(new[] { 1 }, Ids(Pick(TargetRule.FrontFirst, flanker, flanker, front, farBack)));
        }

        [Test]
        public void AlliedTaunter_DoesNotRestrictTheActorsTargets()
        {
            UnitState actor = TestField.Ally(1);
            UnitState alliedGuardian = TestField.Ally(2, Row.Front, posX: 50, stance: Stance.Forward);
            UnitState enemy = TestField.Enemy(101, Row.Front, posX: 50);

            // 도발은 상대편에게만 통한다. 같은 편 도발자가 후보를 가로채면 단일 대상 공격이 상대를 찾지 못한다.
            CollectionAssert.AreEqual(new[] { 101 }, Ids(Pick(TargetRule.FrontFirst, actor, actor, alliedGuardian, enemy)));
        }

        [Test]
        public void DownedTaunter_DoesNotTaunt()
        {
            UnitState actor = TestField.Ally(1);
            UnitState front = TestField.Enemy(101, Row.Front, posX: 50);
            UnitState downed = TestField.Enemy(102, Row.Back, posX: 80, condition: UnitCondition.Downed, stance: Stance.Forward);

            CollectionAssert.AreEqual(new[] { 101 }, Ids(Pick(TargetRule.FrontFirst, actor, actor, front, downed)));
        }
    }
}
