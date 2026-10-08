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
            int skillChance,
            Preference preference,
            int formationChance,
            SkillDefinition skill,
            Catalyst catalyst,
            Magitool magitool,
            bool isBoss)
        {
            if (id < 0) throw new ArgumentException("0 이상이어야 합니다.", nameof(id));
            Guard.Range(skillChance, 0, CombatScale.ChanceRange, nameof(skillChance));
            Guard.Range(formationChance, 0, CombatScale.ChanceRange, nameof(formationChance));
            if (!Enum.IsDefined(typeof(Preference), preference))
            {
                throw new ArgumentException($"알 수 없는 성향입니다. 값: {(int)preference}", nameof(preference));
            }

            Id = id;
            Side = side;
            Job = job;
            Row = row;
            Stats = stats;
            SkillChance = skillChance;
            Preference = preference;
            FormationChance = formationChance;
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

        /// <summary>
        /// 행동할 때마다 기술이 나갈 확률(만분율). 숨은 스탯이라 화면에 보이지 않는다(계획서 D7).
        /// 기획 대역(자주~매우 낮음)은 데이터 로더가 좁히고, 규칙 모듈은 0~<see cref="CombatScale.ChanceRange"/> 만 요구한다.
        /// </summary>
        public int SkillChance { get; }

        /// <summary>조우마다 진형유지 값을 통과했을 때 따르는 성향(계획서 Phase C).</summary>
        public Preference Preference { get; }

        /// <summary>
        /// 숨은 진형유지 값(만분율). 높을수록 조우마다 성향대로 움직이고, 낮을수록 흐트러져 자리를 지킨다.
        /// 화면에는 보이지 않는다.
        /// </summary>
        public int FormationChance { get; }
        public SkillDefinition Skill { get; }

        /// <summary>없으면 null.</summary>
        public Catalyst Catalyst { get; }

        /// <summary>없으면 null.</summary>
        public Magitool Magitool { get; }
        public bool IsBoss { get; }
    }
}
