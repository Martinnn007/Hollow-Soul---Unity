using Hollow.Entities;
using Hollow.Data.Definitions;
using Hollow.Presentation;
using Hollow.Rooms;
using System;
using UnityEngine;

namespace Hollow.Combat
{
    public class EnemyRuntimeController : MonoBehaviour
    {
        public const float ChargeWindupSeconds = 0.42f;
        public const float RangedWindupSeconds = 0.34f;
        public const float BossBurstWindupSeconds = 0.68f;
        public const float ChargeActiveSeconds = 0.38f;

        [SerializeField] private float speedMetersPerSecond = ChaserEnemyController.DefaultSpeedMetersPerSecond;
        [SerializeField] private int contactDamage = ChaserEnemyController.DefaultContactDamage;
        [SerializeField] private float contactCooldownSeconds = ChaserEnemyController.DefaultContactCooldownSeconds;
        [SerializeField] private float radiusMeters = 0.32f;
        [SerializeField] private EnemyArchetypeId archetypeId = EnemyArchetypeId.Normal;
        [SerializeField] private EnemyBehaviorId behaviorId = EnemyBehaviorId.Chaser;
        [SerializeField] private EnemyMovementMode movementMode = EnemyMovementMode.Grounded;

        private RoomRuntimeRoot roomRuntimeRoot;
        private PlaceholderPlayerController playerController;
        private CombatantHealth playerHealth;
        private float nextAllowedContactTime;
        private float nextAllowedAttackTime;
        private float nextAllowedChargeTime;
        private float chargeEndTime;
        private float entryGraceEndTime;
        private float readabilityStateEndTime;
        private bool firedLowHealthBossBurst;
        private Vector3 activeChargeDirection = Vector3.forward;
        private Vector3 telegraphDirection = Vector3.forward;
        private EnemyReadabilityState readabilityState = EnemyReadabilityState.Idle;
        private GameObject enemyPrefab;
        private GameObject enemyProjectilePrefab;
        private EnemyCatalog enemyCatalog;
        private DifficultyTierDefinition difficultyTier;
        private CombatDiagnosticsModel diagnostics;

        public event Action<EnemyRuntimeController> SpawnedChild;

        public CombatantHealth Health { get; private set; }

        public EnemyDefinition Definition { get; private set; }

        public EnemyArchetypeId ArchetypeId => archetypeId;

        public EnemyBehaviorId BehaviorId => behaviorId;

        public EnemyMovementMode MovementMode => movementMode;

        public float SpeedMetersPerSecond => speedMetersPerSecond;

        public int ContactDamage => contactDamage;

        public float RadiusMeters => radiusMeters;

        public bool IsAlive => Health != null && Health.IsAlive;

        public bool IsInEntryGrace(float timeSeconds) => timeSeconds < entryGraceEndTime;

        public EnemyReadabilityState ReadabilityState => readabilityState;

        public float ReadabilityStateEndTime => readabilityStateEndTime;

        public Vector3 TelegraphDirection => telegraphDirection.sqrMagnitude < 0.001f ? Vector3.forward : telegraphDirection.normalized;

        public EnemyReadabilityState ReadabilityStateAt(float timeSeconds)
        {
            if (IsInEntryGrace(timeSeconds))
            {
                return EnemyReadabilityState.EntryGrace;
            }

            if (readabilityState is EnemyReadabilityState.EntryGrace && timeSeconds >= readabilityStateEndTime)
            {
                return EnemyReadabilityState.Idle;
            }

            return readabilityState;
        }

        public float ReadabilitySecondsRemaining(float timeSeconds)
        {
            return Mathf.Max(0f, readabilityStateEndTime - timeSeconds);
        }

        public void BeginEntryGrace(float seconds, float currentTimeSeconds)
        {
            var graceEndTime = currentTimeSeconds + Mathf.Max(0f, seconds);
            entryGraceEndTime = Mathf.Max(entryGraceEndTime, graceEndTime);
            nextAllowedContactTime = Mathf.Max(nextAllowedContactTime, entryGraceEndTime);
            nextAllowedAttackTime = Mathf.Max(nextAllowedAttackTime, entryGraceEndTime);
            nextAllowedChargeTime = Mathf.Max(nextAllowedChargeTime, entryGraceEndTime);
            StartReadabilityState(EnemyReadabilityState.EntryGrace, Mathf.Max(0f, seconds), currentTimeSeconds, Vector3.forward);
        }

        public void Configure(RoomRuntimeRoot room, PlaceholderPlayerController player, EnemyDefinition definition, DifficultyTierDefinition difficultyTier)
        {
            roomRuntimeRoot = room;
            playerController = player;
            playerHealth = playerController != null ? playerController.GetComponent<CombatantHealth>() : null;
            Definition = definition != null ? definition : EnemyDefinition.CreateRuntimeNormal();
            var tuning = difficultyTier != null ? difficultyTier.Tuning : DifficultyTierDefinition.CreateRuntimeDeveloperSample().Tuning;

            archetypeId = Definition.ArchetypeId;
            behaviorId = Definition.BehaviorId;
            movementMode = Definition.MovementMode;
            speedMetersPerSecond = tuning.ApplySpeed(Definition.SpeedMetersPerSecond);
            contactDamage = tuning.ApplyContactDamage(Definition.ContactDamage);
            contactCooldownSeconds = Definition.ContactCooldownSeconds;
            radiusMeters = Definition.RadiusMeters;

            Health = GetComponent<CombatantHealth>() ?? gameObject.AddComponent<CombatantHealth>();
            Health.Configure(tuning.ApplyHealth(Definition.MaxHealth));
            Health.Died += OnDied;
            ApplyVisualMaterial(RoleForDefinition(Definition));
            PresentationPrefabResolver.InstantiateVisual(PrefabRoleForDefinition(Definition), transform, Vector3.zero, Vector3.one);

            var presenter = GetComponent<CombatReadabilityPresenter>() ?? gameObject.AddComponent<CombatReadabilityPresenter>();
            presenter.Bind(this);
        }

        public void ConfigureSpawnContext(
            GameObject nextEnemyPrefab,
            GameObject nextEnemyProjectilePrefab,
            EnemyCatalog nextCatalog,
            DifficultyTierDefinition nextDifficultyTier,
            CombatDiagnosticsModel nextDiagnostics)
        {
            enemyPrefab = nextEnemyPrefab;
            enemyProjectilePrefab = nextEnemyProjectilePrefab;
            enemyCatalog = nextCatalog;
            difficultyTier = nextDifficultyTier;
            diagnostics = nextDiagnostics;
        }

        private void Update()
        {
            Tick(Time.deltaTime, Time.time);
        }

        public void Tick(float deltaTime, float timeSeconds)
        {
            if (!IsAlive || playerController == null)
            {
                return;
            }

            if (IsInEntryGrace(timeSeconds))
            {
                return;
            }

            if (ResolvePendingReadabilityState(timeSeconds))
            {
                return;
            }

            if (behaviorId == EnemyBehaviorId.TurretShooter)
            {
                TryRangedAttack(timeSeconds);
                TryApplyContactDamage(timeSeconds);
                return;
            }

            if (behaviorId == EnemyBehaviorId.BossWarden)
            {
                TickBoss(deltaTime, timeSeconds);
                TryApplyContactDamage(timeSeconds);
                return;
            }

            if (behaviorId == EnemyBehaviorId.Charger && TickCharge(deltaTime, timeSeconds))
            {
                TryApplyContactDamage(timeSeconds);
                return;
            }

            var delta = playerController.transform.localPosition - transform.localPosition;
            delta.y = 0f;
            if (delta.sqrMagnitude > 0.01f)
            {
                var desired = transform.localPosition + delta.normalized * speedMetersPerSecond * deltaTime;
                transform.localPosition = movementMode == EnemyMovementMode.Flying
                    ? RoomLocalCollision.ResolveMoveIgnoringObstacles(roomRuntimeRoot, desired, radiusMeters)
                    : RoomLocalCollision.ResolveMove(roomRuntimeRoot, transform.localPosition, desired, radiusMeters);
            }

            TryApplyContactDamage(timeSeconds);
        }

        private void TickBoss(float deltaTime, float timeSeconds)
        {
            if (Health != null &&
                !firedLowHealthBossBurst &&
                Health.CurrentHealth <= Mathf.CeilToInt(Health.MaxHealth * 0.5f))
            {
                StartReadabilityState(EnemyReadabilityState.BossBurstWindup, BossBurstWindupSeconds, timeSeconds, Vector3.forward);
                return;
            }

            if (TickCharge(deltaTime, timeSeconds))
            {
                return;
            }

            TickChase(deltaTime);
            TryRangedAttack(timeSeconds);
        }

        private bool TickCharge(float deltaTime, float timeSeconds)
        {
            if (timeSeconds < chargeEndTime)
            {
                readabilityState = EnemyReadabilityState.Charging;
                readabilityStateEndTime = Mathf.Max(readabilityStateEndTime, chargeEndTime);
                var desired = transform.localPosition + activeChargeDirection * Definition.ChargeSpeedMetersPerSecond * deltaTime;
                transform.localPosition = RoomLocalCollision.ResolveMove(roomRuntimeRoot, transform.localPosition, desired, radiusMeters);
                return true;
            }

            if (readabilityState == EnemyReadabilityState.Charging)
            {
                readabilityState = EnemyReadabilityState.Idle;
                readabilityStateEndTime = 0f;
            }

            if (timeSeconds < nextAllowedChargeTime || playerController == null)
            {
                return false;
            }

            var delta = playerController.transform.localPosition - transform.localPosition;
            delta.y = 0f;
            if (delta.sqrMagnitude < 0.01f || delta.magnitude > Definition.AttackRangeMeters)
            {
                return false;
            }

            StartReadabilityState(EnemyReadabilityState.ChargeWindup, ChargeWindupSeconds, timeSeconds, delta.normalized);
            return true;
        }

        private void TickChase(float deltaTime)
        {
            var delta = playerController.transform.localPosition - transform.localPosition;
            delta.y = 0f;
            if (delta.sqrMagnitude <= 0.01f)
            {
                return;
            }

            var desired = transform.localPosition + delta.normalized * speedMetersPerSecond * deltaTime;
            transform.localPosition = movementMode == EnemyMovementMode.Flying
                ? RoomLocalCollision.ResolveMoveIgnoringObstacles(roomRuntimeRoot, desired, radiusMeters)
                : RoomLocalCollision.ResolveMove(roomRuntimeRoot, transform.localPosition, desired, radiusMeters);
        }

        private bool TryRangedAttack(float timeSeconds)
        {
            if (playerController == null || timeSeconds < nextAllowedAttackTime)
            {
                return false;
            }

            var delta = playerController.transform.localPosition - transform.localPosition;
            delta.y = 0f;
            if (delta.sqrMagnitude < 0.01f || delta.magnitude > Definition.AttackRangeMeters)
            {
                return false;
            }

            StartReadabilityState(EnemyReadabilityState.RangedWindup, RangedWindupSeconds, timeSeconds, delta.normalized);
            return true;
        }

        private bool ResolvePendingReadabilityState(float timeSeconds)
        {
            if (readabilityState == EnemyReadabilityState.EntryGrace)
            {
                if (timeSeconds < readabilityStateEndTime)
                {
                    return true;
                }

                readabilityState = EnemyReadabilityState.Idle;
                readabilityStateEndTime = 0f;
                return false;
            }

            if (readabilityState == EnemyReadabilityState.ChargeWindup)
            {
                if (timeSeconds < readabilityStateEndTime)
                {
                    return true;
                }

                activeChargeDirection = TelegraphDirection;
                chargeEndTime = timeSeconds + ChargeActiveSeconds;
                nextAllowedChargeTime = timeSeconds + Definition.ChargeCooldownSeconds;
                readabilityState = EnemyReadabilityState.Charging;
                readabilityStateEndTime = chargeEndTime;
                return true;
            }

            if (readabilityState == EnemyReadabilityState.RangedWindup)
            {
                if (timeSeconds < readabilityStateEndTime)
                {
                    return true;
                }

                nextAllowedAttackTime = timeSeconds + Definition.AttackCooldownSeconds;
                FireProjectile(TelegraphDirection);
                readabilityState = EnemyReadabilityState.Idle;
                readabilityStateEndTime = 0f;
                return true;
            }

            if (readabilityState == EnemyReadabilityState.BossBurstWindup)
            {
                if (timeSeconds < readabilityStateEndTime)
                {
                    return true;
                }

                firedLowHealthBossBurst = true;
                FireProjectile(Vector3.forward);
                FireProjectile(Vector3.back);
                FireProjectile(Vector3.left);
                FireProjectile(Vector3.right);
                readabilityState = EnemyReadabilityState.Idle;
                readabilityStateEndTime = 0f;
                return true;
            }

            return false;
        }

        private void StartReadabilityState(EnemyReadabilityState state, float durationSeconds, float timeSeconds, Vector3 direction)
        {
            readabilityState = state;
            readabilityStateEndTime = timeSeconds + Mathf.Max(0f, durationSeconds);
            if (direction.sqrMagnitude > 0.001f)
            {
                direction.y = 0f;
                telegraphDirection = direction.normalized;
            }
        }

        public bool TryApplyContactDamage(float timeSeconds)
        {
            if (!IsAlive || playerHealth == null || !playerHealth.IsAlive || IsInEntryGrace(timeSeconds) || timeSeconds < nextAllowedContactTime)
            {
                return false;
            }

            var distance = Vector3.Distance(Flat(transform.localPosition), Flat(playerController.transform.localPosition));
            if (distance > radiusMeters + PlaceholderPlayerController.DefaultRadiusMeters + 0.12f)
            {
                return false;
            }

            nextAllowedContactTime = timeSeconds + contactCooldownSeconds;
            var damaged = DamageSystem.ApplyDamage(playerHealth, new DamageRequest(contactDamage, gameObject));
            if (damaged)
            {
                VfxPresenter.Play(VfxCueId.PlayerHit, playerController.transform.position, playerController.transform.parent);
                AudioPresenter.Play(AudioCueId.PlayerHit, playerController.transform.position);
            }

            return damaged;
        }

        private void OnDied(CombatantHealth _)
        {
            SpawnSplitChildren();
            VfxPresenter.Play(VfxCueId.EnemyDeath, transform.position, transform.parent);
            AudioPresenter.Play(AudioCueId.EnemyDeath, transform.position);
            gameObject.SetActive(false);
        }

        private void FireProjectile(Vector3 direction)
        {
            var projectileObject = enemyProjectilePrefab != null
                ? Instantiate(enemyProjectilePrefab, transform.parent)
                : GameObject.CreatePrimitive(PrimitiveType.Sphere);
            projectileObject.name = $"EnemyProjectile.{Definition.SpawnKind}";
            projectileObject.transform.SetParent(transform.parent, worldPositionStays: false);
            projectileObject.transform.localPosition = transform.localPosition + direction.normalized * (radiusMeters + 0.22f) + new Vector3(0f, 0.35f, 0f);
            projectileObject.transform.localScale = Vector3.one * 0.22f;
            var playerProjectile = projectileObject.GetComponent<ProjectileController>();
            if (playerProjectile != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(playerProjectile);
                }
                else
                {
                    DestroyImmediate(playerProjectile);
                }
            }

            var collider = projectileObject.GetComponent<Collider>();
            if (collider != null)
            {
                collider.enabled = false;
            }

            var projectile = projectileObject.GetComponent<EnemyProjectileController>() ?? projectileObject.AddComponent<EnemyProjectileController>();
            projectile.Configure(
                roomRuntimeRoot,
                playerController,
                direction,
                Definition.ProjectileDamage,
                Definition.ProjectileSpeedMetersPerSecond);
        }

        private void SpawnSplitChildren()
        {
            if (behaviorId != EnemyBehaviorId.Splitter ||
                Definition.SplitCount <= 0 ||
                enemyPrefab == null ||
                roomRuntimeRoot == null ||
                playerController == null)
            {
                return;
            }

            var catalog = enemyCatalog != null ? enemyCatalog : EnemyCatalog.CreateRuntimeDefault();
            var definition = EnemyDefinitionResolver.Resolve(catalog, Definition.SplitSpawnKind, out _);
            var difficulty = difficultyTier != null ? difficultyTier : DifficultyTierDefinition.CreateRuntimeDeveloperSample();
            var angleStep = 360f / Definition.SplitCount;
            for (var index = 0; index < Definition.SplitCount; index++)
            {
                var childObject = Instantiate(enemyPrefab, transform.parent);
                childObject.name = $"Enemy.Split.{index:00}.{definition.SpawnKind}";
                childObject.SetActive(true);
                var offset = Quaternion.Euler(0f, angleStep * index, 0f) * Vector3.forward * 0.48f;
                childObject.transform.localPosition = RoomLocalCollision.ResolveMoveIgnoringObstacles(roomRuntimeRoot, transform.localPosition + offset, definition.RadiusMeters);
                var child = childObject.GetComponent<EnemyRuntimeController>() ?? childObject.AddComponent<EnemyRuntimeController>();
                child.Configure(roomRuntimeRoot, playerController, definition, difficulty);
                child.ConfigureSpawnContext(enemyPrefab, enemyProjectilePrefab, catalog, difficulty, diagnostics);
                child.BeginEntryGrace(RoomCombatController.EntryGraceSeconds, Time.time);
                SpawnedChild?.Invoke(child);
            }
        }

        private void ApplyVisualMaterial(MaterialRole role)
        {
            var renderer = GetComponentInChildren<Renderer>();
            MaterialResolver.ApplyTo(renderer, role);
        }

        private static MaterialRole RoleForDefinition(EnemyDefinition definition)
        {
            if (definition == null)
            {
                return MaterialRole.EnemyNormal;
            }

            return definition.BehaviorId switch
            {
                EnemyBehaviorId.Charger => MaterialRole.EnemyCharger,
                EnemyBehaviorId.TurretShooter => MaterialRole.EnemyTurret,
                EnemyBehaviorId.Splitter => MaterialRole.EnemySplitter,
                EnemyBehaviorId.BossWarden => MaterialRole.EnemyBoss,
                EnemyBehaviorId.FlyingChaser => MaterialRole.EnemyFlying,
                _ => definition.ArchetypeId switch
                {
                    EnemyArchetypeId.Fast => MaterialRole.EnemyFast,
                    EnemyArchetypeId.Heavy => MaterialRole.EnemyHeavy,
                    EnemyArchetypeId.Boss => MaterialRole.EnemyBoss,
                    _ => MaterialRole.EnemyNormal
                }
            };
        }

        private static PresentationPrefabRole PrefabRoleForDefinition(EnemyDefinition definition)
        {
            if (definition == null)
            {
                return PresentationPrefabRole.EnemyNormal;
            }

            return definition.BehaviorId switch
            {
                EnemyBehaviorId.Charger => PresentationPrefabRole.EnemyCharger,
                EnemyBehaviorId.TurretShooter => PresentationPrefabRole.EnemyTurret,
                EnemyBehaviorId.Splitter => PresentationPrefabRole.EnemySplitter,
                EnemyBehaviorId.BossWarden => PresentationPrefabRole.EnemyBoss,
                EnemyBehaviorId.FlyingChaser => PresentationPrefabRole.EnemyFlying,
                _ => definition.ArchetypeId switch
                {
                    EnemyArchetypeId.Fast => PresentationPrefabRole.EnemyFast,
                    EnemyArchetypeId.Heavy => PresentationPrefabRole.EnemyHeavy,
                    EnemyArchetypeId.Boss => PresentationPrefabRole.EnemyBoss,
                    _ => PresentationPrefabRole.EnemyNormal
                }
            };
        }

        private static Vector3 Flat(Vector3 value)
        {
            return new Vector3(value.x, 0f, value.z);
        }
    }
}
