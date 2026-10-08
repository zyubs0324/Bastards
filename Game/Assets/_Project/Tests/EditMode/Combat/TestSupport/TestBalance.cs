using System.Collections.Generic;

namespace TAoB.Combat.Tests
{
    /// <summary>
    /// 테스트 전용 밸런스 값. 규칙 코드에는 기본값을 두지 않기로 했으므로(수직 슬라이스 계획서 Phase 2) 테스트가 직접 만든다.
    /// </summary>
    internal static class TestBalance
    {
        public static IReadOnlyList<OverloadTier> DefaultTiers()
        {
            return new[]
            {
                new OverloadTier(500, 4000, 3000, 2000, 1000),
                new OverloadTier(1000, 3000, 3000, 3000, 1000),
                new OverloadTier(2000, 2000, 4000, 3500, 500),
                new OverloadTier(int.MaxValue, 1000, 4500, 4400, 100),
            };
        }

        /// <summary>
        /// 정지 프리셋(계획서 D11). 사거리가 전장보다 크고 자리 간격·여유가 0이라 모든 쌍이 사거리 안이고,
        /// 교전이 늘 성립해 기준점이 멈춘 채로 아무도 움직이지 않는다. 그래서 위치가 규칙에 들어와도 기존 동작이 그대로 남는다.
        /// 이동을 보는 테스트는 필요한 값만 이름으로 바꿔 쓴다.
        /// </summary>
        public static LaneConfig StaticLane(
            int marchSpeedFu = 1,
            int frontSlotOffsetFu = 0,
            int backSlotOffsetFu = 0,
            int frontReachFu = LaneConfig.MaxReachFu,
            int backReachFu = LaneConfig.MaxReachFu,
            int engageMarginFu = 0,
            int formationDriftFu = 0,
            int chargeDriftFu = 0,
            int stepBackFu = 0,
            int flankChance = 0)
        {
            return new LaneConfig(
                marchSpeedFu, frontSlotOffsetFu, backSlotOffsetFu, frontReachFu, backReachFu, engageMarginFu,
                formationDriftFu, chargeDriftFu, stepBackFu, flankChance);
        }

        /// <summary>
        /// 실데이터(balance.json) 수치를 옮긴 이동 설정 - 자세와 침투가 실제로 맞물리는 구성을 볼 때 쓴다.
        /// 데이터가 바뀌면 이 값도 함께 바꾼다(불변식 검증은 생성자가 한다).
        /// </summary>
        public static LaneConfig RealisticLane(int flankChance = 0)
        {
            return StaticLane(
                marchSpeedFu: 40, frontSlotOffsetFu: 120, backSlotOffsetFu: 120, frontReachFu: 440, backReachFu: 680, engageMarginFu: 120,
                formationDriftFu: 40, chargeDriftFu: 200, stepBackFu: 60, flankChance: flankChance);
        }

        public static BalanceConfig Create(
            int defenseK = 100,
            int weakAffinityPermille = 750,
            int normalAffinityPermille = 1000,
            int strongAffinityPermille = 1250,
            int frontDamageTakenPermille = 1000,
            int backDamageTakenPermille = 800,
            int critBaseChance = 500,
            int critChancePerSpd = 20,
            int critMultiplierPermille = 1500,
            int minimumDamage = 1,
            int resSelfDamagePerShortfall = 2,
            int equipBlockResShortfall = 20,
            int mndPenaltyPerShortfallPermille = 25,
            int minMndEffectPermille = 200,
            int actionGaugeMax = 1000,
            int scatterHpLossPerTick = 5,
            int rescueExtraGauge = 200,
            int overloadPowerPermille = 1500,
            int runawayDamagePermille = 1000,
            int runawaySelfDamagePermille = 200,
            int breakthroughMndGain = 10,
            IReadOnlyList<OverloadTier> overloadTiers = null,
            LaneConfig lane = null)
        {
            return new BalanceConfig(
                defenseK,
                weakAffinityPermille,
                normalAffinityPermille,
                strongAffinityPermille,
                frontDamageTakenPermille,
                backDamageTakenPermille,
                critBaseChance,
                critChancePerSpd,
                critMultiplierPermille,
                minimumDamage,
                resSelfDamagePerShortfall,
                equipBlockResShortfall,
                mndPenaltyPerShortfallPermille,
                minMndEffectPermille,
                actionGaugeMax,
                scatterHpLossPerTick,
                rescueExtraGauge,
                overloadPowerPermille,
                runawayDamagePermille,
                runawaySelfDamagePermille,
                breakthroughMndGain,
                overloadTiers ?? DefaultTiers(),
                lane ?? StaticLane());
        }
    }
}
