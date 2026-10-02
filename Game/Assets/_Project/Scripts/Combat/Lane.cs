namespace TAoB.Combat
{
    /// <summary>
    /// 전장 1차원 좌표(fu)의 순수 정수 산술(계획서 D3). <see cref="Row"/> 를 모른다 —
    /// Row 에서 파생되는 값은 전부 <see cref="Formation"/> 이 갖는다(D4).
    /// </summary>
    internal static class Lane
    {
        /// <summary>아군은 +x(오른쪽)로 전진하고 적은 그 반대를 본다.</summary>
        public static int Facing(Side side) => side == Side.Ally ? 1 : -1;

        /// <summary>두 위치의 거리. 좌표 상한 안에서도 뺄셈이 넘치지 않도록 long 으로 계산한다.</summary>
        public static int Distance(int a, int b)
        {
            long diff = (long)a - b;
            return CombatScale.ClampToInt(diff < 0 ? -diff : diff);
        }

        /// <summary>
        /// <paramref name="from"/> 에서 <paramref name="to"/> 쪽으로 한 걸음. 남은 거리보다 크게 가지 않는다
        /// (넘어갔다 되돌아오는 ±step 진동 방지).
        /// </summary>
        public static int StepToward(int from, int to, int maxStep)
        {
            if (maxStep <= 0 || from == to) return from;

            long diff = (long)to - from;
            long step = diff < 0 ? -diff : diff;
            if (step > maxStep) step = maxStep;
            return CombatScale.ClampToInt(diff < 0 ? from - step : from + step);
        }
    }
}
