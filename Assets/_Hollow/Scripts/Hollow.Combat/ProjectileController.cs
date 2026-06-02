using Hollow.Data.Definitions;
using Hollow.Core;
using Hollow.Core.Diagnostics;
using Hollow.Presentation;
using Hollow.Rooms;
using UnityEngine;

namespace Hollow.Combat
{
    public sealed class ProjectileController : MonoBehaviour, IPooledRuntimeObject
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
        private ImpactForceClass impactForceClass = ImpactForceClass.Light;
        private float knockbackMeters;
        private GameObject sourceOwner;
        private GameObject presentationVisual;
        private bool countedActive;

        public Vector3 ConfiguredLocalDirection => localDirection.sqrMagnitude > 0.001f ? localDirection.normalized : Vector3.forward;

        public float ConfiguredSpeedMetersPerSecond => speedMetersPerSecond;

        public float ConfiguredLifetimeSeconds => lifetimeSeconds;

        public float AgeSeconds => ageSeconds;

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
            Configure(room, controller, direction, nextDamage, nextSpeedMetersPerSecond, nextLifetimeSeconds, null);
        }

        public void Configure(RoomRuntimeRoot room, RoomCombatController controller, Vector3 direction, int nextDamage, float nextSpeedMetersPerSecond, float nextLifetimeSeconds, GameObject nextSourceOwner)
        {
            roomRuntimeRoot = room;
            combatController = controller;
            diagnostics = controller != null ? controller.Diagnostics : null;
            sourceOwner = nextSourceOwner;
            localDirection = direction.sqrMagnitude < 0.001f ? Vector3.forward : direction.normalized;
            damage = Mathf.Max(1, nextDamage);
            speedMetersPerSecond = Mathf.Max(0.1f, nextSpeedMetersPerSecond);
            lifetimeSeconds = Mathf.Max(0.1f, nextLifetimeSeconds);
            ageSeconds = 0f;
            if (presentationVisual == null)
            {
                presentationVisual = PresentationPrefabResolver.InstantiateVisual(PresentationPrefabRole.Projectile, transform, Vector3.zero, Vector3.one);
            }
            else
            {
                presentationVisual.SetActive(true);
            }

            if (!countedActive)
            {
                countedActive = true;
                M136PerformanceOperationCounters.ReportProjectileSpawn();
            }
        }

        public void ConfigureCombatFeel(CombatFeelProfileDefinition profile, bool isHeavyAttackProjectile)
        {
            ConfigureCombatFeel(
                profile,
                isHeavyAttackProjectile,
                isHeavyAttackProjectile ? ImpactForceClass.Medium : ImpactForceClass.Light,
                0f);
        }

        public void ConfigureCombatFeel(
            CombatFeelProfileDefinition profile,
            bool isHeavyAttackProjectile,
            ImpactForceClass nextImpactForceClass,
            float nextKnockbackMeters)
        {
            combatFeelProfile = CombatFeelProfileDefinition.Resolve(profile);
            heavyAttackProjectile = isHeavyAttackProjectile;
            impactForceClass = nextImpactForceClass;
            knockbackMeters = Mathf.Max(0f, nextKnockbackMeters);
        }

        private void Update()
        {
            Tick(Time.deltaTime);
        }

        public bool Tick(float deltaTime)
        {
            var start = Time.realtimeSinceStartup;
            try
            {
                return TickInternal(deltaTime);
            }
            finally
            {
                M136PerformanceOperationCounters.ReportProjectileUpdate((Time.realtimeSinceStartup - start) * 1000f);
            }
        }

        private bool TickInternal(float deltaTime)
        {
            ageSeconds += Mathf.Max(0f, deltaTime);
            if (CheckImpact(transform.localPosition, transform.localPosition))
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
            var previousPosition = transform.localPosition;
            for (var index = 0; index < stepCount; index++)
            {
                var nextPosition = previousPosition + increment;
                if (CheckImpact(previousPosition, nextPosition))
                {
                    return false;
                }

                transform.localPosition = previousPosition = nextPosition;
            }

            return true;
        }

        private bool CheckImpact(Vector3 fromLocalPosition, Vector3 toLocalPosition)
        {
            M136PerformanceOperationCounters.ReportProjectileCollisionCheck();
            var enemy = combatController != null ? combatController.FindEnemyHit(fromLocalPosition, toLocalPosition, hitRadiusMeters) : null;
            if (enemy != null)
            {
                var profile = CombatFeelProfileDefinition.Resolve(combatFeelProfile);
                var knockback = knockbackMeters > 0f
                    ? knockbackMeters
                    : profile.EnemyProjectileKnockbackMeters * (heavyAttackProjectile ? profile.HeavyAttackKnockbackMultiplier : 1f);
                if (DamageSystem.ApplyDamage(
                    enemy.Health,
                    new DamageRequest(
                        damage,
                        sourceOwner != null ? sourceOwner : gameObject,
                        DamageFeedbackContext.Knockback(localDirection, knockback, profile.KnockbackSeconds),
                        DamageClassification.PhysicalProjectile(impactForceClass))))
                {
                    var aimLock = sourceOwner != null ? sourceOwner.GetComponent<PlayerAimLockController>() : null;
                    aimLock?.NotifyEnemyDamaged(enemy);
                }

                DestroyProjectile(ProjectileDespawnReason.EnemyHit);
                return true;
            }

            var destructible = combatController != null ? combatController.FindDestructibleHit(fromLocalPosition, toLocalPosition, hitRadiusMeters) : null;
            if (destructible != null)
            {
                destructible.TryApplyHit(damage, gameObject);
                DestroyProjectile(ProjectileDespawnReason.ObstacleHit);
                return true;
            }

            if (RoomLocalCollision.IsOutsideBounds(roomRuntimeRoot, toLocalPosition, hitRadiusMeters))
            {
                DestroyProjectile(ProjectileDespawnReason.BoundsExit);
                return true;
            }

            if (RoomLocalCollision.IntersectsProjectileBlocker(roomRuntimeRoot, toLocalPosition, hitRadiusMeters))
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
                HollowRuntimePool.Return(gameObject);
            }
            else
            {
                DestroyImmediate(gameObject);
            }
        }

        public void OnRentFromPool()
        {
            ageSeconds = 0f;
            gameObject.SetActive(true);
        }

        public void OnReturnToPool()
        {
            roomRuntimeRoot = null;
            combatController = null;
            diagnostics = null;
            sourceOwner = null;
            ageSeconds = 0f;
            heavyAttackProjectile = false;
            impactForceClass = ImpactForceClass.Light;
            knockbackMeters = 0f;
            if (countedActive)
            {
                countedActive = false;
                M136PerformanceOperationCounters.ReportProjectileReturn();
            }

            if (presentationVisual != null)
            {
                presentationVisual.SetActive(false);
            }
        }
    }
}
