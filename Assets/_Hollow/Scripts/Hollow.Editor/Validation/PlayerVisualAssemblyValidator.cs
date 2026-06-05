using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Hollow.Combat;
using Hollow.Data.Definitions;
using Hollow.Editor.Generation;
using Hollow.Presentation;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.Animations.Rigging;

namespace Hollow.Editor.Validation
{
    public static class PlayerVisualAssemblyValidator
    {
        public const string PlayerPrefabPath = "Assets/_Hollow/Prefabs/Player/PlayerCharacter.prefab";
        public const string VisualRootName = "MainCharacter_VisualRoot";
        public const string ModelInstanceName = "MainCharacter_MeshyModel";
        public const string VisualBodyName = "VisualBody";
        public const string EquipmentScaleReportPath = "Assets/_Hollow/Data/AnimationProfiles/EquipmentScaleReport.txt";
        public const string TemporaryStaticBodyFallbackLabel = "TEMP_STATIC_BODY_FALLBACK_NON_SKINNED";
        public const string FinalBodyReplacementRequirement =
            "Replace with a skinned mesh bound to Hollow_Main_Rig.fbx, a single rig+body FBX, or a Mixamo-compatible skinned character prefab.";

        private const float MinimumBodyHeightMeters = 0.75f;
        private const float MaximumBodyHeightMeters = 3.0f;
        private const float MinimumBodyWidthMeters = 0.12f;
        private const float MinimumBodyDepthMeters = 0.04f;
        private const float MinimumEquipmentMaxDimensionMeters = 0.05f;
        private const float MaximumEquipmentMaxDimensionMeters = 3.0f;
        private const float MaximumShieldMaxDimensionMeters = 1.2f;
        private const float MaximumMeleeMaxDimensionMeters = 2.2f;
        private const float MaximumRangedMaxDimensionMeters = 1.5f;

        [MenuItem("Hollow/Debug/Validate Player Visual Assembly")]
        public static void ValidatePlayerVisualAssemblyMenu()
        {
            var result = ValidatePlayerPrefab();
            var report = result.ToReportString();
            var equipmentReportPath = WriteEquipmentScaleReportForPlayerPrefab(result);
            Debug.Log($"Wrote equipment scale report: {equipmentReportPath}");
            if (result.HasErrors)
            {
                Debug.LogError(report);
                return;
            }

            if (result.Warnings.Count > 0)
            {
                Debug.LogWarning(report);
                return;
            }

            Debug.Log(report);
        }

        public static PlayerVisualAssemblyValidationResult ValidatePlayerPrefab()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            return Validate(prefab, PlayerPrefabPath);
        }

        public static string WriteEquipmentScaleReportForPlayerPrefab(PlayerVisualAssemblyValidationResult validation = null)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            validation ??= Validate(prefab, PlayerPrefabPath);
            return WriteEquipmentScaleReport(prefab, validation);
        }

        public static string WriteEquipmentScaleReport(
            GameObject prefabRoot,
            PlayerVisualAssemblyValidationResult validation = null)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(EquipmentScaleReportPath) ?? string.Empty);
            var builder = new StringBuilder();
            builder.AppendLine("Hollow Soul Equipment Scale Report");
            builder.AppendLine("ReportVersion: 1");
            builder.AppendLine($"UnityTimestampUtc: {DateTime.UtcNow:O}");
            builder.AppendLine($"PrefabPath: {PlayerPrefabPath}");
            builder.AppendLine($"ValidationErrors: {validation?.Errors.Count ?? 0}");
            builder.AppendLine($"ValidationWarnings: {validation?.Warnings.Count ?? 0}");
            if (prefabRoot == null)
            {
                builder.AppendLine("ERROR: Player prefab is missing.");
                File.WriteAllText(EquipmentScaleReportPath, builder.ToString());
                AssetDatabase.ImportAsset(EquipmentScaleReportPath);
                return EquipmentScaleReportPath;
            }

            var profile = prefabRoot.GetComponent<PlayerAnimationProfileController>();
            var heldVisual = prefabRoot.GetComponent<PlayerHeldWeaponVisualController>();
            var visualRoot = FindDescendant(prefabRoot.transform, VisualRootName);
            var animator = prefabRoot.GetComponentInChildren<Animator>(includeInactive: true);
            builder.AppendLine($"CurrentProfile: {(profile != null ? profile.CurrentProfileId.ToString() : "<none>")}");
            builder.AppendLine($"NormalizationPassCount: {(heldVisual != null ? heldVisual.EquipmentNormalizationPassCount : 0)}");
            AppendScaleLine(builder, "PlayerCharacterRoot", prefabRoot.transform);
            AppendScaleLine(builder, VisualRootName, visualRoot);
            AppendScaleLine(builder, "Animator", animator != null ? animator.transform : null);
            AppendScaleLine(builder, "mixamorig:Hips", FindDescendantNormalized(prefabRoot.transform, "Hips"));
            AppendScaleLine(builder, "RightHand", FindDescendantNormalized(prefabRoot.transform, "RightHand"));
            AppendScaleLine(builder, "LeftHand", FindDescendantNormalized(prefabRoot.transform, "LeftHand"));
            AppendScaleLine(builder, "LeftForeArm", FindDescendantNormalized(prefabRoot.transform, "LeftForeArm"));
            AppendScaleLine(builder, PlayerHeldWeaponVisualController.MeleeHandSocketName, FindDescendant(prefabRoot.transform, PlayerHeldWeaponVisualController.MeleeHandSocketName));
            AppendScaleLine(builder, PlayerHeldWeaponVisualController.RangedHandSocketName, FindDescendant(prefabRoot.transform, PlayerHeldWeaponVisualController.RangedHandSocketName));
            AppendScaleLine(builder, PlayerHeldWeaponVisualController.MeleeHolsterSocketName, FindDescendant(prefabRoot.transform, PlayerHeldWeaponVisualController.MeleeHolsterSocketName));
            AppendScaleLine(builder, PlayerHeldWeaponVisualController.RangedHolsterSocketName, FindDescendant(prefabRoot.transform, PlayerHeldWeaponVisualController.RangedHolsterSocketName));
            AppendScaleLine(builder, PlayerHeldWeaponVisualController.ShieldForearmSocketName, FindDescendant(prefabRoot.transform, PlayerHeldWeaponVisualController.ShieldForearmSocketName));
            AppendScaleLine(builder, PlayerHeldWeaponVisualController.ShieldBackSocketName, FindDescendant(prefabRoot.transform, PlayerHeldWeaponVisualController.ShieldBackSocketName));
            AppendScaleLine(builder, PlayerHeldWeaponVisualController.RangedMuzzleSocketName, FindDescendant(prefabRoot.transform, PlayerHeldWeaponVisualController.RangedMuzzleSocketName));

            var wrappers = KnownEquipmentWrappers(prefabRoot.transform).ToArray();
            var duplicateWrappers = wrappers
                .GroupBy(wrapper => wrapper.name)
                .Where(group => group.Count() > 1)
                .ToArray();
            var equipmentMarkers = prefabRoot.GetComponentsInChildren<PresentationVisualMarker>(includeInactive: true)
                .Where(IsEquipmentMarker)
                .ToArray();
            var duplicateMarkers = equipmentMarkers
                .GroupBy(marker => marker.Role)
                .Where(group => group.Count() > 1)
                .ToArray();

            builder.AppendLine();
            var hasUnnormalizedWrapperScale = wrappers.Any(IsLikelyUnnormalizedInheritedScale);
            var hasOversizedSourcePrefab = equipmentMarkers.Any(marker => HasOversizedSourcePrefab(marker, out _));
            var hasInheritedMixamoScale = wrappers.Any(wrapper => wrapper.parent != null && MaxDimension(wrapper.parent.lossyScale) > 10f);
            var hasInvalidEquipmentBounds = wrappers.Any(HasInvalidEquipmentBounds);
            var hasNoRecordedRuntimeNormalizePass = heldVisual == null || heldVisual.EquipmentNormalizationPassCount == 0;

            builder.AppendLine("RootCauseClassification");
            builder.AppendLine($"A_PrefabNotRegeneratedOrStaleWrapperScales: {hasUnnormalizedWrapperScale}");
            builder.AppendLine($"B_EquipmentPrefabImportScaleHuge: {hasOversizedSourcePrefab}");
            builder.AppendLine($"C_EquipmentInheritsMixamo100xScale: {hasInheritedMixamoScale}");
            builder.AppendLine($"D_CompensationAppliedBeforeVisualsSpawned: {hasNoRecordedRuntimeNormalizePass && hasInvalidEquipmentBounds}");
            builder.AppendLine($"E_RuntimeProfileSwitchRecreatedWithoutCompensation: {duplicateWrappers.Length > 0}");
            builder.AppendLine($"F_ShieldHandBackMovementBypassedNormalization: {wrappers.Any(wrapper => wrapper.name == PlayerHeldWeaponVisualController.EquippedShieldVisualName && IsLikelyUnnormalizedInheritedScale(wrapper))}");
            builder.AppendLine($"G_ValidatorMeasuringWrongObject: {validation != null && validation.EquipmentRendererDetails.Count != equipmentMarkers.Length}");
            builder.AppendLine($"H_DuplicateStaleEquipmentInstances: {duplicateWrappers.Length > 0 || duplicateMarkers.Length > 0}");

            builder.AppendLine();
            builder.AppendLine("EquipmentInstances");
            foreach (var wrapper in wrappers)
            {
                AppendEquipmentWrapperReport(builder, prefabRoot.transform, wrapper, profile, heldVisual);
            }

            foreach (var marker in equipmentMarkers.Where(marker => !IsUnderKnownEquipmentWrapper(marker.transform)))
            {
                AppendLooseEquipmentMarkerReport(builder, prefabRoot.transform, marker, profile, heldVisual);
            }

            builder.AppendLine();
            builder.AppendLine("ValidationDetails");
            if (validation != null)
            {
                builder.Append(validation.ToReportString());
            }

            File.WriteAllText(EquipmentScaleReportPath, builder.ToString());
            AssetDatabase.ImportAsset(EquipmentScaleReportPath);
            return EquipmentScaleReportPath;
        }

        public static PlayerVisualAssemblyValidationResult Validate(GameObject prefabRoot, string prefabPath = PlayerPrefabPath)
        {
            var result = new PlayerVisualAssemblyValidationResult(prefabPath);
            if (prefabRoot == null)
            {
                result.Errors.Add($"Missing player prefab: {prefabPath}");
                return result;
            }

            var animator = prefabRoot.GetComponentInChildren<Animator>(includeInactive: true);
            result.AnimatorAvatarAssigned = animator != null && animator.avatar != null && animator.avatar.isValid;
            result.AnimatorControllerAssigned = animator != null && animator.runtimeAnimatorController != null;
            result.AnimatorPath = animator != null ? TransformPath(prefabRoot.transform, animator.transform) : string.Empty;

            if (animator == null)
            {
                result.Errors.Add("Animator is missing from PlayerCharacter.prefab.");
            }
            else
            {
                if (!result.AnimatorAvatarAssigned)
                {
                    result.Errors.Add("Animator Avatar is null or invalid.");
                }

                if (!result.AnimatorControllerAssigned)
                {
                    result.Errors.Add("Animator controller is null.");
                }
            }

            var visualRoot = FindDescendant(prefabRoot.transform, VisualRootName);
            if (visualRoot == null)
            {
                result.Errors.Add($"Missing {VisualRootName}.");
                return result;
            }

            result.SelectedSkinnedBodyFbx = PlayerAnimationProfileAssetGenerator.ResolveSelectedSkinnedBodyFbxPath() ?? string.Empty;
            result.SelectedAvatarSource = PlayerAnimationProfileAssetGenerator.ResolveSharedAvatarSourcePath();
            var fallbackRoot = visualRoot.Find(VisualBodyName);
            var animatorTransform = animator != null ? animator.transform : null;
            var bodyRenderers = ResolveBodyRenderers(visualRoot, animatorTransform, fallbackRoot).ToArray();
            var bodyRendererSet = new HashSet<Renderer>(bodyRenderers);
            var allVisualRenderers = visualRoot.GetComponentsInChildren<Renderer>(includeInactive: true);
            var equipmentRenderers = allVisualRenderers
                .Where(renderer =>
                    renderer != null &&
                    !bodyRendererSet.Contains(renderer) &&
                    TryFindEquipmentMarker(renderer.transform, out _))
                .ToArray();

            result.BodyRendererCount = bodyRenderers.Length;
            result.SkinnedBodyRendererCount = bodyRenderers.OfType<SkinnedMeshRenderer>().Count();
            result.MeshBodyRendererCount = bodyRenderers.OfType<MeshRenderer>().Count();
            result.WeaponRendererCount = equipmentRenderers.Count(renderer =>
                TryFindEquipmentMarker(renderer.transform, out var marker) &&
                (marker.Role == PresentationPrefabRole.WeaponMelee ||
                    marker.Role == PresentationPrefabRole.WeaponRanged));
            result.ShieldRendererCount = equipmentRenderers.Count(renderer =>
                TryFindEquipmentMarker(renderer.transform, out var marker) &&
                marker.Role == PresentationPrefabRole.Armor);
            result.UsesTemporaryStaticFallback = fallbackRoot != null &&
                result.MeshBodyRendererCount > 0 &&
                result.SkinnedBodyRendererCount == 0;
            result.BodyUsesStaticFallback = result.UsesTemporaryStaticFallback;
            result.BodyUnderAnimatorHierarchy = animatorTransform != null &&
                bodyRenderers.Length > 0 &&
                bodyRenderers.All(renderer => IsDescendantOf(renderer.transform, animatorTransform));
            PopulateSkinnedBodyDiagnostics(prefabRoot.transform, animatorTransform, bodyRenderers, result);
            result.BodySkinnedAndAnimationReady =
                result.AnimatorAvatarAssigned &&
                result.AnimatorControllerAssigned &&
                result.SkinnedBodyRendererCount > 0 &&
                result.BodyUnderAnimatorHierarchy &&
                result.BodyWillDeformWithAnimator &&
                !result.UsesTemporaryStaticFallback;

            PopulateRendererDetails(prefabRoot.transform, bodyRenderers, result);
            PopulateEquipmentDetails(prefabRoot.transform, equipmentRenderers, result);
            PopulateDuplicateEquipmentDiagnostics(prefabRoot.transform, result);
            result.BodyVisibleForDebug =
                result.BodyRendererCount > 0 &&
                result.EnabledBodyRendererCount > 0 &&
                result.BodyRenderersWithMaterialCount == result.BodyRendererCount &&
                IsSaneBodyBounds(result.BodyBoundsSize);
            result.EquipmentVisualScaleValid = result.OversizedEquipmentCount == 0;

            if (result.BodyRendererCount == 0)
            {
                result.Errors.Add("No body renderer found. Weapon/shield renderers do not count as player body visibility.");
            }

            if (result.EnabledBodyRendererCount == 0)
            {
                result.Errors.Add("All body renderers are disabled.");
            }

            if (result.BodyRenderersWithMaterialCount != result.BodyRendererCount)
            {
                result.Errors.Add("At least one body renderer has no assigned material.");
            }

            if (!result.UsesTemporaryStaticFallback && result.BodyRenderersWithTextureCount == 0)
            {
                result.Errors.Add("Skinned player body has no assigned albedo/base texture.");
            }

            if (!IsSaneBodyBounds(result.BodyBoundsSize))
            {
                result.Errors.Add($"Body renderer bounds are tiny or implausible: {FormatVector(result.BodyBoundsSize)}.");
            }

            if (!result.EquipmentVisualScaleValid)
            {
                result.Errors.Add($"{result.OversizedEquipmentCount} equipment renderer(s) have implausible world bounds.");
            }

            ValidateRigHierarchy(prefabRoot.transform, animatorTransform, result);
            PopulateAnimationSystemDiagnostics(prefabRoot, result);
            ValidateMissingScriptsAndReferences(prefabRoot, result);

            if (result.AnimationSystemMode == PlayerAnimationSystemMode.SimpleFullBodyAnimation &&
                result.SimpleModeHasActiveRigInfluence)
            {
                result.Errors.Add("SimpleFullBodyAnimation mode has active rig, IK, or foot-placement influence.");
            }

            if (result.UsesTemporaryStaticFallback)
            {
                var message = $"{TemporaryStaticBodyFallbackLabel}: {FinalBodyReplacementRequirement}";
                if (!string.IsNullOrWhiteSpace(result.SelectedSkinnedBodyFbx))
                {
                    result.Errors.Add($"{message} Valid skinned body candidate exists: {result.SelectedSkinnedBodyFbx}");
                }
                else
                {
                    result.Warnings.Add(message);
                }
            }

            if (!result.UsesTemporaryStaticFallback && result.SkinnedBodyRendererCount == 0)
            {
                result.Errors.Add("Player body is not using a SkinnedMeshRenderer.");
            }

            if (!result.UsesTemporaryStaticFallback && !result.BodyWillDeformWithAnimator)
            {
                result.Errors.Add("Player body skinned renderer is not fully bound to the active Animator skeleton.");
            }

            return result;
        }

        private static IEnumerable<Renderer> ResolveBodyRenderers(
            Transform visualRoot,
            Transform animatorTransform,
            Transform fallbackRoot)
        {
            if (fallbackRoot != null)
            {
                return fallbackRoot.GetComponentsInChildren<Renderer>(includeInactive: true)
                    .Where(renderer => renderer != null);
            }

            if (animatorTransform != null && IsDescendantOf(animatorTransform, visualRoot))
            {
                return animatorTransform.GetComponentsInChildren<Renderer>(includeInactive: true)
                    .Where(renderer => renderer != null && !IsUnderPresentationVisual(renderer.transform));
            }

            var modelRoot = visualRoot.Find(ModelInstanceName);
            return modelRoot != null
                ? modelRoot.GetComponentsInChildren<Renderer>(includeInactive: true)
                    .Where(renderer => renderer != null && !IsUnderPresentationVisual(renderer.transform))
                : Enumerable.Empty<Renderer>();
        }

        private static bool IsUnderPresentationVisual(Transform transform)
        {
            var cursor = transform;
            while (cursor != null)
            {
                if (cursor.GetComponent<PresentationVisualMarker>() != null)
                {
                    return true;
                }

                cursor = cursor.parent;
            }

            return false;
        }

        private static bool TryFindEquipmentMarker(Transform transform, out PresentationVisualMarker marker)
        {
            marker = null;
            var cursor = transform;
            while (cursor != null)
            {
                marker = cursor.GetComponent<PresentationVisualMarker>();
                if (marker != null)
                {
                    return marker.Role == PresentationPrefabRole.WeaponMelee ||
                        marker.Role == PresentationPrefabRole.WeaponRanged ||
                        marker.Role == PresentationPrefabRole.Armor;
                }

                cursor = cursor.parent;
            }

            return false;
        }

        private static void PopulateSkinnedBodyDiagnostics(
            Transform prefabRoot,
            Transform animatorTransform,
            Renderer[] bodyRenderers,
            PlayerVisualAssemblyValidationResult result)
        {
            var skinnedRenderers = bodyRenderers.OfType<SkinnedMeshRenderer>().ToArray();
            result.BodyVisibleSkinnedMesh = skinnedRenderers.Any(renderer =>
                renderer != null &&
                renderer.enabled &&
                renderer.sharedMesh != null &&
                renderer.sharedMaterials != null &&
                renderer.sharedMaterials.Length > 0 &&
                renderer.sharedMaterials.All(material => material != null));
            result.SkinnedBodyRootBoneAssigned = skinnedRenderers.Any(renderer => renderer != null && renderer.rootBone != null);
            result.SkinnedBodyBoneCount = skinnedRenderers.Sum(renderer => renderer != null && renderer.bones != null
                ? renderer.bones.Count(bone => bone != null)
                : 0);

            var firstSkinned = skinnedRenderers.FirstOrDefault(renderer => renderer != null);
            if (firstSkinned != null)
            {
                result.SkinnedBodyRootBonePath = firstSkinned.rootBone != null
                    ? TransformPath(prefabRoot, firstSkinned.rootBone)
                    : string.Empty;
                result.FirstSkinnedBodyBonePaths.AddRange((firstSkinned.bones ?? Array.Empty<Transform>())
                    .Where(bone => bone != null)
                    .Take(10)
                    .Select(bone => TransformPath(prefabRoot, bone)));
            }

            var hasValidHierarchy = animatorTransform != null &&
                skinnedRenderers.Length > 0 &&
                skinnedRenderers.All(renderer =>
                    renderer != null &&
                    renderer.rootBone != null &&
                    renderer.bones != null &&
                    renderer.bones.Length > 0 &&
                    IsDescendantOf(renderer.rootBone, animatorTransform) &&
                    renderer.bones.All(bone => bone != null && IsDescendantOf(bone, animatorTransform)));
            result.BodyWillDeformWithAnimator =
                result.BodyVisibleSkinnedMesh &&
                result.SkinnedBodyRootBoneAssigned &&
                result.SkinnedBodyBoneCount > 0 &&
                hasValidHierarchy;
        }

        private static void PopulateRendererDetails(
            Transform prefabRoot,
            Renderer[] bodyRenderers,
            PlayerVisualAssemblyValidationResult result)
        {
            Bounds? aggregateBounds = null;
            foreach (var renderer in bodyRenderers)
            {
                var detail = new PlayerVisualAssemblyRendererDetail(
                    TransformPath(prefabRoot, renderer.transform),
                    renderer.GetType().Name,
                    renderer.enabled,
                    renderer.gameObject.layer,
                    renderer.sharedMaterials.Where(material => material != null).Select(material => material.name).ToArray(),
                    renderer.sharedMaterials.Where(material => material != null).Select(material => material.shader != null ? material.shader.name : "<missing shader>").ToArray(),
                    renderer.sharedMaterials.Where(material => material != null).Select(AssetDatabase.GetAssetPath).ToArray(),
                    RendererBounds(renderer).size,
                    renderer.transform.localScale);
                result.BodyRendererDetails.Add(detail);

                if (renderer.enabled)
                {
                    result.EnabledBodyRendererCount++;
                }

                if (renderer.sharedMaterials.Length > 0 && renderer.sharedMaterials.All(material => material != null))
                {
                    result.BodyRenderersWithMaterialCount++;
                }

                if (renderer.sharedMaterials.Any(MaterialHasBaseTexture))
                {
                    result.BodyRenderersWithTextureCount++;
                }

                var bounds = RendererBounds(renderer);
                aggregateBounds = aggregateBounds.HasValue ? Encapsulate(aggregateBounds.Value, bounds) : bounds;
            }

            if (aggregateBounds.HasValue)
            {
                result.BodyBoundsCenter = aggregateBounds.Value.center;
                result.BodyBoundsSize = aggregateBounds.Value.size;
            }
        }

        private static void PopulateEquipmentDetails(
            Transform prefabRoot,
            Renderer[] equipmentRenderers,
            PlayerVisualAssemblyValidationResult result)
        {
            foreach (var renderer in equipmentRenderers)
            {
                if (!TryFindEquipmentMarker(renderer.transform, out var marker))
                {
                    continue;
                }

                var wrapper = marker.transform.parent != null ? marker.transform.parent : marker.transform;
                var socket = wrapper.parent;
                var bounds = RendererBounds(renderer);
                var valid = IsSaneEquipmentBounds(marker.Role, bounds.size);
                if (!valid)
                {
                    result.OversizedEquipmentCount++;
                }

                result.EquipmentRendererDetails.Add(new PlayerVisualAssemblyEquipmentDetail(
                    TransformPath(prefabRoot, renderer.transform),
                    TransformPath(prefabRoot, marker.transform),
                    marker.Role,
                    TransformPath(prefabRoot, wrapper),
                    TransformPath(prefabRoot, socket),
                    socket != null ? socket.lossyScale : Vector3.one,
                    wrapper.localScale,
                    wrapper.localScale,
                    bounds.size,
                    valid));
            }
        }

        private static void PopulateDuplicateEquipmentDiagnostics(
            Transform prefabRoot,
            PlayerVisualAssemblyValidationResult result)
        {
            var duplicateWrappers = KnownEquipmentWrappers(prefabRoot)
                .GroupBy(wrapper => wrapper.name)
                .Where(group => group.Count() > 1)
                .ToArray();
            result.DuplicateEquipmentWrapperCount = duplicateWrappers.Sum(group => group.Count() - 1);
            foreach (var group in duplicateWrappers)
            {
                result.Errors.Add(
                    $"Duplicate equipment wrapper {group.Key}: {string.Join(", ", group.Select(wrapper => TransformPath(prefabRoot, wrapper)))}");
            }

            var duplicateMarkers = prefabRoot.GetComponentsInChildren<PresentationVisualMarker>(includeInactive: true)
                .Where(IsEquipmentMarker)
                .GroupBy(marker => marker.Role)
                .Where(group => group.Count() > 1)
                .ToArray();
            result.DuplicateEquipmentMarkerCount = duplicateMarkers.Sum(group => group.Count() - 1);
            foreach (var group in duplicateMarkers)
            {
                result.Errors.Add(
                    $"Duplicate equipment ArtPass markers for {group.Key}: {string.Join(", ", group.Select(marker => TransformPath(prefabRoot, marker.transform)))}");
            }
        }

        private static void ValidateRigHierarchy(
            Transform prefabRoot,
            Transform animatorTransform,
            PlayerVisualAssemblyValidationResult result)
        {
            var rigBuilders = prefabRoot.GetComponentsInChildren<RigBuilder>(includeInactive: true);
            result.RigBuilderCount = rigBuilders.Length;
            result.RigBuilderEnabled = rigBuilders.Any(rigBuilder => rigBuilder != null && rigBuilder.enabled);
            if (animatorTransform == null)
            {
                return;
            }

            foreach (var rigBuilder in rigBuilders)
            {
                if (!IsDescendantOf(rigBuilder.transform, animatorTransform))
                {
                    result.Errors.Add($"RigBuilder is outside Animator hierarchy: {TransformPath(prefabRoot, rigBuilder.transform)}");
                }

                foreach (var layer in rigBuilder.layers)
                {
                    if (layer.rig == null)
                    {
                        continue;
                    }

                    if (!IsDescendantOf(layer.rig.transform, animatorTransform))
                    {
                        result.Errors.Add($"Rig layer is outside Animator hierarchy: {TransformPath(prefabRoot, layer.rig.transform)}");
                    }
                }
            }

            foreach (var constraint in prefabRoot.GetComponentsInChildren<TwoBoneIKConstraint>(includeInactive: true))
            {
                if (constraint != null && constraint.enabled)
                {
                    result.EnabledRigConstraintCount++;
                }

                ValidateConstraintTransform(prefabRoot, animatorTransform, constraint.transform, result);
                ValidateTwoBoneConstraint(prefabRoot, animatorTransform, constraint, result);
            }

            foreach (var constraint in prefabRoot.GetComponentsInChildren<MultiAimConstraint>(includeInactive: true))
            {
                if (constraint != null && constraint.enabled)
                {
                    result.EnabledRigConstraintCount++;
                    result.TorsoAimEnabled = true;
                }

                ValidateConstraintTransform(prefabRoot, animatorTransform, constraint.transform, result);
                ValidateMultiAimConstraint(prefabRoot, animatorTransform, constraint, result);
            }

            foreach (var constraint in prefabRoot.GetComponentsInChildren<MultiPositionConstraint>(includeInactive: true))
            {
                if (constraint != null && constraint.enabled)
                {
                    result.EnabledRigConstraintCount++;
                }

                ValidateConstraintTransform(prefabRoot, animatorTransform, constraint.transform, result);
                ValidateMultiPositionConstraint(prefabRoot, animatorTransform, constraint, result);
            }
        }

        private static void PopulateAnimationSystemDiagnostics(
            GameObject prefabRoot,
            PlayerVisualAssemblyValidationResult result)
        {
            var coordinator = prefabRoot.GetComponentInChildren<PlayerAnimationPoseCoordinator>(includeInactive: true);
            result.AnimationSystemMode = coordinator != null
                ? coordinator.AnimationSystemMode
                : MainCharacterAnimationIntegrator.DefaultAnimationSystemMode;
            result.FootPlacementEnabled = prefabRoot
                .GetComponentsInChildren<PlayerFootPlacementController>(includeInactive: true)
                .Any(component => component != null && component.enabled);
            result.HandIkEnabled = prefabRoot
                .GetComponentsInChildren<PlayerRangedHandPoseController>(includeInactive: true)
                .Any(component => component != null && component.enabled);
            result.ShieldIkEnabled = prefabRoot
                .GetComponentsInChildren<PlayerShieldGuardPoseController>(includeInactive: true)
                .Any(component => component != null && component.enabled);
            result.HandIkEnabled = result.HandIkEnabled || prefabRoot
                .GetComponentsInChildren<TwoBoneIKConstraint>(includeInactive: true)
                .Any(constraint => constraint != null &&
                    constraint.enabled &&
                    string.Equals(constraint.name, PlayerAnimationPoseCoordinator.RightHandWeaponIkConstraintName, StringComparison.Ordinal));
            result.ShieldIkEnabled = result.ShieldIkEnabled || prefabRoot
                .GetComponentsInChildren<TwoBoneIKConstraint>(includeInactive: true)
                .Any(constraint => constraint != null &&
                    constraint.enabled &&
                    string.Equals(constraint.name, PlayerAnimationPoseCoordinator.LeftHandShieldIkConstraintName, StringComparison.Ordinal));
            result.SimpleModeHasActiveRigInfluence =
                result.AnimationSystemMode == PlayerAnimationSystemMode.SimpleFullBodyAnimation &&
                (result.RigBuilderEnabled ||
                    result.FootPlacementEnabled ||
                    result.HandIkEnabled ||
                    result.ShieldIkEnabled ||
                    result.TorsoAimEnabled ||
                    result.EnabledRigConstraintCount > 0);

            var animator = prefabRoot.GetComponentInChildren<Animator>(includeInactive: true);
            if (animator != null && animator.runtimeAnimatorController is AnimatorController controller)
            {
                result.AnimatorLayerCount = controller.layers.Length;
                result.AnimatorBaseLayerIkPass = controller.layers.Length > 0 && controller.layers[0].iKPass;
            }
        }

        private static void ValidateConstraintTransform(
            Transform prefabRoot,
            Transform animatorTransform,
            Transform constraintTransform,
            PlayerVisualAssemblyValidationResult result)
        {
            if (!IsDescendantOf(constraintTransform, animatorTransform))
            {
                result.Errors.Add($"Rig constraint is outside Animator hierarchy: {TransformPath(prefabRoot, constraintTransform)}");
            }
        }

        private static void ValidateTwoBoneConstraint(
            Transform prefabRoot,
            Transform animatorTransform,
            TwoBoneIKConstraint constraint,
            PlayerVisualAssemblyValidationResult result)
        {
            var valid = constraint != null &&
                constraint.data.root != null &&
                constraint.data.mid != null &&
                constraint.data.tip != null &&
                constraint.data.target != null &&
                constraint.data.hint != null &&
                IsDescendantOf(constraint.data.root, animatorTransform) &&
                IsDescendantOf(constraint.data.mid, animatorTransform) &&
                IsDescendantOf(constraint.data.tip, animatorTransform) &&
                IsDescendantOf(constraint.data.target, animatorTransform) &&
                IsDescendantOf(constraint.data.hint, animatorTransform);
            RecordConstraintValidity(prefabRoot, constraint, valid, result);
        }

        private static void ValidateMultiAimConstraint(
            Transform prefabRoot,
            Transform animatorTransform,
            MultiAimConstraint constraint,
            PlayerVisualAssemblyValidationResult result)
        {
            var valid = constraint != null &&
                constraint.data.constrainedObject != null &&
                constraint.data.sourceObjects.Count > 0 &&
                constraint.data.sourceObjects[0].transform != null &&
                IsDescendantOf(constraint.data.constrainedObject, animatorTransform) &&
                IsDescendantOf(constraint.data.sourceObjects[0].transform, animatorTransform);
            RecordConstraintValidity(prefabRoot, constraint, valid, result);
        }

        private static void ValidateMultiPositionConstraint(
            Transform prefabRoot,
            Transform animatorTransform,
            MultiPositionConstraint constraint,
            PlayerVisualAssemblyValidationResult result)
        {
            var valid = constraint != null &&
                constraint.data.constrainedObject != null &&
                constraint.data.sourceObjects.Count > 0 &&
                constraint.data.sourceObjects[0].transform != null &&
                IsDescendantOf(constraint.data.constrainedObject, animatorTransform) &&
                IsDescendantOf(constraint.data.sourceObjects[0].transform, animatorTransform);
            RecordConstraintValidity(prefabRoot, constraint, valid, result);
        }

        private static void RecordConstraintValidity(
            Transform prefabRoot,
            Behaviour constraint,
            bool valid,
            PlayerVisualAssemblyValidationResult result)
        {
            if (valid)
            {
                return;
            }

            result.InvalidConstraintsCount++;
            var message = $"Animation Rigging constraint has missing/out-of-hierarchy data: {TransformPath(prefabRoot, constraint != null ? constraint.transform : null)}";
            if (constraint != null && constraint.enabled)
            {
                result.Errors.Add(message);
            }
            else
            {
                result.Warnings.Add(message);
            }
        }

        private static void ValidateMissingScriptsAndReferences(
            GameObject prefabRoot,
            PlayerVisualAssemblyValidationResult result)
        {
            result.MissingScriptCount = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(prefabRoot);
            if (result.MissingScriptCount > 0)
            {
                result.Errors.Add($"Player prefab has {result.MissingScriptCount} missing script component(s).");
            }

            foreach (var component in prefabRoot.GetComponentsInChildren<Component>(includeInactive: true))
            {
                if (component == null)
                {
                    result.MissingScriptCount++;
                    result.Errors.Add("Player prefab contains a null component slot, likely a missing script.");
                    continue;
                }

                using var serialized = new SerializedObject(component);
                var property = serialized.GetIterator();
                var enterChildren = true;
                while (property.NextVisible(enterChildren))
                {
                    enterChildren = false;
                    if (property.propertyType != SerializedPropertyType.ObjectReference)
                    {
                        continue;
                    }

                    if (property.objectReferenceValue == null && property.objectReferenceInstanceIDValue != 0)
                    {
                        result.MissingReferenceCount++;
                        result.Errors.Add(
                            $"Missing serialized reference on {TransformPath(prefabRoot.transform, component.transform)}::{component.GetType().Name}.{property.propertyPath}");
                    }
                }
            }
        }

        private static Bounds RendererBounds(Renderer renderer)
        {
            if (renderer is SkinnedMeshRenderer skinned && skinned.sharedMesh != null)
            {
                return ScaleBounds(skinned.sharedMesh.bounds, skinned.transform.lossyScale, skinned.transform.position);
            }

            var meshFilter = renderer.GetComponent<MeshFilter>();
            if (meshFilter != null && meshFilter.sharedMesh != null)
            {
                return ScaleBounds(meshFilter.sharedMesh.bounds, meshFilter.transform.lossyScale, meshFilter.transform.position);
            }

            return renderer.bounds;
        }

        private static Bounds ScaleBounds(Bounds localBounds, Vector3 scale, Vector3 position)
        {
            var size = localBounds.size;
            size.x *= Mathf.Abs(scale.x);
            size.y *= Mathf.Abs(scale.y);
            size.z *= Mathf.Abs(scale.z);
            return new Bounds(position + Vector3.Scale(localBounds.center, scale), size);
        }

        private static Bounds Encapsulate(Bounds current, Bounds next)
        {
            current.Encapsulate(next);
            return current;
        }

        private static bool IsSaneBodyBounds(Vector3 size)
        {
            return size.y >= MinimumBodyHeightMeters &&
                size.y <= MaximumBodyHeightMeters &&
                size.x >= MinimumBodyWidthMeters &&
                size.z >= MinimumBodyDepthMeters;
        }

        private static bool MaterialHasBaseTexture(Material material)
        {
            if (material == null)
            {
                return false;
            }

            return (material.HasProperty("_BaseMap") && material.GetTexture("_BaseMap") != null) ||
                (material.HasProperty("_MainTex") && material.GetTexture("_MainTex") != null);
        }

        private static bool IsSaneEquipmentBounds(PresentationPrefabRole role, Vector3 size)
        {
            var maxDimension = MaxDimension(size);
            if (maxDimension < MinimumEquipmentMaxDimensionMeters ||
                maxDimension > MaximumEquipmentMaxDimensionMeters)
            {
                return false;
            }

            return role switch
            {
                PresentationPrefabRole.Armor => maxDimension <= MaximumShieldMaxDimensionMeters,
                PresentationPrefabRole.WeaponMelee => maxDimension <= MaximumMeleeMaxDimensionMeters,
                PresentationPrefabRole.WeaponRanged => maxDimension <= MaximumRangedMaxDimensionMeters,
                _ => true
            };
        }

        private static IEnumerable<Transform> KnownEquipmentWrappers(Transform root)
        {
            return root != null
                ? root.GetComponentsInChildren<Transform>(includeInactive: true)
                    .Where(transform =>
                        transform != null &&
                        (transform.name == PlayerHeldWeaponVisualController.ActiveMeleeWeaponVisualName ||
                            transform.name == PlayerHeldWeaponVisualController.ActiveRangedWeaponVisualName ||
                            transform.name == PlayerHeldWeaponVisualController.HolsteredMeleeWeaponVisualName ||
                            transform.name == PlayerHeldWeaponVisualController.HolsteredRangedWeaponVisualName ||
                            transform.name == PlayerHeldWeaponVisualController.EquippedShieldVisualName))
                : Enumerable.Empty<Transform>();
        }

        private static bool IsEquipmentMarker(PresentationVisualMarker marker)
        {
            return marker != null &&
                (marker.Role == PresentationPrefabRole.WeaponMelee ||
                    marker.Role == PresentationPrefabRole.WeaponRanged ||
                    marker.Role == PresentationPrefabRole.Armor);
        }

        private static bool IsUnderKnownEquipmentWrapper(Transform transform)
        {
            var cursor = transform;
            while (cursor != null)
            {
                if (cursor.name == PlayerHeldWeaponVisualController.ActiveMeleeWeaponVisualName ||
                    cursor.name == PlayerHeldWeaponVisualController.ActiveRangedWeaponVisualName ||
                    cursor.name == PlayerHeldWeaponVisualController.HolsteredMeleeWeaponVisualName ||
                    cursor.name == PlayerHeldWeaponVisualController.HolsteredRangedWeaponVisualName ||
                    cursor.name == PlayerHeldWeaponVisualController.EquippedShieldVisualName)
                {
                    return true;
                }

                cursor = cursor.parent;
            }

            return false;
        }

        private static void AppendEquipmentWrapperReport(
            StringBuilder builder,
            Transform prefabRoot,
            Transform wrapper,
            PlayerAnimationProfileController profile,
            PlayerHeldWeaponVisualController heldVisual)
        {
            var marker = wrapper.GetComponentsInChildren<PresentationVisualMarker>(includeInactive: true)
                .FirstOrDefault(IsEquipmentMarker);
            var role = marker != null ? marker.Role.ToString() : "Unknown";
            builder.AppendLine($"- role: {RoleLabelForWrapper(wrapper, marker)}");
            builder.AppendLine($"  instancePath: {TransformPath(prefabRoot, wrapper)}");
            builder.AppendLine($"  sourcePrefabOrModelPath: {SourcePathForEquipment(marker, wrapper.gameObject)}");
            builder.AppendLine($"  rootLocalScale: {FormatVector(wrapper.localScale)}");
            builder.AppendLine($"  rootLossyScale: {FormatVector(wrapper.lossyScale)}");
            builder.AppendLine($"  parentPath: {TransformPath(prefabRoot, wrapper.parent)}");
            builder.AppendLine($"  parentLocalScale: {FormatVector(wrapper.parent != null ? wrapper.parent.localScale : Vector3.one)}");
            builder.AppendLine($"  parentLossyScale: {FormatVector(wrapper.parent != null ? wrapper.parent.lossyScale : Vector3.one)}");
            builder.AppendLine($"  socketPath: {TransformPath(prefabRoot, wrapper.parent)}");
            builder.AppendLine($"  socketLocalScale: {FormatVector(wrapper.parent != null ? wrapper.parent.localScale : Vector3.one)}");
            builder.AppendLine($"  socketLossyScale: {FormatVector(wrapper.parent != null ? wrapper.parent.lossyScale : Vector3.one)}");
            builder.AppendLine($"  markerRole: {role}");
            AppendBoundsReport(builder, prefabRoot, wrapper.gameObject, marker != null ? marker.Role : PresentationPrefabRole.Player);
            builder.AppendLine($"  currentProfile: {(profile != null ? profile.CurrentProfileId.ToString() : "<none>")}");
            builder.AppendLine($"  normalizationPassCount: {(heldVisual != null ? heldVisual.EquipmentNormalizationPassCount : 0)}");
            builder.AppendLine($"  visualWasSpawnedAfterNormalize: {(heldVisual != null && heldVisual.EquipmentNormalizationPassCount > 0)}");
            builder.AppendLine($"  staleOrDuplicate: {KnownEquipmentWrappers(prefabRoot).Count(candidate => candidate.name == wrapper.name) > 1}");
        }

        private static void AppendLooseEquipmentMarkerReport(
            StringBuilder builder,
            Transform prefabRoot,
            PresentationVisualMarker marker,
            PlayerAnimationProfileController profile,
            PlayerHeldWeaponVisualController heldVisual)
        {
            builder.AppendLine($"- role: Unknown");
            builder.AppendLine($"  instancePath: {TransformPath(prefabRoot, marker.transform)}");
            builder.AppendLine($"  sourcePrefabOrModelPath: {SourcePathForEquipment(marker, marker.gameObject)}");
            builder.AppendLine($"  rootLocalScale: {FormatVector(marker.transform.localScale)}");
            builder.AppendLine($"  rootLossyScale: {FormatVector(marker.transform.lossyScale)}");
            builder.AppendLine($"  parentPath: {TransformPath(prefabRoot, marker.transform.parent)}");
            builder.AppendLine($"  parentLocalScale: {FormatVector(marker.transform.parent != null ? marker.transform.parent.localScale : Vector3.one)}");
            builder.AppendLine($"  parentLossyScale: {FormatVector(marker.transform.parent != null ? marker.transform.parent.lossyScale : Vector3.one)}");
            builder.AppendLine($"  markerRole: {marker.Role}");
            AppendBoundsReport(builder, prefabRoot, marker.gameObject, marker.Role);
            builder.AppendLine($"  currentProfile: {(profile != null ? profile.CurrentProfileId.ToString() : "<none>")}");
            builder.AppendLine($"  normalizationPassCount: {(heldVisual != null ? heldVisual.EquipmentNormalizationPassCount : 0)}");
            builder.AppendLine("  staleOrDuplicate: true");
        }

        private static void AppendBoundsReport(
            StringBuilder builder,
            Transform prefabRoot,
            GameObject root,
            PresentationPrefabRole role)
        {
            var renderers = root != null
                ? root.GetComponentsInChildren<Renderer>(includeInactive: true)
                    .Where(renderer => renderer != null && renderer.enabled)
                    .ToArray()
                : Array.Empty<Renderer>();
            if (renderers.Length == 0)
            {
                builder.AppendLine("  rendererBoundsSize: <none>");
                builder.AppendLine("  rendererBoundsCenterDistanceFromPlayerRoot: <none>");
                builder.AppendLine("  boundsValid: false");
                return;
            }

            var bounds = RendererBounds(renderers[0]);
            for (var index = 1; index < renderers.Length; index++)
            {
                bounds.Encapsulate(RendererBounds(renderers[index]));
            }

            builder.AppendLine($"  rendererBoundsSize: {FormatVector(bounds.size)}");
            builder.AppendLine($"  rendererBoundsCenterDistanceFromPlayerRoot: {Vector3.Distance(prefabRoot.position, bounds.center):0.###}");
            builder.AppendLine($"  boundsValid: {IsSaneEquipmentBounds(role, bounds.size)}");
            foreach (var renderer in renderers)
            {
                builder.AppendLine($"  renderer: {TransformPath(prefabRoot, renderer.transform)} bounds={FormatVector(RendererBounds(renderer).size)}");
            }
        }

        private static string RoleLabelForWrapper(Transform wrapper, PresentationVisualMarker marker)
        {
            return wrapper.name switch
            {
                PlayerHeldWeaponVisualController.ActiveMeleeWeaponVisualName => "HeldWeapon",
                PlayerHeldWeaponVisualController.ActiveRangedWeaponVisualName => "HeldWeapon",
                PlayerHeldWeaponVisualController.HolsteredMeleeWeaponVisualName => "BackWeapon",
                PlayerHeldWeaponVisualController.HolsteredRangedWeaponVisualName => "BackWeapon",
                PlayerHeldWeaponVisualController.EquippedShieldVisualName when wrapper.parent != null &&
                    wrapper.parent.name == PlayerHeldWeaponVisualController.ShieldForearmSocketName => "HeldShield",
                PlayerHeldWeaponVisualController.EquippedShieldVisualName => "BackShield",
                _ => marker != null ? marker.Role.ToString() : "Unknown"
            };
        }

        private static bool IsLikelyUnnormalizedInheritedScale(Transform wrapper)
        {
            return wrapper != null &&
                wrapper.parent != null &&
                MaxDimension(wrapper.parent.lossyScale) > 10f &&
                MaxDimension(wrapper.localScale) > 0.1f;
        }

        private static bool HasInvalidEquipmentBounds(Transform wrapper)
        {
            if (wrapper == null)
            {
                return false;
            }

            var marker = wrapper.GetComponentsInChildren<PresentationVisualMarker>(includeInactive: true)
                .FirstOrDefault(IsEquipmentMarker);
            if (marker == null)
            {
                return false;
            }

            var renderers = wrapper.GetComponentsInChildren<Renderer>(includeInactive: true)
                .Where(renderer => renderer != null && renderer.enabled)
                .ToArray();
            if (renderers.Length == 0)
            {
                return true;
            }

            var bounds = RendererBounds(renderers[0]);
            for (var index = 1; index < renderers.Length; index++)
            {
                bounds.Encapsulate(RendererBounds(renderers[index]));
            }

            return !IsSaneEquipmentBounds(marker.Role, bounds.size);
        }

        private static bool HasOversizedSourcePrefab(PresentationVisualMarker marker, out string sourcePath)
        {
            sourcePath = SourcePathForEquipment(marker, marker != null ? marker.gameObject : null);
            if (string.IsNullOrWhiteSpace(sourcePath))
            {
                return false;
            }

            var source = AssetDatabase.LoadAssetAtPath<GameObject>(sourcePath);
            if (source == null)
            {
                return false;
            }

            var renderers = source.GetComponentsInChildren<Renderer>(includeInactive: true);
            if (renderers.Length == 0)
            {
                return false;
            }

            var bounds = RendererBounds(renderers[0]);
            for (var index = 1; index < renderers.Length; index++)
            {
                bounds.Encapsulate(RendererBounds(renderers[index]));
            }

            return MaxDimension(bounds.size) > MaximumEquipmentMaxDimensionMeters;
        }

        private static string SourcePathFor(GameObject instance)
        {
            if (instance == null)
            {
                return string.Empty;
            }

            var source = PrefabUtility.GetCorrespondingObjectFromOriginalSource(instance) ??
                PrefabUtility.GetCorrespondingObjectFromSource(instance);
            var path = source != null ? AssetDatabase.GetAssetPath(source) : string.Empty;
            if (!string.IsNullOrWhiteSpace(path))
            {
                return path;
            }

            var renderer = instance.GetComponentInChildren<Renderer>(includeInactive: true);
            if (renderer is SkinnedMeshRenderer skinned && skinned.sharedMesh != null)
            {
                return AssetDatabase.GetAssetPath(skinned.sharedMesh);
            }

            var meshFilter = instance.GetComponentInChildren<MeshFilter>(includeInactive: true);
            return meshFilter != null && meshFilter.sharedMesh != null
                ? AssetDatabase.GetAssetPath(meshFilter.sharedMesh)
                : string.Empty;
        }

        private static string SourcePathForEquipment(PresentationVisualMarker marker, GameObject instance)
        {
            var sourcePath = SourcePathFor(instance);
            if (!string.IsNullOrWhiteSpace(sourcePath) &&
                !string.Equals(sourcePath, PlayerPrefabPath, StringComparison.Ordinal))
            {
                return sourcePath;
            }

            var rolePath = marker != null ? ArtPassPrefabPathForRole(marker.Role) : string.Empty;
            return !string.IsNullOrWhiteSpace(rolePath) ? rolePath : sourcePath;
        }

        private static string ArtPassPrefabPathForRole(PresentationPrefabRole role)
        {
            return role switch
            {
                PresentationPrefabRole.WeaponMelee => "Assets/_Hollow/Prefabs/ArtPass/AP_WeaponMelee.prefab",
                PresentationPrefabRole.WeaponRanged => "Assets/_Hollow/Prefabs/ArtPass/AP_WeaponRanged.prefab",
                PresentationPrefabRole.Armor => "Assets/_Hollow/Prefabs/ArtPass/AP_Armor.prefab",
                _ => string.Empty
            };
        }

        private static void AppendScaleLine(StringBuilder builder, string label, Transform transform)
        {
            builder.AppendLine(
                $"{label}: path={(transform != null ? TransformPath(transform.root, transform) : "<missing>")} localScale={FormatVector(transform != null ? transform.localScale : Vector3.zero)} lossyScale={FormatVector(transform != null ? transform.lossyScale : Vector3.zero)}");
        }

        private static Transform FindDescendantNormalized(Transform root, string name)
        {
            if (root == null)
            {
                return null;
            }

            foreach (var child in root.GetComponentsInChildren<Transform>(includeInactive: true))
            {
                var normalized = child.name;
                var separator = normalized.LastIndexOf(':');
                if (separator >= 0 && separator < normalized.Length - 1)
                {
                    normalized = normalized[(separator + 1)..];
                }

                if (normalized == name || (name == "Spine02" && normalized == "Spine2"))
                {
                    return child;
                }
            }

            return null;
        }

        private static float MaxDimension(Vector3 size)
        {
            return Mathf.Max(Mathf.Abs(size.x), Mathf.Max(Mathf.Abs(size.y), Mathf.Abs(size.z)));
        }

        private static Transform FindDescendant(Transform root, string name)
        {
            if (root == null)
            {
                return null;
            }

            if (root.name == name)
            {
                return root;
            }

            foreach (Transform child in root)
            {
                var match = FindDescendant(child, name);
                if (match != null)
                {
                    return match;
                }
            }

            return null;
        }

        private static bool IsDescendantOf(Transform child, Transform ancestor)
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

        private static string FormatVector(Vector3 value)
        {
            return $"{value.x:0.###}, {value.y:0.###}, {value.z:0.###}";
        }
    }

    public sealed class PlayerVisualAssemblyValidationResult
    {
        public PlayerVisualAssemblyValidationResult(string prefabPath)
        {
            PrefabPath = prefabPath;
        }

        public string PrefabPath { get; }
        public bool AnimatorAvatarAssigned { get; set; }
        public bool AnimatorControllerAssigned { get; set; }
        public string AnimatorPath { get; set; } = string.Empty;
        public bool BodyVisibleForDebug { get; set; }
        public bool BodySkinnedAndAnimationReady { get; set; }
        public bool BodyWillDeformWithAnimator { get; set; }
        public bool BodyVisibleSkinnedMesh { get; set; }
        public bool BodyUsesStaticFallback { get; set; }
        public PlayerAnimationSystemMode AnimationSystemMode { get; set; } = PlayerAnimationSystemMode.AdvancedLayeredAnimation;
        public int AnimatorLayerCount { get; set; }
        public bool AnimatorBaseLayerIkPass { get; set; }
        public bool RigBuilderEnabled { get; set; }
        public bool FootPlacementEnabled { get; set; }
        public bool HandIkEnabled { get; set; }
        public bool ShieldIkEnabled { get; set; }
        public bool TorsoAimEnabled { get; set; }
        public bool SimpleModeHasActiveRigInfluence { get; set; }
        public bool UsesTemporaryStaticFallback { get; set; }
        public bool BodyUnderAnimatorHierarchy { get; set; }
        public bool SkinnedBodyRootBoneAssigned { get; set; }
        public int SkinnedBodyBoneCount { get; set; }
        public string SelectedSkinnedBodyFbx { get; set; } = string.Empty;
        public string SelectedAvatarSource { get; set; } = string.Empty;
        public string SkinnedBodyRootBonePath { get; set; } = string.Empty;
        public int BodyRendererCount { get; set; }
        public int EnabledBodyRendererCount { get; set; }
        public int BodyRenderersWithMaterialCount { get; set; }
        public int BodyRenderersWithTextureCount { get; set; }
        public int SkinnedBodyRendererCount { get; set; }
        public int MeshBodyRendererCount { get; set; }
        public int WeaponRendererCount { get; set; }
        public int ShieldRendererCount { get; set; }
        public bool EquipmentVisualScaleValid { get; set; }
        public int OversizedEquipmentCount { get; set; }
        public int DuplicateEquipmentWrapperCount { get; set; }
        public int DuplicateEquipmentMarkerCount { get; set; }
        public int RigBuilderCount { get; set; }
        public int EnabledRigConstraintCount { get; set; }
        public int InvalidConstraintsCount { get; set; }
        public int MissingReferenceCount { get; set; }
        public int MissingScriptCount { get; set; }
        public Vector3 BodyBoundsCenter { get; set; }
        public Vector3 BodyBoundsSize { get; set; }
        public List<PlayerVisualAssemblyRendererDetail> BodyRendererDetails { get; } = new();
        public List<PlayerVisualAssemblyEquipmentDetail> EquipmentRendererDetails { get; } = new();
        public List<string> FirstSkinnedBodyBonePaths { get; } = new();
        public List<string> Errors { get; } = new();
        public List<string> Warnings { get; } = new();
        public bool HasErrors => Errors.Count > 0;

        public string ToReportString()
        {
            var builder = new StringBuilder();
            builder.AppendLine("Player Visual Assembly Validation");
            builder.AppendLine($"Prefab: {PrefabPath}");
            builder.AppendLine($"BODY_VISIBLE_FOR_DEBUG = {BodyVisibleForDebug}");
            builder.AppendLine($"BODY_SKINNED_AND_ANIMATION_READY = {BodySkinnedAndAnimationReady}");
            builder.AppendLine($"BODY_WILL_DEFORM_WITH_ANIMATOR = {BodyWillDeformWithAnimator}");
            builder.AppendLine($"BODY_VISIBLE_SKINNED_MESH = {BodyVisibleSkinnedMesh}");
            builder.AppendLine($"BODY_USES_STATIC_FALLBACK = {BodyUsesStaticFallback}");
            builder.AppendLine($"PlayerAnimationSystemMode: {AnimationSystemMode}");
            builder.AppendLine($"SELECTED_SKINNED_BODY_FBX = {(string.IsNullOrWhiteSpace(SelectedSkinnedBodyFbx) ? "<none>" : SelectedSkinnedBodyFbx)}");
            builder.AppendLine($"SELECTED_AVATAR_SOURCE = {SelectedAvatarSource}");
            builder.AppendLine($"Animator Avatar: {(AnimatorAvatarAssigned ? "assigned" : "missing")}");
            builder.AppendLine($"Animator Controller: {(AnimatorControllerAssigned ? "assigned" : "missing")}");
            builder.AppendLine($"Animator Layers: {AnimatorLayerCount}");
            builder.AppendLine($"Animator Base Layer IK Pass: {AnimatorBaseLayerIkPass}");
            builder.AppendLine($"Animator Path: {AnimatorPath}");
            builder.AppendLine($"Body Renderers: {BodyRendererCount} enabled {EnabledBodyRendererCount} skinned {SkinnedBodyRendererCount} mesh {MeshBodyRendererCount}");
            builder.AppendLine($"Body Textured Renderers: {BodyRenderersWithTextureCount}");
            builder.AppendLine($"Skinned RootBone: {(SkinnedBodyRootBoneAssigned ? SkinnedBodyRootBonePath : "<missing>")}");
            builder.AppendLine($"Skinned Bone Count: {SkinnedBodyBoneCount}");
            builder.AppendLine($"Weapon Renderers: {WeaponRendererCount}");
            builder.AppendLine($"Shield Renderers: {ShieldRendererCount}");
            builder.AppendLine($"Equipment Visual Scale Valid: {EquipmentVisualScaleValid}");
            builder.AppendLine($"Oversized Equipment Count: {OversizedEquipmentCount}");
            builder.AppendLine($"Duplicate Equipment Wrappers: {DuplicateEquipmentWrapperCount}");
            builder.AppendLine($"Duplicate Equipment Markers: {DuplicateEquipmentMarkerCount}");
            builder.AppendLine($"Body Under Animator Hierarchy: {BodyUnderAnimatorHierarchy}");
            builder.AppendLine($"Body Bounds Center: {FormatVector(BodyBoundsCenter)}");
            builder.AppendLine($"Body Bounds Size: {FormatVector(BodyBoundsSize)}");
            builder.AppendLine($"RigBuilders: {RigBuilderCount}");
            builder.AppendLine($"RigBuilderEnabled: {RigBuilderEnabled}");
            builder.AppendLine($"FootPlacementEnabled: {FootPlacementEnabled}");
            builder.AppendLine($"HandIkEnabled: {HandIkEnabled}");
            builder.AppendLine($"ShieldIkEnabled: {ShieldIkEnabled}");
            builder.AppendLine($"TorsoAimEnabled: {TorsoAimEnabled}");
            builder.AppendLine($"Enabled Rig Constraints: {EnabledRigConstraintCount}");
            builder.AppendLine($"SimpleModeHasActiveRigInfluence: {SimpleModeHasActiveRigInfluence}");
            builder.AppendLine($"Invalid Constraints: {InvalidConstraintsCount}");
            builder.AppendLine($"Missing References: {MissingReferenceCount}");
            builder.AppendLine($"Missing Scripts: {MissingScriptCount}");
            if (UsesTemporaryStaticFallback)
            {
                builder.AppendLine(PlayerVisualAssemblyValidator.TemporaryStaticBodyFallbackLabel);
                builder.AppendLine($"Mesh Path: {PlayerAnimationProfileAssetGenerator.HollowMainModelObjPath}");
                builder.AppendLine("Skinned: false");
                builder.AppendLine(PlayerVisualAssemblyValidator.FinalBodyReplacementRequirement);
            }

            foreach (var detail in BodyRendererDetails)
            {
                builder.AppendLine(
                    $"- {detail.Path} [{detail.RendererType}] enabled={detail.Enabled} layer={detail.Layer} bounds={FormatVector(detail.BoundsSize)} scale={FormatVector(detail.LocalScale)}");
                builder.AppendLine($"  Materials: {string.Join(", ", detail.MaterialNames)}");
                builder.AppendLine($"  Shaders: {string.Join(", ", detail.ShaderNames)}");
                builder.AppendLine($"  Material Paths: {string.Join(", ", detail.MaterialPaths)}");
            }

            foreach (var detail in EquipmentRendererDetails)
            {
                builder.AppendLine(
                    $"- Equipment {detail.Role} renderer={detail.RendererPath} artpass={detail.MarkerPath} wrapper={detail.WrapperPath} socket={detail.ParentSocketPath}");
                builder.AppendLine(
                    $"  parentLossyScale={FormatVector(detail.ParentLossyScale)} wrapperLocalScale={FormatVector(detail.WrapperLocalScale)} compensationScale={FormatVector(detail.CompensationScale)} bounds={FormatVector(detail.BoundsSize)} valid={detail.BoundsValid}");
            }

            if (FirstSkinnedBodyBonePaths.Count > 0)
            {
                builder.AppendLine("First Skinned Body Bones:");
                foreach (var path in FirstSkinnedBodyBonePaths)
                {
                    builder.AppendLine($"- {path}");
                }
            }

            foreach (var warning in Warnings)
            {
                builder.AppendLine($"WARNING: {warning}");
            }

            foreach (var error in Errors)
            {
                builder.AppendLine($"ERROR: {error}");
            }

            return builder.ToString();
        }

        private static string FormatVector(Vector3 value)
        {
            return $"{value.x:0.###}, {value.y:0.###}, {value.z:0.###}";
        }
    }

    public sealed class PlayerVisualAssemblyRendererDetail
    {
        public PlayerVisualAssemblyRendererDetail(
            string path,
            string rendererType,
            bool enabled,
            int layer,
            string[] materialNames,
            string[] shaderNames,
            string[] materialPaths,
            Vector3 boundsSize,
            Vector3 localScale)
        {
            Path = path;
            RendererType = rendererType;
            Enabled = enabled;
            Layer = layer;
            MaterialNames = materialNames;
            ShaderNames = shaderNames;
            MaterialPaths = materialPaths;
            BoundsSize = boundsSize;
            LocalScale = localScale;
        }

        public string Path { get; }
        public string RendererType { get; }
        public bool Enabled { get; }
        public int Layer { get; }
        public string[] MaterialNames { get; }
        public string[] ShaderNames { get; }
        public string[] MaterialPaths { get; }
        public Vector3 BoundsSize { get; }
        public Vector3 LocalScale { get; }
    }

    public sealed class PlayerVisualAssemblyEquipmentDetail
    {
        public PlayerVisualAssemblyEquipmentDetail(
            string rendererPath,
            string markerPath,
            PresentationPrefabRole role,
            string wrapperPath,
            string parentSocketPath,
            Vector3 parentLossyScale,
            Vector3 wrapperLocalScale,
            Vector3 compensationScale,
            Vector3 boundsSize,
            bool boundsValid)
        {
            RendererPath = rendererPath;
            MarkerPath = markerPath;
            Role = role;
            WrapperPath = wrapperPath;
            ParentSocketPath = parentSocketPath;
            ParentLossyScale = parentLossyScale;
            WrapperLocalScale = wrapperLocalScale;
            CompensationScale = compensationScale;
            BoundsSize = boundsSize;
            BoundsValid = boundsValid;
        }

        public string RendererPath { get; }
        public string MarkerPath { get; }
        public PresentationPrefabRole Role { get; }
        public string WrapperPath { get; }
        public string ParentSocketPath { get; }
        public Vector3 ParentLossyScale { get; }
        public Vector3 WrapperLocalScale { get; }
        public Vector3 CompensationScale { get; }
        public Vector3 BoundsSize { get; }
        public bool BoundsValid { get; }
    }
}
