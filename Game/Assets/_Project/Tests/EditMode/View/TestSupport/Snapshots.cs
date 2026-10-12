using System.Collections.Generic;
using TAoB.Combat;

namespace TAoB.View.Tests
{
    /// <summary>표시 계층 테스트용 스냅샷 조각. 규칙 모듈을 돌리지 않고 특정 상태만 만들어 본다.</summary>
    internal static class Snapshots
    {
        public static UnitSnapshot Unit(
            int id,
            Side side = Side.Ally,
            UnitCondition condition = UnitCondition.Normal,
            bool isOnField = true,
            Row row = Row.Front,
            int hp = 100,
            int overloadRisk = 0,
            bool isBoss = false,
            int posX = 0,
            Stance stance = Stance.Hold,
            bool isFlanking = false,
            int cooldown = 0,
            int rescueTargetId = BattleEvent.NoUnit,
            int rescueProgress = 0)
        {
            return new UnitSnapshot(
                id, side, row, isOnField, condition,
                hp, 100, 0, 1000, cooldown,
                overloadRisk, 50, false, 0, 0, isBoss, posX, stance, isFlanking, rescueTargetId, rescueProgress);
        }

        public static IReadOnlyList<UnitSnapshot> List(params UnitSnapshot[] units)
        {
            return units;
        }
    }
}
