#if UNITY_EDITOR || DEVELOPMENT_BUILD
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
    /// 개발용 장비 교체 패널(수직 슬라이스 계획서 Phase 4): 아군별 원석·마도구를 바꾸고 재시작해 2축 규칙을 체감한다. 덱세팅 화면이 아니다.
    /// RES 부족으로 장착할 수 없는 마도구는 목록에서 뺀다.
    /// </summary>
    internal sealed class DebugLoadoutPanel : MonoBehaviour
    {
        private sealed class Slot
        {
            public CharacterData Base;
            public List<CatalystData> Catalysts;
            public List<MagitoolData> Magitools;
            public int CatalystIndex;
            public int MagitoolIndex;
            public TMP_Text CatalystLabel;
            public TMP_Text MagitoolLabel;
        }

        private readonly List<Slot> _slots = new List<Slot>(5);
        private readonly List<Button> _buttons = new List<Button>(24);
        private Action<int, CharacterData> _setReplacement;
        private Action _restart;

        public static DebugLoadoutPanel Create(
            Transform parent, TMP_FontAsset font, GameData data, IReadOnlyList<CharacterData> allies,
            IReadOnlyDictionary<int, CharacterData> current, Action<int, CharacterData> setReplacement, Action restart)
        {
            RectTransform rect = UiFactory.CreateStretched(parent, "DebugLoadoutPanel");
            var panel = rect.gameObject.AddComponent<DebugLoadoutPanel>();
            panel._setReplacement = setReplacement;
            panel._restart = restart;
            panel.Build(font, data, allies, current);
            return panel;
        }

        private void Build(TMP_FontAsset font, GameData data, IReadOnlyList<CharacterData> allies, IReadOnlyDictionary<int, CharacterData> current)
        {
            const float panelX = 800f;
            const float panelWidth = 300f;
            float panelHeight = 80f + allies.Count * 110f;
            float top = 360f;

            UiFactory.CreateImage(transform, "Background", new Vector2(panelX, top - panelHeight * 0.5f), new Vector2(panelWidth, panelHeight), BattleLayout.PanelColor);
            UiFactory.CreateText(transform, "Title", font, "장비 교체 (개발용)", 22f, Color.white, new Vector2(panelX, top - 22f), new Vector2(panelWidth, 30f));

            for (int i = 0; i < allies.Count; i++)
            {
                CharacterData ally = allies[i];
                CharacterData shown = data.ResolveCharacter(ally.Id, current);
                float y = top - 60f - i * 110f;

                var slot = new Slot
                {
                    Base = ally,
                    Catalysts = new List<CatalystData> { null },
                    Magitools = new List<MagitoolData> { null },
                };
                slot.Catalysts.AddRange(data.Equipment.Catalysts);
                foreach (MagitoolData tool in data.Equipment.Magitools)
                {
                    if (EquipRules.CanEquip(ally.Stats.Res, tool.Magitool, data.Balance)) slot.Magitools.Add(tool);
                }

                slot.CatalystIndex = IndexOf(slot.Catalysts, shown.Catalyst);
                slot.MagitoolIndex = IndexOf(slot.Magitools, shown.Magitool);

                UiFactory.CreateText(transform, "Name", font, ally.Name, 20f, Color.white, new Vector2(panelX, y), new Vector2(panelWidth, 26f));
                slot.CatalystLabel = UiFactory.CreateText(transform, "Catalyst", font, "", 18f, Color.white, new Vector2(panelX, y - 30f), new Vector2(200f, 26f));
                slot.MagitoolLabel = UiFactory.CreateText(transform, "Magitool", font, "", 18f, Color.white, new Vector2(panelX, y - 62f), new Vector2(200f, 26f));

                Slot captured = slot;
                AddArrow(font, new Vector2(panelX - 125f, y - 30f), "◀", () => Cycle(captured, true, -1));
                AddArrow(font, new Vector2(panelX + 125f, y - 30f), "▶", () => Cycle(captured, true, 1));
                AddArrow(font, new Vector2(panelX - 125f, y - 62f), "◀", () => Cycle(captured, false, -1));
                AddArrow(font, new Vector2(panelX + 125f, y - 62f), "▶", () => Cycle(captured, false, 1));

                RefreshLabels(slot);
                _slots.Add(slot);
            }

            Button apply = UiFactory.CreateButton(
                transform, "Apply", font, "적용 후 재시작", new Vector2(panelX, top - panelHeight + 30f), new Vector2(240f, 44f), out _, out _);
            apply.onClick.AddListener(Apply);
            _buttons.Add(apply);
        }

        private void AddArrow(TMP_FontAsset font, Vector2 position, string label, UnityEngine.Events.UnityAction onClick)
        {
            Button button = UiFactory.CreateButton(transform, "Arrow", font, label, position, new Vector2(40f, 30f), out _, out _);
            button.onClick.AddListener(onClick);
            _buttons.Add(button);
        }

        private static int IndexOf<T>(List<T> list, T item) where T : class
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (ReferenceEquals(list[i], item)) return i;
            }

            return 0;
        }

        private static void Cycle(Slot slot, bool catalyst, int direction)
        {
            if (catalyst) slot.CatalystIndex = Wrap(slot.CatalystIndex + direction, slot.Catalysts.Count);
            else slot.MagitoolIndex = Wrap(slot.MagitoolIndex + direction, slot.Magitools.Count);
            RefreshLabels(slot);
        }

        private static int Wrap(int index, int count)
        {
            return (index % count + count) % count;
        }

        private static void RefreshLabels(Slot slot)
        {
            CatalystData catalyst = slot.Catalysts[slot.CatalystIndex];
            MagitoolData tool = slot.Magitools[slot.MagitoolIndex];
            slot.CatalystLabel.text = "원석: " + (catalyst != null ? catalyst.Name : "없음");
            slot.MagitoolLabel.text = "마도구: " + (tool != null ? tool.Name : "없음");
        }

        private void Apply()
        {
            for (int i = 0; i < _slots.Count; i++)
            {
                Slot slot = _slots[i];
                CharacterData changed = slot.Base.WithEquipment(slot.Catalysts[slot.CatalystIndex], slot.Magitools[slot.MagitoolIndex]);
                _setReplacement(changed.Id, changed);
            }

            _restart();
        }

        private void OnDestroy()
        {
            for (int i = 0; i < _buttons.Count; i++) _buttons[i].onClick.RemoveAllListeners();
        }
    }
}
#endif
