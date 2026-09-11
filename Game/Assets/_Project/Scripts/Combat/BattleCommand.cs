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
        private BattleCommand(CommandKind kind, int actorId, int targetId)
        {
            Kind = kind;
            ActorId = actorId;
            TargetId = targetId;
        }

        public CommandKind Kind { get; }
        public int ActorId { get; }

        /// <summary>구조 대상. 대상이 없는 명령은 <see cref="BattleEvent.NoUnit"/>.</summary>
        public int TargetId { get; }

        public static BattleCommand BasicAttack(int actorId) => new BattleCommand(CommandKind.BasicAttack, actorId, BattleEvent.NoUnit);

        public static BattleCommand UseSkill(int actorId) => new BattleCommand(CommandKind.UseSkill, actorId, BattleEvent.NoUnit);

        public static BattleCommand Overload(int actorId) => new BattleCommand(CommandKind.Overload, actorId, BattleEvent.NoUnit);

        public static BattleCommand Rescue(int rescuerId, int targetId) => new BattleCommand(CommandKind.Rescue, rescuerId, targetId);

        public static BattleCommand Retreat() => new BattleCommand(CommandKind.Retreat, BattleEvent.NoUnit, BattleEvent.NoUnit);
    }
}
