using UnityEngine;
using System.Collections.Generic;

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
        [SerializeField] private RewardEffect[] effects = System.Array.Empty<RewardEffect>();

        public string RewardId => rewardId;

        public string DisplayName => displayName;

        public RewardKind RewardKind => rewardKind;

        public RewardRarity Rarity => rarity;

        public int Souls => souls;

        public IReadOnlyList<RewardEffect> Effects => effects;

        public void Configure(string nextRewardId, string nextDisplayName, RewardKind nextRewardKind, int nextSouls)
        {
            Configure(
                nextRewardId,
                nextDisplayName,
                nextRewardKind,
                RewardRarity.Common,
                nextSouls,
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
            rewardId = nextRewardId;
            displayName = nextDisplayName;
            rewardKind = nextRewardKind;
            rarity = nextRarity;
            souls = nextSouls;
            effects = RewardEffect.Clean(nextEffects);
        }

        public RewardGrant ToGrant(string roomId)
        {
            return new RewardGrant(roomId, rewardId, displayName, rewardKind, souls, effects);
        }
    }
}
