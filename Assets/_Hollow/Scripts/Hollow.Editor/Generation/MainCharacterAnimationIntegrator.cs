using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Hollow.Combat;
using Hollow.Data.Definitions;
using Hollow.Entities;
using Hollow.Editor.Validation;
using Hollow.Presentation;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using Object = UnityEngine.Object;

namespace Hollow.Editor.Generation
{
    public static class MainCharacterAnimationIntegrator
    {
        private const string IdleFbxPath = "Assets/MeshyImports/MainCharacter_001/Idle_11_20260506_124917/Meshy_AI_Grey_Sentinel_biped_Animation_Idle_11_withSkin.fbx";
        private const string WalkFbxPath = "Assets/MeshyImports/MainCharacter_001/Walking_20260506_124626/Meshy_AI_Grey_Sentinel_biped_Animation_Walking_withSkin.fbx";
        private const string RunFbxPath = "Assets/MeshyImports/Running_20260506_131917/Meshy_AI_Grey_Sentinel_biped_Animation_Running_withSkin.fbx";
        private const string DirectionalLocomotionRoot = "Assets/MeshyImports/MainCharacter_001/Locomotion360";
        private const string RollFbxPath = "Assets/MeshyImports/Roll_Dodge_1_20260506_131201/Meshy_AI_Grey_Sentinel_biped_Animation_Roll_Dodge_1_withSkin.fbx";
        private const string SlashFbxPath = "Assets/MeshyImports/Left_Slash_20260506_131213/Meshy_AI_Grey_Sentinel_biped_Animation_Left_Slash_withSkin.fbx";
        private const string HitFbxPath = "Assets/MeshyImports/Hit_Reaction_1_20260506_131232/Meshy_AI_Grey_Sentinel_biped_Animation_Hit_Reaction_1_withSkin.fbx";
        private const string DeadFbxPath = "Assets/MeshyImports/Dead_20260506_131241/Meshy_AI_Grey_Sentinel_biped_Animation_Dead_withSkin.fbx";
        private const string IdleMaterialPath = "Assets/MeshyImports/MainCharacter_001/Idle_11_20260506_124917/Material_1.mat";
        private const string AlbedoTexturePath = "Assets/MeshyImports/MainCharacter_001/Idle_11_20260506_124917/Meshy_AI_Grey_Sentinel_biped_texture_0.png";
        private const string NormalTexturePath = "Assets/MeshyImports/MainCharacter_001/Idle_11_20260506_124917/Meshy_AI_Grey_Sentinel_biped_texture_0_normal.png";
        private const string MetallicTexturePath = "Assets/MeshyImports/MainCharacter_001/Idle_11_20260506_124917/Meshy_AI_Grey_Sentinel_biped_texture_0_metallic.png";
        private const string PlayerPrefabPath = "Assets/_Hollow/Prefabs/Player/PlayerCharacter.prefab";
        private const string PlayerControllerPath = "Assets/_Hollow/Art/Models/Characters/Player/MainCharacter_Player.controller";
        public const string RawMixamoDebugScenePath = "Assets/_Hollow/Scenes/DeveloperLab/RawMixamoAnimationDebug.unity";
        private const string RollInPlaceClipPath = "Assets/_Hollow/Art/Models/Characters/Player/MainCharacter_Roll_InPlace.anim";
        private const string CanonicalMaterialPath = "Assets/_Hollow/Art/Materials/ArtPass/AP_M_MainCharacter_GreySentinel.mat";
        private const string CleanRebuildBackupRoot = "/private/tmp/hollow-player-clean-rebuild";
        private const string IdleClipName = "MainCharacter_Idle";
        private const string WalkClipName = "MainCharacter_Walk";
        private const string RunClipName = "MainCharacter_Run";
        private const string RollClipName = "MainCharacter_Roll";
        private const string SlashClipName = "MainCharacter_LeftSlash";
        private const string HitClipName = "MainCharacter_HitReaction";
        private const string DeadClipName = "MainCharacter_Dead";
        private const float RunStartMoveSpeedThreshold = 0.5f;
        private const float RunTransitionThreshold = RunStartMoveSpeedThreshold - 0.001f;
        private const float RollRootDriftStripThresholdMeters = 0.25f;
        private const string VisualRootName = "MainCharacter_VisualRoot";
        private const string ModelInstanceName = "MainCharacter_MeshyModel";
        private const string LegacyCapsuleName = "PlayerHeight_1_78m";
        private const string HipsBoneName = "Hips";
        private const string RightUpperArmBoneName = "RightArm";
        private const string RightForearmBoneName = "RightForeArm";
        private const string RightHandBoneName = "RightHand";
        private const string LeftUpperArmBoneName = "LeftArm";
        private const string LeftForearmBoneName = "LeftForeArm";
        private const string LeftHandBoneName = "LeftHand";
        private const string LeftUpperLegBoneName = "LeftUpLeg";
        private const string LeftLowerLegBoneName = "LeftLeg";
        private const string LeftFootBoneName = "LeftFoot";
        private const string RightUpperLegBoneName = "RightUpLeg";
        private const string RightLowerLegBoneName = "RightLeg";
        private const string RightFootBoneName = "RightFoot";
        private const string BackShieldBoneName = "Spine02";
        public const PlayerAnimationSystemMode DefaultAnimationSystemMode = PlayerAnimationSystemMode.SimpleFullBodyAnimation;
        private static readonly DirectionalLocomotionImportSpec[] DirectionalLocomotionImportSpecs =
        {
            new("WalkForward", WalkFbxPath, "MainCharacter_Walk_Forward", new Vector2(0f, 1f), 0.45f, usesBaseForwardClip: true),
            new("WalkForwardRight", DirectionalPath("Walk_ForwardRight", "Meshy_AI_Grey_Sentinel_biped_Animation_Walk_ForwardRight_withSkin.fbx"), "MainCharacter_Walk_ForwardRight", new Vector2(0.70710677f, 0.70710677f), 0.45f),
            new("WalkRight", DirectionalPath("Walk_Right", "Meshy_AI_Grey_Sentinel_biped_Animation_Walk_Right_withSkin.fbx"), "MainCharacter_Walk_Right", new Vector2(1f, 0f), 0.45f),
            new("WalkBackRight", DirectionalPath("Walk_BackRight", "Meshy_AI_Grey_Sentinel_biped_Animation_Walk_BackRight_withSkin.fbx"), "MainCharacter_Walk_BackRight", new Vector2(0.70710677f, -0.70710677f), 0.45f),
            new("WalkBackward", DirectionalPath("Walk_Backward", "Meshy_AI_Grey_Sentinel_biped_Animation_Walk_Backward_withSkin.fbx"), "MainCharacter_Walk_Backward", new Vector2(0f, -1f), 0.45f),
            new("WalkBackLeft", DirectionalPath("Walk_BackLeft", "Meshy_AI_Grey_Sentinel_biped_Animation_Walk_BackLeft_withSkin.fbx"), "MainCharacter_Walk_BackLeft", new Vector2(-0.70710677f, -0.70710677f), 0.45f),
            new("WalkLeft", DirectionalPath("Walk_Left", "Meshy_AI_Grey_Sentinel_biped_Animation_Walk_Left_withSkin.fbx"), "MainCharacter_Walk_Left", new Vector2(-1f, 0f), 0.45f),
            new("WalkForwardLeft", DirectionalPath("Walk_ForwardLeft", "Meshy_AI_Grey_Sentinel_biped_Animation_Walk_ForwardLeft_withSkin.fbx"), "MainCharacter_Walk_ForwardLeft", new Vector2(-0.70710677f, 0.70710677f), 0.45f),
            new("RunForward", RunFbxPath, "MainCharacter_Run_Forward", new Vector2(0f, 1f), 1f, usesBaseForwardClip: true),
            new("RunForwardRight", DirectionalPath("Run_ForwardRight", "Meshy_AI_Grey_Sentinel_biped_Animation_Run_ForwardRight_withSkin.fbx"), "MainCharacter_Run_ForwardRight", new Vector2(0.70710677f, 0.70710677f), 1f),
            new("RunRight", DirectionalPath("Run_Right", "Meshy_AI_Grey_Sentinel_biped_Animation_Run_Right_withSkin.fbx"), "MainCharacter_Run_Right", new Vector2(1f, 0f), 1f),
            new("RunBackRight", DirectionalPath("Run_BackRight", "Meshy_AI_Grey_Sentinel_biped_Animation_Run_BackRight_withSkin.fbx"), "MainCharacter_Run_BackRight", new Vector2(0.70710677f, -0.70710677f), 1f),
            new("RunBackward", DirectionalPath("Run_Backward", "Meshy_AI_Grey_Sentinel_biped_Animation_Run_Backward_withSkin.fbx"), "MainCharacter_Run_Backward", new Vector2(0f, -1f), 1f),
            new("RunBackLeft", DirectionalPath("Run_BackLeft", "Meshy_AI_Grey_Sentinel_biped_Animation_Run_BackLeft_withSkin.fbx"), "MainCharacter_Run_BackLeft", new Vector2(-0.70710677f, -0.70710677f), 1f),
            new("RunLeft", DirectionalPath("Run_Left", "Meshy_AI_Grey_Sentinel_biped_Animation_Run_Left_withSkin.fbx"), "MainCharacter_Run_Left", new Vector2(-1f, 0f), 1f),
            new("RunForwardLeft", DirectionalPath("Run_ForwardLeft", "Meshy_AI_Grey_Sentinel_biped_Animation_Run_ForwardLeft_withSkin.fbx"), "MainCharacter_Run_ForwardLeft", new Vector2(-0.70710677f, 0.70710677f), 1f)
        };
        private static readonly HashSet<string> RollRootLikeBindingNames = new(StringComparer.OrdinalIgnoreCase)
        {
            "Armature",
            "Hips",
            "Pelvis",
            "Root",
            "RootNode"
        };

        [MenuItem("Hollow/Art/Integrate Meshy Main Character")]
        public static void Integrate()
        {
            Integrate(DefaultAnimationSystemMode);
        }

        [MenuItem("Hollow/Art/Integrate Meshy Main Character (Advanced Layered)")]
        public static void IntegrateAdvancedLayeredAnimation()
        {
            Integrate(PlayerAnimationSystemMode.AdvancedLayeredAnimation);
        }

        public static void Integrate(PlayerAnimationSystemMode animationSystemMode)
        {
            BackupGeneratedPlayerAssetsForCleanRebuild();
            var profileCatalog = PlayerAnimationProfileAssetGenerator.GenerateProfiles();
            var unarmedProfile = profileCatalog.Resolve(PlayerAnimationProfileId.UnarmedLocomotion);
            var swordShieldProfile = profileCatalog.Resolve(PlayerAnimationProfileId.SwordShieldCombat);
            var greatSwordProfile = profileCatalog.Resolve(PlayerAnimationProfileId.GreatSwordCombat);
            var rifleProfile = profileCatalog.Resolve(PlayerAnimationProfileId.RifleCombat);
            var locomotionProfile = SelectLockedLocomotionProfile(profileCatalog);
            var idleClip = unarmedProfile != null && unarmedProfile.IdleClip != null
                ? unarmedProfile.IdleClip
                : ConfigureAnimationImport(IdleFbxPath, IdleClipName, loop: true);
            var forwardLocomotion = ResolveDirectionalSet(
                unarmedProfile,
                PlayerAnimationDirection.Forward,
                required: false);
            var walkClip = forwardLocomotion.WalkClip != null
                ? forwardLocomotion.WalkClip
                : ConfigureAnimationImport(WalkFbxPath, WalkClipName, loop: true);
            var runClip = forwardLocomotion.RunClip != null
                ? forwardLocomotion.RunClip
                : ConfigureAnimationImport(RunFbxPath, RunClipName, loop: true);
            var directionalLocomotionClips = ConfigureDirectionalLocomotionFromProfile(locomotionProfile ?? rifleProfile ?? unarmedProfile);
            var importedRollClip = ConfigureAnimationImport(RollFbxPath, RollClipName, loop: false);
            var rollClip = CreateOrUpdateInPlaceRollClip(importedRollClip);
            var slashClip = swordShieldProfile != null && swordShieldProfile.FirstAttackClip() != null
                ? swordShieldProfile.FirstAttackClip()
                : ConfigureAnimationImport(SlashFbxPath, SlashClipName, loop: false);
            var hitClip = swordShieldProfile != null && swordShieldProfile.FirstImpactClip() != null
                ? swordShieldProfile.FirstImpactClip()
                : ConfigureAnimationImport(HitFbxPath, HitClipName, loop: false);
            var deadClip = swordShieldProfile != null && swordShieldProfile.FirstDeathClip() != null
                ? swordShieldProfile.FirstDeathClip()
                : ConfigureAnimationImport(DeadFbxPath, DeadClipName, loop: false);
            var guardBlockClip = swordShieldProfile != null && swordShieldProfile.ShieldGuardClips.Count > 0
                ? swordShieldProfile.ShieldGuardClips[0]
                : greatSwordProfile != null && greatSwordProfile.WeaponBlockClips.Count > 0
                    ? greatSwordProfile.WeaponBlockClips[0]
                    : slashClip;
            var material = CreateOrUpdateCanonicalMaterial();
            var controller = CreateAnimatorController(
                idleClip,
                walkClip,
                runClip,
                directionalLocomotionClips,
                rollClip,
                slashClip,
                guardBlockClip,
                hitClip,
                deadClip,
                animationSystemMode);
            UpdatePlayerPrefab(controller, material, rollClip, slashClip, hitClip, deadClip, profileCatalog, animationSystemMode);
            PlayerAnimationProfileAssetGenerator.GenerateDebugScene(profileCatalog);
            GenerateRawMixamoAnimationDebugScene(controller, material, animationSystemMode);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"Integrated Meshy main character in {animationSystemMode} mode into PlayerCharacter.prefab.");
        }

        [MenuItem("Hollow/Debug/Clean Rebuild Generated Player + Debug Scene")]
        public static void CleanRebuildGeneratedPlayerAndDebugScene()
        {
            Integrate();
        }

        public static string[] RequiredDirectionalLocomotionFbxPaths()
        {
            return DirectionalLocomotionImportSpecs
                .Select(spec => spec.Path)
                .Distinct(StringComparer.Ordinal)
                .ToArray();
        }

        public static string[] MissingDirectionalLocomotionFbxPaths()
        {
            return RequiredDirectionalLocomotionFbxPaths()
                .Where(path => !File.Exists(path))
                .ToArray();
        }

        private static void BackupGeneratedPlayerAssetsForCleanRebuild()
        {
            var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
            var backupDirectory = Path.Combine(CleanRebuildBackupRoot, timestamp);
            Directory.CreateDirectory(backupDirectory);

            foreach (var assetPath in new[]
            {
                PlayerPrefabPath,
                PlayerAnimationProfileAssetGenerator.DebugScenePath,
                RawMixamoDebugScenePath,
                PlayerControllerPath
            })
            {
                BackupFileIfPresent(assetPath, backupDirectory);
                BackupFileIfPresent($"{assetPath}.meta", backupDirectory);
            }

            Debug.Log($"Backed up generated player assets for clean rebuild to {backupDirectory}");
        }

        private static void BackupFileIfPresent(string projectRelativePath, string backupDirectory)
        {
            if (!File.Exists(projectRelativePath))
            {
                return;
            }

            var destination = Path.Combine(backupDirectory, projectRelativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(destination) ?? backupDirectory);
            File.Copy(projectRelativePath, destination, overwrite: true);
        }

        private static string DirectionalPath(string folderName, string fileName)
        {
            return $"{DirectionalLocomotionRoot}/{folderName}/{fileName}";
        }

        private static AnimationClip ConfigureAnimationImport(string path, string clipName, bool loop)
        {
            if (!File.Exists(path))
            {
                throw new FileNotFoundException($"Missing Meshy animation FBX: {path}", path);
            }

            var importer = AssetImporter.GetAtPath(path) as ModelImporter;
            if (importer == null)
            {
                throw new InvalidOperationException($"Asset is not a model import: {path}");
            }

            importer.importAnimation = true;
            importer.animationType = ModelImporterAnimationType.Generic;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.optimizeGameObjects = false;
            importer.animationWrapMode = loop ? WrapMode.Loop : WrapMode.Once;

            var defaultClips = importer.defaultClipAnimations;
            if (defaultClips == null || defaultClips.Length == 0)
            {
                throw new InvalidOperationException($"Meshy FBX has no animation takes to configure: {path}");
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
            clip.lockRootPositionXZ = true;
            clip.keepOriginalPositionXZ = false;

            importer.clipAnimations = new[] { clip };
            importer.SaveAndReimport();

            var importedClip = AssetDatabase
                .LoadAllAssetsAtPath(path)
                .OfType<AnimationClip>()
                .FirstOrDefault(asset => string.Equals(asset.name, clipName, StringComparison.Ordinal));
            if (importedClip == null)
            {
                throw new InvalidOperationException($"Failed to load configured animation clip {clipName} from {path}.");
            }

            return importedClip;
        }

        private static DirectionalLocomotionClip[] ConfigureDirectionalLocomotionImports(
            AnimationClip walkForwardClip,
            AnimationClip runForwardClip)
        {
            var missing = MissingDirectionalLocomotionFbxPaths();
            if (missing.Length > 0)
            {
                throw new FileNotFoundException(
                    "Missing required 360 directional locomotion FBX assets:\n" +
                    string.Join("\n", missing));
            }

            return DirectionalLocomotionImportSpecs
                .Select(spec =>
                {
                    var clip = spec.UsesBaseForwardClip
                        ? spec.SpeedMagnitude >= 1f ? runForwardClip : walkForwardClip
                        : ConfigureAnimationImport(spec.Path, spec.ClipName, loop: true);
                    return new DirectionalLocomotionClip(
                        spec.Label,
                        clip,
                        spec.Direction.normalized * spec.SpeedMagnitude);
                })
                .ToArray();
        }

        private static PlayerAnimationProfileDefinition SelectLockedLocomotionProfile(PlayerAnimationProfileCatalogDefinition profileCatalog)
        {
            var rifle = profileCatalog.Resolve(PlayerAnimationProfileId.RifleCombat);
            return HasCompleteDirectionalLocomotion(rifle)
                ? rifle
                : profileCatalog.Resolve(PlayerAnimationProfileId.UnarmedLocomotion);
        }

        private static bool HasCompleteDirectionalLocomotion(PlayerAnimationProfileDefinition profile)
        {
            return profile != null &&
                Enum.GetValues(typeof(PlayerAnimationDirection))
                    .Cast<PlayerAnimationDirection>()
                    .All(direction =>
                        profile.TryGetDirectionalClipSet(direction, out var clipSet) &&
                        clipSet.WalkClip != null &&
                        clipSet.RunClip != null);
        }

        private static DirectionalAnimationClipSet ResolveDirectionalSet(
            PlayerAnimationProfileDefinition profile,
            PlayerAnimationDirection direction,
            bool required)
        {
            if (profile != null && profile.TryGetDirectionalClipSet(direction, out var clipSet))
            {
                return clipSet;
            }

            if (required)
            {
                throw new InvalidOperationException($"{profile?.ProfileName ?? "Missing profile"} does not define directional locomotion for {direction}.");
            }

            return default;
        }

        private static DirectionalLocomotionClip[] ConfigureDirectionalLocomotionFromProfile(PlayerAnimationProfileDefinition profile)
        {
            if (profile == null)
            {
                throw new InvalidOperationException("Cannot build locked locomotion without a player animation profile.");
            }

            var clips = new List<DirectionalLocomotionClip>();
            foreach (PlayerAnimationDirection direction in Enum.GetValues(typeof(PlayerAnimationDirection)))
            {
                var clipSet = ResolveDirectionalSet(profile, direction, required: true);
                var unit = DirectionVector(direction);
                if (clipSet.WalkClip == null)
                {
                    throw new InvalidOperationException($"{profile.ProfileName} missing walk clip for {direction}.");
                }

                if (clipSet.RunClip == null)
                {
                    throw new InvalidOperationException($"{profile.ProfileName} missing run clip for {direction}.");
                }

                clips.Add(new DirectionalLocomotionClip($"{profile.ProfileName}.Walk.{direction}", clipSet.WalkClip, unit * 0.45f));
                clips.Add(new DirectionalLocomotionClip($"{profile.ProfileName}.Run.{direction}", clipSet.RunClip, unit));
            }

            return clips.ToArray();
        }

        private static Vector2 DirectionVector(PlayerAnimationDirection direction)
        {
            const float diagonal = 0.70710677f;
            return direction switch
            {
                PlayerAnimationDirection.Forward => new Vector2(0f, 1f),
                PlayerAnimationDirection.ForwardRight => new Vector2(diagonal, diagonal),
                PlayerAnimationDirection.Right => new Vector2(1f, 0f),
                PlayerAnimationDirection.BackwardRight => new Vector2(diagonal, -diagonal),
                PlayerAnimationDirection.Backward => new Vector2(0f, -1f),
                PlayerAnimationDirection.BackwardLeft => new Vector2(-diagonal, -diagonal),
                PlayerAnimationDirection.Left => new Vector2(-1f, 0f),
                PlayerAnimationDirection.ForwardLeft => new Vector2(-diagonal, diagonal),
                _ => Vector2.zero
            };
        }

        private static AnimationClip CreateOrUpdateInPlaceRollClip(AnimationClip sourceClip)
        {
            if (sourceClip == null)
            {
                throw new InvalidOperationException("Cannot generate an in-place roll clip from a null source clip.");
            }

            Directory.CreateDirectory(Path.GetDirectoryName(RollInPlaceClipPath) ?? string.Empty);
            var inPlaceClip = Object.Instantiate(sourceClip);
            inPlaceClip.name = RollClipName;
            var settings = AnimationUtility.GetAnimationClipSettings(sourceClip);
            settings.loopTime = false;
            settings.loopBlend = false;
            settings.loopBlendPositionXZ = false;
            AnimationUtility.SetAnimationClipSettings(inPlaceClip, settings);

            var strippedBindings = new List<string>();
            foreach (var binding in AnimationUtility.GetCurveBindings(inPlaceClip))
            {
                if (!IsRootLikeRollPositionBinding(binding))
                {
                    continue;
                }

                var curve = AnimationUtility.GetEditorCurve(inPlaceClip, binding);
                if (curve == null || curve.length == 0)
                {
                    continue;
                }

                if (!HasSignificantRollDrift(curve))
                {
                    continue;
                }

                var initialValue = curve.Evaluate(0f);
                var lockedCurve = AnimationCurve.Constant(0f, Mathf.Max(0.01f, sourceClip.length), initialValue);
                AnimationUtility.SetEditorCurve(inPlaceClip, binding, lockedCurve);
                strippedBindings.Add($"{binding.path}.{binding.propertyName}");
            }

            if (AssetDatabase.LoadAssetAtPath<AnimationClip>(RollInPlaceClipPath) != null)
            {
                AssetDatabase.DeleteAsset(RollInPlaceClipPath);
            }

            AssetDatabase.CreateAsset(inPlaceClip, RollInPlaceClipPath);
            AssetDatabase.ImportAsset(RollInPlaceClipPath);
            var asset = AssetDatabase.LoadAssetAtPath<AnimationClip>(RollInPlaceClipPath);
            if (asset == null)
            {
                throw new InvalidOperationException($"Failed to create in-place roll clip at {RollInPlaceClipPath}.");
            }

            asset.name = RollClipName;
            EditorUtility.SetDirty(asset);
            if (strippedBindings.Count > 0)
            {
                Debug.Log($"Generated in-place roll by locking {strippedBindings.Count} drifting root-like curves: {string.Join(", ", strippedBindings)}");
            }

            return asset;
        }

        private static bool IsRootLikeRollPositionBinding(EditorCurveBinding binding)
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
            var segment = segments[^1];
            var normalized = segment;
            var namespaceSeparator = normalized.LastIndexOf(':');
            if (namespaceSeparator >= 0 && namespaceSeparator < normalized.Length - 1)
            {
                normalized = normalized[(namespaceSeparator + 1)..];
            }

            return RollRootLikeBindingNames.Contains(normalized);
        }

        private static bool HasSignificantRollDrift(AnimationCurve curve)
        {
            if (curve == null || curve.length <= 1)
            {
                return false;
            }

            var first = curve.keys[0].value;
            var last = curve.keys[^1].value;
            var minimum = first;
            var maximum = first;
            foreach (var key in curve.keys)
            {
                minimum = Mathf.Min(minimum, key.value);
                maximum = Mathf.Max(maximum, key.value);
            }

            var range = maximum - minimum;
            var drift = Mathf.Abs(last - first);
            return Mathf.Max(range, drift) > RollRootDriftStripThresholdMeters;
        }

        private static Material CreateOrUpdateCanonicalMaterial()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(CanonicalMaterialPath) ?? string.Empty);
            var sourceMaterial = AssetDatabase.LoadAssetAtPath<Material>(IdleMaterialPath);
            if (sourceMaterial == null && Shader.Find("Universal Render Pipeline/Lit") == null && Shader.Find("Standard") == null)
            {
                throw new InvalidOperationException("Could not resolve a shader for the generated main character material.");
            }

            var material = AssetDatabase.LoadAssetAtPath<Material>(CanonicalMaterialPath);
            if (material == null)
            {
                material = sourceMaterial != null
                    ? new Material(sourceMaterial)
                    : new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
                AssetDatabase.CreateAsset(material, CanonicalMaterialPath);
            }
            else if (sourceMaterial != null)
            {
                EditorUtility.CopySerialized(sourceMaterial, material);
            }

            material.name = Path.GetFileNameWithoutExtension(CanonicalMaterialPath);
            var albedo = LoadTexture(PlayerAnimationProfileAssetGenerator.ResolveHollowMainModelAlbedoTexturePath()) ??
                AssetDatabase.LoadAssetAtPath<Texture2D>(PlayerAnimationProfileAssetGenerator.HollowMainModelTexturePath) ??
                AssetDatabase.LoadAssetAtPath<Texture2D>(AlbedoTexturePath);
            var normal = LoadTexture(PlayerAnimationProfileAssetGenerator.ResolveHollowMainModelNormalTexturePath()) ??
                AssetDatabase.LoadAssetAtPath<Texture2D>(NormalTexturePath);
            var metallic = LoadTexture(PlayerAnimationProfileAssetGenerator.ResolveHollowMainModelMetallicTexturePath()) ??
                AssetDatabase.LoadAssetAtPath<Texture2D>(MetallicTexturePath);
            AssignTexture(material, "_BaseMap", albedo);
            AssignTexture(material, "_MainTex", albedo);
            AssignTexture(material, "_BumpMap", normal);
            AssignTexture(material, "_MetallicGlossMap", metallic);
            if (normal != null)
            {
                material.EnableKeyword("_NORMALMAP");
                if (material.HasProperty("_BumpScale"))
                {
                    material.SetFloat("_BumpScale", 0.5f);
                }
            }

            if (metallic != null)
            {
                material.EnableKeyword("_METALLICSPECGLOSSMAP");
            }

            material.DisableKeyword("_EMISSION");
            ClearTexture(material, "_EmissionMap");
            if (material.HasProperty("_EmissionColor"))
            {
                material.SetColor("_EmissionColor", Color.black);
            }

            material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;

            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", Color.white);
            }

            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", Color.white);
            }

            EditorUtility.SetDirty(material);
            return material;
        }

        private static Texture2D LoadTexture(string path)
        {
            return string.IsNullOrWhiteSpace(path)
                ? null
                : AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        private static void AssignTexture(Material material, string propertyName, Texture texture)
        {
            if (material != null && texture != null && material.HasProperty(propertyName))
            {
                material.SetTexture(propertyName, texture);
            }
        }

        private static void ClearTexture(Material material, string propertyName)
        {
            if (material != null && material.HasProperty(propertyName))
            {
                material.SetTexture(propertyName, null);
            }
        }

        private static AnimatorController CreateAnimatorController(
            AnimationClip idleClip,
            AnimationClip walkClip,
            AnimationClip runClip,
            IReadOnlyList<DirectionalLocomotionClip> directionalLocomotionClips,
            AnimationClip rollClip,
            AnimationClip slashClip,
            AnimationClip guardBlockClip,
            AnimationClip hitClip,
            AnimationClip deadClip,
            PlayerAnimationSystemMode animationSystemMode)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(PlayerControllerPath) ?? string.Empty);
            if (AssetDatabase.LoadAssetAtPath<AnimatorController>(PlayerControllerPath) != null)
            {
                AssetDatabase.DeleteAsset(PlayerControllerPath);
            }

            var controller = AnimatorController.CreateAnimatorControllerAtPath(PlayerControllerPath);
            AddPlayerAnimatorParameters(controller);
            if (animationSystemMode == PlayerAnimationSystemMode.SimpleFullBodyAnimation)
            {
                BuildSimpleFullBodyController(controller, idleClip, walkClip, runClip, rollClip, slashClip, guardBlockClip, hitClip, deadClip);
            }
            else
            {
                BuildAdvancedLayeredController(controller, idleClip, walkClip, runClip, directionalLocomotionClips, rollClip, slashClip, hitClip, deadClip);
            }

            EditorUtility.SetDirty(controller);
            return controller;
        }

        private static void AddPlayerAnimatorParameters(AnimatorController controller)
        {
            controller.AddParameter(PlayerLocomotionAnimator.IsMovingParameter, AnimatorControllerParameterType.Bool);
            controller.AddParameter(PlayerLocomotionAnimator.MoveSpeedParameter, AnimatorControllerParameterType.Float);
            controller.AddParameter(PlayerLocomotionAnimator.ActionSpeedParameter, AnimatorControllerParameterType.Float);
            controller.AddParameter(PlayerLocomotionAnimator.RollTriggerParameter, AnimatorControllerParameterType.Trigger);
            controller.AddParameter(PlayerLocomotionAnimator.SlashTriggerParameter, AnimatorControllerParameterType.Trigger);
            controller.AddParameter(PlayerLocomotionAnimator.HitTriggerParameter, AnimatorControllerParameterType.Trigger);
            controller.AddParameter(PlayerLocomotionAnimator.DeathTriggerParameter, AnimatorControllerParameterType.Trigger);
            controller.AddParameter(PlayerLocomotionAnimator.IsDeadParameter, AnimatorControllerParameterType.Bool);
            controller.AddParameter(PlayerLocomotionAnimator.IsTargetLockedParameter, AnimatorControllerParameterType.Bool);
            controller.AddParameter(PlayerLocomotionAnimator.LockedMoveXParameter, AnimatorControllerParameterType.Float);
            controller.AddParameter(PlayerLocomotionAnimator.LockedMoveYParameter, AnimatorControllerParameterType.Float);
        }

        private static void BuildSimpleFullBodyController(
            AnimatorController controller,
            AnimationClip idleClip,
            AnimationClip walkClip,
            AnimationClip runClip,
            AnimationClip rollClip,
            AnimationClip attackClip,
            AnimationClip guardBlockClip,
            AnimationClip hitClip,
            AnimationClip deadClip)
        {
            var layers = controller.layers;
            if (layers.Length > 0)
            {
                layers[0].name = "Simple Full Body";
                layers[0].defaultWeight = 1f;
                layers[0].iKPass = false;
                controller.layers = layers;
            }

            var stateMachine = controller.layers[0].stateMachine;
            var idleState = AddState(stateMachine, "Idle", idleClip, new Vector3(220f, 120f, 0f));
            stateMachine.defaultState = idleState;
            var walkState = AddState(stateMachine, "Walk", walkClip, new Vector3(500f, 120f, 0f));
            var runState = AddState(stateMachine, "Run", runClip, new Vector3(780f, 120f, 0f));
            var rollState = AddActionState(stateMachine, "Roll", rollClip, new Vector3(220f, 320f, 0f));
            var attackState = AddActionState(stateMachine, "Attack", attackClip, new Vector3(500f, 320f, 0f));
            AddState(stateMachine, "GuardBlock", guardBlockClip, new Vector3(500f, 520f, 0f));
            var hitState = AddActionState(stateMachine, "HitReaction", hitClip, new Vector3(780f, 320f, 0f));
            var deadState = AddState(stateMachine, "Death", deadClip, new Vector3(1060f, 120f, 0f));

            AddSimpleLocomotionTransitions(idleState, walkState, runState);
            AddAnyStateTriggerTransition(stateMachine, deadState, PlayerLocomotionAnimator.DeathTriggerParameter, allowWhileDead: true, duration: 0.04f);
            AddAnyStateBoolTransition(stateMachine, deadState, PlayerLocomotionAnimator.IsDeadParameter, duration: 0.04f);
            AddAnyStateTriggerTransition(stateMachine, hitState, PlayerLocomotionAnimator.HitTriggerParameter, allowWhileDead: false, duration: 0.04f);
            AddAnyStateTriggerTransition(stateMachine, rollState, PlayerLocomotionAnimator.RollTriggerParameter, allowWhileDead: false, duration: 0.05f);
            AddAnyStateTriggerTransition(stateMachine, attackState, PlayerLocomotionAnimator.SlashTriggerParameter, allowWhileDead: false, duration: 0.04f);
            AddSimpleActionExitTransitions(rollState, idleState, walkState, runState);
            AddSimpleActionExitTransitions(attackState, idleState, walkState, runState);
            AddSimpleActionExitTransitions(hitState, idleState, walkState, runState);
        }

        private static void BuildAdvancedLayeredController(
            AnimatorController controller,
            AnimationClip idleClip,
            AnimationClip walkClip,
            AnimationClip runClip,
            IReadOnlyList<DirectionalLocomotionClip> directionalLocomotionClips,
            AnimationClip rollClip,
            AnimationClip slashClip,
            AnimationClip hitClip,
            AnimationClip deadClip)
        {
            var layers = controller.layers;
            if (layers.Length > 0)
            {
                layers[0].iKPass = true;
                controller.layers = layers;
            }

            var stateMachine = controller.layers[0].stateMachine;
            var idleState = AddState(stateMachine, "Idle", idleClip, new Vector3(220f, 120f, 0f));
            stateMachine.defaultState = idleState;
            var walkState = AddState(stateMachine, "Walk", walkClip, new Vector3(500f, 120f, 0f));
            var runState = AddState(stateMachine, "Run", runClip, new Vector3(780f, 120f, 0f));
            var lockedState = AddState(
                stateMachine,
                "LockedLocomotion",
                CreateLockedLocomotionBlendTree(controller, idleClip, directionalLocomotionClips),
                new Vector3(500f, -80f, 0f));
            var rollState = AddActionState(stateMachine, "Roll", rollClip, new Vector3(220f, 320f, 0f));
            var slashState = AddActionState(stateMachine, "LeftSlash", slashClip, new Vector3(500f, 320f, 0f));
            var hitState = AddActionState(stateMachine, "HitReaction", hitClip, new Vector3(780f, 320f, 0f));
            var deadState = AddState(stateMachine, "Dead", deadClip, new Vector3(1060f, 120f, 0f));

            AddLocomotionTransitions(idleState, walkState, runState);
            AddLockedLocomotionTransitions(idleState, walkState, runState, lockedState);
            AddAnyStateTriggerTransition(stateMachine, deadState, PlayerLocomotionAnimator.DeathTriggerParameter, allowWhileDead: true, duration: 0.04f);
            AddAnyStateBoolTransition(stateMachine, deadState, PlayerLocomotionAnimator.IsDeadParameter, duration: 0.04f);
            AddAnyStateTriggerTransition(stateMachine, hitState, PlayerLocomotionAnimator.HitTriggerParameter, allowWhileDead: false, duration: 0.04f);
            AddAnyStateTriggerTransition(stateMachine, rollState, PlayerLocomotionAnimator.RollTriggerParameter, allowWhileDead: false, duration: 0.05f);
            AddAnyStateTriggerTransition(stateMachine, slashState, PlayerLocomotionAnimator.SlashTriggerParameter, allowWhileDead: false, duration: 0.04f);
            AddActionExitTransitions(rollState, idleState, walkState, runState, lockedState);
            AddActionExitTransitions(slashState, idleState, walkState, runState, lockedState);
            AddActionExitTransitions(hitState, idleState, walkState, runState, lockedState);

            AddModernAnimationLayers(controller);
        }

        private static void AddModernAnimationLayers(AnimatorController controller)
        {
            var layers = controller.layers;
            if (layers.Length > 0)
            {
                layers[0].name = "Base Locomotion";
                layers[0].defaultWeight = 1f;
                layers[0].iKPass = true;
                controller.layers = layers;
            }

            AddEmptyLayer(controller, "Full-Body Actions", 0f);
            AddEmptyLayer(controller, "Upper-Body Combat", 0f);
            AddEmptyLayer(controller, "Additive Physical Response", 0f);
        }

        private static void AddEmptyLayer(AnimatorController controller, string layerName, float defaultWeight)
        {
            controller.AddLayer(layerName);
            var layers = controller.layers;
            var layer = layers[layers.Length - 1];
            layer.defaultWeight = defaultWeight;
            layer.iKPass = true;
            layers[layers.Length - 1] = layer;
            controller.layers = layers;
        }

        private static BlendTree CreateLockedLocomotionBlendTree(
            AnimatorController controller,
            Motion idleClip,
            IReadOnlyList<DirectionalLocomotionClip> directionalLocomotionClips)
        {
            if (directionalLocomotionClips == null || directionalLocomotionClips.Count != 16)
            {
                throw new InvalidOperationException("Locked locomotion requires exactly 16 directional walk/run clips.");
            }

            var tree = new BlendTree
            {
                name = "LockedLocomotionBlendTree",
                blendType = BlendTreeType.FreeformCartesian2D,
                blendParameter = PlayerLocomotionAnimator.LockedMoveXParameter,
                blendParameterY = PlayerLocomotionAnimator.LockedMoveYParameter,
                useAutomaticThresholds = false
            };
            AssetDatabase.AddObjectToAsset(tree, controller);

            tree.AddChild(idleClip, Vector2.zero);
            foreach (var clip in directionalLocomotionClips)
            {
                tree.AddChild(clip.Clip, clip.Threshold);
            }

            return tree;
        }

        private static AnimatorState AddState(AnimatorStateMachine stateMachine, string name, Motion motion, Vector3 position)
        {
            var state = stateMachine.AddState(name, position);
            state.motion = motion;
            state.writeDefaultValues = true;
            return state;
        }

        private static AnimatorState AddActionState(AnimatorStateMachine stateMachine, string name, Motion motion, Vector3 position)
        {
            var state = AddState(stateMachine, name, motion, position);
            state.speedParameter = PlayerLocomotionAnimator.ActionSpeedParameter;
            state.speedParameterActive = true;
            return state;
        }

        private static void AddLocomotionTransitions(AnimatorState idleState, AnimatorState walkState, AnimatorState runState)
        {
            var idleToWalk = idleState.AddTransition(walkState);
            idleToWalk.hasExitTime = false;
            idleToWalk.duration = 0.1f;
            idleToWalk.offset = 0f;
            idleToWalk.AddCondition(AnimatorConditionMode.IfNot, 0f, PlayerLocomotionAnimator.IsTargetLockedParameter);
            idleToWalk.AddCondition(AnimatorConditionMode.If, 0f, PlayerLocomotionAnimator.IsMovingParameter);
            idleToWalk.AddCondition(AnimatorConditionMode.Less, RunStartMoveSpeedThreshold, PlayerLocomotionAnimator.MoveSpeedParameter);

            var idleToRun = idleState.AddTransition(runState);
            idleToRun.hasExitTime = false;
            idleToRun.duration = 0.1f;
            idleToRun.offset = 0f;
            idleToRun.AddCondition(AnimatorConditionMode.IfNot, 0f, PlayerLocomotionAnimator.IsTargetLockedParameter);
            idleToRun.AddCondition(AnimatorConditionMode.If, 0f, PlayerLocomotionAnimator.IsMovingParameter);
            idleToRun.AddCondition(AnimatorConditionMode.Greater, RunTransitionThreshold, PlayerLocomotionAnimator.MoveSpeedParameter);

            var walkToIdle = walkState.AddTransition(idleState);
            walkToIdle.hasExitTime = false;
            walkToIdle.duration = 0.12f;
            walkToIdle.offset = 0f;
            walkToIdle.AddCondition(AnimatorConditionMode.IfNot, 0f, PlayerLocomotionAnimator.IsTargetLockedParameter);
            walkToIdle.AddCondition(AnimatorConditionMode.IfNot, 0f, PlayerLocomotionAnimator.IsMovingParameter);

            var walkToRun = walkState.AddTransition(runState);
            walkToRun.hasExitTime = false;
            walkToRun.duration = 0.1f;
            walkToRun.offset = 0f;
            walkToRun.AddCondition(AnimatorConditionMode.IfNot, 0f, PlayerLocomotionAnimator.IsTargetLockedParameter);
            walkToRun.AddCondition(AnimatorConditionMode.If, 0f, PlayerLocomotionAnimator.IsMovingParameter);
            walkToRun.AddCondition(AnimatorConditionMode.Greater, RunTransitionThreshold, PlayerLocomotionAnimator.MoveSpeedParameter);

            var runToIdle = runState.AddTransition(idleState);
            runToIdle.hasExitTime = false;
            runToIdle.duration = 0.12f;
            runToIdle.offset = 0f;
            runToIdle.AddCondition(AnimatorConditionMode.IfNot, 0f, PlayerLocomotionAnimator.IsTargetLockedParameter);
            runToIdle.AddCondition(AnimatorConditionMode.IfNot, 0f, PlayerLocomotionAnimator.IsMovingParameter);

            var runToWalk = runState.AddTransition(walkState);
            runToWalk.hasExitTime = false;
            runToWalk.duration = 0.12f;
            runToWalk.offset = 0f;
            runToWalk.AddCondition(AnimatorConditionMode.IfNot, 0f, PlayerLocomotionAnimator.IsTargetLockedParameter);
            runToWalk.AddCondition(AnimatorConditionMode.If, 0f, PlayerLocomotionAnimator.IsMovingParameter);
            runToWalk.AddCondition(AnimatorConditionMode.Less, RunStartMoveSpeedThreshold, PlayerLocomotionAnimator.MoveSpeedParameter);
        }

        private static void AddSimpleLocomotionTransitions(AnimatorState idleState, AnimatorState walkState, AnimatorState runState)
        {
            var idleToWalk = idleState.AddTransition(walkState);
            idleToWalk.hasExitTime = false;
            idleToWalk.duration = 0.1f;
            idleToWalk.offset = 0f;
            idleToWalk.AddCondition(AnimatorConditionMode.If, 0f, PlayerLocomotionAnimator.IsMovingParameter);
            idleToWalk.AddCondition(AnimatorConditionMode.Less, RunStartMoveSpeedThreshold, PlayerLocomotionAnimator.MoveSpeedParameter);

            var idleToRun = idleState.AddTransition(runState);
            idleToRun.hasExitTime = false;
            idleToRun.duration = 0.1f;
            idleToRun.offset = 0f;
            idleToRun.AddCondition(AnimatorConditionMode.If, 0f, PlayerLocomotionAnimator.IsMovingParameter);
            idleToRun.AddCondition(AnimatorConditionMode.Greater, RunTransitionThreshold, PlayerLocomotionAnimator.MoveSpeedParameter);

            var walkToIdle = walkState.AddTransition(idleState);
            walkToIdle.hasExitTime = false;
            walkToIdle.duration = 0.12f;
            walkToIdle.offset = 0f;
            walkToIdle.AddCondition(AnimatorConditionMode.IfNot, 0f, PlayerLocomotionAnimator.IsMovingParameter);

            var walkToRun = walkState.AddTransition(runState);
            walkToRun.hasExitTime = false;
            walkToRun.duration = 0.1f;
            walkToRun.offset = 0f;
            walkToRun.AddCondition(AnimatorConditionMode.If, 0f, PlayerLocomotionAnimator.IsMovingParameter);
            walkToRun.AddCondition(AnimatorConditionMode.Greater, RunTransitionThreshold, PlayerLocomotionAnimator.MoveSpeedParameter);

            var runToIdle = runState.AddTransition(idleState);
            runToIdle.hasExitTime = false;
            runToIdle.duration = 0.12f;
            runToIdle.offset = 0f;
            runToIdle.AddCondition(AnimatorConditionMode.IfNot, 0f, PlayerLocomotionAnimator.IsMovingParameter);

            var runToWalk = runState.AddTransition(walkState);
            runToWalk.hasExitTime = false;
            runToWalk.duration = 0.12f;
            runToWalk.offset = 0f;
            runToWalk.AddCondition(AnimatorConditionMode.If, 0f, PlayerLocomotionAnimator.IsMovingParameter);
            runToWalk.AddCondition(AnimatorConditionMode.Less, RunStartMoveSpeedThreshold, PlayerLocomotionAnimator.MoveSpeedParameter);
        }

        private static void AddLockedLocomotionTransitions(
            AnimatorState idleState,
            AnimatorState walkState,
            AnimatorState runState,
            AnimatorState lockedState)
        {
            AddToLockedTransition(idleState, lockedState, 0.08f);
            AddToLockedTransition(walkState, lockedState, 0.08f);
            AddToLockedTransition(runState, lockedState, 0.08f);

            var lockedToIdle = lockedState.AddTransition(idleState);
            lockedToIdle.hasExitTime = false;
            lockedToIdle.duration = 0.1f;
            lockedToIdle.offset = 0f;
            lockedToIdle.AddCondition(AnimatorConditionMode.IfNot, 0f, PlayerLocomotionAnimator.IsTargetLockedParameter);
            lockedToIdle.AddCondition(AnimatorConditionMode.IfNot, 0f, PlayerLocomotionAnimator.IsMovingParameter);

            var lockedToWalk = lockedState.AddTransition(walkState);
            lockedToWalk.hasExitTime = false;
            lockedToWalk.duration = 0.1f;
            lockedToWalk.offset = 0f;
            lockedToWalk.AddCondition(AnimatorConditionMode.IfNot, 0f, PlayerLocomotionAnimator.IsTargetLockedParameter);
            lockedToWalk.AddCondition(AnimatorConditionMode.If, 0f, PlayerLocomotionAnimator.IsMovingParameter);
            lockedToWalk.AddCondition(AnimatorConditionMode.Less, RunStartMoveSpeedThreshold, PlayerLocomotionAnimator.MoveSpeedParameter);

            var lockedToRun = lockedState.AddTransition(runState);
            lockedToRun.hasExitTime = false;
            lockedToRun.duration = 0.1f;
            lockedToRun.offset = 0f;
            lockedToRun.AddCondition(AnimatorConditionMode.IfNot, 0f, PlayerLocomotionAnimator.IsTargetLockedParameter);
            lockedToRun.AddCondition(AnimatorConditionMode.If, 0f, PlayerLocomotionAnimator.IsMovingParameter);
            lockedToRun.AddCondition(AnimatorConditionMode.Greater, RunTransitionThreshold, PlayerLocomotionAnimator.MoveSpeedParameter);
        }

        private static void AddToLockedTransition(AnimatorState fromState, AnimatorState lockedState, float duration)
        {
            var transition = fromState.AddTransition(lockedState);
            transition.hasExitTime = false;
            transition.duration = duration;
            transition.offset = 0f;
            transition.AddCondition(AnimatorConditionMode.If, 0f, PlayerLocomotionAnimator.IsTargetLockedParameter);
            transition.AddCondition(AnimatorConditionMode.IfNot, 0f, PlayerLocomotionAnimator.IsDeadParameter);
        }

        private static void AddAnyStateTriggerTransition(
            AnimatorStateMachine stateMachine,
            AnimatorState targetState,
            string triggerParameter,
            bool allowWhileDead,
            float duration)
        {
            var transition = stateMachine.AddAnyStateTransition(targetState);
            transition.hasExitTime = false;
            transition.duration = duration;
            transition.offset = 0f;
            transition.canTransitionToSelf = false;
            transition.AddCondition(AnimatorConditionMode.If, 0f, triggerParameter);
            if (!allowWhileDead)
            {
                transition.AddCondition(AnimatorConditionMode.IfNot, 0f, PlayerLocomotionAnimator.IsDeadParameter);
            }
        }

        private static void AddAnyStateBoolTransition(
            AnimatorStateMachine stateMachine,
            AnimatorState targetState,
            string boolParameter,
            float duration)
        {
            var transition = stateMachine.AddAnyStateTransition(targetState);
            transition.hasExitTime = false;
            transition.duration = duration;
            transition.offset = 0f;
            transition.canTransitionToSelf = false;
            transition.AddCondition(AnimatorConditionMode.If, 0f, boolParameter);
        }

        private static void AddActionExitTransitions(
            AnimatorState actionState,
            AnimatorState idleState,
            AnimatorState walkState,
            AnimatorState runState,
            AnimatorState lockedState)
        {
            var toLocked = actionState.AddTransition(lockedState);
            toLocked.hasExitTime = true;
            toLocked.exitTime = 0.92f;
            toLocked.duration = 0.08f;
            toLocked.offset = 0f;
            toLocked.AddCondition(AnimatorConditionMode.If, 0f, PlayerLocomotionAnimator.IsTargetLockedParameter);
            toLocked.AddCondition(AnimatorConditionMode.IfNot, 0f, PlayerLocomotionAnimator.IsDeadParameter);

            var toRun = actionState.AddTransition(runState);
            toRun.hasExitTime = true;
            toRun.exitTime = 0.92f;
            toRun.duration = 0.08f;
            toRun.offset = 0f;
            toRun.AddCondition(AnimatorConditionMode.IfNot, 0f, PlayerLocomotionAnimator.IsTargetLockedParameter);
            toRun.AddCondition(AnimatorConditionMode.If, 0f, PlayerLocomotionAnimator.IsMovingParameter);
            toRun.AddCondition(AnimatorConditionMode.Greater, RunTransitionThreshold, PlayerLocomotionAnimator.MoveSpeedParameter);
            toRun.AddCondition(AnimatorConditionMode.IfNot, 0f, PlayerLocomotionAnimator.IsDeadParameter);

            var toWalk = actionState.AddTransition(walkState);
            toWalk.hasExitTime = true;
            toWalk.exitTime = 0.92f;
            toWalk.duration = 0.08f;
            toWalk.offset = 0f;
            toWalk.AddCondition(AnimatorConditionMode.IfNot, 0f, PlayerLocomotionAnimator.IsTargetLockedParameter);
            toWalk.AddCondition(AnimatorConditionMode.If, 0f, PlayerLocomotionAnimator.IsMovingParameter);
            toWalk.AddCondition(AnimatorConditionMode.Less, RunStartMoveSpeedThreshold, PlayerLocomotionAnimator.MoveSpeedParameter);
            toWalk.AddCondition(AnimatorConditionMode.IfNot, 0f, PlayerLocomotionAnimator.IsDeadParameter);

            var toIdle = actionState.AddTransition(idleState);
            toIdle.hasExitTime = true;
            toIdle.exitTime = 0.92f;
            toIdle.duration = 0.08f;
            toIdle.offset = 0f;
            toIdle.AddCondition(AnimatorConditionMode.IfNot, 0f, PlayerLocomotionAnimator.IsTargetLockedParameter);
            toIdle.AddCondition(AnimatorConditionMode.IfNot, 0f, PlayerLocomotionAnimator.IsMovingParameter);
            toIdle.AddCondition(AnimatorConditionMode.IfNot, 0f, PlayerLocomotionAnimator.IsDeadParameter);
        }

        private static void AddSimpleActionExitTransitions(
            AnimatorState actionState,
            AnimatorState idleState,
            AnimatorState walkState,
            AnimatorState runState)
        {
            var toRun = actionState.AddTransition(runState);
            toRun.hasExitTime = true;
            toRun.exitTime = 0.92f;
            toRun.duration = 0.08f;
            toRun.offset = 0f;
            toRun.AddCondition(AnimatorConditionMode.If, 0f, PlayerLocomotionAnimator.IsMovingParameter);
            toRun.AddCondition(AnimatorConditionMode.Greater, RunTransitionThreshold, PlayerLocomotionAnimator.MoveSpeedParameter);
            toRun.AddCondition(AnimatorConditionMode.IfNot, 0f, PlayerLocomotionAnimator.IsDeadParameter);

            var toWalk = actionState.AddTransition(walkState);
            toWalk.hasExitTime = true;
            toWalk.exitTime = 0.92f;
            toWalk.duration = 0.08f;
            toWalk.offset = 0f;
            toWalk.AddCondition(AnimatorConditionMode.If, 0f, PlayerLocomotionAnimator.IsMovingParameter);
            toWalk.AddCondition(AnimatorConditionMode.Less, RunStartMoveSpeedThreshold, PlayerLocomotionAnimator.MoveSpeedParameter);
            toWalk.AddCondition(AnimatorConditionMode.IfNot, 0f, PlayerLocomotionAnimator.IsDeadParameter);

            var toIdle = actionState.AddTransition(idleState);
            toIdle.hasExitTime = true;
            toIdle.exitTime = 0.92f;
            toIdle.duration = 0.08f;
            toIdle.offset = 0f;
            toIdle.AddCondition(AnimatorConditionMode.IfNot, 0f, PlayerLocomotionAnimator.IsMovingParameter);
            toIdle.AddCondition(AnimatorConditionMode.IfNot, 0f, PlayerLocomotionAnimator.IsDeadParameter);
        }

        private static void UpdatePlayerPrefab(
            RuntimeAnimatorController controller,
            Material canonicalMaterial,
            AnimationClip rollClip,
            AnimationClip slashClip,
            AnimationClip hitClip,
            AnimationClip deadClip,
            PlayerAnimationProfileCatalogDefinition profileCatalog,
            PlayerAnimationSystemMode animationSystemMode)
        {
            var selectedBodyPath = PlayerAnimationProfileAssetGenerator.ResolveSelectedSkinnedBodyFbxPath();
            var modelSourcePath = selectedBodyPath ?? PlayerAnimationProfileAssetGenerator.HollowMainRigPath;
            var modelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(modelSourcePath) ??
                AssetDatabase.LoadAssetAtPath<GameObject>(IdleFbxPath);
            if (modelPrefab == null)
            {
                throw new InvalidOperationException($"Could not load player model prefab from {modelSourcePath} or {IdleFbxPath}.");
            }

            if (controller == null)
            {
                throw new InvalidOperationException("Cannot rebuild PlayerCharacter.prefab without a generated AnimatorController.");
            }

            var prefabRoot = CreateFreshPlayerPrefabRoot();
            try
            {
                var visualRoot = new GameObject(VisualRootName);
                visualRoot.transform.SetParent(prefabRoot.transform, false);
                visualRoot.transform.localPosition = Vector3.zero;
                visualRoot.transform.localRotation = Quaternion.identity;
                visualRoot.transform.localScale = Vector3.one;

                var modelInstance = PrefabUtility.InstantiatePrefab(modelPrefab, visualRoot.transform) as GameObject;
                if (modelInstance == null)
                {
                    modelInstance = Object.Instantiate(modelPrefab, visualRoot.transform);
                }

                modelInstance.name = ModelInstanceName;
                modelInstance.transform.localPosition = Vector3.zero;
                modelInstance.transform.localRotation = Quaternion.identity;
                var selectedBodyLocalScale = !string.IsNullOrWhiteSpace(selectedBodyPath)
                    ? PlayerAnimationProfileAssetGenerator.ResolveSelectedSkinnedBodyLocalScale()
                    : 1f;
                modelInstance.transform.localScale = Vector3.one * selectedBodyLocalScale;

                StripGameplayComponentsFromVisual(modelInstance);
                EnsureRendererMaterials(modelInstance, canonicalMaterial);
                EnsureVisibleBody(visualRoot.transform, modelInstance, canonicalMaterial);
                var grounding = prefabRoot.GetComponent<SimpleFullBodyGroundingController>() ??
                    prefabRoot.AddComponent<SimpleFullBodyGroundingController>();
                grounding.Configure(
                    modelInstance.transform,
                    visualRoot.transform,
                    prefabRoot.transform,
                    animationSystemMode == PlayerAnimationSystemMode.SimpleFullBodyAnimation,
                    SimpleFullBodyGroundingController.DefaultGroundClearanceMeters,
                    SimpleFullBodyGroundingController.DefaultMaxCorrectionMeters);
                grounding.ApplyGrounding();
                EditorUtility.SetDirty(grounding);

                var animator = modelInstance.GetComponent<Animator>();
                var sourceAnimator = modelPrefab.GetComponent<Animator>();
                if (animator == null)
                {
                    animator = modelInstance.AddComponent<Animator>();
                }

                animator.avatar = ResolveMainCharacterAvatar(sourceAnimator);
                if (animator.avatar == null || !animator.avatar.isValid || !animator.avatar.isHuman)
                {
                    throw new InvalidOperationException($"Could not resolve a valid Humanoid Avatar from {modelSourcePath}.");
                }

                animator.runtimeAnimatorController = controller;
                animator.applyRootMotion = false;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                var modernRig = EnsureModernAnimationRig(animator, visualRoot.transform, modelInstance.transform);
                var meleeHandSocket = EnsureMeleeHandSocket(modelInstance.transform);
                var rangedHandSocket = EnsureSocket(
                    FindDescendant(modelInstance.transform, RightHandBoneName),
                    PlayerHeldWeaponVisualController.RangedHandSocketName,
                    PlayerHeldWeaponVisualController.DefaultRangedHandSocketLocalPosition,
                    PlayerHeldWeaponVisualController.DefaultRangedHandSocketLocalEuler,
                    PlayerHeldWeaponVisualController.DefaultRangedHandSocketLocalScale);
                var meleeHolsterSocket = EnsureSocket(
                    visualRoot.transform,
                    PlayerHeldWeaponVisualController.MeleeHolsterSocketName,
                    PlayerHeldWeaponVisualController.DefaultMeleeHolsterSocketLocalPosition,
                    PlayerHeldWeaponVisualController.DefaultMeleeHolsterSocketLocalEuler,
                    PlayerHeldWeaponVisualController.DefaultMeleeHolsterSocketLocalScale);
                var rangedHolsterSocket = EnsureSocket(
                    visualRoot.transform,
                    PlayerHeldWeaponVisualController.RangedHolsterSocketName,
                    PlayerHeldWeaponVisualController.DefaultRangedHolsterSocketLocalPosition,
                    PlayerHeldWeaponVisualController.DefaultRangedHolsterSocketLocalEuler,
                    PlayerHeldWeaponVisualController.DefaultRangedHolsterSocketLocalScale);
                var rangedMuzzleSocket = EnsureSocket(
                    rangedHandSocket,
                    PlayerHeldWeaponVisualController.RangedMuzzleSocketName,
                    PlayerWeaponVisualPosePolicy.MuzzleLocalPosition(),
                    Vector3.zero,
                    Vector3.one);
                var shieldForearmSocket = EnsureSocket(
                    FindDescendant(modelInstance.transform, LeftForearmBoneName) ?? visualRoot.transform,
                    PlayerHeldWeaponVisualController.ShieldForearmSocketName,
                    PlayerHeldWeaponVisualController.DefaultShieldForearmSocketLocalPosition,
                    PlayerHeldWeaponVisualController.DefaultShieldForearmSocketLocalEuler,
                    PlayerHeldWeaponVisualController.DefaultShieldForearmSocketLocalScale);
                var shieldBackSocket = EnsureSocket(
                    FindDescendant(modelInstance.transform, BackShieldBoneName) ?? visualRoot.transform,
                    PlayerHeldWeaponVisualController.ShieldBackSocketName,
                    PlayerHeldWeaponVisualController.DefaultShieldBackSocketLocalPosition,
                    PlayerHeldWeaponVisualController.DefaultShieldBackSocketLocalEuler,
                    PlayerHeldWeaponVisualController.DefaultShieldBackSocketLocalScale);

                var locomotionAnimator = prefabRoot.GetComponent<PlayerLocomotionAnimator>() ??
                    prefabRoot.AddComponent<PlayerLocomotionAnimator>();
                var aimLockController = prefabRoot.GetComponent<PlayerAimLockController>() ??
                    prefabRoot.AddComponent<PlayerAimLockController>();
                var profileController = prefabRoot.GetComponent<PlayerAnimationProfileController>() ??
                    prefabRoot.AddComponent<PlayerAnimationProfileController>();
                profileController.Configure(profileCatalog);
                profileController.Bind(prefabRoot.GetComponent<PlayerWeaponController>());
                EditorUtility.SetDirty(profileController);
                locomotionAnimator.Bind(animator, visualRoot.transform);
                locomotionAnimator.BindGameplay(
                    prefabRoot.GetComponent<PlayerWeaponController>(),
                    prefabRoot.GetComponent<CombatantHealth>(),
                    aimLockController);
                locomotionAnimator.Configure(0.05f, 720f, PlayerMovementController.DefaultSpeedMetersPerSecond, 1.5f);
                locomotionAnimator.ConfigureActionClips(
                    rollClip != null ? rollClip.length : PlayerWeaponController.RollDurationSeconds,
                    slashClip != null ? slashClip.length : 0.75f,
                    hitClip != null ? hitClip.length : 0.45f,
                    deadClip != null ? deadClip.length : 1.1f);
                EditorUtility.SetDirty(aimLockController);

                var heldWeaponVisual = prefabRoot.GetComponent<PlayerHeldWeaponVisualController>() ??
                    prefabRoot.AddComponent<PlayerHeldWeaponVisualController>();
                heldWeaponVisual.BindWeaponSockets(
                    meleeHandSocket,
                    rangedHandSocket,
                    meleeHolsterSocket,
                    rangedHolsterSocket,
                    rangedMuzzleSocket,
                    shieldForearmSocket,
                    shieldBackSocket);
                heldWeaponVisual.Bind(prefabRoot.GetComponent<PlayerWeaponController>());
                heldWeaponVisual.NormalizeEquipmentVisualScales();
                heldWeaponVisual.RefreshAllEquipmentVisualTransforms();
                EditorUtility.SetDirty(heldWeaponVisual);

                var rangedHandPose = prefabRoot.GetComponent<PlayerRangedHandPoseController>() ??
                    prefabRoot.AddComponent<PlayerRangedHandPoseController>();
                rangedHandPose.Bind(
                    animator,
                    prefabRoot.GetComponent<PlayerWeaponController>(),
                    heldWeaponVisual);
                rangedHandPose.Configure(
                    PlayerRangedHandPoseController.DefaultBlendSpeed,
                    PlayerRangedHandPoseController.DefaultPositionWeight,
                    PlayerRangedHandPoseController.DefaultRotationWeight,
                    PlayerRangedHandPoseController.DefaultHandHeightMeters,
                    PlayerRangedHandPoseController.DefaultForwardOffsetMeters,
                    PlayerRangedHandPoseController.DefaultSideOffsetMeters);
                EditorUtility.SetDirty(rangedHandPose);
                var rangedHandPoseRelay = animator.GetComponent<PlayerRangedHandPoseIkRelay>() ??
                    animator.gameObject.AddComponent<PlayerRangedHandPoseIkRelay>();
                rangedHandPoseRelay.Bind(rangedHandPose);

                var shieldGuardPose = prefabRoot.GetComponent<PlayerShieldGuardPoseController>() ??
                    prefabRoot.AddComponent<PlayerShieldGuardPoseController>();
                shieldGuardPose.Bind(
                    animator,
                    prefabRoot.GetComponent<PlayerDefenseController>(),
                    heldWeaponVisual);
                shieldGuardPose.Configure(
                    PlayerShieldGuardPoseController.DefaultBlendSpeed,
                    PlayerShieldGuardPoseController.DefaultPositionWeight,
                    PlayerShieldGuardPoseController.DefaultRotationWeight,
                    PlayerShieldGuardPoseController.DefaultHandHeightMeters,
                    PlayerShieldGuardPoseController.DefaultForwardOffsetMeters,
                    PlayerShieldGuardPoseController.DefaultSideOffsetMeters);
                EditorUtility.SetDirty(shieldGuardPose);
                rangedHandPoseRelay.BindShield(shieldGuardPose);
                EditorUtility.SetDirty(rangedHandPoseRelay);

                var footPlacement = prefabRoot.GetComponent<PlayerFootPlacementController>() ??
                    prefabRoot.AddComponent<PlayerFootPlacementController>();
                footPlacement.Bind(
                    animator,
                    locomotionAnimator,
                    prefabRoot.GetComponent<PlayerWeaponController>(),
                    prefabRoot.GetComponent<CombatantHealth>(),
                    modernRig.LeftFootGroundTarget,
                    modernRig.RightFootGroundTarget,
                    modernRig.PelvisTarget);
                footPlacement.BindConstraints(
                    modernRig.LeftFootIkConstraint,
                    modernRig.RightFootIkConstraint,
                    modernRig.PelvisPositionConstraint);
                footPlacement.Configure(
                    PlayerFootPlacementController.DefaultStrideLengthMeters,
                    PlayerFootPlacementController.DefaultLockThresholdMetersPerSecond,
                    PlayerFootPlacementController.DefaultPelvisSmoothing,
                    PlayerFootPlacementController.DefaultFootHeightMeters,
                    PlayerFootPlacementController.DefaultRaycastDistanceMeters,
                    PlayerFootPlacementController.DefaultIkBlendSpeed,
                    PlayerFootPlacementController.DefaultYawBlend,
                    PlayerFootPlacementController.DefaultFootPlantHalfCycleSeconds);
                EditorUtility.SetDirty(footPlacement);

                var poseCoordinator = prefabRoot.GetComponent<PlayerAnimationPoseCoordinator>() ??
                    prefabRoot.AddComponent<PlayerAnimationPoseCoordinator>();
                poseCoordinator.Bind(
                    animator,
                    locomotionAnimator,
                    prefabRoot.GetComponent<PlayerWeaponController>(),
                    prefabRoot.GetComponent<PlayerDefenseController>(),
                    prefabRoot.GetComponent<CombatantHealth>(),
                    heldWeaponVisual,
                    rangedHandPose,
                    shieldGuardPose);
                poseCoordinator.BindRigs(
                    modernRig.BaseLocomotionRig,
                    modernRig.FullBodyActionRig,
                    modernRig.UpperBodyCombatRig,
                    modernRig.AdditivePhysicalResponseRig);
                poseCoordinator.BindRigConstraints(
                    modernRig.RightHandWeaponIkConstraint,
                    modernRig.LeftHandShieldIkConstraint,
                    modernRig.ChestAimConstraint);
                poseCoordinator.BindTargets(
                    modernRig.RightHandWeaponTarget,
                    modernRig.LeftHandShieldTarget,
                    modernRig.ChestAimTarget,
                    modernRig.PhysicalResponseTarget,
                    modernRig.LeftFootGroundTarget,
                    modernRig.RightFootGroundTarget);
                poseCoordinator.BindFootPlacement(
                    footPlacement,
                    modernRig.LeftFootIkConstraint,
                    modernRig.RightFootIkConstraint,
                    modernRig.PelvisPositionConstraint,
                    modernRig.PelvisTarget);
                poseCoordinator.Configure(
                    PlayerAnimationPoseCoordinator.DefaultRigBlendSpeed,
                    PlayerAnimationPoseCoordinator.DefaultImpulseDecaySpeed,
                    PlayerAnimationPoseCoordinator.DefaultLeanBlendSpeed,
                    PlayerAnimationPoseCoordinator.DefaultLeanSpeedReferenceMetersPerSecond,
                    PlayerAnimationPoseCoordinator.DefaultFootYawAimInfluenceMaxDegrees,
                    PlayerAnimationPoseCoordinator.DefaultHitReactionFootIkSuppressSeconds);
                poseCoordinator.ConfigureAnimationSystemMode(animationSystemMode);
                EditorUtility.SetDirty(poseCoordinator);

                ApplyAnimationSystemMode(prefabRoot, animationSystemMode);
                SanitizeRigForSave(prefabRoot.transform, animator.transform);
                var preSaveValidation = PlayerVisualAssemblyValidator.Validate(prefabRoot, PlayerPrefabPath);
                if (preSaveValidation.HasErrors)
                {
                    throw new InvalidOperationException(preSaveValidation.ToReportString());
                }

                Directory.CreateDirectory(Path.GetDirectoryName(PlayerPrefabPath) ?? string.Empty);
                PrefabUtility.SaveAsPrefabAsset(prefabRoot, PlayerPrefabPath);
                ValidateSavedPlayerVisualAssembly();
            }
            finally
            {
                Object.DestroyImmediate(prefabRoot);
            }
        }

        private static void GenerateRawMixamoAnimationDebugScene(
            RuntimeAnimatorController controller,
            Material canonicalMaterial,
            PlayerAnimationSystemMode animationSystemMode)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(RawMixamoDebugScenePath) ?? string.Empty);
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "RawMixamoDebug.FlatFloor";
            floor.transform.position = new Vector3(0f, -0.05f, 0f);
            floor.transform.localScale = new Vector3(8f, 0.1f, 8f);

            var selectedBodyPath = PlayerAnimationProfileAssetGenerator.ResolveSelectedSkinnedBodyFbxPath();
            var modelSourcePath = selectedBodyPath ?? PlayerAnimationProfileAssetGenerator.HollowMainRigPath;
            var modelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(modelSourcePath);
            if (modelPrefab == null)
            {
                throw new InvalidOperationException($"Cannot generate raw Mixamo debug scene without body prefab: {modelSourcePath}");
            }

            var bodyRoot = new GameObject("RawMixamoDebug.SelectedBody");
            bodyRoot.transform.position = Vector3.zero;
            var modelInstance = PrefabUtility.InstantiatePrefab(modelPrefab, bodyRoot.transform) as GameObject;
            modelInstance ??= Object.Instantiate(modelPrefab, bodyRoot.transform);
            modelInstance.name = ModelInstanceName;
            modelInstance.transform.localPosition = Vector3.zero;
            modelInstance.transform.localRotation = Quaternion.identity;
            modelInstance.transform.localScale = Vector3.one * PlayerAnimationProfileAssetGenerator.ResolveSelectedSkinnedBodyLocalScale();
            StripGameplayComponentsFromVisual(modelInstance);
            EnsureRendererMaterials(modelInstance, canonicalMaterial);

            var animator = modelInstance.GetComponent<Animator>();
            var sourceAnimator = modelPrefab.GetComponent<Animator>();
            if (animator == null)
            {
                animator = modelInstance.AddComponent<Animator>();
            }

            animator.avatar = ResolveMainCharacterAvatar(sourceAnimator);
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.Rebind();
            animator.Update(0f);
            AlignRendererBottomToGround(modelInstance.transform, groundY: 0f, clearanceMeters: 0.02f);
            var grounding = bodyRoot.AddComponent<SimpleFullBodyGroundingController>();
            grounding.Configure(
                modelInstance.transform,
                bodyRoot.transform,
                null,
                animationSystemMode == PlayerAnimationSystemMode.SimpleFullBodyAnimation,
                SimpleFullBodyGroundingController.DefaultGroundClearanceMeters,
                SimpleFullBodyGroundingController.DefaultMaxCorrectionMeters);
            grounding.ApplyGrounding();

            var cameraObject = new GameObject("RawMixamoDebug.Camera");
            var camera = cameraObject.AddComponent<Camera>();
            camera.transform.position = new Vector3(0f, 2.4f, -4.2f);
            camera.transform.rotation = Quaternion.Euler(18f, 0f, 0f);
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;

            var lightObject = new GameObject("RawMixamoDebug.DirectionalLight");
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.25f;
            light.transform.rotation = Quaternion.Euler(50f, -35f, 0f);

            var overlayObject = new GameObject("RawMixamoDebug.Overlay");
            var overlay = overlayObject.AddComponent<RawMixamoAnimationDebugOverlay>();
            overlay.Configure(animator, animationSystemMode);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, RawMixamoDebugScenePath);
        }

        private static void AlignRendererBottomToGround(Transform visualRoot, float groundY, float clearanceMeters)
        {
            if (visualRoot == null || !TryGetRendererBounds(visualRoot, out var bounds))
            {
                return;
            }

            var offsetY = groundY + clearanceMeters - bounds.min.y;
            visualRoot.position += Vector3.up * offsetY;
        }

        private static bool TryGetRendererBounds(Transform root, out Bounds bounds)
        {
            bounds = default;
            var hasBounds = false;
            foreach (var renderer in root.GetComponentsInChildren<Renderer>(includeInactive: true))
            {
                if (renderer == null || !renderer.enabled)
                {
                    continue;
                }

                if (!hasBounds)
                {
                    bounds = renderer.bounds;
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(renderer.bounds);
                }
            }

            return hasBounds && bounds.size.sqrMagnitude > 0.0001f;
        }

        private static GameObject CreateFreshPlayerPrefabRoot()
        {
            var root = new GameObject("PlayerCharacter", typeof(CapsuleCollider));
            var placeholder = root.AddComponent<PlaceholderPlayerController>();
            placeholder.ConfigureDefault();

            var health = root.AddComponent<CombatantHealth>();
            health.Configure(RoomCombatController.PlayerMaxHealth);

            var movement = root.AddComponent<PlayerMovementController>();
            var weapon = root.AddComponent<PlayerWeaponController>();
            var defense = root.AddComponent<PlayerDefenseController>();
            defense.ConfigureShieldProfile(ShieldGuardProfileDefinition.Resolve(null));
            root.AddComponent<PlayerAimLockController>();
            root.AddComponent<PlayerLocomotionAnimator>();
            root.AddComponent<PlayerHeldWeaponVisualController>();
            var rollVisual = root.AddComponent<PlayerRollVisualController>();
            rollVisual.Bind(weapon);
            var feedback = root.AddComponent<PlayerDamageFeedbackController>();
            feedback.Configure(null, null);
            root.AddComponent<PlayerAnimationProfileController>();
            root.AddComponent<PlayerRangedHandPoseController>();
            root.AddComponent<PlayerShieldGuardPoseController>();
            root.AddComponent<PlayerFootPlacementController>();
            root.AddComponent<PlayerAnimationPoseCoordinator>();
            root.AddComponent<SimpleFullBodyGroundingController>();

            var collider = root.GetComponent<CapsuleCollider>();
            collider.radius = PlaceholderPlayerController.DefaultRadiusMeters;
            collider.height = PlaceholderPlayerController.DefaultHeightMeters;
            collider.center = new Vector3(0f, PlaceholderPlayerController.DefaultHeightMeters * 0.5f, 0f);
            EditorUtility.SetDirty(movement);
            return root;
        }

        private static void ApplyAnimationSystemMode(GameObject prefabRoot, PlayerAnimationSystemMode animationSystemMode)
        {
            if (prefabRoot == null)
            {
                return;
            }

            var simpleMode = animationSystemMode == PlayerAnimationSystemMode.SimpleFullBodyAnimation;
            foreach (var rigBuilder in prefabRoot.GetComponentsInChildren<RigBuilder>(includeInactive: true))
            {
                rigBuilder.enabled = !simpleMode;
                EditorUtility.SetDirty(rigBuilder);
            }

            foreach (var constraint in prefabRoot.GetComponentsInChildren<TwoBoneIKConstraint>(includeInactive: true))
            {
                constraint.enabled = !simpleMode;
                constraint.weight = simpleMode ? 0f : constraint.weight;
                EditorUtility.SetDirty(constraint);
            }

            foreach (var constraint in prefabRoot.GetComponentsInChildren<MultiAimConstraint>(includeInactive: true))
            {
                constraint.enabled = !simpleMode;
                constraint.weight = simpleMode ? 0f : constraint.weight;
                EditorUtility.SetDirty(constraint);
            }

            foreach (var constraint in prefabRoot.GetComponentsInChildren<MultiPositionConstraint>(includeInactive: true))
            {
                constraint.enabled = !simpleMode;
                constraint.weight = simpleMode ? 0f : constraint.weight;
                EditorUtility.SetDirty(constraint);
            }

            foreach (var footPlacement in prefabRoot.GetComponentsInChildren<PlayerFootPlacementController>(includeInactive: true))
            {
                footPlacement.enabled = !simpleMode;
                EditorUtility.SetDirty(footPlacement);
            }

            foreach (var rangedHandPose in prefabRoot.GetComponentsInChildren<PlayerRangedHandPoseController>(includeInactive: true))
            {
                rangedHandPose.enabled = !simpleMode;
                EditorUtility.SetDirty(rangedHandPose);
            }

            foreach (var shieldPose in prefabRoot.GetComponentsInChildren<PlayerShieldGuardPoseController>(includeInactive: true))
            {
                shieldPose.enabled = !simpleMode;
                EditorUtility.SetDirty(shieldPose);
            }

            foreach (var relay in prefabRoot.GetComponentsInChildren<PlayerRangedHandPoseIkRelay>(includeInactive: true))
            {
                relay.enabled = !simpleMode;
                EditorUtility.SetDirty(relay);
            }

            foreach (var coordinator in prefabRoot.GetComponentsInChildren<PlayerAnimationPoseCoordinator>(includeInactive: true))
            {
                coordinator.ConfigureAnimationSystemMode(animationSystemMode);
                EditorUtility.SetDirty(coordinator);
            }

            foreach (var grounding in prefabRoot.GetComponentsInChildren<SimpleFullBodyGroundingController>(includeInactive: true))
            {
                grounding.SetGroundingEnabled(simpleMode);
                if (simpleMode)
                {
                    grounding.ApplyGrounding();
                }

                EditorUtility.SetDirty(grounding);
            }
        }

        private static ModernAnimationRigSetup EnsureModernAnimationRig(Animator animator, Transform visualRoot, Transform modelRoot)
        {
            var rigRoot = EnsureChild(
                modelRoot,
                PlayerAnimationPoseCoordinator.ModernAnimationRigRootName,
                Vector3.zero,
                Vector3.zero,
                Vector3.one);
            var targetsRoot = EnsureChild(
                rigRoot,
                PlayerAnimationPoseCoordinator.RigTargetsRootName,
                Vector3.zero,
                Vector3.zero,
                Vector3.one);
            var baseRig = EnsureRig(
                rigRoot,
                PlayerAnimationPoseCoordinator.BaseLocomotionRigName,
                1f);
            var fullBodyRig = EnsureRig(
                rigRoot,
                PlayerAnimationPoseCoordinator.FullBodyActionRigName,
                0f);
            var upperBodyRig = EnsureRig(
                rigRoot,
                PlayerAnimationPoseCoordinator.UpperBodyCombatRigName,
                0f);
            var additiveRig = EnsureRig(
                rigRoot,
                PlayerAnimationPoseCoordinator.AdditivePhysicalResponseRigName,
                0f);

            var rightHandTarget = EnsureChild(
                targetsRoot,
                PlayerAnimationPoseCoordinator.RightHandWeaponTargetName,
                new Vector3(0.42f, 1.08f, 0.48f),
                Vector3.zero,
                Vector3.one);
            var leftHandTarget = EnsureChild(
                targetsRoot,
                PlayerAnimationPoseCoordinator.LeftHandShieldTargetName,
                new Vector3(-0.34f, 1.04f, 0.46f),
                Vector3.zero,
                Vector3.one);
            var chestTarget = EnsureChild(
                targetsRoot,
                PlayerAnimationPoseCoordinator.ChestAimTargetName,
                new Vector3(0f, 1.15f, 2f),
                Vector3.zero,
                Vector3.one);
            var responseTarget = EnsureChild(
                targetsRoot,
                PlayerAnimationPoseCoordinator.PhysicalResponseTargetName,
                new Vector3(0f, 0.95f, -0.08f),
                Vector3.zero,
                Vector3.one);
            var leftFootTarget = EnsureChild(
                targetsRoot,
                PlayerAnimationPoseCoordinator.LeftFootGroundTargetName,
                new Vector3(-0.12f, 0f, 0.02f),
                Vector3.zero,
                Vector3.one);
            var rightFootTarget = EnsureChild(
                targetsRoot,
                PlayerAnimationPoseCoordinator.RightFootGroundTargetName,
                new Vector3(0.12f, 0f, 0.02f),
                Vector3.zero,
                Vector3.one);
            var pelvisTarget = EnsureChild(
                targetsRoot,
                PlayerAnimationPoseCoordinator.PelvisTargetName,
                new Vector3(0f, 0.9f, 0f),
                Vector3.zero,
                Vector3.one);
            var rightElbowHint = EnsureChild(
                targetsRoot,
                PlayerAnimationPoseCoordinator.RightElbowHintTargetName,
                new Vector3(0.48f, 0.9f, 0.08f),
                Vector3.zero,
                Vector3.one);
            var leftElbowHint = EnsureChild(
                targetsRoot,
                PlayerAnimationPoseCoordinator.LeftElbowHintTargetName,
                new Vector3(-0.48f, 0.9f, 0.08f),
                Vector3.zero,
                Vector3.one);
            var leftKneeHint = EnsureChild(
                targetsRoot,
                PlayerAnimationPoseCoordinator.LeftKneeHintTargetName,
                new Vector3(-0.24f, 0.45f, 0.18f),
                Vector3.zero,
                Vector3.one);
            var rightKneeHint = EnsureChild(
                targetsRoot,
                PlayerAnimationPoseCoordinator.RightKneeHintTargetName,
                new Vector3(0.24f, 0.45f, 0.18f),
                Vector3.zero,
                Vector3.one);

            var rightHandIk = EnsureTwoBoneIkConstraint(
                upperBodyRig.transform,
                PlayerAnimationPoseCoordinator.RightHandWeaponIkConstraintName,
                FindDescendant(modelRoot, RightUpperArmBoneName),
                FindDescendant(modelRoot, RightForearmBoneName),
                FindDescendant(modelRoot, RightHandBoneName),
                rightHandTarget,
                rightElbowHint);
            var leftHandIk = EnsureTwoBoneIkConstraint(
                upperBodyRig.transform,
                PlayerAnimationPoseCoordinator.LeftHandShieldIkConstraintName,
                FindDescendant(modelRoot, LeftUpperArmBoneName),
                FindDescendant(modelRoot, LeftForearmBoneName),
                FindDescendant(modelRoot, LeftHandBoneName),
                leftHandTarget,
                leftElbowHint);
            var chestAim = EnsureChestAimConstraint(
                upperBodyRig.transform,
                PlayerAnimationPoseCoordinator.ChestAimConstraintName,
                FindDescendant(modelRoot, BackShieldBoneName),
                chestTarget);
            var leftFootIk = EnsureTwoBoneIkConstraint(
                baseRig.transform,
                PlayerAnimationPoseCoordinator.LeftFootIkConstraintName,
                FindDescendant(modelRoot, LeftUpperLegBoneName),
                FindDescendant(modelRoot, LeftLowerLegBoneName),
                FindDescendant(modelRoot, LeftFootBoneName),
                leftFootTarget,
                leftKneeHint);
            var rightFootIk = EnsureTwoBoneIkConstraint(
                baseRig.transform,
                PlayerAnimationPoseCoordinator.RightFootIkConstraintName,
                FindDescendant(modelRoot, RightUpperLegBoneName),
                FindDescendant(modelRoot, RightLowerLegBoneName),
                FindDescendant(modelRoot, RightFootBoneName),
                rightFootTarget,
                rightKneeHint);
            var pelvisPosition = EnsureMultiPositionConstraint(
                baseRig.transform,
                PlayerAnimationPoseCoordinator.PelvisPositionConstraintName,
                FindDescendant(modelRoot, HipsBoneName),
                pelvisTarget,
                constrainX: false,
                constrainY: true,
                constrainZ: false);

            var rigBuilder = animator.GetComponent<RigBuilder>() ?? animator.gameObject.AddComponent<RigBuilder>();
            rigBuilder.layers.Clear();
            rigBuilder.layers.Add(new RigLayer(baseRig, IsConstraintValid(leftFootIk) || IsConstraintValid(rightFootIk) || IsConstraintValid(pelvisPosition)));
            rigBuilder.layers.Add(new RigLayer(fullBodyRig, false));
            rigBuilder.layers.Add(new RigLayer(upperBodyRig, IsConstraintValid(rightHandIk) || IsConstraintValid(leftHandIk) || IsConstraintValid(chestAim)));
            rigBuilder.layers.Add(new RigLayer(additiveRig, false));
            EditorUtility.SetDirty(rigBuilder);

            return new ModernAnimationRigSetup(
                baseRig,
                fullBodyRig,
                upperBodyRig,
                additiveRig,
                rightHandIk,
                leftHandIk,
                chestAim,
                leftFootIk,
                rightFootIk,
                pelvisPosition,
                rightHandTarget,
                leftHandTarget,
                chestTarget,
                responseTarget,
                leftFootTarget,
                rightFootTarget,
                pelvisTarget);
        }

        private static void SanitizeRigForSave(Transform prefabRoot, Transform animatorTransform)
        {
            if (prefabRoot == null || animatorTransform == null)
            {
                return;
            }

            foreach (var rigBuilder in prefabRoot.GetComponentsInChildren<RigBuilder>(includeInactive: true))
            {
                if (!IsTransformDescendantOf(rigBuilder.transform, animatorTransform))
                {
                    rigBuilder.enabled = false;
                    EditorUtility.SetDirty(rigBuilder);
                    Debug.LogWarning($"Disabled RigBuilder outside Animator hierarchy: {TransformPath(prefabRoot, rigBuilder.transform)}");
                    continue;
                }

                var layers = rigBuilder.layers
                    .Where(layer => layer.rig != null && IsTransformDescendantOf(layer.rig.transform, animatorTransform))
                    .ToList();
                if (layers.Count != rigBuilder.layers.Count)
                {
                    rigBuilder.layers.Clear();
                    rigBuilder.layers.AddRange(layers);
                    EditorUtility.SetDirty(rigBuilder);
                    Debug.LogWarning("Removed null/out-of-hierarchy RigBuilder layers before saving PlayerCharacter.prefab.");
                }
            }

            foreach (var constraint in prefabRoot.GetComponentsInChildren<TwoBoneIKConstraint>(includeInactive: true))
            {
                if (!IsTransformDescendantOf(constraint.transform, animatorTransform) || !HasValidTwoBoneData(constraint))
                {
                    DisableInvalidConstraint(prefabRoot, constraint);
                }
            }

            foreach (var constraint in prefabRoot.GetComponentsInChildren<MultiAimConstraint>(includeInactive: true))
            {
                if (!IsTransformDescendantOf(constraint.transform, animatorTransform) || !HasValidMultiAimData(constraint))
                {
                    DisableInvalidConstraint(prefabRoot, constraint);
                }
            }

            foreach (var constraint in prefabRoot.GetComponentsInChildren<MultiPositionConstraint>(includeInactive: true))
            {
                if (!IsTransformDescendantOf(constraint.transform, animatorTransform) || !HasValidMultiPositionData(constraint))
                {
                    DisableInvalidConstraint(prefabRoot, constraint);
                }
            }
        }

        private static void DisableInvalidConstraint<T>(Transform prefabRoot, T constraint) where T : Behaviour
        {
            if (constraint == null)
            {
                return;
            }

            constraint.enabled = false;
            EditorUtility.SetDirty(constraint);
            Debug.LogWarning($"Disabled invalid Animation Rigging constraint before save: {TransformPath(prefabRoot, constraint.transform)}");
        }

        private static bool HasValidTwoBoneData(TwoBoneIKConstraint constraint)
        {
            return constraint != null &&
                constraint.data.root != null &&
                constraint.data.mid != null &&
                constraint.data.tip != null &&
                constraint.data.target != null &&
                constraint.data.hint != null;
        }

        private static bool HasValidMultiAimData(MultiAimConstraint constraint)
        {
            return constraint != null &&
                constraint.data.constrainedObject != null &&
                constraint.data.sourceObjects.Count > 0 &&
                constraint.data.sourceObjects[0].transform != null;
        }

        private static bool HasValidMultiPositionData(MultiPositionConstraint constraint)
        {
            return constraint != null &&
                constraint.data.constrainedObject != null &&
                constraint.data.sourceObjects.Count > 0 &&
                constraint.data.sourceObjects[0].transform != null;
        }

        private static Rig EnsureRig(Transform parent, string rigName, float weight)
        {
            var rigTransform = EnsureChild(parent, rigName, Vector3.zero, Vector3.zero, Vector3.one);
            var rig = rigTransform.GetComponent<Rig>() ?? rigTransform.gameObject.AddComponent<Rig>();
            rig.weight = Mathf.Clamp01(weight);
            EditorUtility.SetDirty(rig);
            return rig;
        }

        private static TwoBoneIKConstraint EnsureTwoBoneIkConstraint(
            Transform parent,
            string constraintName,
            Transform root,
            Transform mid,
            Transform tip,
            Transform target,
            Transform hint)
        {
            var constraintTransform = EnsureChild(parent, constraintName, Vector3.zero, Vector3.zero, Vector3.one);
            var constraint = constraintTransform.GetComponent<TwoBoneIKConstraint>() ??
                constraintTransform.gameObject.AddComponent<TwoBoneIKConstraint>();
            var isValid = root != null && mid != null && tip != null && target != null;
            constraint.enabled = isValid;
            constraint.weight = 0f;
            constraint.data.root = root;
            constraint.data.mid = mid;
            constraint.data.tip = tip;
            constraint.data.target = target;
            constraint.data.hint = hint;
            constraint.data.targetPositionWeight = 1f;
            constraint.data.targetRotationWeight = 1f;
            constraint.data.hintWeight = hint != null ? 0.75f : 0f;
            constraint.data.maintainTargetPositionOffset = false;
            constraint.data.maintainTargetRotationOffset = false;
            EditorUtility.SetDirty(constraint);
            return constraint;
        }

        private static MultiAimConstraint EnsureChestAimConstraint(
            Transform parent,
            string constraintName,
            Transform chest,
            Transform target)
        {
            var constraintTransform = EnsureChild(parent, constraintName, Vector3.zero, Vector3.zero, Vector3.one);
            var constraint = constraintTransform.GetComponent<MultiAimConstraint>() ??
                constraintTransform.gameObject.AddComponent<MultiAimConstraint>();
            var isValid = chest != null && target != null;
            var sourceObjects = new WeightedTransformArray(1);
            sourceObjects[0] = new WeightedTransform(target, 1f);
            constraint.enabled = isValid;
            constraint.weight = 0f;
            constraint.data.constrainedObject = chest;
            constraint.data.sourceObjects = sourceObjects;
            constraint.data.maintainOffset = true;
            constraint.data.aimAxis = MultiAimConstraintData.Axis.Z;
            constraint.data.upAxis = MultiAimConstraintData.Axis.Y;
            constraint.data.worldUpType = MultiAimConstraintData.WorldUpType.SceneUp;
            constraint.data.worldUpAxis = MultiAimConstraintData.Axis.Y;
            constraint.data.constrainedXAxis = true;
            constraint.data.constrainedYAxis = true;
            constraint.data.constrainedZAxis = false;
            constraint.data.limits = new Vector2(-50f, 50f);
            EditorUtility.SetDirty(constraint);
            return constraint;
        }

        private static MultiPositionConstraint EnsureMultiPositionConstraint(
            Transform parent,
            string constraintName,
            Transform constrainedObject,
            Transform target,
            bool constrainX,
            bool constrainY,
            bool constrainZ)
        {
            var constraintTransform = EnsureChild(parent, constraintName, Vector3.zero, Vector3.zero, Vector3.one);
            var constraint = constraintTransform.GetComponent<MultiPositionConstraint>() ??
                constraintTransform.gameObject.AddComponent<MultiPositionConstraint>();
            var isValid = constrainedObject != null && target != null;
            var sourceObjects = new WeightedTransformArray(1);
            sourceObjects[0] = new WeightedTransform(target, 1f);
            constraint.enabled = isValid;
            constraint.weight = 0f;
            constraint.data.constrainedObject = constrainedObject;
            constraint.data.sourceObjects = sourceObjects;
            constraint.data.maintainOffset = true;
            constraint.data.constrainedXAxis = constrainX;
            constraint.data.constrainedYAxis = constrainY;
            constraint.data.constrainedZAxis = constrainZ;
            EditorUtility.SetDirty(constraint);
            return constraint;
        }

        private static bool IsConstraintValid(TwoBoneIKConstraint constraint)
        {
            return constraint != null &&
                constraint.enabled &&
                constraint.data.root != null &&
                constraint.data.mid != null &&
                constraint.data.tip != null &&
                constraint.data.target != null;
        }

        private static bool IsConstraintValid(MultiAimConstraint constraint)
        {
            return constraint != null &&
                constraint.enabled &&
                constraint.data.constrainedObject != null &&
                constraint.data.sourceObjects.Count > 0 &&
                constraint.data.sourceObjects[0].transform != null;
        }

        private static bool IsConstraintValid(MultiPositionConstraint constraint)
        {
            return constraint != null &&
                constraint.enabled &&
                constraint.data.constrainedObject != null &&
                constraint.data.sourceObjects.Count > 0 &&
                constraint.data.sourceObjects[0].transform != null;
        }

        private static Transform EnsureChild(
            Transform parent,
            string childName,
            Vector3 localPosition,
            Vector3 localEuler,
            Vector3 localScale)
        {
            var child = FindDirectChild(parent, childName);
            if (child == null)
            {
                var childObject = new GameObject(childName);
                child = childObject.transform;
                child.SetParent(parent, false);
            }

            child.localPosition = localPosition;
            child.localRotation = Quaternion.Euler(localEuler);
            child.localScale = localScale;
            return child;
        }

        private static Transform FindDirectChild(Transform parent, string childName)
        {
            if (parent == null)
            {
                return null;
            }

            for (var index = 0; index < parent.childCount; index++)
            {
                var child = parent.GetChild(index);
                if (child.name == childName)
                {
                    return child;
                }
            }

            return null;
        }

        private static bool IsTransformDescendantOf(Transform child, Transform ancestor)
        {
            var current = child;
            while (current != null)
            {
                if (current == ancestor)
                {
                    return true;
                }

                current = current.parent;
            }

            return false;
        }

        private static string TransformPath(Transform root, Transform child)
        {
            if (root == null || child == null)
            {
                return string.Empty;
            }

            var names = new Stack<string>();
            var current = child;
            while (current != null)
            {
                names.Push(current.name);
                if (current == root)
                {
                    break;
                }

                current = current.parent;
            }

            return string.Join("/", names);
        }

        private readonly struct DirectionalLocomotionImportSpec
        {
            public DirectionalLocomotionImportSpec(
                string label,
                string path,
                string clipName,
                Vector2 direction,
                float speedMagnitude,
                bool usesBaseForwardClip = false)
            {
                Label = label;
                Path = path;
                ClipName = clipName;
                Direction = direction;
                SpeedMagnitude = speedMagnitude;
                UsesBaseForwardClip = usesBaseForwardClip;
            }

            public string Label { get; }

            public string Path { get; }

            public string ClipName { get; }

            public Vector2 Direction { get; }

            public float SpeedMagnitude { get; }

            public bool UsesBaseForwardClip { get; }
        }

        private readonly struct DirectionalLocomotionClip
        {
            public DirectionalLocomotionClip(string label, AnimationClip clip, Vector2 threshold)
            {
                Label = label;
                Clip = clip;
                Threshold = threshold;
            }

            public string Label { get; }

            public AnimationClip Clip { get; }

            public Vector2 Threshold { get; }
        }

        private readonly struct ModernAnimationRigSetup
        {
            public ModernAnimationRigSetup(
                Rig baseLocomotionRig,
                Rig fullBodyActionRig,
                Rig upperBodyCombatRig,
                Rig additivePhysicalResponseRig,
                TwoBoneIKConstraint rightHandWeaponIkConstraint,
                TwoBoneIKConstraint leftHandShieldIkConstraint,
                MultiAimConstraint chestAimConstraint,
                TwoBoneIKConstraint leftFootIkConstraint,
                TwoBoneIKConstraint rightFootIkConstraint,
                MultiPositionConstraint pelvisPositionConstraint,
                Transform rightHandWeaponTarget,
                Transform leftHandShieldTarget,
                Transform chestAimTarget,
                Transform physicalResponseTarget,
                Transform leftFootGroundTarget,
                Transform rightFootGroundTarget,
                Transform pelvisTarget)
            {
                BaseLocomotionRig = baseLocomotionRig;
                FullBodyActionRig = fullBodyActionRig;
                UpperBodyCombatRig = upperBodyCombatRig;
                AdditivePhysicalResponseRig = additivePhysicalResponseRig;
                RightHandWeaponIkConstraint = rightHandWeaponIkConstraint;
                LeftHandShieldIkConstraint = leftHandShieldIkConstraint;
                ChestAimConstraint = chestAimConstraint;
                LeftFootIkConstraint = leftFootIkConstraint;
                RightFootIkConstraint = rightFootIkConstraint;
                PelvisPositionConstraint = pelvisPositionConstraint;
                RightHandWeaponTarget = rightHandWeaponTarget;
                LeftHandShieldTarget = leftHandShieldTarget;
                ChestAimTarget = chestAimTarget;
                PhysicalResponseTarget = physicalResponseTarget;
                LeftFootGroundTarget = leftFootGroundTarget;
                RightFootGroundTarget = rightFootGroundTarget;
                PelvisTarget = pelvisTarget;
            }

            public Rig BaseLocomotionRig { get; }

            public Rig FullBodyActionRig { get; }

            public Rig UpperBodyCombatRig { get; }

            public Rig AdditivePhysicalResponseRig { get; }

            public TwoBoneIKConstraint RightHandWeaponIkConstraint { get; }

            public TwoBoneIKConstraint LeftHandShieldIkConstraint { get; }

            public MultiAimConstraint ChestAimConstraint { get; }

            public TwoBoneIKConstraint LeftFootIkConstraint { get; }

            public TwoBoneIKConstraint RightFootIkConstraint { get; }

            public MultiPositionConstraint PelvisPositionConstraint { get; }

            public Transform RightHandWeaponTarget { get; }

            public Transform LeftHandShieldTarget { get; }

            public Transform ChestAimTarget { get; }

            public Transform PhysicalResponseTarget { get; }

            public Transform LeftFootGroundTarget { get; }

            public Transform RightFootGroundTarget { get; }

            public Transform PelvisTarget { get; }
        }

        private static void RemoveExistingVisuals(Transform root)
        {
            var targets = new List<GameObject>();
            for (var index = 0; index < root.childCount; index++)
            {
                var child = root.GetChild(index);
                if (child.name == LegacyCapsuleName || child.name == VisualRootName || child.name == ModelInstanceName)
                {
                    targets.Add(child.gameObject);
                    continue;
                }

                var marker = child.GetComponentInChildren<PresentationVisualMarker>(includeInactive: true);
                if (marker != null && marker.Role == PresentationPrefabRole.Player)
                {
                    targets.Add(child.gameObject);
                }
            }

            foreach (var target in targets.Distinct())
            {
                Object.DestroyImmediate(target);
            }
        }

        private static void StripGameplayComponentsFromVisual(GameObject visual)
        {
            foreach (var component in visual.GetComponentsInChildren<Collider>(includeInactive: true))
            {
                Object.DestroyImmediate(component);
            }

            foreach (var component in visual.GetComponentsInChildren<Rigidbody>(includeInactive: true))
            {
                Object.DestroyImmediate(component);
            }

            foreach (var component in visual.GetComponentsInChildren<Camera>(includeInactive: true))
            {
                Object.DestroyImmediate(component.gameObject);
            }

            foreach (var component in visual.GetComponentsInChildren<Light>(includeInactive: true))
            {
                Object.DestroyImmediate(component.gameObject);
            }

            foreach (var component in visual.GetComponentsInChildren<Animation>(includeInactive: true))
            {
                Object.DestroyImmediate(component);
            }
        }

        private static Transform EnsureMeleeHandSocket(Transform modelRoot)
        {
            var rightHand = FindDescendant(modelRoot, RightHandBoneName);
            if (rightHand == null)
            {
                throw new InvalidOperationException($"Meshy main character model does not contain a {RightHandBoneName} bone.");
            }

            var socket = rightHand.Find(PlayerHeldWeaponVisualController.MeleeHandSocketName);
            if (socket == null)
            {
                var socketObject = new GameObject(PlayerHeldWeaponVisualController.MeleeHandSocketName);
                socketObject.transform.SetParent(rightHand, false);
                socket = socketObject.transform;
            }

            socket.localPosition = PlayerHeldWeaponVisualController.DefaultMeleeSocketLocalPosition;
            socket.localRotation = Quaternion.Euler(PlayerHeldWeaponVisualController.DefaultMeleeSocketLocalEuler);
            socket.localScale = PlayerHeldWeaponVisualController.DefaultMeleeSocketLocalScale;
            return socket;
        }

        private static Transform EnsureSocket(
            Transform parent,
            string socketName,
            Vector3 localPosition,
            Vector3 localEuler,
            Vector3 localScale)
        {
            if (parent == null)
            {
                throw new InvalidOperationException($"Cannot create {socketName} without a parent transform.");
            }

            var socket = parent.Find(socketName);
            if (socket == null)
            {
                var socketObject = new GameObject(socketName);
                socketObject.transform.SetParent(parent, false);
                socket = socketObject.transform;
            }

            socket.localPosition = localPosition;
            socket.localRotation = Quaternion.Euler(localEuler);
            socket.localScale = localScale;
            return socket;
        }

        private static Transform FindDescendant(Transform root, string childName)
        {
            if (root == null)
            {
                return null;
            }

            foreach (var child in root.GetComponentsInChildren<Transform>(includeInactive: true))
            {
                if (string.Equals(child.name, childName, StringComparison.Ordinal) ||
                    IsNormalizedBoneNameMatch(NormalizeTransformName(child.name), childName))
                {
                    return child;
                }
            }

            return null;
        }

        private static string NormalizeTransformName(string transformName)
        {
            if (string.IsNullOrEmpty(transformName))
            {
                return string.Empty;
            }

            var namespaceSeparator = transformName.LastIndexOf(':');
            return namespaceSeparator >= 0 && namespaceSeparator < transformName.Length - 1
                ? transformName[(namespaceSeparator + 1)..]
                : transformName;
        }

        private static bool IsNormalizedBoneNameMatch(string normalizedName, string expectedName)
        {
            if (string.Equals(normalizedName, expectedName, StringComparison.Ordinal))
            {
                return true;
            }

            return expectedName switch
            {
                BackShieldBoneName => string.Equals(normalizedName, "Spine2", StringComparison.Ordinal),
                _ => false
            };
        }

        private static void EnsureRendererMaterials(GameObject visual, Material material)
        {
            if (material == null)
            {
                return;
            }

            foreach (var renderer in visual.GetComponentsInChildren<Renderer>(includeInactive: true))
            {
                renderer.enabled = true;
                var materials = renderer.sharedMaterials;
                if (materials == null || materials.Length == 0)
                {
                    renderer.sharedMaterials = new[] { material };
                    continue;
                }

                for (var index = 0; index < materials.Length; index++)
                {
                    materials[index] = material;
                }

                renderer.sharedMaterials = materials;
            }
        }

        private static Avatar ResolveMainCharacterAvatar(Animator sourceAnimator)
        {
            if (sourceAnimator != null && sourceAnimator.avatar != null && sourceAnimator.avatar.isValid)
            {
                return sourceAnimator.avatar;
            }

            return PlayerAnimationProfileAssetGenerator.EnsureSharedAvatar();
        }

        private static void ValidateSavedPlayerVisualAssembly()
        {
            var validation = PlayerVisualAssemblyValidator.ValidatePlayerPrefab();
            PlayerVisualAssemblyValidator.WriteEquipmentScaleReportForPlayerPrefab(validation);
            if (validation.HasErrors)
            {
                throw new InvalidOperationException(validation.ToReportString());
            }

            if (validation.Warnings.Count > 0)
            {
                Debug.LogWarning(validation.ToReportString());
            }
            else
            {
                Debug.Log(validation.ToReportString());
            }
        }

        private static void EnsureVisibleBody(Transform visualRoot, GameObject rigInstance, Material material)
        {
            if (visualRoot == null || rigInstance == null)
            {
                return;
            }

            var existingFallback = visualRoot.Find(PlayerVisualAssemblyValidator.VisualBodyName);
            var hasUsableSkinnedBody = rigInstance.GetComponentsInChildren<SkinnedMeshRenderer>(includeInactive: true)
                .Any(IsUsableSkinnedBodyRenderer);
            if (hasUsableSkinnedBody)
            {
                if (existingFallback != null)
                {
                    Object.DestroyImmediate(existingFallback.gameObject);
                }

                return;
            }

            if (existingFallback != null)
            {
                Object.DestroyImmediate(existingFallback.gameObject);
            }

            var modelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerAnimationProfileAssetGenerator.HollowMainModelObjPath);
            if (modelPrefab == null)
            {
                Debug.LogWarning(
                    $"Main character rig has no usable skinned body renderer and visible fallback model is missing: {PlayerAnimationProfileAssetGenerator.HollowMainModelObjPath}");
                return;
            }

            var fallback = PrefabUtility.InstantiatePrefab(modelPrefab, visualRoot) as GameObject;
            if (fallback == null)
            {
                fallback = Object.Instantiate(modelPrefab, visualRoot);
            }

            fallback.name = PlayerVisualAssemblyValidator.VisualBodyName;
            fallback.transform.localPosition = Vector3.zero;
            fallback.transform.localRotation = Quaternion.identity;
            fallback.transform.localScale = Vector3.one;
            SetLayerRecursively(fallback, visualRoot.gameObject.layer);
            StripGameplayComponentsFromVisual(fallback);
            EnsureRendererMaterials(fallback, material);
            Debug.LogWarning(
                $"{PlayerVisualAssemblyValidator.TemporaryStaticBodyFallbackLabel}: using {PlayerAnimationProfileAssetGenerator.HollowMainModelObjPath}. " +
                PlayerVisualAssemblyValidator.FinalBodyReplacementRequirement);
            EditorUtility.SetDirty(fallback);
        }

        private static bool IsUsableSkinnedBodyRenderer(SkinnedMeshRenderer renderer)
        {
            if (renderer == null ||
                !renderer.enabled ||
                renderer.sharedMesh == null ||
                renderer.rootBone == null ||
                renderer.bones == null ||
                renderer.bones.Length == 0)
            {
                return false;
            }

            if (renderer.sharedMaterials == null ||
                renderer.sharedMaterials.Length == 0 ||
                renderer.sharedMaterials.Any(material => material == null))
            {
                return false;
            }

            var boundsSize = ScaledBoundsSize(renderer);
            return boundsSize.y > 0.75f &&
                boundsSize.y < 3f &&
                boundsSize.x > 0.12f &&
                boundsSize.z > 0.04f;
        }

        private static Vector3 ScaledBoundsSize(SkinnedMeshRenderer renderer)
        {
            var size = renderer.sharedMesh.bounds.size;
            var scale = renderer.transform.lossyScale;
            return new Vector3(
                Mathf.Abs(size.x * scale.x),
                Mathf.Abs(size.y * scale.y),
                Mathf.Abs(size.z * scale.z));
        }

        private static void SetLayerRecursively(GameObject root, int layer)
        {
            if (root == null)
            {
                return;
            }

            root.layer = layer;
            foreach (Transform child in root.transform)
            {
                SetLayerRecursively(child.gameObject, layer);
            }
        }
    }
}
