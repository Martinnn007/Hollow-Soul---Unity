using System;
using System.IO;
using System.Linq;
using Hollow.Data.Definitions;
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
        public void PlayerLogValidatorFailsShaderMaterialAddressablesAndExceptions()
        {
            var path = Path.Combine(tempDirectory, "Player.log");
            File.WriteAllText(path, "NullReferenceException\nShader error\npink material\nAddressables Exception\nThe referenced script on this Behaviour is missing!\n");

            var summary = M140PlayerLogValidator.Validate(path);

            Assert.IsFalse(summary.passed);
            Assert.Greater(summary.exceptionCount, 0);
            Assert.Greater(summary.shaderIssueCount, 0);
            Assert.Greater(summary.materialIssueCount, 0);
            Assert.Greater(summary.addressablesIssueCount, 0);
            Assert.Greater(summary.missingScriptWarningCount, 0);
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
    }
}
