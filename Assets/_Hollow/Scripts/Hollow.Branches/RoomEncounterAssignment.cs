using System.Collections.Generic;
using System.Linq;

namespace Hollow.Branches
{
    public sealed class RoomEncounterAssignment
    {
        public RoomEncounterAssignment(string roomId, string encounterId, IEnumerable<string> enemySpawnKinds)
            : this(roomId, encounterId, enemySpawnKinds, 0, 0, 0)
        {
        }

        public RoomEncounterAssignment(string roomId, string encounterId, IEnumerable<string> enemySpawnKinds, int worldIndex, int difficultyBand, int directorPressure)
            : this(roomId, encounterId, enemySpawnKinds, worldIndex, difficultyBand, directorPressure, string.Empty, string.Empty, 0, string.Empty)
        {
        }

        public RoomEncounterAssignment(
            string roomId,
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
            RoomId = string.IsNullOrWhiteSpace(roomId) ? string.Empty : roomId;
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

        public string RoomId { get; }

        public string EncounterId { get; }

        public IReadOnlyList<string> EnemySpawnKinds { get; }

        public int WorldIndex { get; }

        public int DifficultyBand { get; }

        public int DirectorPressure { get; }

        public string BossId { get; }

        public string BossArenaId { get; }

        public int BossWorldBand { get; }

        public string BossPhaseState { get; }
    }
}
