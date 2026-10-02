namespace TAoB.Combat
{
    /// <summary>
    /// 전투 중 바뀌는 유닛 상태. 전투 시작 시 한 번 만들고 <see cref="BattleSimulation"/> 안에서만 갱신한다.
    /// 밖으로는 <see cref="UnitSnapshot"/> 으로만 노출한다.
    /// </summary>
    internal sealed class UnitState
    {
        /// <summary>아군의 웨이브 번호.</summary>
        public const int AllyWave = -1;

        public UnitState(UnitSetup setup, int waveIndex)
        {
            Setup = setup;
            WaveIndex = waveIndex;
            Hp = setup.Stats.Hp;
            Mnd = setup.Stats.Mnd;
            MagitoolDurability = setup.Magitool != null ? setup.Magitool.MaxDurability : 0;
            Condition = UnitCondition.Normal;
            // 아군(-1)과 첫 웨이브(0)는 전투 시작부터 전장에 있고, 뒤 웨이브는 앞 웨이브가 정리되면 들어온다.
            IsOnField = waveIndex <= 0;
        }

        public UnitSetup Setup { get; }
        public int WaveIndex { get; }

        public int Id => Setup.Id;
        public Side Side => Setup.Side;
        public Job Job => Setup.Job;
        public Row Row => Setup.Row;
        public int MaxHp => Setup.Stats.Hp;

        public int Hp { get; set; }
        public int Mnd { get; set; }

        /// <summary>전장 1차원 좌표(fu). 아군은 +x 로 전진한다. Y 는 표시 계층 전담(계획서 D3).</summary>
        public int PosX { get; set; }
        public int ActionGauge { get; set; }
        public int Cooldown { get; set; }
        public int MagitoolDurability { get; set; }
        public UnitCondition Condition { get; set; }
        public bool IsOnField { get; set; }

        public bool CanAct => IsOnField && Condition == UnitCondition.Normal;

        /// <summary>흩어진 유닛도 공격받을 수 있다(행동만 못 한다).</summary>
        public bool IsTargetable => IsOnField && Condition != UnitCondition.Downed;
    }
}
