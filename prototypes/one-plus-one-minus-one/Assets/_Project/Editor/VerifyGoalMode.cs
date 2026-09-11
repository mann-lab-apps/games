using System;
using System.Collections.Generic;
using MannLab.Games.OnePlusOneMinusOne;
using UnityEditor;

namespace MannLab.Games.OnePlusOneMinusOne.EditorTools
{
    public static class VerifyGoalMode
    {
        public static void Run()
        {
            AssertValue(11, "1", "1");
            AssertValue(11, "11");
            AssertValue(111, "1", "11");
            AssertValue(111, "11", "1");
            AssertValue(111, "111");
            AssertValue(2, "1", "+", "1", "×", "1");
            AssertValue(1, "1", "*", "1");
            AssertValue(1, "1", "+", "1", "-", "1", "×", "1", "/", "1");
            AssertValue(1, "1", "+", "1", "-", "1", "x", "1", "/", "1");
            AssertValue(1, "1", "+", "1", "-", "1", "*", "1", "/", "1");
            AssertRoundStickCount(0, 1);
            AssertRoundStickCount(1, 4);
            AssertRoundStickCount(2, 3);
            AssertRecognizes("1", StickPose.CenterVertical);
            AssertRecognizes("-", StickPose.CenterHorizontal);
            AssertRecognizes("/", StickPose.CenterSlash);
            AssertRecognizes("11", StickPose.LeftVertical, StickPose.RightVertical);
            AssertRecognizes("111", StickPose.LeftVertical, StickPose.CenterVertical, StickPose.RightVertical);
            AssertRecognizes("+", StickPose.CenterVertical, StickPose.CenterHorizontal);
            AssertRecognizes("×", StickPose.CenterSlash, StickPose.CenterBackslash);
            AssertRecognizes("*", StickPose.CenterVertical, StickPose.CenterSlash, StickPose.CenterBackslash);
            AssertRecognizes("*", StickPose.CenterHorizontal, StickPose.CenterSlash, StickPose.CenterBackslash);
            AssertRecognizes("=", StickPose.TopHorizontal, StickPose.BottomHorizontal);
            AssertRoundSolved(4, "1", "=", "1");
            AssertRoundCount(100);
            AssertRoundStickCount(7, 3);
            AssertRoundStickCount(8, 5);
            AssertRoundStickCount(29, 11);
            AssertRoundStickCount(99, 11);
            AssertReleaseUiAndAdPolicy();

            var sampleOwners = new Dictionary<string, int>();
            for (var roundIndex = 0; roundIndex < OnePlusOneMinusOneRules.GoalModeRounds.Length; roundIndex++)
            {
                var round = OnePlusOneMinusOneRules.GoalModeRounds[roundIndex];
                var symbols = ExtractSampleSymbols(round.SampleSolution);
                AssertNoUnexpectedDuplicateSample(sampleOwners, roundIndex, round);
                AssertSampleShape(round, symbols);
                AssertLayoutPlan(round, symbols);
                if (!OnePlusOneMinusOneRules.IsRoundSolved(round, symbols, out _, out var reason))
                {
                    throw new InvalidOperationException($"{round.RoundName} failed: {reason}");
                }

                AssertSampleTargetMatchesRound(round, symbols);
                AssertTutorialScope(roundIndex, round);
                AssertPostTutorialTitleDoesNotSpoil(roundIndex, round);
            }

            AssertRoundQualityBands();
            AssetDatabase.Refresh();
        }

        private static void AssertRoundQualityBands()
        {
            AssertRoundBandContains(15, 29, "early puzzles", "=");
            AssertRoundBandContains(15, 29, "early puzzles", "*");
            AssertRoundBandContains(15, 29, "early puzzles", "×", "x");
            AssertRoundBandContains(30, 49, "mid puzzles", "=");
            AssertRoundBandContains(30, 49, "mid puzzles", "*");
            AssertRoundBandContains(30, 49, "mid puzzles", "×", "x");
            AssertRoundBandContains(50, 79, "equation focus", "=");
            AssertRoundBandContains(50, 79, "equation focus", "*");
            AssertRoundBandContains(50, 79, "equation focus", "×", "x");
            AssertRoundBandContains(80, 99, "finale", "=");
            AssertRoundBandContains(80, 99, "finale", "*");
            AssertRoundBandContains(80, 99, "finale", "×", "x");
        }

        private static void AssertRoundBandContains(int startRoundIndex, int endRoundIndex, string label, params string[] symbols)
        {
            for (var roundIndex = startRoundIndex; roundIndex <= endRoundIndex; roundIndex++)
            {
                var roundSymbols = ExtractSampleSymbols(OnePlusOneMinusOneRules.GoalModeRounds[roundIndex].SampleSolution);
                for (var symbolIndex = 0; symbolIndex < symbols.Length; symbolIndex++)
                {
                    if (Array.IndexOf(roundSymbols, symbols[symbolIndex]) >= 0)
                    {
                        return;
                    }
                }
            }

            throw new InvalidOperationException(
                $"Round quality band '{label}' is missing one of: {string.Join(", ", symbols)}.");
        }

        private static void AssertReleaseUiAndAdPolicy()
        {
            if (OnePlusOneMinusOneController.ReleaseRoundSelectPageSize != 12)
            {
                throw new InvalidOperationException("Round select should stay paged at 12 rounds per page.");
            }

            var pageCount = (OnePlusOneMinusOneRules.GoalModeRounds.Length + OnePlusOneMinusOneController.ReleaseRoundSelectPageSize - 1) /
                            OnePlusOneMinusOneController.ReleaseRoundSelectPageSize;
            if (pageCount != 9)
            {
                throw new InvalidOperationException($"Expected 9 round-select pages for 100 rounds, got {pageCount}.");
            }

            if (OnePlusOneMinusOneController.ReleaseRoundClearInterstitialInterval != 10)
            {
                throw new InvalidOperationException("Release interstitial cadence should be 10 cleared rounds.");
            }

            if (OnePlusOneMinusOneController.ReleaseInterstitialGraceRoundCount != 5)
            {
                throw new InvalidOperationException("The first 5 rounds should stay protected from interstitials.");
            }

            if (OnePlusOneMinusOneController.ReleaseInterstitialMaxFailuresBeforeSkip != 3)
            {
                throw new InvalidOperationException("Hard clears should skip interstitials after 3 failed checks.");
            }

            if (OnePlusOneMinusOneController.ReleaseSfxVolume <= 0f ||
                OnePlusOneMinusOneController.ReleaseSfxVolume > 0.35f)
            {
                throw new InvalidOperationException("Release SFX volume should stay present but gentle.");
            }

            if (OnePlusOneMinusOneController.ReleaseSfxCooldownSeconds <= 0f ||
                OnePlusOneMinusOneController.ReleaseSfxCooldownSeconds > 0.12f)
            {
                throw new InvalidOperationException("Release SFX cooldown should prevent noisy overlap without making feedback feel laggy.");
            }

            if (OnePlusOneMinusOneController.ReleaseWebGlReferenceResolution.x > 720f ||
                OnePlusOneMinusOneController.ReleaseWebGlReferenceResolution.y > 1280f)
            {
                throw new InvalidOperationException("WebGL reference resolution should keep the UI readable on mobile browser viewports.");
            }

            AssertStartupFlowPlans();
            AssertInterstitialDecisionPlans();
            AssertRoundSelectLayoutPlans();
        }

        private static void AssertStartupFlowPlans()
        {
            AssertStartupFlow(-10, 0, 0, false);
            AssertStartupFlow(0, 0, 0, false);
            AssertStartupFlow(1, 1, 1, true);
            AssertStartupFlow(42, 42, 42, true);
            AssertStartupFlow(1000, 99, 99, true);
        }

        private static void AssertStartupFlow(
            int savedHighestUnlockedRoundIndex,
            int expectedHighestUnlockedRoundIndex,
            int expectedStartRoundIndex,
            bool expectedShowRoundSelect)
        {
            var plan = OnePlusOneMinusOneController.CalculateStartupFlowPlan(savedHighestUnlockedRoundIndex);
            if (plan.HighestUnlockedRoundIndex != expectedHighestUnlockedRoundIndex ||
                plan.StartRoundIndex != expectedStartRoundIndex ||
                plan.ShowRoundSelect != expectedShowRoundSelect)
            {
                throw new InvalidOperationException(
                    $"Unexpected startup plan for saved progress {savedHighestUnlockedRoundIndex}: " +
                    $"highest={plan.HighestUnlockedRoundIndex}, start={plan.StartRoundIndex}, " +
                    $"roundSelect={plan.ShowRoundSelect}");
            }
        }

        private static void AssertInterstitialDecisionPlans()
        {
            AssertInterstitialDecision(0, 0, 0, false, "early_round");
            AssertInterstitialDecision(4, 4, 0, false, "early_round");
            AssertInterstitialDecision(5, 5, 0, false, "cadence");
            AssertInterstitialDecision(9, 9, 0, true, "round_milestone");
            AssertInterstitialDecision(9, 10, 0, false, "replay_round");
            AssertInterstitialDecision(19, 19, 3, false, "hard_clear");
            AssertInterstitialDecision(19, 19, 0, true, "round_milestone");
            AssertInterstitialDecision(99, 99, 0, true, "round_milestone");
            AssertInterstitialDecision(0, 99, 5, true, "forced_test_ads", true);
        }

        private static void AssertInterstitialDecision(
            int roundIndex,
            int highestUnlockedRoundIndex,
            int failureCount,
            bool expectedShouldOffer,
            string expectedReason,
            bool forceTestAds = false)
        {
            var decision = OnePlusOneMinusOneController.CalculateInterstitialDecision(
                roundIndex,
                highestUnlockedRoundIndex,
                failureCount,
                forceTestAds);
            if (decision.ShouldOffer != expectedShouldOffer ||
                decision.Reason != expectedReason)
            {
                throw new InvalidOperationException(
                    $"Unexpected interstitial decision for roundIndex={roundIndex}, " +
                    $"highestUnlocked={highestUnlockedRoundIndex}, failures={failureCount}, " +
                    $"forceTestAds={forceTestAds}: shouldOffer={decision.ShouldOffer}, reason={decision.Reason}");
            }
        }

        private static void AssertRoundSelectLayoutPlans()
        {
            var safeSizes = new[]
            {
                new[] { 320f, 568f },
                new[] { 390f, 844f },
                new[] { 430f, 932f },
                new[] { 412f, 915f },
                new[] { 768f, 1024f }
            };

            for (var i = 0; i < safeSizes.Length; i++)
            {
                var safeWidth = safeSizes[i][0];
                var safeHeight = safeSizes[i][1];
                var plan = OnePlusOneMinusOneController.CalculateRoundSelectLayoutPlan(safeWidth, safeHeight);
                if (plan.PanelWidth > safeWidth + 0.1f)
                {
                    throw new InvalidOperationException($"Round select panel exceeds safe width {safeWidth}: {plan.PanelWidth}");
                }

                if (plan.PanelHeight > safeHeight + 0.1f)
                {
                    throw new InvalidOperationException($"Round select panel exceeds safe height {safeHeight}: {plan.PanelHeight}");
                }

                if (plan.CellWidth < OnePlusOneMinusOneController.ReleaseMinRoundSelectCellWidth - 0.1f ||
                    plan.CellHeight < OnePlusOneMinusOneController.ReleaseMinRoundSelectCellHeight - 0.1f)
                {
                    throw new InvalidOperationException($"Round select cells are too small at {safeWidth}x{safeHeight}: {plan.CellWidth}x{plan.CellHeight}");
                }

                var gridWidth = plan.CellWidth * 3f + plan.Spacing * 2f;
                if (gridWidth > plan.PanelWidth - plan.InnerPadding + 0.1f)
                {
                    throw new InvalidOperationException($"Round select grid overflows panel at {safeWidth}x{safeHeight}: {gridWidth}");
                }

                if (plan.GridHeight > plan.PanelHeight - 230f + 0.1f)
                {
                    throw new InvalidOperationException($"Round select grid leaves too little room for title/pager/close at {safeWidth}x{safeHeight}: {plan.GridHeight}");
                }
            }
        }

        private static void AssertTutorialScope(int roundIndex, PuzzleRoundData round)
        {
            if (roundIndex < 15)
            {
                if (string.IsNullOrWhiteSpace(round.TutorialMessage))
                {
                    throw new InvalidOperationException($"{round.RoundName} should keep tutorial copy.");
                }

                return;
            }

            if (!string.IsNullOrWhiteSpace(round.TutorialMessage))
            {
                throw new InvalidOperationException($"{round.RoundName} should not show tutorial copy after round 15.");
            }
        }

        private static void AssertPostTutorialTitleDoesNotSpoil(int roundIndex, PuzzleRoundData round)
        {
            if (roundIndex < 15)
            {
                return;
            }

            var forbiddenWords = new[] { "Star", "Cross", "Slash", "Divide", "Times", "Multiply", "Plus", "Minus", "Eleven", "Triple", "Hundred", "Ninety", "Twenty", "Twelve", "Ten", "One" };
            for (var i = 0; i < forbiddenWords.Length; i++)
            {
                if (round.RoundName.IndexOf(forbiddenWords[i], StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    throw new InvalidOperationException($"{round.RoundName} is too direct for a post-tutorial title.");
                }
            }
        }

        private static void AssertNoUnexpectedDuplicateSample(
            Dictionary<string, int> sampleOwners,
            int roundIndex,
            PuzzleRoundData round)
        {
            if (!sampleOwners.TryGetValue(round.SampleSolution, out var previousRoundIndex))
            {
                sampleOwners.Add(round.SampleSolution, roundIndex);
                return;
            }

            if (previousRoundIndex == 29 && roundIndex == 99)
            {
                return;
            }

            throw new InvalidOperationException(
                $"{round.RoundName} repeats round {previousRoundIndex + 1}: {round.SampleSolution}");
        }

        private static void AssertRoundSolved(int roundIndex, params string[] symbols)
        {
            var round = OnePlusOneMinusOneRules.GoalModeRounds[roundIndex];
            if (!OnePlusOneMinusOneRules.IsRoundSolved(round, symbols, out _, out var reason))
            {
                throw new InvalidOperationException($"{round.RoundName} should accept {string.Join(" ", symbols)}: {reason}");
            }
        }

        private static void AssertValue(double expected, params string[] symbols)
        {
            var result = OnePlusOneMinusOneRules.Evaluate(symbols);
            if (!result.IsValid)
            {
                throw new InvalidOperationException(result.Error);
            }

            if (Math.Abs(result.Value - expected) >= OnePlusOneMinusOneRules.TargetTolerance)
            {
                throw new InvalidOperationException($"Expected {expected}, got {result.Value}");
            }
        }

        private static void AssertRoundStickCount(int roundIndex, int expected)
        {
            var round = OnePlusOneMinusOneRules.GoalModeRounds[roundIndex];
            if (round.StickCount != expected)
            {
                throw new InvalidOperationException($"{round.RoundName} should start with {expected} sticks, got {round.StickCount}");
            }
        }

        private static void AssertRoundCount(int expected)
        {
            var actual = OnePlusOneMinusOneRules.GoalModeRounds.Length;
            if (actual != expected)
            {
                throw new InvalidOperationException($"Expected {expected} rounds, got {actual}");
            }
        }

        private static void AssertRecognizes(string expected, params StickPose[] poses)
        {
            var actual = OnePlusOneMinusOneRules.RecognizeToken(poses);
            if (actual != expected)
            {
                throw new InvalidOperationException($"Expected poses to read as {expected}, got {actual}");
            }
        }

        private static void AssertSampleShape(PuzzleRoundData round, string[] symbols)
        {
            if (symbols.Length != round.SlotTypes.Length)
            {
                throw new InvalidOperationException($"{round.RoundName} should have {symbols.Length} slots for sample, got {round.SlotTypes.Length}");
            }

            var usedSticks = OnePlusOneMinusOneRules.UsedSticks(symbols);
            if (usedSticks != round.StickCount)
            {
                throw new InvalidOperationException($"{round.RoundName} sample should use {round.StickCount} sticks, got {usedSticks}");
            }

            for (var i = 0; i < symbols.Length; i++)
            {
                OnePlusOneMinusOneRules.GetToken(symbols[i]);
                if (!OnePlusOneMinusOneRules.IsTokenAllowed(round, symbols[i]))
                {
                    throw new InvalidOperationException($"{round.RoundName} disallows sample token {symbols[i]}");
                }
            }

            if (symbols.Length > 11)
            {
                throw new InvalidOperationException($"{round.RoundName} has {symbols.Length} slots; keep release rounds at 11 or fewer");
            }
        }

        private static void AssertLayoutPlan(PuzzleRoundData round, string[] symbols)
        {
            var usesFixedTarget = round.SampleSolution.IndexOf("=", StringComparison.Ordinal) < 0;
            AssertLayoutPlanForWidth(round, symbols.Length, usesFixedTarget, 320f, OnePlusOneMinusOneController.ReleaseCompactMaxEquationRows, OnePlusOneMinusOneController.ReleaseCompactMinEquationSlotWidth, OnePlusOneMinusOneController.ReleaseCompactMinEquationSlotHeight, 500f);
            AssertLayoutPlanForWidth(round, symbols.Length, usesFixedTarget, 390f, OnePlusOneMinusOneController.ReleaseCompactMaxEquationRows, 96f, 108f, 500f);
            AssertLayoutPlanForWidth(round, symbols.Length, usesFixedTarget, 488f, OnePlusOneMinusOneController.ReleaseMaxEquationRows, OnePlusOneMinusOneController.ReleaseMinEquationSlotWidth, OnePlusOneMinusOneController.ReleaseMinEquationSlotHeight, 470f);
        }

        private static void AssertLayoutPlanForWidth(
            PuzzleRoundData round,
            int slotCount,
            bool usesFixedTarget,
            float width,
            int maxRows,
            float minSlotWidth,
            float minSlotHeight,
            float maxContentHeight)
        {
            var plan = OnePlusOneMinusOneController.CalculateEquationLayoutPlan(slotCount, usesFixedTarget, width);
            if (plan.Rows > maxRows)
            {
                throw new InvalidOperationException($"{round.RoundName} uses {plan.Rows} expression rows at {width}px.");
            }

            if (plan.SlotWidth < minSlotWidth - 0.1f)
            {
                throw new InvalidOperationException($"{round.RoundName} slot width is too small at {width}px: {plan.SlotWidth}");
            }

            if (plan.SlotHeight < minSlotHeight - 0.1f)
            {
                throw new InvalidOperationException($"{round.RoundName} slot height is too small at {width}px: {plan.SlotHeight}");
            }

            if (plan.TotalWidth > width + 0.1f)
            {
                throw new InvalidOperationException($"{round.RoundName} layout exceeds portrait width {width}px: {plan.TotalWidth}");
            }

            if (plan.ContentHeight > maxContentHeight)
            {
                throw new InvalidOperationException($"{round.RoundName} expression stack is too tall at {width}px: {plan.ContentHeight}");
            }

            var aspect = plan.SlotHeight / Mathf.Max(1f, plan.SlotWidth);
            if (aspect < 1.1f || aspect > 1.24f)
            {
                throw new InvalidOperationException($"{round.RoundName} slot aspect is unstable at {width}px: {aspect}");
            }
        }

        private static void AssertSampleTargetMatchesRound(PuzzleRoundData round, string[] symbols)
        {
            var equalsIndex = Array.IndexOf(symbols, "=");
            var symbolsToEvaluate = symbols;
            if (equalsIndex >= 0)
            {
                symbolsToEvaluate = new string[equalsIndex];
                Array.Copy(symbols, symbolsToEvaluate, equalsIndex);
            }

            var result = OnePlusOneMinusOneRules.Evaluate(symbolsToEvaluate);
            if (!result.IsValid)
            {
                throw new InvalidOperationException($"{round.RoundName} target sample is invalid: {result.Error}");
            }

            if (Math.Abs(result.Value - round.TargetValue) >= OnePlusOneMinusOneRules.TargetTolerance)
            {
                throw new InvalidOperationException($"{round.RoundName} target should be {result.Value}, got {round.TargetValue}");
            }
        }

        private static string[] ExtractSampleSymbols(string sampleSolution)
        {
            return sampleSolution.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        }
    }
}
