using System.IO;
using TAoB.View;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TAoB.EditorTools
{
    /// <summary>
    /// 에디터 수작업을 대신하는 일회성 도구. 메뉴 또는 `-executeMethod` 로 실행한다.
    /// 씬은 진입점 객체 하나만 담는다(계획서 Phase 4).
    /// <para>
    /// 「전투 씬 만들기」를 다시 실행하면 <see cref="ScenePath"/> 를 새로 만들어 덮어쓰고 빌드 씬 목록도 이 씬 하나로 바꾼다.
    /// 씬을 에디터에서 직접 고치지 말고, 바뀔 내용은 이 도구를 고쳐 다시 만든다. 씬이 둘 이상 필요해지면 빌드 목록 처리를 먼저 손봐야 한다.
    /// </para>
    /// </summary>
    public static class ProjectSetupTools
    {
        public const string ScenePath = "Assets/_Project/Scenes/Battle.unity";
        public const string KoreanFontPath = "Assets/_Project/Fonts/NanumGothic-Regular.ttf";

        [MenuItem("TAoB/TMP 필수 리소스 가져오기")]
        public static void ImportTmpEssentials()
        {
            // 패키지 가져오기는 비동기라 batchmode 에서는 `-quit` 없이 실행하고 완료 콜백에서 에디터를 닫는다.
            if (Application.isBatchMode)
            {
                AssetDatabase.importPackageCompleted += OnImportCompleted;
                AssetDatabase.importPackageFailed += OnImportFailed;
            }

            TMP_PackageResourceImporter.ImportResources(true, false, false);
        }

        private static void OnImportCompleted(string packageName)
        {
            Debug.Log($"패키지 가져오기 완료: {packageName}");
            AssetDatabase.SaveAssets();
            EditorApplication.Exit(0);
        }

        private static void OnImportFailed(string packageName, string error)
        {
            Debug.LogError($"패키지 가져오기 실패: {packageName} — {error}");
            EditorApplication.Exit(1);
        }

        [MenuItem("TAoB/전투 씬 만들기")]
        public static void CreateBattleScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 카메라를 같은 객체에 붙여 씬 객체를 하나로 유지한다. 오버레이 캔버스라 카메라는 배경색만 담당한다.
            var entry = new GameObject("BattleBootstrap");
            var camera = entry.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.12f, 0.13f, 0.16f);
            camera.orthographic = true;

            var bootstrap = entry.AddComponent<BattleBootstrap>();
            var font = AssetDatabase.LoadAssetAtPath<Font>(KoreanFontPath);
            if (font == null) Debug.LogWarning($"한글 폰트가 없습니다: {KoreanFontPath}");

            var serialized = new SerializedObject(bootstrap);
            serialized.FindProperty("_koreanFont").objectReferenceValue = font;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            Debug.Log($"전투 씬 생성: {ScenePath}");
        }
    }
}
