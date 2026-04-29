using System.Collections.Generic;
using System.Linq;
using Hollow.Persistence;
using Hollow.Rewards;

namespace Hollow.Branches
{
    public sealed class InterBranchHubState
    {
        public InterBranchHubState(bool isActive, IEnumerable<HubShopOffer> shopOffers, IEnumerable<NextBranchChoice> nextBranchChoices)
            : this(isActive, shopOffers, nextBranchChoices, 0, 1, 0)
        {
        }

        public InterBranchHubState(
            bool isActive,
            IEnumerable<HubShopOffer> shopOffers,
            IEnumerable<NextBranchChoice> nextBranchChoices,
            int runSeed,
            int worldIndex,
            int shopRefreshIndex)
        {
            IsActive = isActive;
            ShopOffers = (shopOffers ?? Enumerable.Empty<HubShopOffer>()).Where(offer => offer != null).ToArray();
            NextBranchChoices = (nextBranchChoices ?? Enumerable.Empty<NextBranchChoice>()).Where(choice => choice != null).ToArray();
            RunSeed = runSeed;
            WorldIndex = worldIndex <= 0 ? 1 : worldIndex;
            ShopRefreshIndex = shopRefreshIndex < 0 ? 0 : shopRefreshIndex;
        }

        public static InterBranchHubState Inactive { get; } = new(false, null, null);

        public bool IsActive { get; }

        public IReadOnlyList<HubShopOffer> ShopOffers { get; }

        public IReadOnlyList<NextBranchChoice> NextBranchChoices { get; }

        public int RunSeed { get; }

        public int WorldIndex { get; }

        public int ShopRefreshIndex { get; }

        public bool AreAllBranchPortalsDefeated =>
            NextBranchChoices.Where(choice => choice.Kind == HubPortalKind.Branch).All(choice => choice.State == HubBranchPortalState.Defeated) &&
            NextBranchChoices.Any(choice => choice.Kind == HubPortalKind.Branch);

        public HubShopStateSaveState ToSaveState()
        {
            return new HubShopStateSaveState
            {
                isActive = IsActive,
                runSeed = RunSeed,
                worldIndex = WorldIndex,
                shopRefreshIndex = ShopRefreshIndex,
                isNextWorldPortalAvailable = NextBranchChoices.Any(choice => choice.Kind == HubPortalKind.NextWorld),
                isFinalExtractionPortalAvailable = NextBranchChoices.Any(choice => choice.Kind == HubPortalKind.FinalExtraction),
                offers = ShopOffers.Select(offer => offer.ToSaveState()).ToList(),
                nextChoices = NextBranchChoices.Select(choice => choice.ToSaveState()).ToList()
            };
        }

        public static InterBranchHubState FromSaveState(HubShopStateSaveState save, int branchSeed, int branchDepth, RewardPoolDefinition standardPool, RewardPoolDefinition weaponPool = null, RewardPoolDefinition shopRewardPool = null)
        {
            if (save == null || !save.isActive)
            {
                return Inactive;
            }

            var offers = save.offers?.Select(HubShopOffer.FromSaveState).Where(offer => offer != null).ToArray();
            var choices = save.nextChoices?.Select(NextBranchChoice.FromSaveState).Where(choice => choice != null).ToArray();
            var runSeed = save.runSeed == 0 ? branchSeed : save.runSeed;
            var worldIndex = save.worldIndex <= 0 ? 1 : save.worldIndex;
            var refreshIndex = save.shopRefreshIndex < 0 ? 0 : save.shopRefreshIndex;
            return new InterBranchHubState(
                true,
                offers != null && offers.Length > 0 ? offers : HubShopOffer.CreateSeededOffers(RunSeedDeriver.ShopSeed(runSeed, worldIndex, refreshIndex), refreshIndex, standardPool, weaponPool, shopRewardPool),
                choices != null && choices.Length > 0 ? choices : CreateChoices(branchSeed, branchDepth),
                runSeed,
                worldIndex,
                refreshIndex);
        }

        public static InterBranchHubState Create(int branchSeed, int branchDepth, RewardPoolDefinition standardPool)
        {
            return Create(branchSeed, branchDepth, standardPool, null);
        }

        public static InterBranchHubState Create(int branchSeed, int branchDepth, RewardPoolDefinition standardPool, RewardPoolDefinition weaponPool)
        {
            return Create(branchSeed, branchDepth, standardPool, weaponPool, null);
        }

        public static InterBranchHubState Create(int branchSeed, int branchDepth, RewardPoolDefinition standardPool, RewardPoolDefinition weaponPool, RewardPoolDefinition shopRewardPool)
        {
            return new InterBranchHubState(true, HubShopOffer.CreateSeededOffers(branchSeed, branchDepth, standardPool, weaponPool, shopRewardPool), CreateChoices(branchSeed, branchDepth));
        }

        public static InterBranchHubState CreateWorldHub(int runSeed, int worldIndex, int shopRefreshIndex, RewardPoolDefinition standardPool)
        {
            return CreateWorldHub(runSeed, worldIndex, shopRefreshIndex, standardPool, null);
        }

        public static InterBranchHubState CreateWorldHub(int runSeed, int worldIndex, int shopRefreshIndex, RewardPoolDefinition standardPool, RewardPoolDefinition weaponPool)
        {
            return CreateWorldHub(runSeed, worldIndex, shopRefreshIndex, standardPool, weaponPool, null);
        }

        public static InterBranchHubState CreateWorldHub(int runSeed, int worldIndex, int shopRefreshIndex, RewardPoolDefinition standardPool, RewardPoolDefinition weaponPool, RewardPoolDefinition shopRewardPool)
        {
            var refresh = shopRefreshIndex < 0 ? 0 : shopRefreshIndex;
            return new InterBranchHubState(
                true,
                HubShopOffer.CreateSeededOffers(RunSeedDeriver.ShopSeed(runSeed, worldIndex, refresh), refresh, standardPool, weaponPool, shopRewardPool),
                CreateWorldChoices(runSeed, worldIndex, new[] { HubBranchPortalState.Open, HubBranchPortalState.Open, HubBranchPortalState.Open }),
                runSeed,
                worldIndex,
                refresh);
        }

        public InterBranchHubState MarkBranchPortalDefeated(string choiceId, RewardPoolDefinition standardPool)
        {
            return MarkBranchPortalDefeated(choiceId, standardPool, null);
        }

        public InterBranchHubState MarkBranchPortalDefeated(string choiceId, RewardPoolDefinition standardPool, RewardPoolDefinition weaponPool)
        {
            return MarkBranchPortalDefeated(choiceId, standardPool, weaponPool, null);
        }

        public InterBranchHubState MarkBranchPortalDefeated(string choiceId, RewardPoolDefinition standardPool, RewardPoolDefinition weaponPool, RewardPoolDefinition shopRewardPool)
        {
            if (string.IsNullOrWhiteSpace(choiceId))
            {
                return RefreshShop(standardPool, weaponPool, shopRewardPool);
            }

            var branchStates = NextBranchChoices
                .Where(choice => choice.Kind == HubPortalKind.Branch)
                .OrderBy(choice => choice.SlotIndex)
                .Select(choice => choice.ChoiceId == choiceId ? HubBranchPortalState.Defeated : choice.State)
                .ToArray();
            return new InterBranchHubState(
                true,
                HubShopOffer.CreateSeededOffers(RunSeedDeriver.ShopSeed(RunSeed, WorldIndex, ShopRefreshIndex + 1), ShopRefreshIndex + 1, standardPool, weaponPool, shopRewardPool),
                CreateWorldChoices(RunSeed, WorldIndex, branchStates),
                RunSeed,
                WorldIndex,
                ShopRefreshIndex + 1);
        }

        public InterBranchHubState RefreshShop(RewardPoolDefinition standardPool)
        {
            return RefreshShop(standardPool, null);
        }

        public InterBranchHubState RefreshShop(RewardPoolDefinition standardPool, RewardPoolDefinition weaponPool)
        {
            return RefreshShop(standardPool, weaponPool, null);
        }

        public InterBranchHubState RefreshShop(RewardPoolDefinition standardPool, RewardPoolDefinition weaponPool, RewardPoolDefinition shopRewardPool)
        {
            return new InterBranchHubState(
                true,
                HubShopOffer.CreateSeededOffers(RunSeedDeriver.ShopSeed(RunSeed, WorldIndex, ShopRefreshIndex + 1), ShopRefreshIndex + 1, standardPool, weaponPool, shopRewardPool),
                NextBranchChoices,
                RunSeed,
                WorldIndex,
                ShopRefreshIndex + 1);
        }

        private static IReadOnlyList<NextBranchChoice> CreateChoices(int branchSeed, int branchDepth)
        {
            return Enumerable.Range(0, 3)
                .Select(index => NextBranchChoice.Create(branchSeed, branchDepth, index))
                .ToArray();
        }

        private static IReadOnlyList<NextBranchChoice> CreateWorldChoices(int runSeed, int worldIndex, IReadOnlyList<HubBranchPortalState> branchStates)
        {
            var choices = Enumerable.Range(0, 3)
                .Select(index => NextBranchChoice.CreateWorldBranch(
                    runSeed,
                    worldIndex,
                    index,
                    branchStates != null && index < branchStates.Count ? branchStates[index] : HubBranchPortalState.Open))
                .ToList();

            if (choices.All(choice => choice.State == HubBranchPortalState.Defeated))
            {
                choices.Add(worldIndex >= 3
                    ? NextBranchChoice.CreateFinalExtraction(runSeed, worldIndex)
                    : NextBranchChoice.CreateNextWorld(runSeed, worldIndex));
            }

            return choices;
        }
    }
}
