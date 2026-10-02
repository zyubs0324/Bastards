using System;

namespace TAoB.Data
{
    /// <summary>
    /// 데이터 파일 검증 실패. 어느 파일의 어느 필드가 왜 틀렸는지를 담아, 사용자가 코드를 보지 않고 파일을 고칠 수 있게 한다(수직 슬라이스 계획서 Phase 3).
    /// </summary>
    public sealed class GameDataException : Exception
    {
        /// <summary>특정 필드가 아니라 파일 전체가 문제일 때의 <see cref="Field"/> 값.</summary>
        public const string WholeFile = "(파일)";

        public GameDataException(string fileName, string field, string reason)
            : base($"{fileName} — {field}: {reason}")
        {
            FileName = fileName;
            Field = field;
            Reason = reason;
        }

        public string FileName { get; }

        /// <summary>파일 안 위치. 예: <c>characters[2].stats.hp</c></summary>
        public string Field { get; }
        public string Reason { get; }
    }
}
