using TAoB.Combat;

namespace TAoB.View
{
    /// <summary>
    /// 스냅샷에서 읽는 "지금 할 수 있는가" 판정. 버튼 활성과 화면 표시가 같은 함수를 써서 갈리지 않게 한다.
    /// 명령을 받을지의 정본은 규칙 모듈(<see cref="BattleSimulation.Enqueue"/>)이고, 여기는 그 결과를 미리 보여 줄 뿐이다.
    /// </summary>
    public static class UnitReadiness
    {
        /// <summary>
        /// 사용자가 이 동료의 기술을 강제 발동할 수 있는가(계획서 Phase D). 참이면 기술 버튼이 켜지고 동료의 몸이 빛난다.
        /// 구조하는 동안에는 재사용 대기가 끝나 있어도 거짓이다 — 구조자는 다른 명령을 받지 않는다.
        /// </summary>
        public static bool CanForceSkill(in UnitSnapshot unit)
        {
            return unit.Side == Side.Ally
                && unit.IsOnField
                && unit.Condition == UnitCondition.Normal
                && unit.CooldownRemaining == 0
                && !unit.IsRescuing;
        }
    }
}
