using System.IO;
using System.Linq;
using Hollow.Branches;
using Hollow.Combat;
using Hollow.Data.Definitions;
using Hollow.Editor.Generation;
using Hollow.Editor.Validation;
using Hollow.Rewards;
using Hollow.RoomDesigner;
using Hollow.Rooms;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Hollow.Tests.EditMode
{
    public sealed class Milestone133NpcSpecialEncounterPrototypeSetTests
    {
        private const string SamplePath = "Assets/_Hollow/Data/Rooms/Templates/combat_single_sample.hollowruntime.json";

        [Test]
        public void SpecialEncounterPolicyIsRareTerminalAndSeeded()
        {
            var content = CreateContent(out var settings);
            var profile = AssetDatabase.LoadAssetAtPath<EncounterDirectorProfileDefinition>(Milestone46AssetGenerator.DirectorProfilePath);
            var specialRoomsPlaced = 0;
            var rolledSeeds = 0;

            for (var seed = 13300; seed < 13500; seed++)
            {
                if (BranchGenerator.ShouldRollSpecialEncounterLeaf(BranchGenerator.BranchFeaturesId, seed))
                {
                    rolledSeeds++;
                }

                var m20Graph = BranchGenerator.CreateSeededBranchFeatures(
                    content,
                    settings,
                    seed,
                    RoomBiomeIds.HollowThreshold,
                    enableCorruptedChestLeaf: true,
                    enableWaveRoomLeaf: true,
                    enableSpecialEncounterLeaf: true);
                specialRoomsPlaced += AssertSpecialLeafPolicy(m20Graph);

                var m46Graph = BranchGenerator.CreateDirectedEncounterBranch(
                    content,
                    settings,
                    profile,
                    worldIndex: 1,
                    seed: seed,
                    bossRoomAssetId: string.Empty,
                    biomeId: RoomBiomeIds.HollowThreshold,
                    enableCorruptedChestLeaf: true,
                    enableWaveRoomLeaf: true,
                    enableSpecialEncounterLeaf: true);
                specialRoomsPlaced += AssertSpecialLeafPolicy(m46Graph);
            }

            Assert.Greater(rolledSeeds, 0);
            Assert.Greater(specialRoomsPlaced, 0);
            var selection = Enumerable.Range(13300, 200)
                .Select(seed => SpecialEncounterResolver.ResolveKind(BranchGenerator.BranchFeaturesId, seed))
                .ToArray();
            CollectionAssert.Contains(selection, SpecialEncounterKind.SoulEater);
            CollectionAssert.Contains(selection, SpecialEncounterKind.Escapist);
        }

        [Test]
        public void SoulEaterOfferUsesRunSoulsAndCuratedReward()
        {
            var economy = new RunEconomy();
            economy.AddSouls(SpecialEncounterResolver.SoulEaterSoulPrice - 1);
            Assert.IsFalse(economy.SpendSouls(SpecialEncounterResolver.SoulEaterSoulPrice));
            economy.AddSouls(1);

            var grant = SpecialEncounterResolver.ResolveSoulEaterOffer("m133_test", 13301, "room_special");
            CollectionAssert.Contains(ChestRewardResolver.CorruptedRareRewardIds.ToArray(), grant.RewardId);
            Assert.AreEqual(SpecialEncounterResolver.SoulEaterRewardContextId("room_special"), grant.RoomId);
            Assert.IsTrue(economy.SpendSouls(SpecialEncounterResolver.SoulEaterSoulPrice));
            Assert.IsTrue(economy.ApplyReward(grant));
            Assert.AreEqual(0, economy.RunSouls);
            Assert.IsTrue(economy.HasCollectedRoomReward(SpecialEncounterResolver.SoulEaterRewardContextId("room_special")));

            var runtimeCopy = File.ReadAllText(Milestone133NpcSpecialEncounterPrototypeSetAssetGenerator.BranchSessionControllerPath) +
                              File.ReadAllText(Milestone133NpcSpecialEncounterPrototypeSetAssetGenerator.SpecialEncounterResolverPath);
            StringAssert.Contains("Need {SpecialEncounterResolver.SoulEaterSoulPrice} Souls", runtimeCopy);
            Assert.IsFalse(runtimeCopy.Contains("Unbanked Souls"));
            Assert.IsFalse(runtimeCopy.Contains("Banked Souls"));
        }

        [Test]
        public void EscapistRewardsGoldenChestOnlyOnSuccessPolicy()
        {
            var graph = new BranchFloorGraph("m133_reward_policy", 13301);
            graph.AddRoom(new BranchRoomState(
                new BranchRoomId("room_escapist"),
                Vector2Int.zero,
                new BranchRoomInstanceId("room_escapist"),
                SpecialEncounterResolver.EscapistRoomAssetId,
                null,
                BranchRoomRole.SpecialEncounter));
            graph.AddRoom(new BranchRoomState(
                new BranchRoomId("room_soul_eater"),
                new Vector2Int(1, 0),
                new BranchRoomInstanceId("room_soul_eater"),
                SpecialEncounterResolver.SoulEaterRoomAssetId,
                null,
                BranchRoomRole.SpecialEncounter));

            var rewards = ProceduralRewardResolver.CreateSeededPlan(graph, null, null, null);
            Assert.IsTrue(rewards.TryResolve("room_escapist", out var escapistGrant));
            Assert.AreEqual(ChestRewardResolver.GoldenChestRewardId, escapistGrant.RewardId);
            Assert.AreEqual(ChestKind.Golden, ChestRewardResolver.KindForGrant(escapistGrant));

            Assert.IsTrue(rewards.TryResolve("room_soul_eater", out var soulEaterGrant));
            Assert.IsTrue(soulEaterGrant.IsEmpty);

            var session = File.ReadAllText(Milestone133NpcSpecialEncounterPrototypeSetAssetGenerator.BranchSessionControllerPath);
            StringAssert.Contains("ForceClearRoomWithoutReward", session);
            StringAssert.Contains("Escapist escaped. No reward.", session);
        }

        [Test]
        public void SpecialRoomsKeepDoorwayEntriesWalkableAfterDoorLock()
        {
            Assert.IsTrue(HollowRuntimeV2Importer.TryImport(
                File.ReadAllText(Milestone133NpcSpecialEncounterPrototypeSetAssetGenerator.EscapistRoomPath),
                out var escapist,
                out var escapistError),
                escapistError);
            Assert.IsTrue(HollowRuntimeV2Importer.TryImport(
                File.ReadAllText(Milestone133NpcSpecialEncounterPrototypeSetAssetGenerator.SoulEaterRoomPath),
                out var soulEater,
                out var soulEaterError),
                soulEaterError);

            AssertDoorwayEntriesStayWalkableAfterDoorLock(escapist);
            AssertDoorwayEntriesStayWalkableAfterDoorLock(soulEater);
        }

        private static void AssertDoorwayEntriesStayWalkableAfterDoorLock(ImportedRoomRuntimeAsset asset)
        {
            Assert.AreEqual(91, asset.Layout.WalkableTiles.Count, asset.Id);
            var host = new GameObject($"{asset.Id}_doorway_access_test");
            try
            {
                var room = host.AddComponent<RoomRuntimeRoot>();
                room.BuildFrom(asset);
                foreach (var port in room.DoorPorts)
                {
                    room.SetDoorVisualStateById(port.Id, RoomDoorVisualState.Locked);
                }

                var radius = Hollow.Entities.PlaceholderPlayerController.DefaultRadiusMeters;
                var safeStart = room.SafeStartLocalPosition;
                Assert.IsTrue(RoomLocalCollision.CanOccupy(room, safeStart, radius), $"{asset.Id} safe start must stay occupiable after door locks.");

                foreach (var direction in new[] { "north", "south", "east", "west" })
                {
                    var entry = BranchTraversalService.EntryPositionFor(room, direction);
                    var inward = BranchTraversalService.EntryInsetDirectionFor(direction);
                    var resolved = RoomLocalCollision.ResolveNearestOccupiablePosition(room, entry, radius, inward, 3f);
                    Assert.IsTrue(RoomLocalCollision.CanOccupy(room, resolved, radius), $"{asset.Id}:{direction} doorway entry must resolve to an occupiable point after door locks.");
                    Assert.LessOrEqual(Vector3.Distance(resolved, entry), 0.05f, $"{asset.Id}:{direction} doorway entry should stay at the door inset instead of snapping inward.");

                    var moved = RoomLocalCollision.ResolveMove(room, resolved, resolved + inward * 0.35f, radius);
                    Assert.Greater(Vector3.Distance(resolved, moved), 0.1f, $"{asset.Id}:{direction} doorway entry must allow inward movement after door locks.");
                }
            }
            finally
            {
                Object.DestroyImmediate(host);
            }
        }

        [Test]
        public void EscapistRoomKeepsDoorwayEntryMovableAfterDoorLock()
        {
            Assert.IsTrue(HollowRuntimeV2Importer.TryImport(
                File.ReadAllText(Milestone133NpcSpecialEncounterPrototypeSetAssetGenerator.EscapistRoomPath),
                out var asset,
                out var error),
                error);

            AssertDoorwayEntriesStayWalkableAfterDoorLock(asset);
        }

        [Test]
        public void SpecialTemplatesImportAndInheritBranchBiome()
        {
            AssertTemplate(Milestone133NpcSpecialEncounterPrototypeSetAssetGenerator.SoulEaterRoomPath, SpecialEncounterResolver.SoulEaterRoomAssetId, "spawnEnemySoulEater");
            AssertTemplate(Milestone133NpcSpecialEncounterPrototypeSetAssetGenerator.EscapistRoomPath, SpecialEncounterResolver.EscapistRoomAssetId, SpecialEncounterResolver.EscapistSpawnKind);

            var content = CreateContent(out _);
            Assert.IsTrue(content.TryGetRoomAsset(SpecialEncounterResolver.SoulEaterRoomAssetId, RoomBiomeIds.RustChoir, out var soulEaterAsset));
            Assert.AreEqual(RoomBiomeIds.RustChoir, soulEaterAsset.BiomeId);
            Assert.IsTrue(content.TryGetRoomAsset(SpecialEncounterResolver.EscapistRoomAssetId, RoomBiomeIds.RustChoir, out var escapistAsset));
            Assert.AreEqual(RoomBiomeIds.RustChoir, escapistAsset.BiomeId);

            var host = new GameObject("m133_room_designer_marker_preview");
            try
            {
                Assert.IsTrue(RoomDesignerScenePreviewBuilder.BuildVisualForMarker(
                    host,
                    new RoomDesignerMarker("escapist", RoomDesignerMarkerKinds.EnemyEscapist, 0f, 0f, 0f),
                    RoomBiomeIds.RustChoir));
            }
            finally
            {
                Object.DestroyImmediate(host);
            }
        }

        [Test]
        public void LiveReportPassesAllSpecialEncounterChecks()
        {
            var report = Milestone133NpcSpecialEncounterPrototypeSetAssetGenerator.BuildReport();

            Assert.IsTrue(report.passed, string.Join("\n", report.failures ?? new string[0]));
            Assert.Greater(report.totalChecks, 10);
            Assert.AreEqual(report.totalChecks, report.passedChecks);
            CollectionAssert.Contains(report.evidencePaths, Milestone133NpcSpecialEncounterPrototypeSetAssetGenerator.DocsPath);
            CollectionAssert.Contains(report.evidencePaths, Milestone133NpcSpecialEncounterPrototypeSetAssetGenerator.M132ReportPath);
        }

        [Test]
        public void GeneratedReportsArePresentPassingAndUseM133LockId()
        {
            Assert.IsTrue(File.Exists(Milestone133NpcSpecialEncounterPrototypeSetAssetGenerator.ReportMarkdownPath));
            Assert.IsTrue(File.Exists(Milestone133NpcSpecialEncounterPrototypeSetAssetGenerator.ReportJsonPath));

            var markdown = File.ReadAllText(Milestone133NpcSpecialEncounterPrototypeSetAssetGenerator.ReportMarkdownPath);
            StringAssert.Contains("# M133 NPC/Special Encounter Prototype Set Report", markdown);
            StringAssert.Contains("- Result: PASSED", markdown);
            StringAssert.Contains(Milestone133NpcSpecialEncounterPrototypeSetAssetGenerator.LockId, markdown);
            StringAssert.Contains("15% optional terminal Special Encounter", markdown);
            StringAssert.Contains("Soul Eater", markdown);
            StringAssert.Contains("Escapist", markdown);

            var json = JsonUtility.FromJson<Milestone133NpcSpecialEncounterPrototypeSetReport>(
                File.ReadAllText(Milestone133NpcSpecialEncounterPrototypeSetAssetGenerator.ReportJsonPath));
            Assert.IsNotNull(json);
            Assert.AreEqual(Milestone133NpcSpecialEncounterPrototypeSetAssetGenerator.LockId, json.lockId);
            Assert.IsTrue(json.passed);
            Assert.IsTrue((json.failures ?? new string[0]).Length == 0);
        }

        [Test]
        public void ValidatorReportsGeneratedStateValid()
        {
            Assert.IsTrue(Milestone133NpcSpecialEncounterPrototypeSetValidator.Validate(exitOnFailure: false));
        }

        private static int AssertSpecialLeafPolicy(BranchFloorGraph graph)
        {
            Assert.IsTrue(BranchGenerator.ValidateSpecialRoomTopology(graph, out var topologyError), topologyError);
            var specialRooms = graph.Rooms.Where(room => room.Role == BranchRoomRole.SpecialEncounter).ToArray();
            Assert.LessOrEqual(specialRooms.Length, 1);
            if (specialRooms.Length == 0)
            {
                return 0;
            }

            var special = specialRooms[0];
            CollectionAssert.Contains(
                new[] { SpecialEncounterResolver.SoulEaterRoomAssetId, SpecialEncounterResolver.EscapistRoomAssetId },
                special.RuntimeRoomAssetId);
            var neighbors = graph.ConnectionsFrom(special.Id).Select(connection => connection.ToRoomId).Distinct().ToArray();
            Assert.AreEqual(1, neighbors.Length);
            Assert.IsTrue(graph.TryGetRoom(neighbors[0], out var parent));
            Assert.IsFalse(parent.Role is BranchRoomRole.Boss or BranchRoomRole.Secret or BranchRoomRole.Treasure or BranchRoomRole.CorruptedChest or BranchRoomRole.Wave);

            var branchFeaturePlan = BranchFeaturePlan.Create(graph);
            Assert.AreNotEqual(special.Id.Value, branchFeaturePlan.BossKeyRoomId);
            Assert.IsFalse(graph.ConnectionsFrom(special.Id).Any(connection => connection.LockKind == BranchConnectionLockKind.BossKey));
            return 1;
        }

        private static void AssertTemplate(string path, string expectedId, string expectedSpawnKind)
        {
            Assert.IsTrue(File.Exists(path), path);
            Assert.IsTrue(HollowRuntimeV2Importer.TryImport(File.ReadAllText(path), out var asset, out var error), error);
            Assert.AreEqual(expectedId, asset.Id);
            Assert.AreEqual("hollow_threshold", asset.BiomeId);
            Assert.AreEqual(91, asset.Layout.WalkableTiles.Count);
            Assert.IsTrue(asset.EnemySpawns.Any(spawn => spawn.kind == expectedSpawnKind));
            Assert.IsTrue(RuntimeRoomValidator.Validate(asset).IsValid);
        }

        private static BranchSessionContent CreateContent(out BranchGenerationSettingsDefinition settings)
        {
            settings = AssetDatabase.LoadAssetAtPath<BranchGenerationSettingsDefinition>(Milestone15AssetGenerator.SettingsPath);
            var sample = HollowRuntimeV2Importer.Import(File.ReadAllText(SamplePath));
            var catalog = AssetDatabase.LoadAssetAtPath<BranchRoomTemplateCatalogDefinition>(Milestone14AssetGenerator.CatalogPath);
            Assert.IsNotNull(catalog);
            var content = BranchSessionContent.Create(sample, catalog, settings.DefaultSeed, out var error);
            Assert.IsTrue(string.IsNullOrWhiteSpace(error), error);
            Assert.IsNotNull(content.SpecialSoulEaterRoomAsset);
            Assert.IsNotNull(content.SpecialEscapistRoomAsset);
            return content;
        }
    }
}
