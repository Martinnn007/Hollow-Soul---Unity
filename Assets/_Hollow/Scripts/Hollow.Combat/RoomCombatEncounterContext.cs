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
        {
            EncounterId = string.IsNullOrWhiteSpace(encounterId) ? string.Empty : encounterId;
            EnemySpawnKinds = enemySpawnKinds?
                .Where(kind => !string.IsNullOrWhiteSpace(kind))
                .ToArray() ?? System.Array.Empty<string>();
            WorldIndex = worldIndex;
            DifficultyBand = difficultyBand;
            DirectorPressure = directorPressure;
        }

        public static RoomCombatEncounterContext Empty { get; } = new(string.Empty, System.Array.Empty<string>());

        public string EncounterId { get; }

        public IReadOnlyList<string> EnemySpawnKinds { get; }

        public int WorldIndex { get; }

        public int DifficultyBand { get; }

        public int DirectorPressure { get; }

        public bool HasAssignedSpawns => EnemySpawnKinds.Count > 0;

        public string DirectorDebugLine => WorldIndex > 0 && !string.IsNullOrWhiteSpace(EncounterId)
            ? $"Director: W{WorldIndex} B{DifficultyBand} | {EncounterId}"
            : "Director: --";
    }
}
