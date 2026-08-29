using System;

namespace MannLab.Games.GatherAndShot
{
    public enum GatherAndShotGameState
    {
        Playing,
        GameOver
    }

    public enum EnemyKind
    {
        Walker,
        Runner,
        Heavy
    }

    public enum PickupKind
    {
        Snowball,
        Snowdrift,
        BigSnowdrift
    }

    public readonly struct GatherAndShotBalance
    {
        public const float MaxWarmth = 100f;
        public const int MaxAmmo = 10;
        public const float FireRange = 4.25f;
        public const float FireCooldownSeconds = 0.45f;
        public const float ContactDamage = 18f;
        public const float ContactCooldownSeconds = 0.55f;
        public const float BasePickupRadius = 0.52f;
        public const float ProjectileSpeed = 10.5f;
        public const float ProjectileHitRadius = 0.34f;
        public const float SpawnRampSeconds = 180f;
        public const float SpeedRampSeconds = 210f;
        public const float StationaryGatherDelaySeconds = 0.18f;
        public const int StationaryGatherAmmo = 1;
        public const int MaxLivePickups = 3;

        public static float PlayerSpeed(float elapsedSeconds)
        {
            return 4.15f + 0.95f * Saturate(elapsedSeconds / SpeedRampSeconds);
        }

        public static float EnemySpeedMultiplier(float elapsedSeconds)
        {
            return 1f + 0.78f * Saturate(elapsedSeconds / SpeedRampSeconds);
        }

        public static float SpawnGap(float elapsedSeconds)
        {
            var t = Saturate(elapsedSeconds / SpawnRampSeconds);
            return Lerp(1.15f, 0.24f, t);
        }

        public static int MaxLiveEnemies(float elapsedSeconds)
        {
            return 7 + (int)Math.Floor(Saturate(elapsedSeconds / SpawnRampSeconds) * 31f);
        }

        public static float PickupSpawnGapMin(float elapsedSeconds)
        {
            return Lerp(5.8f, 4.4f, Saturate(elapsedSeconds / SpawnRampSeconds));
        }

        public static float PickupSpawnGapMax(float elapsedSeconds)
        {
            return Lerp(9.2f, 6.8f, Saturate(elapsedSeconds / SpawnRampSeconds));
        }

        public static float StationaryGatherCycleSeconds(float elapsedSeconds)
        {
            return Lerp(0.82f, 0.62f, Saturate(elapsedSeconds / SpeedRampSeconds));
        }

        public static int StartingHealth(EnemyKind kind)
        {
            return kind == EnemyKind.Heavy ? 3 : 1;
        }

        public static float EnemyBaseSpeed(EnemyKind kind)
        {
            switch (kind)
            {
                case EnemyKind.Runner:
                    return 3.05f;
                case EnemyKind.Heavy:
                    return 1.45f;
                default:
                    return 2.05f;
            }
        }

        public static float EnemyContactRadius(EnemyKind kind)
        {
            return kind == EnemyKind.Heavy ? 0.68f : 0.50f;
        }

        public static int PickupAmmo(PickupKind kind)
        {
            switch (kind)
            {
                case PickupKind.BigSnowdrift:
                    return 6;
                case PickupKind.Snowdrift:
                    return 4;
                default:
                    return 2;
            }
        }

        public static int PickupAmmo(string pickupKind)
        {
            if (string.Equals(pickupKind, "BigSnowdrift", StringComparison.Ordinal)
                || string.Equals(pickupKind, "BigDrift", StringComparison.Ordinal))
            {
                return PickupAmmo(PickupKind.BigSnowdrift);
            }

            return string.Equals(pickupKind, "Drift", StringComparison.Ordinal)
                || string.Equals(pickupKind, "Snowdrift", StringComparison.Ordinal)
                ? PickupAmmo(PickupKind.Snowdrift)
                : PickupAmmo(PickupKind.Snowball);
        }

        public static float PickupRadius(PickupKind kind)
        {
            return kind == PickupKind.BigSnowdrift ? 0.72f : BasePickupRadius;
        }

        public static PickupKind RollPickupKind(Random random, float elapsedSeconds)
        {
            if (random == null)
            {
                throw new ArgumentNullException(nameof(random));
            }

            var t = Saturate(elapsedSeconds / SpawnRampSeconds);
            var bigChance = elapsedSeconds < 12f ? 0.08d : 0.16d + 0.06d * t;
            var driftChance = 0.34d + 0.08d * t;
            var roll = random.NextDouble();

            if (roll < bigChance)
            {
                return PickupKind.BigSnowdrift;
            }

            return roll < bigChance + driftChance ? PickupKind.Snowdrift : PickupKind.Snowball;
        }

        public static EnemyKind RollEnemyKind(Random random, float elapsedSeconds)
        {
            if (random == null)
            {
                throw new ArgumentNullException(nameof(random));
            }

            var t = Saturate(elapsedSeconds / SpawnRampSeconds);
            var roll = random.NextDouble();
            var runnerChance = 0.18d + 0.11d * t;
            var heavyChance = 0.10d + 0.13d * t;

            if (roll < runnerChance)
            {
                return EnemyKind.Runner;
            }

            if (roll > 1d - heavyChance)
            {
                return EnemyKind.Heavy;
            }

            return EnemyKind.Walker;
        }

        public static bool IsGameOver(float warmth)
        {
            return warmth <= 0f;
        }

        public static float ApplyContactDamage(float warmth)
        {
            return Clamp(warmth - ContactDamage, 0f, MaxWarmth);
        }

        private static float Saturate(float value)
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

        private static float Lerp(float start, float end, float t)
        {
            return start + (end - start) * Saturate(t);
        }
    }
}
