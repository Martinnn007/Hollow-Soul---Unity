using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Hollow.Combat;
using Hollow.Data.Definitions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using Object = UnityEngine.Object;

namespace Hollow.Editor.Generation
{
    public static class PlayerAnimationProfileAssetGenerator
    {
        public const string AnimationPackRoot = "Assets/_Hollow/Animation Packs";
        public const string ProfileDirectory = "Assets/_Hollow/Data/AnimationProfiles";
        public const string ProfileCatalogPath = ProfileDirectory + "/PlayerAnimationProfileCatalog.asset";
        public const string ProfileMappingReportPath = ProfileDirectory + "/ProfileMappingReport.txt";
        public const string StaticProfileMappingReportPath = ProfileDirectory + "/ProfileMappingReport.StaticPreview.txt";
        public const string GeneratedPlaceholderDirectory = ProfileDirectory + "/GeneratedTemporaryDirectionalPlaceholders";
        public const string DebugScenePath = "Assets/_Hollow/Scenes/DeveloperLab/Locomotion360ProfileDebug.unity";
        public const string HollowMainRigPath = AnimationPackRoot + "/Hollow_Main_Rig.fbx";
        public const string HollowMainModelDirectory = AnimationPackRoot + "/Hollow_Main_Model";
        public const string HollowMainModelObjPath = AnimationPackRoot + "/Hollow_Main_Model/Meshy_AI_Neon_Exoskeleton_0603210958_texture.obj";
        public const string HollowMainModelTexturePath = AnimationPackRoot + "/Hollow_Main_Model/Meshy_AI_Neon_Exoskeleton_0603210958_texture.png";
        public const string SkinnedBodyFbxSearchPattern = "Meshy_AI_Neon_Exoskeleton_*texture*.fbx";
        public const float SkinnedBodyCentimeterImportScale = 100f;
        public const float TargetSkinnedBodyHeightMeters = 1.78f;

        private const string CatalogId = "player_animation_profiles_v1";
        private const string PlayerPrefabPath = "Assets/_Hollow/Prefabs/Player/PlayerCharacter.prefab";
        private const string LocomotionPack = AnimationPackRoot + "/Male Locomotion Pack";
        private const string SwordShieldPack = AnimationPackRoot + "/Pro Sword and Shield Pack";
        private const string GreatSwordPack = AnimationPackRoot + "/Great Sword Pack";
        private const string RiflePack = AnimationPackRoot + "/Rifle 8-Way Locomotion Pack";
        private const string ShooterPack = AnimationPackRoot + "/Shooter Pack";
        private const string PistolPack = AnimationPackRoot + "/Pistol_Handgun Locomotion Pack";
        private static readonly string[] SkinnedBodyPriorityFolders =
        {
            HollowMainModelDirectory,
            LocomotionPack,
            RiflePack,
            SwordShieldPack,
            GreatSwordPack,
            PistolPack,
            ShooterPack,
            AnimationPackRoot + "/Locomotion Pack"
        };

        [MenuItem("Hollow/Animation/Generate Player Animation Profiles")]
        public static void GenerateMenu()
        {
            var catalog = GenerateProfiles();
            GenerateDebugScene(catalog);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"Generated player animation profiles at {ProfileDirectory}.");
        }

        public static PlayerAnimationProfileCatalogDefinition GenerateProfiles()
        {
            Directory.CreateDirectory(ProfileDirectory);
            Directory.CreateDirectory(GeneratedPlaceholderDirectory);
            var sharedAvatar = EnsureSharedAvatar();

            var profiles = new[]
            {
                GenerateUnarmed(sharedAvatar),
                GenerateSwordShield(sharedAvatar),
                GenerateGreatSword(sharedAvatar),
                GenerateRifle(sharedAvatar),
                GeneratePistol(sharedAvatar)
            };

            var catalog = LoadOrCreate<PlayerAnimationProfileCatalogDefinition>(ProfileCatalogPath);
            catalog.Configure(
                CatalogId,
                profiles,
                profiles.FirstOrDefault(profile => profile.ProfileId == PlayerAnimationProfileId.UnarmedLocomotion));
            EditorUtility.SetDirty(catalog);
            TryWriteProfileMappingReport(catalog);
            return catalog;
        }

        public static PlayerAnimationProfileCatalogDefinition LoadCatalog()
        {
            return AssetDatabase.LoadAssetAtPath<PlayerAnimationProfileCatalogDefinition>(ProfileCatalogPath);
        }

        public static PlayerAnimationProfileId[] RequiredProfileIds()
        {
            return new[]
            {
                PlayerAnimationProfileId.UnarmedLocomotion,
                PlayerAnimationProfileId.SwordShieldCombat,
                PlayerAnimationProfileId.GreatSwordCombat,
                PlayerAnimationProfileId.RifleCombat,
                PlayerAnimationProfileId.PistolCombat
            };
        }

        public static string ProfilePath(PlayerAnimationProfileId profileId)
        {
            return $"{ProfileDirectory}/{profileId}Profile.asset";
        }

        public static IReadOnlyList<string> MissingGeneratedProfileAssetPaths()
        {
            return RequiredProfileIds()
                .Select(ProfilePath)
                .Concat(new[] { ProfileCatalogPath })
                .Where(path => !File.Exists(path))
                .ToArray();
        }

        public static bool TryWriteProfileMappingReport(PlayerAnimationProfileCatalogDefinition catalog)
        {
            try
            {
                if (catalog == null)
                {
                    return false;
                }

                Directory.CreateDirectory(ProfileDirectory);
                File.WriteAllText(ProfileMappingReportPath, BuildProfileMappingReport(catalog));
                AssetDatabase.ImportAsset(ProfileMappingReportPath);
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Could not write animation profile mapping report at {ProfileMappingReportPath}: {exception.Message}");
                return false;
            }
        }

        public static string BuildProfileMappingReport(PlayerAnimationProfileCatalogDefinition catalog)
        {
            var builder = new StringBuilder();
            builder.AppendLine("Hollow Soul Player Animation Profile Mapping Report");
            builder.AppendLine("ReportVersion: 1");
            builder.AppendLine($"GeneratedAtUtc: {DateTime.UtcNow:O}");
            builder.AppendLine($"UnityVersion: {Application.unityVersion}");
            builder.AppendLine($"AnimationPackRoot: {AnimationPackRoot}");
            builder.AppendLine($"ProfileCatalogPath: {ProfileCatalogPath}");
            builder.AppendLine($"SELECTED_SKINNED_BODY_FBX: {ResolveSelectedSkinnedBodyFbxPath() ?? "<none>"}");
            builder.AppendLine($"SELECTED_AVATAR_SOURCE: {ResolveSharedAvatarSourcePath()}");
            builder.AppendLine($"SELECTED_SKINNED_BODY_SCALE: {ResolveSelectedSkinnedBodyLocalScale():0.###}");
            builder.AppendLine($"FallbackProfile: {ProfileName(catalog.FallbackProfile)}");
            builder.AppendLine();
            builder.AppendLine("SkinnedBodyCandidates:");
            foreach (var candidate in BuildSkinnedBodyCandidateReportLines())
            {
                builder.AppendLine($"- {candidate}");
            }

            builder.AppendLine();
            AppendAnimationPackValidationReport(builder);
            builder.AppendLine();
            builder.AppendLine("GeneratedProfileAssets:");
            foreach (var profileId in RequiredProfileIds())
            {
                builder.AppendLine($"- {profileId}: {ProfilePath(profileId)}");
            }

            var unarmed = catalog.Resolve(PlayerAnimationProfileId.UnarmedLocomotion);
            var swordShield = catalog.Resolve(PlayerAnimationProfileId.SwordShieldCombat);
            var greatSword = catalog.Resolve(PlayerAnimationProfileId.GreatSwordCombat);
            var rifle = catalog.Resolve(PlayerAnimationProfileId.RifleCombat);
            var pistol = catalog.Resolve(PlayerAnimationProfileId.PistolCombat);

            builder.AppendLine();
            builder.AppendLine("SummaryChecks:");
            builder.AppendLine($"- RifleReal8WayCoverage: {Bool(HasReal8WayCoverage(rifle))}");
            builder.AppendLine($"- SwordShieldHasBlockGuardClips: {Bool(swordShield != null && swordShield.ShieldGuardClips.Count > 0)}");
            builder.AppendLine($"- GreatSwordWeaponBlockOnly: {Bool(greatSword != null && greatSword.WeaponBlockClips.Count > 0 && greatSword.ShieldGuardClips.Count == 0 && !greatSword.AllowsShieldGuard)}");
            builder.AppendLine($"- RifleShieldGuardDisabled: {Bool(rifle != null && !rifle.AllowsShieldGuard && !rifle.AllowsShieldInHand)}");
            builder.AppendLine($"- PistolShieldGuardDisabled: {Bool(pistol != null && !pistol.AllowsShieldGuard && !pistol.AllowsShieldInHand)}");
            builder.AppendLine($"- GreatSwordShieldGuardDisabled: {Bool(greatSword != null && !greatSword.AllowsShieldGuard && !greatSword.AllowsShieldInHand)}");
            builder.AppendLine($"- UnarmedSafeFallback: {Bool(unarmed != null && !unarmed.AllowsShieldGuard && !unarmed.AllowsShieldInHand && catalog.FallbackProfile == unarmed)}");
            builder.AppendLine();

            foreach (var profileId in RequiredProfileIds())
            {
                AppendProfileReport(builder, catalog.Resolve(profileId));
            }

            return builder.ToString();
        }

        private static void AppendProfileReport(StringBuilder builder, PlayerAnimationProfileDefinition profile)
        {
            builder.AppendLine($"Profile: {ProfileName(profile)}");
            if (profile == null)
            {
                builder.AppendLine("- WARNING: Profile asset is missing.");
                builder.AppendLine();
                return;
            }

            builder.AppendLine($"- ProfileId: {profile.ProfileId}");
            builder.AppendLine($"- ProfileName: {profile.ProfileName}");
            builder.AppendLine("- CapabilityFlags:");
            builder.AppendLine($"  AllowsShieldInHand: {Bool(profile.AllowsShieldInHand)}");
            builder.AppendLine($"  AllowsShieldGuard: {Bool(profile.AllowsShieldGuard)}");
            builder.AppendLine($"  RequiresTwoHandedWeapon: {Bool(profile.RequiresTwoHandedWeapon)}");
            builder.AppendLine($"  UsesRangedAim: {Bool(profile.UsesRangedAim)}");
            builder.AppendLine($"  UsesFootIk: {Bool(profile.UsesFootIk)}");
            builder.AppendLine($"  UsesTorsoAim: {Bool(profile.UsesTorsoAim)}");
            builder.AppendLine($"- DirectionalCoverageReal8Way: {Bool(HasReal8WayCoverage(profile))}");
            AppendStringSection(builder, "MappedRealFbxClips", RealMappedClipReports(profile));
            AppendStringSection(builder, "MissingRequiredClipSlots", profile.MissingRequiredClipSlots);
            AppendStringSection(builder, "MissingOptionalClipSlots", profile.MissingOptionalClipSlots);
            AppendStringSection(
                builder,
                "TemporaryPlaceholderClipSlots_NON_PRODUCTION",
                profile.PlaceholderClipSlots.Select(slot => $"{slot} (TEMPORARY_PLACEHOLDER_NON_PRODUCTION)"));
            AppendProfileWarnings(builder, profile);
            builder.AppendLine();
        }

        private static void AppendProfileWarnings(StringBuilder builder, PlayerAnimationProfileDefinition profile)
        {
            var warnings = new List<string>();
            if (profile.HasMissingRequiredClips)
            {
                warnings.Add("Missing required clip slots; profile needs final animation coverage.");
            }

            if (profile.UsesTemporaryPlaceholders)
            {
                warnings.Add("Uses temporary placeholders; these are diagnostic-only and not production animation.");
            }

            if (profile.ProfileId == PlayerAnimationProfileId.GreatSwordCombat &&
                profile.WeaponBlockClips.Count > 0 &&
                (profile.AllowsShieldGuard || profile.ShieldGuardClips.Count > 0))
            {
                warnings.Add("GreatSword weapon-block clips must not be treated as shield guard.");
            }

            if ((profile.ProfileId == PlayerAnimationProfileId.RifleCombat ||
                    profile.ProfileId == PlayerAnimationProfileId.PistolCombat ||
                    profile.ProfileId == PlayerAnimationProfileId.GreatSwordCombat) &&
                (profile.AllowsShieldGuard || profile.AllowsShieldInHand))
            {
                warnings.Add("Non-shield profile unexpectedly allows shield-in-hand or shield guard.");
            }

            AppendStringSection(builder, "NotesWarnings", warnings);
        }

        private static void AppendStringSection(StringBuilder builder, string title, IEnumerable<string> values)
        {
            builder.AppendLine($"- {title}:");
            var compact = (values ?? Enumerable.Empty<string>())
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Distinct(StringComparer.Ordinal)
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();
            if (compact.Length == 0)
            {
                builder.AppendLine("  - none");
                return;
            }

            foreach (var value in compact)
            {
                builder.AppendLine($"  - {value}");
            }
        }

        private static IEnumerable<string> RealMappedClipReports(PlayerAnimationProfileDefinition profile)
        {
            return profile?.MappedClipReports
                .Where(report => !report.Contains("TEMP placeholder", StringComparison.OrdinalIgnoreCase)) ??
                Enumerable.Empty<string>();
        }

        private static bool HasReal8WayCoverage(PlayerAnimationProfileDefinition profile)
        {
            if (profile == null)
            {
                return false;
            }

            foreach (PlayerAnimationDirection direction in Enum.GetValues(typeof(PlayerAnimationDirection)))
            {
                if (!profile.TryGetDirectionalClipSet(direction, out var clipSet) ||
                    clipSet.WalkClip == null ||
                    clipSet.RunClip == null ||
                    clipSet.WalkUsesTemporaryPlaceholder ||
                    clipSet.RunUsesTemporaryPlaceholder)
                {
                    return false;
                }
            }

            return true;
        }

        private static string ProfileName(PlayerAnimationProfileDefinition profile)
        {
            return profile != null ? $"{profile.ProfileName} ({profile.ProfileId})" : "none";
        }

        private static string Bool(bool value)
        {
            return value ? "yes" : "no";
        }

        public static void GenerateDebugScene(PlayerAnimationProfileCatalogDefinition catalog = null)
        {
            catalog ??= LoadCatalog() ?? GenerateProfiles();
            Directory.CreateDirectory(Path.GetDirectoryName(DebugScenePath) ?? string.Empty);
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Locomotion360Debug.FlatFloor";
            floor.transform.position = new Vector3(0f, -0.05f, 0f);
            floor.transform.localScale = new Vector3(16f, 0.1f, 16f);

            var playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            if (playerPrefab == null)
            {
                throw new InvalidOperationException($"Cannot generate {DebugScenePath}: rebuilt player prefab is missing at {PlayerPrefabPath}.");
            }

            var player = PrefabUtility.InstantiatePrefab(playerPrefab) as GameObject;
            player ??= Object.Instantiate(playerPrefab);
            player.name = "PlayerCharacter.ProfileDebug";
            player.transform.position = Vector3.zero;
            var profileController = player.GetComponent<PlayerAnimationProfileController>() ??
                player.AddComponent<PlayerAnimationProfileController>();
            profileController.Configure(catalog);
            profileController.Bind(player.GetComponent<PlayerWeaponController>());
            var heldVisual = player.GetComponent<PlayerHeldWeaponVisualController>();
            var locomotion = player.GetComponent<PlayerLocomotionAnimator>();
            var poseCoordinator = player.GetComponent<PlayerAnimationPoseCoordinator>();
            var footPlacement = player.GetComponent<PlayerFootPlacementController>();

            var cameraObject = new GameObject("Locomotion360Debug.Camera");
            var camera = cameraObject.AddComponent<Camera>();
            camera.transform.position = new Vector3(0f, 7.5f, -8f);
            camera.transform.rotation = Quaternion.Euler(55f, 0f, 0f);
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;

            var lightObject = new GameObject("Locomotion360Debug.DirectionalLight");
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.15f;
            light.transform.rotation = Quaternion.Euler(50f, -35f, 0f);

            var overlayObject = new GameObject("Locomotion360Debug.ProfileOverlay");
            var overlay = overlayObject.AddComponent<Locomotion360ProfileDebugOverlay>();
            overlay.Configure(
                catalog,
                profileController,
                locomotion,
                poseCoordinator,
                footPlacement,
                player.GetComponent<PlayerWeaponController>());

            if (heldVisual != null)
            {
                EditorUtility.SetDirty(heldVisual);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, DebugScenePath);
        }

        private static PlayerAnimationProfileDefinition GenerateUnarmed(Avatar sharedAvatar)
        {
            var context = new ProfileBuildContext(PlayerAnimationProfileId.UnarmedLocomotion);
            var idle = context.Load(ClipPath(LocomotionPack, "idle.fbx"), "Unarmed_Idle", loop: true, sharedAvatar, requiredSlot: "Idle");
            var walkForward = context.Load(ClipPath(LocomotionPack, "walking.fbx"), "Unarmed_Walk_Forward", loop: true, sharedAvatar, requiredSlot: "Walk.Forward");
            var runForward = context.Load(ClipPath(LocomotionPack, "standard run.fbx"), "Unarmed_Run_Forward", loop: true, sharedAvatar, requiredSlot: "Run.Forward");
            var walkLeft = context.Load(ClipPath(LocomotionPack, "left strafe walking.fbx"), "Unarmed_Walk_Left", loop: true, sharedAvatar, optionalSlot: "Walk.Left");
            var runLeft = context.Load(ClipPath(LocomotionPack, "left strafe.fbx"), "Unarmed_Run_Left", loop: true, sharedAvatar, optionalSlot: "Run.Left");
            var walkRight = context.Load(ClipPath(LocomotionPack, "right strafe walking.fbx"), "Unarmed_Walk_Right", loop: true, sharedAvatar, optionalSlot: "Walk.Right");
            var runRight = context.Load(ClipPath(LocomotionPack, "right strafe.fbx"), "Unarmed_Run_Right", loop: true, sharedAvatar, optionalSlot: "Run.Right");

            var directional = DirectionalSets(
                context,
                new Dictionary<PlayerAnimationDirection, (AnimationClip walk, AnimationClip run)>
                {
                    [PlayerAnimationDirection.Forward] = (walkForward, runForward),
                    [PlayerAnimationDirection.Left] = (walkLeft, runLeft),
                    [PlayerAnimationDirection.Right] = (walkRight, runRight)
                },
                walkForward,
                runForward);

            return SaveProfile(
                context,
                "UnarmedLocomotionProfile",
                allowsShieldInHand: false,
                allowsShieldGuard: false,
                requiresTwoHandedWeapon: false,
                usesRangedAim: false,
                usesFootIk: true,
                usesTorsoAim: false,
                idle,
                directional,
                strafing: Clips(walkLeft, walkRight, runLeft, runRight),
                turns: Clips(
                    context.Load(ClipPath(LocomotionPack, "left turn 90.fbx"), "Unarmed_Turn_Left_90", loop: false, sharedAvatar, optionalSlot: "Turn.Left"),
                    context.Load(ClipPath(LocomotionPack, "right turn 90.fbx"), "Unarmed_Turn_Right_90", loop: false, sharedAvatar, optionalSlot: "Turn.Right")),
                draw: null,
                sheathe: null,
                attack: null,
                fire: null,
                shieldGuard: null,
                weaponBlock: null,
                impact: null,
                death: null,
                jump: Clips(context.Load(ClipPath(LocomotionPack, "jump.fbx"), "Unarmed_Jump", loop: false, sharedAvatar, optionalSlot: "Jump")),
                crouch: null);
        }

        private static PlayerAnimationProfileDefinition GenerateSwordShield(Avatar sharedAvatar)
        {
            var context = new ProfileBuildContext(PlayerAnimationProfileId.SwordShieldCombat);
            var idle = context.Load(ClipPath(SwordShieldPack, "sword and shield idle.fbx"), "SwordShield_Idle", loop: true, sharedAvatar, requiredSlot: "Idle");
            var walkForward = context.Load(ClipPath(SwordShieldPack, "sword and shield walk.fbx"), "SwordShield_Walk_Forward", loop: true, sharedAvatar, requiredSlot: "Walk.Forward");
            var runForward = context.Load(ClipPath(SwordShieldPack, "sword and shield run.fbx"), "SwordShield_Run_Forward", loop: true, sharedAvatar, requiredSlot: "Run.Forward");
            var walkLeft = context.Load(ClipPath(SwordShieldPack, "sword and shield strafe.fbx"), "SwordShield_Walk_Left", loop: true, sharedAvatar, optionalSlot: "Walk.Left");
            var runLeft = context.Load(ClipPath(SwordShieldPack, "sword and shield strafe (2).fbx"), "SwordShield_Run_Left", loop: true, sharedAvatar, optionalSlot: "Run.Left");
            var walkRight = context.Load(ClipPath(SwordShieldPack, "sword and shield strafe (3).fbx"), "SwordShield_Walk_Right", loop: true, sharedAvatar, optionalSlot: "Walk.Right");
            var runRight = context.Load(ClipPath(SwordShieldPack, "sword and shield strafe (4).fbx"), "SwordShield_Run_Right", loop: true, sharedAvatar, optionalSlot: "Run.Right");
            var guardStrafeLeft = context.Load(ClipPath(SwordShieldPack, "sword and shield strafe (2).fbx"), "SwordShield_GuardStrafe_Left", loop: true, sharedAvatar, optionalSlot: "GuardStrafe.Left");
            var guardStrafeRight = context.Load(ClipPath(SwordShieldPack, "sword and shield strafe.fbx"), "SwordShield_GuardStrafe_Right", loop: true, sharedAvatar, optionalSlot: "GuardStrafe.Right");

            var directional = DirectionalSets(
                context,
                new Dictionary<PlayerAnimationDirection, (AnimationClip walk, AnimationClip run)>
                {
                    [PlayerAnimationDirection.Forward] = (walkForward, runForward),
                    [PlayerAnimationDirection.Left] = (walkLeft, runLeft),
                    [PlayerAnimationDirection.Right] = (walkRight, runRight)
                },
                walkForward,
                runForward);

            return SaveProfile(
                context,
                "SwordShieldCombatProfile",
                allowsShieldInHand: true,
                allowsShieldGuard: true,
                requiresTwoHandedWeapon: false,
                usesRangedAim: false,
                usesFootIk: true,
                usesTorsoAim: true,
                idle,
                directional,
                strafing: Clips(walkLeft, walkRight, runLeft, runRight, guardStrafeLeft, guardStrafeRight),
                turns: LoadMany(context, SwordShieldPack, "SwordShield_Turn", sharedAvatar, false, "sword and shield turn.fbx", "sword and shield turn (2).fbx", "sword and shield 180 turn.fbx", "sword and shield 180 turn (2).fbx"),
                draw: LoadMany(context, SwordShieldPack, "SwordShield_Draw", sharedAvatar, false, "draw sword 1.fbx", "draw sword 2.fbx"),
                sheathe: LoadMany(context, SwordShieldPack, "SwordShield_Sheathe", sharedAvatar, false, "sheath sword 1.fbx", "sheath sword 2.fbx"),
                attack: LoadMany(
                    context,
                    SwordShieldPack,
                    "SwordShield_Attack",
                    sharedAvatar,
                    false,
                    "sword and shield attack (2).fbx",
                    "sword and shield attack (3).fbx",
                    "sword and shield attack (4).fbx",
                    "sword and shield attack.fbx",
                    "sword and shield slash (2).fbx",
                    "sword and shield slash (3).fbx",
                    "sword and shield slash (4).fbx",
                    "sword and shield slash (5).fbx",
                    "sword and shield slash.fbx"),
                fire: null,
                shieldGuard: LoadMany(
                    context,
                    SwordShieldPack,
                    "SwordShield_ShieldGuard",
                    sharedAvatar,
                    true,
                    "sword and shield block (2).fbx",
                    "sword and shield block idle.fbx",
                    "sword and shield block.fbx",
                    "sword and shield crouch block (2).fbx",
                    "sword and shield crouch block idle.fbx",
                    "sword and shield crouch block.fbx"),
                weaponBlock: null,
                impact: LoadMany(
                    context,
                    SwordShieldPack,
                    "SwordShield_Impact",
                    sharedAvatar,
                    false,
                    "sword and shield impact (2).fbx",
                    "sword and shield impact (3).fbx",
                    "sword and shield impact.fbx"),
                death: LoadMatching(context, SwordShieldPack, "SwordShield_Death", sharedAvatar, false, "death"),
                jump: LoadMatching(context, SwordShieldPack, "SwordShield_Jump", sharedAvatar, false, "jump"),
                crouch: LoadMatching(context, SwordShieldPack, "SwordShield_Crouch", sharedAvatar, true, "crouch", "crouching"));
        }

        private static PlayerAnimationProfileDefinition GenerateGreatSword(Avatar sharedAvatar)
        {
            var context = new ProfileBuildContext(PlayerAnimationProfileId.GreatSwordCombat);
            var idle = context.Load(ClipPath(GreatSwordPack, "great sword idle.fbx"), "GreatSword_Idle", loop: true, sharedAvatar, requiredSlot: "Idle");
            var walkForward = context.Load(ClipPath(GreatSwordPack, "great sword walk.fbx"), "GreatSword_Walk_Forward", loop: true, sharedAvatar, requiredSlot: "Walk.Forward");
            var runForward = context.Load(ClipPath(GreatSwordPack, "great sword run.fbx"), "GreatSword_Run_Forward", loop: true, sharedAvatar, requiredSlot: "Run.Forward");
            var walkLeft = context.Load(ClipPath(GreatSwordPack, "great sword strafe.fbx"), "GreatSword_Walk_Left", loop: true, sharedAvatar, optionalSlot: "Walk.Left");
            var runLeft = context.Load(ClipPath(GreatSwordPack, "great sword strafe (2).fbx"), "GreatSword_Run_Left", loop: true, sharedAvatar, optionalSlot: "Run.Left");
            var walkRight = context.Load(ClipPath(GreatSwordPack, "great sword strafe (3).fbx"), "GreatSword_Walk_Right", loop: true, sharedAvatar, optionalSlot: "Walk.Right");
            var runRight = context.Load(ClipPath(GreatSwordPack, "great sword strafe (4).fbx"), "GreatSword_Run_Right", loop: true, sharedAvatar, optionalSlot: "Run.Right");

            var directional = DirectionalSets(
                context,
                new Dictionary<PlayerAnimationDirection, (AnimationClip walk, AnimationClip run)>
                {
                    [PlayerAnimationDirection.Forward] = (walkForward, runForward),
                    [PlayerAnimationDirection.Left] = (walkLeft, runLeft),
                    [PlayerAnimationDirection.Right] = (walkRight, runRight)
                },
                walkForward,
                runForward);

            return SaveProfile(
                context,
                "GreatSwordCombatProfile",
                allowsShieldInHand: false,
                allowsShieldGuard: false,
                requiresTwoHandedWeapon: true,
                usesRangedAim: false,
                usesFootIk: true,
                usesTorsoAim: true,
                idle,
                directional,
                strafing: Clips(walkLeft, walkRight, runLeft, runRight),
                turns: LoadMany(context, GreatSwordPack, "GreatSword_Turn", sharedAvatar, false, "great sword turn.fbx", "great sword turn (2).fbx", "great sword 180 turn.fbx", "great sword 180 turn (2).fbx"),
                draw: LoadMany(context, GreatSwordPack, "GreatSword_Draw", sharedAvatar, false, "draw a great sword 1.fbx", "draw a great sword 2.fbx"),
                sheathe: null,
                attack: LoadMatching(context, GreatSwordPack, "GreatSword_Attack", sharedAvatar, false, "attack", "slash", "spin"),
                fire: null,
                shieldGuard: null,
                weaponBlock: LoadMatching(context, GreatSwordPack, "GreatSword_WeaponBlock", sharedAvatar, true, "blocking"),
                impact: LoadMatching(context, GreatSwordPack, "GreatSword_Impact", sharedAvatar, false, "impact"),
                death: LoadMatching(context, GreatSwordPack, "GreatSword_Death", sharedAvatar, false, "death"),
                jump: LoadMatching(context, GreatSwordPack, "GreatSword_Jump", sharedAvatar, false, "jump"),
                crouch: LoadMatching(context, GreatSwordPack, "GreatSword_Crouch", sharedAvatar, true, "crouching"));
        }

        private static PlayerAnimationProfileDefinition GenerateRifle(Avatar sharedAvatar)
        {
            var context = new ProfileBuildContext(PlayerAnimationProfileId.RifleCombat);
            var idle = context.Load(ClipPath(RiflePack, "idle aiming.fbx"), "Rifle_Idle_Aiming", loop: true, sharedAvatar, requiredSlot: "Idle");
            var directional = DirectionalSets(
                context,
                new Dictionary<PlayerAnimationDirection, (AnimationClip walk, AnimationClip run)>
                {
                    [PlayerAnimationDirection.Forward] = DirectionalPair(context, sharedAvatar, RiflePack, "Rifle", "forward"),
                    [PlayerAnimationDirection.ForwardRight] = DirectionalPair(context, sharedAvatar, RiflePack, "Rifle", "forward right"),
                    [PlayerAnimationDirection.Right] = DirectionalPair(context, sharedAvatar, RiflePack, "Rifle", "right"),
                    [PlayerAnimationDirection.BackwardRight] = DirectionalPair(context, sharedAvatar, RiflePack, "Rifle", "backward right"),
                    [PlayerAnimationDirection.Backward] = DirectionalPair(context, sharedAvatar, RiflePack, "Rifle", "backward"),
                    [PlayerAnimationDirection.BackwardLeft] = DirectionalPair(context, sharedAvatar, RiflePack, "Rifle", "backward left"),
                    [PlayerAnimationDirection.Left] = DirectionalPair(context, sharedAvatar, RiflePack, "Rifle", "left"),
                    [PlayerAnimationDirection.ForwardLeft] = DirectionalPair(context, sharedAvatar, RiflePack, "Rifle", "forward left")
                },
                null,
                null);

            return SaveProfile(
                context,
                "RifleCombatProfile",
                allowsShieldInHand: false,
                allowsShieldGuard: false,
                requiresTwoHandedWeapon: false,
                usesRangedAim: true,
                usesFootIk: true,
                usesTorsoAim: true,
                idle,
                directional,
                strafing: null,
                turns: LoadMany(context, RiflePack, "Rifle_Turn", sharedAvatar, false, "turn 90 left.fbx", "turn 90 right.fbx"),
                draw: null,
                sheathe: null,
                attack: null,
                fire: Clips(context.Load(ClipPath(ShooterPack, "firing rifle.fbx"), "Rifle_Fire", loop: false, sharedAvatar, optionalSlot: "Fire")),
                shieldGuard: null,
                weaponBlock: null,
                impact: null,
                death: LoadMatching(context, RiflePack, "Rifle_Death", sharedAvatar, false, "death"),
                jump: LoadMatching(context, RiflePack, "Rifle_Jump", sharedAvatar, false, "jump"),
                crouch: LoadMatching(context, RiflePack, "Rifle_Crouch", sharedAvatar, true, "crouch", "crouching"));
        }

        private static PlayerAnimationProfileDefinition GeneratePistol(Avatar sharedAvatar)
        {
            var context = new ProfileBuildContext(PlayerAnimationProfileId.PistolCombat);
            var idle = context.Load(ClipPath(PistolPack, "pistol idle.fbx"), "Pistol_Idle", loop: true, sharedAvatar, requiredSlot: "Idle");
            var walkForward = context.Load(ClipPath(PistolPack, "pistol walk.fbx"), "Pistol_Walk_Forward", loop: true, sharedAvatar, requiredSlot: "Walk.Forward");
            var runForward = context.Load(ClipPath(PistolPack, "pistol run.fbx"), "Pistol_Run_Forward", loop: true, sharedAvatar, requiredSlot: "Run.Forward");
            var walkBackward = context.Load(ClipPath(PistolPack, "pistol walk backward.fbx"), "Pistol_Walk_Backward", loop: true, sharedAvatar, requiredSlot: "Walk.Backward");
            var runBackward = context.Load(ClipPath(PistolPack, "pistol run backward.fbx"), "Pistol_Run_Backward", loop: true, sharedAvatar, requiredSlot: "Run.Backward");
            var walkLeft = context.Load(ClipPath(PistolPack, "pistol strafe.fbx"), "Pistol_Walk_Left", loop: true, sharedAvatar, requiredSlot: "Walk.Left");
            var runLeft = context.Load(ClipPath(PistolPack, "pistol strafe.fbx"), "Pistol_Run_Left", loop: true, sharedAvatar, requiredSlot: "Run.Left");
            var walkRight = context.Load(ClipPath(PistolPack, "pistol strafe (2).fbx"), "Pistol_Walk_Right", loop: true, sharedAvatar, requiredSlot: "Walk.Right");
            var runRight = context.Load(ClipPath(PistolPack, "pistol strafe (2).fbx"), "Pistol_Run_Right", loop: true, sharedAvatar, requiredSlot: "Run.Right");
            var walkForwardRight = context.Load(ClipPath(PistolPack, "pistol walk arc.fbx"), "Pistol_Walk_ForwardRight", loop: true, sharedAvatar, optionalSlot: "Walk.ForwardRight");
            var runForwardRight = context.Load(ClipPath(PistolPack, "pistol run arc.fbx"), "Pistol_Run_ForwardRight", loop: true, sharedAvatar, optionalSlot: "Run.ForwardRight");
            var walkForwardLeft = context.Load(ClipPath(PistolPack, "pistol walk arc (2).fbx"), "Pistol_Walk_ForwardLeft", loop: true, sharedAvatar, optionalSlot: "Walk.ForwardLeft");
            var runForwardLeft = context.Load(ClipPath(PistolPack, "pistol run arc (2).fbx"), "Pistol_Run_ForwardLeft", loop: true, sharedAvatar, optionalSlot: "Run.ForwardLeft");
            var walkBackwardRight = context.Load(ClipPath(PistolPack, "pistol walk backward arc.fbx"), "Pistol_Walk_BackwardRight", loop: true, sharedAvatar, optionalSlot: "Walk.BackwardRight");
            var runBackwardRight = context.Load(ClipPath(PistolPack, "pistol run backward arc.fbx"), "Pistol_Run_BackwardRight", loop: true, sharedAvatar, optionalSlot: "Run.BackwardRight");
            var walkBackwardLeft = context.Load(ClipPath(PistolPack, "pistol walk backward arc (2).fbx"), "Pistol_Walk_BackwardLeft", loop: true, sharedAvatar, optionalSlot: "Walk.BackwardLeft");
            var runBackwardLeft = context.Load(ClipPath(PistolPack, "pistol run backward arc (2).fbx"), "Pistol_Run_BackwardLeft", loop: true, sharedAvatar, optionalSlot: "Run.BackwardLeft");

            var directional = DirectionalSets(
                context,
                new Dictionary<PlayerAnimationDirection, (AnimationClip walk, AnimationClip run)>
                {
                    [PlayerAnimationDirection.Forward] = (walkForward, runForward),
                    [PlayerAnimationDirection.ForwardRight] = (walkForwardRight, runForwardRight),
                    [PlayerAnimationDirection.Right] = (walkRight, runRight),
                    [PlayerAnimationDirection.BackwardRight] = (walkBackwardRight, runBackwardRight),
                    [PlayerAnimationDirection.Backward] = (walkBackward, runBackward),
                    [PlayerAnimationDirection.BackwardLeft] = (walkBackwardLeft, runBackwardLeft),
                    [PlayerAnimationDirection.Left] = (walkLeft, runLeft),
                    [PlayerAnimationDirection.ForwardLeft] = (walkForwardLeft, runForwardLeft)
                },
                walkForward,
                runForward);

            return SaveProfile(
                context,
                "PistolCombatProfile",
                allowsShieldInHand: false,
                allowsShieldGuard: false,
                requiresTwoHandedWeapon: false,
                usesRangedAim: true,
                usesFootIk: true,
                usesTorsoAim: true,
                idle,
                directional,
                strafing: Clips(walkLeft, walkRight, runLeft, runRight),
                turns: null,
                draw: null,
                sheathe: null,
                attack: null,
                fire: null,
                shieldGuard: null,
                weaponBlock: null,
                impact: null,
                death: null,
                jump: LoadMatching(context, PistolPack, "Pistol_Jump", sharedAvatar, false, "jump"),
                crouch: LoadMatching(context, PistolPack, "Pistol_Kneel", sharedAvatar, true, "kneel", "kneeling"));
        }

        private static (AnimationClip walk, AnimationClip run) DirectionalPair(
            ProfileBuildContext context,
            Avatar sharedAvatar,
            string pack,
            string prefix,
            string directionName)
        {
            var slotSuffix = Pascal(directionName);
            return (
                context.Load(ClipPath(pack, $"walk {directionName}.fbx"), $"{prefix}_Walk_{slotSuffix}", loop: true, sharedAvatar, requiredSlot: $"Walk.{slotSuffix}"),
                context.Load(ClipPath(pack, $"run {directionName}.fbx"), $"{prefix}_Run_{slotSuffix}", loop: true, sharedAvatar, requiredSlot: $"Run.{slotSuffix}"));
        }

        private static DirectionalAnimationClipSet[] DirectionalSets(
            ProfileBuildContext context,
            IReadOnlyDictionary<PlayerAnimationDirection, (AnimationClip walk, AnimationClip run)> realClips,
            AnimationClip walkFallback,
            AnimationClip runFallback)
        {
            var result = new List<DirectionalAnimationClipSet>();
            foreach (PlayerAnimationDirection direction in Enum.GetValues(typeof(PlayerAnimationDirection)))
            {
                realClips.TryGetValue(direction, out var clips);
                var walkPlaceholder = false;
                var runPlaceholder = false;
                var walk = clips.walk;
                var run = clips.run;

                if (walk == null)
                {
                    walk = context.Placeholder($"Walk.{direction}", walkFallback);
                    walkPlaceholder = walk != null;
                }

                if (run == null)
                {
                    run = context.Placeholder($"Run.{direction}", runFallback);
                    runPlaceholder = run != null;
                }

                result.Add(new DirectionalAnimationClipSet(direction, walk, run, walkPlaceholder, runPlaceholder));
            }

            return result.ToArray();
        }

        private static PlayerAnimationProfileDefinition SaveProfile(
            ProfileBuildContext context,
            string profileName,
            bool allowsShieldInHand,
            bool allowsShieldGuard,
            bool requiresTwoHandedWeapon,
            bool usesRangedAim,
            bool usesFootIk,
            bool usesTorsoAim,
            AnimationClip idle,
            IEnumerable<DirectionalAnimationClipSet> directional,
            IEnumerable<AnimationClip> strafing,
            IEnumerable<AnimationClip> turns,
            IEnumerable<AnimationClip> draw,
            IEnumerable<AnimationClip> sheathe,
            IEnumerable<AnimationClip> attack,
            IEnumerable<AnimationClip> fire,
            IEnumerable<AnimationClip> shieldGuard,
            IEnumerable<AnimationClip> weaponBlock,
            IEnumerable<AnimationClip> impact,
            IEnumerable<AnimationClip> death,
            IEnumerable<AnimationClip> jump,
            IEnumerable<AnimationClip> crouch)
        {
            var profile = LoadOrCreate<PlayerAnimationProfileDefinition>(ProfilePath(context.ProfileId));
            profile.Configure(
                context.ProfileId,
                profileName,
                allowsShieldInHand,
                allowsShieldGuard,
                requiresTwoHandedWeapon,
                usesRangedAim,
                usesFootIk,
                usesTorsoAim,
                idle,
                directional,
                strafing,
                turns,
                draw,
                sheathe,
                attack,
                fire,
                shieldGuard,
                weaponBlock,
                impact,
                death,
                jump,
                crouch,
                context.MissingRequired,
                context.MissingOptional,
                context.Placeholders,
                context.Reports);
            EditorUtility.SetDirty(profile);
            return profile;
        }

        private static AnimationClip[] LoadMatching(
            ProfileBuildContext context,
            string pack,
            string clipPrefix,
            Avatar sharedAvatar,
            bool loop,
            params string[] requiredTokens)
        {
            return Directory.Exists(pack)
                ? Directory.GetFiles(pack, "*.fbx")
                    .Where(path => requiredTokens.Any(token => Path.GetFileNameWithoutExtension(path).Contains(token, StringComparison.OrdinalIgnoreCase)))
                    .OrderBy(path => path, StringComparer.Ordinal)
                    .Select((path, index) => context.Load(path, $"{clipPrefix}_{index + 1:00}", loop, sharedAvatar, optionalSlot: clipPrefix))
                    .Where(clip => clip != null)
                    .ToArray()
                : Array.Empty<AnimationClip>();
        }

        private static AnimationClip[] LoadMany(
            ProfileBuildContext context,
            string pack,
            string clipPrefix,
            Avatar sharedAvatar,
            bool loop,
            params string[] files)
        {
            return files
                .Select((file, index) => context.Load(ClipPath(pack, file), $"{clipPrefix}_{index + 1:00}", loop, sharedAvatar, optionalSlot: clipPrefix))
                .Where(clip => clip != null)
                .ToArray();
        }

        private static AnimationClip[] Clips(params AnimationClip[] clips)
        {
            return clips.Where(clip => clip != null).Distinct().ToArray();
        }

        private static string ClipPath(string folder, string file)
        {
            return $"{folder}/{file}";
        }

        private static string Pascal(string value)
        {
            return string.Concat(value
                .Split(new[] { ' ', '-', '_' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(token => char.ToUpperInvariant(token[0]) + token[1..]));
        }

        private static Avatar LoadSharedAvatar()
        {
            var avatarSourcePath = ResolveSharedAvatarSourcePath();
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(avatarSourcePath);
            var animator = prefab != null ? prefab.GetComponent<Animator>() : null;
            if (animator != null && animator.avatar != null && animator.avatar.isValid && animator.avatar.isHuman)
            {
                return animator.avatar;
            }

            return AssetDatabase.LoadAllAssetsAtPath(avatarSourcePath)
                .OfType<Avatar>()
                .FirstOrDefault(avatar => avatar != null && avatar.isValid && avatar.isHuman);
        }

        public static Avatar EnsureSharedAvatar()
        {
            var avatarSourcePath = ResolveSharedAvatarSourcePath();
            EnsureHumanoidAvatarImport(avatarSourcePath);
            return LoadSharedAvatar();
        }

        public static string ResolveSharedAvatarSourcePath()
        {
            return ResolveSelectedSkinnedBodyFbxPath() ?? HollowMainRigPath;
        }

        public static string ResolveSelectedSkinnedBodyFbxPath()
        {
            foreach (var path in ResolveSkinnedBodyCandidatePaths())
            {
                if (TryValidateSkinnedBodyCandidate(path, out _))
                {
                    return path;
                }
            }

            return null;
        }

        public static IReadOnlyList<string> SkinnedBodyCandidateFbxPaths()
        {
            return ResolveSkinnedBodyCandidatePaths();
        }

        public static float ResolveSelectedSkinnedBodyLocalScale()
        {
            var selected = ResolveSelectedSkinnedBodyFbxPath();
            return string.IsNullOrWhiteSpace(selected) ? 1f : ResolveSkinnedBodyLocalScale(selected);
        }

        public static float ResolveSkinnedBodyLocalScale(string path)
        {
            return TryValidateSkinnedBodyCandidate(path, out _, out var localScale) ? localScale : 1f;
        }

        private static IEnumerable<string> BuildSkinnedBodyCandidateReportLines()
        {
            foreach (var path in ResolveSkinnedBodyCandidatePaths())
            {
                var valid = TryValidateSkinnedBodyCandidate(path, out var reason, out var localScale);
                yield return $"{(valid ? "VALID" : "INVALID")} {path}: {reason}; localScale={localScale:0.###}; {TextureSourceReport(path)}";
            }
        }

        public static string ResolveHollowMainModelAlbedoTexturePath()
        {
            return ResolveHollowMainModelTexturePath(path =>
                !path.Contains("_normal", StringComparison.OrdinalIgnoreCase) &&
                !path.Contains("_metallic", StringComparison.OrdinalIgnoreCase) &&
                !path.Contains("_roughness", StringComparison.OrdinalIgnoreCase));
        }

        public static string ResolveHollowMainModelNormalTexturePath()
        {
            return ResolveHollowMainModelTexturePath(path => path.Contains("_normal", StringComparison.OrdinalIgnoreCase));
        }

        public static string ResolveHollowMainModelMetallicTexturePath()
        {
            return ResolveHollowMainModelTexturePath(path => path.Contains("_metallic", StringComparison.OrdinalIgnoreCase));
        }

        public static string ResolveHollowMainModelRoughnessTexturePath()
        {
            return ResolveHollowMainModelTexturePath(path => path.Contains("_roughness", StringComparison.OrdinalIgnoreCase));
        }

        private static string ResolveHollowMainModelTexturePath(Func<string, bool> predicate)
        {
            if (!Directory.Exists(HollowMainModelDirectory))
            {
                return string.Empty;
            }

            return Directory.GetFiles(HollowMainModelDirectory, "Meshy_AI_Neon_Exoskeleton_*texture*.png", SearchOption.TopDirectoryOnly)
                .Select(NormalizeAssetPath)
                .Where(predicate)
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                .FirstOrDefault() ?? string.Empty;
        }

        private static string[] ResolveSkinnedBodyCandidatePaths()
        {
            if (!Directory.Exists(AnimationPackRoot))
            {
                return Array.Empty<string>();
            }

            return Directory.GetFiles(AnimationPackRoot, SkinnedBodyFbxSearchPattern, SearchOption.AllDirectories)
                .Select(NormalizeAssetPath)
                .Where(IsSkinnedBodyCandidatePath)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(BodyCandidatePriority)
                .ThenBy(path => path, StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        private static bool IsSkinnedBodyCandidatePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || path.EndsWith(".meta", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var fileName = Path.GetFileName(path);
            return fileName.StartsWith("Meshy_AI_Neon_Exoskeleton_", StringComparison.OrdinalIgnoreCase) &&
                fileName.Contains("texture", StringComparison.OrdinalIgnoreCase) &&
                fileName.EndsWith(".fbx", StringComparison.OrdinalIgnoreCase);
        }

        private static int BodyCandidatePriority(string path)
        {
            var folder = NormalizeAssetPath(Path.GetDirectoryName(path) ?? string.Empty);
            for (var index = 0; index < SkinnedBodyPriorityFolders.Length; index++)
            {
                if (string.Equals(folder, SkinnedBodyPriorityFolders[index], StringComparison.OrdinalIgnoreCase))
                {
                    return index;
                }
            }

            return SkinnedBodyPriorityFolders.Length + 1;
        }

        private static string NormalizeAssetPath(string path)
        {
            return (path ?? string.Empty).Replace('\\', '/');
        }

        private static void AppendAnimationPackValidationReport(StringBuilder builder)
        {
            builder.AppendLine("AnimationPackValidation:");
            if (!Directory.Exists(AnimationPackRoot))
            {
                builder.AppendLine("- Animation pack root missing.");
                return;
            }

            foreach (var directory in Directory.GetDirectories(AnimationPackRoot).OrderBy(path => path, StringComparer.OrdinalIgnoreCase))
            {
                var folder = NormalizeAssetPath(directory);
                var fbxFiles = Directory.GetFiles(folder, "*.fbx", SearchOption.TopDirectoryOnly)
                    .Select(NormalizeAssetPath)
                    .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                    .ToArray();
                var textures = Directory.GetFiles(folder, "*.*", SearchOption.TopDirectoryOnly)
                    .Select(NormalizeAssetPath)
                    .Where(path =>
                        path.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ||
                        path.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
                        path.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase) ||
                        path.EndsWith(".tga", StringComparison.OrdinalIgnoreCase))
                    .ToArray();
                var materials = Directory.GetFiles(folder, "*.mat", SearchOption.TopDirectoryOnly)
                    .Select(NormalizeAssetPath)
                    .ToArray();
                var bodyCandidates = fbxFiles.Where(IsSkinnedBodyCandidatePath).ToArray();

                builder.AppendLine($"- Pack: {folder}");
                builder.AppendLine($"  FBXCount: {fbxFiles.Length}");
                builder.AppendLine($"  TextureCount: {textures.Length}");
                builder.AppendLine($"  MaterialCount: {materials.Length}");
                builder.AppendLine($"  WithSkinBodyCandidates: {bodyCandidates.Length}");
                builder.AppendLine($"  AnimationClipCandidates: {fbxFiles.Length - bodyCandidates.Length}");
                foreach (var candidate in bodyCandidates)
                {
                    var valid = TryValidateSkinnedBodyCandidate(candidate, out var reason, out var localScale);
                    builder.AppendLine($"  - BodyCandidate: {(valid ? "VALID" : "INVALID")} {candidate}; {reason}; localScale={localScale:0.###}; {TextureSourceReport(candidate)}");
                }
            }
        }

        private static bool TryValidateSkinnedBodyCandidate(string path, out string reason)
        {
            return TryValidateSkinnedBodyCandidate(path, out reason, out _);
        }

        private static bool TryValidateSkinnedBodyCandidate(string path, out string reason, out float localScale)
        {
            localScale = 1f;
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                reason = "missing FBX file";
                return false;
            }

            EnsureHumanoidAvatarImport(path);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
            {
                reason = "could not load model prefab";
                return false;
            }

            var animator = prefab.GetComponent<Animator>();
            var avatar = animator != null && animator.avatar != null && animator.avatar.isValid && animator.avatar.isHuman
                ? animator.avatar
                : AssetDatabase.LoadAllAssetsAtPath(path)
                    .OfType<Avatar>()
                    .FirstOrDefault(candidate => candidate != null && candidate.isValid && candidate.isHuman);
            if (avatar == null)
            {
                reason = "missing valid Humanoid Avatar";
                return false;
            }

            var skinnedRenderers = prefab.GetComponentsInChildren<SkinnedMeshRenderer>(includeInactive: true);
            if (skinnedRenderers.Length == 0)
            {
                reason = "no SkinnedMeshRenderer";
                return false;
            }

            var lastRendererReason = "no usable SkinnedMeshRenderer";
            foreach (var renderer in skinnedRenderers)
            {
                if (IsValidSkinnedBodyRenderer(prefab.transform, renderer, out reason, out localScale, out var boundsSize))
                {
                    reason = $"valid Avatar={avatar.name}, renderer={renderer.name}, rootBone={renderer.rootBone.name}, bones={renderer.bones.Length}, targetHeight={TargetSkinnedBodyHeightMeters:0.###}, bounds={FormatVector(boundsSize)}";
                    return true;
                }

                lastRendererReason = reason;
            }

            reason = lastRendererReason;
            return false;
        }

        private static void EnsureHumanoidAvatarImport(string path)
        {
            var importer = AssetImporter.GetAtPath(path) as ModelImporter;
            if (importer != null)
            {
                var needsReimport =
                    importer.animationType != ModelImporterAnimationType.Human ||
                    importer.avatarSetup != ModelImporterAvatarSetup.CreateFromThisModel ||
                    importer.optimizeGameObjects;

                importer.importAnimation = true;
                importer.animationType = ModelImporterAnimationType.Human;
                importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                importer.sourceAvatar = null;
                importer.optimizeGameObjects = false;

                if (needsReimport)
                {
                    importer.SaveAndReimport();
                }
            }
        }

        private static bool IsValidSkinnedBodyRenderer(
            Transform root,
            SkinnedMeshRenderer renderer,
            out string reason,
            out float localScale,
            out Vector3 boundsSize)
        {
            localScale = 1f;
            boundsSize = Vector3.zero;
            if (root == null || renderer == null)
            {
                reason = "renderer/root missing";
                return false;
            }

            if (!renderer.enabled)
            {
                reason = $"{renderer.name}: renderer disabled";
                return false;
            }

            if (renderer.sharedMesh == null)
            {
                reason = $"{renderer.name}: sharedMesh missing";
                return false;
            }

            if (renderer.rootBone == null)
            {
                reason = $"{renderer.name}: rootBone missing";
                return false;
            }

            if (renderer.bones == null || renderer.bones.Length == 0)
            {
                reason = $"{renderer.name}: bones missing";
                return false;
            }

            if (!IsDescendantOf(renderer.rootBone, root))
            {
                reason = $"{renderer.name}: rootBone outside model hierarchy";
                return false;
            }

            if (renderer.bones.Any(bone => bone == null || !IsDescendantOf(bone, root)))
            {
                reason = $"{renderer.name}: one or more bones missing/outside model hierarchy";
                return false;
            }

            if (!HasUsableMaterial(renderer))
            {
                reason = $"{renderer.name}: material missing";
                return false;
            }

            if (!HasTextureSource(renderer))
            {
                reason = $"{renderer.name}: texture source missing";
                return false;
            }

            if (!TryResolveHumanScaleBounds(renderer, out localScale, out boundsSize))
            {
                reason = $"{renderer.name}: implausible height bounds {FormatVector(boundsSize)}";
                return false;
            }

            reason = "valid";
            return true;
        }

        private static bool HasUsableMaterial(Renderer renderer)
        {
            return renderer != null &&
                renderer.sharedMaterials != null &&
                renderer.sharedMaterials.Length > 0 &&
                renderer.sharedMaterials.All(material => material != null);
        }

        private static bool HasTextureSource(Renderer renderer)
        {
            if (renderer != null &&
                renderer.sharedMaterials != null &&
                renderer.sharedMaterials.Any(MaterialHasTexture))
            {
                return true;
            }

            return !string.IsNullOrWhiteSpace(ResolveHollowMainModelAlbedoTexturePath());
        }

        private static bool MaterialHasTexture(Material material)
        {
            if (material == null)
            {
                return false;
            }

            return (material.HasProperty("_BaseMap") && material.GetTexture("_BaseMap") != null) ||
                (material.HasProperty("_MainTex") && material.GetTexture("_MainTex") != null);
        }

        private static string TextureSourceReport(string candidatePath)
        {
            var albedo = ResolveHollowMainModelAlbedoTexturePath();
            var normal = ResolveHollowMainModelNormalTexturePath();
            var metallic = ResolveHollowMainModelMetallicTexturePath();
            var roughness = ResolveHollowMainModelRoughnessTexturePath();
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(candidatePath);
            var sourceMaterialHasTexture = prefab != null &&
                prefab.GetComponentsInChildren<Renderer>(includeInactive: true)
                    .SelectMany(renderer => renderer.sharedMaterials ?? Array.Empty<Material>())
                    .Any(MaterialHasTexture);
            return $"materialTextureAssigned={Bool(sourceMaterialHasTexture)}; " +
                $"canonicalAlbedo={(string.IsNullOrWhiteSpace(albedo) ? "<missing>" : albedo)}; " +
                $"canonicalNormal={(string.IsNullOrWhiteSpace(normal) ? "<missing>" : normal)}; " +
                $"canonicalMetallic={(string.IsNullOrWhiteSpace(metallic) ? "<missing>" : metallic)}; " +
                $"canonicalRoughness={(string.IsNullOrWhiteSpace(roughness) ? "<missing>" : roughness)}";
        }

        private static bool TryResolveHumanScaleBounds(SkinnedMeshRenderer renderer, out float localScale, out Vector3 boundsSize)
        {
            foreach (var candidateScale in new[] { 1f, SkinnedBodyCentimeterImportScale })
            {
                var rawBoundsSize = ScaledBoundsSize(renderer, candidateScale);
                if (rawBoundsSize.y > 0.75f &&
                    rawBoundsSize.y < 3f &&
                    rawBoundsSize.x > 0.12f &&
                    rawBoundsSize.z > 0.04f)
                {
                    localScale = candidateScale * (TargetSkinnedBodyHeightMeters / rawBoundsSize.y);
                    boundsSize = ScaledBoundsSize(renderer, localScale);
                    return true;
                }
            }

            localScale = 1f;
            boundsSize = ScaledBoundsSize(renderer, 1f);
            return false;
        }

        private static string FormatVector(Vector3 value)
        {
            return $"{value.x:0.###},{value.y:0.###},{value.z:0.###}";
        }

        private static Vector3 ScaledBoundsSize(SkinnedMeshRenderer renderer, float extraScale = 1f)
        {
            var size = renderer.sharedMesh.bounds.size;
            var scale = renderer.transform.lossyScale;
            return new Vector3(
                Mathf.Abs(size.x * scale.x * extraScale),
                Mathf.Abs(size.y * scale.y * extraScale),
                Mathf.Abs(size.z * scale.z * extraScale));
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

        private static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path) ?? string.Empty);
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null)
            {
                return asset;
            }

            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private sealed class ProfileBuildContext
        {
            public ProfileBuildContext(PlayerAnimationProfileId profileId)
            {
                ProfileId = profileId;
            }

            public PlayerAnimationProfileId ProfileId { get; }

            public List<string> MissingRequired { get; } = new();

            public List<string> MissingOptional { get; } = new();

            public List<string> Placeholders { get; } = new();

            public List<string> Reports { get; } = new();

            public AnimationClip Load(
                string path,
                string clipName,
                bool loop,
                Avatar sharedAvatar,
                string requiredSlot = null,
                string optionalSlot = null)
            {
                if (!File.Exists(path))
                {
                    if (!string.IsNullOrWhiteSpace(requiredSlot))
                    {
                        MissingRequired.Add($"{requiredSlot}: {path}");
                    }
                    else if (!string.IsNullOrWhiteSpace(optionalSlot))
                    {
                        MissingOptional.Add($"{optionalSlot}: {path}");
                    }

                    return null;
                }

                var clip = ConfigureHumanoidClip(path, clipName, loop, sharedAvatar);
                if (clip != null)
                {
                    var slot = requiredSlot ?? optionalSlot ?? clipName;
                    Reports.Add($"{slot}: {path}");
                }
                else if (!string.IsNullOrWhiteSpace(requiredSlot))
                {
                    MissingRequired.Add($"{requiredSlot}: {path}");
                }
                else if (!string.IsNullOrWhiteSpace(optionalSlot))
                {
                    MissingOptional.Add($"{optionalSlot}: {path}");
                }

                return clip;
            }

            public AnimationClip Placeholder(string slotName, AnimationClip sourceClip)
            {
                var path = $"{GeneratedPlaceholderDirectory}/{ProfileId}/TEMP_{ProfileId}_{slotName.Replace('.', '_')}.anim";
                var clip = CreateOrUpdatePlaceholderClip(path, Path.GetFileNameWithoutExtension(path), sourceClip);
                if (clip != null)
                {
                    Placeholders.Add(slotName);
                    Reports.Add($"{slotName}: TEMP placeholder {path}");
                }
                else
                {
                    MissingRequired.Add($"{slotName}: missing fallback source for temporary placeholder");
                }

                return clip;
            }
        }

        private static AnimationClip ConfigureHumanoidClip(string path, string clipName, bool loop, Avatar sharedAvatar)
        {
            var importer = AssetImporter.GetAtPath(path) as ModelImporter;
            if (importer == null)
            {
                return AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            }

            importer.importAnimation = true;
            importer.animationType = ModelImporterAnimationType.Human;
            if (sharedAvatar != null && path != ResolveSharedAvatarSourcePath())
            {
                importer.avatarSetup = ModelImporterAvatarSetup.CopyFromOther;
                importer.sourceAvatar = sharedAvatar;
            }
            else
            {
                importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            }

            importer.optimizeGameObjects = false;
            importer.animationWrapMode = loop ? WrapMode.Loop : WrapMode.Once;

            var defaultClips = importer.defaultClipAnimations;
            if (defaultClips == null || defaultClips.Length == 0)
            {
                defaultClips = importer.clipAnimations;
            }

            if (defaultClips == null || defaultClips.Length == 0)
            {
                importer.SaveAndReimport();
                defaultClips = importer.defaultClipAnimations;
            }

            if (defaultClips == null || defaultClips.Length == 0)
            {
                return null;
            }

            var clip = defaultClips[0];
            clip.name = clipName;
            clip.loopTime = loop;
            clip.loopPose = loop;
            clip.wrapMode = loop ? WrapMode.Loop : WrapMode.Once;
            clip.lockRootRotation = true;
            clip.keepOriginalOrientation = false;
            clip.lockRootHeightY = true;
            clip.keepOriginalPositionY = false;
            clip.lockRootPositionXZ = false;
            clip.keepOriginalPositionXZ = true;
            importer.clipAnimations = new[] { clip };
            importer.SaveAndReimport();

            return AssetDatabase
                .LoadAllAssetsAtPath(path)
                .OfType<AnimationClip>()
                .FirstOrDefault(asset => string.Equals(asset.name, clipName, StringComparison.Ordinal)) ??
                AssetDatabase
                    .LoadAllAssetsAtPath(path)
                    .OfType<AnimationClip>()
                    .FirstOrDefault(asset => !asset.name.StartsWith("__", StringComparison.Ordinal));
        }

        private static AnimationClip CreateOrUpdatePlaceholderClip(string path, string clipName, AnimationClip sourceClip)
        {
            if (sourceClip == null)
            {
                return null;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(path) ?? string.Empty);
            var generated = Object.Instantiate(sourceClip);
            generated.name = clipName;
            var settings = AnimationUtility.GetAnimationClipSettings(generated);
            settings.loopTime = true;
            settings.loopBlend = true;
            AnimationUtility.SetAnimationClipSettings(generated, settings);

            var existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (existing == null)
            {
                AssetDatabase.CreateAsset(generated, path);
                existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            }
            else
            {
                EditorUtility.CopySerialized(generated, existing);
                existing.name = clipName;
                EditorUtility.SetDirty(existing);
                Object.DestroyImmediate(generated);
            }

            AssetDatabase.ImportAsset(path);
            return existing != null ? existing : AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        }
    }
}
