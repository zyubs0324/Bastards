using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace TAoB.Combat.Tests
{
    /// <summary>
    /// 파티 소모품(계획서 Phase D). 대상은 HP 비율이 가장 낮은 아군이고, 재고는 실제로 회복했을 때만 줄어든다.
    /// </summary>
    public class ItemBagTests
    {
        private const int PotionId = 7;

        private static ItemBag Bag(int healAmount = 100, int count = 2)
        {
            return new ItemBag(new[] { new ConsumableSetup(PotionId, healAmount, count) });
        }

        private static UnitState Ally(int id, int hp, int maxHp = 1000, UnitCondition condition = UnitCondition.Normal)
        {
            var unit = new UnitState(TestUnits.Ally(id, stats: TestUnits.MakeStats(hp: maxHp)), UnitState.AllyWave);
            unit.Hp = hp;
            unit.Condition = condition;
            return unit;
        }

        private static List<BattleEvent> Use(ItemBag bag, UnitState[] units)
        {
            var events = new List<BattleEvent>();
            bag.UseRequested(units, 3, events);
            return events;
        }

        [Test]
        public void HealsTheAllyWithTheLowestHpRatio_NotTheLowestHp()
        {
            // 1 번은 400/1000(40%), 2 번은 90/200(45%). 남은 HP 는 2 번이 적지만 비율은 1 번이 낮다.
            UnitState[] units = TestField.ById(Ally(1, 400), Ally(2, 90, maxHp: 200));
            ItemBag bag = Bag();

            Assert.AreEqual(CommandResult.Accepted, bag.Request(PotionId, units));
            BattleEvent used = Use(bag, units).Single();

            Assert.AreEqual(BattleEventKind.ItemUsed, used.Kind);
            Assert.AreEqual(1, used.TargetId);
            Assert.AreEqual(100, used.Value);
            Assert.AreEqual(PotionId, used.Detail);
            Assert.AreEqual(500, units[0].Hp);
            Assert.AreEqual(1, bag.RemainingAt(0));
        }

        [Test]
        public void EqualRatios_GoToTheLowerId()
        {
            UnitState[] units = TestField.ById(Ally(2, 100, maxHp: 200), Ally(1, 500));
            ItemBag bag = Bag();
            bag.Request(PotionId, units);

            Assert.AreEqual(1, Use(bag, units).Single().TargetId);
        }

        [Test]
        public void HealIsCappedAtMaxHp()
        {
            UnitState[] units = TestField.ById(Ally(1, 950));
            ItemBag bag = Bag(healAmount: 100);
            bag.Request(PotionId, units);

            BattleEvent used = Use(bag, units).Single();

            Assert.AreEqual(50, used.Value, "이벤트는 실제로 회복한 양을 싣는다.");
            Assert.AreEqual(1000, units[0].Hp);
        }

        [Test]
        public void EachUsePicksItsTargetAgain()
        {
            // 첫 번째는 1 번(30%)에게 가고, 그 뒤 1 번이 40% 가 되어 두 번째는 2 번(35%)에게 간다.
            UnitState[] units = TestField.ById(Ally(1, 300), Ally(2, 350));
            ItemBag bag = Bag(healAmount: 100, count: 2);
            bag.Request(PotionId, units);
            bag.Request(PotionId, units);

            List<BattleEvent> events = Use(bag, units);

            CollectionAssert.AreEqual(new[] { 1, 2 }, events.Select(e => e.TargetId).ToArray());
            Assert.AreEqual(0, bag.RemainingAt(0));
        }

        [Test]
        public void ScatteredAlly_CanBeHealed_ButDownedFullAndEnemyUnitsCannot()
        {
            UnitState scattered = Ally(1, 500, condition: UnitCondition.Scattered);
            UnitState downed = Ally(2, 0, condition: UnitCondition.Downed);
            UnitState full = Ally(3, 1000);
            UnitState enemy = TestField.Enemy(101);
            enemy.Hp = 1;

            Assert.AreEqual(CommandResult.Accepted, Bag().Request(PotionId, TestField.ById(scattered, downed, full, enemy)));
            Assert.AreEqual(CommandResult.RejectedNoHealTarget, Bag().Request(PotionId, TestField.ById(downed, full, enemy)));
        }

        [Test]
        public void UnknownItem_IsRejected()
        {
            Assert.AreEqual(CommandResult.RejectedUnknownItem, Bag().Request(PotionId + 1, TestField.ById(Ally(1, 500))));
        }

        [Test]
        public void RequestsBeyondTheStock_AreRejected_CountingTheOnesAlreadyWaiting()
        {
            UnitState[] units = TestField.ById(Ally(1, 100));
            ItemBag bag = Bag(count: 1);

            Assert.AreEqual(CommandResult.Accepted, bag.Request(PotionId, units));
            Assert.AreEqual(CommandResult.RejectedNoStock, bag.Request(PotionId, units), "한 틱 안에 연달아 눌러도 재고보다 많이 받지 않는다.");

            Use(bag, units);
            Assert.AreEqual(0, bag.RemainingAt(0));
            Assert.AreEqual(CommandResult.RejectedNoStock, bag.Request(PotionId, units));
        }

        [Test]
        public void TargetGoneBeforeTheUse_KeepsTheStock_AndDropsTheRequest()
        {
            UnitState ally = Ally(1, 500);
            UnitState[] units = TestField.ById(ally);
            ItemBag bag = Bag(count: 1);
            bag.Request(PotionId, units);
            ally.Hp = ally.MaxHp;

            Assert.IsEmpty(Use(bag, units));
            Assert.AreEqual(1, bag.RemainingAt(0), "회복하지 못했으면 재고를 쓰지 않는다.");

            ally.Hp = 500;
            Assert.AreEqual(CommandResult.Accepted, bag.Request(PotionId, units), "버려진 요청이 재고를 붙잡고 있지 않다.");
        }

        [Test]
        public void TwoKinds_AreTrackedSeparately()
        {
            UnitState[] units = TestField.ById(Ally(1, 500));
            var bag = new ItemBag(new[] { new ConsumableSetup(PotionId, 100, 2), new ConsumableSetup(9, 30, 4) });

            Assert.AreEqual(CommandResult.Accepted, bag.Request(9, units));
            BattleEvent used = Use(bag, units).Single();

            Assert.AreEqual(9, used.Detail);
            Assert.AreEqual(30, used.Value, "두 번째 종류의 회복량을 쓴다.");
            Assert.AreEqual(2, bag.RemainingAt(0), "첫 번째 종류의 재고는 그대로다.");
            Assert.AreEqual(3, bag.RemainingAt(1));
            Assert.AreEqual(9, bag.IdAt(1));
        }

        [Test]
        public void EmptyStock_IsReportedBeforeTheMissingTarget()
        {
            // 재고도 없고 회복할 동료도 없으면 사용자에게는 "남은 소모품이 없다"가 먼저다.
            UnitState ally = Ally(1, 500);
            UnitState[] units = TestField.ById(ally);
            ItemBag bag = Bag(healAmount: 600, count: 1);
            bag.Request(PotionId, units);
            Use(bag, units);
            Assert.AreEqual(ally.MaxHp, ally.Hp, "전제: 가득 회복했다.");

            Assert.AreEqual(CommandResult.RejectedNoStock, bag.Request(PotionId, units));
        }

        [Test]
        public void SeveralRequests_WithTheTargetGone_AreAllDropped_WithoutHanging()
        {
            UnitState ally = Ally(1, 500);
            UnitState[] units = TestField.ById(ally);
            ItemBag bag = Bag(count: 3);
            bag.Request(PotionId, units);
            bag.Request(PotionId, units);
            ally.Hp = ally.MaxHp;

            Assert.IsEmpty(Use(bag, units));
            Assert.AreEqual(3, bag.RemainingAt(0));

            ally.Hp = 500;
            Assert.IsEmpty(Use(bag, units), "버려진 요청이 다음 틱에 되살아나지 않는다.");
        }

        [TestCase(-1, 100)]
        [TestCase(PotionId, CombatScale.MaxValue + 1)]
        public void ConsumableSetup_RejectsAnIdOrHealAmountOutOfRange(int id, int healAmount)
        {
            Assert.Throws<ArgumentException>(() => new ConsumableSetup(id, healAmount, 1));
        }

        [TestCase(1)]
        [TestCase(CombatScale.MaxValue)]
        public void ConsumableSetup_AcceptsTheHealAmountBounds(int healAmount)
        {
            Assert.AreEqual(healAmount, new ConsumableSetup(PotionId, healAmount, 1).HealAmount);
        }

        [TestCase(0, 1)]
        [TestCase(100, 0)]
        [TestCase(100, ConsumableSetup.MaxCount + 1)]
        public void ConsumableSetup_RejectsValuesOutOfRange(int healAmount, int count)
        {
            Assert.Throws<ArgumentException>(() => new ConsumableSetup(PotionId, healAmount, count));
        }

        [Test]
        public void ConsumableSetup_AcceptsTheMaximumCount()
        {
            Assert.AreEqual(ConsumableSetup.MaxCount, new ConsumableSetup(PotionId, 1, ConsumableSetup.MaxCount).Count);
        }

        // ---- 전투 안에서 ----

        private static BattleSimulation BattleWithPotion(int count = 2)
        {
            // 적은 10틱마다 앞열 아군(1)을 50 씩 때린다.
            var enemy = TestUnits.Enemy(101, stats: TestUnits.MakeStats(atk: 50, hp: TestUnits.Unkillable, spd: 100));
            return new BattleSimulation(
                TestUnits.Setup(
                    new[] { TestUnits.Ally(1) },
                    TestUnits.OneWave(enemy),
                    consumables: new[] { new ConsumableSetup(PotionId, 30, count) }),
                new FixedRandom());
        }

        private static int Remaining(BattleSimulation sim)
        {
            BattleSnapshot snapshot = sim.CreateSnapshotBuffer();
            sim.FillSnapshot(snapshot);
            Assert.AreEqual(PotionId, snapshot.Items.Single().Id);
            return snapshot.Items.Single().Remaining;
        }

        [Test]
        public void UseItem_IsRejectedAtFullHp_ThenHealsAtTheStartOfTheNextTick()
        {
            var sim = BattleWithPotion();
            Assert.AreEqual(CommandResult.RejectedNoHealTarget, sim.Enqueue(BattleCommand.UseItem(PotionId)));

            SimRunner.Run(sim, 10);
            Assert.AreEqual(950, SimRunner.Unit(sim, 1).Hp, "전제: 10틱에 50 을 맞았다.");
            Assert.AreEqual(CommandResult.Accepted, sim.Enqueue(BattleCommand.UseItem(PotionId)));
            Assert.AreEqual(2, Remaining(sim), "요청만으로는 재고가 줄지 않는다.");

            BattleEvent used = SimRunner.OfKind(SimRunner.Run(sim, 1), BattleEventKind.ItemUsed).Single();

            Assert.AreEqual(11, used.Tick);
            Assert.AreEqual(1, used.TargetId);
            Assert.AreEqual(30, used.Value);
            Assert.AreEqual(980, SimRunner.Unit(sim, 1).Hp);
            Assert.AreEqual(1, Remaining(sim));
        }

        [Test]
        public void UseItem_DoesNotDisturbTheUnitsPendingCommand()
        {
            var sim = BattleWithPotion();
            SimRunner.Run(sim, 10);
            sim.Enqueue(BattleCommand.UseSkill(1));

            Assert.AreEqual(CommandResult.Accepted, sim.Enqueue(BattleCommand.UseItem(PotionId)));
            var events = SimRunner.Run(sim, 10);

            Assert.AreEqual(1, SimRunner.OfKind(events, BattleEventKind.ItemUsed).Count);
            Assert.IsTrue(SimRunner.ActionsBy(events, 1).Any(e => e.Detail == (int)CommandKind.UseSkill), "소모품이 대기 중인 기술 명령을 덮어쓰지 않는다.");
        }

        [Test]
        public void UseItem_IsAppliedBeforeTheScatterLoss_SoItCanSaveAnAllyOnTheBrink()
        {
            // 마법사(HP 60)는 10틱에 흩어져 틱마다 5 를 잃고 21틱에 HP 5 가 된다. 22틱의 피해가 먼저면 쓰러지고 회복약은 쓸 곳이 없다.
            var mage = TestUnits.Ally(1, Row.Back, Job.Mage, TestUnits.MakeStats(hp: 60), TestUnits.Skill(canOverload: true), new Catalyst(100));
            var sim = new BattleSimulation(
                TestUnits.Setup(
                    new[] { mage, TestUnits.Ally(2) },
                    TestUnits.OneWave(TestUnits.Enemy(101)),
                    consumables: new[] { new ConsumableSetup(PotionId, 30, 1) }),
                new FixedRandom(4000));
            sim.Enqueue(BattleCommand.Overload(1));
            SimRunner.Run(sim, 21);
            Assert.AreEqual(5, SimRunner.Unit(sim, 1).Hp, "전제: 다음 틱의 흩어짐 피해로 쓰러질 HP 다.");
            Assert.AreEqual(CommandResult.Accepted, sim.Enqueue(BattleCommand.UseItem(PotionId)));

            var events = SimRunner.Run(sim, 1);

            Assert.AreEqual(1, SimRunner.OfKind(events, BattleEventKind.ItemUsed).Single().TargetId);
            Assert.AreEqual(UnitCondition.Scattered, SimRunner.Unit(sim, 1).Condition);
            Assert.AreEqual(30, SimRunner.Unit(sim, 1).Hp, "5 + 30 − 5");
        }

        [Test]
        public void RetreatInTheSameTick_EndsTheBattleBeforeTheItemIsUsed()
        {
            var sim = BattleWithPotion();
            SimRunner.Run(sim, 10);
            sim.Enqueue(BattleCommand.UseItem(PotionId));
            sim.Enqueue(BattleCommand.Retreat());

            var events = SimRunner.Run(sim, 1);

            Assert.AreEqual(BattleOutcome.DefeatRetreat, sim.Outcome);
            Assert.IsEmpty(SimRunner.OfKind(events, BattleEventKind.ItemUsed));
            Assert.AreEqual(2, Remaining(sim));
        }

        [Test]
        public void UseItem_AfterTheBattle_IsRejected()
        {
            var sim = BattleWithPotion();
            SimRunner.Run(sim, 10);
            sim.Enqueue(BattleCommand.Retreat());
            SimRunner.Run(sim, 1);

            Assert.AreEqual(CommandResult.RejectedBattleOver, sim.Enqueue(BattleCommand.UseItem(PotionId)));
        }

        [Test]
        public void SameBattle_WithAndWithoutAPendingItemRequest_HashesDifferently()
        {
            // 요청은 다음 틱까지 대기하는 지속 상태라 해시에 들어가야 한다. HP·재고는 아직 같다.
            var plain = BattleWithPotion();
            var requested = BattleWithPotion();
            SimRunner.Run(plain, 10);
            SimRunner.Run(requested, 10);
            requested.Enqueue(BattleCommand.UseItem(PotionId));

            Assert.AreNotEqual(plain.BuildResult().StateHash, requested.BuildResult().StateHash);
        }

        [Test]
        public void SameBattle_WithDifferentStock_HashesDifferently()
        {
            Assert.AreNotEqual(BattleWithPotion(count: 2).BuildResult().StateHash, BattleWithPotion(count: 3).BuildResult().StateHash);
        }

        // ---- 전투 설정 검증 ----

        private static BattleSetup SetupWith(IReadOnlyList<ConsumableSetup> consumables)
        {
            return new BattleSetup(
                TestUnits.Balance(),
                new AffinityTable(new AffinityEntry[0]),
                new[] { TestUnits.Ally(1) },
                TestUnits.OneWave(TestUnits.Enemy(101)),
                TestUnits.Approach(1),
                BattleSetup.MaxTimeLimitTicks,
                PreconditionKind.None,
                consumables);
        }

        [Test]
        public void Setup_RequiresTheConsumableList_EvenWhenEmpty()
        {
            Assert.AreEqual("consumables", Assert.Throws<ArgumentNullException>(() => SetupWith(null)).ParamName);
            Assert.IsEmpty(SetupWith(new ConsumableSetup[0]).Consumables);
        }

        [Test]
        public void Setup_RejectsAnEmptySlotInTheList()
        {
            Assert.Throws<ArgumentException>(() => SetupWith(new ConsumableSetup[] { null }));
        }

        [Test]
        public void Setup_RejectsDuplicateItemIds()
        {
            var ex = Assert.Throws<ArgumentException>(() => SetupWith(new[]
            {
                new ConsumableSetup(PotionId, 10, 1),
                new ConsumableSetup(PotionId, 20, 1),
            }));

            Assert.AreEqual("consumables", ex.ParamName);
        }

        [Test]
        public void Setup_RejectsMoreKindsThanTheLimit_AndAcceptsExactlyTheLimit()
        {
            ConsumableSetup[] Kinds(int count) => Enumerable.Range(0, count).Select(i => new ConsumableSetup(i, 10, 1)).ToArray();

            Assert.Throws<ArgumentException>(() => SetupWith(Kinds(BattleSetup.MaxConsumableKinds + 1)));
            Assert.AreEqual(BattleSetup.MaxConsumableKinds, SetupWith(Kinds(BattleSetup.MaxConsumableKinds)).Consumables.Count);
        }
    }
}
