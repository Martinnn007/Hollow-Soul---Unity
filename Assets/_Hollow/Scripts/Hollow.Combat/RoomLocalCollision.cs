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
            if (!IntersectsObstacle(room, clamped, radius))
            {
                return clamped;
            }

            var xOnly = ClampToBounds(room, new Vector3(clamped.x, currentLocal.y, currentLocal.z), radius);
            if (!IntersectsObstacle(room, xOnly, radius))
            {
                return xOnly;
            }

            var zOnly = ClampToBounds(room, new Vector3(currentLocal.x, currentLocal.y, clamped.z), radius);
            return IntersectsObstacle(room, zOnly, radius) ? currentLocal : zOnly;
        }

        public static Vector3 ResolveMoveIgnoringObstacles(RoomRuntimeRoot room, Vector3 desiredLocal, float radius)
        {
            return room == null ? desiredLocal : ClampToBounds(room, desiredLocal, radius);
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
                   localPosition.z > bounds.yMax - radius;
        }

        private static Vector3 ClampToBounds(RoomRuntimeRoot room, Vector3 localPosition, float radius)
        {
            var bounds = room.LocalBounds;
            localPosition.x = Mathf.Clamp(localPosition.x, bounds.xMin + radius, bounds.xMax - radius);
            localPosition.z = Mathf.Clamp(localPosition.z, bounds.yMin + radius, bounds.yMax - radius);
            return localPosition;
        }
    }
}
