using UnityEngine;

namespace Hollow.Combat
{
    public sealed class DifficultyTierDefinition : ScriptableObject
    {
        [SerializeField] private string displayName = "Developer Sample";
        [SerializeField] private float healthMultiplier = 1f;
        [SerializeField] private float speedMultiplier = 1f;
        [SerializeField] private float contactDamageMultiplier = 1f;

        public string DisplayName => displayName;

        public DifficultyTuning Tuning => new(healthMultiplier, speedMultiplier, contactDamageMultiplier);

        public void Configure(string nextDisplayName, float nextHealthMultiplier, float nextSpeedMultiplier, float nextContactDamageMultiplier)
        {
            displayName = nextDisplayName;
            healthMultiplier = Mathf.Max(0.01f, nextHealthMultiplier);
            speedMultiplier = Mathf.Max(0.01f, nextSpeedMultiplier);
            contactDamageMultiplier = Mathf.Max(0.01f, nextContactDamageMultiplier);
        }

        public static DifficultyTierDefinition CreateRuntimeDeveloperSample()
        {
            var tier = CreateInstance<DifficultyTierDefinition>();
            tier.Configure("Developer Sample", 1f, 1f, 1f);
            return tier;
        }
    }
}
