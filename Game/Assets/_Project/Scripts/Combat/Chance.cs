namespace TAoB.Combat
{
    /// <summary>
    /// 만분율 확률 판정의 단일 진입점(계획서 D8). 결과가 확정인 판정은 난수를 쓰지 않는다 —
    /// 그래야 확률을 끈 구성에서 뒤 판정들의 난수 순서가 흔들리지 않는다.
    /// </summary>
    internal static class Chance
    {
        /// <param name="chance">만분율(0 = 절대 안 일어남, <see cref="CombatScale.ChanceRange"/> = 반드시 일어남).</param>
        public static bool Roll(IRandom random, int chance)
        {
            if (chance <= 0) return false;
            if (chance >= CombatScale.ChanceRange) return true;
            return random.Next(CombatScale.ChanceRange) < chance;
        }
    }
}
