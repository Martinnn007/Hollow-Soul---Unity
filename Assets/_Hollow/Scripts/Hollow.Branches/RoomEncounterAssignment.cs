using System.Collections.Generic;
using System.Linq;

namespace Hollow.Branches
{
    public sealed class RoomEncounterAssignment
    {
        public RoomEncounterAssignment(string roomId, string encounterId, IEnumerable<string> enemySpawnKinds)
        {
            RoomId = string.IsNullOrWhiteSpace(roomId) ? string.Empty : roomId;
            EncounterId = string.IsNullOrWhiteSpace(encounterId) ? string.Empty : encounterId;
            EnemySpawnKinds = enemySpawnKinds?
                .Where(kind => !string.IsNullOrWhiteSpace(kind))
                .ToArray() ?? System.Array.Empty<string>();
        }

        public string RoomId { get; }

        public string EncounterId { get; }

        public IReadOnlyList<string> EnemySpawnKinds { get; }
    }
}
