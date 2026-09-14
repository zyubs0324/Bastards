using NUnit.Framework;
using TAoB.Combat;

namespace TAoB.Data.Tests
{
    /// <summary>계획서 Phase 3 검증 목록: 정상 로드 · 필드 누락 · 음수 · 미존재 ID · 상한 규칙 5종, 그리고 파일 규칙(마지막 구간·열거형·생략 bool).</summary>
    public class DataValidationTests
    {
        private static GameDataException Fails(GameDataFiles files)
        {
            return Assert.Throws<GameDataException>(() => GameDataLoader.Parse(files));
        }

        private static void AssertLocation(GameDataException ex, string file, string field)
        {
            Assert.AreEqual(file, ex.FileName, ex.Message);
            Assert.AreEqual(field, ex.Field, ex.Message);
        }

        [Test]
        public void ValidFixture_Parses_AndBuildsBattleSetup()
        {
            GameData data = GameDataLoader.Parse(TestJson.Files());

            Assert.AreEqual(100, data.Balance.DefenseK);
            Assert.AreEqual(int.MaxValue, data.Balance.OverloadTiers[1].MaxExcessPermille);
            Assert.AreEqual(AffinityLevel.Strong, data.Affinity.Get(Job.Knight, Job.Perceptive));
            Assert.AreEqual(AffinityLevel.Normal, data.Affinity.Get(Job.Perceptive, Job.Knight));
            Assert.AreEqual(600, data.Stage.TimeLimitTicks);
            Assert.AreEqual("tool", data.FindCharacter(1).Magitool.Id);
            Assert.IsNull(data.FindCharacter(1).Catalyst);

            BattleSetup setup = data.CreateBattleSetup();
            Assert.AreEqual(2, setup.Allies.Count);
            Assert.AreEqual(2, setup.Waves.Count);
            Assert.IsTrue(setup.Waves[1][0].IsBoss);
        }

        [Test]
        public void OmittedBool_ReadsAsFalse()
        {
            GameData data = GameDataLoader.Parse(TestJson.Files());

            Assert.IsFalse(data.FindCharacter(1).Skill.CanOverload);
            Assert.IsFalse(data.FindCharacter(1).IsBoss);
            Assert.IsTrue(data.FindCharacter(2).Skill.CanOverload);
            Assert.IsTrue(data.FindCharacter(201).IsBoss);
        }

        [Test]
        public void MissingField_NamesFileAndField()
        {
            string json = TestJson.Swap(TestJson.Characters, @"""hp"": 300, ", "");

            var ex = Fails(TestJson.Files(characters: json));

            AssertLocation(ex, DataFiles.Characters, "characters[0].stats.hp");
        }

        [Test]
        public void MissingObject_IsReportedAsMissingFieldInside()
        {
            string json = TestJson.Swap(TestJson.Characters,
                @"""stats"": { ""atk"": 40, ""def"": 5, ""hp"": 300, ""spd"": 100, ""mnd"": 20, ""res"": 30 },", "");

            var ex = Fails(TestJson.Files(characters: json));

            AssertLocation(ex, DataFiles.Characters, "characters[0].stats.atk");
        }

        [Test]
        public void NegativeValue_IsRejected()
        {
            string json = TestJson.Swap(TestJson.Characters, @"""def"": 5, ""hp"": 300", @"""def"": -5, ""hp"": 300");

            var ex = Fails(TestJson.Files(characters: json));

            AssertLocation(ex, DataFiles.Characters, "characters[0].stats.def");
        }

        [Test]
        public void ZeroHp_IsRejected()
        {
            string json = TestJson.Swap(TestJson.Characters, @"""hp"": 300", @"""hp"": 0");

            var ex = Fails(TestJson.Files(characters: json));

            AssertLocation(ex, DataFiles.Characters, "characters[0].stats.hp");
        }

        [Test]
        public void UnknownEquipmentId_IsRejected()
        {
            string json = TestJson.Swap(TestJson.Characters, @"""magitoolId"": ""tool""", @"""magitoolId"": ""ghost""");

            var ex = Fails(TestJson.Files(characters: json));

            AssertLocation(ex, DataFiles.Characters, "characters[0].magitoolId");
            StringAssert.Contains("ghost", ex.Message);
        }

        [Test]
        public void UnknownCatalystId_IsRejected()
        {
            string json = TestJson.Swap(TestJson.Characters, @"""catalystId"": ""cat""", @"""catalystId"": ""ghost""");

            var ex = Fails(TestJson.Files(characters: json));

            AssertLocation(ex, DataFiles.Characters, "characters[1].catalystId");
        }

        [Test]
        public void DuplicateCatalystId_IsRejected()
        {
            string json = TestJson.Swap(TestJson.Equipment,
                @"{ ""id"": ""cat"", ""name"": ""원석"", ""power"": 80 }",
                @"{ ""id"": ""cat"", ""name"": ""원석"", ""power"": 80 }, { ""id"": ""cat"", ""name"": ""원석2"", ""power"": 90 }");

            var ex = Fails(TestJson.Files(equipment: json));

            AssertLocation(ex, DataFiles.Equipment, "catalysts[1].id");
        }

        [Test]
        public void MissingBalanceField_NamesField()
        {
            string json = TestJson.Swap(TestJson.Balance, @"""defenseK"": 100,", "");

            var ex = Fails(TestJson.Files(balance: json));

            AssertLocation(ex, DataFiles.Balance, "defenseK");
        }

        [Test]
        public void BalanceBelowMinimum_NamesFieldFromConstructor()
        {
            // 하한은 BalanceConfig 생성자만 안다. 그 예외의 매개변수 이름이 필드명으로 옮겨져야 한다.
            string json = TestJson.Swap(TestJson.Balance, @"""defenseK"": 100,", @"""defenseK"": 0,");

            var ex = Fails(TestJson.Files(balance: json));

            AssertLocation(ex, DataFiles.Balance, "defenseK");
        }

        [Test]
        public void EveryBalanceKey_MatchesConstructorParameterName()
        {
            // 로더는 BalanceConfig 생성자 예외의 매개변수 이름을 JSON 키로 되돌려 준다. 그 이름 규약이 21개 전부에서 성립하는지 고정한다.
            var ctor = typeof(BalanceConfig).GetConstructors()[0];
            int checkedCount = 0;

            foreach (var parameter in ctor.GetParameters())
            {
                if (parameter.ParameterType != typeof(int)) continue;
                string key = "\"" + parameter.Name + "\": ";
                StringAssert.Contains(key, TestJson.Balance, $"픽스처에 생성자 매개변수 {parameter.Name} 과 같은 키가 없습니다.");

                string json = System.Text.RegularExpressions.Regex.Replace(TestJson.Balance, System.Text.RegularExpressions.Regex.Escape(key) + @"\d+", key + int.MaxValue);
                var ex = Fails(TestJson.Files(balance: json));
                AssertLocation(ex, DataFiles.Balance, parameter.Name);
                checkedCount++;
            }

            Assert.AreEqual(21, checkedCount);
        }

        /// <summary>상한이 있는 구간 <paramref name="boundedCount"/>개를 픽스처의 첫 구간 자리에 넣는다(마지막 무상한 구간은 픽스처 것을 그대로 둠).</summary>
        private static string BalanceWithBoundedTiers(int boundedCount)
        {
            var tiers = new System.Text.StringBuilder();
            for (int i = 0; i < boundedCount; i++)
            {
                if (i > 0) tiers.Append(", ");
                tiers.Append(@"{ ""maxExcessPermille"": " + (100 * (i + 1)) + @", ""successChance"": 10000, ""scatterChance"": 0, ""runawayChance"": 0, ""breakthroughChance"": 0 }");
            }

            return TestJson.Swap(TestJson.Balance,
                @"{ ""maxExcessPermille"": 1000, ""successChance"": 4000, ""scatterChance"": 3000, ""runawayChance"": 2000, ""breakthroughChance"": 1000 },",
                tiers + ",");
        }

        [Test]
        public void TooManyOverloadTiers_IsRejected()
        {
            var ex = Fails(TestJson.Files(balance: BalanceWithBoundedTiers(10)));

            AssertLocation(ex, DataFiles.Balance, "overloadTiers");
        }

        [Test]
        public void MaxOverloadTiers_IsAccepted()
        {
            GameData data = GameDataLoader.Parse(TestJson.Files(balance: BalanceWithBoundedTiers(9)));

            Assert.AreEqual(10, data.Balance.OverloadTiers.Count);
        }

        [Test]
        public void MaxWaves_IsAccepted()
        {
            string characters = TestJson.CharactersWithExtraEnemies(3);
            string stage = TestJson.Swap(TestJson.Stage,
                @"[ { ""enemyIds"": [101] }, { ""enemyIds"": [201] } ]",
                @"[ { ""enemyIds"": [101] }, { ""enemyIds"": [201] }, { ""enemyIds"": [202] }, { ""enemyIds"": [203] }, { ""enemyIds"": [204] } ]");

            GameData data = GameDataLoader.Parse(TestJson.Files(characters: characters, stage: stage));

            Assert.AreEqual(5, data.Stage.Waves.Count);
        }

        [Test]
        public void MaxEnemiesInWave_IsAccepted()
        {
            string characters = TestJson.CharactersWithExtraEnemies(3);
            string stage = TestJson.Swap(TestJson.Stage,
                @"[ { ""enemyIds"": [101] }, { ""enemyIds"": [201] } ]",
                @"[ { ""enemyIds"": [101, 201, 202, 203, 204] } ]");

            GameData data = GameDataLoader.Parse(TestJson.Files(characters: characters, stage: stage));

            Assert.AreEqual(5, data.Stage.Waves[0].Count);
        }

        [Test]
        public void UnknownCharacterIdInStage_IsRejected()
        {
            string json = TestJson.Swap(TestJson.Stage, "[201]", "[999]");

            var ex = Fails(TestJson.Files(stage: json));

            AssertLocation(ex, DataFiles.Stage, "waves[1].enemyIds[0]");
        }

        [Test]
        public void TimeLimitOverMaximum_IsRejected()
        {
            string json = TestJson.Swap(TestJson.Stage, @"""timeLimitSeconds"": 60", @"""timeLimitSeconds"": 601");

            var ex = Fails(TestJson.Files(stage: json));

            AssertLocation(ex, DataFiles.Stage, "timeLimitSeconds");
        }

        [Test]
        public void TimeLimitAtMaximum_IsAccepted()
        {
            string json = TestJson.Swap(TestJson.Stage, @"""timeLimitSeconds"": 60", @"""timeLimitSeconds"": 600");

            GameData data = GameDataLoader.Parse(TestJson.Files(stage: json));

            Assert.AreEqual(BattleSetup.MaxTimeLimitTicks, data.Stage.TimeLimitTicks);
        }

        [Test]
        public void TooManyWaves_IsRejected()
        {
            string json = TestJson.Swap(TestJson.Stage,
                @"[ { ""enemyIds"": [101] }, { ""enemyIds"": [201] } ]",
                @"[ { ""enemyIds"": [101] }, { ""enemyIds"": [201] }, { ""enemyIds"": [101] }, { ""enemyIds"": [101] }, { ""enemyIds"": [101] }, { ""enemyIds"": [101] } ]");

            var ex = Fails(TestJson.Files(stage: json));

            AssertLocation(ex, DataFiles.Stage, "waves");
        }

        [Test]
        public void TooManyEnemiesInWave_IsRejected()
        {
            string json = TestJson.Swap(TestJson.Stage, "[101]", "[101, 101, 101, 101, 101, 101]");

            var ex = Fails(TestJson.Files(stage: json));

            AssertLocation(ex, DataFiles.Stage, "waves[0].enemyIds");
        }

        [Test]
        public void ChanceOutOfRange_IsRejected()
        {
            string json = TestJson.Swap(TestJson.Balance, @"""critBaseChance"": 500", @"""critBaseChance"": 10001");

            var ex = Fails(TestJson.Files(balance: json));

            AssertLocation(ex, DataFiles.Balance, "critBaseChance");
        }

        [Test]
        public void OverloadTierSumNot10000_IsRejected()
        {
            string json = TestJson.Swap(TestJson.Balance, @"""successChance"": 4000", @"""successChance"": 4001");

            var ex = Fails(TestJson.Files(balance: json));

            AssertLocation(ex, DataFiles.Balance, "overloadTiers[0]");
            StringAssert.Contains("10001", ex.Message);
        }

        [Test]
        public void LastTierWithMaxExcess_IsRejected()
        {
            string json = TestJson.Swap(TestJson.Balance, @"{ ""successChance"": 1000", @"{ ""maxExcessPermille"": 5000, ""successChance"": 1000");

            var ex = Fails(TestJson.Files(balance: json));

            AssertLocation(ex, DataFiles.Balance, "overloadTiers[1].maxExcessPermille");
        }

        [Test]
        public void TiersNotAscending_IsRejected()
        {
            string json = TestJson.Swap(TestJson.Balance,
                @"{ ""maxExcessPermille"": 1000, ""successChance"": 4000, ""scatterChance"": 3000, ""runawayChance"": 2000, ""breakthroughChance"": 1000 },",
                @"{ ""maxExcessPermille"": 1000, ""successChance"": 4000, ""scatterChance"": 3000, ""runawayChance"": 2000, ""breakthroughChance"": 1000 },
    { ""maxExcessPermille"": 1000, ""successChance"": 4000, ""scatterChance"": 3000, ""runawayChance"": 2000, ""breakthroughChance"": 1000 },");

            var ex = Fails(TestJson.Files(balance: json));

            AssertLocation(ex, DataFiles.Balance, "overloadTiers[1].maxExcessPermille");
        }

        [Test]
        public void UnknownEnumName_IsRejected()
        {
            string json = TestJson.Swap(TestJson.Characters, @"""job"": ""Mage""", @"""job"": ""mage""");

            var ex = Fails(TestJson.Files(characters: json));

            AssertLocation(ex, DataFiles.Characters, "characters[1].job");
            StringAssert.Contains("Knight, Mage, Perceptive", ex.Message);
        }

        [Test]
        public void DuplicateAffinity_IsRejected()
        {
            string json = TestJson.Swap(TestJson.Balance,
                @"{ ""attacker"": ""Knight"", ""defender"": ""Perceptive"", ""level"": ""Strong"" }",
                @"{ ""attacker"": ""Knight"", ""defender"": ""Perceptive"", ""level"": ""Strong"" }, { ""attacker"": ""Knight"", ""defender"": ""Perceptive"", ""level"": ""Weak"" }");

            var ex = Fails(TestJson.Files(balance: json));

            AssertLocation(ex, DataFiles.Balance, "affinities");
        }

        [Test]
        public void MagitoolBlockedByRes_IsRejected()
        {
            // 요구 RES 10 → 100: 기사 RES 30 으로는 부족 70 ≥ 장착 불가 임계 20.
            string json = TestJson.Swap(TestJson.Equipment, @"""requiredRes"": 10", @"""requiredRes"": 100");

            var ex = Fails(TestJson.Files(equipment: json));

            AssertLocation(ex, DataFiles.Characters, "characters[0].magitoolId");
        }

        [Test]
        public void DuplicateCharacterId_IsRejected()
        {
            string json = TestJson.Swap(TestJson.Characters, @"""id"": 2,", @"""id"": 1,");

            var ex = Fails(TestJson.Files(characters: json));

            AssertLocation(ex, DataFiles.Characters, "characters[1].id");
        }

        [Test]
        public void EnemyPlacedAsAlly_IsRejected()
        {
            string json = TestJson.Swap(TestJson.Stage, "[1, 2]", "[1, 101]");

            var ex = Fails(TestJson.Files(stage: json));

            AssertLocation(ex, DataFiles.Stage, "allyIds[1]");
        }

        [Test]
        public void CharacterPlacedTwice_IsRejected()
        {
            string json = TestJson.Swap(TestJson.Stage, "[201]", "[101]");

            var ex = Fails(TestJson.Files(stage: json));

            AssertLocation(ex, DataFiles.Stage, "waves[1].enemyIds[0]");
        }

        [Test]
        public void MalformedJson_IsRejectedWithFileName()
        {
            var ex = Fails(TestJson.Files(equipment: "{ \"catalysts\": ["));

            AssertLocation(ex, DataFiles.Equipment, GameDataException.WholeFile);
        }

        [Test]
        public void EmptyText_IsRejectedWithFileName()
        {
            var ex = Fails(TestJson.Files(balance: "  "));

            AssertLocation(ex, DataFiles.Balance, GameDataException.WholeFile);
        }

        [Test]
        public void MissingArray_IsRejected()
        {
            string json = TestJson.Swap(TestJson.Stage, @"""allyIds"": [1, 2],", "");

            var ex = Fails(TestJson.Files(stage: json));

            AssertLocation(ex, DataFiles.Stage, "allyIds");
        }

        [Test]
        public void Swap_RejectsMissingOrRepeatedFragment()
        {
            Assert.Throws<System.InvalidOperationException>(() => TestJson.Swap("ab", "x", "y"));
            Assert.Throws<System.InvalidOperationException>(() => TestJson.Swap("aa", "a", "b"));
        }
    }
}
