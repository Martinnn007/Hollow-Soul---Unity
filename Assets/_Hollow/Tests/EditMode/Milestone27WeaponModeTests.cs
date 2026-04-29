using System.Linq;
using Hollow.Branches;
using Hollow.Combat;
using Hollow.Data.Definitions;
using Hollow.Editor.Generation;
using Hollow.Editor.Validation;
using Hollow.Input;
using Hollow.Rewards;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Hollow.Tests.EditMode
{
    public sealed class Milestone27WeaponModeTests
    {
        [Test]
        public void WeaponCatalogContainsStarterAndRareWeapons()
        {
            var catalog = LoadCatalog();

            Assert.IsTrue(catalog.TryGetWeapon("starter_blade", out var starterBlade));
            Assert.AreEqual(WeaponSlot.Melee, starterBlade.Slot);
            Assert.AreEqual(AttackKind.Light, starterBlade.LightAttack.AttackKind);
            Assert.Greater(starterBlade.HeavyAttack.Damage, starterBlade.LightAttack.Damage);
            Assert.IsTrue(catalog.TryGetWeapon("starter_bolt", out var starterBolt));
            Assert.AreEqual(WeaponSlot.Ranged, starterBolt.Slot);
            Assert.IsTrue(catalog.TryGetWeapon("iron_cleaver", out var ironCleaver));
            Assert.AreEqual(WeaponSlot.Melee, ironCleaver.Slot);
            Assert.IsTrue(catalog.TryGetWeapon("ember_bolt", out var emberBolt));
            Assert.AreEqual(WeaponSlot.Ranged, emberBolt.Slot);
        }

        [Test]
        public void StarterWeaponsUseReadableCombatBalance()
        {
            var catalog = LoadCatalog();

            Assert.IsTrue(catalog.TryGetWeapon("starter_blade", out var starterBlade));
            Assert.AreEqual(0.67f, starterBlade.LightAttack.CooldownSeconds, 0.001f);
            Assert.AreEqual(6f, starterBlade.LightAttack.StaminaCost, 0.001f);
            Assert.AreEqual(3.5f, starterBlade.HeavyAttack.CooldownSeconds, 0.001f);
            Assert.AreEqual(40f, starterBlade.HeavyAttack.StaminaCost, 0.001f);

            Assert.IsTrue(catalog.TryGetWeapon("starter_bolt", out var starterBolt));
            Assert.AreEqual(1f, starterBolt.LightAttack.CooldownSeconds, 0.001f);
            Assert.AreEqual(0f, starterBolt.LightAttack.StaminaCost, 0.001f);
            Assert.AreEqual(10f, starterBolt.HeavyAttack.CooldownSeconds, 0.001f);
            Assert.AreEqual(35f, starterBolt.HeavyAttack.StaminaCost, 0.001f);
        }

        [Test]
        public void BuildApplierPassesCatalogEquipmentAndActiveSlotToWeaponController()
        {
            var player = new GameObject("Player");
            try
            {
                var weapon = player.AddComponent<PlayerWeaponController>();
                var build = new PlayerRunBuild();
                build.Equipment.EquipMeleeWeapon("iron_cleaver");
                build.Equipment.EquipRangedWeapon("ember_bolt");
                build.Equipment.SetActiveWeaponSlot(WeaponSlot.Melee);

                PlayerBuildApplier.Apply(build, player, LoadCatalog());

                Assert.AreEqual("iron_cleaver", weapon.MeleeWeaponId);
                Assert.AreEqual("ember_bolt", weapon.RangedWeaponId);
                Assert.AreEqual(WeaponSlot.Melee, weapon.ActiveWeaponSlot);
                Assert.AreEqual("Iron Cleaver", weapon.ActiveWeaponDisplayName);
            }
            finally
            {
                Object.DestroyImmediate(player);
            }
        }

        [Test]
        public void RangeBonusesAffectEffectiveWeaponRanges()
        {
            var player = new GameObject("Player");
            try
            {
                var weapon = player.AddComponent<PlayerWeaponController>();
                weapon.ConfigureBuildStats(
                    1f,
                    0,
                    0,
                    100f,
                    18f,
                    "starter_blade",
                    "starter_bolt",
                    WeaponSlot.Ranged,
                    100f,
                    LoadCatalog(),
                    0.3f,
                    1.25f);

                Assert.AreEqual(1.25f, weapon.EffectiveMeleeLightRangeMeters, 0.001f);
                Assert.AreEqual(10.25f, weapon.EffectiveRangedLightRangeMeters, 0.001f);
            }
            finally
            {
                Object.DestroyImmediate(player);
            }
        }

        [Test]
        public void MeleeSwipePresenterCreatesVisualOnlyRangeBox()
        {
            var parent = new GameObject("SwipeParent");
            try
            {
                var swipe = MeleeSwipePresenter.Spawn(parent.transform, Vector3.zero, Vector3.forward, 1.75f, AttackKind.Heavy);

                Assert.IsNotNull(swipe);
                Assert.IsNull(swipe.GetComponent<Collider>());
                Assert.AreEqual(1.75f, swipe.transform.localScale.z, 0.001f);
                Assert.AreEqual("MeleeSwipe.Heavy", swipe.name);
            }
            finally
            {
                Object.DestroyImmediate(parent);
            }
        }

        [Test]
        public void GameplayDebugHudDefaultsHiddenAndCanToggle()
        {
            GameplayDebugHudState.SetVisible(false);
            Assert.IsFalse(GameplayDebugHudState.IsVisible);

            GameplayDebugHudState.Toggle();

            Assert.IsTrue(GameplayDebugHudState.IsVisible);
            GameplayDebugHudState.SetVisible(false);
        }

        [Test]
        public void ActiveWeaponSlotPersistsInRunBuildSaveState()
        {
            var build = new PlayerRunBuild();
            build.Equipment.SetActiveWeaponSlot(WeaponSlot.Melee);

            var restored = PlayerRunBuild.FromSaveState(build.ToSaveState());

            Assert.AreEqual(WeaponSlot.Melee, restored.Equipment.ActiveWeaponSlot);
        }

        [Test]
        public void WeaponRewardPoolContainsRareWeaponRewards()
        {
            var pool = AssetDatabase.LoadAssetAtPath<RewardPoolDefinition>(Milestone27AssetGenerator.WeaponRewardPoolPath);

            Assert.IsNotNull(pool, "Run M27 generation before validating weapon rewards.");
            Assert.IsTrue(pool.Rewards.Any(reward => reward.RewardId == "iron_cleaver" && reward.RewardKind == RewardKind.Weapon));
            Assert.IsTrue(pool.Rewards.Any(reward => reward.RewardId == "ember_bolt" && reward.RewardKind == RewardKind.Weapon));
        }

        [Test]
        public void ShopOffersCanDeterministicallyRollWeaponRewards()
        {
            var weaponPool = AssetDatabase.LoadAssetAtPath<RewardPoolDefinition>(Milestone27AssetGenerator.WeaponRewardPoolPath);
            Assert.IsNotNull(weaponPool, "Run M27 generation before validating shop weapon rolls.");

            var foundWeaponOffer = false;
            for (var seed = 27001; seed < 27100; seed++)
            {
                var offers = HubShopOffer.CreateSeededOffers(seed, 0, null, weaponPool);
                if (offers.Any(offer => offer.RewardGrant.RewardKind == RewardKind.Weapon))
                {
                    foundWeaponOffer = true;
                    break;
                }
            }

            Assert.IsTrue(foundWeaponOffer, "Expected rare deterministic shop weapon offers to appear for some seeds.");
        }

        [Test]
        public void Milestone27ValidatorReportsGeneratedStateValid()
        {
            Assert.DoesNotThrow(() => Milestone27Validator.Validate());
        }

        private static WeaponCatalogDefinition LoadCatalog()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<WeaponCatalogDefinition>(Milestone27AssetGenerator.WeaponCatalogPath);
            Assert.IsNotNull(catalog, "Run M27 generation before validating weapon catalog.");
            return catalog;
        }
    }
}
