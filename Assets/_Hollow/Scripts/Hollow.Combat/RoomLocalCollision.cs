using Hollow.Rooms;
using UnityEngine;

namespace Hollow.Combat
{
    public static class RoomLocalCollision
    {
        public const float MinimumRadiusMeters = CombatFeelTuning.MinimumCollisionRadiusMeters;
        private static readonly Vector3[] NearestOccupiableSearchDirections =
        {
            Vector3.forward,
            Vector3.back,
            Vector3.left,
            Vector3.right,
            (Vector3.forward + Vector3.left).normalized,
            (Vector3.forward + Vector3.right).normalized,
            (Vector3.back + Vector3.left).normalized,
            (Vector3.back + Vector3.right).normalized,
            new Vector3(0.38f, 0f, 0.92f).normalized,
            new Vector3(-0.38f, 0f, 0.92f).normalized,
            new Vector3(0.38f, 0f, -0.92f).normalized,
            new Vector3(-0.38f, 0f, -0.92f).normalized,
            new Vector3(0.92f, 0f, 0.38f).normalized,
            new Vector3(-0.92f, 0f, 0.38f).normalized,
            new Vector3(0.92f, 0f, -0.38f).normalized,
            new Vector3(-0.92f, 0f, -0.38f).normalized
        };

        public static Vector3 ResolveMove(RoomRuntimeRoot room, Vector3 currentLocal, Vector3 desiredLocal, float radius)
        {
            if (room == null)
            {
                return desiredLocal;
            }

            radius = Mathf.Max(MinimumRadiusMeters, radius);
            var clamped = ClampToBounds(room, desiredLocal, radius);
            if (CanOccupy(room, clamped, radius))
            {
                return clamped;
            }

            var xOnly = ClampToBounds(room, new Vector3(clamped.x, currentLocal.y, currentLocal.z), radius);
            if (CanOccupy(room, xOnly, radius))
            {
                return xOnly;
            }

            var zOnly = ClampToBounds(room, new Vector3(currentLocal.x, currentLocal.y, clamped.z), radius);
            var canUseZOnly = CanOccupy(room, zOnly, radius);
            if (!canUseZOnly)
            {
                return currentLocal;
            }

            if (!CanOccupy(room, xOnly, radius))
            {
                return zOnly;
            }

            return ProgressTowardDesired(xOnly, currentLocal, clamped) >= ProgressTowardDesired(zOnly, currentLocal, clamped)
                ? xOnly
                : zOnly;
        }

        public static Vector3 ResolveMoveIgnoringObstacles(RoomRuntimeRoot room, Vector3 desiredLocal, float radius)
        {
            if (room == null)
            {
                return desiredLocal;
            }

            radius = Mathf.Max(MinimumRadiusMeters, radius);
            var clamped = ClampToBounds(room, desiredLocal, radius);
            return IsOutsideWalkable(room, clamped, radius)
                ? NearestWalkablePosition(room, clamped)
                : clamped;
        }

        public static Vector3 ResolveNearestOccupiablePosition(
            RoomRuntimeRoot room,
            Vector3 preferredLocal,
            float radius,
            Vector3 preferredDirection,
            float maxSearchRadius = 3f)
        {
            if (room == null)
            {
                return preferredLocal;
            }

            radius = Mathf.Max(MinimumRadiusMeters, radius);
            maxSearchRadius = Mathf.Max(0.25f, maxSearchRadius);
            var clampedPreferred = ClampToBounds(room, preferredLocal, radius);
            if (CanOccupy(room, clampedPreferred, radius))
            {
                return clampedPreferred;
            }

            preferredDirection.y = 0f;
            var hasPreferredDirection = preferredDirection.sqrMagnitude > 0.001f;
            if (hasPreferredDirection)
            {
                preferredDirection.Normalize();
                for (var step = 1; step <= 12; step++)
                {
                    var distance = Mathf.Min(maxSearchRadius, step * 0.25f);
                    var directedCandidate = ClampToBounds(room, clampedPreferred + preferredDirection * distance, radius);
                    if (CanOccupy(room, directedCandidate, radius))
                    {
                        return directedCandidate;
                    }
                }
            }

            var best = clampedPreferred;
            var bestScore = float.PositiveInfinity;
            var directions = NearestOccupiableSearchDirections;

            for (var ring = 1; ring <= 14; ring++)
            {
                var distance = Mathf.Min(maxSearchRadius, ring * 0.25f);
                for (var index = 0; index < directions.Length; index++)
                {
                    var candidate = ClampToBounds(room, clampedPreferred + directions[index] * distance, radius);
                    if (!CanOccupy(room, candidate, radius))
                    {
                        continue;
                    }

                    var score = Vector3.Distance(Flat(candidate), Flat(preferredLocal));
                    if (hasPreferredDirection)
                    {
                        var offset = candidate - clampedPreferred;
                        offset.y = 0f;
                        if (offset.sqrMagnitude > 0.001f)
                        {
                            score -= Mathf.Max(0f, Vector3.Dot(offset.normalized, preferredDirection)) * 0.35f;
                        }
                    }

                    if (score >= bestScore)
                    {
                        continue;
                    }

                    bestScore = score;
                    best = candidate;
                }

                if (bestScore < float.PositiveInfinity)
                {
                    return best;
                }
            }

            var nearestWalkable = ClampToBounds(room, NearestWalkablePosition(room, clampedPreferred), radius);
            return CanOccupy(room, nearestWalkable, radius) ? nearestWalkable : clampedPreferred;
        }

        public static bool IntersectsObstacle(RoomRuntimeRoot room, Vector3 localPosition, float radius)
        {
            if (room?.Obstacles == null)
            {
                return false;
            }

            radius = Mathf.Max(MinimumRadiusMeters, radius);
            foreach (var obstacle in room.Obstacles)
            {
                var halfX = obstacle.Size.x * 0.5f + radius;
                var halfZ = obstacle.Size.z * 0.5f + radius;
                if (Mathf.Abs(localPosition.x - obstacle.Center.x) <= halfX &&
                    Mathf.Abs(localPosition.z - obstacle.Center.z) <= halfZ)
                {
                    return true;
                }
            }

            foreach (var roomObject in room.InteractiveObjectMarkers)
            {
                if (roomObject == null || !roomObject.BlocksMovement)
                {
                    continue;
                }

                var halfX = roomObject.SizeMeters.x * 0.5f + radius;
                var halfZ = roomObject.SizeMeters.z * 0.5f + radius;
                if (Mathf.Abs(localPosition.x - roomObject.transform.localPosition.x) <= halfX &&
                    Mathf.Abs(localPosition.z - roomObject.transform.localPosition.z) <= halfZ)
                {
                    return true;
                }
            }

            foreach (var dynamicObject in room.DynamicNavigationObjects)
            {
                if (dynamicObject == null || !dynamicObject.CarvingActive)
                {
                    continue;
                }

                var halfX = dynamicObject.SizeMeters.x * 0.5f + radius;
                var halfZ = dynamicObject.SizeMeters.z * 0.5f + radius;
                if (Mathf.Abs(localPosition.x - dynamicObject.transform.localPosition.x) <= halfX &&
                    Mathf.Abs(localPosition.z - dynamicObject.transform.localPosition.z) <= halfZ)
                {
                    return true;
                }
            }

            return false;
        }

        public static bool IntersectsProjectileBlocker(RoomRuntimeRoot room, Vector3 localPosition, float radius)
        {
            if (room?.Obstacles == null)
            {
                return false;
            }

            radius = Mathf.Max(0f, radius);
            foreach (var obstacle in room.Obstacles)
            {
                if (!obstacle.BlocksProjectiles)
                {
                    continue;
                }

                var halfX = obstacle.Size.x * 0.5f + radius;
                var halfZ = obstacle.Size.z * 0.5f + radius;
                if (Mathf.Abs(localPosition.x - obstacle.Center.x) <= halfX &&
                    Mathf.Abs(localPosition.z - obstacle.Center.z) <= halfZ)
                {
                    return true;
                }
            }

            foreach (var roomObject in room.InteractiveObjectMarkers)
            {
                if (roomObject == null || !roomObject.BlocksProjectiles)
                {
                    continue;
                }

                var halfX = roomObject.SizeMeters.x * 0.5f + radius;
                var halfZ = roomObject.SizeMeters.z * 0.5f + radius;
                if (Mathf.Abs(localPosition.x - roomObject.transform.localPosition.x) <= halfX &&
                    Mathf.Abs(localPosition.z - roomObject.transform.localPosition.z) <= halfZ)
                {
                    return true;
                }
            }

            foreach (var dynamicObject in room.DynamicNavigationObjects)
            {
                if (dynamicObject == null || !dynamicObject.CarvingActive)
                {
                    continue;
                }

                var halfX = dynamicObject.SizeMeters.x * 0.5f + radius;
                var halfZ = dynamicObject.SizeMeters.z * 0.5f + radius;
                if (Mathf.Abs(localPosition.x - dynamicObject.transform.localPosition.x) <= halfX &&
                    Mathf.Abs(localPosition.z - dynamicObject.transform.localPosition.z) <= halfZ)
                {
                    return true;
                }
            }

            return false;
        }

        public static bool HasClearMovementLane(RoomRuntimeRoot room, Vector3 fromLocalPosition, Vector3 toLocalPosition, float radius, out string blockedReason)
        {
            return HasClearSampledLane(
                room,
                fromLocalPosition,
                toLocalPosition,
                Mathf.Max(MinimumRadiusMeters, radius),
                sampleOutsideBounds: true,
                sampleProjectileBlockers: false,
                blockedReason: out blockedReason);
        }

        public static bool HasClearProjectileLine(RoomRuntimeRoot room, Vector3 fromLocalPosition, Vector3 toLocalPosition, float radius, out string blockedReason)
        {
            return HasClearSampledLane(
                room,
                fromLocalPosition,
                toLocalPosition,
                Mathf.Max(0f, radius),
                sampleOutsideBounds: false,
                sampleProjectileBlockers: true,
                blockedReason: out blockedReason);
        }

        private static bool HasClearSampledLane(
            RoomRuntimeRoot room,
            Vector3 fromLocalPosition,
            Vector3 toLocalPosition,
            float radius,
            bool sampleOutsideBounds,
            bool sampleProjectileBlockers,
            out string blockedReason)
        {
            blockedReason = string.Empty;
            if (room == null)
            {
                return true;
            }

            fromLocalPosition.y = 0f;
            toLocalPosition.y = 0f;
            var delta = toLocalPosition - fromLocalPosition;
            var distance = delta.magnitude;
            if (distance <= 0.001f)
            {
                return true;
            }

            var stepCount = Mathf.Max(1, Mathf.CeilToInt(distance / 0.2f));
            for (var index = 1; index < stepCount; index++)
            {
                var sample = Vector3.Lerp(fromLocalPosition, toLocalPosition, index / (float)stepCount);
                if (sampleOutsideBounds && IsOutsideBounds(room, sample, radius))
                {
                    blockedReason = "blocked_by_bounds_or_hole";
                    return false;
                }

                if (sampleProjectileBlockers)
                {
                    if (IntersectsProjectileBlocker(room, sample, radius))
                    {
                        blockedReason = "blocked_by_projectile_cover";
                        return false;
                    }
                }
                else if (IntersectsObstacle(room, sample, radius))
                {
                    blockedReason = "blocked_by_obstacle";
                    return false;
                }
            }

            return true;
        }

        public static bool IsOutsideBounds(RoomRuntimeRoot room, Vector3 localPosition, float radius)
        {
            if (room == null)
            {
                return false;
            }

            radius = Mathf.Max(MinimumRadiusMeters, radius);
            var bounds = room.LocalBounds;
            return localPosition.x < bounds.xMin + radius ||
                   localPosition.x > bounds.xMax - radius ||
                   localPosition.z < bounds.yMin + radius ||
                   localPosition.z > bounds.yMax - radius ||
                   IsOutsideWalkable(room, localPosition, radius);
        }

        public static bool IsOutsideWalkable(RoomRuntimeRoot room, Vector3 localPosition, float radius)
        {
            var walkableTiles = room?.CurrentLayout?.WalkableTiles;
            if (walkableTiles == null || walkableTiles.Count == 0)
            {
                return false;
            }

            var clampedRadius = Mathf.Max(0f, radius);
            return !IsPointOnWalkableTile(walkableTiles, localPosition) ||
                   !IsPointOnWalkableTile(walkableTiles, localPosition + new Vector3(clampedRadius, 0f, 0f)) ||
                   !IsPointOnWalkableTile(walkableTiles, localPosition + new Vector3(-clampedRadius, 0f, 0f)) ||
                   !IsPointOnWalkableTile(walkableTiles, localPosition + new Vector3(0f, 0f, clampedRadius)) ||
                   !IsPointOnWalkableTile(walkableTiles, localPosition + new Vector3(0f, 0f, -clampedRadius));
        }

        public static Vector3 ResolveFlyingMove(RoomRuntimeRoot room, Vector3 desiredLocal, float radius)
        {
            if (room == null)
            {
                return desiredLocal;
            }

            radius = Mathf.Max(MinimumRadiusMeters, radius);
            var clamped = ClampToBounds(room, desiredLocal, radius);
            return IsOutsideFloorRegions(room, clamped, radius)
                ? NearestFloorRegionPosition(room, clamped)
                : clamped;
        }

        public static bool IsOutsideFloorRegions(RoomRuntimeRoot room, Vector3 localPosition, float radius)
        {
            var regions = room?.CurrentLayout?.FloorRegions;
            if (regions == null || regions.Count == 0)
            {
                return false;
            }

            var clampedRadius = Mathf.Max(0f, radius);
            return !IsPointInAnyFloorRegion(regions, localPosition, clampedRadius);
        }

        private static Vector3 ClampToBounds(RoomRuntimeRoot room, Vector3 localPosition, float radius)
        {
            var bounds = room.LocalBounds;
            localPosition.x = Mathf.Clamp(localPosition.x, bounds.xMin + radius, bounds.xMax - radius);
            localPosition.z = Mathf.Clamp(localPosition.z, bounds.yMin + radius, bounds.yMax - radius);
            return localPosition;
        }

        public static bool CanOccupy(RoomRuntimeRoot room, Vector3 localPosition, float radius)
        {
            return !IsOutsideWalkable(room, localPosition, radius) && !IntersectsObstacle(room, localPosition, radius);
        }

        private static bool IsPointOnWalkableTile(System.Collections.Generic.IReadOnlyList<Vector2Int> walkableTiles, Vector3 localPosition)
        {
            foreach (var tile in walkableTiles)
            {
                if (Mathf.Abs(localPosition.x - tile.x) <= 0.5f &&
                    Mathf.Abs(localPosition.z - tile.y) <= 0.5f)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsPointInAnyFloorRegion(System.Collections.Generic.IReadOnlyList<RoomLayoutFloorRegion> regions, Vector3 localPosition, float radius)
        {
            foreach (var region in regions)
            {
                if (Mathf.Abs(localPosition.x - region.Center.x) <= region.HalfSize.x - radius &&
                    Mathf.Abs(localPosition.z - region.Center.z) <= region.HalfSize.y - radius)
                {
                    return true;
                }
            }

            return false;
        }

        private static Vector3 NearestFloorRegionPosition(RoomRuntimeRoot room, Vector3 localPosition)
        {
            var regions = room?.CurrentLayout?.FloorRegions;
            if (regions == null || regions.Count == 0)
            {
                return localPosition;
            }

            var nearest = localPosition;
            var nearestDistance = float.MaxValue;
            foreach (var region in regions)
            {
                var x = Mathf.Clamp(localPosition.x, region.Center.x - region.HalfSize.x, region.Center.x + region.HalfSize.x);
                var z = Mathf.Clamp(localPosition.z, region.Center.z - region.HalfSize.y, region.Center.z + region.HalfSize.y);
                var candidate = new Vector3(x, localPosition.y, z);
                var distance = (candidate - localPosition).sqrMagnitude;
                if (distance < nearestDistance)
                {
                    nearest = candidate;
                    nearestDistance = distance;
                }
            }

            return nearest;
        }

        private static Vector3 NearestWalkablePosition(RoomRuntimeRoot room, Vector3 localPosition)
        {
            var walkableTiles = room?.CurrentLayout?.WalkableTiles;
            if (walkableTiles == null || walkableTiles.Count == 0)
            {
                return localPosition;
            }

            var nearest = walkableTiles[0];
            var nearestDistance = float.MaxValue;
            foreach (var tile in walkableTiles)
            {
                var dx = localPosition.x - tile.x;
                var dz = localPosition.z - tile.y;
                var distance = dx * dx + dz * dz;
                if (distance < nearestDistance)
                {
                    nearest = tile;
                    nearestDistance = distance;
                }
            }

            return new Vector3(nearest.x, localPosition.y, nearest.y);
        }

        private static float ProgressTowardDesired(Vector3 candidate, Vector3 current, Vector3 desired)
        {
            var desiredDelta = desired - current;
            desiredDelta.y = 0f;
            if (desiredDelta.sqrMagnitude < 0.0001f)
            {
                return 0f;
            }

            var candidateDelta = candidate - current;
            candidateDelta.y = 0f;
            return Vector3.Dot(candidateDelta, desiredDelta.normalized);
        }

        private static Vector3 Flat(Vector3 value)
        {
            value.y = 0f;
            return value;
        }
    }
}
