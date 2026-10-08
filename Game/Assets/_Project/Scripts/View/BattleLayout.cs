using TAoB.Combat;
using UnityEngine;

namespace TAoB.View
{
    /// <summary>기준 해상도 1920×1080 가로 화면의 배치 상수. 좌표는 화면 중심 기준.</summary>
    public static class BattleLayout
    {
        public const float Width = 1920f;
        public const float Height = 1080f;
        public const float UnitWidth = 170f;
        public const float UnitHeight = 210f;

        private const float RowSpacing = 215f;
        private const float FieldCenterY = 40f;

        /// <summary>
        /// 전장 좌표(fu) → 화면 px 환산의 **유일한 지점**(계획서 D3). 기준 해상도에서 1fu = 1px 로 잡았지만
        /// 값이 1 이어도 상수로 남긴다 — 기준 해상도가 바뀌면 여기만 고친다.
        /// </summary>
        public const float PxPerFu = 1f;

        /// <summary>
        /// 아군 진형 기준점이 놓이는 화면 X. 중앙보다 왼쪽에 두어 앞쪽(오른쪽)에서 다가오는 적이 미리 보이게 한다.
        /// </summary>
        public const float AllyAnchorScreenX = -500f;

        /// <summary>
        /// 배경의 하늘·바닥 경계 y좌표. 아래열 유닛의 가장 낮은 글자(내구력 표시, 약 -180)보다 아래여야 선이 유닛을 가로지르지 않는다.
        /// </summary>
        public const float GroundTopY = -190f;

        /// <summary>
        /// 배경을 화면보다 크게 그려, 화면 흔들림 진폭 안에서는 가장자리가 드러나지 않게 한다.
        /// 폭은 설계 폭이 아니라 <see cref="UiFit.MaxCanvasWidth"/> 기준이다 — 가로로 긴 폰에서 가장자리가 비지 않게.
        /// </summary>
        public const float BackgroundOverscan = 40f;

        /// <summary>팝업 풀 객체 이름. 플레이 스모크의 재시작 누수 검사가 이 이름 아래를 개수에서 빼므로 한 곳에서 정한다.</summary>
        public const string PopupPoolName = "PopupPool";

        /// <summary>스크롤 층 객체 이름. 플레이 스모크가 유닛 표시를 이 아래에서 찾으므로 한 곳에서 정한다.</summary>
        public const string LaneName = "Lane";

        // 배경 띠(계획서 Phase B) — 색 네모만 쓰는 단계라 바닥에 일정 간격의 표식을 세워 흐름을 보인다.
        // 묶음은 화면 중앙을 가운데로 펼치고 랩어라운드로 최대 한 칸 밀리므로,
        // 반폭(간격 × (개수−1) ÷ 2)이 지원하는 가장 넓은 화면의 반폭 + 한 칸 이상이어야 가장자리가 비지 않는다.
        public const float LaneMarkerSpacingPx = 240f;
        public const int LaneMarkerCount = 15;
        public const float LaneMarkerWidth = 10f;
        public const float LaneMarkerHeight = 70f;
        public const float LaneFarSpacingPx = 520f;
        public const int LaneFarCount = 9;
        public const float LaneFarWidth = 26f;
        public const float LaneFarHeight = 150f;

        /// <summary>먼 층이 흐르는 속도 비율. 1 이면 전장과 같은 속도라 깊이가 느껴지지 않는다.</summary>
        public const float LaneFarParallax = 0.45f;

        public static readonly Color LaneMarkerColor = new Color(0.18f, 0.21f, 0.24f);
        public static readonly Color LaneFarColor = new Color(0.2f, 0.24f, 0.32f);

        public static readonly Color DownedColor = new Color(0.3f, 0.3f, 0.3f);
        public static readonly Color BossFrameColor = new Color(1f, 0.85f, 0.2f);
        public static readonly Color PanelColor = new Color(0.08f, 0.08f, 0.1f, 0.85f);
        public static readonly Color DimColor = new Color(0f, 0f, 0f, 0.6f);
        public static readonly Color ButtonColor = new Color(0.25f, 0.3f, 0.4f);
        public static readonly Color ButtonActiveColor = new Color(0.9f, 0.6f, 0.2f);
        public static readonly Color ButtonDisabledColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
        public static readonly Color FaintTextColor = new Color(0.7f, 0.7f, 0.7f);
        public static readonly Color ToastColor = new Color(1f, 0.9f, 0.6f);

        // 배경(수직 슬라이스 계획서 Phase 4.5) — 검은 허공 대신 하늘·바닥 두 색 띠와 경계선을 그린다.
        public static readonly Color SkyColor = new Color(0.16f, 0.2f, 0.28f);
        public static readonly Color GroundColor = new Color(0.1f, 0.12f, 0.14f);
        public static readonly Color GroundLineColor = new Color(0.28f, 0.32f, 0.38f);

        // 이벤트 팝업 색. 종류를 색으로 구분해 한 화면에 여러 알림이 떠도 읽히게 한다.
        public static readonly Color DamageColor = Color.white;
        public static readonly Color CriticalColor = Color.yellow;
        public static readonly Color SelfDamageColor = new Color(1f, 0.45f, 0.3f);
        public static readonly Color ScatterDamageColor = new Color(0.7f, 0.7f, 0.9f);
        public static readonly Color DownedTextColor = new Color(1f, 0.3f, 0.3f);
        public static readonly Color OverloadColor = new Color(1f, 0.7f, 0.2f);
        public static readonly Color BreakthroughColor = new Color(0.6f, 0.9f, 1f);
        public static readonly Color RescuedColor = new Color(0.5f, 1f, 0.6f);
        public static readonly Color FallbackColor = new Color(0.8f, 0.8f, 0.8f);
        public static readonly Color MagitoolColor = new Color(0.9f, 0.8f, 0.6f);

        /// <summary>
        /// 같은 진영·같은 열 안에서 위에서 아래로 <paramref name="slot"/> 번째 자리의 Y.
        /// X 는 규칙 모듈의 좌표(<c>PosX</c>)에서 오므로 배치가 정하지 않는다(계획서 D3).
        /// </summary>
        public static float UnitY(int slot, int slotCount)
        {
            return FieldCenterY + ((slotCount - 1) * 0.5f - slot) * RowSpacing;
        }

        /// <summary>전장 좌표를 화면 X 로 바꾼다. 스크롤 층(<c>Lane</c>) 안의 지역 좌표다.</summary>
        public static float LaneX(float posXFu)
        {
            return posXFu * PxPerFu;
        }

        /// <summary>바닥 표식의 Y. 경계선 아래(바닥 쪽)에 세운다.</summary>
        public static float LaneMarkerY()
        {
            return GroundTopY - LaneMarkerHeight * 0.5f;
        }

        /// <summary>먼 층의 Y. 경계선 위(하늘 쪽)에 세워 깊이를 만든다.</summary>
        public static float LaneFarY()
        {
            return GroundTopY + LaneFarHeight * 0.5f;
        }

        /// <summary>띠 묶음이 가장자리까지 덮는가. 반폭이 지원하는 가장 넓은 화면의 반폭 + 한 칸(랩어라운드 밀림) 이상이어야 한다.</summary>
        public static bool TilesCoverScreen(float spacingPx, int count)
        {
            return spacingPx * (count - 1) * 0.5f >= UiFit.MaxCanvasWidth * 0.5f + spacingPx;
        }

        public static Color JobColor(Job job, Side side)
        {
            Color color;
            switch (job)
            {
                case Job.Knight: color = new Color(0.35f, 0.55f, 0.9f); break;
                case Job.Mage: color = new Color(0.7f, 0.45f, 0.9f); break;
                default: color = new Color(0.45f, 0.8f, 0.5f); break;
            }

            // 적은 같은 직업색을 어둡게 해 진영을 구분한다.
            return side == Side.Enemy ? color * 0.6f + new Color(0.25f, 0.05f, 0.05f) : color;
        }
    }
}
