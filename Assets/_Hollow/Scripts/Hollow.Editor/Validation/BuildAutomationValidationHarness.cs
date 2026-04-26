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
    public static class BuildAutomationValidationHarness
    {
        public static ContentValidationReport ValidateAll(BuildAutomationProfileDefinition profile, bool runAudit)
        {
            var report = new ContentValidationReport();
            ValidateProfile(profile, report);
            ValidateAddressables(report);
            ValidateOutputConventions(profile, report);
            ValidateBuildScenes(profile, report);
            ValidateVisionOSPlaceholders(profile, report);
            if (runAudit)
            {
                ValidatePrototypeAudit(profile, report);
            }

            return report;
        }

        private static void ValidateProfile(BuildAutomationProfileDefinition profile, ContentValidationReport report)
        {
            if (profile == null)
            {
                report.AddFailure("Build automation profile is missing.");
                return;
            }

            if (profile.RequiredScenes.Length != Milestone11AssetGenerator.RequiredBuildScenes.Length)
            {
                report.AddFailure("Build automation profile must list all prototype build scenes.");
            }

            if (profile.ValidationTypes.Length != Milestone12AssetGenerator.ValidationTypes.Length)
            {
                report.AddFailure("Build automation profile must list M0-M11 validators.");
            }

            foreach (var validatorType in Milestone12AssetGenerator.ValidationTypes)
            {
                if (!profile.ValidationTypes.Contains(validatorType))
                {
                    report.AddFailure($"Build automation profile is missing validator: {validatorType}");
                }
            }
        }

        private static void ValidateAddressables(ContentValidationReport report)
        {
            var settings = AddressableAssetSettingsDefaultObject.GetSettings(create: false);
            if (settings == null)
            {
                report.AddFailure("M12 requires Addressables settings.");
                return;
            }

            if (!settings.GetLabels().Contains(Milestone12AssetGenerator.BuildAutomationAddressableLabel))
            {
                report.AddFailure($"Missing Addressables label {Milestone12AssetGenerator.BuildAutomationAddressableLabel}.");
            }

            var guid = AssetDatabase.AssetPathToGUID(Milestone12AssetGenerator.BuildAutomationProfilePath);
            var entry = string.IsNullOrWhiteSpace(guid) ? null : settings.FindAssetEntry(guid, includeImplicit: false);
            if (entry == null || !entry.labels.Contains(Milestone12AssetGenerator.BuildAutomationAddressableLabel) || !entry.labels.Contains("hollow.data"))
            {
                report.AddFailure("Build automation profile must be addressable with build-automation and data labels.");
            }
        }

        private static void ValidateOutputConventions(BuildAutomationProfileDefinition profile, ContentValidationReport report)
        {
            if (profile == null)
            {
                return;
            }

            if (!Directory.Exists(profile.BuildRoot))
            {
                report.AddFailure($"Build output root is missing: {profile.BuildRoot}");
            }

            if (!Directory.Exists(profile.ReportRoot))
            {
                report.AddFailure($"Report output root is missing: {profile.ReportRoot}");
                return;
            }

            var latestAuditJson = Path.Combine(profile.ReportRoot, profile.LatestAuditJsonFileName);
            var latestAuditMarkdown = Path.Combine(profile.ReportRoot, profile.LatestAuditMarkdownFileName);
            var latestManifest = Path.Combine(profile.ReportRoot, profile.LatestBuildManifestFileName);
            if (!File.Exists(latestAuditJson))
            {
                report.AddFailure($"Latest prototype audit JSON is missing: {latestAuditJson}");
            }

            if (!File.Exists(latestAuditMarkdown))
            {
                report.AddFailure($"Latest prototype audit Markdown is missing: {latestAuditMarkdown}");
            }

            if (!File.Exists(latestManifest))
            {
                report.AddFailure($"Latest build manifest is missing: {latestManifest}");
            }
        }

        private static void ValidateBuildScenes(BuildAutomationProfileDefinition profile, ContentValidationReport report)
        {
            if (profile == null)
            {
                return;
            }

            var enabledScenes = EditorBuildSettings.scenes
                .Where(scene => scene.enabled)
                .Select(scene => scene.path)
                .ToHashSet();

            foreach (var scenePath in Milestone11AssetGenerator.RequiredBuildScenes)
            {
                if (!File.Exists(scenePath))
                {
                    report.AddFailure($"Required M12 build scene is missing: {scenePath}");
                }

                if (!profile.RequiredScenes.Contains(scenePath))
                {
                    report.AddFailure($"Build automation profile does not include scene: {scenePath}");
                }

                if (!enabledScenes.Contains(scenePath))
                {
                    report.AddFailure($"Editor build settings do not include enabled scene: {scenePath}");
                }
            }
        }

        private static void ValidateVisionOSPlaceholders(BuildAutomationProfileDefinition profile, ContentValidationReport report)
        {
            if (!HollowBuildAutomation.ValidateVisionOSProfilePlaceholders(profile, out var error))
            {
                report.AddFailure(error);
            }
        }

        private static void ValidatePrototypeAudit(BuildAutomationProfileDefinition profile, ContentValidationReport report)
        {
            if (profile == null)
            {
                return;
            }

            var audit = PrototypeAuditRunner.RunFullAudit(profile, writeReports: false);
            if (!audit.Passed)
            {
                foreach (var entry in audit.entries.Where(entry => !entry.passed))
                {
                    report.AddFailure($"Full prototype audit failed at {entry.id}: {string.Join("; ", entry.messages)}");
                }
            }
        }
    }
}
