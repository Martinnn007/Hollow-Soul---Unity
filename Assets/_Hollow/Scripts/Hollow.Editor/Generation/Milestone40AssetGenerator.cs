using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Hollow.Data.Definitions;
using Hollow.Editor.Build;
using UnityEditor;
using UnityEngine;

namespace Hollow.Editor.Generation
{
    public static class Milestone40AssetGenerator
    {
        public const string HandoffDirectory = "Assets/_Hollow/Data/Handoff/M40";
        public const string HandoffDefinitionPath = HandoffDirectory + "/ExternalHandoff_M40.asset";
        public const string LatestJsonPath = "output/reports/latest_m40_external_handoff.json";
        public const string LatestMarkdownPath = "output/reports/latest_m40_external_handoff.md";
        public const string TimestampedReportPrefix = "m40-external-handoff";

        [MenuItem("Hollow/Generation/Generate Milestone 40 Assets")]
        public static void Generate()
        {
            if (!File.Exists(Milestone39AssetGenerator.RunFramingCatalogPath))
            {
                Milestone39AssetGenerator.Generate();
            }

            Directory.CreateDirectory(HandoffDirectory);
            Directory.CreateDirectory("output/reports");

            var definition = AssetDatabase.LoadAssetAtPath<ExternalHandoffDefinition>(HandoffDefinitionPath);
            if (definition == null)
            {
                definition = ScriptableObject.CreateInstance<ExternalHandoffDefinition>();
                AssetDatabase.CreateAsset(definition, HandoffDefinitionPath);
            }

            definition.Configure(
                "m40_vertical_slice_external_handoff_v1",
                "M40 Vertical Slice Re-Lock + External Handoff",
                "output/reports",
                Path.GetFileName(LatestJsonPath),
                Path.GetFileName(LatestMarkdownPath),
                "output/reports/latest_vertical_slice_lock.json",
                "output/reports/latest_platform_build_qa.json",
                "output/reports/m24-editmode-results.xml",
                RequiredDocs(),
                RequiredReports(),
                new[] { "windows-development-build" },
                ManualChecklist());
            EditorUtility.SetDirty(definition);

            var report = BuildReport(definition);
            WriteReports(definition, report);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"Generated Hollow Milestone 40 external handoff report: {report.result}.");
        }

        public static ExternalHandoffReport BuildReport(ExternalHandoffDefinition definition)
        {
            var report = new ExternalHandoffReport
            {
                reportId = $"{TimestampedReportPrefix}-{DateTime.UtcNow:yyyyMMdd-HHmmss}",
                generatedAtUtc = DateTime.UtcNow.ToString("O"),
                handoffId = definition != null ? definition.HandoffId : "missing",
                displayName = definition != null ? definition.DisplayName : "Missing M40 handoff definition",
                unityVersion = Application.unityVersion,
                gitBranch = BuildManifestWriter.ReadGitValue("rev-parse --abbrev-ref HEAD"),
                gitCommit = BuildManifestWriter.ReadGitValue("rev-parse --short HEAD")
            };

            if (definition == null)
            {
                report.checks.Add(ExternalHandoffCheckResult.Failed("handoff-definition", "Missing M40 external handoff definition.", "Run Hollow/Generation/Generate Milestone 40 Assets."));
                report.Recalculate();
                return report;
            }

            report.manualChecklist.AddRange(definition.ManualHandoffChecklist);
            ValidateRequiredFiles("required-docs", definition.RequiredDocs, report);
            ValidateRequiredFiles("required-reports", definition.RequiredReports, report);
            ValidateM39WorldFraming(report);
            ValidatePlatformQa(definition.PlatformQaReportPath, report);
            ValidateVerticalSliceLock(definition.VerticalSliceReportPath, report);
            ValidateAcceptedEnvironmentBlocks(definition, report);
            report.acceptedEnvironmentBlocks = string.Join(", ", definition.AcceptedEnvironmentBlocks);
            report.Recalculate();
            return report;
        }

        private static void ValidateRequiredFiles(string checkId, IEnumerable<string> files, ExternalHandoffReport report)
        {
            var missing = (files ?? Enumerable.Empty<string>())
                .Where(file => !File.Exists(file))
                .ToArray();
            if (missing.Length == 0)
            {
                report.checks.Add(ExternalHandoffCheckResult.Passed(checkId, "All required files are present."));
                return;
            }

            report.checks.Add(ExternalHandoffCheckResult.Failed(checkId, $"Missing files: {string.Join(", ", missing)}", "Regenerate the corresponding milestone assets/reports."));
        }

        private static void ValidateM39WorldFraming(ExternalHandoffReport report)
        {
            var catalog = AssetDatabase.LoadAssetAtPath<RunFramingCatalogDefinition>(Milestone39AssetGenerator.RunFramingCatalogPath);
            if (catalog == null || catalog.Worlds.Count < 3)
            {
                report.checks.Add(ExternalHandoffCheckResult.Failed("m39-world-framing", "M39 run framing catalog is missing or incomplete.", "Run M39 generation and validation."));
                return;
            }

            report.checks.Add(ExternalHandoffCheckResult.Passed("m39-world-framing", $"Run framing catalog contains {catalog.Worlds.Count} prototype worlds."));
        }

        private static void ValidatePlatformQa(string reportPath, ExternalHandoffReport report)
        {
            if (!File.Exists(reportPath))
            {
                report.platformQaResult = PlatformBuildQaResult.NotRun;
                report.checks.Add(ExternalHandoffCheckResult.Failed("platform-qa", $"Missing platform QA report: {reportPath}", "Run Hollow/Platform QA/Run Full M24 QA Gate."));
                return;
            }

            var qaReport = JsonUtility.FromJson<PlatformBuildQaReport>(File.ReadAllText(reportPath));
            report.platformQaResult = qaReport?.result ?? PlatformBuildQaResult.NotRun;
            if (qaReport == null || qaReport.result == PlatformBuildQaResult.Failed || qaReport.targets.Any(target => target.result == PlatformBuildQaResult.Failed))
            {
                report.checks.Add(ExternalHandoffCheckResult.Failed("platform-qa", $"Platform QA is not handoff-ready: {report.platformQaResult}.", "Open latest_platform_build_qa.md and fix failed targets."));
                return;
            }

            var editMode = qaReport.targets.FirstOrDefault(target => target.id == "editmode-tests");
            var sceneSmoke = qaReport.targets.FirstOrDefault(target => target.id == "playmode-smoke-tests");
            if (editMode?.result != PlatformBuildQaResult.Passed || sceneSmoke?.result != PlatformBuildQaResult.Passed)
            {
                report.checks.Add(ExternalHandoffCheckResult.Failed("platform-qa", "Platform QA must include passing EditMode and scene smoke targets.", "Rerun the M24 QA gate and fix test/smoke failures."));
                return;
            }

            var message = qaReport.result == PlatformBuildQaResult.PassedWithEnvironmentBlocks
                ? "Platform QA passed with explicit environment blocks only."
                : "Platform QA passed without environment blocks.";
            report.checks.Add(ExternalHandoffCheckResult.Passed("platform-qa", message));
        }

        private static void ValidateVerticalSliceLock(string reportPath, ExternalHandoffReport report)
        {
            if (!File.Exists(reportPath))
            {
                report.verticalSliceResult = PlatformBuildQaResult.NotRun;
                report.checks.Add(ExternalHandoffCheckResult.Failed("vertical-slice-lock", $"Missing vertical-slice lock report: {reportPath}", "Run the M25 vertical-slice lock gate."));
                return;
            }

            var lockReport = JsonUtility.FromJson<VerticalSliceLockReport>(File.ReadAllText(reportPath));
            report.verticalSliceResult = lockReport?.result ?? PlatformBuildQaResult.NotRun;
            if (lockReport == null || lockReport.result == PlatformBuildQaResult.Failed || lockReport.checks.Any(check => check.result == PlatformBuildQaResult.Failed))
            {
                report.checks.Add(ExternalHandoffCheckResult.Failed("vertical-slice-lock", $"Vertical-slice lock is not handoff-ready: {report.verticalSliceResult}.", "Open latest_vertical_slice_lock.md and fix failed checks."));
                return;
            }

            report.checks.Add(ExternalHandoffCheckResult.Passed("vertical-slice-lock", $"Vertical slice lock passed with {lockReport.roomCount} rooms and {lockReport.connectionCount} connections."));
        }

        private static void ValidateAcceptedEnvironmentBlocks(ExternalHandoffDefinition definition, ExternalHandoffReport report)
        {
            if (!File.Exists(definition.PlatformQaReportPath))
            {
                return;
            }

            var qaReport = JsonUtility.FromJson<PlatformBuildQaReport>(File.ReadAllText(definition.PlatformQaReportPath));
            if (qaReport == null)
            {
                return;
            }

            var unexpectedBlocks = qaReport.targets
                .Where(target => target.result == PlatformBuildQaResult.BlockedByEnvironment)
                .Where(target => !definition.AcceptedEnvironmentBlocks.Contains(target.id))
                .Select(target => target.id)
                .ToArray();
            if (unexpectedBlocks.Length > 0)
            {
                report.checks.Add(ExternalHandoffCheckResult.Failed("environment-blocks", $"Unexpected environment blocks: {string.Join(", ", unexpectedBlocks)}", "Resolve the environment block or add it explicitly to the M40 handoff policy."));
                return;
            }

            var acceptedBlocks = qaReport.targets
                .Where(target => target.result == PlatformBuildQaResult.BlockedByEnvironment)
                .Select(target => target.id)
                .ToArray();
            report.checks.Add(acceptedBlocks.Length == 0
                ? ExternalHandoffCheckResult.Passed("environment-blocks", "No environment blocks reported.")
                : ExternalHandoffCheckResult.BlockedByEnvironment("environment-blocks", $"Accepted environment blocks: {string.Join(", ", acceptedBlocks)}", "Install optional platform modules before producing a full device/player build."));
        }

        private static void WriteReports(ExternalHandoffDefinition definition, ExternalHandoffReport report)
        {
            var json = JsonUtility.ToJson(report, prettyPrint: true);
            File.WriteAllText(definition.LatestJsonPath, json);
            File.WriteAllText(Path.Combine(definition.ReportRoot, $"{report.reportId}.json"), json);

            var markdown = ToMarkdown(report);
            File.WriteAllText(definition.LatestMarkdownPath, markdown);
            File.WriteAllText(Path.Combine(definition.ReportRoot, $"{report.reportId}.md"), markdown);
        }

        private static string ToMarkdown(ExternalHandoffReport report)
        {
            return "# Hollow M40 External Handoff Readiness\n\n" +
                   $"- Result: {report.result}\n" +
                   $"- Generated: {report.generatedAtUtc}\n" +
                   $"- Unity: {report.unityVersion}\n" +
                   $"- Git: {report.gitBranch} @ {report.gitCommit}\n" +
                   $"- Platform QA: {report.platformQaResult}\n" +
                   $"- Vertical Slice Lock: {report.verticalSliceResult}\n" +
                   $"- Accepted Environment Blocks: {report.acceptedEnvironmentBlocks}\n\n" +
                   "| Check | Result | Notes | Remediation |\n" +
                   "| --- | --- | --- | --- |\n" +
                   string.Join("\n", report.checks.Select(check => $"| {check.id} | {check.result} | {Format(check.messages)} | {Format(check.remediation, fallback: "OK")} |")) +
                   "\n\n## Manual Handoff Checklist\n" +
                   string.Join("\n", report.manualChecklist.Select(item => $"- {item}")) +
                   "\n";
        }

        private static string Format(IReadOnlyList<string> lines, string fallback = "")
        {
            return lines == null || lines.Count == 0
                ? fallback
                : string.Join("<br>", lines.Select(line => line.Replace("|", "\\|")));
        }

        private static string[] RequiredDocs()
        {
            return new[]
            {
                "Docs/Milestone38ArtPassRafalPipeline.md",
                "Docs/Milestone39StoryWorldIdentityRunFraming.md"
            };
        }

        private static string[] RequiredReports()
        {
            return new[]
            {
                "output/reports/latest_platform_build_qa.json",
                "output/reports/latest_platform_build_qa.md",
                "output/reports/latest_vertical_slice_lock.json",
                "output/reports/latest_vertical_slice_lock.md",
                "output/reports/m24-editmode-results.xml",
                "output/reports/m39_story_world_identity_run_framing.md"
            };
        }

        private static string[] ManualChecklist()
        {
            return new[]
            {
                "Run Windows/Vision Pro manual smoke from latest M24 QA checklist before external sharing.",
                "If a Windows executable is required, install Unity Windows Build Support and rerun the M24 Windows build target.",
                "Confirm M39 run-framing HUD is readable and does not obscure combat HUD/minimap in all game routes.",
                "Use the M38 ArtPass target catalog as Rafal's source of truth for visual replacement priorities.",
                "Do not treat ArtPass visuals as gameplay-authoritative; colliders, room layout, rewards, and saves remain runtime-model driven."
            };
        }
    }
}
