using System.Collections.Generic;
using System.Linq;
using Hollow.Rewards;

namespace Hollow.Branches
{
    public readonly struct HubShopCardViewModel
    {
        public HubShopCardViewModel(
            string offerId,
            string title,
            string priceText,
            string effectText,
            string statusText,
            bool isSold,
            bool isAffordable)
        {
            OfferId = offerId ?? string.Empty;
            Title = title ?? string.Empty;
            PriceText = priceText ?? string.Empty;
            EffectText = effectText ?? string.Empty;
            StatusText = statusText ?? string.Empty;
            IsSold = isSold;
            IsAffordable = isAffordable;
        }

        public string OfferId { get; }

        public string Title { get; }

        public string PriceText { get; }

        public string EffectText { get; }

        public string StatusText { get; }

        public bool IsSold { get; }

        public bool IsAffordable { get; }

        public bool IsInteractable => !IsSold && IsAffordable;

        public string BodyText => $"{Title}\n{PriceText}\n{EffectText}\n{StatusText}";

        public static HubShopCardViewModel FromOffer(HubShopOffer offer, int runSouls)
        {
            if (offer == null)
            {
                return new HubShopCardViewModel(string.Empty, "Empty", string.Empty, string.Empty, string.Empty, isSold: true, isAffordable: false);
            }

            var affordable = runSouls >= offer.Price;
            var status = offer.IsPurchased
                ? "SOLD"
                : affordable ? "Press E / A to buy" : $"Need {offer.Price - runSouls} souls";
            return new HubShopCardViewModel(
                offer.OfferId,
                string.IsNullOrWhiteSpace(offer.DisplayName) ? "Unknown Offer" : offer.DisplayName,
                $"{offer.Price} souls",
                EffectTextFor(offer),
                status,
                offer.IsPurchased,
                affordable);
        }

        private static string EffectTextFor(HubShopOffer offer)
        {
            if (offer.HealAmount > 0)
            {
                return $"Heal +{offer.HealAmount} HP";
            }

            var grant = offer.RewardGrant;
            if (grant.Effects != null && grant.Effects.Count > 0)
            {
                return string.Join(", ", grant.Effects.Select(DescribeEffect));
            }

            return grant.RewardKind switch
            {
                RewardKind.Card => "Card reward",
                RewardKind.PassiveItem => "Passive item",
                RewardKind.Weapon => "Weapon replacement",
                RewardKind.Currency => "Currency",
                _ => "Reward"
            };
        }

        private static string DescribeEffect(RewardEffect effect)
        {
            return effect.Kind switch
            {
                RewardEffectKind.MaxHealthBonus => $"+{effect.IntValue} max HP",
                RewardEffectKind.Heal => $"Heal +{effect.IntValue}",
                RewardEffectKind.MoveSpeedBonus => $"+{effect.FloatValue:0.##}m/s speed",
                RewardEffectKind.ShotCooldownMultiplier => $"{(1f - effect.FloatValue) * 100f:0}% faster shots",
                RewardEffectKind.ProjectileDamageBonus => $"+{effect.IntValue} damage",
                RewardEffectKind.ProjectileSpeedBonus => $"+{effect.FloatValue:0.##} projectile speed",
                RewardEffectKind.PlayerContactDamageResist => $"{effect.IntValue} contact resist",
                _ => "Reward effect"
            };
        }
    }
}
