using UnityEngine;

namespace TAoB.View
{
    /// <summary>
    /// 1920×1080 기준으로 좌표를 잡은 UI 를 실제 화면의 세이프 에어리어 안에 맞추는 순수 계산.
    /// 엔진 객체를 만지지 않아 종횡비·노치 조합을 테스트로 훑을 수 있다.
    ///
    /// 왜 필요한가: UI 가 1920×1080 중앙 기준 절대 좌표라, 노치·제스처 바·둥근 모서리가 있는 폰은 세이프 에어리어 밖(안드로이드가
    /// 밖까지 그린다)의 버튼을 가리고, 가로가 모자란 화면(4:3 등)에서는 양끝 버튼이 잘린다. 세로는 스케일러를 높이 기준으로
    /// 고정(<see cref="ScalerMatch"/>)해 막았다 — 섞어 두면 가로로 긴 폰에서 캔버스 높이가 줄어 하단 버튼이 화면 밖으로 나간다.
    /// </summary>
    public static class UiFit
    {
        /// <summary>
        /// 지원하는 가장 넓은 캔버스 폭(캔버스 단위). 실제 21:9 계열 기기(3840×1644 → 약 2523)까지 여유를 둔 2600 이다.
        /// 축소·이동하지 않는 전체 화면 층(전장 배경·바닥 표식 묶음)이 이 폭까지 덮는다. 이보다 넓은 화면은 지원하지 않는다.
        /// 축소되는 UI 컨테이너 아래의 배경·딤은 <see cref="BleedSize"/> 를 쓴다.
        /// </summary>
        public const float MaxCanvasWidth = 2600f;

        /// <summary>
        /// CanvasScaler 의 높이/너비 혼합값. **1 = 높이 기준**이라 캔버스 높이가 늘 설계 높이(1080)다.
        /// 0.5 로 섞으면 가로로 긴 폰에서 캔버스 높이가 줄어(20:9 → 966) 하단 버튼·상단 HUD 가 잘린다.
        /// 스케일러 설정(<c>UiFactory</c>)과 테스트가 이 상수를 같이 쓴다.
        /// </summary>
        public const float ScalerMatch = 1f;

        /// <summary>
        /// 화면 끝까지 덮어야 하는 배경 띠·딤의 한 변 크기. 설계 영역 컨테이너(<see cref="SafeAreaFit"/>)가
        /// 줄어들거나 노치 쪽으로 치우쳐도 가장자리가 비지 않도록 지원 최대 폭(<see cref="MaxCanvasWidth"/>)보다 훨씬 크게 잡는다.
        /// </summary>
        public const float BleedSize = 8000f;

        public readonly struct Result
        {
            public Result(float scale, Vector2 center)
            {
                Scale = scale;
                Center = center;
            }

            /// <summary>설계 영역에 곱할 배율. 1 이하이며, 세이프 에어리어가 설계 영역보다 작을 때만 줄어든다.</summary>
            public float Scale { get; }

            /// <summary>설계 영역의 가운데를 둘 캔버스 좌표(캔버스 중심 기준). 세이프 에어리어의 가운데다.</summary>
            public Vector2 Center { get; }
        }

        /// <summary>
        /// CanvasScaler(ScaleWithScreenSize)가 정하는 캔버스 단위 크기. 엔진 규칙을 그대로 옮겼다 —
        /// 배율은 화면/기준 비율의 로그를 <paramref name="match"/> 로 섞은 값이다(0 = 너비 기준, 1 = 높이 기준).
        /// 실행 중에는 엔진이 계산하므로 이 함수는 **테스트 전용 이식**이다 — 종횡비별 캔버스 크기를 엔진 없이 구해
        /// 스케일러 설정(<see cref="ScalerMatch"/>)의 회귀를 보호한다.
        /// </summary>
        public static Vector2 CanvasSize(float screenWidth, float screenHeight, Vector2 referenceSize, float match)
        {
            if (screenWidth <= 0f || screenHeight <= 0f || referenceSize.x <= 0f || referenceSize.y <= 0f)
            {
                return referenceSize;
            }

            float logWidth = Mathf.Log(screenWidth / referenceSize.x, 2f);
            float logHeight = Mathf.Log(screenHeight / referenceSize.y, 2f);
            float scale = Mathf.Pow(2f, Mathf.Lerp(logWidth, logHeight, match));
            return new Vector2(screenWidth / scale, screenHeight / scale);
        }

        /// <param name="canvasSize">캔버스 단위의 전체 크기.</param>
        /// <param name="designSize">좌표를 잡은 기준 크기(1920×1080).</param>
        /// <param name="safeNormalized">화면 대비 0~1 로 환산한 세이프 에어리어(왼쪽 아래 원점).</param>
        public static Result Fit(Vector2 canvasSize, Vector2 designSize, Rect safeNormalized)
        {
            // 크기를 아직 모르는 프레임(캔버스가 첫 갱신 전)에는 아무것도 바꾸지 않는다.
            if (canvasSize.x <= 0f || canvasSize.y <= 0f || designSize.x <= 0f || designSize.y <= 0f
                || safeNormalized.width <= 0f || safeNormalized.height <= 0f)
            {
                return new Result(1f, Vector2.zero);
            }

            float safeWidth = safeNormalized.width * canvasSize.x;
            float safeHeight = safeNormalized.height * canvasSize.y;

            // 줄이기만 한다. 세이프 에어리어가 설계 영역보다 커도 늘리면 글자·버튼이 화면 비율에 따라 부풀어 오른다.
            float scale = Mathf.Min(1f, Mathf.Min(safeWidth / designSize.x, safeHeight / designSize.y));

            var center = new Vector2(
                (safeNormalized.x + safeNormalized.width * 0.5f - 0.5f) * canvasSize.x,
                (safeNormalized.y + safeNormalized.height * 0.5f - 0.5f) * canvasSize.y);

            return new Result(scale, center);
        }

        /// <summary>화면 픽셀 단위 세이프 에어리어를 0~1 로 환산한다. 화면 크기를 모르면 전체 화면으로 본다.</summary>
        public static Rect Normalize(Rect safePixels, float screenWidth, float screenHeight)
        {
            if (screenWidth <= 0f || screenHeight <= 0f) return new Rect(0f, 0f, 1f, 1f);

            return new Rect(
                safePixels.x / screenWidth,
                safePixels.y / screenHeight,
                safePixels.width / screenWidth,
                safePixels.height / screenHeight);
        }
    }
}
