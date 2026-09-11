namespace TAoB.Combat.Tests
{
    /// <summary>전투 시뮬레이션 테스트용 유닛·설정 생성기.</summary>
    internal static class TestUnits
    {
        public const int Unkillable = 1000000;

        /// <summary>게이지 1000 을 채우는 데 1000틱이 걸려 테스트 구간 안에서는 행동하지 않는다.</summary>
        public const int NeverActs = 1;

        /// <summary>크리티컬이 절대 나지 않고 진형 배율이 1.0 인 밸런스. 난수 소비를 과부하 판정으로만 한정한다.</summary>
        public static BalanceConfig Balance(
            int scatterHpLossPerTick = 5,
            int resSelfDamagePerShortfall = 2,
            int equipBlockResShortfall = 20,
            int critBaseChance = 0)
        {
            return TestBalance.Create(
                critBaseChance: critBaseChance,
                critChancePerSpd: 0,
                frontDamageTakenPermille: 1000,
                backDamageTakenPermille: 1000,
                scatterHpLossPerTick: scatterHpLossPerTick,
                resSelfDamagePerShortfall: resSelfDamagePerShortfall,
                equipBlockResShortfall: equipBlockResShortfall);
        }

        public static Stats MakeStats(int atk = 50, int def = 0, int hp = 1000, int spd = 100, int mnd = 100, int res = 100)
        {
            return new Stats(atk, def, hp, spd, mnd, res);
        }

        public static SkillDefinition Skill(
            int powerPermille = 1500,
            TargetRule targetRule = TargetRule.FrontFirst,
            bool canOverload = false,
            int cooldownTicks = 30)
        {
            return new SkillDefinition(powerPermille, targetRule, canOverload, cooldownTicks);
        }

        public static UnitSetup Ally(
            int id,
            Row row = Row.Front,
            Job job = Job.Knight,
            Stats? stats = null,
            SkillDefinition skill = null,
            Catalyst catalyst = null,
            Magitool magitool = null)
        {
            return new UnitSetup(id, Side.Ally, job, row, stats ?? MakeStats(), skill ?? Skill(), catalyst, magitool, isBoss: false);
        }

        public static UnitSetup Enemy(
            int id,
            Row row = Row.Front,
            Job job = Job.Knight,
            Stats? stats = null,
            SkillDefinition skill = null,
            Catalyst catalyst = null,
            bool isBoss = false)
        {
            return new UnitSetup(id, Side.Enemy, job, row, stats ?? MakeStats(hp: Unkillable, spd: NeverActs), skill ?? Skill(), catalyst, null, isBoss);
        }

        public static BattleSetup Setup(
            UnitSetup[] allies,
            UnitSetup[][] waves,
            BalanceConfig balance = null,
            int timeLimitTicks = BattleSetup.MaxTimeLimitTicks,
            PreconditionKind precondition = PreconditionKind.None,
            AffinityTable affinity = null)
        {
            return new BattleSetup(
                balance ?? Balance(),
                affinity ?? new AffinityTable(new AffinityEntry[0]),
                allies,
                waves,
                timeLimitTicks,
                precondition);
        }

        public static UnitSetup[][] OneWave(params UnitSetup[] enemies)
        {
            return new[] { enemies };
        }
    }
}
