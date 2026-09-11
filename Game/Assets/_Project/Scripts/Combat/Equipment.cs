using System;

namespace TAoB.Combat
{
    /// <summary>
    /// 원석 촉매 — 세공한 자연 룬스톤. 내구력이 없다(설정집 2장 1번).
    /// </summary>
    public sealed class Catalyst
    {
        public Catalyst(int power)
        {
            Guard.Range(power, 0, CombatScale.MaxValue, nameof(power));
            Power = power;
        }

        public int Power { get; }

        // 설정집 5-1: 실제로 끌어낼 수 있는 양은 재능(MND)이 상한
        public int OutputFor(int mnd)
        {
            return Math.Min(Power, Math.Max(0, mnd));
        }

        public int OverloadOutput(BalanceConfig balance)
        {
            return CombatScale.ClampToInt((long)Power * balance.OverloadPowerPermille / CombatScale.Permille);
        }

        /// <summary>과부하 출력이 MND 를 넘는 비율(‰). 넘지 않으면 0.</summary>
        public int OverloadExcessPermille(int mnd, BalanceConfig balance)
        {
            // MND 0 으로 나누지 않도록 상한을 최소 1 로 본다.
            int cap = Math.Max(1, mnd);
            long over = (long)OverloadOutput(balance) - cap;
            if (over <= 0) return 0;
            return CombatScale.ClampToInt(over * CombatScale.Permille / cap);
        }
    }

    /// <summary>
    /// 마도구 — 녹였다 굳힌 룬스톤. 사용할 때마다 내구력이 줄어든다(설정집 2장 7번). 현재 내구력은 전투 상태가 가진다.
    /// </summary>
    public sealed class Magitool
    {
        public Magitool(int atkBonus, int requiredRes, int requiredMnd, int maxDurability, int durabilityCostPerUse)
        {
            Guard.Range(atkBonus, 0, CombatScale.MaxValue, nameof(atkBonus));
            Guard.Range(requiredRes, 0, CombatScale.MaxValue, nameof(requiredRes));
            Guard.Range(requiredMnd, 0, CombatScale.MaxValue, nameof(requiredMnd));
            Guard.Range(maxDurability, 0, CombatScale.MaxValue, nameof(maxDurability));
            Guard.Range(durabilityCostPerUse, 0, CombatScale.MaxValue, nameof(durabilityCostPerUse));

            AtkBonus = atkBonus;
            RequiredRes = requiredRes;
            RequiredMnd = requiredMnd;
            MaxDurability = maxDurability;
            DurabilityCostPerUse = durabilityCostPerUse;
        }

        public int AtkBonus { get; }
        public int RequiredRes { get; }
        public int RequiredMnd { get; }
        public int MaxDurability { get; }
        public int DurabilityCostPerUse { get; }
    }

    public readonly struct MagitoolUse
    {
        public MagitoolUse(int atkBonus, int selfDamage, int durabilityAfter)
        {
            AtkBonus = atkBonus;
            SelfDamage = selfDamage;
            DurabilityAfter = durabilityAfter;
        }

        public int AtkBonus { get; }
        public int SelfDamage { get; }
        public int DurabilityAfter { get; }
    }

    /// <summary>
    /// 마도구 사용 조건 2축(기획서 4-1, 설정집 7-5): RES 는 반발력(자해·장착 가능), MND 는 효과 배율.
    /// </summary>
    public static class EquipRules
    {
        public static int ResShortfall(int res, Magitool tool)
        {
            return Math.Max(0, tool.RequiredRes - res);
        }

        public static bool CanEquip(int res, Magitool tool, BalanceConfig balance)
        {
            return ResShortfall(res, tool) < balance.EquipBlockResShortfall;
        }

        public static int SelfDamagePerUse(int res, Magitool tool, BalanceConfig balance)
        {
            return CombatScale.ClampToInt((long)ResShortfall(res, tool) * balance.ResSelfDamagePerShortfall);
        }

        public static int MndEffectPermille(int mnd, Magitool tool, BalanceConfig balance)
        {
            int shortfall = Math.Max(0, tool.RequiredMnd - mnd);
            long effect = CombatScale.Permille - (long)shortfall * balance.MndPenaltyPerShortfallPermille;
            return (int)Math.Max(balance.MinMndEffectPermille, Math.Min(CombatScale.Permille, effect));
        }

        public static MagitoolUse Use(int res, int mnd, int durability, Magitool tool, BalanceConfig balance)
        {
            // 내구력이 다하면 효과와 반발이 함께 사라진다.
            if (durability <= 0) return new MagitoolUse(0, 0, 0);

            int atkBonus = (int)((long)tool.AtkBonus * MndEffectPermille(mnd, tool, balance) / CombatScale.Permille);
            int selfDamage = SelfDamagePerUse(res, tool, balance);
            int durabilityAfter = Math.Max(0, durability - tool.DurabilityCostPerUse);
            return new MagitoolUse(atkBonus, selfDamage, durabilityAfter);
        }
    }
}
