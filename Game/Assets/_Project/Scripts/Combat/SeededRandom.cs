using System;

namespace TAoB.Combat
{
    /// <summary>
    /// 시드로 재현 가능한 xorshift32 난수.
    /// 에디터와 기기에서 같은 수열을 내기 위해 엔진·표준 라이브러리 난수를 쓰지 않는다.
    /// </summary>
    public sealed class SeededRandom : IRandom
    {
        // 0 은 xorshift 의 고정점(계속 0 만 나옴)이라 다른 값으로 바꾼다.
        private const uint ZeroSeedReplacement = 0x9E3779B9u;

        public SeededRandom(uint seed)
        {
            State = seed == 0 ? ZeroSeedReplacement : seed;
        }

        public uint State { get; private set; }

        public int Next(int maxExclusive)
        {
            if (maxExclusive <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxExclusive), maxExclusive, "양수여야 합니다.");
            }

            uint x = State;
            x ^= x << 13;
            x ^= x >> 17;
            x ^= x << 5;
            State = x;
            return (int)(x % (uint)maxExclusive);
        }
    }
}
