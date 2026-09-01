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
