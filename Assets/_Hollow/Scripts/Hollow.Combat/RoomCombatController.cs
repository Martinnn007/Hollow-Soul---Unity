using System.Collections.Generic;
using System.Linq;
using System;
using Hollow.Data.Definitions;
using Hollow.Entities;
using Hollow.Presentation;
using Hollow.Rooms;
using UnityEngine;

namespace Hollow.Combat
{
    public sealed class RoomCombatController : MonoBehaviour
    {
        public const int PlayerMaxHealth = 6;
        public const float EntryGraceSeconds = 1f;
        public const float EnemyAttackBudgetWindowSeconds = 0.45f;
        public const float EnemyMeleeAttackBudgetWindowSeconds = 0.3f;
        public const float PlayerFootstepStimulusIntervalSeconds = 0.45f;
        public const float PlayerFootstepMinimumDistanceMeters = 0.08f;

        [SerializeField] private GameObject enemyPrefab;
        [SerializeField] private GameObject projectilePrefab;
        [SerializeField] private EnemyCatalog enemyCatalog;
        [SerializeField] private BossCatalogDefinition bossCatalog;
        [SerializeField] private DifficultyTierDefinition difficultyTier;
        [SerializeField] private CombatFeelProfileDefinition combatFeelProfile;
        [SerializeField] private RoomHazardTuningProfileDefinition hazardTuningProfile;
        [SerializeField] private RoomRuntimeRoot roomRuntimeRoot;
        [SerializeField] private PlaceholderPlayerController playerController;

        private readonly List<EnemyRuntimeController> enemies = new();
        private readonly List<RoomHazardController> hazards = new();
        private readonly List<DestructibleRoomObjectController> destructibleObjects = new();
        private CombatantHealth playerHealth;
        private CombatFeelProfileDefinition resolvedCombatFeelProfile;
        private RoomHazardTuningProfileDefinition resolvedHazardTuningProfile;
        private RoomCombatEncounterContext activeEncounterContext = RoomCombatEncounterContext.Empty;
        private bool initialized;
        private InspectionEntityMode inspectionMode = InspectionEntityMode.LiveRuntime;
        private bool ignoreEnemiesForRoomClear;
        private readonly CombatDiagnosticsModel diagnostics = new();
        private float nextEnemyAttackBudgetTime;
        private float nextEnemyMeleeAttackBudgetTime;
        private float nextPlayerFootstepStimulusTime;
        private Vector3 lastPlayerFootstepStimulusLocalPosition;
        private bool hasLastPlayerFootstepStimulusLocalPosition;

        public event Action<RoomCombatController> RoomCleared;

        public event Action<RoomInteractiveObjectDestroyedContext> InteractiveObjectDestroyed;

        public RoomObjectiveState ObjectiveState { get; private set; } = RoomObjectiveState.WaitingToStart;

        public GameObject EnemyPrefab => enemyPrefab;

        public GameObject ProjectilePrefab => projectilePrefab;

        public EnemyCatalog EnemyCatalog => enemyCatalog;

        public BossCatalogDefinition BossCatalog => bossCatalog;

        public DifficultyTierDefinition DifficultyTier => difficultyTier;

        public CombatFeelProfileDefinition CombatFeelProfile => ResolveCombatFeelProfile();

        public CombatDiagnosticsModel Diagnostics => diagnostics;

        public IReadOnlyList<EnemyRuntimeController> Enemies => enemies;

        public EnemyRuntimeController ActiveBoss => enemies.FirstOrDefault(enemy => enemy != null && enemy.IsAlive && enemy.BossDefinition != null);

        public IReadOnlyList<DestructibleRoomObjectController> DestructibleObjects => destructibleObjects;

        public CombatantHealth PlayerHealth => playerHealth;

        public PlaceholderPlayerController PlayerController => playerController;

        public InspectionEntityMode InspectionMode => inspectionMode;

        public bool IgnoresEnemiesForRoomClear => ignoreEnemiesForRoomClear;

        public void Configure(GameObject nextEnemyPrefab, GameObject nextProjectilePrefab)
        {
            enemyPrefab = nextEnemyPrefab;
            projectilePrefab = nextProjectilePrefab;
        }

        public void Configure(GameObject nextEnemyPrefab, GameObject nextProjectilePrefab, EnemyCatalog nextEnemyCatalog, DifficultyTierDefinition nextDifficultyTier)
        {
            enemyPrefab = nextEnemyPrefab;
            projectilePrefab = nextProjectilePrefab;
            enemyCatalog = nextEnemyCatalog;
            difficultyTier = nextDifficultyTier;
        }

        public void ConfigureBossCatalog(BossCatalogDefinition nextBossCatalog)
        {
            bossCatalog = nextBossCatalog;
        }

        public void ConfigureInspectionMode(InspectionEntityMode mode, bool ignoreRoomClear)
        {
            inspectionMode = mode;
            ignoreEnemiesForRoomClear = ignoreRoomClear;
        }

        private void Start()
        {
            InitializeCombat();
        }

        private void Update()
        {
            TickPlayerFootstepStimuli(Time.time);
            EvaluateRoomState();
        }

        public void InitializeCombat()
        {
            if (initialized)
            {
                return;
            }

            ResolveReferences();
            if (roomRuntimeRoot == null || playerController == null)
            {
                return;
            }

            BeginRoom(roomRuntimeRoot, playerController, alreadyCleared: false);
        }

        public void BeginRoom(RoomRuntimeRoot room, PlaceholderPlayerController player, bool alreadyCleared)
        {
            BeginRoom(room, player, alreadyCleared, RoomCombatEncounterKind.Standard);
        }

        public void BeginRoom(RoomRuntimeRoot room, PlaceholderPlayerController player, bool alreadyCleared, RoomCombatEncounterKind encounterKind)
        {
            BeginRoom(room, player, alreadyCleared, encounterKind, RoomCombatEncounterContext.Empty);
        }

        public void BeginRoom(
            RoomRuntimeRoot room,
            PlaceholderPlayerController player,
            bool alreadyCleared,
            RoomCombatEncounterKind encounterKind,
            RoomCombatEncounterContext encounterContext)
        {
            roomRuntimeRoot = room;
            playerController = player;
            activeEncounterContext = encounterContext ?? RoomCombatEncounterContext.Empty;
            if (roomRuntimeRoot == null || playerController == null)
            {
                return;
            }

            CleanupRoomCombatObjects();
            nextEnemyAttackBudgetTime = 0f;
            nextEnemyMeleeAttackBudgetTime = 0f;
            nextPlayerFootstepStimulusTime = 0f;
            hasLastPlayerFootstepStimulusLocalPosition = false;
            ConfigureRoomHazards();
            playerHealth = playerController.GetComponent<CombatantHealth>() ?? playerController.gameObject.AddComponent<CombatantHealth>();
            if (playerHealth.MaxHealth < PlayerMaxHealth)
            {
                playerHealth.Configure(PlayerMaxHealth);
            }

            var movement = playerController.GetComponent<PlayerMovementController>() ?? playerController.gameObject.AddComponent<PlayerMovementController>();
            movement.Configure(roomRuntimeRoot);

            var weapon = playerController.GetComponent<PlayerWeaponController>() ?? playerController.gameObject.AddComponent<PlayerWeaponController>();
            weapon.Configure(roomRuntimeRoot, this, projectilePrefab);
            weapon.ConfigureCombatFeel(CombatFeelProfile);
            var heldWeaponVisual = playerController.GetComponent<PlayerHeldWeaponVisualController>() ?? playerController.gameObject.AddComponent<PlayerHeldWeaponVisualController>();
            heldWeaponVisual.Bind(weapon);

            var defense = playerController.GetComponent<PlayerDefenseController>() ?? playerController.gameObject.AddComponent<PlayerDefenseController>();
            defense.Bind(roomRuntimeRoot, this);
            defense.ConfigureShieldProfile(ShieldGuardProfileDefinition.Resolve(null));
            var playerFeedback = playerController.GetComponent<PlayerDamageFeedbackController>() ?? playerController.gameObject.AddComponent<PlayerDamageFeedbackController>();
            playerFeedback.Configure(roomRuntimeRoot, CombatFeelProfile);
            PresentationPrefabResolver.InstantiateVisual(PresentationPrefabRole.Player, playerController.transform, Vector3.zero, Vector3.one);
            lastPlayerFootstepStimulusLocalPosition = playerController.transform.localPosition;
            hasLastPlayerFootstepStimulusLocalPosition = true;

            enemies.Clear();
            initialized = true;
            AttachHud();

            if (alreadyCleared)
            {
                ObjectiveState = RoomObjectiveState.Cleared;
                diagnostics.SetEnemyCounts(enemies);
                TintDoorsOnClear();
                return;
            }

            if (encounterKind == RoomCombatEncounterKind.Boss)
            {
                var boss = EnemySpawnService.SpawnBoss(roomRuntimeRoot, playerController.transform.parent, enemyPrefab, projectilePrefab, playerController, enemyCatalog, difficultyTier, diagnostics, bossCatalog, activeEncounterContext);
                if (boss != null)
                {
                    RegisterEnemy(boss);
                }
            }
            else
            {
                var spawnResult = EnemySpawnService.SpawnEnemies(new EnemySpawnRequest(
                    roomRuntimeRoot,
                    playerController.transform.parent,
                    enemyPrefab,
                    projectilePrefab,
                    playerController,
                    enemyCatalog,
                    difficultyTier,
                    diagnostics,
                    encounterContext));
                foreach (var enemy in spawnResult.Enemies)
                {
                    RegisterEnemy(enemy);
                }
            }

            ObjectiveState = RoomObjectiveState.InCombat;
            EvaluateRoomState();
        }

        public EnemyRuntimeController FindEnemyHit(Vector3 localPosition, float radius)
        {
            foreach (var enemy in enemies)
            {
                if (enemy == null || !enemy.IsAlive)
                {
                    continue;
                }

                var enemyPosition = enemy.transform.localPosition;
                enemyPosition.y = localPosition.y;
                if (Vector3.Distance(enemyPosition, localPosition) <= radius + 0.36f)
                {
                    return enemy;
                }
            }

            return null;
        }

        public DestructibleRoomObjectController FindDestructibleHit(Vector3 localPosition, float radius)
        {
            foreach (var roomObject in destructibleObjects)
            {
                if (roomObject == null || roomObject.IsDestroyed)
                {
                    continue;
                }

                var objectPosition = roomObject.transform.localPosition;
                objectPosition.y = localPosition.y;
                if (Vector3.Distance(objectPosition, localPosition) <= roomObject.RadiusMeters + radius)
                {
                    return roomObject;
                }
            }

            return null;
        }

        public CombatHudModel CreateHudModel()
        {
            return new CombatHudModel(
                playerHealth != null ? playerHealth.CurrentHealth : 0,
                playerHealth != null ? playerHealth.MaxHealth : PlayerMaxHealth,
                EnemiesRemaining(),
                ObjectiveState,
                difficultyTier != null ? difficultyTier.DisplayName : "Developer Sample",
                diagnostics.EnemySummary(),
                diagnostics.ProjectileSummary(),
                playerController != null ? playerController.GetComponent<PlayerDefenseController>() : null,
                activeEncounterContext);
        }

        public void EvaluateRoomState()
        {
            if (!initialized || ObjectiveState == RoomObjectiveState.Cleared)
            {
                return;
            }

            if (ignoreEnemiesForRoomClear)
            {
                ObjectiveState = RoomObjectiveState.Cleared;
                TintDoorsOnClear();
                diagnostics.SetEnemyCounts(enemies);
                return;
            }

            if (EnemiesRemaining() == 0)
            {
                ObjectiveState = RoomObjectiveState.Cleared;
                TintDoorsOnClear();
                VfxPresenter.Play(VfxCueId.RoomClear, roomRuntimeRoot.transform.position, roomRuntimeRoot.transform);
                AudioPresenter.Play(AudioCueId.RoomClear, roomRuntimeRoot.transform.position);
                RoomCleared?.Invoke(this);
            }

            diagnostics.SetEnemyCounts(enemies);
        }

        public int EnemiesRemaining()
        {
            if (ignoreEnemiesForRoomClear)
            {
                return 0;
            }

            return enemies.Count(enemy => enemy != null && enemy.IsAlive);
        }

        public bool TryReserveEnemyAttack(EnemyRuntimeController enemy, float timeSeconds)
        {
            if (enemy == null || enemy.BossDefinition != null)
            {
                return true;
            }

            if (timeSeconds < nextEnemyAttackBudgetTime)
            {
                return false;
            }

            var highestPriorityReadyEnemy = enemies
                .Where(candidate => candidate != null && candidate.CanStartBudgetedAttack(timeSeconds))
                .OrderByDescending(candidate => candidate.AttackPriorityScore(timeSeconds))
                .FirstOrDefault();
            if (highestPriorityReadyEnemy != null && highestPriorityReadyEnemy != enemy)
            {
                return false;
            }

            nextEnemyAttackBudgetTime = timeSeconds + EnemyAttackBudgetWindowSeconds;
            return true;
        }

        public bool TryReserveEnemyMeleeAttack(EnemyRuntimeController enemy, float timeSeconds)
        {
            if (enemy == null || enemy.BossDefinition != null)
            {
                return true;
            }

            if (timeSeconds < nextEnemyMeleeAttackBudgetTime)
            {
                return false;
            }

            var highestPriorityReadyEnemy = enemies
                .Where(candidate => candidate != null && candidate.CanStartBudgetedMeleeAttack(timeSeconds))
                .OrderByDescending(candidate => candidate.MeleeAttackPriorityScore(timeSeconds))
                .FirstOrDefault();
            if (highestPriorityReadyEnemy != null && highestPriorityReadyEnemy != enemy)
            {
                return false;
            }

            nextEnemyMeleeAttackBudgetTime = timeSeconds + EnemyMeleeAttackBudgetWindowSeconds;
            return true;
        }

        public static EnemyStimulusTier StimulusTierForPlayerAttack(AttackKind attackKind)
        {
            return attackKind == AttackKind.Heavy ? EnemyStimulusTier.Loud : EnemyStimulusTier.Normal;
        }

        public static EnemyStimulusTier DefaultStimulusTierFor(EnemyStimulusKind kind)
        {
            return EnemyStimulusTierExtensions.DefaultFor(kind);
        }

        public void EmitPlayerStimulus(EnemyStimulusKind kind, Vector3 localPosition, float timeSeconds)
        {
            EmitPlayerStimulus(kind, localPosition, timeSeconds, DefaultStimulusTierFor(kind), string.Empty);
        }

        public void EmitPlayerStimulus(EnemyStimulusKind kind, Vector3 localPosition, float timeSeconds, EnemyStimulusTier tier, string context = "")
        {
            foreach (var enemy in enemies)
            {
                if (enemy == null || !enemy.IsAlive || enemy.BossDefinition != null)
                {
                    continue;
                }

                enemy.ReceiveStimulus(kind, localPosition, timeSeconds, tier, context);
            }
        }

        public int EmitEnemyAllyAlert(
            EnemyRuntimeController source,
            Vector3 sourceLocalPosition,
            float radiusMeters,
            float timeSeconds,
            EnemyStimulusTier tier,
            string context = "")
        {
            if (source == null || source.BossDefinition != null || radiusMeters <= 0f)
            {
                return 0;
            }

            var recipients = 0;
            var safeRadius = Mathf.Max(0.1f, radiusMeters);
            var sourceFlat = sourceLocalPosition;
            sourceFlat.y = 0f;
            foreach (var enemy in enemies)
            {
                if (enemy == null ||
                    enemy == source ||
                    !enemy.IsAlive ||
                    enemy.ArchetypeId == EnemyArchetypeId.Boss ||
                    enemy.BossDefinition != null ||
                    enemy.AwarenessState == EnemyAwarenessState.Engaged)
                {
                    continue;
                }

                var targetFlat = enemy.transform.localPosition;
                targetFlat.y = 0f;
                if (Vector3.Distance(sourceFlat, targetFlat) > safeRadius)
                {
                    continue;
                }

                if (!enemy.CanReceiveStimulus(EnemyStimulusKind.AllyAlert, sourceLocalPosition, tier))
                {
                    continue;
                }

                var reason = string.IsNullOrWhiteSpace(context)
                    ? (source.Definition != null ? source.Definition.SpawnKind : source.BehaviorId.ToString())
                    : context;
                enemy.ReceiveStimulus(
                    EnemyStimulusKind.AllyAlert,
                    sourceLocalPosition,
                    timeSeconds,
                    tier,
                    $"ally_alert:{reason}");
                recipients++;
            }

            return recipients;
        }

        private void TickPlayerFootstepStimuli(float timeSeconds)
        {
            if (!initialized || ObjectiveState != RoomObjectiveState.InCombat || playerController == null)
            {
                return;
            }

            var currentPosition = playerController.transform.localPosition;
            if (!hasLastPlayerFootstepStimulusLocalPosition)
            {
                lastPlayerFootstepStimulusLocalPosition = currentPosition;
                hasLastPlayerFootstepStimulusLocalPosition = true;
                return;
            }

            var delta = currentPosition - lastPlayerFootstepStimulusLocalPosition;
            delta.y = 0f;
            if (delta.magnitude < PlayerFootstepMinimumDistanceMeters || timeSeconds < nextPlayerFootstepStimulusTime)
            {
                return;
            }

            EmitPlayerStimulus(EnemyStimulusKind.Footstep, currentPosition, timeSeconds, EnemyStimulusTier.Quiet, "footstep");
            lastPlayerFootstepStimulusLocalPosition = currentPosition;
            nextPlayerFootstepStimulusTime = timeSeconds + PlayerFootstepStimulusIntervalSeconds;
        }

        public bool TryGetEnemyIntelligenceSnapshot(
            int expectedSpawnCount,
            out IReadOnlyList<int> intelligenceLevels,
            out IReadOnlyList<string> dispositions)
        {
            intelligenceLevels = System.Array.Empty<int>();
            dispositions = System.Array.Empty<string>();
            if (expectedSpawnCount <= 0)
            {
                return false;
            }

            var orderedEnemies = enemies
                .Where(enemy => enemy != null && enemy.SpawnIndex >= 0)
                .GroupBy(enemy => enemy.SpawnIndex)
                .OrderBy(group => group.Key)
                .Select(group => group.First())
                .ToArray();
            if (orderedEnemies.Length != expectedSpawnCount ||
                orderedEnemies[0].SpawnIndex != 0 ||
                orderedEnemies[orderedEnemies.Length - 1].SpawnIndex != expectedSpawnCount - 1)
            {
                return false;
            }

            intelligenceLevels = orderedEnemies
                .Select(enemy => (int)enemy.Intelligence)
                .ToArray();
            dispositions = orderedEnemies
                .Select(enemy => enemy.Disposition.ToSaveString())
                .ToArray();
            return true;
        }

        private void RegisterEnemy(EnemyRuntimeController enemy)
        {
            if (enemy == null || enemies.Contains(enemy))
            {
                return;
            }

            enemy.SpawnedChild -= OnEnemySpawnedChild;
            enemy.SpawnedChild += OnEnemySpawnedChild;
            enemy.BindRoomCombatController(this);
            enemy.ConfigureCombatFeel(CombatFeelProfile);
            enemy.SetInspectionMode(inspectionMode);
            enemy.BeginEntryGrace(EntryGraceSeconds, Time.time);
            enemies.Add(enemy);
            diagnostics.SetEnemyCounts(enemies);
        }

        private void ConfigureRoomHazards()
        {
            hazards.Clear();
            destructibleObjects.Clear();
            var tuning = ResolveHazardTuningProfile();
            if (roomRuntimeRoot == null)
            {
                return;
            }

            foreach (var marker in roomRuntimeRoot.HazardMarkers)
            {
                if (marker == null || marker.HazardKind != RoomHazardKind.Spike)
                {
                    continue;
                }

                var spike = marker.GetComponent<SpikeHazardController>() ?? marker.gameObject.AddComponent<SpikeHazardController>();
                spike.Configure(marker, roomRuntimeRoot, this, playerController, tuning);
                hazards.Add(spike);
            }

            foreach (var marker in roomRuntimeRoot.InteractiveObjectMarkers)
            {
                if (marker == null || marker.IsDestroyed)
                {
                    continue;
                }

                var destructible = marker.ObjectKind == RoomInteractiveObjectKind.ExplosiveBarrel
                    ? marker.GetComponent<ExplosiveBarrelController>() ?? marker.gameObject.AddComponent<ExplosiveBarrelController>()
                    : marker.GetComponent<DestructibleRoomObjectController>() ?? marker.gameObject.AddComponent<DestructibleRoomObjectController>();
                destructible.Destroyed -= OnInteractiveObjectDestroyed;
                destructible.Destroyed += OnInteractiveObjectDestroyed;
                destructible.Configure(marker, roomRuntimeRoot, this, tuning);
                destructibleObjects.Add(destructible);
            }
        }

        private void OnInteractiveObjectDestroyed(DestructibleRoomObjectController _, RoomInteractiveObjectDestroyedContext context)
        {
            InteractiveObjectDestroyed?.Invoke(context);
        }

        private void OnEnemySpawnedChild(EnemyRuntimeController child)
        {
            RegisterEnemy(child);
        }

        private void ResolveReferences()
        {
            if (roomRuntimeRoot == null)
            {
                roomRuntimeRoot = GetComponentInChildren<RoomRuntimeRoot>(includeInactive: true) ?? FindAnyObjectByType<RoomRuntimeRoot>();
            }

            if (playerController == null)
            {
                playerController = GetComponentInChildren<PlaceholderPlayerController>(includeInactive: true) ?? FindAnyObjectByType<PlaceholderPlayerController>();
            }
        }

        private CombatFeelProfileDefinition ResolveCombatFeelProfile()
        {
            if (combatFeelProfile != null)
            {
                resolvedCombatFeelProfile = combatFeelProfile;
                return resolvedCombatFeelProfile;
            }

            resolvedCombatFeelProfile ??= CombatFeelProfileDefinition.Resolve(null);
            return resolvedCombatFeelProfile;
        }

        private RoomHazardTuningProfileDefinition ResolveHazardTuningProfile()
        {
            if (resolvedHazardTuningProfile == null)
            {
                resolvedHazardTuningProfile = RoomHazardTuningProfileDefinition.Resolve(hazardTuningProfile);
            }

            return resolvedHazardTuningProfile;
        }

        private void AttachHud()
        {
            var shellCanvas = GameObject.Find("PlatformShellCanvas");
            if (shellCanvas == null)
            {
                return;
            }

            var hud = shellCanvas.GetComponent<CombatHudController>() ?? shellCanvas.AddComponent<CombatHudController>();
            hud.Bind(this);
            var bossHud = shellCanvas.GetComponent<BossHudController>() ?? shellCanvas.AddComponent<BossHudController>();
            bossHud.Bind(this);
        }

        private void CleanupRoomCombatObjects()
        {
            foreach (var enemy in enemies)
            {
                if (enemy != null)
                {
                    DestroyRuntimeObject(enemy.gameObject);
                }
            }

            enemies.Clear();
            var parent = playerController != null ? playerController.transform.parent : transform;
            if (parent == null)
            {
                return;
            }

            foreach (var projectile in parent.GetComponentsInChildren<ProjectileController>(includeInactive: true))
            {
                if (projectile != null && projectile.gameObject.activeInHierarchy)
                {
                    DestroyRuntimeObject(projectile.gameObject);
                }
            }

            foreach (var projectile in parent.GetComponentsInChildren<EnemyProjectileController>(includeInactive: true))
            {
                if (projectile != null && projectile.gameObject.activeInHierarchy)
                {
                    DestroyRuntimeObject(projectile.gameObject);
                }
            }
        }

        private static void DestroyRuntimeObject(GameObject target)
        {
            if (target == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(target);
            }
            else
            {
                DestroyImmediate(target);
            }
        }

        private void TintDoorsOnClear()
        {
            foreach (var port in roomRuntimeRoot.DoorPorts)
            {
                roomRuntimeRoot.SetDoorVisualStateById(port.Id, RoomDoorVisualState.Cleared);
            }

            foreach (var renderer in roomRuntimeRoot.GetComponentsInChildren<Renderer>())
            {
                if (!renderer.gameObject.name.StartsWith("doorAnchorActive.", System.StringComparison.Ordinal))
                {
                    continue;
                }

                renderer.sharedMaterial = MaterialResolver.Resolve(MaterialRole.DoorCleared);
            }
        }
    }
}
