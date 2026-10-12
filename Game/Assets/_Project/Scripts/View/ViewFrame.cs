namespace TAoB.View
{
    /// <summary>한 프레임에 모든 유닛 표시가 함께 받는 값. 유닛마다 같은 인수를 늘어놓지 않으려고 묶는다.</summary>
    internal readonly struct ViewFrame
    {
        public ViewFrame(float alpha, float dt, int speed, bool isBattleOn, int rescueTicks)
        {
            Alpha = alpha;
            Dt = dt;
            GameDt = dt * speed;
            IsBattleOn = isBattleOn;
            RescueTicks = rescueTicks;
        }

        /// <summary>직전·현재 스냅샷 사이의 보간 위치(0~1).</summary>
        public float Alpha { get; }

        /// <summary>실제로 흐른 시간(초). 연출(돌진·흔들림·빛남)은 배속과 무관하게 이 시간으로 흐른다.</summary>
        public float Dt { get; }

        /// <summary>배속을 곱한 시간(초). 전장이 움직이는 속도에 맞춰야 하는 표시가 쓴다 — 배속에서는 진형도 그만큼 빨리 전진한다.</summary>
        public float GameDt { get; }

        /// <summary>전투가 끝나면 강제 발동 표시(빛남)를 끈다.</summary>
        public bool IsBattleOn { get; }

        /// <summary>구조가 끝나는 진행 틱. 구조자 위에 남은 시간을 적는 데 쓴다.</summary>
        public int RescueTicks { get; }
    }
}
