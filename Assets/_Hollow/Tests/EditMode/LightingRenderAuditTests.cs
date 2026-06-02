using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Hollow.Data.Definitions;
using Hollow.Performance;
using NUnit.Framework;
using UnityEngine;

namespace Hollow.Tests.EditMode
{
    public sealed class LightingRenderAuditTests
    {
        [Test]
        public void OptionsParseLightingAuditCommandLine()
        {
            var args = new[]
            {
                "Player",
                "--hollow-lighting-audit",
                "--hollow-lighting-audit-auto-exit",
                "--hollow-lighting-audit-output",
                "output/custom_lighting_audit",
                "--hollow-lighting-audit-platform=macos-apple-silicon",
                "--hollow-lighting-audit-mode=full",
                "--hollow-lighting-audit-scenarios=biome_verdant_props;biome_transition_sweep",
                "--hollow-lighting-audit-fps-cap=72"
            };

            Assert.IsTrue(LightingRenderAuditOptions.TryParse(args, out var options));
            Assert.AreEqual("output/custom_lighting_audit", options.outputRoot);
            Assert.AreEqual("macos-apple-silicon", options.platformId);
            Assert.AreEqual(LightingRenderAuditMode.Full, options.mode);
            Assert.IsTrue(options.autoExit);
            Assert.AreEqual(72, options.targetFrameRate);
            CollectionAssert.AreEqual(new[]
            {
                LightingRenderAuditScenarioPolicy.VerdantProps,
                LightingRenderAuditScenarioPolicy.TransitionSweep
            }, options.scenarioIds);
        }

        [Test]
        public void ScenarioManifestContainsRequiredV1Scenarios()
        {
            var ids = new HashSet<string>(LightingRenderAuditScenarioPolicy.Manifest.Select(scenario => scenario.id), StringComparer.Ordinal);
            CollectionAssert.IsSubsetOf(new[]
            {
                LightingRenderAuditScenarioPolicy.HollowThresholdBaseline,
                LightingRenderAuditScenarioPolicy.VerdantProps,
                LightingRenderAuditScenarioPolicy.AshenProjectiles,
                LightingRenderAuditScenarioPolicy.SunkenManyLights,
                LightingRenderAuditScenarioPolicy.RustManyEnemies,
                LightingRenderAuditScenarioPolicy.TransitionSweep
            }, ids);
        }

        [Test]
        public void FullModeDefaultsToAllScenariosWhenNoExplicitScenarioListIsProvided()
        {
            var args = new[]
            {
                "Player",
                "--hollow-lighting-audit",
                "--hollow-lighting-audit-mode=full"
            };

            Assert.IsTrue(LightingRenderAuditOptions.TryParse(args, out var options));
            CollectionAssert.AreEqual(LightingRenderAuditScenarioPolicy.AllScenarioIds, options.scenarioIds);
        }

        [Test]
        public void ReportGeneratorPassesCleanScenarioSummary()
        {
            var summary = CreateScenarioSummary(
                LightingRenderAuditScenarioPolicy.HollowThresholdBaseline,
                RoomBiomeIds.HollowThreshold,
                activeLocalLights: 2,
                maxActiveLocalLights: 4,
                shadowedLights: 1,
                maxShadowedLights: 1,
                activeEffectLights: 0,
                maxEffectLights: 2,
                activePropLights: 1,
                maxPropLights: 4,
                visualPassed: true);

            var report = LightingRenderAuditReportGenerator.BuildReport(
                LightingRenderAuditOptions.Smoke("output/reports/lighting_render_audit/editmode_clean"),
                new List<LightingRenderAuditScenarioSummary> { summary });

            Assert.IsTrue(GetField<bool>(report, "passed"));
            Assert.AreEqual(0, GetField<string[]>(report, "failures").Length);
        }

        [Test]
        public void ReportGeneratorFailsWhenBudgetsAreExceeded()
        {
            var summary = CreateScenarioSummary(
                LightingRenderAuditScenarioPolicy.SunkenManyLights,
                RoomBiomeIds.SunkenCartouche,
                activeLocalLights: 12,
                maxActiveLocalLights: 4,
                shadowedLights: 3,
                maxShadowedLights: 1,
                activeEffectLights: 5,
                maxEffectLights: 2,
                activePropLights: 9,
                maxPropLights: 4,
                visualPassed: true);

            var report = LightingRenderAuditReportGenerator.BuildReport(
                LightingRenderAuditOptions.Full("output/reports/lighting_render_audit/editmode_budget_fail"),
                new List<LightingRenderAuditScenarioSummary> { summary });

            Assert.IsFalse(GetField<bool>(report, "passed"));
            Assert.That(GetField<string[]>(report, "failures"), Is.Not.Empty);
        }

        private static LightingRenderAuditScenarioSummary CreateScenarioSummary(
            string scenarioId,
            string biomeId,
            int activeLocalLights,
            int maxActiveLocalLights,
            int shadowedLights,
            int maxShadowedLights,
            int activeEffectLights,
            int maxEffectLights,
            int activePropLights,
            int maxPropLights,
            bool visualPassed)
        {
            var visual = new M140VisualValidationSummary
            {
                screenshotPath = Path.Combine("output", scenarioId + ".png"),
                exists = true,
                loadable = true,
                width = 640,
                height = 360,
                averageLuminance = 0.35f,
                passed = visualPassed,
                failures = visualPassed ? Array.Empty<string>() : new[] { "Synthetic visual failure." }
            };

            var summary = new LightingRenderAuditScenarioSummary();
            SetField(summary, "scenarioId", scenarioId);
            SetField(summary, "displayName", scenarioId);
            SetField(summary, "requestedBiomeId", biomeId);
            SetField(summary, "appliedBiomeId", biomeId);
            SetField(summary, "profileId", biomeId + "_lighting");
            SetField(summary, "passed", true);
            SetField(summary, "screenshotPath", visual.screenshotPath);
            SetField(summary, "visual", visual);
            SetField(summary, "sampleCount", 16);
            SetField(summary, "frameP50Ms", 16.6d);
            SetField(summary, "frameP95Ms", 17.2d);
            SetField(summary, "frameMaxMs", 19.4d);
            SetField(summary, "peakActiveLights", activeLocalLights + 1);
            SetField(summary, "peakActiveLocalLights", activeLocalLights);
            SetField(summary, "peakShadowedLights", shadowedLights);
            SetField(summary, "peakActivePropLights", activePropLights);
            SetField(summary, "peakActiveDynamicEffectLights", activeEffectLights);
            SetField(summary, "maxActiveLocalLights", maxActiveLocalLights);
            SetField(summary, "maxShadowedLocalLights", maxShadowedLights);
            SetField(summary, "maxPropLights", maxPropLights);
            SetField(summary, "maxDynamicEffectLights", maxEffectLights);
            SetField(summary, "failures", Array.Empty<string>());
            SetField(summary, "samples", Array.Empty<LightingRenderAuditFrameSample>());
            return summary;
        }

        private static void SetField<T>(object target, string name, T value)
        {
            var field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.Public);
            Assert.NotNull(field, $"Expected `{target.GetType().Name}.{name}` to exist.");
            field.SetValue(target, value);
        }

        private static T GetField<T>(object target, string name)
        {
            var field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.Public);
            Assert.NotNull(field, $"Expected `{target.GetType().Name}.{name}` to exist.");
            return (T)field.GetValue(target);
        }
    }
}
