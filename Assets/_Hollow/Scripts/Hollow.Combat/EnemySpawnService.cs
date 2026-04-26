using System.Collections.Generic;
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

            foreach (var spawn in request.Room.EnemySpawns)
            {
                var definition = EnemyDefinitionResolver.Resolve(catalog, spawn.kind, out var usedFallback);
                if (usedFallback)
                {
                    var warning = $"Unknown enemy spawn kind '{spawn.kind}', using {definition.SpawnKind}.";
                    warnings.Add(warning);
                    Debug.LogWarning(warning);
                }

                var enemyObject = Object.Instantiate(request.EnemyPrefab, request.Parent);
                enemyObject.name = $"Enemy.{definition.ArchetypeId}.{spawn.id}";
                enemyObject.SetActive(true);
                enemyObject.transform.localPosition = spawn.position.ToUnityVector3();

                var enemy = enemyObject.GetComponent<EnemyRuntimeController>() ?? enemyObject.AddComponent<EnemyRuntimeController>();
                enemy.Configure(request.Room, request.Player, definition, difficulty);
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
    }
}
