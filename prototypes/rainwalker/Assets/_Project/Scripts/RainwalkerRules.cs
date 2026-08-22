namespace MannLab.Games.Rainwalker
{
    public enum RainwalkerGameState
    {
        Ready,
        Playing,
        Result
    }

    public static class RainwalkerRules
    {
        public const float RoundSeconds = 30f;
        public const float UmbrellaMinAngle = -70f;
        public const float UmbrellaMaxAngle = 70f;
        public const int PerfectScore = 1000;
        public const int HitPenalty = 12;

        public static int ScoreForHits(int wetHits)
        {
            var score = PerfectScore - wetHits * HitPenalty;
            return score < 0 ? 0 : score;
        }

        public static string GradeForScore(int score)
        {
            if (score >= 950)
            {
                return "S";
            }

            if (score >= 820)
            {
                return "A";
            }

            if (score >= 650)
            {
                return "B";
            }

            if (score >= 420)
            {
                return "C";
            }

            return "Soaked";
        }

        public static float SpawnIntervalForProgress(float progress)
        {
            return Lerp(0.05f, 0.022f, Clamp01(progress));
        }

        public static float RainSpeedForProgress(float progress)
        {
            return Lerp(1040f, 1580f, Clamp01(progress));
        }

        public static float DirectionChangeSecondsForProgress(float progress)
        {
            return Lerp(1.45f, 0.62f, Clamp01(progress));
        }

        private static float Lerp(float from, float to, float t)
        {
            return from + (to - from) * t;
        }

        private static float Clamp01(float value)
        {
            if (value < 0f)
            {
                return 0f;
            }

            return value > 1f ? 1f : value;
        }
    }
}
