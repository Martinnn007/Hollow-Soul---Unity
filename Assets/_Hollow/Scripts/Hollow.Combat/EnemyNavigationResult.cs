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
            bool blocked)
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
    }
}
