using System.IO;
using DiagnosticsProcess = System.Diagnostics.Process;
using DiagnosticsProcessStartInfo = System.Diagnostics.ProcessStartInfo;
using UnityEditor;
using UnityEngine;

namespace Hollow.Editor.Generation
{
    public static class Milestone78AssetGenerator
    {
        public const string DocsPath = "Docs/Hollow_M78_Enemy_Action_Bible.md";
        public const string ReportPath = "output/reports/m78_enemy_action_bible.md";
        public const string PdfPath = "output/pdf/Hollow_M78_Enemy_Action_Bible.pdf";
        public const string GeneratorScriptPath = "tools/generate_m78_enemy_action_bible_pdf.py";
        public const string VerifyScriptPath = "tools/verify_m78_enemy_action_bible_pdf.py";
        public const int MinimumActionCards = 120;
        public const int MaximumActionCards = 180;

        [MenuItem("Hollow/Generation/Generate Milestone 78 Assets")]
        public static void Generate()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(DocsPath) ?? "Docs");
            Directory.CreateDirectory(Path.GetDirectoryName(ReportPath) ?? "output/reports");
            Directory.CreateDirectory(Path.GetDirectoryName(PdfPath) ?? "output/pdf");
            GenerateBibleWithReportLab();
            AssetDatabase.Refresh();
            Debug.Log("Generated Hollow Milestone 78 enemy action bible artifacts.");
        }

        private static void GenerateBibleWithReportLab()
        {
            if (!File.Exists(GeneratorScriptPath))
            {
                Debug.LogWarning($"M78 generator script not found at {GeneratorScriptPath}.");
                return;
            }

            try
            {
                var startInfo = new DiagnosticsProcessStartInfo
                {
                    FileName = "python3",
                    Arguments = GeneratorScriptPath,
                    WorkingDirectory = Directory.GetCurrentDirectory(),
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using var process = DiagnosticsProcess.Start(startInfo);
                if (process == null)
                {
                    Debug.LogWarning("M78 action bible generation did not start.");
                    return;
                }

                var output = process.StandardOutput.ReadToEnd();
                var error = process.StandardError.ReadToEnd();
                process.WaitForExit();
                if (process.ExitCode == 0)
                {
                    Debug.Log(string.IsNullOrWhiteSpace(output) ? $"Generated {PdfPath}." : output.Trim());
                    return;
                }

                Debug.LogWarning($"M78 action bible generation failed with exit code {process.ExitCode}: {error}");
            }
            catch (System.Exception exception)
            {
                Debug.LogWarning($"M78 action bible generation skipped: {exception.Message}");
            }
        }
    }
}
