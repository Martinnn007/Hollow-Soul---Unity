using System.Collections.Generic;
using System.IO;
using System.Linq;
using Hollow.Data.Definitions;
using Hollow.Editor.Build;
using Hollow.Editor.Generation;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEngine;

namespace Hollow.Editor.Validation
{
    public static class Milestone24Validator
    {
        private static readonly string[] RequiredFiles =
        {
            "Assets/_Hollow/Scripts/Hollow.Data/Definitions/PlatformBuildQaProfileDefinition.cs",
            "Assets/_Hollow/Scripts/Hollow.Diagnostics/PlatformRuntimeQaProbe.cs",
            "Assets/_Hollow/Scripts/Hollow.Editor/Build/PlatformBuildQaReport.cs",
            "Assets/_Hollow/Scripts/Hollow.Editor/Build/PlatformBuildQaRunner.cs",
            "Assets/_Hollow/Scripts/Hollow.Editor/Generation/Milestone24AssetGenerator.cs",
            "Assets/_Hollow/Scripts/Hollow.Editor/Validation/Milestone24Validator.cs",
            "Assets/_Hollow/Tests/EditMode/Milestone24PlatformBuildQaTests.cs",
            "Assets/_Hollow/Tests/PlayMode/Milestone24PlatformSceneSmokePlayModeTests.cs",
            "Docs/Milestone24PlatformBuildDeviceQA.md",
            Milestone24AssetGenerator.PlatformBuildQaProfilePath
        };

        [MenuItem("Hollow/Validation/Run Milestone 24 Validation")]
        public static void ValidateFromMenu()
        {
            Validate(exitOnFailure: false);
        }

        public static void Validate()
        {
            Validate(exitOnFailure: Application.isBatchMode);
        }

        private static void Validate(bool exitOnFailure)
        {
            AssetDatabase.Refresh();
            var failures = new List<string>();
            foreach (var file in RequiredFiles)
            {
                if (!File.Exists(file))
                {
                    failures.Add($"Missing M24 file: {file}");
                }
            }

            var profile = AssetDatabase.LoadAssetAtPath<PlatformBuildQaProfileDefinition>(Milestone24AssetGenerator.PlatformBuildQaProfilePath);
            ValidateProfile(profile, failures);
            ValidateAddressables(profile, failures);
            ValidateReports(profile, failures);
            ValidateVisionOSStaticReadiness(profile, failures);

            if (failures.Count == 0)
            {
                Debug.Log("Hollow Milestone 24 validation passed.");
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

        private static void ValidateProfile(PlatformBuildQaProfileDefinition profile, List<string> failures)
        {
            if (profile == null)
            {
                failures.Add("M24 platform QA profile is missing.");
                return;
            }

            if (!profile.RequireAddressablesBuild)
            {
                failures.Add("M24 profile must require local Addressables build validation.");
            }

            if (!profile.AllowEnvironmentBlocks)
            {
                failures.Add("M24 profile must allow explicit environment-blocked results for missing platform modules/signing.");
            }

            if (profile.PerformanceBudget == null)
            {
                failures.Add("M24 profile must reference the M11 performance budget.");
            }

            if (profile.BuildAutomationProfile == null)
            {
                failures.Add("M24 profile must reference the M12 build automation profile.");
            }

            foreach (var scene in Milestone11AssetGenerator.RequiredBuildScenes)
            {
                if (!profile.RequiredScenes.Contains(scene))
                {
                    failures.Add($"M24 profile missing required scene: {scene}");
                }
            }

            foreach (var validatorType in Milestone24AssetGenerator.ValidationTypes)
            {
                if (!profile.ValidationTypes.Contains(validatorType))
                {
                    failures.Add($"M24 profile missing validation type: {validatorType}");
                }
            }

            if (profile.WindowsBuildFolderName != Milestone24AssetGenerator.WindowsM24BuildFolderName)
            {
                failures.Add("M24 profile must target the HollowSoul_M24_Windows output folder.");
            }
        }

        private static void ValidateAddressables(PlatformBuildQaProfileDefinition profile, List<string> failures)
        {
            var settings = AddressableAssetSettingsDefaultObject.GetSettings(create: false);
            if (settings == null)
            {
                failures.Add("M24 requires Addressables settings.");
                return;
            }

            if (!settings.GetLabels().Contains(Milestone24AssetGenerator.PlatformQaAddressableLabel))
            {
                failures.Add($"Missing Addressables label {Milestone24AssetGenerator.PlatformQaAddressableLabel}.");
            }

            if (profile == null)
            {
                return;
            }

            var guid = AssetDatabase.AssetPathToGUID(Milestone24AssetGenerator.PlatformBuildQaProfilePath);
            var entry = string.IsNullOrWhiteSpace(guid) ? null : settings.FindAssetEntry(guid, includeImplicit: false);
            if (entry == null || !entry.labels.Contains(Milestone24AssetGenerator.PlatformQaAddressableLabel) || !entry.labels.Contains("hollow.data"))
            {
                failures.Add("M24 profile must be Addressable with platform-qa and data labels.");
            }
        }

        private static void ValidateReports(PlatformBuildQaProfileDefinition profile, List<string> failures)
        {
            if (profile == null)
            {
                return;
            }

            if (!Directory.Exists(profile.BuildRoot))
            {
                failures.Add($"M24 build root is missing: {profile.BuildRoot}");
            }

            if (!Directory.Exists(profile.ReportRoot))
            {
                failures.Add($"M24 report root is missing: {profile.ReportRoot}");
                return;
            }

            var latestJson = Path.Combine(profile.ReportRoot, profile.LatestQaJsonFileName);
            var latestMarkdown = Path.Combine(profile.ReportRoot, profile.LatestQaMarkdownFileName);
            if (!File.Exists(latestJson))
            {
                failures.Add($"Latest M24 QA JSON report is missing: {latestJson}");
            }
            else
            {
                var report = JsonUtility.FromJson<PlatformBuildQaReport>(File.ReadAllText(latestJson));
                if (report == null || string.IsNullOrWhiteSpace(report.reportId))
                {
                    failures.Add("Latest M24 QA JSON report could not be decoded.");
                }
            }

            if (!File.Exists(latestMarkdown))
            {
                failures.Add($"Latest M24 QA Markdown report is missing: {latestMarkdown}");
            }
        }

        private static void ValidateVisionOSStaticReadiness(PlatformBuildQaProfileDefinition profile, List<string> failures)
        {
            if (profile == null)
            {
                return;
            }

            foreach (var scenePath in new[] { "Assets/_Hollow/Scenes/Game_VisionOS_Bounded.unity", "Assets/_Hollow/Scenes/Game_VisionOS_Immersive.unity" })
            {
                if (!File.Exists(scenePath))
                {
                    failures.Add($"Vision Pro scene is missing: {scenePath}");
                }

                if (!EditorBuildSettings.scenes.Any(scene => scene.enabled && scene.path == scenePath))
                {
                    failures.Add($"Vision Pro scene must be enabled in build settings: {scenePath}");
                }
            }

            if (!File.Exists(Milestone10AssetGenerator.BoundedProfilePath))
            {
                failures.Add($"Missing bounded Vision Pro polish profile: {Milestone10AssetGenerator.BoundedProfilePath}");
            }

            if (!File.Exists(Milestone10AssetGenerator.ImmersiveProfilePath))
            {
                failures.Add($"Missing immersive Vision Pro polish profile: {Milestone10AssetGenerator.ImmersiveProfilePath}");
            }
        }
    }
}
