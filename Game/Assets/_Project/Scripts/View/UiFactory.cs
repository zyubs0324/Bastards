using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace TAoB.View
{
    /// <summary>uGUI 요소를 코드로 만든다(계획서 Phase 4: 씬에는 진입점 객체 하나, 나머지는 코드 생성). 스프라이트 없이 색 사각형만 쓴다.</summary>
    internal static class UiFactory
    {
        private static readonly Vector2 Center = new Vector2(0.5f, 0.5f);

        public static Canvas CreateCanvas(Transform parent)
        {
            var go = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            go.transform.SetParent(parent, false);

            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(BattleLayout.Width, BattleLayout.Height);
            scaler.matchWidthOrHeight = 0.5f;
            return canvas;
        }

        public static void CreateEventSystem(Transform parent)
        {
            // 프로젝트 입력 설정이 Input System 전용이라 StandaloneInputModule 은 동작하지 않는다.
            var go = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            go.transform.SetParent(parent, false);
        }

        public static RectTransform CreateRect(Transform parent, string name, Vector2 position, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = Center;
            rect.anchorMax = Center;
            rect.pivot = Center;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }

        public static RectTransform CreateStretched(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = Center;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return rect;
        }

        public static Image CreateImage(Transform parent, string name, Vector2 position, Vector2 size, Color color)
        {
            RectTransform rect = CreateRect(parent, name, position, size);
            var image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        public static TMP_Text CreateText(
            Transform parent, string name, TMP_FontAsset font, string text, float fontSize, Color color,
            Vector2 position, Vector2 size, TextAlignmentOptions alignment = TextAlignmentOptions.Center)
        {
            RectTransform rect = CreateRect(parent, name, position, size);
            var label = rect.gameObject.AddComponent<TextMeshProUGUI>();
            label.font = font;
            label.text = text;
            label.fontSize = fontSize;
            label.color = color;
            label.alignment = alignment;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.overflowMode = TextOverflowModes.Overflow;
            label.raycastTarget = false;
            return label;
        }

        /// <summary>배경 이미지는 활성 색을 직접 바꿀 때 쓴다(<paramref name="background"/>).</summary>
        public static Button CreateButton(
            Transform parent, string name, TMP_FontAsset font, string label, Vector2 position, Vector2 size,
            out TMP_Text labelText, out Image background)
        {
            background = CreateImage(parent, name, position, size, BattleLayout.ButtonColor);
            background.raycastTarget = true;

            var button = background.gameObject.AddComponent<Button>();
            button.targetGraphic = background;
            ColorBlock colors = button.colors;
            colors.disabledColor = BattleLayout.ButtonDisabledColor;
            button.colors = colors;

            labelText = CreateText(background.transform, "Label", font, label, 28f, Color.white, Vector2.zero, size);
            return button;
        }

        /// <summary>
        /// 왼쪽부터 채워지는 막대. 스프라이트 없는 색 사각형이라 <c>Image.fillAmount</c>(스프라이트가 있어야 동작)를 쓸 수 없어 폭 조절로 대체한다.
        /// 채움 폭은 <see cref="SetBarFill"/> 로 바꾼다.
        /// </summary>
        public static RectTransform CreateBar(Transform parent, string name, Vector2 position, Vector2 size, Color back, Color fill)
        {
            Image background = CreateImage(parent, name, position, size, back);
            Image fillImage = CreateImage(background.transform, "Fill", Vector2.zero, size, fill);

            RectTransform rect = fillImage.rectTransform;
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(size.x, 0f);
            return rect;
        }

        public static void SetBarFill(RectTransform fill, float fullWidth, float ratio)
        {
            fill.sizeDelta = new Vector2(fullWidth * Mathf.Clamp01(ratio), 0f);
        }
    }
}
