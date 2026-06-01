using System.Collections.Generic;
using System;
using System.Collections;
using Hollow.Core.Diagnostics;
using Hollow.Data.Definitions;
using Hollow.Entities;
using Hollow.Presentation;
using Hollow.Rooms;
using Unity.Profiling;
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
        [SerializeField] private bool autoInitialize = true;

        private readonly List<EnemyRuntimeController> enemies = new();
        private readonly Dictionary<CombatantHealth, EnemyRuntimeController> enemyByHealth = new();
        private readonly List<RoomHazardController> hazards = new();
        private readonly List<DestructibleRoomObjectController> destructibleObjects = new();
        private readonly RoomThreatDirector threatDirector = new();
        private readonly RoomTacticalDirector tacticalDirector = new();
        private CombatantHealth playerHealth;
        private CombatFeelProfileDefinition resolvedCombatFeelProfile;
        private RoomHazardTuningProfileDefinition resolvedHazardTuningProfile;
        private RoomCombatEncounterContext activeEncounterContext = RoomCombatEncounterContext.Empty;
        private RoomCombatEncounterContext waveSourceEncounterContext = RoomCombatEncounterContext.Empty;
        private RoomWaveEncounterPlan activeWavePlan = RoomWaveEncounterPlan.Empty;
        private int activeWaveIndex = -1;
        private bool initialized;
        private InspectionEntityMode inspectionMode = InspectionEntityMode.LiveRuntime;
        private bool ignoreEnemiesForRoomClear;
        private readonly CombatDiagnosticsModel diagnostics = new();
        private string runtimeStatusOverride = string.Empty;
        private float nextEnemyAttackBudgetTime;
        private float nextEnemyMeleeAttackBudgetTime;
        private float nextPlayerFootstepStimulusTime;
        private float nextTacticalDirectorTickTime;
        private Vector3 lastPlayerFootstepStimulusLocalPosition;
        private bool hasLastPlayerFootstepStimulusLocalPosition;
        private PlayerDefenseController playerDefenseController;
        private PlayerWeaponController playerWeaponController;
        private bool transitionSuspended;
        private string branchEnemyPoolKey = string.Empty;

        public event Action<RoomCombatController> RoomCleared;

        public event Action<EnemyRuntimeController> EnemyDefeated;

        public event Action<RoomInteractiveObjectDestroyedContext> InteractiveObjectDestroyed;

        public RoomObjectiveState ObjectiveState { get; private set; } = RoomObjectiveState.WaitingToStart;

        public GameObject EnemyPrefab => enemyPrefab;

        public GameObject ProjectilePrefab => projectilePrefab;

        public EnemyCatalog EnemyCatalog => enemyCatalog;

        public BossCatalogDefinition BossCatalog => bossCatalog;

        public DifficultyTierDefinition DifficultyTier => difficultyTier;

        public CombatFeelProfileDefinition CombatFeelProfile => ResolveCombatFeelProfile();

        public CombatDiagnosticsModel Diagnostics => diagnostics;

        public RoomThreatDirector ThreatDirector => threatDirector;

        public RoomTacticalDirector TacticalDirector => tacticalDirector;

        public IReadOnlyList<EnemyRuntimeController> Enemies => enemies;

        public EnemyRuntimeController ActiveBoss
        {
            get
            {
                for (var index = 0; index < enemies.Count; index++)
                {
                    var enemy = enemies[index];
                    if (enemy != null && enemy.IsAlive && enemy.BossDefinition != null)
                    {
                        return enemy;
                    }
                }

                return null;
            }
        }

        public IReadOnlyList<DestructibleRoomObjectController> DestructibleObjects => destructibleObjects;

        public CombatantHealth PlayerHealth => playerHealth;

        public PlaceholderPlayerController PlayerController => playerController;

        public InspectionEntityMode InspectionMode => inspectionMode;

        public bool IgnoresEnemiesForRoomClear => ignoreEnemiesForRoomClear;

        public bool AutoInitialize => autoInitialize;

        public bool TransitionSuspended => transitionSuspended;

        public int LivingNonBossEnemyCountForAiBudget => LivingNonBossEnemyCount();

        public bool IsWaveEncounterActive => activeWavePlan.IsActive;

        public int CurrentWaveNumber => activeWavePlan.IsActive ? Mathf.Clamp(activeWaveIndex + 1, 1, activeWavePlan.TotalWaves) : 0;

        public int TotalWaveCount => activeWavePlan.TotalWaves;

        public string CurrentWaveStatusText => activeWavePlan.StatusTextForWave(activeWaveIndex);

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

        public void ConfigureAutoInitialize(bool enabled)
        {
            autoInitialize = enabled;
        }

        public void SetTransitionSuspended(bool suspended)
        {
            transitionSuspended = suspended;
        }

        public void ConfigureBranchEnemyPool(string poolKey)
        {
            branchEnemyPoolKey = poolKey ?? string.Empty;
        }

        private void Start()
        {
            if (autoInitialize)
            {
                InitializeCombat();
            }
        }

        private void Update()
        {
            if (transitionSuspended)
            {
                return;
            }

            TickPlayerFootstepStimuli(Time.time);
            var now = Time.time;
            if (now >= nextTacticalDirectorTickTime)
            {
                var stageStarted = BeginCpuStage(out var stageStartingGc);
                M136PerformanceOperationCounters.ReportActiveEnemyCount(EnemiesRemaining());
                threatDirector.Tick(enemies);
                EnemyAiDebugOverlay.ReportRoomEnemyCount(LivingNonBossEnemyCount());
                EnemyAiDebugOverlay.ReportRoomPressure(
                    threatDirector.MeleePressure,
                    threatDirector.RangedPressure,
                    threatDirector.AreaPressure,
                    threatDirector.ChargePressure);
                tacticalDirector.Tick(enemies, roomRuntimeRoot, playerController, now);
                M136PerformanceOperationCounters.ReportTacticalDirectorTick();
                EndCpuStage(M136CpuStageKind.TacticalDirector, stageStarted, stageStartingGc);
                nextTacticalDirectorTickTime = now + ResolveTacticalDirectorTickIntervalSeconds();
            }

            EvaluateRoomState();
        }

        private float ResolveTacticalDirectorTickIntervalSeconds()
        {
            return ActiveBoss != null
                ? M137PerformanceComfortPolicy.M3BossRoomTacticalDirectorMinTickIntervalSeconds
                : M137PerformanceComfortPolicy.TacticalDirectorMinTickIntervalSeconds;
        }

        private static float BeginCpuStage(out long startingGc)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            startingGc = 0;
            return Time.realtimeSinceStartup;
#else
            startingGc = 0;
            return 0f;
#endif
        }

        private static void EndCpuStage(M136CpuStageKind stage, float startedRealtime, long startingGc)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            var elapsedMilliseconds = Mathf.Max(0f, (Time.realtimeSinceStartup - startedRealtime) * 1000f);
            M136PerformanceOperationCounters.ReportCpuStage(stage, elapsedMilliseconds, 0L);
#endif
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

            if (!roomRuntimeRoot.HasNavMeshBake)
            {
                Debug.LogError(
                    $"Cannot begin room '{roomRuntimeRoot.LastBuiltAsset?.Id ?? "<unknown>"}': Unity NavMesh data is unavailable ({roomRuntimeRoot.NavMeshBakeError}). Run {RoomNavMeshCatalogDefinition.PreferredBakeMenuPath}; Designer Room and Arena playtests may use dev-only fallback, but authored runtime rooms require a catalog bake.",
                    roomRuntimeRoot);
                initialized = false;
                ObjectiveState = RoomObjectiveState.WaitingToStart;
                return;
            }

            CleanupRoomCombatObjects();
            nextEnemyAttackBudgetTime = 0f;
            nextEnemyMeleeAttackBudgetTime = 0f;
            activeWavePlan = encounterKind == RoomCombatEncounterKind.Wave
                ? RoomWaveEncounterPlan.Create(activeEncounterContext)
                : RoomWaveEncounterPlan.Empty;
            activeWaveIndex = -1;
            runtimeStatusOverride = string.Empty;
            waveSourceEncounterContext = activeEncounterContext;
            threatDirector.Reset();
            tacticalDirector.Reset();
            nextTacticalDirectorTickTime = 0f;
            nextPlayerFootstepStimulusTime = 0f;
            hasLastPlayerFootstepStimulusLocalPosition = false;
            ConfigureRoomHazards();
            playerHealth = playerController.GetComponent<CombatantHealth>() ?? playerController.gameObject.AddComponent<CombatantHealth>();
            if (playerHealth.MaxHealth < PlayerMaxHealth)
            {
                playerHealth.Configure(PlayerMaxHealth);
            }

            var movement = playerController.GetComponent<PlayerMovementController>() ?? playerController.gameObject.AddComponent<PlayerMovementController>();
            movement.Configure(roomRuntimeRoot, this);

            playerWeaponController = playerController.GetComponent<PlayerWeaponController>() ?? playerController.gameObject.AddComponent<PlayerWeaponController>();
            playerWeaponController.Configure(roomRuntimeRoot, this, projectilePrefab);
            playerWeaponController.ConfigureCombatFeel(CombatFeelProfile);
            var aimLock = playerController.GetComponent<PlayerAimLockController>() ?? playerController.gameObject.AddComponent<PlayerAimLockController>();
            aimLock.Configure(this);
            var locomotionAnimator = playerController.GetComponent<PlayerLocomotionAnimator>();
            locomotionAnimator?.BindGameplay(playerWeaponController, playerHealth, aimLock);
            var heldWeaponVisual = playerController.GetComponent<PlayerHeldWeaponVisualController>() ?? playerController.gameObject.AddComponent<PlayerHeldWeaponVisualController>();
            heldWeaponVisual.Bind(playerWeaponController);
            var rollVisual = playerController.GetComponent<PlayerRollVisualController>() ?? playerController.gameObject.AddComponent<PlayerRollVisualController>();
            rollVisual.Bind(playerWeaponController);

            playerDefenseController = playerController.GetComponent<PlayerDefenseController>() ?? playerController.gameObject.AddComponent<PlayerDefenseController>();
            playerDefenseController.Bind(roomRuntimeRoot, this);
            playerDefenseController.ConfigureShieldProfile(ShieldGuardProfileDefinition.Resolve(null));
            var playerFeedback = playerController.GetComponent<PlayerDamageFeedbackController>() ?? playerController.gameObject.AddComponent<PlayerDamageFeedbackController>();
            playerFeedback.Configure(roomRuntimeRoot, CombatFeelProfile);
            if (HasMeshyPlayerVisual(playerController.transform))
            {
                RemoveLegacyPlayerPresentationVisuals(playerController.transform);
                aimLock.BindPresentation(null);
            }
            else
            {
                var playerVisual = PresentationPrefabResolver.InstantiateVisual(PresentationPrefabRole.Player, playerController.transform, Vector3.zero, Vector3.one);
                aimLock.BindPresentation(playerVisual);
            }

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
                using (M137PerformanceProfilerMarkers.BossSpawnActivate.Auto())
                {
                    var boss = EnemySpawnService.SpawnBoss(roomRuntimeRoot, playerController.transform.parent, enemyPrefab, projectilePrefab, playerController, enemyCatalog, difficultyTier, diagnostics, bossCatalog, activeEncounterContext);
                    if (boss != null)
                    {
                        RegisterEnemy(boss);
                    }
                }
            }
            else if (encounterKind == RoomCombatEncounterKind.Wave)
            {
                SpawnNextWave();
            }
            else
            {
                SpawnEnemiesForContext(encounterContext);
            }

            ObjectiveState = RoomObjectiveState.InCombat;
            EvaluateRoomState();
        }

        public IEnumerator BeginRoomStaged(
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
                yield break;
            }

            if (!roomRuntimeRoot.HasNavMeshBake)
            {
                Debug.LogError(
                    $"Cannot begin room '{roomRuntimeRoot.LastBuiltAsset?.Id ?? "<unknown>"}': Unity NavMesh data is unavailable ({roomRuntimeRoot.NavMeshBakeError}). Run {RoomNavMeshCatalogDefinition.PreferredBakeMenuPath}; Designer Room and Arena playtests may use dev-only fallback, but authored runtime rooms require a catalog bake.",
                    roomRuntimeRoot);
                initialized = false;
                ObjectiveState = RoomObjectiveState.WaitingToStart;
                yield break;
            }

            CleanupRoomCombatObjects();
            nextEnemyAttackBudgetTime = 0f;
            nextEnemyMeleeAttackBudgetTime = 0f;
            activeWavePlan = encounterKind == RoomCombatEncounterKind.Wave
                ? RoomWaveEncounterPlan.Create(activeEncounterContext)
                : RoomWaveEncounterPlan.Empty;
            activeWaveIndex = -1;
            runtimeStatusOverride = string.Empty;
            waveSourceEncounterContext = activeEncounterContext;
            threatDirector.Reset();
            tacticalDirector.Reset();
            nextTacticalDirectorTickTime = 0f;
            nextPlayerFootstepStimulusTime = 0f;
            hasLastPlayerFootstepStimulusLocalPosition = false;
            ConfigureRoomHazards();
            yield return null;

            playerHealth = playerController.GetComponent<CombatantHealth>() ?? playerController.gameObject.AddComponent<CombatantHealth>();
            if (playerHealth.MaxHealth < PlayerMaxHealth)
            {
                playerHealth.Configure(PlayerMaxHealth);
            }

            var movement = playerController.GetComponent<PlayerMovementController>() ?? playerController.gameObject.AddComponent<PlayerMovementController>();
            movement.Configure(roomRuntimeRoot, this);
            playerWeaponController = playerController.GetComponent<PlayerWeaponController>() ?? playerController.gameObject.AddComponent<PlayerWeaponController>();
            playerWeaponController.Configure(roomRuntimeRoot, this, projectilePrefab);
            playerWeaponController.ConfigureCombatFeel(CombatFeelProfile);
            var aimLock = playerController.GetComponent<PlayerAimLockController>() ?? playerController.gameObject.AddComponent<PlayerAimLockController>();
            aimLock.Configure(this);
            var locomotionAnimator = playerController.GetComponent<PlayerLocomotionAnimator>();
            locomotionAnimator?.BindGameplay(playerWeaponController, playerHealth, aimLock);
            var heldWeaponVisual = playerController.GetComponent<PlayerHeldWeaponVisualController>() ?? playerController.gameObject.AddComponent<PlayerHeldWeaponVisualController>();
            heldWeaponVisual.Bind(playerWeaponController);
            var rollVisual = playerController.GetComponent<PlayerRollVisualController>() ?? playerController.gameObject.AddComponent<PlayerRollVisualController>();
            rollVisual.Bind(playerWeaponController);
            playerDefenseController = playerController.GetComponent<PlayerDefenseController>() ?? playerController.gameObject.AddComponent<PlayerDefenseController>();
            playerDefenseController.Bind(roomRuntimeRoot, this);
            playerDefenseController.ConfigureShieldProfile(ShieldGuardProfileDefinition.Resolve(null));
            var playerFeedback = playerController.GetComponent<PlayerDamageFeedbackController>() ?? playerController.gameObject.AddComponent<PlayerDamageFeedbackController>();
            playerFeedback.Configure(roomRuntimeRoot, CombatFeelProfile);
            yield return null;

            if (HasMeshyPlayerVisual(playerController.transform))
            {
                RemoveLegacyPlayerPresentationVisuals(playerController.transform);
                aimLock.BindPresentation(null);
            }
            else
            {
                var playerVisual = PresentationPrefabResolver.InstantiateVisual(PresentationPrefabRole.Player, playerController.transform, Vector3.zero, Vector3.one);
                aimLock.BindPresentation(playerVisual);
            }

            lastPlayerFootstepStimulusLocalPosition = playerController.transform.localPosition;
            hasLastPlayerFootstepStimulusLocalPosition = true;
            enemies.Clear();
            initialized = true;
            AttachHud();
            yield return null;

            if (alreadyCleared)
            {
                ObjectiveState = RoomObjectiveState.Cleared;
                diagnostics.SetEnemyCounts(enemies);
                TintDoorsOnClear();
                yield break;
            }

            if (encounterKind == RoomCombatEncounterKind.Boss)
            {
                yield return RunProfiledStage(
                    M137PerformanceProfilerMarkers.BossSpawnActivate,
                    EnemySpawnService.SpawnBossStaged(
                        roomRuntimeRoot,
                        playerController.transform.parent,
                        enemyPrefab,
                        projectilePrefab,
                        playerController,
                        enemyCatalog,
                        difficultyTier,
                        diagnostics,
                        bossCatalog,
                        activeEncounterContext,
                        RegisterEnemy,
                        activateOnComplete: false));
            }
            else if (encounterKind == RoomCombatEncounterKind.Wave)
            {
                if (activeWavePlan.IsActive &&
                    activeWavePlan.TryCreateContextForWave(activeWaveIndex + 1, waveSourceEncounterContext, out var waveContext))
                {
                    activeWaveIndex++;
                    activeEncounterContext = waveContext;
                    yield return SpawnEnemiesForContextStaged(waveContext);
                }
            }
            else
            {
                yield return SpawnEnemiesForContextStaged(encounterContext);
            }

            ObjectiveState = RoomObjectiveState.InCombat;
            EvaluateRoomState();
        }

        public void ActivateStagedEnemiesForReveal()
        {
            for (var index = 0; index < enemies.Count; index++)
            {
                var enemy = enemies[index];
                if (enemy == null)
                {
                    continue;
                }

                enemy.gameObject.SetActive(true);
                enemy.enabled = true;
            }
        }

        private static IEnumerator RunProfiledStage(ProfilerMarker marker, IEnumerator stage)
        {
            while (stage != null)
            {
                bool hasNext;
                using (marker.Auto())
                {
                    hasNext = stage.MoveNext();
                }

                if (!hasNext)
                {
                    yield break;
                }

                yield return stage.Current;
            }
        }

        public IReadOnlyList<EnemyRuntimeController> SpawnAdditionalEnemies(
            IReadOnlyList<ImportedSpawnPoint> spawnAnchors,
            RoomCombatEncounterContext encounterContext)
        {
            ResolveReferences();
            if (roomRuntimeRoot == null ||
                playerController == null ||
                spawnAnchors == null ||
                spawnAnchors.Count == 0)
            {
                return System.Array.Empty<EnemyRuntimeController>();
            }

            initialized = true;
            activeEncounterContext = encounterContext ?? activeEncounterContext ?? RoomCombatEncounterContext.Empty;
            var spawnResult = EnemySpawnService.SpawnEnemies(new EnemySpawnRequest(
                roomRuntimeRoot,
                playerController.transform.parent,
                enemyPrefab,
                projectilePrefab,
                playerController,
                enemyCatalog,
                difficultyTier,
                diagnostics,
                activeEncounterContext,
                spawnAnchors,
                branchEnemyPoolKey));
            foreach (var enemy in spawnResult.Enemies)
            {
                RegisterEnemy(enemy);
            }

            ObjectiveState = RoomObjectiveState.InCombat;
            diagnostics.SetEnemyCounts(enemies);
            return spawnResult.Enemies;
        }

        public void RegisterRuntimeEnemy(EnemyRuntimeController enemy)
        {
            RegisterEnemy(enemy);
        }

        public void SetRuntimeStatusOverride(string statusText)
        {
            runtimeStatusOverride = statusText ?? string.Empty;
        }

        public void ClearRuntimeStatusOverride()
        {
            runtimeStatusOverride = string.Empty;
        }

        public void ForceClearRoomWithoutReward()
        {
            if (!initialized)
            {
                return;
            }

            CleanupRoomCombatObjects();
            activeWavePlan = RoomWaveEncounterPlan.Empty;
            activeWaveIndex = -1;
            runtimeStatusOverride = string.Empty;
            ObjectiveState = RoomObjectiveState.Cleared;
            TintDoorsOnClear();
            diagnostics.SetEnemyCounts(enemies);
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
            M136PerformanceOperationCounters.ReportCombatHudModelBuild();
            return new CombatHudModel(
                playerHealth != null ? playerHealth.CurrentHealth : 0,
                playerHealth != null ? playerHealth.MaxHealth : PlayerMaxHealth,
                EnemiesRemaining(),
                ObjectiveState,
                difficultyTier != null ? difficultyTier.DisplayName : "Developer Sample",
                diagnostics.EnemySummary(),
                diagnostics.ProjectileSummary(),
                playerDefenseController,
                activeEncounterContext,
                playerWeaponController,
                string.IsNullOrWhiteSpace(runtimeStatusOverride) ? CurrentWaveStatusText : runtimeStatusOverride);
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
                if (activeWavePlan.IsActive && activeWaveIndex + 1 < activeWavePlan.TotalWaves)
                {
                    SpawnNextWave();
                    diagnostics.SetEnemyCounts(enemies);
                    return;
                }

                ObjectiveState = RoomObjectiveState.Cleared;
                activeWavePlan = RoomWaveEncounterPlan.Empty;
                activeWaveIndex = -1;
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

            var count = 0;
            for (var index = 0; index < enemies.Count; index++)
            {
                var enemy = enemies[index];
                if (enemy != null && enemy.IsAlive)
                {
                    count++;
                }
            }

            return count;
        }

        private void SpawnNextWave()
        {
            if (!activeWavePlan.IsActive ||
                !activeWavePlan.TryCreateContextForWave(activeWaveIndex + 1, waveSourceEncounterContext, out var waveContext))
            {
                return;
            }

            activeWaveIndex++;
            activeEncounterContext = waveContext;
            SpawnEnemiesForContext(waveContext);
        }

        private IReadOnlyList<EnemyRuntimeController> SpawnEnemiesForContext(RoomCombatEncounterContext encounterContext)
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
                encounterContext,
                null,
                branchEnemyPoolKey));
            foreach (var enemy in spawnResult.Enemies)
            {
                RegisterEnemy(enemy);
            }

            return spawnResult.Enemies;
        }

        private IEnumerator SpawnEnemiesForContextStaged(RoomCombatEncounterContext encounterContext)
        {
            yield return EnemySpawnService.SpawnEnemiesStaged(new EnemySpawnRequest(
                roomRuntimeRoot,
                playerController.transform.parent,
                enemyPrefab,
                projectilePrefab,
                playerController,
                enemyCatalog,
                difficultyTier,
                diagnostics,
                encounterContext,
                null,
                branchEnemyPoolKey),
                RegisterEnemy,
                maxEnemiesPerFrame: 2,
                activateOnComplete: false);
            diagnostics.SetEnemyCounts(enemies);
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

            var highestPriorityReadyEnemy = HighestPriorityBudgetedAttackEnemy(timeSeconds, melee: false);
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

            var highestPriorityReadyEnemy = HighestPriorityBudgetedAttackEnemy(timeSeconds, melee: true);
            if (highestPriorityReadyEnemy != null && highestPriorityReadyEnemy != enemy)
            {
                return false;
            }

            nextEnemyMeleeAttackBudgetTime = timeSeconds + EnemyMeleeAttackBudgetWindowSeconds;
            return true;
        }

        private int LivingNonBossEnemyCount()
        {
            var count = 0;
            for (var index = 0; index < enemies.Count; index++)
            {
                var enemy = enemies[index];
                if (enemy != null && enemy.IsAlive && enemy.BossDefinition == null)
                {
                    count++;
                }
            }

            return count;
        }

        private EnemyRuntimeController HighestPriorityBudgetedAttackEnemy(float timeSeconds, bool melee)
        {
            EnemyRuntimeController best = null;
            var bestScore = float.NegativeInfinity;
            for (var index = 0; index < enemies.Count; index++)
            {
                var candidate = enemies[index];
                if (candidate == null)
                {
                    continue;
                }

                var canStart = melee
                    ? candidate.CanStartBudgetedMeleeAttack(timeSeconds)
                    : candidate.CanStartBudgetedAttack(timeSeconds);
                if (!canStart)
                {
                    continue;
                }

                var score = melee
                    ? candidate.MeleeAttackPriorityScore(timeSeconds)
                    : candidate.AttackPriorityScore(timeSeconds);
                if (score <= bestScore)
                {
                    continue;
                }

                bestScore = score;
                best = candidate;
            }

            return best;
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

            var orderedEnemies = new EnemyRuntimeController[expectedSpawnCount];
            for (var index = 0; index < enemies.Count; index++)
            {
                var enemy = enemies[index];
                if (enemy == null || enemy.SpawnIndex < 0 || enemy.SpawnIndex >= expectedSpawnCount)
                {
                    continue;
                }

                orderedEnemies[enemy.SpawnIndex] ??= enemy;
            }

            for (var index = 0; index < orderedEnemies.Length; index++)
            {
                if (orderedEnemies[index] == null)
                {
                    return false;
                }
            }

            var intelligenceSnapshot = new int[orderedEnemies.Length];
            var dispositionSnapshot = new string[orderedEnemies.Length];
            for (var index = 0; index < orderedEnemies.Length; index++)
            {
                intelligenceSnapshot[index] = (int)orderedEnemies[index].Intelligence;
                dispositionSnapshot[index] = orderedEnemies[index].Disposition.ToSaveString();
            }

            intelligenceLevels = intelligenceSnapshot;
            dispositions = dispositionSnapshot;
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
            if (enemy.Health != null)
            {
                enemy.Health.Died -= OnEnemyDied;
                enemy.Health.Died += OnEnemyDied;
                enemyByHealth[enemy.Health] = enemy;
            }

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

        private void OnEnemyDied(CombatantHealth health)
        {
            if (health == null || !enemyByHealth.TryGetValue(health, out var enemy))
            {
                return;
            }

            health.Died -= OnEnemyDied;
            enemyByHealth.Remove(health);
            EnemyDefeated?.Invoke(enemy);
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
                    enemy.SpawnedChild -= OnEnemySpawnedChild;
                    if (enemy.Health != null)
                    {
                        enemy.Health.Died -= OnEnemyDied;
                    }

                    if (!EnemyRuntimePool.TryReturn(enemy))
                    {
                        DestroyRuntimeObject(enemy.gameObject);
                    }
                }
            }

            enemies.Clear();
            enemyByHealth.Clear();
            var parent = playerController != null ? playerController.transform.parent : transform;
            if (parent == null)
            {
                return;
            }

            foreach (var projectile in parent.GetComponentsInChildren<ProjectileController>(includeInactive: true))
            {
                if (projectile != null && projectile.gameObject.activeInHierarchy)
                {
                    Hollow.Core.HollowRuntimePool.Return(projectile.gameObject);
                }
            }

            foreach (var projectile in parent.GetComponentsInChildren<EnemyProjectileController>(includeInactive: true))
            {
                if (projectile != null && projectile.gameObject.activeInHierarchy)
                {
                    Hollow.Core.HollowRuntimePool.Return(projectile.gameObject);
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

        private static bool HasMeshyPlayerVisual(Transform playerRoot)
        {
            return playerRoot != null &&
                (playerRoot.Find("MainCharacter_VisualRoot") != null ||
                 playerRoot.GetComponent<PlayerLocomotionAnimator>() != null);
        }

        private static void RemoveLegacyPlayerPresentationVisuals(Transform playerRoot)
        {
            if (playerRoot == null)
            {
                return;
            }

            var meshyRoot = playerRoot.Find("MainCharacter_VisualRoot");
            var legacyVisuals = playerRoot.GetComponentsInChildren<PresentationVisualMarker>(includeInactive: true);
            var removedVisuals = new HashSet<GameObject>();
            for (var index = 0; index < legacyVisuals.Length; index++)
            {
                var marker = legacyVisuals[index];
                if (marker == null ||
                    marker.Role != PresentationPrefabRole.Player ||
                    marker.transform == playerRoot ||
                    IsChildOf(marker.transform, meshyRoot) ||
                    marker.gameObject == null ||
                    !removedVisuals.Add(marker.gameObject))
                {
                    continue;
                }

                DestroyRuntimeObject(marker.gameObject);
            }
        }

        private static bool IsChildOf(Transform candidate, Transform parent)
        {
            if (candidate == null || parent == null)
            {
                return false;
            }

            var current = candidate;
            while (current != null)
            {
                if (current == parent)
                {
                    return true;
                }

                current = current.parent;
            }

            return false;
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
