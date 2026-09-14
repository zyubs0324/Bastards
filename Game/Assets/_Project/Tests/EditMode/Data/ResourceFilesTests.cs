using System.Collections.Generic;
using NUnit.Framework;
using TAoB.Combat;

namespace TAoB.Data.Tests
{
    /// <summary>
    /// Resources/Data 의 실제 파일이 로드되고, 그 편성으로 전투가 끝까지 돌아가는지 본다.
    /// 개수 같은 밸런스 사실은 단언하지 않는다 — 재미 판정 뒤 수치·편성이 바뀌어도 로더가 멀쩡하면 통과해야 한다.
    /// </summary>
    public class ResourceFilesTests
    {
        [Test]
        public void RealFiles_Load_AndEveryPlacedIdResolves()
        {
            GameData data = GameDataLoader.LoadFromResources();

            Assert.GreaterOrEqual(data.Stage.AllyIds.Count, 1);
            Assert.GreaterOrEqual(data.Stage.Waves.Count, 1);
            foreach (int id in data.Stage.AllyIds) Assert.IsNotNull(data.FindCharacter(id), $"아군 {id}");
            foreach (IReadOnlyList<int> wave in data.Stage.Waves)
            {
                foreach (int id in wave) Assert.IsNotNull(data.FindCharacter(id), $"적 {id}");
            }
        }

        [Test]
        public void RealFiles_BattleRunsToAnOutcome_WithoutInput()
        {
            BattleSetup setup = GameDataLoader.LoadFromResources().CreateBattleSetup();
            var sim = new BattleSimulation(setup, new SeededRandom(1));
            var events = new List<BattleEvent>();

            for (int i = 0; i < setup.TimeLimitTicks && sim.Outcome == BattleOutcome.InProgress; i++)
            {
                sim.Step(events);
            }

            Assert.AreNotEqual(BattleOutcome.InProgress, sim.Outcome);
        }
    }
}
