namespace TAoB.Combat
{
    /// <summary>유닛 기본 스탯 6종(기획서 4-1).</summary>
    public readonly struct Stats
    {
        public Stats(int atk, int def, int hp, int spd, int mnd, int res)
        {
            Guard.Range(atk, 0, CombatScale.MaxValue, nameof(atk));
            Guard.Range(def, 0, CombatScale.MaxValue, nameof(def));
            Guard.Range(hp, 1, CombatScale.MaxValue, nameof(hp));
            Guard.Range(spd, 0, CombatScale.MaxValue, nameof(spd));
            Guard.Range(mnd, 0, CombatScale.MaxValue, nameof(mnd));
            Guard.Range(res, 0, CombatScale.MaxValue, nameof(res));

            Atk = atk;
            Def = def;
            Hp = hp;
            Spd = spd;
            Mnd = mnd;
            Res = res;
        }

        public int Atk { get; }
        public int Def { get; }
        public int Hp { get; }
        public int Spd { get; }

        /// <summary>마나 친화 — 재능. 원석 출력 상한과 마도구 효과를 정한다.</summary>
        public int Mnd { get; }

        /// <summary>반발력 내성 — 수련 축적. 마도구 반발을 버티는 정도.</summary>
        public int Res { get; }
    }
}
