using System;
using System.Collections;
using System.Collections.Generic;
using Hollow.Combat;
using Hollow.Core.Diagnostics;
using Hollow.Data.Definitions;
using Hollow.Rooms;
using UnityEngine;

namespace Hollow.Branches
{
    public sealed class BranchLiveRoomCache
    {
        private readonly Dictionary<string, BranchRoomRuntimeInstance> roomsById = new(StringComparer.Ordinal);
        private readonly List<BranchRoomRuntimeInstance> rooms = new();
        private GameObject cacheRoot;
        private BranchRoomRuntimeInstance activeRoom;

        public BranchRoomRuntimeInstance ActiveRoom => activeRoom;

        public int Count => rooms.Count;

        public IEnumerator BuildBranchRooms(
            IReadOnlyList<BranchRoomState> branchRooms,
            Func<BranchRoomState, ImportedRoomRuntimeAsset> resolveAsset,
            Transform parent,
            Action<BranchRoomState, RoomRuntimeRoot> configureBuiltRoom = null,
            Action<string, float> reportStage = null)
        {
            DisposeBranchRooms();
            if (branchRooms == null || resolveAsset == null)
            {
                yield break;
            }

            cacheRoot = new GameObject("BranchLiveRoomCache");
            cacheRoot.transform.SetParent(parent, false);
            cacheRoot.SetActive(true);

            var total = Mathf.Max(1, branchRooms.Count);
            for (var index = 0; index < branchRooms.Count; index++)
            {
                var started = Time.realtimeSinceStartup;
                var startingGc = GC.GetTotalMemory(false);
                var room = branchRooms[index];
                var asset = resolveAsset(room);
                if (room == null || asset == null)
                {
                    M136PerformanceOperationCounters.ReportBranchLiveRoomCacheMiss();
                    yield return null;
                    continue;
                }

                var instanceRoot = new GameObject($"BranchLiveRoom.{room.Id.Value}");
                instanceRoot.transform.SetParent(cacheRoot.transform, false);
                instanceRoot.SetActive(false);

                var runtimeRoot = instanceRoot.AddComponent<RoomRuntimeRoot>();
                runtimeRoot.ConfigureDefault();
                runtimeRoot.BuildFrom(asset, RoomNavMeshRuntimeFallbackMode.RequireCatalogBake);
                configureBuiltRoom?.Invoke(room, runtimeRoot);
                RoomLightingPrewarm.Prepare(runtimeRoot, applyGlobalSettings: false);
                var navMeshValidated = runtimeRoot.HasNavMeshBake;
                runtimeRoot.SetRuntimeNavMeshActive(false);

                var instance = new BranchRoomRuntimeInstance(room, asset, runtimeRoot, navMeshValidated);
                roomsById[room.Id.Value] = instance;
                rooms.Add(instance);
                M136PerformanceOperationCounters.ReportBranchLiveRoomBuilt(
                    (Time.realtimeSinceStartup - started) * 1000f,
                    Math.Max(0L, GC.GetTotalMemory(false) - startingGc));
                reportStage?.Invoke(room.Id.Value, Mathf.Clamp01((index + 1) / (float)total));
                yield return null;
            }
        }

        public bool TryGetRoom(BranchRoomId roomId, out BranchRoomRuntimeInstance instance)
        {
            return TryGetRoom(roomId.Value, out instance);
        }

        public bool TryGetRoom(string roomId, out BranchRoomRuntimeInstance instance)
        {
            return roomsById.TryGetValue(roomId ?? string.Empty, out instance) && instance != null;
        }

        public bool PrepareRoomForEntry(BranchRoomId roomId)
        {
            if (!TryGetRoom(roomId, out var instance))
            {
                M136PerformanceOperationCounters.ReportBranchLiveRoomCacheMiss();
                return false;
            }

            if (activeRoom != null && activeRoom != instance)
            {
                activeRoom.SetNavMeshActive(false);
            }

            var prepared = instance.PrepareForEntry();
            if (prepared)
            {
                M136PerformanceOperationCounters.ReportBranchLiveRoomCacheHit();
            }
            else
            {
                M136PerformanceOperationCounters.ReportBranchLiveRoomCacheMiss();
            }

            return prepared;
        }

        public bool ActivateRoom(BranchRoomId roomId)
        {
            if (!TryGetRoom(roomId, out var instance))
            {
                M136PerformanceOperationCounters.ReportBranchLiveRoomCacheMiss();
                return false;
            }

            if (activeRoom != null && activeRoom != instance)
            {
                activeRoom.HibernateOnExit();
            }

            instance.ActivateForEntry();
            activeRoom = instance;
            M136PerformanceOperationCounters.ReportNormalTraversalReveal(0);
            return true;
        }

        public void HibernateCurrentRoom()
        {
            activeRoom?.HibernateOnExit();
            activeRoom = null;
        }

        public void DisposeBranchRooms()
        {
            for (var index = 0; index < rooms.Count; index++)
            {
                rooms[index]?.Dispose();
            }

            rooms.Clear();
            roomsById.Clear();
            activeRoom = null;
            if (cacheRoot != null)
            {
                if (Application.isPlaying)
                {
                    UnityEngine.Object.Destroy(cacheRoot);
                }
                else
                {
                    UnityEngine.Object.DestroyImmediate(cacheRoot);
                }
            }

            cacheRoot = null;
        }

        public BranchLiveRoomCacheSnapshot Snapshot()
        {
            var hibernatedLeakCount = 0;
            for (var index = 0; index < rooms.Count; index++)
            {
                var room = rooms[index];
                if (room != null && !room.IsActive)
                {
                    hibernatedLeakCount += room.CountActiveHibernatedObjects();
                }
            }

            return new BranchLiveRoomCacheSnapshot(rooms.Count, activeRoom?.RoomId.Value ?? string.Empty, hibernatedLeakCount);
        }
    }

    public sealed class BranchRoomRuntimeInstance
    {
        public BranchRoomRuntimeInstance(
            BranchRoomState roomState,
            ImportedRoomRuntimeAsset asset,
            RoomRuntimeRoot runtimeRoot,
            bool navMeshValidated)
        {
            RoomState = roomState;
            RoomId = roomState?.Id ?? BranchRoomId.Origin;
            Asset = asset;
            RuntimeRoot = runtimeRoot;
            RootObject = runtimeRoot != null ? runtimeRoot.gameObject : null;
            NavMeshValidated = navMeshValidated;
        }

        public BranchRoomId RoomId { get; }

        public BranchRoomState RoomState { get; }

        public ImportedRoomRuntimeAsset Asset { get; }

        public RoomRuntimeRoot RuntimeRoot { get; }

        public GameObject RootObject { get; }

        public bool NavMeshValidated { get; private set; }

        public bool IsActive { get; private set; }

        public bool PrepareForEntry()
        {
            if (RuntimeRoot == null)
            {
                return false;
            }

            NavMeshValidated = RuntimeRoot.SetRuntimeNavMeshActive(true, RoomNavMeshRuntimeFallbackMode.RequireCatalogBake);
            RoomLightingPrewarm.Prepare(RuntimeRoot, applyGlobalSettings: false);
            return NavMeshValidated;
        }

        public void ActivateForEntry()
        {
            RoomLightingPrewarm.ApplyForEntry(RuntimeRoot);
            if (RootObject != null)
            {
                RootObject.SetActive(true);
            }

            IsActive = true;
        }

        public void HibernateOnExit()
        {
            if (RootObject != null)
            {
                RootObject.SetActive(false);
            }

            SetNavMeshActive(false);
            IsActive = false;
            var leaks = CountActiveHibernatedObjects();
            if (leaks > 0)
            {
                M136PerformanceOperationCounters.ReportHibernatedRoomActiveObjectLeak(leaks);
            }
        }

        public void SetNavMeshActive(bool active)
        {
            RuntimeRoot?.SetRuntimeNavMeshActive(active, RoomNavMeshRuntimeFallbackMode.RequireCatalogBake);
        }

        public int CountActiveHibernatedObjects()
        {
            if (RootObject == null || IsActive)
            {
                return 0;
            }

            var leaks = RootObject.activeInHierarchy ? 1 : 0;
            var renderers = RootObject.GetComponentsInChildren<Renderer>(true);
            for (var index = 0; index < renderers.Length; index++)
            {
                if (renderers[index] != null && renderers[index].enabled && renderers[index].gameObject.activeInHierarchy)
                {
                    leaks++;
                }
            }

            var colliders = RootObject.GetComponentsInChildren<Collider>(true);
            for (var index = 0; index < colliders.Length; index++)
            {
                if (colliders[index] != null && colliders[index].enabled && colliders[index].gameObject.activeInHierarchy)
                {
                    leaks++;
                }
            }

            var enemies = RootObject.GetComponentsInChildren<EnemyRuntimeController>(true);
            for (var index = 0; index < enemies.Length; index++)
            {
                if (enemies[index] != null && enemies[index].enabled && enemies[index].gameObject.activeInHierarchy)
                {
                    leaks++;
                }
            }

            return leaks;
        }

        public void Dispose()
        {
            RuntimeRoot?.SetRuntimeNavMeshActive(false, RoomNavMeshRuntimeFallbackMode.RequireCatalogBake);
            if (RootObject == null)
            {
                return;
            }

            RootObject.SetActive(false);
            if (Application.isPlaying)
            {
                UnityEngine.Object.Destroy(RootObject);
            }
            else
            {
                UnityEngine.Object.DestroyImmediate(RootObject);
            }
        }
    }

    public readonly struct BranchLiveRoomCacheSnapshot
    {
        public BranchLiveRoomCacheSnapshot(int roomCount, string activeRoomId, int hibernatedActiveObjectLeaks)
        {
            RoomCount = roomCount;
            ActiveRoomId = activeRoomId ?? string.Empty;
            HibernatedActiveObjectLeaks = hibernatedActiveObjectLeaks;
        }

        public int RoomCount { get; }

        public string ActiveRoomId { get; }

        public int HibernatedActiveObjectLeaks { get; }
    }
}
