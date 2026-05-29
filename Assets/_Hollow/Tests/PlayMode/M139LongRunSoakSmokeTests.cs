using System.Collections;
using System.IO;
using System.Linq;
using Hollow.Performance;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace Hollow.Tests.PlayMode
{
    public sealed class M139LongRunSoakSmokeTests
    {
        [UnityTest]
        [Timeout(180000)]
        public IEnumerator RunsM139CiSmokeAndWritesSoakReport()
        {
            var jsonPath = "output/reports/m139_long_run_soak_playmode_smoke.json";
            var markdownPath = "output/reports/m139_long_run_soak_playmode_smoke.md";
            M139LongRunSoakReport report = null;

            yield return M139LongRunSoakRunner.RunAllScenarios(
                M139LongRunSoakOptions.SmokeGate(jsonPath, markdownPath),
                nextReport => report = nextReport);

            Assert.IsNotNull(report);
            Assert.AreEqual(M139LongRunSoakReportGenerator.ScenarioIds.Length, report.scenarioCount);
            Assert.IsTrue(File.Exists(jsonPath), jsonPath);
            Assert.IsTrue(File.Exists(markdownPath), markdownPath);
            Assert.IsTrue(report.passed, string.Join("\n", report.failures ?? new string[0]));

            var fresh = report.scenarios.FirstOrDefault(scenario => scenario.scenarioId == "fresh_multi_branch_soak");
            Assert.IsNotNull(fresh);
            Assert.GreaterOrEqual(fresh.roomTraversalsCompleted, 4);
            Assert.AreEqual(0, fresh.normalTraversalColdCacheMissesAfterLoad);

            var restore = report.scenarios.FirstOrDefault(scenario => scenario.scenarioId == "save_load_restore_soak");
            Assert.IsNotNull(restore);
            Assert.GreaterOrEqual(restore.saveLoadRestoresCompleted, 1);

            Assert.IsTrue(report.scenarios.All(scenario => scenario.staleEnemyStateFailures == 0));
            Assert.IsTrue(report.scenarios.All(scenario => scenario.runtimeNavMeshFallbacks == 0));
            Assert.IsTrue(report.scenarios.All(scenario => scenario.poolActiveLeaks == 0));
        }
    }
}
