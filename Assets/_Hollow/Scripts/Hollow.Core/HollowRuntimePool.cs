using System;
using System.Collections;
using System.Collections.Generic;
using Hollow.Core.Diagnostics;
using UnityEngine;

namespace Hollow.Core
{
    [Serializable]
    public sealed class HollowRuntimePoolSnapshot
    {
        public int prefabPoolCount;
        public int generatedPoolCount;
        public int pooledInactiveCount;
        public int activeTrackedCount;
        public int activeLeakCount;
        public int rents;
        public int returns;
        public int misses;
        public int hardInstantiates;
        public int warmRequests;
        public int warmCompletions;
        public int warmVisibleObjects;
        public int warmRootActiveErrors;
        public int warmActiveLeaks;
        public string[] recentMissKeys = Array.Empty<string>();
    }

    public interface IPooledRuntimeObject
    {
        void OnRentFromPool();

        void OnReturnToPool();
    }

    public static class HollowRuntimePool
    {
        private sealed class RuntimePoolRunner : MonoBehaviour
        {
        }

        private static readonly Dictionary<GameObject, Stack<GameObject>> PrefabPools = new();
        private static readonly Dictionary<string, Stack<GameObject>> GeneratedPools = new(StringComparer.Ordinal);
        private static readonly Dictionary<GameObject, GameObject> PrefabByInstance = new();
        private static readonly Dictionary<GameObject, string> GeneratedKeyByInstance = new();
        private static readonly Dictionary<string, int> MissesByKey = new(StringComparer.Ordinal);
        private static readonly HashSet<GameObject> ActiveInstances = new();
        private static RuntimePoolRunner runner;
        private static Transform warmRoot;
        private const string WarmRootName = "HollowRuntimePool.HiddenWarmRoot";

        public static bool IsWarmRootVisibleForDiagnostics => warmRoot != null && warmRoot.gameObject.activeInHierarchy;

        public static GameObject Rent(GameObject prefab, Transform parent)
        {
            if (prefab == null)
            {
                return null;
            }

            if (!Application.isPlaying)
            {
                M136PerformanceOperationCounters.ReportRuntimePoolHardInstantiate();
                return UnityEngine.Object.Instantiate(prefab, parent);
            }

            if (!PrefabPools.TryGetValue(prefab, out var pool))
            {
                pool = new Stack<GameObject>();
                PrefabPools[prefab] = pool;
            }

            GameObject instance = null;
            while (pool.Count > 0 && instance == null)
            {
                instance = pool.Pop();
            }

            if (instance == null)
            {
                instance = UnityEngine.Object.Instantiate(prefab, parent);
                PrefabByInstance[instance] = prefab;
                RecordMissKey($"prefab:{prefab.name}");
                M136PerformanceOperationCounters.ReportRuntimePoolMiss();
                M136PerformanceOperationCounters.ReportRuntimePoolHardInstantiate();
            }
            else
            {
                instance.transform.SetParent(parent, worldPositionStays: false);
            }

            PrepareRentedInstance(instance);
            return instance;
        }

        public static GameObject RentGenerated(string key, Transform parent, Func<GameObject> factory)
        {
            if (string.IsNullOrWhiteSpace(key) || factory == null)
            {
                return null;
            }

            if (!Application.isPlaying)
            {
                M136PerformanceOperationCounters.ReportRuntimePoolHardInstantiate();
                var immediate = factory();
                immediate?.transform.SetParent(parent, worldPositionStays: false);
                return immediate;
            }

            if (!GeneratedPools.TryGetValue(key, out var pool))
            {
                pool = new Stack<GameObject>();
                GeneratedPools[key] = pool;
            }

            GameObject instance = null;
            while (pool.Count > 0 && instance == null)
            {
                instance = pool.Pop();
            }

            if (instance == null)
            {
                instance = factory();
                if (instance == null)
                {
                    return null;
                }

                GeneratedKeyByInstance[instance] = key;
                RecordMissKey($"generated:{key}");
                M136PerformanceOperationCounters.ReportRuntimePoolMiss();
                M136PerformanceOperationCounters.ReportRuntimePoolHardInstantiate();
            }

            instance.transform.SetParent(parent, worldPositionStays: false);
            PrepareRentedInstance(instance);
            return instance;
        }

        public static GameObject RentPrimitive(string key, PrimitiveType primitiveType, Transform parent)
        {
            return RentGenerated(key, parent, () => GameObject.CreatePrimitive(primitiveType));
        }

        public static IEnumerator WarmPrefabPool(GameObject prefab, int count, int perFrame = 4)
        {
            if (prefab == null || count <= 0 || !Application.isPlaying)
            {
                yield break;
            }

            M136PerformanceOperationCounters.ReportRuntimePoolWarmRequest();
            var warmed = 0;
            var budget = Mathf.Max(1, perFrame);
            var rented = new List<GameObject>(count);
            var hiddenRoot = HiddenWarmRoot();
            while (warmed < count)
            {
                var slice = Mathf.Min(budget, count - warmed);
                for (var index = 0; index < slice; index++)
                {
                    var instance = Rent(prefab, hiddenRoot);
                    if (instance != null)
                    {
                        instance.transform.SetParent(hiddenRoot, worldPositionStays: false);
                        rented.Add(instance);
                    }

                    warmed++;
                }

                ReportVisibleWarmObjects(rented, hiddenRoot);
                yield return null;
            }

            for (var index = 0; index < rented.Count; index++)
            {
                Return(rented[index]);
                if ((index + 1) % budget == 0)
                {
                    yield return null;
                }
            }

            ReportWarmReturnLeaks(rented);
            M136PerformanceOperationCounters.ReportRuntimePoolWarmCompletion();
        }

        public static IEnumerator WarmGeneratedPool(string key, int count, Func<GameObject> factory, int perFrame = 4)
        {
            if (string.IsNullOrWhiteSpace(key) || factory == null || count <= 0 || !Application.isPlaying)
            {
                yield break;
            }

            M136PerformanceOperationCounters.ReportRuntimePoolWarmRequest();
            var warmed = 0;
            var budget = Mathf.Max(1, perFrame);
            var rented = new List<GameObject>(count);
            var hiddenRoot = HiddenWarmRoot();
            while (warmed < count)
            {
                var slice = Mathf.Min(budget, count - warmed);
                for (var index = 0; index < slice; index++)
                {
                    var instance = RentGenerated(key, hiddenRoot, factory);
                    if (instance != null)
                    {
                        instance.transform.SetParent(hiddenRoot, worldPositionStays: false);
                        rented.Add(instance);
                    }

                    warmed++;
                }

                ReportVisibleWarmObjects(rented, hiddenRoot);
                yield return null;
            }

            for (var index = 0; index < rented.Count; index++)
            {
                Return(rented[index]);
                if ((index + 1) % budget == 0)
                {
                    yield return null;
                }
            }

            ReportWarmReturnLeaks(rented);
            M136PerformanceOperationCounters.ReportRuntimePoolWarmCompletion();
        }

        public static IEnumerator WarmPrimitivePool(string key, PrimitiveType primitiveType, int count, int perFrame = 4)
        {
            yield return WarmGeneratedPool(key, count, () => GameObject.CreatePrimitive(primitiveType), perFrame);
        }

        private static Transform HiddenWarmRoot()
        {
            if (warmRoot != null)
            {
                if (warmRoot.gameObject.activeSelf || warmRoot.gameObject.activeInHierarchy)
                {
                    M136PerformanceOperationCounters.ReportPoolWarmRootActiveError();
                    warmRoot.gameObject.SetActive(false);
                }

                return warmRoot;
            }

            var root = new GameObject(WarmRootName);
            root.hideFlags = HideFlags.HideAndDontSave;
            root.SetActive(false);
            if (Application.isPlaying)
            {
                UnityEngine.Object.DontDestroyOnLoad(root);
            }

            warmRoot = root.transform;
            return warmRoot;
        }

        private static void ReportVisibleWarmObjects(IReadOnlyList<GameObject> warmedObjects, Transform hiddenRoot)
        {
            if (hiddenRoot == null)
            {
                return;
            }

            if (hiddenRoot.gameObject.activeSelf || hiddenRoot.gameObject.activeInHierarchy)
            {
                M136PerformanceOperationCounters.ReportPoolWarmRootActiveError();
                hiddenRoot.gameObject.SetActive(false);
            }

            var visible = 0;
            for (var index = 0; index < warmedObjects.Count; index++)
            {
                var instance = warmedObjects[index];
                if (instance == null)
                {
                    continue;
                }

                if (instance.activeInHierarchy)
                {
                    visible++;
                    continue;
                }

                var renderers = instance.GetComponentsInChildren<Renderer>(includeInactive: false);
                for (var rendererIndex = 0; rendererIndex < renderers.Length; rendererIndex++)
                {
                    if (renderers[rendererIndex] != null && renderers[rendererIndex].enabled && renderers[rendererIndex].isVisible)
                    {
                        visible++;
                        break;
                    }
                }
            }

            if (visible > 0)
            {
                M136PerformanceOperationCounters.ReportPoolWarmVisibleObject(visible);
            }
        }

        private static void ReportWarmReturnLeaks(IReadOnlyList<GameObject> warmedObjects)
        {
            var activeLeaks = 0;
            for (var index = 0; index < warmedObjects.Count; index++)
            {
                var instance = warmedObjects[index];
                if (instance != null && instance.activeInHierarchy)
                {
                    activeLeaks++;
                }
            }

            if (activeLeaks > 0)
            {
                M136PerformanceOperationCounters.ReportPoolWarmActiveLeak(activeLeaks);
            }
        }

        public static void ResetDiagnostics()
        {
            MissesByKey.Clear();
        }

        public static void Return(GameObject instance)
        {
            if (instance == null)
            {
                return;
            }

            if (!Application.isPlaying)
            {
                UnityEngine.Object.DestroyImmediate(instance);
                return;
            }

            NotifyReturn(instance);
            ActiveInstances.Remove(instance);
            instance.SetActive(false);
            instance.transform.SetParent(null, worldPositionStays: false);
            if (PrefabByInstance.TryGetValue(instance, out var prefab) && prefab != null)
            {
                PushPrefab(instance, prefab);
                return;
            }

            if (GeneratedKeyByInstance.TryGetValue(instance, out var key))
            {
                PushGenerated(instance, key);
                return;
            }

            UnityEngine.Object.Destroy(instance);
        }

        public static void ReturnAfter(GameObject instance, float delaySeconds)
        {
            if (instance == null || !Application.isPlaying)
            {
                return;
            }

            Runner().StartCoroutine(ReturnAfterRoutine(instance, Mathf.Max(0f, delaySeconds)));
        }

        private static IEnumerator ReturnAfterRoutine(GameObject instance, float delaySeconds)
        {
            yield return new WaitForSeconds(delaySeconds);
            Return(instance);
        }

        private static void PushPrefab(GameObject instance, GameObject prefab)
        {
            if (!PrefabPools.TryGetValue(prefab, out var pool))
            {
                pool = new Stack<GameObject>();
                PrefabPools[prefab] = pool;
            }

            pool.Push(instance);
            M136PerformanceOperationCounters.ReportRuntimePoolReturn();
        }

        private static void PushGenerated(GameObject instance, string key)
        {
            if (!GeneratedPools.TryGetValue(key, out var pool))
            {
                pool = new Stack<GameObject>();
                GeneratedPools[key] = pool;
            }

            pool.Push(instance);
            M136PerformanceOperationCounters.ReportRuntimePoolReturn();
        }

        private static void PrepareRentedInstance(GameObject instance)
        {
            if (instance == null)
            {
                return;
            }

            instance.SetActive(true);
            ActiveInstances.Add(instance);
            M136PerformanceOperationCounters.ReportRuntimePoolRent();
            foreach (var behaviour in instance.GetComponents<MonoBehaviour>())
            {
                if (behaviour is IPooledRuntimeObject pooled)
                {
                    pooled.OnRentFromPool();
                }
            }
        }

        private static void NotifyReturn(GameObject instance)
        {
            foreach (var behaviour in instance.GetComponents<MonoBehaviour>())
            {
                if (behaviour is IPooledRuntimeObject pooled)
                {
                    pooled.OnReturnToPool();
                }
            }
        }

        public static HollowRuntimePoolSnapshot Snapshot()
        {
            var inactive = 0;
            foreach (var pool in PrefabPools.Values)
            {
                inactive += CountLive(pool);
            }

            foreach (var pool in GeneratedPools.Values)
            {
                inactive += CountLive(pool);
            }

            var active = 0;
            foreach (var instance in ActiveInstances)
            {
                if (instance == null)
                {
                    continue;
                }

                active++;
            }

            var counters = M136PerformanceOperationCounters.Snapshot();
            return new HollowRuntimePoolSnapshot
            {
                prefabPoolCount = PrefabPools.Count,
                generatedPoolCount = GeneratedPools.Count,
                pooledInactiveCount = inactive,
                activeTrackedCount = active,
                activeLeakCount = 0,
                rents = counters.RuntimePoolRents,
                returns = counters.RuntimePoolReturns,
                misses = counters.RuntimePoolMisses,
                hardInstantiates = counters.RuntimePoolHardInstantiates,
                warmRequests = counters.RuntimePoolWarmRequests,
                warmCompletions = counters.RuntimePoolWarmCompletions,
                warmVisibleObjects = counters.PoolWarmVisibleObjects,
                warmRootActiveErrors = counters.PoolWarmRootActiveErrors,
                warmActiveLeaks = counters.PoolWarmActiveLeaks,
                recentMissKeys = SnapshotMissKeys()
            };
        }

        private static void RecordMissKey(string key)
        {
            key = string.IsNullOrWhiteSpace(key) ? "<unknown>" : key;
            MissesByKey.TryGetValue(key, out var count);
            MissesByKey[key] = count + 1;
        }

        private static string[] SnapshotMissKeys()
        {
            if (MissesByKey.Count == 0)
            {
                return Array.Empty<string>();
            }

            var result = new List<string>(Mathf.Min(32, MissesByKey.Count));
            foreach (var pair in MissesByKey)
            {
                result.Add($"{pair.Key}={pair.Value}");
                if (result.Count >= 32)
                {
                    break;
                }
            }

            return result.ToArray();
        }

        private static int CountLive(Stack<GameObject> pool)
        {
            if (pool == null || pool.Count == 0)
            {
                return 0;
            }

            var count = 0;
            foreach (var instance in pool)
            {
                if (instance != null)
                {
                    count++;
                }
            }

            return count;
        }

        private static RuntimePoolRunner Runner()
        {
            if (runner != null)
            {
                return runner;
            }

            var runnerObject = new GameObject("HollowRuntimePool");
            UnityEngine.Object.DontDestroyOnLoad(runnerObject);
            runner = runnerObject.AddComponent<RuntimePoolRunner>();
            return runner;
        }
    }
}
