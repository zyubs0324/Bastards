namespace TAoB.Combat
{
    public enum TargetRule
    {
        /// <summary>앞열의 가장 작은 ID, 앞열이 비면 뒷열.</summary>
        FrontFirst,

        /// <summary>현재 HP 가 가장 낮은 대상, 같으면 작은 ID.</summary>
        LowestHp,

        /// <summary>살아 있는 상대 전원.</summary>
        All,
    }

    /// <summary>캐릭터 고유 기술 1개의 정의(수직 슬라이스 계획서 Phase 2).</summary>
    public sealed class SkillDefinition
    {
        public SkillDefinition(int powerPermille, TargetRule targetRule, bool canOverload, int cooldownTicks)
        {
            Guard.Range(powerPermille, 0, CombatScale.MaxPermille, nameof(powerPermille));
            Guard.Range(cooldownTicks, 0, CombatScale.MaxValue, nameof(cooldownTicks));

            PowerPermille = powerPermille;
            TargetRule = targetRule;
            CanOverload = canOverload;
            CooldownTicks = cooldownTicks;
        }

        public int PowerPermille { get; }
        public TargetRule TargetRule { get; }
        public bool CanOverload { get; }
        public int CooldownTicks { get; }
    }
}
