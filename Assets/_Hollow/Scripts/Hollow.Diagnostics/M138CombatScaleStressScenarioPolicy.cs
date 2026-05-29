using System;
using System.Linq;

namespace Hollow.Diagnostics
{
    [Serializable]
    public sealed class M138CombatScaleStressScenarioDefinition
    {
        public string id;
        public string displayName;
        public string intent;
        public int targetEnemyCount;
        public bool projectileHeavy;
        public bool includesBoss;
        public bool includesAdds;
        public float warmupSeconds;
        public float sampleSeconds;
    }

    public static class M138CombatScaleStressScenarioPolicy
    {
        public const string LockId = "M138";
        public const int WindowsComfortTargetFrameRate = 60;
        public const float FixedWarmupSeconds = 3f;
        public const float FixedSampleSeconds = 30f;

        public static M138CombatScaleStressScenarioDefinition[] StressManifest => new[]
        {
            Scenario("enemy_stress_10", "10 Enemy Stress Room", "Small crowded fight baseline for AI LOD and NavMesh budget validation.", 10),
            Scenario("enemy_stress_20", "20 Enemy Stress Room", "Mid-scale crowded fight for staggered AI thinking and repath distribution.", 20),
            Scenario("enemy_stress_30", "30 Enemy Stress Room", "Windows 60 FPS comfort target gate for crowded combat scale.", 30),
            Scenario("projectile_heavy_room", "Projectile Heavy Room", "Ranged-heavy room with dense projectile simulation and reduced background AI work.", 18, projectileHeavy: true),
            Scenario("boss_plus_adds", "Boss Plus Adds", "Boss responsiveness with lower-priority add AI and navigation degradation.", 12, includesBoss: true, includesAdds: true),
            Scenario("boss_plus_projectile_adds", "Boss Plus Projectile Adds", "Boss fight with ranged adds, projectile pressure, and staggered NavMesh repaths.", 16, projectileHeavy: true, includesBoss: true, includesAdds: true)
        };

        public static bool ValidateStressManifest(out string detail)
        {
            var scenarios = StressManifest;
            var duplicate = scenarios
                .GroupBy(scenario => scenario.id)
                .FirstOrDefault(group => group.Count() > 1);
            if (duplicate != null)
            {
                detail = $"Duplicate M138 stress scenario id `{duplicate.Key}`.";
                return false;
            }

            var missingTiming = scenarios.FirstOrDefault(scenario =>
                Math.Abs(scenario.warmupSeconds - FixedWarmupSeconds) > 0.001f ||
                Math.Abs(scenario.sampleSeconds - FixedSampleSeconds) > 0.001f);
            if (missingTiming != null)
            {
                detail = $"M138 stress scenario `{missingTiming.id}` does not use the fixed warmup/sample window.";
                return false;
            }

            var hasEnemyScale = scenarios.Any(scenario => scenario.id == "enemy_stress_10" && scenario.targetEnemyCount == 10) &&
                scenarios.Any(scenario => scenario.id == "enemy_stress_20" && scenario.targetEnemyCount == 20) &&
                scenarios.Any(scenario => scenario.id == "enemy_stress_30" && scenario.targetEnemyCount == 30);
            var hasProjectileAndBoss = scenarios.Any(scenario => scenario.projectileHeavy) &&
                scenarios.Any(scenario => scenario.includesBoss && scenario.includesAdds);
            if (!hasEnemyScale || !hasProjectileAndBoss)
            {
                detail = "M138 stress manifest must cover 10/20/30 enemies, projectile-heavy combat, and boss plus adds.";
                return false;
            }

            detail = $"M138 stress manifest contains {scenarios.Length} deterministic combat-scale scenarios.";
            return scenarios.Length == 6;
        }

        private static M138CombatScaleStressScenarioDefinition Scenario(
            string id,
            string displayName,
            string intent,
            int targetEnemyCount,
            bool projectileHeavy = false,
            bool includesBoss = false,
            bool includesAdds = false)
        {
            return new M138CombatScaleStressScenarioDefinition
            {
                id = id,
                displayName = displayName,
                intent = intent,
                targetEnemyCount = targetEnemyCount,
                projectileHeavy = projectileHeavy,
                includesBoss = includesBoss,
                includesAdds = includesAdds,
                warmupSeconds = FixedWarmupSeconds,
                sampleSeconds = FixedSampleSeconds
            };
        }
    }
}
