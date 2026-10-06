using UnityEngine;

namespace TAoB.View
{
    /// <summary>
    /// 전장 스크롤 층(계획서 Phase B·D12). 배경 띠와 유닛 표시를 모두 이 아래에 두고 층 하나만 옮겨
    /// 진형이 전진하는 것을 화면에 보인다. <b>중첩 캔버스</b>로 두어 스크롤 때문에 다시 묶이는 범위가
    /// HUD·개입 바·팝업으로 번지지 않게 한다.
    ///
    /// 화면 흔들림을 담당하는 <c>Field</c> 의 자식이므로 두 좌표 쓰기가 섞이지 않는다 —
    /// <see cref="ScreenShake"/> 는 <c>Field</c>, 이 타입은 <c>Lane</c> 만 만진다.
    /// </summary>
    internal sealed class LaneView
    {
        private readonly RectTransform _rect;
        private readonly RectTransform[] _markers;
        private readonly RectTransform[] _far;

        private float _lastScrollPx = float.NaN;

        private LaneView(RectTransform rect, RectTransform[] markers, RectTransform[] far)
        {
            _rect = rect;
            _markers = markers;
            _far = far;
        }

        /// <summary>유닛 표시를 붙일 부모. 유닛의 지역 X 는 <see cref="BattleLayout.LaneX"/> 가 정한다.</summary>
        public Transform Content => _rect;

        /// <summary>이 층이 옮겨 간 양. 팝업은 캔버스 좌표를 쓰므로 호출자가 이 값을 더해야 한다.</summary>
        public Vector2 Offset => _rect.anchoredPosition;

        public static LaneView Create(Transform field)
        {
            RectTransform rect = UiFactory.CreateStretched(field, BattleLayout.LaneName);

            // 중첩 캔버스. 부모 캔버스의 렌더 모드·스케일러를 그대로 물려받는다.
            Canvas canvas = rect.gameObject.AddComponent<Canvas>();
            canvas.overrideSorting = false;

            RectTransform[] far = CreateTiles(
                rect, "Far", BattleLayout.LaneFarCount,
                new Vector2(BattleLayout.LaneFarWidth, BattleLayout.LaneFarHeight), BattleLayout.LaneFarColor);
            RectTransform[] markers = CreateTiles(
                rect, "Marker", BattleLayout.LaneMarkerCount,
                new Vector2(BattleLayout.LaneMarkerWidth, BattleLayout.LaneMarkerHeight), BattleLayout.LaneMarkerColor);

            return new LaneView(rect, markers, far);
        }

        /// <summary>
        /// 기준점 보간값으로 층과 배경 띠를 옮긴다. 스크롤이 멈춘 프레임에는 좌표를 다시 쓰지 않는다 —
        /// 전투 중에는 기준점이 멈춰 있으므로 대부분의 프레임이 이 경로다.
        /// </summary>
        public void Apply(int previousAnchorX, int currentAnchorX, float alpha)
        {
            float scrollPx = LaneScroll.AnchorPx(previousAnchorX, currentAnchorX, alpha, BattleLayout.PxPerFu);
            if (scrollPx == _lastScrollPx) return;

            _lastScrollPx = scrollPx;
            float anchorScreenX = BattleLayout.AllyAnchorScreenX;
            _rect.anchoredPosition = new Vector2(LaneScroll.LaneOffsetX(scrollPx, anchorScreenX), 0f);

            // 합성식은 LaneScroll 이 정본이다 — 테스트가 같은 함수로 화면 덮임을 검증한다.
            float farY = BattleLayout.LaneFarY();
            for (int i = 0; i < _far.Length; i++)
            {
                float x = LaneScroll.TileLocalXAt(
                    scrollPx, anchorScreenX, BattleLayout.LaneFarParallax, BattleLayout.LaneFarSpacingPx, i, _far.Length);
                _far[i].anchoredPosition = new Vector2(x, farY);
            }

            float markerY = BattleLayout.LaneMarkerY();
            for (int i = 0; i < _markers.Length; i++)
            {
                float x = LaneScroll.TileLocalXAt(
                    scrollPx, anchorScreenX, 1f, BattleLayout.LaneMarkerSpacingPx, i, _markers.Length);
                _markers[i].anchoredPosition = new Vector2(x, markerY);
            }
        }

        private static RectTransform[] CreateTiles(Transform parent, string name, int count, Vector2 size, Color color)
        {
            var tiles = new RectTransform[count];
            for (int i = 0; i < count; i++)
            {
                tiles[i] = UiFactory.CreateImage(parent, name + " " + i, Vector2.zero, size, color).rectTransform;
            }

            return tiles;
        }
    }
}
