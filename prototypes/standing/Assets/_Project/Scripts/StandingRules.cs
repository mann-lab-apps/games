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
        public const float VisitorDetectionLeftX = 0.18f;
        public const float VisitorDetectionRightX = 0.82f;
        public const float MinVisitorWalkSpeed = 0.28f;
        public const float MaxVisitorWalkSpeed = 0.42f;
        public const float VisitorSpeedRampSeconds = 80f;
        public const float MaxVisitorRampBonus = 0.26f;
        public const float VisitorHurryStartsAtSeconds = 24f;
        public const float MaxVisitorHurryBonus = 0.16f;
        public const double MaxVisitorHurryChance = 0.20d;
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

        public static bool IsVisitorInDetectionZone(float routeCenterX)
        {
            return routeCenterX >= VisitorDetectionLeftX
                && routeCenterX <= VisitorDetectionRightX;
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

        public static float NextVisitorWalkSpeed(Random random)
        {
            return NextVisitorWalkSpeed(random, 0f);
        }

        public static float NextVisitorWalkSpeed(Random random, float elapsedSeconds)
        {
            if (random == null)
            {
                throw new ArgumentNullException(nameof(random));
            }

            var baseSpeed = MinVisitorWalkSpeed
                + (float)random.NextDouble() * (MaxVisitorWalkSpeed - MinVisitorWalkSpeed);
            var difficulty = Clamp(elapsedSeconds / VisitorSpeedRampSeconds, 0f, 1f);
            var rampBonus = MaxVisitorRampBonus
                * difficulty
                * (0.65f + (float)random.NextDouble() * 0.35f);
            var hurryChance = MaxVisitorHurryChance
                * Clamp((elapsedSeconds - VisitorHurryStartsAtSeconds)
                    / Math.Max(1f, VisitorSpeedRampSeconds - VisitorHurryStartsAtSeconds), 0f, 1f);
            var hurryBonus = random.NextDouble() < hurryChance
                ? MaxVisitorHurryBonus * (0.5f + difficulty * 0.5f)
                : 0f;

            return baseSpeed + rampBonus + hurryBonus;
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
