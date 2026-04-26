using System;
using UnityEngine;

namespace Hollow.Data.Definitions
{
    [CreateAssetMenu(menuName = "Hollow/Platform Build QA Profile", fileName = "PlatformBuildQaProfile")]
    public sealed class PlatformBuildQaProfileDefinition : HollowDefinition
    {
        [SerializeField] private string prototypeVersion = "M24 Platform Build QA";
        [SerializeField] private string buildRoot = "output/builds";
        [SerializeField] private string reportRoot = "output/reports";
        [SerializeField] private string windowsBuildFolderName = "HollowSoul_M24_Windows";
        [SerializeField] private string windowsExecutableName = "HollowSoul";
        [SerializeField] private string latestQaJsonFileName = "latest_platform_build_qa.json";
        [SerializeField] private string latestQaMarkdownFileName = "latest_platform_build_qa.md";
        [SerializeField] private bool requireAddressablesBuild = true;
        [SerializeField] private bool allowEnvironmentBlocks = true;
        [SerializeField] private string[] requiredScenes = Array.Empty<string>();
        [SerializeField] private string[] validationTypes = Array.Empty<string>();
        [SerializeField] private PerformanceBudgetDefinition performanceBudget;
        [SerializeField] private BuildAutomationProfileDefinition buildAutomationProfile;

        public string PrototypeVersion => prototypeVersion;

        public string BuildRoot => buildRoot;

        public string ReportRoot => reportRoot;

        public string WindowsBuildFolderName => windowsBuildFolderName;

        public string WindowsExecutableName => windowsExecutableName;

        public string LatestQaJsonFileName => latestQaJsonFileName;

        public string LatestQaMarkdownFileName => latestQaMarkdownFileName;

        public bool RequireAddressablesBuild => requireAddressablesBuild;

        public bool AllowEnvironmentBlocks => allowEnvironmentBlocks;

        public string[] RequiredScenes => requiredScenes;

        public string[] ValidationTypes => validationTypes;

        public PerformanceBudgetDefinition PerformanceBudget => performanceBudget;

        public BuildAutomationProfileDefinition BuildAutomationProfile => buildAutomationProfile;

        public void Configure(
            string nextPrototypeVersion,
            string nextBuildRoot,
            string nextReportRoot,
            string nextWindowsBuildFolderName,
            string nextWindowsExecutableName,
            string nextLatestQaJsonFileName,
            string nextLatestQaMarkdownFileName,
            bool nextRequireAddressablesBuild,
            bool nextAllowEnvironmentBlocks,
            string[] nextRequiredScenes,
            string[] nextValidationTypes,
            PerformanceBudgetDefinition nextPerformanceBudget,
            BuildAutomationProfileDefinition nextBuildAutomationProfile)
        {
            prototypeVersion = string.IsNullOrWhiteSpace(nextPrototypeVersion) ? "M24 Platform Build QA" : nextPrototypeVersion;
            buildRoot = string.IsNullOrWhiteSpace(nextBuildRoot) ? "output/builds" : nextBuildRoot;
            reportRoot = string.IsNullOrWhiteSpace(nextReportRoot) ? "output/reports" : nextReportRoot;
            windowsBuildFolderName = string.IsNullOrWhiteSpace(nextWindowsBuildFolderName) ? "HollowSoul_M24_Windows" : nextWindowsBuildFolderName;
            windowsExecutableName = string.IsNullOrWhiteSpace(nextWindowsExecutableName) ? "HollowSoul" : nextWindowsExecutableName;
            latestQaJsonFileName = string.IsNullOrWhiteSpace(nextLatestQaJsonFileName) ? "latest_platform_build_qa.json" : nextLatestQaJsonFileName;
            latestQaMarkdownFileName = string.IsNullOrWhiteSpace(nextLatestQaMarkdownFileName) ? "latest_platform_build_qa.md" : nextLatestQaMarkdownFileName;
            requireAddressablesBuild = nextRequireAddressablesBuild;
            allowEnvironmentBlocks = nextAllowEnvironmentBlocks;
            requiredScenes = nextRequiredScenes ?? Array.Empty<string>();
            validationTypes = nextValidationTypes ?? Array.Empty<string>();
            performanceBudget = nextPerformanceBudget;
            buildAutomationProfile = nextBuildAutomationProfile;
        }
    }
}
