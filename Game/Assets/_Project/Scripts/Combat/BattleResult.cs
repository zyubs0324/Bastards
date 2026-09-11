using System.Collections.Generic;

namespace TAoB.Combat
{
    public readonly struct MagitoolWear
    {
        public MagitoolWear(int unitId, int durabilityUsed)
        {
            UnitId = unitId;
            DurabilityUsed = durabilityUsed;
        }

        public int UnitId { get; }
        public int DurabilityUsed { get; }
    }

    /// <summary>전투 종료 화면과 기기 간 결과 비교에 쓰는 요약.</summary>
    public sealed class BattleResult
    {
        internal BattleResult(
            BattleOutcome outcome,
            int tick,
            IReadOnlyList<int> downedAllyIds,
            IReadOnlyList<MagitoolWear> magitoolWear,
            ulong stateHash)
        {
            Outcome = outcome;
            Tick = tick;
            DownedAllyIds = downedAllyIds;
            MagitoolWear = magitoolWear;
            StateHash = stateHash;
        }

        public BattleOutcome Outcome { get; }
        public int Tick { get; }

        /// <summary>흩어진 채로 끝난 아군은 종료 틱에 빈사로 확정되므로 여기에 포함된다.</summary>
        public IReadOnlyList<int> DownedAllyIds { get; }
        public IReadOnlyList<MagitoolWear> MagitoolWear { get; }

        /// <summary>최종 상태의 FNV-1a 64비트 해시(<see cref="StateHasher"/>).</summary>
        public ulong StateHash { get; }
    }
}
