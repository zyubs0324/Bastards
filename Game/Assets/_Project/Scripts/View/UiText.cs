using TAoB.Combat;

namespace TAoB.View
{
    /// <summary>화면 문구. 규칙 값을 문구로 바꾸기만 하고 규칙을 다시 계산하지 않는다.</summary>
    public static class UiText
    {
        public static string Outcome(BattleOutcome outcome)
        {
            switch (outcome)
            {
                case BattleOutcome.Victory: return "승리";
                case BattleOutcome.DefeatRetreat: return "패배 — 후퇴";
                case BattleOutcome.DefeatPrecondition: return "패배 — 사전 조건 위반(아군 빈사)";
                case BattleOutcome.DefeatWipe: return "패배 — 전멸";
                case BattleOutcome.DefeatTimeLimit: return "패배 — 제한 시간(자동 후퇴)";
                default: return "진행 중";
            }
        }

        public static string OverloadOutcome(OverloadOutcome outcome)
        {
            switch (outcome)
            {
                case Combat.OverloadOutcome.Success: return "과부하 성공!";
                case Combat.OverloadOutcome.Scatter: return "흩어짐!";
                case Combat.OverloadOutcome.Runaway: return "폭주!";
                case Combat.OverloadOutcome.Breakthrough: return "한계돌파!";
                default: return "과부하";
            }
        }

        public static string CommandResult(CommandResult result)
        {
            switch (result)
            {
                case Combat.CommandResult.Accepted: return "명령 수락";
                case Combat.CommandResult.Replaced: return "대기 명령 교체";
                case Combat.CommandResult.RejectedBattleOver: return "전투가 끝났습니다";
                case Combat.CommandResult.RejectedUnknownUnit: return "없는 유닛";
                case Combat.CommandResult.RejectedNotControllable: return "조작할 수 없는 유닛";
                case Combat.CommandResult.RejectedIncapacitated: return "행동 불가 상태";
                case Combat.CommandResult.RejectedCooldown: return "재사용 대기 중";
                case Combat.CommandResult.RejectedCannotOverload: return "과부하 불가";
                case Combat.CommandResult.RejectedInvalidTarget: return "구조 대상이 아닙니다";
                default: return "거부";
            }
        }

        public static string Condition(UnitCondition condition)
        {
            switch (condition)
            {
                case UnitCondition.Scattered: return "흩어짐";
                case UnitCondition.Downed: return "빈사";
                default: return "";
            }
        }

        /// <summary>색 네모 위에 쓰는 직업 한 글자(수직 슬라이스 계획서 Phase 4 "색 네모 + 직업 글자").</summary>
        public static string JobGlyph(Job job)
        {
            switch (job)
            {
                case Job.Knight: return "기";
                case Job.Mage: return "마";
                case Job.Perceptive: return "초";
                default: return "?";
            }
        }
    }
}
