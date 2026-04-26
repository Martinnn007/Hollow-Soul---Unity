using System;
using System.Collections.Generic;
using System.Linq;
using Hollow.Persistence;
using Hollow.Rewards;

namespace Hollow.Branches
{
    public sealed class HubShopOffer
    {
        public HubShopOffer(string offerId, string displayName, int price, int healAmount, RewardGrant rewardGrant, bool isPurchased = false)
        {
            OfferId = offerId ?? string.Empty;
            DisplayName = displayName ?? string.Empty;
            Price = Math.Max(0, price);
            HealAmount = Math.Max(0, healAmount);
            RewardGrant = rewardGrant;
            IsPurchased = isPurchased;
        }

        public string OfferId { get; }

        public string DisplayName { get; }

        public int Price { get; }

        public int HealAmount { get; }

        public RewardGrant RewardGrant { get; }

        public bool IsPurchased { get; private set; }

        public bool TryPurchase(RunEconomy economy, out RewardGrant rewardGrant, out int healAmount)
        {
            rewardGrant = RewardGrant;
            healAmount = HealAmount;
            if (IsPurchased || economy == null || !economy.SpendSouls(Price))
            {
                rewardGrant = default;
                healAmount = 0;
                return false;
            }

            IsPurchased = true;
            return true;
        }

        public HubShopOfferSaveState ToSaveState()
        {
            return new HubShopOfferSaveState
            {
                offerId = OfferId,
                displayName = DisplayName,
                price = Price,
                healAmount = HealAmount,
                isPurchased = IsPurchased,
                reward = ToRewardSaveState(RewardGrant)
            };
        }

        public static HubShopOffer FromSaveState(HubShopOfferSaveState save)
        {
            return save == null
                ? null
                : new HubShopOffer(
                    save.offerId,
                    save.displayName,
                    save.price,
                    save.healAmount,
                    FromRewardSaveState(save.reward),
                    save.isPurchased);
        }

        public static IReadOnlyList<HubShopOffer> CreateSeededOffers(int branchSeed, int branchDepth, RewardPoolDefinition standardPool)
        {
            var offers = new List<HubShopOffer>
            {
                new("heal_2", "Heal 2 HP", 8, 2, default)
            };

            for (var index = 0; index < 2; index++)
            {
                var roomId = $"shop_offer_{branchDepth}_{index}";
                var grant = standardPool != null && standardPool.TryRoll(roomId, "m20_hub_shop", branchSeed + branchDepth + index, out var rolled) && rolled.RewardKind != RewardKind.Currency
                    ? new RewardGrant(roomId, rolled.RewardId, rolled.DisplayName, rolled.RewardKind, 0, rolled.Effects)
                    : FallbackReward(roomId, branchSeed, branchDepth, index);
                var price = grant.RewardKind == RewardKind.Card ? 14 : 16;
                offers.Add(new HubShopOffer($"reward_{index}", grant.DisplayName, price, 0, grant));
            }

            return offers;
        }

        private static RewardGrant FallbackReward(string roomId, int branchSeed, int branchDepth, int index)
        {
            var pool = new[]
            {
                ("stone_heart", "Stone Heart", RewardKind.PassiveItem),
                ("quick_draw", "Quick Draw", RewardKind.Card),
                ("fleet_step", "Fleet Step", RewardKind.PassiveItem),
                ("ember_charm", "Ember Charm", RewardKind.PassiveItem)
            };
            var selected = pool[StableHash($"{branchSeed}|{branchDepth}|shop|{index}") % pool.Length];
            return new RewardGrant(roomId, selected.Item1, selected.Item2, selected.Item3, 0);
        }

        private static RunRewardSaveState ToRewardSaveState(RewardGrant grant)
        {
            return new RunRewardSaveState
            {
                roomId = grant.RoomId,
                rewardId = grant.RewardId,
                displayName = grant.DisplayName,
                rewardKind = grant.RewardKind.ToString(),
                souls = grant.Souls,
                effects = grant.Effects?.Select(effect => effect.ToSaveState()).ToList() ?? new List<RunRewardEffectSaveState>()
            };
        }

        private static RewardGrant FromRewardSaveState(RunRewardSaveState save)
        {
            if (save == null || string.IsNullOrWhiteSpace(save.rewardId))
            {
                return default;
            }

            var kind = Enum.TryParse(save.rewardKind, out RewardKind parsedKind) ? parsedKind : RewardKind.PassiveItem;
            return new RewardGrant(
                save.roomId,
                save.rewardId,
                save.displayName,
                kind,
                save.souls,
                save.effects?.Select(RewardEffect.FromSaveState));
        }

        private static int StableHash(string value)
        {
            unchecked
            {
                var hash = 2166136261u;
                foreach (var character in value ?? string.Empty)
                {
                    hash ^= character;
                    hash *= 16777619u;
                }

                return (int)(hash & 0x7fffffff);
            }
        }
    }
}
