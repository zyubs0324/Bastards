using System.Collections.Generic;
using TAoB.Combat;

namespace TAoB.Data
{
    /// <summary>equipment.json → <see cref="EquipmentCatalog"/>.</summary>
    internal static class EquipmentReader
    {
        // 배열 크기 상한은 밸런스 값이 아니라 로더 규칙이다(현재 원석 2·마도구 5).
        public const int MaxCatalysts = 50;
        public const int MaxMagitools = 50;

        public static EquipmentCatalog Read(string file, EquipmentDto dto)
        {
            const int max = CombatScale.MaxValue;

            CatalystDto[] catalystDtos = FieldCheck.Items(file, "catalysts", dto.catalysts, 0, MaxCatalysts);
            var catalysts = new List<CatalystData>(catalystDtos.Length);
            var catalystIds = new HashSet<string>();
            for (int i = 0; i < catalystDtos.Length; i++)
            {
                CatalystDto c = catalystDtos[i];
                string field = $"catalysts[{i}]";
                string id = UniqueId(file, field + ".id", c.id, catalystIds);
                string name = FieldCheck.Text(file, field + ".name", c.name);
                int power = FieldCheck.Int(file, field + ".power", c.power, 0, max);
                catalysts.Add(new CatalystData(id, name, new Catalyst(power)));
            }

            MagitoolDto[] magitoolDtos = FieldCheck.Items(file, "magitools", dto.magitools, 0, MaxMagitools);
            var magitools = new List<MagitoolData>(magitoolDtos.Length);
            var magitoolIds = new HashSet<string>();
            for (int i = 0; i < magitoolDtos.Length; i++)
            {
                MagitoolDto m = magitoolDtos[i];
                string field = $"magitools[{i}]";
                string id = UniqueId(file, field + ".id", m.id, magitoolIds);
                string name = FieldCheck.Text(file, field + ".name", m.name);
                var tool = new Magitool(
                    FieldCheck.Int(file, field + ".atkBonus", m.atkBonus, 0, max),
                    FieldCheck.Int(file, field + ".requiredRes", m.requiredRes, 0, max),
                    FieldCheck.Int(file, field + ".requiredMnd", m.requiredMnd, 0, max),
                    FieldCheck.Int(file, field + ".maxDurability", m.maxDurability, 0, max),
                    FieldCheck.Int(file, field + ".durabilityCostPerUse", m.durabilityCostPerUse, 0, max));
                magitools.Add(new MagitoolData(id, name, tool));
            }

            return new EquipmentCatalog(catalysts, magitools);
        }

        private static string UniqueId(string file, string field, string value, HashSet<string> seen)
        {
            string id = FieldCheck.Text(file, field, value);
            if (!seen.Add(id)) throw new GameDataException(file, field, $"ID '{id}' 가 중복됩니다.");
            return id;
        }
    }
}
