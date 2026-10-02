using System;
using System.Collections.Generic;
using TAoB.Combat;

namespace TAoB.View
{
    /// <summary>
    /// BattleEvent → 연출 대응 전담(수직 슬라이스 계획서 Phase 4.5). <see cref="BattleBootstrap.Dispatch"/> 는 팝업을 띄운 뒤
    /// 이 클래스에 한 줄만 맡긴다. 규칙 모듈(TAoB.Combat)은 건드리지 않고 UnitView·ScreenShake·컷인 호출만 한다.
    /// </summary>
    internal sealed class BattleEffects
    {
        private const float CriticalHitStopSeconds = 0.08f;
        private const float DownedHitStopSeconds = 0.08f;

        private const float CriticalShakeDuration = 0.2f;
        private const float CriticalShakeMagnitude = 8f;
        private const float DownedShakeDuration = 0.15f;
        private const float DownedShakeMagnitude = 5f;
        private const float OverloadShakeDuration = 0.2f;
        private const float OverloadShakeMagnitude = 7f;

        private readonly IReadOnlyDictionary<int, UnitView> _viewById;
        private readonly ScreenShake _screenShake;
        private readonly Action<string> _showCutin;
        private readonly IReadOnlyDictionary<int, string> _cutinTextById;

        // 같은 틱에 한 유닛이 여러 대상을 때려도 돌진 연출은 한 번만(수직 슬라이스 계획서 Phase 4.5 이벤트 표 "같은 틱 첫 피해만").
        private readonly Dictionary<int, int> _lastLungeTick = new Dictionary<int, int>(8);

        private float _hitStopRemaining;

        /// <summary><paramref name="cutinTextById"/> 는 아군 ID만 담는다(전투 시작 때 한 번 생성) — 적 고유 기술에는 컷인이 없다.</summary>
        public BattleEffects(
            IReadOnlyDictionary<int, UnitView> viewById, ScreenShake screenShake, Action<string> showCutin,
            IReadOnlyDictionary<int, string> cutinTextById)
        {
            _viewById = viewById;
            _screenShake = screenShake;
            _showCutin = showCutin;
            _cutinTextById = cutinTextById;
        }

        public bool IsHitStopped => _hitStopRemaining > 0f;

        /// <summary>
        /// <see cref="BattleEvent.Detail"/> 의 크리티컬 인코딩(1 = 크리티컬, `BattleEvent.cs` 문서 주석)을 해석하는 유일한 지점.
        /// 팝업(BattleBootstrap.DispatchPopup)과 연출(HandleDamage) 양쪽이 같은 판정을 쓰게 해 드리프트를 막는다(리뷰 반영).
        /// </summary>
        public static bool IsCriticalDamage(BattleEvent e) => e.Detail == 1;

        /// <summary>매 프레임 짧은 멈춤 타이머만 줄인다. 유닛 자체 애니메이션은 UnitView.Apply 가 받는 dt 로 흐른다.</summary>
        public void Tick(float dt)
        {
            if (_hitStopRemaining > 0f) _hitStopRemaining -= dt;
        }

        public void Handle(BattleEvent e)
        {
            switch (e.Kind)
            {
                case BattleEventKind.Damage:
                    HandleDamage(e);
                    break;
                case BattleEventKind.SelfDamage:
                    if (TryGetView(e.TargetId, out UnitView selfDamaged)) selfDamaged.PlaySelfDamageFlash();
                    break;
                case BattleEventKind.Downed:
                    HandleDowned(e.TargetId);
                    break;
                case BattleEventKind.Rescued:
                    // 구조되면 정상으로 돌아오므로 빈사 자세(기울기·가라앉음)를 되돌린다(리뷰 반영 — 되돌리지 않으면 영구히 남는다).
                    if (TryGetView(e.TargetId, out UnitView rescued)) rescued.ResetDownedPose();
                    break;
                case BattleEventKind.OverloadResolved:
                    HandleOverloadResolved(e);
                    break;
                case BattleEventKind.ActionStarted:
                    HandleActionStarted(e);
                    break;
            }
        }

        private void HandleDamage(BattleEvent e)
        {
            if (!_lastLungeTick.TryGetValue(e.SourceId, out int lastTick) || lastTick != e.Tick)
            {
                _lastLungeTick[e.SourceId] = e.Tick;
                if (TryGetView(e.SourceId, out UnitView attacker)) attacker.PlayLunge();
            }

            bool isCritical = IsCriticalDamage(e);
            if (TryGetView(e.TargetId, out UnitView target)) target.PlayHit(isCritical);

            if (isCritical)
            {
                _screenShake.Trigger(CriticalShakeDuration, CriticalShakeMagnitude);
                _hitStopRemaining = CriticalHitStopSeconds;
            }
        }

        private void HandleDowned(int targetId)
        {
            if (TryGetView(targetId, out UnitView view)) view.PlayDowned();
            _screenShake.Trigger(DownedShakeDuration, DownedShakeMagnitude);
            _hitStopRemaining = DownedHitStopSeconds;
        }

        private void HandleOverloadResolved(BattleEvent e)
        {
            var outcome = (OverloadOutcome)e.Detail;
            if (outcome == OverloadOutcome.Runaway || outcome == OverloadOutcome.Breakthrough)
            {
                _screenShake.Trigger(OverloadShakeDuration, OverloadShakeMagnitude);
            }
        }

        private void HandleActionStarted(BattleEvent e)
        {
            if ((CommandKind)e.Detail != CommandKind.UseSkill) return;
            if (_cutinTextById.TryGetValue(e.SourceId, out string text)) _showCutin(text);
        }

        private bool TryGetView(int unitId, out UnitView view) => _viewById.TryGetValue(unitId, out view);
    }
}
