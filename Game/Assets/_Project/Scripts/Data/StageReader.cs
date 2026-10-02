using System.Collections.Generic;
using TAoB.Combat;

namespace TAoB.Data
{
    /// <summary>stage_slice.json → <see cref="StageData"/>. 캐릭터 ID 는 이미 읽은 목록에서 찾는다.</summary>
    internal static class StageReader
    {
        // 편성 규모 상한은 밸런스 값이 아니라 로더 규칙이다(수직 슬라이스 계획서 Phase 3).
        public const int MaxAllies = 5;
        public const int MaxWaves = 5;
        public const int MaxEnemiesPerWave = 5;
        public const int MaxTimeLimitSeconds = BattleSetup.MaxTimeLimitTicks / BattleSimulation.TicksPerSecond;

        public static StageData Read(string file, StageDto dto, IReadOnlyList<CharacterData> characters)
        {
            int seconds = FieldCheck.Int(file, "timeLimitSeconds", dto.timeLimitSeconds, 1, MaxTimeLimitSeconds);
            PreconditionKind precondition = FieldCheck.EnumName<PreconditionKind>(file, "precondition", dto.precondition);

            var placed = new HashSet<int>();
            IReadOnlyList<int> allyIds = ReadIds(file, "allyIds", dto.allyIds, Side.Ally, MaxAllies, characters, placed);

            WaveDto[] waveDtos = FieldCheck.Items(file, "waves", dto.waves, 1, MaxWaves);
            var waves = new List<IReadOnlyList<int>>(waveDtos.Length);
            var approach = new List<int>(waveDtos.Length);
            for (int i = 0; i < waveDtos.Length; i++)
            {
                string field = $"waves[{i}]";
                IReadOnlyList<int> enemyIds = ReadIds(file, field + ".enemyIds", waveDtos[i].enemyIds, Side.Enemy, MaxEnemiesPerWave, characters, placed);
                RejectEarlyBoss(file, field + ".enemyIds", enemyIds, characters, isLastWave: i == waveDtos.Length - 1);
                waves.Add(enemyIds);
                approach.Add(FieldCheck.Int(file, field + ".approachFu", waveDtos[i].approachFu, 1, BattleSetup.MaxApproachFu));
            }

            return new StageData(
                seconds * BattleSimulation.TicksPerSecond, precondition, allyIds, waves.AsReadOnly(), approach.AsReadOnly());
        }

        /// <summary>보스는 마지막 조우에만 선다 — 중간 조우에 서면 뒤 조우가 보스보다 약해 전장의 절정이 사라진다.</summary>
        private static void RejectEarlyBoss(
            string file, string field, IReadOnlyList<int> enemyIds, IReadOnlyList<CharacterData> characters, bool isLastWave)
        {
            if (isLastWave) return;

            for (int j = 0; j < enemyIds.Count; j++)
            {
                if (CharacterData.Find(characters, enemyIds[j]).IsBoss)
                {
                    throw new GameDataException(file, $"{field}[{j}]", $"보스 {enemyIds[j]} 는 마지막 조우에만 설 수 있습니다.");
                }
            }
        }

        private static IReadOnlyList<int> ReadIds(
            string file, string field, int[] ids, Side side, int maxCount, IReadOnlyList<CharacterData> characters, HashSet<int> placed)
        {
            ids = FieldCheck.Ints(file, field, ids, 1, maxCount);
            var result = new List<int>(ids.Length);

            for (int j = 0; j < ids.Length; j++)
            {
                int id = ids[j];
                string itemField = $"{field}[{j}]";
                CharacterData character = CharacterData.Find(characters, id);
                if (character == null) throw new GameDataException(file, itemField, $"캐릭터 ID {id} 가 {DataFiles.Characters} 에 없습니다.");
                if (character.Side != side) throw new GameDataException(file, itemField, $"캐릭터 {id} 는 {character.Side} 진영입니다. {side} 여야 합니다.");
                if (!placed.Add(id)) throw new GameDataException(file, itemField, $"캐릭터 {id} 가 두 번 배치됐습니다.");
                result.Add(id);
            }

            return result.AsReadOnly();
        }
    }
}
