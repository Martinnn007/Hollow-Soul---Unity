using System;
using UnityEngine;

namespace Hollow.Data.Definitions
{
    [CreateAssetMenu(menuName = "Hollow/Performance/M140 Build Real Gate Profile", fileName = "M140BuildRealGateProfile")]
    public sealed class M140BuildRealGateProfileDefinition : HollowDefinition
    {
        [SerializeField] private string buildRoot = "output/builds/m140";
        [SerializeField] private string reportRoot = "output/reports/m140";
        [SerializeField] private string macOSBuildFolderName = "HollowSoul_M140_macOS_AppleSilicon";
        [SerializeField] private string windowsBuildFolderName = "HollowSoul_M140_Windows";
        [SerializeField] private string productName = "HollowSoul";
        [SerializeField] private int targetFrameRate = 60;
        [SerializeField] private int playerTimeoutSeconds = 1800;
        [SerializeField] private bool buildDevelopmentPlayers = true;
        [SerializeField] private bool buildReleaseSmokePlayers = true;
        [SerializeField] private bool requireMacOSAppleSilicon = true;
        [SerializeField] private bool requireWindowsRuntimeArtifacts = true;
        [SerializeField] private string[] targetPlatforms = { "macos-apple-silicon", "windows-x64" };
        [SerializeField] private string[] requiredScenes =
        {
            "Assets/_Hollow/Scenes/Boot.unity",
            "Assets/_Hollow/Scenes/MainMenu.unity",
            "Assets/_Hollow/Scenes/Game_Windows.unity",
            "Assets/_Hollow/Scenes/Spaceship_Windows.unity"
        };
        [SerializeField] private string[] scenarioManifest =
        {
            "boot_loading_screen",
            "branch_entry_loading",
            "normal_traversal",
            "return_to_previous_room",
            "reward_room",
            "enemy_stress_30",
            "projectile_heavy_room",
            "boss_plus_adds",
            "boss_entry",
            "long_run_smoke",
            "next_branch_entry"
        };
        [SerializeField] private string[] releaseSmokeScenarioManifest =
        {
            "boot_loading_screen",
            "branch_entry_loading",
            "normal_traversal"
        };

        public string BuildRoot => string.IsNullOrWhiteSpace(buildRoot) ? "output/builds/m140" : buildRoot;

        public string ReportRoot => string.IsNullOrWhiteSpace(reportRoot) ? "output/reports/m140" : reportRoot;

        public string MacOSBuildFolderName => string.IsNullOrWhiteSpace(macOSBuildFolderName) ? "HollowSoul_M140_macOS_AppleSilicon" : macOSBuildFolderName;

        public string WindowsBuildFolderName => string.IsNullOrWhiteSpace(windowsBuildFolderName) ? "HollowSoul_M140_Windows" : windowsBuildFolderName;

        public string ProductName => string.IsNullOrWhiteSpace(productName) ? "HollowSoul" : productName;

        public int TargetFrameRate => Mathf.Max(1, targetFrameRate);

        public int PlayerTimeoutSeconds => Mathf.Max(30, playerTimeoutSeconds);

        public bool BuildDevelopmentPlayers => buildDevelopmentPlayers;

        public bool BuildReleaseSmokePlayers => buildReleaseSmokePlayers;

        public bool RequireMacOSAppleSilicon => requireMacOSAppleSilicon;

        public bool RequireWindowsRuntimeArtifacts => requireWindowsRuntimeArtifacts;

        public string[] TargetPlatforms => targetPlatforms ?? Array.Empty<string>();

        public string[] RequiredScenes => requiredScenes ?? Array.Empty<string>();

        public string[] ScenarioManifest => scenarioManifest ?? Array.Empty<string>();

        public string[] ReleaseSmokeScenarioManifest => releaseSmokeScenarioManifest ?? Array.Empty<string>();

        public void ConfigureForTests(
            string nextBuildRoot,
            string nextReportRoot,
            string nextMacOSBuildFolderName,
            string nextWindowsBuildFolderName,
            string nextProductName,
            int nextTargetFrameRate,
            int nextPlayerTimeoutSeconds,
            bool nextBuildDevelopmentPlayers,
            bool nextBuildReleaseSmokePlayers,
            bool nextRequireMacOSAppleSilicon,
            bool nextRequireWindowsRuntimeArtifacts,
            string[] nextTargetPlatforms,
            string[] nextRequiredScenes,
            string[] nextScenarioManifest,
            string[] nextReleaseSmokeScenarioManifest)
        {
            buildRoot = string.IsNullOrWhiteSpace(nextBuildRoot) ? "output/builds/m140" : nextBuildRoot;
            reportRoot = string.IsNullOrWhiteSpace(nextReportRoot) ? "output/reports/m140" : nextReportRoot;
            macOSBuildFolderName = string.IsNullOrWhiteSpace(nextMacOSBuildFolderName) ? "HollowSoul_M140_macOS_AppleSilicon" : nextMacOSBuildFolderName;
            windowsBuildFolderName = string.IsNullOrWhiteSpace(nextWindowsBuildFolderName) ? "HollowSoul_M140_Windows" : nextWindowsBuildFolderName;
            productName = string.IsNullOrWhiteSpace(nextProductName) ? "HollowSoul" : nextProductName;
            targetFrameRate = Mathf.Max(1, nextTargetFrameRate);
            playerTimeoutSeconds = Mathf.Max(30, nextPlayerTimeoutSeconds);
            buildDevelopmentPlayers = nextBuildDevelopmentPlayers;
            buildReleaseSmokePlayers = nextBuildReleaseSmokePlayers;
            requireMacOSAppleSilicon = nextRequireMacOSAppleSilicon;
            requireWindowsRuntimeArtifacts = nextRequireWindowsRuntimeArtifacts;
            targetPlatforms = nextTargetPlatforms ?? Array.Empty<string>();
            requiredScenes = nextRequiredScenes ?? Array.Empty<string>();
            scenarioManifest = nextScenarioManifest ?? Array.Empty<string>();
            releaseSmokeScenarioManifest = nextReleaseSmokeScenarioManifest ?? Array.Empty<string>();
        }
    }
}
