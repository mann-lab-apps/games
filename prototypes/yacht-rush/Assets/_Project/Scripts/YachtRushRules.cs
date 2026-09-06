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

    public enum VoyageDeckZone
    {
        Overboard,
        Sail,
        Repair,
        Supply,
        Trade,
        Storm
    }

    public enum CaptainOrder
    {
        PushForward,
        SecureCargo,
        PatchBeforeDawn,
        AvoidStorm,
        TradeAtPort,
        RallyCrew
    }

    public enum CrewResource
    {
        Wind = 1,
        Hull = 2,
        Supply = 3,
        Crew = 4,
        Trade = 5,
        Chart = 6
    }

    public enum VoyageStrategy
    {
        TailwindRun,
        PatchTheHull,
        StockTheHold,
        RallyTheCrew,
        PortBargain,
        ReadTheStars,
        SafePassage,
        LongVoyage,
        RepairConvoy,
        TradeRoute,
        FullDeck,
        CaptainsGambit,
        CaptainsCall
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

    public readonly struct HarborYachtActionInfo
    {
        public HarborYachtActionInfo(YachtRushCategory category, string name, string pattern, string effect)
        {
            Category = category;
            Name = name;
            Pattern = pattern;
            Effect = effect;
        }

        public YachtRushCategory Category { get; }
        public string Name { get; }
        public string Pattern { get; }
        public string Effect { get; }
    }

    public readonly struct HarborYachtState
    {
        public HarborYachtState(int day, int routeProgress, int hull, int supplies, int contractScore)
        {
            Day = day;
            RouteProgress = routeProgress;
            Hull = hull;
            Supplies = supplies;
            ContractScore = contractScore;
        }

        public int Day { get; }
        public int RouteProgress { get; }
        public int Hull { get; }
        public int Supplies { get; }
        public int ContractScore { get; }
        public int Discovery => ContractScore;
    }

    public readonly struct HarborYachtActionEffect
    {
        public HarborYachtActionEffect(
            int routeDelta,
            int hullDelta,
            int suppliesDelta,
            int contractScoreDelta,
            int hazardDelta,
            string summary,
            bool isAvailable = true,
            string lockedReason = "")
        {
            RouteDelta = routeDelta;
            HullDelta = hullDelta;
            SuppliesDelta = suppliesDelta;
            ContractScoreDelta = contractScoreDelta;
            HazardDelta = hazardDelta;
            Summary = summary ?? string.Empty;
            IsAvailable = isAvailable;
            LockedReason = lockedReason ?? string.Empty;
        }

        public int RouteDelta { get; }
        public int HullDelta { get; }
        public int SuppliesDelta { get; }
        public int ContractScoreDelta { get; }
        public int HazardDelta { get; }
        public string Summary { get; }
        public bool IsAvailable { get; }
        public string LockedReason { get; }
    }

    public readonly struct HarborYachtRunResult
    {
        public HarborYachtRunResult(bool isComplete, bool isSuccess, string title)
        {
            IsComplete = isComplete;
            IsSuccess = isSuccess;
            Title = title ?? string.Empty;
        }

        public bool IsComplete { get; }
        public bool IsSuccess { get; }
        public string Title { get; }
    }

    public readonly struct VoyageDieLanding
    {
        public VoyageDieLanding(int value, VoyageDeckZone zone)
        {
            Value = Math.Max(1, Math.Min(6, value));
            Zone = zone;
        }

        public int Value { get; }
        public VoyageDeckZone Zone { get; }
        public bool IsOverboard => Zone == VoyageDeckZone.Overboard;
    }

    public readonly struct VoyageTurnResult
    {
        public VoyageTurnResult(
            int distanceDelta,
            int hullDelta,
            int supplyDelta,
            int discoveryDelta,
            int lostDice,
            string summary,
            string[] combos)
        {
            DistanceDelta = distanceDelta;
            HullDelta = hullDelta;
            SupplyDelta = supplyDelta;
            DiscoveryDelta = discoveryDelta;
            LostDice = lostDice;
            Summary = summary ?? string.Empty;
            Combos = combos ?? Array.Empty<string>();
        }

        public int DistanceDelta { get; }
        public int HullDelta { get; }
        public int SupplyDelta { get; }
        public int DiscoveryDelta { get; }
        public int LostDice { get; }
        public string Summary { get; }
        public string[] Combos { get; }
    }

    public readonly struct VoyageStrategyPreview
    {
        public VoyageStrategyPreview(
            VoyageStrategy strategy,
            bool isAvailable,
            string name,
            string condition,
            string have,
            IReadOnlyList<int> resourceCost,
            string effect,
            int distanceDelta,
            int hullDelta,
            int supplyDelta,
            int goldDelta,
            int upkeepReduction,
            bool stormShield)
        {
            Strategy = strategy;
            IsAvailable = isAvailable;
            Name = name ?? string.Empty;
            Condition = condition ?? string.Empty;
            Have = have ?? string.Empty;
            ResourceCost = resourceCost?.ToArray() ?? Array.Empty<int>();
            Effect = effect ?? string.Empty;
            DistanceDelta = distanceDelta;
            HullDelta = hullDelta;
            SupplyDelta = supplyDelta;
            GoldDelta = goldDelta;
            UpkeepReduction = upkeepReduction;
            StormShield = stormShield;
        }

        public VoyageStrategy Strategy { get; }
        public bool IsAvailable { get; }
        public string Name { get; }
        public string Condition { get; }
        public string Have { get; }
        public IReadOnlyList<int> ResourceCost { get; }
        public string Effect { get; }
        public int DistanceDelta { get; }
        public int HullDelta { get; }
        public int SupplyDelta { get; }
        public int GoldDelta { get; }
        public int UpkeepReduction { get; }
        public bool StormShield { get; }
    }

    public static class YachtRushRules
    {
        public const int DiceCount = 5;
        public const int MaxRollsPerRound = 3;
        public const int RoundCount = 12;
        public const int HarborTargetRoute = 120;
        public const int HarborMaxHull = 30;
        public const int HarborMaxSupplies = 24;
        public const int HarborStartingHull = 18;
        public const int HarborStartingSupplies = 8;

        public static readonly CaptainOrder[] CaptainOrders =
        {
            CaptainOrder.PushForward,
            CaptainOrder.SecureCargo,
            CaptainOrder.PatchBeforeDawn,
            CaptainOrder.AvoidStorm,
            CaptainOrder.TradeAtPort,
            CaptainOrder.RallyCrew
        };

        public static readonly CaptainOrder[] CoreCaptainOrders =
        {
            CaptainOrder.PushForward,
            CaptainOrder.PatchBeforeDawn,
            CaptainOrder.TradeAtPort
        };

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
            new YachtRushRushDieInfo(YachtRushRushDie.Anchor, "Anchor Hazard", "Auto-locks after landing"),
            new YachtRushRushDieInfo(YachtRushRushDie.Storm, "Storm Hazard", "Throws harder; rough seas hit hull"),
            new YachtRushRushDieInfo(YachtRushRushDie.Cracked, "Cracked Cargo", "Weakens combo contract scoring"),
            new YachtRushRushDieInfo(YachtRushRushDie.Mirror, "Shifting Current", "Flips value: 1<->6, 2<->5"),
            new YachtRushRushDieInfo(YachtRushRushDie.Blank, "Fog Hazard", "Selected die scores 0 unless order succeeds")
        };

        public static readonly HarborYachtActionInfo[] HarborActions =
        {
            new HarborYachtActionInfo(YachtRushCategory.Ones, "Tailwind", "1 Wind + 1 Wind + 4 Sail", "Distance +18, Supply -1"),
            new HarborYachtActionInfo(YachtRushCategory.Twos, "Stock Up", "2 Supply + 2 Supply + 5 Trade", "Supply +7, Distance +2"),
            new HarborYachtActionInfo(YachtRushCategory.Threes, "Patch Hull", "3 Repair + 3 Repair + 6 Crew", "Hull +6"),
            new HarborYachtActionInfo(YachtRushCategory.Fours, "Full Sail", "4 Sail + 4 Sail + 6 Crew", "Distance +18, Supply -1"),
            new HarborYachtActionInfo(YachtRushCategory.Fives, "Harbor Trade", "two 5 Trade + one 2 Supply", "Discovery +10, Supply +2"),
            new HarborYachtActionInfo(YachtRushCategory.Sixes, "Crew Vote", "two 6 Crew + any pair", "Distance +12, Hull +2, Supply +1"),
            new HarborYachtActionInfo(YachtRushCategory.FourOfAKind, "Balanced Watch", "one low pair + one high pair", "Distance +8, Hull +3, Supply +2"),
            new HarborYachtActionInfo(YachtRushCategory.FullHouse, "Supply Chain", "2 Supply + 3 Repair + 5 Trade", "Supply +5, Hull +3, Discovery +5"),
            new HarborYachtActionInfo(YachtRushCategory.SmallStraight, "Safe Passage", "1 Wind + 3 Repair + 6 Crew", "Distance +9, Hull +4"),
            new HarborYachtActionInfo(YachtRushCategory.LargeStraight, "Open Sea", "1-2-3-4 route sequence", "Distance +16, Supply -1"),
            new HarborYachtActionInfo(YachtRushCategory.Yacht, "Grand Voyage", "2-3-4-5-6 route sequence", "Distance +26, Discovery +10"),
            new HarborYachtActionInfo(YachtRushCategory.Chance, "Captain's Call", "any supply roll", "Fallback command")
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

        public static HarborYachtActionInfo GetHarborAction(YachtRushCategory category)
        {
            foreach (var item in HarborActions)
            {
                if (item.Category == category)
                {
                    return item;
                }
            }

            throw new ArgumentOutOfRangeException(nameof(category), category, null);
        }

        public static HarborYachtActionEffect PreviewHarborAction(
            YachtRushCategory category,
            YachtRushRoundScorePreview scorePreview,
            YachtRushRushDie hazardDie)
        {
            var dice = scorePreview.EffectiveDice.Length == DiceCount
                ? scorePreview.EffectiveDice
                : Array.Empty<int>();
            if (dice.Length != DiceCount)
            {
                return LockedVoyageCommand("Roll supplies first");
            }

            var counts = CountDice(dice);
            var total = dice.Sum();
            var route = 0;
            var hull = 0;
            var supplies = 0;
            var contract = 0;
            var available = true;
            var lockedReason = string.Empty;

            switch (category)
            {
                case YachtRushCategory.Ones:
                    available = CountFace(counts, 1) >= 2 && CountFace(counts, 4) >= 1;
                    lockedReason = "needs 1 Wind + 1 Wind + 4 Sail";
                    route = 12 + CountFace(counts, 1) * 2 + CountFace(counts, 4);
                    supplies = -1;
                    break;
                case YachtRushCategory.Twos:
                    available = CountFace(counts, 2) >= 2 && CountFace(counts, 5) >= 1;
                    lockedReason = "needs 2 Supply + 2 Supply + 5 Trade";
                    route = 2;
                    supplies = 5 + CountFace(counts, 2);
                    contract = 2;
                    break;
                case YachtRushCategory.Threes:
                    available = CountFace(counts, 3) >= 2 && CountFace(counts, 6) >= 1;
                    lockedReason = "needs 3 Repair + 3 Repair + 6 Crew";
                    hull = 4 + CountFace(counts, 3);
                    break;
                case YachtRushCategory.Fours:
                    available = CountFace(counts, 4) >= 2 && CountFace(counts, 6) >= 1;
                    lockedReason = "needs 4 Sail + 4 Sail + 6 Crew";
                    route = 14 + CountFace(counts, 4) * 2;
                    supplies = -1;
                    break;
                case YachtRushCategory.Fives:
                    available = CountFace(counts, 5) >= 2 && CountFace(counts, 2) >= 1;
                    lockedReason = "needs two 5 Trade + one 2 Supply";
                    route = 4;
                    supplies = 2;
                    contract = 10;
                    break;
                case YachtRushCategory.Sixes:
                    available = CountFace(counts, 6) >= 2 && HasSupportingPair(counts, 6);
                    lockedReason = "needs two 6 Crew + any pair";
                    route = 12;
                    hull = 2;
                    supplies = 1;
                    contract = 4;
                    break;
                case YachtRushCategory.FourOfAKind:
                    available = HasLowHighPair(counts);
                    lockedReason = "needs one low pair + one high pair";
                    route = 8;
                    hull = 3;
                    supplies = 2;
                    break;
                case YachtRushCategory.FullHouse:
                    available = HasAll(dice, 2, 3, 5);
                    lockedReason = "needs 2 Supply + 3 Repair + 5 Trade";
                    hull = 3;
                    supplies = 5;
                    contract = 5;
                    break;
                case YachtRushCategory.SmallStraight:
                    available = HasAll(dice, 1, 3, 6);
                    lockedReason = "needs 1 Wind + 3 Repair + 6 Crew";
                    route = 9;
                    hull = 4;
                    supplies = -1;
                    break;
                case YachtRushCategory.LargeStraight:
                    available = HasAll(dice, 1, 2, 3, 4);
                    lockedReason = "needs 1-2-3-4 route sequence";
                    route = 16;
                    supplies = -1;
                    break;
                case YachtRushCategory.Yacht:
                    available = HasAll(dice, 2, 3, 4, 5, 6);
                    lockedReason = "needs 2-3-4-5-6 route sequence";
                    route = 26;
                    supplies = -2;
                    contract = 10;
                    break;
                case YachtRushCategory.Chance:
                    available = true;
                    lockedReason = string.Empty;
                    route = Math.Max(3, total / 2);
                    hull = total <= 10 ? -2 : CountFace(counts, 3) > 0 ? 1 : 0;
                    supplies = total <= 10 ? -2 : CountFace(counts, 2) > 0 ? 2 : 0;
                    contract = CountFace(counts, 5) > 0 ? 2 : 0;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(category), category, null);
            }

            if (!available)
            {
                return new HarborYachtActionEffect(0, 0, 0, 0, 0, lockedReason, false, lockedReason);
            }

            var hazard = VoyageHazardDelta(dice);
            hull += hazard;
            return new HarborYachtActionEffect(
                route,
                hull,
                supplies,
                contract,
                hazard,
                BuildHarborEffectSummary(route, hull, supplies, contract));
        }

        private static HarborYachtActionEffect LockedVoyageCommand(string reason)
        {
            return new HarborYachtActionEffect(0, 0, 0, 0, 0, reason, false, reason);
        }

        public static HarborYachtState ApplyHarborAction(HarborYachtState state, HarborYachtActionEffect effect)
        {
            return new HarborYachtState(
                Math.Min(RoundCount, state.Day + 1),
                Math.Max(0, state.RouteProgress + effect.RouteDelta),
                Clamp(state.Hull + effect.HullDelta, 0, HarborMaxHull),
                Clamp(state.Supplies + effect.SuppliesDelta, 0, HarborMaxSupplies),
                Math.Max(0, state.ContractScore + effect.ContractScoreDelta));
        }

        public static HarborYachtRunResult EvaluateHarborRun(HarborYachtState state, int completedActions)
        {
            if (state.Hull <= 0)
            {
                return new HarborYachtRunResult(true, false, "Lost at Sea");
            }

            if (completedActions >= RoundCount || state.Day > RoundCount)
            {
                var success = state.Discovery >= 30 || state.RouteProgress >= HarborTargetRoute;
                return new HarborYachtRunResult(true, success, success ? "Voyage Complete" : "Drifted Home");
            }

            return new HarborYachtRunResult(false, false, "At Sea");
        }

        public static HarborYachtRunResult EvaluateVoyageRun(HarborYachtState state, int completedMonths)
        {
            if (state.Hull <= 0)
            {
                return new HarborYachtRunResult(true, false, "Lost at Sea");
            }

            if (state.Supplies <= 0)
            {
                return new HarborYachtRunResult(true, false, "Out of Supplies");
            }

            if (completedMonths >= RoundCount)
            {
                return new HarborYachtRunResult(true, true, "12-Month Voyage Complete");
            }

            return new HarborYachtRunResult(false, false, "At Sea");
        }

        public static int[] CountCrewResources(IReadOnlyList<int> dice)
        {
            ValidateDice(dice);
            var counts = new int[6];
            foreach (var value in dice)
            {
                counts[value - 1] += 1;
            }

            return counts;
        }

        public static string CrewResourceName(int face)
        {
            switch (face)
            {
                case 1:
                    return "Sail";
                case 2:
                    return "Hull";
                case 3:
                    return "Food";
                case 4:
                    return "Crew";
                case 5:
                    return "Gold";
                case 6:
                    return "Map";
                default:
                    throw new ArgumentOutOfRangeException(nameof(face), face, null);
            }
        }

        public static VoyageStrategyPreview[] AvailableVoyageStrategies(IReadOnlyList<int> resourceCounts)
        {
            var previews = AllVoyageStrategyPreviews(resourceCounts)
                .Where(preview => preview.IsAvailable)
                .OrderByDescending(StrategyPriority)
                .ThenBy(preview => preview.Name)
                .ToList();

            if (previews.Count < 4)
            {
                previews.Add(PreviewVoyageStrategy(VoyageStrategy.CaptainsCall, resourceCounts));
            }

            return previews.Take(6).ToArray();
        }

        public static VoyageStrategyPreview[] AllVoyageStrategyPreviews(IReadOnlyList<int> resourceCounts)
        {
            return AllVoyageStrategies()
                .Select(strategy => PreviewVoyageStrategy(strategy, resourceCounts))
                .OrderBy(preview => preview.IsAvailable ? 0 : 1)
                .ThenByDescending(StrategyPriority)
                .ThenBy(preview => preview.Name)
                .ToArray();
        }

        public static VoyageStrategyPreview PreviewVoyageStrategy(VoyageStrategy strategy, IReadOnlyList<int> resourceCounts)
        {
            ValidateResourceCounts(resourceCounts);
            var wind = resourceCounts[0];
            var hull = resourceCounts[1];
            var supply = resourceCounts[2];
            var crew = resourceCounts[3];
            var trade = resourceCounts[4];
            var chart = resourceCounts[5];
            var different = resourceCounts.Count(count => count > 0);
            var maxSame = resourceCounts.Max();
            var dominantFace = Array.IndexOf(resourceCounts.ToArray(), maxSame) + 1;
            var name = string.Empty;
            var condition = string.Empty;
            var available = false;
            var distance = 0;
            var hullDelta = 0;
            var supplyDelta = 0;
            var gold = 0;
            var upkeepReduction = 0;
            var stormShield = false;
            var cost = new int[6];

            switch (strategy)
            {
                case VoyageStrategy.TailwindRun:
                    name = "Tailwind Run";
                    condition = "Need Sail x2";
                    available = wind >= 2;
                    cost[0] = 2;
                    distance = 8 + Math.Min(3, Math.Max(0, wind - 2)) * 4;
                    break;
                case VoyageStrategy.PatchTheHull:
                    name = "Patch the Hull";
                    condition = "Need Hull x2";
                    available = hull >= 2;
                    cost[1] = 2;
                    hullDelta = 5 + Math.Min(2, Math.Max(0, hull - 2)) * 2;
                    stormShield = hull >= 3;
                    break;
                case VoyageStrategy.StockTheHold:
                    name = "Stock the Hold";
                    condition = "Need Food x2";
                    available = supply >= 2;
                    cost[2] = 2;
                    supplyDelta = 5 + Math.Min(2, Math.Max(0, supply - 2)) * 2;
                    break;
                case VoyageStrategy.RallyTheCrew:
                    name = "Rally the Crew";
                    condition = "Need Crew x2";
                    available = crew >= 2;
                    cost[3] = 2;
                    hullDelta = 2;
                    supplyDelta = 2;
                    break;
                case VoyageStrategy.PortBargain:
                    name = "Port Bargain";
                    condition = "Need Gold x2";
                    available = trade >= 2;
                    cost[4] = 2;
                    gold = 8 + Math.Min(2, Math.Max(0, trade - 2)) * 4;
                    supplyDelta = 2;
                    break;
                case VoyageStrategy.ReadTheStars:
                    name = "Read the Stars";
                    condition = "Need Map x2";
                    available = chart >= 2;
                    cost[5] = 2;
                    distance = 5 + Math.Min(2, Math.Max(0, chart - 2)) * 3;
                    stormShield = true;
                    break;
                case VoyageStrategy.SafePassage:
                    name = "Safe Passage";
                    condition = "Need Sail + Hull + Map";
                    available = wind >= 1 && hull >= 1 && chart >= 1;
                    cost[0] = 1;
                    cost[1] = 1;
                    cost[5] = 1;
                    distance = 10;
                    hullDelta = 1;
                    stormShield = true;
                    break;
                case VoyageStrategy.LongVoyage:
                    name = "Long Voyage";
                    condition = "Need Sail + Food + Map";
                    available = wind >= 1 && supply >= 1 && chart >= 1;
                    cost[0] = 1;
                    cost[2] = 1;
                    cost[5] = 1;
                    distance = 16;
                    supplyDelta = -1;
                    break;
                case VoyageStrategy.RepairConvoy:
                    name = "Repair Convoy";
                    condition = "Need Hull + Crew + Food";
                    available = hull >= 1 && crew >= 1 && supply >= 1;
                    cost[1] = 1;
                    cost[2] = 1;
                    cost[3] = 1;
                    hullDelta = 4;
                    supplyDelta = 2;
                    break;
                case VoyageStrategy.TradeRoute:
                    name = "Trade Route";
                    condition = "Need Gold + Map + Food";
                    available = trade >= 1 && chart >= 1 && supply >= 1;
                    cost[2] = 1;
                    cost[4] = 1;
                    cost[5] = 1;
                    gold = 10;
                    distance = 4;
                    break;
                case VoyageStrategy.FullDeck:
                    name = "Full Deck";
                    condition = "Need 5 resource types";
                    available = different >= 5;
                    for (var face = 0; face < cost.Length; face += 1)
                    {
                        cost[face] = resourceCounts[face] > 0 ? 1 : 0;
                    }

                    distance = 12;
                    hullDelta = 2;
                    supplyDelta = 2;
                    gold = 2;
                    break;
                case VoyageStrategy.CaptainsGambit:
                    name = "Captain's Gambit";
                    condition = "Need Sail/Crew/Gold/Map x3";
                    var gambitCounts = new[] { wind, 0, 0, crew, trade, chart };
                    var gambitMaxSame = gambitCounts.Max();
                    var gambitDominantFace = Array.IndexOf(gambitCounts, gambitMaxSame) + 1;
                    available = gambitMaxSame >= 3;
                    if (available)
                    {
                        cost[gambitDominantFace - 1] = 3;
                    }

                    distance = gambitDominantFace == 1 || gambitDominantFace == 6 ? 18 : 6;
                    supplyDelta = gambitDominantFace == 4 ? 3 : -1;
                    gold = gambitDominantFace == 5 ? 12 : 0;
                    break;
                case VoyageStrategy.CaptainsCall:
                    name = "Captain's Call";
                    condition = "Any dice";
                    available = true;
                    distance = Math.Min(8, Math.Max(3, wind * 2 + chart));
                    hullDelta = hull > 0 ? 1 : 0;
                    supplyDelta = supply > 0 ? 1 : 0;
                    gold = Math.Min(3, trade);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(strategy), strategy, null);
            }

            return new VoyageStrategyPreview(
                strategy,
                available,
                name,
                condition,
                BuildResourceHaveLine(resourceCounts),
                cost,
                BuildVoyageStrategyEffectSummary(distance, hullDelta, supplyDelta, gold, upkeepReduction, stormShield),
                distance,
                hullDelta,
                supplyDelta,
                gold,
                upkeepReduction,
                stormShield);
        }

        public static HarborYachtState ApplyVoyageStrategy(
            HarborYachtState state,
            VoyageStrategyPreview preview,
            int completedMonths,
            out int supplyUpkeep,
            out int stormDamage)
        {
            supplyUpkeep = 0;
            stormDamage = 0;

            return new HarborYachtState(
                Math.Min(RoundCount, state.Day + 1),
                Math.Max(0, state.RouteProgress + preview.DistanceDelta),
                Clamp(state.Hull + preview.HullDelta - stormDamage, 0, HarborMaxHull),
                Clamp(state.Supplies + preview.SupplyDelta - supplyUpkeep, 0, HarborMaxSupplies),
                Math.Max(0, state.ContractScore + preview.GoldDelta));
        }

        private static IEnumerable<VoyageStrategy> AllVoyageStrategies()
        {
            yield return VoyageStrategy.TailwindRun;
            yield return VoyageStrategy.PatchTheHull;
            yield return VoyageStrategy.StockTheHold;
            yield return VoyageStrategy.RallyTheCrew;
            yield return VoyageStrategy.PortBargain;
            yield return VoyageStrategy.ReadTheStars;
            yield return VoyageStrategy.SafePassage;
            yield return VoyageStrategy.LongVoyage;
            yield return VoyageStrategy.RepairConvoy;
            yield return VoyageStrategy.TradeRoute;
            yield return VoyageStrategy.FullDeck;
            yield return VoyageStrategy.CaptainsGambit;
        }

        private static int StrategyPriority(VoyageStrategyPreview preview)
        {
            return preview.DistanceDelta * 2 +
                Math.Max(0, preview.HullDelta) * 3 +
                Math.Max(0, preview.SupplyDelta) * 3 +
                preview.GoldDelta * 2 +
                Math.Abs(Math.Min(0, preview.SupplyDelta)) * 2;
        }

        private static string BuildResourceHaveLine(IReadOnlyList<int> resourceCounts)
        {
            ValidateResourceCounts(resourceCounts);
            var parts = new List<string>(6);
            for (var face = 1; face <= resourceCounts.Count; face += 1)
            {
                var count = resourceCounts[face - 1];
                if (count > 0)
                {
                    parts.Add($"{CrewResourceName(face)} x{count}");
                }
            }

            return parts.Count == 0 ? "No resources" : string.Join("  ", parts);
        }

        private static string BuildVoyageStrategyEffectSummary(
            int distance,
            int hull,
            int supplies,
            int gold,
            int upkeepReduction,
            bool stormShield)
        {
            var parts = new List<string>(6);
            AppendDelta(parts, "Dist", distance);
            AppendDelta(parts, "Hull", hull);
            AppendDelta(parts, "Supply", supplies);
            AppendDelta(parts, "Gold", gold);
            return parts.Count == 0 ? "Small captain order" : string.Join("  ", parts);
        }

        private static void ValidateResourceCounts(IReadOnlyList<int> resourceCounts)
        {
            if (resourceCounts == null)
            {
                throw new ArgumentNullException(nameof(resourceCounts));
            }

            if (resourceCounts.Count != 6)
            {
                throw new ArgumentException("Voyage strategy previews expect six resource counts.", nameof(resourceCounts));
            }

            foreach (var count in resourceCounts)
            {
                if (count < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(resourceCounts), "Resource counts cannot be negative.");
                }
            }
        }

        public static VoyageDeckZone ZoneForPosition(float x, float z)
        {
            if (x < -4.9f || x > 4.9f || z < -2.25f || z > 2.85f)
            {
                return VoyageDeckZone.Overboard;
            }

            if (x < -2.9f)
            {
                return VoyageDeckZone.Sail;
            }

            if (x < -1f)
            {
                return VoyageDeckZone.Repair;
            }

            if (x < 1f)
            {
                return VoyageDeckZone.Supply;
            }

            if (x < 2.9f)
            {
                return VoyageDeckZone.Trade;
            }

            return VoyageDeckZone.Storm;
        }

        public static CaptainOrder[] OrdersForMonth(int month)
        {
            var start = Math.Max(0, month - 1) % CaptainOrders.Length;
            return new[]
            {
                CaptainOrders[start],
                CaptainOrders[(start + 2) % CaptainOrders.Length],
                CaptainOrders[(start + 4) % CaptainOrders.Length]
            };
        }

        public static CaptainOrder[] CoreOrders()
        {
            return CoreCaptainOrders.ToArray();
        }

        public static CaptainOrder[] OrdersForLandings(IEnumerable<VoyageDieLanding> dice)
        {
            var landings = dice?.ToArray() ?? Array.Empty<VoyageDieLanding>();
            return CaptainOrders
                .Select((order, index) => new
                {
                    Order = order,
                    Score = CaptainOrderRelevance(order, landings) * 100 - index
                })
                .OrderByDescending(item => item.Score)
                .Take(3)
                .Select(item => item.Order)
                .ToArray();
        }

        public static string CaptainOrderReason(CaptainOrder order, IEnumerable<VoyageDieLanding> dice)
        {
            switch (order)
            {
                case CaptainOrder.PushForward:
                    return "Uses dice in Sail to push Distance.";
                case CaptainOrder.SecureCargo:
                    return "Protects Supplies from bad landings.";
                case CaptainOrder.PatchBeforeDawn:
                    return "Uses Repair dice and reduces Storm damage.";
                case CaptainOrder.AvoidStorm:
                    return "Cancels most Storm damage.";
                case CaptainOrder.TradeAtPort:
                    return "Uses Trade and Supply dice for rewards.";
                case CaptainOrder.RallyCrew:
                    return "Turns a messy spread into a small gain.";
                default:
                    throw new ArgumentOutOfRangeException(nameof(order), order, null);
            }
        }

        public static string CaptainOrderName(CaptainOrder order)
        {
            switch (order)
            {
                case CaptainOrder.PushForward:
                    return "Sail Farther";
                case CaptainOrder.SecureCargo:
                    return "Secure Cargo";
                case CaptainOrder.PatchBeforeDawn:
                    return "Secure Ship";
                case CaptainOrder.AvoidStorm:
                    return "Avoid Storm";
                case CaptainOrder.TradeAtPort:
                    return "Trade & Stock";
                case CaptainOrder.RallyCrew:
                    return "Rally Crew";
                default:
                    throw new ArgumentOutOfRangeException(nameof(order), order, null);
            }
        }

        public static string CaptainOrderDescription(CaptainOrder order)
        {
            switch (order)
            {
                case CaptainOrder.PushForward:
                    return "Distance plan";
                case CaptainOrder.SecureCargo:
                    return "Protect supplies from bad landings.";
                case CaptainOrder.PatchBeforeDawn:
                    return "Safety plan";
                case CaptainOrder.AvoidStorm:
                    return "Cancel most storm damage this month.";
                case CaptainOrder.TradeAtPort:
                    return "Resource plan";
                case CaptainOrder.RallyCrew:
                    return "A flexible order when the roll is messy.";
                default:
                    throw new ArgumentOutOfRangeException(nameof(order), order, null);
            }
        }

        private static int CaptainOrderRelevance(CaptainOrder order, IReadOnlyList<VoyageDieLanding> landings)
        {
            var sail = ZoneCount(landings, VoyageDeckZone.Sail);
            var repair = ZoneCount(landings, VoyageDeckZone.Repair);
            var supply = ZoneCount(landings, VoyageDeckZone.Supply);
            var trade = ZoneCount(landings, VoyageDeckZone.Trade);
            var storm = ZoneCount(landings, VoyageDeckZone.Storm);
            var overboard = ZoneCount(landings, VoyageDeckZone.Overboard);
            var sailSum = ZoneSum(landings, VoyageDeckZone.Sail);
            var tradeSum = ZoneSum(landings, VoyageDeckZone.Trade);
            var stormSum = ZoneSum(landings, VoyageDeckZone.Storm);

            switch (order)
            {
                case CaptainOrder.PushForward:
                    return sail * 8 + sailSum + (sail >= 2 && storm == 0 ? 12 : 0);
                case CaptainOrder.SecureCargo:
                    return overboard * 14 + supply * 6 + storm * 2;
                case CaptainOrder.PatchBeforeDawn:
                    return repair * 9 + storm * 4 + overboard * 3;
                case CaptainOrder.AvoidStorm:
                    return storm * 10 + stormSum + overboard * 4;
                case CaptainOrder.TradeAtPort:
                    return trade * 9 + tradeSum;
                case CaptainOrder.RallyCrew:
                    return DistinctLandingZones(landings) * 5 + (sail + repair + supply + trade == 0 ? 10 : 0);
                default:
                    throw new ArgumentOutOfRangeException(nameof(order), order, null);
            }
        }

        private static int ZoneCount(IEnumerable<VoyageDieLanding> landings, VoyageDeckZone zone)
        {
            return landings.Count(landing => landing.Zone == zone);
        }

        private static int ZoneSum(IEnumerable<VoyageDieLanding> landings, VoyageDeckZone zone)
        {
            return landings.Where(landing => landing.Zone == zone).Sum(landing => landing.Value);
        }

        private static int DistinctLandingZones(IEnumerable<VoyageDieLanding> landings)
        {
            return landings.Select(landing => landing.Zone).Distinct().Count();
        }

        public static VoyageTurnResult ResolveVoyageTurn(IEnumerable<VoyageDieLanding> dice, CaptainOrder order)
        {
            var landings = dice?.ToArray() ?? Array.Empty<VoyageDieLanding>();
            var sailDice = landings.Where(landing => landing.Zone == VoyageDeckZone.Sail).ToArray();
            var repairDice = landings.Where(landing => landing.Zone == VoyageDeckZone.Repair).ToArray();
            var supplyDice = landings.Where(landing => landing.Zone == VoyageDeckZone.Supply).ToArray();
            var tradeDice = landings.Where(landing => landing.Zone == VoyageDeckZone.Trade).ToArray();
            var stormDice = landings.Where(landing => landing.Zone == VoyageDeckZone.Storm).ToArray();
            var overboardDice = landings.Where(landing => landing.Zone == VoyageDeckZone.Overboard).ToArray();
            var combos = new List<string>();

            var distance = sailDice.Sum(landing => landing.Value);
            var hull = repairDice.Sum(landing => Math.Max(1, landing.Value / 2));
            var supplies = supplyDice.Sum(landing => Math.Max(1, landing.Value / 2)) + supplyDice.Length;
            var discovery = tradeDice.Sum(landing => landing.Value);
            var stormDamage = stormDice.Sum(landing => Math.Max(1, (landing.Value + 1) / 2));
            var lostDice = overboardDice.Length;

            if (tradeDice.Length > 0)
            {
                distance += tradeDice.Length;
            }

            hull -= stormDamage + lostDice;
            supplies -= lostDice;

            if (sailDice.Length >= 2 && stormDice.Length == 0)
            {
                distance += 6;
                combos.Add("Clean Sail");
            }

            if (stormDice.Length > 0 && repairDice.Length > 0)
            {
                hull += 3;
                combos.Add("Emergency Repair");
            }

            if (supplyDice.Length >= 2)
            {
                supplies += 4;
                combos.Add("Supply Drop");
            }

            if (tradeDice.Any(landing => landing.Value >= 5))
            {
                discovery += 5;
                combos.Add("Lucky Harbor");
            }

            if (stormDice.Any(landing => landing.Value >= 5))
            {
                hull -= 2;
                combos.Add("Risky Throw");
            }

            if (lostDice > 0)
            {
                combos.Add("Overboard");
            }

            switch (order)
            {
                case CaptainOrder.PushForward:
                    distance += 6 + sailDice.Length * 2;
                    break;
                case CaptainOrder.SecureCargo:
                    supplies += Math.Max(3, lostDice + stormDice.Length);
                    break;
                case CaptainOrder.PatchBeforeDawn:
                    hull += 4 + repairDice.Length + Math.Min(4, stormDamage);
                    break;
                case CaptainOrder.AvoidStorm:
                    hull += Math.Min(7, stormDamage + (stormDice.Any(landing => landing.Value >= 5) ? 2 : 0));
                    break;
                case CaptainOrder.TradeAtPort:
                    discovery += 5 + tradeDice.Length * 2;
                    supplies += Math.Max(0, supplyDice.Length - stormDice.Length);
                    break;
                case CaptainOrder.RallyCrew:
                    distance += landings.Where(landing => !landing.IsOverboard).Select(landing => landing.Value).DefaultIfEmpty(0).Min();
                    supplies += 1;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(order), order, null);
            }

            return new VoyageTurnResult(
                distance,
                hull,
                supplies,
                discovery,
                lostDice,
                BuildVoyageTurnSummary(distance, hull, supplies, discovery, combos),
                combos.ToArray());
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
                return new YachtRushRushDieInfo(YachtRushRushDie.None, "Clear Water", "All dice score normally");
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
                return new YachtRushContractInfo(YachtRushContract.None, "Open Harbor", "No port order this day", 0);
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

        private static bool HasAll(IReadOnlyList<int> dice, params int[] requiredValues)
        {
            foreach (var required in requiredValues)
            {
                if (!dice.Contains(required))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool HasLowHighPair(IReadOnlyList<int> counts)
        {
            var lowPair = false;
            var highPair = false;
            for (var face = 1; face <= 6; face += 1)
            {
                if (CountFace(counts, face) < 2)
                {
                    continue;
                }

                if (face <= 3)
                {
                    lowPair = true;
                }
                else
                {
                    highPair = true;
                }
            }

            return lowPair && highPair;
        }

        private static bool HasSupportingPair(IReadOnlyList<int> counts, int excludedFace)
        {
            for (var face = 1; face <= 6; face += 1)
            {
                if (face != excludedFace && CountFace(counts, face) >= 2)
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

        private static int HazardVoyageDelta(YachtRushRushDie hazardDie)
        {
            switch (hazardDie)
            {
                case YachtRushRushDie.Storm:
                    return -2;
                case YachtRushRushDie.Cracked:
                    return -1;
                case YachtRushRushDie.Anchor:
                case YachtRushRushDie.Mirror:
                case YachtRushRushDie.Blank:
                case YachtRushRushDie.None:
                    return 0;
                default:
                    throw new ArgumentOutOfRangeException(nameof(hazardDie), hazardDie, null);
            }
        }

        private static int VoyageHazardDelta(IReadOnlyList<int> dice)
        {
            var total = dice.Sum();
            var counts = CountDice(dice);
            if (total <= 9)
            {
                return -2;
            }

            if (counts.Any(count => count >= 4))
            {
                return -1;
            }

            return 0;
        }

        private static string BuildHarborEffectSummary(int route, int hull, int supplies, int contract)
        {
            var parts = new List<string>(4);
            AppendDelta(parts, "Dist", route);
            AppendDelta(parts, "Hull", hull);
            AppendDelta(parts, "Supply", supplies);
            AppendDelta(parts, "Discovery", contract);
            return parts.Count == 0 ? "No voyage gain" : string.Join("  ", parts);
        }

        private static string BuildVoyageTurnSummary(int distance, int hull, int supplies, int discovery, IReadOnlyCollection<string> combos)
        {
            var parts = new List<string>(5);
            AppendDelta(parts, "Dist", distance);
            AppendDelta(parts, "Hull", hull);
            AppendDelta(parts, "Supply", supplies);
            AppendDelta(parts, "Discovery", discovery);
            if (combos != null && combos.Count > 0)
            {
                parts.Add(string.Join(", ", combos));
            }

            return parts.Count == 0 ? "No clear order" : string.Join("  ", parts);
        }

        private static void AppendDelta(ICollection<string> parts, string label, int value)
        {
            if (value == 0)
            {
                return;
            }

            parts.Add($"{label} {(value > 0 ? "+" : string.Empty)}{value}");
        }

        private static int Clamp(int value, int minimum, int maximum)
        {
            return Math.Max(minimum, Math.Min(maximum, value));
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
