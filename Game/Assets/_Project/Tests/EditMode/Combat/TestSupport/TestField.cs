using System;

namespace TAoB.Combat.Tests
{
    /// <summary>
    /// 위치 판정 테스트용 유닛 상태 생성기. 전투를 돌리지 않고 좌표·상태만 꾸며,
    /// 이동·교전·전진 판정을 한 틱 단위로 들여다볼 수 있게 한다.
    /// </summary>
    internal static class TestField
    {
        public static UnitState Ally(int id, Row row = Row.Front, int posX = 0, UnitCondition condition = UnitCondition.Normal)
        {
            return Place(new UnitState(TestUnits.Ally(id, row), UnitState.AllyWave), posX, condition);
        }

        /// <param name="wave">0 이면 전투 시작부터 전장에 있고, 1 이상이면 아직 등장하지 않았다.</param>
        public static UnitState Enemy(int id, Row row = Row.Front, int posX = 0, UnitCondition condition = UnitCondition.Normal, int wave = 0)
        {
            return Place(new UnitState(TestUnits.Enemy(id, row), wave), posX, condition);
        }

        /// <summary>ID 오름차순 배열 — 규칙 모듈이 요구하는 순서다.</summary>
        public static UnitState[] ById(params UnitState[] units)
        {
            var sorted = (UnitState[])units.Clone();
            Array.Sort(sorted, (a, b) => a.Id.CompareTo(b.Id));
            return sorted;
        }

        private static UnitState Place(UnitState unit, int posX, UnitCondition condition)
        {
            unit.PosX = posX;
            unit.Condition = condition;
            return unit;
        }
    }
}
