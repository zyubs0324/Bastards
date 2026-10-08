using UnityEngine;

namespace TAoB.View
{
    /// <summary>
    /// 1920×1080 설계 영역을 가진 UI 컨테이너. 화면의 세이프 에어리어 안에 맞춰 옮기고 줄인다(<see cref="UiFit"/>).
    /// HUD·개입 바·결과 화면처럼 버튼이 있는 요소만 이 아래에 두고, 배경·유닛(전장 층)과 팝업은 화면 전체를 쓴다.
    /// </summary>
    internal sealed class SafeAreaFit : MonoBehaviour
    {
        private RectTransform _rect;
        private RectTransform _canvasRect;
        private Vector2 _lastCanvasSize = new Vector2(-1f, -1f);
        private Rect _lastSafe = new Rect(-1f, -1f, -1f, -1f);
        private Vector2 _lastScreen = new Vector2(-1f, -1f);

        /// <param name="parent">캔버스 전체를 덮는(스트레치된) 부모.</param>
        public static RectTransform Create(RectTransform parent, string name)
        {
            RectTransform rect = UiFactory.CreateRect(parent, name, Vector2.zero, new Vector2(BattleLayout.Width, BattleLayout.Height));
            var fit = rect.gameObject.AddComponent<SafeAreaFit>();
            fit._rect = rect;
            fit._canvasRect = parent;
            fit.Apply();
            return rect;
        }

        // 화면 회전·분할 화면·키보드 등으로 세이프 에어리어가 바뀔 수 있어 매 프레임 값만 비교한다.
        // 바뀐 프레임에만 좌표를 쓰므로 평소에는 UI 재구성이 일어나지 않는다.
        private void LateUpdate()
        {
            Apply();
        }

        private void Apply()
        {
            Vector2 canvasSize = _canvasRect.rect.size;
            Rect safe = CurrentSafeArea();
            var screen = new Vector2(Screen.width, Screen.height);
            if (canvasSize == _lastCanvasSize && safe == _lastSafe && screen == _lastScreen) return;

            _lastCanvasSize = canvasSize;
            _lastSafe = safe;
            _lastScreen = screen;

            UiFit.Result fit = UiFit.Fit(
                canvasSize,
                new Vector2(BattleLayout.Width, BattleLayout.Height),
                UiFit.Normalize(safe, screen.x, screen.y));

            _rect.anchoredPosition = fit.Center;
            _rect.localScale = new Vector3(fit.Scale, fit.Scale, 1f);
        }

        private static Rect CurrentSafeArea()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            // 노치·제스처 바는 PC 에서 재현되지 않으므로 개발 빌드에서만 실행 인자로 흉내 낸다.
            // `-safeInsets <왼> <아래> <오른> <위>` (화면 픽셀). 폰에 설치하는 빌드에는 인자를 줄 수 없어 영향이 없다.
            if (TryReadInsets(out Rect simulated)) return simulated;
#endif
            return Screen.safeArea;
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        // 인자는 처음 한 번만 읽는다 — 매 프레임 불리는 경로라 파싱하면 할당이 생긴다.
        private static bool _hasReadInsets;
        private static bool _hasInsets;
        private static Vector4 _insets;

        private static bool TryReadInsets(out Rect safe)
        {
            if (!_hasReadInsets)
            {
                _hasReadInsets = true;
                _hasInsets = ParseInsets(System.Environment.GetCommandLineArgs(), out _insets);
            }

            safe = default;
            if (!_hasInsets) return false;

            safe = new Rect(_insets.x, _insets.y, Screen.width - _insets.x - _insets.z, Screen.height - _insets.y - _insets.w);
            return safe.width > 0f && safe.height > 0f;
        }

        private static bool ParseInsets(string[] args, out Vector4 insets)
        {
            insets = default;
            for (int i = 0; i + 4 < args.Length; i++)
            {
                if (args[i] != "-safeInsets") continue;

                var values = new float[4];
                for (int k = 0; k < 4; k++)
                {
                    if (!float.TryParse(args[i + 1 + k], System.Globalization.NumberStyles.Float,
                            System.Globalization.CultureInfo.InvariantCulture, out values[k]))
                    {
                        // 오타를 조용히 전체 화면으로 돌리면 시뮬레이션이 안 되는 줄 모르고 지나간다.
                        Debug.LogWarning($"-safeInsets 값 '{args[i + 1 + k]}' 을(를) 읽을 수 없어 무시합니다. 형식: -safeInsets 왼 아래 오른 위 (px)");
                        return false;
                    }
                }

                insets = new Vector4(values[0], values[1], values[2], values[3]);
                return true;
            }

            return false;
        }
#endif
    }
}
