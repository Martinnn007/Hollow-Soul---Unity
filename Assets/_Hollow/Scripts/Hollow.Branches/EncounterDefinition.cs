using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Hollow.Branches
{
    public sealed class EncounterDefinition : ScriptableObject
    {
        [SerializeField] private string encounterId = "encounter_intro";
        [SerializeField] private string displayName = "Intro Encounter";
        [SerializeField] private BranchRoomRole roomRole = BranchRoomRole.Combat;
        [SerializeField] private int minDifficultyBand;
        [SerializeField] private int maxDifficultyBand = 99;
        [SerializeField] private int minFootprintCells = 1;
        [SerializeField] private int maxFootprintCells = 99;
        [SerializeField] private int weight = 1;
        [SerializeField] private List<EncounterSpawnEntry> spawns = new();

        public string EncounterId => encounterId;

        public string DisplayName => displayName;

        public BranchRoomRole RoomRole => roomRole;

        public int Weight => Mathf.Max(1, weight);

        public int MinDifficultyBand => minDifficultyBand;

        public int MaxDifficultyBand => maxDifficultyBand;

        public int MinFootprintCells => minFootprintCells;

        public int MaxFootprintCells => maxFootprintCells;

        public IReadOnlyList<EncounterSpawnEntry> Spawns => spawns;

        public void Configure(
            string nextEncounterId,
            string nextDisplayName,
            BranchRoomRole nextRoomRole,
            int nextMinDifficultyBand,
            int nextMaxDifficultyBand,
            int nextMinFootprintCells,
            int nextMaxFootprintCells,
            int nextWeight,
            IEnumerable<EncounterSpawnEntry> nextSpawns)
        {
            encounterId = string.IsNullOrWhiteSpace(nextEncounterId) ? "encounter" : nextEncounterId;
            displayName = string.IsNullOrWhiteSpace(nextDisplayName) ? encounterId : nextDisplayName;
            roomRole = nextRoomRole;
            minDifficultyBand = Mathf.Max(0, nextMinDifficultyBand);
            maxDifficultyBand = Mathf.Max(minDifficultyBand, nextMaxDifficultyBand);
            minFootprintCells = Mathf.Max(1, nextMinFootprintCells);
            maxFootprintCells = Mathf.Max(minFootprintCells, nextMaxFootprintCells);
            weight = Mathf.Max(1, nextWeight);
            spawns = nextSpawns?.Where(spawn => spawn != null).ToList() ?? new List<EncounterSpawnEntry>();
        }

        public bool Supports(BranchRoomRole role, int difficultyBand, int footprintCells)
        {
            return roomRole == role &&
                   difficultyBand >= minDifficultyBand &&
                   difficultyBand <= maxDifficultyBand &&
                   footprintCells >= minFootprintCells &&
                   footprintCells <= maxFootprintCells &&
                   spawns.Count > 0;
        }

        public IReadOnlyList<string> ExpandSpawnKinds()
        {
            var kinds = new List<string>();
            foreach (var spawn in spawns)
            {
                var count = Mathf.Max(1, spawn.count);
                for (var index = 0; index < count; index++)
                {
                    kinds.Add(string.IsNullOrWhiteSpace(spawn.spawnKind) ? "spawnEnemyNormal" : spawn.spawnKind);
                }
            }

            return kinds;
        }
    }
}
