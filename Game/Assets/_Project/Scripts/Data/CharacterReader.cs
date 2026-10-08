using System.Collections.Generic;
using TAoB.Combat;

namespace TAoB.Data
{
    /// <summary>characters.json → <see cref="CharacterData"/> 목록. 장비 ID 는 <see cref="EquipmentCatalog"/> 에서 찾는다.</summary>
    internal static class CharacterReader
    {
        // 배열 크기 상한은 밸런스 값이 아니라 로더 규칙이다(현재 캐릭터 7).
        public const int MaxCharacters = 50;

        // 기술 자동 사용 빈도의 기획 대역: 매우 낮음 1~4% ~ 자주 31~39%(계획서 D7).
        // 규칙 모듈은 0~만분율 전체를 받으므로 대역을 좁히는 것은 로더의 몫이다.
        public const int MinSkillChance = 100;
        public const int MaxSkillChance = 3900;

        public static IReadOnlyList<CharacterData> Read(string file, CharactersDto dto, EquipmentCatalog equipment, BalanceConfig balance)
        {
            CharacterDto[] dtos = FieldCheck.Items(file, "characters", dto.characters, 1, MaxCharacters);
            var ids = new HashSet<int>();
            var result = new List<CharacterData>(dtos.Length);

            for (int i = 0; i < dtos.Length; i++)
            {
                CharacterDto c = dtos[i];
                string field = $"characters[{i}]";

                int id = FieldCheck.Int(file, field + ".id", c.id, 0, int.MaxValue);
                if (!ids.Add(id)) throw new GameDataException(file, field + ".id", $"ID {id} 가 중복됩니다.");

                string name = FieldCheck.Text(file, field + ".name", c.name);
                Side side = FieldCheck.EnumName<Side>(file, field + ".side", c.side);
                Job job = FieldCheck.EnumName<Job>(file, field + ".job", c.job);
                Row row = FieldCheck.EnumName<Row>(file, field + ".row", c.row);
                Stats stats = ReadStats(file, field + ".stats", c.stats);
                int skillChance = FieldCheck.Int(file, field + ".skillChance", c.skillChance, MinSkillChance, MaxSkillChance);
                Preference preference = FieldCheck.EnumName<Preference>(file, field + ".preference", c.preference);
                int formationChance = FieldCheck.Int(file, field + ".formationChance", c.formationChance, 0, CombatScale.ChanceRange);
                SkillDefinition skill = ReadSkill(file, field + ".skill", c.skill);
                CatalystData catalyst = FindCatalyst(file, field + ".catalystId", c.catalystId, equipment);
                MagitoolData magitool = FindMagitool(file, field + ".magitoolId", c.magitoolId, equipment, stats, balance);

                result.Add(new CharacterData(id, name, side, job, row, stats, skillChance, preference, formationChance, skill, catalyst, magitool, c.isBoss));
            }

            return result.AsReadOnly();
        }

        private static Stats ReadStats(string file, string field, StatsDto s)
        {
            if (s == null) throw new GameDataException(file, field, "필드가 없습니다.");
            const int max = CombatScale.MaxValue;
            return new Stats(
                FieldCheck.Int(file, field + ".atk", s.atk, 0, max),
                FieldCheck.Int(file, field + ".def", s.def, 0, max),
                FieldCheck.Int(file, field + ".hp", s.hp, 1, max),
                FieldCheck.Int(file, field + ".spd", s.spd, 0, max),
                FieldCheck.Int(file, field + ".mnd", s.mnd, 0, max),
                FieldCheck.Int(file, field + ".res", s.res, 0, max));
        }

        private static SkillDefinition ReadSkill(string file, string field, SkillDto s)
        {
            if (s == null) throw new GameDataException(file, field, "필드가 없습니다.");
            return new SkillDefinition(
                FieldCheck.Int(file, field + ".powerPermille", s.powerPermille, 0, CombatScale.MaxPermille),
                FieldCheck.EnumName<TargetRule>(file, field + ".targetRule", s.targetRule),
                s.canOverload,
                FieldCheck.Int(file, field + ".cooldownTicks", s.cooldownTicks, 0, CombatScale.MaxValue));
        }

        private static CatalystData FindCatalyst(string file, string field, string id, EquipmentCatalog equipment)
        {
            if (string.IsNullOrEmpty(id)) return null;
            CatalystData found = equipment.FindCatalyst(id);
            if (found == null) throw new GameDataException(file, field, $"원석 '{id}' 가 {DataFiles.Equipment} 에 없습니다.");
            return found;
        }

        private static MagitoolData FindMagitool(string file, string field, string id, EquipmentCatalog equipment, Stats stats, BalanceConfig balance)
        {
            if (string.IsNullOrEmpty(id)) return null;
            MagitoolData found = equipment.FindMagitool(id);
            if (found == null) throw new GameDataException(file, field, $"마도구 '{id}' 가 {DataFiles.Equipment} 에 없습니다.");

            // BattleSetup 도 같은 검사를 하지만, 여기서 걸러야 어느 캐릭터의 어느 필드인지 알려줄 수 있다.
            if (!EquipRules.CanEquip(stats.Res, found.Magitool, balance))
            {
                int shortfall = EquipRules.ResShortfall(stats.Res, found.Magitool);
                throw new GameDataException(file, field,
                    $"RES {stats.Res} 로는 요구 RES {found.Magitool.RequiredRes} 의 마도구 '{id}' 를 장착할 수 없습니다(부족 {shortfall}, 장착 불가 임계 {balance.EquipBlockResShortfall}).");
            }

            return found;
        }
    }
}
