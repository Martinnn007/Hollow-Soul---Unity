using System.Collections.Generic;
using System.IO;
using System.Linq;
using Hollow.Data.Definitions;
using Hollow.Editor.Generation;
using Hollow.Platform;
using Hollow.Presentation;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Hollow.Editor.Validation
{
    public static class Milestone10Validator
    {
        private static readonly string[] RequiredFiles =
        {
            "Assets/_Hollow/Scripts/Hollow.Data/Definitions/PlatformPolishProfileDefinition.cs",
            "Assets/_Hollow/Scripts/Hollow.Presentation/PlatformPolishApplier.cs",
            "Assets/_Hollow/Scripts/Hollow.Presentation/ComfortVignettePresenter.cs",
            "Assets/_Hollow/Scripts/Hollow.Presentation/PlatformPolishModeExtensions.cs",
            "Assets/_Hollow/Scripts/Hollow.Editor/Generation/Milestone10AssetGenerator.cs",
            "Assets/_Hollow/Scripts/Hollow.Editor/Validation/Milestone10Validator.cs",
            "Assets/_Hollow/Tests/EditMode/Milestone10PlatformPolishTests.cs",
            "Docs/Milestone10PlatformPolish.md",
            Milestone10AssetGenerator.WindowsProfilePath,
            Milestone10AssetGenerator.BoundedProfilePath,
            Milestone10AssetGenerator.ImmersiveProfilePath
        };

        private static readonly (string ScenePath, HollowPlatformKind PlatformKind, string ProfilePath)[] GameScenes =
        {
            ("Assets/_Hollow/Scenes/Game_Windows.unity", HollowPlatformKind.WindowsStandard3D, Milestone10AssetGenerator.WindowsProfilePath),
            ("Assets/_Hollow/Scenes/Game_VisionOS_Bounded.unity", HollowPlatformKind.VisionOSBoundedTabletop, Milestone10AssetGenerator.BoundedProfilePath),
            ("Assets/_Hollow/Scenes/Game_VisionOS_Immersive.unity", HollowPlatformKind.VisionOSImmersive, Milestone10AssetGenerator.ImmersiveProfilePath)
        };

        [MenuItem("Hollow/Validation/Run Milestone 10 Validation")]
        public static void ValidateFromMenu()
        {
            Validate(exitOnFailure: false);
        }

        public static void Validate()
        {
            Validate(exitOnFailure: MilestoneValidationExitPolicy.ShouldExitForValidate());
        }

        private static void Validate(bool exitOnFailure)
        {
            AssetDatabase.Refresh();
            var failures = new List<string>();
            foreach (var file in RequiredFiles)
            {
                if (!File.Exists(file))
                {
                    failures.Add($"Missing M10 file: {file}");
                }
            }

            ValidateProfiles(failures);
            ValidateAddressables(failures);
            foreach (var (scenePath, platformKind, profilePath) in GameScenes)
            {
                ValidateScene(scenePath, platformKind, profilePath, failures);
            }

            if (failures.Count == 0)
            {
                Debug.Log("Hollow Milestone 10 validation passed.");
                if (exitOnFailure)
                {
                    EditorApplication.Exit(0);
                }

                return;
            }

            foreach (var failure in failures)
            {
                Debug.LogError(failure);
            }

            if (exitOnFailure)
            {
                EditorApplication.Exit(1);
            }
        }

        private static void ValidateProfiles(List<string> failures)
        {
            AssertProfile(Milestone10AssetGenerator.WindowsProfilePath, PlatformPresentationMode.WindowsStandard3D, 1f, false, 120, failures);
            AssertProfile(Milestone10AssetGenerator.BoundedProfilePath, PlatformPresentationMode.VisionOSBoundedTabletop, PresentationScalePolicy.VisionOSBoundedTabletopScale, false, 90, failures);
            AssertProfile(Milestone10AssetGenerator.ImmersiveProfilePath, PlatformPresentationMode.VisionOSImmersive, 1f, true, 90, failures);
        }

        private static void AssertProfile(string path, PlatformPresentationMode expectedMode, float expectedScale, bool expectedVignette, int minimumTargetFrameRate, List<string> failures)
        {
            var profile = AssetDatabase.LoadAssetAtPath<PlatformPolishProfileDefinition>(path);
            if (profile == null)
            {
                failures.Add($"Could not load platform polish profile: {path}");
                return;
            }

            if (profile.Mode != expectedMode || Mathf.Abs(profile.WorldScale - expectedScale) > 0.0001f)
            {
                failures.Add($"{path} has invalid mode or world scale.");
            }

            if (profile.UseComfortVignette != expectedVignette)
            {
                failures.Add($"{path} has invalid comfort vignette setting.");
            }

            if (profile.TargetFrameRate < minimumTargetFrameRate || profile.VSyncCount != 0)
            {
                failures.Add($"{path} has invalid performance budget settings.");
            }

            if (profile.CameraFieldOfView < 40f || profile.CameraFieldOfView > 65f || profile.RenderScale < 0.75f || profile.RenderScale > 1.05f)
            {
                failures.Add($"{path} has invalid camera comfort or render-scale values.");
            }
        }

        private static void ValidateAddressables(List<string> failures)
        {
            var settings = AddressableAssetSettingsDefaultObject.GetSettings(create: false);
            if (settings == null)
            {
                failures.Add("M10 requires Addressables settings.");
                return;
            }

            if (!settings.GetLabels().Contains(Milestone10AssetGenerator.PlatformAddressableLabel))
            {
                failures.Add($"Missing Addressables label {Milestone10AssetGenerator.PlatformAddressableLabel}.");
            }

            AssertAddressable(settings, Milestone10AssetGenerator.WindowsProfilePath, failures);
            AssertAddressable(settings, Milestone10AssetGenerator.BoundedProfilePath, failures);
            AssertAddressable(settings, Milestone10AssetGenerator.ImmersiveProfilePath, failures);
        }

        private static void AssertAddressable(UnityEditor.AddressableAssets.Settings.AddressableAssetSettings settings, string path, List<string> failures)
        {
            var guid = AssetDatabase.AssetPathToGUID(path);
            var entry = string.IsNullOrWhiteSpace(guid) ? null : settings.FindAssetEntry(guid, includeImplicit: false);
            if (entry == null || !entry.labels.Contains(Milestone10AssetGenerator.PlatformAddressableLabel))
            {
                failures.Add($"Platform polish profile is not addressable with the platform label: {path}");
            }
        }

        private static void ValidateScene(string scenePath, HollowPlatformKind expectedPlatformKind, string profilePath, List<string> failures)
        {
            if (!File.Exists(scenePath))
            {
                failures.Add($"Missing M10 scene: {scenePath}");
                return;
            }

            var expectedProfile = AssetDatabase.LoadAssetAtPath<PlatformPolishProfileDefinition>(profilePath);
            EditorSceneManager.OpenScene(scenePath);
            var metadata = Object.FindAnyObjectByType<CameraRigMetadata>();
            var applier = Object.FindAnyObjectByType<PlatformPolishApplier>();
            var presentationRoot = Object.FindAnyObjectByType<PlatformPresentationRoot>();
            var camera = Camera.main != null ? Camera.main : Object.FindAnyObjectByType<Camera>();

            if (metadata == null || metadata.PlatformKind != expectedPlatformKind)
            {
                failures.Add($"{scenePath} must contain a camera rig for {expectedPlatformKind}.");
            }

            if (applier == null || applier.Profile != expectedProfile)
            {
                failures.Add($"{scenePath} must contain a PlatformPolishApplier assigned to the expected profile.");
            }

            if (presentationRoot == null || Mathf.Abs(presentationRoot.WorldScale - (expectedProfile != null ? expectedProfile.WorldScale : 1f)) > 0.0001f)
            {
                failures.Add($"{scenePath} must apply the M10 profile world scale.");
            }

            if (camera == null)
            {
                failures.Add($"{scenePath} must contain a MainCamera.");
            }
            else
            {
                var vignette = camera.GetComponent<ComfortVignettePresenter>();
                if (expectedProfile != null && (vignette == null || vignette.VignetteEnabled != expectedProfile.UseComfortVignette))
                {
                    failures.Add($"{scenePath} must configure comfort vignette from the platform polish profile.");
                }
            }

            var shellCanvas = GameObject.Find("PlatformShellCanvas");
            if (shellCanvas == null || presentationRoot != null && shellCanvas.transform.IsChildOf(presentationRoot.transform))
            {
                failures.Add($"{scenePath} must keep HUD/shell UI outside WorldPresentationRoot.");
            }
        }
    }
}
