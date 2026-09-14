using System.Collections.Generic;
using TAoB.Combat;

namespace TAoB.Data
{
    public sealed class CatalystData
    {
        internal CatalystData(string id, string name, Catalyst catalyst)
        {
            Id = id;
            Name = name;
            Catalyst = catalyst;
        }

        public string Id { get; }
        public string Name { get; }
        public Catalyst Catalyst { get; }
    }

    public sealed class MagitoolData
    {
        internal MagitoolData(string id, string name, Magitool magitool)
        {
            Id = id;
            Name = name;
            Magitool = magitool;
        }

        public string Id { get; }
        public string Name { get; }
        public Magitool Magitool { get; }
    }

    public sealed class EquipmentCatalog
    {
        internal EquipmentCatalog(List<CatalystData> catalysts, List<MagitoolData> magitools)
        {
            Catalysts = catalysts.AsReadOnly();
            Magitools = magitools.AsReadOnly();
        }

        public IReadOnlyList<CatalystData> Catalysts { get; }
        public IReadOnlyList<MagitoolData> Magitools { get; }

        /// <summary>없으면 null.</summary>
        public CatalystData FindCatalyst(string id)
        {
            for (int i = 0; i < Catalysts.Count; i++)
            {
                if (Catalysts[i].Id == id) return Catalysts[i];
            }

            return null;
        }

        /// <summary>없으면 null.</summary>
        public MagitoolData FindMagitool(string id)
        {
            for (int i = 0; i < Magitools.Count; i++)
            {
                if (Magitools[i].Id == id) return Magitools[i];
            }

            return null;
        }
    }

    /// <summary>캐릭터 한 명의 정의와 표시 이름. 전투 입력은 <see cref="ToUnitSetup"/> 으로 만든다.</summary>
    public sealed class CharacterData
    {
        internal CharacterData(
            int id, string name, Side side, Job job, Row row, Stats stats, SkillDefinition skill,
            CatalystData catalyst, MagitoolData magitool, bool isBoss)
        {
            Id = id;
            Name = name;
            Side = side;
            Job = job;
            Row = row;
            Stats = stats;
            Skill = skill;
            Catalyst = catalyst;
            Magitool = magitool;
            IsBoss = isBoss;
        }

        public int Id { get; }
        public string Name { get; }
        public Side Side { get; }
        public Job Job { get; }
        public Row Row { get; }
        public Stats Stats { get; }
        public SkillDefinition Skill { get; }

        /// <summary>없으면 null.</summary>
        public CatalystData Catalyst { get; }

        /// <summary>없으면 null.</summary>
        public MagitoolData Magitool { get; }
        public bool IsBoss { get; }

        public UnitSetup ToUnitSetup()
        {
            return new UnitSetup(Id, Side, Job, Row, Stats, Skill, Catalyst?.Catalyst, Magitool?.Magitool, IsBoss);
        }

        /// <summary>없으면 null. 캐릭터는 열 명 안팎이라 선형 탐색으로 충분하다.</summary>
        public static CharacterData Find(IReadOnlyList<CharacterData> characters, int id)
        {
            for (int i = 0; i < characters.Count; i++)
            {
                if (characters[i].Id == id) return characters[i];
            }

            return null;
        }
    }

    public sealed class StageData
    {
        internal StageData(int timeLimitTicks, PreconditionKind precondition, IReadOnlyList<int> allyIds, IReadOnlyList<IReadOnlyList<int>> waves)
        {
            TimeLimitTicks = timeLimitTicks;
            Precondition = precondition;
            AllyIds = allyIds;
            Waves = waves;
        }

        public int TimeLimitTicks { get; }
        public PreconditionKind Precondition { get; }
        public IReadOnlyList<int> AllyIds { get; }

        /// <summary>웨이브별 적 캐릭터 ID.</summary>
        public IReadOnlyList<IReadOnlyList<int>> Waves { get; }
    }

    /// <summary>검증을 통과한 데이터 전체. 로더만 만든다.</summary>
    public sealed class GameData
    {
        internal GameData(BalanceConfig balance, AffinityTable affinity, EquipmentCatalog equipment, IReadOnlyList<CharacterData> characters, StageData stage)
        {
            Balance = balance;
            Affinity = affinity;
            Equipment = equipment;
            Characters = characters;
            Stage = stage;
        }

        public BalanceConfig Balance { get; }
        public AffinityTable Affinity { get; }
        public EquipmentCatalog Equipment { get; }
        public IReadOnlyList<CharacterData> Characters { get; }
        public StageData Stage { get; }

        /// <summary>없으면 null.</summary>
        public CharacterData FindCharacter(int id)
        {
            return CharacterData.Find(Characters, id);
        }

        /// <summary>스테이지 편성으로 전투 입력을 만든다. 로더가 이미 검증했으므로 여기서는 실패하지 않아야 한다.</summary>
        public BattleSetup CreateBattleSetup()
        {
            var waves = new List<IReadOnlyList<UnitSetup>>(Stage.Waves.Count);
            for (int i = 0; i < Stage.Waves.Count; i++)
            {
                waves.Add(ToUnits(Stage.Waves[i]));
            }

            return new BattleSetup(Balance, Affinity, ToUnits(Stage.AllyIds), waves, Stage.TimeLimitTicks, Stage.Precondition);
        }

        private IReadOnlyList<UnitSetup> ToUnits(IReadOnlyList<int> ids)
        {
            var units = new List<UnitSetup>(ids.Count);
            for (int i = 0; i < ids.Count; i++)
            {
                units.Add(FindCharacter(ids[i]).ToUnitSetup());
            }

            return units;
        }
    }
}
