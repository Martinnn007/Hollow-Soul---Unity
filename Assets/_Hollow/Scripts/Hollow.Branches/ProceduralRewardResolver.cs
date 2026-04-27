using System.Collections.Generic;
using System.Linq;
using Hollow.Rewards;

namespace Hollow.Branches
{
    public static class ProceduralRewardResolver
    {
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

            var pool = room.Role switch
            {
                BranchRoomRole.Boss => bossRoomPool,
                BranchRoomRole.Treasure => treasureRoomPool,
                BranchRoomRole.Secret => treasureRoomPool,
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
                _ => FallbackStandardReward(room.Id.Value, graph.BranchId, graph.Seed)
            };
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
