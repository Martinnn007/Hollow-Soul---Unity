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
            ("ember_charm", "Ember Charm", RewardKind.PassiveItem)
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

                var reward = RewardPool[rewardIndex % RewardPool.Length];
                rewards.Add(new RewardGrant(room.Id.Value, reward.rewardId, reward.displayName, reward.rewardKind, 10));
                rewardIndex++;
            }

            return new ProceduralRewardPlan(rewards);
        }

        public static RewardGrant Resolve(string roomId, ProceduralRewardPlan plan)
        {
            return plan != null && plan.TryResolve(roomId, out var grant)
                ? grant
                : RewardResolver.Resolve(roomId);
        }
    }
}
