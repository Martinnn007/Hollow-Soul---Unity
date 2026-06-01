using System;
using System.IO;
using System.Linq;
using Hollow.Core.Diagnostics;
using Hollow.Data.Definitions;
using Hollow.Diagnostics;
using Hollow.Editor.Build;
using Hollow.Performance;
using NUnit.Framework;
using UnityEngine;

namespace Hollow.Tests.EditMode
{
    public sealed class Milestone140BuildRealGateTests
    {
        private string tempDirectory;

        [SetUp]
        public void SetUp()
        {
            tempDirectory = Path.Combine("output", "test-artifacts", "m140", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDirectory);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(tempDirectory))
            {
                Directory.Delete(tempDirectory, recursive: true);
            }
        }

        [Test]
        public void ProfileValidatesWindowsAndMacAppleSiliconTargets()
        {
            var profile = ScriptableObject.CreateInstance<M140BuildRealGateProfileDefinition>();
            try
            {
                Assert.IsTrue(M140BuildRealGateRunner.ValidateProfile(profile, out var detail), detail);
                StringAssert.Contains("macOS Apple silicon", detail);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(profile);
            }
        }

        [Test]
        public void ProfileValidationFailsWhenBossEntryIsMissing()
        {
            var profile = ScriptableObject.CreateInstance<M140BuildRealGateProfileDefinition>();
            try
            {
                profile.ConfigureForTests(
                    "output/builds/m140",
                    "output/reports/m140",
                    "HollowSoul_M140_macOS_AppleSilicon",
                    "HollowSoul_M140_Windows",
                    "HollowSoul",
                    60,
                    1800,
                    true,
                    true,
                    true,
                    true,
                    new[] { "macos-apple-silicon", "windows-x64" },
                    new[] { "Assets/_Hollow/Scenes/Boot.unity", "Assets/_Hollow/Scenes/Game_Windows.unity" },
                    M140BuildRealReportGenerator.RequiredDevelopmentScenarioIds
                        .Where(id => id != "boss_entry")
                        .ToArray(),
                    M140BuildRealReportGenerator.RequiredReleaseSmokeScenarioIds);

                Assert.IsFalse(M140BuildRealGateRunner.ValidateProfile(profile, out var detail));
                StringAssert.Contains("boss_entry", detail);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(profile);
            }
        }

        [Test]
        public void CommandLineParserHandlesOutputScenariosPlatformAndReleaseSmoke()
        {
            var parsed = M140BuiltPlayerCaptureOptions.TryParse(
                new[]
                {
                    "HollowSoul",
                    "--hollow-m140-capture",
                    "--hollow-m140-output=/tmp/m140",
                    "--hollow-m140-platform=windows-x64",
                    "--hollow-m140-build-kind=release-smoke",
                    "--hollow-m140-scenarios=boot_loading_screen,normal_traversal",
                    "--hollow-m140-fps-cap=60",
                    "--hollow-m140-auto-exit"
                },
                out var options);

            Assert.IsTrue(parsed);
            Assert.AreEqual("/tmp/m140", options.outputRoot);
            Assert.AreEqual("windows-x64", options.platformId);
            Assert.AreEqual(M140BuildKind.ReleaseSmoke, options.buildKind);
            Assert.IsTrue(options.autoExit);
            Assert.AreEqual(60, options.targetFrameRate);
            CollectionAssert.AreEqual(new[] { "boot_loading_screen", "normal_traversal" }, options.scenarioIds);
        }

        [Test]
        public void VisualValidatorPassesBrightScreenshotAndFailsDimOrPinkScreenshots()
        {
            var bright = WriteTexture("bright.png", new Color(0.42f, 0.48f, 0.52f, 1f));
            var dim = WriteTexture("dim.png", new Color(0.01f, 0.01f, 0.01f, 1f));
            var pink = WriteTexture("pink.png", new Color(1f, 0f, 1f, 1f));

            Assert.IsTrue(M140VisualScreenshotValidator.Validate(bright).passed);
            Assert.IsFalse(M140VisualScreenshotValidator.Validate(dim).passed);
            Assert.IsFalse(M140VisualScreenshotValidator.Validate(pink).passed);
        }

        [Test]
        public void ReportGeneratorFailsCoreBuildRealViolations()
        {
            var dim = WriteTexture("dim_report.png", new Color(0.01f, 0.01f, 0.01f, 1f));
            var visual = M140VisualScreenshotValidator.Validate(dim);
            var scenario = new M140ScenarioSummary
            {
                scenarioId = "normal_traversal",
                displayName = "Normal Traversal",
                platformId = "macos-apple-silicon",
                buildKind = M140BuildKind.Development,
                timingAuthoritative = true,
                frameP95Ms = M140BuildRealReportGenerator.FrameP95BudgetMs + 1d,
                frameMaxMs = M140BuildRealReportGenerator.MaxFrameBudgetMs + 1d,
                runtimeNavMeshFallbacks = 1,
                normalTraversalColdCacheMissesAfterLoad = 1,
                transitionCurtainMaxFramesAfterReady = 1,
                shaderMaterialFirstUseMissesAfterLoad = 1,
                visual = visual,
                passed = false,
                failures = new[] { "synthetic failure" }
            };

            var report = M140BuildRealReportGenerator.BuildReport(
                "macos-apple-silicon",
                M140BuildKind.Development,
                tempDirectory,
                new[] { scenario },
                new M140RenderRuntimeSnapshot { processorType = "Apple M3", operatingSystem = "macOS", targetFrameRate = 60, vSyncCount = 0 },
                new M140PlayerLogValidationSummary { passed = true });

            Assert.IsFalse(report.passed);
            var failures = string.Join("\n", report.failures);
            StringAssert.Contains("normal_traversal", failures);
            StringAssert.Contains("Missing M140 built-player scenario", failures);
        }

        [Test]
        public void M140FailsProjectileHeavyWithoutProjectilePressureEvidence()
        {
            var summary = M140BuildRealReportGenerator.FromM138Summary(
                new M138CombatScaleStressScenarioSummary
                {
                    scenarioId = "projectile_heavy_room",
                    displayName = "Projectile Heavy Room",
                    passed = true,
                    peakActiveEnemies = 4,
                    peakProjectiles = 2,
                    projectileActivePeak = 2,
                    observedBoss = false,
                    timingAuthoritative = false,
                    frameCadenceConfidence = "Trusted"
                },
                "macos-apple-silicon",
                M140BuildKind.Development,
                new M140VisualValidationSummary { passed = true },
                enforceTiming: false,
                new Hollow.Diagnostics.M136LiveObjectCountSnapshot
                {
                    activeRenderers = 12,
                    activeProjectiles = 2,
                    observedCombatController = true
                });

            Assert.IsFalse(summary.passed);
            StringAssert.Contains("Projectile-heavy", string.Join("\n", summary.failures));
        }

        [Test]
        public void M140FailsBossEntryWithoutBossEvidence()
        {
            var result = new Hollow.Diagnostics.M136PerformanceScenarioResult
            {
                scenarioId = "boss_entry",
                displayName = "Boss Entry",
                samplingSource = Hollow.Diagnostics.M136FrameCadencePolicy.RuntimeUpdateSamplingSource,
                frameCadenceConfidence = Hollow.Diagnostics.M136FrameCadencePolicy.Trusted,
                rawSampleCount = 120,
                metrics = new[]
                {
                    new Hollow.Diagnostics.M136PerformanceMetricSummary { id = "frame_time_ms", supported = true, sampleCount = 120, p95 = 12d, max = 14d },
                    new Hollow.Diagnostics.M136PerformanceMetricSummary { id = "gc_allocated_bytes", supported = true, sampleCount = 120 }
                },
                operations = new Hollow.Diagnostics.M136RuntimeOperationSummary(),
                objectCounts = new Hollow.Diagnostics.M136LiveObjectCountSummary
                {
                    peakRenderers = 20,
                    observedBranchSession = true,
                    observedCombatController = true,
                    observedBoss = false
                }
            };

            var summary = M140BuildRealReportGenerator.FromM136Result(
                "boss_entry",
                "Boss Entry",
                "macos-apple-silicon",
                M140BuildKind.Development,
                result,
                new M140VisualValidationSummary { passed = true },
                enforceTiming: false);

            Assert.IsFalse(summary.passed);
            StringAssert.Contains("Boss-entry", string.Join("\n", summary.failures));
        }

        [Test]
        public void M140AcceptsBorderlineFrameCadenceWhenDeterministicAndCpuStagesAreClean()
        {
            var summary = M140BuildRealReportGenerator.FromM138Summary(
                new M138CombatScaleStressScenarioSummary
                {
                    scenarioId = "boss_plus_adds",
                    displayName = "Boss Plus Adds",
                    passed = true,
                    timingAuthoritative = true,
                    frameCadenceConfidence = Hollow.Diagnostics.M136FrameCadencePolicy.Trusted,
                    rawSampleCount = 1800,
                    frameP95Ms = 17.6d,
                    frameMaxMs = 24.1d,
                    cpuWorkMetricSupported = false,
                    cpuWorkP95Ms = 17.59d,
                    cpuWorkMaxMs = 24.03d,
                    gcMaxBytes = 0,
                    peakActiveEnemies = 13,
                    observedBoss = true,
                    cpuStageSummary = "tactical_director count=30 maxMs=1.328 maxGc=0; add_ai_think_scorer count=32 maxMs=1.35 maxGc=0"
                },
                "macos-apple-silicon",
                M140BuildKind.Development,
                new M140VisualValidationSummary { passed = true },
                enforceTiming: true,
                new Hollow.Diagnostics.M136LiveObjectCountSnapshot
                {
                    activeEnemies = 13,
                    activeRenderers = 80,
                    observedCombatController = true,
                    observedBoss = true
                });

            Assert.IsTrue(summary.passed, string.Join("\n", summary.failures));
            StringAssert.Contains("Accepted borderline", summary.note);
        }

        [Test]
        public void M140AcceptsNestedM138TimingOnlyFailureWhenCleanJitterRulePasses()
        {
            var summary = M140BuildRealReportGenerator.FromM138Summary(
                new M138CombatScaleStressScenarioSummary
                {
                    scenarioId = "boss_plus_adds",
                    displayName = "Boss Plus Adds",
                    passed = false,
                    failures = new[] { "Trusted frame p95 17.12 ms exceeds 16.7 ms." },
                    timingAuthoritative = true,
                    frameCadenceConfidence = Hollow.Diagnostics.M136FrameCadencePolicy.Trusted,
                    rawSampleCount = 1800,
                    frameP95Ms = 17.12d,
                    frameMaxMs = 24.1d,
                    cpuWorkMetricSupported = false,
                    gcMaxBytes = 0,
                    peakActiveEnemies = 13,
                    observedBoss = true,
                    cpuStageSummary = "boss_ai_update count=1791 maxMs=1.2 maxGc=0; tactical_director count=30 maxMs=1.1 maxGc=0; add_ai_think_scorer count=32 maxMs=1.35 maxGc=0"
                },
                "macos-apple-silicon",
                M140BuildKind.Development,
                new M140VisualValidationSummary { passed = true },
                enforceTiming: true,
                new Hollow.Diagnostics.M136LiveObjectCountSnapshot
                {
                    activeEnemies = 13,
                    activeRenderers = 80,
                    observedCombatController = true,
                    observedBoss = true
                });

            Assert.IsTrue(summary.passed, string.Join("\n", summary.failures));
            Assert.IsTrue(summary.m138GatePassed);
            StringAssert.Contains("Nested M138 frame-p95-only failure accepted", summary.note);
        }

        [Test]
        public void M140RejectsNestedM138DeterministicFailuresEvenWhenFrameJitterIsClean()
        {
            var summary = M140BuildRealReportGenerator.FromM138Summary(
                new M138CombatScaleStressScenarioSummary
                {
                    scenarioId = "boss_plus_adds",
                    displayName = "Boss Plus Adds",
                    passed = false,
                    failures = new[] { "Max path solves in one frame 4 exceeds budget 2." },
                    timingAuthoritative = true,
                    frameCadenceConfidence = Hollow.Diagnostics.M136FrameCadencePolicy.Trusted,
                    rawSampleCount = 1800,
                    frameP95Ms = 17.12d,
                    frameMaxMs = 24.1d,
                    cpuWorkMetricSupported = false,
                    gcMaxBytes = 0,
                    peakActiveEnemies = 13,
                    observedBoss = true,
                    cpuStageSummary = "boss_ai_update count=1791 maxMs=1.2 maxGc=0; tactical_director count=30 maxMs=1.1 maxGc=0; add_ai_think_scorer count=32 maxMs=1.35 maxGc=0"
                },
                "macos-apple-silicon",
                M140BuildKind.Development,
                new M140VisualValidationSummary { passed = true },
                enforceTiming: true,
                new Hollow.Diagnostics.M136LiveObjectCountSnapshot
                {
                    activeEnemies = 13,
                    activeRenderers = 80,
                    observedCombatController = true,
                    observedBoss = true
                });

            Assert.IsFalse(summary.passed);
            Assert.IsFalse(summary.m138GatePassed);
            StringAssert.Contains("M138 scenario gate failed", string.Join("\n", summary.failures));
        }

        [Test]
        public void M140RejectsFrameCadenceAboveCleanJitterCeilingEvenWhenStagesAreClean()
        {
            var summary = M140BuildRealReportGenerator.FromM138Summary(
                new M138CombatScaleStressScenarioSummary
                {
                    scenarioId = "boss_plus_adds",
                    displayName = "Boss Plus Adds",
                    passed = true,
                    timingAuthoritative = true,
                    frameCadenceConfidence = Hollow.Diagnostics.M136FrameCadencePolicy.Trusted,
                    rawSampleCount = 1800,
                    frameP95Ms = 18.1d,
                    frameMaxMs = 24.1d,
                    gcMaxBytes = 0,
                    peakActiveEnemies = 13,
                    observedBoss = true,
                    cpuStageSummary = "tactical_director count=30 maxMs=1.328 maxGc=0; add_ai_think_scorer count=32 maxMs=1.35 maxGc=0"
                },
                "macos-apple-silicon",
                M140BuildKind.Development,
                new M140VisualValidationSummary { passed = true },
                enforceTiming: true,
                new Hollow.Diagnostics.M136LiveObjectCountSnapshot
                {
                    activeEnemies = 13,
                    activeRenderers = 80,
                    observedCombatController = true,
                    observedBoss = true
                });

            Assert.IsFalse(summary.passed);
            StringAssert.Contains("Frame p95", string.Join("\n", summary.failures));
        }

        [Test]
        public void M140RejectsBorderlineFrameCadenceWhenCpuStagesAreNotClean()
        {
            var summary = M140BuildRealReportGenerator.FromM138Summary(
                new M138CombatScaleStressScenarioSummary
                {
                    scenarioId = "boss_plus_adds",
                    displayName = "Boss Plus Adds",
                    passed = true,
                    timingAuthoritative = true,
                    frameCadenceConfidence = Hollow.Diagnostics.M136FrameCadencePolicy.Trusted,
                    rawSampleCount = 1800,
                    frameP95Ms = 16.9d,
                    frameMaxMs = 22.1d,
                    gcMaxBytes = 0,
                    peakActiveEnemies = 13,
                    observedBoss = true,
                    cpuStageSummary = "add_ai_think_scorer count=1 maxMs=26.527 maxGc=0"
                },
                "macos-apple-silicon",
                M140BuildKind.Development,
                new M140VisualValidationSummary { passed = true },
                enforceTiming: true,
                new Hollow.Diagnostics.M136LiveObjectCountSnapshot
                {
                    activeEnemies = 13,
                    activeRenderers = 80,
                    observedCombatController = true,
                    observedBoss = true
                });

            Assert.IsFalse(summary.passed);
            StringAssert.Contains("Frame p95", string.Join("\n", summary.failures));
        }

        [Test]
        public void PlayerLogValidatorFailsShaderMaterialAddressablesAndExceptions()
        {
            var path = Path.Combine(tempDirectory, "Player.log");
            File.WriteAllText(path, "[M140] ScenarioStart boss_plus_adds (8/14)\nNullReferenceException\nShader error\npink material\nAddressables Exception\nThe referenced script on this Behaviour is missing!\n[M140] ScenarioEnd boss_plus_adds FAIL\n");

            var summary = M140PlayerLogValidator.Validate(path);

            Assert.IsFalse(summary.passed);
            Assert.Greater(summary.exceptionCount, 0);
            Assert.Greater(summary.shaderIssueCount, 0);
            Assert.Greater(summary.materialIssueCount, 0);
            Assert.Greater(summary.addressablesIssueCount, 0);
            Assert.AreEqual(1, summary.missingScriptWarningCount);
            Assert.AreEqual(0, summary.startupMissingScriptWarningCount);
            Assert.AreEqual(1, summary.scenarioMissingScriptWarningCount);
            Assert.IsNotEmpty(summary.missingScriptWarningContextLines);
            StringAssert.Contains("referenced script", summary.missingScriptWarningContextLines[0]);
        }

        [Test]
        public void PlayerLogValidatorReportsStartupMissingScriptsWithoutFailingGameplayScenarios()
        {
            var path = Path.Combine(tempDirectory, "PlayerStartup.log");
            File.WriteAllText(path, "Initialize engine version\nThe referenced script (Unknown) on this Behaviour is missing!\nThe referenced script on this Behaviour (Game Object '<null>') is missing!\n[M140] ScenarioStart boot_loading_screen (1/3)\n[M140] ScenarioEnd boot_loading_screen PASS\n");

            var summary = M140PlayerLogValidator.Validate(path);

            Assert.IsTrue(summary.passed);
            Assert.AreEqual(2, summary.missingScriptWarningCount);
            Assert.AreEqual(2, summary.startupMissingScriptWarningCount);
            Assert.AreEqual(0, summary.scenarioMissingScriptWarningCount);
        }

        [Test]
        public void PlayerLogValidatorFailsScenarioRuntimeNavMeshFallbackWarnings()
        {
            var path = Path.Combine(tempDirectory, "PlayerRuntimeNavMeshFallback.log");
            File.WriteAllText(path, "Initialize engine version\n[M140] ScenarioStart boss_plus_adds (8/11)\nRoom 'm138_boss_plus_adds' is using a dev-only runtime Unity NavMesh fallback because no catalog bake was found.\n[M140] ScenarioEnd boss_plus_adds FAIL\n");

            var summary = M140PlayerLogValidator.Validate(path);

            Assert.IsFalse(summary.passed);
            Assert.AreEqual(1, summary.runtimeNavMeshFallbackWarningCount);
            Assert.AreEqual(0, summary.startupRuntimeNavMeshFallbackWarningCount);
            Assert.AreEqual(1, summary.scenarioRuntimeNavMeshFallbackWarningCount);
            Assert.IsNotEmpty(summary.runtimeNavMeshFallbackWarningContextLines);
            StringAssert.Contains("runtime NavMesh fallback", string.Join("\n", summary.failures));
        }

        [Test]
        public void PlayerLogValidatorReportsStartupRuntimeNavMeshFallbackWithoutPassingItAsScenarioClean()
        {
            var path = Path.Combine(tempDirectory, "PlayerStartupRuntimeNavMeshFallback.log");
            File.WriteAllText(path, "Room 'startup_probe' is using a dev-only runtime Unity NavMesh fallback because no catalog bake was found.\n[M140] ScenarioStart boot_loading_screen (1/3)\n[M140] ScenarioEnd boot_loading_screen PASS\n");

            var summary = M140PlayerLogValidator.Validate(path);

            Assert.IsTrue(summary.passed);
            Assert.AreEqual(1, summary.runtimeNavMeshFallbackWarningCount);
            Assert.AreEqual(1, summary.startupRuntimeNavMeshFallbackWarningCount);
            Assert.AreEqual(0, summary.scenarioRuntimeNavMeshFallbackWarningCount);
        }

        [Test]
        public void M140RunnerRejectsStaleCaptureReportFile()
        {
            var path = Path.Combine(tempDirectory, M140BuildRealReportGenerator.DefaultJsonFileName);
            var captureStartedUtc = DateTime.UtcNow;
            File.WriteAllText(path, "{}");
            File.SetLastWriteTimeUtc(path, captureStartedUtc.AddMinutes(-5));

            var fresh = M140BuildRealGateRunner.IsFreshCaptureReportForTests(path, captureStartedUtc, out var detail);

            Assert.IsFalse(fresh);
            StringAssert.Contains("predates capture start", detail);
        }

        [Test]
        public void M140RunnerRejectsStaleGeneratedReportMetadata()
        {
            var captureStartedUtc = DateTime.UtcNow;
            var report = new M140BuildRealReport
            {
                generatedAtUtc = captureStartedUtc.AddMinutes(-5).ToString("O")
            };

            var fresh = M140BuildRealGateRunner.IsFreshGeneratedReportForTests(report, captureStartedUtc, out var detail);

            Assert.IsFalse(fresh);
            StringAssert.Contains("predates capture start", detail);
        }

        [Test]
        public void MarkdownSeparatesPreloadBuildAttributionFromPostLoadMisses()
        {
            var report = new M140BuildRealReport
            {
                result = M140GateResult.Passed,
                passed = true,
                platformId = "macos-apple-silicon",
                buildKind = M140BuildKind.Development,
                playerLog = new M140PlayerLogValidationSummary { passed = true },
                renderRuntime = new M140RenderRuntimeSnapshot { targetFrameRate = 60, vSyncCount = 0 },
                scenarios = new[]
                {
                    new M140ScenarioSummary
                    {
                        scenarioId = "normal_traversal",
                        displayName = "Normal Traversal",
                        passed = true,
                        cacheMissAttributionSummary = "presentation|preload|role=floor",
                        preloadBuildCacheMissAttributionSummary = "presentation|preload|role=floor",
                        postLoadCacheMissAttributionSummary = string.Empty
                    }
                }
            };

            var markdown = M140BuildRealReportGenerator.ToMarkdown(report);

            Assert.That(markdown, Does.Not.Contain("Cache miss attribution"));
            Assert.That(markdown, Does.Not.Contain("Preload/build attribution"));
            Assert.That(markdown, Does.Not.Contain("Post-load traversal miss attribution"));
        }

        [Test]
        public void PerformanceComparisonReportsCrowdCounterReductionsAndRegressions()
        {
            var passing = PerformanceComparisonReportGenerator.BuildComparison(
                "before",
                "after",
                BuildM138ComparisonReport(reservationCandidates: 1000, reservationPathSolves: 1000, passed: true),
                BuildM138ComparisonReport(reservationCandidates: 100, reservationPathSolves: 100, passed: true),
                BuildM140ComparisonReport(passScenarios: true),
                BuildM140ComparisonReport(passScenarios: true));

            Assert.IsTrue(passing.passed, string.Join("; ", passing.failures));
            var enemyStress = passing.scenarios.First(scenario => scenario.scenarioId == "enemy_stress_30");
            Assert.AreEqual(90d, enemyStress.reservationCandidateReductionPercent, 0.001d);
            Assert.AreEqual(90d, enemyStress.reservationPathSolveReductionPercent, 0.001d);

            var failing = PerformanceComparisonReportGenerator.BuildComparison(
                "before",
                "after",
                BuildM138ComparisonReport(reservationCandidates: 1000, reservationPathSolves: 1000, passed: true),
                BuildM138ComparisonReport(reservationCandidates: 600, reservationPathSolves: 600, passed: true),
                BuildM140ComparisonReport(passScenarios: true),
                BuildM140ComparisonReport(passScenarios: false));

            Assert.IsFalse(failing.passed);
            Assert.That(string.Join("\n", failing.failures), Does.Contain("reservation candidate reduction"));
            Assert.That(string.Join("\n", failing.failures), Does.Contain("boss_plus_adds"));
        }

        [Test]
        public void PerformanceComparisonAllowsLegacyBaselineWithoutTacticalCounters()
        {
            var report = PerformanceComparisonReportGenerator.BuildComparison(
                "legacy-before",
                "after",
                BuildLegacyM138ComparisonReport(passed: true),
                BuildM138ComparisonReport(reservationCandidates: 100, reservationPathSolves: 100, passed: true),
                BuildM140ComparisonReport(passScenarios: true),
                BuildM140ComparisonReport(passScenarios: true));

            Assert.IsTrue(report.passed, string.Join("; ", report.failures));
            var enemyStress = report.scenarios.First(scenario => scenario.scenarioId == "enemy_stress_30");
            Assert.IsFalse(enemyStress.baselineReservationCountersAvailable);
            Assert.That(enemyStress.note, Does.Contain("Baseline predates tactical reservation counters"));
            Assert.Greater(enemyStress.candidateCrowdReservationSkips, 0);
        }

        [Test]
        public void WindowsArtifactImporterValidatesPassingWindowsReport()
        {
            var source = Path.Combine(tempDirectory, "windows_source");
            Directory.CreateDirectory(source);
            var report = M140BuildRealReportGenerator.BuildReport(
                "windows-x64",
                M140BuildKind.Development,
                source,
                BuildPassingScenarioSet("windows-x64", M140BuildKind.Development),
                new M140RenderRuntimeSnapshot { targetFrameRate = 60, vSyncCount = 0 },
                new M140PlayerLogValidationSummary { passed = true });
            M140BuildRealReportGenerator.WriteReport(
                report,
                Path.Combine(source, M140BuildRealReportGenerator.DefaultJsonFileName),
                Path.Combine(source, M140BuildRealReportGenerator.DefaultMarkdownFileName));

            var result = M140PlayerArtifactImporter.ImportWindowsArtifacts(source, tempDirectory);

            Assert.AreEqual(PlatformBuildQaResult.Passed, result.result);
            Assert.IsTrue(File.Exists(Path.Combine(tempDirectory, "imported_windows", M140BuildRealReportGenerator.DefaultJsonFileName)));
        }

        [Test]
        public void WholeGameAuditCanDetectM140ImplementationSurface()
        {
            Assert.IsTrue(File.Exists("Assets/_Hollow/Scripts/Hollow.Performance/M140BuiltPlayerCaptureRunner.cs"));
            Assert.IsTrue(File.Exists("Assets/_Hollow/Scripts/Hollow.Performance/M140BuildRealGateReport.cs"));
            Assert.IsTrue(File.Exists("Assets/_Hollow/Scripts/Hollow.Editor/Build/M140BuildRealGateRunner.cs"));
            Assert.IsTrue(File.Exists("Assets/_Hollow/Scripts/Hollow.Data/Definitions/M140BuildRealGateProfileDefinition.cs"));
        }

        private string WriteTexture(string fileName, Color color)
        {
            var path = Path.Combine(tempDirectory, fileName);
            var texture = new Texture2D(16, 16, TextureFormat.RGBA32, false);
            try
            {
                var pixels = new Color[16 * 16];
                for (var index = 0; index < pixels.Length; index++)
                {
                    pixels[index] = color;
                }

                texture.SetPixels(pixels);
                texture.Apply();
                File.WriteAllBytes(path, texture.EncodeToPNG());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(texture);
            }

            return path;
        }

        private static M140ScenarioSummary[] BuildPassingScenarioSet(string platformId, string buildKind)
        {
            var scenarios = new M140ScenarioSummary[M140BuildRealReportGenerator.RequiredDevelopmentScenarioIds.Length];
            for (var index = 0; index < scenarios.Length; index++)
            {
                var id = M140BuildRealReportGenerator.RequiredDevelopmentScenarioIds[index];
                scenarios[index] = new M140ScenarioSummary
                {
                    scenarioId = id,
                    displayName = id,
                    platformId = platformId,
                    buildKind = buildKind,
                    passed = true,
                    failures = Array.Empty<string>()
                };
            }

            return scenarios;
        }

        private static M138CombatScaleStressReport BuildM138ComparisonReport(
            int reservationCandidates,
            int reservationPathSolves,
            bool passed)
        {
            return new M138CombatScaleStressReport
            {
                lockId = M138CombatScaleStressScenarioPolicy.LockId,
                title = "comparison-test",
                generatedAtUtc = DateTime.UtcNow.ToString("O"),
                passed = passed,
                scenarioCount = 2,
                failures = Array.Empty<string>(),
                scenarios = new[]
                {
                    M138ComparisonScenario("enemy_stress_30", reservationCandidates, reservationPathSolves, passed),
                    M138ComparisonScenario("projectile_heavy_room", reservationCandidates, reservationPathSolves, passed)
                }
            };
        }

        private static M138CombatScaleStressReport BuildLegacyM138ComparisonReport(bool passed)
        {
            return new M138CombatScaleStressReport
            {
                lockId = M138CombatScaleStressScenarioPolicy.LockId,
                title = "legacy-comparison-test",
                generatedAtUtc = DateTime.UtcNow.ToString("O"),
                passed = passed,
                scenarioCount = 2,
                failures = Array.Empty<string>(),
                scenarios = new[]
                {
                    M138LegacyComparisonScenario("enemy_stress_30", passed),
                    M138LegacyComparisonScenario("projectile_heavy_room", passed)
                }
            };
        }

        private static M138CombatScaleStressScenarioSummary M138ComparisonScenario(
            string id,
            int reservationCandidates,
            int reservationPathSolves,
            bool passed)
        {
            return new M138CombatScaleStressScenarioSummary
            {
                scenarioId = id,
                displayName = id,
                frameP95Ms = 12d,
                frameMaxMs = 18d,
                aiScorerCalls = reservationCandidates / 10,
                aiBehaviorGraphTicks = reservationCandidates / 5,
                navPathRequests = reservationPathSolves,
                navPathSolves = reservationPathSolves,
                tacticalDirectorSummary = $"reservationCandidatesChecked={reservationCandidates}; reservationPathSolves={reservationPathSolves};",
                tacticalCrowdReservationSkips = passed ? 50 : 0,
                tacticalCrowdCachedIntentReuses = passed ? 100 : 0,
                tacticalCrowdScorerSkips = passed ? 40 : 0,
                passed = passed,
                failures = passed ? Array.Empty<string>() : new[] { "synthetic failure" }
            };
        }

        private static M138CombatScaleStressScenarioSummary M138LegacyComparisonScenario(string id, bool passed)
        {
            return new M138CombatScaleStressScenarioSummary
            {
                scenarioId = id,
                displayName = id,
                frameP95Ms = 35d,
                frameMaxMs = 70d,
                aiScorerCalls = 2000,
                aiBehaviorGraphTicks = 4000,
                navPathRequests = 1000,
                navPathSolves = 1000,
                tacticalDirectorSummary = string.Empty,
                passed = passed,
                failures = passed ? Array.Empty<string>() : new[] { "synthetic failure" }
            };
        }

        private static M140BuildRealReport BuildM140ComparisonReport(bool passScenarios)
        {
            var ids = new[]
            {
                "normal_traversal",
                "return_to_previous_room",
                "reward_room",
                "boss_entry",
                "boss_plus_adds"
            };
            return new M140BuildRealReport
            {
                lockId = M140BuildRealReportGenerator.LockId,
                title = "comparison-test",
                generatedAtUtc = DateTime.UtcNow.ToString("O"),
                platformId = "macos-apple-silicon",
                buildKind = M140BuildKind.Development,
                result = passScenarios ? M140GateResult.Passed : M140GateResult.Failed,
                passed = passScenarios,
                scenarioCount = ids.Length,
                scenarios = ids.Select(id => new M140ScenarioSummary
                {
                    scenarioId = id,
                    displayName = id,
                    platformId = "macos-apple-silicon",
                    buildKind = M140BuildKind.Development,
                    frameP95Ms = passScenarios ? 12d : 19d,
                    frameMaxMs = passScenarios ? 20d : 55d,
                    tacticalDirectorSummary = "reservationCandidatesChecked=0; reservationPathSolves=0;",
                    passed = passScenarios,
                    failures = passScenarios ? Array.Empty<string>() : new[] { "synthetic failure" }
                }).ToArray(),
                failures = passScenarios ? Array.Empty<string>() : new[] { "synthetic failure" }
            };
        }
    }
}
