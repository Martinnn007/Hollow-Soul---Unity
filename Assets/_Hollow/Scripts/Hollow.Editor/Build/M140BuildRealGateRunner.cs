using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Hollow.Data.Definitions;
using Hollow.Performance;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Hollow.Editor.Build
{
    [Serializable]
    public sealed class M140BuildRealGateEditorReport
    {
        public string lockId;
        public string generatedAtUtc;
        public string result;
        public string reportRoot;
        public string buildRoot;
        public List<PlatformBuildTargetResult> targets = new();
        public List<M140BuildRealReport> playerReports = new();
        public string[] failures = Array.Empty<string>();

        public void Recalculate()
        {
            var failuresList = new List<string>();
            var blocked = false;
            foreach (var target in targets)
            {
                if (target.result == PlatformBuildQaResult.Failed)
                {
                    failuresList.Add($"{target.id}: {string.Join("; ", target.messages)}");
                }
                else if (target.result == PlatformBuildQaResult.BlockedByEnvironment)
                {
                    blocked = true;
                    failuresList.Add($"{target.id}: {string.Join("; ", target.messages)}");
                }
            }

            foreach (var report in playerReports)
            {
                if (report == null || report.passed)
                {
                    continue;
                }

                failuresList.Add($"{report.platformId}/{report.buildKind}: {string.Join("; ", report.failures ?? Array.Empty<string>())}");
            }

            failures = failuresList.ToArray();
            result = failuresList.Any(item => !item.Contains("requires a Windows host", StringComparison.OrdinalIgnoreCase)) && targets.Any(target => target.result == PlatformBuildQaResult.Failed)
                ? M140GateResult.Failed
                : blocked ? M140GateResult.BlockedByEnvironment : failuresList.Count == 0 ? M140GateResult.Passed : M140GateResult.Failed;
        }
    }

    public static class M140BuildRealGateRunner
    {
        public const string ProfilePath = "Assets/_Hollow/Data/Performance/M140BuildRealGateProfile.asset";
        public const string LatestEditorJsonFileName = "m140_build_real_gate_editor.json";
        public const string LatestEditorMarkdownFileName = "m140_build_real_gate_editor.md";

        [MenuItem("Hollow/Performance/Run M140 Build-Real Release Gate")]
        public static void RunFullGateMenu()
        {
            var report = RunFullGate();
            LogReport(report);
        }

        [MenuItem("Hollow/Performance/Run M140 macOS Apple Silicon Gate")]
        public static void RunMacOSAppleSiliconGateMenu()
        {
            var report = RunMacOSAppleSiliconGate();
            LogReport(report);
        }

        [MenuItem("Hollow/Performance/Import M140 Windows Player Artifacts")]
        public static void ImportWindowsPlayerArtifactsMenu()
        {
            var source = EditorUtility.OpenFolderPanel("Import M140 Windows Player Artifacts", string.Empty, string.Empty);
            if (string.IsNullOrWhiteSpace(source))
            {
                return;
            }

            var profile = LoadProfileOrCreateDefault();
            var result = M140PlayerArtifactImporter.ImportWindowsArtifacts(source, profile.ReportRoot);
            if (result.result == PlatformBuildQaResult.Passed)
            {
                UnityEngine.Debug.Log($"{result.id}: imported Windows M140 artifacts from {source}");
            }
            else
            {
                UnityEngine.Debug.LogError($"{result.id}: {string.Join("; ", result.messages)}");
            }
        }

        public static M140BuildRealGateEditorReport RunFullGate()
        {
            var profile = LoadProfileOrCreateDefault();
            var report = CreateEditorReport(profile);
            Directory.CreateDirectory(profile.BuildRoot);
            Directory.CreateDirectory(profile.ReportRoot);

            if (profile.BuildDevelopmentPlayers)
            {
                RunBuildAndMaybeCapture(report, profile, "macos-apple-silicon", M140BuildKind.Development);
                RunBuildAndMaybeCapture(report, profile, "windows-x64", M140BuildKind.Development);
            }

            if (profile.BuildReleaseSmokePlayers)
            {
                RunBuildAndMaybeCapture(report, profile, "macos-apple-silicon", M140BuildKind.ReleaseSmoke);
                RunBuildAndMaybeCapture(report, profile, "windows-x64", M140BuildKind.ReleaseSmoke);
            }

            if (profile.RequireWindowsRuntimeArtifacts && !HasPassingWindowsRuntimeReport(report))
            {
                var imported = Path.Combine(profile.ReportRoot, "imported_windows", M140BuildRealReportGenerator.DefaultJsonFileName);
                if (File.Exists(imported))
                {
                    var result = M140PlayerArtifactImporter.ValidateWindowsReport(imported);
                    report.targets.Add(result);
                    TryAddPlayerReport(report, imported);
                }
                else
                {
                    report.targets.Add(PlatformBuildTargetResult.BlockedByEnvironment(
                        "windows-runtime-capture",
                        "windows-x64",
                        imported,
                        0,
                        "Windows runtime capture requires a Windows host or imported M140 Windows player artifacts.",
                        "Run the built Windows player with --hollow-m140-capture on Windows, then import the artifact folder."));
                }
            }

            report.Recalculate();
            WriteEditorReport(profile, report);
            return report;
        }

        public static M140BuildRealGateEditorReport RunMacOSAppleSiliconGate()
        {
            var profile = LoadProfileOrCreateDefault();
            var report = CreateEditorReport(profile);
            Directory.CreateDirectory(profile.BuildRoot);
            Directory.CreateDirectory(profile.ReportRoot);

            if (profile.BuildDevelopmentPlayers)
            {
                RunBuildAndMaybeCapture(report, profile, "macos-apple-silicon", M140BuildKind.Development);
            }

            if (profile.BuildReleaseSmokePlayers)
            {
                RunBuildAndMaybeCapture(report, profile, "macos-apple-silicon", M140BuildKind.ReleaseSmoke);
            }

            report.Recalculate();
            WriteEditorReport(profile, report);
            return report;
        }

        public static M140BuildRealGateProfileDefinition LoadProfileOrCreateDefault()
        {
            var profile = AssetDatabase.LoadAssetAtPath<M140BuildRealGateProfileDefinition>(ProfilePath);
            if (profile != null)
            {
                return profile;
            }

            return ScriptableObject.CreateInstance<M140BuildRealGateProfileDefinition>();
        }

        public static bool ValidateProfile(M140BuildRealGateProfileDefinition profile, out string detail)
        {
            if (profile == null)
            {
                detail = "M140 profile is missing.";
                return false;
            }

            var platforms = new HashSet<string>(profile.TargetPlatforms ?? Array.Empty<string>(), StringComparer.Ordinal);
            var scenes = new HashSet<string>(profile.RequiredScenes ?? Array.Empty<string>(), StringComparer.Ordinal);
            var scenarios = new HashSet<string>(profile.ScenarioManifest ?? Array.Empty<string>(), StringComparer.Ordinal);
            if (!platforms.Contains("macos-apple-silicon") || !platforms.Contains("windows-x64"))
            {
                detail = "M140 profile must include both macos-apple-silicon and windows-x64 targets.";
                return false;
            }

            if (!scenes.Contains("Assets/_Hollow/Scenes/Boot.unity") || !scenes.Contains("Assets/_Hollow/Scenes/Game_Windows.unity"))
            {
                detail = "M140 profile must include Boot and Game_Windows scenes.";
                return false;
            }

            foreach (var scenario in M140BuildRealReportGenerator.RequiredDevelopmentScenarioIds)
            {
                if (!scenarios.Contains(scenario))
                {
                    detail = $"M140 profile missing scenario `{scenario}`.";
                    return false;
                }
            }

            detail = "M140 profile covers Windows, macOS Apple silicon, required scenes, and development scenarios.";
            return true;
        }

        private static void RunBuildAndMaybeCapture(
            M140BuildRealGateEditorReport report,
            M140BuildRealGateProfileDefinition profile,
            string platformId,
            string buildKind)
        {
            var build = BuildPlayer(profile, platformId, buildKind);
            report.targets.Add(build);
            if (build.result != PlatformBuildQaResult.Passed)
            {
                return;
            }

            var capture = RunPlayerCapture(profile, platformId, buildKind, build.outputPath);
            report.targets.Add(capture);
            TryAddPlayerReport(report, CaptureReportPath(profile, platformId, buildKind));
        }

        private static PlatformBuildTargetResult BuildPlayer(M140BuildRealGateProfileDefinition profile, string platformId, string buildKind)
        {
            var stopwatch = Stopwatch.StartNew();
            var buildTarget = BuildTargetFor(platformId);
            var buildTargetGroup = BuildTargetGroup.Standalone;
            var outputPath = PlannedBuildPath(profile, platformId, buildKind);
            var targetId = $"{platformId}-{buildKind}-build";
            try
            {
                if (!buildTarget.HasValue || !BuildPipeline.IsBuildTargetSupported(buildTargetGroup, buildTarget.Value))
                {
                    stopwatch.Stop();
                    return PlatformBuildTargetResult.BlockedByEnvironment(
                        targetId,
                        platformId,
                        outputPath,
                        stopwatch.Elapsed.TotalMilliseconds,
                        $"{platformId} build support is not installed in this Unity editor.",
                        "Install the matching Unity standalone build module and rerun M140.");
                }

                Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
                var notes = platformId == "macos-apple-silicon"
                    ? DescribeMacArchitectureValidation()
                    : string.Empty;
                var options = BuildOptions.None;
                if (string.Equals(buildKind, M140BuildKind.Development, StringComparison.Ordinal))
                {
                    options |= BuildOptions.Development;
                }

                var buildReport = BuildPipeline.BuildPlayer(new BuildPlayerOptions
                {
                    scenes = profile.RequiredScenes,
                    locationPathName = outputPath,
                    target = buildTarget.Value,
                    options = options
                });
                stopwatch.Stop();
                if (buildReport.summary.result == BuildResult.Succeeded)
                {
                    return PlatformBuildTargetResult.Passed(
                        targetId,
                        platformId,
                        outputPath,
                        stopwatch.Elapsed.TotalMilliseconds,
                        $"{platformId} {buildKind} build succeeded: {buildReport.summary.totalSize} bytes. {notes}".Trim());
                }

                return PlatformBuildTargetResult.Failed(
                    targetId,
                    platformId,
                    outputPath,
                    stopwatch.Elapsed.TotalMilliseconds,
                    $"{platformId} {buildKind} build failed: {buildReport.summary.result}.",
                    "Open the Unity editor log for BuildPipeline errors.");
            }
            catch (Exception exception)
            {
                stopwatch.Stop();
                return PlatformBuildTargetResult.Failed(
                    targetId,
                    platformId,
                    outputPath,
                    stopwatch.Elapsed.TotalMilliseconds,
                    exception.Message,
                    "Inspect build scenes, scripts, Addressables, and platform build support.");
            }
        }

        private static PlatformBuildTargetResult RunPlayerCapture(
            M140BuildRealGateProfileDefinition profile,
            string platformId,
            string buildKind,
            string buildPath)
        {
            var targetId = $"{platformId}-{buildKind}-player-capture";
            var outputRoot = CaptureOutputRoot(profile, platformId, buildKind);
            var reportPath = CaptureReportPath(profile, platformId, buildKind);
            var stopwatch = Stopwatch.StartNew();
            try
            {
                if (platformId == "windows-x64" && Application.platform != RuntimePlatform.WindowsEditor)
                {
                    stopwatch.Stop();
                    return PlatformBuildTargetResult.BlockedByEnvironment(
                        targetId,
                        platformId,
                        reportPath,
                        stopwatch.Elapsed.TotalMilliseconds,
                        "Windows runtime capture requires a Windows host or imported M140 Windows player artifacts.",
                        "Run the built Windows player on Windows with --hollow-m140-capture and import the artifact folder.");
                }

                var executable = ResolveExecutablePath(platformId, buildPath);
                if (string.IsNullOrWhiteSpace(executable) || !File.Exists(executable))
                {
                    stopwatch.Stop();
                    return PlatformBuildTargetResult.Failed(
                        targetId,
                        platformId,
                        reportPath,
                        stopwatch.Elapsed.TotalMilliseconds,
                        $"Could not find player executable for {buildPath}.",
                        "Inspect the build output and product name.");
                }

                Directory.CreateDirectory(outputRoot);
                var scenarios = string.Equals(buildKind, M140BuildKind.ReleaseSmoke, StringComparison.Ordinal)
                    ? profile.ReleaseSmokeScenarioManifest
                    : profile.ScenarioManifest;
                var arguments = BuildCaptureArguments(outputRoot, platformId, buildKind, profile.TargetFrameRate, scenarios);
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo(executable, arguments)
                    {
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true,
                        WorkingDirectory = Path.GetDirectoryName(executable)
                    },
                    EnableRaisingEvents = true
                };
                var stdout = new StringBuilder();
                var stderr = new StringBuilder();
                process.OutputDataReceived += (_, args) =>
                {
                    if (args.Data != null)
                    {
                        stdout.AppendLine(args.Data);
                    }
                };
                process.ErrorDataReceived += (_, args) =>
                {
                    if (args.Data != null)
                    {
                        stderr.AppendLine(args.Data);
                    }
                };
                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                var timeoutMs = Math.Max(30, profile.PlayerTimeoutSeconds) * 1000;
                if (!process.WaitForExit(timeoutMs))
                {
                    try
                    {
                        process.Kill();
                    }
                    catch
                    {
                        // Best effort cleanup.
                    }

                    stopwatch.Stop();
                    File.WriteAllText(Path.Combine(outputRoot, "player_stdout_timeout.txt"), stdout.ToString());
                    File.WriteAllText(Path.Combine(outputRoot, "player_stderr_timeout.txt"), stderr.ToString());
                    return PlatformBuildTargetResult.Failed(
                        targetId,
                        platformId,
                        reportPath,
                        stopwatch.Elapsed.TotalMilliseconds,
                        $"M140 player capture timed out after {profile.PlayerTimeoutSeconds}s.",
                        "Open the player logs and reduce scenario scope only if the runner is stuck.");
                }

                process.WaitForExit();
                File.WriteAllText(Path.Combine(outputRoot, "player_stdout.txt"), stdout.ToString());
                File.WriteAllText(Path.Combine(outputRoot, "player_stderr.txt"), stderr.ToString());
                stopwatch.Stop();
                if (!File.Exists(reportPath))
                {
                    return PlatformBuildTargetResult.Failed(
                        targetId,
                        platformId,
                        reportPath,
                        stopwatch.Elapsed.TotalMilliseconds,
                        $"M140 player exited with code {process.ExitCode}, but no report was written.",
                        "Inspect player_stdout.txt, player_stderr.txt, and Player.log.");
                }

                var playerReport = JsonUtility.FromJson<M140BuildRealReport>(File.ReadAllText(reportPath));
                if (playerReport != null && playerReport.passed && process.ExitCode == 0)
                {
                    return PlatformBuildTargetResult.Passed(
                        targetId,
                        platformId,
                        reportPath,
                        stopwatch.Elapsed.TotalMilliseconds,
                        $"{platformId} {buildKind} player capture passed.");
                }

                return PlatformBuildTargetResult.Failed(
                    targetId,
                    platformId,
                    reportPath,
                    stopwatch.Elapsed.TotalMilliseconds,
                    $"{platformId} {buildKind} player capture failed. Exit={process.ExitCode}. {string.Join("; ", playerReport?.failures ?? Array.Empty<string>())}",
                    "Open the M140 player report and screenshots.");
            }
            catch (Exception exception)
            {
                stopwatch.Stop();
                return PlatformBuildTargetResult.Failed(
                    targetId,
                    platformId,
                    reportPath,
                    stopwatch.Elapsed.TotalMilliseconds,
                    exception.Message,
                    "Inspect the build output, process permissions, and player logs.");
            }
        }

        private static string BuildCaptureArguments(string outputRoot, string platformId, string buildKind, int targetFrameRate, string[] scenarios)
        {
            var scenarioText = string.Join(",", scenarios ?? Array.Empty<string>());
            var builder = new StringBuilder();
            builder.Append("--hollow-m140-capture ");
            builder.Append("--hollow-m140-auto-exit ");
            builder.Append("--hollow-m140-output=").Append(Quote(Path.GetFullPath(outputRoot))).Append(' ');
            builder.Append("--hollow-m140-platform=").Append(platformId).Append(' ');
            builder.Append("--hollow-m140-build-kind=").Append(buildKind).Append(' ');
            builder.Append("--hollow-m140-scenarios=").Append(Quote(scenarioText)).Append(' ');
            builder.Append("--hollow-m140-fps-cap=").Append(Mathf.Max(1, targetFrameRate));
            if (string.Equals(buildKind, M140BuildKind.ReleaseSmoke, StringComparison.Ordinal))
            {
                builder.Append(" --hollow-m140-release-smoke");
            }

            return builder.ToString();
        }

        private static string Quote(string value)
        {
            return "\"" + (value ?? string.Empty).Replace("\"", "\\\"") + "\"";
        }

        private static string PlannedBuildPath(M140BuildRealGateProfileDefinition profile, string platformId, string buildKind)
        {
            if (platformId == "macos-apple-silicon")
            {
                return Path.Combine(profile.BuildRoot, profile.MacOSBuildFolderName, buildKind, $"{profile.ProductName}.app");
            }

            return Path.Combine(profile.BuildRoot, profile.WindowsBuildFolderName, buildKind, $"{profile.ProductName}.exe");
        }

        private static BuildTarget? BuildTargetFor(string platformId)
        {
            return platformId switch
            {
                "macos-apple-silicon" => BuildTarget.StandaloneOSX,
                "windows-x64" => BuildTarget.StandaloneWindows64,
                _ => null
            };
        }

        private static string ResolveExecutablePath(string platformId, string buildPath)
        {
            if (platformId == "macos-apple-silicon")
            {
                var macOsDirectory = Path.Combine(buildPath, "Contents", "MacOS");
                return Directory.Exists(macOsDirectory)
                    ? Directory.GetFiles(macOsDirectory).FirstOrDefault(path => !Path.GetFileName(path).StartsWith(".", StringComparison.Ordinal))
                    : string.Empty;
            }

            return buildPath;
        }

        private static string CaptureOutputRoot(M140BuildRealGateProfileDefinition profile, string platformId, string buildKind)
        {
            return Path.Combine(profile.ReportRoot, platformId, buildKind);
        }

        private static string CaptureReportPath(M140BuildRealGateProfileDefinition profile, string platformId, string buildKind)
        {
            return Path.Combine(CaptureOutputRoot(profile, platformId, buildKind), M140BuildRealReportGenerator.DefaultJsonFileName);
        }

        private static bool HasPassingWindowsRuntimeReport(M140BuildRealGateEditorReport report)
        {
            return report.playerReports.Any(playerReport =>
                playerReport != null &&
                playerReport.passed &&
                string.Equals(playerReport.platformId, "windows-x64", StringComparison.Ordinal));
        }

        private static void TryAddPlayerReport(M140BuildRealGateEditorReport report, string path)
        {
            try
            {
                if (!File.Exists(path))
                {
                    return;
                }

                var playerReport = JsonUtility.FromJson<M140BuildRealReport>(File.ReadAllText(path));
                if (playerReport != null)
                {
                    report.playerReports.Add(playerReport);
                }
            }
            catch
            {
                // The capture target already reports parse/read failures. Avoid hiding that with an editor exception.
            }
        }

        private static string DescribeMacArchitectureValidation()
        {
            try
            {
                var method = typeof(PlayerSettings).GetMethod(
                    "GetArchitecture",
                    BindingFlags.Public | BindingFlags.Static,
                    null,
                    new[] { typeof(BuildTargetGroup) },
                    null);
                if (method == null)
                {
                    return "Mac architecture API not available through reflection; runtime report will validate Apple silicon.";
                }

                var architecture = method.Invoke(null, new object[] { BuildTargetGroup.Standalone });
                return $"Current standalone architecture setting `{architecture}`; runtime report must confirm Apple silicon/ARM64 before pass.";
            }
            catch (Exception exception)
            {
                return $"Could not inspect macOS architecture ({exception.GetType().Name}); runtime report will validate Apple silicon.";
            }
        }

        private static M140BuildRealGateEditorReport CreateEditorReport(M140BuildRealGateProfileDefinition profile)
        {
            return new M140BuildRealGateEditorReport
            {
                lockId = M140BuildRealReportGenerator.LockId,
                generatedAtUtc = DateTime.UtcNow.ToString("O"),
                result = M140GateResult.Failed,
                reportRoot = profile.ReportRoot,
                buildRoot = profile.BuildRoot
            };
        }

        private static void WriteEditorReport(M140BuildRealGateProfileDefinition profile, M140BuildRealGateEditorReport report)
        {
            Directory.CreateDirectory(profile.ReportRoot);
            var jsonPath = Path.Combine(profile.ReportRoot, LatestEditorJsonFileName);
            var markdownPath = Path.Combine(profile.ReportRoot, LatestEditorMarkdownFileName);
            File.WriteAllText(jsonPath, JsonUtility.ToJson(report, true));
            File.WriteAllText(markdownPath, ToMarkdown(report));
            AssetDatabase.Refresh();
        }

        private static string ToMarkdown(M140BuildRealGateEditorReport report)
        {
            var builder = new StringBuilder();
            builder.AppendLine("# M140 Build-Real Gate Editor Orchestration");
            builder.AppendLine();
            builder.AppendLine($"- Status: `{report.result}`");
            builder.AppendLine($"- Build root: `{report.buildRoot}`");
            builder.AppendLine($"- Report root: `{report.reportRoot}`");
            builder.AppendLine();
            builder.AppendLine("## Targets");
            foreach (var target in report.targets)
            {
                builder.AppendLine($"- `{target.id}` {target.platform}: {target.result} ({target.durationMs:0} ms) `{target.outputPath}`");
                foreach (var message in target.messages)
                {
                    builder.AppendLine($"  - {message}");
                }
            }

            if (report.failures != null && report.failures.Length > 0)
            {
                builder.AppendLine();
                builder.AppendLine("## Failures");
                foreach (var failure in report.failures)
                {
                    builder.AppendLine($"- {failure}");
                }
            }

            return builder.ToString();
        }

        private static void LogReport(M140BuildRealGateEditorReport report)
        {
            if (report == null || report.result == M140GateResult.Failed)
            {
                UnityEngine.Debug.LogError($"M140 build-real gate failed. Report: {LatestEditorJsonFileName}");
            }
            else
            {
                UnityEngine.Debug.Log($"M140 build-real gate completed: {report.result}. Report: {LatestEditorJsonFileName}");
            }
        }
    }

    public static class M140PlayerArtifactImporter
    {
        public static PlatformBuildTargetResult ImportWindowsArtifacts(string sourceRoot, string reportRoot)
        {
            var stopwatch = Stopwatch.StartNew();
            var sourceReport = Path.Combine(sourceRoot ?? string.Empty, M140BuildRealReportGenerator.DefaultJsonFileName);
            var validation = ValidateWindowsReport(sourceReport);
            if (validation.result != PlatformBuildQaResult.Passed)
            {
                stopwatch.Stop();
                validation.durationMs = stopwatch.Elapsed.TotalMilliseconds;
                return validation;
            }

            var destinationRoot = Path.Combine(reportRoot, "imported_windows");
            Directory.CreateDirectory(destinationRoot);
            foreach (var file in Directory.GetFiles(sourceRoot, "*", SearchOption.AllDirectories))
            {
                var relative = file.Substring(sourceRoot.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                var destination = Path.Combine(destinationRoot, relative);
                Directory.CreateDirectory(Path.GetDirectoryName(destination));
                File.Copy(file, destination, overwrite: true);
            }

            stopwatch.Stop();
            return PlatformBuildTargetResult.Passed(
                "windows-runtime-artifact-import",
                "windows-x64",
                Path.Combine(destinationRoot, M140BuildRealReportGenerator.DefaultJsonFileName),
                stopwatch.Elapsed.TotalMilliseconds,
                "Imported passing M140 Windows runtime artifacts.");
        }

        public static PlatformBuildTargetResult ValidateWindowsReport(string reportPath)
        {
            if (string.IsNullOrWhiteSpace(reportPath) || !File.Exists(reportPath))
            {
                return PlatformBuildTargetResult.Failed(
                    "windows-runtime-artifact-import",
                    "windows-x64",
                    reportPath ?? string.Empty,
                    0,
                    "M140 Windows artifact folder does not contain m140_build_real_gate.json.",
                    "Run the Windows built player with --hollow-m140-capture and import that output folder.");
            }

            try
            {
                var report = JsonUtility.FromJson<M140BuildRealReport>(File.ReadAllText(reportPath));
                if (report == null)
                {
                    return PlatformBuildTargetResult.Failed("windows-runtime-artifact-import", "windows-x64", reportPath, 0, "Could not parse M140 Windows report.");
                }

                if (!string.Equals(report.platformId, "windows-x64", StringComparison.Ordinal))
                {
                    return PlatformBuildTargetResult.Failed("windows-runtime-artifact-import", "windows-x64", reportPath, 0, $"Imported M140 report platform is `{report.platformId}`, expected `windows-x64`.");
                }

                if (!report.passed)
                {
                    return PlatformBuildTargetResult.Failed("windows-runtime-artifact-import", "windows-x64", reportPath, 0, $"Imported M140 Windows report failed: {string.Join("; ", report.failures ?? Array.Empty<string>())}");
                }

                return PlatformBuildTargetResult.Passed("windows-runtime-artifact-import", "windows-x64", reportPath, 0, "Imported M140 Windows report passed.");
            }
            catch (Exception exception)
            {
                return PlatformBuildTargetResult.Failed("windows-runtime-artifact-import", "windows-x64", reportPath, 0, exception.Message);
            }
        }
    }
}
