namespace TAoB.Combat
{
    /// <summary>
    /// 조우가 시작될 때 유닛마다 자세와 침투 여부를 한 번 정한다(계획서 Phase C).
    /// 판정은 전부 <see cref="Chance.Roll"/> 을 쓰므로 확률이 0 이나 최대면 난수를 쓰지 않고,
    /// 선호가 없는 유닛은 규율 롤도 건너뛴다. 순서는 ID 오름차순이라 같은 시드면 항상 같은 결정이 나온다.
    /// </summary>
    internal static class Tactics
    {
        /// <summary>
        /// 이번 조우에 참여하는 유닛(아군 전원 + 이 조우의 적)을 다시 정한다. 아군은 조우마다 다시 뽑는다.
        /// </summary>
        /// <param name="unitsById">ID 오름차순 전체 유닛.</param>
        public static void DecideEncounter(UnitState[] unitsById, int wave, LaneConfig lane, IRandom random)
        {
            for (int i = 0; i < unitsById.Length; i++)
            {
                UnitState unit = unitsById[i];
                bool takesPart = unit.WaveIndex == wave || unit.WaveIndex == UnitState.AllyWave;
                if (!takesPart || unit.Condition == UnitCondition.Downed) continue;

                Decide(unit, lane, random);
            }
        }

        private static void Decide(UnitState unit, LaneConfig lane, IRandom random)
        {
            // 진형유지 값을 통과하면 성향대로, 못 하면 흐트러져 자리를 지킨다(기본 행동). 성향이 없으면 굴릴 이유가 없다.
            bool followsPreference = unit.Setup.Preference != Preference.None && Chance.Roll(random, unit.Setup.FormationChance);
            unit.Stance = followsPreference ? Stances.FromPreference(unit.Setup.Preference) : Stance.Hold;
            unit.IsFlanking = false;

            // 측면 침투: 적 앞열이 유닛별로 굴린다. 인원수에 비율을 곱해 절삭하면 2명 × 30% 가 0 명이 되기 때문이다.
            if (unit.Side == Side.Enemy && unit.Row == Row.Front && Chance.Roll(random, lane.FlankChance))
            {
                unit.IsFlanking = true;
                unit.Stance = Stance.Charge;
            }
        }
    }
}
