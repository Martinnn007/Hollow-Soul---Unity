using System;
using System.IO;
using Hollow.Branches;
using Hollow.Combat;
using Hollow.Core;
using Hollow.Core.Diagnostics;
using Hollow.Data.Definitions;
using Hollow.Entities;
using Hollow.Platform;
using Hollow.Presentation;
using Hollow.Rooms;
using Hollow.World;
using NUnit.Framework;
using UnityEngine;

namespace Hollow.Tests.EditMode
{
    public sealed class BranchLevelLoadingScreenTests
    {
        private const string BranchSessionControllerPath = "Assets/_Hollow/Scripts/Hollow.Branches/BranchSessionController.cs";
        private const string SamplePath = "Assets/_Hollow/Data/Rooms/Templates/combat_single_sample.hollowruntime.json";

        [SetUp]
        public void SetUp()
        {
            M136PerformanceOperationCounters.Reset();
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var loadingScreen in UnityEngine.Object.FindObjectsByType<BranchLoadingScreenController>(FindObjectsInactive.Include))
            {
                if (loadingScreen != null)
                {
                    UnityEngine.Object.DestroyImmediate(loadingScreen.gameObject);
                }
            }
        }

        [Test]
        public void BranchLoadingScreenShowsProgressAndHidesBeforeGameplayReveal()
        {
            var root = new GameObject("BranchLoadingHarness");
            try
            {
                var screen = BranchLoadingScreenController.Create(root.transform);

                screen.Show("Entering Branch", "Warming enemies", 0.42f);
                Assert.IsTrue(screen.IsVisible);
                Assert.AreEqual("Warming enemies", screen.CurrentStage);
                Assert.AreEqual(0.42f, screen.CurrentProgress01, 0.001f);
                Assert.AreSame(root.transform, screen.transform.parent);

                screen.Hide();
                Assert.IsFalse(screen.IsVisible);
                Assert.AreEqual(0f, screen.CurrentProgress01, 0.001f);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void NormalTraversalRoutineUsesBranchLoadingForBossOnlyAndNeverShowsRoomCurtain()
        {
            var source = File.ReadAllText(BranchSessionControllerPath);
            var routine = ExtractMethodBlock(source, "private IEnumerator TraverseStagedRoutine");

            Assert.IsFalse(routine.Contains("ShowTransitionCurtain(", StringComparison.Ordinal));
            Assert.IsFalse(routine.Contains("HideTransitionCurtain(", StringComparison.Ordinal));
            StringAssert.Contains("ShouldShowBossLoading", routine);
            StringAssert.Contains("ShowBranchLoadingScreen(\"Boss Room\"", routine);
            StringAssert.Contains("ActivateCurrentRoomFromLiveCacheRoutine", routine);
            StringAssert.Contains("branchLiveRoomCache.Count == 0", routine);
            Assert.IsFalse(routine.Contains("IsSpaceshipHub || branchLiveRoomCache.Count == 0", StringComparison.Ordinal));
            StringAssert.Contains("ReportTraversalColdCacheMiss", routine);
        }

        [Test]
        public void BranchLoadingBuildsLiveRoomsBeforeGameplayReveal()
        {
            var source = File.ReadAllText(BranchSessionControllerPath);
            var branchLoadRoutine = ExtractMethodBlock(source, "private IEnumerator LoadCurrentBranchRoutine");
            var loadCurrentBranch = ExtractMethodBlock(source, "private void LoadCurrentBranchWithLoading");
            var liveActivationRoutine = ExtractMethodBlock(source, "private IEnumerator ActivateCurrentRoomFromLiveCacheRoutine");

            Assert.IsFalse(loadCurrentBranch.Contains("|| IsSpaceshipHub", StringComparison.Ordinal));
            StringAssert.Contains("BuildLiveRoomsForLoadingRoutine", branchLoadRoutine);
            StringAssert.Contains("ActivateCurrentRoomFromLiveCacheRoutine", branchLoadRoutine);
            StringAssert.Contains("branchLiveRoomCache.PrepareRoomForEntry", liveActivationRoutine);
            StringAssert.Contains("branchLiveRoomCache.ActivateRoom", liveActivationRoutine);
            StringAssert.Contains("DestroyTransientInteractables();", liveActivationRoutine);
            StringAssert.Contains("roomCombatController.BeginRoom(", liveActivationRoutine);
            Assert.IsFalse(liveActivationRoutine.Contains("BeginRoomStaged", StringComparison.Ordinal));
        }

        [Test]
        public void BranchSessionDisablesCombatAutoInitializeBeforeLoadingRoom()
        {
            var root = new GameObject("BranchAutoInitializeHarness");
            try
            {
                var roomObject = new GameObject("RoomRuntimeRoot");
                roomObject.transform.SetParent(root.transform, false);
                roomObject.AddComponent<RoomRuntimeRoot>();

                var playerObject = new GameObject("PlayerCharacter");
                playerObject.transform.SetParent(root.transform, false);
                var player = playerObject.AddComponent<PlaceholderPlayerController>();
                player.ConfigureDefault();
                playerObject.AddComponent<CombatantHealth>().Configure(RoomCombatController.PlayerMaxHealth);

                var enemyPrefab = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                enemyPrefab.name = "EnemyBase_TestPrefab";
                enemyPrefab.transform.SetParent(root.transform, false);
                enemyPrefab.SetActive(false);
                enemyPrefab.AddComponent<CombatantHealth>();
                enemyPrefab.AddComponent<ChaserEnemyController>();

                var projectilePrefab = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                projectilePrefab.name = "ProjectileBase_TestPrefab";
                projectilePrefab.transform.SetParent(root.transform, false);
                projectilePrefab.SetActive(false);
                projectilePrefab.AddComponent<ProjectileController>();

                var combat = root.AddComponent<RoomCombatController>();
                combat.Configure(enemyPrefab, projectilePrefab, EnemyCatalog.CreateRuntimeDefault(), DifficultyTierDefinition.CreateRuntimeDeveloperSample());
                Assert.IsTrue(combat.AutoInitialize);

                var branch = root.AddComponent<BranchSessionController>();
                branch.Configure(null, null);
                branch.Initialize(
                    HollowRuntimeV2Importer.Import(File.ReadAllText(SamplePath)),
                    GameSessionState.Create(RuntimeSessionMode.ProfileBacked, HollowPlatformKind.WindowsStandard3D, null, Vector3.zero));

                Assert.IsFalse(combat.AutoInitialize);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void M136CountersExposeBranchLoadingBossLoadingColdMissAndEnemyPoolTelemetry()
        {
            M136PerformanceOperationCounters.ReportBranchLoadingStart();
            M136PerformanceOperationCounters.ReportBranchLoadingCompletion(123.4f);
            M136PerformanceOperationCounters.ReportBossLoadingStart();
            M136PerformanceOperationCounters.ReportBossLoadingCompletion(45.6f);
            M136PerformanceOperationCounters.ReportFullBranchPreloadRoom();
            M136PerformanceOperationCounters.ReportBranchLiveRoomBuilt(9.5f, 1024);
            M136PerformanceOperationCounters.ReportBranchLiveRoomCacheHit();
            M136PerformanceOperationCounters.ReportBranchLiveRoomCacheMiss();
            M136PerformanceOperationCounters.ReportNormalTraversalRoomRebuildCall();
            M136PerformanceOperationCounters.ReportNormalTraversalWarmCall();
            M136PerformanceOperationCounters.ReportHibernatedRoomActiveObjectLeak(2);
            M136PerformanceOperationCounters.ReportTraversalColdCacheMiss();
            M136PerformanceOperationCounters.ReportEnemyPoolWarmRequest();
            M136PerformanceOperationCounters.ReportEnemyPoolWarmCompletion();
            M136PerformanceOperationCounters.ReportEnemyPoolRent();
            M136PerformanceOperationCounters.ReportEnemyPoolReturn();
            M136PerformanceOperationCounters.ReportEnemyPoolMiss();
            M136PerformanceOperationCounters.ReportEnemyPoolHardInstantiate();

            var snapshot = M136PerformanceOperationCounters.Snapshot();
            Assert.AreEqual(1, snapshot.BranchLoadingStarts);
            Assert.AreEqual(1, snapshot.BranchLoadingCompletions);
            Assert.AreEqual(1, snapshot.BossLoadingStarts);
            Assert.AreEqual(1, snapshot.BossLoadingCompletions);
            Assert.AreEqual(123.4f, snapshot.BranchLoadingMaxMilliseconds, 0.001f);
            Assert.AreEqual(45.6f, snapshot.BossLoadingMaxMilliseconds, 0.001f);
            Assert.AreEqual(1, snapshot.FullBranchPreloadRooms);
            Assert.AreEqual(1, snapshot.BranchLiveRoomsBuilt);
            Assert.AreEqual(9.5f, snapshot.BranchLiveRoomBuildMaxMilliseconds, 0.001f);
            Assert.AreEqual(1024, snapshot.BranchLiveRoomBuildGcMaxBytes);
            Assert.AreEqual(1, snapshot.BranchLiveRoomCacheHits);
            Assert.AreEqual(1, snapshot.BranchLiveRoomCacheMisses);
            Assert.AreEqual(1, snapshot.NormalTraversalRoomRebuildCalls);
            Assert.AreEqual(1, snapshot.NormalTraversalWarmCalls);
            Assert.AreEqual(2, snapshot.HibernatedRoomActiveObjectLeaks);
            Assert.AreEqual(1, snapshot.TraversalColdCacheMisses);
            Assert.AreEqual(1, snapshot.EnemyPoolWarmRequests);
            Assert.AreEqual(1, snapshot.EnemyPoolWarmCompletions);
            Assert.AreEqual(1, snapshot.EnemyPoolRents);
            Assert.AreEqual(1, snapshot.EnemyPoolReturns);
            Assert.AreEqual(1, snapshot.EnemyPoolMisses);
            Assert.AreEqual(1, snapshot.EnemyPoolHardInstantiates);
        }

        [Test]
        public void EnemyRuntimePoolResetClearsRuntimeVisualsAndDisablesEnemy()
        {
            var root = new GameObject("PooledEnemy");
            try
            {
                var enemy = root.AddComponent<EnemyRuntimeController>();
                enemy.MarkBranchPooled("branch:test");
                enemy.enabled = true;
                root.SetActive(true);

                var visual = new GameObject("RuntimePresentationVisual");
                visual.transform.SetParent(root.transform, worldPositionStays: false);
                visual.AddComponent<PresentationVisualMarker>();

                enemy.PrepareForBranchPoolReturn();

                Assert.AreEqual("branch:test", enemy.BranchPoolKey);
                Assert.IsTrue(enemy.IsBranchPooledEnemy);
                Assert.IsFalse(enemy.enabled);
                Assert.IsFalse(root.activeSelf);
                Assert.AreEqual(0, root.GetComponentsInChildren<PresentationVisualMarker>(includeInactive: true).Length);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static string ExtractMethodBlock(string source, string methodSignature)
        {
            var start = source.IndexOf(methodSignature, StringComparison.Ordinal);
            Assert.GreaterOrEqual(start, 0, methodSignature);
            var braceStart = source.IndexOf('{', start);
            Assert.GreaterOrEqual(braceStart, 0, methodSignature);

            var depth = 0;
            for (var index = braceStart; index < source.Length; index++)
            {
                if (source[index] == '{')
                {
                    depth++;
                }
                else if (source[index] == '}')
                {
                    depth--;
                    if (depth == 0)
                    {
                        return source.Substring(start, index - start + 1);
                    }
                }
            }

            Assert.Fail($"Could not extract method block for {methodSignature}.");
            return string.Empty;
        }
    }
}
