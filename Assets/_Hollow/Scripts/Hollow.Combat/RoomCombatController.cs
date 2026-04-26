using System.Collections.Generic;
using System.Linq;
using System;
using Hollow.Entities;
using Hollow.Rooms;
using UnityEngine;

namespace Hollow.Combat
{
    public sealed class RoomCombatController : MonoBehaviour
    {
        public const int PlayerMaxHealth = 6;

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
            roomRuntimeRoot = room;
            playerController = player;
            if (roomRuntimeRoot == null || playerController == null)
            {
                return;
            }

            CleanupRoomCombatObjects();
            playerHealth = playerController.GetComponent<CombatantHealth>() ?? playerController.gameObject.AddComponent<CombatantHealth>();
            if (playerHealth.MaxHealth != PlayerMaxHealth)
            {
                playerHealth.Configure(PlayerMaxHealth);
            }

            var movement = playerController.GetComponent<PlayerMovementController>() ?? playerController.gameObject.AddComponent<PlayerMovementController>();
            movement.Configure(roomRuntimeRoot);

            var weapon = playerController.GetComponent<PlayerWeaponController>() ?? playerController.gameObject.AddComponent<PlayerWeaponController>();
            weapon.Configure(roomRuntimeRoot, this, projectilePrefab);

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

            var spawnResult = EnemySpawnService.SpawnEnemies(new EnemySpawnRequest(
                roomRuntimeRoot,
                playerController.transform.parent,
                enemyPrefab,
                playerController,
                enemyCatalog,
                difficultyTier,
                diagnostics));
            enemies.AddRange(spawnResult.Enemies);

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
                diagnostics.ProjectileSummary());
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
                RoomCleared?.Invoke(this);
            }

            diagnostics.SetEnemyCounts(enemies);
        }

        public int EnemiesRemaining()
        {
            return enemies.Count(enemy => enemy != null && enemy.IsAlive);
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
            foreach (var renderer in roomRuntimeRoot.GetComponentsInChildren<Renderer>())
            {
                if (!renderer.gameObject.name.StartsWith("doorAnchorActive.", System.StringComparison.Ordinal))
                {
                    continue;
                }

                renderer.sharedMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"))
                {
                    color = new Color(0.25f, 1f, 0.45f, 1f)
                };
            }
        }
    }
}
