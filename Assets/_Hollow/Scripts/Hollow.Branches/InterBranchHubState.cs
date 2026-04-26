using System.Collections.Generic;
using System.Linq;
using Hollow.Persistence;
using Hollow.Rewards;

namespace Hollow.Branches
{
    public sealed class InterBranchHubState
    {
        public InterBranchHubState(bool isActive, IEnumerable<HubShopOffer> shopOffers, IEnumerable<NextBranchChoice> nextBranchChoices)
        {
            IsActive = isActive;
            ShopOffers = (shopOffers ?? Enumerable.Empty<HubShopOffer>()).Where(offer => offer != null).ToArray();
            NextBranchChoices = (nextBranchChoices ?? Enumerable.Empty<NextBranchChoice>()).Where(choice => choice != null).ToArray();
        }

        public static InterBranchHubState Inactive { get; } = new(false, null, null);

        public bool IsActive { get; }

        public IReadOnlyList<HubShopOffer> ShopOffers { get; }

        public IReadOnlyList<NextBranchChoice> NextBranchChoices { get; }

        public HubShopStateSaveState ToSaveState()
        {
            return new HubShopStateSaveState
            {
                isActive = IsActive,
                offers = ShopOffers.Select(offer => offer.ToSaveState()).ToList(),
                nextChoices = NextBranchChoices.Select(choice => choice.ToSaveState()).ToList()
            };
        }

        public static InterBranchHubState FromSaveState(HubShopStateSaveState save, int branchSeed, int branchDepth, RewardPoolDefinition standardPool)
        {
            if (save == null || !save.isActive)
            {
                return Inactive;
            }

            var offers = save.offers?.Select(HubShopOffer.FromSaveState).Where(offer => offer != null).ToArray();
            var choices = save.nextChoices?.Select(NextBranchChoice.FromSaveState).Where(choice => choice != null).ToArray();
            return new InterBranchHubState(
                true,
                offers != null && offers.Length > 0 ? offers : HubShopOffer.CreateSeededOffers(branchSeed, branchDepth, standardPool),
                choices != null && choices.Length > 0 ? choices : CreateChoices(branchSeed, branchDepth));
        }

        public static InterBranchHubState Create(int branchSeed, int branchDepth, RewardPoolDefinition standardPool)
        {
            return new InterBranchHubState(true, HubShopOffer.CreateSeededOffers(branchSeed, branchDepth, standardPool), CreateChoices(branchSeed, branchDepth));
        }

        private static IReadOnlyList<NextBranchChoice> CreateChoices(int branchSeed, int branchDepth)
        {
            return Enumerable.Range(0, 3)
                .Select(index => NextBranchChoice.Create(branchSeed, branchDepth, index))
                .ToArray();
        }
    }
}
