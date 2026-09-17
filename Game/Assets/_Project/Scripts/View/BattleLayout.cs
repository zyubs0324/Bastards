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

        private const float FrontX = 330f;
        private const float BackX = 560f;
        private const float RowSpacing = 215f;
        private const float FieldCenterY = 40f;

        /// <summary>배경의 하늘·바닥 경계 y좌표(유닛 발밑 바로 아래).</summary>
        public const float GroundTopY = -60f;

        /// <summary>배경을 화면보다 크게 그려, 화면 흔들림 진폭 안에서는 가장자리가 드러나지 않게 한다.</summary>
        public const float BackgroundOverscan = 40f;

        public static readonly Color DownedColor = new Color(0.3f, 0.3f, 0.3f);
        public static readonly Color BossFrameColor = new Color(1f, 0.85f, 0.2f);
        public static readonly Color PanelColor = new Color(0.08f, 0.08f, 0.1f, 0.85f);
        public static readonly Color DimColor = new Color(0f, 0f, 0f, 0.6f);
        public static readonly Color ButtonColor = new Color(0.25f, 0.3f, 0.4f);
        public static readonly Color ButtonActiveColor = new Color(0.9f, 0.6f, 0.2f);
        public static readonly Color ButtonDisabledColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
        public static readonly Color FaintTextColor = new Color(0.7f, 0.7f, 0.7f);
        public static readonly Color ToastColor = new Color(1f, 0.9f, 0.6f);

        // 배경(계획서 Phase 4.5) — 검은 허공 대신 하늘·바닥 두 색 띠와 경계선을 그린다.
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

        /// <summary>같은 진영·같은 열 안에서 위에서 아래로 <paramref name="slot"/> 번째 자리. 아군은 왼쪽, 적은 오른쪽, 앞열이 중앙에 가깝다.</summary>
        public static Vector2 UnitPosition(Side side, Row row, int slot, int slotCount)
        {
            float x = row == Row.Front ? FrontX : BackX;
            if (side == Side.Ally) x = -x;
            float y = FieldCenterY + ((slotCount - 1) * 0.5f - slot) * RowSpacing;
            return new Vector2(x, y);
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
