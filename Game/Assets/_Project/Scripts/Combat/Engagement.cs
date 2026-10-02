namespace TAoB.Combat
{
    /// <summary>
    /// "사거리 안인가"의 단일 정본(계획서 D5·D6). 유닛 플래그를 늘리지 않고 쌍 술어로 둔다 —
    /// 거리 개념이 이 타입에만 있어 <see cref="UnitState.IsOnField"/>·<see cref="UnitState.CanAct"/> 의 뜻이 그대로 남는다.
    /// </summary>
    internal static class Engagement
    {
        /// <summary>행위자 기준 비대칭 술어 — 사거리는 행위자의 열에서 나온다. 거리만 보고 적·생존 여부는 호출자가 본다.</summary>
        public static bool CanReach(UnitState actor, UnitState target, LaneConfig lane)
        {
            return IsWithin(actor, target, Formation.ReachFu(actor.Row, lane));
        }

        /// <summary>
        /// 교전이 성립했는가. 사거리 경계보다 여유만큼 안쪽을 본다 —
        /// 경계에서 멈추면 한 걸음에 들락거리며 떨리기 때문이다.
        /// </summary>
        public static bool IsEngaged(UnitState actor, UnitState target, LaneConfig lane)
        {
            return IsWithin(actor, target, Formation.ReachFu(actor.Row, lane) - lane.EngageMarginFu);
        }

        /// <summary>때릴 수 있는 상대가 하나라도 있는가(계획서 D6 의 단일 질의).</summary>
        public static bool HasReachableOpponent(UnitState actor, UnitState[] units, LaneConfig lane)
        {
            return HasOpponent(actor, units, lane, Test.Reach);
        }

        /// <summary>교전이 성립한 상대가 하나라도 있는가. 앵커 정지 판정이 쓴다.</summary>
        public static bool HasEngagedOpponent(UnitState actor, UnitState[] units, LaneConfig lane)
        {
            return HasOpponent(actor, units, lane, Test.Engaged);
        }

        /// <summary>거리를 빼고 상대가 전장에 남아 있는가. "사거리 밖"과 "상대가 아예 없음"을 구분하는 데 쓴다.</summary>
        public static bool HasOpponentOnField(UnitState actor, UnitState[] units)
        {
            return HasOpponent(actor, units, null, Test.Any);
        }

        private enum Test
        {
            Any,
            Reach,
            Engaged,
        }

        private static bool HasOpponent(UnitState actor, UnitState[] units, LaneConfig lane, Test test)
        {
            for (int i = 0; i < units.Length; i++)
            {
                UnitState other = units[i];
                if (other.Side == actor.Side || !other.IsTargetable) continue;

                switch (test)
                {
                    case Test.Reach:
                        if (CanReach(actor, other, lane)) return true;
                        break;

                    case Test.Engaged:
                        if (IsEngaged(actor, other, lane)) return true;
                        break;

                    default:
                        return true;
                }
            }

            return false;
        }

        private static bool IsWithin(UnitState actor, UnitState target, int reachFu)
        {
            if (reachFu < 0) return false;
            return Lane.Distance(actor.PosX, target.PosX) <= reachFu;
        }
    }
}
