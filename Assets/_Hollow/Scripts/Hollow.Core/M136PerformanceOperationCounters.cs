using System.Diagnostics;

namespace Hollow.Core.Diagnostics
{
    public readonly struct M136PerformanceOperationSnapshot
    {
        public M136PerformanceOperationSnapshot(
            int miniMapRebuilds,
            int miniMapModelBuilds,
            int wallVisibilityUpdates,
            int combatHudRefreshes,
            int combatHudModelBuilds,
            int playerBuildHudModelBuilds,
            int tacticalDirectorTicks,
            int runtimePoolRents,
            int runtimePoolReturns,
            int runtimePoolMisses,
            int runtimePoolHardInstantiates,
            int runtimePoolWarmRequests,
            int runtimePoolWarmCompletions,
            int debugOverlayTicksDisabled,
            int debugOverlayTicksEnabled,
            int activeEnemyMax,
            int aiThinkFull,
            int aiThinkReduced,
            int aiThinkBackground,
            int aiCommandReuses,
            int aiScorerCalls,
            int aiScorerCandidates,
            int aiBehaviorGraphTicks,
            int aiLodTransitions,
            int navPathRequests,
            int navPathSolves,
            int navPathDeferred,
            int navPathFallbacks,
            float navPathMaxSolveMilliseconds,
            int avoidanceHigh,
            int avoidanceReduced,
            int avoidanceBackground,
            int transitionStageCount,
            float transitionMaxStageMilliseconds,
            long transitionGcMaxBytes,
            int transitionCurtainShows,
            int transitionCurtainHides,
            float transitionCurtainMaxVisibleMilliseconds,
            int transitionCurtainMaxFramesAfterReady,
            float transitionLockMaxMilliseconds,
            int transitionOrphanCurtainsRemoved,
            int roomBuildStageCount,
            int transitionDestroyedObjectMaxPerFrame,
            int enemySpawnSlices,
            int bossActivationSlices,
            int navMeshCatalogAttachCount,
            int runtimeNavMeshFallbacks,
            int roomTransitionEvents,
            int branchRuntimeCacheHits,
            int branchRuntimeCacheMisses,
            int branchRuntimeCacheEvictions,
            int branchContentBuilds,
            int branchGraphBuilds,
            int branchRoomDistanceMapBuilds,
            int branchFeaturePlanBuilds,
            int branchRewardPlanBuilds,
            int branchEncounterPlanBuilds,
            int branchRoomAssetResolves,
            int roomDescriptorBuilds,
            int branchPreloadWarmRequests,
            int branchPreloadWarmCompletions,
            int branchPreloadSkippedStale,
            int presentationMaterialCacheHits,
            int presentationMaterialCacheMisses,
            int presentationPrefabCacheHits,
            int presentationPrefabCacheMisses,
            int presentationBiomeCacheHits,
            int presentationBiomeCacheMisses,
            int presentationColliderStripPasses,
            int branchLoadingStarts,
            int branchLoadingCompletions,
            int bossLoadingStarts,
            int bossLoadingCompletions,
            float branchLoadingMaxMilliseconds,
            float bossLoadingMaxMilliseconds,
            int fullBranchPreloadRooms,
            int traversalColdCacheMisses,
            int enemyPoolWarmRequests,
            int enemyPoolWarmCompletions,
            int enemyPoolRents,
            int enemyPoolReturns,
            int enemyPoolMisses,
            int enemyPoolHardInstantiates,
            int bootLoadingStarts,
            int bootLoadingCompletions,
            int bootLoadingFailures,
            int bootLoadingStageCount,
            float bootLoadingMaxStageMilliseconds,
            float bootLoadingMaxMilliseconds,
            int bootPreloadResourceLoads,
            int bootPreloadWarmRequests,
            int bootPreloadWarmCompletions,
            int bootPreloadShaderWarmAttempts,
            int bootPreloadShaderWarmCollections,
            int bootPreloadShaderWarmSuccesses,
            int bootPreloadShaderWarmMisses,
            float bootPreloadShaderWarmMaxMilliseconds,
            int m139StaleEnemyStateFailures,
            int m139StaleRuntimePoolStateFailures,
            int m139PoolActiveLeaks,
            int m139CacheHitRateWindows,
            int m139CacheHitRateWindowFailures,
            int m139ShaderMaterialFirstUseMissesAfterLoad,
            float m139ManagedMemoryDriftMaxMb,
            float m139GraphicsMemoryDriftMaxMb)
        {
            MiniMapRebuilds = miniMapRebuilds;
            MiniMapModelBuilds = miniMapModelBuilds;
            WallVisibilityUpdates = wallVisibilityUpdates;
            CombatHudRefreshes = combatHudRefreshes;
            CombatHudModelBuilds = combatHudModelBuilds;
            PlayerBuildHudModelBuilds = playerBuildHudModelBuilds;
            TacticalDirectorTicks = tacticalDirectorTicks;
            RuntimePoolRents = runtimePoolRents;
            RuntimePoolReturns = runtimePoolReturns;
            RuntimePoolMisses = runtimePoolMisses;
            RuntimePoolHardInstantiates = runtimePoolHardInstantiates;
            RuntimePoolWarmRequests = runtimePoolWarmRequests;
            RuntimePoolWarmCompletions = runtimePoolWarmCompletions;
            DebugOverlayTicksDisabled = debugOverlayTicksDisabled;
            DebugOverlayTicksEnabled = debugOverlayTicksEnabled;
            ActiveEnemyMax = activeEnemyMax;
            AiThinkFull = aiThinkFull;
            AiThinkReduced = aiThinkReduced;
            AiThinkBackground = aiThinkBackground;
            AiCommandReuses = aiCommandReuses;
            AiScorerCalls = aiScorerCalls;
            AiScorerCandidates = aiScorerCandidates;
            AiBehaviorGraphTicks = aiBehaviorGraphTicks;
            AiLodTransitions = aiLodTransitions;
            NavPathRequests = navPathRequests;
            NavPathSolves = navPathSolves;
            NavPathDeferred = navPathDeferred;
            NavPathFallbacks = navPathFallbacks;
            NavPathMaxSolveMilliseconds = navPathMaxSolveMilliseconds;
            AvoidanceHigh = avoidanceHigh;
            AvoidanceReduced = avoidanceReduced;
            AvoidanceBackground = avoidanceBackground;
            TransitionStageCount = transitionStageCount;
            TransitionMaxStageMilliseconds = transitionMaxStageMilliseconds;
            TransitionGcMaxBytes = transitionGcMaxBytes;
            TransitionCurtainShows = transitionCurtainShows;
            TransitionCurtainHides = transitionCurtainHides;
            TransitionCurtainMaxVisibleMilliseconds = transitionCurtainMaxVisibleMilliseconds;
            TransitionCurtainMaxFramesAfterReady = transitionCurtainMaxFramesAfterReady;
            TransitionLockMaxMilliseconds = transitionLockMaxMilliseconds;
            TransitionOrphanCurtainsRemoved = transitionOrphanCurtainsRemoved;
            RoomBuildStageCount = roomBuildStageCount;
            TransitionDestroyedObjectMaxPerFrame = transitionDestroyedObjectMaxPerFrame;
            EnemySpawnSlices = enemySpawnSlices;
            BossActivationSlices = bossActivationSlices;
            NavMeshCatalogAttachCount = navMeshCatalogAttachCount;
            RuntimeNavMeshFallbacks = runtimeNavMeshFallbacks;
            RoomTransitionEvents = roomTransitionEvents;
            BranchRuntimeCacheHits = branchRuntimeCacheHits;
            BranchRuntimeCacheMisses = branchRuntimeCacheMisses;
            BranchRuntimeCacheEvictions = branchRuntimeCacheEvictions;
            BranchContentBuilds = branchContentBuilds;
            BranchGraphBuilds = branchGraphBuilds;
            BranchRoomDistanceMapBuilds = branchRoomDistanceMapBuilds;
            BranchFeaturePlanBuilds = branchFeaturePlanBuilds;
            BranchRewardPlanBuilds = branchRewardPlanBuilds;
            BranchEncounterPlanBuilds = branchEncounterPlanBuilds;
            BranchRoomAssetResolves = branchRoomAssetResolves;
            RoomDescriptorBuilds = roomDescriptorBuilds;
            BranchPreloadWarmRequests = branchPreloadWarmRequests;
            BranchPreloadWarmCompletions = branchPreloadWarmCompletions;
            BranchPreloadSkippedStale = branchPreloadSkippedStale;
            PresentationMaterialCacheHits = presentationMaterialCacheHits;
            PresentationMaterialCacheMisses = presentationMaterialCacheMisses;
            PresentationPrefabCacheHits = presentationPrefabCacheHits;
            PresentationPrefabCacheMisses = presentationPrefabCacheMisses;
            PresentationBiomeCacheHits = presentationBiomeCacheHits;
            PresentationBiomeCacheMisses = presentationBiomeCacheMisses;
            PresentationColliderStripPasses = presentationColliderStripPasses;
            BranchLoadingStarts = branchLoadingStarts;
            BranchLoadingCompletions = branchLoadingCompletions;
            BossLoadingStarts = bossLoadingStarts;
            BossLoadingCompletions = bossLoadingCompletions;
            BranchLoadingMaxMilliseconds = branchLoadingMaxMilliseconds;
            BossLoadingMaxMilliseconds = bossLoadingMaxMilliseconds;
            FullBranchPreloadRooms = fullBranchPreloadRooms;
            TraversalColdCacheMisses = traversalColdCacheMisses;
            EnemyPoolWarmRequests = enemyPoolWarmRequests;
            EnemyPoolWarmCompletions = enemyPoolWarmCompletions;
            EnemyPoolRents = enemyPoolRents;
            EnemyPoolReturns = enemyPoolReturns;
            EnemyPoolMisses = enemyPoolMisses;
            EnemyPoolHardInstantiates = enemyPoolHardInstantiates;
            BootLoadingStarts = bootLoadingStarts;
            BootLoadingCompletions = bootLoadingCompletions;
            BootLoadingFailures = bootLoadingFailures;
            BootLoadingStageCount = bootLoadingStageCount;
            BootLoadingMaxStageMilliseconds = bootLoadingMaxStageMilliseconds;
            BootLoadingMaxMilliseconds = bootLoadingMaxMilliseconds;
            BootPreloadResourceLoads = bootPreloadResourceLoads;
            BootPreloadWarmRequests = bootPreloadWarmRequests;
            BootPreloadWarmCompletions = bootPreloadWarmCompletions;
            BootPreloadShaderWarmAttempts = bootPreloadShaderWarmAttempts;
            BootPreloadShaderWarmCollections = bootPreloadShaderWarmCollections;
            BootPreloadShaderWarmSuccesses = bootPreloadShaderWarmSuccesses;
            BootPreloadShaderWarmMisses = bootPreloadShaderWarmMisses;
            BootPreloadShaderWarmMaxMilliseconds = bootPreloadShaderWarmMaxMilliseconds;
            M139StaleEnemyStateFailures = m139StaleEnemyStateFailures;
            M139StaleRuntimePoolStateFailures = m139StaleRuntimePoolStateFailures;
            M139PoolActiveLeaks = m139PoolActiveLeaks;
            M139CacheHitRateWindows = m139CacheHitRateWindows;
            M139CacheHitRateWindowFailures = m139CacheHitRateWindowFailures;
            M139ShaderMaterialFirstUseMissesAfterLoad = m139ShaderMaterialFirstUseMissesAfterLoad;
            M139ManagedMemoryDriftMaxMb = m139ManagedMemoryDriftMaxMb;
            M139GraphicsMemoryDriftMaxMb = m139GraphicsMemoryDriftMaxMb;
        }

        public int MiniMapRebuilds { get; }

        public int MiniMapModelBuilds { get; }

        public int WallVisibilityUpdates { get; }

        public int CombatHudRefreshes { get; }

        public int CombatHudModelBuilds { get; }

        public int PlayerBuildHudModelBuilds { get; }

        public int TacticalDirectorTicks { get; }

        public int RuntimePoolRents { get; }

        public int RuntimePoolReturns { get; }

        public int RuntimePoolMisses { get; }

        public int RuntimePoolHardInstantiates { get; }

        public int RuntimePoolWarmRequests { get; }

        public int RuntimePoolWarmCompletions { get; }

        public int DebugOverlayTicksDisabled { get; }

        public int DebugOverlayTicksEnabled { get; }

        public int ActiveEnemyMax { get; }

        public int AiThinkFull { get; }

        public int AiThinkReduced { get; }

        public int AiThinkBackground { get; }

        public int AiCommandReuses { get; }

        public int AiScorerCalls { get; }

        public int AiScorerCandidates { get; }

        public int AiBehaviorGraphTicks { get; }

        public int AiLodTransitions { get; }

        public int NavPathRequests { get; }

        public int NavPathSolves { get; }

        public int NavPathDeferred { get; }

        public int NavPathFallbacks { get; }

        public float NavPathMaxSolveMilliseconds { get; }

        public int AvoidanceHigh { get; }

        public int AvoidanceReduced { get; }

        public int AvoidanceBackground { get; }

        public int TransitionStageCount { get; }

        public float TransitionMaxStageMilliseconds { get; }

        public long TransitionGcMaxBytes { get; }

        public int TransitionCurtainShows { get; }

        public int TransitionCurtainHides { get; }

        public float TransitionCurtainMaxVisibleMilliseconds { get; }

        public int TransitionCurtainMaxFramesAfterReady { get; }

        public float TransitionLockMaxMilliseconds { get; }

        public int TransitionOrphanCurtainsRemoved { get; }

        public int RoomBuildStageCount { get; }

        public int TransitionDestroyedObjectMaxPerFrame { get; }

        public int EnemySpawnSlices { get; }

        public int BossActivationSlices { get; }

        public int NavMeshCatalogAttachCount { get; }

        public int RuntimeNavMeshFallbacks { get; }

        public int RoomTransitionEvents { get; }

        public int BranchRuntimeCacheHits { get; }

        public int BranchRuntimeCacheMisses { get; }

        public int BranchRuntimeCacheEvictions { get; }

        public int BranchContentBuilds { get; }

        public int BranchGraphBuilds { get; }

        public int BranchRoomDistanceMapBuilds { get; }

        public int BranchFeaturePlanBuilds { get; }

        public int BranchRewardPlanBuilds { get; }

        public int BranchEncounterPlanBuilds { get; }

        public int BranchRoomAssetResolves { get; }

        public int RoomDescriptorBuilds { get; }

        public int BranchPreloadWarmRequests { get; }

        public int BranchPreloadWarmCompletions { get; }

        public int BranchPreloadSkippedStale { get; }

        public int PresentationMaterialCacheHits { get; }

        public int PresentationMaterialCacheMisses { get; }

        public int PresentationPrefabCacheHits { get; }

        public int PresentationPrefabCacheMisses { get; }

        public int PresentationBiomeCacheHits { get; }

        public int PresentationBiomeCacheMisses { get; }

        public int PresentationColliderStripPasses { get; }

        public int BranchLoadingStarts { get; }

        public int BranchLoadingCompletions { get; }

        public int BossLoadingStarts { get; }

        public int BossLoadingCompletions { get; }

        public float BranchLoadingMaxMilliseconds { get; }

        public float BossLoadingMaxMilliseconds { get; }

        public int FullBranchPreloadRooms { get; }

        public int TraversalColdCacheMisses { get; }

        public int EnemyPoolWarmRequests { get; }

        public int EnemyPoolWarmCompletions { get; }

        public int EnemyPoolRents { get; }

        public int EnemyPoolReturns { get; }

        public int EnemyPoolMisses { get; }

        public int EnemyPoolHardInstantiates { get; }

        public int BootLoadingStarts { get; }

        public int BootLoadingCompletions { get; }

        public int BootLoadingFailures { get; }

        public int BootLoadingStageCount { get; }

        public float BootLoadingMaxStageMilliseconds { get; }

        public float BootLoadingMaxMilliseconds { get; }

        public int BootPreloadResourceLoads { get; }

        public int BootPreloadWarmRequests { get; }

        public int BootPreloadWarmCompletions { get; }

        public int BootPreloadShaderWarmAttempts { get; }

        public int BootPreloadShaderWarmCollections { get; }

        public int BootPreloadShaderWarmSuccesses { get; }

        public int BootPreloadShaderWarmMisses { get; }

        public float BootPreloadShaderWarmMaxMilliseconds { get; }

        public int M139StaleEnemyStateFailures { get; }

        public int M139StaleRuntimePoolStateFailures { get; }

        public int M139PoolActiveLeaks { get; }

        public int M139CacheHitRateWindows { get; }

        public int M139CacheHitRateWindowFailures { get; }

        public int M139ShaderMaterialFirstUseMissesAfterLoad { get; }

        public float M139ManagedMemoryDriftMaxMb { get; }

        public float M139GraphicsMemoryDriftMaxMb { get; }
    }

    public static class M136PerformanceOperationCounters
    {
        private static int miniMapRebuilds;
        private static int miniMapModelBuilds;
        private static int wallVisibilityUpdates;
        private static int combatHudRefreshes;
        private static int combatHudModelBuilds;
        private static int playerBuildHudModelBuilds;
        private static int tacticalDirectorTicks;
        private static int runtimePoolRents;
        private static int runtimePoolReturns;
        private static int runtimePoolMisses;
        private static int runtimePoolHardInstantiates;
        private static int runtimePoolWarmRequests;
        private static int runtimePoolWarmCompletions;
        private static int debugOverlayTicksDisabled;
        private static int debugOverlayTicksEnabled;
        private static int activeEnemyMax;
        private static int aiThinkFull;
        private static int aiThinkReduced;
        private static int aiThinkBackground;
        private static int aiCommandReuses;
        private static int aiScorerCalls;
        private static int aiScorerCandidates;
        private static int aiBehaviorGraphTicks;
        private static int aiLodTransitions;
        private static int navPathRequests;
        private static int navPathSolves;
        private static int navPathDeferred;
        private static int navPathFallbacks;
        private static float navPathMaxSolveMilliseconds;
        private static int avoidanceHigh;
        private static int avoidanceReduced;
        private static int avoidanceBackground;
        private static int transitionStageCount;
        private static float transitionMaxStageMilliseconds;
        private static long transitionGcMaxBytes;
        private static int transitionCurtainShows;
        private static int transitionCurtainHides;
        private static float transitionCurtainMaxVisibleMilliseconds;
        private static int transitionCurtainMaxFramesAfterReady;
        private static float transitionLockMaxMilliseconds;
        private static int transitionOrphanCurtainsRemoved;
        private static int roomBuildStageCount;
        private static int transitionDestroyedObjectMaxPerFrame;
        private static int enemySpawnSlices;
        private static int bossActivationSlices;
        private static int navMeshCatalogAttachCount;
        private static int runtimeNavMeshFallbacks;
        private static int roomTransitionEvents;
        private static int branchRuntimeCacheHits;
        private static int branchRuntimeCacheMisses;
        private static int branchRuntimeCacheEvictions;
        private static int branchContentBuilds;
        private static int branchGraphBuilds;
        private static int branchRoomDistanceMapBuilds;
        private static int branchFeaturePlanBuilds;
        private static int branchRewardPlanBuilds;
        private static int branchEncounterPlanBuilds;
        private static int branchRoomAssetResolves;
        private static int roomDescriptorBuilds;
        private static int branchPreloadWarmRequests;
        private static int branchPreloadWarmCompletions;
        private static int branchPreloadSkippedStale;
        private static int presentationMaterialCacheHits;
        private static int presentationMaterialCacheMisses;
        private static int presentationPrefabCacheHits;
        private static int presentationPrefabCacheMisses;
        private static int presentationBiomeCacheHits;
        private static int presentationBiomeCacheMisses;
        private static int presentationColliderStripPasses;
        private static int branchLoadingStarts;
        private static int branchLoadingCompletions;
        private static int bossLoadingStarts;
        private static int bossLoadingCompletions;
        private static float branchLoadingMaxMilliseconds;
        private static float bossLoadingMaxMilliseconds;
        private static int fullBranchPreloadRooms;
        private static int traversalColdCacheMisses;
        private static int enemyPoolWarmRequests;
        private static int enemyPoolWarmCompletions;
        private static int enemyPoolRents;
        private static int enemyPoolReturns;
        private static int enemyPoolMisses;
        private static int enemyPoolHardInstantiates;
        private static int bootLoadingStarts;
        private static int bootLoadingCompletions;
        private static int bootLoadingFailures;
        private static int bootLoadingStageCount;
        private static float bootLoadingMaxStageMilliseconds;
        private static float bootLoadingMaxMilliseconds;
        private static int bootPreloadResourceLoads;
        private static int bootPreloadWarmRequests;
        private static int bootPreloadWarmCompletions;
        private static int bootPreloadShaderWarmAttempts;
        private static int bootPreloadShaderWarmCollections;
        private static int bootPreloadShaderWarmSuccesses;
        private static int bootPreloadShaderWarmMisses;
        private static float bootPreloadShaderWarmMaxMilliseconds;
        private static int m139StaleEnemyStateFailures;
        private static int m139StaleRuntimePoolStateFailures;
        private static int m139PoolActiveLeaks;
        private static int m139CacheHitRateWindows;
        private static int m139CacheHitRateWindowFailures;
        private static int m139ShaderMaterialFirstUseMissesAfterLoad;
        private static float m139ManagedMemoryDriftMaxMb;
        private static float m139GraphicsMemoryDriftMaxMb;

        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        public static void ReportMiniMapRebuild()
        {
            miniMapRebuilds++;
        }

        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        public static void ReportMiniMapModelBuild()
        {
            miniMapModelBuilds++;
        }

        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        public static void ReportWallVisibilityUpdate()
        {
            wallVisibilityUpdates++;
        }

        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        public static void ReportCombatHudRefresh()
        {
            combatHudRefreshes++;
        }

        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        public static void ReportCombatHudModelBuild()
        {
            combatHudModelBuilds++;
        }

        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        public static void ReportPlayerBuildHudModelBuild()
        {
            playerBuildHudModelBuilds++;
        }

        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        public static void ReportTacticalDirectorTick()
        {
            tacticalDirectorTicks++;
        }

        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        public static void ReportRuntimePoolRent()
        {
            runtimePoolRents++;
        }

        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        public static void ReportRuntimePoolReturn()
        {
            runtimePoolReturns++;
        }

        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        public static void ReportRuntimePoolMiss()
        {
            runtimePoolMisses++;
        }

        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        public static void ReportRuntimePoolHardInstantiate()
        {
            runtimePoolHardInstantiates++;
        }

        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        public static void ReportRuntimePoolWarmRequest()
        {
            runtimePoolWarmRequests++;
        }

        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        public static void ReportRuntimePoolWarmCompletion()
        {
            runtimePoolWarmCompletions++;
        }

        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        public static void ReportDebugOverlayTick(bool enabled)
        {
            if (enabled)
            {
                debugOverlayTicksEnabled++;
            }
            else
            {
                debugOverlayTicksDisabled++;
            }
        }

        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        public static void ReportActiveEnemyCount(int count)
        {
            if (count > activeEnemyMax)
            {
                activeEnemyMax = count;
            }
        }

        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        public static void ReportAiThink(int lodTier)
        {
            switch (lodTier)
            {
                case 2:
                    aiThinkFull++;
                    break;
                case 1:
                    aiThinkReduced++;
                    break;
                default:
                    aiThinkBackground++;
                    break;
            }
        }

        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        public static void ReportAiLodTransition()
        {
            aiLodTransitions++;
        }

        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        public static void ReportAiCommandReuse()
        {
            aiCommandReuses++;
        }

        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        public static void ReportAiScorerCall(int candidateCount)
        {
            aiScorerCalls++;
            aiScorerCandidates += candidateCount > 0 ? candidateCount : 0;
        }

        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        public static void ReportAiBehaviorGraphTick()
        {
            aiBehaviorGraphTicks++;
        }

        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        public static void ReportNavPathRequest()
        {
            navPathRequests++;
        }

        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        public static void ReportNavPathSolve(float milliseconds)
        {
            navPathSolves++;
            if (milliseconds > navPathMaxSolveMilliseconds)
            {
                navPathMaxSolveMilliseconds = milliseconds;
            }
        }

        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        public static void ReportNavPathDeferred()
        {
            navPathDeferred++;
        }

        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        public static void ReportNavPathFallback()
        {
            navPathFallbacks++;
        }

        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        public static void ReportAvoidanceTier(int lodTier)
        {
            switch (lodTier)
            {
                case 2:
                    avoidanceHigh++;
                    break;
                case 1:
                    avoidanceReduced++;
                    break;
                default:
                    avoidanceBackground++;
                    break;
            }
        }

        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        public static void ReportTransitionStage(float milliseconds, long gcBytes)
        {
            transitionStageCount++;
            if (milliseconds > transitionMaxStageMilliseconds)
            {
                transitionMaxStageMilliseconds = milliseconds;
            }

            if (gcBytes > transitionGcMaxBytes)
            {
                transitionGcMaxBytes = gcBytes;
            }
        }

        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        public static void ReportRoomBuildStage()
        {
            roomBuildStageCount++;
        }

        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        public static void ReportTransitionDestroyedObjects(int count)
        {
            if (count > transitionDestroyedObjectMaxPerFrame)
            {
                transitionDestroyedObjectMaxPerFrame = count;
            }
        }

        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        public static void ReportEnemySpawnSlice()
        {
            enemySpawnSlices++;
        }

        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        public static void ReportBossActivationSlice()
        {
            bossActivationSlices++;
        }

        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        public static void ReportNavMeshCatalogAttach()
        {
            navMeshCatalogAttachCount++;
        }

        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        public static void ReportRuntimeNavMeshFallback()
        {
            runtimeNavMeshFallbacks++;
        }

        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        public static void ReportRoomTransition()
        {
            roomTransitionEvents++;
        }

        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        public static void ReportTransitionCurtainShow()
        {
            transitionCurtainShows++;
        }

        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        public static void ReportTransitionCurtainHide(float visibleMilliseconds, int framesAfterReady)
        {
            transitionCurtainHides++;
            if (visibleMilliseconds > transitionCurtainMaxVisibleMilliseconds)
            {
                transitionCurtainMaxVisibleMilliseconds = visibleMilliseconds;
            }

            if (framesAfterReady > transitionCurtainMaxFramesAfterReady)
            {
                transitionCurtainMaxFramesAfterReady = framesAfterReady;
            }
        }

        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        public static void ReportTransitionLock(float milliseconds)
        {
            if (milliseconds > transitionLockMaxMilliseconds)
            {
                transitionLockMaxMilliseconds = milliseconds;
            }
        }

        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        public static void ReportTransitionOrphanCurtainsRemoved(int count)
        {
            if (count > 0)
            {
                transitionOrphanCurtainsRemoved += count;
            }
        }

        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        public static void ReportBranchLoadingStart()
        {
            branchLoadingStarts++;
        }

        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        public static void ReportBranchLoadingCompletion(float milliseconds)
        {
            branchLoadingCompletions++;
            if (milliseconds > branchLoadingMaxMilliseconds)
            {
                branchLoadingMaxMilliseconds = milliseconds;
            }
        }

        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        public static void ReportBossLoadingStart()
        {
            bossLoadingStarts++;
        }

        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        public static void ReportBossLoadingCompletion(float milliseconds)
        {
            bossLoadingCompletions++;
            if (milliseconds > bossLoadingMaxMilliseconds)
            {
                bossLoadingMaxMilliseconds = milliseconds;
            }
        }

        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        public static void ReportFullBranchPreloadRoom()
        {
            fullBranchPreloadRooms++;
        }

        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        public static void ReportTraversalColdCacheMiss()
        {
            traversalColdCacheMisses++;
        }

        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        public static void ReportEnemyPoolWarmRequest()
        {
            enemyPoolWarmRequests++;
        }

        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        public static void ReportEnemyPoolWarmCompletion()
        {
            enemyPoolWarmCompletions++;
        }

        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        public static void ReportEnemyPoolRent()
        {
            enemyPoolRents++;
        }

        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        public static void ReportEnemyPoolReturn()
        {
            enemyPoolReturns++;
        }

        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        public static void ReportEnemyPoolMiss()
        {
            enemyPoolMisses++;
        }

        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        public static void ReportEnemyPoolHardInstantiate()
        {
            enemyPoolHardInstantiates++;
        }

        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        public static void ReportM139StaleEnemyStateFailure()
        {
            m139StaleEnemyStateFailures++;
        }

        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        public static void ReportM139StaleRuntimePoolStateFailure()
        {
            m139StaleRuntimePoolStateFailures++;
        }

        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        public static void ReportM139PoolActiveLeak(int leakedCount = 1)
        {
            m139PoolActiveLeaks += leakedCount <= 0 ? 1 : leakedCount;
        }

        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        public static void ReportM139CacheHitRateWindow(bool failed)
        {
            m139CacheHitRateWindows++;
            if (failed)
            {
                m139CacheHitRateWindowFailures++;
            }
        }

        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        public static void ReportM139ShaderMaterialFirstUseMissAfterLoad()
        {
            m139ShaderMaterialFirstUseMissesAfterLoad++;
        }

        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        public static void ReportM139MemoryDrift(float managedMb, float graphicsMb)
        {
            if (managedMb > m139ManagedMemoryDriftMaxMb)
            {
                m139ManagedMemoryDriftMaxMb = managedMb;
            }

            if (graphicsMb > m139GraphicsMemoryDriftMaxMb)
            {
                m139GraphicsMemoryDriftMaxMb = graphicsMb;
            }
        }

        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        public static void ReportBootLoadingStart()
        {
            bootLoadingStarts++;
        }

        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        public static void ReportBootLoadingCompletion(float milliseconds)
        {
            bootLoadingCompletions++;
            if (milliseconds > bootLoadingMaxMilliseconds)
            {
                bootLoadingMaxMilliseconds = milliseconds;
            }
        }

        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        public static void ReportBootLoadingFailure()
        {
            bootLoadingFailures++;
        }

        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        public static void ReportBootLoadingStage(float milliseconds)
        {
            bootLoadingStageCount++;
            if (milliseconds > bootLoadingMaxStageMilliseconds)
            {
                bootLoadingMaxStageMilliseconds = milliseconds;
            }
        }

        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        public static void ReportBootPreloadResourceLoad(int count)
        {
            if (count > 0)
            {
                bootPreloadResourceLoads += count;
            }
        }

        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        public static void ReportBootPreloadWarmRequest()
        {
            bootPreloadWarmRequests++;
        }

        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        public static void ReportBootPreloadWarmCompletion()
        {
            bootPreloadWarmCompletions++;
        }

        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        public static void ReportBootPreloadShaderWarmAttempt()
        {
            bootPreloadShaderWarmAttempts++;
        }

        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        public static void ReportBootPreloadShaderWarmCollectionCount(int count)
        {
            if (count > 0)
            {
                bootPreloadShaderWarmCollections += count;
            }
        }

        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        public static void ReportBootPreloadShaderWarmSuccess(float milliseconds)
        {
            bootPreloadShaderWarmSuccesses++;
            if (milliseconds > bootPreloadShaderWarmMaxMilliseconds)
            {
                bootPreloadShaderWarmMaxMilliseconds = milliseconds;
            }
        }

        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        public static void ReportBootPreloadShaderWarmMiss()
        {
            bootPreloadShaderWarmMisses++;
        }

        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        public static void ReportBranchRuntimeCacheHit()
        {
            branchRuntimeCacheHits++;
        }

        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        public static void ReportBranchRuntimeCacheMiss()
        {
            branchRuntimeCacheMisses++;
        }

        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        public static void ReportBranchRuntimeCacheEviction()
        {
            branchRuntimeCacheEvictions++;
        }

        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        public static void ReportBranchContentBuild()
        {
            branchContentBuilds++;
        }

        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        public static void ReportBranchGraphBuild()
        {
            branchGraphBuilds++;
        }

        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        public static void ReportBranchRoomDistanceMapBuild()
        {
            branchRoomDistanceMapBuilds++;
        }

        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        public static void ReportBranchFeaturePlanBuild()
        {
            branchFeaturePlanBuilds++;
        }

        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        public static void ReportBranchRewardPlanBuild()
        {
            branchRewardPlanBuilds++;
        }

        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        public static void ReportBranchEncounterPlanBuild()
        {
            branchEncounterPlanBuilds++;
        }

        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        public static void ReportBranchRoomAssetResolve()
        {
            branchRoomAssetResolves++;
        }

        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        public static void ReportRoomDescriptorBuild()
        {
            roomDescriptorBuilds++;
        }

        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        public static void ReportBranchPreloadWarmRequest()
        {
            branchPreloadWarmRequests++;
        }

        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        public static void ReportBranchPreloadWarmCompletion()
        {
            branchPreloadWarmCompletions++;
        }

        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        public static void ReportBranchPreloadSkippedStale()
        {
            branchPreloadSkippedStale++;
        }

        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        public static void ReportPresentationMaterialCacheHit()
        {
            presentationMaterialCacheHits++;
        }

        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        public static void ReportPresentationMaterialCacheMiss()
        {
            presentationMaterialCacheMisses++;
        }

        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        public static void ReportPresentationPrefabCacheHit()
        {
            presentationPrefabCacheHits++;
        }

        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        public static void ReportPresentationPrefabCacheMiss()
        {
            presentationPrefabCacheMisses++;
        }

        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        public static void ReportPresentationBiomeCacheHit()
        {
            presentationBiomeCacheHits++;
        }

        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        public static void ReportPresentationBiomeCacheMiss()
        {
            presentationBiomeCacheMisses++;
        }

        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        public static void ReportPresentationColliderStripPass()
        {
            presentationColliderStripPasses++;
        }

        public static M136PerformanceOperationSnapshot Snapshot(bool reset = false)
        {
            var snapshot = new M136PerformanceOperationSnapshot(
                miniMapRebuilds,
                miniMapModelBuilds,
                wallVisibilityUpdates,
                combatHudRefreshes,
                combatHudModelBuilds,
                playerBuildHudModelBuilds,
                tacticalDirectorTicks,
                runtimePoolRents,
                runtimePoolReturns,
                runtimePoolMisses,
                runtimePoolHardInstantiates,
                runtimePoolWarmRequests,
                runtimePoolWarmCompletions,
                debugOverlayTicksDisabled,
                debugOverlayTicksEnabled,
                activeEnemyMax,
                aiThinkFull,
                aiThinkReduced,
                aiThinkBackground,
                aiCommandReuses,
                aiScorerCalls,
                aiScorerCandidates,
                aiBehaviorGraphTicks,
                aiLodTransitions,
                navPathRequests,
                navPathSolves,
                navPathDeferred,
                navPathFallbacks,
                navPathMaxSolveMilliseconds,
                avoidanceHigh,
                avoidanceReduced,
                avoidanceBackground,
                transitionStageCount,
                transitionMaxStageMilliseconds,
                transitionGcMaxBytes,
                transitionCurtainShows,
                transitionCurtainHides,
                transitionCurtainMaxVisibleMilliseconds,
                transitionCurtainMaxFramesAfterReady,
                transitionLockMaxMilliseconds,
                transitionOrphanCurtainsRemoved,
                roomBuildStageCount,
                transitionDestroyedObjectMaxPerFrame,
                enemySpawnSlices,
                bossActivationSlices,
                navMeshCatalogAttachCount,
                runtimeNavMeshFallbacks,
                roomTransitionEvents,
                branchRuntimeCacheHits,
                branchRuntimeCacheMisses,
                branchRuntimeCacheEvictions,
                branchContentBuilds,
                branchGraphBuilds,
                branchRoomDistanceMapBuilds,
                branchFeaturePlanBuilds,
                branchRewardPlanBuilds,
                branchEncounterPlanBuilds,
                branchRoomAssetResolves,
                roomDescriptorBuilds,
                branchPreloadWarmRequests,
                branchPreloadWarmCompletions,
                branchPreloadSkippedStale,
                presentationMaterialCacheHits,
                presentationMaterialCacheMisses,
                presentationPrefabCacheHits,
                presentationPrefabCacheMisses,
                presentationBiomeCacheHits,
                presentationBiomeCacheMisses,
                presentationColliderStripPasses,
                branchLoadingStarts,
                branchLoadingCompletions,
                bossLoadingStarts,
                bossLoadingCompletions,
                branchLoadingMaxMilliseconds,
                bossLoadingMaxMilliseconds,
                fullBranchPreloadRooms,
                traversalColdCacheMisses,
                enemyPoolWarmRequests,
                enemyPoolWarmCompletions,
                enemyPoolRents,
                enemyPoolReturns,
                enemyPoolMisses,
                enemyPoolHardInstantiates,
                bootLoadingStarts,
                bootLoadingCompletions,
                bootLoadingFailures,
                bootLoadingStageCount,
                bootLoadingMaxStageMilliseconds,
                bootLoadingMaxMilliseconds,
                bootPreloadResourceLoads,
                bootPreloadWarmRequests,
                bootPreloadWarmCompletions,
                bootPreloadShaderWarmAttempts,
                bootPreloadShaderWarmCollections,
                bootPreloadShaderWarmSuccesses,
                bootPreloadShaderWarmMisses,
                bootPreloadShaderWarmMaxMilliseconds,
                m139StaleEnemyStateFailures,
                m139StaleRuntimePoolStateFailures,
                m139PoolActiveLeaks,
                m139CacheHitRateWindows,
                m139CacheHitRateWindowFailures,
                m139ShaderMaterialFirstUseMissesAfterLoad,
                m139ManagedMemoryDriftMaxMb,
                m139GraphicsMemoryDriftMaxMb);
            if (reset)
            {
                Reset();
            }

            return snapshot;
        }

        public static void Reset()
        {
            miniMapRebuilds = 0;
            miniMapModelBuilds = 0;
            wallVisibilityUpdates = 0;
            combatHudRefreshes = 0;
            combatHudModelBuilds = 0;
            playerBuildHudModelBuilds = 0;
            tacticalDirectorTicks = 0;
            runtimePoolRents = 0;
            runtimePoolReturns = 0;
            runtimePoolMisses = 0;
            runtimePoolHardInstantiates = 0;
            runtimePoolWarmRequests = 0;
            runtimePoolWarmCompletions = 0;
            debugOverlayTicksDisabled = 0;
            debugOverlayTicksEnabled = 0;
            activeEnemyMax = 0;
            aiThinkFull = 0;
            aiThinkReduced = 0;
            aiThinkBackground = 0;
            aiCommandReuses = 0;
            aiScorerCalls = 0;
            aiScorerCandidates = 0;
            aiBehaviorGraphTicks = 0;
            aiLodTransitions = 0;
            navPathRequests = 0;
            navPathSolves = 0;
            navPathDeferred = 0;
            navPathFallbacks = 0;
            navPathMaxSolveMilliseconds = 0f;
            avoidanceHigh = 0;
            avoidanceReduced = 0;
            avoidanceBackground = 0;
            transitionStageCount = 0;
            transitionMaxStageMilliseconds = 0f;
            transitionGcMaxBytes = 0;
            transitionCurtainShows = 0;
            transitionCurtainHides = 0;
            transitionCurtainMaxVisibleMilliseconds = 0f;
            transitionCurtainMaxFramesAfterReady = 0;
            transitionLockMaxMilliseconds = 0f;
            transitionOrphanCurtainsRemoved = 0;
            roomBuildStageCount = 0;
            transitionDestroyedObjectMaxPerFrame = 0;
            enemySpawnSlices = 0;
            bossActivationSlices = 0;
            navMeshCatalogAttachCount = 0;
            runtimeNavMeshFallbacks = 0;
            roomTransitionEvents = 0;
            branchRuntimeCacheHits = 0;
            branchRuntimeCacheMisses = 0;
            branchRuntimeCacheEvictions = 0;
            branchContentBuilds = 0;
            branchGraphBuilds = 0;
            branchRoomDistanceMapBuilds = 0;
            branchFeaturePlanBuilds = 0;
            branchRewardPlanBuilds = 0;
            branchEncounterPlanBuilds = 0;
            branchRoomAssetResolves = 0;
            roomDescriptorBuilds = 0;
            branchPreloadWarmRequests = 0;
            branchPreloadWarmCompletions = 0;
            branchPreloadSkippedStale = 0;
            presentationMaterialCacheHits = 0;
            presentationMaterialCacheMisses = 0;
            presentationPrefabCacheHits = 0;
            presentationPrefabCacheMisses = 0;
            presentationBiomeCacheHits = 0;
            presentationBiomeCacheMisses = 0;
            presentationColliderStripPasses = 0;
            branchLoadingStarts = 0;
            branchLoadingCompletions = 0;
            bossLoadingStarts = 0;
            bossLoadingCompletions = 0;
            branchLoadingMaxMilliseconds = 0f;
            bossLoadingMaxMilliseconds = 0f;
            fullBranchPreloadRooms = 0;
            traversalColdCacheMisses = 0;
            enemyPoolWarmRequests = 0;
            enemyPoolWarmCompletions = 0;
            enemyPoolRents = 0;
            enemyPoolReturns = 0;
            enemyPoolMisses = 0;
            enemyPoolHardInstantiates = 0;
            bootLoadingStarts = 0;
            bootLoadingCompletions = 0;
            bootLoadingFailures = 0;
            bootLoadingStageCount = 0;
            bootLoadingMaxStageMilliseconds = 0f;
            bootLoadingMaxMilliseconds = 0f;
            bootPreloadResourceLoads = 0;
            bootPreloadWarmRequests = 0;
            bootPreloadWarmCompletions = 0;
            bootPreloadShaderWarmAttempts = 0;
            bootPreloadShaderWarmCollections = 0;
            bootPreloadShaderWarmSuccesses = 0;
            bootPreloadShaderWarmMisses = 0;
            bootPreloadShaderWarmMaxMilliseconds = 0f;
            m139StaleEnemyStateFailures = 0;
            m139StaleRuntimePoolStateFailures = 0;
            m139PoolActiveLeaks = 0;
            m139CacheHitRateWindows = 0;
            m139CacheHitRateWindowFailures = 0;
            m139ShaderMaterialFirstUseMissesAfterLoad = 0;
            m139ManagedMemoryDriftMaxMb = 0f;
            m139GraphicsMemoryDriftMaxMb = 0f;
        }
    }
}
