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
    /// 씬의 유일한 진입점 객체(계획서 Phase 4). 데이터를 읽고, 화면 요소를 코드로 만들고, 틱 드라이버로 규칙 모듈을 돌린다.
    /// 전투 표시 객체는 전부 BattleRoot 아래에 두고 재시작 때 통째로 파괴한다. PopupPool 만 살아남아 회수된다.
    /// </summary>
    public sealed class BattleBootstrap : MonoBehaviour
    {
        /// <summary>재현용 고정 시드(계획서 Phase 4 검증 "시드 고정으로 재현").</summary>
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
        }

        private void Update()
        {
            if (_isEnded) return;

            int ticks = _driver.Advance(Time.deltaTime, _speed);
            for (int i = 0; i < ticks; i++)
            {
                RunTick();
                if (_isEnded) break;
            }

            // 전투가 끝난 프레임은 보간을 끝까지 밀어 최종 상태를 그린다. 이후 프레임은 위에서 막혀 다시 그리지 않는다.
            float alpha = _isEnded ? 1f : _driver.Alpha;
            for (int i = 0; i < _current.Units.Count; i++)
            {
                _viewById[_current.Units[i].Id].Apply(_previous.Units[i], _current.Units[i], alpha);
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
            PlaceGroup(root, _data.Stage.AllyIds);
            for (int i = 0; i < _data.Stage.Waves.Count; i++) PlaceGroup(root, _data.Stage.Waves[i]);

            var allies = new List<CharacterData>(_data.Stage.AllyIds.Count);
            for (int i = 0; i < _data.Stage.AllyIds.Count; i++) allies.Add(_data.ResolveCharacter(_data.Stage.AllyIds[i], _replacements));

            _hud = BattleHud.Create(root, _font, _data.Stage.TimeLimitTicks, Seed, _setSpeed);
            _hud.SetSpeed(_speed);
            _bar = InterventionBar.Create(root, _font, allies, _submit);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            DebugLoadoutPanel.Create(root, _font, _data, allies, _replacements, _setReplacement, _restart);
#endif
            _result = ResultPanel.Create(root, _font, _restart);

            // 팝업은 항상 맨 위에 그린다.
            _popups.transform.SetAsLastSibling();
            _hud.ShowBanner(UiText.WaveBanner(0, _data.Stage.Waves.Count));
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
                switch (e.Kind)
                {
                    case BattleEventKind.Damage:
                        bool isCritical = e.Detail == 1;
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

        private void Restart()
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
