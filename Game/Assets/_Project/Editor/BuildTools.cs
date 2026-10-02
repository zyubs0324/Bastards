using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace TAoB.EditorTools
{
    /// <summary>
    /// 개발 빌드를 만드는 에디터 도구(수직 슬라이스 계획서 Phase 5). 사용자 PC 에서 에디터 GUI 가 느려 배치모드로만 쓴다.
    /// batchmode: `-executeMethod TAoB.EditorTools.BuildTools.BuildAndroid` (`-quit -nographics`), 실행은 `Tools/run-editor-tool.ps1 -Tool apk|win`.
    /// <para>
    /// 성공하면 로그에 `[빌드] 성공 …` 을 남긴다 — 실행 스크립트가 이 표시로 성공을 판정한다(종료 코드는 비어 돌아오는 경우가 있다).
    /// 산출물은 `Game/Builds/` 아래에 두어 `.gitignore` 의 `/Game/[Bb]uilds/` 로 제외된다.
    /// </para>
    /// </summary>
    public static class BuildTools
    {
        private const string AndroidOutput = "Builds/Android/TAoB-slice.apk";
        private const string WindowsOutput = "Builds/Windows/TAoB.exe";

        [MenuItem("TAoB/안드로이드 APK 빌드 (개발 빌드)")]
        public static void BuildAndroid()
        {
            // 스토어가 아니라 파일로 옮겨 설치하는 시험판이라 AAB 가 아닌 APK 로 만든다(사용자 환경 결정 2026-09-15).
            EditorUserBuildSettings.buildAppBundle = false;
            Build(BuildTarget.Android, BuildTargetGroup.Android, AndroidOutput);
        }

        [MenuItem("TAoB/윈도우 실행 파일 빌드 (개발 빌드)")]
        public static void BuildWindows()
        {
            Build(BuildTarget.StandaloneWindows64, BuildTargetGroup.Standalone, WindowsOutput);
        }

        private static void Build(BuildTarget target, BuildTargetGroup group, string relativeOutput)
        {
            try
            {
                BuildOrFail(target, group, relativeOutput);
            }
            catch (System.Exception e)
            {
                // 예외도 같은 실패 표시로 남겨, 스크립트가 로그만 보고 원인을 알 수 있게 한다.
                Fail($"예외 {e.GetType().Name}: {e.Message}");
            }
        }

        private static void BuildOrFail(BuildTarget target, BuildTargetGroup group, string relativeOutput)
        {
            string[] scenes = EnabledScenes();
            if (scenes.Length == 0)
            {
                Fail("빌드 씬 목록이 비어 있습니다 — 먼저 `run-editor-tool.ps1 -Tool scene` 으로 전투 씬을 만드세요.");
                return;
            }

            if (EditorUserBuildSettings.activeBuildTarget != target &&
                !EditorUserBuildSettings.SwitchActiveBuildTarget(group, target))
            {
                Fail($"빌드 타깃을 {target} 으로 바꾸지 못했습니다 — 해당 플랫폼 모듈이 설치되지 않았을 수 있습니다.");
                return;
            }

            // Application.dataPath 는 `<프로젝트>/Assets` 이므로 그 부모가 프로젝트 루트다.
            string output = Path.Combine(Path.GetDirectoryName(Application.dataPath), relativeOutput);
            Directory.CreateDirectory(Path.GetDirectoryName(output));

            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = output,
                target = target,
                targetGroup = group,

                // 개발 빌드 = 디버그 장비 패널·자동 캡처(DEVELOPMENT_BUILD)가 들어가고, 안드로이드는 Unity 기본 디버그 키로 서명된다.
                options = BuildOptions.Development,
            };

            BuildSummary summary = BuildPipeline.BuildPlayer(options).summary;
            if (summary.result != BuildResult.Succeeded)
            {
                Fail($"{summary.result} — 오류 {summary.totalErrors}건, {summary.totalTime.TotalMinutes:F1}분 경과");
                return;
            }

            // BuildSummary.totalSize 는 중간 산출물까지 합친 값이라(실측: 안드로이드 854MB 대 실제 APK 45MB) 파일 크기를 직접 잰다.
            // Windows 는 exe 옆 `_Data` 폴더가 본체라 보고서 크기를 쓴다.
            long bytes = target == BuildTarget.Android ? new FileInfo(output).Length : (long)summary.totalSize;
            Debug.Log($"[빌드] 성공 {output} — {bytes / (1024 * 1024)}MB, {summary.totalTime.TotalMinutes:F1}분");
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }

        private static string[] EnabledScenes()
        {
            var paths = new List<string>(EditorBuildSettings.scenes.Length);
            foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
            {
                if (scene.enabled) paths.Add(scene.path);
            }

            return paths.ToArray();
        }

        private static void Fail(string reason)
        {
            Debug.LogError($"[빌드] 실패 — {reason}");
            if (Application.isBatchMode) EditorApplication.Exit(1);
        }
    }
}
