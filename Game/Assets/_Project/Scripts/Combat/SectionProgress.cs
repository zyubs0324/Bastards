namespace TAoB.Combat
{
    /// <summary>
    /// 진형 기준점이 전진할지 멈출지만 정한다(계획서 D2). 이동-전투 전환을 상태로 두지 않기 때문에
    /// 심리스 여부가 전부 이 판정 하나에 달려 있다 — <see cref="BattleSimulation"/> 에 섞지 않고 따로 둔다.
    /// </summary>
    internal static class SectionProgress
    {
        /// <summary>이번 틱에 기준점이 전진할 거리. 0 이면 멈춘다.</summary>
        public static int AdvanceFu(UnitState[] units, LaneConfig lane)
        {
            if (HasScatteredAlly(units)) return 0;

            Row reference = ReferenceRow(units);
            for (int i = 0; i < units.Length; i++)
            {
                UnitState unit = units[i];
                if (!IsAliveAlly(unit) || unit.Row != reference) continue;
                if (Engagement.HasEngagedOpponent(unit, units, lane)) return 0;
            }

            return lane.MarchSpeedFu;
        }

        /// <summary>
        /// 정지선을 정하는 열. 앞열 생존자가 있으면 앞열이다 — 뒷열 사거리가 더 길어 뒷열로 정하면
        /// 앞열이 사거리 밖에 남아 "진형을 유지한 채 전투"와 반대가 된다.
        /// 앞열이 전멸했거나 전원 뒷열 편성이면 뒷열로 떨어져, 교전 중에 행군하는 일을 막는다.
        /// </summary>
        public static Row ReferenceRow(UnitState[] units)
        {
            for (int i = 0; i < units.Length; i++)
            {
                if (IsAliveAlly(units[i]) && units[i].Row == Row.Front) return Row.Front;
            }

            return Row.Back;
        }

        private static bool HasScatteredAlly(UnitState[] units)
        {
            for (int i = 0; i < units.Length; i++)
            {
                UnitState unit = units[i];
                if (unit.Side == Side.Ally && unit.IsOnField && unit.Condition == UnitCondition.Scattered) return true;
            }

            return false;
        }

        private static bool IsAliveAlly(UnitState unit)
        {
            return unit.Side == Side.Ally && unit.IsOnField && unit.Condition != UnitCondition.Downed;
        }
    }
}
