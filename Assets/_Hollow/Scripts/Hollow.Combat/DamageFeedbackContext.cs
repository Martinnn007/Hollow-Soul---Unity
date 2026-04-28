using UnityEngine;

namespace Hollow.Combat
{
    public readonly struct DamageFeedbackContext
    {
        public DamageFeedbackContext(Vector3 direction, float knockbackMeters, float knockbackSeconds)
        {
            var flatDirection = new Vector3(direction.x, 0f, direction.z);
            HasDirection = flatDirection.sqrMagnitude > 0.001f;
            Direction = HasDirection ? flatDirection.normalized : Vector3.zero;
            KnockbackMeters = Mathf.Max(0f, knockbackMeters);
            KnockbackSeconds = Mathf.Max(0.01f, knockbackSeconds);
        }

        public Vector3 Direction { get; }

        public float KnockbackMeters { get; }

        public float KnockbackSeconds { get; }

        public bool HasDirection { get; }

        public bool HasKnockback => HasDirection && KnockbackMeters > 0f;

        public static DamageFeedbackContext None => new(Vector3.zero, 0f, 0.01f);

        public static DamageFeedbackContext Knockback(Vector3 direction, float knockbackMeters, float knockbackSeconds)
        {
            return new DamageFeedbackContext(direction, knockbackMeters, knockbackSeconds);
        }
    }
}
