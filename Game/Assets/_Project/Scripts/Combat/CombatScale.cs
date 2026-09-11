using System;

namespace TAoB.Combat
{
    /// <summary>
    /// 규칙 모듈의 정수 배율 단위와 수치 상한. 에디터와 기기의 결과를 맞추려고 소수 대신 정수 배율을 쓴다.
    /// </summary>
    public static class CombatScale
    {
        /// <summary>배율 1.0 = 1000.</summary>
        public const int Permille = 1000;

        /// <summary>확률 100% = 10000 (만분율).</summary>
        public const int ChanceRange = 10000;

        /// <summary>스탯·장비·밸런스 정수 값의 상한. 데이터 실수로 곱셈이 넘치지 않게 생성자에서 막는다.</summary>
        public const int MaxValue = 1000000;

        /// <summary>배율 상한(×100).</summary>
        public const int MaxPermille = 100000;

        public static int ClampToInt(long value)
        {
            if (value > int.MaxValue) return int.MaxValue;
            if (value < int.MinValue) return int.MinValue;
            return (int)value;
        }
    }

    internal static class Guard
    {
        public static void Range(int value, int min, int max, string name)
        {
            if (value < min || value > max)
            {
                throw new ArgumentException($"{name} 은(는) {min}~{max} 범위여야 합니다. 값: {value}", name);
            }
        }
    }
}
