using System;
using System.Diagnostics;
using System.IO;
using Hollow.Data.Definitions;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEngine;

namespace Hollow.Editor.Build
{
    public static class BuildManifestWriter
    {
        public static BuildArtifactManifest WriteManifest(
            BuildAutomationProfileDefinition profile,
            PrototypeAuditReport auditReport,
            string buildTarget,
            string buildPath,
            string buildResult)
        {
            if (profile == null)
            {
                throw new ArgumentNullException(nameof(profile));
            }

            Directory.CreateDirectory(profile.ReportRoot);
            var manifest = new BuildArtifactManifest
            {
                manifestId = $"build-manifest-{DateTime.UtcNow:yyyyMMdd-HHmmss}",
                generatedAtUtc = DateTime.UtcNow.ToString("O"),
                prototypeVersion = profile.PrototypeVersion,
                unityVersion = Application.unityVersion,
                gitBranch = ReadGitValue("rev-parse --abbrev-ref HEAD"),
                gitCommit = ReadGitValue("rev-parse --short HEAD"),
                buildTarget = buildTarget ?? string.Empty,
                buildResult = buildResult ?? string.Empty,
                buildPath = buildPath ?? string.Empty,
                auditResult = auditReport?.result ?? "NotRun",
                auditReportPath = Path.Combine(profile.ReportRoot, profile.LatestAuditJsonFileName),
                addressablesProfile = ResolveAddressablesProfileName()
            };
            manifest.scenes.AddRange(profile.RequiredScenes);

            var json = JsonUtility.ToJson(manifest, prettyPrint: true);
            File.WriteAllText(Path.Combine(profile.ReportRoot, profile.LatestBuildManifestFileName), json);
            File.WriteAllText(Path.Combine(profile.ReportRoot, $"{manifest.manifestId}.json"), json);
            AssetDatabase.Refresh();
            return manifest;
        }

        public static string ReadGitValue(string arguments)
        {
            try
            {
                using var process = new Process();
                process.StartInfo = new ProcessStartInfo("git", arguments)
                {
                    WorkingDirectory = Directory.GetCurrentDirectory(),
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };
                process.Start();
                var output = process.StandardOutput.ReadToEnd().Trim();
                process.WaitForExit(1200);
                return string.IsNullOrWhiteSpace(output) ? "unknown" : output;
            }
            catch
            {
                return "unknown";
            }
        }

        private static string ResolveAddressablesProfileName()
        {
            var settings = AddressableAssetSettingsDefaultObject.GetSettings(create: false);
            if (settings == null || settings.profileSettings == null || string.IsNullOrWhiteSpace(settings.activeProfileId))
            {
                return "none";
            }

            return settings.profileSettings.GetProfileName(settings.activeProfileId);
        }
    }
}
