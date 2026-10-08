using System;

namespace TAoB.Combat
{
    /// <summary>
    /// 이동·교전 수치 묶음(계획서 Phase A). 범위와 교착 방지 불변식의 **정본**이다.
    /// 단위 fu 는 전장 단위다(계획서 D3). <see cref="BalanceConfig"/> 가 이 객체를 받는다.
    /// 자세·침투 수치(Phase C)는 <see cref="Stances"/>·<see cref="Tactics"/> 가 쓴다.
    /// </summary>
    public sealed class LaneConfig
    {
        public LaneConfig(
            int marchSpeedFu,
            int frontSlotOffsetFu,
            int backSlotOffsetFu,
            int frontReachFu,
            int backReachFu,
            int engageMarginFu,
            int formationDriftFu,
            int chargeDriftFu,
            int stepBackFu,
            int flankChance)
        {
            Guard.Range(marchSpeedFu, 1, MaxSpeedFu, nameof(marchSpeedFu));
            Guard.Range(frontSlotOffsetFu, 0, MaxOffsetFu, nameof(frontSlotOffsetFu));
            Guard.Range(backSlotOffsetFu, 0, MaxOffsetFu, nameof(backSlotOffsetFu));
            Guard.Range(frontReachFu, 1, MaxReachFu, nameof(frontReachFu));
            Guard.Range(backReachFu, 1, MaxReachFu, nameof(backReachFu));
            Guard.Range(engageMarginFu, 0, MaxMarginFu, nameof(engageMarginFu));
            Guard.Range(formationDriftFu, 0, MaxDriftFu, nameof(formationDriftFu));
            Guard.Range(chargeDriftFu, 0, MaxDriftFu, nameof(chargeDriftFu));
            Guard.Range(stepBackFu, 0, MaxDriftFu, nameof(stepBackFu));
            Guard.Range(flankChance, 0, CombatScale.ChanceRange, nameof(flankChance));

            // 교차 조건은 Guard 가 다루지 못하므로 수치를 담아 직접 던진다. 하나라도 어기면 아무도 사거리에 닿지 못하거나 제자리에서 떨린다.
            RequireEngagementWindows(marchSpeedFu, frontReachFu, backReachFu, engageMarginFu);
            RequireRetreatsStayInReach(frontReachFu, backReachFu, engageMarginFu, frontSlotOffsetFu, backSlotOffsetFu, formationDriftFu, stepBackFu);
            RequireChargeBounds(frontReachFu, engageMarginFu, frontSlotOffsetFu, backSlotOffsetFu, formationDriftFu, chargeDriftFu, stepBackFu, flankChance);

            MarchSpeedFu = marchSpeedFu;
            FrontSlotOffsetFu = frontSlotOffsetFu;
            BackSlotOffsetFu = backSlotOffsetFu;
            FrontReachFu = frontReachFu;
            BackReachFu = backReachFu;
            EngageMarginFu = engageMarginFu;
            FormationDriftFu = formationDriftFu;
            ChargeDriftFu = chargeDriftFu;
            StepBackFu = stepBackFu;
            FlankChance = flankChance;
        }

        // 교전 창(사거리 − 여유)이 한 걸음보다 좁으면 한 틱에 창을 건너뛴다 — 정지선이 영영 걸리지 않아
        // 진형이 적을 지나쳐 제한 시간까지 행군한다. 두 열 모두 창이 한 걸음 이상이어야 한다.
        private static void RequireEngagementWindows(int marchSpeedFu, int frontReachFu, int backReachFu, int engageMarginFu)
        {
            if (frontReachFu - engageMarginFu < marchSpeedFu)
            {
                throw new ArgumentException(
                    $"frontReachFu 는 engageMarginFu + marchSpeedFu({engageMarginFu + marchSpeedFu}) 이상이어야 앞열이 교전 창을 건너뛰지 않습니다. 값: {frontReachFu}",
                    nameof(frontReachFu));
            }

            if (backReachFu - engageMarginFu < marchSpeedFu)
            {
                throw new ArgumentException(
                    $"backReachFu 는 engageMarginFu + marchSpeedFu({engageMarginFu + marchSpeedFu}) 이상이어야 뒷열이 교전 창을 건너뛰지 않습니다. 값: {backReachFu}",
                    nameof(backReachFu));
            }
        }

        // 양쪽 진영이 같은 수치로 자세를 정하므로 서로 마주 보는 두 줄이 **각자** 물러설 수 있다 — 후퇴는 두 번 센다.
        // 앞열끼리는 정지선(사거리 − 여유)에서 양쪽이 물러선 만큼 더 멀어지고, 뒷열은 거기서 진형 간격만큼 더 뒤에 선다.
        private static void RequireRetreatsStayInReach(
            int frontReachFu, int backReachFu, int engageMarginFu,
            int frontSlotOffsetFu, int backSlotOffsetFu, int formationDriftFu, int stepBackFu)
        {
            int deepestRetreat = Math.Max(formationDriftFu, stepBackFu);
            long backReachNeeded = (long)frontReachFu - engageMarginFu + frontSlotOffsetFu + backSlotOffsetFu + 2L * deepestRetreat;
            if (backReachFu < backReachNeeded)
            {
                throw new ArgumentException(
                    $"backReachFu 는 앞열이 멈춰 서는 거리·진형 간격·양쪽 후퇴 거리의 합({backReachNeeded}) 이상이어야 뒷열이 적에 닿습니다. 값: {backReachFu}",
                    nameof(backReachFu));
            }

            // 양쪽이 모두 물러서도 앞열끼리 사거리 안에 남는다 — 여유(engageMarginFu)의 절반이 한 쪽의 한계다.
            // 넘으면 후방 유지·비겁이 맞선 판은 제한 시간까지 아무도 때리지 못한다.
            if (2L * formationDriftFu > engageMarginFu)
            {
                throw new ArgumentException(
                    $"formationDriftFu 의 두 배는 engageMarginFu({engageMarginFu}) 이하여야 양쪽이 물러서도 사거리 안에 남습니다. 값: {formationDriftFu}",
                    nameof(formationDriftFu));
            }

            if (2L * stepBackFu > engageMarginFu)
            {
                throw new ArgumentException(
                    $"stepBackFu 의 두 배는 engageMarginFu({engageMarginFu}) 이하여야 양쪽이 물러서도 사거리 안에 남습니다. 값: {stepBackFu}",
                    nameof(stepBackFu));
            }
        }

        // 돌진은 진형 앞쪽 자세보다 멀리 나가야 하고, 한쪽이 정지선(사거리 − 여유)을 넘어 상대의 자리를 지나치면 안 된다.
        // 양쪽이 동시에 돌진해 서로 엇갈리는 것은 허용한다(규칙상 문제가 없고 색 네모라 겹쳐 보일 뿐이다).
        private static void RequireChargeBounds(
            int frontReachFu, int engageMarginFu, int frontSlotOffsetFu, int backSlotOffsetFu,
            int formationDriftFu, int chargeDriftFu, int stepBackFu, int flankChance)
        {
            if (chargeDriftFu < formationDriftFu)
            {
                throw new ArgumentException(
                    $"chargeDriftFu 는 formationDriftFu({formationDriftFu}) 이상이어야 합니다. 값: {chargeDriftFu}",
                    nameof(chargeDriftFu));
            }

            if (chargeDriftFu >= frontReachFu - engageMarginFu)
            {
                throw new ArgumentException(
                    $"chargeDriftFu 는 frontReachFu − engageMarginFu({frontReachFu - engageMarginFu}) 보다 작아야 상대의 자리를 지나치지 않습니다. 값: {chargeDriftFu}",
                    nameof(chargeDriftFu));
            }

            // 측면 침투: 정지선에 선 적 앞열이 chargeDriftFu 만큼 나아가 아군 뒷열에 닿아야 한다. 뒷열은 진형 간격만큼 더 뒤에 서고,
            // 후방 유지·비겁 자세면 거기서 더 물러서 있다 — 그 가장 깊은 후퇴까지 닿아야 한다.
            // 닿지 못하면 침투한 적이 앞열만 때려 침투가 의미가 없고, 물러선 동료는 침투로부터 영구히 안전해진다.
            int deepestRetreat = Math.Max(formationDriftFu, stepBackFu);
            long flankReachNeeded = (long)frontSlotOffsetFu + backSlotOffsetFu + deepestRetreat - engageMarginFu;
            if (flankChance > 0 && chargeDriftFu < flankReachNeeded)
            {
                throw new ArgumentException(
                    $"flankChance 가 0 보다 크면 chargeDriftFu 는 {flankReachNeeded}(앞뒤 자리 간격 + 가장 깊은 후퇴 − 여유) 이상이어야 침투한 적이 뒷열에 닿습니다. 값: {chargeDriftFu}",
                    nameof(chargeDriftFu));
            }
        }

        /// <summary>자세에 따라 자리에서 벗어나는 거리의 상한(fu).</summary>
        public const int MaxDriftFu = 1000;

        /// <summary>행군·접근 속도 상한(fu/틱).</summary>
        public const int MaxSpeedFu = 1000;

        /// <summary>자리 간격 상한(fu).</summary>
        public const int MaxOffsetFu = 10000;

        /// <summary>사거리 상한(fu).</summary>
        public const int MaxReachFu = 20000;

        /// <summary>교전 여유 상한(fu).</summary>
        public const int MaxMarginFu = 1000;

        /// <summary>행군·접근 속도(fu/틱).</summary>
        public int MarchSpeedFu { get; }

        /// <summary>진형 기준점에서 앞열·뒷열 자리까지의 거리.</summary>
        public int FrontSlotOffsetFu { get; }

        public int BackSlotOffsetFu { get; }

        /// <summary>
        /// 앞열·뒷열 사거리. <see cref="Formation.ReachFu"/> 가 Row 로 고른다.
        /// 뒷열 사거리는 앞열이 멈춰 서는 거리까지 덮어야 한다(생성자 불변식).
        /// </summary>
        public int FrontReachFu { get; }

        public int BackReachFu { get; }

        /// <summary>사거리 경계보다 이만큼 안쪽에 선다. 경계에서 들락거리는 떨림을 막는다.</summary>
        public int EngageMarginFu { get; }

        /// <summary>동료 보호는 이만큼 앞에, 후방 유지는 이만큼 뒤에 선다.</summary>
        public int FormationDriftFu { get; }

        /// <summary>적극 돌진·측면 침투가 자리보다 앞으로 나가는 거리.</summary>
        public int ChargeDriftFu { get; }

        /// <summary>비겁이 자리보다 뒤로 물러서는 거리.</summary>
        public int StepBackFu { get; }

        /// <summary>적 앞열 한 명이 조우 시작에 측면 침투를 고를 확률(만분율).</summary>
        public int FlankChance { get; }
    }
}
