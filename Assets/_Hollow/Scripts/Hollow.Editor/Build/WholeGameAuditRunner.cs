using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Hollow.Combat;
using Hollow.Core.App;
using Hollow.Data.Definitions;
using Hollow.Editor.Generation;
using Hollow.Editor.Validation;
using Hollow.Input;
using Hollow.Persistence;
using Hollow.Platform;
using Hollow.Performance;
using Hollow.Presentation;
using Hollow.Rooms;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace Hollow.Editor.Build
{
    public static class WholeGameAuditRunner
    {
        private const string ReportRoot = "output/reports";
        private const string LatestJsonReportName = "latest_whole_game_audit.json";
        private const string LatestMarkdownReportName = "latest_whole_game_audit.md";
        private const string EnemyCatalogPath = "Assets/_Hollow/Data/Enemies/EnemyCatalog.asset";
        private const string BoundedVolumeCameraConfigPath = "Assets/Resources/Hollow_VisionOS_BoundedVolumeCamera.asset";
        private const string ImmersiveVolumeCameraConfigPath = "Assets/Resources/Hollow_VisionOS_ImmersiveVolumeCamera.asset";
        private const string BoundedPlatformPolishPath = "Assets/_Hollow/Data/Platform/Polish/PlatformPolish_VisionOSBoundedTabletop.asset";
        private const string BoundedSourceDimensionLine = "m_Dimensions: {x: 8, y: 5.333333, z: 8}";

        private static readonly Dictionary<AppShellRoute, string> RouteScenePaths = new()
        {
            { AppShellRoute.Boot, "Assets/_Hollow/Scenes/Boot.unity" },
            { AppShellRoute.MainMenu, "Assets/_Hollow/Scenes/MainMenu.unity" },
            { AppShellRoute.MainMenuVisionOS, "Assets/_Hollow/Scenes/MainMenu_VisionOS.unity" },
            { AppShellRoute.GameWindows, "Assets/_Hollow/Scenes/Game_Windows.unity" },
            { AppShellRoute.GameVisionOSBounded, "Assets/_Hollow/Scenes/Game_VisionOS_Bounded.unity" },
            { AppShellRoute.GameVisionOSImmersive, "Assets/_Hollow/Scenes/Game_VisionOS_Immersive.unity" },
            { AppShellRoute.SpaceshipWindows, "Assets/_Hollow/Scenes/Spaceship_Windows.unity" },
            { AppShellRoute.SpaceshipVisionOSBounded, "Assets/_Hollow/Scenes/Spaceship_VisionOS_Bounded.unity" },
            { AppShellRoute.SpaceshipVisionOSImmersive, "Assets/_Hollow/Scenes/Spaceship_VisionOS_Immersive.unity" },
            { AppShellRoute.RoomDesigner, "Assets/_Hollow/Scenes/RoomDesigner.unity" },
            { AppShellRoute.ArenaMode, "Assets/_Hollow/Scenes/ArenaMode/ArenaMode.unity" },
            { AppShellRoute.DeveloperSandbox, "Assets/_Hollow/Scenes/DeveloperSandbox.unity" }
        };

        private static readonly AppShellRoute[] RequiredRuntimeRoutes =
        {
            AppShellRoute.Boot,
            AppShellRoute.MainMenu,
            AppShellRoute.MainMenuVisionOS,
            AppShellRoute.GameWindows,
            AppShellRoute.GameVisionOSBounded,
            AppShellRoute.GameVisionOSImmersive,
            AppShellRoute.SpaceshipWindows,
            AppShellRoute.SpaceshipVisionOSBounded,
            AppShellRoute.SpaceshipVisionOSImmersive,
            AppShellRoute.RoomDesigner,
            AppShellRoute.ArenaMode
        };

        public static IReadOnlyList<int> MilestoneNumbers { get; } = Enumerable.Range(116, 10).ToArray();

        [MenuItem("Hollow/Audit/Run Whole Game Audit M116-M125")]
        public static void RunWholeGameAuditMenu()
        {
            var report = RunAudit(writeReports: true, strictReleaseGate: false);
            Debug.Log($"Whole-game audit {report.result}: {report.blockerCount} blockers, {report.warningCount} warnings. Report: {Path.Combine(ReportRoot, LatestMarkdownReportName)}");
        }

        [MenuItem("Hollow/Audit/Run Whole Game Release Gate M125")]
        public static void RunWholeGameReleaseGateMenu()
        {
            var report = RunAudit(writeReports: true, strictReleaseGate: true);
            var log = $"Whole-game release gate {report.result}: {report.blockerCount} blockers, {report.warningCount} warnings. Report: {Path.Combine(ReportRoot, LatestMarkdownReportName)}";
            if (report.Passed)
            {
                Debug.Log(log);
            }
            else
            {
                Debug.LogError(log);
            }
        }

        [MenuItem("Hollow/Audit/Run Focused Whole Game Audit Tests")]
        public static void RunFocusedWholeGameAuditTestsMenu()
        {
            var result = RunFocusedWholeGameAuditTests(Path.Combine(ReportRoot, "whole-game-audit-focused-tests.xml"));
            var message = $"Focused whole-game audit tests: {result.passCount}/{result.totalCount} passed, {result.failCount} failed, {result.inconclusiveCount} inconclusive, {result.skipCount} skipped. Results: {result.outputPath}";
            if (result.Passed)
            {
                Debug.Log(message);
            }
            else
            {
                Debug.LogError(message);
            }
        }

        public static void RunFocusedWholeGameAuditTestsBatch()
        {
            var result = RunFocusedWholeGameAuditTests(Path.Combine(ReportRoot, "whole-game-audit-focused-tests.xml"));
            var message = $"Focused whole-game audit tests: {result.passCount}/{result.totalCount} passed, {result.failCount} failed, {result.inconclusiveCount} inconclusive, {result.skipCount} skipped. Results: {result.outputPath}";
            if (result.Passed)
            {
                Debug.Log(message);
            }
            else
            {
                Debug.LogError(message);
            }

            if (Application.isBatchMode)
            {
                EditorApplication.Exit(result.Passed ? 0 : 1);
            }
        }

        public static WholeGameAuditReport RunAudit(bool writeReports = false, bool strictReleaseGate = false)
        {
            var report = new WholeGameAuditReport
            {
                auditId = $"whole-game-audit-{DateTime.UtcNow:yyyyMMdd-HHmmss}",
                generatedAtUtc = DateTime.UtcNow.ToString("O"),
                unityVersion = Application.unityVersion,
                gitBranch = BuildManifestWriter.ReadGitValue("rev-parse --abbrev-ref HEAD"),
                gitCommit = BuildManifestWriter.ReadGitValue("rev-parse --short HEAD"),
                strictReleaseGate = strictReleaseGate,
                milestones = BuildMilestones().ToList()
            };

            RunCheck(report, 116, AuditBaselineIntegrity);
            RunCheck(report, 117, AuditBootRoutingAndMenuLaunch);
            RunCheck(report, 118, AuditInputAndControllerReliability);
            RunCheck(report, 119, AuditSaveProfileAndRunPersistence);
            RunCheck(report, 120, AuditSceneAndPlatformPresentation);
            RunCheck(report, 121, AuditRoomBranchAndNavMesh);
            RunCheck(report, 122, AuditCombatLoopCorrectness);
            RunCheck(report, 123, AuditEnemyAiAndEncounters);
            RunCheck(report, 124, AuditArtPassMeshyAndVisualSafety);
            RunCheck(report, 125, AuditBuildPerformanceAndReleaseGate);

            report.Recalculate();
            if (writeReports)
            {
                WriteReports(report);
            }

            return report;
        }

        public static FocusedAuditTestRunResult RunFocusedWholeGameAuditTests(string outputPath)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath) ?? ReportRoot);

            var callbacks = ScriptableObject.CreateInstance<FocusedAuditTestCallbacks>();
            callbacks.Configure(outputPath);
            var api = ScriptableObject.CreateInstance<TestRunnerApi>();
            api.RegisterCallbacks(callbacks, priority: 1000);

            var previousExitSuppression = MilestoneValidationExitPolicy.SuppressEditorExit;
            MilestoneValidationExitPolicy.SuppressEditorExit = true;
            try
            {
                var settings = new ExecutionSettings(new Filter
                {
                    testMode = TestMode.EditMode,
                    assemblyNames = new[] { "Hollow.Tests.EditMode" },
                    groupNames = new[]
                    {
                        "^Hollow\\.Tests\\.EditMode\\.(WholeGameAuditTests|Milestone7RunEconomyPersistenceTests)(\\.|$)"
                    }
                })
                {
                    runSynchronously = true
                };

                api.Execute(settings);
                var result = FocusedAuditTestRunResult.From(callbacks.Result, outputPath);
                return result;
            }
            finally
            {
                MilestoneValidationExitPolicy.SuppressEditorExit = previousExitSuppression;
                api.UnregisterCallbacks(callbacks);
                UnityEngine.Object.DestroyImmediate(api);
                UnityEngine.Object.DestroyImmediate(callbacks);
            }
        }

        public static IReadOnlyList<string> FindSerializedMissingScriptMarkersForTests(IEnumerable<string> serializedAssetPaths)
        {
            return FindSerializedMissingScriptMarkers(serializedAssetPaths).ToArray();
        }

        public static string ToMarkdown(WholeGameAuditReport report)
        {
            report?.Recalculate();
            var builder = new StringBuilder(4096);
            builder.AppendLine("# Whole Game Audit M116-M125");
            builder.AppendLine();
            builder.AppendLine($"- Result: {report?.result ?? "NotRun"}");
            builder.AppendLine($"- Generated: {report?.generatedAtUtc ?? string.Empty}");
            builder.AppendLine($"- Unity: {report?.unityVersion ?? string.Empty}");
            builder.AppendLine($"- Git: {report?.gitBranch ?? "unknown"} @ {report?.gitCommit ?? "unknown"}");
            builder.AppendLine($"- Strict release gate: {(report?.strictReleaseGate == true ? "yes" : "no")}");
            builder.AppendLine($"- Findings: {report?.totalFindings ?? 0} total, {report?.blockerCount ?? 0} blockers, {report?.warningCount ?? 0} warnings, {report?.infoCount ?? 0} info");
            builder.AppendLine();

            foreach (var milestone in report?.milestones ?? new List<WholeGameAuditMilestone>())
            {
                builder.AppendLine($"## M{milestone.milestone} {milestone.title}");
                builder.AppendLine();
                builder.AppendLine($"- Goal: {milestone.goal}");
                builder.AppendLine($"- Subsystem: {milestone.primarySubsystem}");
                builder.AppendLine($"- Default solution: {milestone.defaultSolution}");
                builder.AppendLine($"- Counts: {milestone.blockerCount} blockers, {milestone.warningCount} warnings, {milestone.infoCount} info");
                builder.AppendLine();

                var findings = report.findings
                    .Where(finding => finding.milestone == milestone.milestone)
                    .OrderByDescending(finding => finding.Severity)
                    .ThenBy(finding => finding.category, StringComparer.Ordinal)
                    .ToArray();
                if (findings.Length == 0)
                {
                    builder.AppendLine("No findings.");
                    builder.AppendLine();
                    continue;
                }

                foreach (var finding in findings)
                {
                    builder.AppendLine($"- [{finding.severity}] {finding.title}");
                    builder.AppendLine($"  - Category: {finding.category}");
                    builder.AppendLine($"  - Location: {finding.location}");
                    builder.AppendLine($"  - Detail: {finding.message}");
                    builder.AppendLine($"  - Solution: {finding.solution}");
                }

                builder.AppendLine();
            }

            return builder.ToString();
        }

        private static IEnumerable<WholeGameAuditMilestone> BuildMilestones()
        {
            yield return Milestone(116, "Baseline Integrity Audit", "Find compile-adjacent data failures before play.", "Scenes, prefabs, catalogs, generated assets", "Fix missing scripts/references, regenerate stale content, and classify dev-only warnings.");
            yield return Milestone(117, "Boot Routing And Menu Launch Reliability", "Keep every public launch and return path deterministic.", "Boot, AppStateMachine, SceneLoaderService, menus", "Centralize route assertions and make direct scene loads visible.");
            yield return Milestone(118, "Input And Controller Reliability", "Make gameplay input robust across keyboard, gamepad, joystick, and visionOS devices.", "GameplayInputReader, Input System diagnostics", "Prefer gamepad-first sampling, strongest-stick selection, fallbacks, and diagnostics.");
            yield return Milestone(119, "Save Profile Run Persistence Safety", "Prevent profile/run state loss or corruption.", "JsonProfileStore, run snapshots, challenge records", "Use atomic saves, backups, schema normalization, and launch-state cleanup.");
            yield return Milestone(120, "Scene And Platform Presentation Stability", "Prevent platform scene drift, scale mistakes, and visionOS volume distortion.", "Platform scenes, VolumeCamera, polish assets", "Validate source/output dimensions, world scale, floor alignment, and shell placement.");
            yield return Milestone(121, "Room Branch And NavMesh Reliability", "Avoid unreachable rooms, missing bakes, invalid doors, and obstacle mismatch.", "Rooms, branches, navigation", "Gate curated content on bakes/reachability and classify authoring fallbacks.");
            yield return Milestone(122, "Combat Loop Correctness", "Lock damage, guard, roll, projectile, death, and room-clear rules.", "Combat state machines and deterministic harnesses", "Expand deterministic tests around hit windows and state transitions.");
            yield return Milestone(123, "Enemy AI And Encounter Stability", "Catch stuck enemies, invalid attacks, budget spikes, and catalog mismatch.", "Enemy catalogs, AI, encounters", "Soak representative enemies across rooms with path/LOD budget checks.");
            yield return Milestone(124, "ArtPass Meshy And Runtime Visual Safety", "Keep production visuals safe and gameplay-neutral.", "ArtPass prefabs, materials, Meshy imports", "Validate markers, bounds, texture slots, no colliders/scripts, and catalog resolution.");
            yield return Milestone(125, "Build Performance And Release Gate", "Combine audit, tests, Addressables, logs, and platform QA into the final ship gate.", "Platform QA, Addressables, build automation", "Run the strict full gate and fail on missing bakes/modules/scripts or unsafe ArtPass assets.");
        }

        private static WholeGameAuditMilestone Milestone(int number, string title, string goal, string subsystem, string solution)
        {
            return new WholeGameAuditMilestone
            {
                milestone = number,
                title = title,
                goal = goal,
                primarySubsystem = subsystem,
                defaultSolution = solution
            };
        }

        private static void RunCheck(WholeGameAuditReport report, int milestone, Action<WholeGameAuditReport> check)
        {
            try
            {
                check(report);
            }
            catch (Exception exception)
            {
                report.findings.Add(WholeGameAuditFinding.Blocker(
                    milestone,
                    "AuditRunner",
                    $"M{milestone} audit check threw",
                    exception.Message,
                    exception.GetType().Name,
                    "Fix the audit checker or the asset state that makes it throw, then rerun the whole-game audit."));
            }
        }

        private static void AuditBaselineIntegrity(WholeGameAuditReport report)
        {
            foreach (var route in RequiredRuntimeRoutes)
            {
                var scenePath = RouteScenePaths[route];
                if (!AssetPathExists(scenePath))
                {
                    report.findings.Add(WholeGameAuditFinding.Blocker(
                        116,
                        "RequiredScene",
                        $"Missing required runtime scene for {route}",
                        $"The audit expected scene '{scenePath}', but it was not found.",
                        scenePath,
                        "Regenerate the platform scenes or restore the scene asset before building."));
                }
            }

            var missingScriptPaths = FindSerializedMissingScriptMarkers(EnumerateSerializedAssetPaths()).ToArray();
            if (missingScriptPaths.Length > 0)
            {
                report.findings.Add(WholeGameAuditFinding.Blocker(
                    116,
                    "MissingScript",
                    "Serialized assets contain missing MonoBehaviour references",
                    $"Found {missingScriptPaths.Length} serialized asset(s) with missing script markers: {string.Join(", ", missingScriptPaths.Take(12))}",
                    "Assets/_Hollow",
                    "Open the listed assets, remove or restore the missing components, and add a validation test for the affected prefab or scene."));
            }
        }

        private static void AuditBootRoutingAndMenuLaunch(WholeGameAuditReport report)
        {
            foreach (AppShellRoute route in Enum.GetValues(typeof(AppShellRoute)))
            {
                var sceneName = SceneLoaderService.SceneNameForRoute(route);
                if (string.IsNullOrWhiteSpace(sceneName))
                {
                    report.findings.Add(WholeGameAuditFinding.Blocker(
                        117,
                        "SceneRoute",
                        $"Route {route} maps to an empty scene name",
                        "SceneLoaderService returned an empty route target.",
                        nameof(SceneLoaderService),
                        "Add an explicit route mapping and cover it with SceneLoaderService route tests."));
                    continue;
                }

                if (!RouteScenePaths.TryGetValue(route, out var expectedPath))
                {
                    report.findings.Add(WholeGameAuditFinding.Warning(
                        117,
                        "SceneRoute",
                        $"Route {route} has no audit path mapping",
                        $"SceneLoaderService maps {route} to '{sceneName}', but the whole-game audit does not know which scene asset should back it.",
                        nameof(RouteScenePaths),
                        "Add the route to WholeGameAuditRunner.RouteScenePaths and decide whether it is required or developer-only."));
                    continue;
                }

                if (!AssetPathExists(expectedPath))
                {
                    var severity = route == AppShellRoute.DeveloperSandbox
                        ? WholeGameAuditSeverity.Warning
                        : WholeGameAuditSeverity.Blocker;
                    AddFinding(report, 117, severity, "SceneRoute", $"Mapped scene asset is missing for {route}",
                        $"SceneLoaderService maps {route} to '{sceneName}', but '{expectedPath}' is missing.",
                        expectedPath,
                        "Restore or regenerate the mapped scene, or remove/rename the route deliberately with tests.");
                }
            }

            var visionOSRoute = BootSceneController.ResolveStartupRoute(
                RuntimePlatform.VisionOS,
                AppShellRoute.MainMenu,
                true,
                AppShellRoute.MainMenuVisionOS);
            if (visionOSRoute != AppShellRoute.MainMenuVisionOS)
            {
                report.findings.Add(WholeGameAuditFinding.Blocker(
                    117,
                    "BootRoute",
                    "visionOS boot does not route to the guided menu",
                    $"Expected {AppShellRoute.MainMenuVisionOS}, got {visionOSRoute}.",
                    nameof(BootSceneController),
                    "Set the visionOS startup route to MainMenuVisionOS and keep the boot route test locked."));
            }

            var directLoadFiles = FindDirectSceneLoadCallers().ToArray();
            if (directLoadFiles.Length > 0)
            {
                report.findings.Add(WholeGameAuditFinding.Warning(
                    117,
                    "DirectSceneLoad",
                    "Runtime scripts bypass SceneLoaderService",
                    $"Found direct SceneManager.LoadScene calls in: {string.Join(", ", directLoadFiles.Take(12))}",
                    "Assets/_Hollow/Scripts",
                    "Route scene changes through AppStateMachine and SceneLoaderService unless this is an editor-only tool."));
            }
        }

        private static void AuditInputAndControllerReliability(WholeGameAuditReport report)
        {
            var readerType = typeof(GameplayInputReader);
            foreach (var memberName in new[] { nameof(GameplayInputReader.HasKeyboardDevice), nameof(GameplayInputReader.HasGamepadDevice), nameof(GameplayInputReader.HasJoystickDevice) })
            {
                if (readerType.GetProperty(memberName) == null)
                {
                    report.findings.Add(WholeGameAuditFinding.Blocker(
                        118,
                        "InputProbe",
                        $"GameplayInputReader is missing {memberName}",
                        "The audit expects runtime-safe device availability probes for diagnostics and tests.",
                        readerType.FullName,
                        "Add the read-only helper and cover it from GameplayInputReader tests."));
                }
            }

            var deviceSummary = GameplayInputReader.DescribeConnectedInputDevices();
            if (string.IsNullOrWhiteSpace(deviceSummary))
            {
                report.findings.Add(WholeGameAuditFinding.Warning(
                    118,
                    "InputProbe",
                    "Connected input device summary is empty",
                    "Diagnostics could not describe the currently visible Input System devices.",
                    readerType.FullName,
                    "Keep DescribeConnectedInputDevices runtime-safe and include layout/display names for every connected device."));
            }

            if (FindType("Hollow.Diagnostics.VisionOSGameplayInputDiagnostics") == null)
            {
                report.findings.Add(WholeGameAuditFinding.Warning(
                    118,
                    "VisionOSInputDiagnostics",
                    "visionOS gameplay input diagnostics component is missing",
                    "Manual device/simulator runs will be harder to classify without on-screen and logged input probes.",
                    "Hollow.Diagnostics",
                    "Restore VisionOSGameplayInputDiagnostics and keep it gated to development/editor diagnostics."));
            }
        }

        private static void AuditSaveProfileAndRunPersistence(WholeGameAuditReport report)
        {
            var tempRoot = Path.Combine(Application.temporaryCachePath, "whole_game_audit_profile_store", Guid.NewGuid().ToString("N"));
            try
            {
                Directory.CreateDirectory(tempRoot);
                var store = new JsonProfileStore(tempRoot);
                var slotId = new ProfileSlotId(0);
                store.CreateOrLoadProfile(slotId, "Audit Profile");
                store.MarkRunStarted(slotId);
                store.SaveActiveRun(slotId, new RunSaveSnapshot
                {
                    runId = "audit-run",
                    currentRoomId = "audit_room",
                    playerCurrentHealth = 3,
                    economy = new RunEconomySaveState { runSouls = 7 },
                    playerStats = new PlayerRunStatsSaveState()
                });

                var savePath = Path.Combine(tempRoot, "hollow_profiles.json");
                var backupPath = savePath + ".bak";
                if (!File.Exists(backupPath))
                {
                    report.findings.Add(WholeGameAuditFinding.Blocker(
                        119,
                        "AtomicSave",
                        "Profile store did not create a backup during save",
                        "The audit expected a .bak file after repeated profile writes.",
                        savePath,
                        "Keep JsonProfileStore writes temp-backed with a recoverable backup file."));
                }

                File.WriteAllText(savePath, "{broken_json");
                var recovered = new JsonProfileStore(tempRoot).LoadSlotSummaries();
                if (recovered.Count == 0 || !string.Equals(recovered[0].DisplayName, "Audit Profile", StringComparison.Ordinal))
                {
                    report.findings.Add(WholeGameAuditFinding.Blocker(
                        119,
                        "CorruptSaveRecovery",
                        "Profile store could not recover from a corrupt primary JSON file",
                        "The backup should recover at least the profile slot metadata after a torn/corrupt write.",
                        savePath,
                        "Load the backup when the primary save cannot deserialize, then rewrite the primary."));
                }
            }
            finally
            {
                if (Directory.Exists(tempRoot))
                {
                    Directory.Delete(tempRoot, recursive: true);
                }
            }
        }

        private static void AuditSceneAndPlatformPresentation(WholeGameAuditReport report)
        {
            if (Mathf.Abs(PresentationScalePolicy.VisionOSBoundedTabletopScale - 0.5f) > 0.0001f ||
                Mathf.Abs(PresentationScalePolicy.WorldScaleFor(HollowPlatformKind.VisionOSBoundedTabletop) - 0.5f) > 0.0001f)
            {
                report.findings.Add(WholeGameAuditFinding.Blocker(
                    120,
                    "VisionOSScale",
                    "Bounded visionOS presentation scale drifted from 0.5",
                    $"Constant={PresentationScalePolicy.VisionOSBoundedTabletopScale}, policy={PresentationScalePolicy.WorldScaleFor(HollowPlatformKind.VisionOSBoundedTabletop)}.",
                    nameof(PresentationScalePolicy),
                    "Keep the bounded tabletop scale at 0.5 and update the scene/polish validators if the design changes intentionally."));
            }

            foreach (var path in new[] { BoundedVolumeCameraConfigPath, ImmersiveVolumeCameraConfigPath, BoundedPlatformPolishPath })
            {
                if (!AssetPathExists(path))
                {
                    report.findings.Add(WholeGameAuditFinding.Blocker(
                        120,
                        "PlatformAsset",
                        "Required platform presentation asset is missing",
                        $"The platform presentation audit could not find '{path}'.",
                        path,
                        "Regenerate the visionOS platform camera/polish assets and rerun VisionOSVolumeCameraSetupTests."));
                }
            }

            foreach (var scenePath in new[] { RouteScenePaths[AppShellRoute.GameVisionOSBounded], RouteScenePaths[AppShellRoute.ArenaMode] })
            {
                if (!SceneTextContains(scenePath, BoundedSourceDimensionLine))
                {
                    report.findings.Add(WholeGameAuditFinding.Blocker(
                        120,
                        "BoundedVolumeCamera",
                        "Bounded gameplay scene source dimensions are not proportional",
                        $"Expected source dimensions line '{BoundedSourceDimensionLine}' in {scenePath}.",
                        scenePath,
                        "Re-run the visionOS volume camera setup so the source/output aspect ratio remains 3:2:3."));
                }
            }

            if (!SceneTextContains(RouteScenePaths[AppShellRoute.MainMenuVisionOS], "m_OutputConfiguration"))
            {
                report.findings.Add(WholeGameAuditFinding.Blocker(
                    120,
                    "VisionOSMenuVolume",
                    "visionOS menu scene has no serialized VolumeCamera output configuration",
                    "The guided menu must boot inside a bounded volumetric window.",
                    RouteScenePaths[AppShellRoute.MainMenuVisionOS],
                    "Regenerate MainMenu_VisionOS with its bounded VolumeCamera and menu root."));
            }
        }

        private static void AuditRoomBranchAndNavMesh(WholeGameAuditReport report)
        {
            var catalog = AssetDatabase.LoadAssetAtPath<RoomNavMeshCatalogDefinition>(RoomNavMeshCatalogDefinition.EditorCatalogAssetPath);
            if (catalog == null)
            {
                AddFinding(report, 121, report.strictReleaseGate ? WholeGameAuditSeverity.Blocker : WholeGameAuditSeverity.Warning,
                    "NavMeshBake",
                    "Room NavMesh catalog is missing",
                    $"No catalog found at {RoomNavMeshCatalogDefinition.EditorCatalogAssetPath}.",
                    RoomNavMeshCatalogDefinition.EditorCatalogAssetPath,
                    $"Run {RoomNavMeshCatalogDefinition.PreferredBakeMenuPath} and commit the generated catalog for QA/release gates.");
                return;
            }

            if (catalog.Entries.Count == 0)
            {
                AddFinding(report, 121, report.strictReleaseGate ? WholeGameAuditSeverity.Blocker : WholeGameAuditSeverity.Warning,
                    "NavMeshBake",
                    "Room NavMesh catalog has no bakes",
                    "Runtime curated rooms will use dev-only fallback navigation without baked entries.",
                    RoomNavMeshCatalogDefinition.EditorCatalogAssetPath,
                    $"Run {RoomNavMeshCatalogDefinition.PreferredBakeMenuPath} before locking QA content.");
            }

            var missingData = catalog.Entries
                .Where(entry => entry == null || entry.NavMeshData == null || string.IsNullOrWhiteSpace(entry.RoomId))
                .ToArray();
            if (missingData.Length > 0)
            {
                AddFinding(report, 121, report.strictReleaseGate ? WholeGameAuditSeverity.Blocker : WholeGameAuditSeverity.Warning,
                    "NavMeshBake",
                    "Room NavMesh catalog contains invalid entries",
                    $"{missingData.Length} NavMesh catalog entries have missing room ids or NavMeshData.",
                    RoomNavMeshCatalogDefinition.EditorCatalogAssetPath,
                    $"Rebake the catalog via {RoomNavMeshCatalogDefinition.PreferredBakeMenuPath} and remove stale entries.");
            }

            if (!AssetPathExists(Milestone14AssetGenerator.CatalogPath))
            {
                report.findings.Add(WholeGameAuditFinding.Blocker(
                    121,
                    "BranchCatalog",
                    "Macro branch room template catalog is missing",
                    $"Expected branch room template catalog at {Milestone14AssetGenerator.CatalogPath}.",
                    Milestone14AssetGenerator.CatalogPath,
                    "Regenerate milestone 14 room templates and rerun branch traversal tests."));
            }
        }

        private static void AuditCombatLoopCorrectness(WholeGameAuditReport report)
        {
            foreach (var testPath in new[]
                     {
                         "Assets/_Hollow/Tests/EditMode/Milestone4CombatLoopTests.cs",
                         "Assets/_Hollow/Tests/EditMode/Milestone90CombatAiQaLockTests.cs"
                     })
            {
                if (!AssetPathExists(testPath))
                {
                    report.findings.Add(WholeGameAuditFinding.Warning(
                        122,
                        "CombatRegressionTests",
                        "Expected combat regression suite is missing",
                        $"The audit could not find {testPath}.",
                        testPath,
                        "Restore or replace this deterministic combat coverage before treating the release gate as complete."));
                }
            }
        }

        private static void AuditEnemyAiAndEncounters(WholeGameAuditReport report)
        {
            var enemyCatalog = AssetDatabase.LoadAssetAtPath<EnemyCatalog>(EnemyCatalogPath);
            if (enemyCatalog == null)
            {
                report.findings.Add(WholeGameAuditFinding.Blocker(
                    123,
                    "EnemyCatalog",
                    "Enemy catalog is missing",
                    $"Expected enemy catalog at {EnemyCatalogPath}.",
                    EnemyCatalogPath,
                    "Regenerate enemy catalog content and rerun encounter/AI validation suites."));
            }
            else if (enemyCatalog.Definitions.Count == 0)
            {
                report.findings.Add(WholeGameAuditFinding.Blocker(
                    123,
                    "EnemyCatalog",
                    "Enemy catalog has no definitions",
                    "Encounter generation and AI smoke tests have no enemies to resolve.",
                    EnemyCatalogPath,
                    "Restore the enemy definitions and rerun Milestone90CombatAiQaLockTests."));
            }

            if (!AssetPathExists(Milestone48AssetGenerator.EncounterCatalogPath))
            {
                report.findings.Add(WholeGameAuditFinding.Blocker(
                    123,
                    "EncounterCatalog",
                    "Encounter catalog is missing",
                    $"Expected encounter catalog at {Milestone48AssetGenerator.EncounterCatalogPath}.",
                    Milestone48AssetGenerator.EncounterCatalogPath,
                    "Regenerate milestone 48 encounters and rerun enemy/encounter validators."));
            }
        }

        private static void AuditArtPassMeshyAndVisualSafety(WholeGameAuditReport report)
        {
            var artReport = ArtPassProductionValidator.BuildReport();
            foreach (var target in artReport.targets ?? Array.Empty<ArtPassProductionTargetRecord>())
            {
                var hasErrors = target.errors != null && target.errors.Length > 0;
                if (!hasErrors)
                {
                    continue;
                }

                var severity = target.corePriority
                    ? WholeGameAuditSeverity.Blocker
                    : WholeGameAuditSeverity.Warning;
                AddFinding(report, 124, severity,
                    "ArtPassPrefab",
                    $"ArtPass role {target.role} is unsafe",
                    string.Join("; ", target.errors),
                    target.prefabPath,
                    "Fix the prefab marker/material/component safety issue or regenerate the ArtPass asset from the approved generator.");
            }

            foreach (var role in new[] { PresentationPrefabRole.RoomObstacleRock, PresentationPrefabRole.ChestNormal, PresentationPrefabRole.ChestCorrupted })
            {
                var target = artReport.targets?.FirstOrDefault(record => string.Equals(record.role, role.ToString(), StringComparison.Ordinal));
                if (target == null || target.status == ArtPassProductionStatus.PrototypeFallback)
                {
                    report.findings.Add(WholeGameAuditFinding.Warning(
                        124,
                        "MeshyReplacement",
                        $"{role} still appears to be a prototype fallback",
                        "Recent Meshy replacements should resolve through the ArtPass catalog without primitive placeholder evidence.",
                        target?.prefabPath ?? role.ToString(),
                        "Regenerate or repair the Meshy environment prop ArtPass prefabs and rerun MeshyEnvironmentPropArtPassTests."));
                }
            }
        }

        private static void AuditBuildPerformanceAndReleaseGate(WholeGameAuditReport report)
        {
            var profile = AssetDatabase.LoadAssetAtPath<PlatformBuildQaProfileDefinition>(Milestone24AssetGenerator.PlatformBuildQaProfilePath);
            if (profile == null)
            {
                report.findings.Add(WholeGameAuditFinding.Blocker(
                    125,
                    "PlatformQaProfile",
                    "Platform build QA profile is missing",
                    $"Expected profile at {Milestone24AssetGenerator.PlatformBuildQaProfilePath}.",
                    Milestone24AssetGenerator.PlatformBuildQaProfilePath,
                    "Regenerate milestone 24 platform build QA assets before running the release gate."));
            }
            else
            {
                var requiredSceneSet = new HashSet<string>(profile.RequiredScenes ?? Array.Empty<string>(), StringComparer.Ordinal);
                foreach (var route in RequiredRuntimeRoutes)
                {
                    var scenePath = RouteScenePaths[route];
                    if (!requiredSceneSet.Contains(scenePath))
                    {
                        report.findings.Add(WholeGameAuditFinding.Warning(
                            125,
                            "PlatformQaProfile",
                            $"Platform QA profile does not list {route}",
                            $"Expected required scene '{scenePath}' in {Milestone24AssetGenerator.PlatformBuildQaProfilePath}.",
                            Milestone24AssetGenerator.PlatformBuildQaProfilePath,
                            "Update the QA profile required scene list when adding platform/menu routes."));
                    }
                }
            }

            var settings = AddressableAssetSettingsDefaultObject.GetSettings(create: false);
            if (settings == null)
            {
                report.findings.Add(WholeGameAuditFinding.Blocker(
                    125,
                    "Addressables",
                    "Addressables settings are missing",
                    "The final release gate cannot validate or build Addressables without settings.",
                    "AddressableAssetSettings",
                    "Restore Addressables settings or run the project generation step that creates them."));
            }

            if (!AssetPathExists("Assets/_Hollow/Tests/EditMode/Milestone24PlatformBuildQaTests.cs"))
            {
                report.findings.Add(WholeGameAuditFinding.Warning(
                    125,
                    "ReleaseGateTests",
                    "Platform build QA EditMode test suite is missing",
                    "The full gate should include focused M24 platform build validation.",
                    "Assets/_Hollow/Tests/EditMode/Milestone24PlatformBuildQaTests.cs",
                    "Restore or replace the M24 platform QA tests and include the whole-game audit in the final gate."));
            }

            AuditBootLoadingStartupSafety(report);
            AuditRoomTransitionDimmingSafety(report);
            AuditM138CombatScaleStressGate(report);
            AuditM139LongRunSoakGate(report);
            AuditM140BuildRealReleaseGate(report);
        }

        private static void AuditBootLoadingStartupSafety(WholeGameAuditReport report)
        {
            const string bootControllerPath = "Assets/_Hollow/Scripts/Hollow.Core/App/BootSceneController.cs";
            const string bootScreenPath = "Assets/_Hollow/Scripts/Hollow.Core/App/BootLoadingScreenController.cs";
            const string bootPreloadPath = "Assets/_Hollow/Scripts/Hollow.Core/App/BootPreloadService.cs";
            const string shaderWarmupProfileScriptPath = "Assets/_Hollow/Scripts/Hollow.Core/App/HollowShaderWarmupProfile.cs";
            const string bootShaderWarmupProfilePath = "Assets/_Hollow/Resources/Hollow/HollowBootShaderWarmupProfile.asset";
            const string bootShaderVariantCollectionPath = "Assets/_Hollow/Shaders/HollowBootShaderVariants.shadervariants";
            const string counterPath = "Assets/_Hollow/Scripts/Hollow.Core/M136PerformanceOperationCounters.cs";
            const string projectSettingsPath = "ProjectSettings/ProjectSettings.asset";
            var bootController = File.Exists(ToAbsolutePath(bootControllerPath))
                ? File.ReadAllText(ToAbsolutePath(bootControllerPath))
                : string.Empty;
            var bootPreload = File.Exists(ToAbsolutePath(bootPreloadPath))
                ? File.ReadAllText(ToAbsolutePath(bootPreloadPath))
                : string.Empty;
            var counters = File.Exists(ToAbsolutePath(counterPath))
                ? File.ReadAllText(ToAbsolutePath(counterPath))
                : string.Empty;
            var projectSettings = File.Exists(ToAbsolutePath(projectSettingsPath))
                ? File.ReadAllText(ToAbsolutePath(projectSettingsPath))
                : string.Empty;

            if (!AssetPathExists(bootScreenPath) ||
                !AssetPathExists(bootPreloadPath) ||
                !bootController.Contains("BootRoutine", StringComparison.Ordinal) ||
                !bootController.Contains("BootPreloadService", StringComparison.Ordinal) ||
                !bootController.Contains("EffectiveMinimumVisibleSeconds", StringComparison.Ordinal) ||
                !bootController.Contains("ShowFailure", StringComparison.Ordinal))
            {
                report.findings.Add(WholeGameAuditFinding.Warning(
                    125,
                    "BootLoading",
                    "Pre-main-menu boot loading safeguards are incomplete",
                    "Boot should show a branded loading screen, preload global immutable resources, keep a fast editor minimum, and surface startup failures before routing to the menu.",
                    bootControllerPath,
                    "Restore the boot loading pipeline and rerun the boot-to-menu PlayMode smoke test."));
            }

            if (!AssetPathExists(shaderWarmupProfileScriptPath) ||
                !AssetPathExists(bootShaderWarmupProfilePath) ||
                !AssetPathExists(bootShaderVariantCollectionPath) ||
                bootPreload.Contains("Warmup" + "AllShaders", StringComparison.Ordinal))
            {
                report.findings.Add(WholeGameAuditFinding.Warning(
                    125,
                    "ShaderWarmup",
                    "Curated boot shader warmup safeguards are incomplete",
                    "Boot should warm assigned ShaderVariantCollection assets only; blanket shader warmup is forbidden because it can touch invalid URP/package keyword spaces.",
                    bootPreloadPath,
                    "Generate the boot shader warmup profile/collection and keep normal boot on curated collection warmup."));
            }

            if (bootPreload.Contains("BranchEnemyPool", StringComparison.Ordinal) ||
                bootPreload.Contains("RoomRuntimeBuildDescriptor", StringComparison.Ordinal) ||
                bootPreload.Contains("RoomNavMeshRuntimeFallback", StringComparison.Ordinal))
            {
                report.findings.Add(WholeGameAuditFinding.Warning(
                    125,
                    "BootPreload",
                    "Boot preload includes branch-specific mutable data",
                    "Boot preload should stay global; branch descriptors, branch enemy pools, and NavMesh attach/fallback work belong behind branch loading.",
                    bootPreloadPath,
                    "Move branch-specific preload back into BranchSessionController branch loading."));
            }

            if (!counters.Contains("ReportBootLoadingStart", StringComparison.Ordinal) ||
                !counters.Contains("ReportBootLoadingCompletion", StringComparison.Ordinal) ||
                !counters.Contains("ReportBootPreloadResourceLoad", StringComparison.Ordinal) ||
                !counters.Contains("ReportBootPreloadWarmRequest", StringComparison.Ordinal) ||
                !counters.Contains("ReportBootPreloadShaderWarmSuccess", StringComparison.Ordinal) ||
                !counters.Contains("ReportBootPreloadShaderWarmMiss", StringComparison.Ordinal))
            {
                report.findings.Add(WholeGameAuditFinding.Warning(
                    125,
                    "Telemetry",
                    "Boot loading telemetry is missing",
                    "M136/M137 summaries should include boot loading duration, failures, stages, global resource loads, and warm requests.",
                    counterPath,
                    "Restore the boot loading counters before accepting startup visual QA."));
            }

            if (!projectSettings.Contains("companyName: CineFit Studio", StringComparison.Ordinal))
            {
                report.findings.Add(WholeGameAuditFinding.Warning(
                    125,
                    "Branding",
                    "Project company name is not set to CineFit Studio",
                    "PlayerSettings should carry the studio name used by the boot splash and build metadata.",
                    projectSettingsPath,
                    "Set PlayerSettings companyName to CineFit Studio."));
            }
        }

        private static void AuditRoomTransitionDimmingSafety(WholeGameAuditReport report)
        {
            const string branchSessionPath = "Assets/_Hollow/Scripts/Hollow.Branches/BranchSessionController.cs";
            const string inputLockPath = "Assets/_Hollow/Scripts/Hollow.Input/GameplayTransitionState.cs";
            const string loadingScreenPath = "Assets/_Hollow/Scripts/Hollow.Branches/BranchLoadingScreenController.cs";
            const string enemyPoolPath = "Assets/_Hollow/Scripts/Hollow.Combat/EnemyRuntimePool.cs";
            const string counterPath = "Assets/_Hollow/Scripts/Hollow.Core/M136PerformanceOperationCounters.cs";
            var branchSession = File.Exists(ToAbsolutePath(branchSessionPath))
                ? File.ReadAllText(ToAbsolutePath(branchSessionPath))
                : string.Empty;
            var counters = File.Exists(ToAbsolutePath(counterPath))
                ? File.ReadAllText(ToAbsolutePath(counterPath))
                : string.Empty;
            var traversalRoutine = ExtractMethodBlock(branchSession, "private IEnumerator TraverseStagedRoutine");

            if (!AssetPathExists(inputLockPath) ||
                !AssetPathExists(loadingScreenPath) ||
                !AssetPathExists(enemyPoolPath) ||
                !branchSession.Contains("GameplayTransitionState.AcquireLock", StringComparison.Ordinal) ||
                !branchSession.Contains("LoadCurrentBranchWithLoading", StringComparison.Ordinal) ||
                !branchSession.Contains("PreloadFullBranchForLoadingRoutine", StringComparison.Ordinal) ||
                !branchSession.Contains("ShowBranchLoadingScreen", StringComparison.Ordinal) ||
                !branchSession.Contains("ShouldShowBossLoading", StringComparison.Ordinal) ||
                !branchSession.Contains("SetTransitionSuspended", StringComparison.Ordinal) ||
                !branchSession.Contains("EnemyRuntimePool", StringComparison.Ordinal) ||
                traversalRoutine.Contains("ShowTransitionCurtain(", StringComparison.Ordinal) ||
                traversalRoutine.Contains("HideTransitionCurtain(", StringComparison.Ordinal))
            {
                report.findings.Add(WholeGameAuditFinding.Warning(
                    125,
                    "BranchLoading",
                    "Branch-level loading and seamless traversal safeguards are incomplete",
                    "Branch entry and boss rooms should use the loading screen, while normal room traversal must avoid the old visual curtain and rely on preloaded descriptors, lookups, and pools.",
                    branchSessionPath,
                    "Restore the branch-level loading path and rerun branch entry, boss, and normal traversal captures."));
            }

            if (!counters.Contains("ReportBranchLoadingStart", StringComparison.Ordinal) ||
                !counters.Contains("ReportBossLoadingStart", StringComparison.Ordinal) ||
                !counters.Contains("ReportFullBranchPreloadRoom", StringComparison.Ordinal) ||
                !counters.Contains("ReportTraversalColdCacheMiss", StringComparison.Ordinal) ||
                !counters.Contains("ReportEnemyPoolRent", StringComparison.Ordinal) ||
                !counters.Contains("ReportTransitionLock", StringComparison.Ordinal))
            {
                report.findings.Add(WholeGameAuditFinding.Warning(
                    125,
                    "Telemetry",
                    "Branch loading telemetry is missing",
                    "M136/M137 reports should include branch loading, boss loading, full-preload, cold-cache-miss, enemy-pool, and transition-lock counters.",
                    counterPath,
                    "Restore the branch loading counters before accepting traversal visual QA."));
            }
        }

        private static void AuditM138CombatScaleStressGate(WholeGameAuditReport report)
        {
            const string runnerPath = "Assets/_Hollow/Scripts/Hollow.Performance/M138CombatScaleStressRunner.cs";
            const string reportPath = "Assets/_Hollow/Scripts/Hollow.Performance/M138CombatScaleStressReport.cs";
            const string editorMenuPath = "Assets/_Hollow/Scripts/Hollow.Editor/Generation/Milestone138CombatScaleStressGateAssetGenerator.cs";
            const string editModeTestPath = "Assets/_Hollow/Tests/EditMode/Milestone138CombatAiNavigationScaleTests.cs";
            const string playModeTestPath = "Assets/_Hollow/Tests/PlayMode/M138AutomatedStressSmokeTests.cs";
            var reportSource = File.Exists(ToAbsolutePath(reportPath))
                ? File.ReadAllText(ToAbsolutePath(reportPath))
                : string.Empty;
            var runnerSource = File.Exists(ToAbsolutePath(runnerPath))
                ? File.ReadAllText(ToAbsolutePath(runnerPath))
                : string.Empty;

            if (!AssetPathExists(runnerPath) ||
                !AssetPathExists(reportPath) ||
                !AssetPathExists(editorMenuPath) ||
                !runnerSource.Contains("RunAllScenarios", StringComparison.Ordinal) ||
                !runnerSource.Contains("M136LivePerformanceCaptureSession", StringComparison.Ordinal) ||
                !runnerSource.Contains("M138CombatScaleStressScenarioPolicy.StressManifest", StringComparison.Ordinal) ||
                !reportSource.Contains("maxPathSolvesInFrame", StringComparison.Ordinal) ||
                !reportSource.Contains("bossFullLodObserved", StringComparison.Ordinal))
            {
                report.findings.Add(WholeGameAuditFinding.Warning(
                    138,
                    "PerformanceGate",
                    "M138 automated combat scale stress gate is incomplete",
                    "M138 should build deterministic temporary combat rooms, reuse M136/M137 telemetry, and report AI/Nav burst and boss/add LOD gates without manual gameplay.",
                    runnerPath,
                    "Restore the M138 runner, report generator, and editor menu integration."));
            }

            if (!AssetPathExists(editModeTestPath) ||
                !AssetPathExists(playModeTestPath))
            {
                report.findings.Add(WholeGameAuditFinding.Warning(
                    138,
                    "Tests",
                    "M138 automated tests are missing",
                    "M138 needs EditMode report logic tests and a PlayMode smoke gate that writes the stress report.",
                    playModeTestPath,
                    "Restore the M138 EditMode and PlayMode test coverage."));
            }

            if (!File.Exists(ToAbsolutePath(M138CombatScaleStressReportGenerator.DefaultJsonReportPath)))
            {
                AddFinding(
                    report,
                    138,
                    WholeGameAuditSeverity.Info,
                    "Evidence",
                    "M138 stress report has not been generated in this workspace",
                    "The automated gate code is present, but no latest `output/reports/m138_combat_scale_stress.json` artifact exists yet.",
                    M138CombatScaleStressReportGenerator.DefaultJsonReportPath,
                    "Run the M138 PlayMode stress gate before using this audit as release evidence.");
            }
        }

        private static void AuditM139LongRunSoakGate(WholeGameAuditReport report)
        {
            const string runnerPath = "Assets/_Hollow/Scripts/Hollow.Performance/M139LongRunSoakRunner.cs";
            const string reportPath = "Assets/_Hollow/Scripts/Hollow.Performance/M139LongRunSoakReport.cs";
            const string editorMenuPath = "Assets/_Hollow/Scripts/Hollow.Editor/Generation/Milestone139LongRunSoakGateAssetGenerator.cs";
            const string editModeTestPath = "Assets/_Hollow/Tests/EditMode/Milestone139LongRunSoakTests.cs";
            const string playModeTestPath = "Assets/_Hollow/Tests/PlayMode/M139LongRunSoakSmokeTests.cs";
            var reportSource = File.Exists(ToAbsolutePath(reportPath))
                ? File.ReadAllText(ToAbsolutePath(reportPath))
                : string.Empty;
            var runnerSource = File.Exists(ToAbsolutePath(runnerPath))
                ? File.ReadAllText(ToAbsolutePath(runnerPath))
                : string.Empty;

            if (!AssetPathExists(runnerPath) ||
                !AssetPathExists(reportPath) ||
                !AssetPathExists(editorMenuPath) ||
                !runnerSource.Contains("BranchSessionController", StringComparison.Ordinal) ||
                !runnerSource.Contains("RunAllScenarios", StringComparison.Ordinal) ||
                !runnerSource.Contains("StartNextBranch", StringComparison.Ordinal) ||
                !reportSource.Contains("managedMemoryDriftMb", StringComparison.Ordinal) ||
                !reportSource.Contains("normalTraversalColdCacheMissesAfterLoad", StringComparison.Ordinal))
            {
                report.findings.Add(WholeGameAuditFinding.Warning(
                    139,
                    "PerformanceGate",
                    "M139 long-run cache/pool soak gate is incomplete",
                    "M139 should run real BranchSessionController branch loads/traversals, save/restore, boss loading, next-branch transitions, and export deterministic cache/pool/memory gates.",
                    runnerPath,
                    "Restore the M139 runner, report generator, and editor menu integration."));
            }

            if (!AssetPathExists(editModeTestPath) ||
                !AssetPathExists(playModeTestPath))
            {
                report.findings.Add(WholeGameAuditFinding.Warning(
                    139,
                    "Tests",
                    "M139 automated tests are missing",
                    "M139 needs EditMode report/snapshot tests and a PlayMode smoke gate that writes the long-run soak report.",
                    playModeTestPath,
                    "Restore the M139 EditMode and PlayMode test coverage."));
            }

            if (!File.Exists(ToAbsolutePath(M139LongRunSoakReportGenerator.DefaultJsonReportPath)))
            {
                AddFinding(
                    report,
                    139,
                    WholeGameAuditSeverity.Info,
                    "Evidence",
                    "M139 soak report has not been generated in this workspace",
                    "The automated gate code is present, but no latest `output/reports/m139_long_run_soak.json` artifact exists yet.",
                    M139LongRunSoakReportGenerator.DefaultJsonReportPath,
                    "Run the M139 PlayMode soak gate before using this audit as release evidence.");
            }
        }

        private static void AuditM140BuildRealReleaseGate(WholeGameAuditReport report)
        {
            const string reportPath = "Assets/_Hollow/Scripts/Hollow.Performance/M140BuildRealGateReport.cs";
            const string runnerPath = "Assets/_Hollow/Scripts/Hollow.Performance/M140BuiltPlayerCaptureRunner.cs";
            const string editorRunnerPath = "Assets/_Hollow/Scripts/Hollow.Editor/Build/M140BuildRealGateRunner.cs";
            const string profilePath = "Assets/_Hollow/Scripts/Hollow.Data/Definitions/M140BuildRealGateProfileDefinition.cs";
            const string editModeTestPath = "Assets/_Hollow/Tests/EditMode/Milestone140BuildRealGateTests.cs";
            var reportSource = File.Exists(ToAbsolutePath(reportPath))
                ? File.ReadAllText(ToAbsolutePath(reportPath))
                : string.Empty;
            var runnerSource = File.Exists(ToAbsolutePath(runnerPath))
                ? File.ReadAllText(ToAbsolutePath(runnerPath))
                : string.Empty;
            var editorSource = File.Exists(ToAbsolutePath(editorRunnerPath))
                ? File.ReadAllText(ToAbsolutePath(editorRunnerPath))
                : string.Empty;

            if (!AssetPathExists(reportPath) ||
                !AssetPathExists(runnerPath) ||
                !AssetPathExists(editorRunnerPath) ||
                !AssetPathExists(profilePath) ||
                !reportSource.Contains("M140VisualScreenshotValidator", StringComparison.Ordinal) ||
                !reportSource.Contains("M140PlayerLogValidator", StringComparison.Ordinal) ||
                !runnerSource.Contains("--hollow-m140-capture", StringComparison.Ordinal) ||
                !runnerSource.Contains("M138CombatScaleStressRunner", StringComparison.Ordinal) ||
                !runnerSource.Contains("M139LongRunSoakRunner", StringComparison.Ordinal) ||
                !editorSource.Contains("BuildTarget.StandaloneOSX", StringComparison.Ordinal) ||
                !editorSource.Contains("StandaloneWindows64", StringComparison.Ordinal) ||
                !editorSource.Contains("ImportWindowsArtifacts", StringComparison.Ordinal))
            {
                report.findings.Add(WholeGameAuditFinding.Warning(
                    140,
                    "BuildRealGate",
                    "M140 built-player release gate is incomplete",
                    "M140 should build/run macOS Apple silicon and Windows player artifacts, execute command-line runtime captures, validate screenshots/logs/shaders, and report honest blocked environment status for unavailable Windows runtime captures.",
                    editorRunnerPath,
                    "Restore the M140 profile, runtime harness, editor runner, report generator, and artifact importer."));
            }

            if (!AssetPathExists(editModeTestPath))
            {
                report.findings.Add(WholeGameAuditFinding.Warning(
                    140,
                    "Tests",
                    "M140 EditMode coverage is missing",
                    "M140 needs synthetic report, parser, screenshot, player-log, and artifact-import tests before trusting built-player gate output.",
                    editModeTestPath,
                    "Restore Milestone140BuildRealGateTests."));
            }

            var latestReportPath = Path.Combine("output/reports/m140", M140BuildRealGateRunner.LatestEditorJsonFileName);
            if (!File.Exists(ToAbsolutePath(latestReportPath)))
            {
                AddFinding(
                    report,
                    140,
                    WholeGameAuditSeverity.Info,
                    "Evidence",
                    "M140 built-player gate report has not been generated in this workspace",
                    "The gate code is present, but no latest M140 built-player orchestration report exists yet.",
                    latestReportPath,
                    "Run Hollow/Performance/Run M140 macOS Apple Silicon Gate, then import or run Windows artifacts before release signoff.");
            }
        }

        private static string ExtractMethodBlock(string source, string methodSignature)
        {
            if (string.IsNullOrEmpty(source))
            {
                return string.Empty;
            }

            var start = source.IndexOf(methodSignature, StringComparison.Ordinal);
            if (start < 0)
            {
                return string.Empty;
            }

            var braceStart = source.IndexOf('{', start);
            if (braceStart < 0)
            {
                return string.Empty;
            }

            var depth = 0;
            for (var index = braceStart; index < source.Length; index++)
            {
                if (source[index] == '{')
                {
                    depth++;
                }
                else if (source[index] == '}')
                {
                    depth--;
                    if (depth == 0)
                    {
                        return source.Substring(start, index - start + 1);
                    }
                }
            }

            return string.Empty;
        }

        private static void AddFinding(
            WholeGameAuditReport report,
            int milestone,
            WholeGameAuditSeverity severity,
            string category,
            string title,
            string message,
            string location,
            string solution)
        {
            report.findings.Add(severity switch
            {
                WholeGameAuditSeverity.Blocker => WholeGameAuditFinding.Blocker(milestone, category, title, message, location, solution),
                WholeGameAuditSeverity.Info => WholeGameAuditFinding.Info(milestone, category, title, message, location, solution),
                _ => WholeGameAuditFinding.Warning(milestone, category, title, message, location, solution)
            });
        }

        private static void WriteReports(WholeGameAuditReport report)
        {
            Directory.CreateDirectory(ReportRoot);
            var json = JsonUtility.ToJson(report, prettyPrint: true);
            File.WriteAllText(Path.Combine(ReportRoot, LatestJsonReportName), json);
            File.WriteAllText(Path.Combine(ReportRoot, $"{report.auditId}.json"), json);
            var markdown = ToMarkdown(report);
            File.WriteAllText(Path.Combine(ReportRoot, LatestMarkdownReportName), markdown);
            File.WriteAllText(Path.Combine(ReportRoot, $"{report.auditId}.md"), markdown);
            AssetDatabase.Refresh();
        }

        private static IEnumerable<string> EnumerateSerializedAssetPaths()
        {
            var projectRoot = ProjectRoot;
            var roots = new[]
            {
                "Assets/_Hollow",
                "Assets/Resources",
                "ProjectSettings"
            };
            var extensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                ".unity",
                ".prefab",
                ".asset",
                ".mat",
                ".controller",
                ".overrideController",
                ".playable",
                ".anim"
            };

            foreach (var root in roots)
            {
                var absoluteRoot = Path.Combine(projectRoot, root);
                if (!Directory.Exists(absoluteRoot))
                {
                    continue;
                }

                foreach (var absolutePath in Directory.EnumerateFiles(absoluteRoot, "*.*", SearchOption.AllDirectories))
                {
                    if (!extensions.Contains(Path.GetExtension(absolutePath)))
                    {
                        continue;
                    }

                    var fileInfo = new FileInfo(absolutePath);
                    if (fileInfo.Length > 5 * 1024 * 1024)
                    {
                        continue;
                    }

                    yield return ProjectRelativePath(absolutePath);
                }
            }
        }

        private static IEnumerable<string> FindSerializedMissingScriptMarkers(IEnumerable<string> serializedAssetPaths)
        {
            foreach (var path in serializedAssetPaths ?? Array.Empty<string>())
            {
                var absolutePath = ToAbsolutePath(path);
                if (!File.Exists(absolutePath))
                {
                    continue;
                }

                string text;
                try
                {
                    text = File.ReadAllText(absolutePath);
                }
                catch
                {
                    continue;
                }

                if (text.Contains("m_Script: {fileID: 0}", StringComparison.Ordinal) ||
                    text.Contains("guid: 00000000000000000000000000000000", StringComparison.Ordinal))
                {
                    yield return NormalizeAssetPath(path);
                }
            }
        }

        private static IEnumerable<string> FindDirectSceneLoadCallers()
        {
            var scriptsRoot = Path.Combine(ProjectRoot, "Assets/_Hollow/Scripts");
            if (!Directory.Exists(scriptsRoot))
            {
                yield break;
            }

            foreach (var path in Directory.EnumerateFiles(scriptsRoot, "*.cs", SearchOption.AllDirectories))
            {
                var normalized = ProjectRelativePath(path);
                if (normalized.Contains("/Hollow.Editor/", StringComparison.Ordinal) ||
                    normalized.EndsWith("/SceneLoaderService.cs", StringComparison.Ordinal))
                {
                    continue;
                }

                var text = File.ReadAllText(path);
                if (text.Contains("SceneManager.LoadScene", StringComparison.Ordinal) ||
                    text.Contains("LoadSceneAsync(", StringComparison.Ordinal))
                {
                    yield return normalized;
                }
            }
        }

        private static bool SceneTextContains(string scenePath, string needle)
        {
            var absolutePath = ToAbsolutePath(scenePath);
            return File.Exists(absolutePath) &&
                   File.ReadAllText(absolutePath).Contains(needle, StringComparison.Ordinal);
        }

        private static Type FindType(string fullName)
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var type = assembly.GetType(fullName, throwOnError: false);
                if (type != null)
                {
                    return type;
                }
            }

            return null;
        }

        private static bool AssetPathExists(string assetPath)
        {
            return File.Exists(ToAbsolutePath(assetPath));
        }

        private static string ToAbsolutePath(string assetPath)
        {
            return Path.IsPathRooted(assetPath)
                ? assetPath
                : Path.Combine(ProjectRoot, assetPath);
        }

        private static string NormalizeAssetPath(string path)
        {
            return path.Replace('\\', '/');
        }

        private static string ProjectRelativePath(string absolutePath)
        {
            var projectRoot = ProjectRoot.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
            return absolutePath.StartsWith(projectRoot, StringComparison.Ordinal)
                ? NormalizeAssetPath(absolutePath.Substring(projectRoot.Length))
                : NormalizeAssetPath(absolutePath);
        }

        private static string ProjectRoot =>
            Path.GetFullPath(Path.Combine(Application.dataPath, ".."));

        public sealed class FocusedAuditTestRunResult
        {
            public string outputPath = string.Empty;
            public int passCount;
            public int failCount;
            public int inconclusiveCount;
            public int skipCount;
            public int totalCount;
            public string status = "NotRun";

            public bool Passed => totalCount > 0 && failCount == 0 && inconclusiveCount == 0 && !string.Equals(status, TestStatus.Failed.ToString(), StringComparison.Ordinal);

            public static FocusedAuditTestRunResult From(ITestResultAdaptor result, string outputPath)
            {
                if (result == null)
                {
                    return new FocusedAuditTestRunResult
                    {
                        outputPath = outputPath ?? string.Empty,
                        status = "NoResult"
                    };
                }

                var total = result.PassCount + result.FailCount + result.InconclusiveCount + result.SkipCount;
                return new FocusedAuditTestRunResult
                {
                    outputPath = outputPath ?? string.Empty,
                    passCount = result.PassCount,
                    failCount = result.FailCount,
                    inconclusiveCount = result.InconclusiveCount,
                    skipCount = result.SkipCount,
                    totalCount = total,
                    status = result.TestStatus.ToString()
                };
            }
        }

        private sealed class FocusedAuditTestCallbacks : ScriptableObject, ICallbacks
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
