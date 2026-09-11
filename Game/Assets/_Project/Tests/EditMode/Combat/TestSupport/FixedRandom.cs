using System;
using System.Collections.Generic;

namespace TAoB.Combat.Tests
{
    /// <summary>
    /// 미리 정한 값을 순서대로 돌려주는 테스트용 난수. 확률 분기를 강제하는 데 쓴다.
    /// </summary>
    internal sealed class FixedRandom : IRandom
    {
        private readonly Queue<int> _values;

        public FixedRandom(params int[] values)
        {
            _values = new Queue<int>(values);
        }

        public int Remaining => _values.Count;

        public int Next(int maxExclusive)
        {
            if (_values.Count == 0)
            {
                throw new InvalidOperationException("FixedRandom 에 준비된 값이 더 없습니다.");
            }

            int value = _values.Dequeue();
            if (value < 0 || value >= maxExclusive)
            {
                throw new InvalidOperationException($"준비된 값 {value} 가 범위 [0, {maxExclusive}) 를 벗어났습니다.");
            }

            return value;
        }
    }
}
