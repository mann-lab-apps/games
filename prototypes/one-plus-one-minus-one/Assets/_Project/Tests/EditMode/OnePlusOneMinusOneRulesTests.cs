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
        public void GoalRoundTargetsStayIntegerForExactAcceptance()
        {
            foreach (var round in OnePlusOneMinusOneRules.GoalModeRounds)
            {
                Assert.That(round.TargetValue, Is.EqualTo(System.Math.Round(round.TargetValue)),
                    $"{round.RoundName} must keep an integer target; fractional target comparisons need explicit exact-rational support.");
            }
        }

        [Test]
        public void EverySlotCountPreservesAspectWhenHeightIsCapped()
        {
            foreach (var width in new[] { 320f, 390f, 488f, 720f })
            for (var count = 1; count <= 11; count++)
            foreach (var fixedTarget in new[] { false, true })
            {
                var plan = OnePlusOneMinusOneController.CalculateEquationLayoutPlan(count, fixedTarget, width);
                Assert.That(plan.SlotHeight / plan.SlotWidth, Is.InRange(1.1f, 1.25f),
                    $"{count} slots at {width}, fixed target {fixedTarget}");
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
        [TestCase(56, 59)]
        [TestCase(62, 64)]
        [TestCase(68, 69)]
        public void ReviewedRoundsDoNotRepeatIdenticalPlayerConstraints(int firstIndex, int secondIndex)
        {
            var first = OnePlusOneMinusOneRules.GoalModeRounds[firstIndex];
            var second = OnePlusOneMinusOneRules.GoalModeRounds[secondIndex];
            var sameTarget = System.Math.Abs(first.TargetValue - second.TargetValue) < OnePlusOneMinusOneRules.TargetTolerance;
            Assert.IsFalse(first.StickCount == second.StickCount &&
                first.SlotTypes.Length == second.SlotTypes.Length && sameTarget,
                $"Rounds {firstIndex + 1}/{secondIndex + 1} need different player constraints, not just different sample ordering.");
        }

        [Test]
        public void RoundIdentityCrossMatrixMatchesActualRules()
        {
            var rounds = OnePlusOneMinusOneRules.GoalModeRounds;
            const string title = "1 + 1 - 1 × 1 / 1";
            Assert.AreEqual(title, rounds[29].SampleSolution);
            Assert.AreEqual(title, rounds[99].SampleSolution);
            var report = new RoundIdentityReport { rows = new RoundIdentityRow[rounds.Length] };
            for (var source = 0; source < rounds.Length; source++)
            {
                var symbols = ExtractSampleSymbols(rounds[source].SampleSolution);
                var accepted = new System.Collections.Generic.List<int>();
                for (var target = 0; target < rounds.Length; target++)
                {
                    if (OnePlusOneMinusOneRules.IsRoundSolved(rounds[target], symbols, out _, out _))
                        accepted.Add(target + 1);
                    if (target <= source || source == 29 && target == 99) continue;
                    var identical = rounds[source].SlotTypes.Length == rounds[target].SlotTypes.Length &&
                        rounds[source].StickCount == rounds[target].StickCount &&
                        System.Math.Abs(rounds[source].TargetValue - rounds[target].TargetValue) < OnePlusOneMinusOneRules.TargetTolerance;
                    Assert.IsFalse(identical, $"Unexplained identical constraints: {source + 1}/{target + 1}");
                }
                Assert.Contains(source + 1, accepted);
                report.rows[source] = new RoundIdentityRow { source = source + 1, expression = rounds[source].SampleSolution, acceptedBy = accepted.ToArray() };
            }
            // A separate Node check compares all 10,000 pairs against this native evaluator output.
            var outputPath = System.IO.Path.Combine(UnityEngine.Application.dataPath, "../Library/round-identity-matrix.json");
            System.IO.File.WriteAllText(outputPath, UnityEngine.JsonUtility.ToJson(report, true));
        }

        [System.Serializable]
        private sealed class RoundIdentityReport { public RoundIdentityRow[] rows; }
        [System.Serializable]
        private sealed class RoundIdentityRow { public int source; public string expression; public int[] acceptedBy; }

        [Test]
        public void AcceptsPlayerEqualityInsteadOfTutorialMultiplySample()
        {
            Assert.IsTrue(OnePlusOneMinusOneRules.IsRoundSolved(
                OnePlusOneMinusOneRules.GoalModeRounds[4], new[] { "1", "=", "1" }, out _, out var reason), reason);
        }

        [TestCase(5, "1 1", "Too many boxes.")]
        [TestCase(6, "11", "Fill every box.")]
        public void RejectsWrongSlotCountWithMatchingStickCost(int index, string sample, string expectedReason)
        {
            var round = OnePlusOneMinusOneRules.GoalModeRounds[index];
            var symbols = ExtractSampleSymbols(sample);
            Assert.AreEqual(round.StickCount, OnePlusOneMinusOneRules.UsedSticks(symbols));
            Assert.IsFalse(OnePlusOneMinusOneRules.IsRoundSolved(round, symbols, out var result, out var reason));
            Assert.AreEqual("Slot count mismatch", result.Error);
            Assert.AreEqual(expectedReason, reason);
        }

        [Test]
        public void RejectsSmallNonzeroDivisionAsZeroTarget()
        {
            var round = OnePlusOneMinusOneRules.GoalModeRounds[47];
            var symbols = ExtractSampleSymbols("1 / 11 111");
            var result = OnePlusOneMinusOneRules.Evaluate(symbols);
            Assert.IsTrue(result.IsValid, result.Error);
            Assert.That(result.Value, Is.GreaterThan(0d).And.LessThan(OnePlusOneMinusOneRules.TargetTolerance));

            Assert.IsFalse(OnePlusOneMinusOneRules.IsRoundSolved(round, symbols, out _, out var reason));
            Assert.AreEqual("Result is 0.00009.", reason);
        }

        [Test]
        public void DisplaysTinyNonzeroResultWithoutRoundingToZero()
        {
            var symbols = ExtractSampleSymbols("1 / 111 111 111");
            var round = new PuzzleRoundData("Tiny nonzero", 0,
                OnePlusOneMinusOneRules.UsedSticks(symbols),
                OnePlusOneMinusOneRules.AnySlots(symbols.Length),
                symbols, string.Empty, string.Join(" ", symbols));

            Assert.IsFalse(OnePlusOneMinusOneRules.IsRoundSolved(round, symbols, out _, out var reason));
            Assert.AreEqual("Result is 1/111111111.", reason);
        }

        [Test]
        public void ExactArithmeticPreservesFractionCancellationAndEquality()
        {
            var fractionalProduct = ExtractSampleSymbols("1 / 11 * 11");
            var productRound = new PuzzleRoundData("Fraction product", 1,
                OnePlusOneMinusOneRules.UsedSticks(fractionalProduct),
                OnePlusOneMinusOneRules.AnySlots(fractionalProduct.Length),
                fractionalProduct, string.Empty, string.Join(" ", fractionalProduct));
            Assert.IsTrue(OnePlusOneMinusOneRules.IsRoundSolved(productRound, fractionalProduct, out _, out var reason), reason);

            var equalFractions = ExtractSampleSymbols("1 / 11 = 1 / 11");
            var equalityRound = new PuzzleRoundData("Fraction equality", 1,
                OnePlusOneMinusOneRules.UsedSticks(equalFractions),
                OnePlusOneMinusOneRules.AnySlots(equalFractions.Length),
                equalFractions, string.Empty, string.Join(" ", equalFractions));
            Assert.IsTrue(OnePlusOneMinusOneRules.IsRoundSolved(equalityRound, equalFractions, out _, out reason), reason);

            var unequalFractions = ExtractSampleSymbols("1 / 111 = 1 / 11");
            var unequalRound = new PuzzleRoundData("Fraction mismatch", 1,
                OnePlusOneMinusOneRules.UsedSticks(unequalFractions),
                OnePlusOneMinusOneRules.AnySlots(unequalFractions.Length),
                unequalFractions, string.Empty, string.Join(" ", unequalFractions));
            Assert.IsFalse(OnePlusOneMinusOneRules.IsRoundSolved(unequalRound, unequalFractions, out _, out reason));
            Assert.AreEqual("0.00901 is not 0.09091.", reason);
        }

        [TestCase(82, "1 1 = 1 × 11", 26, "111 1 / 11")]
        [TestCase(10, "1 = 1", 1, "1 1 - 1")]
        [TestCase(32, "1 1 = 1 1", 20, "11 1 - 1")]
        [TestCase(47, "1 1 - 1", 10, "1 11 - 111")]
        [TestCase(34, "1 / 1 + 1 1", 42, "1 1 - 11")]
        [TestCase(42, "1 = 1 × 1", 15, "1 1 + 1 / 1")]
        [TestCase(56, "111 - 111 = 11 - 11", 43, "11 1 + 11 = 11 + 111")]
        [TestCase(74, "11 + 1 = 11 / 1 + 1", 54, "111 = 111 + 1 1 - 11")]
        [TestCase(87, "1 11 = 1 × 111", 81, "111 - 11 * 111 / 111")]
        [TestCase(37, "11 = 1 1", 17, "1 × 1 1")]
        [TestCase(38, "11 = 11", 19, "11 + 111")]
        [TestCase(51, "11 / 11 = 1", 13, "11 1 / 111")]
        [TestCase(69, "111 / 1 = 111", 86, "111 / 111 * 111")]
        [TestCase(89, "1 + 1 = 1 + 1", 18, "1 1 - 11 - 1")]
        [TestCase(75, "1 1 1 1 111 = 1 111 111", 63, "111 - 111 + 11 / 11 + 1")]
        [TestCase(94, "1 1 1 11 = 11 111", 21, "1 + 1 1 × 11 / 11 + 1")]
        [TestCase(95, "1 1 = 1 1 × 1", 24, "111 - 1 - 1 - 1 / 1 - 1")]
        [TestCase(71, "1 1 1 1 1 = 11 111", 28, "1 1 × 11 - 11 + 11 - 11")]
        [TestCase(72, "1 1 1 1 1 = 1 1 111", 53, "1 / 1 1 - 1 / 11 + 1")]
        [TestCase(76, "1 1 1 = 1 11 × 1", 30, "1 / 1 1 - 1 / 11 + 11")]
        [TestCase(92, "1 1 11 = 1 111", 41, "1 11 + 1 - 1 - 11 / 11")]
        [TestCase(70, "1 1 1 1 = 1 1 11", 57, "1 - 1 / 1 - 11 / 1 1")]
        [TestCase(91, "1 × 11 = 11", 14, "11 + 111 / 111 * 111")]
        [TestCase(98, "1 1 1 = 1 × 111", 58, "111 - 11 - 1 / 1 + 1 - 1")]
        [TestCase(25, "1 1 1 = 1 1 1", 12, "1 1 1 / 1 1 1")]
        [TestCase(93, "1 111 = 1 111 × 1", 64, "11 - 1 / 111 * 1 11 + 1")]
        public void ResourceRedesignSeparatesSharedAnswerWithoutTokenBans(int revised, string shared, int owner, string alternative)
        {
            var rounds = OnePlusOneMinusOneRules.GoalModeRounds;
            var sharedSymbols = ExtractSampleSymbols(shared);
            Assert.IsFalse(OnePlusOneMinusOneRules.IsRoundSolved(rounds[revised], sharedSymbols, out _, out _));
            Assert.IsTrue(OnePlusOneMinusOneRules.IsRoundSolved(rounds[owner], sharedSymbols, out _, out var reason), reason);
            Assert.IsTrue(OnePlusOneMinusOneRules.IsRoundSolved(rounds[revised], ExtractSampleSymbols(alternative), out _, out reason), reason);
        }

        [Test]
        public void LateMixedOperationsDifferFromLeftToRight()
        {
            var round = OnePlusOneMinusOneRules.GoalModeRounds[87];
            var symbols = ExtractSampleSymbols(round.SampleSolution);
            Assert.AreEqual(1, symbols.Length % 2);
            var folded = double.Parse(symbols[0]);
            for (var i = 1; i < symbols.Length; i += 2)
            {
                var right = double.Parse(symbols[i + 1]);
                switch (symbols[i])
                {
                    case "+": folded += right; break;
                    case "-": folded -= right; break;
                    case "/": folded /= right; break;
                    case "*": case "×": folded *= right; break;
                    default: Assert.Fail("This lesson needs alternating numbers/operators."); break;
                }
            }
            var correct = OnePlusOneMinusOneRules.Evaluate(symbols);
            Assert.IsTrue(correct.IsValid);
            Assert.AreEqual(100d, correct.Value);
            Assert.That(System.Math.Abs(correct.Value - folded),
                Is.GreaterThan(OnePlusOneMinusOneRules.TargetTolerance));
        }

        [TestCase(13, "11 / 11 = 1")]
        [TestCase(14, "11 × 1 = 11")]
        [TestCase(59, "111 = 1 * 111")]
        [TestCase(64, "111 × 1 = 111 / 1")]
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
