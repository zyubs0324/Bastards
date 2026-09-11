using System;
using System.Collections.Generic;

namespace TAoB.Combat
{
    /// <summary>SPD 비례 행동 게이지(기획서 4-1 ATB).</summary>
    internal static class ActionGaugeScheduler
    {
        public static void Fill(UnitState[] units, BalanceConfig balance)
        {
            // 한 틱에 한 번만 행동하므로 최대치의 2배를 넘는 게이지는 의미가 없다. 긴 전투에서 int 가 넘치지 않게 여기서 멈춘다.
            long cap = 2L * balance.ActionGaugeMax;
            for (int i = 0; i < units.Length; i++)
            {
                UnitState unit = units[i];
                if (unit.CanAct)
                {
                    unit.ActionGauge = (int)Math.Min(cap, (long)unit.ActionGauge + unit.Setup.Stats.Spd);
                }
            }
        }

        public static bool IsReady(UnitState unit, BalanceConfig balance)
        {
            return unit.CanAct && unit.ActionGauge >= balance.ActionGaugeMax;
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

    /// <summary>게이지가 찬 유닛에 대기 명령이 없을 때의 자동 판단(계획서 Phase 2).</summary>
    internal static class AutoPilot
    {
        // 아군은 기본 공격만 — 기술·과부하·구조는 사용자 개입 전용으로 두어 개입의 가치를 검증한다.
        public static BattleCommand Choose(UnitState unit)
        {
            if (unit.Side == Side.Enemy && unit.Cooldown == 0)
            {
                return BattleCommand.UseSkill(unit.Id);
            }

            return BattleCommand.BasicAttack(unit.Id);
        }
    }

    /// <summary>
    /// 대상 규칙(기획서 4-1 진형·기술 대상)에 따라 공격 대상을 고른다.
    /// 전달받는 유닛 배열은 ID 오름차순이어야 한다 — 틱마다 정렬하지 않고 순서를 얻기 위함.
    /// </summary>
    internal static class TargetSelector
    {
        public static void Select(TargetRule rule, UnitState actor, UnitState[] unitsById, List<UnitState> results)
        {
            results.Clear();
            Side opponent = actor.Side == Side.Ally ? Side.Enemy : Side.Ally;

            switch (rule)
            {
                case TargetRule.All:
                    for (int i = 0; i < unitsById.Length; i++)
                    {
                        if (IsOpponent(unitsById[i], opponent)) results.Add(unitsById[i]);
                    }

                    return;

                case TargetRule.LowestHp:
                    AddIfFound(results, FindLowestHp(unitsById, opponent));
                    return;

                default:
                    UnitState target = FindFirstInRow(unitsById, opponent, Row.Front) ?? FindFirstInRow(unitsById, opponent, Row.Back);
                    AddIfFound(results, target);
                    return;
            }
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

        private static UnitState FindFirstInRow(UnitState[] unitsById, Side opponent, Row row)
        {
            for (int i = 0; i < unitsById.Length; i++)
            {
                UnitState unit = unitsById[i];
                if (IsOpponent(unit, opponent) && unit.Row == row) return unit;
            }

            return null;
        }

        private static UnitState FindLowestHp(UnitState[] unitsById, Side opponent)
        {
            UnitState best = null;
            for (int i = 0; i < unitsById.Length; i++)
            {
                UnitState unit = unitsById[i];
                // ID 순으로 돌기 때문에 HP 가 같으면 먼저 찾은(작은 ID) 유닛이 남는다.
                if (IsOpponent(unit, opponent) && (best == null || unit.Hp < best.Hp)) best = unit;
            }

            return best;
        }

        private static void AddIfFound(List<UnitState> results, UnitState unit)
        {
            if (unit != null) results.Add(unit);
        }
    }
}
