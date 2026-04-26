using System.Collections.Generic;
using System.Linq;

namespace Hollow.Combat
{
    public sealed class CombatDiagnosticsModel
    {
        private readonly Dictionary<EnemyArchetypeId, int> activeEnemyCounts = new();
        private readonly Dictionary<ProjectileDespawnReason, int> projectileDespawnCounts = new();

        public IReadOnlyDictionary<EnemyArchetypeId, int> ActiveEnemyCounts => activeEnemyCounts;

        public IReadOnlyDictionary<ProjectileDespawnReason, int> ProjectileDespawnCounts => projectileDespawnCounts;

        public void SetEnemyCounts(IEnumerable<EnemyRuntimeController> enemies)
        {
            activeEnemyCounts.Clear();
            foreach (var group in enemies.Where(enemy => enemy != null && enemy.IsAlive).GroupBy(enemy => enemy.ArchetypeId))
            {
                activeEnemyCounts[group.Key] = group.Count();
            }
        }

        public void RecordProjectileDespawn(ProjectileDespawnReason reason)
        {
            projectileDespawnCounts.TryGetValue(reason, out var count);
            projectileDespawnCounts[reason] = count + 1;
        }

        public int ProjectileDespawnsFor(ProjectileDespawnReason reason)
        {
            return projectileDespawnCounts.TryGetValue(reason, out var count) ? count : 0;
        }

        public string EnemySummary()
        {
            return activeEnemyCounts.Count == 0
                ? "None"
                : string.Join("  ", activeEnemyCounts.OrderBy(pair => pair.Key).Select(pair => $"{pair.Key}:{pair.Value}"));
        }

        public string ProjectileSummary()
        {
            return projectileDespawnCounts.Count == 0
                ? "Shots:0"
                : string.Join("  ", projectileDespawnCounts.OrderBy(pair => pair.Key).Select(pair => $"{pair.Key}:{pair.Value}"));
        }
    }
}
