namespace TAoB.Combat
{
    /// <summary>규칙 모듈이 주입받는 난수 공급자.</summary>
    public interface IRandom
    {
        /// <summary>[0, maxExclusive) 범위의 정수를 돌려준다.</summary>
        int Next(int maxExclusive);
    }
}
