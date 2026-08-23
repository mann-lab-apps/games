using System;

namespace MannLab.Games.Standing
{
    public enum StandingGameState
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

    public readonly struct StandingBalance
    {
        public const float MaxHealth = 100f;
        public const float StandingDrainPerSecond = 18f;
        public const float SittingRecoveryPerSecond = 22f;
        public const float VisitorWarningSeconds = 0.70f;
        public const float MinVisitorPassingSeconds = 1.65f;
        public const float MaxVisitorPassingSeconds = 2.75f;
        public const float MinVisitorGapSeconds = 0.80f;
        public const float MaxVisitorGapSeconds = 2.40f;
        public const double CustomerChance = 0.66d;
        public const float ResultDelaySeconds = 0.42f;

        public static float TickHealth(float health, bool sitting, float deltaSeconds)
        {
            var rate = sitting ? SittingRecoveryPerSecond : -StandingDrainPerSecond;
            return Clamp(health + rate * Math.Max(0f, deltaSeconds), 0f, MaxHealth);
        }

        public static bool ShouldCatch(bool sitting, VisitorPhase phase, bool isCustomer)
        {
            return sitting && isCustomer && phase == VisitorPhase.Passing;
        }

        public static bool ShouldCatch(bool sitting, VisitorPhase phase)
        {
            return ShouldCatch(sitting, phase, true);
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

        public static float NextVisitorPassingSeconds(Random random)
        {
            if (random == null)
            {
                throw new ArgumentNullException(nameof(random));
            }

            return MinVisitorPassingSeconds
                + (float)random.NextDouble() * (MaxVisitorPassingSeconds - MinVisitorPassingSeconds);
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
