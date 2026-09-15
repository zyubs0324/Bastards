using TAoB.Combat;
using TAoB.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TAoB.View
{
    /// <summary>유닛 한 명의 표시: 색 네모 + 직업 글자, 이름, HP·행동 게이지 막대, 상태·과부하 위험·내구력 글자.</summary>
    internal sealed class UnitView : MonoBehaviour
    {
        private const float BarWidth = 150f;

        private static readonly Color BarBackColor = new Color(0.1f, 0.1f, 0.1f, 0.9f);
        private static readonly Color HpColor = new Color(0.3f, 0.85f, 0.35f);
        private static readonly Color GaugeColor = new Color(0.95f, 0.8f, 0.25f);
        private static readonly Color StatusColor = new Color(1f, 0.35f, 0.3f);
        private static readonly Color RiskColor = new Color(1f, 0.6f, 0.2f);
        private static readonly Color ToolColor = new Color(0.8f, 0.85f, 0.95f);

        private RectTransform _rect;
        private Image _box;
        private TMP_Text _hp;
        private TMP_Text _status;
        private TMP_Text _risk;
        private TMP_Text _tool;
        private RectTransform _hpFill;
        private RectTransform _gaugeFill;
        private Color _baseColor;

        // 바뀐 값만 글자·막대를 갱신하려고 마지막 표시 값을 기억한다(UI 재구성 최소화).
        private int _lastHp = -1;
        private int _lastMaxHp = -1;
        private int _lastRisk = -1;
        private int _lastDurability = -1;
        private float _lastHpRatio = -1f;
        private float _lastGaugeRatio = -1f;
        private UnitCondition _lastCondition = UnitCondition.Normal;
        private bool _wasOnField = true;

        public int Id { get; private set; }

        /// <summary>화면 중심 기준 위치. 팝업을 띄울 때 쓴다.</summary>
        public Vector2 Position => _rect.anchoredPosition;

        public static UnitView Create(Transform parent, TMP_FontAsset font, CharacterData character, Vector2 position)
        {
            RectTransform rect = UiFactory.CreateRect(parent, "Unit " + character.Id, position, new Vector2(BattleLayout.UnitWidth, BattleLayout.UnitHeight));
            var view = rect.gameObject.AddComponent<UnitView>();
            view.Build(font, character);
            return view;
        }

        private void Build(TMP_FontAsset font, CharacterData character)
        {
            Id = character.Id;
            _rect = (RectTransform)transform;
            _baseColor = BattleLayout.JobColor(character.Job, character.Side);

            var boxPosition = new Vector2(0f, 30f);
            if (character.IsBoss)
            {
                UiFactory.CreateImage(transform, "BossFrame", boxPosition, new Vector2(124f, 124f), BattleLayout.BossFrameColor);
            }

            _box = UiFactory.CreateImage(transform, "Box", boxPosition, new Vector2(110f, 110f), _baseColor);
            UiFactory.CreateText(_box.transform, "Glyph", font, UiText.JobGlyph(character.Job), 56f, Color.white, Vector2.zero, new Vector2(110f, 110f));

            string name = character.IsBoss ? "★ " + character.Name : character.Name;
            UiFactory.CreateText(transform, "Name", font, name, 24f, Color.white, new Vector2(0f, -42f), new Vector2(BattleLayout.UnitWidth, 30f));

            _hpFill = UiFactory.CreateBar(transform, "Hp", new Vector2(0f, -64f), new Vector2(BarWidth, 16f), BarBackColor, HpColor);
            _hp = UiFactory.CreateText(transform, "HpText", font, "", 16f, Color.white, new Vector2(0f, -64f), new Vector2(BarWidth, 20f));
            _gaugeFill = UiFactory.CreateBar(transform, "Gauge", new Vector2(0f, -82f), new Vector2(BarWidth, 8f), BarBackColor, GaugeColor);

            _status = UiFactory.CreateText(transform, "Status", font, "", 30f, StatusColor, boxPosition, new Vector2(BattleLayout.UnitWidth, 40f));
            _risk = UiFactory.CreateText(transform, "Risk", font, "", 18f, RiskColor, new Vector2(0f, 98f), new Vector2(BattleLayout.UnitWidth, 24f));
            _tool = UiFactory.CreateText(transform, "Tool", font, "", 18f, ToolColor, new Vector2(0f, -100f), new Vector2(BattleLayout.UnitWidth, 24f));
        }

        /// <summary>직전·현재 스냅샷 사이를 <paramref name="alpha"/> 로 보간해 막대를 그리고, 바뀐 값만 글자를 갱신한다.</summary>
        public void Apply(in UnitSnapshot previous, in UnitSnapshot current, float alpha)
        {
            if (current.IsOnField != _wasOnField)
            {
                _wasOnField = current.IsOnField;
                gameObject.SetActive(current.IsOnField);
            }

            if (!current.IsOnField) return;

            // 막대 폭을 다시 쓰면 레이아웃이 더러워지므로, 보간 결과가 같은 프레임에는 건드리지 않는다.
            float hpRatio = Mathf.Lerp(previous.Hp, current.Hp, alpha) / current.MaxHp;
            if (hpRatio != _lastHpRatio)
            {
                _lastHpRatio = hpRatio;
                UiFactory.SetBarFill(_hpFill, BarWidth, hpRatio);
            }

            float gaugeRatio = Mathf.Lerp(previous.ActionGauge, current.ActionGauge, alpha) / current.ActionGaugeMax;
            if (gaugeRatio != _lastGaugeRatio)
            {
                _lastGaugeRatio = gaugeRatio;
                UiFactory.SetBarFill(_gaugeFill, BarWidth, gaugeRatio);
            }

            if (current.Hp != _lastHp || current.MaxHp != _lastMaxHp)
            {
                _lastHp = current.Hp;
                _lastMaxHp = current.MaxHp;
                _hp.SetText("{0} / {1}", current.Hp, current.MaxHp);
            }

            if (current.Condition != _lastCondition)
            {
                _lastCondition = current.Condition;
                _status.text = UiText.Condition(current.Condition);
                _box.color = current.Condition == UnitCondition.Downed ? BattleLayout.DownedColor : _baseColor;
            }

            if (current.OverloadRisk != _lastRisk)
            {
                _lastRisk = current.OverloadRisk;
                if (current.OverloadRisk > 0) _risk.SetText("과부하 위험 {0}", current.OverloadRisk);
                else _risk.text = "";
            }

            if (current.HasMagitool && current.MagitoolDurability != _lastDurability)
            {
                _lastDurability = current.MagitoolDurability;
                _tool.SetText("내구 {0} / {1}", current.MagitoolDurability, current.MagitoolMaxDurability);
            }
        }
    }
}
