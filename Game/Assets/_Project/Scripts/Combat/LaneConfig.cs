using System;

namespace TAoB.Combat
{
    /// <summary>
    /// 이동·교전 수치 묶음(계획서 Phase A). 범위와 교착 방지 불변식의 **정본**이다.
    /// 단위 fu 는 전장 단위다(계획서 D3). <see cref="BalanceConfig"/> 가 이 객체를 받는다.
    /// 이 Phase 가 소비하는 수치만 둔다 — 이탈 허용치·자세·침투 확률은 쓰는 코드와 함께 Phase C 에서 들어온다.
    /// </summary>
    public sealed class LaneConfig
    {
        public LaneConfig(
            int marchSpeedFu,
            int frontSlotOffsetFu,
            int backSlotOffsetFu,
            int frontReachFu,
            int backReachFu,
            int engageMarginFu)
        {
            Guard.Range(marchSpeedFu, 1, MaxSpeedFu, nameof(marchSpeedFu));
            Guard.Range(frontSlotOffsetFu, 0, MaxOffsetFu, nameof(frontSlotOffsetFu));
            Guard.Range(backSlotOffsetFu, 0, MaxOffsetFu, nameof(backSlotOffsetFu));
            Guard.Range(frontReachFu, 1, MaxReachFu, nameof(frontReachFu));
            Guard.Range(backReachFu, 1, MaxReachFu, nameof(backReachFu));
            Guard.Range(engageMarginFu, 0, MaxMarginFu, nameof(engageMarginFu));

            // 교착 방지 불변식. 하나라도 어기면 아무도 사거리에 닿지 못하거나 제자리에서 떨린다.
            // Guard 는 단일 값 범위만 다루므로 교차 조건은 수치를 담아 직접 던진다.
            // 교전 창(사거리 − 여유)이 한 걸음보다 좁으면 한 틱에 창을 건너뛴다 — 정지선이 영영 걸리지 않아
            // 진형이 적을 지나쳐 제한 시간까지 행군한다. 두 열 모두 창이 한 걸음 이상이어야 한다.
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

            // 진형은 앞열이 "사거리 − 여유" 거리에 닿으면 멈춘다. 뒷열은 거기서 진형 간격만큼 더 뒤에 서므로
            // 그 합을 덮지 못하면 뒷열은 전투 내내 한 번도 때리지 못한다(수치만 보고는 보이지 않는 교착).
            long backReachNeeded = (long)frontReachFu - engageMarginFu + frontSlotOffsetFu + backSlotOffsetFu;
            if (backReachFu < backReachNeeded)
            {
                throw new ArgumentException(
                    $"backReachFu 는 앞열이 멈춰 서는 거리와 진형 간격의 합({backReachNeeded}) 이상이어야 뒷열이 적에 닿습니다. 값: {backReachFu}",
                    nameof(backReachFu));
            }

            MarchSpeedFu = marchSpeedFu;
            FrontSlotOffsetFu = frontSlotOffsetFu;
            BackSlotOffsetFu = backSlotOffsetFu;
            FrontReachFu = frontReachFu;
            BackReachFu = backReachFu;
            EngageMarginFu = engageMarginFu;
        }

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
    }
}
