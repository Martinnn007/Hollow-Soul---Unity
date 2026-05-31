using System;
using System.Collections;
using System.IO;
using System.Linq;
using Hollow.Performance;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace Hollow.Tests.PlayMode
{
    public sealed class AutomatedTruthGateSmokeTests
    {
        [UnityTest]
        [Timeout(360000)]
        public IEnumerator RunsCombinedSmokeGateWithoutManualGameplay()
        {
            var jsonPath = "output/reports/truth_gate/automated_truth_gate_playmode_smoke.json";
            var markdownPath = "output/reports/truth_gate/automated_truth_gate_playmode_smoke.md";
            AutomatedTruthGateReport report = null;

            yield return AutomatedTruthGatePlayModeRunner.Run(
                AutomatedTruthGateOptions.SmokeGate(jsonPath, markdownPath),
                next => report = next);

            Assert.IsNotNull(report);
            Assert.AreEqual(2, report.stageCount);
            Assert.IsTrue(File.Exists(jsonPath), jsonPath);
            Assert.IsTrue(File.Exists(markdownPath), markdownPath);
            Assert.IsTrue(report.stages.Any(stage => stage.stageId == "m138_combat_scale_stress"));
            Assert.IsTrue(report.stages.Any(stage => stage.stageId == "m139_long_run_soak"));
            Assert.IsTrue(report.passed, string.Join("\n", report.failures ?? Array.Empty<string>()));
        }
    }
}
