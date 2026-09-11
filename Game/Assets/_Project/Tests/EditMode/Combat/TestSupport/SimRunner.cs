using System.Collections.Generic;

namespace TAoB.Combat.Tests
{
    /// <summary>틱 실행과 이벤트·스냅샷 조회를 돕는 테스트 도구.</summary>
    internal static class SimRunner
    {
        public static List<BattleEvent> Run(BattleSimulation simulation, int ticks)
        {
            var all = new List<BattleEvent>();
            var buffer = new List<BattleEvent>();
            for (int i = 0; i < ticks; i++)
            {
                simulation.Step(buffer);
                all.AddRange(buffer);
            }

            return all;
        }

        public static List<BattleEvent> OfKind(List<BattleEvent> events, BattleEventKind kind)
        {
            return events.FindAll(e => e.Kind == kind);
        }

        public static List<BattleEvent> ActionsBy(List<BattleEvent> events, int unitId)
        {
            return events.FindAll(e => e.Kind == BattleEventKind.ActionStarted && e.SourceId == unitId);
        }

        public static UnitSnapshot Unit(BattleSimulation simulation, int unitId)
        {
            BattleSnapshot snapshot = simulation.CreateSnapshotBuffer();
            simulation.FillSnapshot(snapshot);
            foreach (UnitSnapshot unit in snapshot.Units)
            {
                if (unit.Id == unitId) return unit;
            }

            throw new KeyNotFoundException($"유닛 {unitId} 가 스냅샷에 없습니다.");
        }

        public static void AddEvent(StateHasher hasher, BattleEvent e)
        {
            hasher.Add((int)e.Kind);
            hasher.Add(e.Tick);
            hasher.Add(e.SourceId);
            hasher.Add(e.TargetId);
            hasher.Add(e.Value);
            hasher.Add(e.Detail);
        }
    }
}
