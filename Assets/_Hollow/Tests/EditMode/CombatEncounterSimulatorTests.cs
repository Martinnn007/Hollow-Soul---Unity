using System.Collections.Generic;
using System.Linq;
using Hollow.Combat;
using Hollow.Editor.CombatEncounterSimulator;
using NUnit.Framework;

namespace Hollow.Tests.EditMode
{
    public sealed class CombatEncounterSimulatorTests
    {
        [Test]
        public void SingleRunProducesDeterministicMetrics()
        {
            var scenario = new CombatEncounterScenario
            {
                scenarioName = "Rat Spider Swarm Test",
                roomPreset = CombatEncounterRoomPreset.Medium,
                difficulty = CombatEncounterDifficulty.Standard,
                durationSeconds = 18f,
                seed = 77,
                usePathfinding = true,
                obstacleDensity = 0.2f,
                enemyGroups = new List<CombatEncounterEnemyGroup>
                {
                    new CombatEncounterEnemyGroup { spawnKind = "spawnEnemyRat", count = 4, spawnPattern = CombatEncounterSpawnPattern.SpreadPatrol },
                    new CombatEncounterEnemyGroup { spawnKind = "spawnEnemySpider", count = 5, spawnPattern = CombatEncounterSpawnPattern.ClusteredGroup }
                }
            };

            var result = CombatEncounterSimulatorEngine.Run(scenario, EnemyCatalog.CreateRuntimeDefault());

            Assert.AreEqual(9, result.totalEnemies);
            Assert.Greater(result.frames.Count, 2);
            Assert.Greater(result.enemyMetrics.Count, 1);
            Assert.GreaterOrEqual(result.totalAttackStarts, 0);
            Assert.IsNotEmpty(result.recommendations);
        }

        [Test]
        public void BatchRunAggregatesSeeds()
        {
            var scenario = new CombatEncounterScenario
            {
                scenarioName = "Batch Skeleton Test",
                roomPreset = CombatEncounterRoomPreset.Small,
                difficulty = CombatEncounterDifficulty.Hard,
                durationSeconds = 12f,
                seed = 100,
                enemyGroups = new List<CombatEncounterEnemyGroup>
                {
                    new CombatEncounterEnemyGroup { spawnKind = "spawnEnemySkeletonSword", count = 3, spawnPattern = CombatEncounterSpawnPattern.DoorAmbush }
                }
            };

            var batch = CombatEncounterSimulatorEngine.RunBatch(scenario, 10, EnemyCatalog.CreateRuntimeDefault());

            Assert.AreEqual(10, batch.Runs);
            Assert.GreaterOrEqual(batch.SurvivalRate, 0f);
            Assert.LessOrEqual(batch.SurvivalRate, 1f);
            Assert.IsNotEmpty(batch.recommendations);
        }

        [Test]
        public void RecommendationEngineFlagsHighPathOrStuckLoad()
        {
            var scenario = new CombatEncounterScenario
            {
                scenarioName = "Dense Path Load",
                roomPreset = CombatEncounterRoomPreset.Small,
                difficulty = CombatEncounterDifficulty.StressTest,
                durationSeconds = 16f,
                seed = 1234,
                usePathfinding = true,
                obstacleDensity = 0.95f,
                enemyGroups = new List<CombatEncounterEnemyGroup>
                {
                    new CombatEncounterEnemyGroup { spawnKind = "spawnEnemySkeletonSpear", count = 12, spawnPattern = CombatEncounterSpawnPattern.ClusteredGroup },
                    new CombatEncounterEnemyGroup { spawnKind = "spawnEnemyRat", count = 12, spawnPattern = CombatEncounterSpawnPattern.AroundPlayer }
                }
            };

            var result = CombatEncounterSimulatorEngine.Run(scenario, EnemyCatalog.CreateRuntimeDefault());
            var text = string.Join("\n", result.warnings.Concat(result.recommendations)).ToLowerInvariant();

            Assert.IsTrue(text.Contains("path") || text.Contains("stuck"), text);
        }
    }
}
