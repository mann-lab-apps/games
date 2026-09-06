using NUnit.Framework;

namespace MannLab.Games.SensitiveBarista.Tests
{
    public sealed class SensitiveBaristaRulesTests
    {
        [Test]
        public void MatchingRatioScoresHigherThanWrongRatio()
        {
            var order = SensitiveBaristaRules.Orders[0];
            var matching = ScaleToIdeal(order);
            var wrong = new IngredientAmounts(0f, order.IdealTotal, 0f, 0f, 0f);

            var matchingScore = SensitiveBaristaRules.Score(order, matching, 0f, 1);
            var wrongScore = SensitiveBaristaRules.Score(order, wrong, 0f, 1);

            Assert.Greater(matchingScore.RoundScore, wrongScore.RoundScore);
            Assert.GreaterOrEqual(matchingScore.RoundScore, 80);
        }

        [Test]
        public void WasteReducesScore()
        {
            var order = SensitiveBaristaRules.Orders[1];
            var drink = ScaleToIdeal(order);

            var clean = SensitiveBaristaRules.Score(order, drink, 0f, 2);
            var messy = SensitiveBaristaRules.Score(order, drink, 40f, 2);

            Assert.Greater(clean.RoundScore, messy.RoundScore);
        }

        [Test]
        public void LaterRoundsAreStricterForSameError()
        {
            var order = SensitiveBaristaRules.Orders[2];
            var nearMiss = new IngredientAmounts(12f, 20f, 0f, 50f, 18f);

            var early = SensitiveBaristaRules.Score(order, nearMiss, 0f, 1);
            var late = SensitiveBaristaRules.Score(order, nearMiss, 0f, 9);

            Assert.Greater(early.RoundScore, late.RoundScore);
        }

        [Test]
        public void PerfectDrinkStillScoresHighInFinalRound()
        {
            var order = SensitiveBaristaRules.Orders[8];

            var score = SensitiveBaristaRules.Score(order, ScaleToIdeal(order), 0f, 10);

            Assert.GreaterOrEqual(score.RoundScore, 95);
            Assert.AreEqual("A", score.BalanceGrade);
            Assert.AreEqual("A", score.VolumeGrade);
        }

        [Test]
        public void EmptyCupScoresZero()
        {
            var order = SensitiveBaristaRules.Orders[3];

            var score = SensitiveBaristaRules.Score(order, new IngredientAmounts(0f, 0f, 0f, 0f, 0f), 0f, 1);

            Assert.AreEqual(0, score.RoundScore);
        }

        [Test]
        public void OverflowIsPenalized()
        {
            var order = SensitiveBaristaRules.Orders[4];
            var ideal = ScaleToIdeal(order);
            var overflow = new IngredientAmounts(
                ideal.Ice * 1.45f,
                ideal.Shot * 1.45f,
                ideal.Water * 1.45f,
                ideal.Milk * 1.45f,
                ideal.Syrup * 1.45f);

            var idealScore = SensitiveBaristaRules.Score(order, ideal, 0f, 4);
            var overflowScore = SensitiveBaristaRules.Score(order, overflow, 0f, 4);

            Assert.Greater(idealScore.RoundScore, overflowScore.RoundScore);
        }

        [Test]
        public void SevereOverflowIsClearlyPenalizedEvenWithCorrectRatio()
        {
            var order = SensitiveBaristaRules.Orders[4];
            var ideal = ScaleToIdeal(order);
            var overflow = new IngredientAmounts(
                ideal.Ice * 1.75f,
                ideal.Shot * 1.75f,
                ideal.Water * 1.75f,
                ideal.Milk * 1.75f,
                ideal.Syrup * 1.75f);

            var score = SensitiveBaristaRules.Score(order, overflow, 0f, 7);

            Assert.Less(score.RoundScore, 75);
        }

        [Test]
        public void OverfilledButRecognizableDrinkKeepsPartialCredit()
        {
            var order = SensitiveBaristaRules.Orders[0];
            var overfilledLatte = new IngredientAmounts(42f, 49f, 0f, 153f, 0f);

            var score = SensitiveBaristaRules.Score(order, overfilledLatte, 0f, 1);

            Assert.Greater(score.RoundScore, 15);
            Assert.AreEqual("C", score.VolumeGrade);
        }

        [Test]
        public void RecognizableOverfilledDrinkDoesNotCollapseToZero()
        {
            var order = SensitiveBaristaRules.Orders[0];
            var overfilledLatte = new IngredientAmounts(42f, 50f, 0f, 152f, 0f);

            var score = SensitiveBaristaRules.Score(order, overfilledLatte, 0f, 1);

            Assert.GreaterOrEqual(score.RoundScore, 24);
            Assert.Less(score.RoundScore, 60);
        }

        [Test]
        public void IngredientTotalIsVolumeBasedAndIncludesIce()
        {
            var drink = new IngredientAmounts(18f, 12f, 30f, 20f, 4f);

            Assert.AreEqual(84f, drink.Total);
        }

        [Test]
        public void MissingPrimaryIngredientIsPenalized()
        {
            var order = SensitiveBaristaRules.Orders[0];
            var withPrimary = ScaleToIdeal(order);
            var missingMilk = new IngredientAmounts(withPrimary.Ice, withPrimary.Shot, withPrimary.Water, 0f, withPrimary.Syrup);

            var completeScore = SensitiveBaristaRules.Score(order, withPrimary, 0f, 1);
            var missingScore = SensitiveBaristaRules.Score(order, missingMilk, 0f, 1);

            Assert.Greater(completeScore.RoundScore, missingScore.RoundScore);
        }

        [Test]
        public void OrderPoolHasEnoughAbstractPromptsForARun()
        {
            Assert.AreEqual(10, SensitiveBaristaRules.RoundCount);
            Assert.GreaterOrEqual(SensitiveBaristaRules.Orders.Length, 12);
            Assert.GreaterOrEqual(SensitiveBaristaRules.GeneratedOrders.Length, 100);
        }

        [Test]
        public void RunOrdersComeFromGeneratedOrderPool()
        {
            var run = SensitiveBaristaRules.CreateRunOrders(SensitiveBaristaRules.RoundCount, 1234);

            Assert.AreEqual(SensitiveBaristaRules.RoundCount, run.Length);
            foreach (var order in run)
            {
                StringAssert.Contains(" - ", order.CustomerLine);
                StringAssert.Contains(order.MemoName, order.CustomerLine);
            }
        }

        [Test]
        public void DifferentSeedsCreateDifferentRuns()
        {
            var firstRun = SensitiveBaristaRules.CreateRunOrders(SensitiveBaristaRules.RoundCount, 1234);
            var secondRun = SensitiveBaristaRules.CreateRunOrders(SensitiveBaristaRules.RoundCount, 5678);

            var sameCount = 0;
            for (var index = 0; index < firstRun.Length; index += 1)
            {
                if (firstRun[index].CustomerLine == secondRun[index].CustomerLine)
                {
                    sameCount += 1;
                }
            }

            Assert.Less(sameCount, SensitiveBaristaRules.RoundCount);
        }

        [Test]
        public void OrdersUseMenuNameWithAdjustmentRequest()
        {
            foreach (var order in SensitiveBaristaRules.GeneratedOrders)
            {
                StringAssert.Contains(" - ", order.CustomerLine);
                StringAssert.Contains(order.MemoName, order.CustomerLine);
            }
        }

        [Test]
        public void RecipeMemoForHighlightsCurrentOrder()
        {
            var order = SensitiveBaristaRules.Orders[5];

            var memo = SensitiveBaristaRules.RecipeMemoFor(order);

            StringAssert.Contains(order.MemoName, memo);
            StringAssert.Contains("Base recipe:", memo);
            StringAssert.Contains("Request:", memo);
            StringAssert.Contains(order.TasteHint, memo);
        }

        [Test]
        public void ScoreIncludesReadableGrades()
        {
            var order = SensitiveBaristaRules.Orders[0];

            var score = SensitiveBaristaRules.Score(order, ScaleToIdeal(order), 0f, 1);

            Assert.AreEqual("A", score.BalanceGrade);
            Assert.AreEqual("A", score.VolumeGrade);
            Assert.AreEqual("A", score.CleanlinessGrade);
        }

        [Test]
        public void ExcessiveSyrupIsPenalized()
        {
            var order = SensitiveBaristaRules.Orders[6];
            var balanced = ScaleToIdeal(order);
            var tooSweet = new IngredientAmounts(5f, 12f, 0f, 24f, 60f);

            var balancedScore = SensitiveBaristaRules.Score(order, balanced, 0f, 2);
            var tooSweetScore = SensitiveBaristaRules.Score(order, tooSweet, 0f, 2);

            Assert.Greater(balancedScore.RoundScore, tooSweetScore.RoundScore);
            Assert.Greater(tooSweetScore.SyrupPenalty, 0f);
        }

        [Test]
        public void SyrupFreeOrdersPunishAddedSyrup()
        {
            var order = SensitiveBaristaRules.Orders[5];
            var baseDrink = ScaleToIdeal(order);
            var syrupy = new IngredientAmounts(
                baseDrink.Ice,
                baseDrink.Shot,
                baseDrink.Water - 12f,
                baseDrink.Milk,
                12f);

            var clean = SensitiveBaristaRules.Score(order, baseDrink, 0f, 5);
            var sweet = SensitiveBaristaRules.Score(order, syrupy, 0f, 5);

            Assert.Greater(clean.RoundScore, sweet.RoundScore);
            Assert.Greater(sweet.SyrupPenalty, 0f);
        }

        private static IngredientAmounts ScaleToIdeal(BaristaOrder order)
        {
            var scale = order.IdealTotal / order.Target.Total;
            return new IngredientAmounts(
                order.Target.Ice * scale,
                order.Target.Shot * scale,
                order.Target.Water * scale,
                order.Target.Milk * scale,
                order.Target.Syrup * scale);
        }
    }
}
