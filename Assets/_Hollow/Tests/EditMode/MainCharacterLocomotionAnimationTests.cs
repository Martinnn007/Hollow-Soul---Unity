using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Linq;
using Hollow.Combat;
using Hollow.Data.Definitions;
using Hollow.Editor.Generation;
using Hollow.Editor.Validation;
using Hollow.Entities;
using Hollow.Input;
using Hollow.Presentation;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Hollow.Tests.EditMode
{
    public sealed class MainCharacterLocomotionAnimationTests
    {
        private const string PlayerPrefabPath = "Assets/_Hollow/Prefabs/Player/PlayerCharacter.prefab";
        private const string CanonicalMaterialPath = "Assets/_Hollow/Art/Materials/ArtPass/AP_M_MainCharacter_GreySentinel.mat";
        private const string RollInPlaceClipPath = "Assets/_Hollow/Art/Models/Characters/Player/MainCharacter_Roll_InPlace.anim";
        private const string RunningFbxPath = "Assets/MeshyImports/Running_20260506_131917/Meshy_AI_Grey_Sentinel_biped_Animation_Running_withSkin.fbx";
        private const string RollClipName = "MainCharacter_Roll";
        private const string RunClipName = "MainCharacter_Run";
        private const float RunStartMoveSpeedThreshold = 0.5f;
        private const float RollRootDriftStripThresholdMeters = 0.25f;
        private const string VisualRootName = "MainCharacter_VisualRoot";
        private const string LegacyCapsuleName = "PlayerHeight_1_78m";

        [Test]
        public void PlayerLocomotionAnimatorUsesActualPlanarDisplacement()
        {
            var player = new GameObject("PlayerCharacter");
            var visualRoot = new GameObject(VisualRootName);
            visualRoot.transform.SetParent(player.transform, false);
            try
            {
                var locomotionAnimator = player.AddComponent<PlayerLocomotionAnimator>();
                locomotionAnimator.Bind(null, visualRoot.transform);
                locomotionAnimator.Configure(0.05f, 3600f, PlayerMovementController.DefaultSpeedMetersPerSecond, 100f);
                locomotionAnimator.ResetTracking();

                locomotionAnimator.Sample(0.1f);
                Assert.IsFalse(locomotionAnimator.IsMoving);

                player.transform.position = new Vector3(0.5f, 0f, 0f);
                locomotionAnimator.Sample(0.1f);

                Assert.IsTrue(locomotionAnimator.IsMoving);
                Assert.Greater(locomotionAnimator.PlanarSpeedMetersPerSecond, 0.05f);
                Assert.Greater(Vector3.Dot(visualRoot.transform.forward, Vector3.right), 0.99f);

                locomotionAnimator.Sample(0.1f);
                Assert.IsFalse(locomotionAnimator.IsMoving);
            }
            finally
            {
                Object.DestroyImmediate(player);
            }
        }

        [Test]
        public void PlayerLocomotionAnimatorObservesRollFacingAndDeath()
        {
            var player = new GameObject("PlayerCharacter");
            var visualRoot = new GameObject(VisualRootName);
            visualRoot.transform.SetParent(player.transform, false);
            try
            {
                var health = player.AddComponent<CombatantHealth>();
                health.Configure(2);
                var weapon = player.AddComponent<PlayerWeaponController>();
                var locomotionAnimator = player.AddComponent<PlayerLocomotionAnimator>();
                locomotionAnimator.Bind(null, visualRoot.transform);
                locomotionAnimator.BindGameplay(weapon, health);
                locomotionAnimator.Configure(0.05f, 3600f, PlayerMovementController.DefaultSpeedMetersPerSecond, 100f);
                locomotionAnimator.ResetTracking();

                Assert.IsTrue(weapon.TryRoll(Vector2.left, Vector2.up, 0f));
                locomotionAnimator.Sample(0.016f);

                Assert.AreEqual(PlayerRollPhase.Startup, locomotionAnimator.LastObservedRollPhase);
                Assert.Greater(Vector3.Dot(visualRoot.transform.forward, Vector3.left), 0.99f);

                Assert.IsTrue(health.ApplyDamage(new DamageRequest(3, player)));
                Assert.IsTrue(locomotionAnimator.IsDead);
                Assert.IsFalse(locomotionAnimator.IsMoving);
            }
            finally
            {
                Object.DestroyImmediate(player);
            }
        }

        [Test]
        public void PlayerLocomotionAnimatorFacesLockedTargetWhileMovingLaterally()
        {
            var root = new GameObject("LockedLateralLocomotionHarness");
            try
            {
                var combat = root.AddComponent<RoomCombatController>();
                var player = new GameObject("PlayerCharacter");
                var visualRoot = new GameObject(VisualRootName);
                player.transform.SetParent(root.transform, false);
                visualRoot.transform.SetParent(player.transform, false);
                var aimLock = player.AddComponent<PlayerAimLockController>();
                aimLock.Configure(combat);
                var enemy = CreateEnemy(root.transform, new Vector3(0f, 0f, 5f));
                AddEnemy(combat, enemy);

                aimLock.TickAim(Snapshot(Vector2.zero), 0f);
                var locomotionAnimator = player.AddComponent<PlayerLocomotionAnimator>();
                locomotionAnimator.Bind(null, visualRoot.transform);
                locomotionAnimator.BindGameplay(null, null, aimLock);
                locomotionAnimator.Configure(0.05f, 3600f, PlayerMovementController.DefaultSpeedMetersPerSecond, 100f);
                locomotionAnimator.ResetTracking();

                player.transform.position = new Vector3(0.4f, 0f, 0f);
                locomotionAnimator.Sample(0.1f);

                var expectedFacing = enemy.transform.position - player.transform.position;
                expectedFacing.y = 0f;
                Assert.IsTrue(locomotionAnimator.IsTargetLockedForLocomotion);
                Assert.Greater(Vector3.Dot(visualRoot.transform.forward, expectedFacing.normalized), 0.99f);
                Assert.Greater(locomotionAnimator.LockedRelativeMove.x, 0.95f);
                Assert.Less(Mathf.Abs(locomotionAnimator.LockedRelativeMove.y), 0.15f);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void PlayerLocomotionAnimatorReportsLockedBackstepMovement()
        {
            var root = new GameObject("LockedBackstepLocomotionHarness");
            try
            {
                var combat = root.AddComponent<RoomCombatController>();
                var player = new GameObject("PlayerCharacter");
                var visualRoot = new GameObject(VisualRootName);
                player.transform.SetParent(root.transform, false);
                visualRoot.transform.SetParent(player.transform, false);
                var aimLock = player.AddComponent<PlayerAimLockController>();
                aimLock.Configure(combat);
                var enemy = CreateEnemy(root.transform, new Vector3(0f, 0f, 5f));
                AddEnemy(combat, enemy);

                aimLock.TickAim(Snapshot(Vector2.zero), 0f);
                var locomotionAnimator = player.AddComponent<PlayerLocomotionAnimator>();
                locomotionAnimator.Bind(null, visualRoot.transform);
                locomotionAnimator.BindGameplay(null, null, aimLock);
                locomotionAnimator.Configure(0.05f, 3600f, PlayerMovementController.DefaultSpeedMetersPerSecond, 100f);
                locomotionAnimator.ResetTracking();

                player.transform.position = new Vector3(0f, 0f, -0.4f);
                locomotionAnimator.Sample(0.1f);

                Assert.IsTrue(locomotionAnimator.IsTargetLockedForLocomotion);
                Assert.Greater(Vector3.Dot(visualRoot.transform.forward, Vector3.forward), 0.99f);
                Assert.Less(locomotionAnimator.LockedRelativeMove.y, -0.95f);
                Assert.Less(Mathf.Abs(locomotionAnimator.LockedRelativeMove.x), 0.05f);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void PlayerLocomotionAnimatorKeepsLockedRollFacingOnTarget()
        {
            var root = new GameObject("LockedRollLocomotionHarness");
            try
            {
                var combat = root.AddComponent<RoomCombatController>();
                var player = new GameObject("PlayerCharacter");
                var visualRoot = new GameObject(VisualRootName);
                player.transform.SetParent(root.transform, false);
                visualRoot.transform.SetParent(player.transform, false);
                var aimLock = player.AddComponent<PlayerAimLockController>();
                aimLock.Configure(combat);
                var weapon = player.AddComponent<PlayerWeaponController>();
                weapon.Configure(null, combat, null);
                var enemy = CreateEnemy(root.transform, new Vector3(0f, 0f, 5f));
                AddEnemy(combat, enemy);

                aimLock.TickAim(Snapshot(Vector2.zero), 0f);
                var locomotionAnimator = player.AddComponent<PlayerLocomotionAnimator>();
                locomotionAnimator.Bind(null, visualRoot.transform);
                locomotionAnimator.BindGameplay(weapon, null, aimLock);
                locomotionAnimator.Configure(0.05f, 3600f, PlayerMovementController.DefaultSpeedMetersPerSecond, 100f);
                locomotionAnimator.ResetTracking();

                Assert.IsTrue(weapon.TryRoll(Vector2.left, Vector2.up, 0f));
                Assert.AreEqual(-1f, weapon.RollDirection.x, 0.001f);
                Assert.AreEqual(0f, weapon.RollDirection.y, 0.001f);
                locomotionAnimator.Sample(0.016f);

                Assert.AreEqual(PlayerRollPhase.Startup, locomotionAnimator.LastObservedRollPhase);
                Assert.IsTrue(locomotionAnimator.IsTargetLockedForLocomotion);
                Assert.Greater(Vector3.Dot(visualRoot.transform.forward, Vector3.forward), 0.99f);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void PlayerLocomotionAnimatorTriggersSlashHitReactionAndDeathInputs()
        {
            var root = new GameObject("CombatRoot");
            var player = new GameObject("PlayerCharacter");
            var visualRoot = new GameObject(VisualRootName);
            try
            {
                player.transform.SetParent(root.transform, false);
                visualRoot.transform.SetParent(player.transform, false);
                var combat = root.AddComponent<RoomCombatController>();
                var health = player.AddComponent<CombatantHealth>();
                health.Configure(3);
                var weapon = player.AddComponent<PlayerWeaponController>();
                weapon.Configure(null, combat, null);
                weapon.SetActiveWeaponSlot(WeaponSlot.Melee);
                var locomotionAnimator = player.AddComponent<PlayerLocomotionAnimator>();
                locomotionAnimator.Bind(null, visualRoot.transform);
                locomotionAnimator.BindGameplay(weapon, health);
                locomotionAnimator.ResetTracking();

                Assert.IsTrue(weapon.TryAttack(AttackKind.Light, Vector2.right, 0f));
                Assert.IsTrue(PendingTrigger(locomotionAnimator, "pendingSlashTrigger"));
                Assert.Greater(Vector3.Dot(visualRoot.transform.forward, Vector3.right), 0.99f);

                Assert.IsTrue(health.ApplyDamage(new DamageRequest(1, player)));
                Assert.IsTrue(PendingTrigger(locomotionAnimator, "pendingHitTrigger"));
                Assert.IsFalse(locomotionAnimator.IsDead);

                Assert.IsTrue(health.ApplyDamage(new DamageRequest(5, player)));
                Assert.IsTrue(locomotionAnimator.IsDead);
                Assert.IsTrue(PendingTrigger(locomotionAnimator, "pendingDeathTrigger"));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void HeldWeaponVisualSwapsActiveAndHolsteredWeaponsWithoutDuplicates()
        {
            var presentationCatalog = AssetDatabase.LoadAssetAtPath<PresentationContentCatalog>(Milestone9AssetGenerator.CatalogPath);
            var weaponCatalog = AssetDatabase.LoadAssetAtPath<WeaponCatalogDefinition>(Milestone27AssetGenerator.WeaponCatalogPath);
            Assert.IsNotNull(presentationCatalog);
            Assert.IsNotNull(weaponCatalog);
            PresentationContentProvider.Configure(presentationCatalog);

            var player = new GameObject("PlayerCharacter");
            var rightHand = new GameObject("RightHand");
            var socket = new GameObject(PlayerHeldWeaponVisualController.MeleeHandSocketName);
            try
            {
                rightHand.transform.SetParent(player.transform, false);
                rightHand.transform.localScale = Vector3.one * 100f;
                socket.transform.SetParent(rightHand.transform, false);
                var weapon = player.AddComponent<PlayerWeaponController>();
                weapon.ConfigureBuildStats(
                    1f,
                    0,
                    1,
                    10000f,
                    1000f,
                    "starter_blade",
                    WeaponIdAliases.StarterPistolId,
                    WeaponSlot.Melee,
                    10000f,
                    weaponCatalog);
                PlayerAnimationProfileTestHelpers.BindProfileCatalog(player, weapon);
                var heldWeaponVisual = player.AddComponent<PlayerHeldWeaponVisualController>();
                heldWeaponVisual.BindMeleeHandSocket(socket.transform);
                heldWeaponVisual.Bind(weapon);

                Assert.IsTrue(heldWeaponVisual.IsUsingHandAttachedMeleeVisual);
                Assert.AreSame(socket.transform, heldWeaponVisual.MeleeHandSocket);
                Assert.IsNotNull(heldWeaponVisual.ActiveWeaponVisual);
                Assert.IsNotNull(heldWeaponVisual.HolsteredRangedVisual);
                Assert.IsNotNull(heldWeaponVisual.EquippedShieldVisual);
                Assert.AreSame(heldWeaponVisual.ShieldForearmSocket, heldWeaponVisual.CurrentShieldSocket);
                Assert.AreEqual(1, socket.GetComponentsInChildren<PresentationVisualMarker>(includeInactive: true)
                    .Count(marker => marker.Role == PresentationPrefabRole.WeaponMelee));
                AssertVisibleMeshyMeleeWeaponVisual(heldWeaponVisual.ActiveWeaponVisual);
                AssertWrapperLossyScaleBelow(heldWeaponVisual.ActiveWeaponVisual, 2f);
                Assert.AreEqual(1, player.GetComponentsInChildren<PresentationVisualMarker>(includeInactive: true)
                    .Count(marker => marker.Role == PresentationPrefabRole.WeaponRanged));
                Assert.AreEqual(1, player.GetComponentsInChildren<PresentationVisualMarker>(includeInactive: true)
                    .Count(marker => marker.Role == PresentationPrefabRole.Armor));
                AssertVisibleMeshyRangedWeaponVisual(heldWeaponVisual.HolsteredRangedVisual);
                AssertWrapperLossyScaleBelow(heldWeaponVisual.HolsteredRangedVisual, 2f);
                AssertVisibleMeshyShieldVisual(heldWeaponVisual.EquippedShieldVisual);
                AssertWrapperLossyScaleBelow(heldWeaponVisual.EquippedShieldVisual, 2f);

                CreateDuplicateEquipmentWrapper(
                    player.transform,
                    PlayerHeldWeaponVisualController.ActiveMeleeWeaponVisualName,
                    PresentationPrefabRole.WeaponMelee);
                heldWeaponVisual.RefreshAllEquipmentVisualTransforms();
                Assert.AreEqual(1, player.GetComponentsInChildren<PresentationVisualMarker>(includeInactive: true)
                    .Count(marker => marker.Role == PresentationPrefabRole.WeaponMelee));
                AssertWrapperLossyScaleBelow(heldWeaponVisual.ActiveWeaponVisual, 2f);

                weapon.SetActiveWeaponSlot(WeaponSlot.Ranged);

                Assert.IsFalse(heldWeaponVisual.IsUsingHandAttachedMeleeVisual);
                Assert.IsTrue(heldWeaponVisual.IsUsingHandAttachedRangedVisual);
                Assert.AreSame(rightHand.transform, heldWeaponVisual.RangedHandSocket.parent);
                Assert.IsNotNull(heldWeaponVisual.ActiveWeaponVisual);
                Assert.IsNotNull(heldWeaponVisual.HolsteredMeleeVisual);
                Assert.IsNull(heldWeaponVisual.HolsteredRangedVisual);
                Assert.AreSame(heldWeaponVisual.ShieldBackSocket, heldWeaponVisual.CurrentShieldSocket);
                Assert.AreEqual(0, socket.GetComponentsInChildren<PresentationVisualMarker>(includeInactive: true)
                    .Count(marker => marker.Role == PresentationPrefabRole.WeaponMelee));
                Assert.AreEqual(1, player.GetComponentsInChildren<PresentationVisualMarker>(includeInactive: true)
                    .Count(marker => marker.Role == PresentationPrefabRole.WeaponRanged));
                Assert.AreEqual(1, player.GetComponentsInChildren<PresentationVisualMarker>(includeInactive: true)
                    .Count(marker => marker.Role == PresentationPrefabRole.WeaponMelee));
                Assert.AreEqual(1, player.GetComponentsInChildren<PresentationVisualMarker>(includeInactive: true)
                    .Count(marker => marker.Role == PresentationPrefabRole.Armor));
                AssertVisibleMeshyRangedWeaponVisual(heldWeaponVisual.ActiveWeaponVisual);
                AssertWrapperLossyScaleBelow(heldWeaponVisual.ActiveWeaponVisual, 2f);
                AssertVisibleMeshyMeleeWeaponVisual(heldWeaponVisual.HolsteredMeleeVisual);
                AssertWrapperLossyScaleBelow(heldWeaponVisual.HolsteredMeleeVisual, 2f);
                AssertVisibleMeshyShieldVisual(heldWeaponVisual.EquippedShieldVisual);
                AssertWrapperLossyScaleBelow(heldWeaponVisual.EquippedShieldVisual, 2f);

                weapon.SetActiveWeaponSlot(WeaponSlot.Melee);
                weapon.SetActiveWeaponSlot(WeaponSlot.Ranged);

                Assert.AreEqual(1, player.GetComponentsInChildren<PresentationVisualMarker>(includeInactive: true)
                    .Count(marker => marker.Role == PresentationPrefabRole.WeaponRanged));
                Assert.AreEqual(1, player.GetComponentsInChildren<PresentationVisualMarker>(includeInactive: true)
                    .Count(marker => marker.Role == PresentationPrefabRole.WeaponMelee));
                Assert.AreEqual(1, player.GetComponentsInChildren<PresentationVisualMarker>(includeInactive: true)
                    .Count(marker => marker.Role == PresentationPrefabRole.Armor));
                Assert.AreSame(heldWeaponVisual.RangedHandSocket, heldWeaponVisual.ActiveWeaponVisual.transform.parent);
                Assert.AreSame(heldWeaponVisual.ShieldBackSocket, heldWeaponVisual.CurrentShieldSocket);
                AssertVisibleMeshyRangedWeaponVisual(heldWeaponVisual.ActiveWeaponVisual);
            }
            finally
            {
                Object.DestroyImmediate(player);
            }
        }

        [Test]
        public void HeldWeaponVisualMovesShieldBetweenForearmAndBackForDoubleHandedWeapons()
        {
            var presentationCatalog = AssetDatabase.LoadAssetAtPath<PresentationContentCatalog>(Milestone9AssetGenerator.CatalogPath);
            var weaponCatalog = AssetDatabase.LoadAssetAtPath<WeaponCatalogDefinition>(Milestone27AssetGenerator.WeaponCatalogPath);
            Assert.IsNotNull(presentationCatalog);
            Assert.IsNotNull(weaponCatalog);
            PresentationContentProvider.Configure(presentationCatalog);

            var player = new GameObject("PlayerCharacter");
            var rightHand = new GameObject("RightHand");
            var leftForearm = new GameObject("LeftForeArm");
            var spine = new GameObject("Spine02");
            try
            {
                rightHand.transform.SetParent(player.transform, false);
                leftForearm.transform.SetParent(player.transform, false);
                spine.transform.SetParent(player.transform, false);
                rightHand.transform.localScale = Vector3.one * 100f;
                leftForearm.transform.localScale = Vector3.one * 100f;
                spine.transform.localScale = Vector3.one * 100f;
                var weapon = player.AddComponent<PlayerWeaponController>();
                weapon.ConfigureBuildStats(
                    1f,
                    0,
                    1,
                    10000f,
                    1000f,
                    "iron_cleaver",
                    WeaponIdAliases.StarterPistolId,
                    WeaponSlot.Melee,
                    10000f,
                    weaponCatalog);
                PlayerAnimationProfileTestHelpers.BindProfileCatalog(player, weapon);
                var heldWeaponVisual = player.AddComponent<PlayerHeldWeaponVisualController>();
                heldWeaponVisual.Bind(weapon);

                Assert.AreSame(rightHand.transform, heldWeaponVisual.MeleeHandSocket.parent);
                Assert.AreSame(rightHand.transform, heldWeaponVisual.RangedHandSocket.parent);
                Assert.AreSame(leftForearm.transform, heldWeaponVisual.ShieldForearmSocket.parent);
                Assert.AreSame(spine.transform, heldWeaponVisual.ShieldBackSocket.parent);
                Assert.AreSame(heldWeaponVisual.ShieldBackSocket, heldWeaponVisual.CurrentShieldSocket);
                AssertVisibleMeshyShieldVisual(heldWeaponVisual.EquippedShieldVisual);
                AssertWrapperLossyScaleBelow(heldWeaponVisual.EquippedShieldVisual, 2f);

                weapon.SetActiveWeaponSlot(WeaponSlot.Ranged);

                Assert.IsTrue(heldWeaponVisual.IsUsingHandAttachedRangedVisual);
                Assert.AreSame(heldWeaponVisual.ShieldBackSocket, heldWeaponVisual.CurrentShieldSocket);
                AssertVisibleMeshyRangedWeaponVisual(heldWeaponVisual.ActiveWeaponVisual);
                AssertWrapperLossyScaleBelow(heldWeaponVisual.ActiveWeaponVisual, 2f);
                AssertWrapperLossyScaleBelow(heldWeaponVisual.EquippedShieldVisual, 2f);
                Assert.AreEqual(1, player.GetComponentsInChildren<PresentationVisualMarker>(includeInactive: true)
                    .Count(marker => marker.Role == PresentationPrefabRole.Armor));
            }
            finally
            {
                Object.DestroyImmediate(player);
                PresentationContentProvider.Reset();
            }
        }

        [Test]
        public void RangedHandPoseControllerBlendsOnlyForActiveRangedWeapon()
        {
            var root = new GameObject("RangedHandPoseHarness");
            var projectilePrefab = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            projectilePrefab.AddComponent<ProjectileController>();
            try
            {
                var combat = root.AddComponent<RoomCombatController>();
                var player = new GameObject("PlayerCharacter");
                player.transform.SetParent(root.transform, false);
                var weapon = player.AddComponent<PlayerWeaponController>();
                weapon.Configure(null, combat, projectilePrefab);
                var pose = player.AddComponent<PlayerRangedHandPoseController>();
                pose.Bind(null, weapon, null);
                pose.Configure(10f, 1f, 1f, 1.08f, 0.48f, 0.24f);

                pose.SamplePose(0.2f);

                Assert.AreEqual(0f, pose.CurrentBlend01, 0.001f);
                Assert.IsFalse(pose.IsRangedPoseActive);

                weapon.SetActiveWeaponSlot(WeaponSlot.Ranged);
                pose.SamplePose(0.2f);

                Assert.AreEqual(0f, pose.CurrentBlend01, 0.001f);
                Assert.IsFalse(pose.IsRangedPoseActive);

                Assert.IsTrue(weapon.TryAttack(AttackKind.Light, Vector2.up, 0f));
                pose.SamplePose(0.2f);

                Assert.AreEqual(1f, pose.CurrentBlend01, 0.001f);
                Assert.IsTrue(pose.IsRangedPoseActive);
                Assert.Greater(pose.TargetWorldPosition.y, 1f);

                weapon.TickAction(3f, 3f);
                pose.SamplePose(0.2f);

                Assert.AreEqual(0f, pose.CurrentBlend01, 0.001f);
            }
            finally
            {
                Object.DestroyImmediate(projectilePrefab);
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void RangedHandPoseControllerRaisesFallbackSocketAndMuzzle()
        {
            var root = new GameObject("RangedFallbackPoseHarness");
            var projectilePrefab = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            projectilePrefab.AddComponent<ProjectileController>();
            var rightHand = new GameObject("RightHand");
            try
            {
                var combat = root.AddComponent<RoomCombatController>();
                var player = new GameObject("PlayerCharacter");
                player.transform.SetParent(root.transform, false);
                rightHand.transform.SetParent(player.transform, false);
                var weapon = player.AddComponent<PlayerWeaponController>();
                weapon.Configure(null, combat, projectilePrefab);
                weapon.SetActiveWeaponSlot(WeaponSlot.Ranged);
                var heldWeaponVisual = player.AddComponent<PlayerHeldWeaponVisualController>();
                heldWeaponVisual.Bind(weapon);
                var pose = player.AddComponent<PlayerRangedHandPoseController>();
                pose.Bind(null, weapon, heldWeaponVisual);
                pose.Configure(10f, 1f, 1f, 1.08f, 0.48f, 0.24f);

                Assert.IsTrue(weapon.TryAttack(AttackKind.Light, Vector2.up, 0f));
                heldWeaponVisual.ForceRangedAimPose(Vector2.up);
                pose.SamplePose(0.2f);
                InvokePrivateLateUpdate(pose);

                Assert.AreEqual(1f, pose.CurrentBlend01, 0.001f);
                Assert.Greater(heldWeaponVisual.RangedHandSocket.position.y, 1f);
                Assert.Greater(heldWeaponVisual.ActiveMuzzleTransform.position.y, 1f);
                Assert.Greater(
                    Vector3.Dot(heldWeaponVisual.ActiveMuzzleTransform.forward, Vector3.forward),
                    0.98f);
            }
            finally
            {
                Object.DestroyImmediate(projectilePrefab);
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void RangedHandPoseControllerStaysRaisedWhileHeldBetweenShots()
        {
            var root = new GameObject("RangedHeldPoseHarness");
            var projectilePrefab = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            projectilePrefab.AddComponent<ProjectileController>();
            try
            {
                var combat = root.AddComponent<RoomCombatController>();
                var player = new GameObject("PlayerCharacter");
                player.transform.SetParent(root.transform, false);
                var weapon = player.AddComponent<PlayerWeaponController>();
                weapon.Configure(null, combat, projectilePrefab);
                weapon.SetActiveWeaponSlot(WeaponSlot.Ranged);
                var pose = player.AddComponent<PlayerRangedHandPoseController>();
                pose.Bind(null, weapon, null);
                pose.Configure(10f, 1f, 1f, 1.08f, 0.48f, 0.24f);

                weapon.TickInput(HeldRangedLightSnapshot(Vector2.up), 0f, 0f);
                pose.SamplePose(0.2f);

                Assert.IsTrue(weapon.IsRangedHeldAttackPoseActive);
                Assert.AreEqual(AttackKind.Light, weapon.RangedHeldAttackKind);
                Assert.AreEqual(1f, pose.CurrentBlend01, 0.001f);
                Assert.IsTrue(pose.IsRangedPoseActive);

                weapon.TickAction(0.21f, 0.21f);
                Assert.IsFalse(weapon.IsRangedAttackCommitted);
                Assert.IsTrue(weapon.IsRangedHeldAttackPoseActive);
                pose.SamplePose(0f);

                Assert.AreEqual(1f, pose.CurrentBlend01, 0.001f);
                Assert.IsTrue(pose.IsRangedPoseActive);

                weapon.TickInput(ReleasedRangedSnapshot(Vector2.up), 0f, 0.22f);
                pose.SamplePose(0.2f);

                Assert.IsFalse(weapon.IsRangedHeldAttackPoseActive);
                Assert.AreEqual(0f, pose.CurrentBlend01, 0.001f);
            }
            finally
            {
                Object.DestroyImmediate(projectilePrefab);
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void ShieldGuardPoseControllerRaisesFallbackShieldSocket()
        {
            var player = new GameObject("PlayerCharacter");
            var leftForearm = new GameObject("LeftForeArm");
            try
            {
                leftForearm.transform.SetParent(player.transform, false);
                var weapon = player.AddComponent<PlayerWeaponController>();
                PlayerAnimationProfileTestHelpers.ForceSwordShieldProfile(player, weapon);
                var defense = player.AddComponent<PlayerDefenseController>();
                var heldWeaponVisual = player.AddComponent<PlayerHeldWeaponVisualController>();
                heldWeaponVisual.Bind(weapon);
                var pose = player.AddComponent<PlayerShieldGuardPoseController>();
                pose.Bind(null, defense, heldWeaponVisual);
                pose.Configure(10f, 1f, 1f, 1.04f, 0.46f, -0.22f);

                defense.Tick(
                    new GameplayInputSnapshot(
                        Vector2.zero,
                        Vector2.up,
                        interactPressed: false,
                        swapWeaponPressed: false,
                        lightAttackPressed: false,
                        heavyAttackPressed: false,
                        useActiveItemPressed: false,
                        useConsumableCardPressed: false,
                        guardHeld: true),
                    0f,
                    0f);
                pose.SamplePose(0.2f);
                InvokePrivateLateUpdate(pose);

                Assert.AreEqual(1f, pose.CurrentBlend01, 0.001f);
                Assert.IsTrue(pose.IsShieldPoseActive);
                Assert.Greater(heldWeaponVisual.ShieldForearmSocket.position.y, 0.95f);
                Assert.Greater(
                    Vector3.Dot(heldWeaponVisual.ShieldForearmSocket.forward, Vector3.forward),
                    0.9f);

                defense.Tick(false, 0.1f);
                pose.SamplePose(0.2f);

                Assert.AreEqual(0f, pose.CurrentBlend01, 0.001f);
            }
            finally
            {
                Object.DestroyImmediate(player);
            }
        }

        [Test]
        public void AnimationRiggingPackageIsDeclaredForModernPlayerRig()
        {
            var manifest = File.ReadAllText("Packages/manifest.json");

            StringAssert.Contains("\"com.unity.animation.rigging\": \"1.4.0\"", manifest);
        }

        [Test]
        public void SimpleFullBodyAnimationIsDefaultWhileAdvancedModeRemainsAvailable()
        {
            Assert.AreEqual(PlayerAnimationSystemMode.SimpleFullBodyAnimation, MainCharacterAnimationIntegrator.DefaultAnimationSystemMode);
            Assert.IsTrue(System.Enum.IsDefined(typeof(PlayerAnimationSystemMode), PlayerAnimationSystemMode.AdvancedLayeredAnimation));
        }

        [Test]
        public void RawMixamoAnimationDebugSceneReferencesSimpleControllerAndBody()
        {
            var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(MainCharacterAnimationIntegrator.RawMixamoDebugScenePath);
            Assert.IsNotNull(sceneAsset);

            var previousScenePath = SceneManager.GetActiveScene().path;
            var scene = EditorSceneManager.OpenScene(MainCharacterAnimationIntegrator.RawMixamoDebugScenePath, OpenSceneMode.Single);
            try
            {
                Assert.IsTrue(scene.IsValid());
                var overlay = Object.FindFirstObjectByType<RawMixamoAnimationDebugOverlay>();
                var animator = Object.FindFirstObjectByType<Animator>();
                var grounding = Object.FindFirstObjectByType<SimpleFullBodyGroundingController>();
                Assert.IsNotNull(overlay);
                Assert.IsNotNull(animator);
                Assert.IsNotNull(grounding);
                Assert.IsNotNull(animator.avatar);
                Assert.IsNotNull(animator.runtimeAnimatorController);
                Assert.IsTrue(grounding.GroundingEnabled);
                Assert.AreSame(animator.transform, grounding.MeasuredRoot);
                Assert.GreaterOrEqual(grounding.GroundClearanceMeters, 0.04f);

                var controller = animator.runtimeAnimatorController as AnimatorController;
                Assert.IsNotNull(controller);
                Assert.AreEqual(1, controller.layers.Length);
                Assert.IsFalse(controller.layers[0].iKPass);
                Assert.IsNotNull(animator.GetComponentInChildren<SkinnedMeshRenderer>(includeInactive: true));
                Assert.AreEqual(0, Object.FindObjectsByType<PresentationVisualMarker>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length);
            }
            finally
            {
                if (!string.IsNullOrWhiteSpace(previousScenePath))
                {
                    EditorSceneManager.OpenScene(previousScenePath, OpenSceneMode.Single);
                }
                else
                {
                    EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                }
            }
        }

        [Test]
        public void SkinnedBodyCandidateResolverSelectsMixamoWithSkinBody()
        {
            var candidates = PlayerAnimationProfileAssetGenerator.SkinnedBodyCandidateFbxPaths();
            Assert.IsNotEmpty(candidates);
            Assert.IsTrue(candidates.Any(File.Exists));

            var selected = PlayerAnimationProfileAssetGenerator.ResolveSelectedSkinnedBodyFbxPath();
            Assert.IsFalse(string.IsNullOrWhiteSpace(selected), "Expected a valid with-skin FBX body candidate.");
            StringAssert.Contains("0604223747", selected);
            StringAssert.Contains("Male Locomotion Pack", selected);

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(selected);
            Assert.IsNotNull(prefab);
            var animator = prefab.GetComponent<Animator>();
            Assert.IsNotNull(animator);
            Assert.IsNotNull(animator.avatar);
            Assert.IsTrue(animator.avatar.isValid);
            Assert.IsTrue(animator.avatar.isHuman);

            var skinnedBody = prefab.GetComponentsInChildren<SkinnedMeshRenderer>(includeInactive: true)
                .FirstOrDefault(renderer =>
                    renderer != null &&
                    renderer.enabled &&
                    renderer.sharedMesh != null &&
                    renderer.rootBone != null &&
                    renderer.bones != null &&
                    renderer.bones.Length > 0);
            Assert.IsNotNull(skinnedBody);
            Assert.IsTrue(IsDescendantOf(skinnedBody.rootBone, prefab.transform));
            Assert.IsTrue(skinnedBody.bones.All(bone => bone != null && IsDescendantOf(bone, prefab.transform)));
            Assert.IsTrue(skinnedBody.sharedMaterials.Length > 0);
            Assert.IsTrue(skinnedBody.sharedMaterials.All(material => material != null));
            var selectedLocalScale = PlayerAnimationProfileAssetGenerator.ResolveSkinnedBodyLocalScale(selected);
            var scaledBoundsSize = Vector3.Scale(skinnedBody.sharedMesh.bounds.size, skinnedBody.transform.lossyScale) *
                selectedLocalScale;
            Assert.Greater(scaledBoundsSize.y, 0.75f);
            Assert.Less(scaledBoundsSize.y, 3f);
        }

        [Test]
        public void DirectionalLocomotionValidationTracksRequiredEightWayImports()
        {
            var required = MainCharacterAnimationIntegrator.RequiredDirectionalLocomotionFbxPaths();
            var expectedMissing = required.Where(path => !File.Exists(path)).ToArray();

            Assert.AreEqual(16, required.Length);
            Assert.AreEqual(required.Length, required.Distinct().Count());
            Assert.IsTrue(required.Any(path => path.Contains("Walk_Backward")));
            Assert.IsTrue(required.Any(path => path.Contains("Walk_Right")));
            Assert.IsTrue(required.Any(path => path.Contains("Run_Backward")));
            Assert.IsTrue(required.Any(path => path.Contains("Run_Left")));
            CollectionAssert.AreEquivalent(expectedMissing, MainCharacterAnimationIntegrator.MissingDirectionalLocomotionFbxPaths());
        }

        [Test]
        public void PlayerLocomotionAnimatorKeepsLowerBodyStableUntilTurnThreshold()
        {
            var root = new GameObject("TurnInPlaceThresholdHarness");
            var projectilePrefab = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            projectilePrefab.AddComponent<ProjectileController>();
            try
            {
                var combat = root.AddComponent<RoomCombatController>();
                var player = new GameObject("PlayerCharacter");
                var visualRoot = new GameObject(VisualRootName);
                player.transform.SetParent(root.transform, false);
                visualRoot.transform.SetParent(player.transform, false);
                var weapon = player.AddComponent<PlayerWeaponController>();
                weapon.Configure(null, combat, projectilePrefab);
                weapon.SetActiveWeaponSlot(WeaponSlot.Ranged);
                var locomotionAnimator = player.AddComponent<PlayerLocomotionAnimator>();
                locomotionAnimator.Bind(null, visualRoot.transform);
                locomotionAnimator.BindGameplay(weapon, null, null);
                locomotionAnimator.Configure(
                    0.05f,
                    3600f,
                    PlayerMovementController.DefaultSpeedMetersPerSecond,
                    100f,
                    PlayerLocomotionAnimator.DefaultTurnInPlaceStartDegrees,
                    PlayerLocomotionAnimator.DefaultTurnInPlaceFullDegrees);
                locomotionAnimator.ResetTracking();

                weapon.TickInput(HeldRangedLightSnapshot(new Vector2(0.70710677f, 0.70710677f)), 0f, 0f);
                locomotionAnimator.Sample(0.1f);

                Assert.IsTrue(locomotionAnimator.IsTargetLockedForLocomotion);
                Assert.IsFalse(locomotionAnimator.IsTurnInPlaceActive);
                Assert.Greater(Vector3.Dot(visualRoot.transform.forward, Vector3.forward), 0.99f);
                Assert.That(locomotionAnimator.AimBodyAngleDegrees, Is.InRange(40f, 50f));

                weapon.TickInput(HeldRangedLightSnapshot(Vector2.right), 0f, 0.2f);
                locomotionAnimator.Sample(0.1f);

                Assert.IsTrue(locomotionAnimator.IsTurnInPlaceActive);
                Assert.Greater(locomotionAnimator.AimBodyAngleDegrees, 40f);
            }
            finally
            {
                Object.DestroyImmediate(projectilePrefab);
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void PlayerAnimationPoseCoordinatorDrivesRangedAndShieldRigState()
        {
            var root = new GameObject("ModernAnimationPoseHarness");
            var projectilePrefab = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            projectilePrefab.AddComponent<ProjectileController>();
            try
            {
                var combat = root.AddComponent<RoomCombatController>();
                var player = new GameObject("PlayerCharacter");
                player.transform.SetParent(root.transform, false);
                var rightHand = new GameObject("RightHand");
                var leftForearm = new GameObject("LeftForeArm");
                rightHand.transform.SetParent(player.transform, false);
                leftForearm.transform.SetParent(player.transform, false);
                var health = player.AddComponent<CombatantHealth>();
                health.Configure(6);
                var weapon = player.AddComponent<PlayerWeaponController>();
                weapon.Configure(null, combat, projectilePrefab);
                weapon.SetActiveWeaponSlot(WeaponSlot.Ranged);
                var defense = player.AddComponent<PlayerDefenseController>();
                defense.Configure(0);
                PlayerAnimationProfileTestHelpers.ForceSwordShieldProfile(player, weapon);
                var heldWeaponVisual = player.AddComponent<PlayerHeldWeaponVisualController>();
                heldWeaponVisual.Bind(weapon);
                var rangedPose = player.AddComponent<PlayerRangedHandPoseController>();
                rangedPose.Bind(null, weapon, heldWeaponVisual);
                rangedPose.Configure(10f, 1f, 1f, 1.08f, 0.48f, 0.24f);
                var shieldPose = player.AddComponent<PlayerShieldGuardPoseController>();
                shieldPose.Bind(null, defense, heldWeaponVisual);
                shieldPose.Configure(10f, 1f, 1f, 1.04f, 0.46f, -0.22f);
                var rigHarness = CreateModernRigHarness(player.transform);
                var constraintHarness = CreateModernConstraintHarness(rigHarness.UpperBodyRig.transform);
                var coordinator = player.AddComponent<PlayerAnimationPoseCoordinator>();
                coordinator.Bind(null, null, weapon, defense, health, heldWeaponVisual, rangedPose, shieldPose);
                coordinator.BindRigs(rigHarness.BaseRig, rigHarness.FullBodyRig, rigHarness.UpperBodyRig, rigHarness.AdditiveRig);
                coordinator.BindRigConstraints(
                    constraintHarness.RightHandIk,
                    constraintHarness.LeftHandIk,
                    constraintHarness.ChestAim);
                coordinator.BindTargets(
                    rigHarness.RightHandTarget,
                    rigHarness.LeftHandTarget,
                    rigHarness.ChestTarget,
                    rigHarness.ResponseTarget,
                    rigHarness.LeftFootTarget,
                    rigHarness.RightFootTarget);
                coordinator.Configure(10f, 10f, 10f, PlayerMovementController.DefaultSpeedMetersPerSecond);
                coordinator.ConfigureAnimationSystemMode(PlayerAnimationSystemMode.AdvancedLayeredAnimation);

                weapon.TickInput(HeldRangedLightSnapshot(Vector2.up), 0f, 0f);
                rangedPose.SamplePose(0.2f);
                coordinator.SamplePose(0.2f);

                Assert.AreEqual(PlayerAnimationUpperBodyPose.RangedAim, coordinator.CurrentUpperBodyPose);
                Assert.AreEqual(PlayerAnimationActionPhase.RangedAttack, coordinator.CurrentActionPhase);
                Assert.AreEqual(1f, coordinator.UpperBodyCombatRigWeight, 0.001f);
                Assert.AreEqual(1f, rigHarness.UpperBodyRig.weight, 0.001f);
                Assert.AreEqual(1f, constraintHarness.RightHandIk.weight, 0.001f);
                Assert.AreEqual(0f, constraintHarness.LeftHandIk.weight, 0.001f);
                Assert.AreEqual(0.45f, constraintHarness.ChestAim.weight, 0.001f);
                Assert.Greater(rigHarness.RightHandTarget.position.y, 1f);
                Assert.Greater(Vector3.Dot(rigHarness.ChestTarget.forward, Vector3.forward), 0.95f);

                weapon.TickAction(1f, 1f);
                weapon.TickInput(ReleasedRangedSnapshot(Vector2.up), 0f, 1.1f);
                defense.Tick(
                    new GameplayInputSnapshot(
                        Vector2.zero,
                        Vector2.right,
                        interactPressed: false,
                        swapWeaponPressed: false,
                        lightAttackPressed: false,
                        heavyAttackPressed: false,
                        useActiveItemPressed: false,
                        useConsumableCardPressed: false,
                        guardHeld: true),
                    0f,
                    1.2f);
                shieldPose.SamplePose(0.2f);
                coordinator.SamplePose(0.2f);

                Assert.AreEqual(PlayerAnimationUpperBodyPose.ShieldGuard, coordinator.CurrentUpperBodyPose);
                Assert.AreEqual(PlayerAnimationActionPhase.Guard, coordinator.CurrentActionPhase);
                Assert.AreEqual(1f, coordinator.UpperBodyCombatRigWeight, 0.001f);
                Assert.AreEqual(0f, constraintHarness.RightHandIk.weight, 0.001f);
                Assert.AreEqual(1f, constraintHarness.LeftHandIk.weight, 0.001f);
                Assert.AreEqual(0.35f, constraintHarness.ChestAim.weight, 0.001f);
                Assert.Greater(rigHarness.LeftHandTarget.position.y, 0.95f);
                Assert.Greater(Vector3.Dot(rigHarness.ChestTarget.forward, Vector3.right), 0.95f);
            }
            finally
            {
                Object.DestroyImmediate(projectilePrefab);
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void PlayerAnimationPoseCoordinatorRaisesPhysicalResponseOnShotAndDamage()
        {
            var root = new GameObject("ModernAnimationResponseHarness");
            var projectilePrefab = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            projectilePrefab.AddComponent<ProjectileController>();
            try
            {
                var combat = root.AddComponent<RoomCombatController>();
                var player = new GameObject("PlayerCharacter");
                player.transform.SetParent(root.transform, false);
                var health = player.AddComponent<CombatantHealth>();
                health.Configure(6);
                var weapon = player.AddComponent<PlayerWeaponController>();
                weapon.Configure(null, combat, projectilePrefab);
                weapon.SetActiveWeaponSlot(WeaponSlot.Ranged);
                var rigHarness = CreateModernRigHarness(player.transform);
                var coordinator = player.AddComponent<PlayerAnimationPoseCoordinator>();
                coordinator.Bind(null, null, weapon, null, health, null, null, null);
                coordinator.BindRigs(rigHarness.BaseRig, rigHarness.FullBodyRig, rigHarness.UpperBodyRig, rigHarness.AdditiveRig);
                coordinator.BindTargets(
                    rigHarness.RightHandTarget,
                    rigHarness.LeftHandTarget,
                    rigHarness.ChestTarget,
                    rigHarness.ResponseTarget,
                    rigHarness.LeftFootTarget,
                    rigHarness.RightFootTarget);
                coordinator.Configure(10f, 3f, 10f, PlayerMovementController.DefaultSpeedMetersPerSecond);

                Assert.IsTrue(weapon.TryAttack(AttackKind.Light, Vector2.up, 0f));
                weapon.TickAction(0.02f, 0.02f);
                coordinator.SamplePose(0.02f);

                Assert.Greater(coordinator.PhysicalImpulse01, 0.1f);
                Assert.Greater(coordinator.AdditivePhysicalResponseRigWeight, 0.1f);
                Assert.Greater(rigHarness.AdditiveRig.weight, 0.1f);

                DamageSystem.ApplyDamage(health, new DamageRequest(1, player));
                coordinator.SamplePose(0.02f);

                Assert.Greater(coordinator.PhysicalImpulse01, 0.5f);
            }
            finally
            {
                Object.DestroyImmediate(projectilePrefab);
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void PlayerFootPlacementPlantsFeetAndBoundsAimYawOnFlatFloor()
        {
            var player = new GameObject("PlayerCharacter");
            var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            try
            {
                floor.name = "FlatFootPlacementFloor";
                floor.transform.position = Vector3.zero;
                var leftTarget = new GameObject("LeftFootTarget").transform;
                var rightTarget = new GameObject("RightFootTarget").transform;
                var pelvisTarget = new GameObject("PelvisTarget").transform;
                leftTarget.SetParent(player.transform, false);
                rightTarget.SetParent(player.transform, false);
                pelvisTarget.SetParent(player.transform, false);
                var leftIk = new GameObject("LeftFootIK").AddComponent<TwoBoneIKConstraint>();
                var rightIk = new GameObject("RightFootIK").AddComponent<TwoBoneIKConstraint>();
                var pelvis = new GameObject("PelvisPosition").AddComponent<MultiPositionConstraint>();
                leftIk.transform.SetParent(player.transform, false);
                rightIk.transform.SetParent(player.transform, false);
                pelvis.transform.SetParent(player.transform, false);

                var footPlacement = player.AddComponent<PlayerFootPlacementController>();
                footPlacement.Bind(null, null, null, null, leftTarget, rightTarget, pelvisTarget);
                footPlacement.BindConstraints(leftIk, rightIk, pelvis);
                footPlacement.Configure(
                    PlayerFootPlacementController.DefaultStrideLengthMeters,
                    PlayerFootPlacementController.DefaultLockThresholdMetersPerSecond,
                    PlayerFootPlacementController.DefaultPelvisSmoothing,
                    PlayerFootPlacementController.DefaultFootHeightMeters,
                    PlayerFootPlacementController.DefaultRaycastDistanceMeters,
                    PlayerFootPlacementController.DefaultIkBlendSpeed,
                    PlayerFootPlacementController.DefaultYawBlend,
                    PlayerFootPlacementController.DefaultFootPlantHalfCycleSeconds);

                footPlacement.SamplePlacement(
                    0.2f,
                    allowFootIk: true,
                    Vector3.forward,
                    Vector3.right,
                    Vector2.up,
                    PlayerAnimationPoseCoordinator.DefaultFootYawAimInfluenceMaxDegrees);
                footPlacement.ApplyConstraintWeights(1f);

                Assert.IsTrue(footPlacement.IsFootIkEligible);
                Assert.IsFalse(footPlacement.IsUsingGroundFallback);
                Assert.Greater(leftTarget.position.y, 0f);
                Assert.Greater(rightTarget.position.y, 0f);
                Assert.That(Mathf.Abs(Vector3.SignedAngle(Vector3.forward, leftTarget.forward, Vector3.up)), Is.LessThanOrEqualTo(16f));
                Assert.That(footPlacement.PelvisOffset, Is.InRange(-0.18f, 0.06f));
                Assert.Greater(leftIk.weight, 0.1f);
                Assert.Greater(rightIk.weight, 0.1f);
                Assert.Greater(pelvis.weight, 0.05f);
            }
            finally
            {
                Object.DestroyImmediate(player);
                Object.DestroyImmediate(floor);
            }
        }

        [Test]
        public void PlayerAnimationPoseCoordinatorWeightsFootPlacementAndSuppressesOnDamage()
        {
            var player = new GameObject("PlayerCharacter");
            var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            try
            {
                var health = player.AddComponent<CombatantHealth>();
                health.Configure(4);
                var rigHarness = CreateModernRigHarness(player.transform);
                var footPlacement = player.AddComponent<PlayerFootPlacementController>();
                var leftIk = new GameObject(PlayerAnimationPoseCoordinator.LeftFootIkConstraintName).AddComponent<TwoBoneIKConstraint>();
                var rightIk = new GameObject(PlayerAnimationPoseCoordinator.RightFootIkConstraintName).AddComponent<TwoBoneIKConstraint>();
                var pelvis = new GameObject(PlayerAnimationPoseCoordinator.PelvisPositionConstraintName).AddComponent<MultiPositionConstraint>();
                leftIk.transform.SetParent(player.transform, false);
                rightIk.transform.SetParent(player.transform, false);
                pelvis.transform.SetParent(player.transform, false);
                var coordinator = player.AddComponent<PlayerAnimationPoseCoordinator>();
                coordinator.Bind(null, null, null, null, health, null, null, null);
                coordinator.BindRigs(rigHarness.BaseRig, rigHarness.FullBodyRig, rigHarness.UpperBodyRig, rigHarness.AdditiveRig);
                coordinator.BindTargets(
                    rigHarness.RightHandTarget,
                    rigHarness.LeftHandTarget,
                    rigHarness.ChestTarget,
                    rigHarness.ResponseTarget,
                    rigHarness.LeftFootTarget,
                    rigHarness.RightFootTarget);
                coordinator.BindFootPlacement(footPlacement, leftIk, rightIk, pelvis, rigHarness.PelvisTarget);
                coordinator.Configure(10f, 10f, 10f, PlayerMovementController.DefaultSpeedMetersPerSecond);

                coordinator.SamplePose(0.2f);

                Assert.Greater(coordinator.FootIkWeight, 0.9f);
                Assert.Greater(coordinator.LeftFootLockWeight, 0.1f);
                Assert.Greater(coordinator.RightFootLockWeight, 0.1f);

                DamageSystem.ApplyDamage(health, new DamageRequest(1, player));
                coordinator.SamplePose(0.02f);

                Assert.Less(coordinator.FootIkWeight, 1f);
            }
            finally
            {
                Object.DestroyImmediate(player);
                Object.DestroyImmediate(floor);
            }
        }

        private static bool UsesMeshyMeleeWeaponMaterial(GameObject root)
        {
            return root != null &&
                root.GetComponentsInChildren<Renderer>(includeInactive: true)
                    .SelectMany(renderer => renderer.sharedMaterials)
                    .Where(material => material != null)
                    .Any(material => AssetDatabase.GetAssetPath(material) == WeaponMeleeMeshyAssetGenerator.MeshyMaterialPath);
        }

        private static GameplayInputSnapshot HeldRangedLightSnapshot(Vector2 aim)
        {
            return new GameplayInputSnapshot(
                Vector2.zero,
                aim,
                interactPressed: false,
                swapWeaponPressed: false,
                lightAttackPressed: true,
                heavyAttackPressed: false,
                useActiveItemPressed: false,
                useConsumableCardPressed: false,
                guardHeld: false,
                pausePressed: false,
                rollPressed: false,
                lockTargetPressed: false,
                pointerScreenPosition: Vector2.zero,
                hasPointerScreenPosition: false,
                mouseAimIntent: false,
                lightAttackHeld: true,
                lightAttackReleased: false,
                heavyAttackHeld: false,
                heavyAttackReleased: false);
        }

        private static GameplayInputSnapshot ReleasedRangedSnapshot(Vector2 aim)
        {
            return new GameplayInputSnapshot(
                Vector2.zero,
                aim,
                interactPressed: false,
                swapWeaponPressed: false,
                lightAttackPressed: false,
                heavyAttackPressed: false,
                useActiveItemPressed: false,
                useConsumableCardPressed: false,
                guardHeld: false,
                pausePressed: false,
                rollPressed: false,
                lockTargetPressed: false,
                pointerScreenPosition: Vector2.zero,
                hasPointerScreenPosition: false,
                mouseAimIntent: false,
                lightAttackHeld: false,
                lightAttackReleased: true,
                heavyAttackHeld: false,
                heavyAttackReleased: false);
        }

        private static ModernRigHarness CreateModernRigHarness(Transform parent)
        {
            var root = new GameObject(PlayerAnimationPoseCoordinator.ModernAnimationRigRootName);
            root.transform.SetParent(parent, false);
            var baseRig = CreateRig(root.transform, PlayerAnimationPoseCoordinator.BaseLocomotionRigName);
            var fullBodyRig = CreateRig(root.transform, PlayerAnimationPoseCoordinator.FullBodyActionRigName);
            var upperBodyRig = CreateRig(root.transform, PlayerAnimationPoseCoordinator.UpperBodyCombatRigName);
            var additiveRig = CreateRig(root.transform, PlayerAnimationPoseCoordinator.AdditivePhysicalResponseRigName);
            var targets = new GameObject(PlayerAnimationPoseCoordinator.RigTargetsRootName);
            targets.transform.SetParent(root.transform, false);

            return new ModernRigHarness(
                baseRig,
                fullBodyRig,
                upperBodyRig,
                additiveRig,
                CreateTarget(targets.transform, PlayerAnimationPoseCoordinator.RightHandWeaponTargetName),
                CreateTarget(targets.transform, PlayerAnimationPoseCoordinator.LeftHandShieldTargetName),
                CreateTarget(targets.transform, PlayerAnimationPoseCoordinator.ChestAimTargetName),
                CreateTarget(targets.transform, PlayerAnimationPoseCoordinator.PhysicalResponseTargetName),
                CreateTarget(targets.transform, PlayerAnimationPoseCoordinator.LeftFootGroundTargetName),
                CreateTarget(targets.transform, PlayerAnimationPoseCoordinator.RightFootGroundTargetName),
                CreateTarget(targets.transform, PlayerAnimationPoseCoordinator.PelvisTargetName));
        }

        private static Rig CreateRig(Transform parent, string rigName)
        {
            var rigObject = new GameObject(rigName);
            rigObject.transform.SetParent(parent, false);
            return rigObject.AddComponent<Rig>();
        }

        private static Transform CreateTarget(Transform parent, string targetName)
        {
            var target = new GameObject(targetName);
            target.transform.SetParent(parent, false);
            return target.transform;
        }

        private static ModernConstraintHarness CreateModernConstraintHarness(Transform parent)
        {
            var rightHand = new GameObject(PlayerAnimationPoseCoordinator.RightHandWeaponIkConstraintName);
            rightHand.transform.SetParent(parent, false);
            var leftHand = new GameObject(PlayerAnimationPoseCoordinator.LeftHandShieldIkConstraintName);
            leftHand.transform.SetParent(parent, false);
            var chestAim = new GameObject(PlayerAnimationPoseCoordinator.ChestAimConstraintName);
            chestAim.transform.SetParent(parent, false);

            var rightHandIk = rightHand.AddComponent<TwoBoneIKConstraint>();
            var leftHandIk = leftHand.AddComponent<TwoBoneIKConstraint>();
            var chestAimConstraint = chestAim.AddComponent<MultiAimConstraint>();
            ConfigureTestTwoBoneIk(rightHandIk, parent, "Right");
            ConfigureTestTwoBoneIk(leftHandIk, parent, "Left");
            ConfigureTestMultiAim(chestAimConstraint, parent);

            return new ModernConstraintHarness(rightHandIk, leftHandIk, chestAimConstraint);
        }

        private static void ConfigureTestTwoBoneIk(TwoBoneIKConstraint constraint, Transform parent, string prefix)
        {
            constraint.data.root = CreateTarget(parent, prefix + "UpperArm");
            constraint.data.mid = CreateTarget(parent, prefix + "Forearm");
            constraint.data.tip = CreateTarget(parent, prefix + "Hand");
            constraint.data.target = CreateTarget(parent, prefix + "HandTarget");
            constraint.data.hint = CreateTarget(parent, prefix + "ElbowHint");
            constraint.data.targetPositionWeight = 1f;
            constraint.data.targetRotationWeight = 1f;
            constraint.data.hintWeight = 0.75f;
        }

        private static void ConfigureTestMultiAim(MultiAimConstraint constraint, Transform parent)
        {
            var sourceObjects = new WeightedTransformArray(1);
            sourceObjects[0] = new WeightedTransform(CreateTarget(parent, "ChestAimSource"), 1f);
            constraint.data.constrainedObject = CreateTarget(parent, "Chest");
            constraint.data.sourceObjects = sourceObjects;
            constraint.data.aimAxis = MultiAimConstraintData.Axis.Z;
            constraint.data.upAxis = MultiAimConstraintData.Axis.Y;
            constraint.data.worldUpType = MultiAimConstraintData.WorldUpType.SceneUp;
            constraint.data.worldUpAxis = MultiAimConstraintData.Axis.Y;
        }

        private readonly struct ModernConstraintHarness
        {
            public ModernConstraintHarness(
                TwoBoneIKConstraint rightHandIk,
                TwoBoneIKConstraint leftHandIk,
                MultiAimConstraint chestAim)
            {
                RightHandIk = rightHandIk;
                LeftHandIk = leftHandIk;
                ChestAim = chestAim;
            }

            public TwoBoneIKConstraint RightHandIk { get; }

            public TwoBoneIKConstraint LeftHandIk { get; }

            public MultiAimConstraint ChestAim { get; }
        }

        private readonly struct ModernRigHarness
        {
            public ModernRigHarness(
                Rig baseRig,
                Rig fullBodyRig,
                Rig upperBodyRig,
                Rig additiveRig,
                Transform rightHandTarget,
                Transform leftHandTarget,
                Transform chestTarget,
                Transform responseTarget,
                Transform leftFootTarget,
                Transform rightFootTarget,
                Transform pelvisTarget)
            {
                BaseRig = baseRig;
                FullBodyRig = fullBodyRig;
                UpperBodyRig = upperBodyRig;
                AdditiveRig = additiveRig;
                RightHandTarget = rightHandTarget;
                LeftHandTarget = leftHandTarget;
                ChestTarget = chestTarget;
                ResponseTarget = responseTarget;
                LeftFootTarget = leftFootTarget;
                RightFootTarget = rightFootTarget;
                PelvisTarget = pelvisTarget;
            }

            public Rig BaseRig { get; }

            public Rig FullBodyRig { get; }

            public Rig UpperBodyRig { get; }

            public Rig AdditiveRig { get; }

            public Transform RightHandTarget { get; }

            public Transform LeftHandTarget { get; }

            public Transform ChestTarget { get; }

            public Transform ResponseTarget { get; }

            public Transform LeftFootTarget { get; }

            public Transform RightFootTarget { get; }

            public Transform PelvisTarget { get; }
        }

        private static bool UsesMeshyRangedWeaponMaterial(GameObject root)
        {
            return root != null &&
                root.GetComponentsInChildren<Renderer>(includeInactive: true)
                    .SelectMany(renderer => renderer.sharedMaterials)
                    .Where(material => material != null)
                    .Any(material => AssetDatabase.GetAssetPath(material) == DefaultEquipmentMeshyAssetGenerator.MeshyPistolMaterialPath);
        }

        private static bool UsesMeshyShieldMaterial(GameObject root)
        {
            return root != null &&
                root.GetComponentsInChildren<Renderer>(includeInactive: true)
                    .SelectMany(renderer => renderer.sharedMaterials)
                    .Where(material => material != null)
                    .Any(material => AssetDatabase.GetAssetPath(material) == DefaultEquipmentMeshyAssetGenerator.MeshyShieldMaterialPath);
        }

        private static void AssertVisibleMeshyMeleeWeaponVisual(GameObject root)
        {
            Assert.IsNotNull(root);
            Assert.IsTrue(UsesMeshyMeleeWeaponMaterial(root));
            var renderers = root.GetComponentsInChildren<Renderer>(includeInactive: false)
                .Where(renderer => renderer.enabled && renderer.gameObject.activeInHierarchy)
                .ToArray();
            Assert.Greater(renderers.Length, 0, "Melee weapon visual should have an active renderer in gameplay hierarchy.");
            var bounds = Encapsulate(renderers);
            var maxDimension = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
            Assert.Greater(maxDimension, 0.5f);
            Assert.Less(maxDimension, 2.2f);
            Assert.Greater(bounds.size.sqrMagnitude, 0.25f);
        }

        private static void AssertVisibleMeshyRangedWeaponVisual(GameObject root)
        {
            Assert.IsNotNull(root);
            Assert.IsTrue(UsesMeshyRangedWeaponMaterial(root));
            var renderers = root.GetComponentsInChildren<Renderer>(includeInactive: false)
                .Where(renderer => renderer.enabled && renderer.gameObject.activeInHierarchy)
                .ToArray();
            Assert.Greater(renderers.Length, 0, "Ranged weapon visual should have an active renderer in gameplay hierarchy.");
            var bounds = Encapsulate(renderers);
            var maxDimension = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
            Assert.Greater(maxDimension, 0.15f);
            Assert.Less(maxDimension, 1.5f);
            Assert.Greater(bounds.size.sqrMagnitude, 0.08f);
        }

        private static void AssertVisibleMeshyShieldVisual(GameObject root)
        {
            Assert.IsNotNull(root);
            Assert.IsTrue(UsesMeshyShieldMaterial(root));
            Assert.AreEqual(0, root.GetComponentsInChildren<Collider>(includeInactive: true).Length);
            var renderers = root.GetComponentsInChildren<Renderer>(includeInactive: false)
                .Where(renderer => renderer.enabled && renderer.gameObject.activeInHierarchy)
                .ToArray();
            Assert.Greater(renderers.Length, 0, "Shield visual should have an active renderer in gameplay hierarchy.");
            var bounds = Encapsulate(renderers);
            var maxDimension = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
            Assert.Greater(maxDimension, 0.3f);
            Assert.Less(maxDimension, 1.3f);
            Assert.Greater(bounds.size.sqrMagnitude, 0.08f);
        }

        private static void AssertWrapperLossyScaleBelow(GameObject root, float maxScale)
        {
            Assert.IsNotNull(root);
            var scale = root.transform.lossyScale;
            Assert.Less(Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z)), maxScale);
        }

        private static void CreateDuplicateEquipmentWrapper(
            Transform parent,
            string wrapperName,
            PresentationPrefabRole role)
        {
            var wrapper = new GameObject(wrapperName);
            wrapper.transform.SetParent(parent, false);
            wrapper.AddComponent<PresentationVisualMarker>().Configure(role, isFallback: false);
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = "StaleDuplicateRenderer";
            cube.transform.SetParent(wrapper.transform, false);
            Object.DestroyImmediate(cube.GetComponent<Collider>());
        }

        private static Bounds Encapsulate(Renderer[] renderers)
        {
            Assert.Greater(renderers.Length, 0);
            var bounds = renderers[0].bounds;
            for (var index = 1; index < renderers.Length; index++)
            {
                bounds.Encapsulate(renderers[index].bounds);
            }

            return bounds;
        }

        [Test]
        public void HeldWeaponVisualAlignsActiveWeaponToCombatAimDirections()
        {
            var root = new GameObject("WeaponVisualAimHarness");
            var projectilePrefab = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            projectilePrefab.AddComponent<ProjectileController>();
            try
            {
                var combat = root.AddComponent<RoomCombatController>();
                var player = new GameObject("PlayerCharacter");
                var rightHand = new GameObject("RightHand");
                var socket = new GameObject(PlayerHeldWeaponVisualController.MeleeHandSocketName);
                player.transform.SetParent(root.transform, false);
                rightHand.transform.SetParent(player.transform, false);
                socket.transform.SetParent(rightHand.transform, false);
                var weapon = player.AddComponent<PlayerWeaponController>();
                weapon.Configure(null, combat, projectilePrefab);
                weapon.ConfigureBuildStats(
                    1f,
                    0,
                    1,
                    10000f,
                    1000f,
                    "starter_blade",
                    WeaponIdAliases.StarterPistolId,
                    WeaponSlot.Melee,
                    10000f);
                var heldWeaponVisual = player.AddComponent<PlayerHeldWeaponVisualController>();
                heldWeaponVisual.BindMeleeHandSocket(socket.transform);
                heldWeaponVisual.Bind(weapon);

                var directions = new[]
                {
                    Vector2.up,
                    Vector2.down,
                    Vector2.left,
                    Vector2.right,
                    new Vector2(1f, 1f).normalized,
                    new Vector2(-1f, 1f).normalized,
                    new Vector2(1f, -1f).normalized,
                    new Vector2(-1f, -1f).normalized
                };
                var time = 0f;
                foreach (var slot in new[] { WeaponSlot.Melee, WeaponSlot.Ranged })
                {
                    weapon.SetActiveWeaponSlot(slot);
                    foreach (var direction in directions)
                    {
                        Assert.IsTrue(weapon.TryAttack(AttackKind.Light, direction, time));
                        var expected = PlayerWeaponVisualPosePolicy.PlanarForward(direction);
                        Assert.IsNotNull(heldWeaponVisual.ActiveWeaponVisual);
                        Assert.Greater(
                            Vector3.Dot(heldWeaponVisual.ActiveWeaponVisual.transform.forward, expected),
                            0.98f,
                            $"Expected active {slot} visual to face {direction}.");
                        if (slot == WeaponSlot.Ranged)
                        {
                            Assert.IsNotNull(heldWeaponVisual.ActiveMuzzleTransform);
                            Assert.Greater(
                                Vector3.Dot(heldWeaponVisual.ActiveMuzzleTransform.forward, expected),
                                0.98f,
                                $"Expected active ranged muzzle to face {direction}.");
                        }

                        time += 3f;
                        weapon.TickAction(3f, time);
                    }
                }
            }
            finally
            {
                Object.DestroyImmediate(projectilePrefab);
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void WeaponAimCommitmentKeepsVisualFacingWhileStrafing()
        {
            var root = new GameObject("WeaponAimCommitmentHarness");
            var projectilePrefab = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            projectilePrefab.AddComponent<ProjectileController>();
            try
            {
                var combat = root.AddComponent<RoomCombatController>();
                var player = new GameObject("PlayerCharacter");
                var visualRoot = new GameObject(VisualRootName);
                player.transform.SetParent(root.transform, false);
                visualRoot.transform.SetParent(player.transform, false);
                var weapon = player.AddComponent<PlayerWeaponController>();
                weapon.Configure(null, combat, projectilePrefab);
                weapon.SetActiveWeaponSlot(WeaponSlot.Ranged);
                var locomotionAnimator = player.AddComponent<PlayerLocomotionAnimator>();
                locomotionAnimator.Bind(null, visualRoot.transform);
                locomotionAnimator.BindGameplay(weapon, null);
                locomotionAnimator.Configure(0.05f, 3600f, PlayerMovementController.DefaultSpeedMetersPerSecond, 100f);
                locomotionAnimator.ResetTracking();

                Assert.IsTrue(weapon.TryAttack(AttackKind.Light, Vector2.up, 0f));
                player.transform.position = new Vector3(0.4f, 0f, 0f);
                locomotionAnimator.Sample(0.1f);

                Assert.IsTrue(locomotionAnimator.IsTargetLockedForLocomotion);
                Assert.Greater(Vector3.Dot(visualRoot.transform.forward, Vector3.forward), 0.99f);
                Assert.Greater(locomotionAnimator.LockedRelativeMove.x, 0.95f);
                Assert.Less(Mathf.Abs(locomotionAnimator.LockedRelativeMove.y), 0.15f);
            }
            finally
            {
                Object.DestroyImmediate(projectilePrefab);
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void PlayerDamageFeedbackRestoresFullMeshyMaterialArrays()
        {
            var player = new GameObject("PlayerCharacter");
            var child = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var firstMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            var secondMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            try
            {
                child.transform.SetParent(player.transform, false);
                var renderer = child.GetComponent<Renderer>();
                renderer.sharedMaterials = new[] { firstMaterial, secondMaterial };
                var feedback = player.AddComponent<PlayerDamageFeedbackController>();
                var applyFlash = typeof(PlayerDamageFeedbackController).GetMethod(
                    "ApplyFlash",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.IsNotNull(applyFlash);

                applyFlash.Invoke(feedback, new object[] { true });
                Assert.AreEqual(2, renderer.sharedMaterials.Length);
                Assert.AreNotSame(firstMaterial, renderer.sharedMaterials[0]);
                Assert.AreNotSame(secondMaterial, renderer.sharedMaterials[1]);

                applyFlash.Invoke(feedback, new object[] { false });
                var restoredMaterials = renderer.sharedMaterials;
                Assert.AreEqual(2, restoredMaterials.Length);
                Assert.AreSame(firstMaterial, restoredMaterials[0]);
                Assert.AreSame(secondMaterial, restoredMaterials[1]);
            }
            finally
            {
                Object.DestroyImmediate(player);
                Object.DestroyImmediate(firstMaterial);
                Object.DestroyImmediate(secondMaterial);
            }
        }

        [Test]
        public void RunningFbxIsConfiguredAsLoopedRootLockedGenericClip()
        {
            var importer = AssetImporter.GetAtPath(RunningFbxPath) as ModelImporter;
            Assert.IsNotNull(importer);
            Assert.IsTrue(importer.importAnimation);
            Assert.AreEqual(ModelImporterAnimationType.Generic, importer.animationType);
            Assert.AreEqual(WrapMode.Loop, importer.animationWrapMode);

            var clips = importer.clipAnimations;
            Assert.AreEqual(1, clips.Length);
            var clip = clips[0];
            Assert.AreEqual(RunClipName, clip.name);
            Assert.IsTrue(clip.loopTime);
            Assert.IsTrue(clip.loopPose);
            Assert.AreEqual(WrapMode.Loop, clip.wrapMode);
            Assert.IsTrue(clip.lockRootRotation);
            Assert.IsTrue(clip.lockRootHeightY);
            Assert.IsTrue(clip.lockRootPositionXZ);
        }

        [Test]
        public void GeneratedRollClipLocksRootLikePositionDrift()
        {
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(RollInPlaceClipPath);
            Assert.IsNotNull(clip);
            Assert.AreEqual(RollClipName, clip.name);
            var foundHipsVerticalBinding = false;

            foreach (var binding in AnimationUtility.GetCurveBindings(clip))
            {
                if (!IsRootLikePositionBinding(binding))
                {
                    continue;
                }

                var curve = AnimationUtility.GetEditorCurve(clip, binding);
                if (binding.path == "Armature/Hips" &&
                    (binding.propertyName == "m_LocalPosition.y" || binding.propertyName == "localPosition.y"))
                {
                    foundHipsVerticalBinding = true;
                }

                Assert.IsTrue(
                    curve == null || CurveRange(curve) <= RollRootDriftStripThresholdMeters,
                    $"Expected {binding.path} {binding.propertyName} to stay within {RollRootDriftStripThresholdMeters:0.##}m in the in-place roll clip.");
            }

            Assert.IsTrue(foundHipsVerticalBinding, "Expected the Meshy roll clip to include Armature/Hips vertical position binding.");
        }

        [Test]
        public void PlayerCharacterPrefabHasMeshyAnimatorVisual()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            Assert.IsNotNull(prefab);
            Assert.IsNotNull(prefab.GetComponent<PlaceholderPlayerController>());
            Assert.IsNotNull(prefab.GetComponent<CombatantHealth>());
            Assert.IsNotNull(prefab.GetComponent<PlayerMovementController>());
            Assert.IsNotNull(prefab.GetComponent<PlayerWeaponController>());
            Assert.IsNotNull(prefab.GetComponent<CapsuleCollider>());

            Assert.IsNull(prefab.transform.Find(LegacyCapsuleName));
            var visualRoot = prefab.transform.Find(VisualRootName);
            Assert.IsNotNull(visualRoot);
            Assert.AreEqual(1, prefab.GetComponentsInChildren<Transform>(includeInactive: true)
                .Count(child => child.name == VisualRootName));
            Assert.AreEqual(0, prefab.GetComponentsInChildren<PresentationVisualMarker>(includeInactive: true)
                .Count(marker => marker.Role == PresentationPrefabRole.Player));
            var meleeSockets = visualRoot.GetComponentsInChildren<Transform>(includeInactive: true)
                .Where(child => child.name == PlayerHeldWeaponVisualController.MeleeHandSocketName)
                .ToArray();
            var rangedHandSockets = visualRoot.GetComponentsInChildren<Transform>(includeInactive: true)
                .Where(child => child.name == PlayerHeldWeaponVisualController.RangedHandSocketName)
                .ToArray();
            var meleeHolsterSockets = visualRoot.GetComponentsInChildren<Transform>(includeInactive: true)
                .Where(child => child.name == PlayerHeldWeaponVisualController.MeleeHolsterSocketName)
                .ToArray();
            var rangedHolsterSockets = visualRoot.GetComponentsInChildren<Transform>(includeInactive: true)
                .Where(child => child.name == PlayerHeldWeaponVisualController.RangedHolsterSocketName)
                .ToArray();
            var rangedMuzzleSockets = visualRoot.GetComponentsInChildren<Transform>(includeInactive: true)
                .Where(child => child.name == PlayerHeldWeaponVisualController.RangedMuzzleSocketName)
                .ToArray();
            var shieldForearmSockets = visualRoot.GetComponentsInChildren<Transform>(includeInactive: true)
                .Where(child => child.name == PlayerHeldWeaponVisualController.ShieldForearmSocketName)
                .ToArray();
            var shieldBackSockets = visualRoot.GetComponentsInChildren<Transform>(includeInactive: true)
                .Where(child => child.name == PlayerHeldWeaponVisualController.ShieldBackSocketName)
                .ToArray();
            Assert.AreEqual(1, meleeSockets.Length);
            Assert.AreEqual(1, rangedHandSockets.Length);
            Assert.AreEqual(1, meleeHolsterSockets.Length);
            Assert.AreEqual(1, rangedHolsterSockets.Length);
            Assert.AreEqual(1, rangedMuzzleSockets.Length);
            Assert.LessOrEqual(shieldForearmSockets.Length, 1);
            Assert.LessOrEqual(shieldBackSockets.Length, 1);
            Assert.IsTrue(IsBoneNamed(meleeSockets[0].parent, "RightHand"));
            Assert.IsTrue(
                IsBoneNamed(rangedHandSockets[0].parent, "RightHand") || rangedHandSockets[0].parent == visualRoot,
                "Regenerated prefabs should parent the ranged hand socket to RightHand; older prefabs are repaired by PlayerHeldWeaponVisualController at runtime.");
            Assert.AreSame(visualRoot, meleeHolsterSockets[0].parent);
            Assert.AreSame(visualRoot, rangedHolsterSockets[0].parent);
            Assert.AreSame(rangedHandSockets[0], rangedMuzzleSockets[0].parent);
            if (shieldForearmSockets.Length > 0)
            {
                Assert.IsTrue(IsBoneNamed(shieldForearmSockets[0].parent, "LeftForeArm"));
            }

            if (shieldBackSockets.Length > 0)
            {
                Assert.IsTrue(IsBoneNamed(shieldBackSockets[0].parent, "Spine02"));
            }

            var locomotionAnimator = prefab.GetComponent<PlayerLocomotionAnimator>();
            var heldWeaponVisual = prefab.GetComponent<PlayerHeldWeaponVisualController>();
            var aimLockController = prefab.GetComponent<PlayerAimLockController>();
            var rangedHandPose = prefab.GetComponent<PlayerRangedHandPoseController>();
            var shieldGuardPose = prefab.GetComponent<PlayerShieldGuardPoseController>();
            var grounding = prefab.GetComponent<SimpleFullBodyGroundingController>();
            var animator = prefab.GetComponentInChildren<Animator>(includeInactive: true);
            var visualValidation = PlayerVisualAssemblyValidator.Validate(prefab, PlayerPrefabPath);
            Assert.IsFalse(visualValidation.HasErrors, visualValidation.ToReportString());
            Assert.IsTrue(visualValidation.BodyVisibleForDebug, visualValidation.ToReportString());
            Assert.IsTrue(visualValidation.AnimatorAvatarAssigned, visualValidation.ToReportString());
            Assert.IsTrue(visualValidation.AnimatorControllerAssigned, visualValidation.ToReportString());
            Assert.Greater(visualValidation.BodyRendererCount, 0, visualValidation.ToReportString());
            Assert.Greater(visualValidation.EnabledBodyRendererCount, 0, visualValidation.ToReportString());
            Assert.Greater(visualValidation.BodyRenderersWithMaterialCount, 0, visualValidation.ToReportString());
            Assert.Greater(visualValidation.BodyBoundsSize.y, 0.75f, visualValidation.ToReportString());
            Assert.AreEqual(0, visualValidation.MissingScriptCount, visualValidation.ToReportString());
            Assert.AreEqual(0, visualValidation.MissingReferenceCount, visualValidation.ToReportString());
            Assert.AreEqual(0, visualValidation.InvalidConstraintsCount, visualValidation.ToReportString());
            Assert.IsTrue(visualValidation.BodySkinnedAndAnimationReady, visualValidation.ToReportString());
            Assert.IsTrue(visualValidation.BodyWillDeformWithAnimator, visualValidation.ToReportString());
            Assert.IsTrue(visualValidation.BodyVisibleSkinnedMesh, visualValidation.ToReportString());
            Assert.IsFalse(visualValidation.BodyUsesStaticFallback, visualValidation.ToReportString());
            Assert.IsFalse(visualValidation.UsesTemporaryStaticFallback, visualValidation.ToReportString());
            Assert.Greater(visualValidation.SkinnedBodyRendererCount, 0, visualValidation.ToReportString());
            Assert.IsTrue(visualValidation.EquipmentVisualScaleValid, visualValidation.ToReportString());
            Assert.AreEqual(0, visualValidation.OversizedEquipmentCount, visualValidation.ToReportString());
            Assert.Greater(visualValidation.EquipmentRendererDetails.Count, 0, visualValidation.ToReportString());
            Assert.IsTrue(visualValidation.EquipmentRendererDetails.All(detail => detail.BoundsValid), visualValidation.ToReportString());
            Assert.IsTrue(visualValidation.EquipmentRendererDetails.All(detail =>
            {
                var finalWrapperScale = Vector3.Scale(detail.ParentLossyScale, detail.WrapperLocalScale);
                return Mathf.Max(
                    Mathf.Abs(finalWrapperScale.x),
                    Mathf.Abs(finalWrapperScale.y),
                    Mathf.Abs(finalWrapperScale.z)) < 2f;
            }), visualValidation.ToReportString());
            Assert.IsFalse(string.IsNullOrWhiteSpace(visualValidation.SelectedSkinnedBodyFbx), visualValidation.ToReportString());
            StringAssert.Contains("0604223747", visualValidation.SelectedSkinnedBodyFbx);
            StringAssert.Contains("Male Locomotion Pack", visualValidation.SelectedSkinnedBodyFbx);
            Assert.AreEqual(visualValidation.SelectedSkinnedBodyFbx, visualValidation.SelectedAvatarSource);
            Assert.IsTrue(visualValidation.SkinnedBodyRootBoneAssigned, visualValidation.ToReportString());
            Assert.Greater(visualValidation.SkinnedBodyBoneCount, 0, visualValidation.ToReportString());
            Assert.AreEqual(PlayerAnimationSystemMode.SimpleFullBodyAnimation, visualValidation.AnimationSystemMode, visualValidation.ToReportString());
            Assert.AreEqual(1, visualValidation.AnimatorLayerCount, visualValidation.ToReportString());
            Assert.IsFalse(visualValidation.AnimatorBaseLayerIkPass, visualValidation.ToReportString());
            Assert.IsFalse(visualValidation.RigBuilderEnabled, visualValidation.ToReportString());
            Assert.IsFalse(visualValidation.FootPlacementEnabled, visualValidation.ToReportString());
            Assert.IsFalse(visualValidation.HandIkEnabled, visualValidation.ToReportString());
            Assert.IsFalse(visualValidation.ShieldIkEnabled, visualValidation.ToReportString());
            Assert.IsFalse(visualValidation.SimpleModeHasActiveRigInfluence, visualValidation.ToReportString());

            Assert.IsNotNull(locomotionAnimator);
            Assert.IsNotNull(heldWeaponVisual);
            Assert.IsNotNull(aimLockController);
            Assert.IsNotNull(rangedHandPose);
            Assert.IsNotNull(shieldGuardPose);
            Assert.IsNotNull(grounding);
            Assert.IsTrue(grounding.GroundingEnabled);
            Assert.AreSame(visualRoot.transform, grounding.OffsetRoot);
            Assert.AreSame(prefab.transform, grounding.GroundReference);
            Assert.IsNotNull(grounding.MeasuredRoot);
            Assert.AreEqual("MainCharacter_MeshyModel", grounding.MeasuredRoot.name);
            Assert.AreSame(meleeSockets[0], heldWeaponVisual.MeleeHandSocket);
            Assert.AreSame(rangedHandSockets[0], heldWeaponVisual.RangedHandSocket);
            Assert.AreSame(meleeHolsterSockets[0], heldWeaponVisual.MeleeHolsterSocket);
            Assert.AreSame(rangedHolsterSockets[0], heldWeaponVisual.RangedHolsterSocket);
            Assert.AreSame(rangedMuzzleSockets[0], heldWeaponVisual.ActiveMuzzleTransform);
            if (shieldForearmSockets.Length > 0)
            {
                Assert.AreSame(shieldForearmSockets[0], heldWeaponVisual.ShieldForearmSocket);
            }

            if (shieldBackSockets.Length > 0)
            {
                Assert.AreSame(shieldBackSockets[0], heldWeaponVisual.ShieldBackSocket);
            }
            Assert.IsNotNull(animator);
            Assert.IsFalse(animator.applyRootMotion);
            Assert.IsTrue(IsDescendantOf(meleeSockets[0].parent, animator.transform));
            Assert.IsTrue(IsDescendantOf(rangedHandSockets[0].parent, animator.transform));
            if (shieldForearmSockets.Length > 0)
            {
                Assert.IsTrue(IsDescendantOf(shieldForearmSockets[0].parent, animator.transform));
            }

            if (shieldBackSockets.Length > 0)
            {
                Assert.IsTrue(IsDescendantOf(shieldBackSockets[0].parent, animator.transform));
            }

            var aimLockField = typeof(PlayerLocomotionAnimator).GetField(
                "aimLockController",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(aimLockField);
            Assert.AreSame(aimLockController, aimLockField.GetValue(locomotionAnimator));

            var controller = animator.runtimeAnimatorController as AnimatorController;
            Assert.IsNotNull(controller);
            Assert.AreEqual(1, controller.layers.Length);
            Assert.IsFalse(controller.layers[0].iKPass);
            AssertControllerParameter(controller, PlayerLocomotionAnimator.IsMovingParameter, AnimatorControllerParameterType.Bool);
            AssertControllerParameter(controller, PlayerLocomotionAnimator.MoveSpeedParameter, AnimatorControllerParameterType.Float);
            AssertControllerParameter(controller, PlayerLocomotionAnimator.ActionSpeedParameter, AnimatorControllerParameterType.Float);
            AssertControllerParameter(controller, PlayerLocomotionAnimator.RollTriggerParameter, AnimatorControllerParameterType.Trigger);
            AssertControllerParameter(controller, PlayerLocomotionAnimator.SlashTriggerParameter, AnimatorControllerParameterType.Trigger);
            AssertControllerParameter(controller, PlayerLocomotionAnimator.HitTriggerParameter, AnimatorControllerParameterType.Trigger);
            AssertControllerParameter(controller, PlayerLocomotionAnimator.DeathTriggerParameter, AnimatorControllerParameterType.Trigger);
            AssertControllerParameter(controller, PlayerLocomotionAnimator.IsDeadParameter, AnimatorControllerParameterType.Bool);
            AssertControllerParameter(controller, PlayerLocomotionAnimator.IsTargetLockedParameter, AnimatorControllerParameterType.Bool);
            AssertControllerParameter(controller, PlayerLocomotionAnimator.LockedMoveXParameter, AnimatorControllerParameterType.Float);
            AssertControllerParameter(controller, PlayerLocomotionAnimator.LockedMoveYParameter, AnimatorControllerParameterType.Float);

            var states = controller.layers[0].stateMachine.states.Select(child => child.state).ToArray();
            var idleState = FindState(states, "Idle");
            var walkState = FindState(states, "Walk");
            var runState = FindState(states, "Run");
            var rollState = FindState(states, "Roll");
            var attackState = FindState(states, "Attack");
            var guardBlockState = FindState(states, "GuardBlock");
            var hitState = FindState(states, "HitReaction");
            var deathState = FindState(states, "Death");
            Assert.IsNotNull(idleState.motion);
            Assert.IsNotNull(walkState.motion);
            Assert.IsNotNull(runState.motion);
            Assert.IsNull(states.FirstOrDefault(state => state.name == "LockedLocomotion"));
            Assert.IsNotNull(rollState.motion);
            Assert.IsNotNull(attackState.motion);
            Assert.IsNotNull(guardBlockState.motion);
            Assert.IsNotNull(hitState.motion);
            Assert.IsNotNull(deathState.motion);
            Assert.AreEqual(RollClipName, rollState.motion.name);
            Assert.AreEqual(RollInPlaceClipPath, AssetDatabase.GetAssetPath(rollState.motion));
            Assert.That(runState.motion.name, Does.Contain("Run"));
            Assert.IsNotNull(deathState);
            Assert.AreEqual(0, deathState.transitions.Length);

            AssertHasSimpleLocomotionTransition(idleState, "Walk", AnimatorConditionMode.Less, RunStartMoveSpeedThreshold);
            AssertHasSimpleLocomotionTransition(idleState, "Run", AnimatorConditionMode.Greater, RunStartMoveSpeedThreshold);
            AssertHasSimpleLocomotionTransition(walkState, "Run", AnimatorConditionMode.Greater, RunStartMoveSpeedThreshold);
            AssertHasSimpleLocomotionTransition(runState, "Walk", AnimatorConditionMode.Less, RunStartMoveSpeedThreshold);
            AssertHasActionExitTransition(rollState, "Run", AnimatorConditionMode.Greater);
            AssertHasActionExitTransition(attackState, "Run", AnimatorConditionMode.Greater);
            AssertHasActionExitTransition(hitState, "Run", AnimatorConditionMode.Greater);

            var canonicalMaterial = AssetDatabase.LoadAssetAtPath<Material>(CanonicalMaterialPath);
            Assert.IsNotNull(canonicalMaterial);
            Assert.IsTrue(HasTexture(canonicalMaterial));
            Assert.IsFalse(canonicalMaterial.IsKeywordEnabled("_EMISSION"));
            Assert.AreEqual(Color.black, canonicalMaterial.GetColor("_EmissionColor"));

            var renderers = visualRoot.GetComponentsInChildren<Renderer>(includeInactive: true);
            Assert.Greater(renderers.Length, 0);
            Assert.IsTrue(renderers.All(renderer => renderer.sharedMaterials.Length > 0));
            Assert.IsTrue(visualValidation.BodyRendererDetails
                .SelectMany(detail => detail.MaterialPaths)
                .All(path => path == CanonicalMaterialPath), visualValidation.ToReportString());
            Assert.IsTrue(visualValidation.BodyRendererDetails
                .SelectMany(detail => detail.MaterialNames)
                .All(name => !string.IsNullOrWhiteSpace(name)), visualValidation.ToReportString());
            Assert.AreEqual(0, visualRoot.GetComponentsInChildren<Collider>(includeInactive: true).Length);
        }

        [Test]
        public void PlayerCharacterSkinnedBodyUsesSkeletonAnimatedByAnimator()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            Assert.IsNotNull(prefab);

            var instance = Object.Instantiate(prefab);
            try
            {
                var animator = instance.GetComponentInChildren<Animator>(includeInactive: true);
                Assert.IsNotNull(animator);
                Assert.IsNotNull(animator.avatar);
                Assert.IsNotNull(animator.runtimeAnimatorController);

                var skinnedBody = instance.GetComponentsInChildren<SkinnedMeshRenderer>(includeInactive: true)
                    .FirstOrDefault(renderer =>
                        renderer != null &&
                        renderer.enabled &&
                        renderer.sharedMesh != null &&
                        renderer.rootBone != null &&
                        renderer.bones != null &&
                        renderer.bones.Length > 0);
                Assert.IsNotNull(skinnedBody);
                Assert.IsTrue(IsDescendantOf(skinnedBody.rootBone, animator.transform));
                Assert.IsTrue(skinnedBody.bones.All(bone => bone != null && IsDescendantOf(bone, animator.transform)));

                var movingBone = skinnedBody.bones.FirstOrDefault(bone => IsBoneNamed(bone, "RightFoot")) ??
                    skinnedBody.bones.FirstOrDefault(bone => IsBoneNamed(bone, "LeftFoot")) ??
                    skinnedBody.bones.FirstOrDefault();
                Assert.IsNotNull(movingBone);

                animator.Rebind();
                animator.Update(0f);
                var initialPosition = movingBone.position;
                var initialRotation = movingBone.rotation;

                animator.SetBool(PlayerLocomotionAnimator.IsMovingParameter, true);
                animator.SetBool(PlayerLocomotionAnimator.IsTargetLockedParameter, true);
                animator.SetFloat(PlayerLocomotionAnimator.MoveSpeedParameter, 1f);
                animator.SetFloat(PlayerLocomotionAnimator.LockedMoveXParameter, 0f);
                animator.SetFloat(PlayerLocomotionAnimator.LockedMoveYParameter, 1f);
                for (var index = 0; index < 20; index++)
                {
                    animator.Update(0.05f);
                }

                var movedDistance = Vector3.Distance(initialPosition, movingBone.position);
                var movedAngle = Quaternion.Angle(initialRotation, movingBone.rotation);
                Assert.IsTrue(
                    movedDistance > 0.001f || movedAngle > 0.1f,
                    $"Expected Animator locomotion to move a skinned body bone. Distance={movedDistance:0.####}, angle={movedAngle:0.####}");
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }

        private static void AssertControllerParameter(
            AnimatorController controller,
            string parameterName,
            AnimatorControllerParameterType parameterType)
        {
            Assert.IsTrue(controller.parameters.Any(parameter =>
                    parameter.name == parameterName && parameter.type == parameterType),
                $"Expected animator parameter {parameterName} ({parameterType}).");
        }

        private static bool HasTexture(Material material)
        {
            if (material == null)
            {
                return false;
            }

            return material.mainTexture != null ||
                (material.HasProperty("_BaseMap") && material.GetTexture("_BaseMap") != null);
        }

        private static AnimatorState FindState(AnimatorState[] states, string stateName)
        {
            var state = states.FirstOrDefault(candidate => candidate.name == stateName);
            Assert.IsNotNull(state, $"Expected animator state {stateName}.");
            return state;
        }

        private static void AssertHasLocomotionTransition(
            AnimatorState fromState,
            string toStateName,
            AnimatorConditionMode speedConditionMode,
            float threshold)
        {
            var transition = fromState.transitions.FirstOrDefault(candidate =>
                candidate.destinationState != null &&
                candidate.destinationState.name == toStateName &&
                HasCondition(candidate, PlayerLocomotionAnimator.IsTargetLockedParameter, AnimatorConditionMode.IfNot) &&
                HasCondition(candidate, PlayerLocomotionAnimator.IsMovingParameter, AnimatorConditionMode.If) &&
                HasSpeedCondition(candidate, speedConditionMode, threshold));
            Assert.IsNotNull(
                transition,
                $"Expected {fromState.name} -> {toStateName} transition using {speedConditionMode} {threshold:0.###}.");
            Assert.IsFalse(transition.hasExitTime);
        }

        private static void AssertHasSimpleLocomotionTransition(
            AnimatorState fromState,
            string toStateName,
            AnimatorConditionMode speedConditionMode,
            float threshold)
        {
            var transition = fromState.transitions.FirstOrDefault(candidate =>
                candidate.destinationState != null &&
                candidate.destinationState.name == toStateName &&
                HasCondition(candidate, PlayerLocomotionAnimator.IsMovingParameter, AnimatorConditionMode.If) &&
                HasSpeedCondition(candidate, speedConditionMode, threshold) &&
                !candidate.conditions.Any(condition => condition.parameter == PlayerLocomotionAnimator.IsTargetLockedParameter));
            Assert.IsNotNull(
                transition,
                $"Expected simple {fromState.name} -> {toStateName} transition using {speedConditionMode} {threshold:0.###} without target-lock conditions.");
            Assert.IsFalse(transition.hasExitTime);
        }

        private static void AssertHasActionExitTransition(
            AnimatorState fromState,
            string toStateName,
            AnimatorConditionMode speedConditionMode)
        {
            var transition = fromState.transitions.FirstOrDefault(candidate =>
                candidate.destinationState != null &&
                candidate.destinationState.name == toStateName &&
                HasCondition(candidate, PlayerLocomotionAnimator.IsMovingParameter, AnimatorConditionMode.If) &&
                HasCondition(candidate, PlayerLocomotionAnimator.IsDeadParameter, AnimatorConditionMode.IfNot) &&
                HasSpeedCondition(candidate, speedConditionMode, RunStartMoveSpeedThreshold));
            Assert.IsNotNull(transition, $"Expected action exit {fromState.name} -> {toStateName}.");
            Assert.IsTrue(transition.hasExitTime);
        }

        private static void AssertHasLockedTransition(AnimatorState fromState, AnimatorState lockedState)
        {
            var transition = fromState.transitions.FirstOrDefault(candidate =>
                candidate.destinationState == lockedState &&
                HasCondition(candidate, PlayerLocomotionAnimator.IsTargetLockedParameter, AnimatorConditionMode.If) &&
                HasCondition(candidate, PlayerLocomotionAnimator.IsDeadParameter, AnimatorConditionMode.IfNot));
            Assert.IsNotNull(transition, $"Expected {fromState.name} -> {lockedState.name} lock transition.");
            Assert.IsFalse(transition.hasExitTime);
        }

        private static void AssertHasLockedExitTransition(AnimatorState lockedState, string toStateName)
        {
            var transition = lockedState.transitions.FirstOrDefault(candidate =>
                candidate.destinationState != null &&
                candidate.destinationState.name == toStateName &&
                HasCondition(candidate, PlayerLocomotionAnimator.IsTargetLockedParameter, AnimatorConditionMode.IfNot));
            Assert.IsNotNull(transition, $"Expected {lockedState.name} -> {toStateName} unlock transition.");
            Assert.IsFalse(transition.hasExitTime);
        }

        private static void AssertHasActionExitToLockedTransition(AnimatorState fromState, AnimatorState lockedState)
        {
            var transition = fromState.transitions.FirstOrDefault(candidate =>
                candidate.destinationState == lockedState &&
                HasCondition(candidate, PlayerLocomotionAnimator.IsTargetLockedParameter, AnimatorConditionMode.If) &&
                HasCondition(candidate, PlayerLocomotionAnimator.IsDeadParameter, AnimatorConditionMode.IfNot));
            Assert.IsNotNull(transition, $"Expected action exit {fromState.name} -> {lockedState.name}.");
            Assert.IsTrue(transition.hasExitTime);
        }

        private static bool HasCondition(AnimatorStateTransition transition, string parameterName, AnimatorConditionMode mode)
        {
            return transition.conditions.Any(condition =>
                condition.parameter == parameterName &&
                condition.mode == mode);
        }

        private static bool HasSpeedCondition(
            AnimatorStateTransition transition,
            AnimatorConditionMode mode,
            float expectedThreshold)
        {
            return transition.conditions.Any(condition =>
                condition.parameter == PlayerLocomotionAnimator.MoveSpeedParameter &&
                condition.mode == mode &&
                Mathf.Abs(condition.threshold - expectedThreshold) <= 0.001f);
        }

        private static bool PendingTrigger(PlayerLocomotionAnimator animator, string fieldName)
        {
            var field = typeof(PlayerLocomotionAnimator).GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field);
            return (bool)field.GetValue(animator);
        }

        private static bool IsBoneNamed(Transform bone, string expectedName)
        {
            if (bone == null)
            {
                return false;
            }

            var actualName = bone.name;
            var separator = actualName.LastIndexOf(':');
            if (separator >= 0 && separator < actualName.Length - 1)
            {
                actualName = actualName[(separator + 1)..];
            }

            if (expectedName == "Spine02" && actualName == "Spine2")
            {
                return true;
            }

            return actualName == expectedName;
        }

        private static bool IsDescendantOf(Transform child, Transform ancestor)
        {
            var cursor = child;
            while (cursor != null)
            {
                if (cursor == ancestor)
                {
                    return true;
                }

                cursor = cursor.parent;
            }

            return false;
        }

        private static void InvokePrivateLateUpdate(object target)
        {
            var method = target.GetType().GetMethod(
                "LateUpdate",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(method);
            method.Invoke(target, null);
        }

        private static GameplayInputSnapshot Snapshot(Vector2 move)
        {
            return new GameplayInputSnapshot(
                move,
                Vector2.zero,
                interactPressed: false,
                swapWeaponPressed: false,
                lightAttackPressed: false,
                heavyAttackPressed: false,
                useActiveItemPressed: false,
                useConsumableCardPressed: false,
                guardHeld: false,
                pausePressed: false,
                rollPressed: false,
                lockTargetPressed: false);
        }

        private static EnemyRuntimeController CreateEnemy(Transform parent, Vector3 localPosition)
        {
            var enemyObject = new GameObject("MainCharacterAnimationEnemy");
            enemyObject.transform.SetParent(parent, false);
            enemyObject.transform.localPosition = localPosition;
            var enemy = enemyObject.AddComponent<EnemyRuntimeController>();
            enemy.Configure(null, null, EnemyCatalog.CreateRuntimeDefault().Resolve("spawnEnemyNormal"), null);
            return enemy;
        }

        private static void AddEnemy(RoomCombatController combat, EnemyRuntimeController enemy)
        {
            var field = typeof(RoomCombatController).GetField("enemies", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field);
            var enemies = (List<EnemyRuntimeController>)field.GetValue(combat);
            enemies.Add(enemy);
        }

        private static bool IsRootLikePositionBinding(EditorCurveBinding binding)
        {
            if (binding.type != typeof(Transform))
            {
                return false;
            }

            if (binding.propertyName != "m_LocalPosition.x" &&
                binding.propertyName != "m_LocalPosition.y" &&
                binding.propertyName != "m_LocalPosition.z" &&
                binding.propertyName != "localPosition.x" &&
                binding.propertyName != "localPosition.y" &&
                binding.propertyName != "localPosition.z")
            {
                return false;
            }

            if (string.IsNullOrEmpty(binding.path))
            {
                return true;
            }

            var segments = binding.path.Split('/');
            var normalized = segments[^1];
            var separator = normalized.LastIndexOf(':');
            if (separator >= 0 && separator < normalized.Length - 1)
            {
                normalized = normalized[(separator + 1)..];
            }

            return normalized == "Armature" ||
                normalized == "Hips" ||
                normalized == "Pelvis" ||
                normalized == "Root" ||
                normalized == "RootNode";
        }

        private static bool IsConstant(AnimationCurve curve)
        {
            if (curve == null || curve.length <= 1)
            {
                return true;
            }

            var firstValue = curve.keys[0].value;
            return curve.keys.All(key => Mathf.Abs(key.value - firstValue) <= 0.0001f);
        }

        private static float CurveRange(AnimationCurve curve)
        {
            if (curve == null || curve.length <= 1)
            {
                return 0f;
            }

            var minimum = curve.keys[0].value;
            var maximum = minimum;
            foreach (var key in curve.keys)
            {
                minimum = Mathf.Min(minimum, key.value);
                maximum = Mathf.Max(maximum, key.value);
            }

            return maximum - minimum;
        }
    }
}
