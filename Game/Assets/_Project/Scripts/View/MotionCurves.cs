using UnityEngine;

namespace TAoB.View
{
    /// <summary>
    /// 연출 곡선(계획서 Phase 4.5). 엔진 상태 없는 순수 함수만 둔다 — 경과 시간 t(초)를 오프셋·배율로 바꾼다.
    /// 지속 시간·진폭 같은 수치는 이 파일에 두지 않는다. 호출부(각 View 파일)가 자기 상수를 넘긴다.
    /// </summary>
    public static class MotionCurves
    {
        /// <summary>0 → 최대 1 → 0 으로 돌아오는 진행도(돌진). t 가 범위 밖(0 이하·duration 이상)이면 0.</summary>
        public static float Lunge(float t, float duration)
        {
            if (duration <= 0f || t <= 0f || t >= duration) return 0f;
            return Mathf.Sin(Mathf.PI * t / duration);
        }

        /// <summary>등장 후 정착하는 배율(튀어오름). 시작·종료(지속 시간 이상)에서 1, 중간에 <paramref name="peakScale"/>.</summary>
        public static float Pop(float t, float duration, float peakScale)
        {
            if (duration <= 0f || t <= 0f || t >= duration) return 1f;
            return 1f + (peakScale - 1f) * Mathf.Sin(Mathf.PI * t / duration);
        }

        /// <summary>
        /// 감쇠하는 좌우 흔들림. 지속 시간 이상이면 정확히 0. <paramref name="phase"/> 로 다른 축과 위상을 어긋내 2D 흔들림을 만든다.
        /// </summary>
        public static float Shake(float t, float duration, float frequency, float magnitude, float phase = 0f)
        {
            if (duration <= 0f || t < 0f || t >= duration) return 0f;
            float decay = 1f - t / duration;
            return magnitude * decay * Mathf.Sin(t * frequency + phase);
        }

        /// <summary>위험 단계가 높을수록 빨라지는 맥동 배율(과부하 위험). 단계 0 이하면 항상 1. 범위는 1 ~ 1+<paramref name="amplitude"/>.</summary>
        public static float Pulse(float t, int riskLevel, float baseFrequency, float frequencyStep, float amplitude)
        {
            if (riskLevel <= 0) return 1f;
            float frequency = baseFrequency + riskLevel * frequencyStep;
            float wave = (Mathf.Sin(t * frequency) + 1f) * 0.5f;
            return 1f + wave * amplitude;
        }

        /// <summary>
        /// 상시 반복하는 작은 오프셋(숨쉬기·흩어짐 떨림 공용). 위상이 다르면 같은 시각에도 값이 다르다.
        /// 값은 항상 <paramref name="amplitude"/> 이내다.
        /// </summary>
        public static float Breathe(float t, float phase, float frequency, float amplitude)
        {
            return Mathf.Sin(t * frequency + phase) * amplitude;
        }
    }
}
