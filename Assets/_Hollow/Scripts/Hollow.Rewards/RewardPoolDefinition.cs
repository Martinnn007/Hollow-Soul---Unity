using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Hollow.Rewards
{
    [CreateAssetMenu(menuName = "Hollow/Rewards/Reward Pool")]
    public sealed class RewardPoolDefinition : ScriptableObject
    {
        [SerializeField] private string poolId;
        [SerializeField] private List<RewardDefinition> rewards = new();

        public string PoolId => poolId;

        public IReadOnlyList<RewardDefinition> Rewards => rewards;

        public void Configure(string nextPoolId, IEnumerable<RewardDefinition> nextRewards)
        {
            poolId = nextPoolId ?? string.Empty;
            rewards = (nextRewards ?? Enumerable.Empty<RewardDefinition>())
                .Where(reward => reward != null)
                .Distinct()
                .OrderBy(reward => reward.RewardId)
                .ToList();
        }

        public bool TryRoll(string roomId, string branchId, int seed, out RewardGrant grant)
        {
            grant = default;
            var available = rewards.Where(reward => reward != null && !string.IsNullOrWhiteSpace(reward.RewardId)).ToArray();
            if (available.Length == 0)
            {
                return false;
            }

            var hash = StableHash($"{branchId}|{seed}|{roomId}|{poolId}|{available.Length}");
            var selected = available[hash % available.Length];
            grant = selected.ToGrant(roomId);
            return !grant.IsEmpty;
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
