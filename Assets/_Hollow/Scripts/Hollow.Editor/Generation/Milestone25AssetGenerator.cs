using System;
using System.IO;
using System.Linq;
using Hollow.Branches;
using Hollow.Data.Definitions;
using Hollow.Editor.Build;
using Hollow.Editor.Validation;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;

namespace Hollow.Editor.Generation
{
    public static class Milestone25AssetGenerator
    {
        public const string VerticalSliceDataDirectory = "Assets/_Hollow/Data/VerticalSlice";
        public const string VerticalSliceLockPath = VerticalSliceDataDirectory + "/VerticalSliceLock_M25.asset";
        public const string VerticalSliceAddressableLabel = "hollow.vertical-slice";
        public const string VerticalSlicePdfPath = "output/pdf/Hollow_M25_Vertical_Slice_Content_Lock.pdf";
        public const string LatestJsonReportName = "latest_vertical_slice_lock.json";
        public const string LatestMarkdownReportName = "latest_vertical_slice_lock.md";

        [MenuItem("Hollow/Generation/Generate Milestone 25 Assets")]
        public static void Generate()
        {
            Milestone24AssetGenerator.Generate();
            Directory.CreateDirectory(VerticalSliceDataDirectory);
            Directory.CreateDirectory(Milestone12AssetGenerator.OutputReportRoot);
            Directory.CreateDirectory("output/pdf");

            var definition = CreateOrUpdateLockDefinition();
            ConfigureAddressables(definition);
            var report = VerticalSliceContentValidator.ValidateLock(definition);
            VerticalSliceLockRunner.WriteReports(definition, report);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Generated Hollow Milestone 25 vertical slice content lock assets and reports.");
        }

        private static VerticalSliceLockDefinition CreateOrUpdateLockDefinition()
        {
            var definition = AssetDatabase.LoadAssetAtPath<VerticalSliceLockDefinition>(VerticalSliceLockPath);
            if (definition == null)
            {
                definition = ScriptableObject.CreateInstance<VerticalSliceLockDefinition>();
                AssetDatabase.CreateAsset(definition, VerticalSliceLockPath);
            }

            var settings = AssetDatabase.LoadAssetAtPath<BranchGenerationSettingsDefinition>(Milestone15AssetGenerator.SettingsPath);
            var catalog = AssetDatabase.LoadAssetAtPath<BranchRoomTemplateCatalogDefinition>(Milestone14AssetGenerator.CatalogPath);
            var presentationCatalog = AssetDatabase.LoadAssetAtPath<PresentationContentCatalog>(Milestone9AssetGenerator.CatalogPath);
            var platformQa = AssetDatabase.LoadAssetAtPath<PlatformBuildQaProfileDefinition>(Milestone24AssetGenerator.PlatformBuildQaProfilePath);
            definition.Configure(
                "M25 Vertical Slice Content Lock",
                BranchGenerator.BranchFeaturesId,
                BranchGenerator.DefaultSeededMacroSeed,
                "M13 fixtures required; DesignerApproved rooms optional and additive",
                Milestone12AssetGenerator.OutputReportRoot,
                VerticalSlicePdfPath,
                LatestJsonReportName,
                LatestMarkdownReportName,
                nextRequireArtPassPrefabs: true,
                nextAllowEmptyApprovedRoomPool: true,
                nextRequireEqualPlatformChecklist: true,
                nextRequiredShopOfferCount: 3,
                nextRequiredNextBranchPortalCount: 3,
                nextRequiredRoomRoles: new[]
                {
                    BranchRoomRole.Origin.ToString(),
                    BranchRoomRole.Combat.ToString(),
                    BranchRoomRole.Reward.ToString(),
                    BranchRoomRole.Boss.ToString(),
                    BranchRoomRole.Secret.ToString()
                },
                nextRequiredPrefabRoles: Enum.GetValues(typeof(PresentationPrefabRole)).Cast<PresentationPrefabRole>().ToArray(),
                nextRequiredVfxCues: Enum.GetValues(typeof(VfxCueId)).Cast<VfxCueId>().ToArray(),
                nextRequiredAudioCues: Enum.GetValues(typeof(AudioCueId)).Cast<AudioCueId>().ToArray(),
                nextPlatformChecklistTargets: new[] { "Windows", "VisionOSBounded", "VisionOSImmersive" },
                nextBranchGenerationSettings: settings,
                nextRoomTemplateCatalog: catalog,
                nextPresentationCatalog: presentationCatalog,
                nextPlatformQaProfile: platformQa);
            EditorUtility.SetDirty(definition);
            return definition;
        }

        private static void ConfigureAddressables(UnityEngine.Object definition)
        {
            var settings = AddressableAssetSettingsDefaultObject.GetSettings(create: true);
            settings.AddLabel(VerticalSliceAddressableLabel, postEvent: false);
            var group = settings.FindGroup(Milestone9AssetGenerator.AddressablesGroupName) ?? settings.CreateGroup(
                Milestone9AssetGenerator.AddressablesGroupName,
                setAsDefaultGroup: false,
                readOnly: false,
                postEvent: false,
                schemasToCopy: null,
                typeof(ContentUpdateGroupSchema),
                typeof(BundledAssetGroupSchema));

            var path = AssetDatabase.GetAssetPath(definition);
            var guid = AssetDatabase.AssetPathToGUID(path);
            if (!string.IsNullOrWhiteSpace(guid))
            {
                var entry = settings.CreateOrMoveEntry(guid, group, readOnly: false, postEvent: false);
                entry.address = "hollow.vertical_slice.m25";
                entry.SetLabel(VerticalSliceAddressableLabel, true, force: true, postEvent: false);
                entry.SetLabel("hollow.data", true, force: true, postEvent: false);
            }

            EditorUtility.SetDirty(settings);
        }
    }
}
