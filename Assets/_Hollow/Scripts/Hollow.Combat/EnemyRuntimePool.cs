using System;
using System.Collections;
using System.Collections.Generic;
using Hollow.Core.Diagnostics;
using UnityEngine;

namespace Hollow.Combat
{
    [Serializable]
    public sealed class EnemyRuntimePoolSnapshot
    {
        public string branchKey;
        public int poolKeyCount;
        public int pooledInactiveCount;
        public int rentedCount;
        public int activeLeakCount;
        public int warmRequests;
        public int warmCompletions;
        public int rents;
        public int returns;
        public int misses;
        public int hardInstantiates;
    }

    public static class EnemyRuntimePool
    {
        private sealed class Pool
        {
            public readonly Stack<EnemyRuntimeController> Enemies = new();
            public readonly HashSet<EnemyRuntimeController> Rented = new();
        }

        private static readonly Dictionary<string, Pool> Pools = new(StringComparer.Ordinal);
        private static readonly Dictionary<EnemyRuntimeController, string> KeyByEnemy = new();

        public static string KeyFor(string branchKey, GameObject prefab, string spawnKind, DifficultyTierDefinition difficultyTier)
        {
            var prefabKey = prefab != null ? $"{prefab.name}:{prefab.GetInstanceID()}" : "prefab:null";
            var difficultyKey = difficultyTier != null ? $"{difficultyTier.name}:{difficultyTier.GetInstanceID()}" : "difficulty:default";
            return $"{Normalize(branchKey)}|{prefabKey}|spawn:{spawnKind ?? string.Empty}|{difficultyKey}";
        }

        public static IEnumerator Warm(string key, GameObject prefab, int count, int perFrame = 2)
        {
            if (string.IsNullOrWhiteSpace(key) || prefab == null || count <= 0 || !Application.isPlaying)
            {
                yield break;
            }

            M136PerformanceOperationCounters.ReportEnemyPoolWarmRequest();
            var pool = GetPool(key);
            var budget = Mathf.Max(1, perFrame);
            var created = 0;
            while (created < count)
            {
                var slice = Mathf.Min(budget, count - created);
                for (var index = 0; index < slice; index++)
                {
                    var enemy = CreateEnemy(prefab, key);
                    enemy.PrepareForBranchPoolReturn();
                    pool.Enemies.Push(enemy);
                    created++;
                }

                yield return null;
            }

            M136PerformanceOperationCounters.ReportEnemyPoolWarmCompletion();
        }

        public static EnemyRuntimeController Rent(string key, GameObject prefab, Transform parent)
        {
            if (string.IsNullOrWhiteSpace(key) || prefab == null)
            {
                return null;
            }

            if (!Application.isPlaying)
            {
                M136PerformanceOperationCounters.ReportEnemyPoolHardInstantiate();
                var immediate = UnityEngine.Object.Instantiate(prefab, parent);
                return immediate.GetComponent<EnemyRuntimeController>() ?? immediate.AddComponent<EnemyRuntimeController>();
            }

            var pool = GetPool(key);
            EnemyRuntimeController enemy = null;
            while (pool.Enemies.Count > 0 && enemy == null)
            {
                enemy = pool.Enemies.Pop();
            }

            if (enemy == null)
            {
                enemy = CreateEnemy(prefab, key);
                M136PerformanceOperationCounters.ReportEnemyPoolMiss();
                M136PerformanceOperationCounters.ReportEnemyPoolHardInstantiate();
            }
            else if (!enemy.ValidateBranchPoolReturnState(out _))
            {
                M136PerformanceOperationCounters.ReportM139StaleEnemyStateFailure();
            }

            enemy.transform.SetParent(parent, worldPositionStays: false);
            enemy.gameObject.SetActive(true);
            enemy.enabled = true;
            enemy.MarkBranchPooled(key);
            pool.Rented.Add(enemy);
            M136PerformanceOperationCounters.ReportEnemyPoolRent();
            return enemy;
        }

        public static bool TryReturn(EnemyRuntimeController enemy)
        {
            if (enemy == null || !Application.isPlaying || !KeyByEnemy.TryGetValue(enemy, out var key) || string.IsNullOrWhiteSpace(key))
            {
                return false;
            }

            if (enemy.BossDefinition != null)
            {
                return false;
            }

            var pool = GetPool(key);
            pool.Rented.Remove(enemy);
            enemy.PrepareForBranchPoolReturn();
            if (!enemy.ValidateBranchPoolReturnState(out _))
            {
                M136PerformanceOperationCounters.ReportM139StaleEnemyStateFailure();
            }

            pool.Enemies.Push(enemy);
            M136PerformanceOperationCounters.ReportEnemyPoolReturn();
            return true;
        }

        public static void ClearBranch(string branchKey)
        {
            branchKey = Normalize(branchKey);
            if (string.IsNullOrWhiteSpace(branchKey))
            {
                return;
            }

            var keys = new List<string>();
            foreach (var key in Pools.Keys)
            {
                if (key.StartsWith(branchKey, StringComparison.Ordinal))
                {
                    keys.Add(key);
                }
            }

            for (var index = 0; index < keys.Count; index++)
            {
                if (!Pools.TryGetValue(keys[index], out var pool))
                {
                    continue;
                }

                while (pool.Enemies.Count > 0)
                {
                    var enemy = pool.Enemies.Pop();
                    if (enemy != null)
                    {
                        KeyByEnemy.Remove(enemy);
                        UnityEngine.Object.Destroy(enemy.gameObject);
                    }
                }

                if (pool.Rented.Count > 0)
                {
                    M136PerformanceOperationCounters.ReportM139PoolActiveLeak(pool.Rented.Count);
                }

                Pools.Remove(keys[index]);
            }
        }

        public static void ValidateRentedEnemy(EnemyRuntimeController enemy)
        {
            if (enemy == null || !enemy.IsBranchPooledEnemy)
            {
                return;
            }

            if (!enemy.ValidateBranchPoolRentState(out _))
            {
                M136PerformanceOperationCounters.ReportM139StaleEnemyStateFailure();
            }
        }

        public static EnemyRuntimePoolSnapshot Snapshot(string branchKey = null)
        {
            var normalizedBranchKey = string.IsNullOrWhiteSpace(branchKey) ? string.Empty : Normalize(branchKey);
            var poolKeys = 0;
            var inactive = 0;
            var rented = 0;
            var activeLeaks = 0;
            foreach (var pair in Pools)
            {
                if (!string.IsNullOrWhiteSpace(normalizedBranchKey) &&
                    !pair.Key.StartsWith(normalizedBranchKey, StringComparison.Ordinal))
                {
                    continue;
                }

                poolKeys++;
                foreach (var enemy in pair.Value.Enemies)
                {
                    if (enemy != null)
                    {
                        inactive++;
                    }
                }

                foreach (var enemy in pair.Value.Rented)
                {
                    if (enemy == null)
                    {
                        continue;
                    }

                    rented++;
                    if (enemy.gameObject.activeInHierarchy)
                    {
                        activeLeaks++;
                    }
                }
            }

            var counters = M136PerformanceOperationCounters.Snapshot();
            return new EnemyRuntimePoolSnapshot
            {
                branchKey = normalizedBranchKey,
                poolKeyCount = poolKeys,
                pooledInactiveCount = inactive,
                rentedCount = rented,
                activeLeakCount = activeLeaks,
                warmRequests = counters.EnemyPoolWarmRequests,
                warmCompletions = counters.EnemyPoolWarmCompletions,
                rents = counters.EnemyPoolRents,
                returns = counters.EnemyPoolReturns,
                misses = counters.EnemyPoolMisses,
                hardInstantiates = counters.EnemyPoolHardInstantiates
            };
        }

        private static EnemyRuntimeController CreateEnemy(GameObject prefab, string key)
        {
            var enemyObject = UnityEngine.Object.Instantiate(prefab);
            var enemy = enemyObject.GetComponent<EnemyRuntimeController>() ?? enemyObject.AddComponent<EnemyRuntimeController>();
            KeyByEnemy[enemy] = key;
            enemy.MarkBranchPooled(key);
            return enemy;
        }

        private static Pool GetPool(string key)
        {
            if (!Pools.TryGetValue(key, out var pool))
            {
                pool = new Pool();
                Pools[key] = pool;
            }

            return pool;
        }

        private static string Normalize(string key)
        {
            return string.IsNullOrWhiteSpace(key) ? "branch:none" : key;
        }
    }
}
