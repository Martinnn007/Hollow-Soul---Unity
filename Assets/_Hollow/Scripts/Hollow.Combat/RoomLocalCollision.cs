using Hollow.Rooms;
using UnityEngine;

namespace Hollow.Combat
{
    public static class RoomLocalCollision
    {
        public static Vector3 ResolveMove(RoomRuntimeRoot room, Vector3 currentLocal, Vector3 desiredLocal, float radius)
        {
            if (room == null)
            {
                return desiredLocal;
            }

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
            return CanOccupy(room, zOnly, radius) ? zOnly : currentLocal;
        }

        public static Vector3 ResolveMoveIgnoringObstacles(RoomRuntimeRoot room, Vector3 desiredLocal, float radius)
        {
            if (room == null)
            {
                return desiredLocal;
            }

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

            return false;
        }

        public static bool IsOutsideBounds(RoomRuntimeRoot room, Vector3 localPosition, float radius)
        {
            if (room == null)
            {
                return false;
            }

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

        private static Vector3 ClampToBounds(RoomRuntimeRoot room, Vector3 localPosition, float radius)
        {
            var bounds = room.LocalBounds;
            localPosition.x = Mathf.Clamp(localPosition.x, bounds.xMin + radius, bounds.xMax - radius);
            localPosition.z = Mathf.Clamp(localPosition.z, bounds.yMin + radius, bounds.yMax - radius);
            return localPosition;
        }

        private static bool CanOccupy(RoomRuntimeRoot room, Vector3 localPosition, float radius)
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
    }
}
