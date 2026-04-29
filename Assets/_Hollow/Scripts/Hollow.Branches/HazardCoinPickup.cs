using UnityEngine;

namespace Hollow.Branches
{
    public sealed class HazardCoinPickup : MonoBehaviour
    {
        [SerializeField] private string roomId = string.Empty;
        [SerializeField] private string objectId = string.Empty;
        [SerializeField] private int coinAmount = 1;
        private bool claimed;

        public string RoomId => roomId;

        public string ObjectId => objectId;

        public int CoinAmount => Mathf.Max(0, coinAmount);

        public bool IsClaimed => claimed;

        public void Configure(string nextRoomId, string nextObjectId, int nextCoinAmount)
        {
            roomId = nextRoomId ?? string.Empty;
            objectId = nextObjectId ?? string.Empty;
            coinAmount = Mathf.Max(0, nextCoinAmount);
            claimed = false;
        }

        public bool Claim()
        {
            if (claimed || coinAmount <= 0)
            {
                return false;
            }

            claimed = true;
            return true;
        }
    }
}
