using System;

namespace MannLab.Games.Sitting
{
    public enum SittingGameState
    {
        Standing,
        Sitting,
        Caught,
        Exhausted,
        GameOver
    }

    public enum VisitorPhase
    {
        Empty,
        Warning,
        Passing
    }

    public readonly struct SittingBalance
    {
        public const float MaxHealth = 100f;
        public const float StandingDrainPerSecond = 8.5f;
        public const float SittingRecoveryPerSecond = 18f;
        public const float VisitorWarningSeconds = 0.62f;
        public const float VisitorPassingSeconds = 0.96f;
        public const float MinVisitorGapSeconds = 1.25f;
        public const float MaxVisitorGapSeconds = 3.45f;
        public const float ResultDelaySeconds = 0.42f;

        public static float TickHealth(float health, bool sitting, float deltaSeconds)
        {
            var rate = sitting ? SittingRecoveryPerSecond : -StandingDrainPerSecond;
            return Clamp(health + rate * Math.Max(0f, deltaSeconds), 0f, MaxHealth);
        }

        public static bool ShouldCatch(bool sitting, VisitorPhase phase)
        {
            return sitting && phase == VisitorPhase.Passing;
        }

        public static bool IsExhausted(float health)
        {
            return health <= 0f;
        }

        public static float NextVisitorGap(Random random)
        {
            if (random == null)
            {
                throw new ArgumentNullException(nameof(random));
            }

            return MinVisitorGapSeconds
                + (float)random.NextDouble() * (MaxVisitorGapSeconds - MinVisitorGapSeconds);
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
