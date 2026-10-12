using TAoB.Combat;
using TAoB.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TAoB.View
{
    /// <summary>
    /// 유닛 한 명의 표시: 색 네모 + 직업 글자, 이름, HP·행동 게이지 막대, 상태·과부하 위험·내구력 글자, 강제 발동 가능 표시(빛남).
    /// 연출(수직 슬라이스 계획서 Phase 4.5)은 박스(<see cref="_box"/>)의 위치·회전·배율·색만 바꾼다 — 막대·글자는 고정한다.
    /// </summary>
    internal sealed class UnitView : MonoBehaviour
    {
        private const float BarWidth = 150f;

        /// <summary>구조 중이 아닐 때 기억해 두는 진행 값. 실제 진행(0 이상)과 겹치지 않는다.</summary>
        private const int NotRescuing = -1;

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
        /// <summary>
        /// 빈사가 된 적을 화면에 남겨 두는 시간(게임 시간 초). 1배속에서는 쓰러지는 연출(<see cref="DownedDuration"/>)이 끝난 직후다 —
        /// 마지막 적이 쓰러지면 진형이 바로 전진하므로, 더 오래 두면 그 자리를 지나는 아군과 겹친다.
        /// 배속에서는 진형도 빨리 전진하므로 게임 시간으로 센다(연출이 끝나기 전에 사라져도 겹치는 것보다 낫다).
        /// </summary>
        private const float DownedEnemyLingerSeconds = 0.5f;
        private const float GlowSize = 134f;
        private const float GlowFrequency = 5f;
        private const float GlowMinAlpha = 0.25f;
        private const float GlowMaxAlpha = 0.9f;

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

        /// <summary>자리로 정해지는 Y. X 는 매 프레임 <c>PosX</c> 보간으로 덮어쓴다.</summary>
        private float _baseY;
        private Image _box;
        private Image _glow;
        private Vector2 _boxBasePosition;
        private TMP_Text _hp;
        private TMP_Text _status;
        private TMP_Text _risk;
        private TMP_Text _tool;
        private TMP_Text _stanceTag;
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
        private Stance _lastStance = Stance.Hold;
        private bool _lastFlanking;
        private float _lastHpRatio = -1f;
        private float _lastGaugeRatio = -1f;
        private UnitCondition _lastCondition = UnitCondition.Normal;
        private int _lastRescueProgress = NotRescuing;
        private bool _wasGlowing;
        private bool _wasShown = true;
        private bool _isEnemy;
        private float _downedEnemyElapsed;

        // 연출 타이머. 음수는 비활성.
        private float _lungeElapsed = -1f;
        private float _hitElapsed = -1f;
        private float _hitShakeMagnitude;
        private Color _hitFlashColor;
        private float _scatterElapsed;
        private float _breatheElapsed;
        private float _pulseElapsed;
        private float _downedElapsed = -1f;
        private float _glowElapsed;

        public int Id { get; private set; }

        /// <summary>
        /// 스크롤 층(<c>Lane</c>) 지역 좌표. 팝업처럼 화면 좌표가 필요하면 층·전장 층의 이동량을 더한다
        /// (<see cref="BattleBootstrap"/> 의 팝업 좌표 계산).
        /// </summary>
        public Vector2 Position => _rect.anchoredPosition;

        /// <param name="position">전장 좌표에서 구한 첫 위치. Y 는 자리로 고정이고 X 는 이후 <c>PosX</c> 가 정한다.</param>
        public static UnitView Create(Transform parent, TMP_FontAsset font, CharacterData character, Vector2 position)
        {
            RectTransform rect = UiFactory.CreateRect(parent, "Unit " + character.Id, position, new Vector2(BattleLayout.UnitWidth, BattleLayout.UnitHeight));
            var view = rect.gameObject.AddComponent<UnitView>();
            view._baseY = position.y;
            view.Build(font, character);
            return view;
        }

        private void Build(TMP_FontAsset font, CharacterData character)
        {
            Id = character.Id;
            _rect = (RectTransform)transform;
            _baseColor = BattleLayout.JobColor(character.Job, character.Side);
            _lungeDirection = character.Side == Side.Ally ? 1f : -1f;
            _isEnemy = character.Side == Side.Enemy;

            // 위상을 유닛마다 달리해 여러 유닛이 같은 박자로 숨쉬지 않게 한다.
            _breathePhase = (character.Id * BreathePhaseIdMultiplier) % (Mathf.PI * 2f);

            _boxBasePosition = new Vector2(0f, 30f);
            if (character.IsBoss)
            {
                UiFactory.CreateImage(transform, "BossFrame", _boxBasePosition, new Vector2(124f, 124f), BattleLayout.BossFrameColor);
            }

            // 빛남은 박스보다 먼저 만들어 뒤에 깔린다. 처음에는 보이지 않는다.
            _glow = UiFactory.CreateImage(transform, "Glow", _boxBasePosition, new Vector2(GlowSize, GlowSize), Color.clear);
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

            // 성향 표식은 과부하 위험 글자 위에 둔다. 처음에는 비어 있고 조우가 시작될 때 자세가 정해지면 채워진다.
            _stanceTag = UiFactory.CreateText(transform, "Stance", font, "", 22f, Color.white, new Vector2(0f, 124f), new Vector2(BattleLayout.UnitWidth, 26f));
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

        /// <summary>빈사: 기울며 가라앉는다. 아군은 그 자세로 남고 적은 잠시 뒤 치워진다. 이미 재생 중이면 다시 트리거하지 않는다.</summary>
        public void PlayDowned()
        {
            if (_downedElapsed < 0f) _downedElapsed = 0f;
        }

        /// <summary>구조되어 정상으로 돌아왔을 때 빈사 자세(기울기·가라앉음)를 되돌린다.</summary>
        public void ResetDownedPose()
        {
            _downedElapsed = -1f;
        }

        /// <summary>직전·현재 스냅샷 사이를 보간해 막대를 그리고, 바뀐 값만 글자를 갱신한다.</summary>
        public void Apply(in UnitSnapshot previous, in UnitSnapshot current, in ViewFrame frame)
        {
            // 빈사가 된 적은 잠시 뒤 치운다 — 남겨 두면 진형이 전진하면서 아군과 겹쳐 글자가 뒤엉킨다.
            // 빈사가 된 아군은 사용자가 알아야 하는 정보라 그대로 둔다.
            // 한번 치우면 되돌리지 않는다: 적의 빈사는 끝난 상태다(규칙상 구조는 흩어진 유닛만 되살린다).
            bool isShown = current.IsOnField && _downedEnemyElapsed < DownedEnemyLingerSeconds;
            if (isShown != _wasShown)
            {
                _wasShown = isShown;
                gameObject.SetActive(isShown);
            }

            if (!isShown) return;

            if (_isEnemy && current.Condition == UnitCondition.Downed) _downedEnemyElapsed += frame.GameDt;

            float alpha = frame.Alpha;
            float dt = frame.Dt;

            // 등장하는 틱에는 보간하지 않는다 — 직전 스냅샷의 좌표에서 미끄러져 오면 순간이동처럼 보인다.
            float posX = previous.IsOnField ? Mathf.Lerp(previous.PosX, current.PosX, alpha) : current.PosX;
            _rect.anchoredPosition = new Vector2(BattleLayout.LaneX(posX), _baseY);

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

            UpdateTexts(current, frame.RescueTicks);
            UpdateBoxMotion(current, dt);
            UpdateSkillGlow(frame.IsBattleOn && UnitReadiness.CanForceSkill(current), dt);
        }

        /// <summary>바뀐 값만 글자를 갱신한다(UI 재구성 최소화). 구조자는 상태 글자 자리에 남은 시간을 적는다.</summary>
        private void UpdateTexts(in UnitSnapshot current, int rescueTicks)
        {
            if (current.Hp != _lastHp || current.MaxHp != _lastMaxHp)
            {
                _lastHp = current.Hp;
                _lastMaxHp = current.MaxHp;
                _hp.SetText("{0} / {1}", current.Hp, current.MaxHp);
            }

            int rescueProgress = current.IsRescuing ? current.RescueProgress : NotRescuing;
            if (current.Condition != _lastCondition || rescueProgress != _lastRescueProgress)
            {
                _lastCondition = current.Condition;
                _lastRescueProgress = rescueProgress;

                // 구조자는 남은 시간을 적는다. 정상 상태라 상태 글자와 겹치지 않는다.
                if (current.IsRescuing) _status.SetText("구조 {0:1}", (float)(rescueTicks - current.RescueProgress) / BattleSimulation.TicksPerSecond);
                else _status.text = UiText.Condition(current.Condition);
            }

            if (current.Stance != _lastStance || current.IsFlanking != _lastFlanking)
            {
                _lastStance = current.Stance;
                _lastFlanking = current.IsFlanking;
                _stanceTag.text = UiText.StanceTag(current.Stance, current.IsFlanking);
                _stanceTag.color = BattleLayout.StanceTagColor(current.Stance, current.IsFlanking);
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

        /// <summary>
        /// 기술을 강제 발동할 수 있는 동안 박스 뒤의 테두리가 맥동한다(계획서 Phase D). 박스를 따라 움직여 어긋나지 않게 한다.
        /// 켜져 있는 동안은 매 프레임 색을 쓰고(재사용 대기가 없는 동료는 늘 켜져 있다), 꺼질 때 한 번 지운 뒤로는 쓰지 않는다.
        /// </summary>
        private void UpdateSkillGlow(bool isReady, float dt)
        {
            if (!isReady)
            {
                if (_wasGlowing)
                {
                    _wasGlowing = false;
                    _glowElapsed = 0f;
                    _glow.color = Color.clear;
                }

                return;
            }

            _wasGlowing = true;
            _glowElapsed += dt;
            Color color = BattleLayout.SkillReadyGlowColor;
            color.a = Mathf.Lerp(GlowMinAlpha, GlowMaxAlpha, MotionCurves.Wave01(_glowElapsed, GlowFrequency));
            _glow.color = color;
            _glow.rectTransform.anchoredPosition = _box.rectTransform.anchoredPosition;
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

        /// <summary>빈사: 한 번만 기울며 가라앉고 그 자세로 남는다(구조되면 <see cref="ResetDownedPose"/> 가 되돌린다). 적은 <see cref="DownedEnemyLingerSeconds"/> 뒤 치워진다.</summary>
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
