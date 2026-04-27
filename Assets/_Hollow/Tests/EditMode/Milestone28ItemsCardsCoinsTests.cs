using System.Linq;
using Hollow.Branches;
using Hollow.Data.Definitions;
using Hollow.Editor.Generation;
using Hollow.Editor.Validation;
using Hollow.Rewards;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Hollow.Tests.EditMode
{
    public sealed class Milestone28ItemsCardsCoinsTests
    {
        [Test]
        public void RewardApplicationRoutesEveryM28RewardKind()
        {
            var economy = new RunEconomy();
            var stats = new PlayerRunStats();
            var build = new PlayerRunBuild();
            var usableCatalog = ScriptableObject.CreateInstance<UsableItemCatalogDefinition>();
            var active = ScriptableObject.CreateInstance<UsableItemDefinition>();
            active.Configure("mending_charm", "Mending Charm", RewardKind.ActiveItem, RewardRarity.Common, 3, false, new[] { new RewardEffect(RewardEffectKind.Heal, intValue: 2) });
            usableCatalog.Configure("test", new[] { active });

            RewardApplicationService.Apply(new RewardGrant("passive", "vital_locket", "Vital Locket", RewardKind.PassiveItem, 0, new[] { new RewardEffect(RewardEffectKind.MaxHealthBonus, intValue: 1) }), economy, stats, build, null, usableCatalog);
            RewardApplicationService.Apply(new RewardGrant("old_card", "quick_draw", "Quick Draw", RewardKind.Card, 0, new[] { new RewardEffect(RewardEffectKind.AttackCooldownMultiplier, floatValue: 0.97f) }), economy, stats, build, null, usableCatalog);
            RewardApplicationService.Apply(new RewardGrant("passive_card", "blade_lesson", "Blade Lesson", RewardKind.PassiveCard, 0, new[] { new RewardEffect(RewardEffectKind.MeleeDamageBonus, intValue: 1) }), economy, stats, build, null, usableCatalog);
            RewardApplicationService.Apply(new RewardGrant("active", "mending_charm", "Mending Charm", RewardKind.ActiveItem, 0, System.Array.Empty<RewardEffect>()), economy, stats, build, null, usableCatalog);
            RewardApplicationService.Apply(new RewardGrant("consumable", "mend_card", "Mend Card", RewardKind.ConsumableCard, 0, System.Array.Empty<RewardEffect>()), economy, stats, build, null, usableCatalog);
            RewardApplicationService.Apply(new RewardGrant("coins", "coin_cache", "Coin Cache", RewardKind.Currency, 0, 6, System.Array.Empty<RewardEffect>()), economy, stats, build, null, usableCatalog);

            Assert.Contains("vital_locket", (System.Collections.ICollection)build.Inventory.PassiveItemIds);
            Assert.Contains("quick_draw", (System.Collections.ICollection)build.Inventory.PassiveCardIds);
            Assert.Contains("blade_lesson", (System.Collections.ICollection)build.Inventory.PassiveCardIds);
            Assert.AreEqual("mending_charm", build.Equipment.ActiveItemId);
            Assert.AreEqual(3, build.Equipment.ActiveItemCharges);
            Assert.AreEqual("mend_card", build.Equipment.ConsumableCardId);
            Assert.AreEqual(6, economy.RunCoins);
        }

        [Test]
        public void ShopOffersUseCoinsForStandardRewardsAndSoulsForWeapons()
        {
            var weaponPool = AssetDatabase.LoadAssetAtPath<RewardPoolDefinition>(Milestone27AssetGenerator.WeaponRewardPoolPath);
            var foundWeaponOffer = false;
            for (var seed = 1; seed < 80 && !foundWeaponOffer; seed++)
            {
                var offers = HubShopOffer.CreateSeededOffers(seed, 0, null, weaponPool);
                foreach (var offer in offers)
                {
                    if (offer.RewardGrant.RewardKind == RewardKind.Weapon)
                    {
                        foundWeaponOffer = true;
                        Assert.AreEqual(22, offer.Price);
                        Assert.AreEqual(ShopPriceCurrency.Souls, offer.PriceCurrency);
                    }
                    else
                    {
                        Assert.AreEqual(ShopPriceCurrency.Coins, offer.PriceCurrency);
                    }
                }
            }

            Assert.IsTrue(foundWeaponOffer, "Expected deterministic seed scan to find at least one rare weapon shop offer.");
        }

        [Test]
        public void UsableCatalogAndRewardPoolsContainM28Content()
        {
            var standardPool = AssetDatabase.LoadAssetAtPath<RewardPoolDefinition>(Milestone28AssetGenerator.StandardRewardPoolPath);
            var treasurePool = AssetDatabase.LoadAssetAtPath<RewardPoolDefinition>(Milestone28AssetGenerator.TreasureRewardPoolPath);
            var catalog = AssetDatabase.LoadAssetAtPath<UsableItemCatalogDefinition>(Milestone28AssetGenerator.UsableItemCatalogPath);

            Assert.IsNotNull(standardPool);
            Assert.IsNotNull(treasurePool);
            Assert.IsNotNull(catalog);
            Assert.IsTrue(standardPool.Rewards.Any(reward => reward.RewardId == "coin_cache" && reward.Coins >= 5 && reward.Coins <= 8));
            Assert.IsTrue(treasurePool.Rewards.Any(reward => reward.RewardId == "treasure_coins" && reward.Coins >= 12 && reward.Coins <= 16));
            Assert.IsTrue(catalog.TryGet("mending_charm", out var mendingCharm));
            Assert.AreEqual(3, mendingCharm.MaxCharges);
            Assert.IsTrue(catalog.TryGet("ember_card", out var emberCard));
            Assert.IsTrue(emberCard.ConsumeOnUse);
        }

        [Test]
        public void ActiveItemChargesSaveRestoreAndRecharge()
        {
            var build = new PlayerRunBuild();
            build.Equipment.EquipActiveItem("mending_charm");
            build.Equipment.SetActiveItemCharges(1);
            build.Equipment.RechargeActiveItem(1, 3);
            Assert.AreEqual(2, build.Equipment.ActiveItemCharges);
            Assert.IsTrue(build.Equipment.SpendActiveItemCharge());

            var restored = PlayerRunBuild.FromSaveState(build.ToSaveState());
            Assert.AreEqual("mending_charm", restored.Equipment.ActiveItemId);
            Assert.AreEqual(1, restored.Equipment.ActiveItemCharges);
        }

        [Test]
        public void Milestone28ValidatorReportsGeneratedStateValid()
        {
            Assert.DoesNotThrow(() => Milestone28Validator.Validate());
        }
    }
}
