using TAoB.Combat;
using TAoB.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TAoB.View
{
    /// <summary>
    /// 유닛 한 명의 표시: 색 네모 + 직업 글자, 이름, HP·행동 게이지 막대, 상태·과부하 위험·내구력 글자.
    /// 연출(수직 슬라이스 계획서 Phase 4.5)은 박스(<see cref="_box"/>)의 위치·회전·배율·색만 바꾼다 — 막대·글자는 고정한다.
    /// </summary>
    internal sealed class UnitView : MonoBehaviour
    {
        private const float BarWidth = 150f;

        private const float LungeDuration = 0.18f;
        private const float LungeDistance = 40f;
        private const float HitFlashDuration = 0.15f;
        private const float HitShakeFrequency = 70f;
        private const float HitShakeMagnitude = 6f;
        private const float HitShakeMagnitudeCritical = 11f;
        private const float ScatterFrequency = 22f;
        private const float ScatterMagnitude = 3f;
        private const float BreatheFrequency = 2.4f;
        private const float BreatheAmplitude = 4f;
        private const float PulseBaseFrequency = 6f;
        private const float PulseFrequencyStep = 2.5f;
        private const float PulseAmplitude = 0.08f;
        private const float DownedDuration = 0.4f;
        private const float DownedTiltDegrees = -12f;
        private const float DownedSinkDistance = 14f;

        // 위상을 어긋내 여러 유닛·축이 같은 박자로 움직이지 않게 하는 계수. 값 자체에 의미는 없다(무리수에 가까운 배수일 뿐).
        private const float BreathePhaseIdMultiplier = 0.9137f;
        private const float ScatterYPhaseOffset = 1.7f;

        private static readonly Color BarBackColor = new Color(0.1f, 0.1f, 0.1f, 0.9f);
        private static readonly Color HpColor = new Color(0.3f, 0.85f, 0.35f);
        private static readonly Color GaugeColor = new Color(0.95f, 0.8f, 0.25f);
        private static readonly Color StatusColor = new Color(1f, 0.35f, 0.3f);
        private static readonly Color RiskColor = new Color(1f, 0.6f, 0.2f);
        private static readonly Color ToolColor = new Color(0.8f, 0.85f, 0.95f);

        private RectTransform _rect;
        private Image _box;
        private Vector2 _boxBasePosition;
        private TMP_Text _hp;
        private TMP_Text _status;
        private TMP_Text _risk;
        private TMP_Text _tool;
        private RectTransform _hpFill;
        private RectTransform _gaugeFill;
        private Color _baseColor;
        private float _lungeDirection = 1f;
        private float _breathePhase;

        // 바뀐 값만 글자·막대를 갱신하려고 마지막 표시 값을 기억한다(UI 재구성 최소화).
        private int _lastHp = -1;
        private int _lastMaxHp = -1;
        private int _lastRisk = -1;
        private int _lastDurability = -1;
        private float _lastHpRatio = -1f;
        private float _lastGaugeRatio = -1f;
        private UnitCondition _lastCondition = UnitCondition.Normal;
        private bool _wasOnField = true;

        // 연출 타이머. 음수는 비활성.
        private float _lungeElapsed = -1f;
        private float _hitElapsed = -1f;
        private float _hitShakeMagnitude;
        private Color _hitFlashColor;
        private float _scatterElapsed;
        private float _breatheElapsed;
        private float _pulseElapsed;
        private float _downedElapsed = -1f;

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
            _lungeDirection = character.Side == Side.Ally ? 1f : -1f;

            // 위상을 유닛마다 달리해 여러 유닛이 같은 박자로 숨쉬지 않게 한다.
            _breathePhase = (character.Id * BreathePhaseIdMultiplier) % (Mathf.PI * 2f);

            _boxBasePosition = new Vector2(0f, 30f);
            if (character.IsBoss)
            {
                UiFactory.CreateImage(transform, "BossFrame", _boxBasePosition, new Vector2(124f, 124f), BattleLayout.BossFrameColor);
            }

            _box = UiFactory.CreateImage(transform, "Box", _boxBasePosition, new Vector2(110f, 110f), _baseColor);
            UiFactory.CreateText(_box.transform, "Glyph", font, UiText.JobGlyph(character.Job), 56f, Color.white, Vector2.zero, new Vector2(110f, 110f));

            string name = character.IsBoss ? "★ " + character.Name : character.Name;
            UiFactory.CreateText(transform, "Name", font, name, 24f, Color.white, new Vector2(0f, -42f), new Vector2(BattleLayout.UnitWidth, 30f));

            _hpFill = UiFactory.CreateBar(transform, "Hp", new Vector2(0f, -64f), new Vector2(BarWidth, 16f), BarBackColor, HpColor);
            _hp = UiFactory.CreateText(transform, "HpText", font, "", 16f, Color.white, new Vector2(0f, -64f), new Vector2(BarWidth, 20f));
            _gaugeFill = UiFactory.CreateBar(transform, "Gauge", new Vector2(0f, -82f), new Vector2(BarWidth, 8f), BarBackColor, GaugeColor);

            _status = UiFactory.CreateText(transform, "Status", font, "", 30f, StatusColor, _boxBasePosition, new Vector2(BattleLayout.UnitWidth, 40f));
            _risk = UiFactory.CreateText(transform, "Risk", font, "", 18f, RiskColor, new Vector2(0f, 98f), new Vector2(BattleLayout.UnitWidth, 24f));
            _tool = UiFactory.CreateText(transform, "Tool", font, "", 18f, ToolColor, new Vector2(0f, -100f), new Vector2(BattleLayout.UnitWidth, 24f));
        }

        /// <summary>돌진: 상대 진영 쪽으로 갔다가 돌아온다(가해자, 수직 슬라이스 계획서 Phase 4.5 이벤트 표).</summary>
        public void PlayLunge()
        {
            _lungeElapsed = 0f;
        }

        /// <summary>번쩍임 + 흔들림(피격). 크리티컬이면 흔들림이 커진다.</summary>
        public void PlayHit(bool isCritical)
        {
            _hitElapsed = 0f;
            _hitFlashColor = BattleLayout.DamageColor;
            _hitShakeMagnitude = isCritical ? HitShakeMagnitudeCritical : HitShakeMagnitude;
        }

        /// <summary>주황 번쩍임(자해). 흔들리지는 않는다 — 피격과 구분하기 위함.</summary>
        public void PlaySelfDamageFlash()
        {
            _hitElapsed = 0f;
            _hitFlashColor = BattleLayout.SelfDamageColor;
            _hitShakeMagnitude = 0f;
        }

        /// <summary>빈사: 기울며 가라앉고 그 자세로 남는다. 이미 재생 중이면 다시 트리거하지 않는다.</summary>
        public void PlayDowned()
        {
            if (_downedElapsed < 0f) _downedElapsed = 0f;
        }

        /// <summary>구조되어 정상으로 돌아왔을 때 빈사 자세(기울기·가라앉음)를 되돌린다.</summary>
        public void ResetDownedPose()
        {
            _downedElapsed = -1f;
        }

        /// <summary>직전·현재 스냅샷 사이를 <paramref name="alpha"/> 로 보간해 막대를 그리고, 바뀐 값만 글자를 갱신한다.</summary>
        public void Apply(in UnitSnapshot previous, in UnitSnapshot current, float alpha, float dt)
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

            UpdateBoxMotion(current, dt);
        }

        /// <summary>
        /// 박스(색 네모)의 위치·회전·배율·색을 매 프레임 다시 계산한다. 이 메서드가 호출하는 것들만 <see cref="_box"/> 를 쓴다
        /// (런타임 중 색·변형 갱신은 여기로 단일화 — 생성은 <see cref="Build"/> 가 맡는다). 효과별로 나눠 각자 자기 타이머만 다룬다.
        /// </summary>
        private void UpdateBoxMotion(in UnitSnapshot current, float dt)
        {
            Vector2 offset = Vector2.zero;
            Color boxColor = BaseBoxColor(current.Condition);
            float scale = 1f;
            float rotation = 0f;

            ApplyLunge(ref offset, dt);
            ApplyHitFlash(ref offset, ref boxColor, dt);
            ApplyConditionMotion(ref offset, current.Condition, dt);
            ApplyOverloadPulse(ref scale, current.OverloadRisk, dt);
            ApplyDowned(ref offset, ref rotation, dt);

            _box.color = boxColor;
            _box.rectTransform.anchoredPosition = _boxBasePosition + offset;
            _box.rectTransform.localScale = new Vector3(scale, scale, 1f);
            _box.rectTransform.localRotation = Quaternion.Euler(0f, 0f, rotation);
        }

        /// <summary>돌진: 가해자가 상대 진영 쪽으로 갔다가 돌아온다.</summary>
        private void ApplyLunge(ref Vector2 offset, float dt)
        {
            if (_lungeElapsed < 0f) return;

            offset.x += MotionCurves.Lunge(_lungeElapsed, LungeDuration) * LungeDistance * _lungeDirection;
            _lungeElapsed += dt;
            if (_lungeElapsed >= LungeDuration) _lungeElapsed = -1f;
        }

        /// <summary>번쩍임 + 흔들림: 피격·자해 공통. 지속 시간이 끝나면 기준색으로 돌아온다.</summary>
        private void ApplyHitFlash(ref Vector2 offset, ref Color boxColor, float dt)
        {
            if (_hitElapsed < 0f) return;

            offset.x += MotionCurves.Shake(_hitElapsed, HitFlashDuration, HitShakeFrequency, _hitShakeMagnitude);
            boxColor = Color.Lerp(_hitFlashColor, boxColor, _hitElapsed / HitFlashDuration);
            _hitElapsed += dt;
            if (_hitElapsed >= HitFlashDuration) _hitElapsed = -1f;
        }

        /// <summary>흩어짐: 잘게 떨린다 / 정상: 위아래로 숨쉰다. 두 상태는 배타적이라 같이 나오지 않는다.</summary>
        private void ApplyConditionMotion(ref Vector2 offset, UnitCondition condition, float dt)
        {
            if (condition == UnitCondition.Scattered)
            {
                _scatterElapsed += dt;
                offset.x += MotionCurves.Breathe(_scatterElapsed, _breathePhase, ScatterFrequency, ScatterMagnitude);
                offset.y += MotionCurves.Breathe(_scatterElapsed, _breathePhase + ScatterYPhaseOffset, ScatterFrequency, ScatterMagnitude);
            }
            else
            {
                _scatterElapsed = 0f;
            }

            if (condition == UnitCondition.Normal)
            {
                _breatheElapsed += dt;
                offset.y += MotionCurves.Breathe(_breatheElapsed, _breathePhase, BreatheFrequency, BreatheAmplitude);
            }
        }

        /// <summary>과부하 위험: 단계가 높을수록 빨라지는 맥동.</summary>
        private void ApplyOverloadPulse(ref float scale, int overloadRisk, float dt)
        {
            if (overloadRisk <= 0)
            {
                _pulseElapsed = 0f;
                return;
            }

            _pulseElapsed += dt;
            scale = MotionCurves.Pulse(_pulseElapsed, overloadRisk, PulseBaseFrequency, PulseFrequencyStep, PulseAmplitude);
        }

        /// <summary>빈사: 한 번만 기울며 가라앉고 그 자세로 남는다(구조되면 <see cref="ResetDownedPose"/> 가 되돌린다).</summary>
        private void ApplyDowned(ref Vector2 offset, ref float rotation, float dt)
        {
            if (_downedElapsed < 0f) return;

            float progress = Mathf.Clamp01(_downedElapsed / DownedDuration);
            offset.y -= DownedSinkDistance * progress;
            rotation = DownedTiltDegrees * progress;
            if (_downedElapsed < DownedDuration) _downedElapsed += dt;
        }

        private Color BaseBoxColor(UnitCondition condition)
        {
            return condition == UnitCondition.Downed ? BattleLayout.DownedColor : _baseColor;
        }
    }
}
