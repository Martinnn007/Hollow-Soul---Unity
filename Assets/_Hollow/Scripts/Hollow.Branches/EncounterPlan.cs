using System.Collections.Generic;
using System.Linq;
using Hollow.Combat;
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
            return ToSaveState(null);
        }

        public List<RoomEncounterSaveState> ToSaveState(EnemyCatalog enemyCatalog)
        {
            return ToSaveState(enemyCatalog, string.Empty, null, null);
        }

        public List<RoomEncounterSaveState> ToSaveState(
            EnemyCatalog enemyCatalog,
            string liveRoomId,
            IReadOnlyList<int> liveEnemyIntelligenceLevels,
            IReadOnlyList<string> liveEnemyDispositions)
        {
            return assignments.Values
                .OrderBy(assignment => assignment.RoomId)
                .Select(assignment =>
                {
                    var hasLiveSnapshot = !string.IsNullOrWhiteSpace(liveRoomId) &&
                        assignment.RoomId == liveRoomId &&
                        liveEnemyIntelligenceLevels != null &&
                        liveEnemyDispositions != null &&
                        liveEnemyIntelligenceLevels.Count == assignment.EnemySpawnKinds.Count &&
                        liveEnemyDispositions.Count == assignment.EnemySpawnKinds.Count;
                    var intelligence = hasLiveSnapshot
                        ? liveEnemyIntelligenceLevels.Select(level => (int)EnemyIntelligenceLevelExtensions.Clamp(level)).ToList()
                        : ResolveIntelligenceSnapshot(assignment, enemyCatalog);
                    var dispositions = hasLiveSnapshot
                        ? liveEnemyDispositions.Select(disposition => EnemyInstinctDispositionExtensions.FromSaveString(disposition, EnemyInstinctDisposition.Predator).ToSaveString()).ToList()
                        : ResolveDispositionSnapshot(assignment, enemyCatalog);
                    return new RoomEncounterSaveState
                    {
                        roomId = assignment.RoomId,
                        encounterId = assignment.EncounterId,
                        enemySpawnKinds = assignment.EnemySpawnKinds.ToList(),
                        enemyIntelligenceLevels = intelligence,
                        enemyDispositions = dispositions,
                        worldIndex = assignment.WorldIndex,
                        difficultyBand = assignment.DifficultyBand,
                        directorPressure = assignment.DirectorPressure,
                        bossId = assignment.BossId,
                        bossArenaId = assignment.BossArenaId,
                        bossWorldBand = assignment.BossWorldBand,
                        bossPhaseState = assignment.BossPhaseState
                    };
                })
                .ToList();
        }

        public static EncounterPlan FromSaveState(IEnumerable<RoomEncounterSaveState> saveState)
        {
            return new EncounterPlan((saveState ?? System.Array.Empty<RoomEncounterSaveState>())
                .Select(state => new RoomEncounterAssignment(
                    state.roomId,
                    state.encounterId,
                    state.enemySpawnKinds,
                    state.worldIndex,
                    state.difficultyBand,
                    state.directorPressure,
                    state.bossId,
                    state.bossArenaId,
                    state.bossWorldBand,
                    state.bossPhaseState,
                    state.enemyIntelligenceLevels,
                    state.enemyDispositions)));
        }

        private static List<int> ResolveIntelligenceSnapshot(RoomEncounterAssignment assignment, EnemyCatalog enemyCatalog)
        {
            if (assignment.EnemyIntelligenceLevels.Count == assignment.EnemySpawnKinds.Count)
            {
                return assignment.EnemyIntelligenceLevels
                    .Select(level => (int)EnemyIntelligenceLevelExtensions.Clamp(level))
                    .ToList();
            }

            var catalog = enemyCatalog != null ? enemyCatalog : EnemyCatalog.CreateRuntimeDefault();
            return assignment.EnemySpawnKinds
                .Select(spawnKind => (int)(catalog.Resolve(spawnKind)?.Intelligence ?? EnemyIntelligenceLevel.Simple))
                .ToList();
        }

        private static List<string> ResolveDispositionSnapshot(RoomEncounterAssignment assignment, EnemyCatalog enemyCatalog)
        {
            if (assignment.EnemyDispositions.Count == assignment.EnemySpawnKinds.Count)
            {
                return assignment.EnemyDispositions
                    .Select(disposition => EnemyInstinctDispositionExtensions.FromSaveString(disposition, EnemyInstinctDisposition.Predator).ToSaveString())
                    .ToList();
            }

            var catalog = enemyCatalog != null ? enemyCatalog : EnemyCatalog.CreateRuntimeDefault();
            return assignment.EnemySpawnKinds
                .Select(spawnKind => (catalog.Resolve(spawnKind)?.Disposition ?? EnemyInstinctDisposition.Predator).ToSaveString())
                .ToList();
        }
    }
}
