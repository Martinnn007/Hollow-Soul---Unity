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

        [SerializeField] private GameObject enemyPrefab;
        [SerializeField] private GameObject projectilePrefab;
        [SerializeField] private EnemyCatalog enemyCatalog;
        [SerializeField] private DifficultyTierDefinition difficultyTier;
        [SerializeField] private RoomRuntimeRoot roomRuntimeRoot;
        [SerializeField] private PlaceholderPlayerController playerController;

        private readonly List<EnemyRuntimeController> enemies = new();
        private CombatantHealth playerHealth;
        private bool initialized;
        private readonly CombatDiagnosticsModel diagnostics = new();

        public event Action<RoomCombatController> RoomCleared;

        public RoomObjectiveState ObjectiveState { get; private set; } = RoomObjectiveState.WaitingToStart;

        public GameObject EnemyPrefab => enemyPrefab;

        public GameObject ProjectilePrefab => projectilePrefab;

        public EnemyCatalog EnemyCatalog => enemyCatalog;

        public DifficultyTierDefinition DifficultyTier => difficultyTier;

        public CombatDiagnosticsModel Diagnostics => diagnostics;

        public IReadOnlyList<EnemyRuntimeController> Enemies => enemies;

        public CombatantHealth PlayerHealth => playerHealth;

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

        private void Start()
        {
            InitializeCombat();
        }

        private void Update()
        {
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
            if (roomRuntimeRoot == null || playerController == null)
            {
                return;
            }

            CleanupRoomCombatObjects();
            playerHealth = playerController.GetComponent<CombatantHealth>() ?? playerController.gameObject.AddComponent<CombatantHealth>();
            if (playerHealth.MaxHealth < PlayerMaxHealth)
            {
                playerHealth.Configure(PlayerMaxHealth);
            }

            var movement = playerController.GetComponent<PlayerMovementController>() ?? playerController.gameObject.AddComponent<PlayerMovementController>();
            movement.Configure(roomRuntimeRoot);

            var weapon = playerController.GetComponent<PlayerWeaponController>() ?? playerController.gameObject.AddComponent<PlayerWeaponController>();
            weapon.Configure(roomRuntimeRoot, this, projectilePrefab);

            var defense = playerController.GetComponent<PlayerDefenseController>() ?? playerController.gameObject.AddComponent<PlayerDefenseController>();
            defense.Bind(roomRuntimeRoot);
            PresentationPrefabResolver.InstantiateVisual(PresentationPrefabRole.Player, playerController.transform, Vector3.zero, Vector3.one);

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
                var boss = EnemySpawnService.SpawnBoss(roomRuntimeRoot, playerController.transform.parent, enemyPrefab, projectilePrefab, playerController, enemyCatalog, difficultyTier, diagnostics);
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
                playerController != null ? playerController.GetComponent<PlayerDefenseController>() : null);
        }

        public void EvaluateRoomState()
        {
            if (!initialized || ObjectiveState == RoomObjectiveState.Cleared)
            {
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
            return enemies.Count(enemy => enemy != null && enemy.IsAlive);
        }

        private void RegisterEnemy(EnemyRuntimeController enemy)
        {
            if (enemy == null || enemies.Contains(enemy))
            {
                return;
            }

            enemy.SpawnedChild -= OnEnemySpawnedChild;
            enemy.SpawnedChild += OnEnemySpawnedChild;
            enemy.BeginEntryGrace(EntryGraceSeconds, Time.time);
            enemies.Add(enemy);
            diagnostics.SetEnemyCounts(enemies);
        }

        private void OnEnemySpawnedChild(EnemyRuntimeController child)
        {
            RegisterEnemy(child);
        }

        private void ResolveReferences()
        {
            if (roomRuntimeRoot == null)
            {
                roomRuntimeRoot = GetComponentInChildren<RoomRuntimeRoot>(includeInactive: true) ?? FindFirstObjectByType<RoomRuntimeRoot>();
            }

            if (playerController == null)
            {
                playerController = GetComponentInChildren<PlaceholderPlayerController>(includeInactive: true) ?? FindFirstObjectByType<PlaceholderPlayerController>();
            }
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
