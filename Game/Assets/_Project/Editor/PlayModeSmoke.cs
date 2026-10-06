using TAoB.View;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TAoB.EditorTools
{
    /// <summary>
    /// 전투 씬을 플레이 모드로 몇 초 돌려 실행 오류가 없는지 본다. 수동 시나리오를 대신하지 않고, 켜자마자 터지는 실수만 거른다.
    /// 재시작 누수 검사(수직 슬라이스 계획서 Phase 4 검증 시나리오 8)도 여기서 자동으로 한다 — 사람이 오브젝트 수를 세기 어렵기 때문.
    /// batchmode: `-executeMethod TAoB.EditorTools.PlayModeSmoke.Run` (`-quit` 없이, `-nographics` 없이).
    /// 도메인 재로드가 있으면(에디터 GUI) 정적 상태가 지워지므로 SessionState 와 <see cref="Hook"/> 으로 다시 붙는다. batchmode 는 재로드 없이 들어간다.
    /// </summary>
    public static class PlayModeSmoke
    {
        private const string ActiveKey = "TAoB.Smoke.Active";
        private const string ErrorCountKey = "TAoB.Smoke.Errors";
        private const float PlaySeconds = 6f;

        /// <summary>재시작 누수 검사 횟수(수직 슬라이스 계획서 Phase 4 시나리오 8 "재시작 5회").</summary>
        private const int RestartCount = 5;

        /// <summary><c>Destroy</c> 는 프레임 끝에 처리되므로, 재시작 뒤 이만큼 기다렸다가 센다.</summary>
        private const int FramesAfterRestart = 2;

        private static bool _isRegistered;
        private static float _startTime = -1f;
        private static bool _hasCheckedHierarchy;
        private static int _baselineCount = -1;
        private static int _restartsDone;
        private static int _waitFrames;
        private static bool _isRestartCheckDone;

        [MenuItem("TAoB/플레이 스모크 (6초)")]
        public static void Run()
        {
            SessionState.SetBool(ActiveKey, true);
            SessionState.SetInt(ErrorCountKey, 0);
            EditorSceneManager.OpenScene(ProjectSetupTools.ScenePath);
            Register();
            EditorApplication.isPlaying = true;
        }

        [InitializeOnLoadMethod]
        private static void Hook()
        {
            if (SessionState.GetBool(ActiveKey, false) && EditorApplication.isPlayingOrWillChangePlaymode) Register();
        }

        private static void Register()
        {
            if (_isRegistered) return;
            _isRegistered = true;
            _startTime = -1f;
            _hasCheckedHierarchy = false;
            _baselineCount = -1;
            _restartsDone = 0;
            _waitFrames = 0;
            _isRestartCheckDone = false;
            Application.logMessageReceived += OnLog;
            EditorApplication.update += Tick;
        }

        private static void OnLog(string message, string stackTrace, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
            {
                SessionState.SetInt(ErrorCountKey, SessionState.GetInt(ErrorCountKey, 0) + 1);
            }
        }

        private static void Tick()
        {
            if (!EditorApplication.isPlaying) return;
            if (_startTime < 0f)
            {
                _startTime = Time.realtimeSinceStartup;
                Debug.Log("[스모크] 플레이 시작");
            }

            float elapsed = Time.realtimeSinceStartup - _startTime;

            // 1초쯤 돌린 뒤에야 화면 요소가 다 만들어져 있다.
            if (!_hasCheckedHierarchy && elapsed > 1f)
            {
                _hasCheckedHierarchy = true;
                CheckHierarchy();
                _baselineCount = CountObjects();
                Debug.Log($"[스모크] 기준 오브젝트 {_baselineCount}개(팝업 풀 제외)");
            }

            if (_hasCheckedHierarchy && !_isRestartCheckDone) StepRestartCheck();

            if (elapsed < PlaySeconds) return;

            // 시간 안에 재시작 검사를 다 못 돌았으면 "검사 안 함"이지 "통과"가 아니다 — 오류로 남겨 실패시킨다.
            if (!_isRestartCheckDone)
            {
                Debug.LogError($"[스모크] 재시작 검사 미완료 — {PlaySeconds}초 안에 {_restartsDone}/{RestartCount}회만 진행됐습니다.");
            }

            int errors = SessionState.GetInt(ErrorCountKey, 0);
            Debug.Log($"[스모크] {PlaySeconds}초 플레이 종료 — 오류 {errors}건");
            SessionState.SetBool(ActiveKey, false);
            EditorApplication.update -= Tick;
            Application.logMessageReceived -= OnLog;
            _isRegistered = false;
            EditorApplication.isPlaying = false;
            if (Application.isBatchMode) EditorApplication.Exit(errors == 0 ? 0 : 1);
        }

        /// <summary>재시작 → 몇 프레임 대기 → 개수 비교를 <see cref="RestartCount"/> 번 반복한다(한 번에 한 단계씩).</summary>
        private static void StepRestartCheck()
        {
            if (_waitFrames > 0)
            {
                _waitFrames--;
                return;
            }

            if (_restartsDone > 0)
            {
                int count = CountObjects();
                if (count != _baselineCount)
                {
                    Debug.LogError($"[스모크] 재시작 {_restartsDone}회 뒤 오브젝트 {count}개 — 기준 {_baselineCount}개와 다릅니다(누수 의심).");
                    _isRestartCheckDone = true; // 한 번 어긋나면 나머지 반복은 같은 오류만 반복하므로 멈춘다.
                    return;
                }
            }

            if (_restartsDone >= RestartCount)
            {
                _isRestartCheckDone = true;
                Debug.Log($"[스모크] 재시작 {RestartCount}회 뒤 오브젝트 수 유지 {_baselineCount}개");
                return;
            }

            var bootstrap = Object.FindAnyObjectByType<BattleBootstrap>();
            if (bootstrap == null)
            {
                Debug.LogError("[스모크] 재시작할 BattleBootstrap 이 없습니다.");
                _isRestartCheckDone = true;
                return;
            }

            bootstrap.Restart();
            _restartsDone++;
            _waitFrames = FramesAfterRestart;
        }

        /// <summary>씬 전체 <see cref="GameObject"/> 수. 팝업 풀은 전투가 진행되며 커지고 재시작 때 회수만 하므로 뺀다.</summary>
        private static int CountObjects()
        {
            GameObject[] all = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            int count = 0;
            for (int i = 0; i < all.Length; i++)
            {
                if (!IsUnderPopupPool(all[i].transform)) count++;
            }

            return count;
        }

        private static bool IsUnderPopupPool(Transform target)
        {
            for (Transform current = target; current != null; current = current.parent)
            {
                if (current.name == BattleLayout.PopupPoolName) return true;
            }

            return false;
        }

        private static void CheckHierarchy()
        {
            var bootstrap = Object.FindAnyObjectByType<BattleBootstrap>();
            if (bootstrap == null)
            {
                Debug.LogError("[스모크] BattleBootstrap 이 없습니다.");
                return;
            }

            // 유닛 표시는 전장 층(Field) 안의 스크롤 층(Lane) 아래에 있다(계획서 Phase B).
            Transform lane = bootstrap.transform.Find("Canvas/BattleRoot/Field/" + BattleLayout.LaneName);
            int units = 0;
            if (lane != null)
            {
                for (int i = 0; i < lane.childCount; i++)
                {
                    if (lane.GetChild(i).name.StartsWith("Unit ")) units++;
                }
            }

            if (units == 0) Debug.LogError("[스모크] 스크롤 층 아래에 유닛 표시가 없습니다.");
            else Debug.Log($"[스모크] 유닛 표시 {units}개 생성됨");
        }
    }
}
