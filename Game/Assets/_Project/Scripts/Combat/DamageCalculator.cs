using System;

namespace TAoB.Combat
{
    public readonly struct DamageInput
    {
        /// <param name="attackPower">ATK + 마도구 보너스 + 원석 출력.</param>
        /// <param name="powerPermille">행동의 피해 배율(기본 공격 1000).</param>
        public DamageInput(int attackPower, int powerPermille, AffinityLevel affinity, Row targetRow, int targetDef, bool isCritical)
        {
            AttackPower = attackPower;
            PowerPermille = powerPermille;
            Affinity = affinity;
            TargetRow = targetRow;
            TargetDef = targetDef;
            IsCritical = isCritical;
        }

        public int AttackPower { get; }
        public int PowerPermille { get; }
        public AffinityLevel Affinity { get; }
        public Row TargetRow { get; }
        public int TargetDef { get; }
        public bool IsCritical { get; }
    }

    public static class DamageCalculator
    {
        // GDD 4-1: 최종 데미지 = (ATK + 장비ATK) × 상성 × 진형 × (1 − DEF/(DEF+K)) × 크리티컬
        public static int Compute(DamageInput input, BalanceConfig balance)
        {
            long value = Math.Max(0, input.AttackPower);
            value = Scale(value, input.PowerPermille);
            value = Scale(value, AffinityPermille(input.Affinity, balance));
            value = Scale(value, Formation.DamageTakenPermille(input.TargetRow, balance));
            value = Scale(value, CombatScale.Permille - DefenseReductionPermille(input.TargetDef, balance));
            if (input.IsCritical)
            {
                value = Scale(value, balance.CritMultiplierPermille);
            }

            return (int)Math.Max(balance.MinimumDamage, value);
        }

        public static int DefenseReductionPermille(int def, BalanceConfig balance)
        {
            if (def <= 0) return 0;
            return (int)((long)def * CombatScale.Permille / ((long)def + balance.DefenseK));
        }

        public static int AffinityPermille(AffinityLevel level, BalanceConfig balance)
        {
            switch (level)
            {
                case AffinityLevel.Weak: return balance.WeakAffinityPermille;
                case AffinityLevel.Strong: return balance.StrongAffinityPermille;
                default: return balance.NormalAffinityPermille;
            }
        }

        public static int CritChance(int spd, BalanceConfig balance)
        {
            long chance = balance.CritBaseChance + (long)Math.Max(0, spd) * balance.CritChancePerSpd;
            return (int)Math.Min(CombatScale.ChanceRange, chance);
        }

        // 에디터·기기 결과를 맞추려고 곱할 때마다 버림하고, 단계마다 int 범위로 눌러 마지막 (int) 변환이 엉뚱한 값으로 넘어가지 않게 한다.
        private static long Scale(long value, int permille)
        {
            return Math.Min(int.MaxValue, value * Math.Max(0, permille) / CombatScale.Permille);
        }
    }
}
