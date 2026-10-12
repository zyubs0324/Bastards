using NUnit.Framework;
using TAoB.Combat;

namespace TAoB.View.Tests
{
    /// <summary>강제 발동 가능 판정(계획서 Phase D). 기술 버튼과 몸의 빛남이 이 함수 하나를 쓴다.</summary>
    public class UnitReadinessTests
    {
        [Test]
        public void NormalAllyOnField_WithNoCooldown_CanForceItsSkill()
        {
            Assert.IsTrue(UnitReadiness.CanForceSkill(Snapshots.Unit(1)));
        }

        [Test]
        public void Cooldown_BlocksIt()
        {
            Assert.IsFalse(UnitReadiness.CanForceSkill(Snapshots.Unit(1, cooldown: 1)));
        }

        [TestCase(UnitCondition.Scattered)]
        [TestCase(UnitCondition.Downed)]
        public void IncapacitatedAlly_CannotForceItsSkill(UnitCondition condition)
        {
            Assert.IsFalse(UnitReadiness.CanForceSkill(Snapshots.Unit(1, condition: condition)));
        }

        [Test]
        public void RescuingAlly_CannotForceItsSkill_EvenWithNoCooldown()
        {
            // 재사용 대기와 상태만 보면 참이 된다 — 구조자는 다른 명령을 받지 않으므로 빛나면 안 된다.
            Assert.IsFalse(UnitReadiness.CanForceSkill(Snapshots.Unit(1, rescueTargetId: 2)));
        }

        [Test]
        public void EnemyAndOffFieldUnits_NeverGlow()
        {
            Assert.IsFalse(UnitReadiness.CanForceSkill(Snapshots.Unit(101, Side.Enemy)));
            Assert.IsFalse(UnitReadiness.CanForceSkill(Snapshots.Unit(1, isOnField: false)));
        }
    }
}
