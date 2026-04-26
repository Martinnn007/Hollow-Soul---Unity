using System;
using UnityEngine;

namespace Hollow.Data.Definitions
{
    [CreateAssetMenu(menuName = "Hollow/Build Automation Profile", fileName = "BuildAutomationProfile")]
    public sealed class BuildAutomationProfileDefinition : HollowDefinition
    {
        [SerializeField] private string prototypeVersion = "M12 Build Automation";
        [SerializeField] private string buildRoot = "output/builds";
        [SerializeField] private string reportRoot = "output/reports";
        [SerializeField] private string windowsBuildFolderName = "HollowSoul_Prototype_Windows";
        [SerializeField] private string windowsExecutableName = "HollowSoul";
        [SerializeField] private string latestAuditJsonFileName = "latest_prototype_audit.json";
        [SerializeField] private string latestAuditMarkdownFileName = "latest_prototype_audit.md";
        [SerializeField] private string latestBuildManifestFileName = "latest_build_manifest.json";
        [SerializeField] private bool requirePassingAuditBeforeBuild = true;
        [SerializeField] private string[] requiredScenes = Array.Empty<string>();
        [SerializeField] private string[] validationTypes = Array.Empty<string>();

        public string PrototypeVersion => prototypeVersion;

        public string BuildRoot => buildRoot;

        public string ReportRoot => reportRoot;

        public string WindowsBuildFolderName => windowsBuildFolderName;

        public string WindowsExecutableName => windowsExecutableName;

        public string LatestAuditJsonFileName => latestAuditJsonFileName;

        public string LatestAuditMarkdownFileName => latestAuditMarkdownFileName;

        public string LatestBuildManifestFileName => latestBuildManifestFileName;

        public bool RequirePassingAuditBeforeBuild => requirePassingAuditBeforeBuild;

        public string[] RequiredScenes => requiredScenes;

        public string[] ValidationTypes => validationTypes;

        public void Configure(
            string nextPrototypeVersion,
            string nextBuildRoot,
            string nextReportRoot,
            string nextWindowsBuildFolderName,
            string nextWindowsExecutableName,
            string nextLatestAuditJsonFileName,
            string nextLatestAuditMarkdownFileName,
            string nextLatestBuildManifestFileName,
            bool nextRequirePassingAuditBeforeBuild,
            string[] nextRequiredScenes,
            string[] nextValidationTypes)
        {
            prototypeVersion = string.IsNullOrWhiteSpace(nextPrototypeVersion) ? "M12 Build Automation" : nextPrototypeVersion;
            buildRoot = string.IsNullOrWhiteSpace(nextBuildRoot) ? "output/builds" : nextBuildRoot;
            reportRoot = string.IsNullOrWhiteSpace(nextReportRoot) ? "output/reports" : nextReportRoot;
            windowsBuildFolderName = string.IsNullOrWhiteSpace(nextWindowsBuildFolderName) ? "HollowSoul_Prototype_Windows" : nextWindowsBuildFolderName;
            windowsExecutableName = string.IsNullOrWhiteSpace(nextWindowsExecutableName) ? "HollowSoul" : nextWindowsExecutableName;
            latestAuditJsonFileName = string.IsNullOrWhiteSpace(nextLatestAuditJsonFileName) ? "latest_prototype_audit.json" : nextLatestAuditJsonFileName;
            latestAuditMarkdownFileName = string.IsNullOrWhiteSpace(nextLatestAuditMarkdownFileName) ? "latest_prototype_audit.md" : nextLatestAuditMarkdownFileName;
            latestBuildManifestFileName = string.IsNullOrWhiteSpace(nextLatestBuildManifestFileName) ? "latest_build_manifest.json" : nextLatestBuildManifestFileName;
            requirePassingAuditBeforeBuild = nextRequirePassingAuditBeforeBuild;
            requiredScenes = nextRequiredScenes ?? Array.Empty<string>();
            validationTypes = nextValidationTypes ?? Array.Empty<string>();
        }
    }
}
