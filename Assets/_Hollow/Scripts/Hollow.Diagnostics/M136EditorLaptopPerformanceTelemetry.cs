using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Hollow.Core.Diagnostics;
using Unity.Profiling;
using UnityEngine;

namespace Hollow.Diagnostics
{
    [Serializable]
    public sealed class M136PerformanceScenarioDefinition
    {
        public string id;
        public string displayName;
        public string intent;
        public float warmupSeconds;
        public float sampleSeconds;
    }

    [Serializable]
    public sealed class M136PerformanceMetricSummary
    {
        public string id;
        public string displayName;
        public string unit;
        public bool supported;
        public int sampleCount;
        public double min;
        public double average;
        public double p50;
        public double p95;
        public double max;
        public string note;
    }

    [Serializable]
    public sealed class M136RuntimeOperationSummary
    {
        public int miniMapRebuilds;
        public int wallVisibilityUpdates;
        public int combatHudRefreshes;
        public int runtimeNavMeshFallbacks;
        public int stressHarnessNavMeshBakes;
        public int roomTransitionEvents;
        public int runtimePoolWarmRequests;
        public int runtimePoolWarmCompletions;
        public int activeEnemyMax;
        public int aiThinkFull;
        public int aiThinkReduced;
        public int aiThinkBackground;
        public int aiCommandReuses;
        public int aiScorerCalls;
        public int aiScorerCandidates;
        public int aiBehaviorGraphTicks;
        public int aiLodTransitions;
        public int navPathRequests;
        public int navPathSolves;
        public int navPathDeferred;
        public int navPathFallbacks;
        public float navPathMaxSolveMilliseconds;
        public int avoidanceHigh;
        public int avoidanceReduced;
        public int avoidanceBackground;
        public int transitionStageCount;
        public float transitionMaxStageMilliseconds;
        public long transitionGcMaxBytes;
        public int transitionCurtainShows;
        public int transitionCurtainHides;
        public float transitionCurtainMaxVisibleMilliseconds;
        public int transitionCurtainMaxFramesAfterReady;
        public float transitionLockMaxMilliseconds;
        public int transitionOrphanCurtainsRemoved;
        public int roomBuildStageCount;
        public int transitionDestroyedObjectMaxPerFrame;
        public int enemySpawnSlices;
        public int bossActivationSlices;
        public int navMeshCatalogAttachCount;
        public int branchRuntimeCacheHits;
        public int branchRuntimeCacheMisses;
        public int branchRuntimeCacheEvictions;
        public int branchContentBuilds;
        public int branchGraphBuilds;
        public int branchRoomDistanceMapBuilds;
        public int branchFeaturePlanBuilds;
        public int branchRewardPlanBuilds;
        public int branchEncounterPlanBuilds;
        public int branchRoomAssetResolves;
        public int roomDescriptorBuilds;
        public int branchPreloadWarmRequests;
        public int branchPreloadWarmCompletions;
        public int branchPreloadSkippedStale;
        public int presentationMaterialCacheHits;
        public int presentationMaterialCacheMisses;
        public int presentationPrefabCacheHits;
        public int presentationPrefabCacheMisses;
        public int presentationBiomeCacheHits;
        public int presentationBiomeCacheMisses;
        public int presentationColliderStripPasses;
        public int stagedRoomVisibleRendererFrames;
        public int normalTraversalRevealFrames;
        public int roomReadyBeforeRevealMaxFrames;
        public int poolWarmVisibleObjects;
        public int poolWarmRootActiveErrors;
        public int poolWarmActiveLeaks;
        public int presentationFallbackVisuals;
        public int roomEntryVfxBeforeReveal;
        public int branchLoadingStarts;
        public int branchLoadingCompletions;
        public int bossLoadingStarts;
        public int bossLoadingCompletions;
        public float branchLoadingMaxMilliseconds;
        public float bossLoadingMaxMilliseconds;
        public int fullBranchPreloadRooms;
        public int branchLiveRoomsBuilt;
        public float branchLiveRoomBuildMaxMilliseconds;
        public long branchLiveRoomBuildGcMaxBytes;
        public int branchLiveRoomCacheHits;
        public int branchLiveRoomCacheMisses;
        public int normalTraversalRoomRebuildCalls;
        public int normalTraversalWarmCalls;
        public int hibernatedRoomActiveObjectLeaks;
        public int traversalColdCacheMisses;
        public int enemyPoolWarmRequests;
        public int enemyPoolWarmCompletions;
        public int enemyPoolRents;
        public int enemyPoolReturns;
        public int enemyPoolMisses;
        public int enemyPoolHardInstantiates;
        public int bootLoadingStarts;
        public int bootLoadingCompletions;
        public int bootLoadingFailures;
        public int bootLoadingStageCount;
        public float bootLoadingMaxStageMilliseconds;
        public float bootLoadingMaxMilliseconds;
        public int bootPreloadResourceLoads;
        public int bootPreloadWarmRequests;
        public int bootPreloadWarmCompletions;
        public int bootPreloadShaderWarmAttempts;
        public int bootPreloadShaderWarmCollections;
        public int bootPreloadShaderWarmSuccesses;
        public int bootPreloadShaderWarmMisses;
        public float bootPreloadShaderWarmMaxMilliseconds;
        public int m139StaleEnemyStateFailures;
        public int m139StaleRuntimePoolStateFailures;
        public int m139PoolActiveLeaks;
        public int m139CacheHitRateWindows;
        public int m139CacheHitRateWindowFailures;
        public int m139ShaderMaterialFirstUseMissesAfterLoad;
        public float m139ManagedMemoryDriftMaxMb;
        public float m139GraphicsMemoryDriftMaxMb;
        public string cpuStageSummary;
        public string tacticalDirectorSummary;
        public int tacticalCrowdReservationSkips;
        public int tacticalCrowdCachedIntentReuses;
        public int tacticalCrowdSupportReservationBudgetUses;
        public int tacticalCrowdActiveThreatLimitMax;
        public int tacticalCrowdScorerSkips;
        public string cacheMissAttributionSummary;
        public string[] cacheMissAttributionRows = Array.Empty<string>();
        public int projectileActivePeak;
        public int projectileSpawns;
        public int projectileReturns;
        public int projectileCollisionChecks;
        public int projectilePoolMisses;
        public int projectileHardInstantiates;
        public float projectileUpdateMaxMilliseconds;
    }

    [Serializable]
    public sealed class M136RenderSettingsSnapshot
    {
        public int targetFrameRate;
        public int vSyncCount;
        public string renderProfileKind;
        public string renderProfileName;
        public string renderPipelineAssetPath;
        public float polishRenderScale;
        public float urpRenderScale;
        public bool supportsHdr;
        public bool requiresDepthTexture;
        public bool requiresOpaqueTexture;
        public bool mainLightShadows;
        public int mainLightShadowmapResolution;
        public int additionalLightsRenderingMode;
        public int additionalLightsPerObjectLimit;
        public bool additionalLightShadows;
        public int additionalLightsShadowmapResolution;
        public float shadowDistance;
        public int shadowCascadeCount;
        public bool softShadows;
        public int softShadowQuality;
        public bool srpBatcher;
        public bool dynamicBatching;
        public bool cameraHdrAllowed;
        public bool cameraMsaaAllowed;
        public bool cameraPostProcessing;
        public bool bloomConfigured;
        public bool bloomActive;
        public float bloomIntensity;
        public string source;
    }

    [Serializable]
    public sealed class M136PerformanceScenarioResult
    {
        public string scenarioId;
        public string displayName;
        public string captureMode;
        public bool liveCaptured;
        public bool requiresManualCapture;
        public float warmupSeconds;
        public float sampleSeconds;
        public int rawSampleCount;
        public string captureId;
        public string captureStartedAtUtc;
        public string captureCompletedAtUtc;
        public string artifactDirectory;
        public string manifestPath;
        public string rawSampleCsvPath;
        public string profilerTracePath;
        public bool profilerTraceSupported;
        public string profilerTraceNote;
        public bool profilerTraceRequested;
        public bool fpsOverrideApplied;
        public int fpsOverrideTarget;
        public string samplingSource;
        public double sampleRateHz;
        public int expectedSampleCountMin;
        public int expectedSampleCountMax;
        public string frameCadenceConfidence;
        public string validityGrade;
        public string validityReason;
        public M136PerformanceMetricSummary[] metrics;
        public M136RuntimeOperationSummary operations;
        public M136LiveObjectCountSummary objectCounts;
        public string note;
    }

    [Serializable]
    public sealed class M136LiveObjectCountSnapshot
    {
        public int activeEnemies;
        public int activeProjectiles;
        public int activeVfx;
        public int activeUiCanvases;
        public int activeCameras;
        public int activeLights;
        public int activeRenderers;
        public int activeParticleSystems;
        public bool observedBranchSession;
        public bool observedSpaceshipHub;
        public bool observedDeveloperLab;
        public bool observedInterBranchHub;
        public bool observedCombatController;
        public bool observedActiveCombat;
        public bool observedWaveEncounter;
        public bool observedBoss;
        public string source;

        public static M136LiveObjectCountSnapshot Empty(string source = "not-collected")
        {
            return new M136LiveObjectCountSnapshot
            {
                source = source
            };
        }
    }

    [Serializable]
    public sealed class M136LiveObjectCountSummary
    {
        public int sampleCount;
        public int peakEnemies;
        public int peakProjectiles;
        public int peakVfx;
        public int peakUiCanvases;
        public int peakCameras;
        public int peakLights;
        public int peakRenderers;
        public int peakParticleSystems;
        public double averageEnemies;
        public double averageProjectiles;
        public double averageVfx;
        public double averageUiCanvases;
        public bool observedBranchSession;
        public bool observedSpaceshipHub;
        public bool observedDeveloperLab;
        public bool observedInterBranchHub;
        public bool observedCombatController;
        public bool observedActiveCombat;
        public bool observedWaveEncounter;
        public bool observedBoss;
        public string source;
    }

    [Serializable]
    public struct M136LiveFrameSample
    {
        public int sampleIndex;
        public float elapsedSeconds;
        public double frameTimeMs;
        public double fps;
        public int activeEnemies;
        public int activeProjectiles;
        public int activeVfx;
        public int activeUiCanvases;
        public int activeCameras;
        public int activeLights;
        public int activeRenderers;
        public int activeParticleSystems;
        public int miniMapRebuilds;
        public int wallVisibilityUpdates;
        public int combatHudRefreshes;
        public int runtimeNavMeshFallbacks;
        public int stressHarnessNavMeshBakes;
        public int roomTransitionEvents;
    }

    [Serializable]
    public sealed class M136LiveFrameSampleCollection
    {
        public M136LiveFrameSample[] samples;
    }

    [Serializable]
    public sealed class M136LiveCaptureManifest
    {
        public string lockId;
        public string captureId;
        public string captureMode;
        public string scenarioId;
        public string scenarioDisplayName;
        public string generatedAtUtc;
        public float warmupSeconds;
        public float sampleSeconds;
        public int sampleCount;
        public bool liveCaptured;
        public bool complete;
        public string artifactDirectory;
        public string manifestPath;
        public string scenarioJsonPath;
        public string rawSampleCsvPath;
        public string rawSampleJsonPath;
        public string profilerTracePath;
        public bool profilerTraceSupported;
        public string profilerTraceNote;
        public bool profilerTraceRequested;
        public bool fpsOverrideApplied;
        public int fpsOverrideTarget;
        public string samplingSource;
        public double sampleRateHz;
        public int expectedSampleCountMin;
        public int expectedSampleCountMax;
        public string frameCadenceConfidence;
        public string validityGrade;
        public string validityReason;
        public string[] unsupportedMetricIds;
        public M136LiveObjectCountSummary objectCounts;
        public string note;
    }

    [Serializable]
    public sealed class M136LiveCaptureExport
    {
        public M136LiveCaptureManifest manifest;
        public M136PerformanceScenarioResult scenarioResult;
        public M136LiveFrameSample[] samples;
    }

    [Serializable]
    public sealed class M136CaptureComparison
    {
        public string scenarioId;
        public string displayName;
        public bool hasComparison;
        public string previousCaptureId;
        public string latestCaptureId;
        public double previousFrameP95Ms;
        public double latestFrameP95Ms;
        public double deltaFrameP95Ms;
        public double previousFpsP50;
        public double latestFpsP50;
        public double deltaFpsP50;
        public string note;
    }

    public static class M136FrameCadencePolicy
    {
        public const string RuntimeUpdateSamplingSource = "playmode-monobehaviour-update";
        public const string UnknownSamplingSource = "unknown-or-legacy";
        public const string Trusted = "Trusted";
        public const string Directional = "Directional";
        public const string Invalid = "Invalid";

        public static (int min, int max) ExpectedSampleRange(float sampleSeconds, bool fpsOverrideApplied, int fpsOverrideTarget)
        {
            var seconds = Mathf.Max(1f, sampleSeconds);
            var minHz = fpsOverrideApplied && fpsOverrideTarget > 0 ? Mathf.Max(1, fpsOverrideTarget - 15) : 20;
            var maxHz = fpsOverrideApplied && fpsOverrideTarget > 0 ? fpsOverrideTarget + 15 : 240;
            return (
                Mathf.FloorToInt(minHz * seconds),
                Mathf.CeilToInt(maxHz * seconds));
        }

        public static string Classify(string samplingSource, double sampleRateHz, int sampleCount, int expectedMin, int expectedMax)
        {
            if (!string.Equals(samplingSource, RuntimeUpdateSamplingSource, StringComparison.Ordinal))
            {
                return Invalid;
            }

            if (sampleCount <= 0 || sampleCount < expectedMin)
            {
                return Invalid;
            }

            if (sampleCount > expectedMax || sampleRateHz > 240d)
            {
                return Directional;
            }

            return Trusted;
        }
    }

    public readonly struct M136CaptureValidity
    {
        public M136CaptureValidity(string grade, string reason)
        {
            Grade = grade;
            Reason = reason;
        }

        public string Grade { get; }

        public string Reason { get; }
    }

    public static class M136CaptureValidityClassifier
    {
        public const string Valid = "Valid";
        public const string Directional = "Directional";
        public const string Invalid = "Invalid";

        public static M136CaptureValidity Classify(M136LiveCaptureManifest manifest, M136PerformanceScenarioResult result)
        {
            if (manifest == null || result == null)
            {
                return new M136CaptureValidity(Invalid, "Missing manifest or scenario result.");
            }

            if (!manifest.complete || manifest.sampleCount <= 0 || result.rawSampleCount <= 0)
            {
                return new M136CaptureValidity(Invalid, "Capture is incomplete or has no sampled frames.");
            }

            var contentGate = ClassifyScenarioContent(manifest.scenarioId, result);
            if (contentGate.Grade == Invalid)
            {
                return contentGate;
            }

            if (!string.Equals(result.samplingSource, M136FrameCadencePolicy.RuntimeUpdateSamplingSource, StringComparison.Ordinal))
            {
                return new M136CaptureValidity(
                    contentGate.Grade == Valid ? Directional : contentGate.Grade,
                    $"{contentGate.Reason} Sampling source is legacy/editor-driven, so frame cadence remains provisional.");
            }

            if (result.frameCadenceConfidence == M136FrameCadencePolicy.Invalid)
            {
                return new M136CaptureValidity(Directional, $"{contentGate.Reason} Runtime sampling cadence is outside the expected frame range.");
            }

            if (result.frameCadenceConfidence == M136FrameCadencePolicy.Directional)
            {
                return new M136CaptureValidity(Directional, $"{contentGate.Reason} Runtime sampling was corrected, but cadence is high enough to treat frame/FPS metrics as directional.");
            }

            return contentGate;
        }

        private static M136CaptureValidity ClassifyScenarioContent(string scenarioId, M136PerformanceScenarioResult result)
        {
            var objectCounts = result.objectCounts;
            var operations = result.operations;
            var enemies = objectCounts?.peakEnemies ?? 0;
            var projectiles = objectCounts?.peakProjectiles ?? 0;
            var vfx = objectCounts?.peakVfx ?? 0;
            var truthMode = M136EditorLaptopPerformancePolicy.IsTruthCaptureMode(result.captureMode);
            var liveCacheActivationObserved =
                operations != null &&
                operations.branchLiveRoomCacheHits > 0 &&
                operations.normalTraversalRevealFrames > 0 &&
                operations.normalTraversalRoomRebuildCalls == 0 &&
                operations.normalTraversalWarmCalls == 0;
            return scenarioId switch
            {
                "ship_hub_idle" when objectCounts?.observedSpaceshipHub == true =>
                    new M136CaptureValidity(Valid, "Ship hub context was observed during the sample window."),
                "ship_hub_idle" when string.IsNullOrWhiteSpace(result.samplingSource) || result.samplingSource == M136FrameCadencePolicy.UnknownSamplingSource =>
                    new M136CaptureValidity(Directional, "Legacy capture lacks corrected ship-context evidence."),
                "ship_hub_idle" =>
                    new M136CaptureValidity(Invalid, "Ship hub capture did not observe spaceship hub context."),

                "normal_branch_idle" when objectCounts?.observedBranchSession == true && objectCounts.observedSpaceshipHub == false =>
                    new M136CaptureValidity(Valid, "Branch session context was observed and ship hub context was absent."),
                "normal_branch_idle" when string.IsNullOrWhiteSpace(result.samplingSource) || result.samplingSource == M136FrameCadencePolicy.UnknownSamplingSource =>
                    new M136CaptureValidity(Directional, "Legacy capture lacks corrected branch-context evidence."),
                "normal_branch_idle" =>
                    new M136CaptureValidity(Invalid, "Normal branch idle capture did not observe active branch context."),

                "active_combat_room" when objectCounts?.observedActiveCombat == true || enemies > 0 || projectiles > 0 || vfx > 0 =>
                    new M136CaptureValidity(Valid, "Combat content was observed through active combat state, enemies, projectiles, or VFX."),
                "active_combat_room" =>
                    new M136CaptureValidity(Invalid, "Combat capture has no active combat state, enemies, projectiles, or VFX."),

                "wave_crowded_room" when objectCounts?.observedWaveEncounter == true && enemies > 0 =>
                    new M136CaptureValidity(Valid, "Wave encounter state and enemies were observed."),
                "wave_crowded_room" =>
                    new M136CaptureValidity(Invalid, "Wave capture did not observe wave encounter state with enemies."),

                "anchor_boss_smoke" when objectCounts?.observedBoss == true =>
                    new M136CaptureValidity(Valid, "Boss runtime or active boss state was observed."),
                "anchor_boss_smoke" =>
                    new M136CaptureValidity(Invalid, "Boss capture did not observe a boss runtime or active boss state."),

                "room_transition_navmesh" when operations?.roomTransitionEvents > 0 =>
                    new M136CaptureValidity(Valid, "At least one room transition event was observed during the sample window."),
                "room_transition_navmesh" when string.IsNullOrWhiteSpace(result.samplingSource) || result.samplingSource == M136FrameCadencePolicy.UnknownSamplingSource =>
                    new M136CaptureValidity(Directional, "Legacy capture lacks corrected transition-event evidence."),
                "room_transition_navmesh" =>
                    new M136CaptureValidity(Invalid, "Room transition capture did not observe a door/room transition event."),

                "steady_room_idle" when truthMode =>
                    new M136CaptureValidity(Valid, "Truth-mode steady room sample completed with low-overhead object scans disabled."),
                "steady_room_idle" =>
                    new M136CaptureValidity(Directional, "Steady room sample is present, but it was not captured in truth mode."),

                "normal_traversal" when liveCacheActivationObserved =>
                    new M136CaptureValidity(Valid, "Live-room-cache activation was observed without room rebuilds or traversal warm calls."),
                "normal_traversal" =>
                    new M136CaptureValidity(Invalid, "Normal traversal capture did not observe a live-room-cache activation."),

                "return_traversal" when liveCacheActivationObserved =>
                    new M136CaptureValidity(Valid, "Return traversal used the live-room cache without room rebuilds or traversal warm calls."),
                "return_traversal" =>
                    new M136CaptureValidity(Invalid, "Return traversal capture did not observe a live-room-cache activation."),

                "spaceship_traversal" when liveCacheActivationObserved && (operations.cpuStageSummary ?? string.Empty).Contains("spaceship_terminal_activation") =>
                    new M136CaptureValidity(Valid, "Spaceship traversal used the live-room cache and terminal activation path."),
                "spaceship_traversal" when liveCacheActivationObserved =>
                    new M136CaptureValidity(Directional, "Spaceship traversal used the live-room cache, but terminal activation was not observed."),
                "spaceship_traversal" =>
                    new M136CaptureValidity(Invalid, "Spaceship traversal capture did not observe a live-room-cache activation."),

                "reward_room_entry" when liveCacheActivationObserved =>
                    new M136CaptureValidity(Valid, "Reward room entry used the live-room cache without room rebuilds or traversal warm calls."),
                "reward_room_entry" =>
                    new M136CaptureValidity(Invalid, "Reward room entry capture did not observe a live-room-cache activation."),

                "projectile_heavy_room" when Mathf.Max(projectiles, operations?.projectileActivePeak ?? 0) >= M136EditorLaptopPerformancePolicy.ProjectileHeavyMinimumPeakProjectiles &&
                                             (operations?.activeEnemyMax > 0 || objectCounts?.observedActiveCombat == true) =>
                    new M136CaptureValidity(Valid, "Projectile-heavy truth capture observed projectile pressure and combat activity."),
                "projectile_heavy_room" when operations?.activeEnemyMax > 0 && (operations.aiThinkFull + operations.aiThinkReduced + operations.aiThinkBackground > 0 || operations.navPathRequests > 0) =>
                    new M136CaptureValidity(Directional, "Projectile-heavy truth capture observed combat AI activity, but not the minimum projectile pressure."),
                "projectile_heavy_room" =>
                    new M136CaptureValidity(Invalid, "Projectile-heavy capture did not observe combat AI or projectile content."),

                "boss_entry" when objectCounts?.observedBoss == true || operations?.bossLoadingStarts > 0 || operations?.bossActivationSlices > 0 =>
                    new M136CaptureValidity(Valid, "Boss entry or boss activation activity was observed."),
                "boss_entry" =>
                    new M136CaptureValidity(Directional, "Boss entry capture is present, but boss activity was not observed in this sample."),

                _ => new M136CaptureValidity(Invalid, $"Unknown scenario id `{scenarioId}`.")
            };
        }
    }

    public sealed class M136CaptureFpsOverride : IDisposable
    {
        private readonly int previousTargetFrameRate;
        private readonly int previousVSyncCount;
        private bool applied;
        private bool disposed;

        public M136CaptureFpsOverride(bool enabled, int targetFrameRate = M136EditorLaptopPerformancePolicy.ComfortTargetFrameRate)
        {
            Enabled = enabled;
            TargetFrameRate = Mathf.Max(30, targetFrameRate);
            previousTargetFrameRate = Application.targetFrameRate;
            previousVSyncCount = QualitySettings.vSyncCount;
            if (!enabled)
            {
                return;
            }

            Application.targetFrameRate = TargetFrameRate;
            QualitySettings.vSyncCount = 0;
            applied = true;
        }

        public bool Enabled { get; }

        public int TargetFrameRate { get; }

        public bool Applied => applied;

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            if (applied)
            {
                Application.targetFrameRate = previousTargetFrameRate;
                QualitySettings.vSyncCount = previousVSyncCount;
                applied = false;
            }

            disposed = true;
        }
    }

    public static class M136EditorLaptopPerformancePolicy
    {
        public const string LockId = "M136";
        public const string Title = "M136 Editor Laptop Performance + Power Investigation";
        public const int ComfortTargetFrameRate = 60;
        public const float FixedWarmupSeconds = 3f;
        public const float FixedSampleSeconds = 30f;
        public const string ManualCaptureMode = "manual-live-editor";
        public const string AutomatedCaptureMode = "automated-live-editor-smoke";
        public const string TruthCaptureMode = "truth-live-editor";
        public const string DiagnosticCaptureMode = "diagnostic-live-editor";

        public static readonly string[] RequiredMetricIds =
        {
            "frame_time_ms",
            "fps",
            "main_thread_ms",
            "render_thread_ms",
            "gpu_frame_ms",
            "gc_allocated_bytes",
            "gc_allocated_recorder_bytes",
            "gc_allocated_frame_delta_bytes",
            "managed_memory_mb",
            "graphics_memory_mb"
        };

        public const int ProjectileHeavyMinimumPeakProjectiles = 24;

        public static readonly string[] RequiredScenarioIds =
        {
            "ship_hub_idle",
            "normal_branch_idle",
            "active_combat_room",
            "wave_crowded_room",
            "anchor_boss_smoke",
            "room_transition_navmesh",
            "steady_room_idle",
            "normal_traversal",
            "return_traversal",
            "spaceship_traversal",
            "reward_room_entry",
            "projectile_heavy_room",
            "boss_entry"
        };

        public static M136PerformanceScenarioDefinition[] ScenarioManifest => new[]
        {
            Scenario("ship_hub_idle", "Ship Hub Idle", "Spaceship hub idle cost, UI, camera, and ship presentation baseline."),
            Scenario("normal_branch_idle", "Normal Branch Idle", "World branch room idle cost with runtime room, minimap, walls, and pickups."),
            Scenario("active_combat_room", "Active Combat Room", "Ordinary combat cost with enemies, melee/ranged feedback, HUD, and projectiles."),
            Scenario("wave_crowded_room", "Wave / Crowded Room", "Crowded wave-room budget pressure with staged spawns and a reward chest endpoint."),
            Scenario("anchor_boss_smoke", "Anchor Boss Smoke", "Boss HUD, arena, attacks, projectiles, VFX, and clear/reward flow smoke."),
            Scenario("room_transition_navmesh", "Room Transition + NavMesh", "Branch room load, door transition, room build, and NavMesh attach/fallback spikes."),
            Scenario("steady_room_idle", "Steady Room Idle", "Truth-mode steady gameplay after branch load and warmup."),
            Scenario("normal_traversal", "Normal Traversal", "Live-room-cache activation between ordinary branch rooms."),
            Scenario("return_traversal", "Return Traversal", "Re-entering a previously visited live-cached room."),
            Scenario("spaceship_traversal", "Spaceship Traversal", "Spaceship branch room activation and terminal binding."),
            Scenario("reward_room_entry", "Reward Room Entry", "Reward/interactable room activation after branch loading."),
            Scenario("projectile_heavy_room", "Projectile Heavy Room", "Projectile-heavy combat pressure with truth-mode CPU sampling."),
            Scenario("boss_entry", "Boss Entry", "Boss-room loading, activation, and first playable frame.")
        };

        public static bool IsTruthCaptureMode(string captureMode)
        {
            return string.Equals(captureMode, TruthCaptureMode, StringComparison.Ordinal) ||
                   string.Equals(captureMode, "m138-automated-playmode", StringComparison.Ordinal) ||
                   string.Equals(captureMode, "m140-built-player", StringComparison.Ordinal) ||
                   string.Equals(captureMode, "m140-built-player-truth", StringComparison.Ordinal);
        }

        public static bool IsDiagnosticCaptureMode(string captureMode)
        {
            return string.Equals(captureMode, DiagnosticCaptureMode, StringComparison.Ordinal) ||
                   string.Equals(captureMode, ManualCaptureMode, StringComparison.Ordinal) ||
                   string.Equals(captureMode, AutomatedCaptureMode, StringComparison.Ordinal);
        }

        public static bool ShouldCollectObjectCountsDuringCapture(string captureMode)
        {
            return IsDiagnosticCaptureMode(captureMode);
        }

        public static bool IsTelemetryAllowed(bool isEditor, bool isDebugBuild)
        {
            return isEditor || isDebugBuild;
        }

        public static bool IsTelemetryAllowedNow()
        {
            return IsTelemetryAllowed(Application.isEditor, Debug.isDebugBuild);
        }

        public static bool ValidateScenarioManifest(out string detail)
        {
            var scenarios = ScenarioManifest;
            var duplicate = scenarios
                .GroupBy(scenario => scenario.id)
                .FirstOrDefault(group => group.Count() > 1);
            if (duplicate != null)
            {
                detail = $"Duplicate scenario id `{duplicate.Key}`.";
                return false;
            }

            var missingTiming = scenarios
                .FirstOrDefault(scenario =>
                    Math.Abs(scenario.warmupSeconds - FixedWarmupSeconds) > 0.001f ||
                    Math.Abs(scenario.sampleSeconds - FixedSampleSeconds) > 0.001f);
            if (missingTiming != null)
            {
                detail = $"Scenario `{missingTiming.id}` does not use the fixed M136 warmup/sample window.";
                return false;
            }

            detail = $"M136 scenario manifest contains {scenarios.Length} representative editor-laptop scenarios.";
            var missingRequired = RequiredScenarioIds
                .Where(id => scenarios.All(scenario => scenario.id != id))
                .ToArray();
            if (missingRequired.Length > 0)
            {
                detail = $"M136 scenario manifest is missing required scenarios: {string.Join(", ", missingRequired)}.";
                return false;
            }

            return true;
        }

        public static bool ValidateScenarioResult(M136PerformanceScenarioResult result, out string detail)
        {
            if (result == null)
            {
                detail = "Scenario result is null.";
                return false;
            }

            var metricIds = new HashSet<string>((result.metrics ?? Array.Empty<M136PerformanceMetricSummary>()).Select(metric => metric.id));
            var missing = RequiredMetricIds.Where(id => !metricIds.Contains(id)).ToArray();
            if (missing.Length > 0)
            {
                detail = $"{result.scenarioId} missing required metrics: {string.Join(", ", missing)}.";
                return false;
            }

            detail = $"{result.scenarioId} includes required metrics and operation counters.";
            return true;
        }

        public static M136PerformanceScenarioDefinition FindScenario(string scenarioId)
        {
            return ScenarioManifest.FirstOrDefault(scenario => scenario.id == scenarioId);
        }

        private static M136PerformanceScenarioDefinition Scenario(string id, string displayName, string intent)
        {
            return new M136PerformanceScenarioDefinition
            {
                id = id,
                displayName = displayName,
                intent = intent,
                warmupSeconds = FixedWarmupSeconds,
                sampleSeconds = FixedSampleSeconds
            };
        }
    }

    public sealed class M136FrameTelemetrySampler : IDisposable
    {
        private readonly List<double> frameTimeMs;
        private readonly List<double> mainThreadMs;
        private readonly List<double> renderThreadMs;
        private readonly List<double> gpuFrameMs;
        private readonly List<double> gcAllocatedBytes;
        private readonly List<double> gcAllocatedRecorderBytes;
        private readonly List<double> gcAllocatedFrameDeltaBytes;
        private readonly List<double> managedMemoryMb;
        private readonly List<double> graphicsMemoryMb;
        private readonly List<M136LiveObjectCountSnapshot> objectSnapshots;
        private readonly List<M136LiveFrameSample> frameSamples;
        private ProfilerRecorder mainThreadRecorder;
        private ProfilerRecorder renderThreadRecorder;
        private ProfilerRecorder gcAllocRecorder;
        private ProfilerRecorder managedMemoryRecorder;
        private ProfilerRecorder graphicsMemoryRecorder;
        private float sampledElapsedSeconds;
        private long lastThreadAllocatedBytes;
        private bool hasThreadAllocatedBaseline;

        public M136FrameTelemetrySampler(int expectedSampleCapacity = 0)
        {
            var capacity = Mathf.Max(0, expectedSampleCapacity);
            frameTimeMs = new List<double>(capacity);
            mainThreadMs = new List<double>(capacity);
            renderThreadMs = new List<double>(capacity);
            gpuFrameMs = new List<double>(capacity);
            gcAllocatedBytes = new List<double>(capacity);
            gcAllocatedRecorderBytes = new List<double>(capacity);
            gcAllocatedFrameDeltaBytes = new List<double>(capacity);
            managedMemoryMb = new List<double>(capacity);
            graphicsMemoryMb = new List<double>(capacity);
            objectSnapshots = new List<M136LiveObjectCountSnapshot>(capacity);
            frameSamples = new List<M136LiveFrameSample>(capacity);
        }

        public IReadOnlyList<M136LiveFrameSample> FrameSamples => frameSamples;

        public int FrameSampleCapacity => frameSamples.Capacity;

        public bool UsesValueTypeFrameSamples => typeof(M136LiveFrameSample).IsValueType;

        public void Begin()
        {
            M136PerformanceOperationCounters.Reset();
            hasThreadAllocatedBaseline = false;
            lastThreadAllocatedBytes = 0L;
            if (!M136EditorLaptopPerformancePolicy.IsTelemetryAllowedNow())
            {
                return;
            }

            TryStartRecorder(ref mainThreadRecorder, ProfilerCategory.Internal, "Main Thread");
            TryStartRecorder(ref renderThreadRecorder, ProfilerCategory.Internal, "Render Thread");
            TryStartRecorder(ref gcAllocRecorder, ProfilerCategory.Memory, "GC Allocated In Frame");
            TryStartRecorder(ref managedMemoryRecorder, ProfilerCategory.Memory, "Total Used Memory");
            TryStartRecorder(ref graphicsMemoryRecorder, ProfilerCategory.Memory, "Gfx Used Memory");
        }

        public void RecordFrame(float deltaTimeSeconds)
        {
            RecordFrame(deltaTimeSeconds, M136LiveObjectCountSnapshot.Empty());
        }

        public void RecordFrame(float deltaTimeSeconds, M136LiveObjectCountSnapshot objectSnapshot)
        {
            if (deltaTimeSeconds > 0f)
            {
                sampledElapsedSeconds += deltaTimeSeconds;
                frameTimeMs.Add(deltaTimeSeconds * 1000d);
            }

            AddGpuFrameTiming(gpuFrameMs);
            AddRecorderMilliseconds(mainThreadRecorder, mainThreadMs);
            AddRecorderMilliseconds(renderThreadRecorder, renderThreadMs);
            AddRecorderBytes(gcAllocRecorder, gcAllocatedRecorderBytes);
            AddThreadAllocationDelta(gcAllocatedFrameDeltaBytes);
            AddRecorderMegabytes(managedMemoryRecorder, managedMemoryMb);
            AddRecorderMegabytes(graphicsMemoryRecorder, graphicsMemoryMb);

            var snapshot = objectSnapshot ?? M136LiveObjectCountSnapshot.Empty();
            objectSnapshots.Add(snapshot);
            var operations = M136PerformanceOperationCounters.Snapshot();
            var frameMs = deltaTimeSeconds > 0f ? deltaTimeSeconds * 1000d : 0d;
            frameSamples.Add(new M136LiveFrameSample
            {
                sampleIndex = frameSamples.Count,
                elapsedSeconds = sampledElapsedSeconds,
                frameTimeMs = frameMs,
                fps = frameMs > 0d ? 1000d / frameMs : 0d,
                activeEnemies = snapshot.activeEnemies,
                activeProjectiles = snapshot.activeProjectiles,
                activeVfx = snapshot.activeVfx,
                activeUiCanvases = snapshot.activeUiCanvases,
                activeCameras = snapshot.activeCameras,
                activeLights = snapshot.activeLights,
                activeRenderers = snapshot.activeRenderers,
                activeParticleSystems = snapshot.activeParticleSystems,
                miniMapRebuilds = operations.MiniMapRebuilds,
                wallVisibilityUpdates = operations.WallVisibilityUpdates,
                combatHudRefreshes = operations.CombatHudRefreshes,
                runtimeNavMeshFallbacks = operations.RuntimeNavMeshFallbacks,
                stressHarnessNavMeshBakes = operations.StressHarnessNavMeshBakes,
                roomTransitionEvents = operations.RoomTransitionEvents
            });
        }

        public void RecordSyntheticFrame(
            double frameMs,
            double gcBytes,
            double mainMs,
            double renderMs,
            double managedMb,
            double graphicsMb,
            double gpuMs = -1d)
        {
            if (frameMs > 0d)
            {
                frameTimeMs.Add(frameMs);
                sampledElapsedSeconds += (float)(frameMs / 1000d);
            }

            if (gcBytes >= 0d)
            {
                gcAllocatedBytes.Add(gcBytes);
                gcAllocatedRecorderBytes.Add(gcBytes);
                gcAllocatedFrameDeltaBytes.Add(gcBytes);
            }

            if (mainMs >= 0d)
            {
                mainThreadMs.Add(mainMs);
            }

            if (renderMs >= 0d)
            {
                renderThreadMs.Add(renderMs);
            }

            if (managedMb >= 0d)
            {
                managedMemoryMb.Add(managedMb);
            }

            if (graphicsMb >= 0d)
            {
                graphicsMemoryMb.Add(graphicsMb);
            }

            if (gpuMs >= 0d)
            {
                gpuFrameMs.Add(gpuMs);
            }

            var operations = M136PerformanceOperationCounters.Snapshot();
            frameSamples.Add(new M136LiveFrameSample
            {
                sampleIndex = frameSamples.Count,
                elapsedSeconds = sampledElapsedSeconds,
                frameTimeMs = frameMs,
                fps = frameMs > 0d ? 1000d / frameMs : 0d,
                miniMapRebuilds = operations.MiniMapRebuilds,
                wallVisibilityUpdates = operations.WallVisibilityUpdates,
                combatHudRefreshes = operations.CombatHudRefreshes,
                runtimeNavMeshFallbacks = operations.RuntimeNavMeshFallbacks,
                stressHarnessNavMeshBakes = operations.StressHarnessNavMeshBakes,
                roomTransitionEvents = operations.RoomTransitionEvents
            });
        }

        public M136PerformanceScenarioResult BuildScenarioResult(
            M136PerformanceScenarioDefinition scenario,
            string captureMode,
            bool liveCaptured,
            bool requiresManualCapture,
            string note,
            string captureId = null,
            string captureStartedAtUtc = null,
            string captureCompletedAtUtc = null,
            string artifactDirectory = null,
            string manifestPath = null,
            string rawSampleCsvPath = null,
            string profilerTracePath = null,
            bool profilerTraceSupported = false,
            string profilerTraceNote = null,
            bool profilerTraceRequested = false,
            bool fpsOverrideApplied = false,
            int fpsOverrideTarget = 0,
            string samplingSource = null)
        {
            var operationSnapshot = M136PerformanceOperationCounters.Snapshot();
            var sampleRate = scenario.sampleSeconds > 0f ? frameSamples.Count / scenario.sampleSeconds : 0d;
            var expectedRange = M136FrameCadencePolicy.ExpectedSampleRange(scenario.sampleSeconds, fpsOverrideApplied, fpsOverrideTarget);
            var cadenceConfidence = M136FrameCadencePolicy.Classify(
                samplingSource,
                sampleRate,
                frameSamples.Count,
                expectedRange.min,
                expectedRange.max);
            return new M136PerformanceScenarioResult
            {
                scenarioId = scenario.id,
                displayName = scenario.displayName,
                captureMode = captureMode,
                liveCaptured = liveCaptured,
                requiresManualCapture = requiresManualCapture,
                warmupSeconds = scenario.warmupSeconds,
                sampleSeconds = scenario.sampleSeconds,
                rawSampleCount = frameSamples.Count,
                captureId = captureId,
                captureStartedAtUtc = captureStartedAtUtc,
                captureCompletedAtUtc = captureCompletedAtUtc,
                artifactDirectory = artifactDirectory,
                manifestPath = manifestPath,
                rawSampleCsvPath = rawSampleCsvPath,
                profilerTracePath = profilerTracePath,
                profilerTraceSupported = profilerTraceSupported,
                profilerTraceNote = profilerTraceNote,
                profilerTraceRequested = profilerTraceRequested,
                fpsOverrideApplied = fpsOverrideApplied,
                fpsOverrideTarget = fpsOverrideTarget,
                samplingSource = string.IsNullOrWhiteSpace(samplingSource) ? M136FrameCadencePolicy.UnknownSamplingSource : samplingSource,
                sampleRateHz = sampleRate,
                expectedSampleCountMin = expectedRange.min,
                expectedSampleCountMax = expectedRange.max,
                frameCadenceConfidence = cadenceConfidence,
                metrics = BuildMetricSummaries().ToArray(),
                operations = new M136RuntimeOperationSummary
                {
                    miniMapRebuilds = operationSnapshot.MiniMapRebuilds,
                    wallVisibilityUpdates = operationSnapshot.WallVisibilityUpdates,
                    combatHudRefreshes = operationSnapshot.CombatHudRefreshes,
                    runtimeNavMeshFallbacks = operationSnapshot.RuntimeNavMeshFallbacks,
                    stressHarnessNavMeshBakes = operationSnapshot.StressHarnessNavMeshBakes,
                    roomTransitionEvents = operationSnapshot.RoomTransitionEvents,
                    runtimePoolWarmRequests = operationSnapshot.RuntimePoolWarmRequests,
                    runtimePoolWarmCompletions = operationSnapshot.RuntimePoolWarmCompletions,
                    activeEnemyMax = operationSnapshot.ActiveEnemyMax,
                    aiThinkFull = operationSnapshot.AiThinkFull,
                    aiThinkReduced = operationSnapshot.AiThinkReduced,
                    aiThinkBackground = operationSnapshot.AiThinkBackground,
                    aiCommandReuses = operationSnapshot.AiCommandReuses,
                    aiScorerCalls = operationSnapshot.AiScorerCalls,
                    aiScorerCandidates = operationSnapshot.AiScorerCandidates,
                    aiBehaviorGraphTicks = operationSnapshot.AiBehaviorGraphTicks,
                    aiLodTransitions = operationSnapshot.AiLodTransitions,
                    navPathRequests = operationSnapshot.NavPathRequests,
                    navPathSolves = operationSnapshot.NavPathSolves,
                    navPathDeferred = operationSnapshot.NavPathDeferred,
                    navPathFallbacks = operationSnapshot.NavPathFallbacks,
                    navPathMaxSolveMilliseconds = operationSnapshot.NavPathMaxSolveMilliseconds,
                    avoidanceHigh = operationSnapshot.AvoidanceHigh,
                    avoidanceReduced = operationSnapshot.AvoidanceReduced,
                    avoidanceBackground = operationSnapshot.AvoidanceBackground,
                    transitionStageCount = operationSnapshot.TransitionStageCount,
                    transitionMaxStageMilliseconds = operationSnapshot.TransitionMaxStageMilliseconds,
                    transitionGcMaxBytes = operationSnapshot.TransitionGcMaxBytes,
                    transitionCurtainShows = operationSnapshot.TransitionCurtainShows,
                    transitionCurtainHides = operationSnapshot.TransitionCurtainHides,
                    transitionCurtainMaxVisibleMilliseconds = operationSnapshot.TransitionCurtainMaxVisibleMilliseconds,
                    transitionCurtainMaxFramesAfterReady = operationSnapshot.TransitionCurtainMaxFramesAfterReady,
                    transitionLockMaxMilliseconds = operationSnapshot.TransitionLockMaxMilliseconds,
                    transitionOrphanCurtainsRemoved = operationSnapshot.TransitionOrphanCurtainsRemoved,
                    roomBuildStageCount = operationSnapshot.RoomBuildStageCount,
                    transitionDestroyedObjectMaxPerFrame = operationSnapshot.TransitionDestroyedObjectMaxPerFrame,
                    enemySpawnSlices = operationSnapshot.EnemySpawnSlices,
                    bossActivationSlices = operationSnapshot.BossActivationSlices,
                    navMeshCatalogAttachCount = operationSnapshot.NavMeshCatalogAttachCount,
                    branchRuntimeCacheHits = operationSnapshot.BranchRuntimeCacheHits,
                    branchRuntimeCacheMisses = operationSnapshot.BranchRuntimeCacheMisses,
                    branchRuntimeCacheEvictions = operationSnapshot.BranchRuntimeCacheEvictions,
                    branchContentBuilds = operationSnapshot.BranchContentBuilds,
                    branchGraphBuilds = operationSnapshot.BranchGraphBuilds,
                    branchRoomDistanceMapBuilds = operationSnapshot.BranchRoomDistanceMapBuilds,
                    branchFeaturePlanBuilds = operationSnapshot.BranchFeaturePlanBuilds,
                    branchRewardPlanBuilds = operationSnapshot.BranchRewardPlanBuilds,
                    branchEncounterPlanBuilds = operationSnapshot.BranchEncounterPlanBuilds,
                    branchRoomAssetResolves = operationSnapshot.BranchRoomAssetResolves,
                    roomDescriptorBuilds = operationSnapshot.RoomDescriptorBuilds,
                    branchPreloadWarmRequests = operationSnapshot.BranchPreloadWarmRequests,
                    branchPreloadWarmCompletions = operationSnapshot.BranchPreloadWarmCompletions,
                    branchPreloadSkippedStale = operationSnapshot.BranchPreloadSkippedStale,
                    presentationMaterialCacheHits = operationSnapshot.PresentationMaterialCacheHits,
                    presentationMaterialCacheMisses = operationSnapshot.PresentationMaterialCacheMisses,
                    presentationPrefabCacheHits = operationSnapshot.PresentationPrefabCacheHits,
                    presentationPrefabCacheMisses = operationSnapshot.PresentationPrefabCacheMisses,
                    presentationBiomeCacheHits = operationSnapshot.PresentationBiomeCacheHits,
                    presentationBiomeCacheMisses = operationSnapshot.PresentationBiomeCacheMisses,
                    presentationColliderStripPasses = operationSnapshot.PresentationColliderStripPasses,
                    stagedRoomVisibleRendererFrames = operationSnapshot.StagedRoomVisibleRendererFrames,
                    normalTraversalRevealFrames = operationSnapshot.NormalTraversalRevealFrames,
                    roomReadyBeforeRevealMaxFrames = operationSnapshot.RoomReadyBeforeRevealMaxFrames,
                    poolWarmVisibleObjects = operationSnapshot.PoolWarmVisibleObjects,
                    poolWarmRootActiveErrors = operationSnapshot.PoolWarmRootActiveErrors,
                    poolWarmActiveLeaks = operationSnapshot.PoolWarmActiveLeaks,
                    presentationFallbackVisuals = operationSnapshot.PresentationFallbackVisuals,
                    roomEntryVfxBeforeReveal = operationSnapshot.RoomEntryVfxBeforeReveal,
                    branchLoadingStarts = operationSnapshot.BranchLoadingStarts,
                    branchLoadingCompletions = operationSnapshot.BranchLoadingCompletions,
                    bossLoadingStarts = operationSnapshot.BossLoadingStarts,
                    bossLoadingCompletions = operationSnapshot.BossLoadingCompletions,
                    branchLoadingMaxMilliseconds = operationSnapshot.BranchLoadingMaxMilliseconds,
                    bossLoadingMaxMilliseconds = operationSnapshot.BossLoadingMaxMilliseconds,
                    fullBranchPreloadRooms = operationSnapshot.FullBranchPreloadRooms,
                    branchLiveRoomsBuilt = operationSnapshot.BranchLiveRoomsBuilt,
                    branchLiveRoomBuildMaxMilliseconds = operationSnapshot.BranchLiveRoomBuildMaxMilliseconds,
                    branchLiveRoomBuildGcMaxBytes = operationSnapshot.BranchLiveRoomBuildGcMaxBytes,
                    branchLiveRoomCacheHits = operationSnapshot.BranchLiveRoomCacheHits,
                    branchLiveRoomCacheMisses = operationSnapshot.BranchLiveRoomCacheMisses,
                    normalTraversalRoomRebuildCalls = operationSnapshot.NormalTraversalRoomRebuildCalls,
                    normalTraversalWarmCalls = operationSnapshot.NormalTraversalWarmCalls,
                    hibernatedRoomActiveObjectLeaks = operationSnapshot.HibernatedRoomActiveObjectLeaks,
                    traversalColdCacheMisses = operationSnapshot.TraversalColdCacheMisses,
                    enemyPoolWarmRequests = operationSnapshot.EnemyPoolWarmRequests,
                    enemyPoolWarmCompletions = operationSnapshot.EnemyPoolWarmCompletions,
                    enemyPoolRents = operationSnapshot.EnemyPoolRents,
                    enemyPoolReturns = operationSnapshot.EnemyPoolReturns,
                    enemyPoolMisses = operationSnapshot.EnemyPoolMisses,
                    enemyPoolHardInstantiates = operationSnapshot.EnemyPoolHardInstantiates,
                    bootLoadingStarts = operationSnapshot.BootLoadingStarts,
                    bootLoadingCompletions = operationSnapshot.BootLoadingCompletions,
                    bootLoadingFailures = operationSnapshot.BootLoadingFailures,
                    bootLoadingStageCount = operationSnapshot.BootLoadingStageCount,
                    bootLoadingMaxStageMilliseconds = operationSnapshot.BootLoadingMaxStageMilliseconds,
                    bootLoadingMaxMilliseconds = operationSnapshot.BootLoadingMaxMilliseconds,
                    bootPreloadResourceLoads = operationSnapshot.BootPreloadResourceLoads,
                    bootPreloadWarmRequests = operationSnapshot.BootPreloadWarmRequests,
                    bootPreloadWarmCompletions = operationSnapshot.BootPreloadWarmCompletions,
                    bootPreloadShaderWarmAttempts = operationSnapshot.BootPreloadShaderWarmAttempts,
                    bootPreloadShaderWarmCollections = operationSnapshot.BootPreloadShaderWarmCollections,
                    bootPreloadShaderWarmSuccesses = operationSnapshot.BootPreloadShaderWarmSuccesses,
                    bootPreloadShaderWarmMisses = operationSnapshot.BootPreloadShaderWarmMisses,
                    bootPreloadShaderWarmMaxMilliseconds = operationSnapshot.BootPreloadShaderWarmMaxMilliseconds,
                    m139StaleEnemyStateFailures = operationSnapshot.M139StaleEnemyStateFailures,
                    m139StaleRuntimePoolStateFailures = operationSnapshot.M139StaleRuntimePoolStateFailures,
                    m139PoolActiveLeaks = operationSnapshot.M139PoolActiveLeaks,
                    m139CacheHitRateWindows = operationSnapshot.M139CacheHitRateWindows,
                    m139CacheHitRateWindowFailures = operationSnapshot.M139CacheHitRateWindowFailures,
                    m139ShaderMaterialFirstUseMissesAfterLoad = operationSnapshot.M139ShaderMaterialFirstUseMissesAfterLoad,
                    m139ManagedMemoryDriftMaxMb = operationSnapshot.M139ManagedMemoryDriftMaxMb,
                    m139GraphicsMemoryDriftMaxMb = operationSnapshot.M139GraphicsMemoryDriftMaxMb,
                    cpuStageSummary = operationSnapshot.CpuStageSummary,
                    tacticalDirectorSummary = operationSnapshot.TacticalDirectorSummary,
                    tacticalCrowdReservationSkips = operationSnapshot.TacticalCrowdReservationSkips,
                    tacticalCrowdCachedIntentReuses = operationSnapshot.TacticalCrowdCachedIntentReuses,
                    tacticalCrowdSupportReservationBudgetUses = operationSnapshot.TacticalCrowdSupportReservationBudgetUses,
                    tacticalCrowdActiveThreatLimitMax = operationSnapshot.TacticalCrowdActiveThreatLimitMax,
                    tacticalCrowdScorerSkips = operationSnapshot.TacticalCrowdScorerSkips,
                    cacheMissAttributionSummary = operationSnapshot.CacheMissAttributionSummary,
                    cacheMissAttributionRows = operationSnapshot.CacheMissAttributionRows,
                    projectileActivePeak = operationSnapshot.ProjectileActivePeak,
                    projectileSpawns = operationSnapshot.ProjectileSpawns,
                    projectileReturns = operationSnapshot.ProjectileReturns,
                    projectileCollisionChecks = operationSnapshot.ProjectileCollisionChecks,
                    projectilePoolMisses = operationSnapshot.ProjectilePoolMisses,
                    projectileHardInstantiates = operationSnapshot.ProjectileHardInstantiates,
                    projectileUpdateMaxMilliseconds = operationSnapshot.ProjectileUpdateMaxMilliseconds
                },
                objectCounts = BuildObjectCountSummary(),
                note = note
            };
        }

        public void Dispose()
        {
            DisposeRecorder(ref mainThreadRecorder);
            DisposeRecorder(ref renderThreadRecorder);
            DisposeRecorder(ref gcAllocRecorder);
            DisposeRecorder(ref managedMemoryRecorder);
            DisposeRecorder(ref graphicsMemoryRecorder);
        }

        private IEnumerable<M136PerformanceMetricSummary> BuildMetricSummaries()
        {
            yield return Metric("frame_time_ms", "Frame Time", "ms", frameTimeMs, "Derived from sampled delta time.");
            yield return Metric("fps", "Frames Per Second", "fps", frameTimeMs.Select(value => value > 0d ? 1000d / value : 0d), "Derived from sampled frame time.");
            yield return Metric("main_thread_ms", "Main Thread", "ms", mainThreadMs, RecorderNote(mainThreadRecorder, "Unity ProfilerRecorder main-thread timing."));
            yield return Metric("render_thread_ms", "Render Thread", "ms", renderThreadMs, RecorderNote(renderThreadRecorder, "Unity ProfilerRecorder render-thread timing."));
            yield return Metric("gpu_frame_ms", "GPU Frame", "ms", gpuFrameMs, "Unity FrameTimingManager GPU timing when the current Editor/platform exposes it; unsupported contexts are reported explicitly.");
            yield return Metric("gc_allocated_bytes", "GC Allocated In Frame", "bytes", gcAllocatedFrameDeltaBytes.Count > 0 ? gcAllocatedFrameDeltaBytes : gcAllocatedRecorderBytes, "Compatibility alias for verified per-frame thread allocation delta when available; falls back to the Unity recorder value.");
            yield return Metric("gc_allocated_recorder_bytes", "GC Allocated Recorder", "bytes", gcAllocatedRecorderBytes, RecorderNote(gcAllocRecorder, "Raw Unity ProfilerRecorder GC allocation counter."));
            yield return Metric("gc_allocated_frame_delta_bytes", "GC Allocated Frame Delta", "bytes", gcAllocatedFrameDeltaBytes, "Verified per-sample delta from GC.GetAllocatedBytesForCurrentThread; low-overhead truth-mode allocation signal.");
            yield return Metric("managed_memory_mb", "Managed Memory", "MB", managedMemoryMb, RecorderNote(managedMemoryRecorder, "Unity ProfilerRecorder total used memory counter."));
            yield return Metric("graphics_memory_mb", "Graphics Memory", "MB", graphicsMemoryMb, RecorderNote(graphicsMemoryRecorder, "Unity ProfilerRecorder graphics memory counter."));
        }

        private M136LiveObjectCountSummary BuildObjectCountSummary()
        {
            var snapshots = objectSnapshots.Where(snapshot => snapshot != null).ToArray();
            if (snapshots.Length == 0)
            {
                return new M136LiveObjectCountSummary
                {
                    source = "not-collected"
                };
            }

            return new M136LiveObjectCountSummary
            {
                sampleCount = snapshots.Length,
                peakEnemies = snapshots.Max(snapshot => snapshot.activeEnemies),
                peakProjectiles = snapshots.Max(snapshot => snapshot.activeProjectiles),
                peakVfx = snapshots.Max(snapshot => snapshot.activeVfx),
                peakUiCanvases = snapshots.Max(snapshot => snapshot.activeUiCanvases),
                peakCameras = snapshots.Max(snapshot => snapshot.activeCameras),
                peakLights = snapshots.Max(snapshot => snapshot.activeLights),
                peakRenderers = snapshots.Max(snapshot => snapshot.activeRenderers),
                peakParticleSystems = snapshots.Max(snapshot => snapshot.activeParticleSystems),
                averageEnemies = snapshots.Average(snapshot => snapshot.activeEnemies),
                averageProjectiles = snapshots.Average(snapshot => snapshot.activeProjectiles),
                averageVfx = snapshots.Average(snapshot => snapshot.activeVfx),
                averageUiCanvases = snapshots.Average(snapshot => snapshot.activeUiCanvases),
                observedBranchSession = snapshots.Any(snapshot => snapshot.observedBranchSession),
                observedSpaceshipHub = snapshots.Any(snapshot => snapshot.observedSpaceshipHub),
                observedDeveloperLab = snapshots.Any(snapshot => snapshot.observedDeveloperLab),
                observedInterBranchHub = snapshots.Any(snapshot => snapshot.observedInterBranchHub),
                observedCombatController = snapshots.Any(snapshot => snapshot.observedCombatController),
                observedActiveCombat = snapshots.Any(snapshot => snapshot.observedActiveCombat),
                observedWaveEncounter = snapshots.Any(snapshot => snapshot.observedWaveEncounter),
                observedBoss = snapshots.Any(snapshot => snapshot.observedBoss),
                source = snapshots.LastOrDefault(snapshot => !string.IsNullOrWhiteSpace(snapshot.source))?.source ?? "live-object-snapshot"
            };
        }

        private static M136PerformanceMetricSummary Metric(
            string id,
            string displayName,
            string unit,
            IEnumerable<double> samples,
            string note)
        {
            var values = (samples ?? Array.Empty<double>())
                .Where(value => !double.IsNaN(value) && !double.IsInfinity(value))
                .OrderBy(value => value)
                .ToArray();
            if (values.Length == 0)
            {
                return Unsupported(id, displayName, unit, note);
            }

            return new M136PerformanceMetricSummary
            {
                id = id,
                displayName = displayName,
                unit = unit,
                supported = true,
                sampleCount = values.Length,
                min = values[0],
                average = values.Average(),
                p50 = Percentile(values, 0.5d),
                p95 = Percentile(values, 0.95d),
                max = values[^1],
                note = note
            };
        }

        private static M136PerformanceMetricSummary Unsupported(string id, string displayName, string unit, string note)
        {
            return new M136PerformanceMetricSummary
            {
                id = id,
                displayName = displayName,
                unit = unit,
                supported = false,
                sampleCount = 0,
                note = note
            };
        }

        private static double Percentile(IReadOnlyList<double> sortedValues, double percentile)
        {
            if (sortedValues == null || sortedValues.Count == 0)
            {
                return 0d;
            }

            var index = Mathf.Clamp((int)Math.Ceiling(percentile * sortedValues.Count) - 1, 0, sortedValues.Count - 1);
            return sortedValues[index];
        }

        private static void TryStartRecorder(ref ProfilerRecorder recorder, ProfilerCategory category, string statName)
        {
            try
            {
                recorder = ProfilerRecorder.StartNew(category, statName, 256);
            }
            catch
            {
                recorder = default;
            }
        }

        private static void AddGpuFrameTiming(ICollection<double> target)
        {
            try
            {
                FrameTimingManager.CaptureFrameTimings();
                var timings = new FrameTiming[1];
                var count = FrameTimingManager.GetLatestTimings(1, timings);
                if (count > 0 && timings[0].gpuFrameTime > 0d)
                {
                    target.Add(timings[0].gpuFrameTime);
                }
            }
            catch
            {
                // Some Editor/graphics backends do not expose frame timing.
            }
        }

        private static void AddRecorderMilliseconds(ProfilerRecorder recorder, ICollection<double> target)
        {
            if (!recorder.Valid || recorder.LastValue <= 0)
            {
                return;
            }

            target.Add(recorder.LastValue / 1000000d);
        }

        private static void AddRecorderBytes(ProfilerRecorder recorder, ICollection<double> target)
        {
            if (!recorder.Valid || recorder.LastValue < 0)
            {
                return;
            }

            target.Add(recorder.LastValue);
        }

        private void AddThreadAllocationDelta(ICollection<double> target)
        {
            try
            {
                var current = GC.GetAllocatedBytesForCurrentThread();
                if (!hasThreadAllocatedBaseline)
                {
                    hasThreadAllocatedBaseline = true;
                    lastThreadAllocatedBytes = current;
                    target.Add(0d);
                    return;
                }

                var delta = current - lastThreadAllocatedBytes;
                lastThreadAllocatedBytes = current;
                target.Add(delta > 0L ? delta : 0d);
            }
            catch
            {
                // Some scripting profiles can block this API; the metric will be unsupported.
            }
        }

        private static void AddRecorderMegabytes(ProfilerRecorder recorder, ICollection<double> target)
        {
            if (!recorder.Valid || recorder.LastValue < 0)
            {
                return;
            }

            target.Add(recorder.LastValue / (1024d * 1024d));
        }

        private static string RecorderNote(ProfilerRecorder recorder, string note)
        {
            return recorder.Valid ? note : $"{note} Unsupported in this editor/platform context.";
        }

        private static void DisposeRecorder(ref ProfilerRecorder recorder)
        {
            if (recorder.Valid)
            {
                recorder.Dispose();
            }

            recorder = default;
        }
    }

    public sealed class M136LivePerformanceCaptureSession : IDisposable
    {
        private readonly M136FrameTelemetrySampler sampler;
        private bool samplerStarted;
        private bool disposed;

        public M136LivePerformanceCaptureSession(M136PerformanceScenarioDefinition scenario, string captureMode)
        {
            Scenario = scenario ?? throw new ArgumentNullException(nameof(scenario));
            CaptureMode = string.IsNullOrWhiteSpace(captureMode)
                ? M136EditorLaptopPerformancePolicy.ManualCaptureMode
                : captureMode;
            CaptureId = M136LivePerformanceCaptureStore.CreateCaptureId(Scenario.id, CaptureMode, DateTime.UtcNow);
            sampler = new M136FrameTelemetrySampler(M137PerformanceComfortPolicy.ExpectedCaptureSampleCapacity(Scenario.sampleSeconds));
        }

        public M136PerformanceScenarioDefinition Scenario { get; }

        public string CaptureMode { get; }

        public string CaptureId { get; }

        public bool IsTruthMode => M136EditorLaptopPerformancePolicy.IsTruthCaptureMode(CaptureMode);

        public bool CollectsObjectCountsDuringSampling => M136EditorLaptopPerformancePolicy.ShouldCollectObjectCountsDuringCapture(CaptureMode);

        public string StartedAtUtc { get; private set; }

        public string CompletedAtUtc { get; private set; }

        public float ElapsedSeconds { get; private set; }

        public float SampledSeconds { get; private set; }

        public bool IsRunning { get; private set; }

        public bool IsSampling => IsRunning && samplerStarted;

        public bool IsComplete { get; private set; }

        public IReadOnlyList<M136LiveFrameSample> Samples => sampler.FrameSamples;

        public bool Begin(DateTime? nowUtc = null)
        {
            if (!M136EditorLaptopPerformancePolicy.IsTelemetryAllowedNow())
            {
                return false;
            }

            StartedAtUtc = (nowUtc ?? DateTime.UtcNow).ToString("O", CultureInfo.InvariantCulture);
            ElapsedSeconds = 0f;
            SampledSeconds = 0f;
            IsRunning = true;
            IsComplete = false;
            samplerStarted = false;
            return true;
        }

        public bool Tick(float deltaTimeSeconds, M136LiveObjectCountSnapshot objectSnapshot = null)
        {
            if (!IsRunning || IsComplete)
            {
                return false;
            }

            var delta = Mathf.Max(0f, deltaTimeSeconds);
            var previousElapsed = ElapsedSeconds;
            ElapsedSeconds += delta;
            if (ElapsedSeconds <= Scenario.warmupSeconds)
            {
                return false;
            }

            if (!samplerStarted)
            {
                sampler.Begin();
                M136PerformanceOperationCounters.Reset();
                samplerStarted = true;
            }

            var availableSampleDelta = previousElapsed < Scenario.warmupSeconds
                ? ElapsedSeconds - Scenario.warmupSeconds
                : delta;
            if (SampledSeconds >= Scenario.sampleSeconds)
            {
                Complete(DateTime.UtcNow);
                return true;
            }

            var sampleDelta = Mathf.Min(availableSampleDelta, Scenario.sampleSeconds - SampledSeconds);
            if (sampleDelta > 0f)
            {
                SampledSeconds += sampleDelta;
                sampler.RecordFrame(sampleDelta, objectSnapshot ?? M136LiveObjectCountSnapshot.Empty());
            }

            if (SampledSeconds >= Scenario.sampleSeconds)
            {
                Complete(DateTime.UtcNow);
                return true;
            }

            return false;
        }

        public void Complete(DateTime? nowUtc = null)
        {
            if (IsComplete)
            {
                return;
            }

            CompletedAtUtc = (nowUtc ?? DateTime.UtcNow).ToString("O", CultureInfo.InvariantCulture);
            IsRunning = false;
            IsComplete = true;
        }

        public M136PerformanceScenarioResult BuildResult(
            string artifactDirectory,
            string manifestPath,
            string rawSampleCsvPath,
            string profilerTracePath,
            bool profilerTraceSupported,
            string profilerTraceNote,
            string note,
            bool profilerTraceRequested = false,
            bool fpsOverrideApplied = false,
            int fpsOverrideTarget = 0,
            string samplingSource = null)
        {
            if (!IsComplete)
            {
                Complete(DateTime.UtcNow);
            }

            return sampler.BuildScenarioResult(
                Scenario,
                CaptureMode,
                liveCaptured: true,
                requiresManualCapture: false,
                note,
                CaptureId,
                StartedAtUtc,
                CompletedAtUtc,
                artifactDirectory,
                manifestPath,
                rawSampleCsvPath,
                profilerTracePath,
                profilerTraceSupported,
                profilerTraceNote,
                profilerTraceRequested,
                fpsOverrideApplied,
                fpsOverrideTarget,
                samplingSource);
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            sampler.Dispose();
            disposed = true;
        }
    }

    public sealed class M136RuntimeLiveCaptureDriver : MonoBehaviour
    {
        public const string SamplingSource = M136FrameCadencePolicy.RuntimeUpdateSamplingSource;
        private readonly RuntimeObjectCountCollector objectCountCollector = new();
        private M136LivePerformanceCaptureSession session;

        public static M136RuntimeLiveCaptureDriver StartCapture(M136LivePerformanceCaptureSession captureSession)
        {
            if (captureSession == null)
            {
                throw new ArgumentNullException(nameof(captureSession));
            }

            var host = new GameObject("M136RuntimeLiveCaptureDriver");
            host.hideFlags = HideFlags.HideAndDontSave;
            DontDestroyOnLoad(host);
            var driver = host.AddComponent<M136RuntimeLiveCaptureDriver>();
            driver.Configure(captureSession);
            return driver;
        }

        public M136LivePerformanceCaptureSession Session => session;

        public M136LiveObjectCountSnapshot LatestSnapshot { get; private set; } = M136LiveObjectCountSnapshot.Empty("runtime-driver-not-started");

        public bool IsComplete => session == null || session.IsComplete;

        public double SampleRateHz => session != null && session.SampledSeconds > 0f
            ? session.Samples.Count / session.SampledSeconds
            : 0d;

        public void StopCapture()
        {
            session?.Complete(DateTime.UtcNow);
            enabled = false;
        }

        private void Configure(M136LivePerformanceCaptureSession captureSession)
        {
            session = captureSession;
            objectCountCollector.Reset(captureSession.CollectsObjectCountsDuringSampling);
            LatestSnapshot = captureSession.CollectsObjectCountsDuringSampling
                ? M136LiveObjectCountSnapshot.Empty("runtime-driver-warmup")
                : M136LiveObjectCountSnapshot.Empty("truth-mode-object-counts-disabled");
            if (!session.Begin())
            {
                session = null;
                enabled = false;
            }
        }

        private void Update()
        {
            if (session == null || session.IsComplete)
            {
                enabled = false;
                return;
            }

            LatestSnapshot = objectCountCollector.Capture(Time.unscaledTime);
            if (session.Tick(Time.unscaledDeltaTime, LatestSnapshot))
            {
                enabled = false;
            }
        }

        private void OnDestroy()
        {
            session = null;
        }

        private sealed class RuntimeObjectCountCollector
        {
            private const float CollectionCadenceSeconds = 0.5f;
            private float nextCollectionTime;
            private M136LiveObjectCountSnapshot lastSnapshot = M136LiveObjectCountSnapshot.Empty("runtime-driver-cadence");
            private bool collectObjectCounts = true;

            public void Reset(bool collectCounts)
            {
                collectObjectCounts = collectCounts;
                nextCollectionTime = 0f;
                lastSnapshot = collectObjectCounts
                    ? M136LiveObjectCountSnapshot.Empty("runtime-driver-cadence")
                    : M136LiveObjectCountSnapshot.Empty("truth-mode-object-counts-disabled");
            }

            public M136LiveObjectCountSnapshot Capture(float unscaledTime)
            {
                if (!collectObjectCounts)
                {
                    return lastSnapshot;
                }

                if (unscaledTime < nextCollectionTime)
                {
                    return lastSnapshot;
                }

                nextCollectionTime = unscaledTime + CollectionCadenceSeconds;
                var particleSystems = FindObjectsByType<ParticleSystem>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
                var canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
                var cameras = FindObjectsByType<Camera>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
                var lights = FindObjectsByType<Light>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
                var renderers = FindObjectsByType<Renderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
                var behaviours = FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
                var enemies = new HashSet<int>();
                var projectiles = new HashSet<int>();
                var vfx = new HashSet<int>();
                var observedBranchSession = false;
                var observedSpaceshipHub = false;
                var observedDeveloperLab = false;
                var observedInterBranchHub = false;
                var observedCombatController = false;
                var observedActiveCombat = false;
                var observedWaveEncounter = false;
                var observedBoss = false;

                foreach (var behaviour in behaviours)
                {
                    if (behaviour == null)
                    {
                        continue;
                    }

                    var type = behaviour.GetType();
                    var typeName = type.Name;
                    var objectName = behaviour.gameObject.name;
                    var search = $"{typeName} {objectName}";
                    var id = behaviour.gameObject.GetInstanceID();
                    if (ContainsAny(search, "Enemy", "BossRuntime", "Chaser", "Turret", "Splitter", "Warden", "Widow", "Choir"))
                    {
                        enemies.Add(id);
                    }

                    if (ContainsAny(search, "Projectile", "Bullet", "Bolt", "Missile", "Shot"))
                    {
                        projectiles.Add(id);
                    }

                    if (ContainsAny(search, "Vfx", "VFX", "Swipe", "Slash", "Trail", "Impact", "Muzzle"))
                    {
                        vfx.Add(id);
                    }

                    if (typeName == "BranchSessionController")
                    {
                        observedBranchSession = true;
                        observedSpaceshipHub |= BoolProperty(behaviour, "IsSpaceshipHub");
                        observedDeveloperLab |= BoolProperty(behaviour, "IsDeveloperLab");
                        observedInterBranchHub |= BoolProperty(behaviour, "IsInInterBranchHub");
                        observedWaveEncounter |= StringPropertyPathContains(behaviour, "State.CurrentRoom.Role", "Wave");
                        observedBoss |= StringPropertyPathContains(behaviour, "State.CurrentRoom.Role", "Boss");
                    }
                    else if (typeName == "GameSessionController")
                    {
                        observedSpaceshipHub |= StringPropertyPathContains(behaviour, "SessionState.SessionMode", "SpaceshipHub");
                    }
                    else if (typeName == "RoomCombatController")
                    {
                        observedCombatController = true;
                        observedActiveCombat |= StringPropertyPathContains(behaviour, "ObjectiveState", "InCombat");
                        observedWaveEncounter |= BoolProperty(behaviour, "IsWaveEncounterActive");
                        observedBoss |= ObjectProperty(behaviour, "ActiveBoss") != null;
                    }
                    else if (typeName.IndexOf("BossRuntime", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        observedBoss = true;
                    }
                }

                foreach (var particleSystem in particleSystems)
                {
                    if (particleSystem != null)
                    {
                        vfx.Add(particleSystem.gameObject.GetInstanceID());
                    }
                }

                lastSnapshot = new M136LiveObjectCountSnapshot
                {
                    activeEnemies = enemies.Count,
                    activeProjectiles = projectiles.Count,
                    activeVfx = vfx.Count,
                    activeUiCanvases = canvases.Length,
                    activeCameras = cameras.Length,
                    activeLights = lights.Length,
                    activeRenderers = renderers.Length,
                    activeParticleSystems = particleSystems.Length,
                    observedBranchSession = observedBranchSession,
                    observedSpaceshipHub = observedSpaceshipHub,
                    observedDeveloperLab = observedDeveloperLab,
                    observedInterBranchHub = observedInterBranchHub,
                    observedCombatController = observedCombatController,
                    observedActiveCombat = observedActiveCombat,
                    observedWaveEncounter = observedWaveEncounter,
                    observedBoss = observedBoss,
                    source = "playmode-runtime-0.5s-cadence"
                };
                return lastSnapshot;
            }

            private static bool ContainsAny(string value, params string[] tokens)
            {
                return tokens.Any(token => value.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0);
            }

            private static bool BoolProperty(object target, string propertyName)
            {
                return ObjectProperty(target, propertyName) is bool value && value;
            }

            private static bool StringPropertyPathContains(object target, string propertyPath, string token)
            {
                var value = ObjectPropertyPath(target, propertyPath);
                return value != null && value.ToString().IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0;
            }

            private static object ObjectPropertyPath(object target, string propertyPath)
            {
                var current = target;
                foreach (var part in propertyPath.Split('.'))
                {
                    current = ObjectProperty(current, part);
                    if (current == null)
                    {
                        return null;
                    }
                }

                return current;
            }

            private static object ObjectProperty(object target, string propertyName)
            {
                if (target == null || string.IsNullOrWhiteSpace(propertyName))
                {
                    return null;
                }

                try
                {
                    return target.GetType().GetProperty(propertyName)?.GetValue(target);
                }
                catch
                {
                    return null;
                }
            }
        }
    }

    public static class M136LivePerformanceCaptureStore
    {
        public const string LiveCaptureRoot = "output/reports/performance/live_captures";
        public const string ManifestFileName = "manifest.json";
        public const string ScenarioResultFileName = "scenario_result.json";
        public const string RawSamplesJsonFileName = "frame_samples.json";
        public const string RawSamplesCsvFileName = "frame_samples.csv";

        public static string CreateCaptureId(string scenarioId, string captureMode, DateTime utcNow)
        {
            return $"{utcNow:yyyyMMdd_HHmmss}_{Sanitize(captureMode)}_{Sanitize(scenarioId)}";
        }

        public static string CreateCaptureDirectory(string scenarioId, string captureMode, DateTime utcNow, string rootDirectory = LiveCaptureRoot)
        {
            return Path.Combine(rootDirectory, CreateCaptureId(scenarioId, captureMode, utcNow));
        }

        public static M136LiveCaptureManifest SaveCapture(
            string directory,
            M136PerformanceScenarioDefinition scenario,
            M136PerformanceScenarioResult result,
            IReadOnlyList<M136LiveFrameSample> samples,
            string profilerTracePath,
            bool profilerTraceSupported,
            string profilerTraceNote,
            string note)
        {
            if (scenario == null)
            {
                throw new ArgumentNullException(nameof(scenario));
            }

            if (result == null)
            {
                throw new ArgumentNullException(nameof(result));
            }

            Directory.CreateDirectory(directory);
            var manifestPath = Path.Combine(directory, ManifestFileName);
            var scenarioJsonPath = Path.Combine(directory, ScenarioResultFileName);
            var rawSampleJsonPath = Path.Combine(directory, RawSamplesJsonFileName);
            var rawSampleCsvPath = Path.Combine(directory, RawSamplesCsvFileName);
            result.artifactDirectory = directory;
            result.manifestPath = manifestPath;
            result.rawSampleCsvPath = rawSampleCsvPath;
            result.profilerTracePath = profilerTracePath;
            result.profilerTraceSupported = profilerTraceSupported;
            result.profilerTraceNote = profilerTraceNote;

            var sampleArray = samples?.ToArray() ?? Array.Empty<M136LiveFrameSample>();
            var unsupportedMetrics = (result.metrics ?? Array.Empty<M136PerformanceMetricSummary>())
                .Where(metric => !metric.supported)
                .Select(metric => metric.id)
                .ToArray();
            var manifest = new M136LiveCaptureManifest
            {
                lockId = M136EditorLaptopPerformancePolicy.LockId,
                captureId = string.IsNullOrWhiteSpace(result.captureId) ? Path.GetFileName(directory) : result.captureId,
                captureMode = result.captureMode,
                scenarioId = scenario.id,
                scenarioDisplayName = scenario.displayName,
                generatedAtUtc = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture),
                warmupSeconds = scenario.warmupSeconds,
                sampleSeconds = scenario.sampleSeconds,
                sampleCount = sampleArray.Length,
                liveCaptured = result.liveCaptured,
                complete = result.liveCaptured && sampleArray.Length > 0,
                artifactDirectory = directory,
                manifestPath = manifestPath,
                scenarioJsonPath = scenarioJsonPath,
                rawSampleCsvPath = rawSampleCsvPath,
                rawSampleJsonPath = rawSampleJsonPath,
                profilerTracePath = profilerTracePath,
                profilerTraceSupported = profilerTraceSupported,
                profilerTraceNote = profilerTraceNote,
                profilerTraceRequested = result.profilerTraceRequested,
                fpsOverrideApplied = result.fpsOverrideApplied,
                fpsOverrideTarget = result.fpsOverrideTarget,
                samplingSource = result.samplingSource,
                sampleRateHz = result.sampleRateHz,
                expectedSampleCountMin = result.expectedSampleCountMin,
                expectedSampleCountMax = result.expectedSampleCountMax,
                frameCadenceConfidence = result.frameCadenceConfidence,
                unsupportedMetricIds = unsupportedMetrics,
                objectCounts = result.objectCounts,
                note = note
            };

            var validity = M136CaptureValidityClassifier.Classify(manifest, result);
            result.validityGrade = validity.Grade;
            result.validityReason = validity.Reason;
            manifest.validityGrade = validity.Grade;
            manifest.validityReason = validity.Reason;

            File.WriteAllText(scenarioJsonPath, JsonUtility.ToJson(result, true));
            File.WriteAllText(rawSampleJsonPath, JsonUtility.ToJson(new M136LiveFrameSampleCollection { samples = sampleArray }, true));
            File.WriteAllText(rawSampleCsvPath, ToCsv(sampleArray));
            File.WriteAllText(manifestPath, JsonUtility.ToJson(manifest, true));
            return manifest;
        }

        public static M136LiveCaptureManifest[] LoadManifests(string rootDirectory = LiveCaptureRoot)
        {
            if (string.IsNullOrWhiteSpace(rootDirectory) || !Directory.Exists(rootDirectory))
            {
                return Array.Empty<M136LiveCaptureManifest>();
            }

            return Directory
                .EnumerateFiles(rootDirectory, ManifestFileName, SearchOption.AllDirectories)
                .Select(ReadManifest)
                .Where(manifest => manifest != null && manifest.lockId == M136EditorLaptopPerformancePolicy.LockId)
                .OrderBy(manifest => manifest.generatedAtUtc, StringComparer.Ordinal)
                .ToArray();
        }

        public static M136PerformanceScenarioResult LoadScenarioResult(M136LiveCaptureManifest manifest)
        {
            if (manifest == null || string.IsNullOrWhiteSpace(manifest.scenarioJsonPath) || !File.Exists(manifest.scenarioJsonPath))
            {
                return null;
            }

            try
            {
                return JsonUtility.FromJson<M136PerformanceScenarioResult>(File.ReadAllText(manifest.scenarioJsonPath));
            }
            catch
            {
                return null;
            }
        }

        private static M136LiveCaptureManifest ReadManifest(string path)
        {
            try
            {
                var manifest = JsonUtility.FromJson<M136LiveCaptureManifest>(File.ReadAllText(path));
                if (manifest != null && string.IsNullOrWhiteSpace(manifest.manifestPath))
                {
                    manifest.manifestPath = path;
                }

                return manifest;
            }
            catch
            {
                return null;
            }
        }

        private static string ToCsv(IEnumerable<M136LiveFrameSample> samples)
        {
            var builder = new StringBuilder();
            builder.AppendLine("sampleIndex,elapsedSeconds,frameTimeMs,fps,activeEnemies,activeProjectiles,activeVfx,activeUiCanvases,activeCameras,activeLights,activeRenderers,activeParticleSystems,miniMapRebuilds,wallVisibilityUpdates,combatHudRefreshes,runtimeNavMeshFallbacks,stressHarnessNavMeshBakes,roomTransitionEvents");
            foreach (var sample in samples ?? Array.Empty<M136LiveFrameSample>())
            {
                builder.Append(sample.sampleIndex).Append(',')
                    .Append(sample.elapsedSeconds.ToString("0.###", CultureInfo.InvariantCulture)).Append(',')
                    .Append(sample.frameTimeMs.ToString("0.###", CultureInfo.InvariantCulture)).Append(',')
                    .Append(sample.fps.ToString("0.###", CultureInfo.InvariantCulture)).Append(',')
                    .Append(sample.activeEnemies).Append(',')
                    .Append(sample.activeProjectiles).Append(',')
                    .Append(sample.activeVfx).Append(',')
                    .Append(sample.activeUiCanvases).Append(',')
                    .Append(sample.activeCameras).Append(',')
                    .Append(sample.activeLights).Append(',')
                    .Append(sample.activeRenderers).Append(',')
                    .Append(sample.activeParticleSystems).Append(',')
                    .Append(sample.miniMapRebuilds).Append(',')
                    .Append(sample.wallVisibilityUpdates).Append(',')
                    .Append(sample.combatHudRefreshes).Append(',')
                    .Append(sample.runtimeNavMeshFallbacks).Append(',')
                    .Append(sample.stressHarnessNavMeshBakes).Append(',')
                    .Append(sample.roomTransitionEvents)
                    .AppendLine();
            }

            return builder.ToString();
        }

        private static string Sanitize(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "capture";
            }

            var builder = new StringBuilder(value.Length);
            foreach (var character in value.ToLowerInvariant())
            {
                builder.Append(char.IsLetterOrDigit(character) ? character : '_');
            }

            return builder.ToString().Trim('_');
        }
    }
}
