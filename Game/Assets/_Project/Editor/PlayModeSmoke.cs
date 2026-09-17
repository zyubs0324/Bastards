using TAoB.View;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TAoB.EditorTools
{
    /// <summary>
    /// 전투 씬을 플레이 모드로 몇 초 돌려 실행 오류가 없는지 본다. 수동 시나리오를 대신하지 않고, 켜자마자 터지는 실수만 거른다.
    /// batchmode: `-executeMethod TAoB.EditorTools.PlayModeSmoke.Run` (`-quit` 없이, `-nographics` 없이).
    /// 도메인 재로드가 있으면(에디터 GUI) 정적 상태가 지워지므로 SessionState 와 <see cref="Hook"/> 으로 다시 붙는다. batchmode 는 재로드 없이 들어간다.
    /// </summary>
    public static class PlayModeSmoke
    {
        private const string ActiveKey = "TAoB.Smoke.Active";
        private const string ErrorCountKey = "TAoB.Smoke.Errors";
        private const float PlaySeconds = 6f;

        private static bool _isRegistered;
        private static float _startTime = -1f;
        private static bool _hasCheckedHierarchy;

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
            if (!_hasCheckedHierarchy && elapsed > 1f)
            {
                _hasCheckedHierarchy = true;
                CheckHierarchy();
            }

            if (elapsed < PlaySeconds) return;

            int errors = SessionState.GetInt(ErrorCountKey, 0);
            Debug.Log($"[스모크] {PlaySeconds}초 플레이 종료 — 오류 {errors}건");
            SessionState.SetBool(ActiveKey, false);
            EditorApplication.update -= Tick;
            Application.logMessageReceived -= OnLog;
            _isRegistered = false;
            EditorApplication.isPlaying = false;
            if (Application.isBatchMode) EditorApplication.Exit(errors == 0 ? 0 : 1);
        }

        private static void CheckHierarchy()
        {
            var bootstrap = Object.FindAnyObjectByType<BattleBootstrap>();
            if (bootstrap == null)
            {
                Debug.LogError("[스모크] BattleBootstrap 이 없습니다.");
                return;
            }

            // 유닛 표시는 BattleRoot 바로 아래가 아니라 전장 층(Field) 아래에 있다(계획서 Phase 4.5).
            Transform field = bootstrap.transform.Find("Canvas/BattleRoot/Field");
            int units = 0;
            if (field != null)
            {
                for (int i = 0; i < field.childCount; i++)
                {
                    if (field.GetChild(i).name.StartsWith("Unit ")) units++;
                }
            }

            if (units == 0) Debug.LogError("[스모크] Field 아래에 유닛 표시가 없습니다.");
            else Debug.Log($"[스모크] 유닛 표시 {units}개 생성됨");
        }
    }
}
