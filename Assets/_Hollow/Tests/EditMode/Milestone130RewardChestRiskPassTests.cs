using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Hollow.Branches;
using Hollow.Combat;
using Hollow.Data.Definitions;
using Hollow.Editor.Generation;
using Hollow.Editor.Validation;
using Hollow.Entities;
using Hollow.Persistence;
using Hollow.Rewards;
using Hollow.RoomDesigner;
using Hollow.Rooms;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Hollow.Tests.EditMode
{
    public sealed class Milestone130RewardChestRiskPassTests
    {
        private const string SamplePath = "Assets/_Hollow/Data/Rooms/Templates/combat_single_sample.hollowruntime.json";

        [Test]
        public void CorruptedChestRoomsAreRareTerminalLeaves()
        {
            var content = CreateContent(out var settings);
            var corruptedCount = 0;
            var expectedCount = 0;
            for (var seed = 13000; seed < 13150; seed++)
            {
                var graph = BranchGenerator.CreateSeededBranchFeatures(content, settings, seed, RoomBiomeIds.HollowThreshold, enableCorruptedChestLeaf: true);
                Assert.IsTrue(BranchGenerator.ValidateSpecialRoomTopology(graph, out var topologyError), topologyError);
                var corruptedRooms = graph.Rooms.Where(room => room.Role == BranchRoomRole.CorruptedChest).ToArray();
                Assert.LessOrEqual(corruptedRooms.Length, 1);

                if (BranchGenerator.ShouldRollCorruptedChestLeaf(BranchGenerator.BranchFeaturesId, seed))
                {
                    expectedCount++;
                }

                if (corruptedRooms.Length == 0)
                {
                    continue;
                }

                corruptedCount++;
                var corrupted = corruptedRooms[0];
                var neighbors = graph.ConnectionsFrom(corrupted.Id).Select(connection => connection.ToRoomId).Distinct().ToArray();
                Assert.AreEqual(1, neighbors.Length);
                Assert.IsTrue(graph.TryGetRoom(neighbors[0], out var parent));
                Assert.IsFalse(parent.Role is BranchRoomRole.Boss or BranchRoomRole.Secret or BranchRoomRole.Treasure);
                Assert.AreEqual(BranchGenerator.CorruptedChestRoomAssetId, corrupted.RuntimeRoomAssetId);
                Assert.IsTrue(content.TryGetRoomAsset(corrupted.RuntimeRoomAssetId, out var roomAsset));
                Assert.AreEqual(BranchGenerator.CorruptedChestRoomAssetId, roomAsset.Id);
                Assert.AreEqual(1, graph.Rooms.Count(room => room.Role == BranchRoomRole.Boss));
                Assert.AreEqual(1, graph.Rooms.Count(room => room.Role == BranchRoomRole.Secret));
            }

            Assert.AreEqual(expectedCount, corruptedCount, "The deterministic M130 roll should map directly to one terminal corrupted room when placement is available.");
            Assert.Greater(corruptedCount, 0);
            Assert.AreEqual(10, ChestRewardResolver.CorruptedChestRollPercent);
        }

        [Test]
        public void CorruptedRoomsSkipEncountersAndUseCorruptedChestReward()
        {
            var graph = FirstGraphWithCorruptedRoom(out var corrupted);
            var encounterCatalog = AssetDatabase.LoadAssetAtPath<EncounterCatalogDefinition>(Milestone19AssetGenerator.EncounterCatalogPath);
            var encounterPlan = EncounterResolver.CreateSeededPlan(graph, encounterCatalog, graph.Seed);
            Assert.IsFalse(encounterPlan.TryResolve(corrupted.Id.Value, out _));

            var rewardPlan = ProceduralRewardResolver.CreateSeededPlan(graph, null, null, null);
            Assert.IsTrue(rewardPlan.TryResolve(corrupted.Id.Value, out var grant));
            Assert.AreEqual(ChestRewardResolver.CorruptedChestRewardId, grant.RewardId);
            Assert.AreEqual(ChestKind.Corrupted, ChestRewardResolver.KindForGrant(grant));
        }

        [Test]
        public void CorruptedChestContentsGrantCuratedRewardCoinsAndRunLongCurse()
        {
            var curated = Milestone130RewardChestRiskPassAssetGenerator.CuratedCorruptedRewardIds.ToHashSet();
            for (var seed = 13000; seed < 13080; seed++)
            {
                var contents = ChestRewardResolver.ResolveContents("branch", seed, "corrupted_01", ChestKind.Corrupted);
                Assert.GreaterOrEqual(contents.CoinValue, 8);
                Assert.LessOrEqual(contents.CoinValue, 12);
                Assert.IsTrue(curated.Contains(contents.RewardGrant.RewardId), contents.RewardGrant.RewardId);
                Assert.IsFalse(contents.RewardGrant.IsEmpty);
            }

            var root = CreateCorruptedChestHarness(out var branch, out var chestState, out var playerHealth);
            try
            {
                Assert.AreEqual(6, playerHealth.MaxHealth);

                Assert.IsTrue((bool)InvokePrivate(branch, "TryOpenNearestChest"));
                Assert.AreEqual(ChestState.Unopened.ToString(), chestState.state);
                Assert.IsFalse(branch.CurrentRoomChests[0].IsOpened);
                Assert.AreEqual(BranchSessionController.CorruptedChestWarningMessage, branch.LastRewardMessage);

                Assert.IsTrue((bool)InvokePrivate(branch, "TryOpenNearestChest"));
                Assert.AreEqual(ChestState.Opened.ToString(), chestState.state);
                Assert.IsTrue(branch.CurrentRoomChests[0].IsOpened);
                Assert.AreEqual(5, playerHealth.MaxHealth);
                StringAssert.Contains("Blade Lesson", branch.LastRewardMessage);
                StringAssert.Contains("-1 max HP", branch.LastRewardMessage);
                StringAssert.Contains("-1 max HP", branch.LatestPickupReveal.BodyText);
                InvokePrivate(branch, "ApplyCorruptedChestCurse", chestState);
                Assert.AreEqual(5, playerHealth.MaxHealth, "Reapplying the same corrupted chest source must not duplicate the curse.");

                var secondChestState = new RunChestStateSave
                {
                    chestId = "corrupted_02_corrupted_treasure_chest",
                    contentDisplayName = "Mend Card"
                };
                InvokePrivate(branch, "ApplyCorruptedChestCurse", secondChestState);
                Assert.AreEqual(4, playerHealth.MaxHealth, "Distinct corrupted chest sources should stack during the run.");

                var snapshot = branch.CreateSnapshot();
                Assert.AreEqual(2, snapshot.runBuild.modifiers.Count(modifier => modifier.sourceId.StartsWith(BranchSessionController.CorruptedChestCurseSourcePrefix)));
                var restored = PlayerRunBuild.FromSaveState(snapshot.runBuild);
                Assert.AreEqual(4, restored.DerivedStats.MaxHealth);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void RoomDesignerCorruptedChestMarkerRoundtripsAndPreviews()
        {
            var project = RoomDesignerProject.CreateDefault(RoomDesignerFootprintPreset.Single1x1, "M130 Corrupted Chest Designer");
            project.markers.RemoveAll(marker => marker.kind == RoomDesignerMarkerKinds.RoomReward);
            project.markers.Add(new RoomDesignerMarker("spawn_corrupted_chest_test", RoomDesignerMarkerKinds.CorruptedChestSpawn, 0f, 0f, 0f));

            var asset = RoomDesignerCompiler.Compile(project);
            Assert.IsTrue(asset.ItemSpawns.Any(spawn => spawn.kind == RoomDesignerMarkerKinds.CorruptedChestSpawn));
            Assert.AreEqual(PresentationPrefabRole.ChestCorrupted, RoomDesignerScenePreviewBuilder.PrefabRoleForMarker(RoomDesignerMarkerKinds.CorruptedChestSpawn));

            var imported = RoomDesignerRuntimeDraftImporter.FromRuntimeJson(RoomDesignerCompiler.ExportRuntimeJson(project), "m130_corrupted_chest.roomdesigner.json");
            Assert.IsTrue(imported.markers.Any(marker => marker.kind == RoomDesignerMarkerKinds.CorruptedChestSpawn));
        }

        [Test]
        public void CorruptedChestEndpointRoomImportsAsSpecialRoom()
        {
            Assert.IsTrue(File.Exists(Milestone130RewardChestRiskPassAssetGenerator.CorruptedChestEndpointRoomPath));
            var json = File.ReadAllText(Milestone130RewardChestRiskPassAssetGenerator.CorruptedChestEndpointRoomPath);
            Assert.IsTrue(HollowRuntimeV2Importer.TryImport(json, out var asset, out var error), error);
            Assert.AreEqual(BranchGenerator.CorruptedChestRoomAssetId, asset.Id);
            Assert.AreEqual("Corrupted Chest Endpoint 1x1", asset.DisplayName);
            Assert.IsFalse(asset.EnemySpawns.Any());
            Assert.IsTrue(asset.ItemSpawns.Any(spawn => spawn.kind == RoomDesignerMarkerKinds.CorruptedChestSpawn));
            Assert.IsTrue(asset.Decor.Any(decor => decor.kind == RoomDesignerMarkerKinds.DecorCrystalCluster));
        }

        [Test]
        public void LiveReportPassesAllRewardChestRiskChecks()
        {
            var report = Milestone130RewardChestRiskPassAssetGenerator.BuildReport();

            Assert.IsTrue(report.passed, string.Join("\n", report.failures ?? new string[0]));
            Assert.Greater(report.totalChecks, 12);
            Assert.AreEqual(report.totalChecks, report.passedChecks);
            CollectionAssert.Contains(report.evidencePaths, Milestone130RewardChestRiskPassAssetGenerator.DocsPath);
            CollectionAssert.Contains(report.evidencePaths, Milestone130RewardChestRiskPassAssetGenerator.M129ReportPath);
        }

        [Test]
        public void GeneratedReportsArePresentPassingAndUseM130LockId()
        {
            Assert.IsTrue(File.Exists(Milestone130RewardChestRiskPassAssetGenerator.ReportMarkdownPath));
            Assert.IsTrue(File.Exists(Milestone130RewardChestRiskPassAssetGenerator.ReportJsonPath));

            var markdown = File.ReadAllText(Milestone130RewardChestRiskPassAssetGenerator.ReportMarkdownPath);
            StringAssert.Contains("# M130 Reward + Chest Risk Pass Report", markdown);
            StringAssert.Contains("- Result: PASSED", markdown);
            StringAssert.Contains(Milestone130RewardChestRiskPassAssetGenerator.LockId, markdown);
            StringAssert.Contains("ordinary rooms stay sparse", markdown);
            StringAssert.Contains("Corrupted Chest rooms are rare terminal leaves", markdown);

            var json = JsonUtility.FromJson<Milestone130RewardChestRiskPassReport>(
                File.ReadAllText(Milestone130RewardChestRiskPassAssetGenerator.ReportJsonPath));
            Assert.IsNotNull(json);
            Assert.AreEqual(Milestone130RewardChestRiskPassAssetGenerator.LockId, json.lockId);
            Assert.IsTrue(json.passed);
            Assert.IsTrue((json.failures ?? new string[0]).Length == 0);
        }

        [Test]
        public void ValidatorReportsGeneratedStateValid()
        {
            Assert.IsTrue(Milestone130RewardChestRiskPassValidator.Validate(exitOnFailure: false));
        }

        private static BranchFloorGraph FirstGraphWithCorruptedRoom(out BranchRoomState corrupted)
        {
            var content = CreateContent(out var settings);
            for (var seed = 13000; seed < 13200; seed++)
            {
                var graph = BranchGenerator.CreateSeededBranchFeatures(content, settings, seed, RoomBiomeIds.HollowThreshold, enableCorruptedChestLeaf: true);
                corrupted = graph.Rooms.FirstOrDefault(room => room.Role == BranchRoomRole.CorruptedChest);
                if (corrupted != null)
                {
                    return graph;
                }
            }

            Assert.Fail("Expected deterministic seed scan to find a corrupted chest room.");
            corrupted = null;
            return null;
        }

        private static BranchSessionContent CreateContent(out BranchGenerationSettingsDefinition settings)
        {
            var catalog = CreateM130Catalog();
            settings = AssetDatabase.LoadAssetAtPath<BranchGenerationSettingsDefinition>(Milestone15AssetGenerator.SettingsPath);
            var sample = HollowRuntimeV2Importer.Import(File.ReadAllText(SamplePath));
            var content = BranchSessionContent.Create(sample, catalog, settings.DefaultSeed, out var error);
            Assert.IsTrue(string.IsNullOrWhiteSpace(error), error);
            return content;
        }

        private static BranchRoomTemplateCatalogDefinition CreateM130Catalog()
        {
            var source = AssetDatabase.LoadAssetAtPath<BranchRoomTemplateCatalogDefinition>(Milestone14AssetGenerator.CatalogPath);
            Assert.IsNotNull(source);
            var corruptedText = new TextAsset(File.ReadAllText(Milestone130RewardChestRiskPassAssetGenerator.CorruptedChestEndpointRoomPath))
            {
                name = BranchGenerator.CorruptedChestRoomAssetId
            };
            var catalog = ScriptableObject.CreateInstance<BranchRoomTemplateCatalogDefinition>();
            catalog.Configure(
                source.Single1x1,
                source.Wide2x1,
                source.Tall1x2,
                source.Block2x2,
                source.L3Cell,
                source.DefaultSeed,
                source.AdditionalTemplates,
                corruptedText);
            return catalog;
        }

        private static GameObject CreateCorruptedChestHarness(
            out BranchSessionController branch,
            out RunChestStateSave chestState,
            out CombatantHealth playerHealth)
        {
            var root = new GameObject("M130CorruptedChestHarness");
            var playerObject = new GameObject("Player");
            playerObject.transform.SetParent(root.transform, false);
            var player = playerObject.AddComponent<PlaceholderPlayerController>();
            player.ConfigureDefault();
            playerHealth = playerObject.AddComponent<CombatantHealth>();
            playerHealth.Configure(6);

            branch = root.AddComponent<BranchSessionController>();
            SetPrivateField(branch, "playerController", player);
            SetPrivateField(branch, "runEconomy", new RunEconomy());
            SetPrivateField(branch, "playerRunStats", new PlayerRunStats());
            SetPrivateField(branch, "playerRunBuild", new PlayerRunBuild());
            SetPrivateField(branch, "proceduralRewardPlan", ProceduralRewardPlan.Empty);
            SetPrivateField(branch, "encounterPlan", EncounterPlan.Empty);

            var graph = new BranchFloorGraph(BranchGenerator.BranchFeaturesId, 13001);
            var origin = CreateRoom("origin", BranchRoomRole.Origin, Vector2Int.zero);
            var corrupted = CreateRoom("corrupted_01", BranchRoomRole.CorruptedChest, new Vector2Int(1, 0));
            corrupted.MarkVisited();
            corrupted.MarkCleared();
            corrupted.MarkRewardPending();
            graph.AddRoom(origin);
            graph.AddRoom(corrupted);
            graph.AddBidirectionalConnection(origin.Id, corrupted.Id, "east", "west");
            SetState(branch, BranchSessionState.Create(graph, corrupted.Id));

            chestState = new RunChestStateSave
            {
                roomId = corrupted.Id.Value,
                chestId = "corrupted_01_corrupted_treasure_chest",
                kind = ChestKind.Corrupted.ToString(),
                state = ChestState.Unopened.ToString(),
                contentRewardId = "blade_lesson",
                contentDisplayName = "Blade Lesson",
                contentRewardKind = RewardKind.PassiveCard.ToString(),
                contentCoins = 8,
                contentEffects = new List<RunRewardEffectSaveState>
                {
                    new RewardEffect(RewardEffectKind.MeleeDamageBonus, intValue: 1).ToSaveState()
                }
            };
            GetPrivateField<List<RunChestStateSave>>(branch, "roomChestStates").Add(chestState);

            var chestObject = new GameObject("Chest_Corrupted");
            chestObject.transform.SetParent(root.transform, false);
            var chest = chestObject.AddComponent<RoomChestController>();
            chest.Configure(chestState.roomId, chestState.chestId, ChestKind.Corrupted, ChestState.Unopened);
            GetPrivateField<List<RoomChestController>>(branch, "currentRoomChests").Add(chest);
            return root;
        }

        private static BranchRoomState CreateRoom(string id, BranchRoomRole role, Vector2Int cell)
        {
            return new BranchRoomState(
                new BranchRoomId(id),
                cell,
                new BranchRoomInstanceId(id),
                "test_room",
                new RoomInstanceFootprint(cell, new[] { cell }, new Vector2Int(13, 7)),
                role);
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, fieldName);
            field.SetValue(target, value);
        }

        private static T GetPrivateField<T>(object target, string fieldName)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, fieldName);
            return (T)field.GetValue(target);
        }

        private static void SetState(BranchSessionController branch, BranchSessionState state)
        {
            var field = typeof(BranchSessionController).GetField("<State>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, nameof(BranchSessionController.State));
            field.SetValue(branch, state);
        }

        private static object InvokePrivate(object target, string methodName)
        {
            var method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(method, methodName);
            return method.Invoke(target, null);
        }

        private static object InvokePrivate(object target, string methodName, params object[] parameters)
        {
            var method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(method, methodName);
            return method.Invoke(target, parameters);
        }
    }
}
