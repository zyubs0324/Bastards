using System;
using System.Collections.Generic;
using NUnit.Framework;
using TAoB.Combat;

namespace TAoB.Data.Tests
{
    /// <summary>개발용 장비 교체 경로: 사본 생성과 편성 교체, 장착 불가 교체는 규칙 모듈이 거부.</summary>
    public class LoadoutTests
    {
        [Test]
        public void WithEquipment_ChangesOnlyEquipment()
        {
            GameData data = GameDataLoader.Parse(TestJson.Files());
            CharacterData knight = data.FindCharacter(1);

            CharacterData changed = knight.WithEquipment(data.Equipment.FindCatalyst("cat"), null);

            Assert.AreEqual(knight.Id, changed.Id);
            Assert.AreEqual(knight.Stats.Res, changed.Stats.Res);
            Assert.AreEqual("cat", changed.Catalyst.Id);
            Assert.IsNull(changed.Magitool);
            Assert.AreEqual("tool", knight.Magitool.Id, "원본은 그대로");
        }

        [Test]
        public void CreateBattleSetup_UsesReplacement()
        {
            GameData data = GameDataLoader.Parse(TestJson.Files());
            CharacterData changed = data.FindCharacter(1).WithEquipment(null, null);
            var replacements = new Dictionary<int, CharacterData> { { 1, changed } };

            BattleSetup setup = data.CreateBattleSetup(replacements);

            Assert.IsNull(setup.Allies[0].Magitool);
            Assert.AreSame(changed, data.ResolveCharacter(1, replacements));
            Assert.AreSame(data.FindCharacter(2), data.ResolveCharacter(2, replacements));
        }

        [Test]
        public void CreateBattleSetup_RejectsReplacementThatCannotEquip()
        {
            // 요구 RES 100 마도구: 기사 RES 30 으로는 장착 불가. 로더가 아니라 BattleSetup 이 막는다.
            string equipment = TestJson.Swap(TestJson.Equipment,
                @"{ ""id"": ""tool"", ""name"": ""마도구"", ""atkBonus"": 10, ""requiredRes"": 10,",
                @"{ ""id"": ""heavy"", ""name"": ""무거운 마도구"", ""atkBonus"": 10, ""requiredRes"": 100, ""requiredMnd"": 10, ""maxDurability"": 20, ""durabilityCostPerUse"": 1 }, { ""id"": ""tool"", ""name"": ""마도구"", ""atkBonus"": 10, ""requiredRes"": 10,");
            GameData data = GameDataLoader.Parse(TestJson.Files(equipment: equipment));
            var replacements = new Dictionary<int, CharacterData>
            {
                { 1, data.FindCharacter(1).WithEquipment(null, data.Equipment.FindMagitool("heavy")) },
            };

            Assert.Throws<ArgumentException>(() => data.CreateBattleSetup(replacements));
        }
    }
}
