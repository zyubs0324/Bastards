using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace TAoB.Combat.Tests
{
    /// <summary>
    /// 자동 구조의 시작 조건과 구조자 고르기(계획서 Phase D). 전투를 돌리지 않고 유닛 상태만 꾸며
    /// <see cref="RescueCasting.Advance"/> 한 번의 결과를 본다. 사거리 50 에 아군은 0, 적은 1000 에 있어 기본 장면은 아무도 교전하지 않는다.
    /// </summary>
    public class AutomaticRescueTests
    {
        private const int FarAway = 1000;

        private static readonly LaneConfig ShortReach = TestBalance.StaticLane(frontReachFu: 50, backReachFu: 50);

        /// <summary>뒷열만 전장 끝까지 닿는다 — 한쪽만 상대를 때릴 수 있는 장면을 만든다.</summary>
        private static readonly LaneConfig LongBackReach = TestBalance.StaticLane(frontReachFu: 50, backReachFu: 3000);

        private sealed class Scene
        {
            public readonly CommandQueue Queue = new CommandQueue();
            private readonly RescueCasting _rescues;

            public Scene(LaneConfig lane, params UnitState[] units)
            {
                UnitState[] byId = TestField.ById(units);
                var map = new Dictionary<int, UnitState>();
                foreach (UnitState unit in byId) map.Add(unit.Id, unit);
                _rescues = new RescueCasting(TestUnits.Balance(lane: lane, rescueTicks: 5), byId, map, Queue);
            }

            /// <summary>한 틱 진행하고 시작된 구조를 "구조자→대상" 으로 돌려준다.</summary>
            public string[] Started()
            {
                var events = new List<BattleEvent>();
                _rescues.Advance(1, events);
                return events.Where(e => e.Kind == BattleEventKind.RescueStarted).Select(e => e.SourceId + "→" + e.TargetId).ToArray();
            }
        }

        private static UnitState Scattered(int id)
        {
            return TestField.Ally(id, condition: UnitCondition.Scattered);
        }

        [Test]
        public void TwoScatteredAllies_GetTwoDifferentRescuers_InIdOrder()
        {
            var scene = new Scene(ShortReach, Scattered(1), Scattered(2), TestField.Ally(3), TestField.Ally(4), TestField.Enemy(101, posX: FarAway));

            CollectionAssert.AreEqual(new[] { "3→1", "4→2" }, scene.Started(), "이미 구조를 맡은 동료는 다시 뽑지 않는다.");
        }

        [Test]
        public void OneFreeAlly_RescuesTheLowerIdFirst_AndTheOtherWaits()
        {
            var scene = new Scene(ShortReach, Scattered(1), Scattered(2), TestField.Ally(3), TestField.Enemy(101, posX: FarAway));

            CollectionAssert.AreEqual(new[] { "3→1" }, scene.Started());
            CollectionAssert.IsEmpty(scene.Started(), "다음 틱에도 남은 구조자가 없으면 시작하지 않는다.");
        }

        [Test]
        public void NobodyAbleToRescue_StartsNothing()
        {
            var scene = new Scene(
                ShortReach, Scattered(1), TestField.Ally(2, condition: UnitCondition.Downed), TestField.Enemy(101, posX: FarAway));

            CollectionAssert.IsEmpty(scene.Started());
        }

        [Test]
        public void AllyWaitingOnAUserRescueOrder_IsNotPulledToAnotherTarget()
        {
            // 사용자가 3 번에게 1 번 구조를 지시해 뒀다(3 번의 차례가 아직 안 왔다). 자동 구조는 2 번을 남은 동료(4)에게 맡겨야 한다 —
            // 3 번을 2 번에 묶으면 1 번은 사용자가 지시했는데도 구조가 끝날 때까지 아무도 구조하지 못한다.
            var scene = new Scene(ShortReach, Scattered(1), Scattered(2), TestField.Ally(3), TestField.Ally(4), TestField.Enemy(101, posX: FarAway));
            scene.Queue.Set(BattleCommand.Rescue(3, 1));

            CollectionAssert.AreEqual(new[] { "4→2" }, scene.Started());
        }

        [Test]
        public void AllyWaitingOnAnyUserOrder_IsNotPickedAsRescuer()
        {
            var scene = new Scene(ShortReach, Scattered(1), TestField.Ally(2), TestField.Ally(3), TestField.Enemy(101, posX: FarAway));
            scene.Queue.Set(BattleCommand.UseSkill(2));

            CollectionAssert.AreEqual(new[] { "3→1" }, scene.Started(), "기술 명령을 기다리는 동료를 구조에 묶지 않는다.");
        }

        [Test]
        public void TargetTheUserAlreadyOrdered_IsLeftAlone_EvenWithFreeAllies()
        {
            var scene = new Scene(ShortReach, Scattered(1), TestField.Ally(2), TestField.Ally(3), TestField.Enemy(101, posX: FarAway));
            scene.Queue.Set(BattleCommand.Rescue(2, 1));

            CollectionAssert.IsEmpty(scene.Started());
        }

        [Test]
        public void EnemyThatCanHitAnAlly_CountsAsEngaged_EvenIfNoAllyCanHitBack()
        {
            // 뒷열 적만 사거리가 길다. 아군은 닿지 않지만 맞을 수 있으니 전투는 끝난 것이 아니다.
            UnitState[] units = { Scattered(1), TestField.Ally(2), TestField.Enemy(101, Row.Back, posX: FarAway) };
            var scene = new Scene(LongBackReach, units);

            Assert.IsTrue(Engagement.IsAnyoneEngaged(TestField.ById(units), LongBackReach));
            CollectionAssert.IsEmpty(scene.Started());
        }

        [Test]
        public void UnitsThatCannotAct_DoNotCountAsEngaged()
        {
            // 흩어진 뒷열 아군과 빈사인 뒷열 적은 사거리가 닿아도 행동하지 못한다.
            UnitState[] units =
            {
                TestField.Ally(1, Row.Back, condition: UnitCondition.Scattered),
                TestField.Ally(2),
                TestField.Enemy(101, Row.Back, posX: FarAway, condition: UnitCondition.Downed),
                TestField.Enemy(102, posX: FarAway),
            };

            Assert.IsFalse(Engagement.IsAnyoneEngaged(TestField.ById(units), LongBackReach));
            CollectionAssert.AreEqual(new[] { "2→1" }, new Scene(LongBackReach, units).Started());
        }

        [Test]
        public void ScatteredEnemies_AreNeverRescuedAutomatically()
        {
            var scene = new Scene(
                ShortReach,
                TestField.Ally(1),
                TestField.Enemy(101, posX: FarAway, condition: UnitCondition.Scattered),
                TestField.Enemy(102, posX: FarAway));

            CollectionAssert.IsEmpty(scene.Started());
        }
    }
}
