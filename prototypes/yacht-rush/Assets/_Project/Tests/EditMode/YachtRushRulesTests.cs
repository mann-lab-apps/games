using MannLab.Games.YachtRush;
using NUnit.Framework;

namespace MannLab.Games.YachtRush.Tests
{
    public sealed class YachtRushRulesTests
    {
        [Test]
        public void ScoresNumberCategories()
        {
            Assert.AreEqual(15, YachtRushRules.ScoreCategory(YachtRushCategory.Fives, new[] { 5, 5, 2, 3, 5 }));
            Assert.AreEqual(0, YachtRushRules.ScoreCategory(YachtRushCategory.Ones, new[] { 2, 3, 4, 5, 6 }));
        }

        [Test]
        public void ScoresCombinationCategories()
        {
            Assert.AreEqual(26, YachtRushRules.ScoreCategory(YachtRushCategory.FourOfAKind, new[] { 6, 6, 6, 6, 2 }));
            Assert.AreEqual(25, YachtRushRules.ScoreCategory(YachtRushCategory.FullHouse, new[] { 4, 4, 4, 1, 1 }));
            Assert.AreEqual(30, YachtRushRules.ScoreCategory(YachtRushCategory.SmallStraight, new[] { 1, 2, 3, 4, 6 }));
            Assert.AreEqual(40, YachtRushRules.ScoreCategory(YachtRushCategory.LargeStraight, new[] { 2, 3, 4, 5, 6 }));
            Assert.AreEqual(50, YachtRushRules.ScoreCategory(YachtRushCategory.Yacht, new[] { 3, 3, 3, 3, 3 }));
            Assert.AreEqual(16, YachtRushRules.ScoreCategory(YachtRushCategory.Chance, new[] { 1, 2, 3, 4, 6 }));
        }

        [Test]
        public void AwardsContractBonuses()
        {
            Assert.AreEqual(10, YachtRushRules.ContractBonus(YachtRushContract.HighTide, new[] { 6, 6, 4, 3, 3 }, 22, 2, 0));
            Assert.AreEqual(12, YachtRushRules.ContractBonus(YachtRushContract.TwinWake, new[] { 2, 2, 5, 5, 6 }, 20, 2, 0));
            Assert.AreEqual(9, YachtRushRules.ContractBonus(YachtRushContract.CaptainPair, new[] { 6, 6, 1, 3, 2 }, 18, 2, 0));
        }

        [Test]
        public void AppliesRushDieEffects()
        {
            CollectionAssert.AreEqual(
                new[] { 1, 2, 3, 2, 5 },
                YachtRushRules.ApplyRushDie(new[] { 1, 2, 3, 5, 5 }, YachtRushRushDie.Mirror, 3, true));
            CollectionAssert.AreEqual(
                new[] { 1, 0, 3, 4, 5 },
                YachtRushRules.ApplyRushDie(new[] { 1, 2, 3, 4, 5 }, YachtRushRushDie.Blank, 1, false));
            CollectionAssert.AreEqual(
                new[] { 1, 2, 3, 4, 5 },
                YachtRushRules.ApplyRushDie(new[] { 1, 2, 3, 4, 5 }, YachtRushRushDie.None, 1, false));
        }

        [Test]
        public void AppliesRollRules()
        {
            Assert.AreEqual(1, YachtRushRules.MaxRollsForRule(YachtRushRollRule.OneShot));
            Assert.AreEqual(2, YachtRushRules.MaxRollsForRule(YachtRushRollRule.SafeHarbor));
            Assert.IsFalse(YachtRushRules.CanHold(YachtRushRollRule.NoHolds));
            Assert.IsFalse(YachtRushRules.CanThrowWithRule(YachtRushRollRule.MustHold2, 1, 1));
            Assert.IsTrue(YachtRushRules.CanThrowWithRule(YachtRushRollRule.MustHold2, 1, 2));
            Assert.IsTrue(YachtRushRules.ShouldRerollHeldDice(YachtRushRollRule.RerollAll));
        }

        [Test]
        public void DetectsContractHands()
        {
            Assert.IsTrue(YachtRushRules.IsContractHandSatisfied(YachtRushContract.EvenFleet, new[] { 2, 4, 6, 2, 4 }, -1, YachtRushRushDie.Anchor, 0, 0));
            Assert.IsTrue(YachtRushRules.IsContractHandSatisfied(YachtRushContract.OddCrew, new[] { 1, 3, 5, 1, 3 }, -1, YachtRushRushDie.Anchor, 0, 0));
            Assert.IsTrue(YachtRushRules.IsContractHandSatisfied(YachtRushContract.TwinWake, new[] { 2, 2, 5, 5, 6 }, -1, YachtRushRushDie.Anchor, 0, 0));
            Assert.IsTrue(YachtRushRules.IsContractHandSatisfied(YachtRushContract.BrokenRun, new[] { 1, 2, 4, 5, 6 }, -1, YachtRushRushDie.Anchor, 0, 0));
            Assert.IsTrue(YachtRushRules.IsContractHandSatisfied(YachtRushContract.CleanBowl, new[] { 1, 2, 3, 4, 5 }, 0, YachtRushRushDie.Anchor, 0, 0, 1));
        }

        [Test]
        public void PreviewsRushAdjustedScore()
        {
            var mirror = YachtRushRules.PreviewScore(
                YachtRushCategory.Sixes,
                YachtRushContract.CaptainPair,
                YachtRushRollRule.Classic,
                YachtRushRushDie.Mirror,
                0,
                new[] { 1, 6, 6, 3, 4 },
                1,
                0,
                0);
            Assert.AreEqual(12, mirror.BaseScore);
            Assert.AreEqual(18, mirror.RushAdjustedScore);
            Assert.AreEqual(27, mirror.Total);

            var blank = YachtRushRules.PreviewScore(
                YachtRushCategory.Chance,
                YachtRushContract.HighTide,
                YachtRushRollRule.Classic,
                YachtRushRushDie.Blank,
                0,
                new[] { 1, 2, 3, 4, 5 },
                1,
                0,
                0);
            Assert.AreEqual(15, blank.BaseScore);
            Assert.AreEqual(14, blank.RushAdjustedScore);
            Assert.AreEqual(0, blank.ContractBonus);

            var cracked = YachtRushRules.PreviewScore(
                YachtRushCategory.Chance,
                YachtRushContract.None,
                YachtRushRollRule.Classic,
                YachtRushRushDie.Cracked,
                4,
                new[] { 1, 2, 3, 4, 5 },
                1,
                0,
                0);
            Assert.AreEqual(15, cracked.BaseScore);
            Assert.AreEqual(10, cracked.RushAdjustedScore);
        }
    }
}
