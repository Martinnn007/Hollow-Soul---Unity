using System;
using System.IO;
using System.Linq;
using Hollow.Data.Definitions;
using Hollow.Editor.Generation;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Hollow.Editor.Build
{
    public static class HollowBuildAutomation
    {
        [MenuItem("Hollow/Validation/Run Full Prototype Audit")]
        public static void RunFullPrototypeAudit()
        {
            var profile = LoadProfileOrThrow();
            var report = PrototypeAuditRunner.RunFullAudit(profile, writeReports: true);
            BuildManifestWriter.WriteManifest(profile, report, "StandaloneWindows64", PlannedWindowsBuildPath(profile), "AuditOnly");
            if (report.Passed)
            {
                Debug.Log($"Hollow full prototype audit passed: {report.passedChecks}/{report.totalChecks} checks.");
            }
            else
            {
                Debug.LogError($"Hollow full prototype audit failed: {report.failedChecks}/{report.totalChecks} checks failed.");
            }
        }

        [MenuItem("Hollow/Build/Write Prototype Build Manifest")]
        public static void WritePrototypeBuildManifest()
        {
            var profile = LoadProfileOrThrow();
            var report = PrototypeAuditRunner.RunFullAudit(profile, writeReports: true);
            BuildManifestWriter.WriteManifest(profile, report, "StandaloneWindows64", PlannedWindowsBuildPath(profile), "ManifestOnly");
            Debug.Log("Wrote Hollow prototype build manifest.");
        }

        [MenuItem("Hollow/Build/Build Windows Development Prototype")]
        public static void BuildWindowsDevelopmentPrototype()
        {
            var profile = LoadProfileOrThrow();
            var report = PrototypeAuditRunner.RunFullAudit(profile, writeReports: true);
            if (profile.RequirePassingAuditBeforeBuild && !report.Passed)
            {
                BuildManifestWriter.WriteManifest(profile, report, "StandaloneWindows64", PlannedWindowsBuildPath(profile), "BlockedByAudit");
                throw new InvalidOperationException("Windows prototype build blocked because the full prototype audit failed.");
            }

            var outputPath = PlannedWindowsBuildPath(profile);
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
            var buildReport = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = profile.RequiredScenes,
                locationPathName = outputPath,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.Development
            });

            BuildManifestWriter.WriteManifest(profile, report, BuildTarget.StandaloneWindows64.ToString(), outputPath, buildReport.summary.result.ToString());
            if (buildReport.summary.result != BuildResult.Succeeded)
            {
                throw new InvalidOperationException($"Windows prototype build failed: {buildReport.summary.result}");
            }
        }

        public static BuildAutomationProfileDefinition LoadProfileOrThrow()
        {
            var profile = AssetDatabase.LoadAssetAtPath<BuildAutomationProfileDefinition>(Milestone12AssetGenerator.BuildAutomationProfilePath);
            if (profile == null)
            {
                throw new FileNotFoundException($"Missing M12 build automation profile at {Milestone12AssetGenerator.BuildAutomationProfilePath}. Run Hollow/Generation/Generate Milestone 12 Assets.");
            }

            return profile;
        }

        public static string PlannedWindowsBuildPath(BuildAutomationProfileDefinition profile)
        {
            var folder = Path.Combine(profile.BuildRoot, profile.WindowsBuildFolderName);
            return Path.Combine(folder, $"{profile.WindowsExecutableName}.exe");
        }

        public static bool ValidateVisionOSProfilePlaceholders(BuildAutomationProfileDefinition profile, out string error)
        {
            if (profile == null)
            {
                error = "Build automation profile is missing.";
                return false;
            }

            var requiredProfiles = new[]
            {
                Milestone10AssetGenerator.BoundedProfilePath,
                Milestone10AssetGenerator.ImmersiveProfilePath
            };
            foreach (var path in requiredProfiles)
            {
                if (!File.Exists(path))
                {
                    error = $"Missing Vision Pro polish profile: {path}";
                    return false;
                }
            }

            if (!profile.RequiredScenes.Contains("Assets/_Hollow/Scenes/Game_VisionOS_Bounded.unity") ||
                !profile.RequiredScenes.Contains("Assets/_Hollow/Scenes/Game_VisionOS_Immersive.unity"))
            {
                error = "Build automation profile must keep both Vision Pro game scenes in the build scene list.";
                return false;
            }

            error = string.Empty;
            return true;
        }
    }
}
