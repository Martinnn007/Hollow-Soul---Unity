using System.Linq;
using Hollow.Branches;
using Hollow.Rewards;
using NUnit.Framework;
using UnityEngine;

namespace Hollow.Tests.EditMode
{
    public sealed class Milestone21ShopChoiceUiTests
    {
        [Test]
        public void CardViewModelReportsAffordableNeedAndSoldStates()
        {
            var offer = new HubShopOffer("heal_2", "Heal 2 HP", 8, ShopPriceCurrency.Coins, 2, default);

            var need = HubShopCardViewModel.FromOffer(offer, runSouls: 0, runCoins: 3);
            Assert.IsFalse(need.IsInteractable);
            Assert.AreEqual("Need 5 coins", need.StatusText);
            Assert.AreEqual("Heal +2 HP", need.EffectText);

            var affordable = HubShopCardViewModel.FromOffer(offer, runSouls: 0, runCoins: 8);
            Assert.IsTrue(affordable.IsInteractable);
            Assert.IsTrue(affordable.BodyText.Contains("Press E / A to buy"));

            var economy = new RunEconomy();
            economy.ApplyReward(new RewardGrant("seed", "debug_coins", "Debug Coins", RewardKind.Currency, 0, 10, System.Array.Empty<RewardEffect>()));
            Assert.IsTrue(offer.TryPurchase(economy, out _, out _));
            var sold = HubShopCardViewModel.FromOffer(offer, economy.RunSouls, economy.RunCoins);
            Assert.IsFalse(sold.IsInteractable);
            Assert.AreEqual("SOLD", sold.StatusText);
        }

        [Test]
        public void HubShopControllerSpawnsThreeOfferCards()
        {
            var root = new GameObject("ShopRoot");
            try
            {
                var controller = root.AddComponent<HubShopController>();
                controller.Configure(InterBranchHubState.Create(21001, 0, null));
                controller.BuildCards(runSouls: 40, runCoins: 40);

                Assert.AreEqual(3, controller.Cards.Count);
                Assert.Contains("heal_2", controller.Cards.Select(card => card.OfferId).ToArray());
                Assert.Contains("reward_0", controller.Cards.Select(card => card.OfferId).ToArray());
                Assert.Contains("reward_1", controller.Cards.Select(card => card.OfferId).ToArray());
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void NearestCardSelectionTargetsSpecificCard()
        {
            var root = new GameObject("ShopParent");
            var shop = new GameObject("HubShop");
            shop.transform.SetParent(root.transform, false);
            try
            {
                var controller = shop.AddComponent<HubShopController>();
                controller.Configure(InterBranchHubState.Create(21001, 0, null));
                controller.BuildCards(runSouls: 40, runCoins: 40);
                var rewardOne = controller.Cards.Single(card => card.OfferId == "reward_1");
                var playerLocalPosition = root.transform.InverseTransformPoint(rewardOne.transform.position);

                Assert.IsTrue(controller.TryGetNearestCard(playerLocalPosition, root.transform, 0.2f, out var nearest));
                Assert.AreEqual("reward_1", nearest.OfferId);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void PurchasedOfferPersistsAsSold()
        {
            var hub = InterBranchHubState.Create(21001, 0, null);
            var economy = new RunEconomy();
            economy.ApplyReward(new RewardGrant("seed", "debug_coins", "Debug Coins", RewardKind.Currency, 0, 40, System.Array.Empty<RewardEffect>()));
            var offer = hub.ShopOffers.First(candidate => candidate.OfferId == "heal_2");
            Assert.IsTrue(offer.TryPurchase(economy, out _, out _));

            var restored = InterBranchHubState.FromSaveState(hub.ToSaveState(), 21001, 0, null);
            var restoredOffer = restored.ShopOffers.First(candidate => candidate.OfferId == "heal_2");

            Assert.IsTrue(restoredOffer.IsPurchased);
            Assert.AreEqual("SOLD", HubShopCardViewModel.FromOffer(restoredOffer, economy.RunSouls, economy.RunCoins).StatusText);
        }

        [Test]
        public void SimplePricesStayFixedForHealCardsAndRewardCards()
        {
            var offers = InterBranchHubState.Create(21001, 0, null).ShopOffers;

            Assert.AreEqual(8, offers.First(offer => offer.OfferId == "heal_2").Price);
            Assert.AreEqual(ShopPriceCurrency.Coins, offers.First(offer => offer.OfferId == "heal_2").PriceCurrency);
            foreach (var offer in offers.Where(offer => offer.OfferId.StartsWith("reward_")))
            {
                Assert.AreEqual(offer.RewardGrant.RewardKind == RewardKind.Weapon ? 22 : 16, offer.Price);
            }
        }
    }
}
