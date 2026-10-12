namespace TAoB.Combat
{
    public enum BattleEventKind
    {
        /// <summary>Detail = <see cref="CommandKind"/>, TargetId = 구조 대상.</summary>
        ActionStarted,

        /// <summary>Value = 피해량, Detail = 크리티컬이면 1.</summary>
        Damage,

        /// <summary>마도구 반발·폭주로 시전자가 받은 피해.</summary>
        SelfDamage,

        /// <summary>흩어짐 상태의 틱당 HP 감소.</summary>
        ScatterDamage,
        Downed,

        /// <summary>Value = 초과 비율(‰), Detail = <see cref="OverloadOutcome"/>.</summary>
        OverloadResolved,
        Scattered,

        /// <summary>Value = 상승 후 MND.</summary>
        Breakthrough,

        /// <summary>SourceId = 구조자, TargetId = 구조 대상. 구조가 끝나 정상으로 돌아왔다.</summary>
        Rescued,

        /// <summary>대기 명령을 실행할 수 없어 자동 판단으로 바꿨다. Detail = 원래 <see cref="CommandKind"/>.</summary>
        CommandFallback,
        MagitoolDepleted,

        /// <summary>Value = 시작된 웨이브 번호(0부터).</summary>
        WaveStarted,

        /// <summary>Detail = <see cref="BattleOutcome"/>.</summary>
        BattleEnded,

        /// <summary>SourceId = 구조자, TargetId = 구조 대상. 구조는 이때부터 정해진 틱이 걸린다.</summary>
        RescueStarted,

        /// <summary>구조자가 맞아 진행이 처음으로 돌아갔다. Value = 잃은 진행 틱.</summary>
        RescueInterrupted,

        /// <summary>구조자나 대상이 더는 구조할 수 없는 상태가 되어 구조가 끝났다.</summary>
        RescueCancelled,

        /// <summary>TargetId = 회복한 유닛, Value = 회복량, Detail = 소모품 ID.</summary>
        ItemUsed,
    }

    /// <summary>한 틱에 일어난 일 하나. 표시 계층은 이것으로 연출하고 규칙을 다시 계산하지 않는다.</summary>
    public readonly struct BattleEvent
    {
        public const int NoUnit = -1;

        public BattleEvent(BattleEventKind kind, int tick, int sourceId, int targetId, int value, int detail)
        {
            Kind = kind;
            Tick = tick;
            SourceId = sourceId;
            TargetId = targetId;
            Value = value;
            Detail = detail;
        }

        public BattleEventKind Kind { get; }
        public int Tick { get; }
        public int SourceId { get; }
        public int TargetId { get; }
        public int Value { get; }
        public int Detail { get; }
    }
}
