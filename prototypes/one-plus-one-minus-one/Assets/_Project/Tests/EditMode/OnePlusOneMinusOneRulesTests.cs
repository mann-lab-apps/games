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
            AssertValue(2, "1", "+", "1", "×", "1");
            AssertValue(1, "1", "+", "1", "-", "1", "×", "1", "/", "1");
            AssertValue(1, "1", "+", "1", "-", "1", "x", "1", "/", "1");
            AssertValue(1, "1", "+", "1", "-", "1", "*", "1", "/", "1");
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
