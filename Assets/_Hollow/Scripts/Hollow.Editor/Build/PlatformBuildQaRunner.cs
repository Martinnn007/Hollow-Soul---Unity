using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Hollow.Data.Definitions;
using Hollow.Diagnostics;
using Hollow.Editor.Generation;
using Hollow.Editor.Validation;
using Hollow.Presentation;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace Hollow.Editor.Build
{
    public static class PlatformBuildQaRunner
    {
        [MenuItem("Hollow/Platform QA/Run Full M24 QA Gate")]
        public static void RunFullM24QaMenu()
        {
            RunFullM24Qa();
        }

        [MenuItem("Hollow/Platform QA/Build Windows Development M24")]
        public static void BuildWindowsDevelopmentMenu()
        {
            var result = BuildWindowsDevelopment();
            LogTargetResult(result);
        }

        [MenuItem("Hollow/Platform QA/Validate Vision Pro Readiness")]
        public static void ValidateVisionOSReadinessMenu()
        {
            var result = ValidateVisionOSReadiness();
            LogTargetResult(result);
        }

        public static PlatformBuildQaReport RunFullM24Qa()
        {
            ConfigureQaEditorRuntime();
            var profile = LoadProfileOrThrow();
            var report = CreateReport(profile);

            report.targets.Add(RunMilestoneAudit(profile));
            if (profile.RequireAddressablesBuild)
            {
                report.targets.Add(BuildLocalAddressables());
            }

            report.targets.Add(RunEditModeTests(profile));
            report.targets.Add(RunPlatformSceneSmoke(profile));
            report.targets.Add(BuildWindowsDevelopment(profile));
            report.targets.Add(ValidateVisionOSReadiness(profile));
            report.manualChecklist.AddRange(ManualChecklist());
            report.Recalculate(profile.AllowEnvironmentBlocks);
            WriteReports(profile, report);
            LogReport(report);
            return report;
        }

        private static void ConfigureQaEditorRuntime()
        {
            TryDisableBurstCompilation();
        }

        private static void TryDisableBurstCompilation()
        {
            try
            {
                var burstCompiler = Type.GetType("Unity.Burst.BurstCompiler, Unity.Burst");
                var options = burstCompiler?.GetField("Options", BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
                var enableProperty = options?.GetType().GetProperty("EnableBurstCompilation", BindingFlags.Public | BindingFlags.Instance);
                if (enableProperty != null && enableProperty.CanWrite)
                {
                    enableProperty.SetValue(options, false);
                }
            }
            catch (Exception exception)
            {
                UnityEngine.Debug.LogWarning($"M32 QA could not disable Burst compilation for batch stability: {exception.Message}");
            }
        }

        public static PlatformBuildTargetResult BuildWindowsDevelopment()
        {
            return BuildWindowsDevelopment(LoadProfileOrThrow());
        }

        public static PlatformBuildTargetResult ValidateVisionOSReadiness()
        {
            return ValidateVisionOSReadiness(LoadProfileOrThrow());
        }

        public static PlatformBuildQaProfileDefinition LoadProfileOrThrow()
        {
            var profile = AssetDatabase.LoadAssetAtPath<PlatformBuildQaProfileDefinition>(Milestone24AssetGenerator.PlatformBuildQaProfilePath);
            if (profile == null)
            {
                throw new FileNotFoundException($"Missing M24 platform QA profile at {Milestone24AssetGenerator.PlatformBuildQaProfilePath}. Run Hollow/Generation/Generate Milestone 24 Assets.");
            }

            return profile;
        }

        public static string PlannedWindowsBuildPath(PlatformBuildQaProfileDefinition profile)
        {
            var folder = Path.Combine(profile.BuildRoot, profile.WindowsBuildFolderName);
            return Path.Combine(folder, $"{profile.WindowsExecutableName}.exe");
        }

        private static PlatformBuildQaReport CreateReport(PlatformBuildQaProfileDefinition profile)
        {
            return new PlatformBuildQaReport
            {
                reportId = $"platform-build-qa-{DateTime.UtcNow:yyyyMMdd-HHmmss}",
                generatedAtUtc = DateTime.UtcNow.ToString("O"),
                prototypeVersion = profile.PrototypeVersion,
                unityVersion = Application.unityVersion,
                gitBranch = BuildManifestWriter.ReadGitValue("rev-parse --abbrev-ref HEAD"),
                gitCommit = BuildManifestWriter.ReadGitValue("rev-parse --short HEAD"),
                reportRoot = profile.ReportRoot,
                buildRoot = profile.BuildRoot
            };
        }

        private static PlatformBuildTargetResult RunMilestoneAudit(PlatformBuildQaProfileDefinition profile)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                var auditProfile = ScriptableObject.CreateInstance<BuildAutomationProfileDefinition>();
                auditProfile.Configure(
                    profile.PrototypeVersion,
                    profile.BuildRoot,
                    profile.ReportRoot,
                    profile.WindowsBuildFolderName,
                    profile.WindowsExecutableName,
                    "latest_m24_dependency_audit.json",
                    "latest_m24_dependency_audit.md",
                    "latest_m24_dependency_manifest.json",
                    nextRequirePassingAuditBeforeBuild: true,
                    profile.RequiredScenes,
                    profile.ValidationTypes);
                var audit = PrototypeAuditRunner.RunFullAudit(auditProfile, writeReports: true);
                UnityEngine.Object.DestroyImmediate(auditProfile);
                stopwatch.Stop();
                if (audit.Passed)
                {
                    return PlatformBuildTargetResult.Passed("m0-m23-audit", "Editor", Path.Combine(profile.ReportRoot, "latest_m24_dependency_audit.json"), stopwatch.Elapsed.TotalMilliseconds, $"M0-M23 audit passed: {audit.passedChecks}/{audit.totalChecks}.");
                }

                return PlatformBuildTargetResult.Failed("m0-m23-audit", "Editor", Path.Combine(profile.ReportRoot, "latest_m24_dependency_audit.json"), stopwatch.Elapsed.TotalMilliseconds, $"M0-M23 audit failed: {audit.failedChecks}/{audit.totalChecks}.", "Open latest_m24_dependency_audit.md for failing validator details.");
            }
            catch (Exception exception)
            {
                stopwatch.Stop();
                return PlatformBuildTargetResult.Failed("m0-m23-audit", "Editor", string.Empty, stopwatch.Elapsed.TotalMilliseconds, exception.Message, "Regenerate M24 assets and rerun the milestone validators.");
            }
        }

        private static PlatformBuildTargetResult BuildLocalAddressables()
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                var settings = AddressableAssetSettingsDefaultObject.GetSettings(create: false);
                if (settings == null)
                {
                    stopwatch.Stop();
                    return PlatformBuildTargetResult.Failed("addressables-build", "Local Addressables", string.Empty, stopwatch.Elapsed.TotalMilliseconds, "Addressables settings are missing.", "Run M23/M24 asset generation to recreate Addressables settings.");
                }

                AddressableAssetSettings.BuildPlayerContent(out AddressablesPlayerBuildResult result);
                stopwatch.Stop();
                if (!string.IsNullOrWhiteSpace(result.Error))
                {
                    return PlatformBuildTargetResult.Failed("addressables-build", "Local Addressables", result.OutputPath, stopwatch.Elapsed.TotalMilliseconds, result.Error, "Inspect Addressables groups, labels, and catalog references.");
                }

                return PlatformBuildTargetResult.Passed("addressables-build", "Local Addressables", result.OutputPath, stopwatch.Elapsed.TotalMilliseconds, "Local Addressables content built successfully.");
            }
            catch (Exception exception)
            {
                stopwatch.Stop();
                return PlatformBuildTargetResult.Failed("addressables-build", "Local Addressables", string.Empty, stopwatch.Elapsed.TotalMilliseconds, exception.Message, "Inspect Addressables package installation and generated group configuration.");
            }
        }

        private static PlatformBuildTargetResult BuildWindowsDevelopment(PlatformBuildQaProfileDefinition profile)
        {
            var stopwatch = Stopwatch.StartNew();
            var outputPath = PlannedWindowsBuildPath(profile);
            try
            {
                if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows64))
                {
                    stopwatch.Stop();
                    return PlatformBuildTargetResult.BlockedByEnvironment(
                        "windows-development-build",
                        "StandaloneWindows64",
                        outputPath,
                        stopwatch.Elapsed.TotalMilliseconds,
                        "Standalone Windows 64-bit build support is not installed in this Unity editor.",
                        "Install Unity Windows Build Support for Unity 6000.4.1f1.",
                        "Rerun Hollow/Platform QA/Build Windows Development M24.");
                }

                Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
                var buildReport = BuildPipeline.BuildPlayer(new BuildPlayerOptions
                {
                    scenes = profile.RequiredScenes,
                    locationPathName = outputPath,
                    target = BuildTarget.StandaloneWindows64,
                    options = BuildOptions.Development
                });
                stopwatch.Stop();

                if (buildReport.summary.result == BuildResult.Succeeded)
                {
                    return PlatformBuildTargetResult.Passed("windows-development-build", "StandaloneWindows64", outputPath, stopwatch.Elapsed.TotalMilliseconds, $"Windows development build succeeded: {buildReport.summary.totalSize} bytes.");
                }

                return PlatformBuildTargetResult.Failed("windows-development-build", "StandaloneWindows64", outputPath, stopwatch.Elapsed.TotalMilliseconds, $"Windows development build failed: {buildReport.summary.result}.", "Open the Unity editor log for BuildPipeline errors.");
            }
            catch (Exception exception) when (LooksLikeEnvironmentBlock(exception))
            {
                stopwatch.Stop();
                return PlatformBuildTargetResult.BlockedByEnvironment("windows-development-build", "StandaloneWindows64", outputPath, stopwatch.Elapsed.TotalMilliseconds, exception.Message, "Install or repair Windows Build Support for the active Unity editor.");
            }
            catch (Exception exception)
            {
                stopwatch.Stop();
                return PlatformBuildTargetResult.Failed("windows-development-build", "StandaloneWindows64", outputPath, stopwatch.Elapsed.TotalMilliseconds, exception.Message, "Inspect build scenes, scripts, Addressables, and player settings.");
            }
        }

        private static PlatformBuildTargetResult ValidateVisionOSReadiness(PlatformBuildQaProfileDefinition profile)
        {
            var stopwatch = Stopwatch.StartNew();
            var failures = new StringBuilder();
            var remediation = new StringBuilder();
            var blocked = false;

            ValidateScene(profile, "Assets/_Hollow/Scenes/Game_VisionOS_Bounded.unity", failures);
            ValidateScene(profile, "Assets/_Hollow/Scenes/Game_VisionOS_Immersive.unity", failures);
            ValidateFile(Milestone10AssetGenerator.BoundedProfilePath, failures);
            ValidateFile(Milestone10AssetGenerator.ImmersiveProfilePath, failures);

            if (!Enum.GetNames(typeof(BuildTargetGroup)).Contains("VisionOS") || !Enum.GetNames(typeof(BuildTarget)).Contains("VisionOS"))
            {
                blocked = true;
                remediation.AppendLine("Use a Unity editor with visionOS build target support installed.");
            }

            if (!CanRunProcess("xcrun", "simctl help"))
            {
                blocked = true;
                remediation.AppendLine("Install Xcode command line tools and verify `xcrun simctl` works for simulator QA.");
            }

            stopwatch.Stop();
            if (failures.Length > 0)
            {
                return PlatformBuildTargetResult.Failed("visionos-readiness", "visionOS Simulator/Readiness", string.Empty, stopwatch.Elapsed.TotalMilliseconds, failures.ToString().Trim(), "Regenerate M10-M24 assets and ensure both Vision Pro scenes/profiles are enabled.");
            }

            if (blocked)
            {
                return PlatformBuildTargetResult.BlockedByEnvironment("visionos-readiness", "visionOS Simulator/Readiness", string.Empty, stopwatch.Elapsed.TotalMilliseconds, "Vision Pro project readiness is present, but local simulator/build tooling is incomplete.", remediation.ToString().Trim());
            }

            return PlatformBuildTargetResult.Passed("visionos-readiness", "visionOS Simulator/Readiness", string.Empty, stopwatch.Elapsed.TotalMilliseconds, "Vision Pro bounded/immersive scenes, polish profiles, and simulator tooling are present.");
        }

        private static PlatformBuildTargetResult RunEditModeTests(PlatformBuildQaProfileDefinition profile)
        {
            var stopwatch = Stopwatch.StartNew();
            var outputPath = Path.Combine(profile.ReportRoot, "m24-editmode-results.xml");
            try
            {
                Directory.CreateDirectory(profile.ReportRoot);
                var callbacks = ScriptableObject.CreateInstance<QaTestCallbacks>();
                callbacks.Configure(outputPath);
                var api = ScriptableObject.CreateInstance<TestRunnerApi>();
                api.RegisterCallbacks(callbacks, priority: 1000);
                var settings = new ExecutionSettings(new Filter
                {
                    testMode = UnityEditor.TestTools.TestRunner.Api.TestMode.EditMode,
                    assemblyNames = new[] { "Hollow.Tests.EditMode" }
                })
                {
                    runSynchronously = true
                };

                var previousExitSuppression = MilestoneValidationExitPolicy.SuppressEditorExit;
                MilestoneValidationExitPolicy.SuppressEditorExit = true;
                try
                {
                    api.Execute(settings);
                }
                finally
                {
                    MilestoneValidationExitPolicy.SuppressEditorExit = previousExitSuppression;
                    api.UnregisterCallbacks(callbacks);
                    UnityEngine.Object.DestroyImmediate(api);
                }

                var result = callbacks.Result;
                UnityEngine.Object.DestroyImmediate(callbacks);
                stopwatch.Stop();

                if (result == null)
                {
                    return PlatformBuildTargetResult.Failed(
                        "editmode-tests",
                        "Unity Test Runner",
                        outputPath,
                        stopwatch.Elapsed.TotalMilliseconds,
                        "Unity Test Runner returned no EditMode result.",
                        "Open the editor log and verify com.unity.test-framework can execute Hollow.Tests.EditMode.");
                }

                var total = result.PassCount + result.FailCount + result.SkipCount + result.InconclusiveCount;
                var summary = $"EditMode tests completed: {result.PassCount}/{total} passed, {result.FailCount} failed, {result.InconclusiveCount} inconclusive, {result.SkipCount} skipped.";
                if (result.FailCount > 0 || result.InconclusiveCount > 0 || result.TestStatus == TestStatus.Failed)
                {
                    return PlatformBuildTargetResult.Failed(
                        "editmode-tests",
                        "Unity Test Runner",
                        outputPath,
                        stopwatch.Elapsed.TotalMilliseconds,
                        summary,
                        "Open m24-editmode-results.xml and fix failing EditMode tests.");
                }

                return PlatformBuildTargetResult.Passed("editmode-tests", "Unity Test Runner", outputPath, stopwatch.Elapsed.TotalMilliseconds, summary);
            }
            catch (Exception exception)
            {
                stopwatch.Stop();
                return PlatformBuildTargetResult.Failed(
                    "editmode-tests",
                    "Unity Test Runner",
                    outputPath,
                    stopwatch.Elapsed.TotalMilliseconds,
                    exception.Message,
                    "Verify Hollow.Editor references UnityEditor.TestRunner and rerun M32/M24 QA.");
            }
        }

        private static PlatformBuildTargetResult RunPlatformSceneSmoke(PlatformBuildQaProfileDefinition profile)
        {
            var stopwatch = Stopwatch.StartNew();
            var outputPath = Path.Combine(profile.ReportRoot, "m24-playmode-smoke-editor-probe.md");
            var failures = new StringBuilder();
            var report = new StringBuilder();
            try
            {
                Directory.CreateDirectory(profile.ReportRoot);
                ValidateQaProbeScene("Assets/_Hollow/Scenes/MainMenu.unity", "MainMenu", expectedScale: null, expectMainMenu: true, expectDesigner: false, expectGame: false, failures, report);
                ValidateQaProbeScene("Assets/_Hollow/Scenes/RoomDesigner.unity", "RoomDesigner", expectedScale: null, expectMainMenu: false, expectDesigner: true, expectGame: false, failures, report);
                ValidateQaProbeScene("Assets/_Hollow/Scenes/Game_Windows.unity", "Game_Windows", 1f, expectMainMenu: false, expectDesigner: false, expectGame: true, failures, report);
                ValidateQaProbeScene("Assets/_Hollow/Scenes/Game_VisionOS_Bounded.unity", "Game_VisionOS_Bounded", PresentationScalePolicy.VisionOSBoundedTabletopScale, expectMainMenu: false, expectDesigner: false, expectGame: true, failures, report);
                ValidateQaProbeScene("Assets/_Hollow/Scenes/Game_VisionOS_Immersive.unity", "Game_VisionOS_Immersive", 1f, expectMainMenu: false, expectDesigner: false, expectGame: true, failures, report);
                File.WriteAllText(outputPath, report.ToString());
                stopwatch.Stop();

                if (failures.Length > 0)
                {
                    return PlatformBuildTargetResult.Failed(
                        "playmode-smoke-tests",
                        "Editor Scene Smoke",
                        outputPath,
                        stopwatch.Elapsed.TotalMilliseconds,
                        failures.ToString().Trim(),
                        "Open m24-playmode-smoke-editor-probe.md and repair scene/root/HUD regressions.");
                }

                return PlatformBuildTargetResult.Passed("playmode-smoke-tests", "Editor Scene Smoke", outputPath, stopwatch.Elapsed.TotalMilliseconds, "Platform scene smoke probe passed for MainMenu, RoomDesigner, Windows, VisionOS bounded, and VisionOS immersive.");
            }
            catch (Exception exception)
            {
                stopwatch.Stop();
                return PlatformBuildTargetResult.Failed(
                    "playmode-smoke-tests",
                    "Editor Scene Smoke",
                    outputPath,
                    stopwatch.Elapsed.TotalMilliseconds,
                    exception.Message,
                    "Verify required scenes are present and can be opened in the editor.");
            }
        }

        private static void ValidateQaProbeScene(
            string scenePath,
            string sceneName,
            float? expectedScale,
            bool expectMainMenu,
            bool expectDesigner,
            bool expectGame,
            StringBuilder failures,
            StringBuilder report)
        {
            if (!File.Exists(scenePath))
            {
                failures.AppendLine($"Missing scene: {scenePath}");
                return;
            }

            EditorSceneManager.OpenScene(scenePath);
            var snapshot = PlatformRuntimeQaProbe.CaptureCurrentScene();
            report.AppendLine($"## {sceneName}");
            report.AppendLine($"- Scene: {snapshot.sceneName}");
            report.AppendLine($"- World scale: {snapshot.worldScale:0.###}");
            report.AppendLine($"- HUD outside world root: {snapshot.hudOutsideWorldRoot}");
            report.AppendLine($"- Presentation catalog: {snapshot.hasPresentationCatalog}");
            report.AppendLine();

            if (snapshot.sceneName != sceneName)
            {
                failures.AppendLine($"{sceneName} smoke expected active scene '{sceneName}' but captured '{snapshot.sceneName}'.");
            }

            if (expectMainMenu && !snapshot.hasMainMenuController)
            {
                failures.AppendLine($"{sceneName} smoke missing MainMenuController.");
            }

            if (expectDesigner && !snapshot.hasRoomDesignerController)
            {
                failures.AppendLine($"{sceneName} smoke missing RoomDesignerController.");
            }

            if (expectGame)
            {
                if (!snapshot.hasGameSessionController || !snapshot.hasRoomRuntimeRoot || !snapshot.hasPresentationRoot || !snapshot.hasPlatformShellCanvas)
                {
                    failures.AppendLine($"{sceneName} smoke missing game session, room runtime, presentation root, or platform shell canvas.");
                }

                if (!snapshot.hudOutsideWorldRoot)
                {
                    failures.AppendLine($"{sceneName} smoke detected HUD/shell under the scaled world root.");
                }
            }

            if (expectedScale.HasValue && Mathf.Abs(snapshot.worldScale - expectedScale.Value) > 0.001f)
            {
                failures.AppendLine($"{sceneName} smoke expected world scale {expectedScale.Value:0.###} but captured {snapshot.worldScale:0.###}.");
            }

            if (!snapshot.hasPresentationCatalog)
            {
                failures.AppendLine($"{sceneName} smoke missing PresentationContentCatalog.");
            }
        }

        private static void ValidateScene(PlatformBuildQaProfileDefinition profile, string scenePath, StringBuilder failures)
        {
            if (!File.Exists(scenePath))
            {
                failures.AppendLine($"Missing scene: {scenePath}");
            }

            if (!profile.RequiredScenes.Contains(scenePath))
            {
                failures.AppendLine($"M24 profile does not include scene: {scenePath}");
            }

            if (!EditorBuildSettings.scenes.Any(scene => scene.enabled && scene.path == scenePath))
            {
                failures.AppendLine($"Scene is not enabled in build settings: {scenePath}");
            }
        }

        private static void ValidateFile(string path, StringBuilder failures)
        {
            if (!File.Exists(path))
            {
                failures.AppendLine($"Missing file: {path}");
            }
        }

        private static bool CanRunProcess(string fileName, string arguments)
        {
            try
            {
                using var process = new Process();
                process.StartInfo = new ProcessStartInfo(fileName, arguments)
                {
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };
                process.Start();
                return process.WaitForExit(2500) && process.ExitCode == 0;
            }
            catch
            {
                return false;
            }
        }

        private static bool LooksLikeEnvironmentBlock(Exception exception)
        {
            var message = exception.Message.ToLowerInvariant();
            return message.Contains("module") || message.Contains("build support") || message.Contains("not installed") || message.Contains("unsupported");
        }

        private static string[] ManualChecklist()
        {
            return new[]
            {
                "Windows: launch HollowSoul.exe, create/select profile, start New Run, move/shoot/clear one room, traverse a door, buy a shop card, quit and Continue.",
                "Windows: open Room Designer, create a 1x1 draft, move cursor, place/erase a rock/enemy marker, export JSON/USDA bundle.",
                "Vision Pro bounded: verify tabletop world scale is 0.1, HUD/minimap are readable and unscaled, and ArtPass visuals do not add gameplay colliders.",
                "Vision Pro immersive: verify full-scale world, comfort vignette profile metadata, camera posture, and readable combat spacing.",
                "All platforms: confirm save/profile state changes only occur in profile-backed sessions and transient designer/sample sessions stay safe."
            };
        }

        public static void WriteReports(PlatformBuildQaProfileDefinition profile, PlatformBuildQaReport report)
        {
            Directory.CreateDirectory(profile.ReportRoot);
            Directory.CreateDirectory(profile.BuildRoot);
            var json = JsonUtility.ToJson(report, prettyPrint: true);
            File.WriteAllText(Path.Combine(profile.ReportRoot, profile.LatestQaJsonFileName), json);
            File.WriteAllText(Path.Combine(profile.ReportRoot, $"{report.reportId}.json"), json);
            var markdown = ToMarkdown(report);
            File.WriteAllText(Path.Combine(profile.ReportRoot, profile.LatestQaMarkdownFileName), markdown);
            File.WriteAllText(Path.Combine(profile.ReportRoot, $"{report.reportId}.md"), markdown);
            AssetDatabase.Refresh();
        }

        private static string ToMarkdown(PlatformBuildQaReport report)
        {
            var builder = new StringBuilder();
            builder.AppendLine("# Hollow Platform Build QA");
            builder.AppendLine();
            builder.AppendLine($"- Result: {report.result}");
            builder.AppendLine($"- Generated: {report.generatedAtUtc}");
            builder.AppendLine($"- Unity: {report.unityVersion}");
            builder.AppendLine($"- Git: {report.gitBranch} @ {report.gitCommit}");
            builder.AppendLine($"- Build root: `{report.buildRoot}`");
            builder.AppendLine();
            builder.AppendLine("| Target | Platform | Result | Output | Notes | Remediation |");
            builder.AppendLine("| --- | --- | --- | --- | --- | --- |");
            foreach (var target in report.targets)
            {
                builder.AppendLine($"| {target.id} | {target.platform} | {target.result} | `{target.outputPath}` | {Join(target.messages)} | {Join(target.remediation)} |");
            }

            builder.AppendLine();
            builder.AppendLine("## Manual Device Checklist");
            foreach (var item in report.manualChecklist)
            {
                builder.AppendLine($"- {item}");
            }

            return builder.ToString();
        }

        private static string Join(System.Collections.Generic.IReadOnlyList<string> values)
        {
            return values == null || values.Count == 0 ? "OK" : string.Join("<br>", values.Select(value => value.Replace("|", "\\|")));
        }

        private static void LogReport(PlatformBuildQaReport report)
        {
            if (report.result == PlatformBuildQaResult.Failed)
            {
                UnityEngine.Debug.LogError($"M24 platform QA failed: {report.reportId}");
                return;
            }

            UnityEngine.Debug.Log($"M24 platform QA completed: {report.result} ({report.reportId})");
        }

        private static void LogTargetResult(PlatformBuildTargetResult result)
        {
            if (result.result == PlatformBuildQaResult.Failed)
            {
                UnityEngine.Debug.LogError($"{result.id}: {result.result} - {string.Join("; ", result.messages)}");
            }
            else
            {
                UnityEngine.Debug.Log($"{result.id}: {result.result} - {string.Join("; ", result.messages)}");
            }
        }

        private sealed class QaTestCallbacks : ScriptableObject, ICallbacks
        {
            private string outputPath = string.Empty;

            public ITestResultAdaptor Result { get; private set; }

            public void Configure(string nextOutputPath)
            {
                outputPath = nextOutputPath ?? string.Empty;
            }

            public void RunStarted(ITestAdaptor testsToRun)
            {
            }

            public void RunFinished(ITestResultAdaptor result)
            {
                Result = result;
                if (!string.IsNullOrWhiteSpace(outputPath))
                {
                    TestRunnerApi.SaveResultToFile(result, outputPath);
                }
            }

            public void TestStarted(ITestAdaptor test)
            {
            }

            public void TestFinished(ITestResultAdaptor result)
            {
            }
        }
    }
}
