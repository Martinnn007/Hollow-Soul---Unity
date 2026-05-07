using UnityEngine;

namespace Hollow.Combat
{
    public sealed class EnemyLocomotionAgent
    {
        private int blockedFrames;
        private Vector3 lastResolvedLocalPosition;
        private string lastBlockedReason = string.Empty;

        public int BlockedFrames => blockedFrames;

        public string LastBlockedReason => lastBlockedReason;

        public void Reset()
        {
            blockedFrames = 0;
            lastResolvedLocalPosition = Vector3.zero;
            lastBlockedReason = string.Empty;
        }

        public EnemyNavigationResult Resolve(in EnemyNavigationRequest request, EnemyTacticalIntent tacticalIntent)
        {
            var result = EnemyNavigationAdapter.Resolve(request);
            var moved = result.ResolvedLocalPosition - request.CurrentLocalPosition;
            moved.y = 0f;
            if (!result.Blocked && moved.sqrMagnitude > 0.0001f)
            {
                blockedFrames = 0;
                lastBlockedReason = string.Empty;
                lastResolvedLocalPosition = result.ResolvedLocalPosition;
                return result;
            }

            blockedFrames++;
            lastBlockedReason = string.IsNullOrWhiteSpace(result.FallbackReason)
                ? "locomotion_blocked"
                : result.FallbackReason;

            if (result.Backend == EnemyNavigationBackend.UnityNavMesh)
            {
                lastResolvedLocalPosition = result.ResolvedLocalPosition;
                return result;
            }

            if (!request.AllowLocalDetour ||
                request.Room == null ||
                !tacticalIntent.HasReservedPosition ||
                request.Mode != EnemyNavigationMode.GroundedLocal)
            {
                lastResolvedLocalPosition = result.ResolvedLocalPosition;
                return result;
            }

            if (TryResolveTacticalSidestep(request, tacticalIntent.ReservedLocalPosition, out var sidestep))
            {
                var steering = sidestep - request.CurrentLocalPosition;
                steering.y = 0f;
                blockedFrames = 0;
                lastBlockedReason = "tactical_sidestep";
                lastResolvedLocalPosition = sidestep;
                return new EnemyNavigationResult(
                    result.Backend,
                    request.Mode,
                    request.Intent,
                    request.DesiredLocalPosition,
                    sidestep,
                    steering.sqrMagnitude > 0.0001f ? steering.normalized : result.SteeringDirection,
                    Vector3.Distance(Flat(sidestep), Flat(request.DesiredLocalPosition)) <= EnemyNavigationAdapter.DefaultReachedToleranceMeters,
                    usedFallbackSteering: true,
                    blocked: false,
                    result.PathStatus,
                    request.FinalGoalLocalPosition,
                    result.NextWaypointLocalPosition,
                    result.PathAgeSeconds,
                    result.PathWaypointCount,
                    "tactical_sidestep",
                    result.PathWaypointsLocalPositions);
            }

            lastResolvedLocalPosition = result.ResolvedLocalPosition;
            return result;
        }

        private static bool TryResolveTacticalSidestep(in EnemyNavigationRequest request, Vector3 reservedLocalPosition, out Vector3 resolved)
        {
            resolved = request.CurrentLocalPosition;
            var toGoal = reservedLocalPosition - request.CurrentLocalPosition;
            toGoal.y = 0f;
            if (toGoal.sqrMagnitude <= 0.0001f)
            {
                return false;
            }

            var direction = toGoal.normalized;
            var step = request.MaxStepDistanceMeters > 0f ? request.MaxStepDistanceMeters : Mathf.Min(0.2f, toGoal.magnitude);
            var side = new Vector3(-direction.z, 0f, direction.x);
            var candidates = new[]
            {
                (direction + side * 0.7f).normalized,
                (direction - side * 0.7f).normalized,
                side,
                -side
            };

            var best = request.CurrentLocalPosition;
            var bestScore = float.NegativeInfinity;
            for (var index = 0; index < candidates.Length; index++)
            {
                var candidate = request.CurrentLocalPosition + candidates[index] * step;
                var candidateResolved = RoomLocalCollision.ResolveMove(request.Room, request.CurrentLocalPosition, candidate, request.RadiusMeters);
                if (!RoomLocalCollision.CanOccupy(request.Room, candidateResolved, request.RadiusMeters))
                {
                    continue;
                }

                var moved = candidateResolved - request.CurrentLocalPosition;
                moved.y = 0f;
                if (moved.sqrMagnitude <= 0.0001f)
                {
                    continue;
                }

                var progress = Vector3.Dot(moved.normalized, direction);
                var distance = Vector3.Distance(Flat(candidateResolved), Flat(reservedLocalPosition));
                var score = progress * 1.5f - distance * 0.08f + moved.magnitude;
                if (score <= bestScore)
                {
                    continue;
                }

                bestScore = score;
                best = candidateResolved;
            }

            if (bestScore <= float.NegativeInfinity)
            {
                return false;
            }

            resolved = best;
            return true;
        }

        private static Vector3 Flat(Vector3 value)
        {
            value.y = 0f;
            return value;
        }
    }
}
