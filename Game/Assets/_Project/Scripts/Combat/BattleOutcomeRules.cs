namespace TAoB.Combat
{
    public enum BattleOutcome
    {
        InProgress,
        Victory,
        DefeatRetreat,
        DefeatPrecondition,
        DefeatWipe,
        DefeatTimeLimit,
    }

    public readonly struct OutcomeInputs
    {
        public OutcomeInputs(bool retreatProcessed, bool preconditionBroken, bool finalWaveCleared, bool allAlliesDowned, bool timeLimitReached)
        {
            RetreatProcessed = retreatProcessed;
            PreconditionBroken = preconditionBroken;
            FinalWaveCleared = finalWaveCleared;
            AllAlliesDowned = allAlliesDowned;
            TimeLimitReached = timeLimitReached;
        }

        public bool RetreatProcessed { get; }
        public bool PreconditionBroken { get; }
        public bool FinalWaveCleared { get; }
        public bool AllAlliesDowned { get; }
        public bool TimeLimitReached { get; }
    }

    public static class BattleOutcomeRules
    {
        // 계획서 Phase 2 승패 표: 한 틱에 여러 조건이 성립하면 위에서부터 우선한다.
        public static BattleOutcome Evaluate(OutcomeInputs inputs)
        {
            if (inputs.RetreatProcessed) return BattleOutcome.DefeatRetreat;
            if (inputs.PreconditionBroken) return BattleOutcome.DefeatPrecondition;
            if (inputs.FinalWaveCleared) return BattleOutcome.Victory;
            if (inputs.AllAlliesDowned) return BattleOutcome.DefeatWipe;
            if (inputs.TimeLimitReached) return BattleOutcome.DefeatTimeLimit;
            return BattleOutcome.InProgress;
        }
    }
}
