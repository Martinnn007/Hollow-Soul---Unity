using System.IO;
using Hollow.Data.Definitions;
using Hollow.Editor.Build;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;

namespace Hollow.Editor.Generation
{
    public static class Milestone24AssetGenerator
    {
        public const string PlatformQaDirectory = "Assets/_Hollow/Data/PlatformQA";
        public const string PlatformBuildQaProfilePath = PlatformQaDirectory + "/PlatformBuildQaProfile_M24.asset";
        public const string PlatformQaAddressableLabel = "hollow.platform-qa";
        public const string WindowsM24BuildFolderName = "HollowSoul_M24_Windows";

        public static readonly string[] ValidationTypes =
        {
            "Hollow.Editor.Validation.Milestone0Validator",
            "Hollow.Editor.Validation.Milestone1Validator",
            "Hollow.Editor.Validation.Milestone2Validator",
            "Hollow.Editor.Validation.Milestone3Validator",
            "Hollow.Editor.Validation.Milestone4Validator",
            "Hollow.Editor.Validation.Milestone5Validator",
            "Hollow.Editor.Validation.Milestone6Validator",
            "Hollow.Editor.Validation.Milestone7Validator",
            "Hollow.Editor.Validation.Milestone8Validator",
            "Hollow.Editor.Validation.Milestone9Validator",
            "Hollow.Editor.Validation.Milestone10Validator",
            "Hollow.Editor.Validation.Milestone11Validator",
            "Hollow.Editor.Validation.Milestone12Validator",
            "Hollow.Editor.Validation.Milestone13Validator",
            "Hollow.Editor.Validation.Milestone14Validator",
            "Hollow.Editor.Validation.Milestone15Validator",
            "Hollow.Editor.Validation.Milestone16Validator",
            "Hollow.Editor.Validation.Milestone17Validator",
            "Hollow.Editor.Validation.Milestone18Validator",
            "Hollow.Editor.Validation.Milestone19Validator",
            "Hollow.Editor.Validation.Milestone20Validator",
            "Hollow.Editor.Validation.Milestone21Validator",
            "Hollow.Editor.Validation.Milestone22Validator",
            "Hollow.Editor.Validation.Milestone23Validator"
        };

        [MenuItem("Hollow/Generation/Generate Milestone 24 Assets")]
        public static void Generate()
        {
            Milestone23AssetGenerator.Generate();
            Directory.CreateDirectory(PlatformQaDirectory);
            Directory.CreateDirectory(Milestone12AssetGenerator.OutputBuildRoot);
            Directory.CreateDirectory(Milestone12AssetGenerator.OutputReportRoot);

            var profile = CreateOrUpdateProfile();
            ConfigureAddressables(profile);
            WriteBootstrapReport(profile);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Generated Hollow Milestone 24 platform build/device QA profile and bootstrap reports.");
        }

        private static PlatformBuildQaProfileDefinition CreateOrUpdateProfile()
        {
            var profile = AssetDatabase.LoadAssetAtPath<PlatformBuildQaProfileDefinition>(PlatformBuildQaProfilePath);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<PlatformBuildQaProfileDefinition>();
                AssetDatabase.CreateAsset(profile, PlatformBuildQaProfilePath);
            }

            var performanceBudget = AssetDatabase.LoadAssetAtPath<PerformanceBudgetDefinition>(Milestone11AssetGenerator.PerformanceBudgetPath);
            var buildAutomationProfile = AssetDatabase.LoadAssetAtPath<BuildAutomationProfileDefinition>(Milestone12AssetGenerator.BuildAutomationProfilePath);
            profile.Configure(
                "Hollow Soul Unity M24 Platform Build QA",
                Milestone12AssetGenerator.OutputBuildRoot,
                Milestone12AssetGenerator.OutputReportRoot,
                WindowsM24BuildFolderName,
                "HollowSoul",
                "latest_platform_build_qa.json",
                "latest_platform_build_qa.md",
                nextRequireAddressablesBuild: true,
                nextAllowEnvironmentBlocks: true,
                Milestone11AssetGenerator.RequiredBuildScenes,
                ValidationTypes,
                performanceBudget,
                buildAutomationProfile);
            EditorUtility.SetDirty(profile);
            return profile;
        }

        private static void WriteBootstrapReport(PlatformBuildQaProfileDefinition profile)
        {
            var report = new PlatformBuildQaReport
            {
                reportId = "platform-build-qa-bootstrap",
                generatedAtUtc = System.DateTime.UtcNow.ToString("O"),
                prototypeVersion = profile.PrototypeVersion,
                unityVersion = Application.unityVersion,
                gitBranch = BuildManifestWriter.ReadGitValue("rev-parse --abbrev-ref HEAD"),
                gitCommit = BuildManifestWriter.ReadGitValue("rev-parse --short HEAD"),
                result = PlatformBuildQaResult.NotRun,
                reportRoot = profile.ReportRoot,
                buildRoot = profile.BuildRoot
            };
            report.targets.Add(PlatformBuildTargetResult.NotRun("m24-full-gate", "All Platforms", "Run Hollow/Platform QA/Run Full M24 QA Gate to produce the full platform QA report."));
            report.manualChecklist.Add("Run the full M24 QA gate before external handoff.");
            PlatformBuildQaRunner.WriteReports(profile, report);
        }

        private static void ConfigureAddressables(Object profile)
        {
            var settings = AddressableAssetSettingsDefaultObject.GetSettings(create: true);
            settings.AddLabel(PlatformQaAddressableLabel, postEvent: false);
            var group = settings.FindGroup(Milestone9AssetGenerator.AddressablesGroupName) ?? settings.CreateGroup(
                Milestone9AssetGenerator.AddressablesGroupName,
                setAsDefaultGroup: false,
                readOnly: false,
                postEvent: false,
                schemasToCopy: null,
                typeof(ContentUpdateGroupSchema),
                typeof(BundledAssetGroupSchema));

            var path = AssetDatabase.GetAssetPath(profile);
            var guid = AssetDatabase.AssetPathToGUID(path);
            if (!string.IsNullOrWhiteSpace(guid))
            {
                var entry = settings.CreateOrMoveEntry(guid, group, readOnly: false, postEvent: false);
                entry.address = "hollow.platform_qa.m24";
                entry.SetLabel(PlatformQaAddressableLabel, true, force: true, postEvent: false);
                entry.SetLabel("hollow.data", true, force: true, postEvent: false);
            }

            EditorUtility.SetDirty(settings);
        }
    }
}
