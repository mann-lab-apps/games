using System;
using System.Collections.Generic;
using System.Linq;

namespace MannLab.Games.SensitiveBarista
{
    public enum BaristaIngredient
    {
        Ice,
        Shot,
        Water,
        Milk,
        Syrup
    }

    public enum BaristaOrderId
    {
        SoftChill,
        LightSweet,
        LongVelvet,
        DenseCalm,
        CloudyLatte,
        BrightRain,
        QuietDessert,
        CleanMorning,
        ThinMoon,
        WarmShadow,
        ClearBitter,
        SnowDessert,
        MutedCaramel,
        ShortFocus,
        SlowCloud,
        LowSignal
    }

    public readonly struct IngredientAmounts
    {
        public IngredientAmounts(float ice, float shot, float water, float milk, float syrup)
        {
            Ice = Math.Max(0f, ice);
            Shot = Math.Max(0f, shot);
            Water = Math.Max(0f, water);
            Milk = Math.Max(0f, milk);
            Syrup = Math.Max(0f, syrup);
        }

        public float Ice { get; }
        public float Shot { get; }
        public float Water { get; }
        public float Milk { get; }
        public float Syrup { get; }
        public float Total => Ice + Shot + Water + Milk + Syrup;

        public float this[BaristaIngredient ingredient]
        {
            get
            {
                switch (ingredient)
                {
                    case BaristaIngredient.Ice:
                        return Ice;
                    case BaristaIngredient.Shot:
                        return Shot;
                    case BaristaIngredient.Water:
                        return Water;
                    case BaristaIngredient.Milk:
                        return Milk;
                    case BaristaIngredient.Syrup:
                        return Syrup;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(ingredient), ingredient, null);
                }
            }
        }

        public IngredientAmounts Add(BaristaIngredient ingredient, float amount)
        {
            switch (ingredient)
            {
                case BaristaIngredient.Ice:
                    return new IngredientAmounts(Ice + amount, Shot, Water, Milk, Syrup);
                case BaristaIngredient.Shot:
                    return new IngredientAmounts(Ice, Shot + amount, Water, Milk, Syrup);
                case BaristaIngredient.Water:
                    return new IngredientAmounts(Ice, Shot, Water + amount, Milk, Syrup);
                case BaristaIngredient.Milk:
                    return new IngredientAmounts(Ice, Shot, Water, Milk + amount, Syrup);
                case BaristaIngredient.Syrup:
                    return new IngredientAmounts(Ice, Shot, Water, Milk, Syrup + amount);
                default:
                    throw new ArgumentOutOfRangeException(nameof(ingredient), ingredient, null);
            }
        }

        public float[] ToArray()
        {
            return new[] { Ice, Shot, Water, Milk, Syrup };
        }
    }

    public readonly struct BaristaOrder
    {
        public BaristaOrder(
            BaristaOrderId id,
            string customerLine,
            string memoName,
            string memoRatio,
            IngredientAmounts target,
            float idealTotal,
            float totalTolerance,
            BaristaIngredient primaryIngredient,
            BaristaIngredient secondaryIngredient,
            string tasteHint)
        {
            Id = id;
            CustomerLine = customerLine;
            MemoName = memoName;
            MemoRatio = memoRatio;
            Target = target;
            IdealTotal = idealTotal;
            TotalTolerance = totalTolerance;
            PrimaryIngredient = primaryIngredient;
            SecondaryIngredient = secondaryIngredient;
            TasteHint = tasteHint;
        }

        public BaristaOrderId Id { get; }
        public string CustomerLine { get; }
        public string MemoName { get; }
        public string MemoRatio { get; }
        public IngredientAmounts Target { get; }
        public float IdealTotal { get; }
        public float TotalTolerance { get; }
        public BaristaIngredient PrimaryIngredient { get; }
        public BaristaIngredient SecondaryIngredient { get; }
        public string TasteHint { get; }
    }

    public readonly struct BaristaScore
    {
        public BaristaScore(
            int roundScore,
            float ratioScore,
            float totalScore,
            float wastePenalty,
            float syrupPenalty,
            float missingPenalty,
            string balanceGrade,
            string volumeGrade,
            string cleanlinessGrade,
            string comment,
            string actualRatio,
            string targetRatio)
        {
            RoundScore = roundScore;
            RatioScore = ratioScore;
            TotalScore = totalScore;
            WastePenalty = wastePenalty;
            SyrupPenalty = syrupPenalty;
            MissingPenalty = missingPenalty;
            BalanceGrade = balanceGrade;
            VolumeGrade = volumeGrade;
            CleanlinessGrade = cleanlinessGrade;
            Comment = comment;
            ActualRatio = actualRatio;
            TargetRatio = targetRatio;
        }

        public int RoundScore { get; }
        public float RatioScore { get; }
        public float TotalScore { get; }
        public float WastePenalty { get; }
        public float SyrupPenalty { get; }
        public float MissingPenalty { get; }
        public string BalanceGrade { get; }
        public string VolumeGrade { get; }
        public string CleanlinessGrade { get; }
        public string Comment { get; }
        public string ActualRatio { get; }
        public string TargetRatio { get; }
    }

    public static class SensitiveBaristaRules
    {
        public const int RoundCount = 10;
        public const float CupCapacity = 100f;
        public const float MinimumPlayableAmount = 4f;

        public static readonly BaristaIngredient[] Ingredients =
        {
            BaristaIngredient.Ice,
            BaristaIngredient.Shot,
            BaristaIngredient.Water,
            BaristaIngredient.Milk,
            BaristaIngredient.Syrup
        };

        public static readonly BaristaOrder[] Orders =
        {
            new BaristaOrder(
                BaristaOrderId.SoftChill,
                "Iced Latte - less syrup, extra chill.",
                "Iced Latte",
                "Ice:Shot:Milk = 3:2:5",
                new IngredientAmounts(30f, 20f, 0f, 48f, 2f),
                82f,
                16f,
                BaristaIngredient.Milk,
                BaristaIngredient.Ice,
                "Classic iced latte base; keep syrup barely there."),
            new BaristaOrder(
                BaristaOrderId.LightSweet,
                "Americano - a little sweet, keep it light.",
                "Iced Americano",
                "Ice:Shot:Water = 3:2:6",
                new IngredientAmounts(24f, 18f, 52f, 0f, 6f),
                76f,
                18f,
                BaristaIngredient.Water,
                BaristaIngredient.Syrup,
                "Americano stays water-forward; syrup is a small adjustment."),
            new BaristaOrder(
                BaristaOrderId.LongVelvet,
                "Cafe Latte - soft, with one small syrup pump.",
                "Cafe Latte",
                "Shot:Milk = 1:3",
                new IngredientAmounts(0f, 24f, 0f, 68f, 8f),
                86f,
                14f,
                BaristaIngredient.Milk,
                BaristaIngredient.Shot,
                "Latte is mostly milk; one small pump is enough."),
            new BaristaOrder(
                BaristaOrderId.DenseCalm,
                "Flat White - strong, but not heavy.",
                "Flat White",
                "Shot:Milk = 2:3",
                new IngredientAmounts(0f, 38f, 0f, 58f, 4f),
                78f,
                13f,
                BaristaIngredient.Shot,
                BaristaIngredient.Milk,
                "Shot is the center; milk rounds it without taking over."),
            new BaristaOrder(
                BaristaOrderId.CloudyLatte,
                "Cold Brew Latte - milkier than usual, low ice.",
                "Cold Brew Latte",
                "Ice:Shot:Milk = 2:2:6",
                new IngredientAmounts(16f, 20f, 0f, 60f, 4f),
                88f,
                12f,
                BaristaIngredient.Milk,
                BaristaIngredient.Shot,
                "Let milk make it hazy; ice stays modest."),
            new BaristaOrder(
                BaristaOrderId.BrightRain,
                "Iced Americano - more water, syrup out.",
                "Iced Americano",
                "Ice:Shot:Water = 3:2:6",
                new IngredientAmounts(26f, 18f, 56f, 0f, 0f),
                84f,
                13f,
                BaristaIngredient.Water,
                BaristaIngredient.Ice,
                "Water and ice sharpen it; sweetness should almost vanish."),
            new BaristaOrder(
                BaristaOrderId.QuietDessert,
                "Vanilla Latte - sweet, but not sticky.",
                "Vanilla Latte",
                "Shot:Milk:Syrup = 2:6:1",
                new IngredientAmounts(6f, 20f, 0f, 62f, 12f),
                80f,
                12f,
                BaristaIngredient.Milk,
                BaristaIngredient.Syrup,
                "Sweetness is welcome; too much syrup takes over."),
            new BaristaOrder(
                BaristaOrderId.CleanMorning,
                "Morning Americano - no milk, no syrup.",
                "Morning Americano",
                "Shot:Water = 2:7",
                new IngredientAmounts(0f, 22f, 78f, 0f, 0f),
                74f,
                15f,
                BaristaIngredient.Water,
                BaristaIngredient.Shot,
                "Mostly water; bitterness should pass quickly."),
            new BaristaOrder(
                BaristaOrderId.ThinMoon,
                "Light Americano - extra ice, weak shot.",
                "Light Americano",
                "Ice:Shot:Water = 4:1:5",
                new IngredientAmounts(36f, 10f, 54f, 0f, 0f),
                72f,
                11f,
                BaristaIngredient.Ice,
                BaristaIngredient.Water,
                "Ice and water lead; shot stays in shadow."),
            new BaristaOrder(
                BaristaOrderId.WarmShadow,
                "Milk Coffee - less ice, deeper shot.",
                "Milk Coffee",
                "Shot:Milk = 3:5",
                new IngredientAmounts(4f, 34f, 0f, 58f, 4f),
                78f,
                10f,
                BaristaIngredient.Milk,
                BaristaIngredient.Shot,
                "Almost no ice; let milk and shot do the work."),
            new BaristaOrder(
                BaristaOrderId.ClearBitter,
                "Black Coffee - syrup out, clear bitter finish.",
                "Black Coffee",
                "Shot:Water = 3:5",
                new IngredientAmounts(0f, 36f, 64f, 0f, 0f),
                76f,
                10f,
                BaristaIngredient.Shot,
                BaristaIngredient.Water,
                "No sweetness; keep only water and shot."),
            new BaristaOrder(
                BaristaOrderId.SnowDessert,
                "Iced Milk - extra cold, a little vanilla.",
                "Iced Milk",
                "Ice:Milk:Syrup = 3:6:1",
                new IngredientAmounts(28f, 0f, 0f, 62f, 10f),
                84f,
                10f,
                BaristaIngredient.Milk,
                BaristaIngredient.Syrup,
                "Cold and sweet, but milk should be bigger."),
            new BaristaOrder(
                BaristaOrderId.MutedCaramel,
                "Caramel Latte - just a hint of syrup.",
                "Caramel Latte",
                "Shot:Milk:Syrup = 2:6:1",
                new IngredientAmounts(4f, 24f, 0f, 62f, 10f),
                80f,
                9f,
                BaristaIngredient.Milk,
                BaristaIngredient.Shot,
                "Balance milk and shot before sweetness."),
            new BaristaOrder(
                BaristaOrderId.ShortFocus,
                "Cortado - short cup, strong shot.",
                "Cortado",
                "Shot:Milk = 1:1",
                new IngredientAmounts(0f, 48f, 0f, 48f, 4f),
                62f,
                8f,
                BaristaIngredient.Shot,
                BaristaIngredient.Syrup,
                "Keep volume small; shot speaks first."),
            new BaristaOrder(
                BaristaOrderId.SlowCloud,
                "Cafe au Lait - milk heavy, syrup light.",
                "Cafe au Lait",
                "Water:Milk:Shot = 2:5:1",
                new IngredientAmounts(0f, 14f, 24f, 58f, 4f),
                88f,
                9f,
                BaristaIngredient.Milk,
                BaristaIngredient.Water,
                "Milk is wide; water makes it drift."),
            new BaristaOrder(
                BaristaOrderId.LowSignal,
                "Thin Americano - light water, two shots.",
                "Thin Americano",
                "Ice:Shot:Water = 2:2:5",
                new IngredientAmounts(18f, 22f, 54f, 0f, 4f),
                70f,
                8f,
                BaristaIngredient.Water,
                BaristaIngredient.Shot,
                "Thin, but the shot still points somewhere.")
        };

        public static readonly BaristaOrder[] GeneratedOrders = BuildGeneratedOrders();

        public static BaristaOrder OrderForRound(int roundNumber)
        {
            var clampedRound = Math.Max(1, roundNumber);
            return GeneratedOrders[(clampedRound - 1) % GeneratedOrders.Length];
        }

        public static BaristaOrder[] CreateRunOrders(int roundCount, int seed)
        {
            var count = Math.Max(1, roundCount);
            var pool = GeneratedOrders;
            var random = new Random(seed);
            var indices = Enumerable.Range(0, pool.Length).ToArray();
            for (var index = indices.Length - 1; index > 0; index -= 1)
            {
                var swapIndex = random.Next(index + 1);
                var value = indices[index];
                indices[index] = indices[swapIndex];
                indices[swapIndex] = value;
            }

            var run = new BaristaOrder[count];
            for (var index = 0; index < count; index += 1)
            {
                run[index] = pool[indices[index % indices.Length]];
            }

            return run;
        }

        public static float ToleranceForRound(int roundNumber)
        {
            var index = Math.Max(0, roundNumber - 1);
            return Math.Max(0.17f, 0.32f - index * 0.015f);
        }

        public static string IngredientName(BaristaIngredient ingredient)
        {
            switch (ingredient)
            {
                case BaristaIngredient.Ice:
                    return "Ice";
                case BaristaIngredient.Shot:
                    return "Shot";
                case BaristaIngredient.Water:
                    return "Water";
                case BaristaIngredient.Milk:
                    return "Milk";
                case BaristaIngredient.Syrup:
                    return "Syrup";
                default:
                    throw new ArgumentOutOfRangeException(nameof(ingredient), ingredient, null);
            }
        }

        public static BaristaScore Score(
            BaristaOrder order,
            IngredientAmounts actual,
            float wasteAmount,
            int roundNumber)
        {
            if (actual.Total < MinimumPlayableAmount)
            {
                return new BaristaScore(
                    0,
                    0f,
                    0f,
                    Math.Max(0f, wasteAmount),
                    0f,
                    100f,
                    "C",
                    "C",
                    "C",
                    "The cup is almost empty.",
                    RatioText(actual),
                    RatioText(order.Target));
            }

            var ratioDistance = RatioDistance(order.Target, actual);
            var tolerance = ToleranceForRound(roundNumber);
            var focusedRatio = Clamp01(1f - ratioDistance / tolerance);
            var readableRatio = Clamp01(1f - ratioDistance / (tolerance * 2.35f));
            var ratioScore = focusedRatio * 44f + readableRatio * 22f;

            var totalWindow = Math.Max(1f, order.TotalTolerance * StrictnessForRound(roundNumber));
            var totalDistance = Math.Abs(actual.Total - order.IdealTotal);
            var focusedTotal = Clamp01(1f - totalDistance / totalWindow);
            var readableTotal = Clamp01(1f - totalDistance / (totalWindow * 2.75f));
            var totalScore = focusedTotal * 16f + readableTotal * 8f;

            var roundPressure = Math.Max(0, roundNumber - 1);
            var wastePenalty = Math.Min(18f, Math.Max(0f, wasteAmount) * (0.22f + roundPressure * 0.018f));
            var syrupShare = actual.Total <= 0f ? 0f : actual.Syrup / actual.Total;
            var targetSyrupShare = order.Target.Total <= 0f ? 0f : order.Target.Syrup / order.Target.Total;
            var syrupAllowance = Math.Max(0.1f, targetSyrupShare + 0.1f);
            var syrupPenalty = Math.Max(0f, syrupShare - syrupAllowance) * 92f;

            var missingPenalty = MissingPenalty(order, actual);
            var overflowRatio = Math.Max(0f, actual.Total / CupCapacity - 1f);
            var capacityPenalty = Math.Min(
                22f,
                overflowRatio * 12f + Math.Max(0f, overflowRatio - 0.35f) * 12f);
            var rawScore = ratioScore + totalScore + 10f - wastePenalty - syrupPenalty - missingPenalty - capacityPenalty;
            rawScore = Math.Max(
                rawScore,
                RecognizableDrinkFloor(order, actual, ratioDistance, tolerance, wastePenalty, syrupPenalty, missingPenalty));
            var overflowCap = OverflowScoreCap(overflowRatio);
            var roundScore = (int)Math.Round(Clamp(Math.Min(rawScore, overflowCap), 0f, 100f), MidpointRounding.AwayFromZero);

            return new BaristaScore(
                roundScore,
                ratioScore,
                totalScore,
                wastePenalty,
                syrupPenalty,
                missingPenalty,
                Grade(ratioScore / 66f),
                Grade(totalScore / 24f),
                Grade(1f - wastePenalty / 18f),
                CommentForScore(roundScore),
                RatioText(actual),
                RatioText(order.Target));
        }

        public static float RatioDistance(IngredientAmounts target, IngredientAmounts actual)
        {
            if (target.Total <= 0f || actual.Total <= 0f)
            {
                return 1f;
            }

            var targetValues = target.ToArray();
            var actualValues = actual.ToArray();
            var distance = 0f;
            for (var index = 0; index < targetValues.Length; index += 1)
            {
                var targetShare = targetValues[index] / target.Total;
                var actualShare = actualValues[index] / actual.Total;
                distance += Math.Abs(targetShare - actualShare);
            }

            return distance * 0.5f;
        }

        public static string RecipeMemo()
        {
            return string.Join(
                "\n",
                Orders.Select(order => $"{order.MemoName}: {order.MemoRatio}"));
        }

        public static string RecipeMemoFor(BaristaOrder currentOrder)
        {
            var request = currentOrder.CustomerLine;
            var separator = request.IndexOf(" - ", StringComparison.Ordinal);
            if (separator >= 0 && separator + 3 < request.Length)
            {
                request = request.Substring(separator + 3);
            }

            var lines = new List<string>
            {
                currentOrder.MemoName,
                $"Base recipe: {currentOrder.MemoRatio}",
                $"Request: {request}",
                currentOrder.TasteHint
            };

            return string.Join("\n", lines);
        }

        private static BaristaOrder[] BuildGeneratedOrders()
        {
            var adjustments = new[]
            {
                new OrderAdjustment("sensitive balance, no extra sweetness.", 1f, 1f, 1f, 1f, 0.55f, 0f, 2f, BaristaIngredient.Syrup, "Sensitivity check: sweetness should stay quiet."),
                new OrderAdjustment("extra chill, keep the body steady.", 1.32f, 0.96f, 0.96f, 0.98f, 0.9f, 3f, 1f, BaristaIngredient.Ice, "Ice is the adjustment, not the whole drink."),
                new OrderAdjustment("less ice, fuller sip.", 0.45f, 1.06f, 1.06f, 1.08f, 1f, -2f, 0f, BaristaIngredient.Ice, "Keep it cool without making the cup icy."),
                new OrderAdjustment("deeper shot, same cup size.", 0.9f, 1.34f, 0.92f, 0.92f, 0.85f, 0f, -1f, BaristaIngredient.Shot, "Shot should land first, but the cup still needs balance."),
                new OrderAdjustment("soft milk finish.", 0.9f, 0.92f, 0.8f, 1.3f, 0.95f, 2f, 0f, BaristaIngredient.Milk, "Milk should round off the edges."),
                new OrderAdjustment("lighter and cleaner.", 0.9f, 0.86f, 1.28f, 0.76f, 0.35f, -2f, 1f, BaristaIngredient.Water, "Make it clean rather than rich."),
                new OrderAdjustment("tiny syrup, do not make it dessert.", 1f, 1f, 1f, 1f, 0.28f, 0f, -1f, BaristaIngredient.Syrup, "Syrup is only a signal here."),
                new OrderAdjustment("a little sweeter, still balanced.", 0.96f, 0.95f, 0.95f, 0.98f, 1.72f, 1f, 0f, BaristaIngredient.Syrup, "Sweetness can show, but it should not take over."),
                new OrderAdjustment("short pour, intense taste.", 0.78f, 1.22f, 0.78f, 0.84f, 0.85f, -12f, -1f, BaristaIngredient.Shot, "Smaller cup, sharper taste."),
                new OrderAdjustment("full cup, gentle flavor.", 1.08f, 0.82f, 1.12f, 1.12f, 0.9f, 8f, 1f, BaristaIngredient.Milk, "Bigger fill, softer impression."),
                new OrderAdjustment("no syrup at all.", 1f, 1.04f, 1.04f, 1.04f, 0f, 0f, -1f, BaristaIngredient.Syrup, "A clean order: sweetness should vanish."),
                new OrderAdjustment("barely cold, smooth finish.", 0.24f, 1.04f, 1.02f, 1.12f, 0.9f, -4f, 0f, BaristaIngredient.Ice, "Use ice carefully; softness matters more.")
            };

            var orders = new List<BaristaOrder>(Orders.Length * adjustments.Length);
            foreach (var baseOrder in Orders)
            {
                foreach (var adjustment in adjustments)
                {
                    var target = NormalizeAmounts(ApplyAdjustment(baseOrder.Target, adjustment), 100f);
                    var idealTotal = Clamp(baseOrder.IdealTotal + adjustment.IdealTotalOffset, 58f, 94f);
                    var totalTolerance = Math.Max(7f, baseOrder.TotalTolerance + adjustment.ToleranceOffset);
                    var secondary = adjustment.FocusIngredient == baseOrder.PrimaryIngredient
                        ? baseOrder.SecondaryIngredient
                        : adjustment.FocusIngredient;

                    orders.Add(new BaristaOrder(
                        baseOrder.Id,
                        $"{baseOrder.MemoName} - {adjustment.Line}",
                        baseOrder.MemoName,
                        baseOrder.MemoRatio,
                        target,
                        idealTotal,
                        totalTolerance,
                        baseOrder.PrimaryIngredient,
                        secondary,
                        $"{baseOrder.TasteHint} {adjustment.Hint}"));
                }
            }

            return orders.ToArray();
        }

        private static IngredientAmounts ApplyAdjustment(IngredientAmounts target, OrderAdjustment adjustment)
        {
            return new IngredientAmounts(
                target.Ice * adjustment.IceFactor,
                target.Shot * adjustment.ShotFactor,
                target.Water * adjustment.WaterFactor,
                target.Milk * adjustment.MilkFactor,
                target.Syrup * adjustment.SyrupFactor);
        }

        private static IngredientAmounts NormalizeAmounts(IngredientAmounts amounts, float targetTotal)
        {
            if (amounts.Total <= 0f)
            {
                return amounts;
            }

            var scale = targetTotal / amounts.Total;
            return new IngredientAmounts(
                amounts.Ice * scale,
                amounts.Shot * scale,
                amounts.Water * scale,
                amounts.Milk * scale,
                amounts.Syrup * scale);
        }

        public static string RatioText(IngredientAmounts amounts)
        {
            if (amounts.Total <= 0f)
            {
                return "empty";
            }

            var parts = new List<string>();
            foreach (var ingredient in Ingredients)
            {
                var amount = amounts[ingredient];
                if (amount <= 0.5f)
                {
                    continue;
                }

                parts.Add($"{IngredientName(ingredient)} {Math.Round(amount / amounts.Total * 10f, 1):0.#}");
            }

            return parts.Count == 0 ? "empty" : string.Join(" : ", parts);
        }

        private static float MissingPenalty(BaristaOrder order, IngredientAmounts actual)
        {
            var penalty = 0f;
            penalty += IngredientMissingPenalty(order.Target[order.PrimaryIngredient], actual[order.PrimaryIngredient], 18f);
            penalty += IngredientMissingPenalty(order.Target[order.SecondaryIngredient], actual[order.SecondaryIngredient], 12f);
            return penalty;
        }

        private readonly struct OrderAdjustment
        {
            public OrderAdjustment(
                string line,
                float iceFactor,
                float shotFactor,
                float waterFactor,
                float milkFactor,
                float syrupFactor,
                float idealTotalOffset,
                float toleranceOffset,
                BaristaIngredient focusIngredient,
                string hint)
            {
                Line = line;
                IceFactor = iceFactor;
                ShotFactor = shotFactor;
                WaterFactor = waterFactor;
                MilkFactor = milkFactor;
                SyrupFactor = syrupFactor;
                IdealTotalOffset = idealTotalOffset;
                ToleranceOffset = toleranceOffset;
                FocusIngredient = focusIngredient;
                Hint = hint;
            }

            public string Line { get; }
            public float IceFactor { get; }
            public float ShotFactor { get; }
            public float WaterFactor { get; }
            public float MilkFactor { get; }
            public float SyrupFactor { get; }
            public float IdealTotalOffset { get; }
            public float ToleranceOffset { get; }
            public BaristaIngredient FocusIngredient { get; }
            public string Hint { get; }
        }

        private static float RecognizableDrinkFloor(
            BaristaOrder order,
            IngredientAmounts actual,
            float ratioDistance,
            float tolerance,
            float wastePenalty,
            float syrupPenalty,
            float missingPenalty)
        {
            if (missingPenalty >= 24f || wastePenalty >= 16f || syrupPenalty >= 28f)
            {
                return 0f;
            }

            var targetIngredients = 0;
            var presentIngredients = 0;
            foreach (var ingredient in Ingredients)
            {
                var targetAmount = order.Target[ingredient];
                if (targetAmount <= 0f)
                {
                    continue;
                }

                targetIngredients += 1;
                if (actual[ingredient] >= Math.Max(1.5f, targetAmount * 0.12f))
                {
                    presentIngredients += 1;
                }
            }

            var presentShare = targetIngredients <= 0 ? 0f : presentIngredients / (float)targetIngredients;
            var ratioCredit = Clamp01(1f - ratioDistance / (tolerance * 2.6f));
            var fillRatio = order.IdealTotal <= 0f ? 1f : actual.Total / order.IdealTotal;
            var fillPenalty = fillRatio >= 1f
                ? Clamp01((fillRatio - 1f) / 2.2f)
                : Clamp01((0.7f - fillRatio) / 0.7f);
            var floor = (12f + presentShare * 14f + ratioCredit * 16f) * (1f - fillPenalty * 0.55f);
            floor -= wastePenalty * 0.35f + syrupPenalty * 0.45f + missingPenalty * 0.5f;
            return Clamp(floor, 0f, 42f);
        }

        private static float OverflowScoreCap(float overflowRatio)
        {
            if (overflowRatio <= 0f)
            {
                return 100f;
            }

            if (overflowRatio <= 0.35f)
            {
                return 84f;
            }

            if (overflowRatio <= 0.85f)
            {
                return 66f;
            }

            if (overflowRatio <= 1.5f)
            {
                return 48f;
            }

            return 36f;
        }

        private static float IngredientMissingPenalty(float targetAmount, float actualAmount, float maxPenalty)
        {
            if (targetAmount <= 0f)
            {
                return 0f;
            }

            var required = targetAmount * 0.28f;
            if (actualAmount >= required)
            {
                return 0f;
            }

            return (1f - actualAmount / required) * maxPenalty;
        }

        private static string CommentForScore(int score)
        {
            if (score >= 92)
            {
                return "Perfectly picky.";
            }

            if (score >= 78)
            {
                return "You read the taste.";
            }

            if (score >= 58)
            {
                return "Close enough.";
            }

            if (score >= 35)
            {
                return "Not quite their mood.";
            }

            return "The customer quietly folded the note.";
        }

        private static float StrictnessForRound(int roundNumber)
        {
            return Math.Max(0.78f, 1.15f - Math.Max(0, roundNumber - 1) * 0.04f);
        }

        private static string Grade(float normalizedScore)
        {
            if (normalizedScore >= 0.86f)
            {
                return "A";
            }

            if (normalizedScore >= 0.68f)
            {
                return "B";
            }

            return "C";
        }

        private static float Clamp01(float value)
        {
            return Clamp(value, 0f, 1f);
        }

        private static float Clamp(float value, float min, float max)
        {
            if (value < min)
            {
                return min;
            }

            return value > max ? max : value;
        }
    }
}
