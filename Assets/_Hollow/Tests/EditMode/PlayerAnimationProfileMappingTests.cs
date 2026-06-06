using System.Linq;
using Hollow.Combat;
using Hollow.Data.Definitions;
using Hollow.Editor.Generation;
using Hollow.Editor.Validation;
using Hollow.Input;
using Hollow.Presentation;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using Object = UnityEngine.Object;

namespace Hollow.Tests.EditMode
{
    public sealed class PlayerAnimationProfileMappingTests
    {
        [Test]
        public void GeneratorMapsAvailableMixamoPacksIntoProfileCapabilities()
        {
            var catalog = PlayerAnimationProfileAssetGenerator.GenerateProfiles();

            Assert.IsNotNull(catalog);
            foreach (var profileId in PlayerAnimationProfileAssetGenerator.RequiredProfileIds())
            {
                Assert.IsNotNull(catalog.Resolve(profileId), profileId.ToString());
            }

            var unarmed = catalog.Resolve(PlayerAnimationProfileId.UnarmedLocomotion);
            var sword = catalog.Resolve(PlayerAnimationProfileId.SwordShieldCombat);
            var great = catalog.Resolve(PlayerAnimationProfileId.GreatSwordCombat);
            var rifle = catalog.Resolve(PlayerAnimationProfileId.RifleCombat);
            var pistol = catalog.Resolve(PlayerAnimationProfileId.PistolCombat);

            Assert.IsFalse(unarmed.AllowsShieldGuard);
            Assert.IsTrue(sword.AllowsShieldInHand);
            Assert.IsTrue(sword.AllowsShieldGuard);
            Assert.IsNotEmpty(sword.ShieldGuardClips);
            AssertProfileClipPath(sword.ShieldGuardClips, "SwordShield_ShieldGuard_02", "Assets/_Hollow/Animation Packs/Pro Sword and Shield Pack/sword and shield block idle.fbx");
            AssertProfileClipPath(sword.StrafingClips, "SwordShield_GuardStrafe_Left", "Assets/_Hollow/Animation Packs/Pro Sword and Shield Pack/sword and shield strafe (2).fbx");
            AssertProfileClipPath(sword.StrafingClips, "SwordShield_GuardStrafe_Right", "Assets/_Hollow/Animation Packs/Pro Sword and Shield Pack/sword and shield strafe.fbx");
            AssertProfileClipPath(sword.TurnClips, "SwordShield_Turn_01", "Assets/_Hollow/Animation Packs/Pro Sword and Shield Pack/sword and shield turn.fbx");
            AssertProfileClipPath(sword.TurnClips, "SwordShield_Turn_02", "Assets/_Hollow/Animation Packs/Pro Sword and Shield Pack/sword and shield turn (2).fbx");
            AssertProfileClipPath(sword.AttackClips, "SwordShield_Attack_09", "Assets/_Hollow/Animation Packs/Pro Sword and Shield Pack/sword and shield slash.fbx");
            AssertProfileClipPath(sword.ImpactClips, "SwordShield_Impact_02", "Assets/_Hollow/Animation Packs/Pro Sword and Shield Pack/sword and shield impact (3).fbx");
            AssertProfileClipPath(sword.ImpactClips, "SwordShield_Impact_03", "Assets/_Hollow/Animation Packs/Pro Sword and Shield Pack/sword and shield impact.fbx");
            AssertRootPositionXZIsNotBaked("Assets/_Hollow/Animation Packs/Pro Sword and Shield Pack/sword and shield run.fbx");
            Assert.IsFalse(great.AllowsShieldGuard);
            Assert.IsTrue(great.RequiresTwoHandedWeapon);
            Assert.IsNotEmpty(great.WeaponBlockClips);
            Assert.IsFalse(rifle.AllowsShieldInHand);
            Assert.IsTrue(rifle.UsesRangedAim);
            Assert.IsFalse(pistol.AllowsShieldInHand);
            Assert.IsTrue(pistol.UsesRangedAim);

            AssertCompleteDirectionalProfile(rifle, expectTemporaryPlaceholders: false);
            AssertCompleteDirectionalProfile(pistol, expectTemporaryPlaceholders: false);
            Assert.IsTrue(unarmed.UsesTemporaryPlaceholders);
            Assert.IsTrue(sword.UsesTemporaryPlaceholders);
            Assert.IsTrue(great.UsesTemporaryPlaceholders);
        }

        [Test]
        public void GeneratorWritesHumanReadableProfileMappingReport()
        {
            PlayerAnimationProfileAssetGenerator.GenerateProfiles();

            Assert.IsTrue(System.IO.File.Exists(PlayerAnimationProfileAssetGenerator.ProfileMappingReportPath));
            var report = System.IO.File.ReadAllText(PlayerAnimationProfileAssetGenerator.ProfileMappingReportPath);

            StringAssert.Contains("Hollow Soul Player Animation Profile Mapping Report", report);
            StringAssert.Contains("ReportVersion: 1", report);
            StringAssert.Contains("AnimationPackRoot: Assets/_Hollow/Animation Packs", report);
            StringAssert.Contains("ProfileCatalogPath: Assets/_Hollow/Data/AnimationProfiles/PlayerAnimationProfileCatalog.asset", report);
            StringAssert.Contains("UnarmedLocomotionProfile", report);
            StringAssert.Contains("SwordShieldCombatProfile", report);
            StringAssert.Contains("GreatSwordCombatProfile", report);
            StringAssert.Contains("RifleCombatProfile", report);
            StringAssert.Contains("PistolCombatProfile", report);
            StringAssert.Contains("CapabilityFlags", report);
            StringAssert.Contains("AllowsShieldInHand", report);
            StringAssert.Contains("AllowsShieldGuard", report);
            StringAssert.Contains("MissingRequiredClipSlots", report);
            StringAssert.Contains("MissingOptionalClipSlots", report);
            StringAssert.Contains("TemporaryPlaceholderClipSlots_NON_PRODUCTION", report);
            StringAssert.Contains("TEMPORARY_PLACEHOLDER_NON_PRODUCTION", report);
            StringAssert.Contains("RifleReal8WayCoverage", report);
            StringAssert.Contains("GreatSwordWeaponBlockOnly", report);
            StringAssert.Contains("UnarmedSafeFallback", report);
        }

        [Test]
        public void StaticScannerWritesLicenseIndependentPreviewReport()
        {
            var scan = PlayerAnimationProfileStaticScanner.Scan();

            Assert.IsTrue(scan.DetectedPackFolders.Any(path => path.EndsWith("Male Locomotion Pack", System.StringComparison.Ordinal)));
            Assert.IsTrue(scan.DetectedPackFolders.Any(path => path.EndsWith("Rifle 8-Way Locomotion Pack", System.StringComparison.Ordinal)));
            Assert.Greater(scan.CandidateFbxFiles.Count, 0);
            foreach (var profileId in PlayerAnimationProfileAssetGenerator.RequiredProfileIds())
            {
                Assert.IsNotNull(scan.Resolve(profileId), profileId.ToString());
            }

            Assert.IsTrue(PlayerAnimationProfileStaticScanner.TryWriteStaticPreviewReport());
            Assert.IsTrue(System.IO.File.Exists(PlayerAnimationProfileAssetGenerator.StaticProfileMappingReportPath));
            var report = System.IO.File.ReadAllText(PlayerAnimationProfileAssetGenerator.StaticProfileMappingReportPath);

            StringAssert.Contains("ReportType: STATIC_PREVIEW", report);
            StringAssert.Contains("UnarmedLocomotionProfile", report);
            StringAssert.Contains("SwordShieldCombatProfile", report);
            StringAssert.Contains("GreatSwordCombatProfile", report);
            StringAssert.Contains("RifleCombatProfile", report);
            StringAssert.Contains("PistolCombatProfile", report);
            StringAssert.Contains("CapabilityFlags", report);
            StringAssert.Contains("AllowsShieldInHand", report);
            StringAssert.Contains("AllowsShieldGuard", report);
            StringAssert.Contains("Profile: RifleCombatProfile", report);
            StringAssert.Contains("Profile: PistolCombatProfile", report);
            StringAssert.Contains("Profile: SwordShieldCombatProfile", report);
            StringAssert.Contains("Profile: GreatSwordCombatProfile", report);
            StringAssert.Contains("Profile: UnarmedLocomotionProfile", report);
            StringAssert.Contains("MissingRequiredSlots", report);
            StringAssert.Contains("MissingOptionalSlots", report);
            StringAssert.Contains("PlaceholderNeededSlots_NON_PRODUCTION", report);
            StringAssert.Contains("PLACEHOLDER_NEEDED_NON_PRODUCTION", report);
            StringAssert.Contains("This report does not prove Avatar/Humanoid import validity", report);
            StringAssert.Contains("prefab generation", report);
            StringAssert.Contains("debug scene generation", report);
            StringAssert.Contains("gameplay correctness", report);
        }

        [Test]
        public void EquipmentScaleReportListsRuntimeScaleDiagnostics()
        {
            var player = new GameObject("PlayerCharacter");
            try
            {
                player.AddComponent<PlayerAnimationProfileController>();
                var rightHand = new GameObject("RightHand");
                rightHand.transform.SetParent(player.transform, false);
                rightHand.transform.localScale = Vector3.one * 100f;
                var wrapper = new GameObject(PlayerHeldWeaponVisualController.ActiveMeleeWeaponVisualName);
                wrapper.transform.SetParent(rightHand.transform, false);
                wrapper.transform.localScale = Vector3.one;
                wrapper.AddComponent<PresentationVisualMarker>().Configure(PresentationPrefabRole.WeaponMelee, isFallback: false);
                var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cube.transform.SetParent(wrapper.transform, false);

                var validation = new PlayerVisualAssemblyValidationResult("SyntheticEquipmentScaleHarness");
                PlayerVisualAssemblyValidator.WriteEquipmentScaleReport(player, validation);

                Assert.IsTrue(System.IO.File.Exists(PlayerVisualAssemblyValidator.EquipmentScaleReportPath));
                var report = System.IO.File.ReadAllText(PlayerVisualAssemblyValidator.EquipmentScaleReportPath);
                StringAssert.Contains("Hollow Soul Equipment Scale Report", report);
                StringAssert.Contains("RootCauseClassification", report);
                StringAssert.Contains("A_PrefabNotRegeneratedOrStaleWrapperScales", report);
                StringAssert.Contains("C_EquipmentInheritsMixamo100xScale", report);
                StringAssert.Contains("H_DuplicateStaleEquipmentInstances", report);
                StringAssert.Contains("EquipmentInstances", report);
                StringAssert.Contains("role: HeldWeapon", report);
                StringAssert.Contains("rootLocalScale", report);
                StringAssert.Contains("parentLossyScale", report);
                StringAssert.Contains("rendererBoundsSize", report);
                StringAssert.Contains("normalizationPassCount", report);
            }
            finally
            {
                Object.DestroyImmediate(player);
            }
        }

        [Test]
        public void DebugSceneGenerationReferencesAllProfiles()
        {
            var catalog = PlayerAnimationProfileAssetGenerator.GenerateProfiles();

            PlayerAnimationProfileAssetGenerator.GenerateDebugScene(catalog);

            Assert.IsTrue(System.IO.File.Exists(PlayerAnimationProfileAssetGenerator.DebugScenePath));
            Assert.IsEmpty(PlayerAnimationProfileAssetGenerator.MissingGeneratedProfileAssetPaths());
            Assert.AreEqual(PlayerAnimationProfileAssetGenerator.RequiredProfileIds().Length, catalog.Profiles.Count);

            var scene = EditorSceneManager.OpenScene(PlayerAnimationProfileAssetGenerator.DebugScenePath);
            Assert.IsTrue(scene.IsValid());
            var profileController = Object.FindFirstObjectByType<PlayerAnimationProfileController>();
            var overlay = Object.FindFirstObjectByType<Locomotion360ProfileDebugOverlay>();
            Assert.IsNotNull(profileController);
            Assert.IsNotNull(overlay);
            Assert.AreSame(catalog, profileController.Catalog);
            foreach (var profileId in PlayerAnimationProfileAssetGenerator.RequiredProfileIds())
            {
                Assert.IsNotNull(profileController.Catalog.Resolve(profileId), profileId.ToString());
            }
        }

        [Test]
        public void RifleAndPistolProfilesRejectShieldGuardButKeepRangedSlowdown()
        {
            AssertRangedProfileRejectsShieldGuard(PlayerAnimationProfileId.RifleCombat, "training_rifle", WeaponCategory.Gun);
            AssertRangedProfileRejectsShieldGuard(PlayerAnimationProfileId.PistolCombat, "starter_pistol", WeaponCategory.Gun);
        }

        [Test]
        public void GreatSwordProfileRejectsShieldGuardAndKeepsShieldOnBack()
        {
            var rig = CreateGameplayRig(PlayerAnimationProfileId.GreatSwordCombat, WeaponSlot.Melee, "iron_cleaver", WeaponCategory.Blade, isDoubleHanded: true);
            try
            {
                rig.Defense.Tick(GuardSnapshot(Vector2.right), 0.1f, 1f);
                rig.Visual.ForceRangedAimPose(Vector2.up);

                Assert.IsFalse(rig.Defense.IsGuarding);
                Assert.IsFalse(rig.Defense.CanUseShieldGuard);
                Assert.IsTrue(rig.Profile.RequiresTwoHandedWeapon);
                Assert.AreSame(rig.BackSocket, rig.Visual.CurrentShieldSocket);
            }
            finally
            {
                rig.Destroy();
            }
        }

        [Test]
        public void SwordShieldProfileAllowsShieldGuardAndGuardSlowdown()
        {
            var rig = CreateGameplayRig(PlayerAnimationProfileId.SwordShieldCombat, WeaponSlot.Melee, "starter_blade", WeaponCategory.Blade, isDoubleHanded: false);
            try
            {
                rig.Defense.Tick(GuardSnapshot(Vector2.right), 0.1f, 1f);
                rig.Coordinator.SamplePose(0.2f);

                Assert.IsTrue(rig.Defense.IsGuarding);
                Assert.IsTrue(rig.Defense.CanUseShieldGuard);
                Assert.AreSame(rig.ForearmSocket, rig.Visual.CurrentShieldSocket);
                Assert.Greater(rig.LeftHandIk.weight, 0.1f);
                Assert.AreEqual(
                    PlayerMovementController.DefaultSpeedMetersPerSecond * rig.Defense.GuardMoveMultiplier,
                    rig.Movement.SpeedMetersPerSecond,
                    0.001f);
            }
            finally
            {
                rig.Destroy();
            }
        }

        [Test]
        public void UnarmedAndUnresolvedProfilesRejectShieldGuard()
        {
            var unarmedRig = CreateGameplayRig(PlayerAnimationProfileId.UnarmedLocomotion, WeaponSlot.Melee, "starter_blade", WeaponCategory.Blade, isDoubleHanded: false);
            try
            {
                unarmedRig.ProfileController.SetDebugProfileOverride(unarmedRig.Catalog.Resolve(PlayerAnimationProfileId.UnarmedLocomotion));
                unarmedRig.Defense.Tick(GuardSnapshot(Vector2.up), 0.1f, 1f);
                Assert.IsFalse(unarmedRig.Defense.IsGuarding);
            }
            finally
            {
                unarmedRig.Destroy();
            }

            var unresolvedRig = CreateGameplayRig(PlayerAnimationProfileId.SwordShieldCombat, WeaponSlot.Melee, "starter_blade", WeaponCategory.Blade, isDoubleHanded: false);
            try
            {
                unresolvedRig.ProfileController.Configure(null);
                unresolvedRig.Defense.Tick(GuardSnapshot(Vector2.up), 0.1f, 1f);
                Assert.IsFalse(unresolvedRig.Defense.IsGuarding);
                Assert.AreEqual(PlayerAnimationProfileId.UnarmedLocomotion, unresolvedRig.ProfileController.CurrentProfileId);
            }
            finally
            {
                unresolvedRig.Destroy();
            }
        }

        private static void AssertRangedProfileRejectsShieldGuard(PlayerAnimationProfileId profileId, string weaponId, WeaponCategory category)
        {
            var rig = CreateGameplayRig(profileId, WeaponSlot.Ranged, weaponId, category, isDoubleHanded: false);
            try
            {
                rig.Defense.Tick(GuardSnapshot(Vector2.right), 0.1f, 1f);
                rig.Coordinator.SamplePose(0.2f);

                Assert.IsFalse(rig.Defense.IsGuarding);
                Assert.IsFalse(rig.Defense.CanUseShieldGuard);
                Assert.AreSame(rig.BackSocket, rig.Visual.CurrentShieldSocket);
                Assert.AreEqual(0f, rig.LeftHandIk.weight, 0.001f);

                rig.Weapon.TickInput(HeldRangedLightSnapshot(Vector2.up), 0f, 2f);
                Assert.IsTrue(rig.Weapon.IsRangedHeldAttackPoseActive);
                Assert.AreEqual(
                    PlayerMovementController.DefaultSpeedMetersPerSecond * rig.Defense.GuardMoveMultiplier,
                    rig.Movement.SpeedMetersPerSecond,
                    0.001f);
            }
            finally
            {
                rig.Destroy();
            }
        }

        private static void AssertCompleteDirectionalProfile(PlayerAnimationProfileDefinition profile, bool expectTemporaryPlaceholders)
        {
            foreach (PlayerAnimationDirection direction in System.Enum.GetValues(typeof(PlayerAnimationDirection)))
            {
                Assert.IsTrue(profile.TryGetDirectionalClipSet(direction, out var clipSet), $"{profile.ProfileName} {direction}");
                Assert.IsNotNull(clipSet.WalkClip, $"{profile.ProfileName} Walk {direction}");
                Assert.IsNotNull(clipSet.RunClip, $"{profile.ProfileName} Run {direction}");
                if (!expectTemporaryPlaceholders)
                {
                    Assert.IsFalse(clipSet.WalkUsesTemporaryPlaceholder, $"{profile.ProfileName} Walk {direction}");
                    Assert.IsFalse(clipSet.RunUsesTemporaryPlaceholder, $"{profile.ProfileName} Run {direction}");
                }
            }
        }

        private static void AssertProfileClipPath(
            System.Collections.Generic.IReadOnlyList<AnimationClip> clips,
            string clipName,
            string expectedAssetPath)
        {
            var clip = clips.FirstOrDefault(candidate => candidate != null && candidate.name == clipName);
            Assert.IsNotNull(clip, $"Expected profile clip {clipName}.");
            Assert.AreEqual(expectedAssetPath, AssetDatabase.GetAssetPath(clip));
        }

        private static void AssertRootPositionXZIsNotBaked(string assetPath)
        {
            var importer = AssetImporter.GetAtPath(assetPath) as ModelImporter;
            Assert.IsNotNull(importer, assetPath);
            var clips = importer.clipAnimations != null && importer.clipAnimations.Length > 0
                ? importer.clipAnimations
                : importer.defaultClipAnimations;
            Assert.IsNotEmpty(clips, assetPath);
            Assert.IsFalse(clips[0].lockRootPositionXZ, $"{assetPath} should not bake XZ root position into pose.");
            Assert.IsTrue(clips[0].keepOriginalPositionXZ, $"{assetPath} should keep original XZ root position.");
        }

        private static GameplayInputSnapshot GuardSnapshot(Vector2 aim)
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
                guardHeld: true);
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

        private static GameplayRig CreateGameplayRig(
            PlayerAnimationProfileId activeProfile,
            WeaponSlot activeSlot,
            string activeWeaponId,
            WeaponCategory activeWeaponCategory,
            bool isDoubleHanded)
        {
            var root = new GameObject($"ProfileRig.{activeProfile}");
            var player = new GameObject("PlayerCharacter");
            player.transform.SetParent(root.transform, false);
            var weaponCatalog = ScriptableObject.CreateInstance<WeaponCatalogDefinition>();
            var melee = CreateWeapon(
                activeSlot == WeaponSlot.Melee ? activeWeaponId : "starter_blade",
                "Melee",
                WeaponSlot.Melee,
                activeSlot == WeaponSlot.Melee ? activeWeaponCategory : WeaponCategory.Blade,
                activeSlot == WeaponSlot.Melee && isDoubleHanded);
            var ranged = CreateWeapon(
                activeSlot == WeaponSlot.Ranged ? activeWeaponId : "starter_pistol",
                "Ranged",
                WeaponSlot.Ranged,
                activeSlot == WeaponSlot.Ranged ? activeWeaponCategory : WeaponCategory.Gun,
                false);
            weaponCatalog.Configure("profile_gate_test_weapons", new[] { melee, ranged });

            var catalog = CreateProfileCatalog();
            var weapon = player.AddComponent<PlayerWeaponController>();
            weapon.ConfigureBuildStats(1f, 0, 0, 100f, 0f, melee.WeaponId, ranged.WeaponId, activeSlot, 100f, weaponCatalog);
            var profileController = player.AddComponent<PlayerAnimationProfileController>();
            profileController.Configure(catalog);
            profileController.Bind(weapon);
            profileController.SetDebugProfileOverride(catalog.Resolve(activeProfile));

            var defense = player.AddComponent<PlayerDefenseController>();
            defense.Configure(0);
            var movement = player.AddComponent<PlayerMovementController>();
            var visual = player.AddComponent<PlayerHeldWeaponVisualController>();
            visual.Bind(weapon);

            var meleeHand = CreateSocket(player.transform, "MeleeHand");
            var rangedHand = CreateSocket(player.transform, "RangedHand");
            var meleeHolster = CreateSocket(player.transform, "MeleeHolster");
            var rangedHolster = CreateSocket(player.transform, "RangedHolster");
            var muzzle = CreateSocket(rangedHand, "Muzzle");
            var forearm = CreateSocket(player.transform, "ForearmShield");
            var back = CreateSocket(player.transform, "BackShield");
            visual.BindWeaponSockets(meleeHand, rangedHand, meleeHolster, rangedHolster, muzzle, forearm, back);

            var shieldPose = player.AddComponent<PlayerShieldGuardPoseController>();
            shieldPose.Bind(null, defense, visual);
            var coordinator = player.AddComponent<PlayerAnimationPoseCoordinator>();
            var leftIk = new GameObject("LeftHandIK").AddComponent<TwoBoneIKConstraint>();
            var rightIk = new GameObject("RightHandIK").AddComponent<TwoBoneIKConstraint>();
            var chest = new GameObject("ChestAim").AddComponent<MultiAimConstraint>();
            leftIk.transform.SetParent(player.transform, false);
            rightIk.transform.SetParent(player.transform, false);
            chest.transform.SetParent(player.transform, false);
            ConfigureTestTwoBoneIk(leftIk, player.transform, "Left");
            ConfigureTestTwoBoneIk(rightIk, player.transform, "Right");
            ConfigureTestMultiAim(chest, player.transform);
            coordinator.Bind(null, null, weapon, defense, null, visual, null, shieldPose);
            coordinator.BindRigConstraints(rightIk, leftIk, chest);
            coordinator.BindTargets(
                CreateSocket(player.transform, "RightHandTarget"),
                CreateSocket(player.transform, "LeftHandTarget"),
                CreateSocket(player.transform, "ChestTarget"),
                CreateSocket(player.transform, "ResponseTarget"),
                CreateSocket(player.transform, "LeftFootTarget"),
                CreateSocket(player.transform, "RightFootTarget"));
            coordinator.Configure(10f, 10f, 10f, PlayerMovementController.DefaultSpeedMetersPerSecond);

            return new GameplayRig(root, catalog, profileController, catalog.Resolve(activeProfile), weapon, defense, movement, visual, coordinator, leftIk, forearm, back, weaponCatalog, melee, ranged);
        }

        private static void ConfigureTestTwoBoneIk(TwoBoneIKConstraint constraint, Transform parent, string prefix)
        {
            constraint.data.root = CreateSocket(parent, prefix + "UpperArm");
            constraint.data.mid = CreateSocket(parent, prefix + "Forearm");
            constraint.data.tip = CreateSocket(parent, prefix + "Hand");
            constraint.data.target = CreateSocket(parent, prefix + "HandTarget");
            constraint.data.hint = CreateSocket(parent, prefix + "ElbowHint");
            constraint.data.targetPositionWeight = 1f;
            constraint.data.targetRotationWeight = 1f;
            constraint.data.hintWeight = 0.75f;
        }

        private static void ConfigureTestMultiAim(MultiAimConstraint constraint, Transform parent)
        {
            var sourceObjects = new WeightedTransformArray(1);
            sourceObjects[0] = new WeightedTransform(CreateSocket(parent, "ChestAimSource"), 1f);
            constraint.data.constrainedObject = CreateSocket(parent, "Chest");
            constraint.data.sourceObjects = sourceObjects;
            constraint.data.aimAxis = MultiAimConstraintData.Axis.Z;
            constraint.data.upAxis = MultiAimConstraintData.Axis.Y;
            constraint.data.worldUpType = MultiAimConstraintData.WorldUpType.SceneUp;
            constraint.data.worldUpAxis = MultiAimConstraintData.Axis.Y;
        }

        private static PlayerAnimationProfileCatalogDefinition CreateProfileCatalog()
        {
            var unarmed = CreateProfile(PlayerAnimationProfileId.UnarmedLocomotion, false, false, false, false);
            var sword = CreateProfile(PlayerAnimationProfileId.SwordShieldCombat, true, true, false, false);
            var great = CreateProfile(PlayerAnimationProfileId.GreatSwordCombat, false, false, true, false);
            var rifle = CreateProfile(PlayerAnimationProfileId.RifleCombat, false, false, false, true);
            var pistol = CreateProfile(PlayerAnimationProfileId.PistolCombat, false, false, false, true);
            var catalog = ScriptableObject.CreateInstance<PlayerAnimationProfileCatalogDefinition>();
            catalog.Configure("profile_gate_test_catalog", new[] { unarmed, sword, great, rifle, pistol }, unarmed);
            return catalog;
        }

        private static PlayerAnimationProfileDefinition CreateProfile(
            PlayerAnimationProfileId profileId,
            bool allowsShieldInHand,
            bool allowsShieldGuard,
            bool requiresTwoHanded,
            bool usesRangedAim)
        {
            var profile = ScriptableObject.CreateInstance<PlayerAnimationProfileDefinition>();
            profile.Configure(
                profileId,
                profileId + "Profile",
                allowsShieldInHand,
                allowsShieldGuard,
                requiresTwoHanded,
                usesRangedAim,
                nextUsesFootIk: true,
                nextUsesTorsoAim: allowsShieldGuard || usesRangedAim || requiresTwoHanded,
                nextIdleClip: null,
                nextDirectionalClips: Enumerable.Empty<DirectionalAnimationClipSet>(),
                nextStrafingClips: null,
                nextTurnClips: null,
                nextDrawClips: null,
                nextSheatheClips: null,
                nextAttackClips: null,
                nextFireClips: null,
                nextShieldGuardClips: allowsShieldGuard ? new[] { new AnimationClip { name = "RuntimeShieldGuard" } } : null,
                nextWeaponBlockClips: requiresTwoHanded ? new[] { new AnimationClip { name = "RuntimeWeaponBlock" } } : null,
                nextImpactClips: null,
                nextDeathClips: null,
                nextJumpClips: null,
                nextCrouchClips: null,
                nextMissingRequiredClipSlots: null,
                nextMissingOptionalClipSlots: null,
                nextPlaceholderClipSlots: null,
                nextMappedClipReports: null);
            return profile;
        }

        private static WeaponDefinition CreateWeapon(string id, string displayName, WeaponSlot slot, WeaponCategory category, bool isDoubleHanded)
        {
            var weapon = ScriptableObject.CreateInstance<WeaponDefinition>();
            weapon.Configure(
                id,
                displayName,
                slot,
                category,
                nextIsDoubleHandedForPresentation: isDoubleHanded);
            return weapon;
        }

        private static Transform CreateSocket(Transform parent, string name)
        {
            var socket = new GameObject(name).transform;
            socket.SetParent(parent, false);
            return socket;
        }

        private sealed class GameplayRig
        {
            public GameplayRig(
                GameObject root,
                PlayerAnimationProfileCatalogDefinition catalog,
                PlayerAnimationProfileController profileController,
                PlayerAnimationProfileDefinition profile,
                PlayerWeaponController weapon,
                PlayerDefenseController defense,
                PlayerMovementController movement,
                PlayerHeldWeaponVisualController visual,
                PlayerAnimationPoseCoordinator coordinator,
                TwoBoneIKConstraint leftHandIk,
                Transform forearmSocket,
                Transform backSocket,
                params Object[] ownedAssets)
            {
                Root = root;
                Catalog = catalog;
                ProfileController = profileController;
                Profile = profile;
                Weapon = weapon;
                Defense = defense;
                Movement = movement;
                Visual = visual;
                Coordinator = coordinator;
                LeftHandIk = leftHandIk;
                ForearmSocket = forearmSocket;
                BackSocket = backSocket;
                OwnedAssets = ownedAssets;
            }

            public GameObject Root { get; }

            public PlayerAnimationProfileCatalogDefinition Catalog { get; }

            public PlayerAnimationProfileController ProfileController { get; }

            public PlayerAnimationProfileDefinition Profile { get; }

            public PlayerWeaponController Weapon { get; }

            public PlayerDefenseController Defense { get; }

            public PlayerMovementController Movement { get; }

            public PlayerHeldWeaponVisualController Visual { get; }

            public PlayerAnimationPoseCoordinator Coordinator { get; }

            public TwoBoneIKConstraint LeftHandIk { get; }

            public Transform ForearmSocket { get; }

            public Transform BackSocket { get; }

            private Object[] OwnedAssets { get; }

            public void Destroy()
            {
                Object.DestroyImmediate(Root);
                foreach (var asset in OwnedAssets)
                {
                    if (asset != null)
                    {
                        Object.DestroyImmediate(asset);
                    }
                }
            }
        }
    }
}
