namespace TAoB.Combat
{
    /// <summary>
    /// 틱당 이동 한 단계(계획서 Phase A). 이벤트를 만들지 않는다 — 위치는 스냅샷으로만 흐른다(D13).
    /// </summary>
    internal static class Movement
    {
        /// <summary>진형 기준점에 대해 이 유닛이 서야 할 자리.</summary>
        public static int SlotFu(UnitState unit, int anchorX, LaneConfig lane)
        {
            long slot = (long)anchorX + Lane.Facing(unit.Side) * Formation.SlotOffsetFu(unit.Row, lane);
            return CombatScale.ClampToInt(slot);
        }

        /// <summary>
        /// 모든 유닛을 자기 자리 쪽으로 한 걸음 옮긴다. 흩어지거나 빈사인 유닛은 제자리에 남는다.
        /// 흩어진 아군이 있으면 기준점도 멈추므로(계획서 D2) 그 사이에 진형이 벌어지지 않고,
        /// 빈사자는 구조 대상이 아니라 그대로 전장에 남는다.
        /// </summary>
        public static void Advance(UnitState[] units, int allyAnchorX, int enemyAnchorX, LaneConfig lane)
        {
            for (int i = 0; i < units.Length; i++)
            {
                UnitState unit = units[i];
                if (!unit.CanAct) continue;

                int anchorX = unit.Side == Side.Ally ? allyAnchorX : enemyAnchorX;
                unit.PosX = Lane.StepToward(unit.PosX, SlotFu(unit, anchorX, lane), lane.MarchSpeedFu);
            }
        }
    }
}
