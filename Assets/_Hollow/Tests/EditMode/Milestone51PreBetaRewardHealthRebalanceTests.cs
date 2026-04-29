using System.Linq;
using Hollow.Branches;
using Hollow.Data.Definitions;
using Hollow.Editor.Generation;
using Hollow.Editor.Validation;
using Hollow.Rewards;
using Hollow.Rooms;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Hollow.Tests.EditMode
{
    public sealed class Milestone51PreBetaRewardHealthRebalanceTests
    {
        [Test]
        public void StandardRoomsResolveOnlySparseEconomySustainOrNothing()
        {
            var standard = CreatePool(ProceduralRewardResolver.PreBetaStandardPoolId);
            var forbidden = new[]
            {
                RewardKind.PassiveItem,
                RewardKind.Card,
                RewardKind.PassiveCard,
                RewardKind.ActiveItem,
                RewardKind.ConsumableCard,
                RewardKind.Weapon,
                RewardKind.Armor
            };

            try
            {
                var sawCoins = false;
                var sawHeal = false;
                var sawChest = false;
                var sawNothing = false;

                for (var seed = 51001; seed < 51160; seed++)
                {
                    var plan = ProceduralRewardResolver.CreateSeededPlan(CreateGraph(seed), standard, null, null);
                    if (!plan.TryResolve("combat_01", out var grant))
                    {
                        sawNothing = true;
                        continue;
                    }

                    Assert.IsFalse(forbidden.Contains(grant.RewardKind), $"Standard room rolled build-changing reward {grant.RewardId}.");
                    sawCoins |= grant.RewardKind == RewardKind.Currency && grant.RewardId == "small_coin_pouch";
                    sawHeal |= grant.RewardKind == RewardKind.Heal;
                    sawChest |= grant.RewardId == "standard_treasure_chest";
                }

                Assert.IsTrue(sawCoins, "Expected sparse standard rewards to include small coins.");
                Assert.IsTrue(sawHeal, "Expected sparse standard rewards to include HP refill.");
                Assert.IsTrue(sawChest, "Expected sparse standard rewards to include the placeholder treasure chest.");
                Assert.IsTrue(sawNothing, "Expected sparse standard rewards to include no-reward outcomes.");
            }
            finally
            {
                Object.DestroyImmediate(standard);
            }
        }

        [Test]
        public void TreasureAndBossRoomsCanStillResolveBuildChangingRewards()
        {
            var standard = CreatePool(ProceduralRewardResolver.PreBetaStandardPoolId);
            var treasureReward = CreateReward("vital_locket", "Vital Locket", RewardKind.PassiveItem, RewardRarity.Common);
            var bossReward = CreateReward("boss_sigil", "Boss Sigil", RewardKind.PassiveItem, RewardRarity.Boss);
            var treasure = CreatePool("m51_test_treasure", treasureReward);
            var boss = CreatePool("m51_test_boss", bossReward);

            try
            {
                var plan = ProceduralRewardResolver.CreateSeededPlan(CreateGraph(51001), standard, treasure, boss);

                Assert.IsTrue(plan.TryResolve("treasure_01", out var treasureGrant));
                Assert.AreEqual(RewardKind.PassiveItem, treasureGrant.RewardKind);
                Assert.IsTrue(plan.TryResolve("boss_01", out var bossGrant));
                Assert.AreEqual(RewardKind.PassiveItem, bossGrant.RewardKind);
            }
            finally
            {
                Object.DestroyImmediate(standard);
                Object.DestroyImmediate(treasureReward);
                Object.DestroyImmediate(bossReward);
                Object.DestroyImmediate(treasure);
                Object.DestroyImmediate(boss);
            }
        }

        [Test]
        public void GeneratedCharactersUsePreBetaHealthBaseline()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<CharacterCatalogDefinition>(Milestone29AssetGenerator.CharacterCatalogPath);
            Assert.IsNotNull(catalog, "Run M51 generation before validating character health.");

            Assert.IsTrue(catalog.TryGetCharacter("balanced", out var balanced));
            Assert.AreEqual(3, balanced.BaseStats.MaxHealth);

            Assert.IsTrue(catalog.TryGetCharacter("heavy", out var heavy));
            Assert.AreEqual(5, heavy.BaseStats.MaxHealth);
        }

        [Test]
        public void Milestone51ValidatorReportsGeneratedStateValid()
        {
            Assert.IsTrue(Milestone51Validator.Validate());
        }

        private static RewardPoolDefinition CreatePool(string poolId, params RewardDefinition[] rewards)
        {
            var pool = ScriptableObject.CreateInstance<RewardPoolDefinition>();
            pool.Configure(poolId, rewards);
            return pool;
        }

        private static RewardDefinition CreateReward(string rewardId, string displayName, RewardKind kind, RewardRarity rarity)
        {
            var reward = ScriptableObject.CreateInstance<RewardDefinition>();
            reward.Configure(rewardId, displayName, kind, rarity, 0, System.Array.Empty<RewardEffect>());
            return reward;
        }

        private static BranchFloorGraph CreateGraph(int seed)
        {
            var graph = new BranchFloorGraph(BranchGenerator.DirectedEncounterBranchId, seed);
            graph.AddRoom(CreateRoom("origin", BranchRoomRole.Origin, Vector2Int.zero));
            graph.AddRoom(CreateRoom("combat_01", BranchRoomRole.Combat, new Vector2Int(1, 0)));
            graph.AddRoom(CreateRoom("treasure_01", BranchRoomRole.Treasure, new Vector2Int(2, 0)));
            graph.AddRoom(CreateRoom("boss_01", BranchRoomRole.Boss, new Vector2Int(3, 0)));
            graph.AddBidirectionalConnection(new BranchRoomId("origin"), new BranchRoomId("combat_01"), "east", "west");
            graph.AddBidirectionalConnection(new BranchRoomId("combat_01"), new BranchRoomId("treasure_01"), "east", "west");
            graph.AddBidirectionalConnection(new BranchRoomId("treasure_01"), new BranchRoomId("boss_01"), "east", "west");
            return graph;
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
    }
}
