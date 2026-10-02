using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace TAoB.View
{
    /// <summary>
    /// 피해 숫자·짧은 알림을 띄우는 풀. 전투마다 파괴하지 않고 회수해 다시 쓴다(수직 슬라이스 계획서 Phase 4).
    /// 캔버스 아래(`BattleRoot` 형제)에 두어 재시작 때 파괴되지 않는다.
    /// </summary>
    internal sealed class PopupPool : MonoBehaviour
    {
        private const float Lifetime = 1.1f;
        private const float RiseSpeed = 70f;
        private const float FadeStart = 0.5f;
        private const float StackOffset = 30f;
        private const float PopDuration = 0.2f;
        private const float PopPeakScale = 1.3f;

        private sealed class Popup
        {
            public RectTransform Rect;
            public TMP_Text Text;
            public Vector2 Origin;
            public float Age;
            public float BaseScale;
        }

        private readonly List<Popup> _active = new List<Popup>(32);
        private readonly Stack<Popup> _free = new Stack<Popup>(32);
        private TMP_FontAsset _font;
        private RectTransform _rect;

        public static PopupPool Create(Transform canvas, TMP_FontAsset font, int prewarm)
        {
            RectTransform rect = UiFactory.CreateStretched(canvas, BattleLayout.PopupPoolName);
            var pool = rect.gameObject.AddComponent<PopupPool>();
            pool._font = font;
            pool._rect = rect;
            for (int i = 0; i < prewarm; i++) pool._free.Push(pool.Make());
            return pool;
        }

        public void ShowNumber(Vector2 position, int value, Color color, float scale)
        {
            Popup popup = Take(position, color, scale);
            popup.Text.SetText("{0}", value);
        }

        /// <summary>라벨은 상수 문자열만 넘긴다(프레임마다 문자열을 만들지 않기 위함).</summary>
        public void ShowLabel(Vector2 position, string label, Color color, float scale)
        {
            Popup popup = Take(position, color, scale);
            popup.Text.text = label;
        }

        public void RecycleAll()
        {
            for (int i = _active.Count - 1; i >= 0; i--) Recycle(i);
        }

        private Popup Make()
        {
            TMP_Text text = UiFactory.CreateText(_rect, "Popup", _font, "", 36f, Color.white, Vector2.zero, new Vector2(320f, 60f));
            text.gameObject.SetActive(false);
            return new Popup { Rect = text.rectTransform, Text = text };
        }

        private Popup Take(Vector2 position, Color color, float scale)
        {
            // 같은 자리에 같은 틱의 알림이 겹치면 위로 쌓는다.
            int stacked = 0;
            for (int i = 0; i < _active.Count; i++)
            {
                if (_active[i].Origin == position && _active[i].Age < 0.05f) stacked++;
            }

            Popup popup = _free.Count > 0 ? _free.Pop() : Make();
            popup.Age = 0f;
            popup.Origin = position;
            popup.BaseScale = scale;
            popup.Rect.anchoredPosition = position + new Vector2(0f, stacked * StackOffset);
            popup.Rect.localScale = Vector3.one * scale;
            popup.Text.color = color;
            popup.Rect.SetAsLastSibling();
            popup.Text.gameObject.SetActive(true);
            _active.Add(popup);
            return popup;
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                Popup popup = _active[i];
                popup.Age += dt;
                if (popup.Age >= Lifetime)
                {
                    Recycle(i);
                    continue;
                }

                Vector2 current = popup.Rect.anchoredPosition;
                popup.Rect.anchoredPosition = new Vector2(current.x, current.y + RiseSpeed * dt);

                // 등장 직후 살짝 커졌다가 정착한다(튀어오름). 나머지 수명 동안은 BaseScale 그대로다.
                float pop = MotionCurves.Pop(popup.Age, PopDuration, PopPeakScale);
                popup.Rect.localScale = Vector3.one * (popup.BaseScale * pop);

                if (popup.Age > FadeStart)
                {
                    Color color = popup.Text.color;
                    color.a = 1f - (popup.Age - FadeStart) / (Lifetime - FadeStart);
                    popup.Text.color = color;
                }
            }
        }

        private void Recycle(int index)
        {
            Popup popup = _active[index];
            popup.Text.gameObject.SetActive(false);
            _active.RemoveAt(index);
            _free.Push(popup);
        }
    }
}
