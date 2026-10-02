using System;

namespace TAoB.Data
{
    // JsonUtility 는 public 필드만 읽으므로 DTO 는 프로퍼티 대신 필드를 쓴다.
    // 정수 필드의 초기값 FieldCheck.Missing 은 "파일에 없음" 표시다 — JsonUtility 는 빠진 필드의 초기값을 그대로 둔다(JsonUtilityBehaviourTests).
    // 문자열·배열은 빠지면 null, 객체는 초기값이 채워진 새 인스턴스가 된다.

    [Serializable]
    internal sealed class BalanceDto
    {
        public int defenseK = FieldCheck.Missing;
        public int weakAffinityPermille = FieldCheck.Missing;
        public int normalAffinityPermille = FieldCheck.Missing;
        public int strongAffinityPermille = FieldCheck.Missing;
        public int frontDamageTakenPermille = FieldCheck.Missing;
        public int backDamageTakenPermille = FieldCheck.Missing;
        public int critBaseChance = FieldCheck.Missing;
        public int critChancePerSpd = FieldCheck.Missing;
        public int critMultiplierPermille = FieldCheck.Missing;
        public int minimumDamage = FieldCheck.Missing;
        public int resSelfDamagePerShortfall = FieldCheck.Missing;
        public int equipBlockResShortfall = FieldCheck.Missing;
        public int mndPenaltyPerShortfallPermille = FieldCheck.Missing;
        public int minMndEffectPermille = FieldCheck.Missing;
        public int actionGaugeMax = FieldCheck.Missing;
        public int scatterHpLossPerTick = FieldCheck.Missing;
        public int rescueExtraGauge = FieldCheck.Missing;
        public int overloadPowerPermille = FieldCheck.Missing;
        public int runawayDamagePermille = FieldCheck.Missing;
        public int runawaySelfDamagePermille = FieldCheck.Missing;
        public int breakthroughMndGain = FieldCheck.Missing;
        public OverloadTierDto[] overloadTiers;
        public AffinityDto[] affinities;
        public MovementDto movement;
    }

    [Serializable]
    internal sealed class MovementDto
    {
        public int marchSpeedFu = FieldCheck.Missing;
        public int frontSlotOffsetFu = FieldCheck.Missing;
        public int backSlotOffsetFu = FieldCheck.Missing;
        public int frontReachFu = FieldCheck.Missing;
        public int backReachFu = FieldCheck.Missing;
        public int engageMarginFu = FieldCheck.Missing;
    }

    [Serializable]
    internal sealed class OverloadTierDto
    {
        public int maxExcessPermille = FieldCheck.Missing;
        public int successChance = FieldCheck.Missing;
        public int scatterChance = FieldCheck.Missing;
        public int runawayChance = FieldCheck.Missing;
        public int breakthroughChance = FieldCheck.Missing;
    }

    [Serializable]
    internal sealed class AffinityDto
    {
        public string attacker;
        public string defender;
        public string level;
    }

    [Serializable]
    internal sealed class EquipmentDto
    {
        public CatalystDto[] catalysts;
        public MagitoolDto[] magitools;
    }

    [Serializable]
    internal sealed class CatalystDto
    {
        public string id;
        public string name;
        public int power = FieldCheck.Missing;
    }

    [Serializable]
    internal sealed class MagitoolDto
    {
        public string id;
        public string name;
        public int atkBonus = FieldCheck.Missing;
        public int requiredRes = FieldCheck.Missing;
        public int requiredMnd = FieldCheck.Missing;
        public int maxDurability = FieldCheck.Missing;
        public int durabilityCostPerUse = FieldCheck.Missing;
    }

    [Serializable]
    internal sealed class CharactersDto
    {
        public CharacterDto[] characters;
    }

    [Serializable]
    internal sealed class CharacterDto
    {
        public int id = FieldCheck.Missing;
        public string name;
        public string side;
        public string job;
        public string row;

        // bool 은 빠지면 false 다. 누락을 구분할 수 없으므로 "생략 = false" 를 파일 규칙으로 삼는다.
        public bool isBoss;
        public StatsDto stats;

        /// <summary>기술 자동 사용 빈도(만분율). 숨은 스탯이라 stats 블록 밖에 둔다.</summary>
        public int skillChance = FieldCheck.Missing;
        public SkillDto skill;

        // 비어 있거나 빠지면 장비 없음.
        public string catalystId;
        public string magitoolId;
    }

    [Serializable]
    internal sealed class StatsDto
    {
        public int atk = FieldCheck.Missing;
        public int def = FieldCheck.Missing;
        public int hp = FieldCheck.Missing;
        public int spd = FieldCheck.Missing;
        public int mnd = FieldCheck.Missing;
        public int res = FieldCheck.Missing;
    }

    [Serializable]
    internal sealed class SkillDto
    {
        public int powerPermille = FieldCheck.Missing;
        public string targetRule;
        public bool canOverload;
        public int cooldownTicks = FieldCheck.Missing;
    }

    [Serializable]
    internal sealed class StageDto
    {
        public int timeLimitSeconds = FieldCheck.Missing;
        public string precondition;
        public int[] allyIds;
        public WaveDto[] waves;
    }

    [Serializable]
    internal sealed class WaveDto
    {
        public int[] enemyIds;

        /// <summary>이 조우를 만나기까지 행군하는 거리(fu).</summary>
        public int approachFu = FieldCheck.Missing;
    }
}
