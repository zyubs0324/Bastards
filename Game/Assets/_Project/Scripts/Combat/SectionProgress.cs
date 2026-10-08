namespace TAoB.Combat
{
    /// <summary>
    /// 진형 기준점이 전진할지 멈출지만 정한다(계획서 D2). 이동-전투 전환을 상태로 두지 않기 때문에
    /// 심리스 여부가 전부 이 판정 하나에 달려 있다 — <see cref="BattleSimulation"/> 에 섞지 않고 따로 둔다.
    /// </summary>
    internal static class SectionProgress
    {
        /// <summary>
        /// 이번 틱에 기준점이 전진할 거리. 0 이면 멈춘다. 교전 여부는 자세가 아니라 **자리**에서 판정한다(Phase C) —
        /// 돌진한 아군이나 침투하러 앞서 나온 적이 있어도 진형은 정해진 거리에서 멈춘다. 그래야 모두가 같이 사거리 안에 들고,
        /// 침투한 적이 정지선을 앞당겨 제 이점을 스스로 지우는 일이 없다.
        /// </summary>
        /// <param name="enemyAnchorX">지금 조우 적의 진형 기준점.</param>
        public static int AdvanceFu(UnitState[] units, LaneConfig lane, int allyAnchorX, int enemyAnchorX)
        {
            if (HasScatteredAlly(units)) return 0;

            Row reference = ReferenceRow(units);
            for (int i = 0; i < units.Length; i++)
            {
                UnitState ally = units[i];
                if (!IsAliveAlly(ally) || ally.Row != reference) continue;

                int allySlotX = Movement.SlotFu(ally, allyAnchorX, lane);
                for (int j = 0; j < units.Length; j++)
                {
                    UnitState enemy = units[j];
                    if (enemy.Side != Side.Enemy || !enemy.IsTargetable) continue;

                    int enemySlotX = Movement.SlotFu(enemy, enemyAnchorX, lane);
                    if (Engagement.IsEngagedAt(ally, allySlotX, enemySlotX, lane)) return 0;
                }
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
