using System.Collections.Generic;

namespace TAoB.Combat
{
    /// <summary>
    /// 구조 캐스팅(계획서 Phase D). 구조는 시작한 틱부터 <see cref="BalanceConfig.RescueTicks"/> 틱이 걸리고,
    /// 그동안 구조자는 게이지를 채우지도 행동하지도 않는다. 구조자가 맞으면 진행이 처음으로 돌아간다.
    /// 틱 안에서의 호출 순서는 <see cref="BattleSimulation"/> 이 정한다 — 행동 단계가 끝난 뒤 <see cref="Advance"/> 를 불러
    /// 같은 틱의 피격이 완료보다 먼저 반영되게 한다.
    /// </summary>
    internal sealed class RescueCasting
    {
        private readonly BalanceConfig _balance;
        private readonly UnitState[] _unitsById;
        private readonly Dictionary<int, UnitState> _byId;
        private readonly CommandQueue _queue;

        /// <param name="unitsById">ID 오름차순으로 정렬한 전체 유닛.</param>
        public RescueCasting(BalanceConfig balance, UnitState[] unitsById, Dictionary<int, UnitState> byId, CommandQueue queue)
        {
            _balance = balance;
            _unitsById = unitsById;
            _byId = byId;
            _queue = queue;
        }

        public void Start(UnitState rescuer, UnitState target, int tick, List<BattleEvent> events)
        {
            rescuer.RescueTargetId = target.Id;
            // 시작한 틱이 진행 1 로 세어진다 — 같은 틱의 Advance 가 올린다.
            rescuer.RescueProgress = 0;
            events.Add(new BattleEvent(BattleEventKind.RescueStarted, tick, rescuer.Id, target.Id, 0, 0));
        }

        /// <summary>
        /// 이 구조자가 이 대상을 지금 구조하기 시작할 수 있는가. 명령을 받을 때와 실행할 때 모두 이 판정을 쓴다.
        /// 구조자 자신의 상태(행동 가능·구조 중 아님)는 호출자가 본다.
        /// </summary>
        public bool CanStart(UnitState rescuer, int targetId)
        {
            return targetId != rescuer.Id
                && _byId.TryGetValue(targetId, out UnitState target)
                && target.Side == rescuer.Side
                && IsWaitingForRescue(target);
        }

        public bool IsBeingRescued(int targetId)
        {
            for (int i = 0; i < _unitsById.Length; i++)
            {
                if (_unitsById[i].RescueTargetId == targetId) return true;
            }

            return false;
        }

        /// <summary>구조자가 공격을 맞았다. 쌓은 진행을 잃고 처음부터 다시 센다(구조 자체는 이어진다).</summary>
        public void Interrupt(UnitState unit, int tick, List<BattleEvent> events)
        {
            if (!unit.IsRescuing || unit.RescueProgress == 0) return;

            events.Add(new BattleEvent(BattleEventKind.RescueInterrupted, tick, unit.Id, unit.RescueTargetId, unit.RescueProgress, 0));
            unit.RescueProgress = 0;
        }

        /// <summary>
        /// 한 틱 진행: 성립하지 않게 된 구조를 취소하고, 교전이 없으면 자동 구조를 시작하고, 진행을 올려 다 찬 구조를 끝낸다.
        /// </summary>
        public void Advance(int tick, List<BattleEvent> events)
        {
            CancelBroken(tick, events);
            StartAutomatic(tick, events);

            for (int i = 0; i < _unitsById.Length; i++)
            {
                UnitState rescuer = _unitsById[i];
                if (!rescuer.IsRescuing) continue;

                rescuer.RescueProgress++;
                if (rescuer.RescueProgress >= _balance.RescueTicks) Complete(rescuer, tick, events);
            }
        }

        /// <summary>전투가 끝날 때 남은 구조를 조용히 지운다. 흩어진 대상은 이 뒤에 빈사로 확정된다.</summary>
        public void CancelAll()
        {
            for (int i = 0; i < _unitsById.Length; i++)
            {
                Clear(_unitsById[i]);
            }
        }

        private void CancelBroken(int tick, List<BattleEvent> events)
        {
            for (int i = 0; i < _unitsById.Length; i++)
            {
                UnitState rescuer = _unitsById[i];
                if (!rescuer.IsRescuing) continue;

                UnitState target = _byId[rescuer.RescueTargetId];
                bool holds = rescuer.CanAct && target.IsOnField && target.Condition == UnitCondition.Scattered;
                if (holds) continue;

                events.Add(new BattleEvent(BattleEventKind.RescueCancelled, tick, rescuer.Id, target.Id, 0, 0));
                Clear(rescuer);
            }
        }

        /// <summary>
        /// 자동 구조: 아무도 교전 중이 아닐 때만 시작한다(전투가 일단락된 뒤 이동 전). 교전 중의 구조는 사용자가 정한다.
        /// 사용자가 이미 구조를 지시해 둔 대상은 건드리지 않는다 — 가로채면 그 명령이 실행 불가로 떨어진다.
        /// </summary>
        private void StartAutomatic(int tick, List<BattleEvent> events)
        {
            bool isEngagementChecked = false;
            for (int i = 0; i < _unitsById.Length; i++)
            {
                UnitState target = _unitsById[i];
                if (!NeedsAutomaticRescue(target)) continue;

                // 교전 여부는 흩어진 동료가 있을 때만 본다(유닛 쌍을 다 훑는 판정이라 평소에는 건너뛴다).
                if (!isEngagementChecked)
                {
                    if (Engagement.IsAnyoneEngaged(_unitsById, _balance.Lane)) return;
                    isEngagementChecked = true;
                }

                UnitState rescuer = PickRescuer(target);
                if (rescuer == null) return;

                Start(rescuer, target, tick, events);
            }
        }

        private bool NeedsAutomaticRescue(UnitState target)
        {
            return target.Side == Side.Ally && IsWaitingForRescue(target) && !HasPendingRescueFor(target.Id);
        }

        /// <summary>전장에 흩어져 있고 아직 아무도 구조하고 있지 않다 — 한 동료를 둘이 구조하지 않는다.</summary>
        private bool IsWaitingForRescue(UnitState target)
        {
            return target.IsOnField && target.Condition == UnitCondition.Scattered && !IsBeingRescued(target.Id);
        }

        private bool HasPendingRescueFor(int targetId)
        {
            for (int i = 0; i < _unitsById.Length; i++)
            {
                if (_queue.TryPeek(_unitsById[i].Id, out BattleCommand pending)
                    && pending.Kind == CommandKind.Rescue
                    && pending.TargetId == targetId)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 후방 유지 성향이 먼저 나서고, 없으면 ID 가 가장 작은 동료가 맡는다. 사용자의 명령을 기다리는 동료는 뽑지 않는다 —
        /// 구조에 묶이면 그 명령(다른 동료의 구조일 수도 있다)이 구조가 끝날 때까지 밀리거나 실행 불가로 떨어진다.
        /// </summary>
        private UnitState PickRescuer(UnitState target)
        {
            UnitState fallback = null;
            for (int i = 0; i < _unitsById.Length; i++)
            {
                UnitState unit = _unitsById[i];
                if (unit.Side != target.Side || !unit.CanAct || unit.IsRescuing) continue;
                if (_queue.TryPeek(unit.Id, out _)) continue;
                if (unit.Setup.Preference == Preference.Rearguard) return unit;

                if (fallback == null) fallback = unit;
            }

            return fallback;
        }

        private void Complete(UnitState rescuer, int tick, List<BattleEvent> events)
        {
            UnitState target = _byId[rescuer.RescueTargetId];
            target.Condition = UnitCondition.Normal;
            target.ActionGauge = 0;
            // 게이지가 음수로 내려가도 두는 것이 의도 — 구조자의 다음 행동이 그만큼 늦어진다(수직 슬라이스 계획서 Phase 2 결정 로그).
            rescuer.ActionGauge -= _balance.RescueExtraGauge;
            Clear(rescuer);
            events.Add(new BattleEvent(BattleEventKind.Rescued, tick, rescuer.Id, target.Id, 0, 0));
        }

        private static void Clear(UnitState rescuer)
        {
            rescuer.RescueTargetId = BattleEvent.NoUnit;
            rescuer.RescueProgress = 0;
        }
    }
}
