using System;

namespace Hollow.Rewards
{
    public static class RewardResolver
    {
        public static RewardGrant Resolve(string roomId)
        {
            var normalized = (roomId ?? string.Empty).Trim().ToLowerInvariant();
            return normalized switch
            {
                "north" => new RewardGrant(normalized, "stone_heart", "Stone Heart", RewardKind.PassiveItem, 10),
                "south" => new RewardGrant(normalized, "quick_draw", "Quick Draw", RewardKind.Card, 10),
                "east" => new RewardGrant(normalized, "fleet_step", "Fleet Step", RewardKind.PassiveItem, 10),
                "west" => new RewardGrant(normalized, "ember_charm", "Ember Charm", RewardKind.PassiveItem, 10),
                _ => new RewardGrant(normalized, string.Empty, string.Empty, RewardKind.PassiveItem, 0)
            };
        }

        public static RewardGrant ResolveOrThrow(string roomId)
        {
            var grant = Resolve(roomId);
            if (grant.IsEmpty)
            {
                throw new InvalidOperationException($"No deterministic M7 reward is defined for room '{roomId}'.");
            }

            return grant;
        }
    }
}
