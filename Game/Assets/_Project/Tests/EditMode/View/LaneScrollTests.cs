using NUnit.Framework;

namespace TAoB.View.Tests
{
    /// <summary>
    /// 배경 스크롤의 순수 계산. 랩어라운드와 패럴랙스는 눈으로 보고 고치기 어려운 종류의 버그라
    /// 경계값을 여기서 고정한다(계획서 Phase B).
    /// </summary>
    public class LaneScrollTests
    {
        private const float Spacing = BattleLayout.LaneMarkerSpacingPx;
        private const int Count = BattleLayout.LaneMarkerCount;

        [Test]
        public void AnchorPx_InterpolatesBetweenSnapshots()
        {
            Assert.AreEqual(100f, LaneScroll.AnchorPx(100, 140, 0f, 1f), 0.001f);
            Assert.AreEqual(140f, LaneScroll.AnchorPx(100, 140, 1f, 1f), 0.001f);
            Assert.AreEqual(120f, LaneScroll.AnchorPx(100, 140, 0.5f, 1f), 0.001f);
        }

        [Test]
        public void AnchorPx_ScalesByPxPerFu()
        {
            Assert.AreEqual(300f, LaneScroll.AnchorPx(100, 100, 0.5f, 3f), 0.001f);
        }

        [Test]
        public void Wrap_StaysInRange_ForNegativeAndLargeValues()
        {
            // C# 의 % 는 음수에서 음수를 내므로 그대로 쓰면 뒤로 스크롤할 때 띠가 한 칸 튄다.
            Assert.AreEqual(200f, LaneScroll.Wrap(-40f, 240f), 0.001f);
            Assert.AreEqual(0f, LaneScroll.Wrap(-240f, 240f), 0.001f);
            Assert.AreEqual(40f, LaneScroll.Wrap(240f * 1000f + 40f, 240f), 0.01f);
            Assert.AreEqual(0f, LaneScroll.Wrap(100f, 0f), 0.001f, "주기가 0 이면 나눗셈을 하지 않는다.");
        }

        [Test]
        public void Tiles_SpanTheirBundle_AndCentreOnTheGivenPoint()
        {
            float first = LaneScroll.TileLocalX(0f, 0f, Spacing, 0, Count);
            float last = LaneScroll.TileLocalX(0f, 0f, Spacing, Count - 1, Count);

            Assert.AreEqual(Spacing * (Count - 1), last - first, 0.001f, "묶음 폭 = 간격 × (개수 − 1)");
            Assert.AreEqual(0f, (first + last) * 0.5f, 0.001f, "흐른 거리가 0 이면 묶음의 가운데가 기준점이다.");
        }

        [Test]
        public void TileScreenX_SlidesLeft_ThenRepeatsEverySpacing()
        {
            float atZero = ScreenX(0f, 0);
            float atHalf = ScreenX(Spacing * 0.5f, 0);
            float atFull = ScreenX(Spacing, 0);

            Assert.AreEqual(atZero - Spacing * 0.5f, atHalf, 0.001f, "흐른 만큼 왼쪽으로 간다.");
            Assert.AreEqual(atZero, atFull, 0.001f, "한 간격마다 랩어라운드로 제자리");
        }

        [Test]
        public void TilesCoverBothScreenEdges_AtEveryPointOfTheWrapCycle()
        {
            // LaneView 가 쓰는 합성식(층 위치 + 화면 중앙 기준 지역 X)을 같은 함수로 호출해 스크롤을 훑는다.
            // 한 주기를 여러 점으로 나눠, 랩어라운드로 묶음이 가장 많이 밀린 순간도 포함한다.
            const float edge = UiFit.MaxCanvasWidth * 0.5f;
            for (float scroll = 0f; scroll < Spacing * 3f; scroll += Spacing / 8f)
            {
                float left = TileScreen(scroll, 0);
                float right = TileScreen(scroll, Count - 1);

                Assert.LessOrEqual(left, -edge, $"스크롤 {scroll}: 왼쪽 가장자리");
                Assert.GreaterOrEqual(right, edge, $"스크롤 {scroll}: 오른쪽 가장자리");
            }
        }

        [Test]
        public void TilesFollowingTheAllyAnchor_LeaveTheRightEdgeBare()
        {
            // 옛 결함을 재현한다: 묶음을 화면 중앙이 아니라 진형 기준점에 맞추면(centerLocalX = scroll)
            // 어떤 스크롤에서 오른쪽 가장자리가 비어야 한다. 이 테스트가 깨지면 위 덮임 단언이 공허해진 것이다.
            const float edge = UiFit.MaxCanvasWidth * 0.5f;
            bool anyBare = false;
            for (float scroll = 0f; scroll < Spacing; scroll += Spacing / 8f)
            {
                float lane = LaneScroll.LaneOffsetX(scroll, BattleLayout.AllyAnchorScreenX);
                float wrapped = LaneScroll.TileLocalX(scroll, scroll, Spacing, Count - 1, Count);
                if (lane + wrapped < edge) anyBare = true;
            }

            Assert.IsTrue(anyBare, "기준점에 맞춘 묶음은 오른쪽 가장자리를 덮지 못해야 한다.");
        }

        [Test]
        public void TileScreenX_StaysBounded_EvenAfterAVeryLongMarch()
        {
            // 좌표가 커져도 띠는 묶음 폭 + 한 칸 안에 머문다 — 그래서 띠를 새로 만들지 않는다.
            float limit = Spacing * (Count - 1) * 0.5f + Spacing;
            for (int i = 0; i < Count; i++)
            {
                Assert.That(ScreenX(1000000f, i), Is.InRange(-limit, limit), $"{i}번 띠");
            }
        }

        [Test]
        public void FarLayer_FlowsSlowerThanTheNearLayer_ThroughTheSharedComposition()
        {
            // 같은 합성 함수에 계수만 바꿔 호출한다. 한 점이 아니라 이어지는 구간의 기울기를 보고,
            // "더 느리다"를 두 층의 기울기 비교로 직접 단언한다(상수를 다시 꺼내 비교하지 않는다).
            for (float scroll = 10f; scroll < 100f; scroll += 10f)
            {
                float nearStep = TileScreen(scroll, 0, 1f) - TileScreen(scroll + 1f, 0, 1f);
                float farStep = TileScreen(scroll, 0, BattleLayout.LaneFarParallax) - TileScreen(scroll + 1f, 0, BattleLayout.LaneFarParallax);

                Assert.Greater(farStep, 0f, "먼 층도 같은 방향으로 흐른다");
                Assert.Less(farStep, nearStep, "먼 층이 더 느리다");
            }
        }

        [Test]
        public void ParallaxFactor_IsStrictlyBetweenStandingStillAndFieldSpeed()
        {
            // 0 이면 먼 층이 서 있고 1 이면 깊이가 없다. 기획 상한 0.8 은 "눈에 띄게 느릴 것"의 근거다.
            Assert.Greater(BattleLayout.LaneFarParallax, 0f);
            Assert.Less(BattleLayout.LaneFarParallax, 0.8f);
        }

        [Test]
        public void ZeroSpacingOrCount_FallsBackToTheCentre()
        {
            Assert.AreEqual(50f, LaneScroll.TileLocalX(50f, 0f, 0f, 0, Count), 0.001f);
            Assert.AreEqual(50f, LaneScroll.TileLocalX(50f, 0f, Spacing, 0, 0), 0.001f);
        }

        /// <summary>
        /// 띠가 화면에 나타나는 X — <c>LaneView</c> 와 같은 합성 함수(<see cref="LaneScroll.TileScreenX"/>)를 부른다.
        /// 진형 기준점은 실제 배치 상수를 쓴다.
        /// </summary>
        private static float TileScreen(float scrollPx, int index, float parallax = 1f)
        {
            return LaneScroll.TileScreenX(scrollPx, BattleLayout.AllyAnchorScreenX, parallax, Spacing, index, Count);
        }

        /// <summary>합성 없이 한 칸의 화면 X 를 흐른 거리만으로 본다(랩어라운드 단독 검증용, 묶음 중심 0).</summary>
        private static float ScreenX(float movedPx, int index)
        {
            return LaneScroll.TileLocalX(0f, movedPx, Spacing, index, Count);
        }
    }
}
