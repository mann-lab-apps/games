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
        None,
        EvenFleet,
        OddCrew,
        LowTide,
        HighTide,
        TwinWake,
        BrokenRun,
        CaptainPair,
        CleanBowl
    }

    public enum YachtRushRollRule
    {
        Classic,
        OneShot,
        SafeHarbor,
        NoHolds,
        MustHold2,
        RerollAll
    }

    public enum YachtRushRushDie
    {
        None,
        Anchor,
        Storm,
        Cracked,
        Mirror,
        Blank
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

    public readonly struct YachtRushRollRuleInfo
    {
        public YachtRushRollRuleInfo(YachtRushRollRule id, string name, string description)
        {
            Id = id;
            Name = name;
            Description = description;
        }

        public YachtRushRollRule Id { get; }
        public string Name { get; }
        public string Description { get; }
    }

    public readonly struct YachtRushRushDieInfo
    {
        public YachtRushRushDieInfo(YachtRushRushDie id, string name, string description)
        {
            Id = id;
            Name = name;
            Description = description;
        }

        public YachtRushRushDie Id { get; }
        public string Name { get; }
        public string Description { get; }
    }

    public readonly struct YachtRushRoundScorePreview
    {
        public YachtRushRoundScorePreview(
            int baseScore,
            int rushAdjustedScore,
            int contractBonus,
            int total,
            bool contractSatisfied,
            int[] effectiveDice)
        {
            BaseScore = baseScore;
            RushAdjustedScore = rushAdjustedScore;
            ContractBonus = contractBonus;
            Total = total;
            ContractSatisfied = contractSatisfied;
            EffectiveDice = effectiveDice ?? Array.Empty<int>();
        }

        public int BaseScore { get; }
        public int RushAdjustedScore { get; }
        public int ContractBonus { get; }
        public int Total { get; }
        public bool ContractSatisfied { get; }
        public int[] EffectiveDice { get; }
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
            new YachtRushContractInfo(YachtRushContract.EvenFleet, "Even Fleet", "Bonus if all active dice are even", 10),
            new YachtRushContractInfo(YachtRushContract.OddCrew, "Odd Crew", "Bonus if all active dice are odd", 10),
            new YachtRushContractInfo(YachtRushContract.LowTide, "Low Tide", "Bonus if final total is 15 or less", 8),
            new YachtRushContractInfo(YachtRushContract.HighTide, "High Tide", "Bonus if final total is 22 or more", 10),
            new YachtRushContractInfo(YachtRushContract.TwinWake, "Twin Wake", "Bonus if you land two pairs", 12),
            new YachtRushContractInfo(YachtRushContract.BrokenRun, "Broken Run", "Bonus for four values across five slots", 12),
            new YachtRushContractInfo(YachtRushContract.CaptainPair, "Captain Pair", "Bonus if you land a pair of 6s", 9),
            new YachtRushContractInfo(YachtRushContract.CleanBowl, "Clean Bowl", "Bonus if you score after one throw", 7)
        };

        public static readonly YachtRushRollRuleInfo[] RollRules =
        {
            new YachtRushRollRuleInfo(YachtRushRollRule.Classic, "Classic", "3 throws. Hold any dice"),
            new YachtRushRollRuleInfo(YachtRushRollRule.OneShot, "One Shot", "1 throw. Score what lands"),
            new YachtRushRollRuleInfo(YachtRushRollRule.SafeHarbor, "Safe Harbor", "2 throws. Contract pays +3"),
            new YachtRushRollRuleInfo(YachtRushRollRule.NoHolds, "No Holds", "Locks are disabled this round"),
            new YachtRushRollRuleInfo(YachtRushRollRule.MustHold2, "Must Hold 2", "Hold 2 dice before throw 2"),
            new YachtRushRollRuleInfo(YachtRushRollRule.RerollAll, "Reroll All", "Every throw rerolls all 5 dice")
        };

        public static readonly YachtRushRushDieInfo[] RushDice =
        {
            new YachtRushRushDieInfo(YachtRushRushDie.Anchor, "Anchor Die", "Auto-locks after landing"),
            new YachtRushRushDieInfo(YachtRushRushDie.Storm, "Storm Die", "Throws harder and spins more"),
            new YachtRushRushDieInfo(YachtRushRushDie.Cracked, "Cracked Die", "Ignored by combo hands"),
            new YachtRushRushDieInfo(YachtRushRushDie.Mirror, "Mirror Die", "Flips value: 1<->6, 2<->5"),
            new YachtRushRushDieInfo(YachtRushRushDie.Blank, "Blank Die", "Selected die scores 0 this round")
        };

        public static int ScoreCategory(YachtRushCategory category, IReadOnlyList<int> dice)
        {
            ValidateDice(dice);
            return ScoreCategoryValues(category, dice);
        }

        public static int ScoreCategoryValues(YachtRushCategory category, IReadOnlyList<int> dice)
        {
            ValidateScoringDice(dice);
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
            return IsContractHandSatisfied(contract, dice, -1, YachtRushRushDie.Anchor, rerollsUsed, lockedBeforeFinalThrow)
                ? GetContract(contract).Bonus
                : 0;
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

        public static YachtRushRoundScorePreview PreviewScore(
            YachtRushCategory category,
            YachtRushContract contract,
            YachtRushRollRule rollRule,
            YachtRushRushDie rushDie,
            int rushDieIndex,
            IReadOnlyList<int> dice,
            int rerollsUsed,
            int lockedBeforeFinalThrow,
            int heldCount)
        {
            ValidateDice(dice);

            var baseScore = ScoreCategory(category, dice);
            var contractSatisfied = IsContractHandSatisfied(
                contract,
                ApplyRushDie(dice, rushDie, rushDieIndex, true),
                rushDieIndex,
                rushDie,
                rerollsUsed,
                lockedBeforeFinalThrow,
                heldCount);
            var effectiveDice = ApplyRushDie(dice, rushDie, rushDieIndex, rushDie != YachtRushRushDie.Blank || contractSatisfied);
            if (rushDie == YachtRushRushDie.Cracked && IsComboCategory(category) && rushDieIndex >= 0 && rushDieIndex < effectiveDice.Length)
            {
                effectiveDice[rushDieIndex] = 0;
            }

            var rushScore = ScoreCategoryValues(category, effectiveDice);
            var contractBonus = contractSatisfied ? AdjustedContractBonus(contract, rollRule) : 0;
            return new YachtRushRoundScorePreview(
                baseScore,
                rushScore,
                contractBonus,
                rushScore + contractBonus,
                contractSatisfied,
                effectiveDice);
        }

        public static int[] ApplyRushDie(IReadOnlyList<int> dice, YachtRushRushDie rushDie, int rushDieIndex, bool blankUnlocked)
        {
            ValidateDice(dice);
            var values = dice.ToArray();
            if (rushDieIndex < 0 || rushDieIndex >= values.Length)
            {
                return values;
            }

            switch (rushDie)
            {
                case YachtRushRushDie.Mirror:
                    values[rushDieIndex] = 7 - values[rushDieIndex];
                    break;
                case YachtRushRushDie.Blank:
                    if (!blankUnlocked)
                    {
                        values[rushDieIndex] = 0;
                    }

                    break;
                case YachtRushRushDie.Anchor:
                case YachtRushRushDie.Storm:
                case YachtRushRushDie.Cracked:
                case YachtRushRushDie.None:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(rushDie), rushDie, null);
            }

            return values;
        }

        public static int MaxRollsForRule(YachtRushRollRule rollRule)
        {
            switch (rollRule)
            {
                case YachtRushRollRule.OneShot:
                    return 1;
                case YachtRushRollRule.SafeHarbor:
                    return 2;
                case YachtRushRollRule.Classic:
                case YachtRushRollRule.NoHolds:
                case YachtRushRollRule.MustHold2:
                case YachtRushRollRule.RerollAll:
                    return MaxRollsPerRound;
                default:
                    throw new ArgumentOutOfRangeException(nameof(rollRule), rollRule, null);
            }
        }

        public static bool CanHold(YachtRushRollRule rollRule)
        {
            return rollRule != YachtRushRollRule.NoHolds;
        }

        public static bool CanThrowWithRule(YachtRushRollRule rollRule, int rollCount, int heldCount)
        {
            if (rollCount >= MaxRollsForRule(rollRule))
            {
                return false;
            }

            return rollRule != YachtRushRollRule.MustHold2 || rollCount != 1 || heldCount >= 2;
        }

        public static bool ShouldRerollHeldDice(YachtRushRollRule rollRule)
        {
            return rollRule == YachtRushRollRule.RerollAll;
        }

        public static bool IsContractHandSatisfied(
            YachtRushContract contract,
            IReadOnlyList<int> dice,
            int rushDieIndex,
            YachtRushRushDie rushDie,
            int rerollsUsed,
            int lockedBeforeFinalThrow,
            int heldCount = 0)
        {
            ValidateScoringDice(dice);
            if (contract == YachtRushContract.None)
            {
                return false;
            }

            var contractDice = ContractDice(dice, rushDieIndex, rushDie);
            if (contractDice.Count == 0)
            {
                return false;
            }

            var counts = CountDice(contractDice);
            switch (contract)
            {
                case YachtRushContract.EvenFleet:
                    return contractDice.Count >= 4 && contractDice.All(value => value > 0 && value % 2 == 0);
                case YachtRushContract.OddCrew:
                    return contractDice.Count >= 4 && contractDice.All(value => value > 0 && value % 2 == 1);
                case YachtRushContract.LowTide:
                    return contractDice.Sum() <= 15;
                case YachtRushContract.HighTide:
                    return contractDice.Sum() >= 22;
                case YachtRushContract.TwinWake:
                    return counts.Count(count => count >= 2) >= 2;
                case YachtRushContract.BrokenRun:
                    return HasBrokenRun(contractDice);
                case YachtRushContract.CaptainPair:
                    return CountFace(counts, 6) >= 2;
                case YachtRushContract.CleanBowl:
                    return rerollsUsed == 0;
                default:
                    throw new ArgumentOutOfRangeException(nameof(contract), contract, null);
            }
        }

        public static YachtRushRollRuleInfo GetRollRule(YachtRushRollRule rollRule)
        {
            foreach (var item in RollRules)
            {
                if (item.Id == rollRule)
                {
                    return item;
                }
            }

            throw new ArgumentOutOfRangeException(nameof(rollRule), rollRule, null);
        }

        public static YachtRushRushDieInfo GetRushDie(YachtRushRushDie rushDie)
        {
            if (rushDie == YachtRushRushDie.None)
            {
                return new YachtRushRushDieInfo(YachtRushRushDie.None, "No Rush Die", "All dice score normally");
            }

            foreach (var item in RushDice)
            {
                if (item.Id == rushDie)
                {
                    return item;
                }
            }

            throw new ArgumentOutOfRangeException(nameof(rushDie), rushDie, null);
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
            if (contract == YachtRushContract.None)
            {
                return new YachtRushContractInfo(YachtRushContract.None, "Base Yacht", "No bonus hand this round", 0);
            }

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
                if (value > 0)
                {
                    counts[value] += 1;
                }
            }

            return counts;
        }

        private static int CountFace(IReadOnlyList<int> counts, int face)
        {
            return counts[face];
        }

        private static bool HasStraight(IReadOnlyList<int> dice, int length)
        {
            var faces = dice.Where(value => value > 0).Distinct().OrderBy(value => value).ToArray();
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

        private static bool HasBrokenRun(IReadOnlyList<int> dice)
        {
            var faces = dice.Where(value => value > 0).Distinct().OrderBy(value => value).ToArray();
            if (faces.Length < 4)
            {
                return false;
            }

            for (var start = 1; start <= 2; start += 1)
            {
                var inWindow = faces.Count(value => value >= start && value <= start + 4);
                if (inWindow >= 4)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsComboCategory(YachtRushCategory category)
        {
            switch (category)
            {
                case YachtRushCategory.FourOfAKind:
                case YachtRushCategory.FullHouse:
                case YachtRushCategory.SmallStraight:
                case YachtRushCategory.LargeStraight:
                case YachtRushCategory.Yacht:
                case YachtRushCategory.Chance:
                    return true;
                default:
                    return false;
            }
        }

        private static int AdjustedContractBonus(YachtRushContract contract, YachtRushRollRule rollRule)
        {
            var bonus = GetContract(contract).Bonus;
            return rollRule == YachtRushRollRule.SafeHarbor ? bonus + 3 : bonus;
        }

        private static List<int> ContractDice(IReadOnlyList<int> dice, int rushDieIndex, YachtRushRushDie rushDie)
        {
            var values = new List<int>(dice.Count);
            for (var index = 0; index < dice.Count; index += 1)
            {
                if (rushDie == YachtRushRushDie.Cracked && index == rushDieIndex)
                {
                    continue;
                }

                if (dice[index] > 0)
                {
                    values.Add(dice[index]);
                }
            }

            return values;
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

        private static void ValidateScoringDice(IReadOnlyList<int> dice)
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
                if (value < 0 || value > 6)
                {
                    throw new ArgumentOutOfRangeException(nameof(dice), "Dice values must be between 0 and 6.");
                }
            }
        }
    }
}
