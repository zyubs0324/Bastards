namespace TAoB.Combat
{
    public enum OverloadOutcome
    {
        Success,
        Scatter,
        Runaway,
        Breakthrough,
    }

    /// <summary>
    /// 과부하 발동의 네 갈래 판정(기획서 4-1, 설정집 5-1~5-4). 초과 비율로 확률표 구간을 고른 뒤 한 번 굴린다.
    /// </summary>
    public static class OverloadResolver
    {
        /// <summary>표시용 위험 단계의 최댓값(0~3).</summary>
        public const int MaxRiskLevel = 3;

        public static int TierIndex(int excessPermille, BalanceConfig balance)
        {
            var tiers = balance.OverloadTiers;
            for (int i = 0; i < tiers.Count; i++)
            {
                if (excessPermille <= tiers[i].MaxExcessPermille) return i;
            }

            return tiers.Count - 1;
        }

        public static int RiskLevel(int excessPermille, BalanceConfig balance)
        {
            int index = TierIndex(excessPermille, balance);
            return index < MaxRiskLevel ? index : MaxRiskLevel;
        }

        public static OverloadOutcome Resolve(int excessPermille, BalanceConfig balance, IRandom random)
        {
            OverloadTier tier = balance.OverloadTiers[TierIndex(excessPermille, balance)];
            int roll = random.Next(CombatScale.ChanceRange);

            int threshold = tier.SuccessChance;
            if (roll < threshold) return OverloadOutcome.Success;

            threshold += tier.ScatterChance;
            if (roll < threshold) return OverloadOutcome.Scatter;

            threshold += tier.RunawayChance;
            if (roll < threshold) return OverloadOutcome.Runaway;

            return OverloadOutcome.Breakthrough;
        }
    }
}
