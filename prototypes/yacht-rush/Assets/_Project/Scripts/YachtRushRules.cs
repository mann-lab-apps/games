using System;
using System.Collections.Generic;
using System.Linq;

namespace MannLab.Games.YachtRush
{
    public enum YachtRushCategory
    {
        Ones,
        Twos,
        Threes,
        Fours,
        Fives,
        Sixes,
        FourOfAKind,
        FullHouse,
        SmallStraight,
        LargeStraight,
        Yacht,
        Chance
    }

    public enum YachtRushContract
    {
        HighTide,
        TripleSignal,
        CleanRun,
        LowDeck,
        BoldScratch,
        PerfectHold
    }

    public readonly struct YachtRushScore
    {
        public YachtRushScore(int baseScore, int bonus, int total)
        {
            BaseScore = baseScore;
            Bonus = bonus;
            Total = total;
        }

        public int BaseScore { get; }
        public int Bonus { get; }
        public int Total { get; }
    }

    public readonly struct YachtRushContractInfo
    {
        public YachtRushContractInfo(YachtRushContract id, string name, string condition, int bonus)
        {
            Id = id;
            Name = name;
            Condition = condition;
            Bonus = bonus;
        }

        public YachtRushContract Id { get; }
        public string Name { get; }
        public string Condition { get; }
        public int Bonus { get; }
    }

    public static class YachtRushRules
    {
        public const int DiceCount = 5;
        public const int MaxRollsPerRound = 3;
        public const int RoundCount = 12;

        public static readonly YachtRushCategory[] Categories =
        {
            YachtRushCategory.Ones,
            YachtRushCategory.Twos,
            YachtRushCategory.Threes,
            YachtRushCategory.Fours,
            YachtRushCategory.Fives,
            YachtRushCategory.Sixes,
            YachtRushCategory.FourOfAKind,
            YachtRushCategory.FullHouse,
            YachtRushCategory.SmallStraight,
            YachtRushCategory.LargeStraight,
            YachtRushCategory.Yacht,
            YachtRushCategory.Chance
        };

        public static readonly YachtRushContractInfo[] Contracts =
        {
            new YachtRushContractInfo(YachtRushContract.HighTide, "High Tide", "Total 20+", 8),
            new YachtRushContractInfo(YachtRushContract.TripleSignal, "Triple Signal", "3 matching dice", 10),
            new YachtRushContractInfo(YachtRushContract.CleanRun, "Clean Run", "Use 1 reroll or less", 6),
            new YachtRushContractInfo(YachtRushContract.LowDeck, "Low Deck", "Include 1, 2, and 3", 7),
            new YachtRushContractInfo(YachtRushContract.BoldScratch, "Bold Scratch", "Score a zero row", 5),
            new YachtRushContractInfo(YachtRushContract.PerfectHold, "Perfect Hold", "Hold 3 before final throw", 6)
        };

        public static int ScoreCategory(YachtRushCategory category, IReadOnlyList<int> dice)
        {
            ValidateDice(dice);

            var counts = CountDice(dice);
            var total = dice.Sum();

            switch (category)
            {
                case YachtRushCategory.Ones:
                    return CountFace(counts, 1) * 1;
                case YachtRushCategory.Twos:
                    return CountFace(counts, 2) * 2;
                case YachtRushCategory.Threes:
                    return CountFace(counts, 3) * 3;
                case YachtRushCategory.Fours:
                    return CountFace(counts, 4) * 4;
                case YachtRushCategory.Fives:
                    return CountFace(counts, 5) * 5;
                case YachtRushCategory.Sixes:
                    return CountFace(counts, 6) * 6;
                case YachtRushCategory.FourOfAKind:
                    return counts.Any(count => count >= 4) ? total : 0;
                case YachtRushCategory.FullHouse:
                    return counts.Where(count => count > 0).OrderBy(count => count).SequenceEqual(new[] { 2, 3 }) ? 25 : 0;
                case YachtRushCategory.SmallStraight:
                    return HasStraight(dice, 4) ? 30 : 0;
                case YachtRushCategory.LargeStraight:
                    return HasStraight(dice, 5) ? 40 : 0;
                case YachtRushCategory.Yacht:
                    return counts.Any(count => count == 5) ? 50 : 0;
                case YachtRushCategory.Chance:
                    return total;
                default:
                    throw new ArgumentOutOfRangeException(nameof(category), category, null);
            }
        }

        public static int ContractBonus(
            YachtRushContract contract,
            IReadOnlyList<int> dice,
            int baseScore,
            int rerollsUsed,
            int lockedBeforeFinalThrow)
        {
            ValidateDice(dice);

            var counts = CountDice(dice);
            var passed = false;

            switch (contract)
            {
                case YachtRushContract.HighTide:
                    passed = dice.Sum() >= 20;
                    break;
                case YachtRushContract.TripleSignal:
                    passed = counts.Any(count => count >= 3);
                    break;
                case YachtRushContract.CleanRun:
                    passed = rerollsUsed <= 1;
                    break;
                case YachtRushContract.LowDeck:
                    passed = CountFace(counts, 1) > 0 && CountFace(counts, 2) > 0 && CountFace(counts, 3) > 0;
                    break;
                case YachtRushContract.BoldScratch:
                    passed = baseScore == 0;
                    break;
                case YachtRushContract.PerfectHold:
                    passed = lockedBeforeFinalThrow >= 3;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(contract), contract, null);
            }

            return passed ? GetContract(contract).Bonus : 0;
        }

        public static YachtRushScore PreviewScore(
            YachtRushCategory category,
            YachtRushContract contract,
            IReadOnlyList<int> dice,
            int rerollsUsed,
            int lockedBeforeFinalThrow)
        {
            var baseScore = ScoreCategory(category, dice);
            var bonus = ContractBonus(contract, dice, baseScore, rerollsUsed, lockedBeforeFinalThrow);
            return new YachtRushScore(baseScore, bonus, baseScore + bonus);
        }

        public static string CategoryName(YachtRushCategory category)
        {
            switch (category)
            {
                case YachtRushCategory.Ones:
                    return "Ones";
                case YachtRushCategory.Twos:
                    return "Twos";
                case YachtRushCategory.Threes:
                    return "Threes";
                case YachtRushCategory.Fours:
                    return "Fours";
                case YachtRushCategory.Fives:
                    return "Fives";
                case YachtRushCategory.Sixes:
                    return "Sixes";
                case YachtRushCategory.FourOfAKind:
                    return "Four Kind";
                case YachtRushCategory.FullHouse:
                    return "Full House";
                case YachtRushCategory.SmallStraight:
                    return "Small Run";
                case YachtRushCategory.LargeStraight:
                    return "Large Run";
                case YachtRushCategory.Yacht:
                    return "Yacht";
                case YachtRushCategory.Chance:
                    return "Chance";
                default:
                    throw new ArgumentOutOfRangeException(nameof(category), category, null);
            }
        }

        public static string CategoryHint(YachtRushCategory category)
        {
            switch (category)
            {
                case YachtRushCategory.Ones:
                    return "sum of 1s";
                case YachtRushCategory.Twos:
                    return "sum of 2s";
                case YachtRushCategory.Threes:
                    return "sum of 3s";
                case YachtRushCategory.Fours:
                    return "sum of 4s";
                case YachtRushCategory.Fives:
                    return "sum of 5s";
                case YachtRushCategory.Sixes:
                    return "sum of 6s";
                case YachtRushCategory.FourOfAKind:
                    return "4 match";
                case YachtRushCategory.FullHouse:
                    return "3 + 2";
                case YachtRushCategory.SmallStraight:
                    return "4 in a row";
                case YachtRushCategory.LargeStraight:
                    return "5 in a row";
                case YachtRushCategory.Yacht:
                    return "5 match";
                case YachtRushCategory.Chance:
                    return "all dice";
                default:
                    throw new ArgumentOutOfRangeException(nameof(category), category, null);
            }
        }

        public static YachtRushContractInfo GetContract(YachtRushContract contract)
        {
            foreach (var item in Contracts)
            {
                if (item.Id == contract)
                {
                    return item;
                }
            }

            throw new ArgumentOutOfRangeException(nameof(contract), contract, null);
        }

        private static int[] CountDice(IReadOnlyList<int> dice)
        {
            var counts = new int[7];
            foreach (var value in dice)
            {
                counts[value] += 1;
            }

            return counts;
        }

        private static int CountFace(IReadOnlyList<int> counts, int face)
        {
            return counts[face];
        }

        private static bool HasStraight(IReadOnlyList<int> dice, int length)
        {
            var faces = dice.Distinct().OrderBy(value => value).ToArray();
            var run = 1;

            for (var index = 1; index < faces.Length; index += 1)
            {
                run = faces[index] == faces[index - 1] + 1 ? run + 1 : 1;
                if (run >= length)
                {
                    return true;
                }
            }

            return false;
        }

        private static void ValidateDice(IReadOnlyList<int> dice)
        {
            if (dice == null)
            {
                throw new ArgumentNullException(nameof(dice));
            }

            if (dice.Count != DiceCount)
            {
                throw new ArgumentException($"Yacht Rush expects {DiceCount} dice.", nameof(dice));
            }

            foreach (var value in dice)
            {
                if (value < 1 || value > 6)
                {
                    throw new ArgumentOutOfRangeException(nameof(dice), "Dice values must be between 1 and 6.");
                }
            }
        }
    }
}
