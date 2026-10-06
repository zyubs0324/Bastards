using UnityEngine;

namespace TAoB.View
{
    /// <summary>
    /// 전장 스크롤의 순수 계산(계획서 Phase B). 엔진 객체를 만지지 않아 테스트가 쉽다.
    /// 스크롤 양은 <see cref="AnchorPx"/> 로 스냅샷에서만 구한다 — 별도 시간 누적을 두면 배속에서 배경과 유닛이 어긋난다.
    /// <see cref="MotionCurves"/>·<see cref="TickDriver"/> 와 같이 테스트 어셈블리에서 직접 검증하므로 공개한다.
    /// </summary>
    public static class LaneScroll
    {
        /// <summary>직전·현재 기준점을 보간한 화면 좌표(px).</summary>
        public static float AnchorPx(int previousAnchorX, int currentAnchorX, float alpha, float pxPerFu)
        {
            return Mathf.Lerp(previousAnchorX, currentAnchorX, alpha) * pxPerFu;
        }

        /// <summary>
        /// <paramref name="period"/> 로 나눈 나머지를 항상 0 이상으로 돌려준다.
        /// C# 의 <c>%</c> 는 음수에서 음수를 내므로 그대로 쓰면 뒤로 스크롤할 때 띠가 한 칸 튄다.
        /// </summary>
        public static float Wrap(float value, float period)
        {
            if (period <= 0f) return 0f;

            return value - Mathf.Floor(value / period) * period;
        }

        /// <summary>
        /// 스크롤 층(<c>Lane</c>)의 화면 X. 진형 기준점이 <paramref name="anchorScreenX"/> 에 머물도록 스크롤만큼 왼쪽으로 옮긴다.
        /// </summary>
        public static float LaneOffsetX(float scrollPx, float anchorScreenX)
        {
            return anchorScreenX - scrollPx;
        }

        /// <summary>
        /// 띠 묶음을 펼칠 가운데의 지역 X — **화면 중앙에 해당하는 지점**이다.
        /// 진형 기준점(화면 왼쪽에 치우쳐 있다)에 맞추면 오른쪽 끝이 치우친 만큼 비어 띠가 화면 안에서 튀어나온다.
        /// </summary>
        public static float CenterLocalX(float scrollPx, float anchorScreenX)
        {
            return scrollPx - anchorScreenX;
        }

        /// <summary>
        /// 층 하나의 띠 한 칸이 스크롤 층 안에서 갖는 지역 X. <c>LaneView</c> 가 쓰는 합성식 그대로라,
        /// 테스트가 같은 함수로 화면 덮임을 검증한다(식을 따로 베끼면 합성이 어긋나도 테스트가 통과한다).
        /// </summary>
        /// <param name="parallaxFactor">1 이면 전장과 같은 속도, 작을수록 느리게 흘러 멀어 보인다.</param>
        public static float TileLocalXAt(float scrollPx, float anchorScreenX, float parallaxFactor, float spacingPx, int index, int count)
        {
            return TileLocalX(CenterLocalX(scrollPx, anchorScreenX), scrollPx * parallaxFactor, spacingPx, index, count);
        }

        /// <summary>띠 한 칸이 화면에 나타나는 X(= 층 위치 + 지역 X).</summary>
        public static float TileScreenX(float scrollPx, float anchorScreenX, float parallaxFactor, float spacingPx, int index, int count)
        {
            return LaneOffsetX(scrollPx, anchorScreenX) + TileLocalXAt(scrollPx, anchorScreenX, parallaxFactor, spacingPx, index, count);
        }

        /// <summary>
        /// 배경 띠 한 칸의 지역 X. 같은 개수의 띠를 랩어라운드로 돌려 써서, 스크롤이 아무리 커져도
        /// 띠 묶음은 가운데 주변 일정 범위(묶음 폭 + 한 칸)에 머문다 — 띠를 새로 만들지 않는다.
        /// </summary>
        /// <param name="centerLocalX">묶음의 가운데를 둘 지역 좌표. 보통 <see cref="CenterLocalX"/> 의 값이다.</param>
        /// <param name="movedPx">패럴랙스 속도로 흐른 거리(= 스크롤 × 계수). 이 값만 랩어라운드에 쓴다.</param>
        /// <param name="index">0 부터 <paramref name="count"/> − 1.</param>
        public static float TileLocalX(float centerLocalX, float movedPx, float spacingPx, int index, int count)
        {
            if (spacingPx <= 0f || count <= 0) return centerLocalX;

            float wrapped = Wrap(movedPx, spacingPx);
            float half = (count - 1) * 0.5f;
            return centerLocalX - wrapped + (index - half) * spacingPx;
        }
    }
}
