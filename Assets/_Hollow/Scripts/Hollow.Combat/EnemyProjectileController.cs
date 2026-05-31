using Hollow.Data.Definitions;
using Hollow.Core;
using Hollow.Core.Diagnostics;
using Hollow.Entities;
using Hollow.Presentation;
using Hollow.Rooms;
using UnityEngine;

namespace Hollow.Combat
{
    public sealed class EnemyProjectileController : MonoBehaviour, IPooledRuntimeObject
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
        private bool ballistic;
        private Vector3 ballisticStartLocalPosition;
        private Vector3 ballisticTargetLocalPosition;
        private float ballisticTravelSeconds = 0.85f;
        private float ballisticArcHeightMeters = 1.35f;
        private float ballisticSplashRadiusMeters = 0.55f;
        private GameObject ballisticShadow;
        private GameObject presentationVisual;
        private bool countedActive;

        public int Damage => damage;

        public float SpeedMetersPerSecond => speedMetersPerSecond;

        public DamageClassification DamageClassification => damageClassification;

        public float KnockbackMeters => Mathf.Max(0f, knockbackMeters);

        public Vector3 Direction => localDirection;

        public bool IsBallistic => ballistic;

        public Vector3 BallisticTargetLocalPosition => ballisticTargetLocalPosition;

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
            destroyed = false;
            ballistic = false;
            MaterialResolver.ApplyTo(gameObject, MaterialRole.EnemyProjectile);
            if (presentationVisual == null)
            {
                presentationVisual = PresentationPrefabResolver.InstantiateVisual(PresentationPrefabRole.EnemyProjectile, transform, Vector3.zero, Vector3.one);
            }
            else
            {
                presentationVisual.SetActive(true);
            }

            if (ballisticShadow != null)
            {
                ballisticShadow.SetActive(false);
            }

            if (!countedActive)
            {
                countedActive = true;
                M136PerformanceOperationCounters.ReportProjectileSpawn();
            }
        }

        public void ConfigureBallisticLanding(Vector3 targetLocalPosition, float travelSeconds, float arcHeightMeters, float splashRadiusMeters)
        {
            ballistic = true;
            ballisticStartLocalPosition = transform.localPosition;
            ballisticTargetLocalPosition = targetLocalPosition;
            ballisticTargetLocalPosition.y = ballisticStartLocalPosition.y;
            ballisticTravelSeconds = Mathf.Max(0.15f, travelSeconds);
            ballisticArcHeightMeters = Mathf.Max(0.1f, arcHeightMeters);
            ballisticSplashRadiusMeters = Mathf.Max(0.1f, splashRadiusMeters);
            lifetimeSeconds = ballisticTravelSeconds + 0.1f;
            localDirection = ballisticTargetLocalPosition - ballisticStartLocalPosition;
            localDirection.y = 0f;
            localDirection = localDirection.sqrMagnitude <= 0.001f ? Vector3.forward : localDirection.normalized;
            EnsureBallisticShadow();
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
            if (ballistic)
            {
                return TickBallistic();
            }

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

        private bool TickBallistic()
        {
            var progress = Mathf.Clamp01(ageSeconds / ballisticTravelSeconds);
            var flatPosition = Vector3.Lerp(ballisticStartLocalPosition, ballisticTargetLocalPosition, progress);
            var height = Mathf.Sin(progress * Mathf.PI) * ballisticArcHeightMeters;
            flatPosition.y = ballisticStartLocalPosition.y + height;
            transform.localPosition = flatPosition;
            if (ballisticShadow != null)
            {
                ballisticShadow.transform.localPosition = new Vector3(0f, -height + 0.015f, 0f);
                var scale = Mathf.Lerp(0.34f, 0.2f, height / ballisticArcHeightMeters);
                ballisticShadow.transform.localScale = new Vector3(scale, 0.02f, scale);
            }

            if (progress < 1f)
            {
                return true;
            }

            ApplyBallisticLanding();
            DestroyProjectile();
            return false;
        }

        private void ApplyBallisticLanding()
        {
            if (RoomLocalCollision.IsOutsideBounds(roomRuntimeRoot, ballisticTargetLocalPosition, ballisticSplashRadiusMeters) ||
                RoomLocalCollision.IntersectsObstacle(roomRuntimeRoot, ballisticTargetLocalPosition, 0.08f))
            {
                return;
            }

            if (playerHealth == null || !playerHealth.IsAlive || playerController == null)
            {
                return;
            }

            var playerPosition = playerController.transform.localPosition;
            playerPosition.y = ballisticTargetLocalPosition.y;
            if (Vector3.Distance(playerPosition, ballisticTargetLocalPosition) > ballisticSplashRadiusMeters + PlaceholderPlayerController.DefaultRadiusMeters)
            {
                return;
            }

            var profile = CombatFeelProfileDefinition.Resolve(combatFeelProfile);
            var direction = playerController.transform.localPosition - ballisticTargetLocalPosition;
            var resolvedKnockback = KnockbackMeters > 0f ? KnockbackMeters : profile.PlayerKnockbackMeters;
            if (DamageSystem.ApplyDamage(
                    playerHealth,
                    new DamageRequest(
                        damage,
                        gameObject,
                        DamageFeedbackContext.Knockback(direction, resolvedKnockback, profile.KnockbackSeconds),
                        threatKind,
                        damageClassification,
                        guardKnockbackMultiplier)))
            {
                VfxPresenter.Play(VfxCueId.PlayerHit, playerController.transform.position, playerController.transform.parent);
                AudioPresenter.Play(AudioCueId.PlayerHit, playerController.transform.position);
            }
        }

        private void EnsureBallisticShadow()
        {
            if (ballisticShadow != null)
            {
                return;
            }

            ballisticShadow = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ballisticShadow.name = "BallisticShadow";
            ballisticShadow.transform.SetParent(transform, worldPositionStays: false);
            ballisticShadow.transform.localPosition = Vector3.zero;
            ballisticShadow.transform.localScale = new Vector3(0.34f, 0.02f, 0.34f);
            var collider = ballisticShadow.GetComponent<Collider>();
            if (collider != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(collider);
                }
                else
                {
                    DestroyImmediate(collider);
                }
            }

            var renderer = ballisticShadow.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = new Material(Shader.Find("Standard"))
                {
                    color = new Color(0f, 0f, 0f, 0.35f)
                };
            }
        }

        private bool CheckImpact()
        {
            M136PerformanceOperationCounters.ReportProjectileCollisionCheck();
            if (RoomLocalCollision.IsOutsideBounds(roomRuntimeRoot, transform.localPosition, hitRadiusMeters) ||
                RoomLocalCollision.IntersectsProjectileBlocker(roomRuntimeRoot, transform.localPosition, hitRadiusMeters))
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
                HollowRuntimePool.Return(gameObject);
            }
            else
            {
                DestroyImmediate(gameObject);
            }
        }

        public void OnRentFromPool()
        {
            destroyed = false;
            ageSeconds = 0f;
            gameObject.SetActive(true);
        }

        public void OnReturnToPool()
        {
            roomRuntimeRoot = null;
            playerController = null;
            playerHealth = null;
            ageSeconds = 0f;
            destroyed = true;
            ballistic = false;
            knockbackMeters = 0f;
            guardKnockbackMultiplier = 0f;
            if (countedActive)
            {
                countedActive = false;
                M136PerformanceOperationCounters.ReportProjectileReturn();
            }

            threatKind = DamageThreatKind.Light;
            damageClassification = DamageClassification.PhysicalProjectile(ImpactForceClass.Light);
            if (presentationVisual != null)
            {
                presentationVisual.SetActive(false);
            }

            if (ballisticShadow != null)
            {
                ballisticShadow.SetActive(false);
            }
        }
    }
}
