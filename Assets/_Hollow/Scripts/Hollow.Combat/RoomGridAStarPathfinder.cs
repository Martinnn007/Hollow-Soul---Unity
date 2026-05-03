using System.Collections.Generic;
using Hollow.Rooms;
using UnityEngine;

namespace Hollow.Combat
{
    public static class RoomGridAStarPathfinder
    {
        public const float CellSizeMeters = 0.5f;
        private const int SearchBudgetNodes = 4096;
        private const int MaxFreshPathSolvesPerFrame = 24;
        private const int HighIntelligenceReservePathSolvesPerFrame = 8;
        private const float OrthogonalCost = 1f;
        private const float DiagonalCost = 1.4142135f;

        private static readonly Dictionary<int, RoomPathfindingGraph> GraphCache = new();
        private static int budgetFrame = -1;
        private static int freshSolvesUsedThisFrame;

        private static readonly Vector2Int[] NeighborOffsets =
        {
            new(1, 0),
            new(-1, 0),
            new(0, 1),
            new(0, -1),
            new(1, 1),
            new(1, -1),
            new(-1, 1),
            new(-1, -1)
        };

        public static EnemyNavigationResult Resolve(in EnemyNavigationRequest request, EnemyNavigationResult localFallback)
        {
            EnemyNavigationDebugOverlay.RecordPathRequest();
            if (!CanUsePathfinding(request))
            {
                return WithFallbackPathState(localFallback, EnemyPathStatus.NotRequested, "path_not_allowed", request);
            }

            if (!TryConsumeFreshPathBudget(request.Intelligence, out var budgetReason))
            {
                return WithFallbackPathState(localFallback, EnemyPathStatus.FallbackLocal, budgetReason, request);
            }

            var graph = ResolveGraph(request.Room);
            if (graph == null || graph.NodeCount <= 0)
            {
                return WithFallbackPathState(localFallback, EnemyPathStatus.InvalidRequest, "missing_room_graph", request);
            }

            var validNodes = ResolveValidNodes(graph, request.Room, request.RadiusMeters);
            if (!TryFindNearestValidNode(graph, validNodes, request.CurrentLocalPosition, out var startIndex))
            {
                return WithFallbackPathState(localFallback, EnemyPathStatus.InvalidRequest, "invalid_start", request);
            }

            List<int> path;
            bool fullPathFound;
            var finalPathGoal = request.FinalGoalLocalPosition;
            if (request.HasActionEnvelopeGoal)
            {
                if (!TryFindActionEnvelopePath(
                    graph,
                    validNodes,
                    request.Room,
                    startIndex,
                    request.CurrentLocalPosition,
                    request.ActionEnvelopeAnchorLocalPosition,
                    request.ActionEnvelopeDesiredDistanceMeters,
                    request.ActionEnvelopeMinDistanceMeters,
                    request.ActionEnvelopeMaxDistanceMeters,
                    request.RadiusMeters,
                    request.Intelligence,
                    request.PathSeed,
                    out path,
                    out finalPathGoal))
                {
                    return WithFallbackPathState(localFallback, EnemyPathStatus.Unreachable, "no_envelope_path", request);
                }

                fullPathFound = true;
            }
            else
            {
                if (!TryFindNearestValidNode(graph, validNodes, request.FinalGoalLocalPosition, out var goalIndex))
                {
                    return WithFallbackPathState(localFallback, EnemyPathStatus.Unreachable, "invalid_goal", request);
                }

                path = FindPath(graph, validNodes, startIndex, goalIndex, request.FinalGoalLocalPosition, out fullPathFound);
            }

            if (path.Count <= 0)
            {
                return WithFallbackPathState(localFallback, EnemyPathStatus.Unreachable, "no_reachable_node", request);
            }

            var status = fullPathFound ? EnemyPathStatus.Ready : EnemyPathStatus.Partial;
            var nextWaypoint = ResolveLookaheadWaypoint(graph, path, request.CurrentLocalPosition, request.RadiusMeters, request.Room);
            var current = request.CurrentLocalPosition;
            var toWaypoint = nextWaypoint - current;
            toWaypoint.y = 0f;
            var maxStep = request.MaxStepDistanceMeters > 0f
                ? request.MaxStepDistanceMeters
                : FlatDistance(current, request.DesiredLocalPosition);
            if (maxStep <= 0.001f || toWaypoint.sqrMagnitude <= 0.0001f)
            {
                return BuildPathResult(request, current, nextWaypoint, graph, path, status, status == EnemyPathStatus.Partial ? "partial_path" : string.Empty, finalPathGoal);
            }

            var desired = current + toWaypoint.normalized * Mathf.Min(maxStep, toWaypoint.magnitude);
            var resolved = RoomLocalCollision.ResolveMove(request.Room, current, desired, request.RadiusMeters);
            var moved = resolved - current;
            moved.y = 0f;
            if (moved.sqrMagnitude <= 0.0001f && FlatDistance(current, nextWaypoint) > EnemyNavigationAdapter.DefaultReachedToleranceMeters)
            {
                return WithFallbackPathState(localFallback, EnemyPathStatus.FallbackLocal, "path_step_blocked", request);
            }

            return BuildPathResult(request, resolved, nextWaypoint, graph, path, status, status == EnemyPathStatus.Partial ? "partial_path" : string.Empty, finalPathGoal);
        }

        public static void ResetRuntimeStateForTests()
        {
            GraphCache.Clear();
            budgetFrame = -1;
            freshSolvesUsedThisFrame = 0;
            EnemyNavigationDebugOverlay.ResetDiagnostics();
        }

        public static bool TryResolveActionEnvelopeGoal(
            RoomRuntimeRoot room,
            Vector3 currentLocalPosition,
            Vector3 anchorLocalPosition,
            float desiredDistanceMeters,
            float minDistanceMeters,
            float maxDistanceMeters,
            float radiusMeters,
            EnemyIntelligenceLevel intelligence,
            int pathSeed,
            out Vector3 goalLocalPosition)
        {
            goalLocalPosition = anchorLocalPosition;
            var graph = ResolveGraph(room);
            if (graph == null || graph.NodeCount <= 0)
            {
                return false;
            }

            if (!TryConsumeFreshPathBudget(intelligence, out _))
            {
                return false;
            }

            radiusMeters = Mathf.Max(RoomLocalCollision.MinimumRadiusMeters, radiusMeters);
            var validNodes = ResolveValidNodes(graph, room, radiusMeters);
            if (!TryFindNearestValidNode(graph, validNodes, currentLocalPosition, out var startIndex))
            {
                return false;
            }

            return TryFindActionEnvelopePath(
                graph,
                validNodes,
                room,
                startIndex,
                currentLocalPosition,
                anchorLocalPosition,
                desiredDistanceMeters,
                minDistanceMeters,
                maxDistanceMeters,
                radiusMeters,
                intelligence,
                pathSeed,
                out _,
                out goalLocalPosition);
        }

        public static bool CanUsePathfinding(in EnemyNavigationRequest request)
        {
            if (!request.AllowPathfinding ||
                request.Mode != EnemyNavigationMode.GroundedLocal ||
                request.Room == null ||
                request.RadiusMeters <= 0f)
            {
                return false;
            }

            return request.Intent is EnemyNavigationIntent.MoveToPlayer
                or EnemyNavigationIntent.PreferredRange
                or EnemyNavigationIntent.Flee
                or EnemyNavigationIntent.Wander
                or EnemyNavigationIntent.Investigate
                or EnemyNavigationIntent.ReturnHome;
        }

        private static RoomPathfindingGraph ResolveGraph(RoomRuntimeRoot room)
        {
            if (room == null)
            {
                return null;
            }

            var cacheKey = room.GetInstanceID();
            var bounds = room.LocalBounds;
            if (GraphCache.TryGetValue(cacheKey, out var cached) && cached.Matches(bounds))
            {
                return cached;
            }

            var graph = new RoomPathfindingGraph(bounds, CellSizeMeters);
            GraphCache[cacheKey] = graph;
            return graph;
        }

        private static bool[] ResolveValidNodes(RoomPathfindingGraph graph, RoomRuntimeRoot room, float radiusMeters)
        {
            var radiusBucket = RadiusBucketFor(radiusMeters);
            var signature = OccupancySignature(room, radiusBucket);
            if (graph.TryGetValidNodes(radiusBucket, signature, out var cached))
            {
                EnemyNavigationDebugOverlay.RecordCacheHit();
                return cached;
            }

            var valid = BuildValidNodes(graph, room, radiusMeters);
            graph.StoreValidNodes(radiusBucket, signature, valid);
            EnemyNavigationDebugOverlay.RecordOccupancyBuild();
            return valid;
        }

        private static bool[] BuildValidNodes(RoomPathfindingGraph graph, RoomRuntimeRoot room, float radiusMeters)
        {
            var valid = new bool[graph.NodeCount];
            for (var index = 0; index < valid.Length; index++)
            {
                var position = graph.PositionFor(index);
                valid[index] = CanOccupy(room, position, radiusMeters);
            }

            return valid;
        }

        private static int RadiusBucketFor(float radiusMeters)
        {
            return Mathf.Clamp(Mathf.RoundToInt(Mathf.Max(RoomLocalCollision.MinimumRadiusMeters, radiusMeters) * 100f), 1, 500);
        }

        private static int OccupancySignature(RoomRuntimeRoot room, int radiusBucket)
        {
            unchecked
            {
                var hash = 17;
                hash = hash * 31 + radiusBucket;
                if (room == null)
                {
                    return hash;
                }

                var bounds = room.LocalBounds;
                hash = hash * 31 + Quantize(bounds.xMin);
                hash = hash * 31 + Quantize(bounds.xMax);
                hash = hash * 31 + Quantize(bounds.yMin);
                hash = hash * 31 + Quantize(bounds.yMax);
                var walkableTiles = room.CurrentLayout?.WalkableTiles;
                hash = hash * 31 + (walkableTiles?.Count ?? 0);
                if (walkableTiles != null)
                {
                    for (var index = 0; index < walkableTiles.Count; index++)
                    {
                        hash = hash * 31 + walkableTiles[index].x;
                        hash = hash * 31 + walkableTiles[index].y;
                    }
                }

                var obstacles = room.Obstacles;
                hash = hash * 31 + obstacles.Count;
                for (var index = 0; index < obstacles.Count; index++)
                {
                    var obstacle = obstacles[index];
                    hash = hash * 31 + Quantize(obstacle.Center.x);
                    hash = hash * 31 + Quantize(obstacle.Center.z);
                    hash = hash * 31 + Quantize(obstacle.Size.x);
                    hash = hash * 31 + Quantize(obstacle.Size.z);
                }

                var interactiveObjects = room.InteractiveObjectMarkers;
                hash = hash * 31 + interactiveObjects.Count;
                for (var index = 0; index < interactiveObjects.Count; index++)
                {
                    var marker = interactiveObjects[index];
                    if (marker == null || !marker.BlocksMovement)
                    {
                        hash = hash * 31 + 3;
                        continue;
                    }

                    hash = hash * 31 + 7;
                    hash = hash * 31 + Quantize(marker.transform.localPosition.x);
                    hash = hash * 31 + Quantize(marker.transform.localPosition.z);
                    hash = hash * 31 + Quantize(marker.SizeMeters.x);
                    hash = hash * 31 + Quantize(marker.SizeMeters.z);
                }

                return hash;
            }
        }

        private static int Quantize(float value)
        {
            return Mathf.RoundToInt(value * 100f);
        }

        private static bool TryFindNearestValidNode(RoomPathfindingGraph graph, bool[] validNodes, Vector3 localPosition, out int nodeIndex)
        {
            nodeIndex = -1;
            graph.WorldToGrid(localPosition, out var centerX, out var centerZ);
            var maxRing = Mathf.Max(graph.Width, graph.Height);
            var bestDistance = float.MaxValue;
            for (var ring = 0; ring <= maxRing; ring++)
            {
                var foundInRing = false;
                for (var dz = -ring; dz <= ring; dz++)
                {
                    for (var dx = -ring; dx <= ring; dx++)
                    {
                        if (Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dz)) != ring)
                        {
                            continue;
                        }

                        var x = centerX + dx;
                        var z = centerZ + dz;
                        if (!graph.IsInside(x, z))
                        {
                            continue;
                        }

                        var index = graph.IndexFor(x, z);
                        if (!validNodes[index])
                        {
                            continue;
                        }

                        var candidate = graph.PositionFor(index);
                        var distance = (Flat(candidate) - Flat(localPosition)).sqrMagnitude;
                        if (distance >= bestDistance)
                        {
                            continue;
                        }

                        bestDistance = distance;
                        nodeIndex = index;
                        foundInRing = true;
                    }
                }

                if (foundInRing)
                {
                    return true;
                }
            }

            return false;
        }

        private static List<int> FindPath(
            RoomPathfindingGraph graph,
            bool[] validNodes,
            int startIndex,
            int goalIndex,
            Vector3 finalGoal,
            out bool fullPathFound)
        {
            var started = System.Diagnostics.Stopwatch.GetTimestamp();
            var path = FindPathCore(graph, validNodes, startIndex, goalIndex, finalGoal, out fullPathFound);
            EnemyNavigationDebugOverlay.RecordFreshPathSolve(ElapsedMilliseconds(started));
            return path;
        }

        private static List<int> FindPathCore(
            RoomPathfindingGraph graph,
            bool[] validNodes,
            int startIndex,
            int goalIndex,
            Vector3 finalGoal,
            out bool fullPathFound)
        {
            fullPathFound = false;
            var nodeCount = graph.NodeCount;
            var cameFrom = new int[nodeCount];
            var closed = new bool[nodeCount];
            var gScore = new float[nodeCount];
            for (var index = 0; index < nodeCount; index++)
            {
                cameFrom[index] = -1;
                gScore[index] = float.PositiveInfinity;
            }

            var open = new List<int> { startIndex };
            gScore[startIndex] = 0f;
            var bestIndex = startIndex;
            var bestHeuristic = Heuristic(graph.PositionFor(startIndex), finalGoal);
            var expanded = 0;

            while (open.Count > 0 && expanded < Mathf.Min(SearchBudgetNodes, nodeCount))
            {
                var current = PopBestOpenNode(open, graph, gScore, goalIndex);
                if (current == goalIndex)
                {
                    fullPathFound = true;
                    return ReconstructPath(cameFrom, current);
                }

                closed[current] = true;
                expanded++;
                var currentPosition = graph.PositionFor(current);
                var currentHeuristic = Heuristic(currentPosition, finalGoal);
                if (currentHeuristic < bestHeuristic)
                {
                    bestHeuristic = currentHeuristic;
                    bestIndex = current;
                }

                graph.IndexToGrid(current, out var currentX, out var currentZ);
                for (var neighborOffsetIndex = 0; neighborOffsetIndex < NeighborOffsets.Length; neighborOffsetIndex++)
                {
                    var offset = NeighborOffsets[neighborOffsetIndex];
                    var neighborX = currentX + offset.x;
                    var neighborZ = currentZ + offset.y;
                    if (!graph.IsInside(neighborX, neighborZ))
                    {
                        continue;
                    }

                    var neighborIndex = graph.IndexFor(neighborX, neighborZ);
                    if (!validNodes[neighborIndex] ||
                        closed[neighborIndex] ||
                        IsCornerCut(graph, validNodes, currentX, currentZ, offset))
                    {
                        continue;
                    }

                    var moveCost = offset.x != 0 && offset.y != 0 ? DiagonalCost : OrthogonalCost;
                    var tentativeG = gScore[current] + moveCost;
                    if (tentativeG >= gScore[neighborIndex])
                    {
                        continue;
                    }

                    cameFrom[neighborIndex] = current;
                    gScore[neighborIndex] = tentativeG;
                    if (!open.Contains(neighborIndex))
                    {
                        open.Add(neighborIndex);
                    }
                }
            }

            return bestIndex != startIndex ? ReconstructPath(cameFrom, bestIndex) : new List<int>();
        }

        private static bool TryFindActionEnvelopePath(
            RoomPathfindingGraph graph,
            bool[] validNodes,
            RoomRuntimeRoot room,
            int startIndex,
            Vector3 currentLocalPosition,
            Vector3 anchorLocalPosition,
            float desiredDistanceMeters,
            float minDistanceMeters,
            float maxDistanceMeters,
            float radiusMeters,
            EnemyIntelligenceLevel intelligence,
            int pathSeed,
            out List<int> path,
            out Vector3 goalLocalPosition)
        {
            path = new List<int>();
            goalLocalPosition = anchorLocalPosition;
            minDistanceMeters = Mathf.Max(0.15f, minDistanceMeters);
            maxDistanceMeters = Mathf.Max(minDistanceMeters + 0.1f, maxDistanceMeters);
            desiredDistanceMeters = Mathf.Clamp(desiredDistanceMeters, minDistanceMeters, maxDistanceMeters);
            var anchorToCurrent = currentLocalPosition - anchorLocalPosition;
            anchorToCurrent.y = 0f;
            var preferredDirection = anchorToCurrent.sqrMagnitude > 0.01f ? anchorToCurrent.normalized : Vector3.forward;
            var candidates = BuildActionEnvelopeCandidateNodes(
                graph,
                validNodes,
                anchorLocalPosition,
                desiredDistanceMeters,
                minDistanceMeters,
                maxDistanceMeters,
                intelligence,
                pathSeed,
                preferredDirection);
            if (candidates.Count <= 0)
            {
                return false;
            }

            var started = System.Diagnostics.Stopwatch.GetTimestamp();
            var found = FindPathToBestEnvelopeCandidate(
                graph,
                validNodes,
                room,
                startIndex,
                currentLocalPosition,
                anchorLocalPosition,
                desiredDistanceMeters,
                minDistanceMeters,
                maxDistanceMeters,
                radiusMeters,
                preferredDirection,
                candidates,
                out path,
                out goalLocalPosition);
            EnemyNavigationDebugOverlay.RecordFreshPathSolve(ElapsedMilliseconds(started));
            return found;
        }

        private static List<int> BuildActionEnvelopeCandidateNodes(
            RoomPathfindingGraph graph,
            bool[] validNodes,
            Vector3 anchorLocalPosition,
            float desiredDistanceMeters,
            float minDistanceMeters,
            float maxDistanceMeters,
            EnemyIntelligenceLevel intelligence,
            int pathSeed,
            Vector3 preferredDirection)
        {
            var candidates = new List<int>();
            var seen = new bool[graph.NodeCount];
            var sampleCount = GoalSampleCountFor(intelligence);
            var distances = ResolveGoalSampleDistances(desiredDistanceMeters, minDistanceMeters, maxDistanceMeters);
            for (var distanceIndex = 0; distanceIndex < distances.Count; distanceIndex++)
            {
                var distance = distances[distanceIndex];
                for (var sampleIndex = 0; sampleIndex < sampleCount; sampleIndex++)
                {
                    var angle = ResolveSampleAngle(sampleIndex, sampleCount, pathSeed);
                    var direction = Quaternion.Euler(0f, angle, 0f) * preferredDirection;
                    var candidate = anchorLocalPosition + direction.normalized * distance;
                    if (!TryFindNearestValidNode(graph, validNodes, candidate, out var goalIndex) || seen[goalIndex])
                    {
                        continue;
                    }

                    var snappedGoal = graph.PositionFor(goalIndex);
                    var snappedDistance = FlatDistance(anchorLocalPosition, snappedGoal);
                    if (snappedDistance < minDistanceMeters - 0.35f || snappedDistance > maxDistanceMeters + 0.55f)
                    {
                        continue;
                    }

                    seen[goalIndex] = true;
                    candidates.Add(goalIndex);
                }
            }

            return candidates;
        }

        private static bool FindPathToBestEnvelopeCandidate(
            RoomPathfindingGraph graph,
            bool[] validNodes,
            RoomRuntimeRoot room,
            int startIndex,
            Vector3 currentLocalPosition,
            Vector3 anchorLocalPosition,
            float desiredDistanceMeters,
            float minDistanceMeters,
            float maxDistanceMeters,
            float radiusMeters,
            Vector3 preferredDirection,
            IReadOnlyList<int> candidateNodes,
            out List<int> path,
            out Vector3 goalLocalPosition)
        {
            path = new List<int>();
            goalLocalPosition = anchorLocalPosition;
            var nodeCount = graph.NodeCount;
            var cameFrom = new int[nodeCount];
            var closed = new bool[nodeCount];
            var gScore = new float[nodeCount];
            var candidateLookup = new bool[nodeCount];
            for (var index = 0; index < nodeCount; index++)
            {
                cameFrom[index] = -1;
                gScore[index] = float.PositiveInfinity;
            }

            for (var index = 0; index < candidateNodes.Count; index++)
            {
                candidateLookup[candidateNodes[index]] = true;
            }

            var open = new List<int> { startIndex };
            gScore[startIndex] = 0f;
            var remainingCandidates = candidateNodes.Count;
            var bestIndex = -1;
            var bestScore = float.PositiveInfinity;
            var expanded = 0;
            while (open.Count > 0 && expanded < Mathf.Min(SearchBudgetNodes, nodeCount))
            {
                var current = PopBestOpenNodeTowardPosition(open, graph, gScore, anchorLocalPosition);
                closed[current] = true;
                expanded++;
                if (candidateLookup[current])
                {
                    remainingCandidates = Mathf.Max(0, remainingCandidates - 1);
                    var snappedGoal = graph.PositionFor(current);
                    var snappedDistance = FlatDistance(anchorLocalPosition, snappedGoal);
                    if (snappedDistance >= minDistanceMeters - 0.35f && snappedDistance <= maxDistanceMeters + 0.55f)
                    {
                        var candidateDelta = Flat(snappedGoal) - Flat(anchorLocalPosition);
                        var directionBias = candidateDelta.sqrMagnitude > 0.0001f
                            ? Vector3.Dot(candidateDelta.normalized, preferredDirection)
                            : 0f;
                        var clearance = ClearanceScore(room, snappedGoal, radiusMeters);
                        var directSightBonus = HasClearSegment(room, currentLocalPosition, snappedGoal, radiusMeters) ? 0.35f : 0f;
                        var score = gScore[current] * 0.28f +
                                    Mathf.Abs(snappedDistance - desiredDistanceMeters) * 1.15f -
                                    directionBias * 0.32f -
                                    clearance * 0.08f -
                                    directSightBonus;
                        if (score < bestScore)
                        {
                            bestScore = score;
                            bestIndex = current;
                            goalLocalPosition = snappedGoal;
                        }
                    }

                    if (remainingCandidates <= 0)
                    {
                        break;
                    }
                }

                graph.IndexToGrid(current, out var currentX, out var currentZ);
                for (var neighborOffsetIndex = 0; neighborOffsetIndex < NeighborOffsets.Length; neighborOffsetIndex++)
                {
                    var offset = NeighborOffsets[neighborOffsetIndex];
                    var neighborX = currentX + offset.x;
                    var neighborZ = currentZ + offset.y;
                    if (!graph.IsInside(neighborX, neighborZ))
                    {
                        continue;
                    }

                    var neighborIndex = graph.IndexFor(neighborX, neighborZ);
                    if (!validNodes[neighborIndex] ||
                        closed[neighborIndex] ||
                        IsCornerCut(graph, validNodes, currentX, currentZ, offset))
                    {
                        continue;
                    }

                    var moveCost = offset.x != 0 && offset.y != 0 ? DiagonalCost : OrthogonalCost;
                    var tentativeG = gScore[current] + moveCost;
                    if (tentativeG >= gScore[neighborIndex])
                    {
                        continue;
                    }

                    cameFrom[neighborIndex] = current;
                    gScore[neighborIndex] = tentativeG;
                    if (!open.Contains(neighborIndex))
                    {
                        open.Add(neighborIndex);
                    }
                }
            }

            if (bestIndex < 0)
            {
                return false;
            }

            path = ReconstructPath(cameFrom, bestIndex);
            return path.Count > 0;
        }

        private static int GoalSampleCountFor(EnemyIntelligenceLevel intelligence)
        {
            return intelligence switch
            {
                EnemyIntelligenceLevel.Cunning => 18,
                EnemyIntelligenceLevel.Tactical => 16,
                EnemyIntelligenceLevel.Trained => 14,
                EnemyIntelligenceLevel.Basic => 12,
                EnemyIntelligenceLevel.Simple => 10,
                _ => 8
            };
        }

        private static List<float> ResolveGoalSampleDistances(float desired, float min, float max)
        {
            var distances = new List<float>();
            AddUniqueDistance(distances, desired);
            AddUniqueDistance(distances, Mathf.Clamp(desired - 0.45f, min, max));
            AddUniqueDistance(distances, Mathf.Clamp(desired + 0.45f, min, max));
            AddUniqueDistance(distances, min);
            AddUniqueDistance(distances, max);
            return distances;
        }

        private static void AddUniqueDistance(List<float> distances, float value)
        {
            for (var index = 0; index < distances.Count; index++)
            {
                if (Mathf.Abs(distances[index] - value) < 0.05f)
                {
                    return;
                }
            }

            distances.Add(value);
        }

        private static float ResolveSampleAngle(int sampleIndex, int sampleCount, int pathSeed)
        {
            if (sampleIndex == 0)
            {
                return 0f;
            }

            var pair = (sampleIndex + 1) / 2;
            var sign = sampleIndex % 2 == 0 ? -1f : 1f;
            var step = 360f / Mathf.Max(1, sampleCount);
            var jitter = Mathf.Abs(pathSeed % 5) * 3.5f;
            return sign * pair * step + jitter;
        }

        private static float ClearanceScore(RoomRuntimeRoot room, Vector3 localPosition, float radiusMeters)
        {
            var score = 0f;
            var clearanceRadius = radiusMeters + 0.18f;
            for (var index = 0; index < NeighborOffsets.Length; index++)
            {
                var offset = NeighborOffsets[index];
                var sample = localPosition + new Vector3(offset.x, 0f, offset.y).normalized * CellSizeMeters;
                if (CanOccupy(room, sample, clearanceRadius))
                {
                    score += 1f;
                }
            }

            return score;
        }

        private static int PopBestOpenNode(List<int> open, RoomPathfindingGraph graph, float[] gScore, int goalIndex)
        {
            var bestListIndex = 0;
            var bestScore = float.PositiveInfinity;
            var goal = graph.PositionFor(goalIndex);
            for (var index = 0; index < open.Count; index++)
            {
                var node = open[index];
                var score = gScore[node] + Heuristic(graph.PositionFor(node), goal);
                if (score >= bestScore)
                {
                    continue;
                }

                bestScore = score;
                bestListIndex = index;
            }

            var best = open[bestListIndex];
            open.RemoveAt(bestListIndex);
            return best;
        }

        private static int PopBestOpenNodeTowardPosition(List<int> open, RoomPathfindingGraph graph, float[] gScore, Vector3 goal)
        {
            var bestListIndex = 0;
            var bestScore = float.PositiveInfinity;
            for (var index = 0; index < open.Count; index++)
            {
                var node = open[index];
                var score = gScore[node] + Heuristic(graph.PositionFor(node), goal);
                if (score >= bestScore)
                {
                    continue;
                }

                bestScore = score;
                bestListIndex = index;
            }

            var best = open[bestListIndex];
            open.RemoveAt(bestListIndex);
            return best;
        }

        private static bool IsCornerCut(RoomPathfindingGraph graph, bool[] validNodes, int currentX, int currentZ, Vector2Int offset)
        {
            if (offset.x == 0 || offset.y == 0)
            {
                return false;
            }

            var horizontal = graph.IndexFor(currentX + offset.x, currentZ);
            var vertical = graph.IndexFor(currentX, currentZ + offset.y);
            return !validNodes[horizontal] || !validNodes[vertical];
        }

        private static List<int> ReconstructPath(int[] cameFrom, int current)
        {
            var path = new List<int> { current };
            while (cameFrom[current] >= 0)
            {
                current = cameFrom[current];
                path.Add(current);
            }

            path.Reverse();
            return path;
        }

        private static Vector3 ResolveLookaheadWaypoint(
            RoomPathfindingGraph graph,
            IReadOnlyList<int> path,
            Vector3 current,
            float radiusMeters,
            RoomRuntimeRoot room)
        {
            if (path.Count <= 1)
            {
                return graph.PositionFor(path[0]);
            }

            var next = graph.PositionFor(path[1]);
            for (var index = 2; index < path.Count; index++)
            {
                var candidate = graph.PositionFor(path[index]);
                if (!HasClearSegment(room, current, candidate, radiusMeters))
                {
                    break;
                }

                next = candidate;
            }

            return next;
        }

        private static bool HasClearSegment(RoomRuntimeRoot room, Vector3 from, Vector3 to, float radiusMeters)
        {
            var delta = to - from;
            delta.y = 0f;
            var distance = delta.magnitude;
            if (distance <= 0.001f)
            {
                return true;
            }

            var steps = Mathf.Max(1, Mathf.CeilToInt(distance / (CellSizeMeters * 0.5f)));
            for (var step = 1; step <= steps; step++)
            {
                var t = step / (float)steps;
                var sample = Vector3.Lerp(from, to, t);
                if (!CanOccupy(room, sample, radiusMeters))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool CanOccupy(RoomRuntimeRoot room, Vector3 localPosition, float radiusMeters)
        {
            return !RoomLocalCollision.IsOutsideWalkable(room, localPosition, radiusMeters) &&
                   !RoomLocalCollision.IntersectsObstacle(room, localPosition, radiusMeters) &&
                   !RoomLocalCollision.IsOutsideBounds(room, localPosition, radiusMeters);
        }

        private static EnemyNavigationResult BuildPathResult(
            in EnemyNavigationRequest request,
            Vector3 resolvedLocalPosition,
            Vector3 nextWaypoint,
            RoomPathfindingGraph graph,
            IReadOnlyList<int> path,
            EnemyPathStatus status,
            string fallbackReason,
            Vector3? finalGoalOverride = null)
        {
            var moved = resolvedLocalPosition - request.CurrentLocalPosition;
            moved.y = 0f;
            var steering = moved.sqrMagnitude > 0.0001f ? moved.normalized : Vector3.zero;
            var reached = FlatDistance(resolvedLocalPosition, request.DesiredLocalPosition) <= EnemyNavigationAdapter.DefaultReachedToleranceMeters;
            var blocked = !reached && moved.sqrMagnitude <= 0.0001f;
            var pathPositions = BuildPathPositions(graph, path);
            return new EnemyNavigationResult(
                EnemyNavigationBackend.RoomGridAStar,
                request.Mode,
                request.Intent,
                request.DesiredLocalPosition,
                resolvedLocalPosition,
                steering,
                reached,
                usedFallbackSteering: false,
                blocked,
                status,
                finalGoalOverride ?? request.FinalGoalLocalPosition,
                nextWaypoint,
                request.PathAgeSeconds,
                pathPositions.Length,
                fallbackReason,
                pathPositions);
        }

        private static Vector3[] BuildPathPositions(RoomPathfindingGraph graph, IReadOnlyList<int> path)
        {
            if (graph == null || path == null || path.Count == 0)
            {
                return System.Array.Empty<Vector3>();
            }

            var positions = new Vector3[path.Count];
            for (var index = 0; index < path.Count; index++)
            {
                positions[index] = graph.PositionFor(path[index]);
            }

            return positions;
        }

        private static EnemyNavigationResult WithFallbackPathState(
            EnemyNavigationResult fallback,
            EnemyPathStatus status,
            string reason,
            in EnemyNavigationRequest request)
        {
            if (status != EnemyPathStatus.NotRequested)
            {
                EnemyNavigationDebugOverlay.RecordFallback(reason);
            }

            return new EnemyNavigationResult(
                fallback.Backend,
                fallback.Mode,
                fallback.Intent,
                fallback.RequestedLocalPosition,
                fallback.ResolvedLocalPosition,
                fallback.SteeringDirection,
                fallback.ReachedRequestedPosition,
                fallback.UsedFallbackSteering,
                fallback.Blocked,
                status,
                request.FinalGoalLocalPosition,
                fallback.NextWaypointLocalPosition,
                request.PathAgeSeconds,
                fallback.PathWaypointCount,
                reason,
                fallback.PathWaypointsLocalPositions);
        }

        private static bool TryConsumeFreshPathBudget(EnemyIntelligenceLevel intelligence, out string fallbackReason)
        {
            var frame = Time.frameCount;
            if (budgetFrame != frame)
            {
                budgetFrame = frame;
                freshSolvesUsedThisFrame = 0;
            }

            var highIntelligence = intelligence is EnemyIntelligenceLevel.Tactical or EnemyIntelligenceLevel.Cunning;
            var frameLimit = MaxFreshPathSolvesPerFrame + (highIntelligence ? HighIntelligenceReservePathSolvesPerFrame : 0);
            EnemyNavigationDebugOverlay.RecordBudgetUsage(freshSolvesUsedThisFrame, frameLimit);
            if (freshSolvesUsedThisFrame < frameLimit)
            {
                freshSolvesUsedThisFrame++;
                EnemyNavigationDebugOverlay.RecordBudgetUsage(freshSolvesUsedThisFrame, frameLimit);
                fallbackReason = string.Empty;
                return true;
            }

            fallbackReason = "path_budget_deferred";
            EnemyNavigationDebugOverlay.RecordBudgetDeferred(fallbackReason);
            return false;
        }

        private static float ElapsedMilliseconds(long started)
        {
            var elapsed = System.Diagnostics.Stopwatch.GetTimestamp() - started;
            return elapsed * 1000f / System.Diagnostics.Stopwatch.Frequency;
        }

        private static float Heuristic(Vector3 left, Vector3 right)
        {
            return FlatDistance(left, right) / CellSizeMeters;
        }

        private static float FlatDistance(Vector3 left, Vector3 right)
        {
            return (Flat(left) - Flat(right)).magnitude;
        }

        private static Vector3 Flat(Vector3 value)
        {
            value.y = 0f;
            return value;
        }

        private sealed class RoomPathfindingGraph
        {
            private readonly Rect bounds;
            private readonly float cellSize;
            private readonly Dictionary<int, OccupancyCache> occupancyByRadiusBucket = new();

            public RoomPathfindingGraph(Rect nextBounds, float nextCellSize)
            {
                bounds = nextBounds;
                cellSize = Mathf.Max(0.1f, nextCellSize);
                Width = Mathf.Max(1, Mathf.FloorToInt(bounds.width / cellSize) + 1);
                Height = Mathf.Max(1, Mathf.FloorToInt(bounds.height / cellSize) + 1);
            }

            public int Width { get; }

            public int Height { get; }

            public int NodeCount => Width * Height;

            public bool Matches(Rect nextBounds)
            {
                return Mathf.Abs(bounds.xMin - nextBounds.xMin) < 0.001f &&
                       Mathf.Abs(bounds.xMax - nextBounds.xMax) < 0.001f &&
                       Mathf.Abs(bounds.yMin - nextBounds.yMin) < 0.001f &&
                       Mathf.Abs(bounds.yMax - nextBounds.yMax) < 0.001f;
            }

            public bool IsInside(int x, int z)
            {
                return x >= 0 && x < Width && z >= 0 && z < Height;
            }

            public int IndexFor(int x, int z)
            {
                return z * Width + x;
            }

            public void IndexToGrid(int index, out int x, out int z)
            {
                x = index % Width;
                z = index / Width;
            }

            public Vector3 PositionFor(int index)
            {
                IndexToGrid(index, out var x, out var z);
                return new Vector3(bounds.xMin + x * cellSize, 0f, bounds.yMin + z * cellSize);
            }

            public void WorldToGrid(Vector3 localPosition, out int x, out int z)
            {
                x = Mathf.Clamp(Mathf.RoundToInt((localPosition.x - bounds.xMin) / cellSize), 0, Width - 1);
                z = Mathf.Clamp(Mathf.RoundToInt((localPosition.z - bounds.yMin) / cellSize), 0, Height - 1);
            }

            public bool TryGetValidNodes(int radiusBucket, int occupancySignature, out bool[] validNodes)
            {
                validNodes = null;
                if (!occupancyByRadiusBucket.TryGetValue(radiusBucket, out var cached) ||
                    cached.Signature != occupancySignature ||
                    cached.ValidNodes == null ||
                    cached.ValidNodes.Length != NodeCount)
                {
                    return false;
                }

                validNodes = cached.ValidNodes;
                return true;
            }

            public void StoreValidNodes(int radiusBucket, int occupancySignature, bool[] validNodes)
            {
                occupancyByRadiusBucket[radiusBucket] = new OccupancyCache(occupancySignature, validNodes);
            }
        }

        private sealed class OccupancyCache
        {
            public OccupancyCache(int signature, bool[] validNodes)
            {
                Signature = signature;
                ValidNodes = validNodes;
            }

            public int Signature { get; }

            public bool[] ValidNodes { get; }
        }
    }
}
