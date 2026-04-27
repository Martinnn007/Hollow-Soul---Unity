using Hollow.Combat;
using Hollow.Rewards;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Hollow.Tests.EditMode
{
    public sealed class Milestone26PlayerBuildFoundationTests
    {
        [Test]
        public void RunEconomyTracksSoulsCoinsAndCollectedRewardCoins()
        {
            var economy = new RunEconomy();
            var grant = new RewardGrant(
                "room_01",
                "coin_cache",
                "Coin Cache",
                RewardKind.Currency,
                souls: 4,
                coins: 7,
                effects: System.Array.Empty<RewardEffect>());

            Assert.IsTrue(economy.ApplyReward(grant));
            Assert.AreEqual(4, economy.RunSouls);
            Assert.AreEqual(7, economy.RunCoins);
            Assert.AreEqual(7, economy.CollectedRewards[0].Coins);
            Assert.IsTrue(economy.SpendCoins(3));

            var restored = RunEconomy.FromSaveState(economy.ToSaveState());
            Assert.AreEqual(4, restored.RunSouls);
            Assert.AreEqual(4, restored.RunCoins);
            Assert.AreEqual(7, restored.CollectedRewards[0].Coins);
        }

        [Test]
        public void LegacyRewardStatsFlowIntoRunBuildDerivedStats()
        {
            var stats = new PlayerRunStats();
            var grant = new RewardGrant(
                "room_02",
                "training_bundle",
                "Training Bundle",
                RewardKind.PassiveItem,
                souls: 3,
                coins: 2,
                effects: new[]
                {
                    new RewardEffect(RewardEffectKind.MaxHealthBonus, intValue: 2),
                    new RewardEffect(RewardEffectKind.MoveSpeedBonus, floatValue: 0.25f),
                    new RewardEffect(RewardEffectKind.StrengthBonus, intValue: 1),
                    new RewardEffect(RewardEffectKind.MaxStaminaBonus, floatValue: 20f),
                    new RewardEffect(RewardEffectKind.StaminaRegenBonus, floatValue: 3f),
                    new RewardEffect(RewardEffectKind.DefenseBonus, intValue: 2),
                    new RewardEffect(RewardEffectKind.MeleeDamageBonus, intValue: 1),
                    new RewardEffect(RewardEffectKind.RangedDamageBonus, intValue: 2),
                    new RewardEffect(RewardEffectKind.AttackCooldownMultiplier, floatValue: 0.9f)
                });
            var economy = new RunEconomy();

            economy.ApplyReward(grant);
            stats.ApplyReward(grant);
            var build = PlayerRunBuild.FromLegacy(stats, economy);
            var derived = build.DerivedStats;

            Assert.AreEqual(8, derived.MaxHealth);
            Assert.AreEqual(4.25f, derived.SpeedMetersPerSecond, 0.001f);
            Assert.AreEqual(2, derived.Strength);
            Assert.AreEqual(120f, derived.MaxStamina, 0.001f);
            Assert.AreEqual(21f, derived.StaminaRegenPerSecond, 0.001f);
            Assert.AreEqual(2, derived.Defense);
            Assert.AreEqual(1, derived.MeleeDamageBonus);
            Assert.AreEqual(2, derived.RangedDamageBonus);
            Assert.AreEqual(0.9f, derived.AttackCooldownMultiplier, 0.001f);
            Assert.AreEqual(3, build.Wallet.RunSouls);
            Assert.AreEqual(2, build.Wallet.RunCoins);
        }

        [Test]
        public void RunBuildSaveStatePreservesEquipmentInventoryWalletAndStamina()
        {
            var build = new PlayerRunBuild();
            build.Equipment.EquipMeleeWeapon("iron_blade");
            build.Equipment.EquipRangedWeapon("ember_bolt");
            build.Equipment.EquipActiveItem("soul_lantern");
            build.Equipment.EquipConsumableCard("panic_card");
            build.Inventory.AddPassiveItem("stone_heart");
            build.Inventory.AddPassiveCard("quick_draw_rule");
            build.Wallet.AddSouls(12);
            build.Wallet.AddCoins(5);
            Assert.IsTrue(build.SpendStamina(20f));

            var restored = PlayerRunBuild.FromSaveState(build.ToSaveState());

            Assert.AreEqual("iron_blade", restored.Equipment.MeleeWeaponId);
            Assert.AreEqual("ember_bolt", restored.Equipment.RangedWeaponId);
            Assert.AreEqual("soul_lantern", restored.Equipment.ActiveItemId);
            Assert.AreEqual("panic_card", restored.Equipment.ConsumableCardId);
            Assert.Contains("stone_heart", (System.Collections.ICollection)restored.Inventory.PassiveItemIds);
            Assert.Contains("quick_draw_rule", (System.Collections.ICollection)restored.Inventory.PassiveCardIds);
            Assert.AreEqual(12, restored.Wallet.RunSouls);
            Assert.AreEqual(5, restored.Wallet.RunCoins);
            Assert.AreEqual(80f, restored.CurrentStamina, 0.001f);
        }

        [Test]
        public void BuildApplierPushesDerivedStatsIntoPlayerControllers()
        {
            var player = new GameObject("Player");
            try
            {
                var health = player.AddComponent<CombatantHealth>();
                health.Configure(6);
                var movement = player.AddComponent<PlayerMovementController>();
                var weapon = player.AddComponent<PlayerWeaponController>();
                var build = new PlayerRunBuild();
                build.AddModifier(new PlayerStatModifier
                {
                    sourceId = "test",
                    maxHealth = 2,
                    speed = 0.75f,
                    maxStamina = 25f,
                    staminaRegen = 4f,
                    rangedDamage = 3,
                    meleeDamage = 2,
                    attackCooldownMultiplier = 0.8f
                });
                build.Equipment.EquipMeleeWeapon("practice_sword");
                build.Equipment.EquipRangedWeapon("practice_bow");

                PlayerBuildApplier.Apply(build, player, healAmount: 1);

                Assert.AreEqual(8, health.MaxHealth);
                Assert.AreEqual(4.75f, movement.SpeedMetersPerSecond, 0.001f);
                Assert.AreEqual(125f, weapon.MaxStamina, 0.001f);
                Assert.AreEqual(100f, weapon.CurrentStamina, 0.001f);
                Assert.AreEqual("practice_sword", weapon.MeleeWeaponId);
                Assert.AreEqual("practice_bow", weapon.RangedWeaponId);
            }
            finally
            {
                Object.DestroyImmediate(player);
            }
        }
    }
}
