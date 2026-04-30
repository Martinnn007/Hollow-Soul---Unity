using System;
using System.Collections.Generic;
using System.Linq;
using Hollow.Persistence;

namespace Hollow.Rewards
{
    [Serializable]
    public sealed class ProceduralRewardPlan
    {
        private readonly Dictionary<string, RewardGrant> rewardsByRoomId = new();

        public ProceduralRewardPlan(IEnumerable<RewardGrant> grants)
        {
            foreach (var grant in grants ?? Array.Empty<RewardGrant>())
            {
                if (!string.IsNullOrWhiteSpace(grant.RoomId))
                {
                    rewardsByRoomId[grant.RoomId] = grant;
                }
            }
        }

        public IReadOnlyCollection<RewardGrant> Rewards => rewardsByRoomId.Values;

        public bool TryResolve(string roomId, out RewardGrant grant)
        {
            return rewardsByRoomId.TryGetValue(roomId ?? string.Empty, out grant);
        }

        public List<RunRewardSaveState> ToSaveState()
        {
            return rewardsByRoomId.Values
                .OrderBy(grant => grant.RoomId)
                .Select(grant => new RunRewardSaveState
                {
                    roomId = grant.RoomId,
                    rewardId = grant.RewardId,
                    displayName = grant.DisplayName,
                    rewardKind = grant.RewardKind.ToString(),
                    souls = grant.Souls,
                    coins = grant.Coins,
                    maxStacks = grant.MaxStacks,
                    effects = grant.Effects?.Select(effect => effect.ToSaveState()).ToList() ?? new List<RunRewardEffectSaveState>()
                })
                .ToList();
        }

        public static ProceduralRewardPlan Empty { get; } = new(Array.Empty<RewardGrant>());

        public static ProceduralRewardPlan FromSaveState(IEnumerable<RunRewardSaveState> rewards)
        {
            var grants = new List<RewardGrant>();
            foreach (var reward in rewards ?? Enumerable.Empty<RunRewardSaveState>())
            {
                Enum.TryParse(reward.rewardKind, out RewardKind rewardKind);
                var effects = reward.effects != null && reward.effects.Count > 0
                    ? reward.effects.Select(RewardEffect.FromSaveState).ToArray()
                    : RewardEffect.DefaultsForRewardId(reward.rewardId);
                grants.Add(new RewardGrant(reward.roomId, reward.rewardId, reward.displayName, rewardKind, reward.souls, reward.coins, effects, reward.maxStacks));
            }

            return new ProceduralRewardPlan(grants);
        }
    }
}
