using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;

namespace MannLab.Games.OnePlusOneMinusOne
{
    public enum PuzzleTokenType
    {
        Number,
        Operator
    }

    public enum PuzzleSlotType
    {
        Any,
        Number,
        Operator
    }

    public enum StickPose
    {
        CenterVertical,
        LeftVertical,
        RightVertical,
        CenterHorizontal,
        TopHorizontal,
        BottomHorizontal,
        CenterSlash,
        CenterBackslash
    }

    [Serializable]
    public sealed class PuzzleTokenData
    {
        public string Symbol;
        public int StickCost;
        public PuzzleTokenType TokenType;
        public Color DisplayColor;
        public string Personality;

        public PuzzleTokenData(string symbol, int stickCost, PuzzleTokenType tokenType, Color displayColor, string personality)
        {
            Symbol = symbol;
            StickCost = stickCost;
            TokenType = tokenType;
            DisplayColor = displayColor;
            Personality = personality;
        }
    }

    [Serializable]
    public sealed class PuzzleRoundData
    {
        public string RoundName;
        public double TargetValue;
        public int StickCount;
        public PuzzleSlotType[] SlotTypes;
        public string[] AllowedTokens;
        public string TutorialMessage;
        public string SampleSolution;

        public PuzzleRoundData(
            string roundName,
            double targetValue,
            int stickCount,
            PuzzleSlotType[] slotTypes,
            string[] allowedTokens,
            string tutorialMessage,
            string sampleSolution)
        {
            RoundName = roundName;
            TargetValue = targetValue;
            StickCount = stickCount;
            SlotTypes = slotTypes;
            AllowedTokens = allowedTokens;
            TutorialMessage = tutorialMessage;
            SampleSolution = sampleSolution;
        }
    }

    public readonly struct EquationResult
    {
        public bool IsValid { get; }
        public double Value { get; }
        public string Error { get; }

        public EquationResult(bool isValid, double value, string error)
        {
            IsValid = isValid;
            Value = value;
            Error = error;
        }
    }

    public static class OnePlusOneMinusOneRules
    {
        public const double TargetTolerance = 0.0001d;

        public static readonly PuzzleTokenData[] Tokens =
        {
            new PuzzleTokenData("1", 1, PuzzleTokenType.Number, new Color32(248, 248, 242, 255), "steady"),
            new PuzzleTokenData("11", 2, PuzzleTokenType.Number, new Color32(255, 226, 135, 255), "packed"),
            new PuzzleTokenData("111", 3, PuzzleTokenType.Number, new Color32(255, 233, 160, 255), "packed"),
            new PuzzleTokenData("-", 1, PuzzleTokenType.Operator, new Color32(255, 176, 190, 255), "sleepy"),
            new PuzzleTokenData("/", 1, PuzzleTokenType.Operator, new Color32(205, 232, 118, 255), "tilted"),
            new PuzzleTokenData("=", 2, PuzzleTokenType.Operator, new Color32(255, 216, 121, 255), "balanced"),
            new PuzzleTokenData("+", 2, PuzzleTokenType.Operator, new Color32(118, 204, 255, 255), "chatty"),
            new PuzzleTokenData("×", 2, PuzzleTokenType.Operator, new Color32(92, 216, 190, 255), "sparkly"),
            new PuzzleTokenData("x", 2, PuzzleTokenType.Operator, new Color32(92, 216, 190, 255), "sparkly"),
            new PuzzleTokenData("*", 3, PuzzleTokenType.Operator, new Color32(134, 190, 248, 255), "sparkly")
        };

        private static readonly string[] AllPlayableTokens = { "1", "11", "111", "+", "-", "/", "×", "x", "*", "=" };

        public static readonly PuzzleRoundData[] GoalModeRounds = BuildGoalModeRounds();

        private static PuzzleRoundData[] BuildGoalModeRounds()
        {
            return new[]
            {
                Round("Hello, 1", "Drag the little stick into the box. It reads as 1.", "1"),
                Round("Two-Stick Plus", "Drop two sticks in one box. Shape them into +.", "1 + 1"),
                Round("Lazy Minus", "Lay one stick sideways to make -.", "1 - 1"),
                Round("Tilted Divide", "Tilt one stick to make /.", "1 / 1"),
                Round("Cross Multiply", "Cross two tilted sticks to make ×.", "1 × 1"),
                Round("Packed Eleven", "Pack two upright sticks together: 11.", "11"),
                Round("Neighbor Eleven", "Two neighboring 1 boxes can also read as 11.", "1 1"),
                Round("Triple One", "Pack three upright sticks together: 111.", "111"),
                Round("Star Multiply", "Three crossed sticks can become *.", "1 * 1"),
                Round("Eleven Joins", "Now mix a packed 11 into an equation.", "11 + 1"),
                Round("Make Ten", "Subtract a small friend from 11.", "11 - 1"),
                Round("Back To One", "The same number can divide itself.", "11 / 11"),
                Round("Zero Trick", "Add and subtract back to zero.", "1 - 1 + 1 - 1"),
                Round("Order Trick", "× acts before +.", "1 + 1 × 1"),
                Round("Divide Then Add", "/ acts before + too.", "1 + 1 / 1"),
                Puzzle("Little Choir", "1 + 1 + 1"),
                Puzzle("Mirror Drop", "11 - 11"),
                Puzzle("Quiet Match", "1 = 1"),
                Puzzle("Twin Paths", "1 + 1 = 1 + 1"),
                Puzzle("Soft Match", "11 = 11"),
                Puzzle("Small Turn", "1 + 1 - 1"),
                Puzzle("Side Path", "11 + 1 = 1 + 11"),
                Puzzle("High Step", "111 - 11"),
                Puzzle("Long Slide", "111 / 1 - 11"),
                Puzzle("Warmup Line", "1 + 1 - 1 × 1"),
                Puzzle("Soft Save", "1 + 1 - 1 / 1"),
                Puzzle("Hidden Path", "11 * 1 / 11"),
                Puzzle("High Fold", "111 / 111"),
                Puzzle("Short Balance", "1 + 1 - 1 = 1"),
                Puzzle("Familiar Shape", "1 + 1 - 1 × 1 / 1"),
                Puzzle("Busy Row", "1 + 1 + 1 + 1"),
                Puzzle("Soft Lift", "11 + 1 + 1"),
                Puzzle("Soft Dip", "11 - 1 - 1"),
                Puzzle("Small Loop", "1 + 11 / 11"),
                Puzzle("High Loop", "111 / 111 + 1"),
                Puzzle("Clean Slide", "111 / 111 + 11 + 1"),
                Puzzle("Almost There", "111 - 11 - 1"),
                Puzzle("Heavy Pair", "11 × 11"),
                Puzzle("Side By Side", "11 + 11"),
                Puzzle("High Trim", "111 - 1"),
                Puzzle("First Switch", "1 + 11 × 1"),
                Puzzle("Second Switch", "11 × 1 + 1"),
                Puzzle("Clear Turn", "11 / 1 + 1"),
                Puzzle("Even Still", "111 - 111 = 11 - 11"),
                Puzzle("Bright Friend", "1 * 11"),
                Puzzle("Reverse Bright", "11 * 1"),
                Puzzle("Hidden Whisper", "1 + 1 * 11"),
                Puzzle("Hidden Step", "11 * 1 - 1"),
                Puzzle("High Return", "111 - 11 + 1"),
                Puzzle("Crowded Step", "11 + 11 - 1"),
                Puzzle("Tiny Match", "1 / 1 = 1"),
                Puzzle("Soft Echo", "11 / 11 = 1"),
                Puzzle("High Echo", "111 = 111"),
                Puzzle("Small Echo", "1 + 1 = 1 / 1 + 1"),
                Puzzle("Side Echo", "11 + 1 = 11 / 1 + 1"),
                Puzzle("Trim Echo", "11 - 1 = 11 - 1"),
                Puzzle("High Echo Two", "111 - 11 = 111 - 11"),
                Puzzle("Two Ways", "1 × 1 = 1 / 1"),
                Puzzle("Bright Ways", "1 * 1 = 1 × 1"),
                Puzzle("Big Fold", "11 / 11 = 111 / 111"),
                Puzzle("Busy Echo", "1 + 1 + 1 = 1 + 1 + 1"),
                Puzzle("Tucked Step", "11 + 1 = 11 × 1 + 1"),
                Puzzle("Hidden Slide", "11 - 1 = 11 / 1 - 1"),
                Puzzle("High Hidden", "111 - 11 = 111 / 1 - 11"),
                Puzzle("Gentle Echo", "1 + 1 / 1 = 1 + 1"),
                Puzzle("Quick Echo", "1 + 1 × 1 = 1 + 1"),
                Puzzle("Neighbor Talk", "11 = 1 1"),
                Puzzle("Tiny Parade", "111 = 1 1 1"),
                Puzzle("Left Tuck", "111 = 1 11"),
                Puzzle("Right Tuck", "111 = 11 1"),
                Puzzle("Double Tuck", "11 + 11 = 1 1 + 11"),
                Puzzle("High Tuck", "111 - 1 = 11 1 - 1"),
                Puzzle("Quick Secret", "1 + 1 - 1 = 1 × 1"),
                Puzzle("Lean Secret", "1 + 1 - 1 = 1 / 1"),
                Puzzle("Bright Secret", "1 * 1 + 1 = 1 + 1"),
                Puzzle("Fold Secret", "111 / 111 + 1 = 1 + 1"),
                Puzzle("Still Here", "11 + 1 - 1 = 11"),
                Puzzle("Nearly Twin", "111 - 11 - 1 = 111 - 11 - 1"),
                Puzzle("Old Friend", "1 + 1 - 1 × 1 / 1 = 1"),
                Puzzle("Deep Cut", "111 - 11 - 11"),
                Puzzle("Little Dip", "111 - 11 - 1 - 1"),
                Puzzle("High Lift", "111 + 11 - 1"),
                Puzzle("Trade Step", "111 + 1 - 11"),
                Puzzle("Fold Lift", "111 / 111 + 11"),
                Puzzle("Bright Drop", "11 * 11 - 111"),
                Puzzle("Quick Drop", "111 - 11 × 1"),
                Puzzle("Bright Drop Two", "111 - 1 * 11"),
                Puzzle("Heavy Drop", "11 * 11 - 11"),
                Puzzle("High Slide", "111 / 1 - 1"),
                Puzzle("Loop A", "11 + 1 × 1 - 1"),
                Puzzle("Loop Two", "11 + 1 / 1 - 1"),
                Puzzle("Heavy Lift", "1 + 11 × 11"),
                Puzzle("High Again", "111 - 11 + 11"),
                Puzzle("Fold Again", "111 / 111 + 11 - 1"),
                Puzzle("Bright Nudge", "1 + 1 + 11 * 1"),
                Puzzle("Small Steps", "111 - 1 - 1 - 1"),
                Puzzle("Final Balance", "11 * 11 = 111 + 11 - 1"),
                Puzzle("Quick Nudge", "111 - 11 + 1 × 1"),
                Puzzle("Quick Trim", "111 - 11 - 1 × 1"),
                Puzzle("Last Shape", "1 + 1 - 1 × 1 / 1")
            };
        }

        private static PuzzleRoundData Puzzle(string roundName, string sampleSolution)
        {
            return Round(roundName, string.Empty, sampleSolution);
        }

        private static PuzzleRoundData Round(string roundName, string tutorialMessage, string sampleSolution)
        {
            var symbols = SplitSampleSymbols(sampleSolution);
            var targetValue = SampleTarget(symbols);
            return new PuzzleRoundData(
                roundName,
                targetValue,
                UsedSticks(symbols),
                AnySlots(symbols.Length),
                AllPlayableTokens,
                tutorialMessage,
                sampleSolution);
        }

        private static double SampleTarget(IReadOnlyList<string> symbols)
        {
            if (TryEvaluateEquationEquality(symbols, out var equalityResult, out _) && equalityResult.IsValid)
            {
                return equalityResult.Value;
            }

            var result = Evaluate(symbols);
            if (result.IsValid)
            {
                return result.Value;
            }

            throw new InvalidOperationException($"Invalid sample solution: {result.Error}");
        }

        private static string[] SplitSampleSymbols(string sampleSolution)
        {
            return sampleSolution.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        }

        public static PuzzleSlotType[] AnySlots(int count)
        {
            var slots = new PuzzleSlotType[count];
            for (var i = 0; i < slots.Length; i++)
            {
                slots[i] = PuzzleSlotType.Any;
            }

            return slots;
        }

        public static PuzzleSlotType[] Slots(params PuzzleSlotType[] slotTypes)
        {
            return slotTypes;
        }

        public static PuzzleTokenData GetToken(string symbol)
        {
            for (var i = 0; i < Tokens.Length; i++)
            {
                if (Tokens[i].Symbol == symbol)
                {
                    return Tokens[i];
                }
            }

            throw new ArgumentException($"Unknown token: {symbol}");
        }

        public static bool IsTokenAllowed(PuzzleRoundData round, string symbol)
        {
            for (var i = 0; i < round.AllowedTokens.Length; i++)
            {
                if (round.AllowedTokens[i] == symbol)
                {
                    return true;
                }
            }

            return false;
        }

        public static bool CanPlaceToken(PuzzleSlotType slotType, PuzzleTokenData token)
        {
            return slotType == PuzzleSlotType.Any ||
                   slotType == PuzzleSlotType.Number && token.TokenType == PuzzleTokenType.Number ||
                   slotType == PuzzleSlotType.Operator && token.TokenType == PuzzleTokenType.Operator;
        }

        public static string RecognizeToken(IReadOnlyList<StickPose> poses)
        {
            if (poses == null || poses.Count <= 0)
            {
                return string.Empty;
            }

            if (poses.Count == 1)
            {
                return poses[0] == StickPose.CenterVertical ? "1" :
                    poses[0] == StickPose.CenterHorizontal ? "-" :
                    poses[0] == StickPose.CenterSlash ? "/" :
                    string.Empty;
            }

            if (poses.Count == 2)
            {
                if (HasPose(poses, StickPose.LeftVertical) && HasPose(poses, StickPose.RightVertical))
                {
                    return "11";
                }

                if (HasPose(poses, StickPose.CenterVertical) && HasPose(poses, StickPose.CenterHorizontal))
                {
                    return "+";
                }

                if (HasPose(poses, StickPose.CenterSlash) && HasPose(poses, StickPose.CenterBackslash))
                {
                    return "×";
                }

                if (HasPose(poses, StickPose.TopHorizontal) && HasPose(poses, StickPose.BottomHorizontal))
                {
                    return "=";
                }
            }

            if (poses.Count == 3)
            {
                if (HasPose(poses, StickPose.LeftVertical) &&
                    HasPose(poses, StickPose.CenterVertical) &&
                    HasPose(poses, StickPose.RightVertical))
                {
                    return "111";
                }

                if (HasPose(poses, StickPose.CenterSlash) &&
                    HasPose(poses, StickPose.CenterBackslash) &&
                    (HasPose(poses, StickPose.CenterVertical) || HasPose(poses, StickPose.CenterHorizontal)))
                {
                    return "*";
                }
            }

            return string.Empty;
        }

        private static bool HasPose(IReadOnlyList<StickPose> poses, StickPose pose)
        {
            for (var i = 0; i < poses.Count; i++)
            {
                if (poses[i] == pose)
                {
                    return true;
                }
            }

            return false;
        }

        public static int UsedSticks(IReadOnlyList<string> placedTokens)
        {
            var used = 0;
            for (var i = 0; i < placedTokens.Count; i++)
            {
                if (!string.IsNullOrEmpty(placedTokens[i]))
                {
                    used += GetToken(placedTokens[i]).StickCost;
                }
            }

            return used;
        }

        public static bool AllSlotsFilled(IReadOnlyList<string> placedTokens)
        {
            for (var i = 0; i < placedTokens.Count; i++)
            {
                if (string.IsNullOrEmpty(placedTokens[i]))
                {
                    return false;
                }
            }

            return true;
        }

        public static bool IsRoundSolved(PuzzleRoundData round, IReadOnlyList<string> placedTokens, out EquationResult result, out string reason)
        {
            var usedSticks = UsedSticks(placedTokens);
            if (usedSticks != round.StickCount)
            {
                result = new EquationResult(false, 0d, "Stick count mismatch");
                reason = usedSticks < round.StickCount ? "Sticks remain." : "Too many sticks.";
                return false;
            }

            if (TryEvaluateEquationEquality(placedTokens, out result, out reason))
            {
                return result.IsValid;
            }

            result = Evaluate(placedTokens);
            if (!result.IsValid)
            {
                reason = result.Error;
                return false;
            }

            if (Math.Abs(result.Value - round.TargetValue) >= TargetTolerance)
            {
                reason = $"Result is {FormatNumber(result.Value)}.";
                return false;
            }

            reason = "Good!";
            return true;
        }

        private static bool TryEvaluateEquationEquality(IReadOnlyList<string> symbols, out EquationResult result, out string reason)
        {
            var equalsIndex = -1;
            for (var i = 0; i < symbols.Count; i++)
            {
                if (symbols[i] != "=")
                {
                    continue;
                }

                if (equalsIndex >= 0)
                {
                    result = new EquationResult(false, 0d, "Too many equals.");
                    reason = "Use one =.";
                    return true;
                }

                equalsIndex = i;
            }

            if (equalsIndex < 0)
            {
                result = new EquationResult(false, 0d, string.Empty);
                reason = string.Empty;
                return false;
            }

            if (equalsIndex == 0 || equalsIndex == symbols.Count - 1)
            {
                result = new EquationResult(false, 0d, "Incomplete equality.");
                reason = "= needs both sides.";
                return true;
            }

            var leftSymbols = Slice(symbols, 0, equalsIndex);
            var rightSymbols = Slice(symbols, equalsIndex + 1, symbols.Count - equalsIndex - 1);
            var left = Evaluate(leftSymbols);
            if (!left.IsValid)
            {
                result = left;
                reason = $"Left side: {left.Error}";
                return true;
            }

            var right = Evaluate(rightSymbols);
            if (!right.IsValid)
            {
                result = right;
                reason = $"Right side: {right.Error}";
                return true;
            }

            if (Math.Abs(left.Value - right.Value) >= TargetTolerance)
            {
                result = new EquationResult(false, left.Value, "Equality mismatch");
                reason = $"{FormatNumber(left.Value)} is not {FormatNumber(right.Value)}.";
                return true;
            }

            result = new EquationResult(true, left.Value, string.Empty);
            reason = "Good!";
            return true;
        }

        private static string[] Slice(IReadOnlyList<string> symbols, int startIndex, int count)
        {
            var result = new string[count];
            for (var i = 0; i < count; i++)
            {
                result[i] = symbols[startIndex + i];
            }

            return result;
        }

        public static EquationResult Evaluate(IReadOnlyList<string> symbols)
        {
            var numbers = new List<double>();
            var operators = new List<string>();
            var numberBuffer = new StringBuilder();

            for (var i = 0; i < symbols.Count; i++)
            {
                var symbol = symbols[i];
                if (string.IsNullOrEmpty(symbol))
                {
                    return new EquationResult(false, 0d, "Empty box.");
                }

                var token = GetToken(symbol);
                if (token.TokenType == PuzzleTokenType.Number)
                {
                    numberBuffer.Append(symbol);
                    continue;
                }

                if (numberBuffer.Length <= 0)
                {
                    return new EquationResult(false, 0d, "Operator first.");
                }

                numbers.Add(ParseBufferedNumber(numberBuffer));
                numberBuffer.Length = 0;
                operators.Add(symbol);
            }

            if (numberBuffer.Length <= 0)
            {
                return new EquationResult(false, 0d, "Operator last.");
            }

            numbers.Add(ParseBufferedNumber(numberBuffer));

            if (numbers.Count != operators.Count + 1)
            {
                return new EquationResult(false, 0d, "Bad expression order.");
            }

            for (var i = 0; i < operators.Count;)
            {
                var op = operators[i];
                if (op != "*" && op != "x" && op != "×" && op != "/")
                {
                    i++;
                    continue;
                }

                var left = numbers[i];
                var right = numbers[i + 1];
                if (op == "/" && Math.Abs(right) < TargetTolerance)
                {
                    return new EquationResult(false, 0d, "Cannot divide by zero.");
                }

                numbers[i] = op == "*" || op == "x" || op == "×" ? left * right : left / right;
                numbers.RemoveAt(i + 1);
                operators.RemoveAt(i);
            }

            var value = numbers[0];
            for (var i = 0; i < operators.Count; i++)
            {
                var op = operators[i];
                var right = numbers[i + 1];
                if (op == "+")
                {
                    value += right;
                }
                else if (op == "-")
                {
                    value -= right;
                }
                else
                {
                    return new EquationResult(false, 0d, $"Unknown operator: {op}");
                }
            }

            return new EquationResult(true, value, string.Empty);
        }

        public static string FormatNumber(double value)
        {
            if (Math.Abs(value - Math.Round(value)) < TargetTolerance)
            {
                return Math.Round(value).ToString(CultureInfo.InvariantCulture);
            }

            return value.ToString("0.###", CultureInfo.InvariantCulture);
        }

        private static double ParseBufferedNumber(StringBuilder buffer)
        {
            return double.Parse(buffer.ToString(), CultureInfo.InvariantCulture);
        }
    }
}
