using System;
using TAoB.Combat;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TAoB.View
{
    /// <summary>상단 정보: 웨이브, 남은 시간, 시드, 배속 버튼, 가운데 배너.</summary>
    internal sealed class BattleHud : MonoBehaviour
    {
        private const float BannerSeconds = 2f;
        private const float CutinSeconds = 0.6f;

        private TMP_Text _wave;
        private TMP_Text _time;
        private TMP_Text _banner;
        private TMP_Text _cutin;
        private Button _speed1;
        private Button _speed2;
        private Image _speed1Image;
        private Image _speed2Image;
        private int _timeLimitTicks;
        private int _lastWave = -1;
        private int _lastTick = -1;
        private float _bannerAge;
        private float _cutinAge;

        public static BattleHud Create(Transform parent, TMP_FontAsset font, int timeLimitTicks, uint seed, Action<int> setSpeed)
        {
            RectTransform rect = UiFactory.CreateStretched(parent, "Hud");
            var hud = rect.gameObject.AddComponent<BattleHud>();
            hud._timeLimitTicks = timeLimitTicks;
            hud.Build(font, seed, setSpeed);
            return hud;
        }

        private void Build(TMP_FontAsset font, uint seed, Action<int> setSpeed)
        {
            var left = TextAlignmentOptions.Left;
            _wave = UiFactory.CreateText(transform, "Wave", font, "", 30f, Color.white, new Vector2(-740f, 500f), new Vector2(400f, 40f), left);
            _time = UiFactory.CreateText(transform, "Time", font, "", 26f, Color.white, new Vector2(-740f, 462f), new Vector2(400f, 36f), left);
            TMP_Text seedText = UiFactory.CreateText(
                transform, "Seed", font, "", 20f, BattleLayout.FaintTextColor, new Vector2(-740f, 430f), new Vector2(400f, 30f), left);
            seedText.SetText("시드 {0}", seed);

            _speed1 = UiFactory.CreateButton(transform, "Speed1", font, "1x", new Vector2(760f, 500f), new Vector2(90f, 56f), out _, out _speed1Image);
            _speed2 = UiFactory.CreateButton(transform, "Speed2", font, "2x", new Vector2(865f, 500f), new Vector2(90f, 56f), out _, out _speed2Image);
            _speed1.onClick.AddListener(() => setSpeed(1));
            _speed2.onClick.AddListener(() => setSpeed(2));

            _banner = UiFactory.CreateText(transform, "Banner", font, "", 48f, Color.white, new Vector2(0f, 380f), new Vector2(1000f, 70f));
            _banner.gameObject.SetActive(false);

            // 웨이브·결과 배너와 겹치지 않는 별도 슬롯(수직 슬라이스 계획서 Phase 4.5 — 연속 발동 시 서로 덮어쓰지 않게).
            _cutin = UiFactory.CreateText(transform, "Cutin", font, "", 40f, BattleLayout.OverloadColor, new Vector2(0f, 300f), new Vector2(1000f, 60f));
            _cutin.gameObject.SetActive(false);
        }

        public void Apply(BattleSnapshot snapshot)
        {
            if (snapshot.CurrentWave != _lastWave)
            {
                _lastWave = snapshot.CurrentWave;
                _wave.SetText("웨이브 {0} / {1}", snapshot.CurrentWave + 1, snapshot.WaveCount);
            }

            if (snapshot.Tick != _lastTick)
            {
                _lastTick = snapshot.Tick;
                float remaining = (float)(_timeLimitTicks - snapshot.Tick) / BattleSimulation.TicksPerSecond;

                // TMP 의 SetText 는 .NET 서식이 아니라 자체 파서를 쓴다. `{0:1}` 이 소수 1자리이며 `F1` 은 글자 그대로 찍힌다.
                _time.SetText("남은 시간 {0:1}초", remaining);
            }
        }

        public void SetSpeed(int speed)
        {
            _speed1Image.color = speed == 1 ? BattleLayout.ButtonActiveColor : BattleLayout.ButtonColor;
            _speed2Image.color = speed == 2 ? BattleLayout.ButtonActiveColor : BattleLayout.ButtonColor;
        }

        /// <summary>문구는 상수 또는 드물게 만든 문자열만 넘긴다.</summary>
        public void ShowBanner(string text)
        {
            _banner.text = text;
            _banner.gameObject.SetActive(true);
            _bannerAge = 0f;
        }

        /// <summary>기술 컷인 전용 슬롯. 문구는 전투 시작 때 캐릭터별로 한 번 만든 것만 넘긴다(수직 슬라이스 계획서 Phase 4.5).</summary>
        public void ShowCutin(string text)
        {
            _cutin.text = text;
            _cutin.gameObject.SetActive(true);
            _cutinAge = 0f;
        }

        private void Update()
        {
            if (_banner.gameObject.activeSelf)
            {
                _bannerAge += Time.deltaTime;
                if (_bannerAge >= BannerSeconds) _banner.gameObject.SetActive(false);
            }

            if (_cutin.gameObject.activeSelf)
            {
                _cutinAge += Time.deltaTime;
                if (_cutinAge >= CutinSeconds) _cutin.gameObject.SetActive(false);
            }
        }

        private void OnDestroy()
        {
            _speed1.onClick.RemoveAllListeners();
            _speed2.onClick.RemoveAllListeners();
        }
    }
}
