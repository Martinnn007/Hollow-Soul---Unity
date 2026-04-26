using System.Collections.Generic;
using System.Linq;
using Hollow.Persistence;

namespace Hollow.Branches
{
    public sealed class EncounterPlan
    {
        private readonly Dictionary<string, RoomEncounterAssignment> assignments;

        public EncounterPlan(IEnumerable<RoomEncounterAssignment> nextAssignments)
        {
            assignments = nextAssignments?
                .Where(assignment => assignment != null && !string.IsNullOrWhiteSpace(assignment.RoomId))
                .GroupBy(assignment => assignment.RoomId)
                .ToDictionary(group => group.Key, group => group.First()) ?? new Dictionary<string, RoomEncounterAssignment>();
        }

        public static EncounterPlan Empty { get; } = new(System.Array.Empty<RoomEncounterAssignment>());

        public IReadOnlyCollection<RoomEncounterAssignment> Assignments => assignments.Values;

        public bool TryResolve(string roomId, out RoomEncounterAssignment assignment)
        {
            return assignments.TryGetValue(roomId ?? string.Empty, out assignment);
        }

        public List<RoomEncounterSaveState> ToSaveState()
        {
            return assignments.Values
                .OrderBy(assignment => assignment.RoomId)
                .Select(assignment => new RoomEncounterSaveState
                {
                    roomId = assignment.RoomId,
                    encounterId = assignment.EncounterId,
                    enemySpawnKinds = assignment.EnemySpawnKinds.ToList()
                })
                .ToList();
        }

        public static EncounterPlan FromSaveState(IEnumerable<RoomEncounterSaveState> saveState)
        {
            return new EncounterPlan((saveState ?? System.Array.Empty<RoomEncounterSaveState>())
                .Select(state => new RoomEncounterAssignment(state.roomId, state.encounterId, state.enemySpawnKinds)));
        }
    }
}
