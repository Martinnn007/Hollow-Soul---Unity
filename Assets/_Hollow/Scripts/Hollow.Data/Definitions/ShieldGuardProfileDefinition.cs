using UnityEngine;

namespace Hollow.Data.Definitions
{
    [CreateAssetMenu(menuName = "Hollow/Combat/Shield Guard Profile", fileName = "ShieldGuardProfile")]
    public sealed class ShieldGuardProfileDefinition : HollowDefinition
    {
        public const string DefaultResourcePath = "Hollow/Combat/ShieldGuardProfile_M44";

        [SerializeField] private float parryWindowSeconds = 0.3f;
        [SerializeField] private float guardConeDegrees = 140f;
        [SerializeField] private float guardMoveMultiplier = 0.55f;
        [SerializeField] private float guardDrainStaminaPerSecond = 12f;
        [SerializeField] private float guardHitStaminaCost = 12f;
        [SerializeField] private float parryStaminaCost = 16f;
        [SerializeField] private int guardDamageReduction = 1;
        [SerializeField] private float guardPushMeters = 0.25f;
        [SerializeField] private int parryCounterDamage = 1;
        [SerializeField] private float shieldVisualDistanceMeters = 0.52f;
        [SerializeField] private float shieldVisualHeightMeters = 0.58f;
        [SerializeField] private float shieldFeedbackSeconds = 0.18f;

        public float ParryWindowSeconds => Mathf.Max(0f, parryWindowSeconds);
        public float GuardConeDegrees => Mathf.Clamp(guardConeDegrees, 1f, 360f);
        public float GuardMoveMultiplier => Mathf.Clamp(guardMoveMultiplier, 0.1f, 1f);
        public float GuardDrainStaminaPerSecond => Mathf.Max(0f, guardDrainStaminaPerSecond);
        public float GuardHitStaminaCost => Mathf.Max(0f, guardHitStaminaCost);
        public float ParryStaminaCost => Mathf.Max(0f, parryStaminaCost);
        public int GuardDamageReduction => Mathf.Max(0, guardDamageReduction);
        public float GuardPushMeters => Mathf.Max(0f, guardPushMeters);
        public int ParryCounterDamage => Mathf.Max(0, parryCounterDamage);
        public float ShieldVisualDistanceMeters => Mathf.Max(0f, shieldVisualDistanceMeters);
        public float ShieldVisualHeightMeters => Mathf.Max(0f, shieldVisualHeightMeters);
        public float ShieldFeedbackSeconds => Mathf.Max(0f, shieldFeedbackSeconds);

        public void ConfigureM44Defaults()
        {
            parryWindowSeconds = 0.3f;
            guardConeDegrees = 140f;
            guardMoveMultiplier = 0.55f;
            guardDrainStaminaPerSecond = 12f;
            guardHitStaminaCost = 12f;
            parryStaminaCost = 16f;
            guardDamageReduction = 1;
            guardPushMeters = 0.25f;
            parryCounterDamage = 1;
            shieldVisualDistanceMeters = 0.52f;
            shieldVisualHeightMeters = 0.58f;
            shieldFeedbackSeconds = 0.18f;
        }

        public static ShieldGuardProfileDefinition CreateRuntimeDefault()
        {
            var profile = CreateInstance<ShieldGuardProfileDefinition>();
            profile.ConfigureM44Defaults();
            return profile;
        }

        public static ShieldGuardProfileDefinition Resolve(ShieldGuardProfileDefinition configured)
        {
            if (configured != null)
            {
                return configured;
            }

            var resource = Resources.Load<ShieldGuardProfileDefinition>(DefaultResourcePath);
            return resource != null ? resource : CreateRuntimeDefault();
        }
    }
}
