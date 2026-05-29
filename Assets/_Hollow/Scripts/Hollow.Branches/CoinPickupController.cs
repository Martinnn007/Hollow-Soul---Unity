using Hollow.Rewards;
using Hollow.Core;
using UnityEngine;

namespace Hollow.Branches
{
    public sealed class CoinPickupController : MonoBehaviour, IPooledRuntimeObject
    {
        public string RoomId { get; private set; } = string.Empty;

        public string PickupId { get; private set; } = string.Empty;

        public CoinDenomination Denomination { get; private set; }

        public int Value { get; private set; } = 1;

        public bool IsCollected { get; private set; }

        public void Configure(string roomId, string pickupId, CoinDenomination denomination, int value, bool isCollected)
        {
            RoomId = roomId ?? string.Empty;
            PickupId = pickupId ?? string.Empty;
            Denomination = denomination;
            Value = Mathf.Max(1, value);
            IsCollected = isCollected;
            gameObject.name = $"Coin_{Denomination}_{PickupId}";
        }

        public bool Collect()
        {
            if (IsCollected)
            {
                return false;
            }

            IsCollected = true;
            return true;
        }

        public void OnRentFromPool()
        {
            IsCollected = false;
            gameObject.SetActive(true);
        }

        public void OnReturnToPool()
        {
            RoomId = string.Empty;
            PickupId = string.Empty;
            Value = 1;
            IsCollected = false;
        }
    }
}
