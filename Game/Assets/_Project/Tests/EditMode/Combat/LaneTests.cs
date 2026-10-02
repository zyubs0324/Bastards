using NUnit.Framework;

namespace TAoB.Combat.Tests
{
    public class LaneTests
    {
        [Test]
        public void Facing_IsForwardForAlly_AndOppositeForEnemy()
        {
            Assert.AreEqual(1, Lane.Facing(Side.Ally));
            Assert.AreEqual(-1, Lane.Facing(Side.Enemy));
        }

        [Test]
        public void Distance_IsAbsolute_AndOrderIndependent()
        {
            Assert.AreEqual(30, Lane.Distance(100, 130));
            Assert.AreEqual(30, Lane.Distance(130, 100));
            Assert.AreEqual(0, Lane.Distance(-50, -50));
            Assert.AreEqual(80, Lane.Distance(-30, 50));
        }

        [Test]
        public void Distance_DoesNotOverflow_AtIntBounds()
        {
            // int 뺄셈이면 음수로 돌아버리는 조합. long 으로 계산해 상한으로 잘려야 한다.
            Assert.AreEqual(int.MaxValue, Lane.Distance(int.MaxValue, int.MinValue));
            Assert.AreEqual(int.MaxValue, Lane.Distance(int.MinValue, int.MaxValue));
        }

        [Test]
        public void StepToward_MovesOneStepTowardTarget()
        {
            Assert.AreEqual(110, Lane.StepToward(100, 300, 10));
        }

        [Test]
        public void StepToward_MovesBackward_WhenTargetIsBehind()
        {
            Assert.AreEqual(90, Lane.StepToward(100, -300, 10));
        }

        [Test]
        public void StepToward_StopsOnTarget_WhenRemainingIsSmallerThanStep()
        {
            // 남은 거리보다 크게 가면 목표를 넘었다가 되돌아오며 ±step 으로 떨린다.
            Assert.AreEqual(300, Lane.StepToward(295, 300, 10));
            Assert.AreEqual(300, Lane.StepToward(305, 300, 10));
            Assert.AreEqual(300, Lane.StepToward(300, 300, 10));
        }

        [Test]
        public void StepToward_DoesNotMove_WhenStepIsZeroOrNegative()
        {
            Assert.AreEqual(100, Lane.StepToward(100, 300, 0));
            Assert.AreEqual(100, Lane.StepToward(100, 300, -5));
        }
    }
}
