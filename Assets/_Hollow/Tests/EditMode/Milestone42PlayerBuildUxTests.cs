using Hollow.Data.Definitions;
using Hollow.Persistence;
using Hollow.Rewards;
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
