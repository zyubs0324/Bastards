using System.Collections.Generic;

namespace TAoB.Combat
{
    /// <summary>
    /// 유닛당 대기 명령 1개와 후퇴 요청을 보관한다. 새 명령은 기존 대기 명령을 대체한다(계획서 Phase 2).
    /// </summary>
    internal sealed class CommandQueue
    {
        private readonly Dictionary<int, BattleCommand> _pending = new Dictionary<int, BattleCommand>();

        public bool RetreatRequested { get; private set; }

        public void RequestRetreat()
        {
            RetreatRequested = true;
        }

        /// <returns>기존 대기 명령을 대체했으면 true.</returns>
        public bool Set(BattleCommand command)
        {
            bool replaced = _pending.ContainsKey(command.ActorId);
            _pending[command.ActorId] = command;
            return replaced;
        }

        /// <summary>꺼내지 않고 들여다본다. 사거리 밖이라 행동을 건너뛸 때 명령을 잃지 않기 위함(계획서 D6).</summary>
        public bool TryPeek(int unitId, out BattleCommand command)
        {
            return _pending.TryGetValue(unitId, out command);
        }

        public bool TryTake(int unitId, out BattleCommand command)
        {
            if (!_pending.TryGetValue(unitId, out command)) return false;

            _pending.Remove(unitId);
            return true;
        }

        public void Clear(int unitId)
        {
            _pending.Remove(unitId);
        }
    }
}
