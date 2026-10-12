using System;
using System.Collections.Generic;

namespace TAoB.Combat
{
    /// <summary>
    /// 전투 1판을 고정 틱으로 진행한다. 같은 설정·같은 시드·같은 틱별 명령이면 항상 같은 결과를 낸다.
    /// </summary>
    public sealed class BattleSimulation
    {
        /// <summary>규칙 시간 단위. 밸런스 값이 아니므로 데이터 파일에 두지 않는다(수직 슬라이스 계획서 Phase 2).</summary>
        public const int TicksPerSecond = 10;

        private readonly BattleSetup _setup;
        private readonly BalanceConfig _balance;
        private readonly LaneConfig _lane;
        private readonly IRandom _random;

        /// <summary>설정 순서(아군 → 웨이브). 스냅샷 인덱스와 해시 순서의 기준.</summary>
        private readonly UnitState[] _units;

        /// <summary>ID 오름차순. 위치·대상 판정이 틱마다 정렬하지 않고 순서를 얻는 기준.</summary>
        private readonly UnitState[] _unitsById;
        private readonly UnitState[] _actionOrder;

        /// <summary>조우별 적 진형 기준점(접근 거리 누적 합).</summary>
        private readonly int[] _waveAnchorX;
        private readonly Dictionary<int, UnitState> _byId = new Dictionary<int, UnitState>();
        private readonly CommandQueue _queue = new CommandQueue();
        private readonly ActionResolver _resolver;
        private readonly RescueCasting _rescues;
        private readonly ItemBag _items;

        public BattleSimulation(BattleSetup setup, IRandom random)
        {
            _setup = setup ?? throw new ArgumentNullException(nameof(setup));
            if (random == null) throw new ArgumentNullException(nameof(random));
            _random = random;
            _balance = setup.Balance;
            _lane = _balance.Lane;

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

            _unitsById = (UnitState[])_units.Clone();
            Array.Sort(_unitsById, (a, b) => a.Id.CompareTo(b.Id));
            _actionOrder = ActionGaugeScheduler.BuildActionOrder(_units);
            _rescues = new RescueCasting(_balance, _unitsById, _byId, _queue);
            _resolver = new ActionResolver(_balance, setup.Affinity, random, _unitsById, _byId, _queue, _rescues);
            _items = new ItemBag(setup.Consumables);

            _waveAnchorX = BuildWaveAnchors(setup);
            EnemyAnchorX = _waveAnchorX[0];
            PlaceUnits();

            // 첫 조우의 자세·침투를 정한다. 확률이 0 이면 난수를 쓰지 않으므로 성향 없는 구성의 난수열은 그대로다.
            Tactics.DecideEncounter(_unitsById, 0, _lane, _random);
            Outcome = BattleOutcome.InProgress;
        }

        public int Tick { get; private set; }
        public BattleOutcome Outcome { get; private set; }
        public int CurrentWave { get; private set; }
        public int WaveCount => _setup.Waves.Count;

        /// <summary>아군 진형 기준점(fu). 전진·정지는 <see cref="SectionProgress"/> 가 정한다.</summary>
        public int AnchorX { get; private set; }

        /// <summary>지금 조우 적의 진형 기준점(fu). 조우가 바뀔 때만 움직인다.</summary>
        public int EnemyAnchorX { get; private set; }

        public CommandResult Enqueue(BattleCommand command)
        {
            if (Outcome != BattleOutcome.InProgress) return CommandResult.RejectedBattleOver;

            if (command.Kind == CommandKind.Retreat)
            {
                _queue.RequestRetreat();
                return CommandResult.Accepted;
            }

            // 소모품은 유닛이 아니라 파티가 쓴다 — 유닛별 대기 명령과 섞이지 않게 따로 받는다.
            if (command.Kind == CommandKind.UseItem) return _items.Request(command.ItemId, _unitsById);

            if (!_byId.TryGetValue(command.ActorId, out UnitState actor)) return CommandResult.RejectedUnknownUnit;
            if (actor.Side != Side.Ally || command.Kind == CommandKind.BasicAttack) return CommandResult.RejectedNotControllable;
            // 큐는 사용자 경로 전용이다 — 자동 판단이 만든 명령은 그 틱에 바로 실행되고 큐를 거치지 않는다(계획서 D7).
            // 이 가드가 있어 큐에서 나온 기술은 항상 강제 발동이고, 재사용 대기 검사를 따로 갈라 둘 필요가 없다.
            if (!command.IsForced) return CommandResult.RejectedNotControllable;
            if (actor.Condition != UnitCondition.Normal) return CommandResult.RejectedIncapacitated;
            if (actor.IsRescuing) return CommandResult.RejectedBusy;

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
                    if (!_rescues.CanStart(actor, command.TargetId)) return CommandResult.RejectedInvalidTarget;
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

            _items.UseRequested(_unitsById, Tick, events);
            _resolver.ApplyScatterLoss(Tick, events);
            AdvanceCooldowns();
            // 이동이 게이지 충전보다 앞에 와야 사거리 판정 전에 이번 틱의 위치가 확정된다.
            AdvanceSection();
            ActionGaugeScheduler.Fill(_units, _unitsById, _balance);
            ResolveActions(events);
            // 구조 진행은 행동 뒤에 센다 — 같은 틱에 맞은 구조자는 완료보다 초기화가 먼저다(계획서 Phase D).
            _rescues.Advance(Tick, events);

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
            return new BattleSnapshot(this, _units.Length, _items.KindCount);
        }

        public void FillSnapshot(BattleSnapshot target)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));
            if (!ReferenceEquals(target.Owner, this)) throw new ArgumentException("다른 전투에서 만든 스냅샷 버퍼입니다.", nameof(target));

            target.Tick = Tick;
            target.Outcome = Outcome;
            target.CurrentWave = CurrentWave;
            target.WaveCount = WaveCount;
            target.AnchorX = AnchorX;
            target.EnemyAnchorX = EnemyAnchorX;
            target.RescueTicks = _balance.RescueTicks;
            for (int i = 0; i < _items.KindCount; i++)
            {
                target.SetItem(i, new ItemSnapshot(_items.IdAt(i), _items.RemainingAt(i)));
            }

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
                    unit.Setup.IsBoss,
                    unit.PosX,
                    unit.Stance,
                    unit.IsFlanking,
                    unit.RescueTargetId,
                    unit.RescueProgress));
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
                // 사거리 밖이면 명령도 게이지도 건드리지 않고 넘긴다 — 사용자가 누른 기술이 조용히 사라지지 않게(계획서 D6).
                if (!CanActFromHere(unit)) continue;

                // 사거리 관문은 "때릴 상대가 있는가"만 본다. 대상별 사거리 필터는 TargetSelector 가 맡는다(계획서 Phase C).
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

            return AutoPilot.Choose(unit, _random);
        }

        /// <summary>
        /// 지금 자리에서 행동이 성립하는가. 사거리 안에 상대가 있으면 성립한다.
        /// 상대가 전장에 아예 없으면(같은 틱에 마지막 적이 쓰러진 경우) 기존처럼 행동한다 — 사거리 때문에 건너뛰는 것과 구분한다.
        /// 위치와 무관한 대기 명령(구조 시작)은 사거리를 보지 않는다.
        /// </summary>
        private bool CanActFromHere(UnitState unit)
        {
            if (Engagement.HasReachableOpponent(unit, _unitsById, _lane)) return true;
            if (!Engagement.HasOpponentOnField(unit, _unitsById)) return true;

            // 실행 가능 여부까지 본다 — 조건이 깨진 구조 명령으로 관문을 통과하면
            // 폴백 기본 공격이 사거리 밖 상대를 때린다.
            return _queue.TryPeek(unit.Id, out BattleCommand pending)
                && IsPositionIndependent(pending.Kind)
                && IsExecutable(unit, pending);
        }

        /// <summary>
        /// 위치와 무관한 명령은 구조뿐이다. 과부하는 공격이라 대상이 사거리 밖이면 쿨타임과 폭주 위험만 치르고
        /// 아무도 맞히지 못한다 — 사거리 안에 들 때까지 기다려야 한다.
        /// </summary>
        private static bool IsPositionIndependent(CommandKind kind)
        {
            return kind == CommandKind.Rescue;
        }

        private bool IsExecutable(UnitState unit, BattleCommand command)
        {
            switch (command.Kind)
            {
                case CommandKind.UseSkill: return unit.Cooldown == 0;
                case CommandKind.Overload: return unit.Cooldown == 0 && CanOverload(unit);
                case CommandKind.Rescue: return _rescues.CanStart(unit, command.TargetId);
                default: return false;
            }
        }

        private static bool CanOverload(UnitState unit)
        {
            return OverloadResolver.CanOverload(unit.Setup.Catalyst, unit.Setup.Skill);
        }

        private int OverloadRisk(UnitState unit)
        {
            if (!CanOverload(unit)) return 0;
            return OverloadResolver.RiskLevel(unit.Setup.Catalyst.OverloadExcessPermille(unit.Mnd, _balance), _balance);
        }

        /// <summary>진형 기준점을 전진시키고 모든 유닛을 자세까지 반영한 목표 자리 쪽으로 한 걸음 옮긴다.</summary>
        private void AdvanceSection()
        {
            int advanceFu = SectionProgress.AdvanceFu(_unitsById, _lane, AnchorX, EnemyAnchorX);
            if (advanceFu > 0) AnchorX = CombatScale.ClampToInt((long)AnchorX + advanceFu);
            Movement.Advance(_unitsById, AnchorX, EnemyAnchorX, _lane);
        }

        private static int[] BuildWaveAnchors(BattleSetup setup)
        {
            var anchors = new int[setup.Waves.Count];
            long cumulative = 0;
            for (int wave = 0; wave < anchors.Length; wave++)
            {
                cumulative += setup.WaveApproachFu[wave];
                anchors[wave] = CombatScale.ClampToInt(cumulative);
            }

            return anchors;
        }

        /// <summary>
        /// 모든 유닛의 좌표를 전투 시작 전에 확정한다. 아직 등장하지 않은 조우의 적도 함께 두어,
        /// 표시 계층이 등장 순간에 낡은 좌표에서 미끄러지지 않게 한다.
        /// </summary>
        private void PlaceUnits()
        {
            for (int i = 0; i < _units.Length; i++)
            {
                UnitState unit = _units[i];
                int anchorX = unit.Side == Side.Ally ? AnchorX : _waveAnchorX[unit.WaveIndex];
                unit.PosX = Movement.SlotFu(unit, anchorX, _lane);
            }
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
            EnemyAnchorX = _waveAnchorX[wave];
            for (int i = 0; i < _units.Length; i++)
            {
                if (_units[i].WaveIndex == wave) _units[i].IsOnField = true;
            }

            // 아군은 조우마다 자세를 다시 뽑고, 이 조우의 적은 처음 뽑는다.
            Tactics.DecideEncounter(_unitsById, wave, _lane, _random);

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
            // 끝난 전투에 구조가 남지 않게 지운다. 흩어진 채 끝나면 빈사로 확정한 뒤 다시 판정한다 —
            // 빈사자가 늘기만 하므로 이미 성립한 종료 조건은 사라지지 않는다.
            _rescues.CancelAll();
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
            hasher.Add(AnchorX);
            hasher.Add(EnemyAnchorX);
            hasher.Add(_queue.RetreatRequested ? 1 : 0);
            _items.AddTo(hasher);
            foreach (UnitState unit in _units)
            {
                hasher.Add(unit.Id);
                hasher.Add(unit.PosX);
                hasher.Add((int)unit.Stance);
                hasher.Add(unit.IsFlanking ? 1 : 0);
                hasher.Add(unit.IsOnField ? 1 : 0);
                hasher.Add((int)unit.Condition);
                hasher.Add(unit.Hp);
                hasher.Add(unit.Mnd);
                hasher.Add(unit.ActionGauge);
                hasher.Add(unit.Cooldown);
                hasher.Add(unit.MagitoolDurability);
                hasher.Add(unit.RescueTargetId);
                hasher.Add(unit.RescueProgress);
                // 대기 명령은 사거리 판정으로 수명이 길어졌으므로 해시에 넣는다.
                // 딕셔너리를 순회하지 않고 유닛 순서대로 조회해 순서를 고정한다.
                bool hasPending = _queue.TryPeek(unit.Id, out BattleCommand pending);
                hasher.Add(hasPending ? (int)pending.Kind : -1);
                hasher.Add(hasPending ? pending.TargetId : BattleEvent.NoUnit);
                hasher.Add(hasPending && pending.IsForced ? 1 : 0);
            }

            return hasher.Value;
        }
    }
}
