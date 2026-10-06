using System.Collections.Generic;
using NUnit.Framework;
using TAoB.Combat;

namespace TAoB.Data.Tests
{
    /// <summary>
    /// 실제 데이터 파일의 이동·편성 값으로 한 판을 끝까지 돌려 **교착**을 본다(계획서 Phase B).
    /// 승·패는 가리지 않고 제한 시간 만료(DefeatTimeLimit)만 배제한다 — 난이도는 Phase E 에서 계속 바뀌지만
    /// "아무도 사거리에 닿지 못해 제한 시간까지 행군만 한다"는 데이터가 어떻게 바뀌어도 틀린 구성이다.
    /// 수치는 테스트에 복제하지 않고 파일에서 읽고, 시드는 여러 개를 돌려 한 시드의 우연에 기대지 않는다.
    /// </summary>
    public class RealStageFlowTests
    {
        private static BattleSimulation Create(GameData data, uint seed)
        {
            return new BattleSimulation(data.CreateBattleSetup(), new SeededRandom(seed));
        }

        [TestCase(1u)]
        [TestCase(2u)]
        [TestCase(3u)]
        [TestCase(4u)]
        [TestCase(5u)]
        public void RealStage_DoesNotStallUntilTheTimeLimit(uint seed)
        {
            GameData data = GameDataLoader.LoadFromResources();
            BattleSimulation sim = Create(data, seed);
            var events = new List<BattleEvent>();

            for (int i = 0; i < data.Stage.TimeLimitTicks && sim.Outcome == BattleOutcome.InProgress; i++)
            {
                sim.Step(events);
            }

            Assert.AreNotEqual(BattleOutcome.DefeatTimeLimit, sim.Outcome,
                $"시드 {seed}: 제한 시간 안에 결판이 나지 않았다. 도달한 조우 {sim.CurrentWave + 1}/{sim.WaveCount}, 기준점 {sim.AnchorX}");
        }

        [Test]
        public void RealStage_MarchesBeforeTheFirstEncounter_AndAgainBeforeEachNext()
        {
            // 조우 사이에 기준점이 전진하지 않으면 "이동-전투" 흐름이 아니라 제자리 대치다.
            GameData data = GameDataLoader.LoadFromResources();
            BattleSimulation sim = Create(data, 1u);
            var events = new List<BattleEvent>();
            var anchorAtWaveStart = new List<int>();
            int previousAnchor = sim.AnchorX;
            int firstStopTick = -1;

            for (int i = 0; i < data.Stage.TimeLimitTicks && sim.Outcome == BattleOutcome.InProgress; i++)
            {
                sim.Step(events);
                if (firstStopTick < 0 && sim.AnchorX == previousAnchor) firstStopTick = sim.Tick;
                previousAnchor = sim.AnchorX;
                for (int j = 0; j < events.Count; j++)
                {
                    if (events[j].Kind == BattleEventKind.WaveStarted) anchorAtWaveStart.Add(sim.AnchorX);
                }
            }

            // 첫 교전은 접근 거리 ÷ 행군 속도 틱의 **상한**이다 — 사거리 안에 들면 그보다 먼저 선다.
            int upperBound = data.Stage.WaveApproachFu[0] / data.Balance.Lane.MarchSpeedFu;
            Assert.Greater(firstStopTick, 1, "첫 조우까지는 행군해야 한다 — 1틱에 멈추면 적이 코앞에서 시작한 것이다.");
            Assert.LessOrEqual(firstStopTick, upperBound, "접근 거리 안에서 첫 교전이 시작돼야 한다.");

            for (int i = 1; i < anchorAtWaveStart.Count; i++)
            {
                Assert.Greater(anchorAtWaveStart[i], anchorAtWaveStart[i - 1], $"{i}번째 조우 시작 기준점이 이전보다 앞이어야 한다.");
            }
        }
    }
}
