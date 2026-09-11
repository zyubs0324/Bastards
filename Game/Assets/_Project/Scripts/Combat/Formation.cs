namespace TAoB.Combat
{
    public enum Row
    {
        Front,
        Back,
    }

    /// <summary>진형(앞열·뒷열)이 받는 피해 배율(기획서 4-1).</summary>
    public static class Formation
    {
        public static int DamageTakenPermille(Row row, BalanceConfig balance)
        {
            return row == Row.Front ? balance.FrontDamageTakenPermille : balance.BackDamageTakenPermille;
        }
    }
}
