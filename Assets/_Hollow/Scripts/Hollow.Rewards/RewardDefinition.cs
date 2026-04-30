using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Hollow.Data.Definitions;

namespace Hollow.Rewards
{
    [CreateAssetMenu(menuName = "Hollow/Rewards/Reward Definition")]
    public sealed class RewardDefinition : ScriptableObject
    {
        [SerializeField] private string rewardId;
        [SerializeField] private string displayName;
        [SerializeField] private RewardKind rewardKind;
        [SerializeField] private RewardRarity rarity;
        [SerializeField] private int souls;
        [SerializeField] private int coins;
        [SerializeField] private int maxStacks = 1;
        [SerializeField] private BuildTag[] tags = System.Array.Empty<BuildTag>();
        [SerializeField] private RewardEffect[] effects = System.Array.Empty<RewardEffect>();

        public string RewardId => rewardId;

        public string DisplayName => displayName;

        public RewardKind RewardKind => rewardKind;

        public RewardRarity Rarity => rarity;

        public int Souls => souls;

        public int Coins => coins;

        public int MaxStacks => Mathf.Max(1, maxStacks);

        public IReadOnlyList<BuildTag> Tags => tags;

        public IReadOnlyList<RewardEffect> Effects => effects;

        public void Configure(string nextRewardId, string nextDisplayName, RewardKind nextRewardKind, int nextSouls)
        {
            Configure(
                nextRewardId,
                nextDisplayName,
                nextRewardKind,
                RewardRarity.Common,
                nextSouls,
                0,
                RewardEffect.DefaultsForRewardId(nextRewardId));
        }

        public void Configure(
            string nextRewardId,
            string nextDisplayName,
            RewardKind nextRewardKind,
            RewardRarity nextRarity,
            int nextSouls,
            IEnumerable<RewardEffect> nextEffects)
        {
            Configure(nextRewardId, nextDisplayName, nextRewardKind, nextRarity, nextSouls, 0, nextEffects);
        }

        public void Configure(
            string nextRewardId,
            string nextDisplayName,
            RewardKind nextRewardKind,
            RewardRarity nextRarity,
            int nextSouls,
            int nextCoins,
            IEnumerable<RewardEffect> nextEffects)
        {
            Configure(nextRewardId, nextDisplayName, nextRewardKind, nextRarity, nextSouls, nextCoins, nextEffects, null);
        }

        public void Configure(
            string nextRewardId,
            string nextDisplayName,
            RewardKind nextRewardKind,
            RewardRarity nextRarity,
            int nextSouls,
            int nextCoins,
            IEnumerable<RewardEffect> nextEffects,
            IEnumerable<BuildTag> nextTags)
        {
            Configure(nextRewardId, nextDisplayName, nextRewardKind, nextRarity, nextSouls, nextCoins, nextEffects, nextTags, 1);
        }

        public void Configure(
            string nextRewardId,
            string nextDisplayName,
            RewardKind nextRewardKind,
            RewardRarity nextRarity,
            int nextSouls,
            int nextCoins,
            IEnumerable<RewardEffect> nextEffects,
            IEnumerable<BuildTag> nextTags,
            int nextMaxStacks)
        {
            rewardId = nextRewardId;
            displayName = nextDisplayName;
            rewardKind = nextRewardKind;
            rarity = nextRarity;
            souls = nextSouls;
            coins = Mathf.Max(0, nextCoins);
            maxStacks = Mathf.Max(1, nextMaxStacks);
            effects = RewardEffect.Clean(nextEffects);
            tags = (nextTags ?? Enumerable.Empty<BuildTag>())
                .Where(tag => tag != BuildTag.None)
                .Distinct()
                .ToArray();
        }

        public RewardGrant ToGrant(string roomId)
        {
            return new RewardGrant(roomId, rewardId, displayName, rewardKind, souls, coins, effects, MaxStacks);
        }
    }
}
