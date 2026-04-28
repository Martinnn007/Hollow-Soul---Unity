using UnityEngine;

namespace Hollow.Data.Definitions
{
    [CreateAssetMenu(menuName = "Hollow/Combat/Combat Feel Profile", fileName = "CombatFeelProfile")]
    public sealed class CombatFeelProfileDefinition : HollowDefinition
    {
        public const string DefaultResourcePath = "Hollow/Combat/CombatFeelProfile_M43";

        [SerializeField] private float playerInvulnerabilitySeconds = 0.6f;
        [SerializeField] private float playerFlashSeconds = 0.12f;
        [SerializeField] private float playerKnockbackMeters = 0.7f;
        [SerializeField] private float enemyMeleeKnockbackMeters = 0.55f;
        [SerializeField] private float enemyProjectileKnockbackMeters = 0.38f;
        [SerializeField] private float knockbackSeconds = 0.14f;
        [SerializeField] private float heavyAttackKnockbackMultiplier = 1.35f;
        [SerializeField] private float heavyEnemyKnockbackMultiplier = 0.55f;
        [SerializeField] private float bossEnemyKnockbackMultiplier = 0.25f;
        [SerializeField] private float enemyHitFlashSeconds = 0.1f;
        [SerializeField] private float windupPulseStrength = 0.08f;
        [SerializeField] private bool showWindupLabels;
        [SerializeField] private float corpseGhostSeconds = 1.5f;

        public float PlayerInvulnerabilitySeconds => Mathf.Max(0f, playerInvulnerabilitySeconds);
        public float PlayerFlashSeconds => Mathf.Max(0f, playerFlashSeconds);
        public float PlayerKnockbackMeters => Mathf.Max(0f, playerKnockbackMeters);
        public float EnemyMeleeKnockbackMeters => Mathf.Max(0f, enemyMeleeKnockbackMeters);
        public float EnemyProjectileKnockbackMeters => Mathf.Max(0f, enemyProjectileKnockbackMeters);
        public float KnockbackSeconds => Mathf.Max(0.01f, knockbackSeconds);
        public float HeavyAttackKnockbackMultiplier => Mathf.Max(0f, heavyAttackKnockbackMultiplier);
        public float HeavyEnemyKnockbackMultiplier => Mathf.Clamp01(heavyEnemyKnockbackMultiplier);
        public float BossEnemyKnockbackMultiplier => Mathf.Clamp01(bossEnemyKnockbackMultiplier);
        public float EnemyHitFlashSeconds => Mathf.Max(0f, enemyHitFlashSeconds);
        public float WindupPulseStrength => Mathf.Max(0f, windupPulseStrength);
        public bool ShowWindupLabels => showWindupLabels;
        public float CorpseGhostSeconds => Mathf.Max(0f, corpseGhostSeconds);

        public void ConfigureM43Defaults()
        {
            playerInvulnerabilitySeconds = 0.6f;
            playerFlashSeconds = 0.12f;
            playerKnockbackMeters = 0.7f;
            enemyMeleeKnockbackMeters = 0.55f;
            enemyProjectileKnockbackMeters = 0.38f;
            knockbackSeconds = 0.14f;
            heavyAttackKnockbackMultiplier = 1.35f;
            heavyEnemyKnockbackMultiplier = 0.55f;
            bossEnemyKnockbackMultiplier = 0.25f;
            enemyHitFlashSeconds = 0.1f;
            windupPulseStrength = 0.08f;
            showWindupLabels = false;
            corpseGhostSeconds = 1.5f;
        }

        public static CombatFeelProfileDefinition CreateRuntimeDefault()
        {
            var profile = CreateInstance<CombatFeelProfileDefinition>();
            profile.ConfigureM43Defaults();
            return profile;
        }

        public static CombatFeelProfileDefinition Resolve(CombatFeelProfileDefinition configured)
        {
            if (configured != null)
            {
                return configured;
            }

            var resource = Resources.Load<CombatFeelProfileDefinition>(DefaultResourcePath);
            return resource != null ? resource : CreateRuntimeDefault();
        }
    }
}
