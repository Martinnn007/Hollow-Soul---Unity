using Hollow.Rooms;
using UnityEngine;

namespace Hollow.Combat
{
    public static class RoomLocalCollision
    {
        public const float MinimumRadiusMeters = CombatFeelTuning.MinimumCollisionRadiusMeters;

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

            return false;
        }

        public static bool IntersectsProjectileBlocker(RoomRuntimeRoot room, Vector3 localPosition, float radius)
        {
            if (IntersectsObstacle(room, localPosition, radius))
            {
                return true;
            }

            return false;
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
    }
}
