using System;

namespace TAoB.Data.Tests
{
    /// <summary>
    /// 검증 테스트용 최소 데이터. 각 테스트는 여기서 한 조각을 <see cref="Swap"/> 으로 바꿔 깨진 파일을 만든다.
    /// 실제 파일과는 별개라 실제 파일이 바뀌어도 검증 테스트는 흔들리지 않는다.
    /// </summary>
    internal static class TestJson
    {
        public const string Balance = @"{
  ""defenseK"": 100,
  ""weakAffinityPermille"": 750, ""normalAffinityPermille"": 1000, ""strongAffinityPermille"": 1250,
  ""frontDamageTakenPermille"": 1000, ""backDamageTakenPermille"": 800,
  ""critBaseChance"": 500, ""critChancePerSpd"": 20, ""critMultiplierPermille"": 1500,
  ""minimumDamage"": 1,
  ""resSelfDamagePerShortfall"": 2, ""equipBlockResShortfall"": 20,
  ""mndPenaltyPerShortfallPermille"": 25, ""minMndEffectPermille"": 200,
  ""actionGaugeMax"": 1000, ""scatterHpLossPerTick"": 5, ""rescueExtraGauge"": 200,
  ""overloadPowerPermille"": 1500, ""runawayDamagePermille"": 1000, ""runawaySelfDamagePermille"": 200,
  ""breakthroughMndGain"": 10,
  ""overloadTiers"": [
    { ""maxExcessPermille"": 1000, ""successChance"": 4000, ""scatterChance"": 3000, ""runawayChance"": 2000, ""breakthroughChance"": 1000 },
    { ""successChance"": 1000, ""scatterChance"": 4500, ""runawayChance"": 4400, ""breakthroughChance"": 100 }
  ],
  ""movement"": {
    ""marchSpeedFu"": 1, ""frontSlotOffsetFu"": 0, ""backSlotOffsetFu"": 0,
    ""frontReachFu"": 20000, ""backReachFu"": 20000, ""engageMarginFu"": 0,
    ""formationDriftFu"": 0, ""chargeDriftFu"": 0, ""stepBackFu"": 0, ""flankChance"": 0
  },
  ""affinities"": [
    { ""attacker"": ""Knight"", ""defender"": ""Perceptive"", ""level"": ""Strong"" }
  ]
}";

        public const string Equipment = @"{
  ""catalysts"": [ { ""id"": ""cat"", ""name"": ""원석"", ""power"": 80 } ],
  ""magitools"": [ { ""id"": ""tool"", ""name"": ""마도구"", ""atkBonus"": 10, ""requiredRes"": 10, ""requiredMnd"": 10, ""maxDurability"": 20, ""durabilityCostPerUse"": 1 } ]
}";

        public const string Characters = @"{
  ""characters"": [
    { ""id"": 1, ""name"": ""기사"", ""side"": ""Ally"", ""job"": ""Knight"", ""row"": ""Front"", ""skillChance"": 2000, ""preference"": ""None"", ""formationChance"": 0,
      ""stats"": { ""atk"": 40, ""def"": 5, ""hp"": 300, ""spd"": 100, ""mnd"": 20, ""res"": 30 },
      ""skill"": { ""powerPermille"": 1500, ""targetRule"": ""FrontFirst"", ""cooldownTicks"": 50 },
      ""magitoolId"": ""tool"" },
    { ""id"": 2, ""name"": ""마법사"", ""side"": ""Ally"", ""job"": ""Mage"", ""row"": ""Back"", ""skillChance"": 1500, ""preference"": ""None"", ""formationChance"": 0,
      ""stats"": { ""atk"": 50, ""def"": 5, ""hp"": 200, ""spd"": 100, ""mnd"": 60, ""res"": 10 },
      ""skill"": { ""powerPermille"": 1500, ""targetRule"": ""All"", ""canOverload"": true, ""cooldownTicks"": 50 },
      ""catalystId"": ""cat"" },
    { ""id"": 101, ""name"": ""적"", ""side"": ""Enemy"", ""job"": ""Perceptive"", ""row"": ""Front"", ""skillChance"": 2000, ""preference"": ""None"", ""formationChance"": 0,
      ""stats"": { ""atk"": 30, ""def"": 5, ""hp"": 200, ""spd"": 100, ""mnd"": 10, ""res"": 10 },
      ""skill"": { ""powerPermille"": 1500, ""targetRule"": ""FrontFirst"", ""cooldownTicks"": 50 } },
    { ""id"": 201, ""name"": ""보스"", ""side"": ""Enemy"", ""job"": ""Knight"", ""row"": ""Front"", ""isBoss"": true, ""skillChance"": 2000, ""preference"": ""None"", ""formationChance"": 0,
      ""stats"": { ""atk"": 40, ""def"": 10, ""hp"": 500, ""spd"": 100, ""mnd"": 10, ""res"": 10 },
      ""skill"": { ""powerPermille"": 1500, ""targetRule"": ""LowestHp"", ""cooldownTicks"": 50 } }
  ]
}";

        public const string Stage = @"{
  ""timeLimitSeconds"": 60,
  ""precondition"": ""None"",
  ""allyIds"": [1, 2],
  ""waves"": [ { ""enemyIds"": [101], ""approachFu"": 1 }, { ""enemyIds"": [201], ""approachFu"": 1 } ]
}";

        /// <summary>적 캐릭터를 <paramref name="count"/>명 더 붙인 characters.json — ID 는 202 부터. 편성 상한 테스트용.</summary>
        public static string CharactersWithExtraEnemies(int count)
        {
            var extra = new System.Text.StringBuilder();
            for (int i = 0; i < count; i++)
            {
                int id = 202 + i;
                extra.Append(@",
    { ""id"": " + id + @", ""name"": ""적" + id + @""", ""side"": ""Enemy"", ""job"": ""Knight"", ""row"": ""Front"",
      ""stats"": { ""atk"": 30, ""def"": 5, ""hp"": 200, ""spd"": 100, ""mnd"": 10, ""res"": 10 }, ""skillChance"": 2000, ""preference"": ""None"", ""formationChance"": 0,
      ""skill"": { ""powerPermille"": 1500, ""targetRule"": ""FrontFirst"", ""cooldownTicks"": 50 } }");
            }

            return Swap(Characters, @"""LowestHp"", ""cooldownTicks"": 50 } }", @"""LowestHp"", ""cooldownTicks"": 50 } }" + extra);
        }

        public static GameDataFiles Files(string balance = Balance, string equipment = Equipment, string characters = Characters, string stage = Stage)
        {
            return new GameDataFiles(balance, equipment, characters, stage);
        }

        /// <summary>정확히 한 번 나오는 조각을 바꾼다. 없으면 테스트가 잘못된 것이므로 실패시킨다.</summary>
        public static string Swap(string json, string from, string to)
        {
            int first = json.IndexOf(from, StringComparison.Ordinal);
            if (first < 0) throw new InvalidOperationException($"픽스처에 '{from}' 이(가) 없습니다.");
            if (json.IndexOf(from, first + 1, StringComparison.Ordinal) >= 0) throw new InvalidOperationException($"픽스처에 '{from}' 이(가) 여러 번 있습니다.");
            return json.Substring(0, first) + to + json.Substring(first + from.Length);
        }
    }
}
