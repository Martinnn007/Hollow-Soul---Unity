using UnityEngine;

namespace Hollow.Combat
{
    public readonly struct DamageRequest
    {
        public DamageRequest(int amount, GameObject source)
            : this(amount, source, DamageFeedbackContext.None)
        {
        }

        public DamageRequest(int amount, GameObject source, DamageFeedbackContext feedback)
        {
            Amount = amount;
            Source = source;
            Feedback = feedback;
        }

        public int Amount { get; }

        public GameObject Source { get; }

        public DamageFeedbackContext Feedback { get; }
    }
}
