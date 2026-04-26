using UnityEngine;

namespace Hollow.Rewards
{
    public sealed class RoomRewardPickup : MonoBehaviour
    {
        [SerializeField] private string roomId;
        [SerializeField] private bool claimed;

        public string RoomId => roomId;

        public bool Claimed => claimed;

        public void Configure(string nextRoomId)
        {
            roomId = nextRoomId;
            claimed = false;
        }

        public bool Claim()
        {
            if (claimed)
            {
                return false;
            }

            claimed = true;
            return true;
        }
    }
}
