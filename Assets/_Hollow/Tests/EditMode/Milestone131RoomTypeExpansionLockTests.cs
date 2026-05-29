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

namespace Hollow.Tests.EditMode
{
    public sealed class Milestone131RoomTypeExpansionLockTests
    {
        private const string SamplePath = "Assets/_Hollow/Data/Rooms/Templates/combat_single_sample.hollowruntime.json";

        [Test]
        public void NormalBranchesGetExactlyOneOptionalWaveLeaf()
        {
            var content = CreateContent(out var settings);
            var profile = AssetDatabase.LoadAssetAtPath<EncounterDirectorProfileDefinition>(Milestone46AssetGenerator.DirectorProfilePath);

            for (var seed = 13100; seed < 13120; seed++)
            {
                var m20Graph = BranchGenerator.CreateSeededBranchFeatures(
                    content,
                    settings,
                    seed,
                    RoomBiomeIds.HollowThreshold,
                    enableCorruptedChestLeaf: true,
                    enableWaveRoomLeaf: true);
                AssertWaveLeafPolicy(m20Graph, content);

                var m46Graph = BranchGenerator.CreateDirectedEncounterBranch(
                    content,
                    settings,
                    profile,
                    worldIndex: 1,
                    seed: seed,
                    bossRoomAssetId: string.Empty,
                    biomeId: RoomBiomeIds.HollowThreshold,
                    enableCorruptedChestLeaf: true,
                    enableWaveRoomLeaf: true);
                AssertWaveLeafPolicy(m46Graph, content);
            }
        }

        [Test]
        public void WaveRoomsPayGoldenChestAndSplitIntoThreeWaves()
        {
            var content = CreateContent(out var settings);
            var graph = BranchGenerator.CreateSeededBranchFeatures(
                content,
                settings,
                13131,
                RoomBiomeIds.HollowThreshold,
                enableCorruptedChestLeaf: true,
                enableWaveRoomLeaf: true);
            var wave = graph.Rooms.Single(room => room.Role == BranchRoomRole.Wave);

            var standard = ScriptableObject.CreateInstance<RewardPoolDefinition>();
            standard.Configure(ProceduralRewardResolver.PreBetaStandardPoolId, System.Array.Empty<RewardDefinition>());
            var rewards = ProceduralRewardResolver.CreateSeededPlan(graph, standard, null, null);
            Assert.IsTrue(rewards.TryResolve(wave.Id.Value, out var grant));
            Assert.AreEqual(ChestRewardResolver.GoldenChestRewardId, grant.RewardId);
            Assert.AreEqual(ChestKind.Golden, ChestRewardResolver.KindForGrant(grant));

            var plan = RoomWaveEncounterPlan.Create("m131_wave_test", new[] { "spawnEnemyNormal", "spawnEnemyFast", "spawnEnemyFlying" });
            Assert.AreEqual(3, plan.TotalWaves);
            CollectionAssert.AreEqual(new[] { 2, 3, 4 }, plan.Waves.Select(waveSpawns => waveSpawns.Count).ToArray());
            Assert.AreEqual("Wave 1/3", plan.StatusTextForWave(0));
            Assert.AreEqual("Wave 2/3", plan.StatusTextForWave(1));
            Assert.AreEqual("Wave 3/3", plan.StatusTextForWave(2));

            Assert.IsTrue(plan.TryCreateContextForWave(2, new RoomCombatEncounterContext("m131_wave", new[] { "spawnEnemyNormal" }, 1, 2, 3), out var waveContext));
            Assert.AreEqual(4, waveContext.EnemySpawnKinds.Count);
            Assert.AreEqual("m131_wave_test:wave_03", waveContext.EncounterId);

            var model = new CombatHudModel(
                6,
                6,
                4,
                RoomObjectiveState.InCombat,
                "Developer Sample",
                "Normal:4",
                "Shots:0",
                null,
                waveContext,
                null,
                "Wave 3/3");
            Assert.IsTrue(model.HasStatusOverride);
            Assert.AreEqual("Wave 3/3", model.StatusText);
        }

        [Test]
        public void WaveEndpointImportsAndCanInheritBranchBiome()
        {
            var json = File.ReadAllText(Milestone131RoomTypeExpansionLockAssetGenerator.WaveRoomEndpointRoomPath);
            Assert.IsTrue(HollowRuntimeV2Importer.TryImport(json, out var asset, out var error), error);
            Assert.AreEqual(BranchGenerator.WaveRoomAssetId, asset.Id);
            Assert.AreEqual("Wave Room Endpoint 1x1", asset.DisplayName);
            Assert.GreaterOrEqual(asset.EnemySpawns.Count, 4);
            Assert.IsTrue(asset.ItemSpawns.Any(spawn => spawn.kind == RoomDesignerMarkerKinds.GoldenChestSpawn));
            Assert.IsTrue(RuntimeRoomValidator.Validate(asset).IsValid);

            var content = CreateContent(out _);
            Assert.IsTrue(content.TryGetRoomAsset(BranchGenerator.WaveRoomAssetId, RoomBiomeIds.VerdantRuins, out var verdantWaveAsset));
            Assert.AreEqual(RoomBiomeIds.VerdantRuins, verdantWaveAsset.BiomeId);
            Assert.AreEqual(BranchGenerator.WaveRoomAssetId, verdantWaveAsset.Id);
        }

        [Test]
        public void LiveReportPassesAllRoomTypeExpansionChecks()
        {
            var report = Milestone131RoomTypeExpansionLockAssetGenerator.BuildReport();

            Assert.IsTrue(report.passed, string.Join("\n", report.failures ?? new string[0]));
            Assert.Greater(report.totalChecks, 10);
            Assert.AreEqual(report.totalChecks, report.passedChecks);
            CollectionAssert.Contains(report.evidencePaths, Milestone131RoomTypeExpansionLockAssetGenerator.DocsPath);
            CollectionAssert.Contains(report.evidencePaths, Milestone131RoomTypeExpansionLockAssetGenerator.M130ReportPath);
        }

        [Test]
        public void GeneratedReportsArePresentPassingAndUseM131LockId()
        {
            Assert.IsTrue(File.Exists(Milestone131RoomTypeExpansionLockAssetGenerator.ReportMarkdownPath));
            Assert.IsTrue(File.Exists(Milestone131RoomTypeExpansionLockAssetGenerator.ReportJsonPath));

            var markdown = File.ReadAllText(Milestone131RoomTypeExpansionLockAssetGenerator.ReportMarkdownPath);
            StringAssert.Contains("# M131 Room Type Expansion Lock Report", markdown);
            StringAssert.Contains("- Result: PASSED", markdown);
            StringAssert.Contains(Milestone131RoomTypeExpansionLockAssetGenerator.LockId, markdown);
            StringAssert.Contains("every normal world-loop branch gets one optional terminal Wave Room leaf", markdown);
            StringAssert.Contains("Golden Chest", markdown);

            var json = JsonUtility.FromJson<Milestone131RoomTypeExpansionLockReport>(
                File.ReadAllText(Milestone131RoomTypeExpansionLockAssetGenerator.ReportJsonPath));
            Assert.IsNotNull(json);
            Assert.AreEqual(Milestone131RoomTypeExpansionLockAssetGenerator.LockId, json.lockId);
            Assert.IsTrue(json.passed);
            Assert.IsTrue((json.failures ?? new string[0]).Length == 0);
        }

        [Test]
        public void ValidatorReportsGeneratedStateValid()
        {
            Assert.IsTrue(Milestone131RoomTypeExpansionLockValidator.Validate(exitOnFailure: false));
        }

        private static void AssertWaveLeafPolicy(BranchFloorGraph graph, BranchSessionContent content)
        {
            Assert.IsTrue(BranchGenerator.ValidateSpecialRoomTopology(graph, out var topologyError), topologyError);
            var waveRooms = graph.Rooms.Where(room => room.Role == BranchRoomRole.Wave).ToArray();
            Assert.AreEqual(1, waveRooms.Length);

            var wave = waveRooms[0];
            Assert.AreEqual(BranchGenerator.WaveRoomAssetId, wave.RuntimeRoomAssetId);
            Assert.IsTrue(content.TryGetRoomAsset(wave.RuntimeRoomAssetId, out var waveAsset));
            Assert.AreEqual(BranchGenerator.WaveRoomAssetId, waveAsset.Id);

            var neighbors = graph.ConnectionsFrom(wave.Id).Select(connection => connection.ToRoomId).Distinct().ToArray();
            Assert.AreEqual(1, neighbors.Length);
            Assert.IsTrue(graph.TryGetRoom(neighbors[0], out var parent));
            Assert.IsFalse(parent.Role is BranchRoomRole.Boss or BranchRoomRole.Secret or BranchRoomRole.Treasure or BranchRoomRole.CorruptedChest);

            var branchFeaturePlan = BranchFeaturePlan.Create(graph);
            Assert.AreNotEqual(wave.Id.Value, branchFeaturePlan.BossKeyRoomId);
            Assert.IsFalse(graph.ConnectionsFrom(wave.Id).Any(connection => connection.LockKind == BranchConnectionLockKind.BossKey));
            Assert.AreEqual(1, graph.Rooms.Count(room => room.Role == BranchRoomRole.Boss));
            Assert.AreEqual(1, graph.Rooms.Count(room => room.Role == BranchRoomRole.Secret));
        }

        private static BranchSessionContent CreateContent(out BranchGenerationSettingsDefinition settings)
        {
            var catalog = CreateM131Catalog();
            settings = AssetDatabase.LoadAssetAtPath<BranchGenerationSettingsDefinition>(Milestone15AssetGenerator.SettingsPath);
            var sample = HollowRuntimeV2Importer.Import(File.ReadAllText(SamplePath));
            var content = BranchSessionContent.Create(sample, catalog, settings.DefaultSeed, out var error);
            Assert.IsTrue(string.IsNullOrWhiteSpace(error), error);
            return content;
        }

        private static BranchRoomTemplateCatalogDefinition CreateM131Catalog()
        {
            var source = AssetDatabase.LoadAssetAtPath<BranchRoomTemplateCatalogDefinition>(Milestone14AssetGenerator.CatalogPath);
            Assert.IsNotNull(source);
            var corruptedText = new TextAsset(File.ReadAllText(Milestone130RewardChestRiskPassAssetGenerator.CorruptedChestEndpointRoomPath))
            {
                name = BranchGenerator.CorruptedChestRoomAssetId
            };
            var waveText = new TextAsset(File.ReadAllText(Milestone131RoomTypeExpansionLockAssetGenerator.WaveRoomEndpointRoomPath))
            {
                name = BranchGenerator.WaveRoomAssetId
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
                corruptedText,
                waveText);
            return catalog;
        }
    }
}
