using System;
using System.Collections.Generic;

namespace TAoB.Combat
{
    /// <summary>
    /// 파티가 함께 쓰는 소모품의 남은 개수와 사용 요청(계획서 Phase D). 유닛별 대기 명령과 따로 둔다 —
    /// 유닛 ID 가 키인 명령 큐에 넣으면 그 유닛의 기술 명령을 덮어쓴다.
    /// </summary>
    internal sealed class ItemBag
    {
        private readonly int[] _ids;
        private readonly int[] _healAmounts;
        private readonly int[] _remaining;

        /// <summary>종류별로 아직 처리하지 않은 사용 요청 수. 다음 틱에 한꺼번에 처리한다.</summary>
        private readonly int[] _requested;

        public ItemBag(IReadOnlyList<ConsumableSetup> consumables)
        {
            _ids = new int[consumables.Count];
            _healAmounts = new int[consumables.Count];
            _remaining = new int[consumables.Count];
            _requested = new int[consumables.Count];
            for (int i = 0; i < consumables.Count; i++)
            {
                _ids[i] = consumables[i].Id;
                _healAmounts[i] = consumables[i].HealAmount;
                _remaining[i] = consumables[i].Count;
            }
        }

        public int KindCount => _ids.Length;

        public int IdAt(int index) => _ids[index];

        public int RemainingAt(int index) => _remaining[index];

        /// <summary>사용을 요청한다. 알 수 없는 ID·재고 없음·회복할 동료 없음은 여기서 거른다.</summary>
        /// <param name="unitsById">ID 오름차순 전체 유닛.</param>
        public CommandResult Request(int itemId, UnitState[] unitsById)
        {
            int index = Array.IndexOf(_ids, itemId);
            if (index < 0) return CommandResult.RejectedUnknownItem;
            // 이미 요청해 둔 몫까지 빼고 본다 — 한 틱 안에 여러 번 눌러도 재고보다 많이 받지 않는다.
            if (_remaining[index] - _requested[index] <= 0) return CommandResult.RejectedNoStock;
            if (FindHealTarget(unitsById) == null) return CommandResult.RejectedNoHealTarget;

            _requested[index]++;
            return CommandResult.Accepted;
        }

        /// <summary>
        /// 쌓인 요청을 처리한다. 요청과 처리 사이에 회복할 동료가 사라졌으면 재고를 쓰지 않고 요청만 버린다.
        /// </summary>
        public void UseRequested(UnitState[] unitsById, int tick, List<BattleEvent> events)
        {
            for (int i = 0; i < _requested.Length; i++)
            {
                while (_requested[i] > 0)
                {
                    _requested[i]--;
                    UnitState target = FindHealTarget(unitsById);
                    if (target == null) continue;

                    int healed = (int)Math.Min((long)_healAmounts[i], (long)target.MaxHp - target.Hp);
                    target.Hp += healed;
                    _remaining[i]--;
                    events.Add(new BattleEvent(BattleEventKind.ItemUsed, tick, BattleEvent.NoUnit, target.Id, healed, _ids[i]));
                }
            }
        }

        public void AddTo(StateHasher hasher)
        {
            for (int i = 0; i < _ids.Length; i++)
            {
                hasher.Add(_ids[i]);
                hasher.Add(_remaining[i]);
                hasher.Add(_requested[i]);
            }
        }

        /// <summary>
        /// HP 비율이 가장 낮은 아군. 같으면 ID 가 작은 쪽(ID 순으로 돌며 더 낮을 때만 바꾼다).
        /// 빈사자와 HP 가 가득 찬 동료는 대상이 아니다 — 없으면 null.
        /// </summary>
        private static UnitState FindHealTarget(UnitState[] unitsById)
        {
            UnitState best = null;
            for (int i = 0; i < unitsById.Length; i++)
            {
                UnitState unit = unitsById[i];
                if (unit.Side != Side.Ally || !unit.IsTargetable || unit.Hp >= unit.MaxHp) continue;

                // 비율 비교를 나눗셈 없이 한다: a/A < b/B  ⇔  a×B < b×A.
                if (best == null || (long)unit.Hp * best.MaxHp < (long)best.Hp * unit.MaxHp) best = unit;
            }

            return best;
        }
    }
}
