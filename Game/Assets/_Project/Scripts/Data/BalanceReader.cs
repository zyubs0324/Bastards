using System;
using TAoB.Combat;

namespace TAoB.Data
{
    /// <summary>balance.json → <see cref="BalanceConfig"/>·<see cref="AffinityTable"/>.</summary>
    internal static class BalanceReader
    {
        // 배열 크기 상한은 밸런스 값이 아니라 로더 규칙이다. 상성표는 직업 조합 수를 넘을 수 없다(중복은 AffinityTable 이 거부).
        public const int MaxOverloadTiers = 10;

        /// <summary>이동·교전 수치가 들어 있는 블록 이름. 오류 메시지의 필드 경로 접두가 된다.</summary>
        private const string LaneBlock = "movement";
        public static readonly int MaxAffinities = JobCount() * JobCount();

        public static BalanceConfig ReadBalance(string file, BalanceDto dto)
        {
            OverloadTier[] tiers = ReadTiers(file, dto.overloadTiers);
            LaneConfig lane = ReadLane(file, dto.movement);

            // 범위는 BalanceConfig 생성자가 정본이다. 여기서는 누락만 걸러 생성자에 넘기고, 생성자가 던진 예외의
            // 매개변수 이름(= JSON 키)을 필드명으로 옮긴다. 하한·상한을 로더에 다시 적지 않는다.
            try
            {
                return new BalanceConfig(
                    Required(file, nameof(dto.defenseK), dto.defenseK),
                    Required(file, nameof(dto.weakAffinityPermille), dto.weakAffinityPermille),
                    Required(file, nameof(dto.normalAffinityPermille), dto.normalAffinityPermille),
                    Required(file, nameof(dto.strongAffinityPermille), dto.strongAffinityPermille),
                    Required(file, nameof(dto.frontDamageTakenPermille), dto.frontDamageTakenPermille),
                    Required(file, nameof(dto.backDamageTakenPermille), dto.backDamageTakenPermille),
                    Required(file, nameof(dto.critBaseChance), dto.critBaseChance),
                    Required(file, nameof(dto.critChancePerSpd), dto.critChancePerSpd),
                    Required(file, nameof(dto.critMultiplierPermille), dto.critMultiplierPermille),
                    Required(file, nameof(dto.minimumDamage), dto.minimumDamage),
                    Required(file, nameof(dto.resSelfDamagePerShortfall), dto.resSelfDamagePerShortfall),
                    Required(file, nameof(dto.equipBlockResShortfall), dto.equipBlockResShortfall),
                    Required(file, nameof(dto.mndPenaltyPerShortfallPermille), dto.mndPenaltyPerShortfallPermille),
                    Required(file, nameof(dto.minMndEffectPermille), dto.minMndEffectPermille),
                    Required(file, nameof(dto.actionGaugeMax), dto.actionGaugeMax),
                    Required(file, nameof(dto.scatterHpLossPerTick), dto.scatterHpLossPerTick),
                    Required(file, nameof(dto.rescueExtraGauge), dto.rescueExtraGauge),
                    Required(file, nameof(dto.overloadPowerPermille), dto.overloadPowerPermille),
                    Required(file, nameof(dto.runawayDamagePermille), dto.runawayDamagePermille),
                    Required(file, nameof(dto.runawaySelfDamagePermille), dto.runawaySelfDamagePermille),
                    Required(file, nameof(dto.breakthroughMndGain), dto.breakthroughMndGain),
                    Required(file, nameof(dto.rescueTicks), dto.rescueTicks),
                    tiers,
                    lane);
            }
            catch (ArgumentException ex)
            {
                string field = string.IsNullOrEmpty(ex.ParamName) ? GameDataException.WholeFile : ex.ParamName;
                throw new GameDataException(file, field, ex.Message);
            }
        }

        public static AffinityTable ReadAffinity(string file, AffinityDto[] dtos)
        {
            const string name = "affinities";
            dtos = FieldCheck.Items(file, name, dtos, 0, MaxAffinities);

            var entries = new AffinityEntry[dtos.Length];
            for (int i = 0; i < dtos.Length; i++)
            {
                string field = $"{name}[{i}]";
                entries[i] = new AffinityEntry(
                    FieldCheck.EnumName<Job>(file, field + ".attacker", dtos[i].attacker),
                    FieldCheck.EnumName<Job>(file, field + ".defender", dtos[i].defender),
                    FieldCheck.EnumName<AffinityLevel>(file, field + ".level", dtos[i].level));
            }

            return FieldCheck.Build(file, name, () => new AffinityTable(entries));
        }

        /// <summary>
        /// movement 블록 → <see cref="LaneConfig"/>. 범위와 교착 방지 불변식은 생성자가 정본이므로
        /// 여기서는 누락만 걸러 넘기고, 생성자 예외의 매개변수 이름에 블록 이름을 붙여 필드 경로로 되돌린다.
        /// </summary>
        private static LaneConfig ReadLane(string file, MovementDto dto)
        {
            if (dto == null) throw new GameDataException(file, LaneBlock, "필드가 없습니다.");

            try
            {
                return new LaneConfig(
                    Required(file, LaneField(nameof(dto.marchSpeedFu)), dto.marchSpeedFu),
                    Required(file, LaneField(nameof(dto.frontSlotOffsetFu)), dto.frontSlotOffsetFu),
                    Required(file, LaneField(nameof(dto.backSlotOffsetFu)), dto.backSlotOffsetFu),
                    Required(file, LaneField(nameof(dto.frontReachFu)), dto.frontReachFu),
                    Required(file, LaneField(nameof(dto.backReachFu)), dto.backReachFu),
                    Required(file, LaneField(nameof(dto.engageMarginFu)), dto.engageMarginFu),
                    Required(file, LaneField(nameof(dto.formationDriftFu)), dto.formationDriftFu),
                    Required(file, LaneField(nameof(dto.chargeDriftFu)), dto.chargeDriftFu),
                    Required(file, LaneField(nameof(dto.stepBackFu)), dto.stepBackFu),
                    Required(file, LaneField(nameof(dto.flankChance)), dto.flankChance));
            }
            catch (ArgumentException ex)
            {
                string field = string.IsNullOrEmpty(ex.ParamName) ? LaneBlock : LaneField(ex.ParamName);
                throw new GameDataException(file, field, ex.Message);
            }
        }

        private static string LaneField(string name)
        {
            return LaneBlock + "." + name;
        }

        private static int Required(string file, string field, int value)
        {
            return FieldCheck.Required(file, field, value);
        }

        private static int JobCount()
        {
            return Enum.GetValues(typeof(Job)).Length;
        }

        private static OverloadTier[] ReadTiers(string file, OverloadTierDto[] dtos)
        {
            const string name = "overloadTiers";
            dtos = FieldCheck.Items(file, name, dtos, 1, MaxOverloadTiers);

            var tiers = new OverloadTier[dtos.Length];
            for (int i = 0; i < dtos.Length; i++)
            {
                OverloadTierDto dto = dtos[i];
                string field = $"{name}[{i}]";
                string maxField = field + ".maxExcessPermille";
                bool isLast = i == dtos.Length - 1;

                int maxExcess;
                if (isLast)
                {
                    // 마지막 구간은 상한이 없다. 파일에는 쓰지 않게 해 "상한이 있는데 무시된다"는 오해를 막는다.
                    if (dto.maxExcessPermille != FieldCheck.Missing)
                    {
                        throw new GameDataException(file, maxField, "마지막 구간은 상한이 없으므로 이 필드를 쓰지 않습니다.");
                    }

                    maxExcess = int.MaxValue;
                }
                else
                {
                    maxExcess = FieldCheck.Int(file, maxField, dto.maxExcessPermille, 0, CombatScale.MaxPermille);
                    if (i > 0 && maxExcess <= tiers[i - 1].MaxExcessPermille)
                    {
                        throw new GameDataException(file, maxField, $"값 {maxExcess} 은(는) 이전 구간의 상한 {tiers[i - 1].MaxExcessPermille} 보다 커야 합니다.");
                    }
                }

                int success = FieldCheck.Int(file, field + ".successChance", dto.successChance, 0, CombatScale.ChanceRange);
                int scatter = FieldCheck.Int(file, field + ".scatterChance", dto.scatterChance, 0, CombatScale.ChanceRange);
                int runaway = FieldCheck.Int(file, field + ".runawayChance", dto.runawayChance, 0, CombatScale.ChanceRange);
                int breakthrough = FieldCheck.Int(file, field + ".breakthroughChance", dto.breakthroughChance, 0, CombatScale.ChanceRange);

                // BalanceConfig 도 합을 검사하지만, 여기서 걸러야 몇 번째 구간인지 알려줄 수 있다.
                long total = (long)success + scatter + runaway + breakthrough;
                if (total != CombatScale.ChanceRange)
                {
                    throw new GameDataException(file, field, $"네 확률의 합이 {total} 입니다. {CombatScale.ChanceRange} 이어야 합니다.");
                }

                tiers[i] = new OverloadTier(maxExcess, success, scatter, runaway, breakthrough);
            }

            return tiers;
        }
    }
}
