using Hollow.Data.Definitions;
using Hollow.Persistence;
using Hollow.Rewards;
using Hollow.UI.Shell;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Hollow.Tests.EditMode
{
    public sealed class Milestone42PlayerBuildUxTests
    {
        [Test]
        public void PlayerBuildHudModelShowsCoreBuildLines()
        {
            var model = new PlayerBuildHudModel(
                "Balanced",
                5,
                6,
                2,
                true,
                4f,
                1,
                80f,
                100f,
                18f,
                1,
                2,
                0.25f,
                1.5f,
                0.95f,
                9,
                14,
                "Ranged - Starter Bolt",
                "Starter Blade",
                "Starter Bolt",
                "Skeletal Armor",
                "Mending Charm (2/3)",
                "Mend Card",
                "Skeletal Set");

            Assert.IsTrue(model.BodyText.Contains("Coins: 9"));
            Assert.IsTrue(model.BodyText.Contains("Souls: 14"));
            Assert.IsTrue(model.BodyText.Contains("Set: Skeletal Set"));
            Assert.IsTrue(model.BodyText.Contains("Guard"));
            Assert.IsTrue(model.BodyText.Contains("Range: M +0.25m  R +1.5m"));
            Assert.IsTrue(model.BodyText.Contains("Karma 0"));
            Assert.AreEqual("starter_blade", model.ActiveWeaponId);
            Assert.AreEqual(WeaponSlot.Melee, model.ActiveWeaponSlot);
            Assert.AreEqual(string.Empty, model.ActiveItemId);
            Assert.AreEqual(0, model.ActiveItemCharges);
            Assert.AreEqual(string.Empty, model.ConsumableCardId);
        }

        [Test]
        public void PlayerBuildHudModelPreservesUsableIdsAndClampsCharges()
        {
            var model = CreateHudModel(
                currentStamina: 80f,
                maxStamina: 100f,
                activeItemId: " mending_charm ",
                activeItemCharges: -2,
                activeItemMaxCharges: 3,
                consumableCardId: " mend_card ");

            Assert.AreEqual("mending_charm", model.ActiveItemId);
            Assert.AreEqual(0, model.ActiveItemCharges);
            Assert.AreEqual(3, model.ActiveItemMaxCharges);
            Assert.AreEqual("mend_card", model.ConsumableCardId);
        }

        [Test]
        public void PlayerBuildHudRendersAvatarAndHeartContainers()
        {
            var canvasObject = new GameObject("HudCanvas", typeof(Canvas), typeof(PlayerBuildHudController));
            try
            {
                var controller = canvasObject.GetComponent<PlayerBuildHudController>();
                var model = CreateHudModel(
                    currentStamina: 80f,
                    maxStamina: 100f,
                    souls: 14,
                    coins: 9,
                    activeItemId: "mending_charm",
                    activeItemCharges: 2,
                    activeItemMaxCharges: 3,
                    consumableCardId: "mend_card");

                controller.RefreshFromModel(model);

                Assert.AreEqual(5, controller.RenderedHeartCount);
                Assert.AreEqual(3, controller.RenderedFullHeartCount);
                Assert.IsTrue(controller.HasRenderedStaminaBar);
                Assert.IsTrue(controller.HasRenderedSoulsCounter);
                Assert.IsTrue(controller.HasRenderedCoinsCounter);
                Assert.IsTrue(controller.HasRenderedKeysCounter);
                Assert.AreEqual(9, controller.RenderedCoins);
                Assert.AreEqual(0, controller.RenderedKeys);
                Assert.IsFalse(controller.RenderedHasBossKey);
                Assert.AreEqual(14, controller.RenderedSouls);
                Assert.AreEqual(0.8f, controller.RenderedStaminaFillAmount, 0.001f);
                var avatar = canvasObject.transform.Find("PlayerBuildHud.Panel/PlayerBuildHud.Avatar");
                Assert.IsNotNull(avatar);
                var firstHeart = canvasObject.transform.Find("PlayerBuildHud.Panel/PlayerBuildHud.Heart_01");
                Assert.IsNotNull(firstHeart);
                var coinsIcon = canvasObject.transform.Find("PlayerBuildHud.Panel/PlayerBuildHud.CoinsIcon");
                var coinsAmount = canvasObject.transform.Find("PlayerBuildHud.Panel/PlayerBuildHud.CoinsAmount");
                var keysIcon = canvasObject.transform.Find("PlayerBuildHud.Panel/PlayerBuildHud.KeysIcon");
                var keysAmount = canvasObject.transform.Find("PlayerBuildHud.Panel/PlayerBuildHud.KeysAmount");
                var soulsIcon = canvasObject.transform.Find("PlayerBuildHud.Panel/PlayerBuildHud.SoulsIcon");
                var soulsAmount = canvasObject.transform.Find("PlayerBuildHud.Panel/PlayerBuildHud.SoulsAmount");
                var statsBlock = canvasObject.transform.Find("PlayerBuildHud.Panel/PlayerBuildHud.StatsBlock");
                var activeWeaponIcon = canvasObject.transform.Find("PlayerBuildHud.ActiveWeaponIcon");
                var activeItemIcon = canvasObject.transform.Find("PlayerBuildHud.ActiveItemIcon");
                var consumableCardIcon = canvasObject.transform.Find("PlayerBuildHud.ConsumableCardIcon");
                Assert.IsNotNull(coinsIcon);
                Assert.IsNotNull(coinsAmount);
                Assert.IsNotNull(keysIcon);
                Assert.IsNotNull(keysAmount);
                Assert.IsNotNull(soulsIcon);
                Assert.IsNotNull(soulsAmount);
                Assert.IsNotNull(statsBlock);
                Assert.IsNotNull(activeWeaponIcon);
                Assert.IsNotNull(activeItemIcon);
                Assert.IsNotNull(consumableCardIcon);
                Assert.IsTrue(controller.HasRenderedStatsBlock);
                Assert.IsTrue(controller.HasRenderedActiveWeaponIcon);
                Assert.IsTrue(controller.HasRenderedActiveItemIcon);
                Assert.IsTrue(controller.HasRenderedConsumableCardIcon);
                Assert.IsNotNull(coinsIcon.GetComponent<Image>().sprite);
                Assert.IsNotNull(keysIcon.GetComponent<Image>().sprite);
                Assert.IsNotNull(soulsIcon.GetComponent<Image>().sprite);
                Assert.IsNotNull(activeItemIcon.GetComponent<Image>().sprite);
                Assert.IsNotNull(consumableCardIcon.GetComponent<Image>().sprite);
                Assert.AreEqual("9", coinsAmount.GetComponent<Text>().text);
                Assert.AreEqual("0", keysAmount.GetComponent<Text>().text);
                Assert.AreEqual("14", soulsAmount.GetComponent<Text>().text);
                Assert.AreEqual("mending_charm", controller.RenderedActiveItemId);
                Assert.AreEqual("mend_card", controller.RenderedConsumableCardId);
                Assert.AreEqual("2/3", controller.RenderedActiveItemChargesText);
                Assert.AreEqual("2/3", activeItemIcon.Find("Charges").GetComponent<Text>().text);
                AssertStatRow(statsBlock, "MeleeDamage", "3/4");
                AssertStatRow(statsBlock, "MeleeSpeed", "1.5/s");
                AssertStatRow(statsBlock, "RangedDamage", "1/2");
                AssertStatRow(statsBlock, "RangedSpeed", "2.0/s");
                AssertStatRow(statsBlock, "Range", "6.5m");
                AssertStatRow(statsBlock, "Defense", "1");
                AssertStatRow(statsBlock, "Speed", "4.0m/s");
                AssertStatRow(statsBlock, "Karma", "0");
                Assert.AreEqual("starter_blade", controller.RenderedActiveWeaponId);
                var heartRect = (RectTransform)firstHeart;
                var coinIconRect = (RectTransform)coinsIcon;
                var coinAmountRect = (RectTransform)coinsAmount;
                var keyIconRect = (RectTransform)keysIcon;
                var keyAmountRect = (RectTransform)keysAmount;
                var iconRect = (RectTransform)soulsIcon;
                var amountRect = (RectTransform)soulsAmount;
                var activeWeaponIconRect = (RectTransform)activeWeaponIcon;
                var activeItemIconRect = (RectTransform)activeItemIcon;
                var consumableCardIconRect = (RectTransform)consumableCardIcon;
                Assert.LessOrEqual(coinIconRect.sizeDelta.x, heartRect.sizeDelta.x);
                Assert.LessOrEqual(coinIconRect.sizeDelta.y, heartRect.sizeDelta.y);
                Assert.LessOrEqual(keyIconRect.sizeDelta.x, heartRect.sizeDelta.x);
                Assert.LessOrEqual(keyIconRect.sizeDelta.y, heartRect.sizeDelta.y);
                Assert.LessOrEqual(iconRect.sizeDelta.x, heartRect.sizeDelta.x);
                Assert.LessOrEqual(iconRect.sizeDelta.y, heartRect.sizeDelta.y);
                Assert.Greater(iconRect.anchoredPosition.y, heartRect.anchoredPosition.y);
                Assert.Greater(coinAmountRect.anchoredPosition.x, coinIconRect.anchoredPosition.x);
                Assert.Greater(keyAmountRect.anchoredPosition.x, keyIconRect.anchoredPosition.x);
                Assert.Greater(amountRect.anchoredPosition.x, iconRect.anchoredPosition.x);
                Assert.AreEqual(Vector2.zero, activeWeaponIconRect.anchorMin);
                Assert.AreEqual(Vector2.zero, activeWeaponIconRect.anchorMax);
                Assert.AreEqual(Vector2.zero, activeWeaponIconRect.pivot);
                Assert.AreEqual(24f, activeWeaponIconRect.anchoredPosition.x, 0.001f);
                Assert.AreEqual(24f, activeWeaponIconRect.anchoredPosition.y, 0.001f);
                Assert.AreEqual(108f, activeWeaponIconRect.sizeDelta.x, 0.001f);
                Assert.AreEqual(72f, activeWeaponIconRect.sizeDelta.y, 0.001f);
                Assert.AreEqual(1.5f, activeWeaponIconRect.sizeDelta.x / activeWeaponIconRect.sizeDelta.y, 0.001f);
                Assert.Greater(activeWeaponIconRect.sizeDelta.y, ((RectTransform)statsBlock.Find("PlayerBuildHud.Stat.MeleeDamage/Icon")).sizeDelta.y);
                Assert.IsNotNull(activeWeaponIcon.GetComponent<Image>().sprite);
                Assert.AreEqual(new Vector2(1f, 0f), activeItemIconRect.anchorMin);
                Assert.AreEqual(new Vector2(1f, 0f), activeItemIconRect.anchorMax);
                Assert.AreEqual(new Vector2(1f, 0f), activeItemIconRect.pivot);
                Assert.AreEqual(-24f, activeItemIconRect.anchoredPosition.x, 0.001f);
                Assert.AreEqual(24f, activeItemIconRect.anchoredPosition.y, 0.001f);
                Assert.AreEqual(84f, activeItemIconRect.sizeDelta.x, 0.001f);
                Assert.AreEqual(84f, activeItemIconRect.sizeDelta.y, 0.001f);
                Assert.AreEqual(activeItemIconRect.anchoredPosition.x, consumableCardIconRect.anchoredPosition.x, 0.001f);
                Assert.Greater(consumableCardIconRect.anchoredPosition.y, activeItemIconRect.anchoredPosition.y);
                Assert.AreEqual(activeItemIconRect.sizeDelta, consumableCardIconRect.sizeDelta);
                Assert.Greater(activeItemIconRect.sizeDelta.y, ((RectTransform)statsBlock.Find("PlayerBuildHud.Stat.MeleeDamage/Icon")).sizeDelta.y);
                Assert.Less(keyIconRect.anchoredPosition.y, coinIconRect.anchoredPosition.y);
                var keyBottomY = keyIconRect.anchoredPosition.y - keyIconRect.sizeDelta.y * 0.5f;
                var statsRect = (RectTransform)statsBlock;
                Assert.Less(statsRect.anchoredPosition.y, keyBottomY);
                var meleeDamageRow = (RectTransform)statsBlock.Find("PlayerBuildHud.Stat.MeleeDamage");
                var meleeSpeedRow = (RectTransform)statsBlock.Find("PlayerBuildHud.Stat.MeleeSpeed");
                var rangedDamageRow = (RectTransform)statsBlock.Find("PlayerBuildHud.Stat.RangedDamage");
                var rangedSpeedRow = (RectTransform)statsBlock.Find("PlayerBuildHud.Stat.RangedSpeed");
                var rangeRow = (RectTransform)statsBlock.Find("PlayerBuildHud.Stat.Range");
                var defenseRow = (RectTransform)statsBlock.Find("PlayerBuildHud.Stat.Defense");
                var speedRow = (RectTransform)statsBlock.Find("PlayerBuildHud.Stat.Speed");
                var karmaRow = (RectTransform)statsBlock.Find("PlayerBuildHud.Stat.Karma");
                Assert.AreEqual(meleeDamageRow.anchoredPosition.x, meleeSpeedRow.anchoredPosition.x, 0.001f);
                Assert.AreEqual(meleeDamageRow.anchoredPosition.x, rangedDamageRow.anchoredPosition.x, 0.001f);
                Assert.AreEqual(meleeDamageRow.anchoredPosition.x, rangedSpeedRow.anchoredPosition.x, 0.001f);
                Assert.AreEqual(meleeDamageRow.anchoredPosition.x, rangeRow.anchoredPosition.x, 0.001f);
                Assert.AreEqual(meleeDamageRow.anchoredPosition.x, defenseRow.anchoredPosition.x, 0.001f);
                Assert.AreEqual(meleeDamageRow.anchoredPosition.x, speedRow.anchoredPosition.x, 0.001f);
                Assert.AreEqual(rangeRow.anchoredPosition.x, karmaRow.anchoredPosition.x, 0.001f);
                Assert.Less(meleeSpeedRow.anchoredPosition.y, meleeDamageRow.anchoredPosition.y);
                Assert.Less(rangedDamageRow.anchoredPosition.y, meleeSpeedRow.anchoredPosition.y);
                Assert.Less(rangedSpeedRow.anchoredPosition.y, rangedDamageRow.anchoredPosition.y);
                Assert.Less(rangeRow.anchoredPosition.y, rangedSpeedRow.anchoredPosition.y);
                Assert.Less(defenseRow.anchoredPosition.y, rangeRow.anchoredPosition.y);
                Assert.Less(speedRow.anchoredPosition.y, defenseRow.anchoredPosition.y);
                Assert.Less(karmaRow.anchoredPosition.y, speedRow.anchoredPosition.y);
                var staminaBar = canvasObject.transform.Find("PlayerBuildHud.Panel/PlayerBuildHud.StaminaBar");
                Assert.IsNotNull(staminaBar);
                var staminaRect = (RectTransform)staminaBar;
                Assert.AreEqual(330f, staminaRect.sizeDelta.x, 0.001f);
                Assert.AreEqual(heartRect.anchoredPosition.x - heartRect.sizeDelta.x * 0.5f, staminaRect.anchoredPosition.x, 0.001f);
                var avatarRect = (RectTransform)avatar;
                var avatarBottomY = avatarRect.anchoredPosition.y - avatarRect.sizeDelta.y;
                var coinsTopY = coinIconRect.anchoredPosition.y + coinIconRect.sizeDelta.y * 0.5f;
                var coinsVisibleRight = coinAmountRect.anchoredPosition.x + 24f;
                var coinsVisibleCenterX = (coinIconRect.anchoredPosition.x + coinsVisibleRight) * 0.5f;
                var keysVisibleRight = keyAmountRect.anchoredPosition.x + 24f;
                var keysVisibleCenterX = (keyIconRect.anchoredPosition.x + keysVisibleRight) * 0.5f;
                var avatarCenterX = avatarRect.anchoredPosition.x + avatarRect.sizeDelta.x * 0.5f;
                Assert.LessOrEqual(coinsTopY, avatarBottomY);
                Assert.AreEqual(avatarCenterX, coinsVisibleCenterX, 1.5f);
                Assert.AreEqual(avatarCenterX, keysVisibleCenterX, 1.5f);
                var vitalsTop = iconRect.anchoredPosition.y + iconRect.sizeDelta.y * 0.5f;
                var vitalsBottom = staminaRect.anchoredPosition.y - staminaRect.sizeDelta.y * 0.5f;
                var vitalsCenterY = (vitalsTop + vitalsBottom) * 0.5f;
                var avatarCenterY = avatarRect.anchoredPosition.y - avatarRect.sizeDelta.y * 0.5f;
                Assert.AreEqual(avatarCenterY, vitalsCenterY, 1.5f);
                var staminaFrame = staminaBar.Find("PlayerBuildHud.StaminaFrame");
                var staminaFill = staminaBar.Find("PlayerBuildHud.StaminaFill");
                Assert.IsNotNull(staminaFrame);
                Assert.IsNotNull(staminaFill);
                Assert.Greater(staminaFill.GetSiblingIndex(), staminaFrame.GetSiblingIndex());
            }
            finally
            {
                Object.DestroyImmediate(canvasObject);
            }
        }

        [Test]
        public void PlayerBuildHudActiveWeaponIconRefreshesWithoutRebuildingVitals()
        {
            var canvasObject = new GameObject("HudCanvas", typeof(Canvas), typeof(PlayerBuildHudController));
            try
            {
                var controller = canvasObject.GetComponent<PlayerBuildHudController>();
                controller.RefreshFromModel(CreateHudModel(currentStamina: 80f, maxStamina: 100f, activeWeaponId: "starter_blade", activeWeaponSlot: WeaponSlot.Melee));
                var activeWeaponIcon = canvasObject.transform
                    .Find("PlayerBuildHud.ActiveWeaponIcon")
                    .GetComponent<Image>();
                var meleeSprite = activeWeaponIcon.sprite;
                var renderedHeartCount = controller.RenderedHeartCount;
                var renderedStaminaFill = controller.RenderedStaminaFillAmount;

                controller.RefreshFromModel(CreateHudModel(currentStamina: 80f, maxStamina: 100f, activeWeaponId: "starter_pistol", activeWeaponSlot: WeaponSlot.Ranged));

                Assert.AreEqual("starter_pistol", controller.RenderedActiveWeaponId);
                Assert.IsNotNull(activeWeaponIcon.sprite);
                Assert.AreNotSame(meleeSprite, activeWeaponIcon.sprite);
                Assert.AreEqual(renderedHeartCount, controller.RenderedHeartCount);
                Assert.AreEqual(renderedStaminaFill, controller.RenderedStaminaFillAmount, 0.001f);
            }
            finally
            {
                Object.DestroyImmediate(canvasObject);
            }
        }

        [Test]
        public void PlayerBuildHudUsableIconsRefreshAndHideWithoutRebuildingVitals()
        {
            var canvasObject = new GameObject("HudCanvas", typeof(Canvas), typeof(PlayerBuildHudController));
            try
            {
                var controller = canvasObject.GetComponent<PlayerBuildHudController>();
                controller.RefreshFromModel(CreateHudModel(
                    currentStamina: 80f,
                    maxStamina: 100f,
                    activeItemId: "mending_charm",
                    activeItemCharges: 2,
                    activeItemMaxCharges: 3,
                    consumableCardId: "mend_card"));
                var activeItemIcon = canvasObject.transform
                    .Find("PlayerBuildHud.ActiveItemIcon")
                    .GetComponent<Image>();
                var consumableCardIcon = canvasObject.transform
                    .Find("PlayerBuildHud.ConsumableCardIcon")
                    .GetComponent<Image>();
                var charges = activeItemIcon.transform.Find("Charges").GetComponent<Text>();
                var activeItemSprite = activeItemIcon.sprite;
                var consumableCardSprite = consumableCardIcon.sprite;
                var renderedHeartCount = controller.RenderedHeartCount;
                var renderedStaminaFill = controller.RenderedStaminaFillAmount;

                controller.RefreshFromModel(CreateHudModel(
                    currentStamina: 80f,
                    maxStamina: 100f,
                    activeItemId: "echo_burst",
                    activeItemCharges: 1,
                    activeItemMaxCharges: 0,
                    consumableCardId: "swift_card"));

                Assert.AreEqual("echo_burst", controller.RenderedActiveItemId);
                Assert.AreEqual("swift_card", controller.RenderedConsumableCardId);
                Assert.AreEqual("1", controller.RenderedActiveItemChargesText);
                Assert.AreEqual("1", charges.text);
                Assert.IsTrue(activeItemIcon.enabled);
                Assert.IsTrue(consumableCardIcon.enabled);
                Assert.AreNotSame(activeItemSprite, activeItemIcon.sprite);
                Assert.AreNotSame(consumableCardSprite, consumableCardIcon.sprite);
                Assert.AreEqual(renderedHeartCount, controller.RenderedHeartCount);
                Assert.AreEqual(renderedStaminaFill, controller.RenderedStaminaFillAmount, 0.001f);

                controller.RefreshFromModel(CreateHudModel(currentStamina: 80f, maxStamina: 100f));

                Assert.IsFalse(controller.HasRenderedActiveItemIcon);
                Assert.IsFalse(controller.HasRenderedConsumableCardIcon);
                Assert.AreEqual(string.Empty, controller.RenderedActiveItemId);
                Assert.AreEqual(string.Empty, controller.RenderedConsumableCardId);
                Assert.AreEqual(string.Empty, controller.RenderedActiveItemChargesText);
                Assert.AreEqual(string.Empty, charges.text);
                Assert.IsFalse(activeItemIcon.enabled);
                Assert.IsFalse(consumableCardIcon.enabled);
                Assert.AreEqual(renderedHeartCount, controller.RenderedHeartCount);
                Assert.AreEqual(renderedStaminaFill, controller.RenderedStaminaFillAmount, 0.001f);
            }
            finally
            {
                Object.DestroyImmediate(canvasObject);
            }
        }

        [Test]
        public void PlayerBuildHudWeaponIconResourcesCoverWeaponCatalog()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<WeaponCatalogDefinition>("Assets/_Hollow/Data/Weapons/M27/WeaponCatalog_M27.asset");
            Assert.IsNotNull(catalog);

            foreach (var weapon in catalog.Weapons)
            {
                Assert.IsNotNull(weapon);
                var sprite = Resources.Load<Sprite>($"UI/Hud/Weapons/{weapon.WeaponId}");
                Assert.IsNotNull(sprite, $"Missing HUD weapon icon for {weapon.WeaponId}");
            }
        }

        [Test]
        public void PlayerBuildHudUsableIconResourcesCoverUsableCatalog()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<UsableItemCatalogDefinition>("Assets/_Hollow/Data/Rewards/M28/UsableItemCatalog_M28.asset");
            Assert.IsNotNull(catalog);

            foreach (var usable in catalog.Items)
            {
                Assert.IsNotNull(usable);
                if (usable.RewardKind is not (RewardKind.ActiveItem or RewardKind.ConsumableCard))
                {
                    continue;
                }

                var sprite = Resources.Load<Sprite>($"UI/Hud/Usables/{usable.ItemId}");
                Assert.IsNotNull(sprite, $"Missing HUD usable icon for {usable.ItemId}");
            }
        }

        [Test]
        public void PlayerBuildHudCurrencyAmountsRefreshFromRunModel()
        {
            var canvasObject = new GameObject("HudCanvas", typeof(Canvas), typeof(PlayerBuildHudController));
            try
            {
                var controller = canvasObject.GetComponent<PlayerBuildHudController>();

                controller.RefreshFromModel(CreateHudModel(currentStamina: 80f, maxStamina: 100f, souls: 12, coins: 5, keys: 2));
                var coinsAmount = canvasObject.transform
                    .Find("PlayerBuildHud.Panel/PlayerBuildHud.CoinsAmount")
                    .GetComponent<Text>();
                var keysIcon = canvasObject.transform
                    .Find("PlayerBuildHud.Panel/PlayerBuildHud.KeysIcon")
                    .GetComponent<Image>();
                var keysAmount = canvasObject.transform
                    .Find("PlayerBuildHud.Panel/PlayerBuildHud.KeysAmount")
                    .GetComponent<Text>();
                var soulsAmount = canvasObject.transform
                    .Find("PlayerBuildHud.Panel/PlayerBuildHud.SoulsAmount")
                    .GetComponent<Text>();
                var meleeDamage = canvasObject.transform
                    .Find("PlayerBuildHud.Panel/PlayerBuildHud.StatsBlock/PlayerBuildHud.Stat.MeleeDamage/Value")
                    .GetComponent<Text>();
                var karma = canvasObject.transform
                    .Find("PlayerBuildHud.Panel/PlayerBuildHud.StatsBlock/PlayerBuildHud.Stat.Karma/Value")
                    .GetComponent<Text>();
                Assert.AreEqual("5", coinsAmount.text);
                Assert.AreEqual("2", keysAmount.text);
                Assert.AreEqual("12", soulsAmount.text);
                Assert.AreEqual("3/4", meleeDamage.text);
                Assert.AreEqual("0", karma.text);
                var goldKeySprite = keysIcon.sprite;
                var renderedHeartCount = controller.RenderedHeartCount;
                var renderedStaminaFill = controller.RenderedStaminaFillAmount;

                controller.RefreshFromModel(CreateHudModel(
                    currentStamina: 80f,
                    maxStamina: 100f,
                    souls: 27,
                    coins: 19,
                    keys: 2,
                    hasBossKey: true,
                    meleeLightDamage: 7,
                    meleeHeavyDamage: 12,
                    karma: 2));

                Assert.AreEqual("19", coinsAmount.text);
                Assert.AreEqual("1", keysAmount.text);
                Assert.AreEqual("27", soulsAmount.text);
                Assert.AreEqual("7/12", meleeDamage.text);
                Assert.AreEqual("+2", karma.text);
                Assert.AreEqual(19, controller.RenderedCoins);
                Assert.AreEqual(1, controller.RenderedKeys);
                Assert.IsTrue(controller.RenderedHasBossKey);
                Assert.AreNotSame(goldKeySprite, keysIcon.sprite);
                Assert.AreEqual(27, controller.RenderedSouls);
                Assert.AreEqual(renderedHeartCount, controller.RenderedHeartCount);
                Assert.AreEqual(renderedStaminaFill, controller.RenderedStaminaFillAmount, 0.001f);

                controller.RefreshFromModel(CreateHudModel(currentStamina: 80f, maxStamina: 100f, souls: 27, coins: 19, keys: 2));

                Assert.AreEqual("2", keysAmount.text);
                Assert.AreEqual(2, controller.RenderedKeys);
                Assert.IsFalse(controller.RenderedHasBossKey);
                Assert.AreSame(goldKeySprite, keysIcon.sprite);
            }
            finally
            {
                Object.DestroyImmediate(canvasObject);
            }
        }

        [Test]
        public void PlayerBuildHudStaminaFillClampsToCurrentStaminaRatio()
        {
            var canvasObject = new GameObject("HudCanvas", typeof(Canvas), typeof(PlayerBuildHudController));
            try
            {
                var controller = canvasObject.GetComponent<PlayerBuildHudController>();

                controller.RefreshFromModel(CreateHudModel(currentStamina: 0f, maxStamina: 100f));
                Assert.AreEqual(0f, controller.RenderedStaminaFillAmount, 0.001f);

                controller.RefreshFromModel(CreateHudModel(currentStamina: 150f, maxStamina: 100f));
                Assert.AreEqual(1f, controller.RenderedStaminaFillAmount, 0.001f);

                controller.RefreshFromModel(CreateHudModel(currentStamina: 50f, maxStamina: 0f));
                Assert.AreEqual(0f, controller.RenderedStaminaFillAmount, 0.001f);
            }
            finally
            {
                Object.DestroyImmediate(canvasObject);
            }
        }

        [Test]
        public void ReplacementDetectorCapturesOldWeaponForSwapDrop()
        {
            var build = new PlayerRunBuild();
            build.Equipment.EquipMeleeWeapon("starter_blade");
            var incoming = new RewardGrant("origin", "skeletal_sword", "Skeletal Sword", RewardKind.Weapon, 0);

            var replacement = RewardReplacementDetector.CaptureBeforeApply(
                incoming,
                build,
                null,
                null,
                null,
                new Vector3(1f, 0.35f, 2f));

            Assert.IsNotNull(replacement);
            Assert.AreEqual(RewardKind.Weapon, replacement.RewardKind);
            Assert.AreEqual("starter_blade", replacement.RewardId);
            Assert.AreEqual("origin", replacement.RoomId);
        }

        [Test]
        public void PlayerBuildHudStatCalculatorUsesWeaponBasesBuildBonusesAndProjectileFireRate()
        {
            var meleeWeapon = ScriptableObject.CreateInstance<WeaponDefinition>();
            var rangedWeapon = ScriptableObject.CreateInstance<WeaponDefinition>();
            var catalog = ScriptableObject.CreateInstance<WeaponCatalogDefinition>();
            try
            {
                meleeWeapon.Configure(
                    "starter_blade",
                    "Starter Blade",
                    WeaponSlot.Melee,
                    WeaponCategory.Blade,
                    nextLightAttack: new WeaponAttackDefinition(AttackKind.Light, 2, 0.5f, 10f, 1f),
                    nextHeavyAttack: new WeaponAttackDefinition(AttackKind.Heavy, 4, 1f, 20f, 1.2f));
                rangedWeapon.Configure(
                    "starter_pistol",
                    "Basic Pistol",
                    WeaponSlot.Ranged,
                    WeaponCategory.Gun,
                    nextLightAttack: new WeaponAttackDefinition(AttackKind.Light, 1, 0.5f, 6f, 6.5f),
                    nextHeavyAttack: new WeaponAttackDefinition(AttackKind.Heavy, 2, 1f, 20f, 7f));
                catalog.Configure("test_weapons", new[] { meleeWeapon, rangedWeapon });

                var build = new PlayerRunBuild();
                build.Equipment.EquipMeleeWeapon("starter_blade");
                build.Equipment.EquipRangedWeapon("starter_pistol");
                build.AddModifier(new PlayerStatModifier
                {
                    sourceId = "hud_test",
                    speed = 0.5f,
                    strength = 1,
                    meleeDamage = 2,
                    rangedDamage = 1,
                    attackCooldownMultiplier = 1f,
                    rangedRangeBonusMeters = 1f
                });
                build.Inventory.AddPassiveItem(ProjectilePassiveResolver.FireRateUpId);

                var stats = PlayerBuildHudStatCalculator.Calculate(build, catalog, null, null);

                Assert.AreEqual(6, stats.MeleeLightDamage);
                Assert.AreEqual(8, stats.MeleeHeavyDamage);
                Assert.AreEqual(2, stats.RangedLightDamage);
                Assert.AreEqual(3, stats.RangedHeavyDamage);
                Assert.AreEqual(2f, stats.MeleeLightAttacksPerSecond, 0.001f);
                Assert.Greater(stats.RangedLightAttacksPerSecond, 2f);
                Assert.AreEqual(7.5f, stats.EffectiveRangeMeters, 0.001f);
                Assert.AreEqual(0, stats.Karma);
            }
            finally
            {
                Object.DestroyImmediate(meleeWeapon);
                Object.DestroyImmediate(rangedWeapon);
                Object.DestroyImmediate(catalog);
            }
        }

        private static void AssertStatRow(Transform statsBlock, string statName, string expectedValue)
        {
            var row = statsBlock.Find($"PlayerBuildHud.Stat.{statName}");
            Assert.IsNotNull(row);
            var icon = row.Find("Icon")?.GetComponent<Image>();
            var value = row.Find("Value")?.GetComponent<Text>();
            Assert.IsNotNull(icon);
            Assert.IsNotNull(value);
            Assert.IsNotNull(icon.sprite);
            Assert.AreEqual(expectedValue, value.text);
        }

        private static PlayerBuildHudModel CreateHudModel(
            float currentStamina,
            float maxStamina,
            int souls = 0,
            int coins = 0,
            int keys = 0,
            bool hasBossKey = false,
            int meleeLightDamage = 3,
            int meleeHeavyDamage = 4,
            float meleeLightAttacksPerSecond = 1.5f,
            int rangedLightDamage = 1,
            int rangedHeavyDamage = 2,
            float rangedLightAttacksPerSecond = 2f,
            float effectiveRangeMeters = 6.5f,
            float moveSpeedMetersPerSecond = 4f,
            int karma = 0,
            string activeWeaponId = "starter_blade",
            WeaponSlot activeWeaponSlot = WeaponSlot.Melee,
            string activeItemId = "",
            int activeItemCharges = 0,
            int activeItemMaxCharges = 0,
            string consumableCardId = "")
        {
            return new PlayerBuildHudModel(
                "Balanced",
                3,
                5,
                1,
                1,
                false,
                4f,
                1,
                currentStamina,
                maxStamina,
                18f,
                0,
                0,
                0f,
                0f,
                1f,
                coins,
                souls,
                keys,
                hasBossKey,
                meleeLightDamage,
                meleeHeavyDamage,
                meleeLightAttacksPerSecond,
                rangedLightDamage,
                rangedHeavyDamage,
                rangedLightAttacksPerSecond,
                effectiveRangeMeters,
                moveSpeedMetersPerSecond,
                karma,
                activeWeaponId,
                activeWeaponSlot,
                activeItemId,
                activeItemCharges,
                activeItemMaxCharges,
                consumableCardId,
                "Melee - Practice Blade",
                "Practice Blade",
                "Basic Pistol",
                "None",
                "Starter Buckler",
                PlayerEquipmentLoadState.Default,
                "None",
                "None",
                "None");
        }

        [Test]
        public void ReplacementPickupSaveStateRoundTrips()
        {
            var state = new ReplacementPickupState(
                "pickup_01",
                "room_01",
                RewardKind.ActiveItem,
                "mending_charm",
                "Mending Charm",
                2,
                new Vector3(1f, 0.35f, -2f));

            var restored = ReplacementPickupState.FromSaveState(state.ToSaveState());

            Assert.IsNotNull(restored);
            Assert.AreEqual("pickup_01", restored.PickupId);
            Assert.AreEqual(RewardKind.ActiveItem, restored.RewardKind);
            Assert.AreEqual(2, restored.ActiveItemCharges);
            Assert.AreEqual(-2f, restored.LocalPosition.z, 0.001f);
        }

        [Test]
        public void RunSnapshotStoresDroppedReplacementPickups()
        {
            var snapshot = new RunSaveSnapshot();
            snapshot.droppedReplacementPickups.Add(new DroppedReplacementPickupSaveState
            {
                pickupId = "pickup_weapon",
                roomId = "origin",
                rewardKind = RewardKind.Weapon.ToString(),
                rewardId = "starter_bolt",
                displayName = "Starter Bolt"
            });

            Assert.AreEqual(1, snapshot.droppedReplacementPickups.Count);
            Assert.AreEqual("starter_bolt", snapshot.droppedReplacementPickups[0].rewardId);
        }

        [Test]
        public void PickupRevealModelIncludesReplacementLine()
        {
            var reveal = new PickupRevealModel(
                1,
                "Dragon Pistol",
                "Ranged Weapon",
                "Light 2 dmg / Heavy 4 dmg",
                "Epic",
                "W",
                Color.magenta,
                "Dropped old: Starter Bolt",
                "Dragon Pistol equipped");

            Assert.IsTrue(reveal.BodyText.Contains("Dragon Pistol"));
            Assert.IsTrue(reveal.BodyText.Contains("Dropped old: Starter Bolt"));
            Assert.IsFalse(reveal.IsEmpty);
        }
    }
}
