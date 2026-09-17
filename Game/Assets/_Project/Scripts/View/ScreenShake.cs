using UnityEngine;

namespace TAoB.View
{
    /// <summary>
    /// 전장 층(<c>Field</c>)만 감쇠하며 흔든다(계획서 Phase 4.5). HUD·개입 바·결과 화면은 <c>BattleRoot</c> 의 다른 자식이라
    /// 흔들리지 않는다 — 버튼을 누르는 위치가 어긋나지 않게 하기 위함(1차 계획 리뷰 반영).
    /// </summary>
    internal sealed class ScreenShake
    {
        private const float Frequency = 45f;
        private const float PhaseOffsetY = 1.9f;

        private readonly RectTransform _field;
        private float _elapsed = -1f;
        private float _duration;
        private float _magnitude;

        public ScreenShake(RectTransform field)
        {
            _field = field;
        }

        /// <summary>더 강하거나 같은 흔들림이 이미 진행 중이면 무시하고, 아니면 새로 시작한다.</summary>
        public void Trigger(float duration, float magnitude)
        {
            if (_elapsed >= 0f && _magnitude >= magnitude) return;
            _elapsed = 0f;
            _duration = duration;
            _magnitude = magnitude;
        }

        /// <summary>지속 시간이 지나면 정확히 원위치로 고정하고, 그 뒤 프레임에는 좌표를 다시 쓰지 않는다(perf).</summary>
        public void Tick(float dt)
        {
            if (_elapsed < 0f) return;

            _elapsed += dt;
            if (_elapsed >= _duration)
            {
                _elapsed = -1f;
                _field.anchoredPosition = Vector2.zero;
                return;
            }

            float x = MotionCurves.Shake(_elapsed, _duration, Frequency, _magnitude);
            float y = MotionCurves.Shake(_elapsed, _duration, Frequency, _magnitude, PhaseOffsetY);
            _field.anchoredPosition = new Vector2(x, y);
        }
    }
}
