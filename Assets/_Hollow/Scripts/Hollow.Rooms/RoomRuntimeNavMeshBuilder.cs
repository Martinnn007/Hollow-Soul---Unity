using UnityEngine.AI;

namespace Hollow.Rooms
{
    public static class RoomRuntimeNavMeshBuilder
    {
        public static NavMeshData BuildRoom(ImportedRoomRuntimeAsset room)
        {
            return RoomNavMeshBuildUtility.BuildRoom(room, "NavMesh.Runtime", out _);
        }
    }
}
