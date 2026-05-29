using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Hollow.Core.Diagnostics;
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
            var anchors = (request.SpawnAnchors != null && request.SpawnAnchors.Count > 0
                    ? request.SpawnAnchors
                    : request.Room.EnemySpawns)
                .OrderBy(spawn => spawn.id)
                .ToArray();
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

                var enemyObject = UnityEngine.Object.Instantiate(request.EnemyPrefab, request.Parent);
                enemyObject.name = $"Enemy.{definition.ArchetypeId}.{spawn.id}";
                enemyObject.SetActive(true);
                enemyObject.transform.localPosition = spawn.position.ToUnityVector3();

                var enemy = enemyObject.GetComponent<EnemyRuntimeController>() ?? enemyObject.AddComponent<EnemyRuntimeController>();
                enemy.Configure(request.Room, request.Player, definition, difficulty);
                if (request.EncounterContext != null &&
                    request.EncounterContext.TryGetEnemyIntelligenceOverride(index, definition.Intelligence, definition.Disposition, out var intelligence, out var disposition))
                {
                    enemy.ApplyIntelligenceDisposition(intelligence, disposition);
                }

                enemy.ConfigureSpawnContext(request.EnemyPrefab, request.EnemyProjectilePrefab, catalog, difficulty, request.Diagnostics, index);
                enemies.Add(enemy);
            }

            request.Diagnostics?.SetEnemyCounts(enemies);
            return new EnemySpawnResult(enemies, warnings);
        }

        public static IEnumerator SpawnEnemiesStaged(
            EnemySpawnRequest request,
            Action<EnemyRuntimeController> onEnemySpawned,
            int maxEnemiesPerFrame = 2)
        {
            if (request.Room?.EnemySpawns == null || request.EnemyPrefab == null || request.Parent == null)
            {
                yield break;
            }

            var catalog = request.Catalog != null ? request.Catalog : EnemyCatalog.CreateRuntimeDefault();
            var difficulty = request.DifficultyTier != null ? request.DifficultyTier : DifficultyTierDefinition.CreateRuntimeDeveloperSample();
            var anchors = (request.SpawnAnchors != null && request.SpawnAnchors.Count > 0
                    ? request.SpawnAnchors
                    : request.Room.EnemySpawns)
                .OrderBy(spawn => spawn.id)
                .ToArray();
            var assignedSpawnKinds = request.EncounterContext?.EnemySpawnKinds ?? System.Array.Empty<string>();
            var spawnCount = assignedSpawnKinds.Count > 0 ? Mathf.Min(anchors.Length, assignedSpawnKinds.Count) : anchors.Length;
            var budget = Mathf.Max(1, maxEnemiesPerFrame);
            var spawnedThisFrame = 0;
            var spawnedEnemies = new List<EnemyRuntimeController>(spawnCount);

            for (var index = 0; index < spawnCount; index++)
            {
                var spawn = anchors[index];
                var spawnKind = assignedSpawnKinds.Count > 0 ? assignedSpawnKinds[index] : spawn.kind;
                var definition = EnemyDefinitionResolver.Resolve(catalog, spawnKind, out var usedFallback);
                if (usedFallback)
                {
                    Debug.LogWarning($"Unknown enemy spawn kind '{spawnKind}', using {definition.SpawnKind}.");
                }

                var enemyObject = UnityEngine.Object.Instantiate(request.EnemyPrefab, request.Parent);
                enemyObject.name = $"Enemy.{definition.ArchetypeId}.{spawn.id}";
                enemyObject.SetActive(true);
                enemyObject.transform.localPosition = spawn.position.ToUnityVector3();

                var enemy = enemyObject.GetComponent<EnemyRuntimeController>() ?? enemyObject.AddComponent<EnemyRuntimeController>();
                enemy.enabled = false;
                enemy.Configure(request.Room, request.Player, definition, difficulty);
                if (request.EncounterContext != null &&
                    request.EncounterContext.TryGetEnemyIntelligenceOverride(index, definition.Intelligence, definition.Disposition, out var intelligence, out var disposition))
                {
                    enemy.ApplyIntelligenceDisposition(intelligence, disposition);
                }

                enemy.ConfigureSpawnContext(request.EnemyPrefab, request.EnemyProjectilePrefab, catalog, difficulty, request.Diagnostics, index);
                spawnedEnemies.Add(enemy);
                onEnemySpawned?.Invoke(enemy);
                spawnedThisFrame++;
                if (spawnedThisFrame >= budget)
                {
                    M136PerformanceOperationCounters.ReportEnemySpawnSlice();
                    spawnedThisFrame = 0;
                    yield return null;
                }
            }

            if (spawnedThisFrame > 0)
            {
                M136PerformanceOperationCounters.ReportEnemySpawnSlice();
            }

            for (var index = 0; index < spawnedEnemies.Count; index++)
            {
                if (spawnedEnemies[index] != null)
                {
                    spawnedEnemies[index].enabled = true;
                }
            }
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
                var enemyObject = UnityEngine.Object.Instantiate(enemyPrefab, parent);
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
            return SpawnBoss(room, parent, enemyPrefab, enemyProjectilePrefab, player, catalog, difficultyTier, diagnostics, null, null);
        }

        public static EnemyRuntimeController SpawnBoss(
            RoomRuntimeRoot room,
            Transform parent,
            GameObject enemyPrefab,
            GameObject enemyProjectilePrefab,
            PlaceholderPlayerController player,
            EnemyCatalog catalog,
            DifficultyTierDefinition difficultyTier,
            CombatDiagnosticsModel diagnostics,
            BossCatalogDefinition bossCatalog,
            RoomCombatEncounterContext encounterContext)
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

            var enemyObject = UnityEngine.Object.Instantiate(enemyPrefab, parent);
            enemyObject.name = "Enemy.Boss.StoneWarden";
            enemyObject.SetActive(true);
            var safeStart = room.LastBuiltAsset?.SafeStart?.position?.ToUnityVector3() ?? Vector3.zero;
            enemyObject.transform.localPosition = RoomLocalCollision.ResolveMoveIgnoringObstacles(room, safeStart + new Vector3(0f, 0f, 1.4f), definition.RadiusMeters);

            var enemy = enemyObject.GetComponent<EnemyRuntimeController>() ?? enemyObject.AddComponent<EnemyRuntimeController>();
            enemy.Configure(room, player, definition, difficultyTier);
            enemy.ConfigureSpawnContext(enemyPrefab, enemyProjectilePrefab, catalog, difficultyTier, diagnostics);
            var resolvedBossCatalog = bossCatalog != null ? bossCatalog : BossCatalogDefinition.CreateRuntimeDefault();
            var bossDefinition = encounterContext != null &&
                                 !string.IsNullOrWhiteSpace(encounterContext.BossId) &&
                                 resolvedBossCatalog.TryGetBoss(encounterContext.BossId, out var assignedBoss)
                ? assignedBoss
                : resolvedBossCatalog.FallbackBoss;
            enemy.ConfigureBoss(bossDefinition);
            diagnostics?.SetEnemyCounts(new EnemyRuntimeController[] { enemy });
            return enemy;
        }

        public static IEnumerator SpawnBossStaged(
            RoomRuntimeRoot room,
            Transform parent,
            GameObject enemyPrefab,
            GameObject enemyProjectilePrefab,
            PlaceholderPlayerController player,
            EnemyCatalog catalog,
            DifficultyTierDefinition difficultyTier,
            CombatDiagnosticsModel diagnostics,
            BossCatalogDefinition bossCatalog,
            RoomCombatEncounterContext encounterContext,
            Action<EnemyRuntimeController> onBossSpawned)
        {
            if (room == null || enemyPrefab == null || parent == null)
            {
                yield break;
            }

            var definition = EnemyDefinitionResolver.Resolve(catalog, "spawnEnemyBoss", out var usedFallback);
            if (usedFallback)
            {
                definition = EnemyDefinition.CreateRuntimeBoss();
            }

            var enemyObject = UnityEngine.Object.Instantiate(enemyPrefab, parent);
            enemyObject.name = "Enemy.Boss.StoneWarden";
            enemyObject.SetActive(true);
            var safeStart = room.LastBuiltAsset?.SafeStart?.position?.ToUnityVector3() ?? Vector3.zero;
            enemyObject.transform.localPosition = RoomLocalCollision.ResolveMoveIgnoringObstacles(room, safeStart + new Vector3(0f, 0f, 1.4f), definition.RadiusMeters);
            M136PerformanceOperationCounters.ReportBossActivationSlice();
            yield return null;

            var enemy = enemyObject.GetComponent<EnemyRuntimeController>() ?? enemyObject.AddComponent<EnemyRuntimeController>();
            enemy.enabled = false;
            enemy.Configure(room, player, definition, difficultyTier);
            enemy.ConfigureSpawnContext(enemyPrefab, enemyProjectilePrefab, catalog, difficultyTier, diagnostics);
            M136PerformanceOperationCounters.ReportBossActivationSlice();
            yield return null;

            var resolvedBossCatalog = bossCatalog != null ? bossCatalog : BossCatalogDefinition.CreateRuntimeDefault();
            var bossDefinition = encounterContext != null &&
                                 !string.IsNullOrWhiteSpace(encounterContext.BossId) &&
                                 resolvedBossCatalog.TryGetBoss(encounterContext.BossId, out var assignedBoss)
                ? assignedBoss
                : resolvedBossCatalog.FallbackBoss;
            enemy.ConfigureBoss(bossDefinition);
            diagnostics?.SetEnemyCounts(new EnemyRuntimeController[] { enemy });
            onBossSpawned?.Invoke(enemy);
            enemy.enabled = true;
            M136PerformanceOperationCounters.ReportBossActivationSlice();
        }
    }
}
