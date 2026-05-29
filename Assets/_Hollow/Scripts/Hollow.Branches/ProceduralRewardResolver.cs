using System.Collections.Generic;
using System.Linq;
using Hollow.Rewards;

namespace Hollow.Branches
{
    public static class ProceduralRewardResolver
    {
        public const string PreBetaStandardPoolId = "m51_standard_room_rewards";
        public const string RewardRoomCacheRollId = "m134_reward_room_cache";

        private static readonly (string rewardId, string displayName, RewardKind rewardKind)[] RewardPool =
        {
            ("stone_heart", "Stone Heart", RewardKind.PassiveItem),
            ("quick_draw", "Quick Draw", RewardKind.Card),
            ("fleet_step", "Fleet Step", RewardKind.PassiveItem),
            ("ember_charm", "Ember Charm", RewardKind.PassiveItem),
            ("soul_cache", "Soul Cache", RewardKind.Currency),
            ("heavy_spark", "Heavy Spark", RewardKind.PassiveItem),
            ("swift_charge", "Swift Charge", RewardKind.Card)
        };

        public static ProceduralRewardPlan CreatePlan(BranchFloorGraph graph)
        {
            if (graph == null)
            {
                return ProceduralRewardPlan.Empty;
            }

            var rewards = new List<RewardGrant>();
            var rewardIndex = 0;
            foreach (var room in graph.Rooms
                         .Where(room => room.Id != BranchRoomId.Origin)
                         .OrderBy(room => room.Id.Value))
            {
                if (room.Role == BranchRoomRole.Boss)
                {
                    rewards.Add(new RewardGrant(room.Id.Value, "boss_sigil", "Boss Sigil", RewardKind.PassiveItem, 25));
                    continue;
                }

                if (room.Role is BranchRoomRole.Treasure or BranchRoomRole.Secret)
                {
                    rewards.Add(new RewardGrant(room.Id.Value, "treasure_cache", "Treasure Cache", RewardKind.Currency, 15));
                    continue;
                }

                if (room.Role == BranchRoomRole.CorruptedChest)
                {
                    rewards.Add(CorruptedChestGrant(room.Id.Value));
                    continue;
                }

                if (room.Role == BranchRoomRole.Wave)
                {
                    rewards.Add(GoldenChestGrant(room.Id.Value));
                    continue;
                }

                if (room.Role == BranchRoomRole.SpecialEncounter &&
                    SpecialEncounterResolver.KindForRoomAssetId(room.RuntimeRoomAssetId) == SpecialEncounterKind.Escapist)
                {
                    rewards.Add(GoldenChestGrant(room.Id.Value));
                    continue;
                }

                if (room.Role == BranchRoomRole.SpecialEncounter)
                {
                    rewards.Add(new RewardGrant(room.Id.Value, string.Empty, string.Empty, RewardKind.Currency, 0, 0, System.Array.Empty<RewardEffect>()));
                    continue;
                }

                var reward = RewardPool[rewardIndex % RewardPool.Length];
                rewards.Add(new RewardGrant(room.Id.Value, reward.rewardId, reward.displayName, reward.rewardKind, 10));
                rewardIndex++;
            }

            return new ProceduralRewardPlan(rewards);
        }

        public static ProceduralRewardPlan CreateSeededPlan(
            BranchFloorGraph graph,
            RewardPoolDefinition standardRoomPool,
            RewardPoolDefinition treasureRoomPool,
            RewardPoolDefinition bossRoomPool,
            RewardPoolDefinition weaponRewardPool = null)
        {
            if (graph == null)
            {
                return ProceduralRewardPlan.Empty;
            }

            var rewards = new List<RewardGrant>();
            foreach (var room in graph.Rooms
                         .Where(room => room.Id != BranchRoomId.Origin)
                         .OrderBy(room => room.Id.Value))
            {
                rewards.Add(RollRewardForRoom(graph, room, standardRoomPool, treasureRoomPool, bossRoomPool, weaponRewardPool));
            }

            return new ProceduralRewardPlan(rewards);
        }

        public static RewardGrant Resolve(string roomId, ProceduralRewardPlan plan)
        {
            return plan != null && plan.TryResolve(roomId, out var grant)
                ? grant
                : RewardResolver.Resolve(roomId);
        }

        private static RewardGrant RollRewardForRoom(
            BranchFloorGraph graph,
            BranchRoomState room,
            RewardPoolDefinition standardRoomPool,
            RewardPoolDefinition treasureRoomPool,
            RewardPoolDefinition bossRoomPool,
            RewardPoolDefinition weaponRewardPool)
        {
            if (room.Role == BranchRoomRole.Boss &&
                weaponRewardPool != null &&
                StableHash($"{graph.BranchId}|{graph.Seed}|{room.Id.Value}|boss_weapon") % 4 == 0 &&
                weaponRewardPool.TryRoll(room.Id.Value, graph.BranchId, graph.Seed, out var weaponGrant))
            {
                return weaponGrant;
            }

            if (IsPreBetaBalanceActive(standardRoomPool) && room.Role == BranchRoomRole.Reward)
            {
                return RollM134RewardRoomCacheReward(room.Id.Value, graph.BranchId, graph.Seed);
            }

            if (IsPreBetaBalanceActive(standardRoomPool) && IsStandardRewardRoom(room))
            {
                return RollPreBetaStandardReward(room.Id.Value, graph.BranchId, graph.Seed);
            }

            if (room.Role == BranchRoomRole.CorruptedChest)
            {
                return CorruptedChestGrant(room.Id.Value);
            }

            if (room.Role == BranchRoomRole.Wave)
            {
                return GoldenChestGrant(room.Id.Value);
            }

            if (room.Role == BranchRoomRole.SpecialEncounter)
            {
                return SpecialEncounterResolver.KindForRoomAssetId(room.RuntimeRoomAssetId) == SpecialEncounterKind.Escapist
                    ? GoldenChestGrant(room.Id.Value)
                    : new RewardGrant(room.Id.Value, string.Empty, string.Empty, RewardKind.Currency, 0, 0, System.Array.Empty<RewardEffect>());
            }

            var pool = room.Role switch
            {
                BranchRoomRole.Boss => bossRoomPool,
                BranchRoomRole.Treasure => treasureRoomPool,
                BranchRoomRole.Secret => treasureRoomPool,
                BranchRoomRole.CorruptedChest => null,
                BranchRoomRole.Wave => null,
                BranchRoomRole.SpecialEncounter => null,
                _ => standardRoomPool
            };

            if (pool != null && pool.TryRoll(room.Id.Value, graph.BranchId, graph.Seed, out var grant))
            {
                return grant;
            }

            return room.Role switch
            {
                BranchRoomRole.Boss => new RewardGrant(room.Id.Value, "boss_sigil", "Boss Sigil", RewardKind.PassiveItem, 25),
                BranchRoomRole.Treasure or BranchRoomRole.Secret => new RewardGrant(room.Id.Value, "treasure_cache", "Treasure Cache", RewardKind.Currency, 15),
                BranchRoomRole.CorruptedChest => CorruptedChestGrant(room.Id.Value),
                BranchRoomRole.Wave => GoldenChestGrant(room.Id.Value),
                BranchRoomRole.SpecialEncounter => SpecialEncounterResolver.KindForRoomAssetId(room.RuntimeRoomAssetId) == SpecialEncounterKind.Escapist
                    ? GoldenChestGrant(room.Id.Value)
                    : new RewardGrant(room.Id.Value, string.Empty, string.Empty, RewardKind.Currency, 0, 0, System.Array.Empty<RewardEffect>()),
                _ => IsPreBetaBalanceActive(standardRoomPool)
                    ? RollPreBetaStandardReward(room.Id.Value, graph.BranchId, graph.Seed)
                    : FallbackStandardReward(room.Id.Value, graph.BranchId, graph.Seed)
            };
        }

        private static bool IsPreBetaBalanceActive(RewardPoolDefinition standardRoomPool)
        {
            return standardRoomPool != null && standardRoomPool.PoolId == PreBetaStandardPoolId;
        }

        private static bool IsStandardRewardRoom(BranchRoomState room)
        {
            return room.Role == BranchRoomRole.Combat;
        }

        private static RewardGrant CorruptedChestGrant(string roomId)
        {
            return new RewardGrant(
                roomId,
                ChestRewardResolver.CorruptedChestRewardId,
                "Corrupted Chest",
                RewardKind.Currency,
                0,
                0,
                System.Array.Empty<RewardEffect>());
        }

        private static RewardGrant GoldenChestGrant(string roomId)
        {
            return new RewardGrant(
                roomId,
                ChestRewardResolver.GoldenChestRewardId,
                "Golden Chest",
                RewardKind.Currency,
                0,
                0,
                System.Array.Empty<RewardEffect>());
        }

        private static RewardGrant RollPreBetaStandardReward(string roomId, string branchId, int seed)
        {
            var roll = StableHash($"{branchId}|{seed}|{roomId}|m52_chests_coin_drops") % 100;
            if (roll < 2)
            {
                return new RewardGrant(roomId, ChestRewardResolver.GoldenChestRewardId, "Golden Chest", RewardKind.Currency, 0, 0, System.Array.Empty<RewardEffect>());
            }

            if (roll < 14)
            {
                return new RewardGrant(roomId, ChestRewardResolver.NormalChestRewardId, "Normal Chest", RewardKind.Currency, 0, 0, System.Array.Empty<RewardEffect>());
            }

            if (roll < 52)
            {
                var coins = 5 + StableHash($"{branchId}|{seed}|{roomId}|m52_loose_coin_amount") % 4;
                return new RewardGrant(roomId, ChestRewardResolver.SmallCoinPouchRewardId, "Loose Coins", RewardKind.Currency, 0, coins, System.Array.Empty<RewardEffect>());
            }

            if (roll < 76)
            {
                return ChestRewardResolver.HpRefillGrant(roomId);
            }

            return new RewardGrant(roomId, string.Empty, string.Empty, RewardKind.Currency, 0, 0, System.Array.Empty<RewardEffect>());
        }

        public static RewardGrant RollM134RewardRoomCacheReward(string roomId, string branchId, int seed)
        {
            var roll = StableHash($"{branchId}|{seed}|{roomId}|{RewardRoomCacheRollId}") % 100;
            if (roll < 2)
            {
                return new RewardGrant(roomId, ChestRewardResolver.GoldenChestRewardId, "Golden Chest", RewardKind.Currency, 0, 0, System.Array.Empty<RewardEffect>());
            }

            if (roll < 32)
            {
                return new RewardGrant(roomId, ChestRewardResolver.NormalChestRewardId, "Normal Chest", RewardKind.Currency, 0, 0, System.Array.Empty<RewardEffect>());
            }

            if (roll < 66)
            {
                var coins = 5 + StableHash($"{branchId}|{seed}|{roomId}|m134_reward_room_loose_coin_amount") % 4;
                return new RewardGrant(roomId, ChestRewardResolver.SmallCoinPouchRewardId, "Loose Coins", RewardKind.Currency, 0, coins, System.Array.Empty<RewardEffect>());
            }

            if (roll < 90)
            {
                return ChestRewardResolver.HpRefillGrant(roomId);
            }

            return new RewardGrant(roomId, string.Empty, string.Empty, RewardKind.Currency, 0, 0, System.Array.Empty<RewardEffect>());
        }

        private static RewardGrant FallbackStandardReward(string roomId, string branchId, int seed)
        {
            var index = StableHash($"{branchId}|{seed}|{roomId}|standard") % RewardPool.Length;
            var reward = RewardPool[index];
            var souls = reward.rewardId == "soul_cache" ? 15 : 10;
            return new RewardGrant(roomId, reward.rewardId, reward.displayName, reward.rewardKind, souls);
        }

        private static int StableHash(string value)
        {
            unchecked
            {
                var hash = 2166136261u;
                foreach (var character in value ?? string.Empty)
                {
                    hash ^= (uint)character;
                    hash *= 16777619u;
                }

                return (int)(hash & 0x7fffffff);
            }
        }
    }
}
