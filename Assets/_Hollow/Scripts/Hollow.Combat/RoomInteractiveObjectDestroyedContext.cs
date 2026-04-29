using Hollow.Rooms;
using UnityEngine;

namespace Hollow.Combat
{
    public readonly struct RoomInteractiveObjectDestroyedContext
    {
        public RoomInteractiveObjectDestroyedContext(string objectId, string objectKind, Vector3 localPosition, int coinDropAmount)
        {
            ObjectId = objectId ?? string.Empty;
            ObjectKind = objectKind ?? RoomInteractiveObjectKind.StandardBarrel;
            LocalPosition = localPosition;
            CoinDropAmount = Mathf.Max(0, coinDropAmount);
        }

        public string ObjectId { get; }

        public string ObjectKind { get; }

        public Vector3 LocalPosition { get; }

        public int CoinDropAmount { get; }
    }
}
