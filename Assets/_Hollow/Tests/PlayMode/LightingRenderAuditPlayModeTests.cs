using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using Hollow.Performance;
using NUnit.Framework;
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
                LightingRenderAuditScenarioPolicy.TransitionSweep
            };
            options.writeReports = true;
            options.targetFrameRate = 60;

            LightingRenderAuditReport report = null;
            yield return LightingRenderAuditRunner.RunAllScenarios(options, next => report = next);

            Assert.NotNull(report);
            Assert.IsTrue(File.Exists(options.JsonReportPath), "Expected JSON report to be written.");
            Assert.IsTrue(File.Exists(options.MarkdownReportPath), "Expected Markdown report to be written.");

            var scenarios = GetEnumerableField(report, "scenarios").Cast<object>().ToArray();
            Assert.AreEqual(2, scenarios.Length);
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
