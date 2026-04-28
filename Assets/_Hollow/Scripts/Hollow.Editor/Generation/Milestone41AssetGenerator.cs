using System.IO;
using Hollow.Data.Definitions;
using Hollow.Editor.Build;
using UnityEditor;
using UnityEngine;

namespace Hollow.Editor.Generation
{
    public static class Milestone41AssetGenerator
    {
        public const string AuditDirectory = "Assets/_Hollow/Data/Handoff/M41";
        public const string AuditDefinitionPath = AuditDirectory + "/CurrentMilestoneAudit_M41.asset";
        public const string LatestJsonPath = "output/reports/latest_m41_current_milestone_audit.json";
        public const string LatestMarkdownPath = "output/reports/latest_m41_current_milestone_audit.md";

        public static readonly string[] CurrentValidationTypes =
        {
            "Hollow.Editor.Validation.Milestone31Validator",
            "Hollow.Editor.Validation.Milestone32Validator",
            "Hollow.Editor.Validation.Milestone33Validator",
            "Hollow.Editor.Validation.Milestone34Validator",
            "Hollow.Editor.Validation.Milestone35Validator",
            "Hollow.Editor.Validation.Milestone36Validator",
            "Hollow.Editor.Validation.Milestone37Validator",
            "Hollow.Editor.Validation.Milestone38Validator",
            "Hollow.Editor.Validation.Milestone39Validator",
            "Hollow.Editor.Validation.Milestone40Validator"
        };

        [MenuItem("Hollow/Generation/Generate Milestone 41 Assets")]
        public static void Generate()
        {
            Milestone40AssetGenerator.Generate();
            Directory.CreateDirectory(AuditDirectory);
            Directory.CreateDirectory("output/reports");

            var definition = AssetDatabase.LoadAssetAtPath<CurrentMilestoneAuditDefinition>(AuditDefinitionPath);
            if (definition == null)
            {
                definition = ScriptableObject.CreateInstance<CurrentMilestoneAuditDefinition>();
                AssetDatabase.CreateAsset(definition, AuditDefinitionPath);
            }

            definition.Configure(
                "m41_current_milestone_audit_v1",
                "M41 Current Milestone Audit",
                "output/reports",
                Path.GetFileName(LatestJsonPath),
                Path.GetFileName(LatestMarkdownPath),
                CurrentValidationTypes,
                RequiredEvidenceReports());
            EditorUtility.SetDirty(definition);

            RunAudit(definition);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Generated Hollow Milestone 41 current milestone audit.");
        }

        public static PrototypeAuditReport RunAudit(CurrentMilestoneAuditDefinition definition)
        {
            var profile = ScriptableObject.CreateInstance<BuildAutomationProfileDefinition>();
            profile.Configure(
                definition.DisplayName,
                "output/builds",
                definition.ReportRoot,
                "HollowSoul_M41_Audit_Windows",
                "HollowSoul",
                definition.LatestJsonFileName,
                definition.LatestMarkdownFileName,
                "latest_m41_current_milestone_manifest.json",
                nextRequirePassingAuditBeforeBuild: true,
                Milestone11AssetGenerator.RequiredBuildScenes,
                definition.ValidationTypes as string[] ?? CurrentValidationTypes);
            var report = PrototypeAuditRunner.RunFullAudit(profile, writeReports: true);
            Object.DestroyImmediate(profile);
            return report;
        }

        private static string[] RequiredEvidenceReports()
        {
            return new[]
            {
                "output/reports/latest_platform_build_qa.json",
                "output/reports/latest_platform_build_qa.md",
                "output/reports/latest_vertical_slice_lock.json",
                "output/reports/latest_vertical_slice_lock.md",
                "output/reports/latest_m40_external_handoff.json",
                "output/reports/latest_m40_external_handoff.md",
                "output/reports/m24-editmode-results.xml"
            };
        }
    }
}
