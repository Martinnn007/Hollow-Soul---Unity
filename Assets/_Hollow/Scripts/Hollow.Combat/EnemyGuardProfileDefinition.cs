using Hollow.Data.Definitions;
using UnityEngine;

namespace Hollow.Combat
{
    public enum EnemyShieldTier
    {
        None = 0,
        Small = 1,
        Medium = 2,
        Heavy = 3
    }

    [CreateAssetMenu(menuName = "Hollow/Combat/Enemy Guard Profile", fileName = "EnemyGuardProfile")]
    public sealed class EnemyGuardProfileDefinition : ScriptableObject
    {
        [SerializeField] private string guardId = "enemy_guard";
        [SerializeField] private string displayName = "Enemy Guard";
        [SerializeField] private EnemyShieldTier shieldTier = EnemyShieldTier.Medium;
        [SerializeField] private float frontalArcDegrees = 150f;
        [SerializeField] private float lightMediumPhysicalReduction = 0.75f;
        [SerializeField] private float heavyPhysicalReduction = 0.5f;
        [SerializeField] private float massivePhysicalReduction = 0.25f;
        [SerializeField] private ImpactForceClass guardBreakForceThreshold = ImpactForceClass.Heavy;
        [SerializeField] private float guardBreakRecoverySeconds = 0.55f;
        [TextArea(1, 4)]
        [SerializeField] private string notes = string.Empty;

        public string GuardId => string.IsNullOrWhiteSpace(guardId) ? "enemy_guard" : guardId;

        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? GuardId : displayName;

        public EnemyShieldTier ShieldTier => shieldTier;

        public float FrontalArcDegrees => Mathf.Clamp(frontalArcDegrees, 1f, 360f);

        public float LightMediumPhysicalReduction => Mathf.Clamp01(lightMediumPhysicalReduction);

        public float HeavyPhysicalReduction => Mathf.Clamp01(heavyPhysicalReduction);

        public float MassivePhysicalReduction => Mathf.Clamp01(massivePhysicalReduction);

        public ImpactForceClass GuardBreakForceThreshold => guardBreakForceThreshold;

        public float GuardBreakRecoverySeconds => Mathf.Max(0.05f, guardBreakRecoverySeconds);

        public string Notes => notes ?? string.Empty;

        public bool Reduces(DamageRequest request)
        {
            return request.Classification.Channel == DamageChannel.Physical;
        }

        public int ApplyReduction(DamageRequest request, int currentAmount)
        {
            if (currentAmount <= 0 || !Reduces(request))
            {
                return currentAmount;
            }

            var reduction = ReductionFor(request.Classification.ForceClass);
            return Mathf.FloorToInt(currentAmount * (1f - reduction));
        }

        public bool BreaksOn(DamageRequest request)
        {
            return (int)request.Classification.ForceClass >= (int)GuardBreakForceThreshold;
        }

        public float ReductionFor(ImpactForceClass forceClass)
        {
            if ((int)forceClass >= (int)ImpactForceClass.Massive)
            {
                return MassivePhysicalReduction;
            }

            if ((int)forceClass >= (int)ImpactForceClass.Heavy)
            {
                return HeavyPhysicalReduction;
            }

            return LightMediumPhysicalReduction;
        }

        public void Configure(
            string nextGuardId,
            string nextDisplayName,
            EnemyShieldTier nextShieldTier,
            float nextFrontalArcDegrees,
            float nextLightMediumPhysicalReduction,
            float nextHeavyPhysicalReduction,
            float nextMassivePhysicalReduction,
            ImpactForceClass nextGuardBreakForceThreshold,
            float nextGuardBreakRecoverySeconds,
            string nextNotes)
        {
            guardId = string.IsNullOrWhiteSpace(nextGuardId) ? "enemy_guard" : nextGuardId;
            displayName = string.IsNullOrWhiteSpace(nextDisplayName) ? guardId : nextDisplayName;
            shieldTier = nextShieldTier;
            frontalArcDegrees = Mathf.Clamp(nextFrontalArcDegrees, 1f, 360f);
            lightMediumPhysicalReduction = Mathf.Clamp01(nextLightMediumPhysicalReduction);
            heavyPhysicalReduction = Mathf.Clamp01(nextHeavyPhysicalReduction);
            massivePhysicalReduction = Mathf.Clamp01(nextMassivePhysicalReduction);
            guardBreakForceThreshold = nextGuardBreakForceThreshold;
            guardBreakRecoverySeconds = Mathf.Max(0.05f, nextGuardBreakRecoverySeconds);
            notes = nextNotes ?? string.Empty;
        }

        public void ConfigureFromRuntimeDefault(EnemyShieldTier tier)
        {
            var runtime = CreateRuntime(tier);
            Configure(
                runtime.GuardId,
                runtime.DisplayName,
                runtime.ShieldTier,
                runtime.FrontalArcDegrees,
                runtime.LightMediumPhysicalReduction,
                runtime.HeavyPhysicalReduction,
                runtime.MassivePhysicalReduction,
                runtime.GuardBreakForceThreshold,
                runtime.GuardBreakRecoverySeconds,
                runtime.Notes);
        }

        public static EnemyGuardProfileDefinition CreateRuntime(EnemyShieldTier tier)
        {
            var profile = CreateInstance<EnemyGuardProfileDefinition>();
            switch (tier)
            {
                case EnemyShieldTier.Small:
                    profile.Configure("small_shield", "Small Shield", EnemyShieldTier.Small, 135f, 0.5f, 0.25f, 0f, ImpactForceClass.Heavy, 0.42f, "Small shield: light protection, easy heavy break.");
                    break;
                case EnemyShieldTier.Heavy:
                    profile.Configure("heavy_shield", "Heavy Shield", EnemyShieldTier.Heavy, 170f, 1f, 0.8f, 0.55f, ImpactForceClass.Massive, 0.7f, "Heavy shield: near-total frontal protection, later enemy families only.");
                    break;
                case EnemyShieldTier.Medium:
                    profile.Configure("medium_shield", "Medium Shield", EnemyShieldTier.Medium, 150f, 0.75f, 0.5f, 0.25f, ImpactForceClass.Heavy, 0.55f, "Medium shield: Knight V1 frontal guard profile.");
                    break;
                default:
                    profile.Configure("no_shield", "No Shield", EnemyShieldTier.None, 120f, 0f, 0f, 0f, ImpactForceClass.Massive, 0.2f, "No shield guard reduction.");
                    break;
            }

            return profile;
        }

        public static EnemyGuardProfileDefinition DefaultForBehavior(EnemyBehaviorId behaviorId)
        {
            return behaviorId == EnemyBehaviorId.Knight ? CreateRuntime(EnemyShieldTier.Medium) : null;
        }
    }
}
