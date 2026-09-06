using System.Linq;
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

        [Test]
        public void MapsYachtCategoriesToHarborActions()
        {
            Assert.AreEqual("Tailwind", YachtRushRules.GetHarborAction(YachtRushCategory.Ones).Name);
            Assert.AreEqual("Stock Up", YachtRushRules.GetHarborAction(YachtRushCategory.Twos).Name);
            Assert.AreEqual("Grand Voyage", YachtRushRules.GetHarborAction(YachtRushCategory.Yacht).Name);
            Assert.AreEqual(YachtRushRules.Categories.Length, YachtRushRules.HarborActions.Length);
        }

        [Test]
        public void HarborActionsChangeVoyageState()
        {
            var preview = new YachtRushRoundScorePreview(2, 2, 0, 2, false, new[] { 1, 1, 4, 4, 6 });
            var effect = YachtRushRules.PreviewHarborAction(YachtRushCategory.Ones, preview, YachtRushRushDie.None);
            var state = YachtRushRules.ApplyHarborAction(
                new HarborYachtState(1, 0, YachtRushRules.HarborStartingHull, YachtRushRules.HarborStartingSupplies, 0),
                effect);

            Assert.IsTrue(effect.IsAvailable);
            Assert.AreEqual(18, state.RouteProgress);
            Assert.AreEqual(YachtRushRules.HarborStartingSupplies - 1, state.Supplies);
            Assert.AreEqual(2, state.Day);
        }

        [Test]
        public void VoyageCommandsCanBeLockedByWindRoll()
        {
            var preview = new YachtRushRoundScorePreview(0, 0, 0, 0, false, new[] { 1, 2, 3, 4, 5 });
            var effect = YachtRushRules.PreviewHarborAction(YachtRushCategory.Ones, preview, YachtRushRushDie.None);

            Assert.IsFalse(effect.IsAvailable);
            StringAssert.Contains("1 Wind + 1 Wind + 4 Sail", effect.LockedReason);
        }

        [Test]
        public void HarborRunEndsWhenHullIsLost()
        {
            var result = YachtRushRules.EvaluateHarborRun(new HarborYachtState(4, 20, 0, 3, 10), 3);
            Assert.IsTrue(result.IsComplete);
            Assert.IsFalse(result.IsSuccess);
            Assert.AreEqual("Lost at Sea", result.Title);
        }

        [Test]
        public void HarborRunEndsAtTwelveDays()
        {
            var result = YachtRushRules.EvaluateHarborRun(new HarborYachtState(12, 35, 12, 4, 12), YachtRushRules.RoundCount);
            Assert.IsTrue(result.IsComplete);
            Assert.AreEqual("Drifted Home", result.Title);
        }

        [Test]
        public void HarborRunSucceedsAtEndWhenDistanceGoalIsReached()
        {
            var result = YachtRushRules.EvaluateHarborRun(new HarborYachtState(12, YachtRushRules.HarborTargetRoute, 12, 4, 12), YachtRushRules.RoundCount);
            Assert.IsTrue(result.IsComplete);
            Assert.IsTrue(result.IsSuccess);
            Assert.AreEqual("Voyage Complete", result.Title);
        }

        [Test]
        public void LowWindCreatesVoyageRisk()
        {
            var preview = new YachtRushRoundScorePreview(9, 9, 0, 9, false, new[] { 1, 1, 2, 2, 3 });
            var calm = YachtRushRules.PreviewHarborAction(YachtRushCategory.Chance, preview, YachtRushRushDie.None);

            Assert.IsTrue(calm.IsAvailable);
            Assert.AreEqual(4, calm.RouteDelta);
            Assert.AreEqual(-4, calm.HullDelta);
            Assert.AreEqual(-2, calm.HazardDelta);
        }

        [Test]
        public void DiceFacesMapToCrewResources()
        {
            CollectionAssert.AreEqual(new[] { 1, 1, 1, 1, 1, 0 }, YachtRushRules.CountCrewResources(new[] { 1, 2, 3, 4, 5 }));
            Assert.AreEqual("Sail", YachtRushRules.CrewResourceName(1));
            Assert.AreEqual("Hull", YachtRushRules.CrewResourceName(2));
            Assert.AreEqual("Food", YachtRushRules.CrewResourceName(3));
            Assert.AreEqual("Crew", YachtRushRules.CrewResourceName(4));
            Assert.AreEqual("Gold", YachtRushRules.CrewResourceName(5));
            Assert.AreEqual("Map", YachtRushRules.CrewResourceName(6));
        }

        [Test]
        public void MatchingResourcesUnlockSingleResourceStrategies()
        {
            Assert.IsTrue(YachtRushRules.PreviewVoyageStrategy(VoyageStrategy.TailwindRun, YachtRushRules.CountCrewResources(new[] { 1, 1, 3, 4, 6 })).IsAvailable);
            Assert.IsTrue(YachtRushRules.PreviewVoyageStrategy(VoyageStrategy.PatchTheHull, YachtRushRules.CountCrewResources(new[] { 2, 2, 1, 4, 6 })).IsAvailable);
            Assert.IsTrue(YachtRushRules.PreviewVoyageStrategy(VoyageStrategy.StockTheHold, YachtRushRules.CountCrewResources(new[] { 3, 3, 1, 4, 6 })).IsAvailable);
        }

        [Test]
        public void ResourceCombinationsUnlockVoyageStrategies()
        {
            Assert.IsTrue(YachtRushRules.PreviewVoyageStrategy(VoyageStrategy.LongVoyage, YachtRushRules.CountCrewResources(new[] { 1, 3, 6, 4, 4 })).IsAvailable);
            Assert.IsTrue(YachtRushRules.PreviewVoyageStrategy(VoyageStrategy.FullDeck, YachtRushRules.CountCrewResources(new[] { 1, 2, 3, 4, 5 })).IsAvailable);
            Assert.IsTrue(YachtRushRules.PreviewVoyageStrategy(VoyageStrategy.TradeRoute, YachtRushRules.CountCrewResources(new[] { 3, 5, 6, 1, 1 })).IsAvailable);
        }

        [Test]
        public void AvailableVoyageStrategiesExplainWhyTheyAppeared()
        {
            var strategies = YachtRushRules.AvailableVoyageStrategies(YachtRushRules.CountCrewResources(new[] { 1, 1, 3, 4, 6 }));

            Assert.IsTrue(strategies.Any(strategy => strategy.Strategy == VoyageStrategy.TailwindRun));
            Assert.IsTrue(strategies.All(strategy => strategy.Condition.StartsWith("Need")));
            Assert.IsTrue(strategies.All(strategy => strategy.Have.Contains("Sail") || strategy.Have.Contains("Food") || strategy.Have.Contains("Crew") || strategy.Have.Contains("Map")));
        }

        [Test]
        public void ApplyingVoyageStrategyUpdatesStateWithoutHiddenUpkeep()
        {
            var state = new HarborYachtState(1, 0, 10, 4, 0);
            var preview = YachtRushRules.PreviewVoyageStrategy(VoyageStrategy.TailwindRun, YachtRushRules.CountCrewResources(new[] { 1, 1, 1, 2, 5 }));
            var next = YachtRushRules.ApplyVoyageStrategy(state, preview, 0, out var supplyUpkeep, out var stormDamage);

            Assert.AreEqual(12, next.RouteProgress);
            Assert.AreEqual(4, next.Supplies);
            Assert.AreEqual(0, supplyUpkeep);
            Assert.AreEqual(0, stormDamage);
        }

        [Test]
        public void VoyageRunEndsOnFailureOrTwelveMonths()
        {
            Assert.IsTrue(YachtRushRules.EvaluateVoyageRun(new HarborYachtState(4, 20, 0, 3, 0), 3).IsComplete);
            Assert.AreEqual("Out of Supplies", YachtRushRules.EvaluateVoyageRun(new HarborYachtState(4, 20, 5, 0, 0), 3).Title);
            Assert.IsTrue(YachtRushRules.EvaluateVoyageRun(new HarborYachtState(12, 35, 5, 3, 0), YachtRushRules.RoundCount).IsComplete);
            Assert.IsTrue(YachtRushRules.EvaluateVoyageRun(new HarborYachtState(8, YachtRushRules.HarborTargetRoute, 5, 3, 0), 7).IsSuccess);
        }
    }
}
