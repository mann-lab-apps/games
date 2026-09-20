using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
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
        // Legacy double-test/display tolerance only. Solve acceptance must stay exact-rational.
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
                Round("Hello, 1", "Build with your first stick friend. Put it in the empty box.", "1"),
                Round("Two-Stick Plus", "Place two friends in one box. Shape them into +.", "1 + 1"),
                Round("Lazy Minus", "Turn one friend sideways to make -.", "1 - 1"),
                Round("Tilted Divide", "Tilt one friend to make /.", "1 / 1"),
                Round("Cross Multiply", "Cross two tilted friends to make ×.", "1 × 1"),
                Round("Packed Eleven", "Pack two upright friends together: 11.", "11"),
                Round("Neighbor Eleven", "Two neighboring 1 boxes can also read as 11.", "1 1"),
                Round("Triple One", "Pack three upright friends together: 111.", "111"),
                Round("Star Multiply", "Three crossed friends can become *.", "1 * 1"),
                Round("Eleven Joins", "Now build with a packed 11.", "11 + 1"),
                Round("Make Ten", "Take a small friend away from 11.", "1 1 - 1"),
                Round("Back To One", "A built number can divide itself.", "1 1 / 11"),
                Round("Zero Trick", "Build a path that returns to zero.", "1 - 1 + 1 - 1"),
                Round("Order Trick", "Build carefully: × acts before +.", "1 + 1 × 11"),
                Round("Divide Then Add", "Build carefully: / acts before + too.", "11 + 11 / 11"),
                Puzzle("Little Choir", "1 + 1 + 1"),
                Puzzle("Mirror Drop", "11 - 11"),
                Puzzle("Quiet Match", "11 = 1 1"),
                Puzzle("Twin Paths", "1 + 1 = 1 + 1"),
                Puzzle("Soft Match", "11 = 11"),
                Puzzle("Small Turn", "1 + 1 - 1"),
                Puzzle("Side Path", "11 + 1 = 1 + 11"),
                Puzzle("High Step", "111 - 111"),
                Puzzle("Long Slide", "11 / 1 - 1 1"),
                Puzzle("Warmup Line", "1 + 1 - 1 × 1"),
                Puzzle("Soft Save", "1 1 1 / 1 1 1"),
                Puzzle("Hidden Path", "11 * 1 / 1 1"),
                Puzzle("High Fold", "111 / 111"),
                Puzzle("Short Balance", "11 + 1 = 1 1 + 1"),
                Puzzle("Familiar Shape", "1 + 1 - 1 × 1 / 1"),
                Puzzle("Busy Row", "1 + 1 + 1 + 1 1"),
                Puzzle("Soft Lift", "1 + 1 + 1 1"),
                Puzzle("Soft Dip", "1 11 - 1"),
                Puzzle("Small Loop", "1 1 / 1 / 1"),
                Puzzle("High Loop", "11 - 1 1"),
                Puzzle("Clean Slide", "111 / 111 + 11 + 11"),
                Puzzle("Almost There", "11 1 - 1 - 1"),
                Puzzle("Heavy Pair", "1 1 × 1"),
                Puzzle("Side By Side", "111 + 11"),
                Puzzle("High Trim", "111 - 1"),
                Puzzle("First Switch", "1 1 + 1"),
                Puzzle("Second Switch", "11 + 11 + 1 1"),
                Puzzle("Clear Turn", "1 / 1 + 1 1"),
                Puzzle("Even Still", "111 - 111 = 11 - 11"),
                Puzzle("Bright Friend", "11 * 1 1"),
                Puzzle("Reverse Bright", "11 * 11"),
                Puzzle("Hidden Whisper", "1 11 + 11"),
                Puzzle("Hidden Step", "111 - 1 11"),
                Puzzle("High Return", "1 - 1 + 1 1"),
                Puzzle("Crowded Step", "1 - 1 - 1"),
                Puzzle("Tiny Match", "1 1 / 1 - 1"),
                Puzzle("Soft Echo", "1 1 1 - 1"),
                Puzzle("High Lean", "111 / 1"),
                Puzzle("Small Echo", "1 + 1 = 1 / 1 + 1"),
                Puzzle("Side Echo", "11 + 1 = 11 / 1 + 1"),
                Puzzle("Square Trim", "11 * 11 / 11 + 1 1"),
                Puzzle("High Echo Two", "111 + 11 = 11 + 11 1"),
                Puzzle("Two Ways", "1 / 1 = 1 1 / 11"),
                Puzzle("Bright Ways", "111 * 11 - 11 11"),
                Puzzle("Small Chorus", "111 * 1 = 111"),
                Puzzle("Busy Lift", "11 / 11 + 1 + 1"),
                Puzzle("Tucked Step", "11 + 1 = 11 × 1 + 1"),
                Puzzle("Hidden Slide", "1 - 1 = 1 / 1 - 1"),
                Puzzle("High Hidden", "111 - 11 = 111 / 1 - 11"),
                Puzzle("Gentle Echo", "1 - 1 1 / 11 - 1"),
                Puzzle("Quick Echo", "1 + 1 × 1 = 1 + 1"),
                Puzzle("Neighbor Talk", "1 1 / 1"),
                Puzzle("Tiny Parade", "1 11 = 1 * 111"),
                Puzzle("Bright Tuck", "1 11 * 1"),
                Puzzle("Right Tuck", "111 * 111 / 111"),
                Puzzle("Double Tuck", "1 - 1 / 1 - 11 / 1 1"),
                Puzzle("High Tuck", "1 1 × 11 - 11 + 11 - 11"),
                Puzzle("Quick Secret", "1 / 1 1 - 1 / 11 + 1"),
                Puzzle("Lean Secret", "1 1 - 1 - 1"),
                Puzzle("Bright Secret", "111 = 11 1 + 11 - 11"),
                Puzzle("Fold Secret", "111 - 111 + 11 / 11 + 1"),
                Puzzle("Still Here", "1 / 1 1 - 1 / 11 + 11"),
                Puzzle("High Split", "111 × 111 - 111 1 × 11"),
                Puzzle("Old Friend", "1 + 1 - 1 × 1 / 1 = 1"),
                Puzzle("Deep Cut", "1 - 1 / 1"),
                Puzzle("Little Dip", "11 1 - 1 - 1 - 1"),
                Puzzle("High Lift", "111 + 11 - 11 1"),
                Puzzle("Trade Step", "11 11 / 11"),
                Puzzle("Fold Lift", "111 * 1 11 / 111"),
                Puzzle("Bright Drop", "11 * 11 - 111"),
                Puzzle("Quick Drop", "1 1 - 1 × 1"),
                Puzzle("Bright Drop Two", "1 + 11 * 11"),
                Puzzle("Heavy Drop", "111 - 111 / 111 * 11"),
                Puzzle("High Slide", "11 1 / 1 - 1"),
                Puzzle("Loop A", "11 - 1 1 - 1"),
                Puzzle("Loop Two", "1 1 1 - 1 + 1 1"),
                Puzzle("Heavy Lift", "11 + 111 / 111 * 111"),
                Puzzle("High Again", "1 11 + 1 - 1 - 11 / 11"),
                Puzzle("Fold Again", "11 - 1 / 111 * 1 11 + 1"),
                Puzzle("Bright Nudge", "1 + 1 1 × 11 / 11 + 1"),
                Puzzle("Small Steps", "111 - 1 - 1 - 1 / 1 - 1"),
                Puzzle("Final Balance", "11 * 11 = 111 + 11 - 1"),
                Puzzle("Quick Nudge", "111 - 11 + 1 × 1 1"),
                Puzzle("Quick Trim", "111 - 11 - 1 / 1 + 1 - 1"),
                Puzzle("Last Shape", "11 × 11 - 111 + 1")
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
            if (placedTokens.Count != round.SlotTypes.Length)
            {
                result = new EquationResult(false, 0d, "Slot count mismatch");
                reason = placedTokens.Count < round.SlotTypes.Length ? "Fill every box." : "Too many boxes.";
                return false;
            }

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

            var exact = EvaluateExact(placedTokens);
            if (!exact.IsValid)
            {
                result = new EquationResult(false, 0d, exact.Error);
                reason = exact.Error;
                return false;
            }

            if (!MatchesTarget(exact.Value, round.TargetValue))
            {
                reason = $"Result is {FormatNumber(exact.Value)}.";
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
            var left = EvaluateExact(leftSymbols);
            if (!left.IsValid)
            {
                result = new EquationResult(false, 0d, left.Error);
                reason = $"Left side: {left.Error}";
                return true;
            }

            var right = EvaluateExact(rightSymbols);
            if (!right.IsValid)
            {
                result = new EquationResult(false, 0d, right.Error);
                reason = $"Right side: {right.Error}";
                return true;
            }

            if (!left.Value.Equals(right.Value))
            {
                result = new EquationResult(false, left.Value.ToDouble(), "Equality mismatch");
                reason = $"{FormatNumber(left.Value)} is not {FormatNumber(right.Value)}.";
                return true;
            }

            result = new EquationResult(true, left.Value.ToDouble(), string.Empty);
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
            var result = EvaluateExact(symbols);
            return result.IsValid
                ? new EquationResult(true, result.Value.ToDouble(), string.Empty)
                : new EquationResult(false, 0d, result.Error);
        }

        private static ExactResult EvaluateExact(IReadOnlyList<string> symbols)
        {
            var numbers = new List<ExactNumber>();
            var operators = new List<string>();
            var numberBuffer = new StringBuilder();

            for (var i = 0; i < symbols.Count; i++)
            {
                var symbol = symbols[i];
                if (string.IsNullOrEmpty(symbol))
                {
                    return ExactResult.Invalid("Empty box.");
                }

                var token = GetToken(symbol);
                if (token.TokenType == PuzzleTokenType.Number)
                {
                    numberBuffer.Append(symbol);
                    continue;
                }

                if (numberBuffer.Length <= 0)
                {
                    return ExactResult.Invalid("Operator first.");
                }

                numbers.Add(ParseBufferedNumber(numberBuffer));
                numberBuffer.Length = 0;
                operators.Add(symbol);
            }

            if (numberBuffer.Length <= 0)
            {
                return ExactResult.Invalid("Operator last.");
            }

            numbers.Add(ParseBufferedNumber(numberBuffer));

            if (numbers.Count != operators.Count + 1)
            {
                return ExactResult.Invalid("Bad expression order.");
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
                if (op == "/" && right.IsZero)
                {
                    return ExactResult.Invalid("Cannot divide by zero.");
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
                    return ExactResult.Invalid($"Unknown operator: {op}");
                }
            }

            return ExactResult.Valid(value);
        }

        public static string FormatNumber(double value)
        {
            if (Math.Abs(value - Math.Round(value)) < 0.0000000001d)
            {
                return Math.Round(value).ToString(CultureInfo.InvariantCulture);
            }

            return value.ToString("0.#####", CultureInfo.InvariantCulture);
        }

        private static string FormatNumber(ExactNumber value)
        {
            if (value.IsInteger)
            {
                return value.IntegerValue.ToString(CultureInfo.InvariantCulture);
            }

            var rounded = FormatNumber(value.ToDouble());
            return rounded == "0" || rounded == "-0"
                ? $"{value.Numerator.ToString(CultureInfo.InvariantCulture)}/{value.Denominator.ToString(CultureInfo.InvariantCulture)}"
                : rounded;
        }

        private static ExactNumber ParseBufferedNumber(StringBuilder buffer)
        {
            return ExactNumber.FromInteger(BigInteger.Parse(buffer.ToString(), CultureInfo.InvariantCulture));
        }

        private static bool MatchesTarget(ExactNumber value, double target)
        {
            if (double.IsNaN(target) || double.IsInfinity(target))
            {
                return false;
            }

            var rounded = Math.Round(target);
            if (Math.Abs(target - rounded) < 0.0000000001d)
            {
                return value.Equals(ExactNumber.FromInteger(new BigInteger(rounded)));
            }

            return value.ToDouble().Equals(target);
        }

        private readonly struct ExactResult
        {
            public bool IsValid { get; }
            public ExactNumber Value { get; }
            public string Error { get; }

            private ExactResult(bool isValid, ExactNumber value, string error)
            {
                IsValid = isValid;
                Value = value;
                Error = error;
            }

            public static ExactResult Valid(ExactNumber value)
            {
                return new ExactResult(true, value, string.Empty);
            }

            public static ExactResult Invalid(string error)
            {
                return new ExactResult(false, default, error);
            }
        }

        private readonly struct ExactNumber : IEquatable<ExactNumber>
        {
            public BigInteger Numerator { get; }
            public BigInteger Denominator { get; }
            public bool IsZero => Numerator.IsZero;
            public bool IsInteger => Denominator.IsOne;
            public BigInteger IntegerValue => Numerator / Denominator;

            private ExactNumber(BigInteger numerator, BigInteger denominator)
            {
                if (denominator.IsZero)
                {
                    throw new DivideByZeroException();
                }

                if (denominator.Sign < 0)
                {
                    numerator = BigInteger.Negate(numerator);
                    denominator = BigInteger.Negate(denominator);
                }

                var divisor = BigInteger.GreatestCommonDivisor(BigInteger.Abs(numerator), denominator);
                Numerator = numerator / divisor;
                Denominator = denominator / divisor;
            }

            public static ExactNumber FromInteger(BigInteger value)
            {
                return new ExactNumber(value, BigInteger.One);
            }

            public double ToDouble()
            {
                return (double)Numerator / (double)Denominator;
            }

            public bool Equals(ExactNumber other)
            {
                return Numerator.Equals(other.Numerator) && Denominator.Equals(other.Denominator);
            }

            public override bool Equals(object obj)
            {
                return obj is ExactNumber other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return (Numerator.GetHashCode() * 397) ^ Denominator.GetHashCode();
                }
            }

            public static ExactNumber operator +(ExactNumber left, ExactNumber right)
            {
                return new ExactNumber(left.Numerator * right.Denominator + right.Numerator * left.Denominator, left.Denominator * right.Denominator);
            }

            public static ExactNumber operator -(ExactNumber left, ExactNumber right)
            {
                return new ExactNumber(left.Numerator * right.Denominator - right.Numerator * left.Denominator, left.Denominator * right.Denominator);
            }

            public static ExactNumber operator *(ExactNumber left, ExactNumber right)
            {
                return new ExactNumber(left.Numerator * right.Numerator, left.Denominator * right.Denominator);
            }

            public static ExactNumber operator /(ExactNumber left, ExactNumber right)
            {
                return new ExactNumber(left.Numerator * right.Denominator, left.Denominator * right.Numerator);
            }
        }
    }
}
