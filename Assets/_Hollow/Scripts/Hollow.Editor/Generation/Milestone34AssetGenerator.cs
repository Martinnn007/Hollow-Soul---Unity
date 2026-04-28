using System;
using System.IO;
using UnityEditor;

namespace Hollow.Editor.Generation
{
    public static class Milestone34AssetGenerator
    {
        public const string BaselineReportPath = "output/reports/m34_shield_defense_armor_behavior.md";

        [MenuItem("Hollow/Generation/Generate Milestone 34 Assets")]
        public static void Generate()
        {
            Milestone33AssetGenerator.Generate();
            Directory.CreateDirectory(Path.GetDirectoryName(BaselineReportPath) ?? "output/reports");
            File.WriteAllText(
                BaselineReportPath,
                "# M34 Shield / Defense / Armor Behavior V1\n\n" +
                $"- Generated: {DateTime.UtcNow:O}\n" +
                "- Scope: passive defense mitigation, shield guard input, guard stamina costs, contact pushback, and HUD defense status.\n" +
                "- Verification: run Milestone34Validator and the M32 QA gate.\n");
            AssetDatabase.Refresh();
        }
    }
}
