using UnityEngine;
using UnityEngine.AI;

namespace Hollow.Rooms
{
    public sealed class RoomInteractiveObjectMarker : MonoBehaviour
    {
        [SerializeField] private string objectId = string.Empty;
        [SerializeField] private string objectKind = RoomInteractiveObjectKind.StandardBarrel;
        [SerializeField] private Vector3 sizeMeters = Vector3.one;
        [SerializeField] private bool blocksMovement = true;
        [SerializeField] private bool blocksProjectiles = true;
        [SerializeField] private bool destroyed;

        public string ObjectId => objectId;

        public string ObjectKind => objectKind;

        public Vector3 SizeMeters => new(Mathf.Max(0.05f, sizeMeters.x), Mathf.Max(0.05f, sizeMeters.y), Mathf.Max(0.05f, sizeMeters.z));

        public bool BlocksMovement => blocksMovement && !destroyed;

        public bool BlocksProjectiles => blocksProjectiles && !destroyed;

        public bool IsDestroyed => destroyed;

        public void Configure(ImportedRoomInteractiveObject roomObject)
        {
            objectId = string.IsNullOrWhiteSpace(roomObject?.id) ? name : roomObject.id;
            objectKind = string.IsNullOrWhiteSpace(roomObject?.kind) ? RoomInteractiveObjectKind.StandardBarrel : roomObject.kind;
            sizeMeters = roomObject?.size?.ToUnityVector3() ?? Vector3.one;
            blocksMovement = roomObject == null || roomObject.blocksMovement;
            blocksProjectiles = roomObject == null || roomObject.blocksProjectiles;
            destroyed = false;
        }

        public void MarkDestroyed()
        {
            destroyed = true;
            if (TryGetComponent<RoomDynamicNavigationObjectMarker>(out var dynamicNavigation))
            {
                dynamicNavigation.MarkDestroyed();
            }

            var obstacle = GetComponent<NavMeshObstacle>();
            if (obstacle != null)
            {
                obstacle.enabled = false;
            }
        }
    }
}
