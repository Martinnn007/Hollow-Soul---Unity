using Hollow.Core;
using UnityEngine;

namespace Hollow.Rewards
{
    public sealed class RoomRewardPickup : MonoBehaviour, IPooledRuntimeObject
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

        public void OnRentFromPool()
        {
            claimed = false;
            gameObject.SetActive(true);
        }

        public void OnReturnToPool()
        {
            roomId = string.Empty;
            claimed = false;
        }
    }
}
