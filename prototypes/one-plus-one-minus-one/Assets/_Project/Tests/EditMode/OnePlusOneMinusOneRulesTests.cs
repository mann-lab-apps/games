using MannLab.Games.OnePlusOneMinusOne;
using NUnit.Framework;

namespace MannLab.Games.OnePlusOneMinusOne.Tests
{
    public sealed class OnePlusOneMinusOneRulesTests
    {
        [Test]
        public void ConcatenatesAdjacentNumberTokens()
        {
            AssertValue(11, "1", "1");
            AssertValue(11, "11");
            AssertValue(111, "1", "11");
            AssertValue(111, "11", "1");
            AssertValue(111, "1", "1", "1");
        }

        [Test]
        public void AppliesOperatorPrecedence()
        {
            AssertValue(12, "1", "+", "1", "×", "11");
            AssertValue(2, "1", "+", "11", "/", "11");
            AssertValue(2, "1", "+", "1", "×", "1");
            AssertValue(1, "1", "+", "1", "-", "1", "×", "1", "/", "1");
            AssertValue(1, "1", "+", "1", "-", "1", "x", "1", "/", "1");
            AssertValue(1, "1", "+", "1", "-", "1", "*", "1", "/", "1");
        }

        [TestCase(13)]
        [TestCase(14)]
        [TestCase(86)]
        public void PrecedenceLessonsDistinguishLeftToRightCalculation(int roundIndex)
        {
            var round = OnePlusOneMinusOneRules.GoalModeRounds[roundIndex];
            var symbols = ExtractSampleSymbols(round.SampleSolution);
            Assert.AreEqual(5, symbols.Length);
            Assert.AreEqual("+", symbols[1]);
            var firstSum = double.Parse(symbols[0]) + double.Parse(symbols[2]);
            var last = double.Parse(symbols[4]);
            var leftToRight = symbols[3] == "/" ? firstSum / last : firstSum * last;
            var correct = OnePlusOneMinusOneRules.Evaluate(symbols);
            Assert.IsTrue(correct.IsValid);
            Assert.That(System.Math.Abs(correct.Value - leftToRight),
                Is.GreaterThan(OnePlusOneMinusOneRules.TargetTolerance),
                "The precedence lesson must change the result, not only mention the rule.");
        }

        [Test]
        public void RecognizesStickPosesAsTokens()
        {
            Assert.AreEqual("1", OnePlusOneMinusOneRules.RecognizeToken(new[] { StickPose.CenterVertical }));
            Assert.AreEqual("-", OnePlusOneMinusOneRules.RecognizeToken(new[] { StickPose.CenterHorizontal }));
            Assert.AreEqual("/", OnePlusOneMinusOneRules.RecognizeToken(new[] { StickPose.CenterSlash }));
            Assert.AreEqual("11", OnePlusOneMinusOneRules.RecognizeToken(new[] { StickPose.LeftVertical, StickPose.RightVertical }));
            Assert.AreEqual("+", OnePlusOneMinusOneRules.RecognizeToken(new[] { StickPose.CenterVertical, StickPose.CenterHorizontal }));
            Assert.AreEqual("×", OnePlusOneMinusOneRules.RecognizeToken(new[] { StickPose.CenterSlash, StickPose.CenterBackslash }));
            Assert.AreEqual(string.Empty, OnePlusOneMinusOneRules.RecognizeToken(new[] { StickPose.CenterVertical, StickPose.CenterSlash }));
        }

        [Test]
        public void SolvesAllSeedGoalRounds()
        {
            Assert.AreEqual(100, OnePlusOneMinusOneRules.GoalModeRounds.Length);
            Assert.AreEqual(1, OnePlusOneMinusOneRules.GoalModeRounds[0].StickCount);
            Assert.AreEqual(4, OnePlusOneMinusOneRules.GoalModeRounds[1].StickCount);
            Assert.AreEqual(3, OnePlusOneMinusOneRules.GoalModeRounds[2].StickCount);

            foreach (var round in OnePlusOneMinusOneRules.GoalModeRounds)
            {
                var symbols = ExtractSampleSymbols(round.SampleSolution);
                Assert.IsTrue(
                    OnePlusOneMinusOneRules.IsRoundSolved(round, symbols, out _, out var reason),
                    $"{round.RoundName}: {reason}");
            }
        }

        [Test]
        public void EqualPrecedenceOperatorsAssociateLeftToRight()
        {
            AssertValue(9, "11", "-", "1", "-", "1");
            AssertValue(111, "111", "/", "11", "*", "11");
            AssertValue(1d / 121d, "1", "/", "11", "/", "11");
            AssertValue(-10, "1", "-", "11");
        }

        [TestCase(40, 41)]
        [TestCase(44, 45)]
        [TestCase(62, 64)]
        [TestCase(68, 69)]
        public void ReviewedRoundsDoNotRepeatIdenticalPlayerConstraints(int firstIndex, int secondIndex)
        {
            var first = OnePlusOneMinusOneRules.GoalModeRounds[firstIndex];
            var second = OnePlusOneMinusOneRules.GoalModeRounds[secondIndex];
            var firstEquality = first.SampleSolution.Contains("=");
            var secondEquality = second.SampleSolution.Contains("=");
            var sameTarget = firstEquality && secondEquality ||
                !firstEquality && !secondEquality &&
                System.Math.Abs(first.TargetValue - second.TargetValue) < OnePlusOneMinusOneRules.TargetTolerance;
            Assert.IsFalse(first.StickCount == second.StickCount &&
                first.SlotTypes.Length == second.SlotTypes.Length && sameTarget,
                $"Rounds {firstIndex + 1}/{secondIndex + 1} need different player constraints, not just different sample ordering.");
        }

        [Test]
        public void AcceptsPlayerEqualityInsteadOfTutorialMultiplySample()
        {
            Assert.IsTrue(OnePlusOneMinusOneRules.IsRoundSolved(
                OnePlusOneMinusOneRules.GoalModeRounds[4], new[] { "1", "=", "1" }, out _, out var reason), reason);
        }

        [TestCase(13, "11 / 11 = 1")]
        [TestCase(14, "11 × 1 = 11")]
        [TestCase(64, "111 × 1 = 111 / 1")]
        [TestCase(69, "111 = 111 / 1")]
        [TestCase(86, "111 / 1 = 111")]
        public void ReviewedRoundsStillAcceptAlternativeEqualities(int roundIndex, string expression)
        {
            var round = OnePlusOneMinusOneRules.GoalModeRounds[roundIndex];
            var symbols = ExtractSampleSymbols(expression);
            Assert.AreEqual(round.SlotTypes.Length, symbols.Length);
            Assert.AreEqual(round.StickCount, OnePlusOneMinusOneRules.UsedSticks(symbols));
            Assert.IsTrue(OnePlusOneMinusOneRules.IsRoundSolved(round, symbols, out _, out var reason), reason);
        }

        [TestCase("= 1")]
        [TestCase("1 =")]
        [TestCase("1 = 1 = 1")]
        [TestCase("1 = 11")]
        [TestCase("1 + = 1")]
        [TestCase("1 = + 1")]
        public void RejectsMalformedOrUnbalancedEquality(string expression)
        {
            var symbols = ExtractSampleSymbols(expression);
            var round = new PuzzleRoundData("Equality boundary", 1,
                OnePlusOneMinusOneRules.UsedSticks(symbols), OnePlusOneMinusOneRules.AnySlots(symbols.Length),
                symbols, string.Empty, expression);
            Assert.IsFalse(OnePlusOneMinusOneRules.IsRoundSolved(round, symbols, out _, out var reason));
            Assert.IsNotEmpty(reason);
        }

        [Test]
        public void RejectsAnEmptySlotInsideAnOtherwiseValidExpression()
        {
            AssertInvalid("1", string.Empty, "+", "1");
            AssertInvalid("1", "+", null);
        }

        [Test]
        public void ReplayingCompletedFinalRoundNeverOffersReleaseAd()
        {
            var decision = OnePlusOneMinusOneController.CalculateInterstitialDecision(99, 99, 0, false, true);
            Assert.IsFalse(decision.ShouldOffer);
            Assert.AreEqual("replay_round", decision.Reason);
        }

        [Test]
        public void RejectsBadSyntax()
        {
            AssertInvalid("+", "1");
            AssertInvalid("1", "+");
            AssertInvalid("1", "+", "×", "1");
        }

        private static void AssertValue(double expected, params string[] symbols)
        {
            var result = OnePlusOneMinusOneRules.Evaluate(symbols);
            Assert.IsTrue(result.IsValid, result.Error);
            Assert.AreEqual(expected, result.Value, OnePlusOneMinusOneRules.TargetTolerance);
        }

        private static void AssertInvalid(params string[] symbols)
        {
            var result = OnePlusOneMinusOneRules.Evaluate(symbols);
            Assert.IsFalse(result.IsValid);
        }

        private static string[] ExtractSampleSymbols(string sampleSolution)
        {
            return sampleSolution.Split(new[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
        }
    }
}
