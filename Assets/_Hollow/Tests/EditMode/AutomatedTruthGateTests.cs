using System;
using System.IO;
using System.Linq;
using Hollow.Diagnostics;
using Hollow.Performance;
using NUnit.Framework;

namespace Hollow.Tests.EditMode
{
    public sealed class AutomatedTruthGateTests
    {
        [Test]
        public void AggregationPassesWhenSubReportsPass()
        {
            var stages = new[]
            {
                AutomatedTruthGateReportGenerator.FromM138Report(PassingM138Report(), "m138.json", "m138.md", 12d),
                AutomatedTruthGateReportGenerator.FromM139Report(PassingM139Report(), "m139.json", "m139.md", 18d),
                AutomatedTruthGateReportGenerator.FromM140EditorResult(M140GateResult.Passed, 11, Array.Empty<string>(), "m140.json", "m140.md", 24d)
            };

            var report = AutomatedTruthGateReportGenerator.BuildReport(AutomatedTruthGateOptions.FullGate(), stages);

            Assert.IsTrue(report.passed, string.Join("\n", report.failures ?? Array.Empty<string>()));
            Assert.AreEqual(M140GateResult.Passed, report.result);
            Assert.AreEqual(3, report.stageCount);
        }

        [Test]
        public void AggregationFailsColdMissesNavFallbackStaleStateLeaksAndHardInstantiates()
        {
            var m139 = PassingM139Report();
            var scenario = m139.scenarios[0];
            scenario.normalTraversalColdCacheMissesAfterLoad = 1;
            scenario.runtimeNavMeshFallbacks = 1;
            scenario.staleEnemyStateFailures = 1;
            scenario.poolActiveLeaks = 1;
            scenario.runtimePoolHardInstantiatesAfterWarmup = 1;

            var stage = AutomatedTruthGateReportGenerator.FromM139Report(m139, "m139.json", "m139.md", 1d);
            var report = AutomatedTruthGateReportGenerator.BuildReport(AutomatedTruthGateOptions.SmokeGate(), new[] { stage });

            Assert.IsFalse(report.passed);
            var failures = string.Join("\n", report.failures ?? Array.Empty<string>());
            StringAssert.Contains("cold-cache misses", failures);
            StringAssert.Contains("runtime NavMesh", failures);
            StringAssert.Contains("stale pooled state", failures);
            StringAssert.Contains("pool active leaks", failures);
            StringAssert.Contains("post-warmup hard instantiates", failures);
        }

        [Test]
        public void AggregationFailsMissingProjectilePressureBossEvidenceAndEnemyScale()
        {
            var m138 = PassingM138Report();
            m138.scenarios.First(scenario => scenario.scenarioId == "enemy_stress_30").peakActiveEnemies = 29;
            m138.scenarios.First(scenario => scenario.scenarioId == "projectile_heavy_room").projectileActivePeak = 0;
            m138.scenarios.First(scenario => scenario.scenarioId == "boss_plus_adds").observedBoss = false;

            var stage = AutomatedTruthGateReportGenerator.FromM138Report(m138, "m138.json", "m138.md", 1d);
            var report = AutomatedTruthGateReportGenerator.BuildReport(AutomatedTruthGateOptions.SmokeGate(), new[] { stage });

            Assert.IsFalse(report.passed);
            var failures = string.Join("\n", report.failures ?? Array.Empty<string>());
            StringAssert.Contains("enemy_stress_30", failures);
            StringAssert.Contains("projectile_heavy_room", failures);
            StringAssert.Contains("boss_plus_adds", failures);
        }

        [Test]
        public void OptionsSelectExpectedStageGroups()
        {
            var smoke = AutomatedTruthGateOptions.SmokeGate();
            Assert.IsTrue(smoke.includeM138);
            Assert.IsTrue(smoke.includeM139);
            Assert.IsFalse(smoke.includeBuiltPlayer);
            Assert.IsFalse(smoke.IsFull);
            StringAssert.Contains("automated_truth_gate_smoke", smoke.jsonReportPath);

            var full = AutomatedTruthGateOptions.FullGate();
            Assert.IsTrue(full.IsFull);
            StringAssert.Contains("automated_truth_gate_full", full.jsonReportPath);

            var built = AutomatedTruthGateOptions.BuiltPlayerGate();
            Assert.IsTrue(built.includeBuiltPlayer);
            Assert.IsFalse(built.includeM138);
            Assert.IsFalse(built.includeM139);
            Assert.IsTrue(built.IsBuiltPlayer);
            StringAssert.Contains("automated_truth_gate_built-player", built.jsonReportPath);
        }

        [Test]
        public void M139AggregationIncludesColdMissAttribution()
        {
            var m139 = PassingM139Report();
            m139.scenarios[0].normalTraversalColdCacheMissesAfterLoad = 1;
            m139.scenarios[0].cacheMissAttributionSummary = "1x traversal|live-room|room_02|normal_traversal";

            var stage = AutomatedTruthGateReportGenerator.FromM139Report(m139, "m139.json", "m139.md", 1d);

            Assert.IsFalse(stage.passed);
            StringAssert.Contains("room_02", string.Join("\n", stage.failures ?? Array.Empty<string>()));
        }

        [Test]
        public void WriteReportProducesJsonAndMarkdown()
        {
            var directory = Path.Combine("output/reports/truth_gate/tests");
            var json = Path.Combine(directory, "automated_truth_gate_test.json");
            var markdown = Path.Combine(directory, "automated_truth_gate_test.md");
            var stage = AutomatedTruthGateReportGenerator.FromM140EditorResult(M140GateResult.Passed, 3, Array.Empty<string>(), "m140.json", "m140.md", 1d);
            var report = AutomatedTruthGateReportGenerator.BuildReport(AutomatedTruthGateOptions.BuiltPlayerGate(json, markdown), new[] { stage });

            AutomatedTruthGateReportGenerator.WriteReport(report, json, markdown);

            Assert.IsTrue(File.Exists(json), json);
            Assert.IsTrue(File.Exists(markdown), markdown);
            StringAssert.Contains("Automated Truth Gate", File.ReadAllText(markdown));
        }

        private static M138CombatScaleStressReport PassingM138Report()
        {
            var scenarios = M138CombatScaleStressScenarioPolicy.StressManifest
                .Select(definition => new M138CombatScaleStressScenarioSummary
                {
                    scenarioId = definition.id,
                    displayName = definition.displayName,
                    targetEnemyCount = definition.targetEnemyCount,
                    expectedPeakEnemies = definition.targetEnemyCount,
                    peakActiveEnemies = definition.targetEnemyCount,
                    projectileHeavy = definition.projectileHeavy,
                    projectileActivePeak = definition.projectileHeavy
                        ? M138CombatScaleStressReportGenerator.ProjectileHeavyMinimumPeakProjectiles
                        : 0,
                    peakProjectiles = definition.projectileHeavy
                        ? M138CombatScaleStressReportGenerator.ProjectileHeavyMinimumPeakProjectiles
                        : 0,
                    bossPresent = definition.includesBoss,
                    observedBoss = definition.includesBoss,
                    bossFullLodObserved = definition.includesBoss,
                    passed = true,
                    failures = Array.Empty<string>()
                })
                .ToArray();

            return new M138CombatScaleStressReport
            {
                lockId = "M138",
                passed = true,
                scenarioCount = scenarios.Length,
                failures = Array.Empty<string>(),
                scenarios = scenarios
            };
        }

        private static M139LongRunSoakReport PassingM139Report()
        {
            var scenarios = M139LongRunSoakReportGenerator.ScenarioIds
                .Select(id => new M139LongRunSoakScenarioSummary
                {
                    scenarioId = id,
                    displayName = id,
                    roomTraversalsCompleted = 4,
                    branchRuntimeCacheHitRate = 1f,
                    passed = true,
                    failures = Array.Empty<string>()
                })
                .ToArray();

            return new M139LongRunSoakReport
            {
                lockId = "M139",
                passed = true,
                scenarioCount = scenarios.Length,
                failures = Array.Empty<string>(),
                scenarios = scenarios
            };
        }
    }
}
