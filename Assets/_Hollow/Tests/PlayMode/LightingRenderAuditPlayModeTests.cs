using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using Hollow.Presentation;
using Hollow.Performance;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;

namespace Hollow.Tests.PlayMode
{
    public sealed class LightingRenderAuditPlayModeTests
    {
        [UnityTest]
        [Timeout(180000)]
        public IEnumerator RuntimeLightingAuditSmokeWritesReportsAndScenarioEvidence()
        {
            var outputRoot = Path.GetFullPath(Path.Combine("output", "reports", "lighting_render_audit", "playmode_smoke"));
            if (Directory.Exists(outputRoot))
            {
                Directory.Delete(outputRoot, true);
            }

            var options = LightingRenderAuditOptions.Smoke(outputRoot);
            options.scenarioIds = new[]
            {
                LightingRenderAuditScenarioPolicy.HollowThresholdBaseline,
                LightingRenderAuditScenarioPolicy.TransitionSweep,
                LightingRenderAuditScenarioPolicy.BranchTraversalPrewarm
            };
            options.writeReports = true;
            options.targetFrameRate = 60;

            LightingRenderAuditReport report = null;
            yield return LightingRenderAuditRunner.RunAllScenarios(options, next => report = next);

            Assert.NotNull(report);
            Assert.IsTrue(File.Exists(options.JsonReportPath), "Expected JSON report to be written.");
            Assert.IsTrue(File.Exists(options.MarkdownReportPath), "Expected Markdown report to be written.");

            var scenarios = GetEnumerableField(report, "scenarios").Cast<object>().ToArray();
            Assert.AreEqual(3, scenarios.Length);
            foreach (var scenario in scenarios)
            {
                Assert.That(GetField<string>(scenario, "scenarioId"), Is.Not.Empty);
                Assert.That(GetField<string>(scenario, "appliedBiomeId"), Is.Not.Empty);
                Assert.That(GetField<string>(scenario, "profileId"), Is.Not.Empty);
                Assert.Greater(GetField<int>(scenario, "sampleCount"), 0);
                Assert.IsTrue(File.Exists(GetField<string>(scenario, "screenshotPath")), "Expected scenario screenshot to exist.");
                Assert.NotNull(GetField<object>(scenario, "lightingSnapshot"));
            }
        }

        [UnityTest]
        [Explicit("Manual render-audit pass: batch screenshot capture can hang on headless editor runners.")]
        [Timeout(180000)]
        public IEnumerator BranchLightingPrewarmTraversalPassesUnderCoolAndQualityProfiles()
        {
            var previousTargetFrameRate = Application.targetFrameRate;
            var previousVSyncCount = QualitySettings.vSyncCount;
            var previousPipeline = QualitySettings.renderPipeline;
            var hadPreviousPreference = PlayerPrefs.HasKey(RuntimeRenderProfileSettings.PlayerPrefsKey);
            var previousPreference = hadPreviousPreference ? PlayerPrefs.GetString(RuntimeRenderProfileSettings.PlayerPrefsKey) : string.Empty;

            try
            {
                foreach (var mode in new[] { RuntimeRenderProfileMode.Cool, RuntimeRenderProfileMode.Quality })
                {
                    PlayerPrefs.DeleteKey(RuntimeRenderProfileSettings.PlayerPrefsKey);
                    RuntimeRenderProfileSettings.ResetForTests();
                    RuntimeRenderProfileSettings.SetMode(mode, persist: false);
                    var outputRoot = Path.GetFullPath(Path.Combine("output", "reports", "lighting_render_audit", "playmode_branch_prewarm_" + mode.ToString().ToLowerInvariant()));
                    if (Directory.Exists(outputRoot))
                    {
                        Directory.Delete(outputRoot, true);
                    }

                    var options = LightingRenderAuditOptions.Smoke(outputRoot);
                    options.scenarioIds = new[] { LightingRenderAuditScenarioPolicy.BranchTraversalPrewarm };
                    options.writeReports = true;
                    options.targetFrameRate = 60;

                    LightingRenderAuditReport report = null;
                    yield return LightingRenderAuditRunner.RunAllScenarios(options, next => report = next);

                    Assert.NotNull(report);
                    var scenarios = GetEnumerableField(report, "scenarios").Cast<object>().ToArray();
                    Assert.AreEqual(1, scenarios.Length);
                    var scenario = scenarios[0];
                    Assert.AreEqual(LightingRenderAuditScenarioPolicy.BranchTraversalPrewarm, GetField<string>(scenario, "scenarioId"));
                    Assert.IsTrue(GetField<bool>(scenario, "prewarmPreparedBeforeReveal"), mode.ToString());
                    Assert.IsTrue(GetField<bool>(scenario, "prewarmGlobalAppliedBeforeReveal"), mode.ToString());
                    Assert.LessOrEqual(GetField<int>(scenario, "prewarmPreparedFrame"), GetField<int>(scenario, "prewarmRevealFrame"));
                    Assert.LessOrEqual(GetField<int>(scenario, "prewarmGlobalAppliedFrame"), GetField<int>(scenario, "prewarmRevealFrame"));
                    Assert.That(GetField<string[]>(scenario, "failures"), Is.Empty, mode.ToString());
                }
            }
            finally
            {
                Application.targetFrameRate = previousTargetFrameRate;
                QualitySettings.vSyncCount = previousVSyncCount;
                QualitySettings.renderPipeline = previousPipeline;
                if (hadPreviousPreference)
                {
                    PlayerPrefs.SetString(RuntimeRenderProfileSettings.PlayerPrefsKey, previousPreference);
                }
                else
                {
                    PlayerPrefs.DeleteKey(RuntimeRenderProfileSettings.PlayerPrefsKey);
                }

                RuntimeRenderProfileSettings.ResetForTests();
            }
        }

        private static IEnumerable GetEnumerableField(object target, string name)
        {
            return GetField<object>(target, name) as IEnumerable ?? Array.Empty<object>();
        }

        private static T GetField<T>(object target, string name)
        {
            var field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.Public);
            Assert.NotNull(field, $"Expected `{target.GetType().Name}.{name}` to exist.");
            return (T)field.GetValue(target);
        }
    }
}
