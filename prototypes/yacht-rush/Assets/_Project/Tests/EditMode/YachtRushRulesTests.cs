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
            Assert.AreEqual(8, YachtRushRules.ContractBonus(YachtRushContract.HighTide, new[] { 6, 6, 4, 3, 2 }, 21, 2, 0));
            Assert.AreEqual(10, YachtRushRules.ContractBonus(YachtRushContract.TripleSignal, new[] { 2, 2, 2, 5, 6 }, 17, 2, 0));
            Assert.AreEqual(7, YachtRushRules.ContractBonus(YachtRushContract.LowDeck, new[] { 1, 2, 3, 5, 6 }, 17, 2, 0));
            Assert.AreEqual(6, YachtRushRules.ContractBonus(YachtRushContract.CleanRun, new[] { 6, 6, 4, 3, 2 }, 21, 1, 0));
            Assert.AreEqual(5, YachtRushRules.ContractBonus(YachtRushContract.BoldScratch, new[] { 2, 3, 4, 5, 6 }, 0, 2, 0));
            Assert.AreEqual(6, YachtRushRules.ContractBonus(YachtRushContract.PerfectHold, new[] { 6, 6, 6, 3, 2 }, 23, 2, 3));
        }
    }
}
