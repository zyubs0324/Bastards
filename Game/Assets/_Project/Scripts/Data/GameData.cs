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
            int id, string name, Side side, Job job, Row row, Stats stats, int skillChance, Preference preference, int formationChance,
            SkillDefinition skill, CatalystData catalyst, MagitoolData magitool, bool isBoss)
        {
            Id = id;
            Name = name;
            Side = side;
            Job = job;
            Row = row;
            Stats = stats;
            SkillChance = skillChance;
            Preference = preference;
            FormationChance = formationChance;
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

        /// <summary>기술 자동 사용 빈도(만분율). 숨은 스탯이라 화면에 보이지 않는다.</summary>
        public int SkillChance { get; }

        /// <summary>조우마다 진형유지 값을 통과했을 때 따르는 성향.</summary>
        public Preference Preference { get; }

        /// <summary>숨은 진형유지 값(만분율).</summary>
        public int FormationChance { get; }
        public SkillDefinition Skill { get; }

        /// <summary>없으면 null.</summary>
        public CatalystData Catalyst { get; }

        /// <summary>없으면 null.</summary>
        public MagitoolData Magitool { get; }
        public bool IsBoss { get; }

        public UnitSetup ToUnitSetup()
        {
            return new UnitSetup(Id, Side, Job, Row, Stats, SkillChance, Preference, FormationChance, Skill, Catalyst?.Catalyst, Magitool?.Magitool, IsBoss);
        }

        /// <summary>장비만 바꾼 사본(개발용 장비 교체). 장착 가능 여부는 검사하지 않는다 — 호출자가 <see cref="EquipRules.CanEquip"/> 으로 거른다.</summary>
        public CharacterData WithEquipment(CatalystData catalyst, MagitoolData magitool)
        {
            return new CharacterData(Id, Name, Side, Job, Row, Stats, SkillChance, Preference, FormationChance, Skill, catalyst, magitool, IsBoss);
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

    /// <summary>소모품 한 종류의 표시 이름과 전투 입력.</summary>
    public sealed class ConsumableData
    {
        internal ConsumableData(string name, ConsumableSetup setup)
        {
            Name = name;
            Setup = setup;
        }

        public int Id => Setup.Id;
        public string Name { get; }
        public ConsumableSetup Setup { get; }
    }

    public sealed class StageData
    {
        internal StageData(
            int timeLimitTicks,
            PreconditionKind precondition,
            IReadOnlyList<int> allyIds,
            IReadOnlyList<IReadOnlyList<int>> waves,
            IReadOnlyList<int> waveApproachFu)
        {
            TimeLimitTicks = timeLimitTicks;
            Precondition = precondition;
            AllyIds = allyIds;
            Waves = waves;
            WaveApproachFu = waveApproachFu;
        }

        public int TimeLimitTicks { get; }
        public PreconditionKind Precondition { get; }
        public IReadOnlyList<int> AllyIds { get; }

        /// <summary>웨이브별 적 캐릭터 ID.</summary>
        public IReadOnlyList<IReadOnlyList<int>> Waves { get; }

        /// <summary>웨이브별 접근 거리(fu). <see cref="Waves"/> 와 개수가 같다.</summary>
        public IReadOnlyList<int> WaveApproachFu { get; }
    }

    /// <summary>검증을 통과한 데이터 전체. 로더만 만든다.</summary>
    public sealed class GameData
    {
        internal GameData(
            BalanceConfig balance,
            AffinityTable affinity,
            EquipmentCatalog equipment,
            IReadOnlyList<CharacterData> characters,
            StageData stage,
            IReadOnlyList<ConsumableData> consumables)
        {
            RejectUnreachableStage(balance, stage);
            RejectRescueLongerThanTheBattle(balance, stage);
            Balance = balance;
            Affinity = affinity;
            Equipment = equipment;
            Characters = characters;
            Stage = stage;
            Consumables = consumables;
        }

        public BalanceConfig Balance { get; }
        public AffinityTable Affinity { get; }
        public EquipmentCatalog Equipment { get; }
        public IReadOnlyList<CharacterData> Characters { get; }
        public StageData Stage { get; }
        public IReadOnlyList<ConsumableData> Consumables { get; }

        /// <summary>
        /// 구조 한 번이 전투보다 길면 구조가 끝나지 못하고 진형이 제한 시간까지 묶인다. 구조 시간은 밸런스 파일,
        /// 제한 시간은 스테이지 파일에 있어 이 조립 지점에서 본다.
        /// </summary>
        private static void RejectRescueLongerThanTheBattle(BalanceConfig balance, StageData stage)
        {
            if (balance.RescueTicks > stage.TimeLimitTicks)
            {
                throw new GameDataException(DataFiles.Balance, "rescueTicks",
                    $"구조 시간 {balance.RescueTicks}틱이 제한 시간 {stage.TimeLimitTicks}틱보다 깁니다.");
            }
        }

        /// <summary>
        /// 행군으로 닿을 수 없는 거리를 거부한다. 접근 거리는 스테이지 파일, 행군 속도는 밸런스 파일에 있어
        /// 두 파일을 함께 아는 이 조립 지점만 검증할 수 있다 — 리더 한쪽에 두면 조용히 건너뛴다.
        /// </summary>
        private static void RejectUnreachableStage(BalanceConfig balance, StageData stage)
        {
            long span = 0;
            for (int i = 0; i < stage.WaveApproachFu.Count; i++)
            {
                span += stage.WaveApproachFu[i];
            }

            long marchable = (long)balance.Lane.MarchSpeedFu * stage.TimeLimitTicks;
            if (span > marchable)
            {
                throw new GameDataException(DataFiles.Stage, "waves[].approachFu",
                    $"접근 거리 합 {span} fu 는 제한 시간 안에 행군할 수 있는 {marchable} fu 를 넘습니다" +
                    $"(행군 속도 {balance.Lane.MarchSpeedFu} fu/틱 × {stage.TimeLimitTicks}틱).");
            }
        }

        /// <summary>없으면 null.</summary>
        public CharacterData FindCharacter(int id)
        {
            return CharacterData.Find(Characters, id);
        }

        /// <summary>스테이지 편성으로 전투 입력을 만든다. 로더가 이미 검증했으므로 여기서는 실패하지 않아야 한다.</summary>
        public BattleSetup CreateBattleSetup()
        {
            return CreateBattleSetup(null);
        }

        /// <summary>
        /// ID 가 겹치는 캐릭터를 <paramref name="replacements"/> 의 것으로 바꿔 전투 입력을 만든다(개발용 장비 교체).
        /// 바꾼 캐릭터가 장착 불가면 <see cref="BattleSetup"/> 생성자가 예외를 던진다.
        /// </summary>
        public BattleSetup CreateBattleSetup(IReadOnlyDictionary<int, CharacterData> replacements)
        {
            var waves = new List<IReadOnlyList<UnitSetup>>(Stage.Waves.Count);
            for (int i = 0; i < Stage.Waves.Count; i++)
            {
                waves.Add(ToUnits(Stage.Waves[i], replacements));
            }

            var consumables = new List<ConsumableSetup>(Consumables.Count);
            for (int i = 0; i < Consumables.Count; i++)
            {
                consumables.Add(Consumables[i].Setup);
            }

            return new BattleSetup(
                Balance, Affinity, ToUnits(Stage.AllyIds, replacements), waves, Stage.WaveApproachFu, Stage.TimeLimitTicks, Stage.Precondition,
                consumables);
        }

        /// <summary>편성에 실제로 들어가는 캐릭터(교체본이 있으면 그것).</summary>
        public CharacterData ResolveCharacter(int id, IReadOnlyDictionary<int, CharacterData> replacements)
        {
            if (replacements != null && replacements.TryGetValue(id, out CharacterData replaced)) return replaced;
            return FindCharacter(id);
        }

        private IReadOnlyList<UnitSetup> ToUnits(IReadOnlyList<int> ids, IReadOnlyDictionary<int, CharacterData> replacements)
        {
            var units = new List<UnitSetup>(ids.Count);
            for (int i = 0; i < ids.Count; i++)
            {
                units.Add(ResolveCharacter(ids[i], replacements).ToUnitSetup());
            }

            return units;
        }
    }
}
