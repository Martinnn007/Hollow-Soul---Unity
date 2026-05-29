using System.Collections;
using System.IO;
using System.Linq;
using Hollow.Core.Diagnostics;
using Hollow.Performance;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace Hollow.Tests.PlayMode
{
    public sealed class M138AutomatedStressSmokeTests
    {
        [UnityTest]
        [Timeout(120000)]
        public IEnumerator RunsAllM138StressScenariosAndWritesPassingReport()
        {
            var jsonPath = "output/reports/m138_combat_scale_stress_playmode_smoke.json";
            var markdownPath = "output/reports/m138_combat_scale_stress_playmode_smoke.md";
            M138CombatScaleStressReport report = null;

            yield return M138CombatScaleStressRunner.RunAllScenarios(
                M138CombatScaleStressRunOptions.SmokeGate(jsonPath, markdownPath),
                nextReport => report = nextReport);

            Assert.IsNotNull(report);
            Assert.AreEqual(6, report.scenarioCount);
            Assert.IsTrue(File.Exists(jsonPath), jsonPath);
            Assert.IsTrue(File.Exists(markdownPath), markdownPath);
            Assert.IsTrue(report.passed, string.Join("\n", report.failures ?? new string[0]));

            var enemy30 = report.scenarios.FirstOrDefault(scenario => scenario.scenarioId == "enemy_stress_30");
            Assert.IsNotNull(enemy30);
            Assert.GreaterOrEqual(enemy30.peakActiveEnemies, 30);
            Assert.LessOrEqual(enemy30.maxPathSolvesInFrame, M137PerformanceComfortPolicy.M3NavMeshPathSolveBudgetPerFrame);

            var projectile = report.scenarios.FirstOrDefault(scenario => scenario.scenarioId == "projectile_heavy_room");
            Assert.IsNotNull(projectile);
            Assert.Greater(projectile.peakProjectiles, 0);

            var boss = report.scenarios.FirstOrDefault(scenario => scenario.scenarioId == "boss_plus_adds");
            Assert.IsNotNull(boss);
            Assert.IsTrue(boss.observedBoss);
            Assert.IsTrue(boss.bossFullLodObserved);
        }
    }
}
