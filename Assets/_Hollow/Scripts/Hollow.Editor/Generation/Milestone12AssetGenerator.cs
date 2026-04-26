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
    public static class Milestone12AssetGenerator
    {
        public const string BuildAutomationDirectory = "Assets/_Hollow/Data/BuildAutomation";
        public const string BuildAutomationProfilePath = BuildAutomationDirectory + "/BuildAutomationProfile_Prototype.asset";
        public const string BuildAutomationAddressableLabel = "hollow.build-automation";
        public const string OutputBuildRoot = "output/builds";
        public const string OutputReportRoot = "output/reports";

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
            "Hollow.Editor.Validation.Milestone11Validator"
        };

        [MenuItem("Hollow/Generation/Generate Milestone 12 Assets")]
        public static void Generate()
        {
            Milestone11AssetGenerator.Generate();
            Directory.CreateDirectory(BuildAutomationDirectory);
            Directory.CreateDirectory(OutputBuildRoot);
            Directory.CreateDirectory(OutputReportRoot);

            var profile = CreateOrUpdateBuildAutomationProfile();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            ConfigureAddressables(profile);
            var audit = PrototypeAuditRunner.RunFullAudit(profile, writeReports: true);
            profile = AssetDatabase.LoadAssetAtPath<BuildAutomationProfileDefinition>(BuildAutomationProfilePath);
            BuildManifestWriter.WriteManifest(profile, audit, "StandaloneWindows64", HollowBuildAutomation.PlannedWindowsBuildPath(profile), "ManifestOnly");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Generated Hollow Milestone 12 build automation profile, full prototype audit report, and build manifest.");
        }

        private static BuildAutomationProfileDefinition CreateOrUpdateBuildAutomationProfile()
        {
            var profile = AssetDatabase.LoadAssetAtPath<BuildAutomationProfileDefinition>(BuildAutomationProfilePath);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<BuildAutomationProfileDefinition>();
                AssetDatabase.CreateAsset(profile, BuildAutomationProfilePath);
            }

            profile.Configure(
                "Hollow Soul Unity M12 Build Automation",
                OutputBuildRoot,
                OutputReportRoot,
                "HollowSoul_Prototype_Windows",
                "HollowSoul",
                "latest_prototype_audit.json",
                "latest_prototype_audit.md",
                "latest_build_manifest.json",
                nextRequirePassingAuditBeforeBuild: true,
                Milestone11AssetGenerator.RequiredBuildScenes,
                ValidationTypes);
            EditorUtility.SetDirty(profile);
            return profile;
        }

        private static void ConfigureAddressables(Object profile)
        {
            var settings = AddressableAssetSettingsDefaultObject.GetSettings(create: true);
            settings.AddLabel(BuildAutomationAddressableLabel, postEvent: false);
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
                entry.address = "hollow.build_automation.prototype";
                entry.SetLabel(BuildAutomationAddressableLabel, true, force: true, postEvent: false);
                entry.SetLabel("hollow.data", true, force: true, postEvent: false);
            }

            EditorUtility.SetDirty(settings);
        }
    }
}
