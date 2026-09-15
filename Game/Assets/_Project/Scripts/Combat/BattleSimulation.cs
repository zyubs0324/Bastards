using System;
using System.Collections.Generic;

namespace TAoB.Combat
{
    /// <summary>
    /// 전투 1판을 고정 틱으로 진행한다. 같은 설정·같은 시드·같은 틱별 명령이면 항상 같은 결과를 낸다.
    /// </summary>
    public sealed class BattleSimulation
    {
        /// <summary>규칙 시간 단위. 밸런스 값이 아니므로 데이터 파일에 두지 않는다(계획서 Phase 2).</summary>
        public const int TicksPerSecond = 10;

        private readonly BattleSetup _setup;
        private readonly BalanceConfig _balance;

        /// <summary>설정 순서(아군 → 웨이브). 스냅샷 인덱스와 해시 순서의 기준.</summary>
        private readonly UnitState[] _units;
        private readonly UnitState[] _actionOrder;
        private readonly Dictionary<int, UnitState> _byId = new Dictionary<int, UnitState>();
        private readonly CommandQueue _queue = new CommandQueue();
        private readonly ActionResolver _resolver;

        public BattleSimulation(BattleSetup setup, IRandom random)
        {
            _setup = setup ?? throw new ArgumentNullException(nameof(setup));
            if (random == null) throw new ArgumentNullException(nameof(random));
            _balance = setup.Balance;

            var units = new List<UnitState>();
            foreach (UnitSetup ally in setup.Allies)
            {
                units.Add(new UnitState(ally, UnitState.AllyWave));
            }

            for (int wave = 0; wave < setup.Waves.Count; wave++)
            {
                foreach (UnitSetup enemy in setup.Waves[wave])
                {
                    units.Add(new UnitState(enemy, wave));
                }
            }

            _units = units.ToArray();
            foreach (UnitState unit in _units)
            {
                _byId.Add(unit.Id, unit);
            }

            var unitsById = (UnitState[])_units.Clone();
            Array.Sort(unitsById, (a, b) => a.Id.CompareTo(b.Id));
            _actionOrder = ActionGaugeScheduler.BuildActionOrder(_units);
            _resolver = new ActionResolver(_balance, setup.Affinity, random, unitsById, _byId, _queue);
            Outcome = BattleOutcome.InProgress;
        }

        public int Tick { get; private set; }
        public BattleOutcome Outcome { get; private set; }
        public int CurrentWave { get; private set; }
        public int WaveCount => _setup.Waves.Count;

        public CommandResult Enqueue(BattleCommand command)
        {
            if (Outcome != BattleOutcome.InProgress) return CommandResult.RejectedBattleOver;

            if (command.Kind == CommandKind.Retreat)
            {
                _queue.RequestRetreat();
                return CommandResult.Accepted;
            }

            if (!_byId.TryGetValue(command.ActorId, out UnitState actor)) return CommandResult.RejectedUnknownUnit;
            if (actor.Side != Side.Ally || command.Kind == CommandKind.BasicAttack) return CommandResult.RejectedNotControllable;
            if (actor.Condition != UnitCondition.Normal) return CommandResult.RejectedIncapacitated;

            switch (command.Kind)
            {
                case CommandKind.UseSkill:
                    if (actor.Cooldown > 0) return CommandResult.RejectedCooldown;
                    break;

                case CommandKind.Overload:
                    if (!CanOverload(actor)) return CommandResult.RejectedCannotOverload;
                    if (actor.Cooldown > 0) return CommandResult.RejectedCooldown;
                    break;

                case CommandKind.Rescue:
                    if (!IsRescueTarget(actor, command.TargetId)) return CommandResult.RejectedInvalidTarget;
                    break;
            }

            return _queue.Set(command) ? CommandResult.Replaced : CommandResult.Accepted;
        }

        /// <summary>한 틱을 진행한다. 호출자의 버퍼를 비우고 이번 틱의 이벤트를 채운다.</summary>
        public void Step(List<BattleEvent> events)
        {
            if (events == null) throw new ArgumentNullException(nameof(events));
            events.Clear();
            if (Outcome != BattleOutcome.InProgress) return;

            Tick++;
            if (_queue.RetreatRequested)
            {
                FinishBattle(true, false, events);
                return;
            }

            _resolver.ApplyScatterLoss(Tick, events);
            AdvanceCooldowns();
            ActionGaugeScheduler.Fill(_units, _balance);
            ResolveActions(events);

            bool waveCleared = IsWaveCleared(CurrentWave);
            bool finalWaveCleared = waveCleared && CurrentWave == WaveCount - 1;
            if (waveCleared && !finalWaveCleared)
            {
                StartWave(CurrentWave + 1, events);
            }

            if (EvaluateOutcome(false, finalWaveCleared) != BattleOutcome.InProgress)
            {
                FinishBattle(false, finalWaveCleared, events);
            }
        }

        public BattleSnapshot CreateSnapshotBuffer()
        {
            return new BattleSnapshot(this, _units.Length);
        }

        public void FillSnapshot(BattleSnapshot target)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));
            if (!ReferenceEquals(target.Owner, this)) throw new ArgumentException("다른 전투에서 만든 스냅샷 버퍼입니다.", nameof(target));

            target.Tick = Tick;
            target.Outcome = Outcome;
            target.CurrentWave = CurrentWave;
            target.WaveCount = WaveCount;
            for (int i = 0; i < _units.Length; i++)
            {
                UnitState unit = _units[i];
                Magitool tool = unit.Setup.Magitool;
                target.SetUnit(i, new UnitSnapshot(
                    unit.Id,
                    unit.Side,
                    unit.Row,
                    unit.IsOnField,
                    unit.Condition,
                    unit.Hp,
                    unit.MaxHp,
                    Math.Max(0, Math.Min(_balance.ActionGaugeMax, unit.ActionGauge)),
                    _balance.ActionGaugeMax,
                    unit.Cooldown,
                    OverloadRisk(unit),
                    unit.Mnd,
                    tool != null,
                    unit.MagitoolDurability,
                    tool != null ? tool.MaxDurability : 0,
                    unit.Setup.IsBoss));
            }
        }

        public BattleResult BuildResult()
        {
            var downedAllies = new List<int>();
            var wear = new List<MagitoolWear>();
            foreach (UnitState unit in _units)
            {
                if (unit.Side == Side.Ally && unit.Condition == UnitCondition.Downed) downedAllies.Add(unit.Id);
                if (unit.Setup.Magitool != null) wear.Add(new MagitoolWear(unit.Id, unit.Setup.Magitool.MaxDurability - unit.MagitoolDurability));
            }

            return new BattleResult(Outcome, Tick, downedAllies.AsReadOnly(), wear.AsReadOnly(), ComputeStateHash());
        }

        private void ResolveActions(List<BattleEvent> events)
        {
            for (int i = 0; i < _actionOrder.Length; i++)
            {
                UnitState unit = _actionOrder[i];
                if (!ActionGaugeScheduler.IsReady(unit, _balance)) continue;

                BattleCommand command = ChooseCommand(unit, events);
                ActionGaugeScheduler.Consume(unit, _balance);
                _resolver.Execute(unit, command, Tick, events);
            }
        }

        private BattleCommand ChooseCommand(UnitState unit, List<BattleEvent> events)
        {
            if (_queue.TryTake(unit.Id, out BattleCommand pending))
            {
                if (IsExecutable(unit, pending)) return pending;
                events.Add(new BattleEvent(BattleEventKind.CommandFallback, Tick, unit.Id, pending.TargetId, 0, (int)pending.Kind));
            }

            return AutoPilot.Choose(unit);
        }

        private bool IsExecutable(UnitState unit, BattleCommand command)
        {
            switch (command.Kind)
            {
                case CommandKind.UseSkill: return unit.Cooldown == 0;
                case CommandKind.Overload: return unit.Cooldown == 0 && CanOverload(unit);
                case CommandKind.Rescue: return IsRescueTarget(unit, command.TargetId);
                default: return false;
            }
        }

        private static bool CanOverload(UnitState unit)
        {
            return OverloadResolver.CanOverload(unit.Setup.Catalyst, unit.Setup.Skill);
        }

        private bool IsRescueTarget(UnitState rescuer, int targetId)
        {
            return targetId != rescuer.Id
                && _byId.TryGetValue(targetId, out UnitState target)
                && target.Side == rescuer.Side
                && target.IsOnField
                && target.Condition == UnitCondition.Scattered;
        }

        private int OverloadRisk(UnitState unit)
        {
            if (!CanOverload(unit)) return 0;
            return OverloadResolver.RiskLevel(unit.Setup.Catalyst.OverloadExcessPermille(unit.Mnd, _balance), _balance);
        }

        private void AdvanceCooldowns()
        {
            for (int i = 0; i < _units.Length; i++)
            {
                UnitState unit = _units[i];
                if (unit.IsOnField && unit.Condition != UnitCondition.Downed && unit.Cooldown > 0) unit.Cooldown--;
            }
        }

        private bool IsWaveCleared(int wave)
        {
            for (int i = 0; i < _units.Length; i++)
            {
                if (_units[i].WaveIndex == wave && _units[i].Condition != UnitCondition.Downed) return false;
            }

            return true;
        }

        private void StartWave(int wave, List<BattleEvent> events)
        {
            CurrentWave = wave;
            for (int i = 0; i < _units.Length; i++)
            {
                if (_units[i].WaveIndex == wave) _units[i].IsOnField = true;
            }

            events.Add(new BattleEvent(BattleEventKind.WaveStarted, Tick, BattleEvent.NoUnit, BattleEvent.NoUnit, wave, 0));
        }

        private BattleOutcome EvaluateOutcome(bool retreatProcessed, bool finalWaveCleared)
        {
            return BattleOutcomeRules.Evaluate(new OutcomeInputs(
                retreatProcessed,
                IsPreconditionBroken(),
                finalWaveCleared,
                AreAllAlliesDowned(),
                Tick >= _setup.TimeLimitTicks));
        }

        private void FinishBattle(bool retreatProcessed, bool finalWaveCleared, List<BattleEvent> events)
        {
            // 흩어진 채 끝나면 빈사로 확정한 뒤 다시 판정한다. 빈사자가 늘기만 하므로 이미 성립한 종료 조건은 사라지지 않는다.
            _resolver.DownScatteredUnits(Tick, events);
            Outcome = EvaluateOutcome(retreatProcessed, finalWaveCleared);
            events.Add(new BattleEvent(BattleEventKind.BattleEnded, Tick, BattleEvent.NoUnit, BattleEvent.NoUnit, 0, (int)Outcome));
        }

        private bool IsPreconditionBroken()
        {
            if (_setup.Precondition != PreconditionKind.NoAllyDowned) return false;

            for (int i = 0; i < _units.Length; i++)
            {
                if (_units[i].Side == Side.Ally && _units[i].Condition == UnitCondition.Downed) return true;
            }

            return false;
        }

        private bool AreAllAlliesDowned()
        {
            for (int i = 0; i < _units.Length; i++)
            {
                if (_units[i].Side == Side.Ally && _units[i].Condition != UnitCondition.Downed) return false;
            }

            return true;
        }

        private ulong ComputeStateHash()
        {
            var hasher = new StateHasher();
            hasher.Add(Tick);
            hasher.Add((int)Outcome);
            hasher.Add(CurrentWave);
            foreach (UnitState unit in _units)
            {
                hasher.Add(unit.Id);
                hasher.Add(unit.IsOnField ? 1 : 0);
                hasher.Add((int)unit.Condition);
                hasher.Add(unit.Hp);
                hasher.Add(unit.Mnd);
                hasher.Add(unit.ActionGauge);
                hasher.Add(unit.Cooldown);
                hasher.Add(unit.MagitoolDurability);
            }

            return hasher.Value;
        }
    }
}
