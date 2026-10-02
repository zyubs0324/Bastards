using System;
using System.IO;
using TAoB.Combat;
using UnityEngine;

namespace TAoB.Data
{
    /// <summary>데이터 파일 이름. Resources 경로 리터럴은 이 파일 밖에 두지 않는다.</summary>
    public static class DataFiles
    {
        public const string ResourcesFolder = "Data";
        public const string Balance = "balance.json";
        public const string Equipment = "equipment.json";
        public const string Characters = "characters.json";
        public const string Stage = "stage_slice.json";
    }

    /// <summary>네 파일의 본문. 파일 접근과 해석을 나눠, 테스트가 문자열만으로 로더를 돌릴 수 있게 한다.</summary>
    public sealed class GameDataFiles
    {
        public GameDataFiles(string balance, string equipment, string characters, string stage)
        {
            Balance = balance;
            Equipment = equipment;
            Characters = characters;
            Stage = stage;
        }

        public string Balance { get; }
        public string Equipment { get; }
        public string Characters { get; }
        public string Stage { get; }
    }

    /// <summary>
    /// 데이터 파일을 읽어 검증하고 규칙 모듈 타입으로 바꾼다(수직 슬라이스 계획서 Phase 3).
    /// 하나라도 틀리면 <see cref="GameDataException"/> 으로 중단한다 — 잘못된 밸런스로 조용히 돌지 않는다.
    /// </summary>
    public static class GameDataLoader
    {
        public static GameData LoadFromResources()
        {
            return Parse(new GameDataFiles(
                ReadResource(DataFiles.Balance),
                ReadResource(DataFiles.Equipment),
                ReadResource(DataFiles.Characters),
                ReadResource(DataFiles.Stage)));
        }

        public static GameData Parse(GameDataFiles files)
        {
            if (files == null) throw new ArgumentNullException(nameof(files));

            // 읽는 순서 = 참조 방향: 장비 ← 캐릭터 ← 스테이지. 밸런스는 캐릭터의 장착 가능 판정에 필요하다.
            BalanceDto balanceDto = FieldCheck.FromJson<BalanceDto>(DataFiles.Balance, files.Balance);
            BalanceConfig balance = BalanceReader.ReadBalance(DataFiles.Balance, balanceDto);
            AffinityTable affinity = BalanceReader.ReadAffinity(DataFiles.Balance, balanceDto.affinities);

            EquipmentCatalog equipment = EquipmentReader.Read(
                DataFiles.Equipment, FieldCheck.FromJson<EquipmentDto>(DataFiles.Equipment, files.Equipment));

            var characters = CharacterReader.Read(
                DataFiles.Characters, FieldCheck.FromJson<CharactersDto>(DataFiles.Characters, files.Characters), equipment, balance);

            StageData stage = StageReader.Read(
                DataFiles.Stage, FieldCheck.FromJson<StageDto>(DataFiles.Stage, files.Stage), characters);

            return new GameData(balance, affinity, equipment, characters, stage);
        }

        private static string ReadResource(string fileName)
        {
            string path = DataFiles.ResourcesFolder + "/" + Path.GetFileNameWithoutExtension(fileName);
            TextAsset asset = Resources.Load<TextAsset>(path);
            if (asset == null) throw new GameDataException(fileName, GameDataException.WholeFile, $"Resources/{path} 에 없습니다.");
            return asset.text;
        }
    }
}
