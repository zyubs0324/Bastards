using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace TAoB.Combat
{
    public enum UnitCondition
    {
        Normal,
        Scattered,
        Downed,
    }

    /// <summary>표시 계층에 넘기는 유닛 한 명의 읽기 전용 값.</summary>
    public readonly struct UnitSnapshot
    {
        public UnitSnapshot(
            int id,
            Side side,
            Row row,
            bool isOnField,
            UnitCondition condition,
            int hp,
            int maxHp,
            int actionGauge,
            int actionGaugeMax,
            int cooldownRemaining,
            int overloadRisk,
            int mnd,
            bool hasMagitool,
            int magitoolDurability,
            int magitoolMaxDurability,
            bool isBoss,
            int posX,
            Stance stance,
            bool isFlanking)
        {
            Id = id;
            Side = side;
            Row = row;
            IsOnField = isOnField;
            Condition = condition;
            Hp = hp;
            MaxHp = maxHp;
            ActionGauge = actionGauge;
            ActionGaugeMax = actionGaugeMax;
            CooldownRemaining = cooldownRemaining;
            OverloadRisk = overloadRisk;
            Mnd = mnd;
            HasMagitool = hasMagitool;
            MagitoolDurability = magitoolDurability;
            MagitoolMaxDurability = magitoolMaxDurability;
            IsBoss = isBoss;
            PosX = posX;
            Stance = stance;
            IsFlanking = isFlanking;
        }

        public int Id { get; }
        public Side Side { get; }
        public Row Row { get; }

        /// <summary>아직 시작되지 않은 웨이브의 적은 false.</summary>
        public bool IsOnField { get; }
        public UnitCondition Condition { get; }
        public int Hp { get; }
        public int MaxHp { get; }

        /// <summary>0~ActionGaugeMax 로 잘라 표시용으로 담는다.</summary>
        public int ActionGauge { get; }
        public int ActionGaugeMax { get; }
        public int CooldownRemaining { get; }

        /// <summary>지금 과부하하면 걸리는 위험 단계 0~3. 과부하할 수 없으면 0.</summary>
        public int OverloadRisk { get; }
        public int Mnd { get; }
        public bool HasMagitool { get; }
        public int MagitoolDurability { get; }
        public int MagitoolMaxDurability { get; }
        public bool IsBoss { get; }

        /// <summary>전장 1차원 좌표(fu). 표시 계층이 두 스냅샷 사이를 보간한다.</summary>
        public int PosX { get; }

        /// <summary>이번 조우의 자세. 성향 표식을 그리는 데 쓴다.</summary>
        public Stance Stance { get; }

        /// <summary>측면 침투 중인 적인가.</summary>
        public bool IsFlanking { get; }
    }

    /// <summary>
    /// 호출자가 소유하는 재사용 스냅샷 버퍼. <see cref="BattleSimulation.FillSnapshot"/> 이 값을 복사해 채운다.
    /// </summary>
    public sealed class BattleSnapshot
    {
        private readonly UnitSnapshot[] _units;
        private readonly ReadOnlyCollection<UnitSnapshot> _readOnlyUnits;

        internal BattleSnapshot(BattleSimulation owner, int unitCount)
        {
            Owner = owner;
            _units = new UnitSnapshot[unitCount];
            // 배열을 그대로 내주면 형변환으로 내부 버퍼를 덮어쓸 수 있어 읽기 전용 래퍼를 한 번만 만들어 둔다.
            _readOnlyUnits = Array.AsReadOnly(_units);
        }

        internal BattleSimulation Owner { get; }

        public int Tick { get; internal set; }
        public BattleOutcome Outcome { get; internal set; }
        public int CurrentWave { get; internal set; }
        public int WaveCount { get; internal set; }

        /// <summary>아군 진형 기준점(fu). 배경 스크롤은 이 값에서만 구한다 — 별도 시간 누적을 두면 배속에서 어긋난다.</summary>
        public int AnchorX { get; internal set; }

        /// <summary>지금 조우 적의 진형 기준점(fu).</summary>
        public int EnemyAnchorX { get; internal set; }
        public IReadOnlyList<UnitSnapshot> Units => _readOnlyUnits;

        internal void SetUnit(int index, UnitSnapshot unit)
        {
            _units[index] = unit;
        }
    }
}
