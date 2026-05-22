using Hollow.Combat;
using Hollow.Data.Definitions;
using Hollow.Editor.Generation;
using Hollow.Rewards;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Hollow.Tests.EditMode
{
    public sealed class Milestone113MeleePrimaryBowDrawTests
    {
        [Test]
        public void StarterPistolIsDefaultInstantRangedWeapon()
        {
            var catalog = LoadWeaponCatalog();

            Assert.IsTrue(catalog.TryGetWeapon("starter_pistol", out var pistol));
            Assert.AreEqual("Basic Pistol", pistol.DisplayName);
            Assert.AreEqual(WeaponSlot.Ranged, pistol.Slot);
            Assert.AreEqual(WeaponCategory.Gun, pistol.Category);
            Assert.AreEqual(WeaponRangedFireMode.Instant, pistol.RangedFireMode);
            Assert.AreEqual(0.5f, pistol.LightAttack.CooldownSeconds, 0.001f);
            Assert.AreEqual(6f, pistol.LightAttack.StaminaCost, 0.001f);
            Assert.AreEqual(0f, pistol.LightAttack.RequiredDrawSeconds, 0.001f);
            Assert.AreEqual(0f, pistol.HeavyAttack.RequiredDrawSeconds, 0.001f);

            Assert.IsTrue(catalog.TryGetWeapon("starter_bolt", out var bolt));
            Assert.AreEqual(WeaponRangedFireMode.Instant, bolt.RangedFireMode);
        }

        [Test]
        public void FreshRunEquipmentStartsMeleePrimaryWithStarterPistolFallback()
        {
            var build = new PlayerRunBuild();
            var saved = RunEquipmentSlots.FromSaveState(new Hollow.Persistence.RunEquipmentSlotsSaveState
            {
                meleeWeaponId = "starter_blade",
                rangedWeaponId = string.Empty,
                activeWeaponSlot = string.Empty
            });

            Assert.AreEqual("starter_pistol", build.Equipment.RangedWeaponId);
            Assert.AreEqual(WeaponSlot.Melee, build.Equipment.ActiveWeaponSlot);
            Assert.AreEqual("starter_pistol", saved.RangedWeaponId);
            Assert.AreEqual(WeaponSlot.Melee, saved.ActiveWeaponSlot);
        }

        [Test]
        public void PistolLightFiresImmediatelyAndDoesNotUseDrawRelease()
        {
            var rig = CreateWeaponRig("PistolLightImmediate");
            try
            {
                var weapon = rig.Weapon;

                Assert.AreEqual(100f, weapon.CurrentStamina, 0.001f);
                Assert.IsTrue(weapon.TryAttack(AttackKind.Light, Vector2.up, 0f));
                Assert.IsFalse(weapon.IsRangedDrawActive);
                Assert.AreEqual(94f, weapon.CurrentStamina, 0.001f);
                Assert.IsFalse(weapon.TryReleaseRangedDraw(Vector2.up, 0.02f));
                weapon.TickAction(0f, 0.02f);

                Assert.IsFalse(weapon.IsRangedDrawActive);
                Assert.AreEqual(1, CountPlayerProjectiles(rig.Parent));
            }
            finally
            {
                rig.Destroy();
            }
        }

        [Test]
        public void PistolLightFiresTwoRoundsPerSecond()
        {
            var rig = CreateWeaponRig("PistolTwoRoundsPerSecond");
            try
            {
                var weapon = rig.Weapon;

                Assert.IsTrue(weapon.TryAttack(AttackKind.Light, Vector2.up, 0f));
                Assert.IsFalse(weapon.TryAttack(AttackKind.Light, Vector2.up, 0.49f));
                Assert.IsTrue(weapon.TryAttack(AttackKind.Light, Vector2.up, 0.5f));
            }
            finally
            {
                rig.Destroy();
            }
        }

        [Test]
        public void PistolHeavyFiresWithoutDrawRelease()
        {
            var rig = CreateWeaponRig("PistolHeavyInstant");
            try
            {
                var weapon = rig.Weapon;

                Assert.IsTrue(weapon.TryAttack(AttackKind.Heavy, Vector2.up, 0f));
                Assert.IsFalse(weapon.IsRangedDrawActive);
                Assert.IsFalse(weapon.TryReleaseRangedDraw(Vector2.up, 0.1f));

                Assert.AreEqual(66f, weapon.CurrentStamina, 0.001f);
                Assert.AreEqual(1, CountPlayerProjectiles(rig.Parent));
            }
            finally
            {
                rig.Destroy();
            }
        }

        [Test]
        public void PistolHeavyDoesNotRearmBlockLightAfterRecovery()
        {
            var rig = CreateWeaponRig("PistolHeavyNoRearm");
            try
            {
                var weapon = rig.Weapon;

                Assert.IsTrue(weapon.TryAttack(AttackKind.Heavy, Vector2.up, 0f));
                Assert.IsFalse(weapon.TryAttack(AttackKind.Light, Vector2.up, 0.36f));
                Assert.IsTrue(weapon.TryAttack(AttackKind.Light, Vector2.up, 0.38f));
            }
            finally
            {
                rig.Destroy();
            }
        }

        [Test]
        public void InstantRangedWeaponsStillFireOnPress()
        {
            var rig = CreateWeaponRig("InstantBolt", "starter_bolt");
            try
            {
                var weapon = rig.Weapon;

                Assert.IsTrue(weapon.TryFire(Vector2.up, 0f));
                Assert.IsFalse(weapon.IsRangedDrawActive);
                weapon.TickAction(0f, 0.07f);

                Assert.AreEqual(1, CountPlayerProjectiles(rig.Parent));
            }
            finally
            {
                rig.Destroy();
            }
        }

        private static WeaponRig CreateWeaponRig(string name, string rangedWeaponId = "starter_pistol")
        {
            var parent = new GameObject(name);
            var player = new GameObject("Player");
            var combat = new GameObject("Combat").AddComponent<RoomCombatController>();
            var projectilePrefab = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            projectilePrefab.AddComponent<ProjectileController>();
            player.transform.SetParent(parent.transform, false);

            var weapon = player.AddComponent<PlayerWeaponController>();
            weapon.Configure(null, combat, projectilePrefab);
            weapon.ConfigureBuildStats(
                1f,
                0,
                0,
                100f,
                0f,
                "starter_blade",
                rangedWeaponId,
                WeaponSlot.Ranged,
                100f,
                LoadWeaponCatalog());
            return new WeaponRig(parent, player, combat.gameObject, projectilePrefab, weapon);
        }

        private static int CountPlayerProjectiles(GameObject parent)
        {
            var count = 0;
            foreach (Transform child in parent.transform)
            {
                if (child.name == "PlayerProjectile")
                {
                    count++;
                }
            }

            return count;
        }

        private static WeaponCatalogDefinition LoadWeaponCatalog()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<WeaponCatalogDefinition>(Milestone27AssetGenerator.WeaponCatalogPath);
            Assert.IsNotNull(catalog, "Run M27 generation before validating M113 weapon defaults.");
            return catalog;
        }

        private readonly struct WeaponRig
        {
            public WeaponRig(GameObject parent, GameObject player, GameObject combat, GameObject projectilePrefab, PlayerWeaponController weapon)
            {
                Parent = parent;
                Player = player;
                Combat = combat;
                ProjectilePrefab = projectilePrefab;
                Weapon = weapon;
            }

            public GameObject Parent { get; }

            public GameObject Player { get; }

            public GameObject Combat { get; }

            public GameObject ProjectilePrefab { get; }

            public PlayerWeaponController Weapon { get; }

            public void Destroy()
            {
                Object.DestroyImmediate(Parent);
                Object.DestroyImmediate(Combat);
                Object.DestroyImmediate(ProjectilePrefab);
            }
        }
    }
}
