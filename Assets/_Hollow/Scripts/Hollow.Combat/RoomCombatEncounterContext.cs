using System.Collections.Generic;
using System.Linq;

namespace Hollow.Combat
{
    public sealed class RoomCombatEncounterContext
    {
        public RoomCombatEncounterContext(string encounterId, IEnumerable<string> enemySpawnKinds)
        {
            EncounterId = string.IsNullOrWhiteSpace(encounterId) ? string.Empty : encounterId;
            EnemySpawnKinds = enemySpawnKinds?
                .Where(kind => !string.IsNullOrWhiteSpace(kind))
                .ToArray() ?? System.Array.Empty<string>();
        }

        public static RoomCombatEncounterContext Empty { get; } = new(string.Empty, System.Array.Empty<string>());

        public string EncounterId { get; }

        public IReadOnlyList<string> EnemySpawnKinds { get; }

        public bool HasAssignedSpawns => EnemySpawnKinds.Count > 0;
    }
}
