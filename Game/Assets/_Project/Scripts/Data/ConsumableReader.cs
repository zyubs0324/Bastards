using System.Collections.Generic;
using TAoB.Combat;

namespace TAoB.Data
{
    /// <summary>consumables.json → 소모품 목록. 값의 상한은 규칙 모듈의 상수를 쓴다(<see cref="ConsumableSetup"/>·<see cref="BattleSetup"/>).</summary>
    internal static class ConsumableReader
    {
        public static IReadOnlyList<ConsumableData> Read(string file, ConsumablesDto dto)
        {
            const string name = "consumables";
            ConsumableDto[] dtos = FieldCheck.Items(file, name, dto.consumables, 0, BattleSetup.MaxConsumableKinds);

            var items = new List<ConsumableData>(dtos.Length);
            var ids = new HashSet<int>();
            for (int i = 0; i < dtos.Length; i++)
            {
                ConsumableDto c = dtos[i];
                string field = $"{name}[{i}]";
                int id = FieldCheck.Int(file, field + ".id", c.id, 0, CombatScale.MaxValue);
                if (!ids.Add(id)) throw new GameDataException(file, field + ".id", $"ID {id} 가 중복됩니다.");

                string itemName = FieldCheck.Text(file, field + ".name", c.name);
                int healAmount = FieldCheck.Int(file, field + ".healAmount", c.healAmount, 1, CombatScale.MaxValue);
                int count = FieldCheck.Int(file, field + ".count", c.count, 1, ConsumableSetup.MaxCount);
                items.Add(new ConsumableData(itemName, new ConsumableSetup(id, healAmount, count)));
            }

            return items.AsReadOnly();
        }
    }
}
