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
                if (unit.CanAct)
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

    /// <summary>게이지가 찬 유닛에 대기 명령이 없을 때의 자동 판단(계획서 D7).</summary>
    internal static class AutoPilot
    {
        /// <summary>
        /// 기술은 숨은 빈도 스탯(<see cref="UnitSetup.SkillChance"/>)으로 나가고 재사용 대기를 보지도 걸지도 않는다.
        /// 재사용 대기는 사용자의 강제 발동 전용이라 자동 사용이 그것을 소모하면 개입의 가치가 사라진다.
        /// 과부하·구조는 사용자 개입 전용으로 남긴다.
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
