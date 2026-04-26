using System;
using System.IO;
using System.Linq;
using System.Text;
using Hollow.Data.Definitions;
using Hollow.Editor.Generation;
using Hollow.Editor.Validation;
using UnityEditor;
using UnityEngine;

namespace Hollow.Editor.Build
{
    public static class VerticalSliceLockRunner
    {
        [MenuItem("Hollow/Vertical Slice/Run M25 Lock Gate")]
        public static void RunM25LockGateMenu()
        {
            RunM25LockGate();
        }

        public static VerticalSliceLockReport RunM25LockGate()
        {
            var definition = LoadLockOrThrow();
            var report = VerticalSliceContentValidator.ValidateLock(definition);
            WriteReports(definition, report);
            LogReport(report);
            return report;
        }

        public static VerticalSliceLockDefinition LoadLockOrThrow()
        {
            var definition = AssetDatabase.LoadAssetAtPath<VerticalSliceLockDefinition>(Milestone25AssetGenerator.VerticalSliceLockPath);
            if (definition == null)
            {
                throw new FileNotFoundException($"Missing M25 vertical slice lock at {Milestone25AssetGenerator.VerticalSliceLockPath}. Run Hollow/Generation/Generate Milestone 25 Assets.");
            }

            return definition;
        }

        public static void WriteReports(VerticalSliceLockDefinition definition, VerticalSliceLockReport report)
        {
            Directory.CreateDirectory(definition.ReportRoot);
            Directory.CreateDirectory(Path.GetDirectoryName(definition.PdfOutputPath) ?? "output/pdf");

            var json = JsonUtility.ToJson(report, prettyPrint: true);
            File.WriteAllText(Path.Combine(definition.ReportRoot, definition.LatestJsonFileName), json);
            File.WriteAllText(Path.Combine(definition.ReportRoot, $"{report.reportId}.json"), json);

            var markdown = ToMarkdown(report);
            File.WriteAllText(Path.Combine(definition.ReportRoot, definition.LatestMarkdownFileName), markdown);
            File.WriteAllText(Path.Combine(definition.ReportRoot, $"{report.reportId}.md"), markdown);

            try
            {
                VerticalSlicePdfExporter.WritePdf(definition.PdfOutputPath, report);
            }
            catch (Exception exception)
            {
                report.checks.Add(VerticalSliceCheckResult.BlockedByEnvironment("pdf-handoff", exception.Message, "Ensure the output/pdf folder is writable and rerun the M25 lock gate."));
                report.Recalculate();
                json = JsonUtility.ToJson(report, prettyPrint: true);
                File.WriteAllText(Path.Combine(definition.ReportRoot, definition.LatestJsonFileName), json);
                File.WriteAllText(Path.Combine(definition.ReportRoot, $"{report.reportId}.json"), json);
                markdown = ToMarkdown(report);
                File.WriteAllText(Path.Combine(definition.ReportRoot, definition.LatestMarkdownFileName), markdown);
                File.WriteAllText(Path.Combine(definition.ReportRoot, $"{report.reportId}.md"), markdown);
            }

            AssetDatabase.Refresh();
        }

        public static string ToMarkdown(VerticalSliceLockReport report)
        {
            var builder = new StringBuilder();
            builder.AppendLine("# Hollow M25 Vertical Slice Content Lock");
            builder.AppendLine();
            builder.AppendLine($"- Result: {report.result}");
            builder.AppendLine($"- Generated: {report.generatedAtUtc}");
            builder.AppendLine($"- Unity: {report.unityVersion}");
            builder.AppendLine($"- Git: {report.gitBranch} @ {report.gitCommit}");
            builder.AppendLine($"- Branch: `{report.branchIdentity}`");
            builder.AppendLine($"- Seed: `{report.lockedSeed}`");
            builder.AppendLine($"- Rooms: `{report.roomCount}`");
            builder.AppendLine($"- Connections: `{report.connectionCount}`");
            builder.AppendLine($"- Fixture rooms: `{report.fixtureRoomCount}`");
            builder.AppendLine($"- Approved designer rooms: `{report.approvedRoomCount}`");
            builder.AppendLine($"- Shop offers: `{report.shopOfferCount}`");
            builder.AppendLine($"- Next-branch portals: `{report.nextBranchPortalCount}`");
            builder.AppendLine();
            builder.AppendLine("| Check | Result | Notes | Remediation |");
            builder.AppendLine("| --- | --- | --- | --- |");
            foreach (var check in report.checks)
            {
                builder.AppendLine($"| {check.id} | {check.result} | {Join(check.messages)} | {Join(check.remediation)} |");
            }

            builder.AppendLine();
            builder.AppendLine("## Manual QA Checklist");
            foreach (var item in report.manualChecklist)
            {
                builder.AppendLine($"- {item}");
            }

            return builder.ToString();
        }

        private static string Join(System.Collections.Generic.IReadOnlyList<string> values)
        {
            return values == null || values.Count == 0
                ? "OK"
                : string.Join("<br>", values.Select(value => value.Replace("|", "\\|")));
        }

        private static void LogReport(VerticalSliceLockReport report)
        {
            if (report.result == PlatformBuildQaResult.Failed)
            {
                Debug.LogError($"M25 vertical slice lock failed: {report.reportId}");
                return;
            }

            Debug.Log($"M25 vertical slice lock completed: {report.result} ({report.reportId})");
        }
    }
}
