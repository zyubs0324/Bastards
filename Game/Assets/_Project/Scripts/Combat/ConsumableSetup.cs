namespace TAoB.Combat
{
    /// <summary>전투에 들고 들어가는 소모품 한 종류(계획서 Phase D). 지금은 HP 회복 한 가지 효과만 있다.</summary>
    public sealed class ConsumableSetup
    {
        /// <summary>한 종류를 들고 갈 수 있는 최대 개수.</summary>
        public const int MaxCount = 99;

        public ConsumableSetup(int id, int healAmount, int count)
        {
            Guard.Range(id, 0, CombatScale.MaxValue, nameof(id));
            Guard.Range(healAmount, 1, CombatScale.MaxValue, nameof(healAmount));
            Guard.Range(count, 1, MaxCount, nameof(count));

            Id = id;
            HealAmount = healAmount;
            Count = count;
        }

        public int Id { get; }
        public int HealAmount { get; }

        /// <summary>전투 시작 때의 개수. 남은 개수는 전투가 소유한다(<see cref="BattleSnapshot.Items"/>).</summary>
        public int Count { get; }
    }
}
