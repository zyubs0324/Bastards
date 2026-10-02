namespace TAoB.Combat
{
    public enum CommandKind
    {
        BasicAttack,
        UseSkill,
        Overload,
        Rescue,
        Retreat,
    }

    public enum CommandResult
    {
        Accepted,

        /// <summary>같은 유닛의 대기 명령을 대체했다.</summary>
        Replaced,
        RejectedBattleOver,
        RejectedUnknownUnit,

        /// <summary>적 유닛이거나 사용자가 내릴 수 없는 명령(기본 공격).</summary>
        RejectedNotControllable,

        /// <summary>빈사·흩어짐 상태.</summary>
        RejectedIncapacitated,
        RejectedCooldown,
        RejectedCannotOverload,
        RejectedInvalidTarget,
    }

    /// <summary>자동 판단과 사용자 입력이 규칙 모듈로 들어오는 단일 형식(계획서 Phase 2 명령 경로 단일화).</summary>
    public readonly struct BattleCommand
    {
        private BattleCommand(CommandKind kind, int actorId, int targetId, bool isForced)
        {
            Kind = kind;
            ActorId = actorId;
            TargetId = targetId;
            IsForced = isForced;
        }

        public CommandKind Kind { get; }
        public int ActorId { get; }

        /// <summary>구조 대상. 대상이 없는 명령은 <see cref="BattleEvent.NoUnit"/>.</summary>
        public int TargetId { get; }

        /// <summary>
        /// 사용자가 직접 지시했는가(계획서 D7). 재사용 대기는 강제 발동 전용이라
        /// 이 표식이 참일 때만 대기가 걸린다(<see cref="BattleSimulation"/> 의 명령 큐가 거짓인 명령을 거른다).
        /// 자동 판단이 만든 명령은 거짓이다.
        /// </summary>
        public bool IsForced { get; }

        public static BattleCommand BasicAttack(int actorId) => new BattleCommand(CommandKind.BasicAttack, actorId, BattleEvent.NoUnit, false);

        /// <summary>사용자의 기술 강제 발동. 재사용 대기를 쓰고 다시 걸린다.</summary>
        public static BattleCommand UseSkill(int actorId) => new BattleCommand(CommandKind.UseSkill, actorId, BattleEvent.NoUnit, true);

        /// <summary>자동 판단의 기술 사용. 빈도 스탯으로 나가며 재사용 대기와 무관하다.</summary>
        internal static BattleCommand AutoSkill(int actorId) => new BattleCommand(CommandKind.UseSkill, actorId, BattleEvent.NoUnit, false);

        public static BattleCommand Overload(int actorId) => new BattleCommand(CommandKind.Overload, actorId, BattleEvent.NoUnit, true);

        public static BattleCommand Rescue(int rescuerId, int targetId) => new BattleCommand(CommandKind.Rescue, rescuerId, targetId, true);

        public static BattleCommand Retreat() => new BattleCommand(CommandKind.Retreat, BattleEvent.NoUnit, BattleEvent.NoUnit, true);
    }
}
