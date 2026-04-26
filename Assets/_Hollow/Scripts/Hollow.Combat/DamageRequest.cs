using UnityEngine;

namespace Hollow.Combat
{
    public readonly struct DamageRequest
    {
        public DamageRequest(int amount, GameObject source)
        {
            Amount = amount;
            Source = source;
        }

        public int Amount { get; }

        public GameObject Source { get; }
    }
}
