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
    /// 하단 개입 바(수직 슬라이스 계획서 Phase 4): 아군별 고유 기술 버튼, 과부하 버튼(원석 + 과부하 가능 기술), 소모품, 구조, 후퇴.
    /// 기본 공격은 자동 전용이라 버튼이 없다. 명령은 규칙 모듈의 <see cref="BattleSimulation.Enqueue"/> 로만 들어간다.
    /// </summary>
    internal sealed class InterventionBar : MonoBehaviour
    {
        private const float ToastSeconds = 2f;
        private const float ButtonRowY = -480f;
        private const float ButtonHeight = 64f;

        // 소모품 버튼은 구조 버튼 왼쪽에 두고, 종류가 늘면 왼쪽으로 늘어난다.
        private const float ItemButtonX = 395f;
        private const float ItemButtonSpacing = 200f;
        private const float ItemButtonWidth = 190f;

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

        private sealed class ItemControls
        {
            public int Id;
            public string Name;
            public Button Button;
            public TMP_Text Label;
            public int LastRemaining = -1;
            public bool WasEnabled = true;
        }

        private readonly List<AllyControls> _allies = new List<AllyControls>(5);
        private readonly List<ItemControls> _items = new List<ItemControls>(2);
        private Button _rescue;
        private Button _retreat;
        private TMP_Text _toast;
        private float _toastAge;
        private bool _wasRescueEnabled = true;
        private Func<BattleCommand, CommandResult> _submit;
        private BattleSnapshot _latest;

        public static InterventionBar Create(
            Transform parent, TMP_FontAsset font, IReadOnlyList<CharacterData> allies, IReadOnlyList<ConsumableData> consumables,
            Func<BattleCommand, CommandResult> submit)
        {
            RectTransform rect = UiFactory.CreateStretched(parent, "InterventionBar");
            var bar = rect.gameObject.AddComponent<InterventionBar>();
            bar._submit = submit;
            bar.Build(font, allies);
            bar.BuildItems(font, consumables);
            return bar;
        }

        private void BuildItems(TMP_FontAsset font, IReadOnlyList<ConsumableData> consumables)
        {
            for (int i = 0; i < consumables.Count; i++)
            {
                ConsumableData item = consumables[i];
                var controls = new ItemControls { Id = item.Id, Name = item.Name };
                controls.Button = UiFactory.CreateButton(
                    transform, "Item", font, item.Name, new Vector2(ItemButtonX - i * ItemButtonSpacing, ButtonRowY), new Vector2(ItemButtonWidth, ButtonHeight),
                    out controls.Label, out _);

                int id = item.Id;
                controls.Button.onClick.AddListener(() => Report(_submit(BattleCommand.UseItem(id))));
                _items.Add(controls);
            }
        }

        private void Build(TMP_FontAsset font, IReadOnlyList<CharacterData> allies)
        {
            // 배경 띠는 설계 영역을 넘어 화면 끝까지 — 가로로 넓은 화면의 양옆, 좁은 화면에서 설계 영역이 줄어들며 생기는
            // 아래 빈 띠, 노치 쪽으로 치우친 가장자리를 채운다. 이 층은 축소·이동하는 컨테이너 아래라 bleed 크기로 만든다.
            // 윗변(-400)은 그대로 두고 아래로만 늘린다.
            const float top = -400f;
            UiFactory.CreateImage(
                transform, "Background", new Vector2(0f, top - UiFit.BleedSize * 0.5f), new Vector2(UiFit.BleedSize, UiFit.BleedSize), BattleLayout.PanelColor);

            for (int i = 0; i < allies.Count; i++)
            {
                CharacterData ally = allies[i];
                float x = -760f + i * 300f;
                var controls = new AllyControls { Id = ally.Id };

                UiFactory.CreateText(transform, "AllyName", font, ally.Name, 24f, Color.white, new Vector2(x, -418f), new Vector2(280f, 30f));
                controls.Skill = UiFactory.CreateButton(
                    transform, "Skill", font, "기술", new Vector2(x - 60f, ButtonRowY), new Vector2(160f, ButtonHeight), out controls.SkillLabel, out _);

                int id = ally.Id;
                controls.Skill.onClick.AddListener(() => Report(_submit(BattleCommand.UseSkill(id))));

                // 과부하 가능 조건은 규칙 모듈이 정본이다(명령 수락 조건과 버튼 활성 조건이 갈리지 않게).
                if (OverloadResolver.CanOverload(ally.Catalyst?.Catalyst, ally.Skill))
                {
                    controls.Overload = UiFactory.CreateButton(
                        transform, "Overload", font, "과부하", new Vector2(x + 75f, ButtonRowY), new Vector2(100f, ButtonHeight), out _, out _);
                    controls.Overload.onClick.AddListener(() => Report(_submit(BattleCommand.Overload(id))));
                }

                _allies.Add(controls);
            }

            _rescue = UiFactory.CreateButton(transform, "Rescue", font, "구조", new Vector2(600f, ButtonRowY), new Vector2(150f, ButtonHeight), out _, out _);
            _rescue.onClick.AddListener(SubmitRescue);
            _retreat = UiFactory.CreateButton(transform, "Retreat", font, "후퇴", new Vector2(790f, ButtonRowY), new Vector2(150f, ButtonHeight), out _, out _);
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

                bool isReady = isBattleOn && UnitReadiness.CanForceSkill(unit);
                SetInteractable(controls.Skill, ref controls.WasSkillEnabled, isReady);

                if (unit.CooldownRemaining != controls.LastCooldown)
                {
                    controls.LastCooldown = unit.CooldownRemaining;

                    // TMP 의 SetText 는 .NET 서식이 아니라 자체 파서를 쓴다. `{0:1}` 이 소수 1자리이며 `F1` 은 글자 그대로 찍힌다.
                    if (unit.CooldownRemaining > 0) controls.SkillLabel.SetText("기술 {0:1}초", (float)unit.CooldownRemaining / BattleSimulation.TicksPerSecond);
                    else controls.SkillLabel.text = "기술";
                }

                if (controls.Overload != null) SetInteractable(controls.Overload, ref controls.WasOverloadEnabled, isReady);
            }

            ApplyItems(snapshot, isBattleOn);

            SetInteractable(_rescue, ref _wasRescueEnabled, isBattleOn && RescueSelector.CanRescue(snapshot.Units));

            _retreat.interactable = isBattleOn;
        }

        private void ApplyItems(BattleSnapshot snapshot, bool isBattleOn)
        {
            for (int i = 0; i < snapshot.Items.Count; i++)
            {
                ItemSnapshot item = snapshot.Items[i];
                ItemControls controls = FindItem(item.Id);
                if (controls == null) continue;

                // 남은 개수가 바뀔 때만 글자를 다시 만든다(문자열 할당은 사용할 때 한 번).
                if (item.Remaining != controls.LastRemaining)
                {
                    controls.LastRemaining = item.Remaining;
                    controls.Label.text = controls.Name + " ×" + item.Remaining;
                }

                SetInteractable(controls.Button, ref controls.WasEnabled, isBattleOn && item.Remaining > 0);
            }
        }

        /// <summary>바뀔 때만 버튼을 건드린다 — 같은 값을 매 프레임 다시 쓰면 캔버스가 더러워진다.</summary>
        private static void SetInteractable(Button button, ref bool wasEnabled, bool isEnabled)
        {
            if (isEnabled == wasEnabled) return;

            wasEnabled = isEnabled;
            button.interactable = isEnabled;
        }

        private ItemControls FindItem(int id)
        {
            for (int i = 0; i < _items.Count; i++)
            {
                if (_items[i].Id == id) return _items[i];
            }

            return null;
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

            for (int i = 0; i < _items.Count; i++)
            {
                _items[i].Button.onClick.RemoveAllListeners();
            }

            _rescue.onClick.RemoveAllListeners();
            _retreat.onClick.RemoveAllListeners();
        }
    }
}
