using System.Collections.Generic;
using System.Linq;

namespace Hollow.Combat
{
    public sealed class RoomCombatEncounterContext
    {
        public RoomCombatEncounterContext(string encounterId, IEnumerable<string> enemySpawnKinds)
            : this(encounterId, enemySpawnKinds, 0, 0, 0)
        {
        }

        public RoomCombatEncounterContext(string encounterId, IEnumerable<string> enemySpawnKinds, int worldIndex, int difficultyBand, int directorPressure)
            : this(encounterId, enemySpawnKinds, worldIndex, difficultyBand, directorPressure, string.Empty, string.Empty, 0, string.Empty)
        {
        }

        public RoomCombatEncounterContext(
            string encounterId,
            IEnumerable<string> enemySpawnKinds,
            int worldIndex,
            int difficultyBand,
            int directorPressure,
            string bossId,
            string bossArenaId,
            int bossWorldBand,
            string bossPhaseState)
        {
            EncounterId = string.IsNullOrWhiteSpace(encounterId) ? string.Empty : encounterId;
            EnemySpawnKinds = enemySpawnKinds?
                .Where(kind => !string.IsNullOrWhiteSpace(kind))
                .ToArray() ?? System.Array.Empty<string>();
            WorldIndex = worldIndex;
            DifficultyBand = difficultyBand;
            DirectorPressure = directorPressure;
            BossId = string.IsNullOrWhiteSpace(bossId) ? string.Empty : bossId;
            BossArenaId = string.IsNullOrWhiteSpace(bossArenaId) ? string.Empty : bossArenaId;
            BossWorldBand = bossWorldBand;
            BossPhaseState = string.IsNullOrWhiteSpace(bossPhaseState) ? string.Empty : bossPhaseState;
        }

        public static RoomCombatEncounterContext Empty { get; } = new(string.Empty, System.Array.Empty<string>());

        public string EncounterId { get; }

        public IReadOnlyList<string> EnemySpawnKinds { get; }

        public int WorldIndex { get; }

        public int DifficultyBand { get; }

        public int DirectorPressure { get; }

        public string BossId { get; }

        public string BossArenaId { get; }

        public int BossWorldBand { get; }

        public string BossPhaseState { get; }

        public bool HasAssignedSpawns => EnemySpawnKinds.Count > 0;

        public bool HasBossAssignment => !string.IsNullOrWhiteSpace(BossId);

        public string DirectorDebugLine => WorldIndex > 0 && !string.IsNullOrWhiteSpace(EncounterId)
            ? $"Director: W{WorldIndex} B{DifficultyBand} | {EncounterId}{(HasBossAssignment ? $" | Boss {BossId}" : string.Empty)}"
            : "Director: --";
    }
}
