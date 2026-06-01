using Hollow.Rooms;
using Hollow.Core.Diagnostics;
using UnityEngine;
using UnityEngine.AI;

namespace Hollow.Combat
{
    [DisallowMultipleComponent]
    public sealed class EnemyNavMeshAgentBridge : MonoBehaviour
    {
        private const float DestinationRefreshToleranceMeters = 0.22f;

        private NavMeshPath reusablePath;
        private NavMeshAgent agent;
        private RoomRuntimeRoot room;
        private Vector3 lastDestinationWorld;
        private bool hasDestination;
        private float lastRepathTime = float.NegativeInfinity;
        private string lastReason = string.Empty;
        private EnemyLocomotionOwnership currentOwnership = EnemyLocomotionOwnership.None;
        private string lastOwnershipReason = string.Empty;
        private string lastSyncReason = string.Empty;
        private int syncToTransformCount;

        public NavMeshAgent Agent => agent;

        public string LastReason => lastReason;

        public EnemyLocomotionOwnership CurrentOwnership => currentOwnership;

        public string LastOwnershipReason => lastOwnershipReason;

        public string LastSyncReason => lastSyncReason;

        public int SyncToTransformCount => syncToTransformCount;

        public bool IsConfiguredForNavigation => agent != null && agent.enabled && room != null && room.HasNavMeshBake;

        public void Configure(RoomRuntimeRoot nextRoom, float radiusMeters, float speedMetersPerSecond, bool enabledForGroundedNavigation)
        {
            room = nextRoom;
            if (!TryGetComponent(out agent))
            {
                try
                {
                    agent = gameObject.AddComponent<NavMeshAgent>();
                }
                catch (System.Exception exception)
                {
                    lastReason = $"navmesh_agent_add_failed:{exception.GetType().Name}";
                    ClaimOwnership(EnemyLocomotionOwnership.Disabled, lastReason);
                    enabled = false;
                    return;
                }
            }

            if (agent == null)
            {
                lastReason = "navmesh_agent_add_failed";
                ClaimOwnership(EnemyLocomotionOwnership.Disabled, lastReason);
                enabled = false;
                return;
            }

            try
            {
                reusablePath ??= new NavMeshPath();
                agent.enabled = false;
                agent.updatePosition = false;
                agent.updateRotation = false;
                agent.autoBraking = false;
                agent.autoTraverseOffMeshLink = false;
                agent.radius = Mathf.Max(0.08f, radiusMeters);
                agent.height = RoomNavMeshBuildUtility.AgentHeightMeters;
                agent.baseOffset = Mathf.Max(0f, transform.localPosition.y);
                agent.speed = Mathf.Max(0.05f, speedMetersPerSecond);
                agent.acceleration = Mathf.Max(8f, agent.speed * 8f);
                agent.angularSpeed = 720f;
                agent.stoppingDistance = EnemyNavigationAdapter.DefaultReachedToleranceMeters;
                agent.obstacleAvoidanceType = ObstacleAvoidanceType.LowQualityObstacleAvoidance;
                agent.avoidancePriority = Mathf.Clamp(50 + Mathf.RoundToInt(radiusMeters * 20f), 5, 95);
                agent.enabled = enabledForGroundedNavigation && room != null && room.HasNavMeshBake;
            }
            catch (MissingComponentException)
            {
                agent = null;
                lastReason = "navmesh_agent_missing_component";
                ClaimOwnership(EnemyLocomotionOwnership.Disabled, lastReason);
                enabled = false;
                return;
            }
            catch (System.Exception exception)
            {
                lastReason = $"navmesh_agent_config_failed:{exception.GetType().Name}";
                ClaimOwnership(EnemyLocomotionOwnership.Disabled, lastReason);
                enabled = false;
                return;
            }

            hasDestination = false;
            lastDestinationWorld = transform.position;
            lastRepathTime = float.NegativeInfinity;
            lastReason = string.Empty;
            lastSyncReason = string.Empty;
            syncToTransformCount = 0;
            if (agent.enabled)
            {
                if (TryWarpToNearestNavMesh() && agent.isOnNavMesh)
                {
                    agent.nextPosition = transform.position;
                    ClaimOwnership(EnemyLocomotionOwnership.UnityNavMeshAgent, "configured_for_grounded_navigation");
                    return;
                }

                lastReason = "agent_not_on_navmesh";
                agent.enabled = false;
            }

            ClaimOwnership(enabledForGroundedNavigation ? EnemyLocomotionOwnership.Disabled : EnemyLocomotionOwnership.HollowManual, lastReason);
        }

        public void StopAgent(bool clearPath)
        {
            ClaimHollowManual(clearPath, clearPath ? "stop_clear_path" : "stop_keep_path");
        }

        public void ClaimHollowManual(bool clearPath, string reason)
        {
            if (agent == null || !agent.enabled || !agent.isOnNavMesh)
            {
                ClaimOwnership(agent == null ? EnemyLocomotionOwnership.Disabled : EnemyLocomotionOwnership.HollowManual, reason);
                return;
            }

            agent.isStopped = true;
            if (clearPath)
            {
                agent.ResetPath();
                hasDestination = false;
            }

            ClaimOwnership(EnemyLocomotionOwnership.HollowManual, reason);
        }

        public void DisableAgent()
        {
            if (agent == null)
            {
                ClaimOwnership(EnemyLocomotionOwnership.Disabled, "disable_missing_agent");
                return;
            }

            if (agent.enabled && agent.isOnNavMesh)
            {
                agent.ResetPath();
            }

            agent.enabled = false;
            hasDestination = false;
            ClaimOwnership(EnemyLocomotionOwnership.Disabled, "disable_agent");
        }

        public void SyncToTransform()
        {
            SyncAfterHollowOwnedMotion("sync_to_transform");
        }

        public bool SyncAfterHollowOwnedMotion(string reason)
        {
            if (agent == null || !agent.enabled)
            {
                lastSyncReason = agent == null
                    ? $"sync_skipped_missing_agent:{reason}"
                    : $"sync_skipped_agent_disabled:{reason}";
                return false;
            }

            var synced = TryWarpToNearestNavMesh();
            if (synced)
            {
                hasDestination = false;
                lastDestinationWorld = transform.position;
                syncToTransformCount++;
                lastSyncReason = string.IsNullOrWhiteSpace(reason) ? "sync_after_hollow_motion" : reason;
                ClaimOwnership(EnemyLocomotionOwnership.HollowManual, lastSyncReason);
            }
            else
            {
                lastSyncReason = $"sync_failed:{reason}";
            }

            return synced;
        }

        public EnemyNavigationResult Resolve(in EnemyNavigationRequest request, EnemyNavigationResult localResult)
        {
            EnemyNavigationDebugOverlay.RecordPathRequest();
            if (!CanResolve(request, out var invalidReason))
            {
                EnemyNavigationDebugOverlay.RecordFallback(invalidReason);
                return BuildResult(request, request.CurrentLocalPosition, request.CurrentLocalPosition, EnemyPathStatus.InvalidRequest, invalidReason, blocked: true);
            }

            var desiredSpeed = request.DesiredSpeedMetersPerSecond > 0f ? request.DesiredSpeedMetersPerSecond : agent.speed;
            agent.speed = Mathf.Max(0.05f, desiredSpeed);
            agent.acceleration = Mathf.Max(8f, agent.speed * 8f);
            ApplyAvoidanceTier(request);
            agent.nextPosition = transform.position;
            ClaimOwnership(EnemyLocomotionOwnership.UnityNavMeshAgent, $"movement:{request.Intent}");

            if (!TrySampleLocalPosition(request.FinalGoalLocalPosition, out var destinationWorld))
            {
                EnemyNavigationDebugOverlay.RecordFallback("navmesh_goal_unreachable");
                return BuildResult(request, request.CurrentLocalPosition, request.CurrentLocalPosition, EnemyPathStatus.Unreachable, "navmesh_goal_unreachable", blocked: true);
            }

            var shouldRepath = !hasDestination ||
                Vector3.Distance(Flat(lastDestinationWorld), Flat(destinationWorld)) > DestinationRefreshToleranceMeters ||
                Time.time - lastRepathTime >= EnemyNavigationBudget.RepathIntervalFor(request) ||
                agent.pathStatus == NavMeshPathStatus.PathInvalid;

            if (shouldRepath)
            {
                var highPriority = EnemyNavigationBudget.IsHighPriority(request);
                if (!hasDestination &&
                    !highPriority &&
                    Time.time < EnemyNavigationBudget.InitialRepathOffsetSeconds(request.PathSeed))
                {
                    EnemyNavigationDebugOverlay.RecordBudgetDeferred($"m3_initial_stagger:{request.AiLodTier}:{request.TacticalRole}");
                    agent.isStopped = false;
                    return localResult;
                }

                if (!EnemyNavigationBudget.TryAcquirePathSolve(request, highPriority))
                {
                    agent.isStopped = false;
                    return localResult;
                }

                reusablePath ??= new NavMeshPath();
                var started = Time.realtimeSinceStartup;
                var stageStarted = BeginCpuStage(out var stageStartingGc);
                var pathCalculated = agent.CalculatePath(destinationWorld, reusablePath);
                EndCpuStage(M136CpuStageKind.NavMeshRequest, stageStarted, stageStartingGc);
                if (!pathCalculated || reusablePath.status == NavMeshPathStatus.PathInvalid)
                {
                    lastReason = "navmesh_path_invalid";
                    EnemyNavigationDebugOverlay.RecordFreshPathSolve((Time.realtimeSinceStartup - started) * 1000f);
                    EnemyNavigationDebugOverlay.RecordFallback(lastReason);
                    return BuildResult(request, request.CurrentLocalPosition, request.CurrentLocalPosition, EnemyPathStatus.Unreachable, lastReason, blocked: true);
                }

                agent.isStopped = false;
                agent.SetPath(reusablePath);
                hasDestination = true;
                lastDestinationWorld = destinationWorld;
                lastRepathTime = Time.time;
                EnemyNavigationDebugOverlay.RecordFreshPathSolve((Time.realtimeSinceStartup - started) * 1000f);
            }
            else
            {
                agent.isStopped = false;
            }

            var status = MapStatus(agent.pathStatus);
            var corners = agent.path != null ? agent.path.corners : System.Array.Empty<Vector3>();
            var nextWaypointLocal = ResolveNextWaypointLocal(request, corners);
            var waypointsLocal = ConvertCornersToLocal(corners);
            var steering = ResolveSteeringDirection(request, corners);
            var resolvedLocal = ResolveAgentOwnedStepLocal(request, nextWaypointLocal, steering);
            var reached = agent.remainingDistance <= agent.stoppingDistance + EnemyNavigationAdapter.DefaultReachedToleranceMeters &&
                !agent.pathPending;
            var blocked = status is EnemyPathStatus.Unreachable or EnemyPathStatus.InvalidRequest;
            var stuck = IsLikelyStuck(reached, status);
            lastReason = blocked ? "navmesh_blocked" : stuck ? "navmesh_stuck_or_waiting" : string.Empty;
            if (status is EnemyPathStatus.Ready or EnemyPathStatus.Partial)
            {
                EnemyNavigationDebugOverlay.ReportActivePathUser(GetInstanceID(), agent.pathPending, stuck, lastReason);
            }

            return new EnemyNavigationResult(
                EnemyNavigationBackend.UnityNavMesh,
                request.Mode,
                request.Intent,
                request.DesiredLocalPosition,
                resolvedLocal,
                steering,
                reached,
                usedFallbackSteering: false,
                blocked,
                status,
                request.FinalGoalLocalPosition,
                nextWaypointLocal,
                request.PathAgeSeconds,
                waypointsLocal.Length,
                lastReason,
                waypointsLocal);
        }

        private bool IsLikelyStuck(bool reached, EnemyPathStatus status)
        {
            if (agent == null ||
                reached ||
                agent.pathPending ||
                (status != EnemyPathStatus.Ready && status != EnemyPathStatus.Partial) ||
                Time.time - lastRepathTime <= 0.35f)
            {
                return false;
            }

            var remainingDistance = agent.remainingDistance;
            if (float.IsInfinity(remainingDistance) ||
                remainingDistance <= agent.stoppingDistance + 0.55f)
            {
                return false;
            }

            return Flat(agent.desiredVelocity).sqrMagnitude <= 0.0004f;
        }

        private bool CanResolve(in EnemyNavigationRequest request, out string reason)
        {
            if (!request.AllowPathfinding)
            {
                reason = "path_not_allowed";
                return false;
            }

            if (request.Mode != EnemyNavigationMode.GroundedLocal)
            {
                reason = "not_grounded_navmesh_mode";
                return false;
            }

            if (room == null || request.Room == null || !request.Room.HasNavMeshBake)
            {
                reason = request.Room != null && !string.IsNullOrWhiteSpace(request.Room.NavMeshBakeError)
                    ? request.Room.NavMeshBakeError
                    : "missing_room_navmesh";
                return false;
            }

            if (agent == null)
            {
                reason = "missing_navmesh_agent";
                return false;
            }

            if (!agent.enabled)
            {
                reason = string.IsNullOrWhiteSpace(lastReason) ? "navmesh_agent_disabled" : lastReason;
                return false;
            }

            if (!agent.isOnNavMesh && !TryWarpToNearestNavMesh())
            {
                reason = "agent_not_on_navmesh";
                return false;
            }

            reason = string.Empty;
            return true;
        }

        private bool TryWarpToNearestNavMesh()
        {
            if (agent == null || !agent.enabled)
            {
                return false;
            }

            if (NavMesh.SamplePosition(transform.position, out var hit, RoomNavMeshBuildUtility.NavMeshSampleRadiusMeters, NavMesh.AllAreas))
            {
                return agent.Warp(hit.position);
            }

            return false;
        }

        private bool TrySampleLocalPosition(Vector3 localPosition, out Vector3 worldPosition)
        {
            worldPosition = room != null ? room.transform.TransformPoint(localPosition) : transform.TransformPoint(localPosition);
            if (!NavMesh.SamplePosition(worldPosition, out var hit, RoomNavMeshBuildUtility.NavMeshSampleRadiusMeters, NavMesh.AllAreas))
            {
                return false;
            }

            worldPosition = hit.position;
            return true;
        }

        private Vector3 ResolveNextWaypointLocal(in EnemyNavigationRequest request, Vector3[] corners)
        {
            if (corners != null && corners.Length > 1)
            {
                return room != null ? room.transform.InverseTransformPoint(corners[1]) : transform.InverseTransformPoint(corners[1]);
            }

            if (corners != null && corners.Length == 1)
            {
                return room != null ? room.transform.InverseTransformPoint(corners[0]) : transform.InverseTransformPoint(corners[0]);
            }

            return request.FinalGoalLocalPosition;
        }

        private Vector3[] ConvertCornersToLocal(Vector3[] corners)
        {
            if (corners == null || corners.Length == 0)
            {
                return System.Array.Empty<Vector3>();
            }

            var converted = new Vector3[corners.Length];
            for (var index = 0; index < corners.Length; index++)
            {
                converted[index] = room != null ? room.transform.InverseTransformPoint(corners[index]) : transform.InverseTransformPoint(corners[index]);
            }

            return converted;
        }

        private Vector3 ResolveSteeringDirection(in EnemyNavigationRequest request, Vector3[] corners)
        {
            var currentWorld = transform.position;
            var nextWorld = corners != null && corners.Length > 1
                ? corners[1]
                : room != null ? room.transform.TransformPoint(request.FinalGoalLocalPosition) : currentWorld;
            var delta = nextWorld - currentWorld;
            delta.y = 0f;
            if (delta.sqrMagnitude > 0.0001f)
            {
                return delta.normalized;
            }

            var desired = request.DesiredLocalPosition - request.CurrentLocalPosition;
            desired.y = 0f;
            return desired.sqrMagnitude > 0.0001f ? desired.normalized : Vector3.zero;
        }

        private Vector3 ResolveAgentOwnedStepLocal(in EnemyNavigationRequest request, Vector3 nextWaypointLocal, Vector3 steeringDirection)
        {
            var current = request.CurrentLocalPosition;
            var maxStep = Mathf.Max(0f, request.MaxStepDistanceMeters);
            if (maxStep <= 0.0001f)
            {
                return current;
            }

            var toWaypoint = nextWaypointLocal - current;
            toWaypoint.y = 0f;
            var direction = toWaypoint.sqrMagnitude > 0.0001f
                ? toWaypoint.normalized
                : steeringDirection;
            direction.y = 0f;
            if (direction.sqrMagnitude <= 0.0001f)
            {
                return current;
            }

            var distance = toWaypoint.sqrMagnitude > 0.0001f
                ? Mathf.Min(maxStep, toWaypoint.magnitude)
                : maxStep;
            var fallbackWorldDirection = room != null
                ? room.transform.TransformDirection(direction.normalized)
                : transform.TransformDirection(direction.normalized);
            fallbackWorldDirection.y = 0f;
            var velocityDirection = agent != null && Flat(agent.desiredVelocity).sqrMagnitude > 0.0001f
                ? Flat(agent.desiredVelocity).normalized
                : fallbackWorldDirection.sqrMagnitude > 0.0001f
                    ? fallbackWorldDirection.normalized
                    : direction.normalized;
            try
            {
                agent.Move(velocityDirection * distance);
                var nextLocal = room != null
                    ? room.transform.InverseTransformPoint(agent.nextPosition)
                    : transform.InverseTransformPoint(agent.nextPosition);
                nextLocal.y = current.y;
                return nextLocal;
            }
            catch (System.Exception exception)
            {
                lastReason = $"navmesh_agent_move_failed:{exception.GetType().Name}";
                EnemyNavigationDebugOverlay.RecordFallback(lastReason);
                return current;
            }
        }

        private static EnemyPathStatus MapStatus(NavMeshPathStatus status)
        {
            return status switch
            {
                NavMeshPathStatus.PathComplete => EnemyPathStatus.Ready,
                NavMeshPathStatus.PathPartial => EnemyPathStatus.Partial,
                NavMeshPathStatus.PathInvalid => EnemyPathStatus.Unreachable,
                _ => EnemyPathStatus.InvalidRequest
            };
        }

        private static Vector3 Flat(Vector3 value)
        {
            value.y = 0f;
            return value;
        }

        private void ApplyAvoidanceTier(in EnemyNavigationRequest request)
        {
            if (agent == null)
            {
                return;
            }

            if ((!request.RoomHasActiveBoss || request.IsBoss) &&
                request.AiLodTier == EnemyAiLodTier.Full &&
                request.TacticalRole == EnemyTacticalRole.ActiveThreat)
            {
                agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
                agent.avoidancePriority = Mathf.Clamp(30 + Mathf.Abs(request.PathSeed % 18), 5, 45);
                M136PerformanceOperationCounters.ReportAvoidanceTier((int)EnemyAiLodTier.Full);
                return;
            }

            if (request.AiLodTier == EnemyAiLodTier.Reduced)
            {
                agent.obstacleAvoidanceType = ObstacleAvoidanceType.LowQualityObstacleAvoidance;
                agent.avoidancePriority = Mathf.Clamp(55 + Mathf.Abs(request.PathSeed % 20), 45, 78);
                M136PerformanceOperationCounters.ReportAvoidanceTier((int)EnemyAiLodTier.Reduced);
                return;
            }

            agent.obstacleAvoidanceType = ObstacleAvoidanceType.NoObstacleAvoidance;
            agent.avoidancePriority = Mathf.Clamp(82 + Mathf.Abs(request.PathSeed % 12), 78, 95);
            M136PerformanceOperationCounters.ReportAvoidanceTier((int)EnemyAiLodTier.Background);
        }

        private void ClaimOwnership(EnemyLocomotionOwnership ownership, string reason)
        {
            currentOwnership = ownership;
            lastOwnershipReason = reason ?? string.Empty;
        }

        private static EnemyNavigationResult BuildResult(
            in EnemyNavigationRequest request,
            Vector3 resolvedLocal,
            Vector3 nextWaypointLocal,
            EnemyPathStatus status,
            string reason,
            bool blocked)
        {
            return new EnemyNavigationResult(
                EnemyNavigationBackend.UnityNavMesh,
                request.Mode,
                request.Intent,
                request.DesiredLocalPosition,
                resolvedLocal,
                Vector3.zero,
                reachedRequestedPosition: false,
                usedFallbackSteering: false,
                blocked,
                status,
                request.FinalGoalLocalPosition,
                nextWaypointLocal,
                request.PathAgeSeconds,
                0,
                reason);
        }

        private static float BeginCpuStage(out long startingGc)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            startingGc = 0;
            return Time.realtimeSinceStartup;
#else
            startingGc = 0;
            return 0f;
#endif
        }

        private static void EndCpuStage(M136CpuStageKind stage, float startedRealtime, long startingGc)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            var elapsedMilliseconds = Mathf.Max(0f, (Time.realtimeSinceStartup - startedRealtime) * 1000f);
            M136PerformanceOperationCounters.ReportCpuStage(stage, elapsedMilliseconds, 0L);
#endif
        }
    }
}
