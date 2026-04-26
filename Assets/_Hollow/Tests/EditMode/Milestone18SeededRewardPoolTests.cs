using System.IO;
using System.Linq;
using Hollow.Branches;
using Hollow.Data.Definitions;
using Hollow.Editor.Generation;
using Hollow.Rewards;
using Hollow.Rooms;
using NUnit.Framework;
using UnityEditor;

namespace Hollow.Tests.EditMode
{
    public sealed class Milestone18SeededRewardPoolTests
    {
        private const string SamplePath = "Assets/_Hollow/Data/Rooms/Templates/combat_single_sample.hollowruntime.json";
        private const string CatalogPath = "Assets/_Hollow/Data/Branches/BranchRoomTemplateCatalog_MacroFixtures.asset";

        [Test]
        public void GeneratedRewardPoolsContainRoleSpecificRewardsAndEffects()
        {
            var standard = LoadStandardPool();
            var treasure = LoadTreasurePool();
            var boss = LoadBossPool();

            Assert.GreaterOrEqual(standard.Rewards.Count, 6);
            Assert.IsTrue(standard.Rewards.Any(reward => reward.RewardId == "heavy_spark"));
            Assert.IsTrue(standard.Rewards.Any(reward => reward.Effects.Any(effect => effect.Kind == RewardEffectKind.ProjectileDamageBonus)));
            Assert.IsTrue(treasure.Rewards.Any(reward => reward.Rarity == RewardRarity.Treasure));
            Assert.IsTrue(boss.Rewards.Any(reward => reward.Rarity == RewardRarity.Boss));
        }

        [Test]
        public void SeededPlanIsDeterministicAndRestoresEffectData()
        {
            var graph = CreateFeatureGraph(BranchGenerator.DefaultSeededMacroSeed);
            var first = ProceduralRewardResolver.CreateSeededPlan(graph, LoadStandardPool(), LoadTreasurePool(), LoadBossPool());
            var second = ProceduralRewardResolver.CreateSeededPlan(graph, LoadStandardPool(), LoadTreasurePool(), LoadBossPool());

            Assert.AreEqual(Signature(first), Signature(second));
            Assert.AreEqual(7, first.Rewards.Count);

            var restored = ProceduralRewardPlan.FromSaveState(first.ToSaveState());
            Assert.AreEqual(Signature(first), Signature(restored));
            Assert.IsTrue(restored.Rewards.Any(reward => reward.Effects.Count > 0));
        }

        [Test]
        public void DifferentSeedsCanProduceDifferentStandardRewards()
        {
            var baseline = Signature(ProceduralRewardResolver.CreateSeededPlan(CreateFeatureGraph(BranchGenerator.DefaultSeededMacroSeed), LoadStandardPool(), LoadTreasurePool(), LoadBossPool()));
            var foundDifferent = false;
            for (var seed = BranchGenerator.DefaultSeededMacroSeed + 1; seed < BranchGenerator.DefaultSeededMacroSeed + 40; seed++)
            {
                var signature = Signature(ProceduralRewardResolver.CreateSeededPlan(CreateFeatureGraph(seed), LoadStandardPool(), LoadTreasurePool(), LoadBossPool()));
                if (signature != baseline)
                {
                    foundDifferent = true;
                    break;
                }
            }

            Assert.IsTrue(foundDifferent, "Expected seeded reward plans to vary across different seeds.");
        }

        [Test]
        public void PlayerRunStatsApplyRewardEffectsWithoutChoiceUi()
        {
            var stats = new PlayerRunStats();
            var grant = new RewardGrant("room_01", "heavy_spark", "Heavy Spark", RewardKind.PassiveItem, 10, RewardEffect.DefaultsForRewardId("heavy_spark"));

            var heal = stats.ApplyReward(grant);

            Assert.AreEqual(0, heal);
            Assert.AreEqual(1, stats.ProjectileDamageBonus);
            Assert.Greater(stats.ShotCooldownMultiplier, 1f);
        }

        private static BranchFloorGraph CreateFeatureGraph(int seed)
        {
            var settings = AssetDatabase.LoadAssetAtPath<BranchGenerationSettingsDefinition>(Milestone15AssetGenerator.SettingsPath);
            var catalog = AssetDatabase.LoadAssetAtPath<BranchRoomTemplateCatalogDefinition>(CatalogPath);
            var content = BranchSessionContent.Create(ImportSampleRoom(), catalog, seed, out var error);
            Assert.IsTrue(string.IsNullOrWhiteSpace(error), error);
            return BranchGenerator.CreateSeededFeatureBranch(content, settings, seed);
        }

        private static ImportedRoomRuntimeAsset ImportSampleRoom()
        {
            return HollowRuntimeV2Importer.Import(File.ReadAllText(SamplePath));
        }

        private static RewardPoolDefinition LoadStandardPool()
        {
            return AssetDatabase.LoadAssetAtPath<RewardPoolDefinition>(Milestone18AssetGenerator.StandardRewardPoolPath);
        }

        private static RewardPoolDefinition LoadTreasurePool()
        {
            return AssetDatabase.LoadAssetAtPath<RewardPoolDefinition>(Milestone18AssetGenerator.TreasureRewardPoolPath);
        }

        private static RewardPoolDefinition LoadBossPool()
        {
            return AssetDatabase.LoadAssetAtPath<RewardPoolDefinition>(Milestone18AssetGenerator.BossRewardPoolPath);
        }

        private static string Signature(ProceduralRewardPlan plan)
        {
            return string.Join("|", plan.Rewards
                .OrderBy(reward => reward.RoomId)
                .Select(reward => $"{reward.RoomId}:{reward.RewardId}:{reward.Souls}:{string.Join(",", reward.Effects.Select(effect => $"{effect.Kind}:{effect.IntValue}:{effect.FloatValue:0.###}"))}"));
        }
    }
}
