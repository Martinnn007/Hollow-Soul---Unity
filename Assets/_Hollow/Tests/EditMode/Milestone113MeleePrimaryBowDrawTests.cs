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
        public void StarterBowIsDefaultDrawAndReleaseRangedWeapon()
        {
            var catalog = LoadWeaponCatalog();

            Assert.IsTrue(catalog.TryGetWeapon("starter_bow", out var bow));
            Assert.AreEqual("Practice Bow", bow.DisplayName);
            Assert.AreEqual(WeaponSlot.Ranged, bow.Slot);
            Assert.AreEqual(WeaponCategory.Bow, bow.Category);
            Assert.AreEqual(WeaponRangedFireMode.DrawAndRelease, bow.RangedFireMode);
            Assert.AreEqual(1f, bow.LightAttack.RequiredDrawSeconds, 0.001f);
            Assert.Greater(bow.HeavyAttack.RequiredDrawSeconds, bow.LightAttack.RequiredDrawSeconds);

            Assert.IsTrue(catalog.TryGetWeapon("starter_bolt", out var bolt));
            Assert.AreEqual(WeaponRangedFireMode.Instant, bolt.RangedFireMode);
        }

        [Test]
        public void FreshRunEquipmentStartsMeleePrimaryWithStarterBowFallback()
        {
            var build = new PlayerRunBuild();
            var saved = RunEquipmentSlots.FromSaveState(new Hollow.Persistence.RunEquipmentSlotsSaveState
            {
                meleeWeaponId = "starter_blade",
                rangedWeaponId = string.Empty,
                activeWeaponSlot = string.Empty
            });

            Assert.AreEqual("starter_bow", build.Equipment.RangedWeaponId);
            Assert.AreEqual(WeaponSlot.Melee, build.Equipment.ActiveWeaponSlot);
            Assert.AreEqual("starter_bow", saved.RangedWeaponId);
            Assert.AreEqual(WeaponSlot.Melee, saved.ActiveWeaponSlot);
        }

        [Test]
        public void BowLightRequiresDrawReleaseAndEarlyReleaseCancelsWithoutCost()
        {
            var rig = CreateWeaponRig("BowDrawEarlyRelease");
            try
            {
                var weapon = rig.Weapon;

                Assert.AreEqual(100f, weapon.CurrentStamina, 0.001f);
                Assert.IsTrue(weapon.TryAttack(AttackKind.Light, Vector2.up, 0f));
                Assert.IsTrue(weapon.IsRangedDrawActive);
                Assert.AreEqual(100f, weapon.CurrentStamina, 0.001f);

                Assert.IsFalse(weapon.TryReleaseRangedDraw(Vector2.up, 0.5f));

                Assert.IsFalse(weapon.IsRangedDrawActive);
                Assert.AreEqual(100f, weapon.CurrentStamina, 0.001f);
                Assert.AreEqual(0, CountPlayerProjectiles(rig.Parent));
            }
            finally
            {
                rig.Destroy();
            }
        }

        [Test]
        public void BowLightFiresOnCompletedReleaseUsingReleaseAim()
        {
            var rig = CreateWeaponRig("BowDrawRelease");
            try
            {
                var weapon = rig.Weapon;

                Assert.IsTrue(weapon.TryAttack(AttackKind.Light, Vector2.up, 0f));
                Assert.IsTrue(weapon.TryReleaseRangedDraw(new Vector2(0.6f, 0.8f), 1.01f));
                weapon.TickAction(0f, 1.03f);

                Assert.IsFalse(weapon.IsRangedDrawActive);
                Assert.AreEqual(98f, weapon.CurrentStamina, 0.001f);
                Assert.AreEqual(1, CountPlayerProjectiles(rig.Parent));
                var projectile = rig.Parent.transform.Find("PlayerProjectile");
                Assert.IsNotNull(projectile);
                Assert.Greater(projectile.localPosition.x, 0f);
                Assert.Greater(projectile.localPosition.z, 0f);
            }
            finally
            {
                rig.Destroy();
            }
        }

        [Test]
        public void BowHeavyUsesItsOwnDrawTiming()
        {
            var rig = CreateWeaponRig("BowHeavyDraw");
            try
            {
                var weapon = rig.Weapon;

                Assert.IsTrue(weapon.TryAttack(AttackKind.Heavy, Vector2.up, 0f));
                Assert.IsFalse(weapon.TryReleaseRangedDraw(Vector2.up, 1.1f));
                Assert.AreEqual(100f, weapon.CurrentStamina, 0.001f);

                Assert.IsTrue(weapon.TryAttack(AttackKind.Heavy, Vector2.up, 2f));
                Assert.IsTrue(weapon.TryReleaseRangedDraw(Vector2.up, 3.36f));
                weapon.TickAction(0f, 3.38f);

                Assert.AreEqual(72f, weapon.CurrentStamina, 0.001f);
                Assert.AreEqual(1, CountPlayerProjectiles(rig.Parent));
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

        private static WeaponRig CreateWeaponRig(string name, string rangedWeaponId = "starter_bow")
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
