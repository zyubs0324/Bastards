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
            return IsWithin(actor.PosX, target.PosX, Formation.ReachFu(actor.Row, lane));
        }

        /// <summary>
        /// <paramref name="fromX"/> 에 선 <paramref name="actor"/> 가 <paramref name="toX"/> 의 상대와 교전이 성립하는가.
        /// 사거리 경계보다 여유만큼 안쪽을 본다 — 경계에서 멈추면 한 걸음에 들락거리며 떨리기 때문이다.
        /// 진형 정지선은 양쪽 모두 **자리**(자세 이전의 진형 위치)를 넣어 판정한다(계획서 Phase C) — 돌진한 아군이나
        /// 침투하러 앞서 나온 적의 실제 위치를 쓰면 진형이 일찍 멈춰 나머지는 사거리 밖에서 구경만 하거나 침투가 뒷열에 닿지 못한다.
        /// </summary>
        public static bool IsEngagedAt(UnitState actor, int fromX, int toX, LaneConfig lane)
        {
            return IsWithin(fromX, toX, Formation.ReachFu(actor.Row, lane) - lane.EngageMarginFu);
        }

        /// <summary>때릴 수 있는 상대가 하나라도 있는가(계획서 D6 의 단일 질의).</summary>
        public static bool HasReachableOpponent(UnitState actor, UnitState[] units, LaneConfig lane)
        {
            for (int i = 0; i < units.Length; i++)
            {
                UnitState other = units[i];
                if (other.Side == actor.Side || !other.IsTargetable) continue;
                if (CanReach(actor, other, lane)) return true;
            }

            return false;
        }

        /// <summary>거리를 빼고 상대가 전장에 남아 있는가. "사거리 밖"과 "상대가 아예 없음"을 구분하는 데 쓴다.</summary>
        public static bool HasOpponentOnField(UnitState actor, UnitState[] units)
        {
            for (int i = 0; i < units.Length; i++)
            {
                UnitState other = units[i];
                if (other.Side != actor.Side && other.IsTargetable) return true;
            }

            return false;
        }

        private static bool IsWithin(int fromPosX, int toPosX, int reachFu)
        {
            if (reachFu < 0) return false;
            return Lane.Distance(fromPosX, toPosX) <= reachFu;
        }
    }
}
