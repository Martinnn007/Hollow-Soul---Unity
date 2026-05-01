using UnityEngine;

namespace Hollow.Combat
{
    public readonly struct DamageRequest
    {
        public DamageRequest(int amount, GameObject source)
            : this(amount, source, DamageFeedbackContext.None, DamageThreatKind.Light)
        {
        }

        public DamageRequest(int amount, GameObject source, DamageFeedbackContext feedback)
            : this(amount, source, feedback, DamageThreatKind.Light)
        {
        }

        public DamageRequest(int amount, GameObject source, DamageThreatKind threatKind)
            : this(amount, source, DamageFeedbackContext.None, threatKind)
        {
        }

        public DamageRequest(int amount, GameObject source, DamageFeedbackContext feedback, DamageThreatKind threatKind)
            : this(amount, source, feedback, threatKind, DamageClassification.FromThreat(threatKind))
        {
        }

        public DamageRequest(int amount, GameObject source, DamageFeedbackContext feedback, DamageClassification classification)
            : this(amount, source, feedback, DamageThreatKind.Light, classification)
        {
        }

        public DamageRequest(int amount, GameObject source, DamageFeedbackContext feedback, DamageThreatKind threatKind, DamageClassification classification)
        {
            Amount = amount;
            Source = source;
            Feedback = feedback;
            ThreatKind = threatKind;
            Classification = classification;
        }

        public int Amount { get; }

        public GameObject Source { get; }

        public DamageFeedbackContext Feedback { get; }

        public DamageThreatKind ThreatKind { get; }

        public DamageClassification Classification { get; }
    }
}
