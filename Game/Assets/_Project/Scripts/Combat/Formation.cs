namespace TAoB.Combat
{
    public enum Row
    {
        Front,
        Back,
    }

    /// <summary>
    /// 진형(앞열·뒷열)에서 파생되는 값의 단일 소유자(계획서 D4) — 피해 배율(기획서 4-1)과 사거리·자리 간격.
    /// 사거리와 간격은 교착 방지 불변식에서 한 쌍으로 쓰이므로 소유 타입이 갈리면 안 된다.
    /// </summary>
    public static class Formation
    {
        public static int DamageTakenPermille(Row row, BalanceConfig balance)
        {
            return row == Row.Front ? balance.FrontDamageTakenPermille : balance.BackDamageTakenPermille;
        }

        /// <summary>이 열에 선 유닛이 때릴 수 있는 거리(fu).</summary>
        public static int ReachFu(Row row, LaneConfig lane)
        {
            return row == Row.Front ? lane.FrontReachFu : lane.BackReachFu;
        }

        /// <summary>
        /// 진형 기준점에서 이 열의 자리까지, 유닛이 바라보는 방향을 +로 한 거리.
        /// 앞열은 기준점보다 앞(+), 뒷열은 뒤(−)에 선다.
        /// </summary>
        public static int SlotOffsetFu(Row row, LaneConfig lane)
        {
            return row == Row.Front ? lane.FrontSlotOffsetFu : -lane.BackSlotOffsetFu;
        }
    }
}
