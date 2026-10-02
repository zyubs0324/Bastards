// 에디터에서도 컴파일되게 해(인자가 없으면 아무것도 하지 않음) 컴파일 오류를 긴 개발 빌드 전에 테스트·스모크가 잡게 한다.
#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.IO;
using UnityEngine;

namespace TAoB.View
{
    /// <summary>
    /// 개발용 자동 스크린샷(수직 슬라이스 계획서 Phase 5 — 릴리스 빌드에는 들어가지 않는다). 실행 인자 `-captureShots &lt;폴더&gt;` 가 있을 때만 동작하고,
    /// 없으면 아무것도 하지 않는다. 사용자 PC 에서 에디터 GUI 가 느려, 연출을 눈으로 확인하는 수단이 필요해 만들었다.
    /// </summary>
    internal sealed class DevCapture : MonoBehaviour
    {
        private const string Argument = "-captureShots";

        /// <summary>전투 초반(배치)·중반(교전)·후반을 한 장씩 남긴다.</summary>
        private static readonly float[] CaptureSeconds = { 2f, 5f, 9f };

        /// <summary><see cref="ScreenCapture.CaptureScreenshot"/> 은 프레임 끝에 비동기로 저장하므로, 종료 전에 쓰일 시간을 준다.</summary>
        private const float QuitDelaySeconds = 1f;

        private string _folder;
        private float _elapsed;
        private int _taken;

        /// <summary>인자가 있을 때만 <paramref name="host"/> 에 붙인다. 인자가 없으면 컴포넌트를 만들지 않는다.</summary>
        public static void AttachIfRequested(GameObject host)
        {
            string folder = FolderFromCommandLine();
            if (string.IsNullOrEmpty(folder)) return;

            Directory.CreateDirectory(folder);

            // 창이 뒤로 가도 멈추지 않게 한다 — 기본값(runInBackground 꺼짐)이면 포커스를 잃는 순간 캡처가 중간에 끊긴다.
            Application.runInBackground = true;
            host.AddComponent<DevCapture>()._folder = folder;
        }

        private static string FolderFromCommandLine()
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == Argument) return args[i + 1];
            }

            return null;
        }

        private void Update()
        {
            // 짧은 멈춤이 Time.timeScale 을 건드리지는 않지만, 캡처 시점은 연출과 무관해야 하므로 보정 없는 시간을 쓴다.
            _elapsed += Time.unscaledDeltaTime;

            if (_taken < CaptureSeconds.Length)
            {
                if (_elapsed < CaptureSeconds[_taken]) return;

                string path = Path.Combine(_folder, $"shot{_taken + 1}.png");
                ScreenCapture.CaptureScreenshot(path);
                Debug.Log($"[캡처] {path}");
                _taken++;
                return;
            }

            if (_elapsed >= CaptureSeconds[CaptureSeconds.Length - 1] + QuitDelaySeconds) Application.Quit();
        }
    }
}
#endif
