using System;
using System.Collections.Generic;
using System.Globalization;
using System.Diagnostics;
using System.Text;

namespace Hollow.Core.Diagnostics
{
    public enum M136CpuStageKind
    {
        LiveRoomActivation = 0,
        DoorVisualState = 1,
        RoomCombatBegin = 2,
        EnemyRewardInteractableActivation = 3,
        SpaceshipTerminalActivation = 4,
        BranchPreloadSchedule = 5,
        HudMinimapRefreshAfterTraversal = 6,
        BossAiUpdate = 7,
        AddAiThinkScorer = 8,
        BehaviorGraphTick = 9,
        TacticalDirector = 10,
        NavMeshRequest = 11,
        ProjectileLoop = 12,
        CombatHitEvaluation = 13,
        HudRefresh = 14,
        EnemyVisualPresenter = 15
    }

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
            int stressHarnessNavMeshBakes,
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
            int stagedRoomVisibleRendererFrames,
            int normalTraversalRevealFrames,
            int roomReadyBeforeRevealMaxFrames,
            int poolWarmVisibleObjects,
            int poolWarmRootActiveErrors,
            int poolWarmActiveLeaks,
            int presentationFallbackVisuals,
            int roomEntryVfxBeforeReveal,
            int branchLoadingStarts,
            int branchLoadingCompletions,
            int bossLoadingStarts,
            int bossLoadingCompletions,
            float branchLoadingMaxMilliseconds,
            float bossLoadingMaxMilliseconds,
            int fullBranchPreloadRooms,
            int branchLiveRoomsBuilt,
            float branchLiveRoomBuildMaxMilliseconds,
            long branchLiveRoomBuildGcMaxBytes,
            int branchLiveRoomCacheHits,
            int branchLiveRoomCacheMisses,
            int normalTraversalRoomRebuildCalls,
            int normalTraversalWarmCalls,
            int hibernatedRoomActiveObjectLeaks,
            int doorStatePreActivationMisses,
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
            float m139GraphicsMemoryDriftMaxMb,
            string cpuStageSummary,
            string tacticalDirectorSummary,
            int tacticalCrowdReservationSkips,
            int tacticalCrowdCachedIntentReuses,
            int tacticalCrowdSupportReservationBudgetUses,
            int tacticalCrowdActiveThreatLimitMax,
            int tacticalCrowdScorerSkips,
            string cacheMissAttributionSummary,
            string[] cacheMissAttributionRows,
            int projectileActivePeak,
            int projectileSpawns,
            int projectileReturns,
            int projectileCollisionChecks,
            int projectilePoolMisses,
            int projectileHardInstantiates,
            float projectileUpdateMaxMilliseconds)
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
            StressHarnessNavMeshBakes = stressHarnessNavMeshBakes;
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
            StagedRoomVisibleRendererFrames = stagedRoomVisibleRendererFrames;
            NormalTraversalRevealFrames = normalTraversalRevealFrames;
            RoomReadyBeforeRevealMaxFrames = roomReadyBeforeRevealMaxFrames;
            PoolWarmVisibleObjects = poolWarmVisibleObjects;
            PoolWarmRootActiveErrors = poolWarmRootActiveErrors;
            PoolWarmActiveLeaks = poolWarmActiveLeaks;
            PresentationFallbackVisuals = presentationFallbackVisuals;
            RoomEntryVfxBeforeReveal = roomEntryVfxBeforeReveal;
            BranchLoadingStarts = branchLoadingStarts;
            BranchLoadingCompletions = branchLoadingCompletions;
            BossLoadingStarts = bossLoadingStarts;
            BossLoadingCompletions = bossLoadingCompletions;
            BranchLoadingMaxMilliseconds = branchLoadingMaxMilliseconds;
            BossLoadingMaxMilliseconds = bossLoadingMaxMilliseconds;
            FullBranchPreloadRooms = fullBranchPreloadRooms;
            BranchLiveRoomsBuilt = branchLiveRoomsBuilt;
            BranchLiveRoomBuildMaxMilliseconds = branchLiveRoomBuildMaxMilliseconds;
            BranchLiveRoomBuildGcMaxBytes = branchLiveRoomBuildGcMaxBytes;
            BranchLiveRoomCacheHits = branchLiveRoomCacheHits;
            BranchLiveRoomCacheMisses = branchLiveRoomCacheMisses;
            NormalTraversalRoomRebuildCalls = normalTraversalRoomRebuildCalls;
            NormalTraversalWarmCalls = normalTraversalWarmCalls;
            HibernatedRoomActiveObjectLeaks = hibernatedRoomActiveObjectLeaks;
            DoorStatePreActivationMisses = doorStatePreActivationMisses;
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
            CpuStageSummary = cpuStageSummary ?? string.Empty;
            TacticalDirectorSummary = tacticalDirectorSummary ?? string.Empty;
            TacticalCrowdReservationSkips = tacticalCrowdReservationSkips;
            TacticalCrowdCachedIntentReuses = tacticalCrowdCachedIntentReuses;
            TacticalCrowdSupportReservationBudgetUses = tacticalCrowdSupportReservationBudgetUses;
            TacticalCrowdActiveThreatLimitMax = tacticalCrowdActiveThreatLimitMax;
            TacticalCrowdScorerSkips = tacticalCrowdScorerSkips;
            CacheMissAttributionSummary = cacheMissAttributionSummary ?? string.Empty;
            CacheMissAttributionRows = cacheMissAttributionRows ?? Array.Empty<string>();
            ProjectileActivePeak = projectileActivePeak;
            ProjectileSpawns = projectileSpawns;
            ProjectileReturns = projectileReturns;
            ProjectileCollisionChecks = projectileCollisionChecks;
            ProjectilePoolMisses = projectilePoolMisses;
            ProjectileHardInstantiates = projectileHardInstantiates;
            ProjectileUpdateMaxMilliseconds = projectileUpdateMaxMilliseconds;
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

        public int StressHarnessNavMeshBakes { get; }

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

        public int StagedRoomVisibleRendererFrames { get; }

        public int NormalTraversalRevealFrames { get; }

        public int RoomReadyBeforeRevealMaxFrames { get; }

        public int PoolWarmVisibleObjects { get; }

        public int PoolWarmRootActiveErrors { get; }

        public int PoolWarmActiveLeaks { get; }

        public int PresentationFallbackVisuals { get; }

        public int RoomEntryVfxBeforeReveal { get; }

        public int BranchLoadingStarts { get; }

        public int BranchLoadingCompletions { get; }

        public int BossLoadingStarts { get; }

        public int BossLoadingCompletions { get; }

        public float BranchLoadingMaxMilliseconds { get; }

        public float BossLoadingMaxMilliseconds { get; }

        public int FullBranchPreloadRooms { get; }

        public int BranchLiveRoomsBuilt { get; }

        public float BranchLiveRoomBuildMaxMilliseconds { get; }

        public long BranchLiveRoomBuildGcMaxBytes { get; }

        public int BranchLiveRoomCacheHits { get; }

        public int BranchLiveRoomCacheMisses { get; }

        public int NormalTraversalRoomRebuildCalls { get; }

        public int NormalTraversalWarmCalls { get; }

        public int HibernatedRoomActiveObjectLeaks { get; }

        public int DoorStatePreActivationMisses { get; }

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

        public string CpuStageSummary { get; }

        public string TacticalDirectorSummary { get; }

        public int TacticalCrowdReservationSkips { get; }

        public int TacticalCrowdCachedIntentReuses { get; }

        public int TacticalCrowdSupportReservationBudgetUses { get; }

        public int TacticalCrowdActiveThreatLimitMax { get; }

        public int TacticalCrowdScorerSkips { get; }

        public string CacheMissAttributionSummary { get; }

        public string[] CacheMissAttributionRows { get; }

        public int ProjectileActivePeak { get; }

        public int ProjectileSpawns { get; }

        public int ProjectileReturns { get; }

        public int ProjectileCollisionChecks { get; }

        public int ProjectilePoolMisses { get; }

        public int ProjectileHardInstantiates { get; }

        public float ProjectileUpdateMaxMilliseconds { get; }
    }

    public static class M136PerformanceOperationCounters
    {
        private const int CpuStageCount = 16;
        private const int CacheMissAttributionCapacity = 64;
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
        private static int stressHarnessNavMeshBakes;
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
        private static int stagedRoomVisibleRendererFrames;
        private static int normalTraversalRevealFrames;
        private static int roomReadyBeforeRevealMaxFrames;
        private static int poolWarmVisibleObjects;
        private static int poolWarmRootActiveErrors;
        private static int poolWarmActiveLeaks;
        private static int presentationFallbackVisuals;
        private static int roomEntryVfxBeforeReveal;
        private static int branchLoadingStarts;
        private static int branchLoadingCompletions;
        private static int bossLoadingStarts;
        private static int bossLoadingCompletions;
        private static float branchLoadingMaxMilliseconds;
        private static float bossLoadingMaxMilliseconds;
        private static int fullBranchPreloadRooms;
        private static int branchLiveRoomsBuilt;
        private static float branchLiveRoomBuildMaxMilliseconds;
        private static long branchLiveRoomBuildGcMaxBytes;
        private static int branchLiveRoomCacheHits;
        private static int branchLiveRoomCacheMisses;
        private static int normalTraversalRoomRebuildCalls;
        private static int normalTraversalWarmCalls;
        private static int hibernatedRoomActiveObjectLeaks;
        private static int doorStatePreActivationMisses;
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
        private static int projectileActivePeak;
        private static int projectileActiveCurrent;
        private static int projectileSpawns;
        private static int projectileReturns;
        private static int projectileCollisionChecks;
        private static int projectilePoolMisses;
        private static int projectileHardInstantiates;
        private static float projectileUpdateMaxMilliseconds;
        private static int tacticalReservationAttempts;
        private static int tacticalReservationPathSolves;
        private static int tacticalReservationCandidatesChecked;
        private static int tacticalCachedIntentReuses;
        private static int tacticalBossAddReservationSkips;
        private static int bossAddScorerSkips;
        private static int bossAddCachedCommandReuses;
        private static int tacticalCrowdReservationSkips;
        private static int tacticalCrowdCachedIntentReuses;
        private static int tacticalCrowdSupportReservationBudgetUses;
        private static int tacticalCrowdActiveThreatLimitMax;
        private static int tacticalCrowdScorerSkips;
        private static readonly int[] cpuStageCounts = new int[CpuStageCount];
        private static readonly float[] cpuStageMaxMilliseconds = new float[CpuStageCount];
        private static readonly long[] cpuStageGcMaxBytes = new long[CpuStageCount];
        private static readonly string[] cacheMissAttributionRows = new string[CacheMissAttributionCapacity];
        private static int cacheMissAttributionWriteIndex;
        private static int cacheMissAttributionCount;

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
        public static void ReportTacticalReservationAttempt()
        {
            tacticalReservationAttempts++;
        }

        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        public static void ReportTacticalReservationPathSolve()
        {
            tacticalReservationPathSolves++;
        }

        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        public static void ReportTacticalReservationCandidateChecked()
        {
            tacticalReservationCandidatesChecked++;
        }

        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        public static void ReportTacticalCachedIntentReuse(int count = 1)
        {
            tacticalCachedIntentReuses += Math.Max(1, count);
        }

        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        public static void ReportTacticalBossAddReservationSkip(int count = 1)
        {
            tacticalBossAddReservationSkips += Math.Max(1, count);
        }

        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        public static void ReportBossAddScorerSkip(int count = 1)
        {
            bossAddScorerSkips += Math.Max(1, count);
        }

        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        public static void ReportBossAddCachedCommandReuse(int count = 1)
        {
            bossAddCachedCommandReuses += Math.Max(1, count);
        }

        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        public static void ReportTacticalCrowdReservationSkip(int count = 1)
        {
            tacticalCrowdReservationSkips += Math.Max(1, count);
        }

        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        public static void ReportTacticalCrowdCachedIntentReuse(int count = 1)
        {
            tacticalCrowdCachedIntentReuses += Math.Max(1, count);
        }

        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        public static void ReportTacticalCrowdSupportReservationBudgetUse(int count = 1)
        {
            tacticalCrowdSupportReservationBudgetUses += Math.Max(1, count);
        }

        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        public static void ReportTacticalCrowdActiveThreatLimit(int activeThreatLimit)
        {
            if (activeThreatLimit > tacticalCrowdActiveThreatLimitMax)
            {
                tacticalCrowdActiveThreatLimitMax = activeThreatLimit;
            }
        }

        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        public static void ReportTacticalCrowdScorerSkip(int count = 1)
        {
            tacticalCrowdScorerSkips += Math.Max(1, count);
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
        public static void ReportCpuStage(M136CpuStageKind stage, float milliseconds, long gcBytes)
        {
            var index = (int)stage;
            if (index < 0 || index >= CpuStageCount)
            {
                return;
            }

            cpuStageCounts[index]++;
            if (milliseconds > cpuStageMaxMilliseconds[index])
            {
                cpuStageMaxMilliseconds[index] = milliseconds;
            }

            if (gcBytes > cpuStageGcMaxBytes[index])
            {
                cpuStageGcMaxBytes[index] = gcBytes;
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
        public static void ReportStressHarnessNavMeshBake()
        {
            stressHarnessNavMeshBakes++;
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
        public static void ReportBranchLiveRoomBuilt(float milliseconds, long gcBytes)
        {
            branchLiveRoomsBuilt++;
            if (milliseconds > branchLiveRoomBuildMaxMilliseconds)
            {
                branchLiveRoomBuildMaxMilliseconds = milliseconds;
            }

            if (gcBytes > branchLiveRoomBuildGcMaxBytes)
            {
                branchLiveRoomBuildGcMaxBytes = gcBytes;
            }
        }

        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        public static void ReportBranchLiveRoomCacheHit()
        {
            branchLiveRoomCacheHits++;
        }

        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        public static void ReportBranchLiveRoomCacheMiss()
        {
            branchLiveRoomCacheMisses++;
        }

        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        public static void ReportNormalTraversalRoomRebuildCall()
        {
            normalTraversalRoomRebuildCalls++;
        }

        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        public static void ReportNormalTraversalWarmCall()
        {
            normalTraversalWarmCalls++;
        }

        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        public static void ReportHibernatedRoomActiveObjectLeak(int count)
        {
            hibernatedRoomActiveObjectLeaks += count <= 0 ? 1 : count;
        }

        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        public static void ReportDoorStatePreActivationMiss()
        {
            doorStatePreActivationMisses++;
        }

        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        public static void ReportTraversalColdCacheMiss()
        {
            ReportTraversalColdCacheMiss("unknown", "unknown", "unknown");
        }

        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        public static void ReportTraversalColdCacheMiss(string category, string key, string context)
        {
            traversalColdCacheMisses++;
            RecordCacheMissAttribution("traversal", category, key, context);
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
            ReportBranchRuntimeCacheMiss("unknown", "unknown", "unknown");
        }

        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        public static void ReportBranchRuntimeCacheMiss(string category, string key, string context)
        {
            branchRuntimeCacheMisses++;
            RecordCacheMissAttribution("branch-runtime", category, key, context);
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
            ReportPresentationCacheMiss("material", "unknown", "unknown");
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
            ReportPresentationCacheMiss("prefab", "unknown", "unknown");
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
            ReportPresentationCacheMiss("biome", "unknown", "unknown");
        }

        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        public static void ReportPresentationCacheMiss(string category, string key, string context)
        {
            switch (category)
            {
                case "material":
                    presentationMaterialCacheMisses++;
                    break;
                case "prefab":
                    presentationPrefabCacheMisses++;
                    break;
                case "biome":
                    presentationBiomeCacheMisses++;
                    break;
                default:
                    presentationPrefabCacheMisses++;
                    break;
            }

            RecordCacheMissAttribution("presentation", category, key, context);
        }

        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        public static void ReportProjectileActiveCount(int count)
        {
            if (count > projectileActivePeak)
            {
                projectileActivePeak = count;
            }
        }

        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        public static void ReportProjectileSpawn(int count = 1)
        {
            var resolved = count <= 0 ? 1 : count;
            projectileSpawns += resolved;
            projectileActiveCurrent += resolved;
            if (projectileActiveCurrent > projectileActivePeak)
            {
                projectileActivePeak = projectileActiveCurrent;
            }
        }

        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        public static void ReportProjectileReturn(int count = 1)
        {
            var resolved = count <= 0 ? 1 : count;
            projectileReturns += resolved;
            projectileActiveCurrent -= resolved;
            if (projectileActiveCurrent < 0)
            {
                projectileActiveCurrent = 0;
            }
        }

        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        public static void ReportProjectileCollisionCheck(int count = 1)
        {
            projectileCollisionChecks += count <= 0 ? 1 : count;
        }

        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        public static void ReportProjectilePoolMiss()
        {
            projectilePoolMisses++;
        }

        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        public static void ReportProjectileHardInstantiate()
        {
            projectileHardInstantiates++;
        }

        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        public static void ReportProjectileUpdate(float milliseconds)
        {
            if (milliseconds > projectileUpdateMaxMilliseconds)
            {
                projectileUpdateMaxMilliseconds = milliseconds;
            }
        }

        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        public static void ReportPresentationColliderStripPass()
        {
            presentationColliderStripPasses++;
        }

        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        public static void ReportStagedRoomVisibleRendererFrame()
        {
            stagedRoomVisibleRendererFrames++;
        }

        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        public static void ReportNormalTraversalReveal(int framesReadyBeforeReveal)
        {
            normalTraversalRevealFrames++;
            if (framesReadyBeforeReveal > roomReadyBeforeRevealMaxFrames)
            {
                roomReadyBeforeRevealMaxFrames = framesReadyBeforeReveal;
            }
        }

        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        public static void ReportPoolWarmVisibleObject(int count = 1)
        {
            poolWarmVisibleObjects += count <= 0 ? 1 : count;
        }

        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        public static void ReportPoolWarmRootActiveError()
        {
            poolWarmRootActiveErrors++;
        }

        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        public static void ReportPoolWarmActiveLeak(int count = 1)
        {
            poolWarmActiveLeaks += count <= 0 ? 1 : count;
        }

        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        public static void ReportPresentationFallbackVisual()
        {
            presentationFallbackVisuals++;
        }

        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        public static void ReportRoomEntryVfxBeforeReveal()
        {
            roomEntryVfxBeforeReveal++;
        }

        public static string BuildCpuStageSummary()
        {
            var builder = new StringBuilder(192);
            for (var index = 0; index < CpuStageCount; index++)
            {
                if (cpuStageCounts[index] <= 0)
                {
                    continue;
                }

                if (builder.Length > 0)
                {
                    builder.Append("; ");
                }

                builder
                    .Append(CpuStageName((M136CpuStageKind)index))
                    .Append(" count=")
                    .Append(cpuStageCounts[index].ToString(CultureInfo.InvariantCulture))
                    .Append(" maxMs=")
                    .Append(cpuStageMaxMilliseconds[index].ToString("0.###", CultureInfo.InvariantCulture))
                    .Append(" gcMax=")
                    .Append(cpuStageGcMaxBytes[index].ToString(CultureInfo.InvariantCulture));
            }

            return builder.Length > 0 ? builder.ToString() : string.Empty;
        }

        public static string BuildCacheMissAttributionSummary()
        {
            var rows = CacheMissAttributionRowsSnapshot();
            if (rows.Length == 0)
            {
                return string.Empty;
            }

            var counts = new Dictionary<string, int>(StringComparer.Ordinal);
            for (var index = 0; index < rows.Length; index++)
            {
                var row = rows[index];
                if (string.IsNullOrWhiteSpace(row))
                {
                    continue;
                }

                counts[row] = counts.TryGetValue(row, out var count) ? count + 1 : 1;
            }

            var builder = new StringBuilder(256);
            foreach (var pair in counts)
            {
                if (builder.Length > 0)
                {
                    builder.Append("; ");
                }

                builder
                    .Append(pair.Value.ToString(CultureInfo.InvariantCulture))
                    .Append("x ")
                    .Append(pair.Key);
            }

            return builder.ToString();
        }

        public static string BuildTacticalDirectorSummary()
        {
            if (tacticalReservationAttempts <= 0 &&
                tacticalReservationPathSolves <= 0 &&
                tacticalReservationCandidatesChecked <= 0 &&
                tacticalCachedIntentReuses <= 0 &&
                tacticalBossAddReservationSkips <= 0 &&
                bossAddScorerSkips <= 0 &&
                bossAddCachedCommandReuses <= 0 &&
                tacticalCrowdReservationSkips <= 0 &&
                tacticalCrowdCachedIntentReuses <= 0 &&
                tacticalCrowdSupportReservationBudgetUses <= 0 &&
                tacticalCrowdActiveThreatLimitMax <= 0 &&
                tacticalCrowdScorerSkips <= 0)
            {
                return string.Empty;
            }

            return new StringBuilder(320)
                .Append("reservationAttempts=")
                .Append(tacticalReservationAttempts.ToString(CultureInfo.InvariantCulture))
                .Append("; pathSolves=")
                .Append(tacticalReservationPathSolves.ToString(CultureInfo.InvariantCulture))
                .Append("; candidates=")
                .Append(tacticalReservationCandidatesChecked.ToString(CultureInfo.InvariantCulture))
                .Append("; cachedIntentReuses=")
                .Append(tacticalCachedIntentReuses.ToString(CultureInfo.InvariantCulture))
                .Append("; bossAddSkips=")
                .Append(tacticalBossAddReservationSkips.ToString(CultureInfo.InvariantCulture))
                .Append("; bossAddScorerSkips=")
                .Append(bossAddScorerSkips.ToString(CultureInfo.InvariantCulture))
                .Append("; bossAddCachedCommandReuses=")
                .Append(bossAddCachedCommandReuses.ToString(CultureInfo.InvariantCulture))
                .Append("; crowdReservationSkips=")
                .Append(tacticalCrowdReservationSkips.ToString(CultureInfo.InvariantCulture))
                .Append("; crowdCachedIntentReuses=")
                .Append(tacticalCrowdCachedIntentReuses.ToString(CultureInfo.InvariantCulture))
                .Append("; crowdSupportBudgetUses=")
                .Append(tacticalCrowdSupportReservationBudgetUses.ToString(CultureInfo.InvariantCulture))
                .Append("; crowdActiveThreatLimitMax=")
                .Append(tacticalCrowdActiveThreatLimitMax.ToString(CultureInfo.InvariantCulture))
                .Append("; crowdScorerSkips=")
                .Append(tacticalCrowdScorerSkips.ToString(CultureInfo.InvariantCulture))
                .ToString();
        }

        public static string[] CacheMissAttributionRowsSnapshot()
        {
            if (cacheMissAttributionCount <= 0)
            {
                return Array.Empty<string>();
            }

            var rows = new string[cacheMissAttributionCount];
            var start = cacheMissAttributionCount == CacheMissAttributionCapacity
                ? cacheMissAttributionWriteIndex
                : 0;
            for (var index = 0; index < rows.Length; index++)
            {
                rows[index] = cacheMissAttributionRows[(start + index) % CacheMissAttributionCapacity] ?? string.Empty;
            }

            return rows;
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
                stressHarnessNavMeshBakes,
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
                stagedRoomVisibleRendererFrames,
                normalTraversalRevealFrames,
                roomReadyBeforeRevealMaxFrames,
                poolWarmVisibleObjects,
                poolWarmRootActiveErrors,
                poolWarmActiveLeaks,
                presentationFallbackVisuals,
                roomEntryVfxBeforeReveal,
                branchLoadingStarts,
                branchLoadingCompletions,
                bossLoadingStarts,
                bossLoadingCompletions,
                branchLoadingMaxMilliseconds,
                bossLoadingMaxMilliseconds,
                fullBranchPreloadRooms,
                branchLiveRoomsBuilt,
                branchLiveRoomBuildMaxMilliseconds,
                branchLiveRoomBuildGcMaxBytes,
                branchLiveRoomCacheHits,
                branchLiveRoomCacheMisses,
                normalTraversalRoomRebuildCalls,
                normalTraversalWarmCalls,
                hibernatedRoomActiveObjectLeaks,
                doorStatePreActivationMisses,
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
                m139GraphicsMemoryDriftMaxMb,
                BuildCpuStageSummary(),
                BuildTacticalDirectorSummary(),
                tacticalCrowdReservationSkips,
                tacticalCrowdCachedIntentReuses,
                tacticalCrowdSupportReservationBudgetUses,
                tacticalCrowdActiveThreatLimitMax,
                tacticalCrowdScorerSkips,
                BuildCacheMissAttributionSummary(),
                CacheMissAttributionRowsSnapshot(),
                projectileActivePeak,
                projectileSpawns,
                projectileReturns,
                projectileCollisionChecks,
                projectilePoolMisses,
                projectileHardInstantiates,
                projectileUpdateMaxMilliseconds);
            if (reset)
            {
                Reset();
            }

            return snapshot;
        }

        private static void RecordCacheMissAttribution(string kind, string category, string key, string context)
        {
            var row = string.Concat(
                Compact(kind),
                "|",
                Compact(category),
                "|",
                Compact(key),
                "|",
                Compact(context));
            cacheMissAttributionRows[cacheMissAttributionWriteIndex] = row;
            cacheMissAttributionWriteIndex = (cacheMissAttributionWriteIndex + 1) % CacheMissAttributionCapacity;
            if (cacheMissAttributionCount < CacheMissAttributionCapacity)
            {
                cacheMissAttributionCount++;
            }
        }

        private static string Compact(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "unknown";
            }

            var builder = new StringBuilder(Math.Min(value.Length, 96));
            for (var index = 0; index < value.Length && builder.Length < 96; index++)
            {
                var character = value[index];
                builder.Append(character is '|' or '\r' or '\n' or '\t' ? ' ' : character);
            }

            return builder.ToString().Trim();
        }

        private static string CpuStageName(M136CpuStageKind stage)
        {
            return stage switch
            {
                M136CpuStageKind.LiveRoomActivation => "live_room_activation",
                M136CpuStageKind.DoorVisualState => "door_visual_state",
                M136CpuStageKind.RoomCombatBegin => "room_combat_begin",
                M136CpuStageKind.EnemyRewardInteractableActivation => "enemy_reward_interactable_activation",
                M136CpuStageKind.SpaceshipTerminalActivation => "spaceship_terminal_activation",
                M136CpuStageKind.BranchPreloadSchedule => "branch_preload_schedule",
                M136CpuStageKind.HudMinimapRefreshAfterTraversal => "hud_minimap_refresh_after_traversal",
                M136CpuStageKind.BossAiUpdate => "boss_ai_update",
                M136CpuStageKind.AddAiThinkScorer => "add_ai_think_scorer",
                M136CpuStageKind.BehaviorGraphTick => "behavior_graph_tick",
                M136CpuStageKind.TacticalDirector => "tactical_director",
                M136CpuStageKind.NavMeshRequest => "navmesh_request",
                M136CpuStageKind.ProjectileLoop => "projectile_loop",
                M136CpuStageKind.CombatHitEvaluation => "combat_hit_evaluation",
                M136CpuStageKind.HudRefresh => "hud_refresh",
                M136CpuStageKind.EnemyVisualPresenter => "enemy_visual_presenter",
                _ => "unknown"
            };
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
            stressHarnessNavMeshBakes = 0;
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
            stagedRoomVisibleRendererFrames = 0;
            normalTraversalRevealFrames = 0;
            roomReadyBeforeRevealMaxFrames = 0;
            poolWarmVisibleObjects = 0;
            poolWarmRootActiveErrors = 0;
            poolWarmActiveLeaks = 0;
            presentationFallbackVisuals = 0;
            roomEntryVfxBeforeReveal = 0;
            branchLoadingStarts = 0;
            branchLoadingCompletions = 0;
            bossLoadingStarts = 0;
            bossLoadingCompletions = 0;
            branchLoadingMaxMilliseconds = 0f;
            bossLoadingMaxMilliseconds = 0f;
            fullBranchPreloadRooms = 0;
            branchLiveRoomsBuilt = 0;
            branchLiveRoomBuildMaxMilliseconds = 0f;
            branchLiveRoomBuildGcMaxBytes = 0;
            branchLiveRoomCacheHits = 0;
            branchLiveRoomCacheMisses = 0;
            normalTraversalRoomRebuildCalls = 0;
            normalTraversalWarmCalls = 0;
            hibernatedRoomActiveObjectLeaks = 0;
            doorStatePreActivationMisses = 0;
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
            projectileActivePeak = 0;
            projectileActiveCurrent = 0;
            projectileSpawns = 0;
            projectileReturns = 0;
            projectileCollisionChecks = 0;
            projectilePoolMisses = 0;
            projectileHardInstantiates = 0;
            projectileUpdateMaxMilliseconds = 0f;
            tacticalReservationAttempts = 0;
            tacticalReservationPathSolves = 0;
            tacticalReservationCandidatesChecked = 0;
            tacticalCachedIntentReuses = 0;
            tacticalBossAddReservationSkips = 0;
            bossAddScorerSkips = 0;
            bossAddCachedCommandReuses = 0;
            tacticalCrowdReservationSkips = 0;
            tacticalCrowdCachedIntentReuses = 0;
            tacticalCrowdSupportReservationBudgetUses = 0;
            tacticalCrowdActiveThreatLimitMax = 0;
            tacticalCrowdScorerSkips = 0;
            cacheMissAttributionWriteIndex = 0;
            cacheMissAttributionCount = 0;
            Array.Clear(cacheMissAttributionRows, 0, cacheMissAttributionRows.Length);
            for (var index = 0; index < CpuStageCount; index++)
            {
                cpuStageCounts[index] = 0;
                cpuStageMaxMilliseconds[index] = 0f;
                cpuStageGcMaxBytes[index] = 0L;
            }
        }
    }
}
