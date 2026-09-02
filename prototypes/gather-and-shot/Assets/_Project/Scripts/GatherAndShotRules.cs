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
        BigSnowdrift,
        WeaponCache
    }

    public enum UpgradeKind
    {
        AmmoCapacity,
        GatherSpeed,
        ThrowRate,
        SnowballDamage,
        WarmCoat,
        CoinMagnet
    }

    public enum WeaponKind
    {
        BasicSnowball,
        BigSnowball,
        SplitSnowball,
        IceShot,
        SnowBurst
    }

    public enum RewardedOfferKind
    {
        DoubleSnowCoin,
        Revive,
        BonusChest,
        StartWithFullAmmo
    }

    public enum MissionKind
    {
        FirstSnowLoop,
        GatherSnow,
        CollectBigSnowdrift,
        SurviveRunnerWave,
        DefeatHeavy
    }

    public enum RunEndReason
    {
        WarmthDepleted,
        Restart
    }

    public readonly struct GatherAndShotBalance
    {
        public const float BaseMaxWarmth = 100f;
        public const int BaseMaxAmmo = 10;
        public const int MaxAmmo = BaseMaxAmmo;
        public const int UpgradeCount = 6;
        public const float FireRange = 4.25f;
        public const float BaseFireCooldownSeconds = 0.45f;
        public const float ContactDamage = 18f;
        public const float ContactCooldownSeconds = 0.55f;
        public const float BasePickupRadius = 0.52f;
        public const float BaseProjectileSpeed = 10.5f;
        public const float ProjectileHitRadius = 0.34f;
        public const float SpawnRampSeconds = 180f;
        public const float SpeedRampSeconds = 210f;
        public const float StationaryGatherDelaySeconds = 0.18f;
        public const int StationaryGatherAmmo = 1;
        public const int MaxLivePickups = 4;
        public const int FirstMiniGoalKills = 5;
        public const float FirstMiniGoalSurvivalSeconds = 30f;
        public const float FirstFreeUpgradeSeconds = 52f;

        public static float PlayerSpeed(float elapsedSeconds)
        {
            return 4.15f + 0.95f * Saturate(elapsedSeconds / SpeedRampSeconds);
        }

        public static float MaxWarmth(int warmCoatLevel)
        {
            return BaseMaxWarmth + Math.Max(0, warmCoatLevel) * 15f;
        }

        public static int MaxAmmoForLevel(int ammoCapacityLevel)
        {
            return BaseMaxAmmo + Math.Max(0, ammoCapacityLevel) * 2;
        }

        public static int StartingAmmo(int ammoCapacityLevel, bool startFull)
        {
            return startFull ? MaxAmmoForLevel(ammoCapacityLevel) : Math.Min(3 + Math.Max(0, ammoCapacityLevel), MaxAmmoForLevel(ammoCapacityLevel));
        }

        public static float FireCooldownSeconds(int throwRateLevel, bool rapidThrowActive)
        {
            var levelMultiplier = 1f - Math.Min(0.48f, Math.Max(0, throwRateLevel) * 0.08f);
            return BaseFireCooldownSeconds * levelMultiplier * (rapidThrowActive ? 0.46f : 1f);
        }

        public static int SnowballDamage(int snowballDamageLevel)
        {
            return 1 + Math.Max(0, snowballDamageLevel);
        }

        public static float EnemySpeedMultiplier(float elapsedSeconds)
        {
            return 1f + 0.78f * Saturate(elapsedSeconds / SpeedRampSeconds);
        }

        public static float SpawnGap(float elapsedSeconds)
        {
            var t = Saturate(elapsedSeconds / SpawnRampSeconds);
            return Lerp(1.05f, 0.22f, t);
        }

        public static int MaxLiveEnemies(float elapsedSeconds)
        {
            return 8 + (int)Math.Floor(Saturate(elapsedSeconds / SpawnRampSeconds) * 32f);
        }

        public static float PickupSpawnGapMin(float elapsedSeconds)
        {
            return Lerp(5.2f, 4.0f, Saturate(elapsedSeconds / SpawnRampSeconds));
        }

        public static float PickupSpawnGapMax(float elapsedSeconds)
        {
            return Lerp(8.4f, 6.2f, Saturate(elapsedSeconds / SpawnRampSeconds));
        }

        public static float StationaryGatherCycleSeconds(float elapsedSeconds)
        {
            return StationaryGatherCycleSeconds(elapsedSeconds, 0);
        }

        public static float StationaryGatherCycleSeconds(float elapsedSeconds, int gatherSpeedLevel)
        {
            var timeRamp = Lerp(0.82f, 0.62f, Saturate(elapsedSeconds / SpeedRampSeconds));
            var upgradeMultiplier = 1f - Math.Min(0.45f, Math.Max(0, gatherSpeedLevel) * 0.1f);
            return Math.Max(0.32f, timeRamp * upgradeMultiplier);
        }

        public static int StartingHealth(EnemyKind kind)
        {
            return kind == EnemyKind.Heavy ? 4 : 1;
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
                case PickupKind.WeaponCache:
                    return 3;
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

            if (string.Equals(pickupKind, "WeaponCache", StringComparison.Ordinal)
                || string.Equals(pickupKind, "Cache", StringComparison.Ordinal))
            {
                return PickupAmmo(PickupKind.WeaponCache);
            }

            return string.Equals(pickupKind, "Drift", StringComparison.Ordinal)
                || string.Equals(pickupKind, "Snowdrift", StringComparison.Ordinal)
                ? PickupAmmo(PickupKind.Snowdrift)
                : PickupAmmo(PickupKind.Snowball);
        }

        public static float PickupRadius(PickupKind kind)
        {
            return PickupRadius(kind, 0);
        }

        public static float PickupRadius(PickupKind kind, int coinMagnetLevel)
        {
            var baseRadius = kind == PickupKind.BigSnowdrift ? 0.72f : kind == PickupKind.WeaponCache ? 0.66f : BasePickupRadius;
            return baseRadius + Math.Max(0, coinMagnetLevel) * 0.16f;
        }

        public static PickupKind RollPickupKind(Random random, float elapsedSeconds)
        {
            if (random == null)
            {
                throw new ArgumentNullException(nameof(random));
            }

            var t = Saturate(elapsedSeconds / SpawnRampSeconds);
            var cacheChance = elapsedSeconds < 45f ? 0.02d : 0.09d + 0.04d * t;
            var bigChance = elapsedSeconds < 12f ? 0.08d : 0.16d + 0.06d * t;
            var driftChance = 0.34d + 0.08d * t;
            var roll = random.NextDouble();

            if (roll < cacheChance)
            {
                return PickupKind.WeaponCache;
            }

            if (roll < cacheChance + bigChance)
            {
                return PickupKind.BigSnowdrift;
            }

            return roll < cacheChance + bigChance + driftChance ? PickupKind.Snowdrift : PickupKind.Snowball;
        }

        public static EnemyKind RollEnemyKind(Random random, float elapsedSeconds)
        {
            if (random == null)
            {
                throw new ArgumentNullException(nameof(random));
            }

            var roll = random.NextDouble();
            if (elapsedSeconds < 60f)
            {
                return EnemyKind.Walker;
            }

            if (elapsedSeconds < 120f)
            {
                return roll < 0.34d ? EnemyKind.Runner : EnemyKind.Walker;
            }

            if (elapsedSeconds < 240f)
            {
                if (roll < 0.24d)
                {
                    return EnemyKind.Runner;
                }

                return roll > 0.82d ? EnemyKind.Heavy : EnemyKind.Walker;
            }

            if (roll < 0.32d)
            {
                return EnemyKind.Runner;
            }

            return roll > 0.72d ? EnemyKind.Heavy : EnemyKind.Walker;
        }

        public static int WaveStage(float elapsedSeconds)
        {
            if (elapsedSeconds < 60f)
            {
                return 1;
            }

            if (elapsedSeconds < 120f)
            {
                return 2;
            }

            if (elapsedSeconds < 240f)
            {
                return 3;
            }

            return 4;
        }

        public static int EnemyCoinReward(EnemyKind kind)
        {
            switch (kind)
            {
                case EnemyKind.Runner:
                    return 4;
                case EnemyKind.Heavy:
                    return 8;
                default:
                    return 3;
            }
        }

        public static int WaveCoinReward(float survivalSeconds)
        {
            return 8 + (int)Math.Floor(Math.Max(0f, survivalSeconds) / 45f) * 4;
        }

        public static int MissionCoinReward(int runNumber)
        {
            return Math.Max(10, 12 + Math.Max(0, runNumber - 1) * 2);
        }

        public static int BonusChestCoins(int runNumber)
        {
            return 18 + Math.Max(0, runNumber - 1) * 3;
        }

        public static int UpgradeCost(UpgradeKind kind, int currentLevel)
        {
            var baseCost = 12 + (int)kind * 2;
            return baseCost + Math.Max(0, currentLevel) * 10;
        }

        public static string UpgradeName(UpgradeKind kind)
        {
            switch (kind)
            {
                case UpgradeKind.AmmoCapacity:
                    return "Ammo Capacity";
                case UpgradeKind.GatherSpeed:
                    return "Gather Speed";
                case UpgradeKind.ThrowRate:
                    return "Throw Rate";
                case UpgradeKind.SnowballDamage:
                    return "Snowball Damage";
                case UpgradeKind.WarmCoat:
                    return "Warm Coat";
                case UpgradeKind.CoinMagnet:
                    return "Coin Magnet";
                default:
                    return kind.ToString();
            }
        }

        public static string WeaponName(WeaponKind kind)
        {
            switch (kind)
            {
                case WeaponKind.BigSnowball:
                    return "Big Snowball";
                case WeaponKind.SplitSnowball:
                    return "Split Snowball";
                case WeaponKind.IceShot:
                    return "Ice Shot";
                case WeaponKind.SnowBurst:
                    return "Snow Burst";
                default:
                    return "Snowball";
            }
        }

        public static float WeaponDurationSeconds(WeaponKind kind)
        {
            switch (kind)
            {
                case WeaponKind.BigSnowball:
                    return 13f;
                case WeaponKind.SplitSnowball:
                    return 14f;
                case WeaponKind.IceShot:
                    return 16f;
                case WeaponKind.SnowBurst:
                    return 10f;
                default:
                    return 0f;
            }
        }

        public static bool IsGameOver(float warmth)
        {
            return warmth <= 0f;
        }

        public static float ApplyContactDamage(float warmth)
        {
            return ApplyContactDamage(warmth, 0);
        }

        public static float ApplyContactDamage(float warmth, int warmCoatLevel)
        {
            var mitigation = Math.Min(0.4f, Math.Max(0, warmCoatLevel) * 0.08f);
            return Clamp(warmth - ContactDamage * (1f - mitigation), 0f, MaxWarmth(warmCoatLevel));
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
