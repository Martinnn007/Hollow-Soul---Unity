using UnityEngine;

namespace Hollow.Combat
{
    public static class EnemyNavigationAdapter
    {
        public const EnemyNavigationBackend CurrentBackend = EnemyNavigationBackend.UnityNavMesh;
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

            if (ShouldUseUnityNavMesh(request))
            {
                var navMeshResult = request.NavMeshAgentBridge != null
                    ? request.NavMeshAgentBridge.Resolve(request, directResult)
                    : BuildBlockedNavMeshResult(request, "missing_navmesh_agent_bridge");
                return ShouldUseLocalRecovery(navMeshResult)
                    ? ResolveLocalRecovery(request, directResult, navMeshResult.FallbackReason)
                    : navMeshResult;
            }

            if (!ShouldTryFallback(request, directResult))
            {
                return directResult;
            }

            var fallback = ResolveLocalFallback(request, directResult);
            return fallback.UsedFallbackSteering ? fallback : directResult;
        }

        private static bool ShouldUseLocalRecovery(EnemyNavigationResult navMeshResult)
        {
            return navMeshResult.Blocked &&
                navMeshResult.PathStatus is EnemyPathStatus.InvalidRequest or EnemyPathStatus.Unreachable &&
                !string.IsNullOrWhiteSpace(navMeshResult.FallbackReason);
        }

        private static EnemyNavigationResult ResolveLocalRecovery(
            in EnemyNavigationRequest request,
            EnemyNavigationResult directResult,
            string navMeshReason)
        {
            var localResult = directResult;
            if (ShouldTryFallback(request, directResult))
            {
                var detour = ResolveLocalFallback(request, directResult);
                if (detour.UsedFallbackSteering || !detour.Blocked)
                {
                    localResult = detour;
                }
            }

            var moved = localResult.ResolvedLocalPosition - request.CurrentLocalPosition;
            moved.y = 0f;
            var steering = moved.sqrMagnitude > 0.0001f ? moved.normalized : localResult.SteeringDirection;
            return new EnemyNavigationResult(
                EnemyNavigationBackend.LocalSteering,
                request.Mode,
                request.Intent,
                request.DesiredLocalPosition,
                localResult.ResolvedLocalPosition,
                steering,
                localResult.ReachedRequestedPosition,
                usedFallbackSteering: true,
                localResult.Blocked,
                EnemyPathStatus.FallbackLocal,
                request.FinalGoalLocalPosition,
                localResult.NextWaypointLocalPosition,
                request.PathAgeSeconds,
                0,
                $"navmesh_recovery:{navMeshReason}");
        }

        private static bool ShouldUseUnityNavMesh(in EnemyNavigationRequest request)
        {
            return request.AllowPathfinding &&
                request.Mode == EnemyNavigationMode.GroundedLocal &&
                request.Intent is EnemyNavigationIntent.MoveToPlayer
                    or EnemyNavigationIntent.PreferredRange
                    or EnemyNavigationIntent.Flee
                    or EnemyNavigationIntent.Wander
                    or EnemyNavigationIntent.Investigate
                    or EnemyNavigationIntent.ReturnHome;
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
            var pathRequested = ShouldUseUnityNavMesh(request);
            return new EnemyNavigationResult(
                pathRequested ? EnemyNavigationBackend.UnityNavMesh : EnemyNavigationBackend.LocalSteering,
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
                pathRequested ? "navmesh_not_started" : string.Empty);
        }

        private static EnemyNavigationResult BuildBlockedNavMeshResult(in EnemyNavigationRequest request, string reason)
        {
            EnemyNavigationDebugOverlay.RecordPathRequest();
            EnemyNavigationDebugOverlay.RecordFallback(reason);
            return new EnemyNavigationResult(
                EnemyNavigationBackend.UnityNavMesh,
                request.Mode,
                request.Intent,
                request.DesiredLocalPosition,
                request.CurrentLocalPosition,
                Vector3.zero,
                reachedRequestedPosition: false,
                usedFallbackSteering: false,
                blocked: true,
                EnemyPathStatus.InvalidRequest,
                request.FinalGoalLocalPosition,
                Vector3.zero,
                request.PathAgeSeconds,
                0,
                reason);
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
