using UnityEngine;

namespace Hollow.Combat
{
    public static class EnemyNavigationAdapter
    {
        public const EnemyNavigationBackend CurrentBackend = EnemyNavigationBackend.LocalSteering;
        public const float DefaultReachedToleranceMeters = 0.035f;

        private static readonly float[] CandidateAnglesDegrees =
        {
            28f,
            -28f,
            55f,
            -55f,
            82f,
            -82f
        };

        public static EnemyNavigationMode DefaultModeFor(EnemyMovementMode movementMode)
        {
            return movementMode == EnemyMovementMode.Flying
                ? EnemyNavigationMode.FlyingLocal
                : EnemyNavigationMode.GroundedLocal;
        }

        public static EnemyNavigationResult Resolve(in EnemyNavigationRequest request)
        {
            var current = request.CurrentLocalPosition;
            var desired = request.DesiredLocalPosition;
            var direct = ResolveByMode(request.Mode, request.Room, current, desired, request.RadiusMeters);
            var directResult = BuildResult(request, direct, usedFallback: false);
            if (RoomGridAStarPathfinder.CanUsePathfinding(request))
            {
                var pathResult = RoomGridAStarPathfinder.Resolve(request, directResult);
                if (pathResult.Backend == EnemyNavigationBackend.RoomGridAStar)
                {
                    return pathResult;
                }

                directResult = pathResult;
            }

            if (!ShouldTryFallback(request, directResult))
            {
                return directResult;
            }

            var fallback = ResolveLocalFallback(request, directResult);
            return fallback.UsedFallbackSteering ? fallback : directResult;
        }

        private static bool ShouldTryFallback(in EnemyNavigationRequest request, EnemyNavigationResult directResult)
        {
            if (!request.AllowLocalDetour ||
                request.Mode != EnemyNavigationMode.GroundedLocal ||
                request.Room == null ||
                directResult.ReachedRequestedPosition ||
                !CanDetourIntent(request.Intent))
            {
                return false;
            }

            var requestedDelta = request.DesiredLocalPosition - request.CurrentLocalPosition;
            requestedDelta.y = 0f;
            if (requestedDelta.sqrMagnitude <= 0.0001f)
            {
                return false;
            }

            var resolvedDelta = directResult.ResolvedLocalPosition - request.CurrentLocalPosition;
            resolvedDelta.y = 0f;
            var requestedDistance = requestedDelta.magnitude;
            var progress = Vector3.Dot(resolvedDelta, requestedDelta.normalized);
            var minimumProgress = Mathf.Min(0.12f, requestedDistance * 0.25f);
            return progress < minimumProgress || directResult.Blocked;
        }

        private static bool CanDetourIntent(EnemyNavigationIntent intent)
        {
            return intent is EnemyNavigationIntent.MoveToPlayer
                or EnemyNavigationIntent.PreferredRange
                or EnemyNavigationIntent.Flee
                or EnemyNavigationIntent.Wander
                or EnemyNavigationIntent.Investigate
                or EnemyNavigationIntent.ReturnHome
                or EnemyNavigationIntent.BumpSeparation;
        }

        private static EnemyNavigationResult ResolveLocalFallback(in EnemyNavigationRequest request, EnemyNavigationResult directResult)
        {
            var requestedDelta = request.DesiredLocalPosition - request.CurrentLocalPosition;
            requestedDelta.y = 0f;
            var distance = requestedDelta.magnitude;
            if (distance <= 0.001f)
            {
                return directResult;
            }

            var requestedDirection = requestedDelta.normalized;
            var bestPosition = directResult.ResolvedLocalPosition;
            var bestDirection = directResult.SteeringDirection;
            var bestScore = ScoreCandidate(request.CurrentLocalPosition, bestPosition, requestedDirection, request.DesiredLocalPosition);
            var maxCandidates = CandidateCountFor(request.Intelligence);
            for (var index = 0; index < maxCandidates; index++)
            {
                var candidateDirection = Quaternion.Euler(0f, CandidateAnglesDegrees[index], 0f) * requestedDirection;
                var candidateDesired = request.CurrentLocalPosition + candidateDirection.normalized * distance;
                var candidate = RoomLocalCollision.ResolveMove(request.Room, request.CurrentLocalPosition, candidateDesired, request.RadiusMeters);
                var score = ScoreCandidate(request.CurrentLocalPosition, candidate, requestedDirection, request.DesiredLocalPosition);
                if (score <= bestScore + 0.005f)
                {
                    continue;
                }

                bestScore = score;
                bestPosition = candidate;
                var moved = candidate - request.CurrentLocalPosition;
                moved.y = 0f;
                bestDirection = moved.sqrMagnitude > 0.0001f ? moved.normalized : candidateDirection.normalized;
            }

            if ((bestPosition - directResult.ResolvedLocalPosition).sqrMagnitude <= 0.0001f)
            {
                return directResult;
            }

            return BuildResult(request, bestPosition, usedFallback: true, bestDirection);
        }

        private static int CandidateCountFor(EnemyIntelligenceLevel intelligence)
        {
            return intelligence switch
            {
                EnemyIntelligenceLevel.Cunning => CandidateAnglesDegrees.Length,
                EnemyIntelligenceLevel.Tactical => CandidateAnglesDegrees.Length,
                EnemyIntelligenceLevel.Trained => 4,
                EnemyIntelligenceLevel.Basic => 4,
                EnemyIntelligenceLevel.Simple => 2,
                _ => 2
            };
        }

        private static float ScoreCandidate(Vector3 current, Vector3 candidate, Vector3 requestedDirection, Vector3 desired)
        {
            var moved = candidate - current;
            moved.y = 0f;
            var progress = Vector3.Dot(moved, requestedDirection);
            var distanceToDesired = Vector3.Distance(Flat(candidate), Flat(desired));
            return progress * 2f + moved.magnitude * 0.25f - distanceToDesired * 0.08f;
        }

        private static EnemyNavigationResult BuildResult(in EnemyNavigationRequest request, Vector3 resolved, bool usedFallback, Vector3? steeringOverride = null)
        {
            var requestedFlat = Flat(request.DesiredLocalPosition);
            var resolvedFlat = Flat(resolved);
            var moved = resolved - request.CurrentLocalPosition;
            moved.y = 0f;
            var steering = steeringOverride ?? (moved.sqrMagnitude > 0.0001f ? moved.normalized : Vector3.zero);
            var reached = Vector3.Distance(requestedFlat, resolvedFlat) <= DefaultReachedToleranceMeters;
            var requestedDelta = request.DesiredLocalPosition - request.CurrentLocalPosition;
            requestedDelta.y = 0f;
            var progress = requestedDelta.sqrMagnitude > 0.0001f
                ? Vector3.Dot(moved, requestedDelta.normalized)
                : 0f;
            var blocked = (!reached && moved.sqrMagnitude <= 0.0001f && requestedDelta.sqrMagnitude > 0.0001f) ||
                progress < -0.01f;
            var pathRequested = RoomGridAStarPathfinder.CanUsePathfinding(request);
            return new EnemyNavigationResult(
                CurrentBackend,
                request.Mode,
                request.Intent,
                request.DesiredLocalPosition,
                resolved,
                steering,
                reached,
                usedFallback,
                blocked,
                pathRequested ? EnemyPathStatus.FallbackLocal : EnemyPathStatus.NotRequested,
                request.FinalGoalLocalPosition,
                Vector3.zero,
                request.PathAgeSeconds,
                0,
                pathRequested ? "local_backend" : string.Empty);
        }

        private static Vector3 ResolveByMode(EnemyNavigationMode mode, Hollow.Rooms.RoomRuntimeRoot room, Vector3 current, Vector3 desired, float radius)
        {
            return mode switch
            {
                EnemyNavigationMode.FlyingLocal => RoomLocalCollision.ResolveFlyingMove(room, desired, radius),
                EnemyNavigationMode.IgnoreObstaclesPhase => RoomLocalCollision.ResolveMoveIgnoringObstacles(room, desired, radius),
                _ => RoomLocalCollision.ResolveMove(room, current, desired, radius)
            };
        }

        private static Vector3 Flat(Vector3 value)
        {
            value.y = 0f;
            return value;
        }
    }
}
