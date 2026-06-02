using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Hollow.Data.Definitions;
using Hollow.Performance;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Hollow.Editor.Build
{
    [Serializable]
    public sealed class LightingRenderAuditEditorReport
    {
        public string lockId = LightingRenderAuditReportGenerator.LockId;
        public string generatedAtUtc;
        public string result;
        public string reportRoot;
        public string buildRoot;
        public List<PlatformBuildTargetResult> targets = new();
        public LightingRenderAuditReport playerReport;
        public string lightingJsonPath;
        public string lightingMarkdownPath;
        public string[] failures = Array.Empty<string>();

        public void Recalculate()
        {
            var nextFailures = new List<string>();
            var blocked = false;
            foreach (var target in targets)
            {
                if (target.result == PlatformBuildQaResult.Failed)
                {
                    nextFailures.Add($"{target.id}: {(target.messages == null ? string.Empty : string.Join("; ", target.messages))}");
                }
                else if (target.result == PlatformBuildQaResult.BlockedByEnvironment)
                {
                    blocked = true;
                    nextFailures.Add($"{target.id}: {(target.messages == null ? string.Empty : string.Join("; ", target.messages))}");
                }
            }

            if (playerReport != null && !playerReport.passed)
            {
                nextFailures.Add($"lighting-player-audit: {string.Join("; ", playerReport.failures ?? Array.Empty<string>())}");
            }

            failures = nextFailures.ToArray();
            result = blocked && nextFailures.Count > 0
                ? M140GateResult.BlockedByEnvironment
                : nextFailures.Count == 0 ? M140GateResult.Passed : M140GateResult.Failed;
        }
    }

    public static class LightingRenderAuditBuiltPlayerRunner
    {
        public const string LatestEditorJsonFileName = "lighting_render_audit_editor.json";
        public const string LatestEditorMarkdownFileName = "lighting_render_audit_editor.md";

        [MenuItem("Hollow/Performance/Run Lighting Render Audit Built Player")]
        public static void RunBuiltPlayerMenu()
        {
            var report = RunBuiltPlayerAudit();
            LogReport(report);
        }

        [MenuItem("Hollow/Performance/Import Lighting Render Audit Windows Artifacts")]
        public static void ImportWindowsArtifactsMenu()
        {
            var source = EditorUtility.OpenFolderPanel("Import Lighting Render Audit Windows Artifacts", string.Empty, string.Empty);
            if (string.IsNullOrWhiteSpace(source))
            {
                return;
            }

            var result = ImportWindowsArtifacts(source);
            if (result.result == PlatformBuildQaResult.Passed)
            {
                Debug.Log($"{result.id}: imported Windows lighting render audit artifacts from {source}");
            }
            else
            {
                Debug.LogError($"{result.id}: {JoinMessages(result.messages)}");
            }
        }

        public static void RunBatchBuiltPlayer()
        {
            var report = RunBuiltPlayerAudit();
            LogReport(report);
            if (Application.isBatchMode)
            {
                EditorApplication.Exit(string.Equals(report.result, M140GateResult.Passed, StringComparison.Ordinal) ? 0 : 1);
            }
        }

        public static LightingRenderAuditEditorReport RunBuiltPlayerAudit()
        {
            var profile = M140BuildRealGateRunner.LoadProfileOrCreateDefault();
            var profileBuildRoot = string.IsNullOrWhiteSpace(profile.BuildRoot) ? "Builds/M140BuildRealGate" : profile.BuildRoot;
            var reportRoot = Path.GetFullPath(LightingRenderAuditReportGenerator.DefaultReportDirectory);
            var buildRoot = Path.Combine(Path.GetFullPath(profileBuildRoot), "lighting_render_audit");
            var report = new LightingRenderAuditEditorReport
            {
                generatedAtUtc = DateTime.UtcNow.ToString("O"),
                reportRoot = reportRoot,
                buildRoot = buildRoot,
                lightingJsonPath = Path.Combine(reportRoot, "macos-apple-silicon", M140BuildKind.Development, LightingRenderAuditReportGenerator.DefaultJsonFileName),
                lightingMarkdownPath = Path.Combine(reportRoot, "macos-apple-silicon", M140BuildKind.Development, LightingRenderAuditReportGenerator.DefaultMarkdownFileName)
            };

            Directory.CreateDirectory(reportRoot);
            Directory.CreateDirectory(buildRoot);

            var build = BuildMacOSAppleSiliconPlayer(profile, buildRoot);
            report.targets.Add(build);
            if (build.result == PlatformBuildQaResult.Passed)
            {
                var capture = RunMacOSAppleSiliconCapture(profile, build.outputPath, report.lightingJsonPath, report.lightingMarkdownPath);
                report.targets.Add(capture);
                if (File.Exists(report.lightingJsonPath))
                {
                    try
                    {
                        report.playerReport = JsonUtility.FromJson<LightingRenderAuditReport>(File.ReadAllText(report.lightingJsonPath));
                    }
                    catch (Exception exception)
                    {
                        report.targets.Add(PlatformBuildTargetResult.Failed(
                            "lighting-render-audit-json-parse",
                            "macos-apple-silicon",
                            report.lightingJsonPath,
                            0,
                            "Lighting render audit JSON could not be parsed: " + exception.Message,
                            "Open the JSON report and fix the serializer/schema issue before trusting the audit."));
                    }
                }
            }

            report.Recalculate();
            WriteEditorReport(report);
            return report;
        }

        public static PlatformBuildTargetResult ImportWindowsArtifacts(string sourceDirectory)
        {
            var stopwatch = Stopwatch.StartNew();
            const string targetId = "windows-x64-lighting-render-audit-import";
            var reportRoot = Path.GetFullPath(LightingRenderAuditReportGenerator.DefaultReportDirectory);
            var destinationRoot = Path.Combine(reportRoot, "imported_windows");
            var sourceJson = Path.Combine(sourceDirectory, LightingRenderAuditReportGenerator.DefaultJsonFileName);
            var sourceMarkdown = Path.Combine(sourceDirectory, LightingRenderAuditReportGenerator.DefaultMarkdownFileName);
            var destinationJson = Path.Combine(destinationRoot, LightingRenderAuditReportGenerator.DefaultJsonFileName);
            try
            {
                if (!File.Exists(sourceJson))
                {
                    stopwatch.Stop();
                    return PlatformBuildTargetResult.Failed(
                        targetId,
                        "windows-x64",
                        sourceJson,
                        stopwatch.Elapsed.TotalMilliseconds,
                        "Imported Windows artifact folder does not contain lighting_render_audit.json.",
                        "Run the Windows player with --hollow-lighting-audit and import that output folder.");
                }

                Directory.CreateDirectory(destinationRoot);
                File.Copy(sourceJson, destinationJson, true);
                if (File.Exists(sourceMarkdown))
                {
                    File.Copy(sourceMarkdown, Path.Combine(destinationRoot, LightingRenderAuditReportGenerator.DefaultMarkdownFileName), true);
                }

                foreach (var screenshot in Directory.GetFiles(sourceDirectory, "*.png"))
                {
                    File.Copy(screenshot, Path.Combine(destinationRoot, Path.GetFileName(screenshot)), true);
                }

                var importedReport = JsonUtility.FromJson<LightingRenderAuditReport>(File.ReadAllText(destinationJson));
                var failures = new List<string>();
                if (importedReport == null)
                {
                    failures.Add("Imported Windows lighting render audit JSON parsed to null.");
                }
                else
                {
                    if (!importedReport.passed)
                    {
                        failures.Add("Imported Windows lighting render audit report failed: " + string.Join("; ", importedReport.failures ?? Array.Empty<string>()));
                    }

                    foreach (var scenario in importedReport.scenarios ?? Array.Empty<LightingRenderAuditScenarioSummary>())
                    {
                        var importedScreenshot = Path.Combine(destinationRoot, Path.GetFileName(scenario.screenshotPath ?? string.Empty));
                        if (string.IsNullOrWhiteSpace(scenario.screenshotPath) || !File.Exists(importedScreenshot))
                        {
                            failures.Add($"{scenario.scenarioId}: imported screenshot is missing.");
                        }
                        else
                        {
                            scenario.screenshotPath = importedScreenshot;
                            if (scenario.visual != null)
                            {
                                scenario.visual.screenshotPath = importedScreenshot;
                            }
                        }
                    }

                    LightingRenderAuditReportGenerator.WriteReport(
                        importedReport,
                        destinationJson,
                        Path.Combine(destinationRoot, LightingRenderAuditReportGenerator.DefaultMarkdownFileName));
                }

                stopwatch.Stop();
                return failures.Count == 0
                    ? PlatformBuildTargetResult.Passed(
                        targetId,
                        "windows-x64",
                        destinationJson,
                        stopwatch.Elapsed.TotalMilliseconds,
                        "Imported Windows lighting render audit artifacts and validated the JSON plus screenshots.",
                        "Compare imported_windows/lighting_render_audit.md against the macOS audit report for platform differences.")
                    : PlatformBuildTargetResult.Failed(
                        targetId,
                        "windows-x64",
                        destinationJson,
                        stopwatch.Elapsed.TotalMilliseconds,
                        string.Join(" ", failures),
                        "Re-run the Windows player audit and import the complete output folder with screenshots.");
            }
            catch (Exception exception)
            {
                stopwatch.Stop();
                return PlatformBuildTargetResult.Failed(
                    targetId,
                    "windows-x64",
                    destinationJson,
                    stopwatch.Elapsed.TotalMilliseconds,
                    "Windows lighting render audit import threw: " + exception.Message,
                    "Import a complete artifact folder or fix the file permission/path issue.");
            }
        }

        private static PlatformBuildTargetResult BuildMacOSAppleSiliconPlayer(M140BuildRealGateProfileDefinition profile, string buildRoot)
        {
            var stopwatch = Stopwatch.StartNew();
            var productName = string.IsNullOrWhiteSpace(profile.ProductName)
                ? string.IsNullOrWhiteSpace(PlayerSettings.productName) ? "HollowSoul" : PlayerSettings.productName
                : profile.ProductName;
            var outputPath = Path.Combine(buildRoot, "macos-apple-silicon", productName + "_LightingRenderAudit.app");
            const string targetId = "macos-apple-silicon-lighting-render-audit-build";
            try
            {
                if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Standalone, BuildTarget.StandaloneOSX))
                {
                    stopwatch.Stop();
                    return PlatformBuildTargetResult.BlockedByEnvironment(
                        targetId,
                        "macos-apple-silicon",
                        outputPath,
                        stopwatch.Elapsed.TotalMilliseconds,
                        "macOS standalone build support is not installed in this Unity editor.",
                        "Install the macOS standalone build module and rerun the lighting render audit.");
                }

                Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
                var buildReport = BuildPipeline.BuildPlayer(new BuildPlayerOptions
                {
                    scenes = ResolveBuildScenes(profile),
                    locationPathName = outputPath,
                    target = BuildTarget.StandaloneOSX,
                    targetGroup = BuildTargetGroup.Standalone,
                    options = BuildOptions.Development
                });

                stopwatch.Stop();
                if (buildReport.summary.result == BuildResult.Succeeded)
                {
                    return PlatformBuildTargetResult.Passed(
                        targetId,
                        "macos-apple-silicon",
                        outputPath,
                        stopwatch.Elapsed.TotalMilliseconds,
                        $"Built lighting render audit player at `{outputPath}` ({buildReport.summary.totalSize} bytes).",
                        "Launch the player with --hollow-lighting-audit to regenerate runtime lighting evidence.");
                }

                return PlatformBuildTargetResult.Failed(
                    targetId,
                    "macos-apple-silicon",
                    outputPath,
                    stopwatch.Elapsed.TotalMilliseconds,
                    $"Unity build returned {buildReport.summary.result} with {buildReport.summary.totalErrors} error(s) and {buildReport.summary.totalWarnings} warning(s).",
                    "Open the Console build errors, fix them, and rerun the lighting render audit.");
            }
            catch (Exception exception)
            {
                stopwatch.Stop();
                return PlatformBuildTargetResult.Failed(
                    targetId,
                    "macos-apple-silicon",
                    outputPath,
                    stopwatch.Elapsed.TotalMilliseconds,
                    "Lighting render audit player build threw: " + exception.Message,
                    "Fix the thrown build error and rerun the lighting render audit.");
            }
        }

        private static PlatformBuildTargetResult RunMacOSAppleSiliconCapture(
            M140BuildRealGateProfileDefinition profile,
            string appPath,
            string jsonPath,
            string markdownPath)
        {
            var stopwatch = Stopwatch.StartNew();
            const string targetId = "macos-apple-silicon-lighting-render-audit-capture";
            var outputRoot = Path.GetDirectoryName(jsonPath);
            var stdoutPath = Path.Combine(outputRoot, "player_stdout.txt");
            var stderrPath = Path.Combine(outputRoot, "player_stderr.txt");
            var playerLogCopyPath = Path.Combine(outputRoot, "player_log.txt");
            try
            {
                Directory.CreateDirectory(outputRoot);
                TryDelete(jsonPath);
                TryDelete(markdownPath);
                TryDelete(stdoutPath);
                TryDelete(stderrPath);
                TryDelete(playerLogCopyPath);

                var executablePath = ResolveMacOSExecutablePath(appPath);
                if (string.IsNullOrWhiteSpace(executablePath) || !File.Exists(executablePath))
                {
                    stopwatch.Stop();
                    return PlatformBuildTargetResult.Failed(
                        targetId,
                        "macos-apple-silicon",
                        appPath,
                        stopwatch.Elapsed.TotalMilliseconds,
                        "Could not resolve the built macOS player executable inside the app bundle.",
                        "Rebuild the player and confirm the .app bundle contains Contents/MacOS/<executable>.");
                }

                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = executablePath,
                Arguments = BuildPlayerArguments(outputRoot, M140BuildRealReportGenerator.TargetFrameRate),
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        WorkingDirectory = Directory.GetCurrentDirectory()
                    }
                };

                var launchUtc = DateTime.UtcNow;
                process.Start();
                var stdout = process.StandardOutput.ReadToEndAsync();
                var stderr = process.StandardError.ReadToEndAsync();
                var timeoutMs = Math.Max(30, profile.PlayerTimeoutSeconds) * 1000;
                if (!process.WaitForExit(timeoutMs))
                {
                    try
                    {
                        process.Kill();
                    }
                    catch (Exception)
                    {
                    }

                    stopwatch.Stop();
                    File.WriteAllText(stdoutPath, stdout.IsCompleted ? stdout.Result : string.Empty);
                    File.WriteAllText(stderrPath, stderr.IsCompleted ? stderr.Result : string.Empty);
                    CopyPlayerLogAfterExit(profile, playerLogCopyPath);
                    return PlatformBuildTargetResult.Failed(
                        targetId,
                        "macos-apple-silicon",
                        jsonPath,
                        stopwatch.Elapsed.TotalMilliseconds,
                        $"Lighting render audit player did not exit within {timeoutMs / 1000} seconds.",
                        "Open player_stdout.txt, player_stderr.txt, and player_log.txt to find the hang point.");
                }

                File.WriteAllText(stdoutPath, stdout.Result);
                File.WriteAllText(stderrPath, stderr.Result);
                var playerLogCopied = CopyPlayerLogAfterExit(profile, playerLogCopyPath);

                var failures = new List<string>();
                if (process.ExitCode != 0)
                {
                    failures.Add("Player exited with code " + process.ExitCode + ".");
                }

                if (!playerLogCopied)
                {
                    failures.Add("Player log was not found or could not be copied after the audit run.");
                }

                if (!IsFreshFile(jsonPath, launchUtc) || !IsFreshFile(markdownPath, launchUtc))
                {
                    failures.Add("Fresh lighting render audit JSON/Markdown reports were not written.");
                }

                LightingRenderAuditReport playerReport = null;
                if (File.Exists(jsonPath))
                {
                    try
                    {
                        playerReport = JsonUtility.FromJson<LightingRenderAuditReport>(File.ReadAllText(jsonPath));
                        if (playerReport == null)
                        {
                            failures.Add("Lighting render audit JSON parsed to null.");
                        }
                        else if (!playerReport.passed)
                        {
                            failures.Add("Lighting render audit player report failed: " + string.Join("; ", playerReport.failures ?? Array.Empty<string>()));
                        }
                    }
                    catch (Exception exception)
                    {
                        failures.Add("Lighting render audit JSON parse failed: " + exception.Message);
                    }
                }

                if (playerReport != null)
                {
                    foreach (var scenario in playerReport.scenarios ?? Array.Empty<LightingRenderAuditScenarioSummary>())
                    {
                        if (string.IsNullOrWhiteSpace(scenario.screenshotPath) || !File.Exists(scenario.screenshotPath))
                        {
                            failures.Add($"{scenario.scenarioId}: screenshot file is missing.");
                        }
                        else if (scenario.visual == null || !scenario.visual.exists || !scenario.visual.loadable)
                        {
                            failures.Add($"{scenario.scenarioId}: screenshot visual validation did not load the image.");
                        }
                    }
                }

                var logValidation = M140PlayerLogValidator.Validate(playerLogCopyPath);
                if (!logValidation.passed)
                {
                    failures.Add("Player log validation failed: " + string.Join("; ", logValidation.failures ?? Array.Empty<string>()));
                }

                stopwatch.Stop();
                return failures.Count == 0
                    ? PlatformBuildTargetResult.Passed(
                        targetId,
                        "macos-apple-silicon",
                        jsonPath,
                        stopwatch.Elapsed.TotalMilliseconds,
                        "Built player completed the lighting render audit and wrote fresh reports/screenshots with a clean player log.",
                        "Use lighting_render_audit.md for the readable scenario table and screenshot links.")
                    : PlatformBuildTargetResult.Failed(
                        targetId,
                        "macos-apple-silicon",
                        jsonPath,
                        stopwatch.Elapsed.TotalMilliseconds,
                        string.Join(" ", failures),
                        "Inspect the copied player log, screenshots, and lighting_render_audit.json for the failing scenario evidence.");
            }
            catch (Exception exception)
            {
                stopwatch.Stop();
                return PlatformBuildTargetResult.Failed(
                    targetId,
                    "macos-apple-silicon",
                    jsonPath,
                    stopwatch.Elapsed.TotalMilliseconds,
                    "Lighting render audit player capture threw: " + exception.Message,
                    "Fix the launch/capture exception and rerun the lighting render audit.");
            }
        }

        private static string[] ResolveBuildScenes(M140BuildRealGateProfileDefinition profile)
        {
            if (profile.RequiredScenes != null && profile.RequiredScenes.Length > 0)
            {
                return profile.RequiredScenes;
            }

            return EditorBuildSettings.scenes
                .Where(scene => scene.enabled)
                .Select(scene => scene.path)
                .Where(path => !string.IsNullOrWhiteSpace(path))
                .ToArray();
        }

        private static string BuildPlayerArguments(string outputRoot, int targetFrameRate)
        {
            return string.Join(" ", new[]
            {
                "--hollow-lighting-audit",
                "--hollow-lighting-audit-auto-exit",
                "--hollow-lighting-audit-output=" + Quote(outputRoot),
                "--hollow-lighting-audit-platform=macos-apple-silicon",
                "--hollow-lighting-audit-mode=full",
                "--hollow-lighting-audit-fps-cap=" + Mathf.Max(1, targetFrameRate).ToString(System.Globalization.CultureInfo.InvariantCulture)
            });
        }

        private static string ResolveMacOSExecutablePath(string appPath)
        {
            var macOsDirectory = Path.Combine(appPath, "Contents", "MacOS");
            if (!Directory.Exists(macOsDirectory))
            {
                return string.Empty;
            }

            return Directory.GetFiles(macOsDirectory)
                .Where(path => !Path.GetFileName(path).StartsWith(".", StringComparison.Ordinal))
                .OrderByDescending(path => new FileInfo(path).Length)
                .FirstOrDefault() ?? string.Empty;
        }

        private static bool IsFreshFile(string path, DateTime launchUtc)
        {
            return File.Exists(path) && File.GetLastWriteTimeUtc(path) >= launchUtc.AddSeconds(-2d);
        }

        private static bool CopyPlayerLogAfterExit(M140BuildRealGateProfileDefinition profile, string destinationPath)
        {
            try
            {
                var sourcePath = ResolvePlayerLogPath(profile);
                if (File.Exists(sourcePath))
                {
                    File.Copy(sourcePath, destinationPath, true);
                    return true;
                }

                File.WriteAllText(destinationPath, "Player log was not found at " + sourcePath);
                return false;
            }
            catch (Exception exception)
            {
                File.WriteAllText(destinationPath, "Player log copy failed: " + exception.Message);
                return false;
            }
        }

        private static string ResolvePlayerLogPath(M140BuildRealGateProfileDefinition profile)
        {
            var company = string.IsNullOrWhiteSpace(PlayerSettings.companyName) ? "DefaultCompany" : PlayerSettings.companyName;
            var product = string.IsNullOrWhiteSpace(PlayerSettings.productName) ? profile.ProductName : PlayerSettings.productName;
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "Library", "Logs", company, product, "Player.log");
        }

        private static string Quote(string value)
        {
            return "\"" + (value ?? string.Empty).Replace("\"", "\\\"") + "\"";
        }

        private static void TryDelete(string path)
        {
            if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
            {
                File.Delete(path);
            }
        }

        private static void WriteEditorReport(LightingRenderAuditEditorReport report)
        {
            Directory.CreateDirectory(report.reportRoot);
            File.WriteAllText(Path.Combine(report.reportRoot, LatestEditorJsonFileName), JsonUtility.ToJson(report, true));
            File.WriteAllText(Path.Combine(report.reportRoot, LatestEditorMarkdownFileName), ToMarkdown(report));
        }

        private static string ToMarkdown(LightingRenderAuditEditorReport report)
        {
            var builder = new StringBuilder();
            builder.AppendLine("# Lighting Render Audit Built Player");
            builder.AppendLine();
            builder.AppendLine("| Field | Value |");
            builder.AppendLine("| --- | --- |");
            builder.AppendLine($"| Result | {report.result} |");
            builder.AppendLine($"| Generated UTC | {report.generatedAtUtc} |");
            builder.AppendLine($"| Report root | `{report.reportRoot}` |");
            builder.AppendLine($"| Build root | `{report.buildRoot}` |");
            builder.AppendLine($"| Runtime JSON | `{report.lightingJsonPath}` |");
            builder.AppendLine($"| Runtime Markdown | `{report.lightingMarkdownPath}` |");
            builder.AppendLine();
            builder.AppendLine("## Editor targets");
            builder.AppendLine();
            builder.AppendLine("| Target | Result | Output | Messages |");
            builder.AppendLine("| --- | --- | --- | --- |");
            foreach (var target in report.targets ?? new List<PlatformBuildTargetResult>())
            {
                builder.AppendLine($"| {target.id} | {target.result} | `{target.outputPath}` | {JoinMessages(target.messages)} |");
            }

            if (report.playerReport != null)
            {
                builder.AppendLine();
                builder.AppendLine("## Runtime scenario summary");
                builder.AppendLine();
                builder.AppendLine("| Scenario | Result | Biome | Lights | p95 frame | Screenshot |");
                builder.AppendLine("| --- | --- | --- | --- | --- | --- |");
                foreach (var scenario in report.playerReport.scenarios ?? Array.Empty<LightingRenderAuditScenarioSummary>())
                {
                    builder.AppendLine($"| {scenario.scenarioId} | {(scenario.passed ? "PASS" : "FAIL")} | {scenario.biomeId} | {scenario.peakActiveLocalLights}/{scenario.maxActiveLocalLights} | {scenario.frameP95Ms:0.00}ms | `{scenario.screenshotPath}` |");
                }
            }

            if (report.failures != null && report.failures.Length > 0)
            {
                builder.AppendLine();
                builder.AppendLine("## Failures");
                builder.AppendLine();
                foreach (var failure in report.failures)
                {
                    builder.AppendLine("- " + failure);
                }
            }

            return builder.ToString();
        }

        private static void LogReport(LightingRenderAuditEditorReport report)
        {
            var message = $"Lighting render audit built-player result: {report.result}. Report: {Path.Combine(report.reportRoot, LatestEditorMarkdownFileName)}";
            if (string.Equals(report.result, M140GateResult.Passed, StringComparison.Ordinal))
            {
                Debug.Log(message);
            }
            else
            {
                Debug.LogError(message + "\n" + string.Join("\n", report.failures ?? Array.Empty<string>()));
            }
        }

        private static string JoinMessages(IEnumerable<string> messages)
        {
            return messages == null ? string.Empty : string.Join("; ", messages);
        }
    }
}
