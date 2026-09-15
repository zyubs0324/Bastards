using System;

namespace TAoB.View
{
    /// <summary>
    /// 프레임 시간을 고정 틱 횟수로 바꾼다(계획서 Phase 4 틱 드라이버). 배속은 틱 횟수만 늘리고 규칙 모듈에는 전달하지 않는다.
    /// </summary>
    public sealed class TickDriver
    {
        /// <summary>한 프레임에 돌리는 틱 상한. 넘치는 몫은 누적해 다음 프레임에 쓴다(느린 프레임에서 한 번에 몰아 돌지 않게).</summary>
        public const int MaxTicksPerFrame = 20;

        private readonly int _ticksPerSecond;

        // 초가 아니라 틱 단위로 누적한다. 실행한 틱 수(정수)를 빼는 연산이 정확해서 0.1초씩 빼며 생기는 오차가 쌓이지 않는다.
        private float _pendingTicks;

        public TickDriver(int ticksPerSecond)
        {
            if (ticksPerSecond <= 0) throw new ArgumentException("1 이상이어야 합니다.", nameof(ticksPerSecond));
            _ticksPerSecond = ticksPerSecond;
        }

        /// <summary>직전 틱과 현재 틱 사이의 진행 비율(0~1). 표시 보간용.</summary>
        public float Alpha => Math.Min(1f, _pendingTicks);

        /// <summary>이번 프레임에 실행할 틱 수.</summary>
        public int Advance(float deltaSeconds, int speed)
        {
            _pendingTicks += Math.Max(0f, deltaSeconds) * Math.Max(1, speed) * _ticksPerSecond;
            int ticks = (int)_pendingTicks;
            if (ticks > MaxTicksPerFrame) ticks = MaxTicksPerFrame;
            _pendingTicks -= ticks;
            return ticks;
        }

        public void Reset()
        {
            _pendingTicks = 0f;
        }
    }
}
