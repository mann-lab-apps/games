using System;

namespace MannLab.Games.DopamineSwap
{
    public readonly struct ScoreRange
    {
        public ScoreRange(int min, int max)
        {
            Min = min;
            Max = max;
        }

        public int Min { get; }
        public int Max { get; }

        public bool Contains(int score)
        {
            return score >= Min && score <= Max;
        }

        public override string ToString()
        {
            return $"{Min}-{Max}";
        }
    }

    public readonly struct DopamineRound
    {
        public DopamineRound(int round, int opponentScore, ScoreRange visibleRange, bool revealsOpponentScore, int[] playerCards, float timeLimitSeconds)
        {
            Round = round;
            OpponentScore = opponentScore;
            VisibleRange = visibleRange;
            RevealsOpponentScore = revealsOpponentScore;
            PlayerCards = playerCards;
            TimeLimitSeconds = timeLimitSeconds;
        }

        public int Round { get; }
        public int OpponentScore { get; }
        public ScoreRange VisibleRange { get; }
        public bool RevealsOpponentScore { get; }
        public int[] PlayerCards { get; }
        public float TimeLimitSeconds { get; }

        public string OpponentPrompt => RevealsOpponentScore ? OpponentScore.ToString() : VisibleRange.ToString();
    }

    public static class DopamineRoundRules
    {
        public const int CardCount = 3;
        public const int MinScore = 1;
        public const int MaxScore = 100;
        public const int RevealedOpponentRounds = 3;

        public static DopamineRound CreateRound(int round, Random rng)
        {
            if (round < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(round), "Round must start at 1.");
            }

            if (rng == null)
            {
                throw new ArgumentNullException(nameof(rng));
            }

            var opponentBand = OpponentBandForRound(round);
            var opponentScore = rng.Next(opponentBand.Min, opponentBand.Max + 1);
            var cards = CreateCards(round, opponentScore, rng);
            return new DopamineRound(
                round,
                opponentScore,
                VisibleRangeForScore(opponentScore),
                round <= RevealedOpponentRounds,
                cards,
                TimeLimitForRound(round));
        }

        public static float TimeLimitForRound(int round)
        {
            if (round <= 3)
            {
                return 5f;
            }

            if (round <= 7)
            {
                return 4f;
            }

            if (round <= 12)
            {
                return 3f;
            }

            return 2.4f;
        }

        public static ScoreRange OpponentBandForRound(int round)
        {
            if (round <= 3)
            {
                return new ScoreRange(20, 70);
            }

            if (round <= 7)
            {
                return new ScoreRange(30, 80);
            }

            if (round <= 12)
            {
                return new ScoreRange(40, 90);
            }

            return new ScoreRange(50, 98);
        }

        public static ScoreRange VisibleRangeForScore(int score)
        {
            var min = Math.Max(MinScore, score - 10);
            var max = Math.Min(MaxScore, score + 10);
            if (max - min < 20)
            {
                if (min == MinScore)
                {
                    max = Math.Min(MaxScore, min + 20);
                }
                else if (max == MaxScore)
                {
                    min = Math.Max(MinScore, max - 20);
                }
            }

            return new ScoreRange(min, max);
        }

        private static int[] CreateCards(int round, int opponentScore, Random rng)
        {
            var cards = new int[CardCount];
            var winningIndex = rng.Next(0, CardCount);

            for (var i = 0; i < cards.Length; i++)
            {
                cards[i] = i == winningIndex
                    ? rng.Next(Math.Min(MaxScore, opponentScore + 1), MaxScore + 1)
                    : CreatePressureCard(round, opponentScore, rng);
            }

            EnsureUnique(cards, winningIndex, opponentScore);
            Shuffle(cards, rng);
            return cards;
        }

        private static int CreatePressureCard(int round, int opponentScore, Random rng)
        {
            var lowerBound = Math.Max(MinScore, opponentScore - (round <= 3 ? 28 : 20));
            var upperBound = Math.Min(MaxScore, opponentScore + (round <= 7 ? 22 : 14));
            return rng.Next(lowerBound, upperBound + 1);
        }

        private static void EnsureUnique(int[] cards, int winningIndex, int opponentScore)
        {
            for (var i = 0; i < cards.Length; i++)
            {
                for (var j = 0; j < i; j++)
                {
                    if (cards[i] == cards[j])
                    {
                        cards[i] = NextUniqueScore(cards, i, cards[i], i == winningIndex, opponentScore);
                    }
                }
            }
        }

        private static int NextUniqueScore(int[] cards, int count, int score, bool mustWin, int opponentScore)
        {
            for (var step = 1; step <= MaxScore; step++)
            {
                var candidate = ((score + step - 1) % MaxScore) + 1;
                if (mustWin && candidate <= opponentScore)
                {
                    continue;
                }

                var used = false;
                for (var i = 0; i < count; i++)
                {
                    if (cards[i] == candidate)
                    {
                        used = true;
                        break;
                    }
                }

                if (!used)
                {
                    return candidate;
                }
            }

            return mustWin ? MaxScore : MinScore;
        }

        private static void Shuffle(int[] values, Random rng)
        {
            for (var i = values.Length - 1; i > 0; i--)
            {
                var j = rng.Next(0, i + 1);
                (values[i], values[j]) = (values[j], values[i]);
            }
        }
    }
}
