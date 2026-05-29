using System;
using System.Collections.Generic;
using System.Linq;
using Hollow.Core;
using Hollow.Core.Diagnostics;
using Hollow.Data.Definitions;
using Hollow.Rewards;
using Hollow.Rooms;
using UnityEngine;

namespace Hollow.Branches
{
    public sealed class BranchRuntimeCache
    {
        private readonly Dictionary<string, BranchFloorGraph> graphCache = new(StringComparer.Ordinal);
        private readonly Dictionary<string, BranchRoomDistanceMap> roomDistanceMapCache = new(StringComparer.Ordinal);
        private readonly Dictionary<string, BranchFeaturePlan> featurePlanCache = new(StringComparer.Ordinal);
        private readonly Dictionary<string, ProceduralRewardPlan> rewardPlanCache = new(StringComparer.Ordinal);
        private readonly Dictionary<string, EncounterPlan> encounterPlanCache = new(StringComparer.Ordinal);
        private readonly Dictionary<string, ImportedRoomRuntimeAsset> roomAssetCache = new(StringComparer.Ordinal);
        private readonly List<string> graphLru = new();
        private readonly List<string> roomDistanceMapLru = new();
        private readonly List<string> featurePlanLru = new();
        private readonly List<string> rewardPlanLru = new();
        private readonly List<string> encounterPlanLru = new();
        private readonly List<string> roomAssetLru = new();

        private BranchSessionContent cachedContent;
        private string cachedContentKey = string.Empty;
        private string cachedContentError = string.Empty;

        public BranchRuntimeCache()
            : this(HollowRuntimeCachePolicy.Default)
        {
        }

        public BranchRuntimeCache(HollowRuntimeCachePolicy policy)
        {
            Policy = policy ?? HollowRuntimeCachePolicy.Default;
        }

        public HollowRuntimeCachePolicy Policy { get; }

        public string ActiveContentKey => cachedContentKey;

        public BranchSessionContent GetOrCreateContent(
            ImportedRoomRuntimeAsset legacySampleRoomAsset,
            BranchRoomTemplateCatalogDefinition catalog,
            int seed,
            out string error)
        {
            var key = ContentKey(legacySampleRoomAsset, catalog, seed);
            if (cachedContent != null && string.Equals(cachedContentKey, key, StringComparison.Ordinal))
            {
                M136PerformanceOperationCounters.ReportBranchRuntimeCacheHit();
                error = cachedContentError;
                return cachedContent;
            }

            if (cachedContent != null)
            {
                M136PerformanceOperationCounters.ReportBranchRuntimeCacheEviction();
                ClearPlanAndRoomCaches();
            }

            M136PerformanceOperationCounters.ReportBranchRuntimeCacheMiss();
            M136PerformanceOperationCounters.ReportBranchContentBuild();
            cachedContent = BranchSessionContent.Create(legacySampleRoomAsset, catalog, seed, out cachedContentError);
            cachedContentKey = key;
            error = cachedContentError;
            return cachedContent;
        }

        public BranchFloorGraph GetOrCreateGraph(string key, Func<BranchFloorGraph> factory)
        {
            key = NormalizeKey(key);
            if (graphCache.TryGetValue(key, out var cached) && cached != null)
            {
                Touch(graphLru, key);
                M136PerformanceOperationCounters.ReportBranchRuntimeCacheHit();
                return CloneGraph(cached);
            }

            M136PerformanceOperationCounters.ReportBranchRuntimeCacheMiss();
            M136PerformanceOperationCounters.ReportBranchGraphBuild();
            var graph = factory != null ? factory() : null;
            if (graph != null)
            {
                graphCache[key] = CloneGraph(graph);
                Touch(graphLru, key);
                Trim(graphCache, graphLru, Policy.MaxBranchGraphEntries);
            }

            return graph;
        }

        public BranchRoomDistanceMap GetOrCreateRoomDistanceMap(BranchFloorGraph graph)
        {
            var key = $"distance|{GraphSignature(graph)}";
            if (roomDistanceMapCache.TryGetValue(key, out var cached) && cached != null)
            {
                Touch(roomDistanceMapLru, key);
                M136PerformanceOperationCounters.ReportBranchRuntimeCacheHit();
                return cached;
            }

            M136PerformanceOperationCounters.ReportBranchRuntimeCacheMiss();
            M136PerformanceOperationCounters.ReportBranchRoomDistanceMapBuild();
            var distanceMap = BranchRoomDistanceMap.Create(graph);
            roomDistanceMapCache[key] = distanceMap;
            Touch(roomDistanceMapLru, key);
            Trim(roomDistanceMapCache, roomDistanceMapLru, Policy.MaxBranchPlanEntries);
            return distanceMap;
        }

        public BranchFeaturePlan GetOrCreateFeaturePlan(BranchFloorGraph graph, Func<BranchFeaturePlan> factory)
        {
            var key = $"feature|{GraphSignature(graph)}";
            return GetOrCreatePlan(featurePlanCache, featurePlanLru, key, factory, () => M136PerformanceOperationCounters.ReportBranchFeaturePlanBuild());
        }

        public ProceduralRewardPlan GetOrCreateRewardPlan(string key, Func<ProceduralRewardPlan> factory)
        {
            return GetOrCreatePlan(rewardPlanCache, rewardPlanLru, key, factory, () => M136PerformanceOperationCounters.ReportBranchRewardPlanBuild());
        }

        public EncounterPlan GetOrCreateEncounterPlan(string key, Func<EncounterPlan> factory)
        {
            return GetOrCreatePlan(encounterPlanCache, encounterPlanLru, key, factory, () => M136PerformanceOperationCounters.ReportBranchEncounterPlanBuild());
        }

        public ImportedRoomRuntimeAsset GetOrCreateRoomAsset(string key, Func<ImportedRoomRuntimeAsset> factory)
        {
            key = NormalizeKey(key);
            if (roomAssetCache.TryGetValue(key, out var cached) && cached != null)
            {
                Touch(roomAssetLru, key);
                M136PerformanceOperationCounters.ReportBranchRuntimeCacheHit();
                return cached;
            }

            M136PerformanceOperationCounters.ReportBranchRuntimeCacheMiss();
            M136PerformanceOperationCounters.ReportBranchRoomAssetResolve();
            var asset = factory != null ? factory() : null;
            if (asset != null)
            {
                roomAssetCache[key] = asset;
                Touch(roomAssetLru, key);
                Trim(roomAssetCache, roomAssetLru, Policy.MaxRoomAssetEntries);
            }

            return asset;
        }

        public void ClearAll()
        {
            if (cachedContent != null ||
                graphCache.Count > 0 ||
                roomDistanceMapCache.Count > 0 ||
                featurePlanCache.Count > 0 ||
                rewardPlanCache.Count > 0 ||
                encounterPlanCache.Count > 0 ||
                roomAssetCache.Count > 0)
            {
                M136PerformanceOperationCounters.ReportBranchRuntimeCacheEviction();
            }

            cachedContent = null;
            cachedContentKey = string.Empty;
            cachedContentError = string.Empty;
            graphCache.Clear();
            graphLru.Clear();
            ClearPlanAndRoomCaches();
        }

        public string CreateDebugSummary()
        {
            var operationSnapshot = M136PerformanceOperationCounters.Snapshot();
            var attempts = operationSnapshot.BranchRuntimeCacheHits + operationSnapshot.BranchRuntimeCacheMisses;
            var hitRate = attempts > 0 ? operationSnapshot.BranchRuntimeCacheHits / (float)attempts : 0f;
            return $"content={(cachedContent != null ? 1 : 0)}/1, graphs={graphCache.Count}/{Policy.MaxBranchGraphEntries}, distances={roomDistanceMapCache.Count}/{Policy.MaxBranchPlanEntries}, plans={featurePlanCache.Count + rewardPlanCache.Count + encounterPlanCache.Count}/{Policy.MaxBranchPlanEntries * 3}, roomAssets={roomAssetCache.Count}/{Policy.MaxRoomAssetEntries}, preloadRooms={Policy.MaxPredictivePreloadRooms}, descriptors={RoomRuntimeDescriptorCache.Count}/{RoomRuntimeDescriptorCache.MaxDescriptors}, hits={operationSnapshot.BranchRuntimeCacheHits}, misses={operationSnapshot.BranchRuntimeCacheMisses}, hitRate={hitRate:P0}, evictions={operationSnapshot.BranchRuntimeCacheEvictions}, evictionPolicy=lru-count-budget";
        }

        public BranchRuntimeCacheSnapshot Snapshot()
        {
            return new BranchRuntimeCacheSnapshot(
                cachedContent != null ? 1 : 0,
                graphCache.Count,
                roomDistanceMapCache.Count,
                featurePlanCache.Count,
                rewardPlanCache.Count,
                encounterPlanCache.Count,
                roomAssetCache.Count,
                Policy);
        }

        public static string DefinitionKey(UnityEngine.Object definition)
        {
            return definition == null ? "null" : $"{definition.name}:{definition.GetInstanceID()}";
        }

        public static string GraphSignature(BranchFloorGraph graph)
        {
            if (graph == null)
            {
                return "graph:null";
            }

            var rooms = string.Join(
                ";",
                graph.Rooms
                    .OrderBy(room => room.Id.Value, StringComparer.Ordinal)
                    .Select(room => $"{room.Id.Value},{room.Coordinate.x},{room.Coordinate.y},{room.InstanceId.Value},{room.RuntimeRoomAssetId},{room.Role},{FootprintSignature(room.Footprint)}"));
            var connections = string.Join(
                ";",
                graph.Connections
                    .OrderBy(connection => connection.FromRoomId.Value, StringComparer.Ordinal)
                    .ThenBy(connection => connection.ToRoomId.Value, StringComparer.Ordinal)
                    .ThenBy(connection => connection.FromPortId, StringComparer.Ordinal)
                    .Select(connection => $"{connection.FromRoomId.Value}>{connection.ToRoomId.Value}:{connection.FromDirection}>{connection.ToDirection}:{connection.FromPortId}>{connection.ToPortId}:{connection.LockKind}"));
            return $"{graph.BranchId}|{graph.Seed}|{rooms}|{connections}";
        }

        private TPlan GetOrCreatePlan<TPlan>(
            Dictionary<string, TPlan> cache,
            List<string> lru,
            string key,
            Func<TPlan> factory,
            Action reportBuild)
            where TPlan : class
        {
            key = NormalizeKey(key);
            if (cache.TryGetValue(key, out var cached) && cached != null)
            {
                Touch(lru, key);
                M136PerformanceOperationCounters.ReportBranchRuntimeCacheHit();
                return cached;
            }

            M136PerformanceOperationCounters.ReportBranchRuntimeCacheMiss();
            reportBuild?.Invoke();
            var plan = factory != null ? factory() : null;
            if (plan != null)
            {
                cache[key] = plan;
                Touch(lru, key);
                Trim(cache, lru, Policy.MaxBranchPlanEntries);
            }

            return plan;
        }

        private void ClearPlanAndRoomCaches()
        {
            featurePlanCache.Clear();
            rewardPlanCache.Clear();
            encounterPlanCache.Clear();
            roomAssetCache.Clear();
            roomDistanceMapCache.Clear();
            featurePlanLru.Clear();
            rewardPlanLru.Clear();
            encounterPlanLru.Clear();
            roomAssetLru.Clear();
            roomDistanceMapLru.Clear();
        }

        private static void Trim<TValue>(Dictionary<string, TValue> cache, List<string> lru, int maxEntries)
        {
            while (cache.Count > maxEntries && lru.Count > 0)
            {
                var oldest = lru[0];
                lru.RemoveAt(0);
                if (cache.Remove(oldest))
                {
                    M136PerformanceOperationCounters.ReportBranchRuntimeCacheEviction();
                }
            }
        }

        private static void Touch(List<string> lru, string key)
        {
            lru.Remove(key);
            lru.Add(key);
        }

        private static string NormalizeKey(string key)
        {
            return string.IsNullOrWhiteSpace(key) ? "default" : key;
        }

        private static string ContentKey(
            ImportedRoomRuntimeAsset legacySampleRoomAsset,
            BranchRoomTemplateCatalogDefinition catalog,
            int seed)
        {
            var templates = catalog != null
                ? string.Join(";", catalog.AllTemplates.Select(TextAssetKey))
                : "catalog:null";
            return $"legacy:{legacySampleRoomAsset?.Id ?? "null"}|catalog:{DefinitionKey(catalog)}|seed:{seed}|templates:{templates}";
        }

        private static string TextAssetKey(TextAsset asset)
        {
            return asset == null ? "null" : $"{asset.name}:{asset.GetInstanceID()}:{asset.text?.Length ?? 0}";
        }

        private static string FootprintSignature(RoomInstanceFootprint footprint)
        {
            if (footprint == null)
            {
                return "footprint:null";
            }

            var cells = string.Join(
                ".",
                footprint.OccupiedCells
                    .OrderBy(cell => cell.x)
                    .ThenBy(cell => cell.y)
                    .Select(cell => $"{cell.x}:{cell.y}"));
            return $"{footprint.PrimaryCell.x}:{footprint.PrimaryCell.y}:{footprint.ChunkBasisTiles.x}:{footprint.ChunkBasisTiles.y}:{cells}";
        }

        private static BranchFloorGraph CloneGraph(BranchFloorGraph source)
        {
            if (source == null)
            {
                return null;
            }

            var clone = new BranchFloorGraph(source.BranchId, source.Seed);
            foreach (var room in source.Rooms.OrderBy(room => room.Id.Value, StringComparer.Ordinal))
            {
                clone.AddRoom(new BranchRoomState(
                    new BranchRoomId(room.Id.Value),
                    room.Coordinate,
                    new BranchRoomInstanceId(room.InstanceId.Value),
                    room.RuntimeRoomAssetId,
                    CloneFootprint(room.Footprint),
                    room.Role));
            }

            var seenConnections = new HashSet<string>(StringComparer.Ordinal);
            foreach (var connection in source.Connections)
            {
                if (connection == null)
                {
                    continue;
                }

                var pairKey = ConnectionPairKey(connection);
                if (!seenConnections.Add(pairKey))
                {
                    continue;
                }

                clone.AddBidirectionalConnection(
                    new BranchRoomId(connection.FromRoomId.Value),
                    new BranchRoomId(connection.ToRoomId.Value),
                    connection.FromDirection,
                    connection.ToDirection,
                    connection.FromPortId,
                    connection.ToPortId,
                    connection.LockKind);
            }

            return clone;
        }

        private static RoomInstanceFootprint CloneFootprint(RoomInstanceFootprint footprint)
        {
            return footprint == null
                ? null
                : new RoomInstanceFootprint(footprint.PrimaryCell, footprint.OccupiedCells.ToArray(), footprint.ChunkBasisTiles);
        }

        private static string ConnectionPairKey(BranchConnection connection)
        {
            var forward = $"{connection.FromRoomId.Value}|{connection.FromDirection}|{connection.FromPortId}|{connection.ToRoomId.Value}|{connection.ToDirection}|{connection.ToPortId}|{connection.LockKind}";
            var reverse = $"{connection.ToRoomId.Value}|{connection.ToDirection}|{connection.ToPortId}|{connection.FromRoomId.Value}|{connection.FromDirection}|{connection.FromPortId}|{connection.LockKind}";
            return string.CompareOrdinal(forward, reverse) <= 0 ? $"{forward}<>{reverse}" : $"{reverse}<>{forward}";
        }
    }

    public readonly struct BranchRuntimeCacheSnapshot
    {
        public BranchRuntimeCacheSnapshot(
            int contentEntries,
            int graphEntries,
            int roomDistanceMapEntries,
            int featurePlanEntries,
            int rewardPlanEntries,
            int encounterPlanEntries,
            int roomAssetEntries,
            HollowRuntimeCachePolicy policy)
        {
            ContentEntries = contentEntries;
            GraphEntries = graphEntries;
            RoomDistanceMapEntries = roomDistanceMapEntries;
            FeaturePlanEntries = featurePlanEntries;
            RewardPlanEntries = rewardPlanEntries;
            EncounterPlanEntries = encounterPlanEntries;
            RoomAssetEntries = roomAssetEntries;
            Policy = policy ?? HollowRuntimeCachePolicy.Default;
        }

        public int ContentEntries { get; }

        public int GraphEntries { get; }

        public int RoomDistanceMapEntries { get; }

        public int FeaturePlanEntries { get; }

        public int RewardPlanEntries { get; }

        public int EncounterPlanEntries { get; }

        public int RoomAssetEntries { get; }

        public int TotalPlanEntries => RoomDistanceMapEntries + FeaturePlanEntries + RewardPlanEntries + EncounterPlanEntries;

        public HollowRuntimeCachePolicy Policy { get; }
    }
}
