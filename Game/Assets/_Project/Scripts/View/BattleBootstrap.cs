using System;
using System.Collections.Generic;
using TAoB.Combat;
using TAoB.Data;
using TMPro;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace TAoB.View
{
    /// <summary>
    /// 씬의 유일한 진입점 객체(수직 슬라이스 계획서 Phase 4). 데이터를 읽고, 화면 요소를 코드로 만들고, 틱 드라이버로 규칙 모듈을 돌린다.
    /// 전투 표시 객체는 전부 BattleRoot 아래에 두고 재시작 때 통째로 파괴한다. PopupPool 만 살아남아 회수된다.
    /// </summary>
    public sealed class BattleBootstrap : MonoBehaviour
    {
        /// <summary>재현용 고정 시드(수직 슬라이스 계획서 Phase 4 검증 "시드 고정으로 재현").</summary>
        public const uint Seed = 1;

        private const int PopupPrewarm = 24;

        // 씬 생성 도구가 넣는다. 없으면 TMP 기본 폰트를 쓰며 한글이 네모로 보인다.
        [SerializeField] private Font _koreanFont;

        private readonly List<BattleEvent> _events = new List<BattleEvent>(64);
        private readonly Dictionary<int, UnitView> _viewById = new Dictionary<int, UnitView>(16);
        private readonly Dictionary<int, CharacterData> _replacements = new Dictionary<int, CharacterData>();
        private readonly TickDriver _driver = new TickDriver(BattleSimulation.TicksPerSecond);

        private GameData _data;
        private TMP_FontAsset _font;
        private Canvas _canvas;
        private PopupPool _popups;
        private Func<BattleCommand, CommandResult> _submit;
        private Action<int> _setSpeed;
        private Action _restart;
        private Action<int, CharacterData> _setReplacement;

        private BattleSimulation _sim;
        private BattleSnapshot _previous;
        private BattleSnapshot _current;
        private GameObject _root;
        private BattleHud _hud;
        private InterventionBar _bar;
        private ResultPanel _result;
        private ScreenShake _screenShake;
        private BattleEffects _effects;
        private int _speed = 1;
        private bool _isEnded;

        private void Awake()
        {
            _data = GameDataLoader.LoadFromResources();
            _font = CreateFontAsset();
            _submit = Submit;
            _setSpeed = SetSpeed;
            _restart = Restart;
            _setReplacement = SetReplacement;

            _canvas = UiFactory.CreateCanvas(transform);
            UiFactory.CreateEventSystem(transform);
            _popups = PopupPool.Create(_canvas.transform, _font, PopupPrewarm);
            StartBattle();

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            // 실행 인자 `-captureShots <폴더>` 가 있을 때만 스크린샷을 남긴다(수직 슬라이스 계획서 Phase 5). 디버그 패널과 같은 조건으로 구역화.
            DevCapture.AttachIfRequested(gameObject);
#endif
        }

        private void Update()
        {
            // 전투가 끝나도 연출(짧은 멈춤·화면 흔들림·박스 애니메이션)은 계속 흘려보내 끝까지 재생한다.
            // 틱 루프(RunTick)만 막는다 — 그렇지 않으면 마지막 틱의 화면 흔들림·빈사 연출이 시작하자마자 멈춰 버린다(리뷰 반영).
            float dt = Time.deltaTime;
            _effects.Tick(dt);
            _screenShake.Tick(dt);

            // 짧은 멈춤(수직 슬라이스 계획서 Phase 4.5): 남은 시간 동안은 틱 루프를 건너뛴다. 틱 루프만 멈추고 연출은 dt 로 계속 흐른다.
            if (!_isEnded && !_effects.IsHitStopped)
            {
                int ticks = _driver.Advance(dt, _speed);
                int executed = 0;
                for (int i = 0; i < ticks; i++)
                {
                    RunTick();
                    executed = i + 1;
                    if (_isEnded || _effects.IsHitStopped) break;
                }

                // 멈춤이 틱 루프 중간에 걸려 다 돌지 못했으면, 실행하지 못한 만큼을 되돌려 틱을 잃지 않는다.
                if (executed < ticks) _driver.RestoreTicks(ticks - executed);
            }

            // 전투가 끝나면 보간을 끝까지 밀어 최종 상태로 고정한다. 이후 프레임에도 계속 그려서 연출이 끝까지 재생되게 한다.
            float alpha = _isEnded ? 1f : _driver.Alpha;
            for (int i = 0; i < _current.Units.Count; i++)
            {
                _viewById[_current.Units[i].Id].Apply(_previous.Units[i], _current.Units[i], alpha, dt);
            }

            _hud.Apply(_current);
            _bar.Apply(_current);
        }

        private void RunTick()
        {
            // 직전 버퍼를 현재로 돌리고 새 상태를 채운다. 값 복사라 틱마다 할당이 없다.
            BattleSnapshot swap = _previous;
            _previous = _current;
            _current = swap;

            _sim.Step(_events);
            _sim.FillSnapshot(_current);
            Dispatch(_events);

            if (_sim.Outcome != BattleOutcome.InProgress)
            {
                _isEnded = true;
                _result.Show(_sim.BuildResult(), _data);
            }
        }

        private void StartBattle()
        {
            _sim = new BattleSimulation(_data.CreateBattleSetup(_replacements), new SeededRandom(Seed));
            _previous = _sim.CreateSnapshotBuffer();
            _current = _sim.CreateSnapshotBuffer();
            _sim.FillSnapshot(_previous);
            _sim.FillSnapshot(_current);
            _driver.Reset();
            _isEnded = false;

            RectTransform root = UiFactory.CreateStretched(_canvas.transform, "BattleRoot");
            _root = root.gameObject;
            _viewById.Clear();

            // 전장 층(수직 슬라이스 계획서 Phase 4.5): 배경·유닛만 이 아래에 둔다. 화면 흔들림은 이 층만 흔들어 HUD·버튼은 고정한다.
            RectTransform field = UiFactory.CreateStretched(root, "Field");
            CreateBackground(field);
            PlaceGroup(field, _data.Stage.AllyIds);
            for (int i = 0; i < _data.Stage.Waves.Count; i++) PlaceGroup(field, _data.Stage.Waves[i]);

            var allies = new List<CharacterData>(_data.Stage.AllyIds.Count);
            var cutinText = new Dictionary<int, string>(_data.Stage.AllyIds.Count);
            for (int i = 0; i < _data.Stage.AllyIds.Count; i++)
            {
                CharacterData ally = _data.ResolveCharacter(_data.Stage.AllyIds[i], _replacements);
                allies.Add(ally);

                // 컷인 문구는 전투 시작 때 한 번만 만든다(수직 슬라이스 계획서 Phase 4.5 이벤트 표 — 프레임마다 문자열을 만들지 않기 위함).
                cutinText[ally.Id] = ally.Name + " 고유 기술!";
            }

            _hud = BattleHud.Create(root, _font, _data.Stage.TimeLimitTicks, Seed, _setSpeed);
            _hud.SetSpeed(_speed);
            _bar = InterventionBar.Create(root, _font, allies, _submit);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            DebugLoadoutPanel.Create(root, _font, _data, allies, _replacements, _setReplacement, _restart);
#endif
            _result = ResultPanel.Create(root, _font, _restart);

            _screenShake = new ScreenShake(field);
            _effects = new BattleEffects(_viewById, _screenShake, text => _hud.ShowCutin(text), cutinText);

            // 팝업은 항상 맨 위에 그린다.
            _popups.transform.SetAsLastSibling();
            _hud.ShowBanner(UiText.WaveBanner(0, _data.Stage.Waves.Count));
        }

        /// <summary>스프라이트 없이 하늘·바닥 색 띠와 경계선을 그린다. 화면 흔들림 진폭보다 크게 만들어 가장자리가 드러나지 않게 한다.</summary>
        private static void CreateBackground(Transform field)
        {
            float top = BattleLayout.Height / 2f;
            float bottom = -BattleLayout.Height / 2f;
            float groundTop = BattleLayout.GroundTopY;
            float width = BattleLayout.Width + BattleLayout.BackgroundOverscan * 2f;

            float skyHeight = top - groundTop + BattleLayout.BackgroundOverscan;
            float skyCenterY = (top + groundTop) / 2f;
            UiFactory.CreateImage(field, "Sky", new Vector2(0f, skyCenterY), new Vector2(width, skyHeight), BattleLayout.SkyColor);

            float groundHeight = groundTop - bottom + BattleLayout.BackgroundOverscan;
            float groundCenterY = (groundTop + bottom) / 2f;
            UiFactory.CreateImage(field, "Ground", new Vector2(0f, groundCenterY), new Vector2(width, groundHeight), BattleLayout.GroundColor);

            UiFactory.CreateImage(field, "GroundLine", new Vector2(0f, groundTop), new Vector2(width, 4f), BattleLayout.GroundLineColor);
        }

        /// <summary>같은 진영·같은 열끼리 위에서 아래로 세운다. 자리 수를 먼저 세어야 가운데 정렬이 된다.</summary>
        private void PlaceGroup(Transform root, IReadOnlyList<int> ids)
        {
            int frontCount = 0;
            int backCount = 0;
            for (int i = 0; i < ids.Count; i++)
            {
                if (_data.ResolveCharacter(ids[i], _replacements).Row == Row.Front) frontCount++;
                else backCount++;
            }

            int frontSlot = 0;
            int backSlot = 0;
            for (int i = 0; i < ids.Count; i++)
            {
                CharacterData character = _data.ResolveCharacter(ids[i], _replacements);
                bool isFront = character.Row == Row.Front;
                int slot = isFront ? frontSlot++ : backSlot++;
                Vector2 position = BattleLayout.UnitPosition(character.Side, character.Row, slot, isFront ? frontCount : backCount);
                _viewById[character.Id] = UnitView.Create(root, _font, character, position);
            }
        }

        private void Dispatch(List<BattleEvent> events)
        {
            for (int i = 0; i < events.Count; i++)
            {
                BattleEvent e = events[i];
                DispatchPopup(e);
                _effects.Handle(e);
            }
        }

        /// <summary>이벤트별 팝업만 담당한다. 연출(돌진·흔들림 등)은 <see cref="BattleEffects"/> 가 맡는다.</summary>
        private void DispatchPopup(BattleEvent e)
        {
            switch (e.Kind)
            {
                case BattleEventKind.Damage:
                    bool isCritical = BattleEffects.IsCriticalDamage(e);
                    _popups.ShowNumber(
                        PositionOf(e.TargetId), e.Value,
                        isCritical ? BattleLayout.CriticalColor : BattleLayout.DamageColor,
                        isCritical ? 1.4f : 1f);
                    break;
                case BattleEventKind.SelfDamage:
                    _popups.ShowNumber(PositionOf(e.TargetId), e.Value, BattleLayout.SelfDamageColor, 0.9f);
                    break;
                case BattleEventKind.ScatterDamage:
                    // 흩어짐 피해는 가해자가 없어 SourceId 가 비어 있다. 맞은 유닛은 TargetId 에 담긴다.
                    _popups.ShowNumber(PositionOf(e.TargetId), e.Value, BattleLayout.ScatterDamageColor, 0.7f);
                    break;
                case BattleEventKind.Downed:
                    _popups.ShowLabel(PositionOf(e.TargetId), "빈사", BattleLayout.DownedTextColor, 1.2f);
                    break;
                case BattleEventKind.OverloadResolved:
                    _popups.ShowLabel(PositionOf(e.SourceId), UiText.OverloadOutcome((OverloadOutcome)e.Detail), BattleLayout.OverloadColor, 1.3f);
                    break;
                case BattleEventKind.Breakthrough:
                    _popups.ShowNumber(PositionOf(e.SourceId), e.Value, BattleLayout.BreakthroughColor, 1f);
                    break;
                case BattleEventKind.Rescued:
                    _popups.ShowLabel(PositionOf(e.TargetId), "구조!", BattleLayout.RescuedColor, 1.1f);
                    break;
                case BattleEventKind.CommandFallback:
                    _popups.ShowLabel(PositionOf(e.SourceId), "명령 취소", BattleLayout.FallbackColor, 0.8f);
                    break;
                case BattleEventKind.MagitoolDepleted:
                    _popups.ShowLabel(PositionOf(e.SourceId), "내구력 소진", BattleLayout.MagitoolColor, 0.9f);
                    break;
                case BattleEventKind.WaveStarted:
                    _hud.ShowBanner(UiText.WaveBanner(e.Value, _sim.WaveCount));
                    break;
                case BattleEventKind.BattleEnded:
                    _hud.ShowBanner(UiText.Outcome((BattleOutcome)e.Detail));
                    break;
            }
        }

        private Vector2 PositionOf(int unitId)
        {
            return _viewById.TryGetValue(unitId, out UnitView view) ? view.Position : Vector2.zero;
        }

        private CommandResult Submit(BattleCommand command)
        {
            return _sim.Enqueue(command);
        }

        private void SetSpeed(int speed)
        {
            _speed = speed;
            _hud.SetSpeed(speed);
        }

        private void SetReplacement(int id, CharacterData character)
        {
            _replacements[id] = character;
        }

        /// <summary>
        /// 전투를 처음부터 다시 시작한다. 결과 화면·디버그 패널 버튼이 쓰고, 플레이 스모크의 재시작 누수 검사도 부른다(수직 슬라이스 계획서 Phase 5).
        /// <see cref="Destroy"/> 는 프레임 끝에 처리되므로, 호출한 쪽이 개수를 셀 때는 몇 프레임 기다려야 한다.
        /// </summary>
        public void Restart()
        {
            Destroy(_root);
            _popups.RecycleAll();
            StartBattle();
        }

        private TMP_FontAsset CreateFontAsset()
        {
            if (_koreanFont == null)
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogWarning("한글 폰트가 지정되지 않아 TMP 기본 폰트를 씁니다. 씬 생성 도구(TAoB/전투 씬 만들기)를 다시 실행하세요.");
#endif
                return TMP_Settings.defaultFontAsset;
            }

            // 필요한 글자만 실행 중에 아틀라스에 넣는다(한글 전체를 미리 굽지 않음).
            return TMP_FontAsset.CreateFontAsset(_koreanFont, 48, 6, GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic, true);
        }
    }
}
