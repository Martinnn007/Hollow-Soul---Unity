using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using Hollow.Data.Definitions;
using UnityEditor;
using UnityEngine;

namespace Hollow.Editor.Build
{
    public static class PrototypeAuditRunner
    {
        public static PrototypeAuditReport RunFullAudit(BuildAutomationProfileDefinition profile, bool writeReports)
        {
            if (profile == null)
            {
                throw new ArgumentNullException(nameof(profile));
            }

            var report = new PrototypeAuditReport
            {
                auditId = $"prototype-audit-{DateTime.UtcNow:yyyyMMdd-HHmmss}",
                generatedAtUtc = DateTime.UtcNow.ToString("O"),
                unityVersion = Application.unityVersion,
                gitBranch = BuildManifestWriter.ReadGitValue("rev-parse --abbrev-ref HEAD"),
                gitCommit = BuildManifestWriter.ReadGitValue("rev-parse --short HEAD")
            };

            foreach (var validatorTypeName in profile.ValidationTypes)
            {
                report.entries.Add(RunValidator(validatorTypeName));
            }

            report.Recalculate();
            if (writeReports)
            {
                WriteReports(profile, report);
            }

            return report;
        }

        private static PrototypeAuditEntry RunValidator(string validatorTypeName)
        {
            var entry = new PrototypeAuditEntry
            {
                id = ShortName(validatorTypeName),
                validatorType = validatorTypeName
            };
            var messages = new List<string>();
            var stopwatch = Stopwatch.StartNew();

            void HandleLog(string condition, string stackTrace, LogType type)
            {
                if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
                {
                    messages.Add(condition);
                }
            }

            Application.logMessageReceived += HandleLog;
            try
            {
                var validatorType = Type.GetType($"{validatorTypeName}, Hollow.Editor");
                if (validatorType == null)
                {
                    messages.Add($"Validator type not found: {validatorTypeName}");
                    return Finish(entry, stopwatch, messages);
                }

                var method = validatorType.GetMethod("Validate", BindingFlags.Static | BindingFlags.NonPublic, binder: null, types: new[] { typeof(bool) }, modifiers: null);
                if (method == null)
                {
                    messages.Add($"Validator no-exit method not found: {validatorTypeName}.Validate(bool)");
                    return Finish(entry, stopwatch, messages);
                }

                method.Invoke(null, new object[] { false });
            }
            catch (TargetInvocationException exception)
            {
                messages.Add(exception.InnerException != null ? exception.InnerException.Message : exception.Message);
            }
            catch (Exception exception)
            {
                messages.Add(exception.Message);
            }
            finally
            {
                Application.logMessageReceived -= HandleLog;
            }

            return Finish(entry, stopwatch, messages);
        }

        private static PrototypeAuditEntry Finish(PrototypeAuditEntry entry, Stopwatch stopwatch, List<string> messages)
        {
            stopwatch.Stop();
            entry.durationMs = stopwatch.Elapsed.TotalMilliseconds;
            entry.messages = messages;
            entry.passed = messages.Count == 0;
            return entry;
        }

        private static string ShortName(string validatorTypeName)
        {
            if (string.IsNullOrWhiteSpace(validatorTypeName))
            {
                return "Unknown";
            }

            var index = validatorTypeName.LastIndexOf('.');
            return index >= 0 ? validatorTypeName[(index + 1)..] : validatorTypeName;
        }

        private static void WriteReports(BuildAutomationProfileDefinition profile, PrototypeAuditReport report)
        {
            Directory.CreateDirectory(profile.ReportRoot);
            var json = JsonUtility.ToJson(report, prettyPrint: true);
            var latestJsonPath = Path.Combine(profile.ReportRoot, profile.LatestAuditJsonFileName);
            var timestampedJsonPath = Path.Combine(profile.ReportRoot, $"{report.auditId}.json");
            File.WriteAllText(latestJsonPath, json);
            File.WriteAllText(timestampedJsonPath, json);

            var markdown = ToMarkdown(report);
            File.WriteAllText(Path.Combine(profile.ReportRoot, profile.LatestAuditMarkdownFileName), markdown);
            File.WriteAllText(Path.Combine(profile.ReportRoot, $"{report.auditId}.md"), markdown);
            AssetDatabase.Refresh();
        }

        private static string ToMarkdown(PrototypeAuditReport report)
        {
            var builder = new StringBuilder();
            builder.AppendLine("# Hollow Prototype Audit");
            builder.AppendLine();
            builder.AppendLine($"- Result: {report.result}");
            builder.AppendLine($"- Generated: {report.generatedAtUtc}");
            builder.AppendLine($"- Unity: {report.unityVersion}");
            builder.AppendLine($"- Git: {report.gitBranch} @ {report.gitCommit}");
            builder.AppendLine($"- Checks: {report.passedChecks}/{report.totalChecks} passed");
            builder.AppendLine();
            builder.AppendLine("| Check | Result | Duration ms | Notes |");
            builder.AppendLine("| --- | --- | ---: | --- |");
            foreach (var entry in report.entries)
            {
                var notes = entry.messages.Count == 0 ? "OK" : string.Join("<br>", entry.messages);
                builder.AppendLine($"| {entry.id} | {(entry.passed ? "Passed" : "Failed")} | {entry.durationMs:0.0} | {notes} |");
            }

            return builder.ToString();
        }
    }
}
