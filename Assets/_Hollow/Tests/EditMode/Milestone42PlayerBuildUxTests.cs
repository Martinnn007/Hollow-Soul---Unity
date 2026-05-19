using Hollow.Data.Definitions;
using Hollow.Persistence;
using Hollow.Rewards;
using Hollow.UI.Shell;
using NUnit.Framework;
using UnityEngine;

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
        }

        [Test]
        public void PlayerBuildHudRendersAvatarAndHeartContainers()
        {
            var canvasObject = new GameObject("HudCanvas", typeof(Canvas), typeof(PlayerBuildHudController));
            try
            {
                var controller = canvasObject.GetComponent<PlayerBuildHudController>();
                var model = new PlayerBuildHudModel(
                    "Balanced",
                    3,
                    5,
                    1,
                    false,
                    4f,
                    1,
                    80f,
                    100f,
                    18f,
                    0,
                    0,
                    0f,
                    0f,
                    1f,
                    0,
                    0,
                    "Melee - Practice Blade",
                    "Practice Blade",
                    "Practice Bow",
                    "None",
                    "None",
                    "None",
                    "None");

                controller.RefreshFromModel(model);

                Assert.AreEqual(5, controller.RenderedHeartCount);
                Assert.AreEqual(3, controller.RenderedFullHeartCount);
                Assert.IsTrue(controller.HasRenderedStaminaBar);
                Assert.AreEqual(0.8f, controller.RenderedStaminaFillAmount, 0.001f);
                Assert.IsNotNull(canvasObject.transform.Find("PlayerBuildHud.Panel/PlayerBuildHud.Avatar"));
                Assert.IsNotNull(canvasObject.transform.Find("PlayerBuildHud.Panel/PlayerBuildHud.Heart_01"));
                Assert.IsNotNull(canvasObject.transform.Find("PlayerBuildHud.Panel/PlayerBuildHud.StaminaBar"));
                Assert.IsNotNull(canvasObject.transform.Find("PlayerBuildHud.Panel/PlayerBuildHud.StaminaBar/PlayerBuildHud.StaminaFrame"));
                Assert.IsNotNull(canvasObject.transform.Find("PlayerBuildHud.Panel/PlayerBuildHud.StaminaBar/PlayerBuildHud.StaminaFill"));
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

        private static PlayerBuildHudModel CreateHudModel(float currentStamina, float maxStamina)
        {
            return new PlayerBuildHudModel(
                "Balanced",
                3,
                5,
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
                0,
                0,
                "Melee - Practice Blade",
                "Practice Blade",
                "Practice Bow",
                "None",
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
                "Dragon Bow",
                "Ranged Weapon",
                "Light 2 dmg / Heavy 4 dmg",
                "Epic",
                "W",
                Color.magenta,
                "Dropped old: Starter Bolt",
                "Dragon Bow equipped");

            Assert.IsTrue(reveal.BodyText.Contains("Dragon Bow"));
            Assert.IsTrue(reveal.BodyText.Contains("Dropped old: Starter Bolt"));
            Assert.IsFalse(reveal.IsEmpty);
        }
    }
}
