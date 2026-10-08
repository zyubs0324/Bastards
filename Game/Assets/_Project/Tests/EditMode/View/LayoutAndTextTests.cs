using NUnit.Framework;
using TAoB.Combat;
using UnityEngine;

namespace TAoB.View.Tests
{
    public class LayoutAndTextTests
    {
        [Test]
        public void Slots_AreCentredVertically_TopToBottom()
        {
            float single = BattleLayout.UnitY(0, 1);
            float first = BattleLayout.UnitY(0, 2);
            float second = BattleLayout.UnitY(1, 2);

            Assert.Greater(first, second, "0번 자리가 위");
            Assert.AreEqual(single, (first + second) * 0.5f, 0.001f, "두 자리의 가운데 = 한 자리 위치");
        }

        [Test]
        public void AllyAnchorSitsLeftOfCentre_SoApproachingEnemiesAreVisible()
        {
            Assert.Less(BattleLayout.AllyAnchorScreenX, 0f);
            Assert.Greater(BattleLayout.AllyAnchorScreenX, -BattleLayout.Width * 0.5f, "화면 밖으로 나가서는 안 된다.");
        }

        [Test]
        public void BackgroundTiles_CoverTheScreen_SoTheWrapSeamNeverShows()
        {
            // 묶음 폭은 간격 × (개수 − 1)이고 랩어라운드가 한 칸을 더 먹는다. 두 가지를 빼먹으면
            // 모자란 개수를 통과시켜, 행군할 때 화면 가장자리에서 띠가 튀어나온다.
            Assert.IsTrue(BattleLayout.TilesCoverScreen(BattleLayout.LaneMarkerSpacingPx, BattleLayout.LaneMarkerCount), "가까운 층");
            Assert.IsTrue(BattleLayout.TilesCoverScreen(BattleLayout.LaneFarSpacingPx, BattleLayout.LaneFarCount), "먼 층");
            Assert.IsFalse(BattleLayout.TilesCoverScreen(240f, 9), "모자란 구성은 거짓이어야 한다 — 늘 참이면 검사가 아니다.");
        }

        [Test]
        public void HoldingTheLine_ShowsNoStanceTag()
        {
            // 평소와 다르게 움직이는 동료만 표식을 단다.
            Assert.AreEqual("", UiText.StanceTag(Stance.Hold, false));
        }

        [Test]
        public void EveryActiveStance_HasADistinctTag_AndAFlankerIsToldApartFromAChargerAndAColor()
        {
            var seen = new System.Collections.Generic.HashSet<string>();
            foreach (Stance stance in new[] { Stance.Forward, Stance.Charge, Stance.HoldBack, Stance.StepBack })
            {
                string tag = UiText.StanceTag(stance, false);
                Assert.IsNotEmpty(tag, stance.ToString());
                Assert.IsTrue(seen.Add(tag), $"{stance} 표식이 다른 자세와 같다");
            }

            Assert.IsTrue(seen.Add(UiText.StanceTag(Stance.Charge, true)), "침투는 돌진과 다른 표식이다.");
            Assert.AreNotEqual(BattleLayout.StanceTagColor(Stance.Charge, false), BattleLayout.StanceTagColor(Stance.Charge, true));
            Assert.AreNotEqual(BattleLayout.StanceTagColor(Stance.Forward, false), BattleLayout.StanceTagColor(Stance.StepBack, false));
        }

        [Test]
        public void EveryOutcome_HasDistinctText()
        {
            var seen = new System.Collections.Generic.HashSet<string>();
            foreach (BattleOutcome outcome in System.Enum.GetValues(typeof(BattleOutcome)))
            {
                Assert.IsTrue(seen.Add(UiText.Outcome(outcome)), outcome.ToString());
            }
        }

        [Test]
        public void EveryCommandResult_HasText()
        {
            foreach (CommandResult result in System.Enum.GetValues(typeof(CommandResult)))
            {
                Assert.AreNotEqual("거부", UiText.CommandResult(result), result.ToString());
            }
        }

        [Test]
        public void JobGlyph_IsOneCharacter()
        {
            foreach (Job job in System.Enum.GetValues(typeof(Job)))
            {
                Assert.AreEqual(1, UiText.JobGlyph(job).Length, job.ToString());
            }
        }

        [Test]
        public void EveryOverloadOutcome_HasDistinctText()
        {
            var seen = new System.Collections.Generic.HashSet<string>();
            foreach (OverloadOutcome outcome in System.Enum.GetValues(typeof(OverloadOutcome)))
            {
                Assert.IsTrue(seen.Add(UiText.OverloadOutcome(outcome)), outcome.ToString());
            }
        }

        [Test]
        public void Condition_NormalIsEmpty_OthersHaveText()
        {
            Assert.AreEqual("", UiText.Condition(UnitCondition.Normal), "정상 상태는 글자를 띄우지 않는다");
            Assert.AreNotEqual("", UiText.Condition(UnitCondition.Scattered));
            Assert.AreNotEqual("", UiText.Condition(UnitCondition.Downed));
            Assert.AreNotEqual(UiText.Condition(UnitCondition.Scattered), UiText.Condition(UnitCondition.Downed));
        }

        [Test]
        public void JobColor_EnemyIsDarkerThanAlly_AndJobsDiffer()
        {
            foreach (Job job in System.Enum.GetValues(typeof(Job)))
            {
                Color ally = BattleLayout.JobColor(job, Side.Ally);
                Color enemy = BattleLayout.JobColor(job, Side.Enemy);
                Assert.Less(enemy.grayscale, ally.grayscale, job.ToString());
            }

            Assert.AreNotEqual(BattleLayout.JobColor(Job.Knight, Side.Ally), BattleLayout.JobColor(Job.Mage, Side.Ally));
            Assert.AreNotEqual(BattleLayout.JobColor(Job.Mage, Side.Ally), BattleLayout.JobColor(Job.Perceptive, Side.Ally));
        }
    }
}
