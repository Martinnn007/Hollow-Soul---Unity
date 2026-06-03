using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using Hollow.Combat;
using Hollow.Data.Definitions;
using Hollow.Editor.Generation;
using Hollow.Entities;
using Hollow.Input;
using Hollow.Presentation;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
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
            Assert.IsNotNull(presentationCatalog);
            PresentationContentProvider.Configure(presentationCatalog);

            var player = new GameObject("PlayerCharacter");
            var rightHand = new GameObject("RightHand");
            var socket = new GameObject(PlayerHeldWeaponVisualController.MeleeHandSocketName);
            try
            {
                rightHand.transform.SetParent(player.transform, false);
                socket.transform.SetParent(rightHand.transform, false);
                var weapon = player.AddComponent<PlayerWeaponController>();
                var heldWeaponVisual = player.AddComponent<PlayerHeldWeaponVisualController>();
                heldWeaponVisual.BindMeleeHandSocket(socket.transform);
                heldWeaponVisual.Bind(weapon);

                Assert.IsTrue(heldWeaponVisual.IsUsingHandAttachedMeleeVisual);
                Assert.AreSame(socket.transform, heldWeaponVisual.MeleeHandSocket);
                Assert.IsNotNull(heldWeaponVisual.ActiveWeaponVisual);
                Assert.IsNotNull(heldWeaponVisual.HolsteredRangedVisual);
                Assert.AreEqual(1, socket.GetComponentsInChildren<PresentationVisualMarker>(includeInactive: true)
                    .Count(marker => marker.Role == PresentationPrefabRole.WeaponMelee));
                AssertVisibleMeshyMeleeWeaponVisual(heldWeaponVisual.ActiveWeaponVisual);
                Assert.AreEqual(1, player.GetComponentsInChildren<PresentationVisualMarker>(includeInactive: true)
                    .Count(marker => marker.Role == PresentationPrefabRole.WeaponRanged));

                weapon.SetActiveWeaponSlot(WeaponSlot.Ranged);

                Assert.IsFalse(heldWeaponVisual.IsUsingHandAttachedMeleeVisual);
                Assert.IsNotNull(heldWeaponVisual.ActiveWeaponVisual);
                Assert.IsNotNull(heldWeaponVisual.HolsteredMeleeVisual);
                Assert.IsNull(heldWeaponVisual.HolsteredRangedVisual);
                Assert.AreEqual(0, socket.GetComponentsInChildren<PresentationVisualMarker>(includeInactive: true)
                    .Count(marker => marker.Role == PresentationPrefabRole.WeaponMelee));
                Assert.AreEqual(1, player.GetComponentsInChildren<PresentationVisualMarker>(includeInactive: true)
                    .Count(marker => marker.Role == PresentationPrefabRole.WeaponRanged));
                Assert.AreEqual(1, player.GetComponentsInChildren<PresentationVisualMarker>(includeInactive: true)
                    .Count(marker => marker.Role == PresentationPrefabRole.WeaponMelee));
                AssertVisibleMeshyMeleeWeaponVisual(heldWeaponVisual.HolsteredMeleeVisual);

                weapon.SetActiveWeaponSlot(WeaponSlot.Melee);
                weapon.SetActiveWeaponSlot(WeaponSlot.Ranged);

                Assert.AreEqual(1, player.GetComponentsInChildren<PresentationVisualMarker>(includeInactive: true)
                    .Count(marker => marker.Role == PresentationPrefabRole.WeaponRanged));
                Assert.AreEqual(1, player.GetComponentsInChildren<PresentationVisualMarker>(includeInactive: true)
                    .Count(marker => marker.Role == PresentationPrefabRole.WeaponMelee));
                Assert.AreSame(heldWeaponVisual.RangedHandSocket, heldWeaponVisual.ActiveWeaponVisual.transform.parent);
            }
            finally
            {
                Object.DestroyImmediate(player);
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

        private static void AssertVisibleMeshyMeleeWeaponVisual(GameObject root)
        {
            Assert.IsNotNull(root);
            Assert.IsTrue(UsesMeshyMeleeWeaponMaterial(root));
            var renderers = root.GetComponentsInChildren<Renderer>(includeInactive: false)
                .Where(renderer => renderer.enabled && renderer.gameObject.activeInHierarchy)
                .ToArray();
            Assert.Greater(renderers.Length, 0, "Melee weapon visual should have an active renderer in gameplay hierarchy.");
            var bounds = Encapsulate(renderers);
            Assert.Greater(Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z), 0.5f);
            Assert.Greater(bounds.size.sqrMagnitude, 0.25f);
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
            Assert.AreEqual(1, meleeSockets.Length);
            Assert.AreEqual(1, rangedHandSockets.Length);
            Assert.AreEqual(1, meleeHolsterSockets.Length);
            Assert.AreEqual(1, rangedHolsterSockets.Length);
            Assert.AreEqual(1, rangedMuzzleSockets.Length);
            Assert.AreEqual("RightHand", meleeSockets[0].parent.name);
            Assert.AreSame(visualRoot, rangedHandSockets[0].parent);
            Assert.AreSame(visualRoot, meleeHolsterSockets[0].parent);
            Assert.AreSame(visualRoot, rangedHolsterSockets[0].parent);
            Assert.AreSame(rangedHandSockets[0], rangedMuzzleSockets[0].parent);

            var locomotionAnimator = prefab.GetComponent<PlayerLocomotionAnimator>();
            var heldWeaponVisual = prefab.GetComponent<PlayerHeldWeaponVisualController>();
            var aimLockController = prefab.GetComponent<PlayerAimLockController>();
            var animator = prefab.GetComponentInChildren<Animator>(includeInactive: true);
            Assert.IsNotNull(locomotionAnimator);
            Assert.IsNotNull(heldWeaponVisual);
            Assert.IsNotNull(aimLockController);
            Assert.AreSame(meleeSockets[0], heldWeaponVisual.MeleeHandSocket);
            Assert.AreSame(rangedHandSockets[0], heldWeaponVisual.RangedHandSocket);
            Assert.AreSame(meleeHolsterSockets[0], heldWeaponVisual.MeleeHolsterSocket);
            Assert.AreSame(rangedHolsterSockets[0], heldWeaponVisual.RangedHolsterSocket);
            Assert.AreSame(rangedMuzzleSockets[0], heldWeaponVisual.ActiveMuzzleTransform);
            Assert.IsNotNull(animator);
            Assert.IsFalse(animator.applyRootMotion);
            var aimLockField = typeof(PlayerLocomotionAnimator).GetField(
                "aimLockController",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(aimLockField);
            Assert.AreSame(aimLockController, aimLockField.GetValue(locomotionAnimator));

            var controller = animator.runtimeAnimatorController as AnimatorController;
            Assert.IsNotNull(controller);
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
            var lockedState = FindState(states, "LockedLocomotion");
            var rollState = FindState(states, "Roll");
            var slashState = FindState(states, "LeftSlash");
            var hitState = FindState(states, "HitReaction");
            var deadState = FindState(states, "Dead");
            Assert.IsNotNull(idleState.motion);
            Assert.IsNotNull(walkState.motion);
            Assert.IsNotNull(runState.motion);
            var lockedBlendTree = lockedState.motion as BlendTree;
            Assert.IsNotNull(lockedBlendTree);
            Assert.AreEqual(BlendTreeType.FreeformCartesian2D, lockedBlendTree.blendType);
            Assert.AreEqual(PlayerLocomotionAnimator.LockedMoveXParameter, lockedBlendTree.blendParameter);
            Assert.AreEqual(PlayerLocomotionAnimator.LockedMoveYParameter, lockedBlendTree.blendParameterY);
            Assert.GreaterOrEqual(lockedBlendTree.children.Length, 17);
            Assert.IsNotNull(rollState.motion);
            Assert.IsNotNull(slashState.motion);
            Assert.IsNotNull(hitState.motion);
            Assert.IsNotNull(deadState.motion);
            Assert.AreEqual(RollClipName, rollState.motion.name);
            Assert.AreEqual(RollInPlaceClipPath, AssetDatabase.GetAssetPath(rollState.motion));
            Assert.AreEqual(RunClipName, runState.motion.name);
            Assert.IsNotNull(deadState);
            Assert.AreEqual(0, deadState.transitions.Length);

            AssertHasLocomotionTransition(idleState, "Walk", AnimatorConditionMode.Less, RunStartMoveSpeedThreshold);
            AssertHasLocomotionTransition(idleState, "Run", AnimatorConditionMode.Greater, RunStartMoveSpeedThreshold);
            AssertHasLocomotionTransition(walkState, "Run", AnimatorConditionMode.Greater, RunStartMoveSpeedThreshold);
            AssertHasLocomotionTransition(runState, "Walk", AnimatorConditionMode.Less, RunStartMoveSpeedThreshold);
            AssertHasLockedTransition(idleState, lockedState);
            AssertHasLockedTransition(walkState, lockedState);
            AssertHasLockedTransition(runState, lockedState);
            AssertHasLockedExitTransition(lockedState, "Idle");
            AssertHasLockedExitTransition(lockedState, "Walk");
            AssertHasLockedExitTransition(lockedState, "Run");
            AssertHasActionExitToLockedTransition(rollState, lockedState);
            AssertHasActionExitToLockedTransition(slashState, lockedState);
            AssertHasActionExitToLockedTransition(hitState, lockedState);
            AssertHasActionExitTransition(rollState, "Run", AnimatorConditionMode.Greater);
            AssertHasActionExitTransition(slashState, "Run", AnimatorConditionMode.Greater);
            AssertHasActionExitTransition(hitState, "Run", AnimatorConditionMode.Greater);

            var canonicalMaterial = AssetDatabase.LoadAssetAtPath<Material>(CanonicalMaterialPath);
            Assert.IsNotNull(canonicalMaterial);
            Assert.IsTrue(HasTexture(canonicalMaterial));
            Assert.IsFalse(canonicalMaterial.IsKeywordEnabled("_EMISSION"));
            Assert.AreEqual(Color.black, canonicalMaterial.GetColor("_EmissionColor"));

            var renderers = visualRoot.GetComponentsInChildren<Renderer>(includeInactive: true);
            Assert.Greater(renderers.Length, 0);
            Assert.IsTrue(renderers.All(renderer => renderer.sharedMaterials.Length > 0));
            Assert.IsTrue(renderers
                .SelectMany(renderer => renderer.sharedMaterials)
                .All(material => material != null && AssetDatabase.GetAssetPath(material) == CanonicalMaterialPath));
            Assert.IsTrue(renderers
                .SelectMany(renderer => renderer.sharedMaterials)
                .All(HasTexture));
            Assert.AreEqual(0, visualRoot.GetComponentsInChildren<Collider>(includeInactive: true).Length);
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
