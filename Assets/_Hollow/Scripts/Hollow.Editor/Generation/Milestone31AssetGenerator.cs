using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Hollow.Editor.Generation
{
    public static class Milestone31AssetGenerator
    {
        public const string BaselineReportDirectory = "output/reports";
        public const string BaselineReportPath = BaselineReportDirectory + "/m31_validation_debt_recovery_baseline.md";

        [MenuItem("Hollow/Generation/Generate Milestone 31 Baseline Marker")]
        public static void Generate()
        {
            Directory.CreateDirectory(BaselineReportDirectory);
            File.WriteAllText(
                BaselineReportPath,
                "# M31 Validation Debt Recovery Baseline\n\n" +
                $"- Generated: {DateTime.UtcNow:O}\n" +
                "- Scope: validator compatibility, clean prototype baseline, and explicit successor-milestone assumptions.\n" +
                "- Notes: M31 creates no gameplay assets; it records the validation-debt cleanup boundary.\n");

            AssetDatabase.Refresh();
            Debug.Log($"Generated Hollow Milestone 31 baseline marker at {BaselineReportPath}.");
        }
    }
}
