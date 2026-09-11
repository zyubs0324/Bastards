using System;

namespace TAoB.Combat
{
    /// <summary>
    /// 과부하 확률표의 한 구간. 초과 비율이 <see cref="MaxExcessPermille"/> 이하일 때 적용된다.
    /// 네 확률은 만분율이며 합이 10000 이어야 한다(<see cref="BalanceConfig"/> 가 검증).
    /// </summary>
    public sealed class OverloadTier
    {
        public OverloadTier(int maxExcessPermille, int successChance, int scatterChance, int runawayChance, int breakthroughChance)
        {
            if (maxExcessPermille < 0) throw new ArgumentException("0 이상이어야 합니다.", nameof(maxExcessPermille));
            if (successChance < 0) throw new ArgumentException("0 이상이어야 합니다.", nameof(successChance));
            if (scatterChance < 0) throw new ArgumentException("0 이상이어야 합니다.", nameof(scatterChance));
            if (runawayChance < 0) throw new ArgumentException("0 이상이어야 합니다.", nameof(runawayChance));
            if (breakthroughChance < 0) throw new ArgumentException("0 이상이어야 합니다.", nameof(breakthroughChance));

            MaxExcessPermille = maxExcessPermille;
            SuccessChance = successChance;
            ScatterChance = scatterChance;
            RunawayChance = runawayChance;
            BreakthroughChance = breakthroughChance;
        }

        public int MaxExcessPermille { get; }
        public int SuccessChance { get; }
        public int ScatterChance { get; }
        public int RunawayChance { get; }
        public int BreakthroughChance { get; }

        public long TotalChance => (long)SuccessChance + ScatterChance + RunawayChance + BreakthroughChance;
    }
}
