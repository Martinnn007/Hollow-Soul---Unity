using System;
using System.Collections.Generic;
using System.Linq;

namespace Hollow.Combat
{
    public sealed class RoomWaveEncounterPlan
    {
        private const string FallbackSpawnKind = "spawnEnemyNormal";
        private static readonly int[] DefaultWaveCounts = { 2, 3, 4 };

        private RoomWaveEncounterPlan(string encounterId, IReadOnlyList<IReadOnlyList<string>> waves)
        {
            EncounterId = string.IsNullOrWhiteSpace(encounterId) ? "wave_room" : encounterId;
            Waves = waves ?? Array.Empty<IReadOnlyList<string>>();
        }

        public static RoomWaveEncounterPlan Empty { get; } = new("wave_room", Array.Empty<IReadOnlyList<string>>());

        public string EncounterId { get; }

        public IReadOnlyList<IReadOnlyList<string>> Waves { get; }

        public int TotalWaves => Waves.Count;

        public bool IsActive => Waves.Count > 0;

        public static RoomWaveEncounterPlan Create(RoomCombatEncounterContext encounterContext)
        {
            var sourceKinds = encounterContext?.EnemySpawnKinds ?? Array.Empty<string>();
            return Create(encounterContext?.EncounterId, sourceKinds);
        }

        public static RoomWaveEncounterPlan Create(string encounterId, IEnumerable<string> enemySpawnKinds)
        {
            var sourceKinds = NormalizeSourceKinds(enemySpawnKinds);
            var expanded = ExpandToWaveBudget(sourceKinds, DefaultWaveCounts.Sum());
            var waves = new List<IReadOnlyList<string>>(DefaultWaveCounts.Length);
            var cursor = 0;
            foreach (var waveCount in DefaultWaveCounts)
            {
                waves.Add(expanded.Skip(cursor).Take(waveCount).ToArray());
                cursor += waveCount;
            }

            return new RoomWaveEncounterPlan(encounterId, waves);
        }

        public bool TryCreateContextForWave(int waveIndex, RoomCombatEncounterContext sourceContext, out RoomCombatEncounterContext waveContext)
        {
            waveContext = RoomCombatEncounterContext.Empty;
            if (waveIndex < 0 || waveIndex >= Waves.Count)
            {
                return false;
            }

            var source = sourceContext ?? RoomCombatEncounterContext.Empty;
            waveContext = new RoomCombatEncounterContext(
                $"{EncounterId}:wave_{waveIndex + 1:00}",
                Waves[waveIndex],
                source.WorldIndex,
                source.DifficultyBand,
                source.DirectorPressure,
                source.BossId,
                source.BossArenaId,
                source.BossWorldBand,
                source.BossPhaseState);
            return true;
        }

        public string StatusTextForWave(int waveIndex)
        {
            return IsActive && waveIndex >= 0 && waveIndex < Waves.Count
                ? $"Wave {waveIndex + 1}/{Waves.Count}"
                : string.Empty;
        }

        private static string[] NormalizeSourceKinds(IEnumerable<string> enemySpawnKinds)
        {
            var source = (enemySpawnKinds ?? Array.Empty<string>())
                .Where(kind => !string.IsNullOrWhiteSpace(kind))
                .ToArray();
            return source.Length > 0 ? source : new[] { FallbackSpawnKind };
        }

        private static string[] ExpandToWaveBudget(IReadOnlyList<string> sourceKinds, int targetCount)
        {
            var expanded = new string[Math.Max(0, targetCount)];
            for (var index = 0; index < expanded.Length; index++)
            {
                expanded[index] = sourceKinds[index % sourceKinds.Count];
            }

            return expanded;
        }
    }
}
