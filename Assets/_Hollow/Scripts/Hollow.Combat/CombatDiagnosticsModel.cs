using System.Collections.Generic;
using System.Text;

namespace Hollow.Combat
{
    public sealed class CombatDiagnosticsModel
    {
        private readonly Dictionary<EnemyArchetypeId, int> activeEnemyCounts = new();
        private readonly Dictionary<ProjectileDespawnReason, int> projectileDespawnCounts = new();
        private readonly StringBuilder summaryBuilder = new();
        private string cachedEnemySummary = "None";
        private string cachedProjectileSummary = "Shots:0";
        private bool enemySummaryDirty = true;
        private bool projectileSummaryDirty = true;

        public IReadOnlyDictionary<EnemyArchetypeId, int> ActiveEnemyCounts => activeEnemyCounts;

        public IReadOnlyDictionary<ProjectileDespawnReason, int> ProjectileDespawnCounts => projectileDespawnCounts;

        public void SetEnemyCounts(IEnumerable<EnemyRuntimeController> enemies)
        {
            activeEnemyCounts.Clear();
            if (enemies != null)
            {
                foreach (var enemy in enemies)
                {
                    if (enemy == null || !enemy.IsAlive)
                    {
                        continue;
                    }

                    activeEnemyCounts.TryGetValue(enemy.ArchetypeId, out var count);
                    activeEnemyCounts[enemy.ArchetypeId] = count + 1;
                }
            }

            enemySummaryDirty = true;
        }

        public void RecordProjectileDespawn(ProjectileDespawnReason reason)
        {
            projectileDespawnCounts.TryGetValue(reason, out var count);
            projectileDespawnCounts[reason] = count + 1;
            projectileSummaryDirty = true;
        }

        public int ProjectileDespawnsFor(ProjectileDespawnReason reason)
        {
            return projectileDespawnCounts.TryGetValue(reason, out var count) ? count : 0;
        }

        public string EnemySummary()
        {
            if (!enemySummaryDirty)
            {
                return cachedEnemySummary;
            }

            cachedEnemySummary = BuildSummary(activeEnemyCounts, "None");
            enemySummaryDirty = false;
            return cachedEnemySummary;
        }

        public string ProjectileSummary()
        {
            if (!projectileSummaryDirty)
            {
                return cachedProjectileSummary;
            }

            cachedProjectileSummary = BuildSummary(projectileDespawnCounts, "Shots:0");
            projectileSummaryDirty = false;
            return cachedProjectileSummary;
        }

        private string BuildSummary<TKey>(Dictionary<TKey, int> counts, string empty)
        {
            if (counts.Count == 0)
            {
                return empty;
            }

            summaryBuilder.Clear();
            foreach (var pair in counts)
            {
                if (summaryBuilder.Length > 0)
                {
                    summaryBuilder.Append("  ");
                }

                summaryBuilder.Append(pair.Key);
                summaryBuilder.Append(':');
                summaryBuilder.Append(pair.Value);
            }

            return summaryBuilder.ToString();
        }
    }
}
