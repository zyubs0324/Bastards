using NUnit.Framework;
using UnityEngine;

namespace TAoB.View.Tests
{
    /// <summary>
    /// 설계 영역(1920×1080)을 세이프 에어리어 안에 맞추는 계산. 종횡비·노치·제스처 바 조합이 많아
    /// 폰에서 하나씩 눈으로 확인할 수 없으므로 불변식("설계 영역이 세이프 에어리어 밖으로 나가지 않는다")으로 훑는다.
    /// </summary>
    public class UiFitTests
    {
        private static readonly Vector2 Design = new Vector2(BattleLayout.Width, BattleLayout.Height);
        private static readonly Rect Full = new Rect(0f, 0f, 1f, 1f);

        /// <summary>
        /// 실제 스케일러 설정(<see cref="UiFit.ScalerMatch"/>)으로 얻는 캔버스 크기. 설정을 되돌리면(예: 0.5)
        /// 이 값이 달라져 아래 테스트들이 실패한다 — 스케일러 회귀를 보호하는 지점이다.
        /// </summary>
        private static Vector2 Canvas(float screenWidth, float screenHeight)
        {
            return UiFit.CanvasSize(screenWidth, screenHeight, Design, UiFit.ScalerMatch);
        }

        [Test]
        public void SixteenByNine_NoInsets_KeepsTheDesignAsIs()
        {
            UiFit.Result fit = UiFit.Fit(Canvas(1920f, 1080f), Design, Full);

            Assert.AreEqual(1f, fit.Scale, 0.0001f);
            Assert.AreEqual(0f, fit.Center.x, 0.001f);
            Assert.AreEqual(0f, fit.Center.y, 0.001f);
        }

        [TestCase(2340f, 1080f)]
        [TestCase(2400f, 1080f)]
        [TestCase(2520f, 1080f)]
        public void WidePhone_NoInsets_DoesNotShrink_BecauseTheHeightIsAlwaysTheDesignHeight(float w, float h)
        {
            // 폭만 넓어지고 높이는 그대로라 하단 버튼이 화면 안에 남는다 — match 0.5 일 때 잘리던 경우.
            UiFit.Result fit = UiFit.Fit(Canvas(w, h), Design, Full);

            Assert.AreEqual(1f, fit.Scale, 0.0001f);
            Assert.AreEqual(0f, fit.Center.y, 0.001f);
        }

        [TestCase(2340f, 1080f)]
        [TestCase(2400f, 1080f)]
        [TestCase(2520f, 1080f)]
        [TestCase(1920f, 1080f)]
        public void Scaler_KeepsTheCanvasAtLeastAsTallAsTheDesign(float w, float h)
        {
            // 하단 버튼(y −512)·상단 HUD(y +520)가 들어가려면 캔버스 높이가 설계 높이 이상이어야 한다.
            Assert.GreaterOrEqual(Canvas(w, h).y, Design.y - 0.01f);
        }

        [Test]
        public void OldHalfwayMatch_ShrinksTheCanvasBelowTheDesign_WhichIsWhatClippedTheButtons()
        {
            // 옛 결함 재현: match 0.5 면 20:9 에서 캔버스 높이가 966 이라 하단 버튼이 화면 밖으로 나갔다.
            // 이 테스트가 깨지면 위 보호 테스트가 공허해진 것이다(계산식이 엔진 규칙과 어긋났다는 뜻).
            Vector2 old = UiFit.CanvasSize(2400f, 1080f, Design, 0.5f);

            Assert.Less(old.y, Design.y - 100f);
            Assert.AreEqual(966f, old.y, 1f);
        }

        [Test]
        public void FourByThree_ShrinksToFitTheNarrowWidth()
        {
            // 캔버스가 1440×1080 이라 1920 폭 설계 영역은 0.75 배로 줄어야 좌우 버튼이 들어온다.
            UiFit.Result fit = UiFit.Fit(Canvas(1440f, 1080f), Design, Full);

            Assert.AreEqual(0.75f, fit.Scale, 0.0001f);
        }

        [Test]
        public void NotchAndGestureBar_MoveAndShrinkTheDesignInsideTheSafeArea()
        {
            // 2400×1080 화면에서 왼쪽 노치 120px, 아래 제스처 바 60px 를 뺀 세이프 에어리어.
            var safePixels = new Rect(120f, 60f, 2280f, 1020f);
            Rect safe = UiFit.Normalize(safePixels, 2400f, 1080f);

            UiFit.Result fit = UiFit.Fit(Canvas(2400f, 1080f), Design, safe);

            Assert.AreEqual(1020f / 1080f, fit.Scale, 0.0001f, "높이가 모자라 줄어든다");
            Assert.AreEqual(60f, fit.Center.x, 0.01f, "노치 반대편으로 치우친다");
            Assert.AreEqual(30f, fit.Center.y, 0.01f, "제스처 바 위로 올라온다");
        }

        [Test]
        public void DesignNeverLeavesTheSafeArea_AcrossAspectRatiosAndInsets()
        {
            // 종횡비 × 노치(왼/오른/아래/위) 조합을 훑어 설계 영역의 네 변이 세이프 에어리어 안에 있는지 본다.
            float[][] screens =
            {
                new[] { 1920f, 1080f }, new[] { 2340f, 1080f }, new[] { 2400f, 1080f }, new[] { 2520f, 1080f },
                new[] { 1920f, 1200f }, new[] { 1440f, 1080f }, new[] { 2800f, 1260f },
            };
            float[] insets = { 0f, 40f, 120f };

            foreach (float[] screen in screens)
            {
                foreach (float left in insets)
                foreach (float right in insets)
                foreach (float bottom in insets)
                foreach (float top in insets)
                {
                    float w = screen[0];
                    float h = screen[1];
                    var safePixels = new Rect(left, bottom, w - left - right, h - bottom - top);
                    Rect safe = UiFit.Normalize(safePixels, w, h);
                    Vector2 canvas = Canvas(w, h);

                    UiFit.Result fit = UiFit.Fit(canvas, Design, safe);

                    float halfW = Design.x * 0.5f * fit.Scale;
                    float halfH = Design.y * 0.5f * fit.Scale;
                    float safeLeft = (safe.xMin - 0.5f) * canvas.x;
                    float safeRight = (safe.xMax - 0.5f) * canvas.x;
                    float safeBottom = (safe.yMin - 0.5f) * canvas.y;
                    float safeTop = (safe.yMax - 0.5f) * canvas.y;
                    string label = $"{w}x{h} 여백 L{left} R{right} B{bottom} T{top}";

                    Assert.GreaterOrEqual(fit.Center.x - halfW, safeLeft - 0.01f, label + " 왼쪽");
                    Assert.LessOrEqual(fit.Center.x + halfW, safeRight + 0.01f, label + " 오른쪽");
                    Assert.GreaterOrEqual(fit.Center.y - halfH, safeBottom - 0.01f, label + " 아래");
                    Assert.LessOrEqual(fit.Center.y + halfH, safeTop + 0.01f, label + " 위");
                    Assert.LessOrEqual(fit.Scale, 1f, label + " 배율은 1 을 넘지 않는다");

                    // 필요 이상으로 줄이지 않는다 — 항상 0.1 을 돌려주는 구현도 위 포함 검사는 통과하기 때문.
                    float expected = Mathf.Min(1f, Mathf.Min(safePixels.width / w * canvas.x / Design.x, safePixels.height / h * canvas.y / Design.y));
                    Assert.AreEqual(expected, fit.Scale, 0.0001f, label + " 최대한 크게");

                    // 화면 끝까지 덮는 층(bleed)은 컨테이너가 줄고 치우쳐도 캔버스를 덮어야 한다.
                    float bleedHalf = UiFit.BleedSize * 0.5f * fit.Scale;
                    Assert.LessOrEqual(fit.Center.x - bleedHalf, -canvas.x * 0.5f, label + " bleed 왼쪽");
                    Assert.GreaterOrEqual(fit.Center.x + bleedHalf, canvas.x * 0.5f, label + " bleed 오른쪽");
                    Assert.GreaterOrEqual(fit.Center.y + bleedHalf, canvas.y * 0.5f, label + " bleed 위");
                    Assert.LessOrEqual(fit.Center.y - bleedHalf, -canvas.y * 0.5f, label + " bleed 아래");
                }
            }
        }

        [Test]
        public void UnknownSizes_ChangeNothing()
        {
            // 캔버스가 첫 갱신 전이라 크기가 0 인 프레임에 UI 를 0 배로 만들지 않는다.
            UiFit.Result zeroCanvas = UiFit.Fit(Vector2.zero, Design, Full);
            UiFit.Result emptySafe = UiFit.Fit(Canvas(2400f, 1080f), Design, new Rect(0f, 0f, 0f, 0f));

            Assert.AreEqual(1f, zeroCanvas.Scale, 0.0001f);
            Assert.AreEqual(1f, emptySafe.Scale, 0.0001f);
            Assert.AreEqual(Vector2.zero, zeroCanvas.Center);
        }

        [Test]
        public void Normalize_ScalesPixelsToZeroOne_AndFallsBackToFullScreen()
        {
            Rect safe = UiFit.Normalize(new Rect(100f, 50f, 2000f, 1000f), 2400f, 1080f);

            Assert.AreEqual(100f / 2400f, safe.x, 0.0001f);
            Assert.AreEqual(50f / 1080f, safe.y, 0.0001f);
            Assert.AreEqual(2000f / 2400f, safe.width, 0.0001f);
            Assert.AreEqual(1000f / 1080f, safe.height, 0.0001f);
            Assert.AreEqual(Full, UiFit.Normalize(new Rect(1f, 2f, 3f, 4f), 0f, 0f));
        }

        [Test]
        public void MaxCanvasWidth_CoversTheWidestSupportedPhone()
        {
            // 상한은 구성이 만든 값이 아니라 **실제 기기 해상도**에서 나온 캔버스 폭과 비교한다.
            // 3840×1644 는 21:9 계열 기기의 실제 해상도라 캔버스 폭이 약 2523 이다.
            Assert.GreaterOrEqual(UiFit.MaxCanvasWidth, Canvas(3840f, 1644f).x);
            Assert.GreaterOrEqual(UiFit.MaxCanvasWidth, Canvas(2520f, 1080f).x);
        }
    }
}
