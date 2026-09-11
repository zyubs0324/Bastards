namespace TAoB.Combat
{
    /// <summary>
    /// 전투 상태의 정수 값들을 FNV-1a 64비트로 누적한다.
    /// 실행마다 달라질 수 있는 GetHashCode 대신 써서 에디터·기기 결과를 비교한다.
    /// </summary>
    public sealed class StateHasher
    {
        private const ulong OffsetBasis = 14695981039346656037UL;
        private const ulong Prime = 1099511628211UL;
        private const int BytesPerInt = 4;
        private const int BitsPerByte = 8;
        private const uint ByteMask = 0xFF;

        public StateHasher()
        {
            Value = OffsetBasis;
        }

        public ulong Value { get; private set; }

        public void Reset()
        {
            Value = OffsetBasis;
        }

        public void Add(int value)
        {
            uint bits = unchecked((uint)value);
            ulong hash = Value;
            for (int i = 0; i < BytesPerInt; i++)
            {
                hash ^= (bits >> (BitsPerByte * i)) & ByteMask;
                hash = unchecked(hash * Prime);
            }

            Value = hash;
        }
    }
}
