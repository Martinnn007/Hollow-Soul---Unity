using System;
using System.IO;
using UnityEditor;

namespace Hollow.Editor.Generation
{
    public static class Milestone33AssetGenerator
    {
        public const string BaselineReportPath = "output/reports/m33_combat_feel_physics_camera_polish.md";

        [MenuItem("Hollow/Generation/Generate Milestone 33 Assets")]
        public static void Generate()
        {
            Milestone32AssetGenerator.Generate();
            Directory.CreateDirectory(Path.GetDirectoryName(BaselineReportPath) ?? "output/reports");
            File.WriteAllText(
                BaselineReportPath,
                "# M33 Combat Feel, Physics, Collision, and Camera Polish\n\n" +
                $"- Generated: {DateTime.UtcNow:O}\n" +
                "- Scope: sub-stepped movement/projectiles, obstacle sliding consistency, and traversal-safe gameplay camera follow.\n" +
                "- Verification: run Milestone33Validator and the M32 QA gate.\n");
            AssetDatabase.Refresh();
        }
    }
}
