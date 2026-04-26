using UnityEngine;

namespace Hollow.Rewards
{
    [CreateAssetMenu(menuName = "Hollow/Rewards/Reward Definition")]
    public sealed class RewardDefinition : ScriptableObject
    {
        [SerializeField] private string rewardId;
        [SerializeField] private string displayName;
        [SerializeField] private RewardKind rewardKind;
        [SerializeField] private int souls;

        public string RewardId => rewardId;

        public string DisplayName => displayName;

        public RewardKind RewardKind => rewardKind;

        public int Souls => souls;

        public void Configure(string nextRewardId, string nextDisplayName, RewardKind nextRewardKind, int nextSouls)
        {
            rewardId = nextRewardId;
            displayName = nextDisplayName;
            rewardKind = nextRewardKind;
            souls = nextSouls;
        }
    }
}
