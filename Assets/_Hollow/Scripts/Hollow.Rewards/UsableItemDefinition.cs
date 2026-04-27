using System.Collections.Generic;
using UnityEngine;

namespace Hollow.Rewards
{
    [CreateAssetMenu(menuName = "Hollow/Rewards/Usable Item Definition")]
    public sealed class UsableItemDefinition : ScriptableObject
    {
        [SerializeField] private string itemId;
        [SerializeField] private string displayName;
        [SerializeField] private RewardKind rewardKind = RewardKind.ActiveItem;
        [SerializeField] private RewardRarity rarity = RewardRarity.Common;
        [SerializeField] private int maxCharges;
        [SerializeField] private bool consumeOnUse;
        [SerializeField] private RewardEffect[] effects = System.Array.Empty<RewardEffect>();

        public string ItemId => itemId;

        public string DisplayName => displayName;

        public RewardKind RewardKind => rewardKind;

        public RewardRarity Rarity => rarity;

        public int MaxCharges => Mathf.Max(0, maxCharges);

        public bool ConsumeOnUse => consumeOnUse;

        public IReadOnlyList<RewardEffect> Effects => effects;

        public void Configure(
            string nextItemId,
            string nextDisplayName,
            RewardKind nextRewardKind,
            RewardRarity nextRarity,
            int nextMaxCharges,
            bool nextConsumeOnUse,
            IEnumerable<RewardEffect> nextEffects)
        {
            itemId = nextItemId ?? string.Empty;
            displayName = nextDisplayName ?? string.Empty;
            rewardKind = nextRewardKind;
            rarity = nextRarity;
            maxCharges = Mathf.Max(0, nextMaxCharges);
            consumeOnUse = nextConsumeOnUse;
            effects = RewardEffect.Clean(nextEffects);
        }
    }
}
