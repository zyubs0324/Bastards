using NUnit.Framework;

namespace TAoB.View.Tests
{
    /// <summary>틱 드라이버는 표시 계층에서 유일하게 규칙 실행 횟수를 정하므로 자동 테스트로 고정한다(계획서 Phase 4 검증 7 의 근거).</summary>
    public class TickDriverTests
    {
        [Test]
        public void OneTickPerTenthSecond_AtSpeedOne()
        {
            var driver = new TickDriver(10);

            Assert.AreEqual(0, driver.Advance(0.05f, 1));
            Assert.AreEqual(1, driver.Advance(0.05f, 1));
            Assert.AreEqual(3, driver.Advance(0.3f, 1));
        }

        [Test]
        public void SpeedTwo_DoublesTicks_NotTickLength()
        {
            var driver = new TickDriver(10);

            Assert.AreEqual(2, driver.Advance(0.1f, 2));
        }

        [Test]
        public void CapsTicksPerFrame_AndCarriesRemainder()
        {
            var driver = new TickDriver(10);

            Assert.AreEqual(TickDriver.MaxTicksPerFrame, driver.Advance(5f, 1), "50틱 몫 중 20틱만");
            Assert.AreEqual(TickDriver.MaxTicksPerFrame, driver.Advance(0f, 1), "남은 30틱 중 20틱");
            Assert.AreEqual(10, driver.Advance(0f, 1), "나머지 10틱");
            Assert.AreEqual(0, driver.Advance(0f, 1));
        }

        [Test]
        public void Alpha_IsFractionOfNextTick()
        {
            var driver = new TickDriver(10);

            driver.Advance(0.125f, 1);

            Assert.AreEqual(0.25f, driver.Alpha, 0.001f);
        }

        [Test]
        public void Reset_DropsAccumulatedTime()
        {
            var driver = new TickDriver(10);
            driver.Advance(0.09f, 1);

            driver.Reset();

            Assert.AreEqual(0, driver.Advance(0.05f, 1));
            Assert.AreEqual(0f, driver.Alpha, 0.5f);
        }

        [Test]
        public void NegativeDelta_IsIgnored()
        {
            var driver = new TickDriver(10);
            driver.Advance(0.05f, 1);

            Assert.AreEqual(0, driver.Advance(-1f, 1));
            Assert.AreEqual(1, driver.Advance(0.05f, 1));
        }

        /// <summary>짧은 멈춤이 틱 루프 중간에 걸려 일부만 실행했을 때, 나머지를 되돌리면 다음 프레임에 이어진다(계획서 Phase 4.5).</summary>
        [Test]
        public void RestoreTicks_CarriesUnexecutedTicksToNextFrame()
        {
            var driver = new TickDriver(10);

            Assert.AreEqual(3, driver.Advance(0.3f, 1), "3틱 요청");
            driver.RestoreTicks(2); // 1틱만 실행했다고 가정하고 나머지 2틱을 되돌린다.

            Assert.AreEqual(2, driver.Advance(0f, 1), "되돌린 2틱이 이어서 나온다");
            Assert.AreEqual(0, driver.Advance(0f, 1));
        }
    }
}
