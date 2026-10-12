using System;
using System.Collections.Generic;

namespace TAoB.Combat
{
    /// <summary>
    /// 전투 밸런스 수치 묶음. 규칙 코드에는 기본값을 두지 않고 전부 이 객체로 받는다(수직 슬라이스 계획서 Phase 2).
    /// 배율은 ‰(1000 = 1.0), 확률은 만분율(10000 = 100%).
    /// </summary>
    public sealed class BalanceConfig
    {
        public BalanceConfig(
            int defenseK,
            int weakAffinityPermille,
            int normalAffinityPermille,
            int strongAffinityPermille,
            int frontDamageTakenPermille,
            int backDamageTakenPermille,
            int critBaseChance,
            int critChancePerSpd,
            int critMultiplierPermille,
            int minimumDamage,
            int resSelfDamagePerShortfall,
            int equipBlockResShortfall,
            int mndPenaltyPerShortfallPermille,
            int minMndEffectPermille,
            int actionGaugeMax,
            int scatterHpLossPerTick,
            int rescueExtraGauge,
            int overloadPowerPermille,
            int runawayDamagePermille,
            int runawaySelfDamagePermille,
            int breakthroughMndGain,
            int rescueTicks,
            IReadOnlyList<OverloadTier> overloadTiers,
            LaneConfig lane)
        {
            const int max = CombatScale.MaxValue;
            const int maxPermille = CombatScale.MaxPermille;

            Guard.Range(defenseK, 1, max, nameof(defenseK));
            Guard.Range(weakAffinityPermille, 0, maxPermille, nameof(weakAffinityPermille));
            Guard.Range(normalAffinityPermille, 0, maxPermille, nameof(normalAffinityPermille));
            Guard.Range(strongAffinityPermille, 0, maxPermille, nameof(strongAffinityPermille));
            Guard.Range(frontDamageTakenPermille, 0, maxPermille, nameof(frontDamageTakenPermille));
            Guard.Range(backDamageTakenPermille, 0, maxPermille, nameof(backDamageTakenPermille));
            Guard.Range(critBaseChance, 0, CombatScale.ChanceRange, nameof(critBaseChance));
            Guard.Range(critChancePerSpd, 0, CombatScale.ChanceRange, nameof(critChancePerSpd));
            Guard.Range(critMultiplierPermille, 0, maxPermille, nameof(critMultiplierPermille));
            Guard.Range(minimumDamage, 0, max, nameof(minimumDamage));
            Guard.Range(resSelfDamagePerShortfall, 0, max, nameof(resSelfDamagePerShortfall));
            Guard.Range(equipBlockResShortfall, 1, max, nameof(equipBlockResShortfall));
            Guard.Range(mndPenaltyPerShortfallPermille, 0, CombatScale.Permille, nameof(mndPenaltyPerShortfallPermille));
            Guard.Range(minMndEffectPermille, 0, CombatScale.Permille, nameof(minMndEffectPermille));
            Guard.Range(actionGaugeMax, 1, max, nameof(actionGaugeMax));
            Guard.Range(scatterHpLossPerTick, 0, max, nameof(scatterHpLossPerTick));
            Guard.Range(rescueExtraGauge, 0, max, nameof(rescueExtraGauge));
            Guard.Range(overloadPowerPermille, 0, maxPermille, nameof(overloadPowerPermille));
            Guard.Range(runawayDamagePermille, 0, maxPermille, nameof(runawayDamagePermille));
            Guard.Range(runawaySelfDamagePermille, 0, maxPermille, nameof(runawaySelfDamagePermille));
            Guard.Range(breakthroughMndGain, 0, max, nameof(breakthroughMndGain));
            Guard.Range(rescueTicks, 1, BattleSetup.MaxTimeLimitTicks, nameof(rescueTicks));
            ValidateTiers(overloadTiers);
            if (lane == null) throw new ArgumentNullException(nameof(lane));

            DefenseK = defenseK;
            WeakAffinityPermille = weakAffinityPermille;
            NormalAffinityPermille = normalAffinityPermille;
            StrongAffinityPermille = strongAffinityPermille;
            FrontDamageTakenPermille = frontDamageTakenPermille;
            BackDamageTakenPermille = backDamageTakenPermille;
            CritBaseChance = critBaseChance;
            CritChancePerSpd = critChancePerSpd;
            CritMultiplierPermille = critMultiplierPermille;
            MinimumDamage = minimumDamage;
            ResSelfDamagePerShortfall = resSelfDamagePerShortfall;
            EquipBlockResShortfall = equipBlockResShortfall;
            MndPenaltyPerShortfallPermille = mndPenaltyPerShortfallPermille;
            MinMndEffectPermille = minMndEffectPermille;
            ActionGaugeMax = actionGaugeMax;
            ScatterHpLossPerTick = scatterHpLossPerTick;
            RescueExtraGauge = rescueExtraGauge;
            OverloadPowerPermille = overloadPowerPermille;
            RunawayDamagePermille = runawayDamagePermille;
            RunawaySelfDamagePermille = runawaySelfDamagePermille;
            BreakthroughMndGain = breakthroughMndGain;
            RescueTicks = rescueTicks;
            OverloadTiers = new List<OverloadTier>(overloadTiers).AsReadOnly();
            Lane = lane;
        }

        /// <summary>방어 감소율 DEF/(DEF+K) 의 K.</summary>
        public int DefenseK { get; }
        public int WeakAffinityPermille { get; }
        public int NormalAffinityPermille { get; }
        public int StrongAffinityPermille { get; }
        public int FrontDamageTakenPermille { get; }
        public int BackDamageTakenPermille { get; }
        public int CritBaseChance { get; }
        public int CritChancePerSpd { get; }
        public int CritMultiplierPermille { get; }

        /// <summary>공격이 명중했을 때 보장되는 최소 피해.</summary>
        public int MinimumDamage { get; }

        /// <summary>마도구 요구 RES 부족 1당 사용 시 자해 피해.</summary>
        public int ResSelfDamagePerShortfall { get; }

        /// <summary>RES 부족분이 이 값 이상이면 장착 불가.</summary>
        public int EquipBlockResShortfall { get; }

        /// <summary>마도구 요구 MND 부족 1당 효과 감소(‰).</summary>
        public int MndPenaltyPerShortfallPermille { get; }
        public int MinMndEffectPermille { get; }
        public int ActionGaugeMax { get; }
        public int ScatterHpLossPerTick { get; }

        /// <summary>구조를 마친 구조자의 행동 게이지에서 빠지는 양.</summary>
        public int RescueExtraGauge { get; }
        public int OverloadPowerPermille { get; }
        public int RunawayDamagePermille { get; }

        /// <summary>폭주 시 시전자 최대 HP 대비 자해(‰).</summary>
        public int RunawaySelfDamagePermille { get; }
        public int BreakthroughMndGain { get; }

        /// <summary>구조에 걸리는 틱. 그동안 구조자는 행동하지 않고, 맞으면 처음부터 다시 센다.</summary>
        public int RescueTicks { get; }

        /// <summary>초과 비율 오름차순. 마지막 구간의 상한은 int.MaxValue.</summary>
        public IReadOnlyList<OverloadTier> OverloadTiers { get; }

        /// <summary>이동·교전 수치. 범위와 교착 방지 불변식의 정본은 <see cref="LaneConfig"/> 생성자다.</summary>
        public LaneConfig Lane { get; }

        private static void ValidateTiers(IReadOnlyList<OverloadTier> tiers)
        {
            const string name = "overloadTiers";
            if (tiers == null || tiers.Count == 0) throw new ArgumentException("과부하 확률표가 비어 있습니다.", name);

            for (int i = 0; i < tiers.Count; i++)
            {
                OverloadTier tier = tiers[i];
                if (tier == null) throw new ArgumentException($"{i}번 구간이 비어 있습니다.", name);
                if (tier.TotalChance != CombatScale.ChanceRange)
                {
                    throw new ArgumentException($"{i}번 구간 확률 합이 {tier.TotalChance} 입니다. {CombatScale.ChanceRange} 이어야 합니다.", name);
                }

                if (i > 0 && tier.MaxExcessPermille <= tiers[i - 1].MaxExcessPermille)
                {
                    throw new ArgumentException($"{i}번 구간 상한이 이전 구간보다 커야 합니다.", name);
                }
            }

            if (tiers[tiers.Count - 1].MaxExcessPermille != int.MaxValue)
            {
                throw new ArgumentException("마지막 구간의 상한은 int.MaxValue 여야 합니다.", name);
            }
        }
    }
}
