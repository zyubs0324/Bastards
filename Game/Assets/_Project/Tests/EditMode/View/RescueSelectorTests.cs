using System.Collections.Generic;
using NUnit.Framework;
using TAoB.Combat;

namespace TAoB.View.Tests
{
    /// <summary>구조 버튼의 대상 선택 규칙(계획서 Phase 4). 선택 UI 가 없으므로 이 규칙이 사실상 사용자가 보는 동작이다.</summary>
    public class RescueSelectorTests
    {
        [Test]
        public void PicksFirstScatteredAsTarget_AndFirstNormalAsRescuer()
        {
            IReadOnlyList<UnitSnapshot> units = Snapshots.List(
                Snapshots.Unit(1, condition: UnitCondition.Scattered),
                Snapshots.Unit(2, condition: UnitCondition.Scattered),
                Snapshots.Unit(3));

            RescuePair pair = RescueSelector.Select(units);

            Assert.IsTrue(pair.HasPair);
            Assert.AreEqual(1, pair.TargetId, "흩어진 첫 아군");
            Assert.AreEqual(3, pair.RescuerId, "정상 상태인 첫 아군");
        }

        [Test]
        public void SkipsDownedAndOffFieldAndEnemies()
        {
            IReadOnlyList<UnitSnapshot> units = Snapshots.List(
                Snapshots.Unit(1, condition: UnitCondition.Scattered, isOnField: false),
                Snapshots.Unit(101, Side.Enemy, UnitCondition.Scattered),
                Snapshots.Unit(2, condition: UnitCondition.Downed),
                Snapshots.Unit(3, condition: UnitCondition.Scattered),
                Snapshots.Unit(4));

            RescuePair pair = RescueSelector.Select(units);

            Assert.AreEqual(3, pair.TargetId);
            Assert.AreEqual(4, pair.RescuerId);
        }

        [Test]
        public void NoScattered_HasNoPair()
        {
            RescuePair pair = RescueSelector.Select(Snapshots.List(Snapshots.Unit(1), Snapshots.Unit(2)));

            Assert.IsFalse(pair.HasPair);
            Assert.IsFalse(RescueSelector.CanRescue(Snapshots.List(Snapshots.Unit(1))));
        }

        [Test]
        public void NoHealthyAlly_HasNoPair()
        {
            IReadOnlyList<UnitSnapshot> units = Snapshots.List(
                Snapshots.Unit(1, condition: UnitCondition.Scattered),
                Snapshots.Unit(2, condition: UnitCondition.Downed));

            Assert.IsFalse(RescueSelector.CanRescue(units));
        }

        [Test]
        public void ScatteredUnitIsNotItsOwnRescuer()
        {
            // 흩어진 유닛이 먼저 대상으로 잡히고, 뒤의 흩어진 유닛도 구조자가 될 수 없다.
            IReadOnlyList<UnitSnapshot> units = Snapshots.List(
                Snapshots.Unit(1, condition: UnitCondition.Scattered),
                Snapshots.Unit(2, condition: UnitCondition.Scattered));

            RescuePair pair = RescueSelector.Select(units);

            Assert.AreEqual(1, pair.TargetId);
            Assert.AreEqual(BattleEvent.NoUnit, pair.RescuerId);
        }

        [Test]
        public void EmptyOrNull_HasNoPair()
        {
            Assert.IsFalse(RescueSelector.CanRescue(Snapshots.List()));
            Assert.IsFalse(RescueSelector.CanRescue(null));
        }
    }
}
