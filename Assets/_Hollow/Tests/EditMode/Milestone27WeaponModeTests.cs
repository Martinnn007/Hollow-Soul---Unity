using System.Linq;
using System.Reflection;
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
            Assert.IsTrue(catalog.TryGetWeapon("starter_pistol", out var starterPistol));
            Assert.AreEqual(WeaponSlot.Ranged, starterPistol.Slot);
            Assert.AreEqual(WeaponCategory.Gun, starterPistol.Category);
            Assert.AreEqual(WeaponRangedFireMode.Instant, starterPistol.RangedFireMode);
            Assert.IsFalse(catalog.Weapons.Any(weapon => weapon.WeaponId == "starter_bow"));
            Assert.IsTrue(catalog.TryGetWeapon("starter_bolt", out var starterBolt));
            Assert.AreEqual(WeaponSlot.Ranged, starterBolt.Slot);
            Assert.AreEqual(WeaponRangedFireMode.Instant, starterBolt.RangedFireMode);
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
            Assert.AreEqual(14f, starterBlade.LightAttack.StaminaCost, 0.001f);
            Assert.AreEqual(3.5f, starterBlade.HeavyAttack.CooldownSeconds, 0.001f);
            Assert.AreEqual(42f, starterBlade.HeavyAttack.StaminaCost, 0.001f);

            Assert.IsTrue(catalog.TryGetWeapon("starter_bolt", out var starterBolt));
            Assert.AreEqual(1f, starterBolt.LightAttack.CooldownSeconds, 0.001f);
            Assert.AreEqual(8f, starterBolt.LightAttack.StaminaCost, 0.001f);
            Assert.AreEqual(10f, starterBolt.HeavyAttack.CooldownSeconds, 0.001f);
            Assert.AreEqual(36f, starterBolt.HeavyAttack.StaminaCost, 0.001f);

            Assert.IsTrue(catalog.TryGetWeapon("starter_pistol", out var starterPistol));
            Assert.AreEqual(0.5f, starterPistol.LightAttack.CooldownSeconds, 0.001f);
            Assert.AreEqual(0f, starterPistol.LightAttack.RequiredDrawSeconds, 0.001f);
            Assert.AreEqual(0f, starterPistol.HeavyAttack.RequiredDrawSeconds, 0.001f);
            Assert.AreEqual(6f, starterPistol.LightAttack.StaminaCost, 0.001f);
            Assert.AreEqual(34f, starterPistol.HeavyAttack.StaminaCost, 0.001f);

            Assert.IsTrue(catalog.TryGetWeapon("iron_cleaver", out var ironCleaver));
            Assert.AreEqual(18f, ironCleaver.LightAttack.StaminaCost, 0.001f);
            Assert.AreEqual(50f, ironCleaver.HeavyAttack.StaminaCost, 0.001f);

            Assert.IsTrue(catalog.TryGetWeapon("ember_bolt", out var emberBolt));
            Assert.AreEqual(10f, emberBolt.LightAttack.StaminaCost, 0.001f);
            Assert.AreEqual(40f, emberBolt.HeavyAttack.StaminaCost, 0.001f);
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
                Assert.AreEqual(7.25f, weapon.EffectiveRangedLightRangeMeters, 0.001f);
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
        public void DebugLightAttackSpeedDoublesRangedLightCadenceOnly()
        {
            var parent = new GameObject("DebugAttackSpeedRangedParent");
            var player = new GameObject("Player");
            var combat = new GameObject("Combat").AddComponent<RoomCombatController>();
            var projectilePrefab = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            projectilePrefab.AddComponent<ProjectileController>();
            player.transform.SetParent(parent.transform, false);
            try
            {
                var weapon = player.AddComponent<PlayerWeaponController>();
                weapon.Configure(null, combat, projectilePrefab);

                Assert.IsFalse(weapon.DebugLightAttackSpeedDoubled);
                Assert.IsTrue(weapon.TryFire(Vector2.up, 0f));
                Assert.IsFalse(weapon.TryFire(Vector2.up, 0.49f));
                Assert.IsTrue(weapon.TryFire(Vector2.up, 0.5f));

                var fastPlayer = new GameObject("FastPlayer");
                fastPlayer.transform.SetParent(parent.transform, false);
                var fastWeapon = fastPlayer.AddComponent<PlayerWeaponController>();
                fastWeapon.Configure(null, combat, projectilePrefab);
                fastWeapon.SetDebugLightAttackSpeedDoubled(true);

                Assert.IsTrue(fastWeapon.DebugLightAttackSpeedDoubled);
                Assert.IsTrue(fastWeapon.TryFire(Vector2.up, 0f));
                Assert.IsFalse(fastWeapon.TryFire(Vector2.up, 0.24f));
                Assert.IsTrue(fastWeapon.TryFire(Vector2.up, 0.25f));

                fastWeapon.SetActiveWeaponSlot(WeaponSlot.Ranged);
                Assert.IsTrue(fastWeapon.TryAttack(AttackKind.Heavy, Vector2.up, 10f));
                Assert.IsFalse(fastWeapon.TryAttack(AttackKind.Heavy, Vector2.up, 10.36f));
                Assert.IsTrue(fastWeapon.TryAttack(AttackKind.Heavy, Vector2.up, 10.38f));
                Assert.IsFalse(fastWeapon.TryAttack(AttackKind.Heavy, Vector2.up, 10.39f));
            }
            finally
            {
                Object.DestroyImmediate(parent);
                Object.DestroyImmediate(combat.gameObject);
                Object.DestroyImmediate(projectilePrefab);
            }
        }

        [Test]
        public void DebugLightAttackSpeedHalvesFinalPassiveAdjustedRangedCooldown()
        {
            var parent = new GameObject("DebugAttackSpeedPassiveParent");
            var player = new GameObject("Player");
            var combat = new GameObject("Combat").AddComponent<RoomCombatController>();
            var projectilePrefab = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            projectilePrefab.AddComponent<ProjectileController>();
            player.transform.SetParent(parent.transform, false);
            try
            {
                var weapon = player.AddComponent<PlayerWeaponController>();
                weapon.Configure(null, combat, projectilePrefab);
                weapon.ConfigureBuildStats(
                    2f,
                    0,
                    0,
                    100f,
                    18f,
                    "starter_blade",
                    "starter_bolt",
                    WeaponSlot.Ranged,
                    100f);
                weapon.ConfigureProjectilePassives(new ProjectilePassiveState(ProjectilePatternKind.Single, 1f, 1f, ProjectileVisualStyle.Default));
                weapon.SetDebugLightAttackSpeedDoubled(true);

                Assert.IsTrue(weapon.TryFire(Vector2.up, 0f));
                Assert.IsFalse(weapon.TryFire(Vector2.up, 0.49f));
                Assert.IsTrue(weapon.TryFire(Vector2.up, 0.5f));
            }
            finally
            {
                Object.DestroyImmediate(parent);
                Object.DestroyImmediate(combat.gameObject);
                Object.DestroyImmediate(projectilePrefab);
            }
        }

        [Test]
        public void DebugLightAttackSpeedDoublesMeleeLightCadenceOnlyAndKeepsStaminaCost()
        {
            var parent = new GameObject("DebugAttackSpeedMeleeParent");
            var player = new GameObject("Player");
            var combat = new GameObject("Combat").AddComponent<RoomCombatController>();
            player.transform.SetParent(parent.transform, false);
            try
            {
                var weapon = player.AddComponent<PlayerWeaponController>();
                weapon.Configure(null, combat, null);
                weapon.ConfigureBuildStats(1f, 0, 0, 200f, 11f, "starter_blade", "starter_pistol", WeaponSlot.Melee, 200f, LoadCatalog());
                weapon.SetActiveWeaponSlot(WeaponSlot.Melee);
                weapon.SetDebugLightAttackSpeedDoubled(true);

                Assert.AreEqual(200f, weapon.CurrentStamina, 0.001f);
                Assert.IsTrue(weapon.TryAttack(AttackKind.Light, Vector2.up, 0f));
                Assert.AreEqual(186f, weapon.CurrentStamina, 0.001f);
                Assert.IsFalse(weapon.TryAttack(AttackKind.Light, Vector2.up, 0.32f));
                Assert.IsTrue(weapon.TryAttack(AttackKind.Light, Vector2.up, 0.335f));
                Assert.AreEqual(172f, weapon.CurrentStamina, 0.001f);

                Assert.IsTrue(weapon.TryAttack(AttackKind.Heavy, Vector2.up, 5f));
                Assert.IsTrue(weapon.TryAttack(AttackKind.Heavy, Vector2.up, 5.71f));
                Assert.IsFalse(weapon.TryAttack(AttackKind.Heavy, Vector2.up, 5.72f));
            }
            finally
            {
                Object.DestroyImmediate(parent);
                Object.DestroyImmediate(combat.gameObject);
            }
        }

        [Test]
        public void DebugSpawnMenuAppliesCurrentRunLightAttackSpeedToggle()
        {
            EnemyNavigationDebugOverlay.ResetDiagnostics();
            var branchObject = new GameObject("BranchSession");
            var playerObject = new GameObject("Player");
            try
            {
                var branch = branchObject.AddComponent<BranchSessionController>();
                playerObject.AddComponent<Hollow.Entities.PlaceholderPlayerController>();
                var weapon = playerObject.AddComponent<PlayerWeaponController>();
                SetBranchPlayer(branch, playerObject.GetComponent<Hollow.Entities.PlaceholderPlayerController>());

                var menu = branchObject.AddComponent<DebugSpawnMenuController>();
                menu.Bind(branch);

                Assert.IsFalse(menu.DebugLightAttackSpeedDoubled);
                Assert.IsFalse(weapon.DebugLightAttackSpeedDoubled);
                Assert.IsFalse(menu.DebugEnemyPathTracingEnabled);
                Assert.IsFalse(EnemyNavigationDebugOverlay.PathTracingEnabled);
                Assert.IsFalse(menu.DebugEnemyAiBlackboardEnabled);
                Assert.IsFalse(EnemyAiDebugOverlay.BlackboardEnabled);
                StringAssert.Contains("req/s", EnemyNavigationDebugOverlay.DiagnosticsSummary);
                StringAssert.Contains("AI blackboard", EnemyAiDebugOverlay.DiagnosticsSummary);

                menu.SetDebugLightAttackSpeedDoubled(true);

                Assert.IsTrue(menu.DebugLightAttackSpeedDoubled);
                Assert.IsTrue(weapon.DebugLightAttackSpeedDoubled);

                menu.SetDebugEnemyPathTracingEnabled(true);
                Assert.IsTrue(menu.DebugEnemyPathTracingEnabled);
                Assert.IsTrue(EnemyNavigationDebugOverlay.PathTracingEnabled);

                menu.SetDebugEnemyPathTracingEnabled(false);
                Assert.IsFalse(menu.DebugEnemyPathTracingEnabled);
                Assert.IsFalse(EnemyNavigationDebugOverlay.PathTracingEnabled);
                menu.SetDebugEnemyAiBlackboardEnabled(true);
                Assert.IsTrue(menu.DebugEnemyAiBlackboardEnabled);
                Assert.IsTrue(EnemyAiDebugOverlay.BlackboardEnabled);

                menu.SetDebugEnemyAiBlackboardEnabled(false);
                Assert.IsFalse(menu.DebugEnemyAiBlackboardEnabled);
                Assert.IsFalse(EnemyAiDebugOverlay.BlackboardEnabled);

                var nextMenu = new GameObject("NextDebugSpawnMenu").AddComponent<DebugSpawnMenuController>();
                Assert.IsFalse(nextMenu.DebugLightAttackSpeedDoubled);
                Assert.IsFalse(nextMenu.DebugEnemyPathTracingEnabled);
                Assert.IsFalse(nextMenu.DebugEnemyAiBlackboardEnabled);
                Object.DestroyImmediate(nextMenu.gameObject);
            }
            finally
            {
                Object.DestroyImmediate(branchObject);
                Object.DestroyImmediate(playerObject);
            }
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
        public void LegacyBowWeaponIdsNormalizeToPistolIds()
        {
            var catalog = LoadCatalog();
            Assert.IsTrue(catalog.TryGetWeapon("starter_bow", out var starterWeapon));
            Assert.AreEqual("starter_pistol", starterWeapon.WeaponId);
            Assert.IsTrue(catalog.TryGetWeapon("bone_bow", out var boneWeapon));
            Assert.AreEqual("bone_pistol", boneWeapon.WeaponId);
            Assert.IsTrue(catalog.TryGetWeapon("dragon_bow", out var dragonWeapon));
            Assert.AreEqual("dragon_pistol", dragonWeapon.WeaponId);

            var restored = RunEquipmentSlots.FromSaveState(new Hollow.Persistence.RunEquipmentSlotsSaveState
            {
                meleeWeaponId = "starter_blade",
                rangedWeaponId = "dragon_bow",
                activeWeaponSlot = WeaponSlot.Ranged.ToString()
            });
            Assert.AreEqual("dragon_pistol", restored.RangedWeaponId);
            Assert.AreEqual("dragon_pistol", restored.ToSaveState().rangedWeaponId);

            var pickup = ReplacementPickupState.FromSaveState(new Hollow.Persistence.DroppedReplacementPickupSaveState
            {
                pickupId = "legacy_pickup",
                roomId = "room",
                rewardKind = RewardKind.Weapon.ToString(),
                rewardId = "bone_bow",
                displayName = "Bone Bow"
            });
            Assert.IsNotNull(pickup);
            Assert.AreEqual("bone_pistol", pickup.RewardId);
            Assert.AreEqual("Bone Pistol", pickup.DisplayName);

            var build = new PlayerRunBuild();
            build.Equipment.EquipRangedWeapon("bone_pistol");
            var replacement = RewardReplacementDetector.CaptureBeforeApply(
                new RewardGrant("legacy_reward", "bone_bow", "Bone Bow", RewardKind.Weapon, 0),
                build,
                catalog,
                null,
                null,
                Vector3.zero);
            Assert.IsNull(replacement);
        }

        [Test]
        public void MeleeHeavyDoesNotRearmBlockLightAfterRecovery()
        {
            var parent = new GameObject("MeleeHeavyNoRearmParent");
            var player = new GameObject("Player");
            var combat = new GameObject("Combat").AddComponent<RoomCombatController>();
            player.transform.SetParent(parent.transform, false);
            try
            {
                var weapon = player.AddComponent<PlayerWeaponController>();
                weapon.Configure(null, combat, null);
                weapon.ConfigureBuildStats(1f, 0, 0, 200f, 11f, "starter_blade", "starter_pistol", WeaponSlot.Melee, 200f, LoadCatalog());

                Assert.IsTrue(weapon.TryAttack(AttackKind.Heavy, Vector2.up, 0f));
                Assert.IsFalse(weapon.TryAttack(AttackKind.Light, Vector2.up, 0.69f));
                Assert.IsTrue(weapon.TryAttack(AttackKind.Light, Vector2.up, 0.71f));
            }
            finally
            {
                Object.DestroyImmediate(parent);
                Object.DestroyImmediate(combat.gameObject);
            }
        }

        [Test]
        public void RangedHeavyDoesNotRearmBlockLightAfterRecovery()
        {
            var parent = new GameObject("RangedHeavyNoRearmParent");
            var player = new GameObject("Player");
            var combat = new GameObject("Combat").AddComponent<RoomCombatController>();
            var projectilePrefab = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            projectilePrefab.AddComponent<ProjectileController>();
            player.transform.SetParent(parent.transform, false);
            try
            {
                var weapon = player.AddComponent<PlayerWeaponController>();
                weapon.Configure(null, combat, projectilePrefab);
                weapon.ConfigureBuildStats(1f, 0, 0, 200f, 11f, "starter_blade", "starter_bolt", WeaponSlot.Ranged, 200f, LoadCatalog());

                Assert.IsTrue(weapon.TryAttack(AttackKind.Heavy, Vector2.up, 0f));
                Assert.IsFalse(weapon.TryAttack(AttackKind.Light, Vector2.up, 0.67f));
                Assert.IsTrue(weapon.TryAttack(AttackKind.Light, Vector2.up, 0.69f));
            }
            finally
            {
                Object.DestroyImmediate(parent);
                Object.DestroyImmediate(combat.gameObject);
                Object.DestroyImmediate(projectilePrefab);
            }
        }

        [Test]
        public void HeldMeleeLightRepeatsOnlyAfterCooldownAllows()
        {
            var rig = CreateHeldAttackRig("HeldMeleeLight", WeaponSlot.Melee);
            try
            {
                rig.Weapon.TickInput(AttackInput(lightPressed: true, lightHeld: true), 0f, 0f);
                Assert.AreEqual(186f, rig.Weapon.CurrentStamina, 0.001f);

                rig.Weapon.TickInput(AttackInput(lightHeld: true), 0f, 0.24f);
                Assert.AreEqual(186f, rig.Weapon.CurrentStamina, 0.001f);

                rig.Weapon.TickInput(AttackInput(lightHeld: true), 0f, 0.67f);
                Assert.AreEqual(172f, rig.Weapon.CurrentStamina, 0.001f);
            }
            finally
            {
                rig.Destroy();
            }
        }

        [Test]
        public void HeldMeleeHeavyRepeatsAfterRecoveryAllows()
        {
            var rig = CreateHeldAttackRig("HeldMeleeHeavy", WeaponSlot.Melee);
            try
            {
                rig.Weapon.TickInput(AttackInput(heavyPressed: true, heavyHeld: true), 0f, 0f);
                Assert.AreEqual(158f, rig.Weapon.CurrentStamina, 0.001f);

                rig.Weapon.TickInput(AttackInput(heavyHeld: true), 0f, 0.69f);
                Assert.AreEqual(158f, rig.Weapon.CurrentStamina, 0.001f);

                rig.Weapon.TickInput(AttackInput(heavyHeld: true), 0f, 0.71f);
                Assert.AreEqual(116f, rig.Weapon.CurrentStamina, 0.001f);
            }
            finally
            {
                rig.Destroy();
            }
        }

        [Test]
        public void HeldAttacksUseLatestPressedPriorityAndFallbackOnRelease()
        {
            var simultaneousRig = CreateHeldAttackRig("HeldSimultaneousPriority", WeaponSlot.Melee);
            try
            {
                simultaneousRig.Weapon.TickInput(
                    AttackInput(lightPressed: true, lightHeld: true, heavyPressed: true, heavyHeld: true),
                    0f,
                    0f);
                Assert.AreEqual(158f, simultaneousRig.Weapon.CurrentStamina, 0.001f);
            }
            finally
            {
                simultaneousRig.Destroy();
            }

            var rig = CreateHeldAttackRig("HeldPriorityFallback", WeaponSlot.Melee);
            try
            {
                rig.Weapon.TickInput(AttackInput(lightPressed: true, lightHeld: true), 0f, 0f);
                Assert.AreEqual(186f, rig.Weapon.CurrentStamina, 0.001f);

                rig.Weapon.TickInput(AttackInput(heavyPressed: true, lightHeld: true, heavyHeld: true), 0f, 0.67f);
                Assert.AreEqual(144f, rig.Weapon.CurrentStamina, 0.001f);

                rig.Weapon.TickInput(AttackInput(lightHeld: true, heavyReleased: true), 0f, 1.38f);
                Assert.AreEqual(130f, rig.Weapon.CurrentStamina, 0.001f);
            }
            finally
            {
                rig.Destroy();
            }
        }

        [Test]
        public void HeldInstantRangedLightRepeatsOnlyAfterCooldownAllows()
        {
            var rig = CreateHeldAttackRig("HeldRangedLight", WeaponSlot.Ranged, "starter_pistol");
            try
            {
                rig.Weapon.TickInput(AttackInput(lightPressed: true, lightHeld: true), 0f, 0f);
                Assert.AreEqual(194f, rig.Weapon.CurrentStamina, 0.001f);
                rig.Weapon.TickAction(0f, 0.02f);
                Assert.AreEqual(1, CountPlayerProjectiles(rig.Parent));

                rig.Weapon.TickInput(AttackInput(lightHeld: true), 0f, 0.2f);
                Assert.AreEqual(194f, rig.Weapon.CurrentStamina, 0.001f);
                Assert.AreEqual(1, CountPlayerProjectiles(rig.Parent));

                rig.Weapon.TickInput(AttackInput(lightHeld: true), 0f, 0.5f);
                Assert.AreEqual(188f, rig.Weapon.CurrentStamina, 0.001f);
                rig.Weapon.TickAction(0f, 0.52f);
                Assert.AreEqual(2, CountPlayerProjectiles(rig.Parent));
            }
            finally
            {
                rig.Destroy();
            }
        }

        [Test]
        public void HeldInstantRangedHeavyRepeatsOnlyAfterRecoveryAllows()
        {
            var rig = CreateHeldAttackRig("HeldRangedHeavy", WeaponSlot.Ranged, "starter_pistol");
            try
            {
                rig.Weapon.TickInput(AttackInput(heavyPressed: true, heavyHeld: true), 0f, 0f);
                Assert.AreEqual(166f, rig.Weapon.CurrentStamina, 0.001f);
                rig.Weapon.TickAction(0f, 0.02f);
                Assert.AreEqual(1, CountPlayerProjectiles(rig.Parent));

                rig.Weapon.TickInput(AttackInput(heavyHeld: true), 0f, 0.36f);
                Assert.AreEqual(166f, rig.Weapon.CurrentStamina, 0.001f);
                Assert.AreEqual(1, CountPlayerProjectiles(rig.Parent));

                rig.Weapon.TickInput(AttackInput(heavyHeld: true), 0f, 0.38f);
                Assert.AreEqual(132f, rig.Weapon.CurrentStamina, 0.001f);
                rig.Weapon.TickAction(0f, 0.4f);
                Assert.AreEqual(2, CountPlayerProjectiles(rig.Parent));
            }
            finally
            {
                rig.Destroy();
            }
        }

        [Test]
        public void HeldDrawRangedAutoFiresAfterDrawAndRepeats()
        {
            var bow = CreateTestDrawBow();
            var catalog = ScriptableObject.CreateInstance<WeaponCatalogDefinition>();
            catalog.Configure("held_draw_test_catalog", new[] { bow });
            var rig = CreateHeldAttackRig("HeldDrawRanged", WeaponSlot.Ranged, bow.WeaponId, catalog);
            try
            {
                rig.Weapon.TickInput(AttackInput(lightPressed: true, lightHeld: true), 0f, 0f);
                Assert.IsTrue(rig.Weapon.IsRangedDrawActive);

                rig.Weapon.TickInput(AttackInput(lightHeld: true), 0f, 0.19f);
                Assert.IsTrue(rig.Weapon.IsRangedDrawActive);
                Assert.AreEqual(0, CountPlayerProjectiles(rig.Parent));

                rig.Weapon.TickInput(AttackInput(lightHeld: true), 0f, 0.2f);
                Assert.IsFalse(rig.Weapon.IsRangedDrawActive);
                Assert.AreEqual(195f, rig.Weapon.CurrentStamina, 0.001f);
                rig.Weapon.TickAction(0f, 0.22f);
                Assert.AreEqual(1, CountPlayerProjectiles(rig.Parent));

                rig.Weapon.TickInput(AttackInput(lightHeld: true), 0f, 0.3f);
                Assert.IsTrue(rig.Weapon.IsRangedDrawActive);
                rig.Weapon.TickInput(AttackInput(lightHeld: true), 0f, 0.5f);
                rig.Weapon.TickAction(0f, 0.52f);
                Assert.AreEqual(2, CountPlayerProjectiles(rig.Parent));
            }
            finally
            {
                rig.Destroy();
                Object.DestroyImmediate(catalog);
                Object.DestroyImmediate(bow);
            }
        }

        [Test]
        public void HeldDrawRangedReleaseBeforeDrawCancelsWithoutSpending()
        {
            var bow = CreateTestDrawBow();
            var catalog = ScriptableObject.CreateInstance<WeaponCatalogDefinition>();
            catalog.Configure("held_draw_cancel_test_catalog", new[] { bow });
            var rig = CreateHeldAttackRig("HeldDrawCancel", WeaponSlot.Ranged, bow.WeaponId, catalog);
            try
            {
                rig.Weapon.TickInput(AttackInput(lightPressed: true, lightHeld: true), 0f, 1f);
                Assert.IsTrue(rig.Weapon.IsRangedDrawActive);

                rig.Weapon.TickInput(AttackInput(lightReleased: true), 0f, 1.1f);

                Assert.IsFalse(rig.Weapon.IsRangedDrawActive);
                Assert.AreEqual(200f, rig.Weapon.CurrentStamina, 0.001f);
                Assert.AreEqual(0, CountPlayerProjectiles(rig.Parent));
            }
            finally
            {
                rig.Destroy();
                Object.DestroyImmediate(catalog);
                Object.DestroyImmediate(bow);
            }
        }

        [Test]
        public void GuardRollAndHeavyDamageStillBlockHeldAttacksThroughExistingPaths()
        {
            var meleeRig = CreateHeldAttackRig("HeldMeleeBlockers", WeaponSlot.Melee);
            try
            {
                meleeRig.Weapon.TickInput(AttackInput(lightPressed: true, lightHeld: true, guardHeld: true), 0f, 0f);
                Assert.AreEqual(200f, meleeRig.Weapon.CurrentStamina, 0.001f);

                meleeRig.Weapon.TickInput(AttackInput(Vector2.up, lightPressed: true, lightHeld: true, rollPressed: true), 0f, 0.1f);
                Assert.AreEqual(200f - PlayerWeaponController.RollStaminaCost, meleeRig.Weapon.CurrentStamina, 0.001f);

                meleeRig.Weapon.TickInput(AttackInput(lightHeld: true), 0f, 0.2f);
                Assert.AreEqual(200f - PlayerWeaponController.RollStaminaCost, meleeRig.Weapon.CurrentStamina, 0.001f);
            }
            finally
            {
                meleeRig.Destroy();
            }

            var rangedRig = CreateHeldAttackRig("HeldDamageInterrupt", WeaponSlot.Ranged, "starter_pistol");
            var damageSource = new GameObject("HeldInterruptDamageSource");
            try
            {
                var health = rangedRig.Player.AddComponent<CombatantHealth>();
                health.Configure(10);

                rangedRig.Weapon.TickInput(AttackInput(lightPressed: true, lightHeld: true), 0f, 2f);
                Assert.AreEqual(194f, rangedRig.Weapon.CurrentStamina, 0.001f);

                DamageSystem.ApplyDamage(
                    health,
                    new DamageRequest(
                        1,
                        damageSource,
                        DamageFeedbackContext.None,
                        DamageThreatKind.Heavy,
                        DamageClassification.PhysicalMelee(ImpactForceClass.Heavy)));
                rangedRig.Weapon.TickAction(0f, 2.02f);

                Assert.AreEqual(0, CountPlayerProjectiles(rangedRig.Parent));
            }
            finally
            {
                rangedRig.Destroy();
                Object.DestroyImmediate(damageSource);
            }
        }

        [Test]
        public void RepeatedRollsExhaustStaminaUntilRegen()
        {
            var player = new GameObject("RollStaminaPlayer");
            try
            {
                var weapon = player.AddComponent<PlayerWeaponController>();

                Assert.IsTrue(weapon.TryRoll(Vector2.up, Vector2.zero, 0f));
                weapon.TickAction(0f, 0.5f);
                Assert.IsTrue(weapon.TryRoll(Vector2.up, Vector2.zero, 1f));
                weapon.TickAction(0f, 1.5f);
                Assert.IsTrue(weapon.TryRoll(Vector2.up, Vector2.zero, 2f));
                weapon.TickAction(0f, 2.5f);
                Assert.AreEqual(10f, weapon.CurrentStamina, 0.001f);
                Assert.IsFalse(weapon.TryRoll(Vector2.up, Vector2.zero, 3f));
            }
            finally
            {
                Object.DestroyImmediate(player);
            }
        }

        [Test]
        public void LightAttacksConsumeMeaningfulStaminaAndCannotSpamForever()
        {
            var player = new GameObject("LightStaminaPlayer");
            var combat = new GameObject("Combat").AddComponent<RoomCombatController>();
            try
            {
                var weapon = player.AddComponent<PlayerWeaponController>();
                weapon.Configure(null, combat, null);
                weapon.ConfigureBuildStats(1f, 0, 0, 28f, 11f, "starter_blade", "starter_pistol", WeaponSlot.Melee, 28f, LoadCatalog());

                Assert.IsTrue(weapon.TryAttack(AttackKind.Light, Vector2.up, 0f));
                weapon.TickAction(0f, 0.8f);
                Assert.IsTrue(weapon.TryAttack(AttackKind.Light, Vector2.up, 1f));
                weapon.TickAction(0f, 1.8f);
                Assert.AreEqual(0f, weapon.CurrentStamina, 0.001f);
                Assert.IsFalse(weapon.TryAttack(AttackKind.Light, Vector2.up, 2f));
            }
            finally
            {
                Object.DestroyImmediate(player);
                Object.DestroyImmediate(combat.gameObject);
            }
        }

        [Test]
        public void StaminaRegenWaitsForDelayAndClampsToMax()
        {
            var player = new GameObject("RegenDelayPlayer");
            try
            {
                var weapon = player.AddComponent<PlayerWeaponController>();
                weapon.ConfigureBuildStats(1f, 0, 0, 50f, 11f, "starter_blade", "starter_pistol", WeaponSlot.Melee, 50f, LoadCatalog());

                Assert.IsTrue(weapon.TryRoll(Vector2.up, Vector2.zero, 10f));
                weapon.TickAction(0f, 10.5f);
                InvokeRegenerateStamina(weapon, 0.6f, 10.6f);
                Assert.AreEqual(20f, weapon.CurrentStamina, 0.001f);

                InvokeRegenerateStamina(weapon, 1f, 10.66f);
                Assert.AreEqual(31f, weapon.CurrentStamina, 0.001f);

                InvokeRegenerateStamina(weapon, 10f, 12f);
                Assert.AreEqual(50f, weapon.CurrentStamina, 0.001f);
            }
            finally
            {
                Object.DestroyImmediate(player);
            }
        }

        [Test]
        public void GuardHeldRegenIsSlowedAndGuardHoldDoesNotDrainStamina()
        {
            var player = new GameObject("GuardRegenPlayer");
            try
            {
                var weapon = player.AddComponent<PlayerWeaponController>();
                weapon.ConfigureBuildStats(1f, 0, 0, 50f, 11f, "starter_blade", "starter_pistol", WeaponSlot.Melee, 50f, LoadCatalog());
                var defense = player.AddComponent<PlayerDefenseController>();
                defense.ConfigureShieldProfile(null);

                Assert.IsTrue(weapon.TryRoll(Vector2.up, Vector2.zero, 0f));
                weapon.TickAction(0f, 0.5f);
                defense.Tick(true, 0.1f);
                defense.Tick(true, 0.1f);
                var guardedStamina = weapon.CurrentStamina;

                InvokeRegenerateStamina(weapon, 1f, 0.66f);

                Assert.AreEqual(20f, guardedStamina, 0.001f);
                Assert.AreEqual(guardedStamina + 11f * PlayerWeaponController.GuardHeldStaminaRegenMultiplier, weapon.CurrentStamina, 0.001f);
            }
            finally
            {
                Object.DestroyImmediate(player);
            }
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

        private static void SetBranchPlayer(BranchSessionController branch, Hollow.Entities.PlaceholderPlayerController player)
        {
            var field = typeof(BranchSessionController).GetField("playerController", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field);
            field.SetValue(branch, player);
        }

        private static void InvokeRegenerateStamina(PlayerWeaponController weapon, float deltaTime, float timeSeconds)
        {
            var method = typeof(PlayerWeaponController).GetMethod("RegenerateStamina", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(method);
            method.Invoke(weapon, new object[] { deltaTime, timeSeconds });
        }

        private static GameplayInputSnapshot AttackInput(
            Vector2? move = null,
            bool lightPressed = false,
            bool lightHeld = false,
            bool lightReleased = false,
            bool heavyPressed = false,
            bool heavyHeld = false,
            bool heavyReleased = false,
            bool guardHeld = false,
            bool rollPressed = false)
        {
            return new GameplayInputSnapshot(
                move ?? Vector2.zero,
                Vector2.up,
                false,
                false,
                lightPressed,
                heavyPressed,
                false,
                false,
                guardHeld,
                false,
                rollPressed,
                false,
                Vector2.zero,
                false,
                false,
                lightHeld,
                lightReleased,
                heavyHeld,
                heavyReleased);
        }

        private static HeldAttackRig CreateHeldAttackRig(
            string name,
            WeaponSlot activeSlot,
            string rangedWeaponId = "starter_pistol",
            WeaponCatalogDefinition catalog = null)
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
                200f,
                0f,
                "starter_blade",
                rangedWeaponId,
                activeSlot,
                200f,
                catalog ?? LoadCatalog());
            return new HeldAttackRig(parent, player, combat.gameObject, projectilePrefab, weapon);
        }

        private static WeaponDefinition CreateTestDrawBow()
        {
            var bow = ScriptableObject.CreateInstance<WeaponDefinition>();
            bow.Configure(
                "test_draw_bow",
                "Test Draw Bow",
                WeaponSlot.Ranged,
                WeaponCategory.Bow,
                nextLightAttack: new WeaponAttackDefinition(
                    AttackKind.Light,
                    1,
                    0.05f,
                    5f,
                    6f,
                    ImpactForceClass.Light,
                    0.25f,
                    windupSeconds: 0.01f,
                    activeSeconds: 0.03f,
                    recoverySeconds: 0.05f,
                    hitArcDegrees: 1f,
                    requiredDrawSeconds: 0.2f),
                nextHeavyAttack: new WeaponAttackDefinition(
                    AttackKind.Heavy,
                    2,
                    0.05f,
                    12f,
                    6.5f,
                    ImpactForceClass.Medium,
                    0.45f,
                    windupSeconds: 0.01f,
                    activeSeconds: 0.03f,
                    recoverySeconds: 0.05f,
                    hitArcDegrees: 1f,
                    requiredDrawSeconds: 0.2f));
            return bow;
        }

        private static int CountPlayerProjectiles(GameObject parent)
        {
            return parent.transform.Cast<Transform>().Count(child => child.name == "PlayerProjectile");
        }

        private readonly struct HeldAttackRig
        {
            public HeldAttackRig(
                GameObject parent,
                GameObject player,
                GameObject combat,
                GameObject projectilePrefab,
                PlayerWeaponController weapon)
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
