using Hollow.Data.Definitions;
using Hollow.Presentation;
using Hollow.Rooms;
using UnityEngine;

namespace Hollow.Combat
{
    public sealed class ProjectileController : MonoBehaviour
    {
        public const float DefaultSpeedMetersPerSecond = 9f;
        public const float DefaultLifetimeSeconds = 1.5f;
        public const int DefaultDamage = 1;

        [SerializeField] private float speedMetersPerSecond = DefaultSpeedMetersPerSecond;
        [SerializeField] private float lifetimeSeconds = DefaultLifetimeSeconds;
        [SerializeField] private int damage = DefaultDamage;
        [SerializeField] private float hitRadiusMeters = 0.25f;

        private RoomRuntimeRoot roomRuntimeRoot;
        private RoomCombatController combatController;
        private CombatDiagnosticsModel diagnostics;
        private CombatFeelProfileDefinition combatFeelProfile;
        private Vector3 localDirection = Vector3.forward;
        private float ageSeconds;
        private bool heavyAttackProjectile;

        public void Configure(RoomRuntimeRoot room, RoomCombatController controller, Vector3 direction)
        {
            Configure(room, controller, direction, 0);
        }

        public void Configure(RoomRuntimeRoot room, RoomCombatController controller, Vector3 direction, int damageBonus)
        {
            Configure(room, controller, direction, DefaultDamage + Mathf.Max(0, damageBonus), DefaultSpeedMetersPerSecond, DefaultLifetimeSeconds);
        }

        public void Configure(RoomRuntimeRoot room, RoomCombatController controller, Vector3 direction, int nextDamage, float nextSpeedMetersPerSecond, float nextLifetimeSeconds)
        {
            roomRuntimeRoot = room;
            combatController = controller;
            diagnostics = controller != null ? controller.Diagnostics : null;
            localDirection = direction.sqrMagnitude < 0.001f ? Vector3.forward : direction.normalized;
            damage = Mathf.Max(1, nextDamage);
            speedMetersPerSecond = Mathf.Max(0.1f, nextSpeedMetersPerSecond);
            lifetimeSeconds = Mathf.Max(0.1f, nextLifetimeSeconds);
            ageSeconds = 0f;
            PresentationPrefabResolver.InstantiateVisual(PresentationPrefabRole.Projectile, transform, Vector3.zero, Vector3.one);
        }

        public void ConfigureCombatFeel(CombatFeelProfileDefinition profile, bool isHeavyAttackProjectile)
        {
            combatFeelProfile = CombatFeelProfileDefinition.Resolve(profile);
            heavyAttackProjectile = isHeavyAttackProjectile;
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
                DestroyProjectile(ProjectileDespawnReason.LifetimeExpired);
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
            var enemy = combatController != null ? combatController.FindEnemyHit(transform.localPosition, hitRadiusMeters) : null;
            if (enemy != null)
            {
                var profile = CombatFeelProfileDefinition.Resolve(combatFeelProfile);
                var knockback = profile.EnemyProjectileKnockbackMeters *
                                (heavyAttackProjectile ? profile.HeavyAttackKnockbackMultiplier : 1f);
                DamageSystem.ApplyDamage(
                    enemy.Health,
                    new DamageRequest(
                        damage,
                        gameObject,
                        DamageFeedbackContext.Knockback(localDirection, knockback, profile.KnockbackSeconds)));
                DestroyProjectile(ProjectileDespawnReason.EnemyHit);
                return true;
            }

            var destructible = combatController != null ? combatController.FindDestructibleHit(transform.localPosition, hitRadiusMeters) : null;
            if (destructible != null)
            {
                destructible.TryApplyHit(damage, gameObject);
                DestroyProjectile(ProjectileDespawnReason.ObstacleHit);
                return true;
            }

            if (RoomLocalCollision.IsOutsideBounds(roomRuntimeRoot, transform.localPosition, hitRadiusMeters))
            {
                DestroyProjectile(ProjectileDespawnReason.BoundsExit);
                return true;
            }

            if (RoomLocalCollision.IntersectsObstacle(roomRuntimeRoot, transform.localPosition, hitRadiusMeters))
            {
                DestroyProjectile(ProjectileDespawnReason.ObstacleHit);
                return true;
            }

            return false;
        }

        private void DestroyProjectile(ProjectileDespawnReason reason)
        {
            diagnostics?.RecordProjectileDespawn(reason);
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
