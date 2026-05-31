using System.Collections;
using System.IO;
using Hollow.Core;
using Hollow.Core.Diagnostics;
using Hollow.Data.Definitions;
using Hollow.Presentation;
using Hollow.Rooms;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Hollow.Tests.EditMode
{
    public sealed class PreM141RoomEntryVisualArtifactTests
    {
        [SetUp]
        public void SetUp()
        {
            M136PerformanceOperationCounters.Reset();
            PresentationContentProvider.Reset();
            RoomRuntimeDescriptorCache.Clear();
            HollowRuntimePool.ResetDiagnostics();
            VfxPresenter.DebugPrimitivePlaybackEnabled = false;
        }

        [TearDown]
        public void TearDown()
        {
            PresentationContentProvider.Reset();
            RoomRuntimeDescriptorCache.Clear();
            VfxPresenter.DebugPrimitivePlaybackEnabled = false;
        }

        [Test]
        public void StagedRoomBuildKeepsOldRoomVisibleAndNewRoomHiddenUntilCommit()
        {
            var rootObject = new GameObject("PreM141RuntimeRoot");
            var oldVisibleChild = GameObject.CreatePrimitive(PrimitiveType.Cube);
            oldVisibleChild.name = "OldVisibleRoom";
            oldVisibleChild.transform.SetParent(rootObject.transform, false);
            try
            {
                var root = rootObject.AddComponent<RoomRuntimeRoot>();
                var asset = CreateRuntimeRoomAsset("pre_m141_staged_room");
                var routine = root.BuildFromStaged(asset, RoomNavMeshRuntimeFallbackMode.EditorOrDevelopmentRuntimeBake);

                for (var step = 0; step < 4; step++)
                {
                    Assert.IsTrue(routine.MoveNext(), $"Staged build ended before hidden stage {step}.");
                    Assert.IsTrue(oldVisibleChild != null && oldVisibleChild.activeInHierarchy);
                    var stagingRoot = rootObject.transform.Find("__RoomRuntimeStaging");
                    Assert.IsNotNull(stagingRoot);
                    Assert.IsFalse(stagingRoot.gameObject.activeInHierarchy);
                    Assert.AreEqual(0, M136PerformanceOperationCounters.Snapshot().StagedRoomVisibleRendererFrames);
                }

                RunToCompletion(routine);

                Assert.IsTrue(oldVisibleChild == null || !oldVisibleChild.activeInHierarchy);
                Assert.IsNull(rootObject.transform.Find("__RoomRuntimeStaging"));
                Assert.Greater(rootObject.transform.childCount, 0);
                var snapshot = M136PerformanceOperationCounters.Snapshot();
                Assert.AreEqual(0, snapshot.StagedRoomVisibleRendererFrames);
                Assert.AreEqual(1, snapshot.NormalTraversalRevealFrames);
            }
            finally
            {
                Object.DestroyImmediate(rootObject);
            }
        }

        [Test]
        public void DeferredStagedRoomBuildKeepsPreviousRoomVisibleUntilExplicitReveal()
        {
            var rootObject = new GameObject("PreM141DeferredRuntimeRoot");
            var oldVisibleChild = GameObject.CreatePrimitive(PrimitiveType.Cube);
            oldVisibleChild.name = "OldVisibleRoom";
            oldVisibleChild.transform.SetParent(rootObject.transform, false);
            try
            {
                var root = rootObject.AddComponent<RoomRuntimeRoot>();
                var asset = CreateRuntimeRoomAsset("pre_m141_deferred_room");
                RunToCompletion(root.BuildFromStaged(
                    asset,
                    RoomNavMeshRuntimeFallbackMode.EditorOrDevelopmentRuntimeBake,
                    revealOnCommit: false));

                Assert.IsTrue(oldVisibleChild != null && oldVisibleChild.activeInHierarchy);
                var stagingRoot = rootObject.transform.Find("__RoomRuntimeStaging");
                Assert.IsNotNull(stagingRoot);
                Assert.IsFalse(stagingRoot.gameObject.activeInHierarchy);

                root.CommitPendingStagedBuildForReveal();

                Assert.IsTrue(oldVisibleChild == null || !oldVisibleChild.activeInHierarchy);
                Assert.IsNull(rootObject.transform.Find("__RoomRuntimeStaging"));
                Assert.Greater(rootObject.transform.childCount, 0);
                Assert.AreEqual(1, M136PerformanceOperationCounters.Snapshot().NormalTraversalRevealFrames);
            }
            finally
            {
                Object.DestroyImmediate(rootObject);
            }
        }

        [Test]
        public void PoolWarmupKeepsGeneratedPrimitivesUnderInactiveHiddenRoot()
        {
            RunToCompletion(HollowRuntimePool.WarmPrimitivePool(
                "PreM141.HiddenPrimitiveWarm",
                PrimitiveType.Sphere,
                count: 6,
                perFrame: 2));

            var snapshot = M136PerformanceOperationCounters.Snapshot();
            Assert.IsFalse(HollowRuntimePool.IsWarmRootVisibleForDiagnostics);
            Assert.AreEqual(0, snapshot.PoolWarmVisibleObjects);
            Assert.AreEqual(0, snapshot.PoolWarmRootActiveErrors);
            Assert.AreEqual(0, snapshot.PoolWarmActiveLeaks);
        }

        [Test]
        public void PresentationCatalogBindsVisibleGameplayRolesInsteadOfPrimitiveFallbacks()
        {
            var catalog = PresentationContentProvider.ActiveCatalog;
            Assert.IsNotNull(catalog, "Default presentation content catalog must be loadable from Resources.");

            var roles = new[]
            {
                PresentationPrefabRole.RoomHazardSpike,
                PresentationPrefabRole.StandardBarrel,
                PresentationPrefabRole.ExplosiveBarrel,
                PresentationPrefabRole.HazardCoinDrop,
                PresentationPrefabRole.CoinCopper,
                PresentationPrefabRole.CoinSilver,
                PresentationPrefabRole.CoinGold,
                PresentationPrefabRole.VfxChestOpen,
                PresentationPrefabRole.VfxCoinPickup,
                PresentationPrefabRole.EnemyRat,
                PresentationPrefabRole.EnemySpider,
                PresentationPrefabRole.EnemyHollowBird,
                PresentationPrefabRole.EnemySoulEater,
                PresentationPrefabRole.DecorGrassTuft,
                PresentationPrefabRole.DecorCrystalCluster,
                PresentationPrefabRole.DecorSmallTree,
                PresentationPrefabRole.DecorStoneRuin
            };

            foreach (var role in roles)
            {
                Assert.IsTrue(catalog.TryGetPrefab(role, out var prefab) && prefab != null, $"{role} must resolve to a real prefab in gameplay.");
            }
        }

        [Test]
        public void RuntimeDoorVisualsDoNotExposeAnchorRendererOrDoorDots()
        {
            var rootObject = new GameObject("PreM141DoorVisualRuntimeRoot");
            try
            {
                var root = rootObject.AddComponent<RoomRuntimeRoot>();
                root.BuildFrom(CreateRuntimeRoomAsset("pre_m141_door_visuals"), RoomNavMeshRuntimeFallbackMode.EditorOrDevelopmentRuntimeBake);

                var doorAnchor = rootObject.transform.Find("doorAnchorActive.north_0");
                Assert.IsNotNull(doorAnchor);

                var anchorRenderer = doorAnchor.GetComponent<Renderer>();
                Assert.IsNotNull(anchorRenderer);
                Assert.IsFalse(anchorRenderer.enabled, "The primitive door anchor must not render in gameplay.");

                var doorDot = FindChildByName(doorAnchor, "door_dot");
                Assert.IsNotNull(doorDot);
                Assert.IsFalse(doorDot.gameObject.activeSelf, "Art-pass door dots are authoring/debug accents and must not show in production traversal.");
            }
            finally
            {
                Object.DestroyImmediate(rootObject);
            }
        }

        [Test]
        public void StagedTraversalDoesNotSpawnDoorUnlockVfxBeforeReveal()
        {
            var source = File.ReadAllText("Assets/_Hollow/Scripts/Hollow.Branches/BranchSessionController.cs");
            var start = source.IndexOf("private IEnumerator LoadCurrentRoomStaged", System.StringComparison.Ordinal);
            var end = source.IndexOf("private IEnumerator WarmTransitionPools", System.StringComparison.Ordinal);
            Assert.GreaterOrEqual(start, 0);
            Assert.Greater(end, start);
            var stagedLoadBody = source.Substring(start, end - start);

            StringAssert.DoesNotContain("VfxPresenter.Play(VfxCueId.DoorUnlock", stagedLoadBody);
            StringAssert.DoesNotContain("AudioPresenter.Play(AudioCueId.DoorUnlock", stagedLoadBody);
            StringAssert.Contains("BuildFromStaged(currentRoomAsset, RoomNavMeshRuntimeFallbackMode.RequireCatalogBake, revealOnCommit: false)", stagedLoadBody);
            StringAssert.Contains("SuppressRoomEntryRenderers();", stagedLoadBody);
            StringAssert.Contains("RevealRoomEntryVisuals();", stagedLoadBody);
            StringAssert.Contains("CommitPendingStagedBuildForReveal", source);
            StringAssert.Contains("UpdateDoorVisuals", stagedLoadBody);
            StringAssert.Contains("var revealPlayerLocalPosition = playerLocalPosition;", stagedLoadBody);
            StringAssert.Contains("playerController.transform.localPosition = revealPlayerLocalPosition;", stagedLoadBody);
        }

        [Test]
        public void VfxDebugPrimitiveFallbacksAreGatedOutOfNormalPlayMode()
        {
            var source = File.ReadAllText("Assets/_Hollow/Scripts/Hollow.Presentation/VfxPresenter.cs");

            StringAssert.Contains("DebugPrimitivePlaybackEnabled", source);
            StringAssert.Contains("CanPlayDebugPrimitiveVisuals", source);
            StringAssert.Contains("return !Application.isPlaying || DebugPrimitivePlaybackEnabled;", source);
            StringAssert.Contains("M136PerformanceOperationCounters.ReportPresentationFallbackVisual();", source);
        }

        [Test]
        public void StagedEnemyActivationIsDeferredUntilRoomReveal()
        {
            var combatSource = File.ReadAllText("Assets/_Hollow/Scripts/Hollow.Combat/RoomCombatController.cs");
            var spawnSource = File.ReadAllText("Assets/_Hollow/Scripts/Hollow.Combat/EnemySpawnService.cs");

            StringAssert.Contains("activateOnComplete: false", combatSource);
            StringAssert.Contains("ActivateStagedEnemiesForReveal", combatSource);
            StringAssert.Contains("bool activateOnComplete = true", spawnSource);
            StringAssert.Contains("if (!activateOnComplete)", spawnSource);
        }

        private static void RunToCompletion(IEnumerator routine)
        {
            var guard = 0;
            while (routine.MoveNext())
            {
                guard++;
                Assert.Less(guard, 128);
            }
        }

        private static Transform FindChildByName(Transform root, string childName)
        {
            if (root == null)
            {
                return null;
            }

            for (var index = 0; index < root.childCount; index++)
            {
                var child = root.GetChild(index);
                if (child.name == childName)
                {
                    return child;
                }

                var nested = FindChildByName(child, childName);
                if (nested != null)
                {
                    return nested;
                }
            }

            return null;
        }

        private static ImportedRoomRuntimeAsset CreateRuntimeRoomAsset(string id)
        {
            var layout = new RoomLayout(
                5,
                5,
                Rect.MinMaxRect(-2.5f, -2.5f, 2.5f, 2.5f),
                new[]
                {
                    new Vector2Int(0, 0),
                    new Vector2Int(1, 0),
                    new Vector2Int(0, 1),
                    new Vector2Int(1, 1)
                },
                new[] { new Vector2Int(-1, -1) },
                new[]
                {
                    new RoomLayoutFloorRegion("floor_a", Vector3.zero, new Vector2(2.5f, 2.5f))
                },
                new[]
                {
                    new RoomLayoutObstacle("rock_a", "roomObstacleRock", new Vector3(1f, 0.5f, 0f), Vector3.one, true)
                });
            var footprint = new RoomInstanceFootprint(Vector2Int.zero, new[] { Vector2Int.zero }, new Vector2Int(5, 5));
            return new ImportedRoomRuntimeAsset(
                id,
                "Pre M141 Room",
                RoomBiomeIds.HollowThreshold,
                layout,
                footprint,
                new[] { CreateDoorPort("north_0", "north", 0f) },
                new[]
                {
                    new ImportedSpawnPoint
                    {
                        id = "enemy_a",
                        kind = "spawnEnemyRat",
                        position = CreateVector3(-1f, 0f, 0f)
                    }
                },
                new[]
                {
                    new ImportedSpawnPoint
                    {
                        id = "item_a",
                        kind = "spawnReward",
                        position = CreateVector3(1f, 0f, 0f)
                    }
                },
                new ImportedSpawnPoint
                {
                    id = "safe_start",
                    kind = "spawnSafeStart",
                    position = CreateVector3(0f, 0f, -1f)
                },
                new[]
                {
                    new ImportedRoomHazard
                    {
                        id = "hazard_a",
                        kind = RoomHazardKind.Spike,
                        center = CreateVector3(0f, 0f, 1f),
                        radius = 0.45f
                    }
                },
                new[]
                {
                    new ImportedRoomInteractiveObject
                    {
                        id = "barrel_a",
                        kind = RoomInteractiveObjectKind.StandardBarrel,
                        center = CreateVector3(-1f, 0.5f, 1f),
                        size = CreateVector3(0.6f, 1f, 0.6f),
                        blocksMovement = true,
                        blocksProjectiles = true
                    }
                },
                new[]
                {
                    new ImportedRoomDecor
                    {
                        id = "decor_a",
                        kind = "crystal_cluster",
                        center = CreateVector3(1f, 0f, 1f),
                        size = CreateVector3(1f, 1f, 1f),
                        blocking = false,
                        blocksProjectiles = false
                    }
                },
                null);
        }

        private static RoomDoorPort CreateDoorPort(string id, string direction, float x)
        {
            return new RoomDoorPort(
                id,
                direction,
                0,
                Vector2Int.zero,
                new Vector2(x, 2.5f),
                new Vector3(x, 0f, 2.5f),
                "door");
        }

        private static ImportedVector3 CreateVector3(float x, float y, float z)
        {
            return new ImportedVector3
            {
                x = x,
                y = y,
                z = z
            };
        }
    }
}
