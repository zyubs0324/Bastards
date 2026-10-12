using System;
using System.Collections.Generic;

namespace TAoB.Combat
{
    /// <summary>SPD 비례 행동 게이지(기획서 4-1 ATB).</summary>
    internal static class ActionGaugeScheduler
    {
        /// <param name="unitsById">ID 오름차순 전체 유닛. 사거리 안에 상대가 있는지 보는 데 쓴다.</param>
        public static void Fill(UnitState[] units, UnitState[] unitsById, BalanceConfig balance)
        {
            for (int i = 0; i < units.Length; i++)
            {
                UnitState unit = units[i];
                // 구조 중에는 게이지가 멈춘다 — 구조자는 그동안 전투에서 빠진다(계획서 Phase D).
                if (unit.CanAct && !unit.IsRescuing)
                {
                    long filled = (long)unit.ActionGauge + unit.Setup.Stats.Spd;
                    long cap = Cap(unit, unitsById, balance);
                    // 이미 상한을 넘겨 모아둔 게이지는 깎지 않는다 — 상대가 사거리 밖으로 바뀐 틱에 몰수되면
                    // "건너뛴 틱에는 게이지를 유지한다"(D6)가 깨지고 조우 전환마다 빠른 유닛이 손해를 본다.
                    unit.ActionGauge = (int)(filled <= cap ? filled : Math.Max(unit.ActionGauge, cap));
                }
            }
        }

        /// <summary>
        /// 한 틱에 한 번만 행동하므로 최대치의 2배를 넘는 게이지는 의미가 없다. 긴 전투에서 int 가 넘치지 않게 여기서 멈춘다.
        /// 사거리 밖에서 행군하는 동안은 1배로 묶는다 — 2배까지 모아두면 접촉하는 순간 2연타가 터진다(계획서 D6).
        /// </summary>
        private static long Cap(UnitState unit, UnitState[] unitsById, BalanceConfig balance)
        {
            bool reachable = Engagement.HasReachableOpponent(unit, unitsById, balance.Lane);
            return reachable ? 2L * balance.ActionGaugeMax : balance.ActionGaugeMax;
        }

        public static bool IsReady(UnitState unit, BalanceConfig balance)
        {
            return unit.CanAct && !unit.IsRescuing && unit.ActionGauge >= balance.ActionGaugeMax;
        }

        /// <summary>넘친 게이지는 남겨 빠른 유닛의 이득을 보존한다.</summary>
        public static void Consume(UnitState unit, BalanceConfig balance)
        {
            unit.ActionGauge -= balance.ActionGaugeMax;
        }

        /// <summary>같은 틱에 여러 유닛이 준비되면 SPD 가 높은 순, 같으면 작은 ID 순으로 행동한다.</summary>
        public static UnitState[] BuildActionOrder(UnitState[] units)
        {
            var order = (UnitState[])units.Clone();
            Array.Sort(order, (a, b) =>
            {
                int bySpd = b.Setup.Stats.Spd.CompareTo(a.Setup.Stats.Spd);
                return bySpd != 0 ? bySpd : a.Id.CompareTo(b.Id);
            });
            return order;
        }
    }

    /// <summary>게이지가 찬 유닛에 대기 명령이 없을 때의 자동 판단(계획서 D7).</summary>
    internal static class AutoPilot
    {
        /// <summary>
        /// 기술은 숨은 빈도 스탯(<see cref="UnitSetup.SkillChance"/>)으로 나가고 재사용 대기를 보지도 걸지도 않는다.
        /// 재사용 대기는 사용자의 강제 발동 전용이라 자동 사용이 그것을 소모하면 개입의 가치가 사라진다.
        /// 과부하는 사용자 개입 전용으로 남긴다. 구조는 사용자 명령 외에 교전이 없을 때 <see cref="RescueCasting"/> 이 시작한다(이 경로를 거치지 않는다).
        /// </summary>
        public static BattleCommand Choose(UnitState unit, IRandom random)
        {
            if (Chance.Roll(random, unit.Setup.SkillChance)) return BattleCommand.AutoSkill(unit.Id);

            return BattleCommand.BasicAttack(unit.Id);
        }
    }

    /// <summary>
    /// 대상 규칙(기획서 4-1 진형·기술 대상)에 따라 공격 대상을 고른다.
    /// 전달받는 유닛 배열은 ID 오름차순이어야 한다 — 틱마다 정렬하지 않고 순서를 얻기 위함.
    ///
    /// 후보는 **사거리 안의 상대**뿐이다(계획서 Phase C). 정지 프리셋처럼 모든 쌍이 사거리 안이면 이전과 같다.
    /// 단일 대상 규칙은 도발 중인 상대(동료 보호)가 사거리 안에 있으면 그쪽만 후보로 삼는다 —
    /// 가중 난수가 아니라 정해진 우선순위라 플레이어가 읽을 수 있고 난수열도 바뀌지 않는다.
    /// 측면 침투 중인 유닛은 도발을 무시하고, 앞열 우선 규칙에서는 뒷열부터 노린다(앞열이 사거리 안에 있어도).
    /// </summary>
    internal static class TargetSelector
    {
        public static void Select(TargetRule rule, UnitState actor, UnitState[] unitsById, LaneConfig lane, List<UnitState> results)
        {
            results.Clear();
            Side opponent = actor.Side == Side.Ally ? Side.Enemy : Side.Ally;

            switch (rule)
            {
                case TargetRule.All:
                    for (int i = 0; i < unitsById.Length; i++)
                    {
                        if (IsReachable(unitsById[i], opponent, actor, lane)) results.Add(unitsById[i]);
                    }

                    return;

                case TargetRule.LowestHp:
                    AddIfFound(results, FindLowestHp(unitsById, opponent, actor, lane, HasTauntInReach(unitsById, opponent, actor, lane)));
                    return;

                default:
                    bool onlyTaunters = HasTauntInReach(unitsById, opponent, actor, lane);
                    Row first = actor.IsFlanking ? Row.Back : Row.Front;
                    Row second = actor.IsFlanking ? Row.Front : Row.Back;
                    UnitState target = FindFirstInRow(unitsById, opponent, first, actor, lane, onlyTaunters)
                        ?? FindFirstInRow(unitsById, opponent, second, actor, lane, onlyTaunters);
                    AddIfFound(results, target);
                    return;
            }
        }

        private static bool IsReachable(UnitState unit, Side opponent, UnitState actor, LaneConfig lane)
        {
            return IsOpponent(unit, opponent) && Engagement.CanReach(actor, unit, lane);
        }

        /// <summary>사거리 안에 도발 중인 상대가 있는가. 침투 중인 유닛은 도발을 무시한다.</summary>
        private static bool HasTauntInReach(UnitState[] unitsById, Side opponent, UnitState actor, LaneConfig lane)
        {
            if (actor.IsFlanking) return false;

            for (int i = 0; i < unitsById.Length; i++)
            {
                if (unitsById[i].IsTaunting && IsReachable(unitsById[i], opponent, actor, lane)) return true;
            }

            return false;
        }

        /// <summary>폭주 후보: 시전자를 뺀 공격 가능한 모든 유닛(아군 포함), ID 순.</summary>
        public static void SelectRunawayCandidates(UnitState actor, UnitState[] unitsById, List<UnitState> results)
        {
            results.Clear();
            for (int i = 0; i < unitsById.Length; i++)
            {
                if (unitsById[i] != actor && unitsById[i].IsTargetable) results.Add(unitsById[i]);
            }
        }

        private static bool IsOpponent(UnitState unit, Side opponent)
        {
            return unit.Side == opponent && unit.IsTargetable;
        }

        private static UnitState FindFirstInRow(UnitState[] unitsById, Side opponent, Row row, UnitState actor, LaneConfig lane, bool onlyTaunters)
        {
            for (int i = 0; i < unitsById.Length; i++)
            {
                UnitState unit = unitsById[i];
                if (!IsReachable(unit, opponent, actor, lane) || unit.Row != row) continue;
                if (onlyTaunters && !unit.IsTaunting) continue;

                return unit;
            }

            return null;
        }

        private static UnitState FindLowestHp(UnitState[] unitsById, Side opponent, UnitState actor, LaneConfig lane, bool onlyTaunters)
        {
            UnitState best = null;
            for (int i = 0; i < unitsById.Length; i++)
            {
                UnitState unit = unitsById[i];
                if (!IsReachable(unit, opponent, actor, lane)) continue;
                if (onlyTaunters && !unit.IsTaunting) continue;

                // ID 순으로 돌기 때문에 HP 가 같으면 먼저 찾은(작은 ID) 유닛이 남는다.
                if (best == null || unit.Hp < best.Hp) best = unit;
            }

            return best;
        }

        private static void AddIfFound(List<UnitState> results, UnitState unit)
        {
            if (unit != null) results.Add(unit);
        }
    }
}
