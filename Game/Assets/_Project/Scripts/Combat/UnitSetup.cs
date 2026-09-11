using System;

namespace TAoB.Combat
{
    public enum Side
    {
        Ally,
        Enemy,
    }

    /// <summary>전투에 들어가는 유닛 한 명의 정의. 전투 중 바뀌는 값은 내부 상태가 따로 가진다.</summary>
    public sealed class UnitSetup
    {
        public UnitSetup(
            int id,
            Side side,
            Job job,
            Row row,
            Stats stats,
            SkillDefinition skill,
            Catalyst catalyst,
            Magitool magitool,
            bool isBoss)
        {
            if (id < 0) throw new ArgumentException("0 이상이어야 합니다.", nameof(id));

            Id = id;
            Side = side;
            Job = job;
            Row = row;
            Stats = stats;
            Skill = skill ?? throw new ArgumentNullException(nameof(skill));
            Catalyst = catalyst;
            Magitool = magitool;
            IsBoss = isBoss;
        }

        public int Id { get; }
        public Side Side { get; }
        public Job Job { get; }
        public Row Row { get; }
        public Stats Stats { get; }
        public SkillDefinition Skill { get; }

        /// <summary>없으면 null.</summary>
        public Catalyst Catalyst { get; }

        /// <summary>없으면 null.</summary>
        public Magitool Magitool { get; }
        public bool IsBoss { get; }
    }
}
