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

        public BattleSetup(
            BalanceConfig balance,
            AffinityTable affinity,
            IReadOnlyList<UnitSetup> allies,
            IReadOnlyList<IReadOnlyList<UnitSetup>> waves,
            int timeLimitTicks,
            PreconditionKind precondition)
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
            TimeLimitTicks = timeLimitTicks;
            Precondition = precondition;
        }

        public BalanceConfig Balance { get; }
        public AffinityTable Affinity { get; }
        public IReadOnlyList<UnitSetup> Allies { get; }
        public IReadOnlyList<IReadOnlyList<UnitSetup>> Waves { get; }
        public int TimeLimitTicks { get; }
        public PreconditionKind Precondition { get; }

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
