using System;
using UnityEngine;

namespace TAoB.Data
{
    /// <summary>
    /// 파일 검증 도우미. 실패는 전부 <see cref="GameDataException"/> 으로 바꿔 파일명·필드명을 붙인다.
    /// </summary>
    internal static class FieldCheck
    {
        /// <summary>정수 필드의 "파일에 없음" 표시. 데이터의 정수 값은 전부 0 이상이 유효하므로 -1 과 겹치지 않는다.</summary>
        public const int Missing = -1;

        public static T FromJson<T>(string file, string json)
        {
            if (string.IsNullOrWhiteSpace(json)) throw new GameDataException(file, GameDataException.WholeFile, "내용이 비어 있습니다.");

            try
            {
                return JsonUtility.FromJson<T>(json);
            }
            catch (ArgumentException ex)
            {
                throw new GameDataException(file, GameDataException.WholeFile, "JSON 형식이 잘못됐습니다: " + ex.Message);
            }
        }

        /// <summary>누락만 검사한다. 범위는 규칙 모듈 생성자가 정본일 때 쓴다.</summary>
        public static int Required(string file, string field, int value)
        {
            if (value == Missing) throw new GameDataException(file, field, "필드가 없습니다(또는 -1).");
            return value;
        }

        public static int Int(string file, string field, int value, int min, int max)
        {
            Required(file, field, value);
            if (value < min || value > max) throw new GameDataException(file, field, $"값 {value} 은(는) {min}~{max} 범위여야 합니다.");
            return value;
        }

        public static string Text(string file, string field, string value)
        {
            if (string.IsNullOrWhiteSpace(value)) throw new GameDataException(file, field, "필드가 없거나 비어 있습니다.");
            return value;
        }

        /// <summary>열거형 이름을 대소문자까지 그대로 요구한다. 숫자 문자열은 받지 않는다.</summary>
        public static T EnumName<T>(string file, string field, string value) where T : struct, Enum
        {
            if (value == null || !Enum.IsDefined(typeof(T), value))
            {
                string allowed = string.Join(", ", Enum.GetNames(typeof(T)));
                throw new GameDataException(file, field, $"'{value}' 은(는) 허용되지 않습니다. 허용: {allowed}");
            }

            return (T)Enum.Parse(typeof(T), value);
        }

        /// <summary>객체 배열. 빠졌거나 개수가 범위 밖이거나 null 항목이 있으면 실패.</summary>
        public static T[] Items<T>(string file, string field, T[] value, int minCount, int maxCount) where T : class
        {
            CheckCount(file, field, value, minCount, maxCount);
            for (int i = 0; i < value.Length; i++)
            {
                if (value[i] == null) throw new GameDataException(file, $"{field}[{i}]", "항목이 비어 있습니다(null).");
            }

            return value;
        }

        /// <summary>정수 배열. 항목은 null 일 수 없으므로 개수만 본다.</summary>
        public static int[] Ints(string file, string field, int[] value, int minCount, int maxCount)
        {
            CheckCount(file, field, value, minCount, maxCount);
            return value;
        }

        private static void CheckCount(string file, string field, Array value, int minCount, int maxCount)
        {
            if (value == null) throw new GameDataException(file, field, "필드가 없습니다.");
            if (value.Length < minCount || value.Length > maxCount)
            {
                throw new GameDataException(file, field, $"항목이 {value.Length}개입니다. {minCount}~{maxCount}개여야 합니다.");
            }
        }

        /// <summary>
        /// 규칙 모듈 생성자의 검증 실패에 파일명·필드명을 붙인다.
        /// 값의 상한은 생성자가 정본이고, 로더는 같은 상수로 먼저 거른 뒤 여기서 나머지(확률 합·중복 등)를 받는다.
        /// </summary>
        public static T Build<T>(string file, string field, Func<T> build)
        {
            try
            {
                return build();
            }
            catch (ArgumentException ex)
            {
                throw new GameDataException(file, field, ex.Message);
            }
        }
    }
}
