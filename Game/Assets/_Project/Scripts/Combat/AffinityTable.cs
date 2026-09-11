using System;
using System.Collections.Generic;

namespace TAoB.Combat
{
    /// <summary>임시 직업 3종(기획서 4-4 확정 전 가칭).</summary>
    public enum Job
    {
        Knight,
        Mage,
        Perceptive,
    }

    public enum AffinityLevel
    {
        Weak,
        Normal,
        Strong,
    }

    public readonly struct AffinityEntry
    {
        public AffinityEntry(Job attacker, Job defender, AffinityLevel level)
        {
            Attacker = attacker;
            Defender = defender;
            Level = level;
        }

        public Job Attacker { get; }
        public Job Defender { get; }
        public AffinityLevel Level { get; }
    }

    /// <summary>
    /// 공격 직업 → 방어 직업의 상성(기획서 4-1). 방향이 있으며, 등록되지 않은 조합은 보통이다.
    /// </summary>
    public sealed class AffinityTable
    {
        // 열거형 키 사전의 박싱을 피하려고 두 직업을 정수 하나로 합친다.
        private const int JobKeyStride = 1024;
        private readonly Dictionary<int, AffinityLevel> _levels = new Dictionary<int, AffinityLevel>();

        public AffinityTable(IEnumerable<AffinityEntry> entries)
        {
            if (entries == null) throw new ArgumentNullException(nameof(entries));

            foreach (AffinityEntry entry in entries)
            {
                int key = Key(entry.Attacker, entry.Defender);
                if (_levels.ContainsKey(key))
                {
                    throw new ArgumentException($"상성 {entry.Attacker}→{entry.Defender} 가 중복 등록됐습니다.", nameof(entries));
                }

                _levels.Add(key, entry.Level);
            }
        }

        public AffinityLevel Get(Job attacker, Job defender)
        {
            return _levels.TryGetValue(Key(attacker, defender), out AffinityLevel level) ? level : AffinityLevel.Normal;
        }

        private static int Key(Job attacker, Job defender)
        {
            return (int)attacker * JobKeyStride + (int)defender;
        }
    }
}
