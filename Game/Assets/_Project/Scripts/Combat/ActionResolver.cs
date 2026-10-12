using System;
using System.Collections.Generic;

namespace TAoB.Combat
{
    /// <summary>
    /// 한 유닛의 행동(공격·기술·과부하·구조 시작)과 피해 적용을 처리한다. 틱 진행 순서는 <see cref="BattleSimulation"/> 이 정한다.
    /// 확률 판정은 <see cref="Chance.Roll"/> 만 쓴다 — 가드를 복제하면 난수 소비 규약이 두 곳으로 갈린다(계획서 D8).
    /// </summary>
    internal sealed class ActionResolver
    {
        private readonly BalanceConfig _balance;
        private readonly AffinityTable _affinity;
        private readonly IRandom _random;
        private readonly UnitState[] _unitsById;
        private readonly Dictionary<int, UnitState> _byId;
        private readonly CommandQueue _queue;
        private readonly RescueCasting _rescues;

        // 틱마다 할당하지 않으려고 재사용한다.
        private readonly List<UnitState> _targets = new List<UnitState>();

        /// <param name="unitsById">ID 오름차순으로 정렬한 전체 유닛.</param>
        public ActionResolver(
            BalanceConfig balance,
            AffinityTable affinity,
            IRandom random,
            UnitState[] unitsById,
            Dictionary<int, UnitState> byId,
            CommandQueue queue,
            RescueCasting rescues)
        {
            _balance = balance;
            _affinity = affinity;
            _random = random;
            _unitsById = unitsById;
            _byId = byId;
            _queue = queue;
            _rescues = rescues;
        }

        public void Execute(UnitState actor, BattleCommand command, int tick, List<BattleEvent> events)
        {
            events.Add(new BattleEvent(BattleEventKind.ActionStarted, tick, actor.Id, command.TargetId, 0, (int)command.Kind));
            SkillDefinition skill = actor.Setup.Skill;

            switch (command.Kind)
            {
                case CommandKind.UseSkill:
                    // 자동 사용은 재사용 대기를 쓰지도 걸지도 않는다(계획서 D7). 대기는 사용자의 강제 발동 전용이다.
                    if (command.IsForced) actor.Cooldown = skill.CooldownTicks;
                    int catalystOutput = actor.Setup.Catalyst != null ? actor.Setup.Catalyst.OutputFor(actor.Mnd) : 0;
                    Attack(actor, skill.TargetRule, skill.PowerPermille, catalystOutput, tick, events);
                    break;

                case CommandKind.Overload:
                    ResolveOverload(actor, tick, events);
                    break;

                case CommandKind.Rescue:
                    _rescues.Start(actor, _byId[command.TargetId], tick, events);
                    break;

                default:
                    Attack(actor, TargetRule.FrontFirst, CombatScale.Permille, 0, tick, events);
                    break;
            }
        }

        public void ApplyScatterLoss(int tick, List<BattleEvent> events)
        {
            for (int i = 0; i < _unitsById.Length; i++)
            {
                UnitState unit = _unitsById[i];
                if (unit.IsOnField && unit.Condition == UnitCondition.Scattered)
                {
                    ApplyDamage(unit, _balance.ScatterHpLossPerTick, BattleEvent.NoUnit, BattleEventKind.ScatterDamage, 0, tick, events);
                }
            }
        }

        // 수직 슬라이스 계획서 Phase 2: 흩어진 채 승패가 정해지면 그 틱에 빈사로 확정한다.
        public void DownScatteredUnits(int tick, List<BattleEvent> events)
        {
            for (int i = 0; i < _unitsById.Length; i++)
            {
                UnitState unit = _unitsById[i];
                if (unit.IsOnField && unit.Condition == UnitCondition.Scattered) Down(unit, tick, events);
            }
        }

        private void Attack(UnitState actor, TargetRule rule, int powerPermille, int catalystOutput, int tick, List<BattleEvent> events)
        {
            TargetSelector.Select(rule, actor, _unitsById, _balance.Lane, _targets);
            if (_targets.Count == 0) return;

            MagitoolUse tool = UseMagitool(actor, tick, events);
            int attackPower = CombatScale.ClampToInt((long)actor.Setup.Stats.Atk + tool.AtkBonus + catalystOutput);
            int critChance = DamageCalculator.CritChance(actor.Setup.Stats.Spd, _balance);

            for (int i = 0; i < _targets.Count; i++)
            {
                UnitState target = _targets[i];
                bool isCritical = Chance.Roll(_random, critChance);
                int damage = DamageCalculator.Compute(
                    new DamageInput(attackPower, powerPermille, _affinity.Get(actor.Job, target.Job), target.Row, target.Setup.Stats.Def, isCritical),
                    _balance);
                ApplyDamage(target, damage, actor.Id, BattleEventKind.Damage, isCritical ? 1 : 0, tick, events);
            }

            ApplyDamage(actor, tool.SelfDamage, actor.Id, BattleEventKind.SelfDamage, 0, tick, events);
        }

        // 기획서 4-1 과부하 발동 · 설정집 5-2~5-4
        private void ResolveOverload(UnitState actor, int tick, List<BattleEvent> events)
        {
            Catalyst catalyst = actor.Setup.Catalyst;
            SkillDefinition skill = actor.Setup.Skill;
            int excess = catalyst.OverloadExcessPermille(actor.Mnd, _balance);
            OverloadOutcome outcome = OverloadResolver.Resolve(excess, _balance, _random);
            // 과부하는 사용자 개입 전용이라 언제나 강제 발동이다.
            actor.Cooldown = skill.CooldownTicks;
            events.Add(new BattleEvent(BattleEventKind.OverloadResolved, tick, actor.Id, BattleEvent.NoUnit, excess, (int)outcome));

            int output = catalyst.OverloadOutput(_balance);
            switch (outcome)
            {
                case OverloadOutcome.Scatter:
                    actor.Condition = UnitCondition.Scattered;
                    actor.ActionGauge = 0;
                    _queue.Clear(actor.Id);
                    events.Add(new BattleEvent(BattleEventKind.Scattered, tick, actor.Id, actor.Id, 0, 0));
                    break;

                case OverloadOutcome.Runaway:
                    Runaway(actor, output, tick, events);
                    break;

                case OverloadOutcome.Breakthrough:
                    // 가장 드문 결과가 성공보다 나쁘지 않게 같은 피해를 주고 MND 도 올린다(사용자 결정 2026-09-11).
                    Attack(actor, skill.TargetRule, skill.PowerPermille, output, tick, events);
                    actor.Mnd = (int)Math.Min(CombatScale.MaxValue, (long)actor.Mnd + _balance.BreakthroughMndGain);
                    events.Add(new BattleEvent(BattleEventKind.Breakthrough, tick, actor.Id, actor.Id, actor.Mnd, 0));
                    break;

                default:
                    Attack(actor, skill.TargetRule, skill.PowerPermille, output, tick, events);
                    break;
            }
        }

        private void Runaway(UnitState actor, int output, int tick, List<BattleEvent> events)
        {
            TargetSelector.SelectRunawayCandidates(actor, _unitsById, _targets);
            MagitoolUse tool = UseMagitool(actor, tick, events);

            if (_targets.Count > 0)
            {
                UnitState target = _targets[_random.Next(_targets.Count)];
                int attackPower = CombatScale.ClampToInt((long)actor.Setup.Stats.Atk + tool.AtkBonus + output);
                int power = CombatScale.ClampToInt((long)actor.Setup.Skill.PowerPermille * _balance.RunawayDamagePermille / CombatScale.Permille);
                int damage = DamageCalculator.Compute(
                    new DamageInput(attackPower, power, _affinity.Get(actor.Job, target.Job), target.Row, target.Setup.Stats.Def, false),
                    _balance);
                ApplyDamage(target, damage, actor.Id, BattleEventKind.Damage, 0, tick, events);
            }

            long selfDamage = (long)actor.MaxHp * _balance.RunawaySelfDamagePermille / CombatScale.Permille + tool.SelfDamage;
            ApplyDamage(actor, CombatScale.ClampToInt(selfDamage), actor.Id, BattleEventKind.SelfDamage, 0, tick, events);
        }

        private MagitoolUse UseMagitool(UnitState actor, int tick, List<BattleEvent> events)
        {
            Magitool tool = actor.Setup.Magitool;
            if (tool == null) return default;

            int before = actor.MagitoolDurability;
            MagitoolUse use = EquipRules.Use(actor.Setup.Stats.Res, actor.Mnd, before, tool, _balance);
            actor.MagitoolDurability = use.DurabilityAfter;
            if (before > 0 && use.DurabilityAfter == 0)
            {
                events.Add(new BattleEvent(BattleEventKind.MagitoolDepleted, tick, actor.Id, actor.Id, 0, 0));
            }

            return use;
        }

        private void ApplyDamage(UnitState target, int amount, int sourceId, BattleEventKind kind, int detail, int tick, List<BattleEvent> events)
        {
            if (amount <= 0 || target.Condition == UnitCondition.Downed) return;

            target.Hp = Math.Max(0, target.Hp - amount);
            events.Add(new BattleEvent(kind, tick, sourceId, target.Id, amount, detail));
            if (target.Hp == 0) Down(target, tick, events);
            // 공격을 맞은 구조자는 진행을 잃는다(적의 공격과 폭주 유탄). 자해와 흩어짐 피해는 맞은 것이 아니다.
            else if (kind == BattleEventKind.Damage) _rescues.Interrupt(target, tick, events);
        }

        private void Down(UnitState unit, int tick, List<BattleEvent> events)
        {
            unit.Hp = 0;
            unit.Condition = UnitCondition.Downed;
            unit.ActionGauge = 0;
            _queue.Clear(unit.Id);
            events.Add(new BattleEvent(BattleEventKind.Downed, tick, BattleEvent.NoUnit, unit.Id, 0, 0));
        }
    }
}
