using Hollow.Data.Definitions;
using Hollow.Entities;
using Hollow.Presentation;
using Hollow.Rooms;
using UnityEngine;

namespace Hollow.Combat
{
    public sealed class EnemyProjectileController : MonoBehaviour
    {
        private RoomRuntimeRoot roomRuntimeRoot;
        private PlaceholderPlayerController playerController;
        private CombatantHealth playerHealth;
        private CombatFeelProfileDefinition combatFeelProfile;
        private Vector3 localDirection = Vector3.forward;
        private float speedMetersPerSecond = 5f;
        private float lifetimeSeconds = 2f;
        private float ageSeconds;
        private int damage = 1;
        private float hitRadiusMeters = 0.24f;
        private DamageThreatKind threatKind = DamageThreatKind.Light;
        private DamageClassification damageClassification = DamageClassification.PhysicalProjectile(ImpactForceClass.Light);
        private float knockbackMeters;
        private float guardKnockbackMultiplier;
        private bool destroyed;

        public int Damage => damage;

        public float SpeedMetersPerSecond => speedMetersPerSecond;

        public DamageClassification DamageClassification => damageClassification;

        public float KnockbackMeters => Mathf.Max(0f, knockbackMeters);

        public void Configure(
            RoomRuntimeRoot room,
            PlaceholderPlayerController player,
            Vector3 direction,
            int nextDamage,
            float nextSpeedMetersPerSecond,
            float nextLifetimeSeconds = 2f)
        {
            roomRuntimeRoot = room;
            playerController = player;
            playerHealth = playerController != null ? playerController.GetComponent<CombatantHealth>() : null;
            localDirection = direction.sqrMagnitude < 0.001f ? Vector3.forward : direction.normalized;
            damage = Mathf.Max(0, nextDamage);
            speedMetersPerSecond = Mathf.Max(0.1f, nextSpeedMetersPerSecond);
            lifetimeSeconds = Mathf.Max(0.1f, nextLifetimeSeconds);
            ageSeconds = 0f;
            threatKind = DamageThreatKind.Light;
            damageClassification = DamageClassification.PhysicalProjectile(ImpactForceClass.Light);
            knockbackMeters = 0f;
            guardKnockbackMultiplier = 0f;
            MaterialResolver.ApplyTo(gameObject, MaterialRole.EnemyProjectile);
            PresentationPrefabResolver.InstantiateVisual(PresentationPrefabRole.EnemyProjectile, transform, Vector3.zero, Vector3.one);
        }

        public void ConfigureCombatFeel(CombatFeelProfileDefinition profile)
        {
            combatFeelProfile = CombatFeelProfileDefinition.Resolve(profile);
        }

        public void ConfigureThreat(DamageThreatKind nextThreatKind)
        {
            threatKind = nextThreatKind;
            damageClassification = DamageClassification.PhysicalProjectile(ForceClassForThreat(threatKind));
        }

        public void ConfigureAttackProfile(EnemyAttackProfileDefinition profile)
        {
            if (profile == null)
            {
                return;
            }

            damage = profile.Damage;
            threatKind = profile.ThreatKind;
            damageClassification = profile.Classification;
            knockbackMeters = profile.KnockbackMeters;
            guardKnockbackMultiplier = profile.GuardKnockbackMultiplier;
            if (profile.ProjectileSpeedMetersPerSecond > 0f)
            {
                speedMetersPerSecond = profile.ProjectileSpeedMetersPerSecond;
            }
        }

        public void Neutralize()
        {
            DestroyProjectile();
        }

        private void Update()
        {
            Tick(Time.deltaTime);
        }

        public bool Tick(float deltaTime)
        {
            ageSeconds += Mathf.Max(0f, deltaTime);
            if (CheckImpact())
            {
                return false;
            }

            if (ageSeconds >= lifetimeSeconds)
            {
                DestroyProjectile();
                return false;
            }

            var movement = localDirection * speedMetersPerSecond * Mathf.Max(0f, deltaTime);
            var stepCount = Mathf.Max(1, Mathf.CeilToInt(movement.magnitude / CombatFeelTuning.ProjectileSubstepMeters));
            var increment = movement / stepCount;
            for (var index = 0; index < stepCount; index++)
            {
                transform.localPosition += increment;
                if (CheckImpact())
                {
                    return false;
                }
            }

            return true;
        }

        private bool CheckImpact()
        {
            if (RoomLocalCollision.IsOutsideBounds(roomRuntimeRoot, transform.localPosition, hitRadiusMeters) ||
                RoomLocalCollision.IntersectsObstacle(roomRuntimeRoot, transform.localPosition, hitRadiusMeters))
            {
                DestroyProjectile();
                return true;
            }

            if (playerHealth != null && playerHealth.IsAlive && playerController != null)
            {
                var playerPosition = playerController.transform.localPosition;
                playerPosition.y = transform.localPosition.y;
                if (Vector3.Distance(playerPosition, transform.localPosition) <= hitRadiusMeters + PlaceholderPlayerController.DefaultRadiusMeters + 0.08f)
                {
                    var profile = CombatFeelProfileDefinition.Resolve(combatFeelProfile);
                    var resolvedKnockback = KnockbackMeters > 0f ? KnockbackMeters : profile.PlayerKnockbackMeters;
                    if (DamageSystem.ApplyDamage(
                            playerHealth,
                            new DamageRequest(
                                damage,
                                gameObject,
                                DamageFeedbackContext.Knockback(localDirection, resolvedKnockback, profile.KnockbackSeconds),
                                threatKind,
                                damageClassification,
                                guardKnockbackMultiplier)))
                    {
                        VfxPresenter.Play(VfxCueId.PlayerHit, playerController.transform.position, playerController.transform.parent);
                        AudioPresenter.Play(AudioCueId.PlayerHit, playerController.transform.position);
                    }

                    DestroyProjectile();
                    return true;
                }
            }

            return false;
        }

        private static ImpactForceClass ForceClassForThreat(DamageThreatKind threatKind)
        {
            return threatKind switch
            {
                DamageThreatKind.Boss => ImpactForceClass.Massive,
                DamageThreatKind.Heavy or DamageThreatKind.StrongProjectile => ImpactForceClass.Heavy,
                DamageThreatKind.Environmental => ImpactForceClass.Medium,
                _ => ImpactForceClass.Light
            };
        }

        private void DestroyProjectile()
        {
            if (destroyed)
            {
                return;
            }

            destroyed = true;
            if (Application.isPlaying)
            {
                Destroy(gameObject);
            }
            else
            {
                DestroyImmediate(gameObject);
            }
        }
    }
}
