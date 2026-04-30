using System.Linq;
using Hollow.Branches;
using Hollow.Data.Definitions;
using Hollow.Editor.Validation;
using Hollow.Rewards;
using Hollow.RoomDesigner;
using NUnit.Framework;
using UnityEngine;

namespace Hollow.Tests.EditMode
{
    public sealed class Milestone52ChestsCoinDropsTests
    {
        [Test]
        public void StandardRoomsCanRollChestsCoinsHealAndNothing()
        {
            var standard = ScriptableObject.CreateInstance<RewardPoolDefinition>();
            standard.Configure(ProceduralRewardResolver.PreBetaStandardPoolId, System.Array.Empty<RewardDefinition>());
            try
            {
                var sawGolden = false;
                var sawNormal = false;
                var sawCoins = false;
                var sawHeal = false;
                var sawNothing = false;
                for (var seed = 52001; seed < 53000; seed++)
                {
                    var plan = ProceduralRewardResolver.CreateSeededPlan(CreateGraph(seed), standard, null, null);
                    Assert.IsTrue(plan.TryResolve("combat_01", out var grant));
                    if (grant.IsEmpty)
                    {
                        sawNothing = true;
                        continue;
                    }

                    sawGolden |= grant.RewardId == ChestRewardResolver.GoldenChestRewardId;
                    sawNormal |= grant.RewardId == ChestRewardResolver.NormalChestRewardId;
                    sawCoins |= grant.RewardId == ChestRewardResolver.SmallCoinPouchRewardId && grant.Coins > 0;
                    sawHeal |= grant.RewardKind == RewardKind.Heal;
                    Assert.IsFalse(grant.RewardKind is RewardKind.PassiveItem or RewardKind.Card or RewardKind.PassiveCard or RewardKind.ActiveItem or RewardKind.ConsumableCard or RewardKind.Weapon or RewardKind.Armor);
                }

                Assert.IsTrue(sawGolden);
                Assert.IsTrue(sawNormal);
                Assert.IsTrue(sawCoins);
                Assert.IsTrue(sawHeal);
                Assert.IsTrue(sawNothing);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(standard);
            }
        }

        [Test]
        public void CoinMixPreservesExactValueWithDenominations()
        {
            Assert.AreEqual(1, CoinDenominationResolver.ValueFor(CoinDenomination.Copper));
            Assert.AreEqual(5, CoinDenominationResolver.ValueFor(CoinDenomination.Silver));
            Assert.AreEqual(10, CoinDenominationResolver.ValueFor(CoinDenomination.Gold));

            for (var value = 1; value <= 30; value++)
            {
                var coins = CoinDenominationResolver.ResolveExactValue(value, 52000 + value);
                Assert.AreEqual(value, coins.Sum(denomination => CoinDenominationResolver.ValueFor(denomination)));
                Assert.LessOrEqual(coins.Count, CoinDenominationResolver.DefaultMaxPhysicalCoins);
            }
        }

        [Test]
        public void ChestContentsRespectNormalAndGoldenRules()
        {
            var normalSawCoins = false;
            var normalSawHeal = false;
            var goldenSawCoins = false;
            var goldenSawHeal = false;
            var goldenSawCard = false;
            for (var seed = 52001; seed < 53000; seed++)
            {
                var normal = ChestRewardResolver.ResolveContents("branch", seed, "combat_01", ChestKind.Normal);
                normalSawCoins |= normal.CoinValue >= 8 && normal.CoinValue <= 14 && normal.RewardGrant.IsEmpty;
                normalSawHeal |= normal.RewardGrant.RewardKind == RewardKind.Heal;

                var golden = ChestRewardResolver.ResolveContents("branch", seed, "combat_01", ChestKind.Golden);
                goldenSawCoins |= golden.CoinValue >= 15 && golden.CoinValue <= 30 && golden.RewardGrant.IsEmpty;
                goldenSawHeal |= golden.CoinValue > 0 && golden.RewardGrant.RewardKind == RewardKind.Heal;
                goldenSawCard |= golden.CoinValue > 0 && golden.RewardGrant.RewardKind is RewardKind.PassiveCard or RewardKind.ConsumableCard;
            }

            Assert.IsTrue(normalSawCoins);
            Assert.IsTrue(normalSawHeal);
            Assert.IsTrue(goldenSawCoins);
            Assert.IsTrue(goldenSawHeal);
            Assert.IsTrue(goldenSawCard);
        }

        [Test]
        public void RoomDesignerChestMarkerRoundtripsAsItemSpawn()
        {
            var project = RoomDesignerProject.CreateDefault(RoomDesignerFootprintPreset.Single1x1, "M52 Test");
            project.markers.Add(new RoomDesignerMarker("spawn_chest_test", RoomDesignerMarkerKinds.ChestSpawn, 1, 0f, 0));
            var asset = RoomDesignerCompiler.Compile(project);

            Assert.IsTrue(asset.ItemSpawns.Any(spawn => spawn.kind == RoomDesignerMarkerKinds.ChestSpawn));
        }

        [Test]
        public void Milestone52ValidatorPasses()
        {
            Assert.IsTrue(Milestone52Validator.Validate());
        }

        private static BranchFloorGraph CreateGraph(int seed)
        {
            var graph = new BranchFloorGraph(BranchGenerator.DirectedEncounterBranchId, seed);
            graph.AddRoom(CreateRoom("origin", BranchRoomRole.Origin, Vector2Int.zero));
            graph.AddRoom(CreateRoom("combat_01", BranchRoomRole.Combat, new Vector2Int(1, 0)));
            graph.AddBidirectionalConnection(new BranchRoomId("origin"), new BranchRoomId("combat_01"), "east", "west");
            return graph;
        }

        private static BranchRoomState CreateRoom(string id, BranchRoomRole role, Vector2Int cell)
        {
            return new BranchRoomState(
                new BranchRoomId(id),
                cell,
                new BranchRoomInstanceId(id),
                "test_room",
                new Hollow.Rooms.RoomInstanceFootprint(cell, new[] { cell }, new Vector2Int(13, 7)),
                role);
        }
    }
}
