using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Hollow.Branches;
using Hollow.Combat;
using Hollow.Core;
using Hollow.Core.Diagnostics;
using Hollow.Data.Definitions;
using Hollow.Diagnostics;
using Hollow.Entities;
using Hollow.Platform;
using Hollow.Rewards;
using Hollow.Rooms;
using Hollow.World;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Profiling;
using Object = UnityEngine.Object;

namespace Hollow.Performance
{
    public static class M139LongRunSoakRunner
    {
        private const int MaxIdleWaitFrames = 900;

        private static readonly M139SoakScenarioDefinition[] Scenarios =
        {
            new("fresh_multi_branch_soak", "Fresh Multi-Branch Soak", includeMultiBranch: true),
            new("save_load_restore_soak", "Save/Load Restore Soak", includeSaveLoadRestore: true),
            new("branch_abandon_reenter_soak", "Branch Abandon/Re-enter Soak", includeBranchAbandonReenter: true),
            new("boss_room_soak", "Boss Room Soak", includeBossRoom: true),
            new("next_branch_soak", "Next Branch Soak", includeNextBranch: true)
        };

        public static IEnumerator RunAllScenarios(
            M139LongRunSoakOptions options,
            Action<M139LongRunSoakReport> onComplete = null,
            Action<M139LongRunSoakScenarioSummary> onScenarioComplete = null,
            Func<M139SoakScenarioDefinition, IEnumerator> beforeScenarioCleanup = null)
        {
            options ??= M139LongRunSoakOptions.FullGate();
            var summaries = new List<M139LongRunSoakScenarioSummary>();
            var fpsOverride = new M136CaptureFpsOverride(true, options.targetFrameRate);
            try
            {
                for (var index = 0; index < Scenarios.Length; index++)
                {
                    M139LongRunSoakScenarioSummary summary = null;
                    yield return RunScenario(
                        Scenarios[index],
                        options,
                        next => summary = next,
                        beforeScenarioCleanup != null ? () => beforeScenarioCleanup(Scenarios[index]) : null);
                    if (summary != null)
                    {
                        summaries.Add(summary);
                        onScenarioComplete?.Invoke(summary);
                    }
                }
            }
            finally
            {
                fpsOverride.Dispose();
            }

            var report = M139LongRunSoakReportGenerator.BuildReport(summaries, options.ciSmoke);
            if (options.writeReports)
            {
                M139LongRunSoakReportGenerator.WriteReport(report, options.jsonReportPath, options.markdownReportPath);
            }

            onComplete?.Invoke(report);
        }

        public static IEnumerator RunScenario(
            M139SoakScenarioDefinition scenario,
            M139LongRunSoakOptions options,
            Action<M139LongRunSoakScenarioSummary> onComplete,
            Func<IEnumerator> beforeCleanup = null)
        {
            if (scenario == null)
            {
                throw new ArgumentNullException(nameof(scenario));
            }

            options ??= M139LongRunSoakOptions.FullGate();
            M136PerformanceOperationCounters.Reset();
            EnemyRuntimePool.ResetDiagnostics();
            Hollow.Core.HollowRuntimePool.ResetDiagnostics();
            using var sampler = new M139SoakSampler();
            sampler.Begin();
            var gateCounters = new M139AfterWarmupGateCounters();
            var roomTraversals = 0;
            var saveLoadRestores = 0;
            var abandonReentries = 0;
            var nextBranchTransitions = 0;
            var warmupBaseline = default(M136PerformanceOperationSnapshot);
            var harness = M139BranchSoakHarness.Create(scenario.id);

            try
            {
                yield return harness.InitializeFreshAndWait(sampler);
                warmupBaseline = M136PerformanceOperationCounters.Snapshot();
                sampler.MarkGateBaseline();

                var branchCount = scenario.includeMultiBranch ? Mathf.Max(1, options.branches) : 1;
                for (var branchIndex = 0; branchIndex < branchCount; branchIndex++)
                {
                    var traversalTarget = Mathf.Max(1, options.traversalsPerBranch);
                    for (var traversalIndex = 0; traversalIndex < traversalTarget; traversalIndex++)
                    {
                        var preferBoss = scenario.includeBossRoom && traversalIndex >= traversalTarget / 2;
                        if (!harness.TryPrepareTraversal(preferBoss))
                        {
                            break;
                        }

                        var beforeTraversal = M136PerformanceOperationCounters.Snapshot();
                        harness.StartPreparedTraversal();
                        yield return harness.WaitForIdle(sampler);
                        var afterTraversal = M136PerformanceOperationCounters.Snapshot();
                        gateCounters.ObserveTraversalWindow(beforeTraversal, afterTraversal);
                        roomTraversals++;
                        yield return TickFrames(sampler, 2);
                    }

                    if (branchIndex + 1 < branchCount)
                    {
                        yield return harness.StartNextBranchAndWait(sampler);
                        nextBranchTransitions++;
                    }
                }

                if (scenario.includeSaveLoadRestore)
                {
                    var snapshot = harness.Branch.CreateSnapshot();
                    harness.Branch.InitializeFromSnapshot(harness.RoomAsset, harness.SessionState, snapshot);
                    yield return harness.WaitForIdle(sampler);
                    saveLoadRestores++;
                }

                if (scenario.includeBranchAbandonReenter)
                {
                    var snapshot = harness.Branch.CreateSnapshot();
                    harness.Branch.InitializeFresh(harness.RoomAsset, harness.SessionState);
                    yield return harness.WaitForIdle(sampler);
                    harness.Branch.InitializeFromSnapshot(harness.RoomAsset, harness.SessionState, snapshot);
                    yield return harness.WaitForIdle(sampler);
                    abandonReentries++;
                }

                if (scenario.includeBossRoom)
                {
                    for (var attempts = 0; attempts < 16 && !harness.IsCurrentRoom(BranchRoomRole.Boss); attempts++)
                    {
                        if (!harness.TryPrepareTraversal(preferBoss: true))
                        {
                            break;
                        }

                        var beforeTraversal = M136PerformanceOperationCounters.Snapshot();
                        harness.StartPreparedTraversal();
                        yield return harness.WaitForIdle(sampler);
                        gateCounters.ObserveTraversalWindow(beforeTraversal, M136PerformanceOperationCounters.Snapshot());
                        roomTraversals++;
                    }
                }

                if (scenario.includeNextBranch)
                {
                    yield return harness.StartNextBranchAndWait(sampler);
                    nextBranchTransitions++;
                }

                if (beforeCleanup != null)
                {
                    sampler.SuppressSamplesFor(4);
                    var cleanupRoutine = beforeCleanup();
                    if (cleanupRoutine != null)
                    {
                        yield return cleanupRoutine;
                    }

                    sampler.SuppressSamplesFor(2);
                }

                harness.ForceCleanupCurrentRoom();
                sampler.SuppressSamplesFor(3);
                yield return TickFrames(sampler, 3);
                var unload = Resources.UnloadUnusedAssets();
                while (unload != null && !unload.isDone)
                {
                    yield return null;
                }

                GC.Collect();
                sampler.SuppressSamplesFor(2);
                yield return TickFrames(sampler, 2);
                var finalSnapshot = M136PerformanceOperationCounters.Snapshot();
                var enemyPool = EnemyRuntimePool.Snapshot(harness.Branch.ActiveBranchEnemyPoolKey);
                var runtimePool = Hollow.Core.HollowRuntimePool.Snapshot();
                if (enemyPool.activeLeakCount + runtimePool.activeLeakCount > 0)
                {
                    M136PerformanceOperationCounters.ReportM139PoolActiveLeak(enemyPool.activeLeakCount + runtimePool.activeLeakCount);
                }

                onComplete?.Invoke(M139LongRunSoakReportGenerator.BuildScenarioSummary(
                    scenario.id,
                    scenario.displayName,
                    warmupBaseline,
                    finalSnapshot,
                    sampler.BuildSummary(),
                    enemyPool,
                    runtimePool,
                    harness.Branch.BranchRuntimeCacheSnapshot,
                    roomTraversals,
                    saveLoadRestores,
                    abandonReentries,
                    nextBranchTransitions,
                    scenario.includeBossRoom,
                    options.enforceTiming,
                    options.ciSmoke,
                    gateCounters,
                    "M139 automated PlayMode branch soak through BranchSessionController."));
            }
            finally
            {
                harness.Destroy();
            }
        }

        private static IEnumerator TickFrames(M139SoakSampler sampler, int frames)
        {
            for (var index = 0; index < frames; index++)
            {
                sampler?.Tick();
                yield return null;
            }
        }

        [Serializable]
        public sealed class M139SoakScenarioDefinition
        {
            public M139SoakScenarioDefinition(
                string id,
                string displayName,
                bool includeMultiBranch = false,
                bool includeSaveLoadRestore = false,
                bool includeBranchAbandonReenter = false,
                bool includeBossRoom = false,
                bool includeNextBranch = false)
            {
                this.id = id ?? string.Empty;
                this.displayName = displayName ?? id ?? string.Empty;
                this.includeMultiBranch = includeMultiBranch;
                this.includeSaveLoadRestore = includeSaveLoadRestore;
                this.includeBranchAbandonReenter = includeBranchAbandonReenter;
                this.includeBossRoom = includeBossRoom;
                this.includeNextBranch = includeNextBranch;
            }

            public string id;
            public string displayName;
            public bool includeMultiBranch;
            public bool includeSaveLoadRestore;
            public bool includeBranchAbandonReenter;
            public bool includeBossRoom;
            public bool includeNextBranch;
        }

        private sealed class M139BranchSoakHarness
        {
            private const string CatalogResourcePath = "Hollow/Branches/M139BranchRoomTemplateCatalog";
            private const string SingleRoomPath = "Assets/_Hollow/Data/Rooms/MacroFixtures/combat_macro_single_1x1.hollowruntime.json";

            private BranchConnection preparedConnection;

            private M139BranchSoakHarness(
                GameObject root,
                ImportedRoomRuntimeAsset roomAsset,
                GameSessionState sessionState,
                BranchSessionController branch,
                RoomCombatController combat)
            {
                Root = root;
                RoomAsset = roomAsset;
                SessionState = sessionState;
                Branch = branch;
                Combat = combat;
            }

            public GameObject Root { get; }

            public ImportedRoomRuntimeAsset RoomAsset { get; }

            public GameSessionState SessionState { get; }

            public BranchSessionController Branch { get; }

            public RoomCombatController Combat { get; }

            public static M139BranchSoakHarness Create(string scenarioId)
            {
                var root = new GameObject($"M139_{scenarioId}_BranchSoakHarness");
                CreateCaptureCamera(root.transform);
                var roomObject = new GameObject("RoomRuntimeRoot");
                roomObject.transform.SetParent(root.transform, false);
                roomObject.AddComponent<RoomRuntimeRoot>();

                var playerObject = new GameObject("PlayerCharacter");
                playerObject.transform.SetParent(root.transform, false);
                var player = playerObject.AddComponent<PlaceholderPlayerController>();
                player.ConfigureDefault();
                playerObject.AddComponent<CombatantHealth>().Configure(RoomCombatController.PlayerMaxHealth);

                var enemyPrefab = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                enemyPrefab.name = "M139EnemyPoolPrefab";
                enemyPrefab.transform.SetParent(root.transform, false);
                enemyPrefab.SetActive(false);
                enemyPrefab.AddComponent<CombatantHealth>();
                enemyPrefab.AddComponent<EnemyRuntimeController>();

                var projectilePrefab = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                projectilePrefab.name = "M139ProjectilePoolPrefab";
                projectilePrefab.transform.SetParent(root.transform, false);
                projectilePrefab.SetActive(false);
                projectilePrefab.AddComponent<ProjectileController>();
                projectilePrefab.AddComponent<EnemyProjectileController>();

                var rewardPrefab = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                rewardPrefab.name = "M139RewardPickupPrefab";
                rewardPrefab.transform.SetParent(root.transform, false);
                rewardPrefab.SetActive(false);
                rewardPrefab.AddComponent<RoomRewardPickup>();

                var bossKeyPrefab = GameObject.CreatePrimitive(PrimitiveType.Cube);
                bossKeyPrefab.name = "M139BossKeyPickupPrefab";
                bossKeyPrefab.transform.SetParent(root.transform, false);
                bossKeyPrefab.SetActive(false);
                bossKeyPrefab.AddComponent<BossKeyPickup>();

                var hubPortalPrefab = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                hubPortalPrefab.name = "M139HubReturnPortalPrefab";
                hubPortalPrefab.transform.SetParent(root.transform, false);
                hubPortalPrefab.SetActive(false);
                hubPortalPrefab.AddComponent<HubReturnPortal>();

                var nextPortalPrefab = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                nextPortalPrefab.name = "M139NextBranchPortalPrefab";
                nextPortalPrefab.transform.SetParent(root.transform, false);
                nextPortalPrefab.SetActive(false);
                nextPortalPrefab.AddComponent<NextBranchPortal>();

                var combat = root.AddComponent<RoomCombatController>();
                combat.Configure(enemyPrefab, projectilePrefab, EnemyCatalog.CreateRuntimeDefault(), DifficultyTierDefinition.CreateRuntimeDeveloperSample());
                combat.ConfigureAutoInitialize(false);

                var branch = root.AddComponent<BranchSessionController>();
                var templateCatalog = LoadMacroCatalog();
                branch.Configure(rewardPrefab, hubPortalPrefab);
                branch.ConfigureBranchFeaturePrefabs(bossKeyPrefab, null, nextPortalPrefab);
                branch.ConfigureTemplateCatalog(templateCatalog, templateCatalog.DefaultSeed);
                branch.ConfigureGenerationSettings(CreateGenerationSettings());
                branch.ConfigureBossCatalog(BossCatalogDefinition.CreateRuntimeDefault());

                var roomAsset = ImportRoomAsset(templateCatalog.Single1x1);
                var sessionState = GameSessionState.Create(RuntimeSessionMode.ProfileBacked, HollowPlatformKind.WindowsStandard3D, null, Vector3.zero);
                return new M139BranchSoakHarness(root, roomAsset, sessionState, branch, combat);
            }

            private static void CreateCaptureCamera(Transform parent)
            {
                var lightObject = new GameObject("M139.CaptureLight");
                lightObject.transform.SetParent(parent, false);
                lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
                var light = lightObject.AddComponent<Light>();
                light.type = LightType.Directional;
                light.intensity = 1.15f;

                var cameraObject = new GameObject("M139.CaptureCamera");
                cameraObject.transform.SetParent(parent, false);
                cameraObject.transform.position = new Vector3(0f, 18f, -12f);
                cameraObject.transform.rotation = Quaternion.Euler(60f, 0f, 0f);
                var camera = cameraObject.AddComponent<Camera>();
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.035f, 0.04f, 0.05f, 1f);
                camera.orthographic = true;
                camera.orthographicSize = 9f;
                camera.nearClipPlane = 0.1f;
                camera.farClipPlane = 80f;
                camera.depth = 1000f;
            }

            public IEnumerator InitializeFreshAndWait(M139SoakSampler sampler)
            {
                Branch.InitializeFresh(RoomAsset, SessionState);
                yield return WaitForIdle(sampler);
            }

            public IEnumerator WaitForIdle(M139SoakSampler sampler)
            {
                for (var frame = 0; frame < MaxIdleWaitFrames; frame++)
                {
                    sampler?.Tick();
                    if (!Branch.BranchLoadingActive && !Branch.RoomTransitionActive)
                    {
                        yield return null;
                        sampler?.Tick();
                        yield break;
                    }

                    yield return null;
                }
            }

            public bool IsCurrentRoom(BranchRoomRole role)
            {
                return Branch.State?.CurrentRoom?.Role == role;
            }

            public bool TryPrepareTraversal(bool preferBoss)
            {
                ForceClearCurrentRoom();
                preparedConnection = preferBoss
                    ? FindPathToRoom(room => room.Role == BranchRoomRole.Boss)
                    : FindPathToRoom(room => !room.IsVisited && room.Role != BranchRoomRole.Boss) ??
                      FindPathToRoom(room => room.Role is BranchRoomRole.Reward or BranchRoomRole.Treasure or BranchRoomRole.Secret or BranchRoomRole.SpecialEncounter) ??
                      Branch.State?.Graph?.ConnectionsFrom(Branch.State.CurrentRoomId).OrderBy(connection => connection.ToRoomId.Value, StringComparer.Ordinal).FirstOrDefault();
                return preparedConnection != null;
            }

            public void StartPreparedTraversal()
            {
                if (preparedConnection != null)
                {
                    Branch.TryTraverse(preparedConnection.FromDirection);
                }
            }

            public IEnumerator StartNextBranchAndWait(M139SoakSampler sampler)
            {
                ForceClearCurrentRoom();
                Branch.EnterInterBranchHub();
                yield return null;
                var choice = Branch.CurrentNextBranchPortals
                    .Select(portal => portal != null ? portal.Choice : null)
                    .FirstOrDefault(candidate => candidate != null && candidate.IsInteractable) ??
                    NextBranchChoice.Create(Branch.CurrentBranchSeed == 0 ? Branch.MacroBranchSeed : Branch.CurrentBranchSeed, 1, 0);
                Branch.StartNextBranch(choice);
                yield return WaitForIdle(sampler);
            }

            public void ForceCleanupCurrentRoom()
            {
                ForceClearCurrentRoom();
            }

            public void Destroy()
            {
                if (Root != null)
                {
                    Object.Destroy(Root);
                }
            }

            private void ForceClearCurrentRoom()
            {
                if (Combat != null)
                {
                    Combat.ForceClearRoomWithoutReward();
                }

                if (Branch.State?.CurrentRoom != null)
                {
                    Branch.State.CurrentRoom.MarkCleared();
                    if (Branch.State.CurrentRoom.HasPendingReward)
                    {
                        Branch.State.CurrentRoom.MarkRewardClaimed();
                    }
                }
            }

            private BranchConnection FindPathToRoom(Func<BranchRoomState, bool> predicate)
            {
                if (Branch.State?.Graph == null || Branch.State.CurrentRoomId == null || predicate == null)
                {
                    return null;
                }

                var start = Branch.State.CurrentRoomId;
                var visited = new HashSet<string>(StringComparer.Ordinal) { start.Value };
                var queue = new Queue<(BranchRoomId roomId, BranchConnection first)>();
                foreach (var connection in Branch.State.Graph.ConnectionsFrom(start).OrderBy(connection => connection.ToRoomId.Value, StringComparer.Ordinal))
                {
                    queue.Enqueue((connection.ToRoomId, connection));
                }

                while (queue.Count > 0)
                {
                    var (roomId, first) = queue.Dequeue();
                    if (!visited.Add(roomId.Value))
                    {
                        continue;
                    }

                    if (Branch.State.Graph.TryGetRoom(roomId, out var room) && predicate(room))
                    {
                        return first;
                    }

                    foreach (var connection in Branch.State.Graph.ConnectionsFrom(roomId).OrderBy(connection => connection.ToRoomId.Value, StringComparer.Ordinal))
                    {
                        if (!visited.Contains(connection.ToRoomId.Value))
                        {
                            queue.Enqueue((connection.ToRoomId, first));
                        }
                    }
                }

                return null;
            }

            private static BranchRoomTemplateCatalogDefinition LoadMacroCatalog()
            {
                var catalog = Resources.Load<BranchRoomTemplateCatalogDefinition>(CatalogResourcePath);
                if (catalog != null && catalog.Single1x1 != null)
                {
                    return catalog;
                }

                if (File.Exists(SingleRoomPath))
                {
                    return CreateMacroCatalogFromLooseFiles();
                }

                throw new InvalidOperationException(
                    $"M139 branch soak requires the packaged room template catalog at Resources/{CatalogResourcePath}. " +
                    "The built player cannot read project-relative Assets/_Hollow/Data room JSON files.");
            }

            private static BranchRoomTemplateCatalogDefinition CreateMacroCatalogFromLooseFiles()
            {
                var catalog = ScriptableObject.CreateInstance<BranchRoomTemplateCatalogDefinition>();
                catalog.Configure(
                    LoadTextAsset(SingleRoomPath),
                    LoadTextAsset("Assets/_Hollow/Data/Rooms/MacroFixtures/combat_macro_wide_2x1.hollowruntime.json"),
                    LoadTextAsset("Assets/_Hollow/Data/Rooms/MacroFixtures/combat_macro_tall_1x2.hollowruntime.json"),
                    LoadTextAsset("Assets/_Hollow/Data/Rooms/MacroFixtures/combat_macro_block_2x2.hollowruntime.json"),
                    LoadTextAsset("Assets/_Hollow/Data/Rooms/MacroFixtures/combat_macro_l_3cell.hollowruntime.json"),
                    BranchGenerator.DefaultSeededMacroSeed,
                    new[]
                    {
                        LoadTextAsset("Assets/_Hollow/Data/Rooms/DesignerApproved/approved_crossroads_single_1x1.hollowruntime.json"),
                        LoadTextAsset("Assets/_Hollow/Data/Rooms/DesignerApproved/approved_lane_wide_2x1.hollowruntime.json"),
                        LoadTextAsset("Assets/_Hollow/Data/Rooms/DesignerApproved/boss_arena_broken_gateyard.hollowruntime.json")
                    },
                    LoadTextAsset("Assets/_Hollow/Data/Rooms/MacroFixtures/corrupted_chest_single_1x1.hollowruntime.json"),
                    LoadTextAsset("Assets/_Hollow/Data/Rooms/MacroFixtures/wave_room_single_1x1.hollowruntime.json"),
                    LoadTextAsset("Assets/_Hollow/Data/Rooms/MacroFixtures/special_soul_eater_single_1x1.hollowruntime.json"),
                    LoadTextAsset("Assets/_Hollow/Data/Rooms/MacroFixtures/special_escapist_single_1x1.hollowruntime.json"));
                return catalog;
            }

            private static BranchGenerationSettingsDefinition CreateGenerationSettings()
            {
                var settings = ScriptableObject.CreateInstance<BranchGenerationSettingsDefinition>();
                settings.Configure(
                    BranchGenerator.DefaultSeededMacroSeed,
                    nextTargetRoomCount: 8,
                    nextMaxPlacementAttempts: 250,
                    nextAllowLoops: false,
                    nextEnableBossLeaf: true,
                    nextEnableTreasureLeaf: true,
                    nextAllowedFixtureIds: new[]
                    {
                        "combat_macro_single_1x1",
                        "combat_macro_wide_2x1",
                        "combat_macro_tall_1x2",
                        "combat_macro_block_2x2",
                        "combat_macro_l_3cell"
                    });
                return settings;
            }

            private static ImportedRoomRuntimeAsset ImportRoomAsset(TextAsset asset)
            {
                if (asset == null)
                {
                    throw new InvalidOperationException("M139 branch soak room asset is missing from the packaged template catalog.");
                }

                return HollowRuntimeV2Importer.Import(asset.text);
            }

            private static TextAsset LoadTextAsset(string path)
            {
                if (!File.Exists(path))
                {
                    throw new FileNotFoundException($"M139 branch soak room fixture is missing: {path}", path);
                }

                var text = File.ReadAllText(path);
                var asset = new TextAsset(text)
                {
                    name = Path.GetFileNameWithoutExtension(path)
                };
                return asset;
            }
        }
    }

    public sealed class M139SoakSampler : IDisposable
    {
        private readonly List<double> frameMs = new();
        private readonly List<double> gcBytes = new();
        private readonly List<double> managedMb = new();
        private readonly List<double> graphicsMb = new();
        private ProfilerRecorder gcRecorder;
        private ProfilerRecorder managedRecorder;
        private ProfilerRecorder graphicsRecorder;
        private int gateBaselineIndex;
        private int suppressedSampleFrames;

        public void Begin()
        {
            TryStart(ref gcRecorder, ProfilerCategory.Memory, "GC Allocated In Frame");
            TryStart(ref managedRecorder, ProfilerCategory.Memory, "Total Used Memory");
            TryStart(ref graphicsRecorder, ProfilerCategory.Memory, "Gfx Used Memory");
        }

        public void MarkGateBaseline()
        {
            gateBaselineIndex = frameMs.Count;
        }

        public void SuppressSamplesFor(int frames)
        {
            suppressedSampleFrames = Mathf.Max(suppressedSampleFrames, Mathf.Max(0, frames));
        }

        public void Tick()
        {
            if (suppressedSampleFrames > 0)
            {
                suppressedSampleFrames--;
                return;
            }

            frameMs.Add(Mathf.Max(0f, Time.unscaledDeltaTime) * 1000d);
            if (gcRecorder.Valid)
            {
                gcBytes.Add(Math.Max(0, gcRecorder.LastValue));
            }

            if (managedRecorder.Valid)
            {
                managedMb.Add(Math.Max(0, managedRecorder.LastValue) / (1024d * 1024d));
            }
            else
            {
                managedMb.Add(Profiler.GetTotalAllocatedMemoryLong() / (1024d * 1024d));
            }

            if (graphicsRecorder.Valid)
            {
                graphicsMb.Add(Math.Max(0, graphicsRecorder.LastValue) / (1024d * 1024d));
            }
        }

        public M139SoakMetricSummary BuildSummary()
        {
            var frameWindow = Window(frameMs);
            var gcWindow = Window(gcBytes);
            var managedWindow = Window(managedMb);
            var graphicsWindow = Window(graphicsMb);
            return new M139SoakMetricSummary
            {
                FrameP95Ms = Percentile(frameWindow, 0.95d),
                FrameMaxMs = frameWindow.Count > 0 ? frameWindow.Max() : 0d,
                RecurringGcP95Bytes = Percentile(gcWindow, 0.95d),
                ManagedMemoryDriftMb = Drift(managedWindow),
                GraphicsMemoryDriftMb = Drift(graphicsWindow)
            };
        }

        public void Dispose()
        {
            DisposeRecorder(ref gcRecorder);
            DisposeRecorder(ref managedRecorder);
            DisposeRecorder(ref graphicsRecorder);
        }

        private static void TryStart(ref ProfilerRecorder recorder, ProfilerCategory category, string statName)
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

        private static double Percentile(List<double> values, double percentile)
        {
            if (values == null || values.Count == 0)
            {
                return 0d;
            }

            var sorted = values.OrderBy(value => value).ToArray();
            var index = Mathf.Clamp((int)Math.Ceiling(percentile * sorted.Length) - 1, 0, sorted.Length - 1);
            return sorted[index];
        }

        private List<double> Window(List<double> values)
        {
            if (values == null || values.Count == 0)
            {
                return new List<double>();
            }

            var start = Mathf.Clamp(gateBaselineIndex, 0, values.Count - 1);
            return start <= 0 ? values : values.GetRange(start, values.Count - start);
        }

        private static double Drift(List<double> values)
        {
            if (values == null || values.Count < 2)
            {
                return 0d;
            }

            return Math.Max(0d, values.Max() - values[0]);
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
}
