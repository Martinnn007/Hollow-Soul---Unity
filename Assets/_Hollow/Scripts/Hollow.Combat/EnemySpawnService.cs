using System.Collections.Generic;
using System.Linq;
using Hollow.Entities;
using Hollow.Rooms;
using UnityEngine;

namespace Hollow.Combat
{
    public static class EnemySpawnService
    {
        public static EnemySpawnResult SpawnEnemies(EnemySpawnRequest request)
        {
            var enemies = new List<EnemyRuntimeController>();
            var warnings = new List<string>();
            if (request.Room?.EnemySpawns == null || request.EnemyPrefab == null || request.Parent == null)
            {
                return new EnemySpawnResult(enemies, warnings);
            }

            var catalog = request.Catalog != null ? request.Catalog : EnemyCatalog.CreateRuntimeDefault();
            var difficulty = request.DifficultyTier != null ? request.DifficultyTier : DifficultyTierDefinition.CreateRuntimeDeveloperSample();
            var anchors = request.Room.EnemySpawns.OrderBy(spawn => spawn.id).ToArray();
            var assignedSpawnKinds = request.EncounterContext?.EnemySpawnKinds ?? System.Array.Empty<string>();
            var spawnCount = assignedSpawnKinds.Count > 0 ? Mathf.Min(anchors.Length, assignedSpawnKinds.Count) : anchors.Length;

            for (var index = 0; index < spawnCount; index++)
            {
                var spawn = anchors[index];
                var spawnKind = assignedSpawnKinds.Count > 0 ? assignedSpawnKinds[index] : spawn.kind;
                var definition = EnemyDefinitionResolver.Resolve(catalog, spawnKind, out var usedFallback);
                if (usedFallback)
                {
                    var warning = $"Unknown enemy spawn kind '{spawnKind}', using {definition.SpawnKind}.";
                    warnings.Add(warning);
                    Debug.LogWarning(warning);
                }

                var enemyObject = Object.Instantiate(request.EnemyPrefab, request.Parent);
                enemyObject.name = $"Enemy.{definition.ArchetypeId}.{spawn.id}";
                enemyObject.SetActive(true);
                enemyObject.transform.localPosition = spawn.position.ToUnityVector3();

                var enemy = enemyObject.GetComponent<EnemyRuntimeController>() ?? enemyObject.AddComponent<EnemyRuntimeController>();
                enemy.Configure(request.Room, request.Player, definition, difficulty);
                enemy.ConfigureSpawnContext(request.EnemyPrefab, request.EnemyProjectilePrefab, catalog, difficulty, request.Diagnostics);
                enemies.Add(enemy);
            }

            request.Diagnostics?.SetEnemyCounts(enemies);
            return new EnemySpawnResult(enemies, warnings);
        }

        public static IReadOnlyList<ChaserEnemyController> SpawnChasers(
            RoomRuntimeRoot room,
            Transform parent,
            GameObject enemyPrefab,
            PlaceholderPlayerController player)
        {
            var enemies = new List<ChaserEnemyController>();
            if (room?.EnemySpawns == null || enemyPrefab == null || parent == null)
            {
                return enemies;
            }

            foreach (var spawn in room.EnemySpawns)
            {
                var enemyObject = Object.Instantiate(enemyPrefab, parent);
                enemyObject.name = $"ChaserEnemy.{spawn.id}";
                enemyObject.SetActive(true);
                enemyObject.transform.localPosition = spawn.position.ToUnityVector3();
                var enemy = enemyObject.GetComponent<ChaserEnemyController>() ?? enemyObject.AddComponent<ChaserEnemyController>();
                enemy.Configure(room, player);
                enemies.Add(enemy);
            }

            return enemies;
        }

        public static EnemyRuntimeController SpawnBoss(
            RoomRuntimeRoot room,
            Transform parent,
            GameObject enemyPrefab,
            GameObject enemyProjectilePrefab,
            PlaceholderPlayerController player,
            EnemyCatalog catalog,
            DifficultyTierDefinition difficultyTier,
            CombatDiagnosticsModel diagnostics)
        {
            if (room == null || enemyPrefab == null || parent == null)
            {
                return null;
            }

            var definition = EnemyDefinitionResolver.Resolve(catalog, "spawnEnemyBoss", out var usedFallback);
            if (usedFallback)
            {
                definition = EnemyDefinition.CreateRuntimeBoss();
            }

            var enemyObject = Object.Instantiate(enemyPrefab, parent);
            enemyObject.name = "Enemy.Boss.StoneWarden";
            enemyObject.SetActive(true);
            var safeStart = room.LastBuiltAsset?.SafeStart?.position?.ToUnityVector3() ?? Vector3.zero;
            enemyObject.transform.localPosition = RoomLocalCollision.ResolveMoveIgnoringObstacles(room, safeStart + new Vector3(0f, 0f, 1.4f), definition.RadiusMeters);

            var enemy = enemyObject.GetComponent<EnemyRuntimeController>() ?? enemyObject.AddComponent<EnemyRuntimeController>();
            enemy.Configure(room, player, definition, difficultyTier);
            enemy.ConfigureSpawnContext(enemyPrefab, enemyProjectilePrefab, catalog, difficultyTier, diagnostics);
            diagnostics?.SetEnemyCounts(new[] { enemy });
            return enemy;
        }
    }
}
