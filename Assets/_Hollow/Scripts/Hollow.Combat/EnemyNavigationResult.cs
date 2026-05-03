using UnityEngine;

namespace Hollow.Combat
{
    public readonly struct EnemyNavigationResult
    {
        public EnemyNavigationResult(
            EnemyNavigationBackend backend,
            EnemyNavigationMode mode,
            EnemyNavigationIntent intent,
            Vector3 requestedLocalPosition,
            Vector3 resolvedLocalPosition,
            Vector3 steeringDirection,
            bool reachedRequestedPosition,
            bool usedFallbackSteering,
            bool blocked,
            EnemyPathStatus pathStatus = EnemyPathStatus.NotRequested,
            Vector3 finalGoalLocalPosition = default,
            Vector3 nextWaypointLocalPosition = default,
            float pathAgeSeconds = 0f,
            int pathWaypointCount = 0,
            string fallbackReason = "",
            Vector3[] pathWaypointsLocalPositions = null)
        {
            Backend = backend;
            Mode = mode;
            Intent = intent;
            RequestedLocalPosition = requestedLocalPosition;
            ResolvedLocalPosition = resolvedLocalPosition;
            SteeringDirection = steeringDirection;
            ReachedRequestedPosition = reachedRequestedPosition;
            UsedFallbackSteering = usedFallbackSteering;
            Blocked = blocked;
            PathStatus = pathStatus;
            FinalGoalLocalPosition = finalGoalLocalPosition;
            NextWaypointLocalPosition = nextWaypointLocalPosition;
            PathAgeSeconds = Mathf.Max(0f, pathAgeSeconds);
            PathWaypointCount = Mathf.Max(0, pathWaypointCount);
            FallbackReason = fallbackReason ?? string.Empty;
            PathWaypointsLocalPositions = pathWaypointsLocalPositions ?? System.Array.Empty<Vector3>();
        }

        public EnemyNavigationBackend Backend { get; }

        public EnemyNavigationMode Mode { get; }

        public EnemyNavigationIntent Intent { get; }

        public Vector3 RequestedLocalPosition { get; }

        public Vector3 ResolvedLocalPosition { get; }

        public Vector3 SteeringDirection { get; }

        public bool ReachedRequestedPosition { get; }

        public bool UsedFallbackSteering { get; }

        public bool Blocked { get; }

        public EnemyPathStatus PathStatus { get; }

        public Vector3 FinalGoalLocalPosition { get; }

        public Vector3 NextWaypointLocalPosition { get; }

        public float PathAgeSeconds { get; }

        public int PathWaypointCount { get; }

        public string FallbackReason { get; }

        public Vector3[] PathWaypointsLocalPositions { get; }
    }
}
