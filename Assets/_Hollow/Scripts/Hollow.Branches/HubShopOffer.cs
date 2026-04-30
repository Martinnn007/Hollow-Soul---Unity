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
            : this(offerId, displayName, price, ShopPriceCurrency.Souls, healAmount, rewardGrant, isPurchased)
        {
        }

        public HubShopOffer(string offerId, string displayName, int price, ShopPriceCurrency priceCurrency, int healAmount, RewardGrant rewardGrant, bool isPurchased = false)
        {
            OfferId = offerId ?? string.Empty;
            DisplayName = displayName ?? string.Empty;
            Price = Math.Max(0, price);
            PriceCurrency = priceCurrency;
            HealAmount = Math.Max(0, healAmount);
            RewardGrant = rewardGrant;
            IsPurchased = isPurchased;
        }

        public string OfferId { get; }

        public string DisplayName { get; }

        public int Price { get; }

        public ShopPriceCurrency PriceCurrency { get; }

        public int HealAmount { get; }

        public RewardGrant RewardGrant { get; }

        public bool IsPurchased { get; private set; }

        public bool TryPurchase(RunEconomy economy, out RewardGrant rewardGrant, out int healAmount)
        {
            rewardGrant = RewardGrant;
            healAmount = HealAmount;
            if (IsPurchased || economy == null || !SpendPrice(economy))
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
                priceCurrency = PriceCurrency.ToString(),
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
                    Enum.TryParse(save.priceCurrency, out ShopPriceCurrency parsedCurrency) ? parsedCurrency : ShopPriceCurrency.Souls,
                    save.healAmount,
                    FromRewardSaveState(save.reward),
                    save.isPurchased);
        }

        public static IReadOnlyList<HubShopOffer> CreateSeededOffers(int branchSeed, int branchDepth, RewardPoolDefinition standardPool, RewardPoolDefinition weaponPool = null, RewardPoolDefinition shopRewardPool = null)
        {
            var offers = new List<HubShopOffer>
            {
                new("heal_2", "Heal 2 HP", 8, ShopPriceCurrency.Coins, 2, default)
            };

            for (var index = 0; index < 2; index++)
            {
                var roomId = $"shop_offer_{branchDepth}_{index}";
                var shouldOfferWeapon = weaponPool != null && StableHash($"{branchSeed}|{branchDepth}|shop|weapon|{index}") % 5 == 0;
                var grant = shouldOfferWeapon && weaponPool.TryRoll(roomId, "m27_hub_shop_weapons", branchSeed + branchDepth + index, out var weaponGrant)
                    ? new RewardGrant(roomId, weaponGrant.RewardId, weaponGrant.DisplayName, weaponGrant.RewardKind, 0, 0, weaponGrant.Effects, weaponGrant.MaxStacks)
                    : shopRewardPool != null && shopRewardPool.TryRoll(roomId, "m51_hub_shop", branchSeed + branchDepth + index, out var shopRolled) && IsShopRewardKind(shopRolled.RewardKind)
                    ? new RewardGrant(roomId, shopRolled.RewardId, shopRolled.DisplayName, shopRolled.RewardKind, 0, 0, shopRolled.Effects, shopRolled.MaxStacks)
                    : standardPool != null && standardPool.TryRoll(roomId, "m20_hub_shop", branchSeed + branchDepth + index, out var rolled) && IsShopRewardKind(rolled.RewardKind)
                    ? new RewardGrant(roomId, rolled.RewardId, rolled.DisplayName, rolled.RewardKind, 0, 0, rolled.Effects, rolled.MaxStacks)
                    : FallbackReward(roomId, branchSeed, branchDepth, index);
                var price = grant.RewardKind switch
                {
                    RewardKind.Weapon => 22,
                    RewardKind.ConsumableCard => 10,
                    _ => 16
                };
                var currency = grant.RewardKind == RewardKind.Weapon ? ShopPriceCurrency.Souls : ShopPriceCurrency.Coins;
                offers.Add(new HubShopOffer($"reward_{index}", grant.DisplayName, price, currency, 0, grant));
            }

            return offers;
        }

        private static bool IsShopRewardKind(RewardKind kind)
        {
            return kind is RewardKind.PassiveItem or RewardKind.Card or RewardKind.PassiveCard or RewardKind.ActiveItem or RewardKind.ConsumableCard or RewardKind.Armor;
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

        private bool SpendPrice(RunEconomy economy)
        {
            return PriceCurrency == ShopPriceCurrency.Coins
                ? economy.SpendCoins(Price)
                : economy.SpendSouls(Price);
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
                coins = grant.Coins,
                maxStacks = grant.MaxStacks,
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
                save.coins,
                save.effects?.Select(RewardEffect.FromSaveState),
                save.maxStacks);
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
