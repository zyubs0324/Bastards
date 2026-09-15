using System;
using System.Collections.Generic;
using TAoB.Combat;
using TAoB.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TAoB.View
{
    /// <summary>
    /// 하단 개입 바(계획서 Phase 4): 아군별 고유 기술 버튼, 과부하 버튼(원석 + 과부하 가능 기술), 구조, 후퇴.
    /// 기본 공격은 자동 전용이라 버튼이 없다. 명령은 규칙 모듈의 <see cref="BattleSimulation.Enqueue"/> 로만 들어간다.
    /// </summary>
    internal sealed class InterventionBar : MonoBehaviour
    {
        private const float ToastSeconds = 2f;

        private sealed class AllyControls
        {
            public int Id;
            public Button Skill;
            public TMP_Text SkillLabel;
            public Button Overload;
            public int LastCooldown = -1;
            public bool WasSkillEnabled = true;
            public bool WasOverloadEnabled = true;
        }

        private readonly List<AllyControls> _allies = new List<AllyControls>(5);
        private Button _rescue;
        private Button _retreat;
        private TMP_Text _toast;
        private float _toastAge;
        private bool _wasRescueEnabled = true;
        private Func<BattleCommand, CommandResult> _submit;
        private BattleSnapshot _latest;

        public static InterventionBar Create(Transform parent, TMP_FontAsset font, IReadOnlyList<CharacterData> allies, Func<BattleCommand, CommandResult> submit)
        {
            RectTransform rect = UiFactory.CreateStretched(parent, "InterventionBar");
            var bar = rect.gameObject.AddComponent<InterventionBar>();
            bar._submit = submit;
            bar.Build(font, allies);
            return bar;
        }

        private void Build(TMP_FontAsset font, IReadOnlyList<CharacterData> allies)
        {
            UiFactory.CreateImage(transform, "Background", new Vector2(0f, -470f), new Vector2(BattleLayout.Width, 140f), BattleLayout.PanelColor);

            for (int i = 0; i < allies.Count; i++)
            {
                CharacterData ally = allies[i];
                float x = -760f + i * 300f;
                var controls = new AllyControls { Id = ally.Id };

                UiFactory.CreateText(transform, "AllyName", font, ally.Name, 24f, Color.white, new Vector2(x, -418f), new Vector2(280f, 30f));
                controls.Skill = UiFactory.CreateButton(
                    transform, "Skill", font, "기술", new Vector2(x - 60f, -480f), new Vector2(160f, 64f), out controls.SkillLabel, out _);

                int id = ally.Id;
                controls.Skill.onClick.AddListener(() => Report(_submit(BattleCommand.UseSkill(id))));

                // 과부하 가능 조건은 규칙 모듈이 정본이다(명령 수락 조건과 버튼 활성 조건이 갈리지 않게).
                if (OverloadResolver.CanOverload(ally.Catalyst?.Catalyst, ally.Skill))
                {
                    controls.Overload = UiFactory.CreateButton(
                        transform, "Overload", font, "과부하", new Vector2(x + 75f, -480f), new Vector2(100f, 64f), out _, out _);
                    controls.Overload.onClick.AddListener(() => Report(_submit(BattleCommand.Overload(id))));
                }

                _allies.Add(controls);
            }

            _rescue = UiFactory.CreateButton(transform, "Rescue", font, "구조", new Vector2(600f, -480f), new Vector2(150f, 64f), out _, out _);
            _rescue.onClick.AddListener(SubmitRescue);
            _retreat = UiFactory.CreateButton(transform, "Retreat", font, "후퇴", new Vector2(790f, -480f), new Vector2(150f, 64f), out _, out _);
            _retreat.onClick.AddListener(() => Report(_submit(BattleCommand.Retreat())));

            _toast = UiFactory.CreateText(transform, "Toast", font, "", 26f, BattleLayout.ToastColor, new Vector2(0f, -385f), new Vector2(900f, 36f));
        }

        public void Apply(BattleSnapshot snapshot)
        {
            _latest = snapshot;
            bool isBattleOn = snapshot.Outcome == BattleOutcome.InProgress;

            for (int i = 0; i < snapshot.Units.Count; i++)
            {
                UnitSnapshot unit = snapshot.Units[i];
                if (unit.Side != Side.Ally) continue;

                AllyControls controls = FindControls(unit.Id);
                if (controls == null) continue;

                bool isReady = isBattleOn && unit.IsOnField && unit.Condition == UnitCondition.Normal && unit.CooldownRemaining == 0;
                if (isReady != controls.WasSkillEnabled)
                {
                    controls.WasSkillEnabled = isReady;
                    controls.Skill.interactable = isReady;
                }

                if (unit.CooldownRemaining != controls.LastCooldown)
                {
                    controls.LastCooldown = unit.CooldownRemaining;

                    // TMP 의 SetText 는 .NET 서식이 아니라 자체 파서를 쓴다. `{0:1}` 이 소수 1자리이며 `F1` 은 글자 그대로 찍힌다.
                    if (unit.CooldownRemaining > 0) controls.SkillLabel.SetText("기술 {0:1}초", (float)unit.CooldownRemaining / BattleSimulation.TicksPerSecond);
                    else controls.SkillLabel.text = "기술";
                }

                if (controls.Overload != null && isReady != controls.WasOverloadEnabled)
                {
                    controls.WasOverloadEnabled = isReady;
                    controls.Overload.interactable = isReady;
                }
            }

            bool isRescueEnabled = isBattleOn && RescueSelector.CanRescue(snapshot.Units);
            if (isRescueEnabled != _wasRescueEnabled)
            {
                _wasRescueEnabled = isRescueEnabled;
                _rescue.interactable = isRescueEnabled;
            }

            _retreat.interactable = isBattleOn;
        }

        private AllyControls FindControls(int id)
        {
            for (int i = 0; i < _allies.Count; i++)
            {
                if (_allies[i].Id == id) return _allies[i];
            }

            return null;
        }

        private void SubmitRescue()
        {
            if (_latest == null) return;

            RescuePair pair = RescueSelector.Select(_latest.Units);
            if (!pair.HasPair)
            {
                Report(CommandResult.RejectedInvalidTarget);
                return;
            }

            Report(_submit(BattleCommand.Rescue(pair.RescuerId, pair.TargetId)));
        }

        private void Report(CommandResult result)
        {
            _toast.text = UiText.CommandResult(result);
            _toast.gameObject.SetActive(true);
            _toastAge = 0f;
        }

        private void Update()
        {
            if (!_toast.gameObject.activeSelf) return;
            _toastAge += Time.deltaTime;
            if (_toastAge >= ToastSeconds) _toast.gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            for (int i = 0; i < _allies.Count; i++)
            {
                _allies[i].Skill.onClick.RemoveAllListeners();
                if (_allies[i].Overload != null) _allies[i].Overload.onClick.RemoveAllListeners();
            }

            _rescue.onClick.RemoveAllListeners();
            _retreat.onClick.RemoveAllListeners();
        }
    }
}
