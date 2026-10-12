using NUnit.Framework;
using TAoB.Combat;

namespace TAoB.Data.Tests
{
    /// <summary>수직 슬라이스 계획서 Phase 3 검증 목록: 정상 로드 · 필드 누락 · 음수 · 미존재 ID · 상한 규칙 5종, 그리고 파일 규칙(마지막 구간·열거형·생략 bool).</summary>
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
            Assert.AreEqual(1, data.Balance.Lane.MarchSpeedFu);
            Assert.AreEqual(20000, data.Balance.Lane.FrontReachFu);
            Assert.AreEqual(2000, data.FindCharacter(1).SkillChance);
            CollectionAssert.AreEqual(new[] { 1, 1 }, data.Stage.WaveApproachFu);
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
            // 로더는 BalanceConfig 생성자 예외의 매개변수 이름을 JSON 키로 되돌려 준다. 그 이름 규약이 22개 전부에서 성립하는지 고정한다.
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

            Assert.AreEqual(22, checkedCount);
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
                @"[ { ""enemyIds"": [101], ""approachFu"": 1 }, { ""enemyIds"": [201], ""approachFu"": 1 } ]",
                @"[ { ""enemyIds"": [101], ""approachFu"": 1 }, { ""enemyIds"": [202], ""approachFu"": 1 }, { ""enemyIds"": [203], ""approachFu"": 1 }, { ""enemyIds"": [204], ""approachFu"": 1 }, { ""enemyIds"": [201], ""approachFu"": 1 } ]");

            GameData data = GameDataLoader.Parse(TestJson.Files(characters: characters, stage: stage));

            Assert.AreEqual(5, data.Stage.Waves.Count);
        }

        [Test]
        public void MaxEnemiesInWave_IsAccepted()
        {
            string characters = TestJson.CharactersWithExtraEnemies(3);
            string stage = TestJson.Swap(TestJson.Stage,
                @"[ { ""enemyIds"": [101], ""approachFu"": 1 }, { ""enemyIds"": [201], ""approachFu"": 1 } ]",
                @"[ { ""enemyIds"": [101, 201, 202, 203, 204], ""approachFu"": 1 } ]");

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
                @"[ { ""enemyIds"": [101], ""approachFu"": 1 }, { ""enemyIds"": [201], ""approachFu"": 1 } ]",
                @"[ { ""enemyIds"": [101], ""approachFu"": 1 }, { ""enemyIds"": [201], ""approachFu"": 1 }, { ""enemyIds"": [101], ""approachFu"": 1 }, { ""enemyIds"": [101], ""approachFu"": 1 }, { ""enemyIds"": [101], ""approachFu"": 1 }, { ""enemyIds"": [101], ""approachFu"": 1 } ]");

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
        public void MissingMovementBlock_NamesTheFirstFieldInIt()
        {
            // JsonUtility 는 빠진 객체를 초기값이 채워진 새 인스턴스로 만든다 — 그래서 누락이 블록 안 필드로 잡힌다.
            string json = TestJson.Swap(TestJson.Balance, @"""movement"": {", @"""unusedBlock"": {");

            var ex = Fails(TestJson.Files(balance: json));

            AssertLocation(ex, DataFiles.Balance, "movement.marchSpeedFu");
        }

        [Test]
        public void MissingMovementField_NamesFieldWithBlockPrefix()
        {
            string json = TestJson.Swap(TestJson.Balance, @"""frontSlotOffsetFu"": 0,", "");

            var ex = Fails(TestJson.Files(balance: json));

            AssertLocation(ex, DataFiles.Balance, "movement.frontSlotOffsetFu");
        }

        [Test]
        public void MovementBreakingTheDeadlockInvariant_IsRejected()
        {
            // 범위는 하나하나 맞지만 조합이 교착이다. 이 검증이 없으면 뒷열이 영원히 적에 닿지 못한 채 전투가 돈다.
            string json = TestJson.Swap(TestJson.Balance, @"""backReachFu"": 20000", @"""backReachFu"": 10");
            json = TestJson.Swap(json, @"""frontSlotOffsetFu"": 0", @"""frontSlotOffsetFu"": 100");

            var ex = Fails(TestJson.Files(balance: json));

            AssertLocation(ex, DataFiles.Balance, "movement.backReachFu");
        }

        [Test]
        public void EveryMovementKey_MatchesLaneConfigParameterName()
        {
            // 로더는 LaneConfig 생성자 예외의 매개변수 이름에 블록 이름을 붙여 필드 경로로 되돌린다. 그 규약을 10개 전부에서 고정한다.
            var ctor = typeof(LaneConfig).GetConstructors()[0];
            int checkedCount = 0;

            foreach (var parameter in ctor.GetParameters())
            {
                string key = "\"" + parameter.Name + "\": ";
                StringAssert.Contains(key, TestJson.Balance, $"픽스처에 생성자 매개변수 {parameter.Name} 과 같은 키가 없습니다.");

                string json = System.Text.RegularExpressions.Regex.Replace(
                    TestJson.Balance, System.Text.RegularExpressions.Regex.Escape(key) + @"\d+", key + int.MaxValue);
                var ex = Fails(TestJson.Files(balance: json));
                AssertLocation(ex, DataFiles.Balance, "movement." + parameter.Name);
                checkedCount++;
            }

            Assert.AreEqual(10, checkedCount);
        }

        [Test]
        public void MissingSkillChance_IsRejected()
        {
            // 픽스처에서 유일한 값(1500)인 두 번째 캐릭터의 필드를 지운다.
            string json = TestJson.Swap(TestJson.Characters, @"""skillChance"": 1500,", "");

            var ex = Fails(TestJson.Files(characters: json));

            AssertLocation(ex, DataFiles.Characters, "characters[1].skillChance");
        }

        [Test]
        public void SkillChanceOutsideTheDesignBand_IsRejected()
        {
            string tooHigh = TestJson.Swap(TestJson.Characters, @"""skillChance"": 1500,", @"""skillChance"": 4000,");
            string tooLow = TestJson.Swap(TestJson.Characters, @"""skillChance"": 1500,", @"""skillChance"": 99,");

            AssertLocation(Fails(TestJson.Files(characters: tooHigh)), DataFiles.Characters, "characters[1].skillChance");
            AssertLocation(Fails(TestJson.Files(characters: tooLow)), DataFiles.Characters, "characters[1].skillChance");
        }

        [Test]
        public void MissingPreference_IsRejected()
        {
            // 문자열이라 빠지면 null 이 되어 걸러진다 — 열거형 정수였다면 0(첫 값)으로 조용히 통과했을 것이다.
            string json = TestJson.Swap(TestJson.Characters, @"""skillChance"": 1500, ""preference"": ""None"",", @"""skillChance"": 1500,");

            var ex = Fails(TestJson.Files(characters: json));

            AssertLocation(ex, DataFiles.Characters, "characters[1].preference");
        }

        [Test]
        public void UnknownPreference_IsRejectedWithTheAllowedNames()
        {
            string json = TestJson.Swap(TestJson.Characters, @"""skillChance"": 1500, ""preference"": ""None"",", @"""skillChance"": 1500, ""preference"": ""Berserker"",");

            var ex = Fails(TestJson.Files(characters: json));

            AssertLocation(ex, DataFiles.Characters, "characters[1].preference");
            StringAssert.Contains("Guardian", ex.Message);
        }

        [TestCase("None")]
        [TestCase("Guardian")]
        [TestCase("Charger")]
        [TestCase("Rearguard")]
        [TestCase("Coward")]
        public void EveryPreferenceName_IsAccepted(string name)
        {
            string json = TestJson.Swap(TestJson.Characters, @"""skillChance"": 1500, ""preference"": ""None"",", @"""skillChance"": 1500, ""preference"": """ + name + @""",");

            GameData data = GameDataLoader.Parse(TestJson.Files(characters: json));

            Assert.AreEqual(name, data.FindCharacter(2).Preference.ToString());
        }

        [Test]
        public void MissingFormationChance_IsRejected()
        {
            string json = TestJson.Swap(TestJson.Characters, @"""skillChance"": 1500, ""preference"": ""None"", ""formationChance"": 0,", @"""skillChance"": 1500, ""preference"": ""None"",");

            var ex = Fails(TestJson.Files(characters: json));

            AssertLocation(ex, DataFiles.Characters, "characters[1].formationChance");
        }

        [TestCase(10001)]
        [TestCase(-5)]
        public void FormationChanceOutOfRange_IsRejected(int value)
        {
            // -5 는 "필드 없음" 표시(-1)와 다르므로 범위 오류로 걸린다. Swap 은 조각이 없으면 먼저 실패해 이 테스트가 조용히 건너뛰지 않는다.
            string json = TestJson.Swap(TestJson.Characters,
                @"""skillChance"": 1500, ""preference"": ""None"", ""formationChance"": 0,",
                @"""skillChance"": 1500, ""preference"": ""None"", ""formationChance"": " + value + ",");

            var ex = Fails(TestJson.Files(characters: json));

            AssertLocation(ex, DataFiles.Characters, "characters[1].formationChance");
        }

        [Test]
        public void FormationChanceBounds_AreAccepted()
        {
            string full = TestJson.Swap(TestJson.Characters,
                @"""skillChance"": 1500, ""preference"": ""None"", ""formationChance"": 0,",
                @"""skillChance"": 1500, ""preference"": ""None"", ""formationChance"": 10000,");

            Assert.AreEqual(0, GameDataLoader.Parse(TestJson.Files()).FindCharacter(2).FormationChance);
            Assert.AreEqual(10000, GameDataLoader.Parse(TestJson.Files(characters: full)).FindCharacter(2).FormationChance);
        }

        [Test]
        public void MissingApproachFu_IsRejected()
        {
            string json = TestJson.Swap(TestJson.Stage, @"{ ""enemyIds"": [101], ""approachFu"": 1 }", @"{ ""enemyIds"": [101] }");

            var ex = Fails(TestJson.Files(stage: json));

            AssertLocation(ex, DataFiles.Stage, "waves[0].approachFu");
        }

        [Test]
        public void BossBeforeTheLastEncounter_IsRejected()
        {
            // 보스가 중간에 서면 뒤 조우가 보스보다 약해져 전장의 절정이 사라진다.
            string json = TestJson.Swap(TestJson.Stage,
                @"{ ""enemyIds"": [101], ""approachFu"": 1 }, { ""enemyIds"": [201], ""approachFu"": 1 }",
                @"{ ""enemyIds"": [201], ""approachFu"": 1 }, { ""enemyIds"": [101], ""approachFu"": 1 }");

            var ex = Fails(TestJson.Files(stage: json));

            AssertLocation(ex, DataFiles.Stage, "waves[0].enemyIds[0]");
        }

        [Test]
        public void ApproachLongerThanTheTimeLimitAllows_IsRejected()
        {
            // 두 파일을 함께 봐야 알 수 있는 조건이다 — 행군 속도는 밸런스, 접근 거리와 제한 시간은 스테이지에 있다.
            string json = TestJson.Swap(TestJson.Stage,
                @"{ ""enemyIds"": [101], ""approachFu"": 1 }", @"{ ""enemyIds"": [101], ""approachFu"": 700 }");

            var ex = Fails(TestJson.Files(stage: json));

            AssertLocation(ex, DataFiles.Stage, "waves[].approachFu");
        }

        [Test]
        public void SkillChanceAtBandEdges_IsAccepted()
        {
            string low = TestJson.Swap(TestJson.Characters, @"""skillChance"": 1500,", @"""skillChance"": 100,");
            string high = TestJson.Swap(TestJson.Characters, @"""skillChance"": 1500,", @"""skillChance"": 3900,");

            Assert.AreEqual(100, GameDataLoader.Parse(TestJson.Files(characters: low)).FindCharacter(2).SkillChance);
            Assert.AreEqual(3900, GameDataLoader.Parse(TestJson.Files(characters: high)).FindCharacter(2).SkillChance);
        }

        [Test]
        public void ApproachFuBelowOne_IsRejected()
        {
            string json = TestJson.Swap(TestJson.Stage, @"""approachFu"": 1 }, { ""enemyIds"": [201]", @"""approachFu"": 0 }, { ""enemyIds"": [201]");

            var ex = Fails(TestJson.Files(stage: json));

            AssertLocation(ex, DataFiles.Stage, "waves[0].approachFu");
        }

        [Test]
        public void ApproachFuAtItsMaximum_IsAccepted()
        {
            // 상한까지 행군할 수 있어야 하므로 행군 속도도 함께 올린다 — 시간 예산 검증과 엮이는 값이다.
            string balance = TestJson.Swap(TestJson.Balance, @"""marchSpeedFu"": 1,", @"""marchSpeedFu"": 1000,");
            string stage = TestJson.Swap(TestJson.Stage,
                @"""approachFu"": 1 }, { ""enemyIds"": [201]", @"""approachFu"": 20000 }, { ""enemyIds"": [201]");

            GameData data = GameDataLoader.Parse(TestJson.Files(balance: balance, stage: stage));

            Assert.AreEqual(BattleSetup.MaxApproachFu, data.Stage.WaveApproachFu[0]);
        }

        [Test]
        public void ApproachFuAboveItsMaximum_IsRejected()
        {
            string balance = TestJson.Swap(TestJson.Balance, @"""marchSpeedFu"": 1,", @"""marchSpeedFu"": 1000,");
            string stage = TestJson.Swap(TestJson.Stage,
                @"""approachFu"": 1 }, { ""enemyIds"": [201]", @"""approachFu"": 20001 }, { ""enemyIds"": [201]");

            var ex = Fails(TestJson.Files(balance: balance, stage: stage));

            AssertLocation(ex, DataFiles.Stage, "waves[0].approachFu");
        }

        [Test]
        public void ApproachExactlyMatchingTheTimeBudget_IsAccepted()
        {
            // 행군 속도 1 × 제한 시간 600틱 = 600fu 가 예산이다. 합이 정확히 600 이면 통과해야 한다.
            string json = TestJson.Swap(TestJson.Stage,
                @"""approachFu"": 1 }, { ""enemyIds"": [201]", @"""approachFu"": 599 }, { ""enemyIds"": [201]");

            GameData data = GameDataLoader.Parse(TestJson.Files(stage: json));

            CollectionAssert.AreEqual(new[] { 599, 1 }, data.Stage.WaveApproachFu);
        }

        [Test]
        public void Swap_RejectsMissingOrRepeatedFragment()
        {
            Assert.Throws<System.InvalidOperationException>(() => TestJson.Swap("ab", "x", "y"));
            Assert.Throws<System.InvalidOperationException>(() => TestJson.Swap("aa", "a", "b"));
        }

        // ---- 소모품·구조 시간(계획서 Phase D) ----

        [Test]
        public void Consumables_AreLoaded_AndGoIntoTheBattleSetup()
        {
            GameData data = GameDataLoader.Parse(TestJson.Files());

            Assert.AreEqual(1, data.Consumables.Count);
            Assert.AreEqual(1, data.Consumables[0].Id);
            Assert.AreEqual("회복약", data.Consumables[0].Name);
            ConsumableSetup inBattle = data.CreateBattleSetup().Consumables[0];
            Assert.AreEqual(1, inBattle.Id);
            Assert.AreEqual(100, inBattle.HealAmount);
            Assert.AreEqual(2, inBattle.Count);
        }

        [Test]
        public void NoConsumables_IsAllowed_ButTheArrayMustBePresent()
        {
            GameData empty = GameDataLoader.Parse(TestJson.Files(consumables: @"{ ""consumables"": [] }"));
            var missing = Fails(TestJson.Files(consumables: "{ }"));

            Assert.IsEmpty(empty.Consumables);
            AssertLocation(missing, DataFiles.Consumables, "consumables");
        }

        [TestCase(@"""id"": 1, ", "id")]
        [TestCase(@"""name"": ""회복약"", ", "name")]
        [TestCase(@"""healAmount"": 100, ", "healAmount")]
        [TestCase(@", ""count"": 2", "count")]
        public void ConsumableMissingAField_IsRejected(string piece, string field)
        {
            var ex = Fails(TestJson.Files(consumables: TestJson.Swap(TestJson.Consumables, piece, "")));

            AssertLocation(ex, DataFiles.Consumables, "consumables[0]." + field);
        }

        [TestCase(@"""healAmount"": 100", @"""healAmount"": 0", "healAmount")]
        [TestCase(@"""count"": 2", @"""count"": 0", "count")]
        [TestCase(@"""count"": 2", @"""count"": 100", "count")]
        public void ConsumableValueOutOfRange_IsRejected(string from, string to, string field)
        {
            var ex = Fails(TestJson.Files(consumables: TestJson.Swap(TestJson.Consumables, from, to)));

            AssertLocation(ex, DataFiles.Consumables, "consumables[0]." + field);
        }

        [Test]
        public void ConsumableCountAtItsMaximum_IsAccepted()
        {
            string json = TestJson.Swap(TestJson.Consumables, @"""count"": 2", @"""count"": 99");

            Assert.AreEqual(99, GameDataLoader.Parse(TestJson.Files(consumables: json)).Consumables[0].Setup.Count);
        }

        [Test]
        public void DuplicateConsumableId_IsRejected()
        {
            const string item = @"{ ""id"": 1, ""name"": ""회복약"", ""healAmount"": 100, ""count"": 2 }";
            var ex = Fails(TestJson.Files(consumables: TestJson.Swap(TestJson.Consumables, item, item + ", " + item)));

            AssertLocation(ex, DataFiles.Consumables, "consumables[1].id");
        }

        private static string ConsumablesOfKinds(int kinds)
        {
            var items = new System.Text.StringBuilder();
            for (int i = 0; i < kinds; i++)
            {
                if (i > 0) items.Append(", ");
                items.Append(@"{ ""id"": " + i + @", ""name"": ""약"", ""healAmount"": 10, ""count"": 1 }");
            }

            return @"{ ""consumables"": [" + items + "] }";
        }

        [Test]
        public void MoreConsumableKindsThanTheLimit_AreRejected_AndExactlyTheLimitIsAccepted()
        {
            var ex = Fails(TestJson.Files(consumables: ConsumablesOfKinds(BattleSetup.MaxConsumableKinds + 1)));
            GameData exact = GameDataLoader.Parse(TestJson.Files(consumables: ConsumablesOfKinds(BattleSetup.MaxConsumableKinds)));

            AssertLocation(ex, DataFiles.Consumables, "consumables");
            Assert.AreEqual(BattleSetup.MaxConsumableKinds, exact.CreateBattleSetup().Consumables.Count);
        }

        [Test]
        public void RescueLongerThanTheBattle_IsRejected_AndExactlyTheTimeLimitIsAccepted()
        {
            // 제한 시간은 60초 = 600틱이다. 구조 한 번이 그보다 길면 끝나지 못하고 진형이 묶인다.
            var ex = Fails(TestJson.Files(balance: TestJson.Swap(TestJson.Balance, @"""rescueTicks"": 50", @"""rescueTicks"": 601")));
            GameData exact = GameDataLoader.Parse(TestJson.Files(balance: TestJson.Swap(TestJson.Balance, @"""rescueTicks"": 50", @"""rescueTicks"": 600")));

            AssertLocation(ex, DataFiles.Balance, "rescueTicks");
            Assert.AreEqual(600, exact.Balance.RescueTicks);
        }

        [Test]
        public void MissingRescueTicks_IsRejected()
        {
            var ex = Fails(TestJson.Files(balance: TestJson.Swap(TestJson.Balance, @" ""rescueTicks"": 50,", "")));

            AssertLocation(ex, DataFiles.Balance, "rescueTicks");
        }
    }
}
