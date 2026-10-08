namespace TAoB.Combat
{
    /// <summary>
    /// 동료마다 정해진 성향(계획서 Phase C). 숨은 진형유지 값(<see cref="UnitSetup.FormationChance"/>)을 통과하면 이 성향대로 움직이고,
    /// 통과하지 못하면 흐트러져 자리를 지킨다. 대상 선택 성향(가까운 적·약한 적 등)은 Phase F-3 으로 미룬다.
    /// </summary>
    public enum Preference
    {
        /// <summary>성향 없음 — 늘 자리를 지킨다.</summary>
        None,

        /// <summary>동료 보호: 진형 앞쪽에 서고 상대편의 단일 대상 공격을 끌어온다(도발).</summary>
        Guardian,

        /// <summary>적극 돌진: 진형보다 크게 앞으로 나가 적진 깊이 들어간다.</summary>
        Charger,

        /// <summary>후방 유지: 자기 자리보다 조금 뒤에 선다.</summary>
        Rearguard,

        /// <summary>비겁: 자기 자리보다 크게 물러서 선다.</summary>
        Coward,
    }

    /// <summary>
    /// 한 조우 동안 유닛이 취하는 자세. 조우가 시작될 때 한 번 정하고 틱마다 다시 뽑지 않는다 —
    /// 틱마다 바꾸면 사거리 경계에서 진동하기 때문이다(계획서 Phase C).
    /// </summary>
    public enum Stance
    {
        /// <summary>자기 자리를 지킨다.</summary>
        Hold,

        /// <summary>자리보다 약간 앞(동료 보호). 도발한다.</summary>
        Forward,

        /// <summary>자리보다 크게 앞(적극 돌진·측면 침투).</summary>
        Charge,

        /// <summary>자리보다 약간 뒤(후방 유지).</summary>
        HoldBack,

        /// <summary>자리보다 크게 뒤(비겁).</summary>
        StepBack,
    }

    /// <summary>선호 → 자세, 자세 → 자리에서 벗어나는 거리. 성향의 의미를 이 한 곳에 둔다.</summary>
    internal static class Stances
    {
        /// <summary>성향을 따르기로 했을 때의 자세.</summary>
        public static Stance FromPreference(Preference preference)
        {
            switch (preference)
            {
                case Preference.Guardian: return Stance.Forward;
                case Preference.Charger: return Stance.Charge;
                case Preference.Rearguard: return Stance.HoldBack;
                case Preference.Coward: return Stance.StepBack;
                default: return Stance.Hold;
            }
        }

        /// <summary>
        /// 자기 자리에서 **바라보는 방향을 +로** 벗어나는 거리(fu). 아군은 오른쪽, 적은 왼쪽이 앞이다.
        /// </summary>
        public static int OffsetFu(Stance stance, LaneConfig lane)
        {
            switch (stance)
            {
                case Stance.Forward: return lane.FormationDriftFu;
                case Stance.Charge: return lane.ChargeDriftFu;
                case Stance.HoldBack: return -lane.FormationDriftFu;
                case Stance.StepBack: return -lane.StepBackFu;
                default: return 0;
            }
        }
    }
}
