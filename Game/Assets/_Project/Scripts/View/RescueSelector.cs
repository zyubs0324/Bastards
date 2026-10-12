using System.Collections.Generic;
using TAoB.Combat;

namespace TAoB.View
{
    /// <summary>구조 명령의 구조자·대상 한 쌍. 어느 쪽이든 없으면 <see cref="HasPair"/> 가 false.</summary>
    public readonly struct RescuePair
    {
        public RescuePair(int rescuerId, int targetId)
        {
            RescuerId = rescuerId;
            TargetId = targetId;
        }

        public int RescuerId { get; }
        public int TargetId { get; }

        public bool HasPair => RescuerId != BattleEvent.NoUnit && TargetId != BattleEvent.NoUnit;
    }

    /// <summary>
    /// 구조 버튼이 누구를 누구로 구조할지 고른다(수직 슬라이스 계획서 Phase 4: 구조자 = 정상 상태인 첫 아군, 대상 = 흩어진 첫 아군).
    /// 이미 구조하고 있는 동료와 구조받고 있는 동료는 건너뛴다(계획서 Phase D — 구조는 시간이 걸린다).
    /// 선택 UI 가 없는 프로토타입용 규칙이라 표시 계층에 두지만, 순수 함수로 빼 두어 테스트로 고정한다.
    /// </summary>
    public static class RescueSelector
    {
        public static RescuePair Select(IReadOnlyList<UnitSnapshot> units)
        {
            int target = BattleEvent.NoUnit;
            int rescuer = BattleEvent.NoUnit;
            if (units == null) return new RescuePair(rescuer, target);

            for (int i = 0; i < units.Count; i++)
            {
                UnitSnapshot unit = units[i];
                if (unit.Side != Side.Ally || !unit.IsOnField) continue;

                if (unit.Condition == UnitCondition.Scattered)
                {
                    if (target == BattleEvent.NoUnit && !IsBeingRescued(units, unit.Id)) target = unit.Id;
                }
                else if (rescuer == BattleEvent.NoUnit && unit.Condition == UnitCondition.Normal && !unit.IsRescuing)
                {
                    rescuer = unit.Id;
                }
            }

            return new RescuePair(rescuer, target);
        }

        private static bool IsBeingRescued(IReadOnlyList<UnitSnapshot> units, int targetId)
        {
            for (int i = 0; i < units.Count; i++)
            {
                if (units[i].RescueTargetId == targetId) return true;
            }

            return false;
        }

        /// <summary>구조 버튼을 누를 수 있는 상태인지. 활성 조건과 실제 선택이 갈리지 않도록 같은 함수를 쓴다.</summary>
        public static bool CanRescue(IReadOnlyList<UnitSnapshot> units)
        {
            return Select(units).HasPair;
        }
    }
}
