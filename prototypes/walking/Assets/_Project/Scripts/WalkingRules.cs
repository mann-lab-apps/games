using UnityEngine;

namespace MannLab.Games.Walking
{
    public enum WalkingGameState
    {
        Ready,
        Playing,
        Result
    }

    public enum WalkingFootSide
    {
        Left,
        Right
    }

    public enum WalkingObstacleKind
    {
        SmallIceberg,
        Iceberg,
        LowShard
    }

    public enum WalkingUpgradeKind
    {
        BiggerFeet,
        BetterBalance,
        IcebreakerFeet,
        FishMagnet,
        RhythmBonus
    }

    public readonly struct WalkingFootPlacement
    {
        public WalkingFootPlacement(Vector2 position, bool isValid, string reason)
        {
            Position = position;
            IsValid = isValid;
            Reason = reason;
        }

        public Vector2 Position { get; }
        public bool IsValid { get; }
        public string Reason { get; }
    }

    public static class WalkingRules
    {
        public const float TileSize = 1.68f;
        public const float BodyRadius = 0.30f;
        public const float FootRadius = 0.16f;
        public const float MinStepDistance = 0.34f;
        public const float MaxStepDistance = 1.22f;
        public const float NaturalHalfStance = 0.23f;
        public const float SideClearance = 0.12f;
        public const float MaxFootSeparation = 1.34f;
        public const float ReturnGestureMaxScreenY = 0.28f;
        public const int MazeCellColumns = 11;
        public const int MazeCellRows = 18;

        public static Vector2 BuildFootCandidate(
            WalkingFootSide side,
            Vector2 supportFoot,
            Vector2 facingForward,
            Vector2 screenPosition,
            Vector2 screenSize)
        {
            var forward = SafeNormal(facingForward, Vector2.up);
            var right = new Vector2(forward.y, -forward.x);
            var safeWidth = Mathf.Max(1f, screenSize.x);
            var safeHeight = Mathf.Max(1f, screenSize.y);
            var halfWidth = safeWidth * 0.5f;
            var sideX = side == WalkingFootSide.Left
                ? Mathf.Clamp01(screenPosition.x / halfWidth)
                : Mathf.Clamp01((screenPosition.x - halfWidth) / halfWidth);
            var xControl = sideX * 2f - 1f;
            var rawY = Mathf.Clamp01(screenPosition.y / safeHeight);
            var yControl = Mathf.Pow(rawY, 0.58f);
            var forwardDistance = Mathf.Lerp(MinStepDistance + 0.30f, MaxStepDistance, yControl);
            var naturalSide = side == WalkingFootSide.Left ? -NaturalHalfStance : NaturalHalfStance;
            var lateralDistance = naturalSide + xControl * 0.34f;

            return supportFoot + forward * forwardDistance + right * lateralDistance;
        }

        public static WalkingFootPlacement ValidateFootPlacement(
            WalkingFootSide side,
            Vector2 supportFoot,
            Vector2 candidate,
            Vector2 facingForward,
            WalkingMaze maze)
        {
            var delta = candidate - supportFoot;
            var distance = delta.magnitude;
            if (distance < MinStepDistance)
            {
                return new WalkingFootPlacement(candidate, false, "short");
            }

            if (distance > MaxStepDistance)
            {
                return new WalkingFootPlacement(candidate, false, "long");
            }

            if (distance > MaxFootSeparation)
            {
                return new WalkingFootPlacement(candidate, false, "wide");
            }

            var forward = SafeNormal(facingForward, Vector2.up);
            var right = new Vector2(forward.y, -forward.x);
            var lateral = Vector2.Dot(delta, right);
            if (side == WalkingFootSide.Left && lateral > -SideClearance)
            {
                return new WalkingFootPlacement(candidate, false, "cross");
            }

            if (side == WalkingFootSide.Right && lateral < SideClearance)
            {
                return new WalkingFootPlacement(candidate, false, "cross");
            }

            if (maze != null && maze.IsCircleTouchingWall(candidate, FootRadius))
            {
                return new WalkingFootPlacement(candidate, false, "wall");
            }

            return new WalkingFootPlacement(candidate, true, string.Empty);
        }

        public static Vector2 BodyCenter(Vector2 leftFoot, Vector2 rightFoot)
        {
            return (leftFoot + rightFoot) * 0.5f;
        }

        public static Vector2 FacingFromFeet(Vector2 leftFoot, Vector2 rightFoot, Vector2 previousForward)
        {
            var rightAxis = SafeNormal(rightFoot - leftFoot, new Vector2(1f, 0f));
            var forward = new Vector2(-rightAxis.y, rightAxis.x);
            if (Vector2.Dot(forward, previousForward) < 0f)
            {
                forward = -forward;
            }

            return SafeNormal(forward, previousForward);
        }

        public static bool IsBodyColliding(Vector2 bodyCenter, WalkingMaze maze)
        {
            return maze != null && maze.IsCircleTouchingWall(bodyCenter, BodyRadius);
        }

        public static bool IsReturnGesturePosition(Vector2 screenPosition, Vector2 screenSize)
        {
            var safeHeight = Mathf.Max(1f, screenSize.y);
            return screenPosition.y / safeHeight <= ReturnGestureMaxScreenY;
        }

        public static bool IsStepGesturePosition(Vector2 screenPosition, Vector2 screenSize)
        {
            return true;
        }

        public static bool IsAssistableInvalidReason(string reason)
        {
            return reason == "short"
                || reason == "long"
                || reason == "wide"
                || reason == "cross";
        }

        public static Vector2 RecommendedFootPosition(WalkingFootSide side, Vector2 supportFoot, Vector2 facingForward)
        {
            var forward = SafeNormal(facingForward, Vector2.up);
            var right = new Vector2(forward.y, -forward.x);
            var naturalSide = side == WalkingFootSide.Left ? -NaturalHalfStance : NaturalHalfStance;
            return supportFoot + forward * 0.82f + right * naturalSide;
        }

        public static WalkingFootPlacement BuildAssistedFootPlacement(
            WalkingFootSide side,
            Vector2 supportFoot,
            Vector2 candidate,
            Vector2 facingForward,
            WalkingMaze maze,
            float assistStrength)
        {
            var original = ValidateFootPlacement(side, supportFoot, candidate, facingForward, maze);
            if (original.IsValid || !IsAssistableInvalidReason(original.Reason))
            {
                return original;
            }

            var forward = SafeNormal(facingForward, Vector2.up);
            var right = new Vector2(forward.y, -forward.x);
            var delta = candidate - supportFoot;
            var forwardDistance = Vector2.Dot(delta, forward);
            var lateralDistance = Vector2.Dot(delta, right);
            var naturalSide = side == WalkingFootSide.Left ? -NaturalHalfStance : NaturalHalfStance;
            var strength = Mathf.Clamp01(assistStrength);
            var lateralWindow = Mathf.Lerp(0.16f, 0.44f, strength);
            var correctedForward = Mathf.Clamp(forwardDistance, MinStepDistance + 0.24f, MaxStepDistance - 0.12f);
            var correctedLateral = Mathf.Clamp(lateralDistance, naturalSide - lateralWindow, naturalSide + lateralWindow);
            var minClearance = SideClearance + Mathf.Lerp(0.03f, 0.10f, strength);
            correctedLateral = side == WalkingFootSide.Left
                ? Mathf.Min(correctedLateral, -minClearance)
                : Mathf.Max(correctedLateral, minClearance);

            var corrected = supportFoot + forward * correctedForward + right * correctedLateral;
            var placement = ValidateFootPlacement(side, supportFoot, corrected, facingForward, maze);
            if (placement.IsValid)
            {
                return placement;
            }

            corrected = RecommendedFootPosition(side, supportFoot, facingForward);
            placement = ValidateFootPlacement(side, supportFoot, corrected, facingForward, maze);
            return placement.IsValid ? placement : original;
        }

        public static float BestMarkerDistance(float bestDistance, float fieldLength)
        {
            if (bestDistance < 1f)
            {
                return 0f;
            }

            return Mathf.Clamp(bestDistance, 3f, Mathf.Max(3f, fieldLength - 6f));
        }

        public static int CountReachedGoalMarkers(float distance, float[] markerDistances)
        {
            if (markerDistances == null)
            {
                return 0;
            }

            var count = 0;
            for (var i = 0; i < markerDistances.Length; i++)
            {
                if (distance >= markerDistances[i])
                {
                    count++;
                }
            }

            return count;
        }

        public static bool IsWarmupCenterLane(float forwardDistance, float lateralDistance)
        {
            return forwardDistance < 40f && Mathf.Abs(lateralDistance) < 2.2f;
        }

        public static float RhythmQualityAfterStep(float currentQuality, bool alternated, float secondsSincePreviousStep)
        {
            var current = Mathf.Clamp01(currentQuality);
            var timing = secondsSincePreviousStep < 0f
                ? 0.55f
                : Mathf.InverseLerp(1.20f, 0.32f, Mathf.Abs(secondsSincePreviousStep - 0.62f) + 0.32f);
            var target = alternated ? Mathf.Lerp(0.52f, 1f, timing) : 0.18f;
            return Mathf.Clamp01(Mathf.Lerp(current, target, alternated ? 0.42f : 0.58f));
        }

        public static float RhythmQualityAfterBreak(float currentQuality, float severity)
        {
            return Mathf.Clamp01(currentQuality - Mathf.Clamp01(severity) * 0.34f);
        }

        public static WalkingObstacleKind ObstacleKindFor(float radius, float roll)
        {
            if (roll < 0.20f)
            {
                return WalkingObstacleKind.LowShard;
            }

            return radius < 0.56f || roll < 0.48f
                ? WalkingObstacleKind.SmallIceberg
                : WalkingObstacleKind.Iceberg;
        }

        public static int ObstacleDurability(WalkingObstacleKind kind)
        {
            switch (kind)
            {
                case WalkingObstacleKind.LowShard:
                    return 1;
                case WalkingObstacleKind.SmallIceberg:
                    return 2;
                default:
                    return 3;
            }
        }

        public static float ObstacleCollisionScale(WalkingObstacleKind kind, int hits)
        {
            if (hits <= 0)
            {
                return 1f;
            }

            if (hits >= ObstacleDurability(kind))
            {
                return 0f;
            }

            switch (kind)
            {
                case WalkingObstacleKind.LowShard:
                    return 0f;
                case WalkingObstacleKind.SmallIceberg:
                    return 0.30f;
                default:
                    return hits == 1 ? 0.56f : 0.24f;
            }
        }

        public static float ObstacleVisualScale(WalkingObstacleKind kind, int hits)
        {
            if (hits <= 0)
            {
                return kind == WalkingObstacleKind.LowShard ? 0.72f : 1f;
            }

            if (hits >= ObstacleDurability(kind))
            {
                return kind == WalkingObstacleKind.LowShard ? 0.34f : 0.46f;
            }

            switch (kind)
            {
                case WalkingObstacleKind.LowShard:
                    return 0.34f;
                case WalkingObstacleKind.SmallIceberg:
                    return 0.62f;
                default:
                    return hits == 1 ? 0.84f : 0.68f;
            }
        }

        public static int ObstacleFishReward(WalkingObstacleKind kind)
        {
            switch (kind)
            {
                case WalkingObstacleKind.LowShard:
                    return 2;
                case WalkingObstacleKind.SmallIceberg:
                    return 6;
                default:
                    return 12;
            }
        }

        public static int ObstacleDamagePerHit(int icebreakerFeetLevel)
        {
            return 1 + Mathf.Clamp(icebreakerFeetLevel, 0, 4) / 2;
        }

        public static float FishCollectionRadius(int fishMagnetLevel, float rhythmQuality)
        {
            return 0.78f + Mathf.Clamp(fishMagnetLevel, 0, 12) * 0.12f + Mathf.Clamp01(rhythmQuality) * 0.22f;
        }

        public static int RhythmFishBonus(int rhythmBonusLevel, int comboCount)
        {
            if (comboCount < 3)
            {
                return 0;
            }

            return 1 + Mathf.Clamp(rhythmBonusLevel, 0, 10) / 2 + Mathf.Clamp(comboCount / 5, 0, 3);
        }

        public static int UpgradeCost(WalkingUpgradeKind kind, int currentLevel)
        {
            var baseCost = kind == WalkingUpgradeKind.BiggerFeet ? 12
                : kind == WalkingUpgradeKind.BetterBalance ? 14
                : kind == WalkingUpgradeKind.IcebreakerFeet ? 16
                : kind == WalkingUpgradeKind.FishMagnet ? 14
                : 18;
            return baseCost + Mathf.Max(0, currentLevel) * 10;
        }

        public static WalkingFootSide FootSideForScreenPosition(Vector2 screenPosition, Vector2 screenSize)
        {
            var safeWidth = Mathf.Max(1f, screenSize.x);
            return screenPosition.x < safeWidth * 0.5f ? WalkingFootSide.Left : WalkingFootSide.Right;
        }

        private static Vector2 SafeNormal(Vector2 value, Vector2 fallback)
        {
            return value.sqrMagnitude < 0.0001f ? fallback.normalized : value.normalized;
        }
    }
}
