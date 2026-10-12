using System;
using System.Collections.Generic;

namespace TAoB.Combat
{
    public enum PreconditionKind
    {
        None,

        /// <summary>아군이 한 명이라도 빈사가 되면 패배.</summary>
        NoAllyDowned,
    }

    /// <summary>전투 1판의 입력. 생성 시 검증해 잘못된 편성으로는 전투를 시작할 수 없게 한다.</summary>
    public sealed class BattleSetup
    {
        /// <summary>
        /// 제한 시간 상한(600초). 데이터 실수로 전투가 사실상 끝나지 않는 일을 막는다. 데이터 로더도 이 값을 쓴다.
        /// </summary>
        public const int MaxTimeLimitTicks = 600 * BattleSimulation.TicksPerSecond;

        /// <summary>조우 1회 접근 거리 상한(fu). <see cref="LaneConfig"/> 의 거리 상한과 같은 자리수로 맞춘다.</summary>
        public const int MaxApproachFu = 20000;

        /// <summary>
        /// 전장 좌표 상한(fu). 접근 거리 합이 이 값을 넘으면 거부해 좌표·거리 계산이 int 범위 안에 남는다.
        /// </summary>
        public const int MaxFieldSpanFu = 10000000;

        /// <summary>들고 갈 수 있는 소모품 종류 수.</summary>
        public const int MaxConsumableKinds = 10;

        public BattleSetup(
            BalanceConfig balance,
            AffinityTable affinity,
            IReadOnlyList<UnitSetup> allies,
            IReadOnlyList<IReadOnlyList<UnitSetup>> waves,
            IReadOnlyList<int> waveApproachFu,
            int timeLimitTicks,
            PreconditionKind precondition,
            IReadOnlyList<ConsumableSetup> consumables)
        {
            Balance = balance ?? throw new ArgumentNullException(nameof(balance));
            Affinity = affinity ?? throw new ArgumentNullException(nameof(affinity));
            if (allies == null) throw new ArgumentNullException(nameof(allies));
            if (waves == null) throw new ArgumentNullException(nameof(waves));
            Guard.Range(timeLimitTicks, 1, MaxTimeLimitTicks, nameof(timeLimitTicks));

            var ids = new HashSet<int>();
            ValidateGroup(allies, Side.Ally, "allies", ids, balance);
            if (waves.Count == 0) throw new ArgumentException("웨이브가 하나 이상 있어야 합니다.", nameof(waves));

            var waveCopies = new List<IReadOnlyList<UnitSetup>>(waves.Count);
            for (int i = 0; i < waves.Count; i++)
            {
                ValidateGroup(waves[i], Side.Enemy, $"waves[{i}]", ids, balance);
                waveCopies.Add(new List<UnitSetup>(waves[i]).AsReadOnly());
            }

            Allies = new List<UnitSetup>(allies).AsReadOnly();
            Waves = waveCopies.AsReadOnly();
            WaveApproachFu = ValidateApproach(waveApproachFu, waveCopies.Count);
            TimeLimitTicks = timeLimitTicks;
            Precondition = precondition;
            Consumables = ValidateConsumables(consumables);
        }

        public BalanceConfig Balance { get; }
        public AffinityTable Affinity { get; }
        public IReadOnlyList<UnitSetup> Allies { get; }
        public IReadOnlyList<IReadOnlyList<UnitSetup>> Waves { get; }

        /// <summary>조우마다 그 적을 만나기까지 행군하는 거리(fu). 적 진형 기준점은 이 거리의 누적 합이다.</summary>
        public IReadOnlyList<int> WaveApproachFu { get; }
        public int TimeLimitTicks { get; }
        public PreconditionKind Precondition { get; }

        /// <summary>파티가 들고 들어가는 소모품. 없으면 빈 목록이다.</summary>
        public IReadOnlyList<ConsumableSetup> Consumables { get; }

        private static IReadOnlyList<ConsumableSetup> ValidateConsumables(IReadOnlyList<ConsumableSetup> consumables)
        {
            const string name = "consumables";
            if (consumables == null) throw new ArgumentNullException(name);
            if (consumables.Count > MaxConsumableKinds)
            {
                throw new ArgumentException($"소모품이 {consumables.Count}종입니다. {MaxConsumableKinds}종 이하여야 합니다.", name);
            }

            var ids = new HashSet<int>();
            foreach (ConsumableSetup item in consumables)
            {
                if (item == null) throw new ArgumentException("소모품 목록에 빈 항목이 있습니다.", name);
                if (!ids.Add(item.Id)) throw new ArgumentException($"소모품 ID {item.Id} 가 중복됩니다.", name);
            }

            return new List<ConsumableSetup>(consumables).AsReadOnly();
        }

        private static IReadOnlyList<int> ValidateApproach(IReadOnlyList<int> approach, int waveCount)
        {
            const string name = "waveApproachFu";
            if (approach == null) throw new ArgumentNullException(name);
            if (approach.Count != waveCount)
            {
                throw new ArgumentException($"접근 거리가 {approach.Count}개입니다. 조우 수 {waveCount} 와 같아야 합니다.", name);
            }

            long span = 0;
            for (int i = 0; i < approach.Count; i++)
            {
                Guard.Range(approach[i], 1, MaxApproachFu, $"{name}[{i}]");
                span += approach[i];
            }

            if (span > MaxFieldSpanFu)
            {
                throw new ArgumentException($"접근 거리 합이 {span} fu 입니다. {MaxFieldSpanFu} 이하여야 합니다.", name);
            }

            return new List<int>(approach).AsReadOnly();
        }

        private static void ValidateGroup(IReadOnlyList<UnitSetup> units, Side expectedSide, string name, HashSet<int> ids, BalanceConfig balance)
        {
            if (units == null || units.Count == 0) throw new ArgumentException($"{name} 에 유닛이 없습니다.", name);

            foreach (UnitSetup unit in units)
            {
                if (unit == null) throw new ArgumentException($"{name} 에 빈 항목이 있습니다.", name);
                if (unit.Side != expectedSide) throw new ArgumentException($"{name} 의 유닛 {unit.Id} 진영이 {unit.Side} 입니다. {expectedSide} 여야 합니다.", name);
                if (!ids.Add(unit.Id)) throw new ArgumentException($"유닛 ID {unit.Id} 가 중복됩니다.", name);
                if (unit.Magitool != null && !EquipRules.CanEquip(unit.Stats.Res, unit.Magitool, balance))
                {
                    throw new ArgumentException($"유닛 {unit.Id} 는 RES 가 부족해 마도구를 장착할 수 없습니다.", name);
                }
            }
        }
    }
}
