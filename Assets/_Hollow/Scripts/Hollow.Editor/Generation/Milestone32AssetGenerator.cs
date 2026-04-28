using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Hollow.Editor.Generation
{
    public static class Milestone32AssetGenerator
    {
        public const string BaselineReportDirectory = "output/reports";
        public const string BaselineReportPath = BaselineReportDirectory + "/m32_full_qa_rebaseline.md";

        [MenuItem("Hollow/Generation/Generate Milestone 32 Baseline Marker")]
        public static void Generate()
        {
            Milestone31AssetGenerator.Generate();
            Directory.CreateDirectory(BaselineReportDirectory);
            File.WriteAllText(
                BaselineReportPath,
                "# M32 Full QA Gate Rebaseline\n\n" +
                $"- Generated: {DateTime.UtcNow:O}\n" +
                "- Scope: execute QA gate with real EditMode test evidence and editor-side platform scene smoke evidence.\n" +
                "- Notes: Windows build support may still be reported as BlockedByEnvironment on macOS-only editors.\n");

            AssetDatabase.Refresh();
            Debug.Log($"Generated Hollow Milestone 32 baseline marker at {BaselineReportPath}.");
        }
    }
}
