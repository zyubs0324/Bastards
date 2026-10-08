using System;
using System.Text;
using TAoB.Combat;
using TAoB.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TAoB.View
{
    /// <summary>결과 화면: 승패 사유, 빈사자, 마도구 내구력 소모, 상태 해시, 재시작(수직 슬라이스 계획서 Phase 4).</summary>
    internal sealed class ResultPanel : MonoBehaviour
    {
        private TMP_Text _title;
        private TMP_Text _body;
        private Button _restart;

        public static ResultPanel Create(Transform parent, TMP_FontAsset font, Action restart)
        {
            RectTransform rect = UiFactory.CreateStretched(parent, "ResultPanel");
            var panel = rect.gameObject.AddComponent<ResultPanel>();
            panel.Build(font, restart);
            panel.gameObject.SetActive(false);
            return panel;
        }

        private void Build(TMP_FontAsset font, Action restart)
        {
            Image dim = UiFactory.CreateImage(transform, "Dim", Vector2.zero, new Vector2(UiFit.BleedSize, UiFit.BleedSize), BattleLayout.DimColor);
            dim.raycastTarget = true; // 뒤의 개입 바를 누르지 못하게 막는다.

            UiFactory.CreateImage(transform, "Panel", Vector2.zero, new Vector2(900f, 540f), BattleLayout.PanelColor);
            _title = UiFactory.CreateText(transform, "Title", font, "", 52f, Color.white, new Vector2(0f, 200f), new Vector2(860f, 70f));
            _body = UiFactory.CreateText(transform, "Body", font, "", 28f, Color.white, new Vector2(0f, 20f), new Vector2(820f, 260f), TextAlignmentOptions.TopLeft);
            _body.textWrappingMode = TextWrappingModes.Normal;
            _restart = UiFactory.CreateButton(transform, "Restart", font, "재시작", new Vector2(0f, -200f), new Vector2(260f, 70f), out _, out _);
            _restart.onClick.AddListener(() => restart());
        }

        /// <summary>전투가 끝날 때 한 번만 호출되므로 여기서는 문자열을 만들어도 된다.</summary>
        public void Show(BattleResult result, GameData data)
        {
            _title.text = UiText.Outcome(result.Outcome);

            var body = new StringBuilder();
            body.Append("경과 ").Append((float)result.Tick / BattleSimulation.TicksPerSecond).Append("초 (").Append(result.Tick).Append("틱)\n");

            body.Append("빈사: ");
            if (result.DownedAllyIds.Count == 0) body.Append("없음");
            for (int i = 0; i < result.DownedAllyIds.Count; i++)
            {
                if (i > 0) body.Append(", ");
                body.Append(NameOf(data, result.DownedAllyIds[i]));
            }

            body.Append("\n마도구 소모: ");
            int wornCount = 0;
            for (int i = 0; i < result.MagitoolWear.Count; i++)
            {
                MagitoolWear wear = result.MagitoolWear[i];
                if (wear.DurabilityUsed == 0) continue;
                if (wornCount++ > 0) body.Append(", ");
                body.Append(NameOf(data, wear.UnitId)).Append(" -").Append(wear.DurabilityUsed);
            }

            if (wornCount == 0) body.Append("없음");
            body.Append("\n상태 해시: ").Append(result.StateHash.ToString("X16"));

            _body.text = body.ToString();
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
        }

        private static string NameOf(GameData data, int id)
        {
            CharacterData character = data.FindCharacter(id);
            return character != null ? character.Name : id.ToString();
        }

        private void OnDestroy()
        {
            _restart.onClick.RemoveAllListeners();
        }
    }
}
